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

        private static void SetName(MassShaderEditor MSE, MassShaderEditor.SettingType type, string name) {
            if (MSE.IsDebug.Value) Log.Info($"Property name set: {name.Replace(':', ' ').Replace('*', ' ').Trim()}");
            MSE.tab = type;
            MSE.setName = name.Replace(':', ' ').Replace('*', ' ').Trim();
            MSE.setNameInput = MSE.setName;
        }

        private static void SetFilter(MassShaderEditor MSE, string filter) {
            if (MSE.IsDebug.Value) Log.Info($"Shader to be filtered: {filter}");
            MSE.filter = filter.Trim();
            MSE.filterInput = MSE.filter;
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
            private static void FillNameOnClick(GameObject go, RowItemType rowType, string propertyName, string materialName, string rendererName) {
                var MSE = (Object.FindObjectOfType(typeof(MassShaderEditor)) as MassShaderEditor);
                if (rowType == RowItemType.FloatProperty) SetName(MSE, MassShaderEditor.SettingType.Float, propertyName);
                if (rowType == RowItemType.ColorProperty) SetName(MSE, MassShaderEditor.SettingType.Color, propertyName);
                if (rowType == RowItemType.Shader)
                    foreach (var rend in GetRendererList(go))
                        foreach (var mat in GetMaterials(go, rend))
                            if (mat.NameFormatted() == materialName)
                                SetFilter(MSE, mat.shader.NameFormatted());
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
                    if (MSE.IsDebug.Value && content != null) Log.Info("Found content!");

                    var txtList = content.GetComponentsInChildren<Text>(true).ToList();
                    if (MSE.IsDebug.Value) Log.Info($"Found {txtList.Count} text components...");

                    var accepted = new List<string> { "FloatLabel", "ColorLabel", "ShaderLabel" };
                    txtList = txtList.FindAll(x => accepted.Contains(x.gameObject.name));
                    if (MSE.IsDebug.Value) Log.Info($"Found {txtList.Count} labels!");

                    foreach (var txt in txtList) {
                        var btn = txt.gameObject.AddComponent<Button>();
                        switch (txt.gameObject.name) {
                            case "FloatLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Float, txt.text));
                                break;
                            case "ColorLabel":
                                btn.onClick.AddListener(() => SetName(MSE, MassShaderEditor.SettingType.Color, txt.text));
                                break;
                            case "ShaderLabel":
                                GameObject shaderDropdown = null;
                                foreach (Transform tr in txt.transform.parent.GetComponentsInChildren<Transform>(true)) if (tr.name == "ShaderDropdown") { shaderDropdown = tr.gameObject; break; }
                                btn.onClick.AddListener(() => SetFilter(MSE, shaderDropdown.GetComponentInChildren<Text>().text));
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
