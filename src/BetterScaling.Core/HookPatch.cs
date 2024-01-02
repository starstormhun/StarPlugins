using System;
using System.Collections.Generic;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BetterScaling.Koikatu {
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
            [HarmonyPatch(typeof(Studio.AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            private static void AddObjectFolderLoad(ref OCIFolder __result) {
                __result.guideObject.enableScale = true;
                __result.guideObject.isActiveFunc += new GuideObject.IsActiveFunc(__result.OnSelect);
            }
        }
    }
}
