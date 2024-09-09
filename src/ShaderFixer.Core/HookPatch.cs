using static MaterialEditorAPI.MaterialAPI;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ShaderFixer {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        public static string NameFormatted(this Material go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();

        private static class Hooks {
            private static Harmony _harmony;
            private static bool isCoroutine = false;
            private static int fixCount = 0;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Detect shader and texture, apply flat normal if missing
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MaterialEditorAPI.MaterialAPI), "SetShader")]
            private static void MaterialAPIAfterSetShader(GameObject gameObject, string materialName, string shaderName) {
                if (shaderName == null) return;

				Texture2D newTex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
				newTex.SetPixel(0, 0, new Color(1f, 0.5f, 0.5f, 0.5f));
				newTex.Apply();

                List<string> filters = ShaderFixer.Filter.Value.Split(',').ToList();

                bool found = false;
                foreach (string filter in filters) {
                    if (filter.Length == 0) continue;
                    if (filter.ToLower().Trim()[0] == '-') {
                        if (shaderName.ToLower().Contains(filter.ToLower().Replace("-", "").Trim())) return;
                    } else {
                        if (shaderName.ToLower().Contains(filter.ToLower().Trim())) {
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) return;

                foreach (var rend in GetRendererList(gameObject))
                    foreach (var mat in GetMaterials(gameObject, rend).Where((m) => (m.NameFormatted() == materialName) && m.HasProperty("_NormalMap"))) {
                        if (!isCoroutine && ShaderFixer.Instance != null) ShaderFixer.Instance.StartCoroutine(LogCoroutine());
                        fixCount++;
                        mat.SetTexture("_NormalMap", newTex);
                    }

                IEnumerator LogCoroutine() {
                    isCoroutine = true;
                    Log.Debug("Found matching shaders, setting NormalMaps...");
                    yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                    Log.Debug($"KKUSS default normal maps placed: {fixCount}");
                    fixCount = 0;
                    isCoroutine = false;
                }
            }
        }
    }
}
