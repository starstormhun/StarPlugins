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

                List<string> filters = ShaderFixer.Filter.Value.Split(',').Select(f => f.ToLower().Trim()).ToList();

                bool found = false;
                foreach (string filter in filters) {
                    if (filter.Length == 0) continue;
                    if (filter[0] == '-') {
                        if (shaderName.ToLower().Contains(filter.Replace("-", "").Trim())) return;
                    } else {
                        if (shaderName.ToLower().Contains(filter)) {
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) return;

                List<string> props = ShaderFixer.Properties.Value.Split(',').Select(p => p.Trim().TrimStart('_')).ToList();

                foreach (var rend in GetRendererList(gameObject)) { 
                    foreach (var mat in GetMaterials(gameObject, rend).Where((m) => (m.NameFormatted() == materialName) )) {
                        foreach (var prop in props) {
                            if (mat.HasProperty("_" + prop)) {
                                if (!isCoroutine && ShaderFixer.Instance != null) ShaderFixer.Instance.StartCoroutine(LogCoroutine());
                                fixCount++;
                                mat.SetTexture("_" + prop, newTex);
                            }
                        }
                    }
                }
                IEnumerator LogCoroutine() {
                    isCoroutine = true;
                    Log.Info("Found matching shader and property, fixing...");
                    yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                    Log.Info($"Default normal maps fixed: {fixCount}");
                    fixCount = 0;
                    isCoroutine = false;
                }
            }
        }
    }
}
