using System;
using System.Collections.Generic;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace LightToggler.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        private static class Hooks {
            private static Harmony _harmony;
            
            // Setup functionality on launch / enable
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable functionality when disabled in settings
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Makes OnObjectVisibilityToggled fire for folders
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int)})]
            private static void AddObjectFolderLoad(ref OCIFolder __result) {
                __result.treeNodeObject.onVisible += new TreeNodeObject.OnVisibleFunc(__result.OnVisible);
            }

            // Makes OnObjectVisiblityToggled fire for lights, and enables the visibility toggle button
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.AddObjectLight), "Load", new Type[] { typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            private static void AddObjectLightLoad(ref OCILight __result) {
                __result.treeNodeObject.onVisible += new TreeNodeObject.OnVisibleFunc(__result.OnVisible);
                __result.treeNodeObject.enableVisible = true;
                __result.treeNodeObject.visible = (__result.objectInfo as OILightInfo).enable;
            }

            // When an treenodeobject is toggled the lights' OnVisible now triggers, and we use that to toggle the Enabled state
            [HarmonyPostfix]
            [HarmonyPatch(typeof(OCILight),"OnVisible")]
            private static void OCILightOnVisible(ref OCILight __instance) {
                __instance.SetEnable(__instance.objectLight.GetComponentInChildren<Light>().enabled);
            }

            // When the light is toggled visible, it should only turn on if all its parents are visible as well
            [HarmonyPostfix]
            [HarmonyPatch(typeof(OCILight), "SetEnable")]
            private static void OCILightSetEnable(ref OCILight __instance, bool __result, bool _value) {
                if (_value && __result) {
                    TreeNodeObject currentNode = __instance.treeNodeObject;
                    bool toggleTo = true;
                    while(currentNode.parent != null) {
                        if (!currentNode.parent.visible) {
                            toggleTo = false;
                            __instance.treeNodeObject.visible = true;
                            break;
                        }
                        currentNode = currentNode.parent;
                    }
                    __instance.objectLight.GetComponentInChildren<Light>().enabled = toggleTo;
                }
            }

            // When the light is read the display property is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MPLightCtrl), "ociLight", MethodType.Getter)]
            private static void MpLightCtrlOCILightValueGet(ref MPLightCtrl __instance) {
                if (__instance.m_OCILight != null) {
                    if (__instance.m_OCILight.treeNodeObject.visible != __instance.toggleVisible.isOn) {
                        if (__instance.toggleVisible.isOn != (__instance.m_OCILight as OCILight).objectLight.GetComponentInChildren<Light>().enabled) {
                            __instance.UpdateInfo();
                        } else {
                            __instance.m_OCILight.treeNodeObject.visible = __instance.toggleVisible.isOn;
#if DEBUG
                            Debug.Log("MpLightCtrlOCILightValueGet toggling node visibility to: " + __instance.toggleVisible.isOn.ToString());
#endif
                        }
                    }
                }
            }


            // Makes the toggle on the UI update its value according to the visibility of the node instead of the status of the light
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MPLightCtrl), "UpdateInfo")]
            private static void MpLightCtrlUpdateInfo(ref MPLightCtrl __instance) {
                __instance.toggleVisible.isOn = __instance.m_OCILight.treeNodeObject.visible;
#if DEBUG
                Debug.Log("UpdateInfo ran!");
#endif
            }

            // After saving the scene with all lights ON, this updates all lights to their intended state
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), "SaveScene")]
            private static void StudioSaveScene(Studio.Studio __instance) {
                foreach (KeyValuePair<int,ObjectCtrlInfo> keyValuePair in __instance.dicObjectCtrl) {
                    Light[] lights;
                    lights = keyValuePair.Value.GetObject().GetComponentsInChildren<Light>();
                    if (lights.Length > 0) {
                        foreach (Light light in lights) {
                            light.enabled = keyValuePair.Value.treeNodeObject.visible && keyValuePair.Value.treeNodeObject.m_ButtonVisible.interactable;
                        }
                    }
                }
            }
        }
    }
}
