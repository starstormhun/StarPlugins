using static MaterialEditorAPI.MaterialAPI;
using HarmonyLib;
using UnityEngine;
using System.Collections;

namespace KKUSSFix.Sunshine {
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
            private static int kkussCount = 0;

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
                if (shaderName != null)
                    if (shaderName.ToLower().Contains("kkuss"))
                        foreach (var rend in GetRendererList(gameObject))
                            foreach (var mat in GetMaterials(gameObject, rend))
                                if (mat != null && mat.HasProperty("_NormalMap"))
                                    if (mat.NameFormatted() == materialName) {
                                        if (!isCoroutine && KKUSSSunshineFix.Instance != null) KKUSSSunshineFix.Instance.StartCoroutine(LogCoroutine());
                                        kkussCount++;
                                        Texture2D newTex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
                                        newTex.SetPixel(0, 0, new Color(1f, 0.5f, 0.5f, 0.5f));
                                        newTex.Apply();
                                        mat.SetTexture("_NormalMap", newTex);
                                    }

                IEnumerator LogCoroutine() {
                    isCoroutine = true;
                    Log.Debug("Found KKUSS, setting NormalMaps...");
                    yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                    Log.Debug($"KKUSS default normal maps placed: {kkussCount}");
                    kkussCount = 0;
                    isCoroutine = false;
                }
            }
        }
    }
}
