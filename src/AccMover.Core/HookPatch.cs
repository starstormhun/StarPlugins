using ChaCustom;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BepInEx;

namespace AccMover {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
            Conditionals.Setup();
        }

        public static class Hooks {
            private static Harmony _harmony;
            internal static bool disableTransferFuncs = false;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Disable a bunch of functions for batch accessory transfers
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), "AssignCoordinate", new[] { typeof(ChaFileDefine.CoordinateType) })]
            private static bool ChaControlBeforeAssignCoordinate() {
                return !disableTransferFuncs;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), "Reload")]
            private static bool ChaControlBeforeReload() {
                return !disableTransferFuncs;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessoryChange), "CalculateUI")]
            private static bool CvsAccessoryChangeBeforeCalculateUI() {
                return !disableTransferFuncs;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomAcsChangeSlot), "UpdateSlotNames")]
            private static bool CustomAcsChangeSlotBeforeUpdateSlotNames() {
                return !disableTransferFuncs;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomBase), "SetUpdateCvsAccessory")]
            private static bool CustomBaseBeforeSetUpdateCvsAccessory() {
                return !disableTransferFuncs;
            }
        }
    }
}
