using static MaterialEditorAPI.MaterialAPI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using ChaCustom;

namespace MassShaderEditor.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.AlwaysHooks.SetupHooks();
            HookPatch.ConditionalHooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.AlwaysHooks.UnregisterHooks();
            HookPatch.ConditionalHooks.UnregisterHooks();
        }

        private static void SetName(MassShaderEditor MSE, MassShaderEditor.SettingType type, string name) {
            if (MSE.IsDebug.Value) MSE.Log($"Property name set: {name.Replace(':', ' ').Replace('*', ' ').Trim()}");
            MSE.tab = type;
            MSE.setName = name.Replace(':', ' ').Replace('*', ' ').Trim();
            MSE.setNameInput = MSE.setName;
        }

        private static void SetFilter(MassShaderEditor MSE, string filter, int type) {
            if (MSE.IsDebug.Value) MSE.Log($"Shader name to be autofilled: {filter}");
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && type == 2) {
                MSE.tab = MassShaderEditor.SettingType.Shader;
                MSE.setShader = filter.Trim();
                MSE.setShaderInput = filter.Trim();
            } else {
                MSE.currentFilter = type;
                MSE.filters[type] = filter.Trim();
                MSE.filterInput = MSE.filters[type];
            }
        }

        internal static class AlwaysHooks {
            private static Harmony _harmony;
            internal static bool buttonsMade = false;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.AlwaysHooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Sets up buttons on labels in the ME interface for autofilling the property name
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MaterialEditorAPI.MaterialEditorUI), "PopulateList")]
            private static void AddClickableNames() {
                if (!buttonsMade) {
                    var MSE = (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor);
                    GameObject content = GameObject.Find("BepInEx_Manager");
                    foreach (Transform child in content.transform.GetComponentsInChildren<Transform>())
                        if (child.name == "MaterialEditorWindow") {content = child.gameObject; break; }
                    if (MSE.IsDebug.Value && content != null) MSE.Log("Found content!");

                    var txtList = content.GetComponentsInChildren<Text>(true).ToList();
                    if (MSE.IsDebug.Value) MSE.Log($"Found {txtList.Count} text components...");

                    var accepted = new List<string> {
                        "FloatLabel", "ColorLabel", "TextureLabel", "RendererText", "MaterialText",
                        "ShaderLabel", "ShaderRenderQueueLabel", "OffsetScaleLabel", "OffsetXText",
                        "KeywordLabel"
                    };
                    txtList = txtList.FindAll(x => accepted.Contains(x.gameObject.name));
                    if (MSE.IsDebug.Value) MSE.Log($"Found {txtList.Count} labels!");

                    foreach (var txt in txtList) {
                        var btn = txt.gameObject.AddComponent<Button>();
                        switch (txt.gameObject.name) {
                            case "FloatLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Float, txt.text));
                                break;
                            case "ColorLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Color, txt.text));
                                break;
                            case "TextureLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Texture, txt.text));
                                break;
                            case "RendererText":
                                btn.onClick.AddListener(() => SetFilter(MSE, txt.text, 0));
                                break;
                            case "MaterialText":
                                btn.onClick.AddListener(() => SetFilter(MSE, txt.text, 1));
                                break;
                            case "ShaderLabel":
                                GameObject shaderDropdown = null;
                                foreach (Transform tr in txt.transform.parent.GetComponentsInChildren<Transform>(true)) if (tr.name == "ShaderDropdown") { shaderDropdown = tr.gameObject; break; }
                                btn.onClick.AddListener(() => SetFilter(MSE, shaderDropdown.GetComponentInChildren<Text>().text, 2));
                                break;
                            case "ShaderRenderQueueLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Float, "Render Queue"));
                                break;
                            case "OffsetScaleLabel":
                            case "OffsetXText":
                                Transform panel = txt.transform.parent;
                                btn.onClick.AddListener(() => {
                                    var values = new float[] { 0, 0, 0, 0 };
                                    foreach (InputField child in panel.GetComponentsInChildren<InputField>()) {
                                        switch (child.name) {
                                            case "OffsetXInput":
                                                values[0] = Studio.Utility.StringToFloat(child.text); break;
                                            case "OffsetYInput":
                                                values[1] = Studio.Utility.StringToFloat(child.text); break;
                                            case "ScaleXInput":
                                                values[2] = Studio.Utility.StringToFloat(child.text); break;
                                            case "ScaleYInput":
                                                values[3] = Studio.Utility.StringToFloat(child.text); break;
                                        }
                                    }
                                    MSE.tab = MassShaderEditor.SettingType.Texture;
                                    MSE.setTex.offset = new float[] { values[0], values[1] };
                                    MSE.setTex.scale = new float[] { values[2], values[3] };
                                });
                                break;
                            case "KeywordLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Float, txt.text));
                                break;
                        }
                    }
                    buttonsMade = true;
                }
            }

            // Turns off the color picker updates if the picker gets reset
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.ColorPalette), "Setup")]
            private static void ColorPaletteAfterSetup(string winTitle) {
                var MSE = (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor);
                if (winTitle != MSE.pickerName) MSE.isPicker = false;
            }

            // Turns off the color picker updates if the picker gets closed
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.ColorPalette), "Close")]
            private static void ColorPaletteAfterClose() {
                (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).isPicker = false;
            }

            // Keeps track of the current tab while in maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomChangeMainMenu), "ChangeWindowSetting")]
            private static void CustomChangeMainMenuAfterChangeWindowSetting(int no) {
                (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor).makerTabID = no;
            }
        }

        private static class ConditionalHooks {
            private static Harmony _harmony;

            public static void SetupHooks() {
                var MSE = (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor);

                if (MSE.IsDebug.Value) MSE.Log("Attempting to patch timeline buttons...");
                if (typeof(MaterialEditorAPI.MaterialEditorUI).GetMethod("SelectInterpolableButtonOnClick", BindingFlags.NonPublic | BindingFlags.Instance) != null) {
                    _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.ConditionalHooks), null);
                    if (MSE.IsDebug.Value) MSE.Log("Patched timeline buttons!");
                }
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Fills appropriate fields in plugin UI when clicking the ME - Timeline interpolable button
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MaterialEditorAPI.MaterialEditorUI), "SelectInterpolableButtonOnClick")]
            private static void FillFieldsOnClick(GameObject go, RowItemType rowType, string propertyName, string materialName) {
                var MSE = (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor);
                if (rowType == RowItemType.FloatProperty) SetName(MSE, MassShaderEditor.SettingType.Float, propertyName);
                if (rowType == RowItemType.ColorProperty) SetName(MSE, MassShaderEditor.SettingType.Color, propertyName);
                if (rowType == RowItemType.TextureProperty) SetName(MSE, MassShaderEditor.SettingType.Texture, propertyName);
                if (rowType == RowItemType.Shader)
                    foreach (var rend in GetRendererList(go))
                        foreach (var mat in GetMaterials(go, rend))
                            if (mat.NameFormatted() == materialName)
                                SetFilter(MSE, mat.shader.NameFormatted(), 2);
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
