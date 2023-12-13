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
                __result.treeNodeObject.onVisible = new TreeNodeObject.OnVisibleFunc(__result.OnVisible);
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

            // When the light is read the display property is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MPLightCtrl), "ociLight", MethodType.Getter)]
            private static void MpLightCtrlOCILightValueGet(ref MPLightCtrl __instance) {
                __instance.UpdateInfo();
            }

            // The old UI button was causing countless sync problems so I disabled it
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MPLightCtrl), "Awake")]
            private static void MPLightCtrlAwake(ref MPLightCtrl __instance) {
                __instance.toggleVisible.interactable = false;
            }
        }
    }
}
