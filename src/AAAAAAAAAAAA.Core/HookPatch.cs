using Studio;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace Performancer {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            Hooks.UnregisterHooks();
        }

        internal static class Hooks {
            private static Harmony _harmony;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }
        }
    }
}
