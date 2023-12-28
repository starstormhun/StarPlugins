using BepInEx;
using BepInEx.Configuration;
using KK_Plugins.MaterialEditor;
using static KK_Plugins.MaterialEditor.MaterialEditorCharaController;
using static MaterialEditorAPI.MaterialAPI;
using Studio;
using ChaCustom;
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
        public const string Version = "0.2.0." + BuildNumber.Version;

        // General
        public ConfigEntry<float> UIScale { get; private set; }
        public ConfigEntry<bool> ShowTooltips { get; private set; }

        // Studio options
        public ConfigEntry<bool> DiveFolders { get; private set; }
        public ConfigEntry<bool> DiveItems { get; private set; }
        public ConfigEntry<bool> AffectCharacters { get; private set; }
        public ConfigEntry<bool> AffectChaBody { get; private set; }
        public ConfigEntry<bool> AffectChaHair { get; private set; }
        public ConfigEntry<bool> AffectChaClothes { get; private set; }
        public ConfigEntry<bool> AffectChaAccs { get; private set; }

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

        internal int makerTabID = 0;
        private CustomChangeMainMenu makerMenu;

        private void Awake() {
            UIScale = Config.Bind("General", "UI Scale", 1.5f, new ConfigDescription("Can also be set via the built-in settings panel", new AcceptableValueRange<float>(1f, maxScale), null));
            UIScale.SettingChanged += (x, y) => scaled = false;
            ShowTooltips = Config.Bind("General", "Show tooltips", true, "");

            DiveFolders = Config.Bind("Studio", "Dive folders", false, new ConfigDescription(diveFoldersText, null, null));
            DiveItems = Config.Bind("Studio", "Dive items", false, new ConfigDescription(diveItemsText, null, null));
            AffectCharacters = Config.Bind("Studio", "Affect characters", false, new ConfigDescription(affectCharactersText, null, new KKAPI.Utilities.ConfigurationManagerAttributes { Order = 10 }));
            AffectChaBody = Config.Bind("Studio", "Affect character bodies", false, new ConfigDescription(affectChaBodyText, null, null));
            AffectChaHair = Config.Bind("Studio", "Affect character hair", false, new ConfigDescription(affectChaHairText, null, null));
            AffectChaClothes = Config.Bind("Studio", "Affect character clothes", false, new ConfigDescription(affectChaClothesText, null, null));
            AffectChaAccs = Config.Bind("Studio", "Affect character accessories", false, new ConfigDescription(affectChaAccsText, null, null));

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
                makerMenu = (FindObjectOfType(typeof(CustomChangeMainMenu)) as CustomChangeMainMenu);
                controller = MEStudio.GetSceneController();
                makerTabID = 0;
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
                    if (setName != "") {
                        setReset = false;
                        if (tab == SettingType.Color) SetSelectedProperties(setCol);
                        if (tab == SettingType.Float) SetSelectedProperties(setVal);
                    } else ShowMessage("You need to set a property name to edit!");
                }
                if (ResetSelectedHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = true;
                        SetSelectedProperties(0f);
                    } else ShowMessage("You need to set a property name to edit!");
                }
                if (SetAllHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = false;
                        if (DisableWarning.Value) {
                            if (tab == SettingType.Color) SetAllProperties(setCol);
                            if (tab == SettingType.Float) SetAllProperties(setVal);
                        } else showWarning = true;
                    } else ShowMessage("You need to set a property name to edit!");
                }
                if (ResetAllHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = true;
                        if (DisableWarning.Value) SetAllProperties(0f);
                        else showWarning = true;
                    } else ShowMessage("You need to set a property name to edit!");
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

            // Set / reset logic
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
                        windowRect = GUILayout.Window(587, windowRect, WindowFunction, $"Mass Shader Editor v{Version}", newSkin.window, GUILayout.MaxWidth(defaultSize[2] * UIScale.Value));
                        DrawTooltip(tooltip[0]);
                        KKAPI.Utilities.IMGUIUtils.EatInputInRect(windowRect);
                        Redraw(1587, windowRect, redrawNum);

                        helpRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        setRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        infoRect.position = windowRect.position + new Vector2(0, windowRect.size.y + 3);

                        if (isHelp) {
                            helpRect = GUILayout.Window(588, helpRect, HelpFunction, "How to use?", newSkin.window, GUILayout.MaxWidth(defaultSize[2]*UIScale.Value));
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(helpRect);
                            Redraw(1588, helpRect, redrawNum);
                        }
                        if (isSetting) {
                            setRect = GUILayout.Window(588, setRect, SettingFunction, "Settings ۞", newSkin.window, GUILayout.MaxWidth(defaultSize[2] * UIScale.Value));
                            DrawTooltip(tooltip[0]);
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(setRect);
                            Redraw(1588, setRect, redrawNum);
                        }
                        if (showMessage) {
                            var boxStyle = new GUIStyle(newSkin.box) {
                                fontSize = 1
                            };
                            infoRect = GUILayout.Window(589, infoRect, InfoFunction, "", boxStyle);
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(infoRect);
                            Redraw(1589, infoRect, redrawNum, true);
                        }
                    }
                    if (showWarning) {
                        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                        GUI.Box(screenRect, "");
                        warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                        warnRect = GUILayout.Window(590, warnRect, WarnFunction, "", newSkin.window);
                        KKAPI.Utilities.IMGUIUtils.EatInputInRect(screenRect);
                        Redraw(1590, warnRect, redrawNum);
                    }
                } else {
                    Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                    GUI.Box(screenRect, "");
                    warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                    warnRect = GUILayout.Window(591, warnRect, IntroFunction, "", newSkin.window);
                    KKAPI.Utilities.IMGUIUtils.EatInputInRect(screenRect);
                    Redraw(1591, warnRect, redrawNum);
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
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                foreach (ObjectCtrlInfo oci in _ociList) {
                    if (oci is OCIItem item) {
                        SetItemProperties(controller, item, _value);
                    } else if (oci is OCIChar ociChar && AffectCharacters.Value) {
                        if (IsDebug.Value) Log.Info($"Looking into character: {ociChar.treeNodeObject.textName}");
                        var ctrl = KKAPI.Studio.StudioObjectExtensions.GetChaControl(ociChar);
                        if (AffectChaBody.Value) SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value);
                        if (AffectChaHair.Value) for(int i = 0; i<ctrl.objHair.Length; i++) SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Hair, _value);
                        if (AffectChaClothes.Value) for (int i = 0; i < ctrl.objClothes.Length; i++) SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Clothing, _value);
                        for (int i = 0; i < ctrl.objAccessory.Length; i++)
                            SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Accessory, _value, x => (x.Contains("hair") && AffectChaHair.Value) || (!x.Contains("hair") && AffectChaAccs.Value));
                    }
                }
                MEStudio.Instance.RefreshUI();
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                // TODO
                //MEMaker.Instance.RefreshUI();
            }
            
        }

        private void SetItemProperties<T>(SceneController ctrl, OCIItem item, T _value) {
            if (IsDebug.Value) Log.Info($"Looking into {item.NameFormatted()}...");
            foreach (var rend in GetRendererList(item.objectItem)) {
                //if (IsDebug.Value) Log.Info($"Got renderer: {rend.NameFormatted()}");
                foreach (var mat in GetMaterials(item.objectItem, rend)) {
                    //if (IsDebug.Value) Log.Info($"Got material: {mat.NameFormatted()}");
                    if (mat.shader.NameFormatted().Contains(filter))
                        if (mat.HasProperty("_" + setName)) {
                            try {
                                if (setReset) {
                                    ctrl.RemoveMaterialFloatProperty(item.objectInfo.dicKey, mat, setName);
                                    ctrl.RemoveMaterialColorProperty(item.objectInfo.dicKey, mat, setName);
                                    if (IsDebug.Value) Log.Info($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} reset!");
                                } else {
                                    if (_value is float floatval) ctrl.SetMaterialFloatProperty(item.objectInfo.dicKey, mat, setName, floatval);
                                    if (_value is Color colval) ctrl.SetMaterialColorProperty(item.objectInfo.dicKey, mat, setName, colval);
                                    if (IsDebug.Value) Log.Info($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {_value}!");
                                }
                            } catch (Exception e) {
                                Log.Error($"Unknown error during property value assignment of {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName}: {e}");
                            }
                        } else {
                            if (IsDebug.Value) Log.Info($"Material {item.NameFormatted()}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()} did not have the {setName} property...");
                        }
                }
            }
        }

        private void SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value) {
            SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, x => true);
        }

        private void SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<string> match) {
            var chaCtrl = ctrl.ChaControl;
            GameObject go;
            switch (type) {
                case ObjectType.Character:
                    go = ctrl.gameObject; break;
                case ObjectType.Hair:
                    go = chaCtrl.objHair[slot]; break;
                case ObjectType.Clothing:
                    go = chaCtrl.objClothes[slot]; break;
                case ObjectType.Accessory:
                    go = KKAPI.Maker.AccessoriesApi.GetAccessoryObject(chaCtrl, slot); break;
                default:
                    go = null; break;
            }
            foreach (var rend in GetRendererList(go)) {
                //if (IsDebug.Value) Log.Info($"Got renderer: {rend.NameFormatted()}");
                foreach (var mat in GetMaterials(go, rend)) {
                    //if (IsDebug.Value) Log.Info($"Got material: {mat.NameFormatted()}");
                    if (match(mat.shader.NameFormatted().ToLower()) && mat.shader.NameFormatted().Contains(filter))
                        if (mat.HasProperty("_" + setName)) {
                            try {
                                if (setReset) {
                                    ctrl.RemoveMaterialFloatProperty(slot, type, mat, setName, go);
                                    ctrl.RemoveMaterialColorProperty(slot, type, mat, setName, go);
                                    if (IsDebug.Value) Log.Info($"Property {ociChar.NameFormatted()}\\{mat.NameFormatted()}\\{setName} reset!");
                                } else {
                                    if (_value is float floatval) ctrl.SetMaterialFloatProperty(slot, type, mat, setName, floatval, go);
                                    if (_value is Color colval) ctrl.SetMaterialColorProperty(slot, type, mat, setName, colval, go);
                                    if (IsDebug.Value) Log.Info($"Property {ociChar.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {_value}!");
                                }
                            } catch (Exception e) {
                                Log.Error($"Unknown error during property value assignment of {ociChar.NameFormatted()}\\{mat.NameFormatted()}\\{setName}: {e}");
                            }
                        } else {
                            if (IsDebug.Value) Log.Info($"{ociChar.NameFormatted()}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()} did not have the {setName} property...");
                        }
                }
            }
        }
    }
}
