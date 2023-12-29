using static MaterialEditorAPI.MaterialAPI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using ChaCustom;

namespace MassShaderEditor.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        private static class Hooks {
            private static Harmony _harmony;
            private static bool buttonsMade = false;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Makes OnObjectVisibilityToggled fire for folders
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), "SetShader")]
            private static void MaterialAPIAfterSetShader(GameObject gameObject, string materialName, string shaderName) {
                
            }
        }
    }
}
