using TMPro;
using ChaCustom;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AccMover {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
        }

        public static class Hooks {
            private static Harmony _harmony;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // 
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessoryChange), "CopyAcs")]
            private static void KKMoreAccessoryParentsInterfaceAfterCreateInterface() {
            }
        }
    }
}
