using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
            [HarmonyPatch(typeof(MaterialEditorAPI.MaterialEditorUI), "SelectInterpolableButtonOnClick")]
            private static void FillNameOnClick(RowItemType rowType, string propertyName) {
                if (rowType == RowItemType.FloatProperty || rowType == RowItemType.ColorProperty)
                    (UnityEngine.Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).setName = propertyName;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MaterialEditorAPI.MaterialEditorUI), "PopulateList")]
            private static void AddClickableNames() {
                if (!buttonsMade) {
                    GameObject content = GameObject.Find("BepInEx_Manager");
                    foreach (Transform child in content.transform.GetComponentInChildren<Transform>()) {
                        if (child.name == "MaterialEditorWindow") {
                            content = child.gameObject;
                            break;
                        }
                    }
                    if ((Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).IsDebug.Value && content != null) Log.Info("Found content!");
                    var txtList = content.GetComponentsInChildren<Text>(true).ToList();
                    if ((Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).IsDebug.Value) Log.Info($"Found {txtList.Count} text components...");
                    txtList = txtList.FindAll(x => x.gameObject.name == "ColorLabel" || x.gameObject.name == "FloatLabel");
                    if ((Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).IsDebug.Value) Log.Info($"Found {txtList.Count} labels!");
                    foreach (var txt in txtList) {
                        var btn = txt.gameObject.AddComponent<Button>();
                        btn.onClick.AddListener(() => (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).setName = txt.text.Replace(':',' ').Trim());
                    }
                    buttonsMade = true;
                }
            }
        }

        public enum RowItemType {
            Renderer,
            RendererEnabled,
            RendererShadowCastingMode,
            RendererReceiveShadows,
            RendererRecalculateNormals,
            Material,
            Shader,
            ShaderRenderQueue,
            TextureProperty,
            TextureOffsetScale,
            ColorProperty,
            FloatProperty,
            KeywordProperty
        }
    }
}
