using BepInEx;
using BepInEx.Configuration;
using KK_Plugins.MaterialEditor;
using static KK_Plugins.MaterialEditor.MaterialEditorCharaController;
using static MaterialEditorAPI.MaterialAPI;
using Studio;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(MassShaderEditor.Koikatu.MassShaderEditor.Version)]

namespace MassShaderEditor.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "Mass Shader Editor", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public partial class MassShaderEditor : BaseUnityPlugin {
        public const string GUID = "starstorm.massshadereditor";
        public const string Version = "1.0.0." + BuildNumber.Version;

        private ConfigEntry<KeyboardShortcut> VisibleHotkey { get; set; }
        private ConfigEntry<float> UIScale { get; set; }

        private ConfigEntry<bool> IsDebug { get; set; }
        private ConfigEntry<bool> DisableWarning { get; set; }

        private Studio.Studio studio;
        private bool inited = false;
        private bool scaled = false;
        private SceneController controller;

        private void Awake() {
            VisibleHotkey = Config.Bind("General", "UI Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("The key used to toggle the plugin's UI",null,new KKAPI.Utilities.ConfigurationManagerAttributes{ Order = 10}));
            UIScale = Config.Bind("General", "UI Scale", 1f, new ConfigDescription("Can also be set via the built-in settings panel", new AcceptableValueRange<float>(1f, 3f), null));
            UIScale.SettingChanged += (x, y) => scaled = false;

            DisableWarning = Config.Bind("General", "Disable warning", false, new ConfigDescription("Disable the warning screen for the 'Set All' function.", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            IsDebug = Config.Bind("Debug", "Logging", false, new ConfigDescription("Enable verbose logging", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => {
                studio = Singleton<Studio.Studio>.Instance;
                controller = MEStudio.GetSceneController();
            };
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => {
                controller = MEStudio.GetSceneController();
            };

            Log.SetLogSource(Logger);
            if (IsDebug.Value) Log.Info("Awoken!");
        }

        private void Update() {
            if (VisibleHotkey.Value.IsDown())
                isShown = !isShown;
            if (!KKAPI.Maker.MakerAPI.InsideMaker && !KKAPI.Studio.StudioAPI.InsideStudio)
                isShown = false;
        }

        private void OnGUI() {
            if (Input.GetMouseButtonDown(1) && windowRect.Contains(Input.mousePosition.InvertScreenY()) && !showWarning) {
                setReset = true;
                if (IsDebug.Value) Log.Info($"RMB detected! setReset: {setReset}");
            }
            if (Input.GetMouseButtonDown(0) && windowRect.Contains(Input.mousePosition.InvertScreenY()) && !showWarning) {
                setReset = false;
                if (IsDebug.Value) Log.Info($"LMB detected! setReset: {setReset}");
            }

            if (!inited) {
                inited = true;
                InitUI();
            }
            if (!scaled) {
                scaled = true;
                ScaleUI(UIScale.Value);
            }
            if (isShown &&!showWarning) {
                windowRect = GUILayout.Window(587, windowRect, WindowFunction, "Mass Shader Editor", newSkin.window);
                KKAPI.Utilities.IMGUIUtils.EatInputInRect(windowRect);
                helpRect = new Rect(windowRect.position + new Vector2(windowRect.size.x+3, 0), windowRect.size);
                if (isHelp) helpRect.size = new Vector2(helpRect.size.x, newSkin.label.CalcHeight(new GUIContent(helpText[helpPage]),helpRect.size.x) + 2.5f*newSkin.window.padding.top);
                if (isHelp || isSetting) {
                    if (isHelp) helpRect = GUILayout.Window(588, helpRect, HelpFunction, "How to use?", newSkin.window);
                    else if (isSetting) helpRect = GUILayout.Window(588, helpRect, SettingFunction, "Settings ۞", newSkin.window);
                    KKAPI.Utilities.IMGUIUtils.EatInputInRect(helpRect);
                }
            }
            if (showWarning) {
                Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                GUI.Box(screenRect, "");
                warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                warnRect = GUILayout.Window(589, warnRect, WarnFunction, "", newSkin.window);
                KKAPI.Utilities.IMGUIUtils.EatInputInRect(screenRect);
            }
        }

        private void SetAllProperties<T>(T _value) {
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                if (IsDebug.Value) Log.Info($"{(setReset?"Res":"S")}etting ALL items' properties!");
                SetProperties(studio.dicObjectCtrl.Values.ToList(), _value);
            }
            else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                // TODO
            }
        }

        private void SetSelectedProperties<T>(T _value) {
            if (!(_value is float) && !(_value is Color)) return;
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                if (IsDebug.Value) Log.Info($"{(setReset ? "Res" : "S")}etting selected items' properties!");
                SetProperties(KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList(), _value);
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                // TODO
            }
        }

        private void SetProperties<T>(List<ObjectCtrlInfo> _ociList, T _value) {
            foreach (ObjectCtrlInfo oci in _ociList) {
                if (oci is OCIItem item) {
                    if (IsDebug.Value) Log.Info($"Looking into {item.treeNodeObject.transform.GetComponentInChildren<UnityEngine.UI.Text>().text}...");
                    foreach (var rend in GetRendererList(item.objectItem)) {
                        if (IsDebug.Value) Log.Info($"Got renderer: {rend.name}");
                        foreach (var mat in GetMaterials(item.objectItem, rend)) {
                            if (IsDebug.Value) Log.Info($"Got material: {mat.NameFormatted()}");
                            if (mat.HasProperty("_"+setName)) {
                                try {
                                    if (setReset) {
                                        if (_value is float) controller.RemoveMaterialFloatProperty(item.objectInfo.dicKey, mat, setName);
                                        if (_value is Color) controller.RemoveMaterialColorProperty(item.objectInfo.dicKey, mat, setName);
                                        if (IsDebug.Value) Log.Info($"Property {setName}({_value.GetType()}) reset!");
                                    } else {
                                        if (_value is float floatval) controller.SetMaterialFloatProperty(item.objectInfo.dicKey, mat, setName, floatval);
                                        if (_value is Color colval) controller.SetMaterialColorProperty(item.objectInfo.dicKey, mat, setName, colval);
                                        if (IsDebug.Value) Log.Info($"Property {setName} set to {_value}!");
                                    }
                                } catch (Exception e) {
                                    if (IsDebug.Value) Log.Info($"Unknown error during property value assignment: {e}");
                                }
                            } else {
                                if (IsDebug.Value) Log.Info($"Material {mat.name} did not have the property...");
                            }
                        }
                    }
                }
            }
        }
    }
}
