using System;
using System.Collections;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace LightToggler.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        private static class Hooks {
            public static void SetupHooks() {
                Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Makes OnObjectVisibilityToggled fire for folders
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int)})]
            private static void AddObjectFolderLoad(ref OCIFolder __result) {
                __result.treeNodeObject.onVisible += new TreeNodeObject.OnVisibleFunc(__result.OnVisible);
            }

            // Makes OnObjectVisiblityToggled fire for lights, and also enables parenting 
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.AddObjectLight), "Load", new Type[] { typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            private static void AddObjectLightLoad(ref OCILight __result) {
                __result.treeNodeObject.onVisible = new TreeNodeObject.OnVisibleFunc(__result.OnVisible);
                __result.treeNodeObject.enableAddChild = true;
                __result.treeNodeObject.enableVisible = true;
                __result.treeNodeObject.visible = (__result.objectInfo as OILightInfo).enable;
            }

            // When an object is toggled the lights' OnVisible now triggers, and we use that to toggle the Enabled state
            [HarmonyPostfix]
            [HarmonyPatch(typeof(OCILight),"OnVisible")]
            private static void OCILightOnVisible(ref OCILight __instance) {
                __instance.SetEnable(__instance.objectLight.GetComponent<Light>().enabled);
            }

            // When the light is toggled visible, it should only turn on if all its parents are visible as well
            [HarmonyPostfix]
            [HarmonyPatch(typeof(OCILight), "SetEnable")]
            private static void OCILightSetEnable(ref OCILight __instance, bool __result, bool _value) {
                if (_value && __result && __instance.light) {
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
                    __instance.light.enabled = toggleTo;
                }
            }

            // When the light is read the display property is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MPLightCtrl), "ociLight", MethodType.Getter)]
            private static void MpLightCtrlOCILightValueGet(ref MPLightCtrl __instance) {
                if (__instance.m_OCILight != null) {
                    if (__instance.m_OCILight.treeNodeObject.visible != __instance.toggleVisible.isOn) {
                        if (__instance.toggleVisible.isOn != (__instance.m_OCILight as OCILight).objectLight.GetComponent<Light>().enabled) {
                            __instance.UpdateInfo();
                        } else {
                            __instance.m_OCILight.treeNodeObject.visible = __instance.toggleVisible.isOn;
                        }
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MPLightCtrl), "UpdateInfo")]
            private static void MpLightCtrlUpdateInfo(ref MPLightCtrl __instance) {
                __instance.toggleVisible.isOn = __instance.m_OCILight.treeNodeObject.visible;
#if DEBUG
                Console.WriteLine("UpdateInfo ran!");
#endif
            }

        }
    }
}
