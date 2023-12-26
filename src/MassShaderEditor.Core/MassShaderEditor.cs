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
        public const string Version = "0.1.0." + BuildNumber.Version;

        // General
        public ConfigEntry<float> UIScale { get; private set; }
        public ConfigEntry<bool> DiveFolders { get; private set; }
        public ConfigEntry<bool> DiveItems { get; private set; }

        // Hotkeys
        public ConfigEntry<KeyboardShortcut> VisibleHotkey { get; private set; }
        public ConfigEntry<KeyboardShortcut> SetSelectedHotkey { get; private set; }
        public ConfigEntry<KeyboardShortcut> ResetSelectedHotkey { get; private set; }
        public ConfigEntry<KeyboardShortcut> SetAllHotkey { get; private set; }
        public ConfigEntry<KeyboardShortcut> ResetAllHotkey { get; private set; }

        // Advanced
        public ConfigEntry<bool> IsDebug { get; private set; }
        public ConfigEntry<bool> DisableWarning { get; private set; }
        private ConfigEntry<bool> IntroShown { get; set; }

        private Studio.Studio studio;
        private bool inited = false;
        private bool scaled = false;
        private SceneController controller;

        private void Awake() {
            UIScale = Config.Bind("General", "UI Scale", 1.5f, new ConfigDescription("Can also be set via the built-in settings panel", new AcceptableValueRange<float>(1f, maxScale), null));
            UIScale.SettingChanged += (x, y) => scaled = false;
            DiveFolders = Config.Bind("General", "Dive folders", false, new ConfigDescription(diveFoldersText, null, null));
            DiveItems = Config.Bind("General", "Dive items", false, new ConfigDescription(diveItemsText, null, null));

            VisibleHotkey = Config.Bind("Hotkeys", "UI Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("The key used to toggle the plugin's UI",null,new KKAPI.Utilities.ConfigurationManagerAttributes{ Order = 10}));
            SetSelectedHotkey = Config.Bind("Hotkeys", "Set Selected", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate left-clicking 'Set Selected'", null, null));
            ResetSelectedHotkey = Config.Bind("Hotkeys", "Reset Selected", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate right-clicking 'Set Selected'", null, null));
            SetAllHotkey = Config.Bind("Hotkeys", "Set ALL", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate left-clicking 'Set ALL'", null, null));
            ResetAllHotkey = Config.Bind("Hotkeys", "Reset ALL", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate right-clicking 'Set ALL'", null, null));

            DisableWarning = Config.Bind("Advanced", "Disable warning", false, new ConfigDescription("Disable the warning screen for the 'Set All' function.", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            IsDebug = Config.Bind("Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            IntroShown = Config.Bind("Advanced", "Intro Shown", false, new ConfigDescription("Whether the intro message has been shown already", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => {
                studio = Singleton<Studio.Studio>.Instance;
                controller = MEStudio.GetSceneController();
            };
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => {
                controller = MEStudio.GetSceneController();
            };
            HookPatch.Init();

            Log.SetLogSource(Logger);
            if (IsDebug.Value) Log.Info("Awoken!");
        }

        private void Update() {
            if (VisibleHotkey.Value.IsDown())
                isShown = !isShown;
            if (isShown) {
                if (SetSelectedHotkey.Value.IsDown()) {
                    setReset = false;
                    if (tab == SettingType.Color) SetSelectedProperties(setCol);
                    if (tab == SettingType.Float) SetSelectedProperties(setVal);
                }
                if (ResetSelectedHotkey.Value.IsDown()) {
                    setReset = true;
                    SetSelectedProperties(0f);
                }
                if (SetAllHotkey.Value.IsDown()) {
                    setReset = false;
                    if (DisableWarning.Value) {
                        if (tab == SettingType.Color) SetAllProperties(setCol);
                        if (tab == SettingType.Float) SetAllProperties(setVal);
                    } else showWarning = true;
                }
                if (ResetAllHotkey.Value.IsDown()) {
                    setReset = true;
                    if (DisableWarning.Value) SetAllProperties(0f);
                    else showWarning = true;
                }
            }
            if (showMessage && Time.time - messageTime >= messageDur) showMessage = false;

            if ((!KKAPI.Maker.MakerAPI.InsideMaker && !KKAPI.Studio.StudioAPI.InsideStudio) || Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Escape))
                isShown = false;
        }

        private void OnGUI() {
            if (!inited) {
                inited = true;
                InitUI();
                CalcSizes();
            }
            if (!scaled) {
                scaled = true;
                ScaleUI(UIScale.Value);
            }

            if (Input.GetMouseButtonDown(1) && windowRect.Contains(Input.mousePosition.InvertScreenY()) && !showWarning) {
                setReset = true;
                if (IsDebug.Value) Log.Info($"RMB detected! setReset: {setReset}");
            }
            if (Input.GetMouseButtonDown(0) && windowRect.Contains(Input.mousePosition.InvertScreenY()) && !showWarning) {
                setReset = false;
                if (IsDebug.Value) Log.Info($"LMB detected! setReset: {setReset}");
            }

            if (isShown) {
                if (IntroShown.Value) {
                    if (!showWarning) {
                        windowRect = GUILayout.Window(587, windowRect, WindowFunction, $"Mass Shader Editor v{Version}", newSkin.window);

                        KKAPI.Utilities.IMGUIUtils.EatInputInRect(windowRect);

                        helpRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        setRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        infoRect.position = windowRect.position + new Vector2(0, windowRect.size.y + 3);

                        if (isHelp) {
                            helpRect = GUILayout.Window(588, helpRect, HelpFunction, "How to use?", newSkin.window);
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(helpRect);
                        }
                        if (isSetting) {
                            setRect = GUILayout.Window(588, setRect, SettingFunction, "Settings ۞", newSkin.window);
                            DrawTooltip(tooltip[0]);
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(setRect);
                        }
                        if (showMessage) {
                            var boxStyle = new GUIStyle(newSkin.box);
                            boxStyle.fontSize = 1;
                            infoRect = GUILayout.Window(589, infoRect, InfoFunction, "", boxStyle);
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(infoRect);
                        }
                    }
                    if (showWarning) {
                        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                        GUI.Box(screenRect, "");
                        warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                        warnRect = GUILayout.Window(589, warnRect, WarnFunction, "", newSkin.window);
                        KKAPI.Utilities.IMGUIUtils.EatInputInRect(screenRect);
                    }
                } else {
                    Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                    GUI.Box(screenRect, "");
                    warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                    warnRect = GUILayout.Window(589, warnRect, IntroFunction, "", newSkin.window);
                    KKAPI.Utilities.IMGUIUtils.EatInputInRect(screenRect);
                }
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
                var ociList = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                var iterateList = new List<ObjectCtrlInfo>(ociList);
                if (IsDebug.Value) Log.Info("Checking for folders...");
                var diveList = new List<Type>();
                if (DiveFolders.Value) diveList.Add(typeof(OCIFolder));
                if (DiveItems.Value) diveList.Add(typeof(OCIItem));
                foreach (var oci in iterateList) {
                    if (diveList.Contains(oci.GetType())) {
                        if (IsDebug.Value) Log.Info($"Found diveable item: {oci.treeNodeObject.textName}");
                        oci.AddChildrenRecursive(ociList);
                    }
                }
                SetProperties(ociList, _value);
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
                                        controller.RemoveMaterialFloatProperty(item.objectInfo.dicKey, mat, setName);
                                        controller.RemoveMaterialColorProperty(item.objectInfo.dicKey, mat, setName);
                                        if (IsDebug.Value) Log.Info($"Property {setName}({_value.GetType()}) reset!");
                                    } else {
                                        if (_value is float floatval) controller.SetMaterialFloatProperty(item.objectInfo.dicKey, mat, setName, floatval);
                                        if (_value is Color colval) controller.SetMaterialColorProperty(item.objectInfo.dicKey, mat, setName, colval);
                                        if (IsDebug.Value) Log.Info($"Property {setName} set to {_value}!");
                                    }
                                } catch (Exception e) {
                                    Log.Error($"Unknown error during property value assignment: {e}");
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
