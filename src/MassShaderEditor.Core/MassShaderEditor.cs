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
using MaterialEditorAPI;
using KKAPI.Utilities;
using KK_Plugins;
using Illusion.Extensions;

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
        public const string Version = "1.1.3." + BuildNumber.Version;

        // General
        public ConfigEntry<float> UIScale { get; private set; }
        public ConfigEntry<bool> ShowTooltips { get; private set; }
        public ConfigEntry<bool> SaveTextures { get; private set; }

        // Studio options
        public ConfigEntry<bool> DiveFolders { get; private set; }
        public ConfigEntry<bool> DiveItems { get; private set; }
        public ConfigEntry<bool> AffectCharacters { get; private set; }
        public ConfigEntry<bool> AffectChaBody { get; private set; }
        public ConfigEntry<bool> AffectChaHair { get; private set; }
        public ConfigEntry<bool> AffectChaClothes { get; private set; }
        public ConfigEntry<bool> AffectChaAccs { get; private set; }

        // Maker options
        public ConfigEntry<bool> AffectMiscBodyParts { get; private set; }
        public ConfigEntry<bool> HairAccIsHair { get; private set; }

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

        // Data
        private ConfigEntry<string> FloatHistory { get; set; }
        private ConfigEntry<string> ColorHistory { get; set; }

        private Studio.Studio studio;
        private bool inited = false;
        private bool scaled = false;
        private SceneController controller;
        private System.Random rand = new System.Random();

        private List<string> shaders;

        internal int makerTabID = 0;
        private CustomChangeMainMenu makerMenu;

        private void Awake() {
            UIScale = Config.Bind("General", "UI Scale", 1.5f, new ConfigDescription("Can also be set via the built-in settings panel", new AcceptableValueRange<float>(1f, maxScale), null));
            UIScale.SettingChanged += (x, y) => scaled = false;
            ShowTooltips = Config.Bind("General", "Show tooltips", true, "");
            SaveTextures = Config.Bind("General", "Save textures", false, "Whether to save texture edit history to disk. May slow down the game when editing lots of big textures.");
            AffectMiscBodyParts = Config.Bind("General", "Affect misc body parts", false, new ConfigDescription(affectMiscBodyPartsText, null, null));

            DiveFolders = Config.Bind("Studio", "Dive folders", false, new ConfigDescription(diveFoldersText, null, null));
            DiveItems = Config.Bind("Studio", "Dive items", false, new ConfigDescription(diveItemsText, null, null));
            AffectCharacters = Config.Bind("Studio", "Affect characters", false, new ConfigDescription(affectCharactersText, null, new ConfigurationManagerAttributes { Order = 10 }));
            AffectChaBody = Config.Bind("Studio", "Affect character bodies", false, new ConfigDescription(affectChaBodyText, null, null));
            AffectChaHair = Config.Bind("Studio", "Affect character hair", false, new ConfigDescription(affectChaHairText, null, null));
            AffectChaClothes = Config.Bind("Studio", "Affect character clothes", false, new ConfigDescription(affectChaClothesText, null, null));
            AffectChaAccs = Config.Bind("Studio", "Affect character accessories", false, new ConfigDescription(affectChaAccsText, null, null));

            HairAccIsHair = Config.Bind("Maker", "Hair accs are hair", false, new ConfigDescription(hairAccIsHairText, null, null));

            VisibleHotkey = Config.Bind("Hotkeys", "UI Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("The key used to toggle the plugin's UI",null,new ConfigurationManagerAttributes{ Order = 10}));
            SetSelectedHotkey = Config.Bind("Hotkeys", "Modify Selected", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate left-clicking 'Modify Selected'", null, null));
            ResetSelectedHotkey = Config.Bind("Hotkeys", "Reset Selected", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate right-clicking 'Modify Selected'", null, null));
            SetAllHotkey = Config.Bind("Hotkeys", "Modify ALL", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate left-clicking 'Modify ALL'", null, null));
            ResetAllHotkey = Config.Bind("Hotkeys", "Reset ALL", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate right-clicking 'Modify ALL'", null, null));

            DisableWarning = Config.Bind("Advanced", "Disable warning", false, new ConfigDescription("Disable the warning screen for the 'Modify ALL' function.", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            IsDebug = Config.Bind("Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            IntroShown = Config.Bind("Advanced", "Intro Shown", false, new ConfigDescription("Whether the intro message has been shown already", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            FloatHistory = Config.Bind("Data", "Float History", "", new ConfigDescription("The 10 previously set property name/value pairings", null, new ConfigurationManagerAttributes { Browsable = false }));
            ColorHistory = Config.Bind("Data", "Color History", "", new ConfigDescription("The 10 previously set property name/color pairings", null, new ConfigurationManagerAttributes { Browsable = false }));

            KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => {
                studio = Singleton<Studio.Studio>.Instance;
                controller = MEStudio.GetSceneController();
                shaders = MaterialEditorPluginBase.XMLShaderProperties.Keys.Where(z => z != "default").Select(z => z.Trim()).ToList();

            };
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => {
                makerMenu = (FindObjectOfType(typeof(CustomChangeMainMenu)) as CustomChangeMainMenu);
                controller = MEStudio.GetSceneController();
                shaders = MaterialEditorPluginBase.XMLShaderProperties.Keys.Where(z => z != "default").Select(z => z.Trim()).ToList();
                makerTabID = 0;
            };

            ReadHistory();
            HookPatch.Init();

            if (IsDebug.Value) Log("Awoken!");
        }

        private void Update() {
            if (VisibleHotkey.Value.IsDown())
                IsShown = !IsShown;
            if (IsShown) {
                if (SetSelectedHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = false;
                        if (tab == SettingType.Color) SetSelectedProperties(setCol);
                        if (tab == SettingType.Float) SetSelectedProperties(setVal);
                        if (tab == SettingType.Texture) TrySetTexture(SetSelectedProperties);
                    } else ShowMessage("You need to set a property name to edit!");
                }
                if (ResetSelectedHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = true;
                        if (tab == SettingType.Texture) TrySetTexture(SetSelectedProperties);
                        else SetSelectedProperties(0f);
                    } else ShowMessage("You need to set a property name to edit!");
                }
                if (SetAllHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = false;
                        if (DisableWarning.Value) {
                            if (tab == SettingType.Color) SetAllProperties(setCol);
                            if (tab == SettingType.Float) SetAllProperties(setVal);
                            if (tab == SettingType.Texture) TrySetTexture(SetAllProperties);
                        } else showWarning = true;
                    } else ShowMessage("You need to set a property name to edit!");
                }
                if (ResetAllHotkey.Value.IsDown()) {
                    if (setName != "") {
                        setReset = true;
                        if (DisableWarning.Value)
                            if (tab == SettingType.Texture) TrySetTexture(SetAllProperties);
                            else SetAllProperties(0f);
                        else showWarning = true;
                    } else ShowMessage("You need to set a property name to edit!");
                }
            }
            if (showMessage && Time.time - messageTime >= messageDur) showMessage = false;
            if ((!KKAPI.Maker.MakerAPI.InsideMaker && !KKAPI.Studio.StudioAPI.InsideStudio) || Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Escape))
                IsShown = false;

            if (fileToOpen != "") {
                setTex.data = System.IO.File.ReadAllBytes(fileToOpen);
                Texture2D newTex = new Texture2D(1, 1);
                newTex.LoadImage(setTex.data);
                setTex.tex = newTex;

                fileToOpen = "";
            }
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
            if (Input.GetMouseButtonDown(1) && (windowRect.Contains(Input.mousePosition.InvertScreenY()) || setRect.Contains(Input.mousePosition.InvertScreenY())) && !showWarning) {
                setReset = true;
                if (IsDebug.Value) Log($"RMB detected! setReset: {setReset}");
            }
            if (Input.GetMouseButtonDown(0) && (windowRect.Contains(Input.mousePosition.InvertScreenY()) || setRect.Contains(Input.mousePosition.InvertScreenY())) && !showWarning) {
                setReset = false;
                if (IsDebug.Value) Log($"LMB detected! setReset: {setReset}");
            }
            if (shaderDrop > 0 && (Input.GetMouseButton(1) || Input.GetMouseButton(0)) && !shaderRect.Contains(Input.mousePosition.InvertScreenY())) {
                shaderDrop = 0;
            }
            if (historyDrop && (Input.GetMouseButton(1) || Input.GetMouseButton(0)) && !historyRect.Contains(Input.mousePosition.InvertScreenY())) {
                historyDrop = false;
            }

            if (IsShown) {
                if (IntroShown.Value) {
                    if (!showWarning) {
                        windowRect = GUILayout.Window(WinNum("main"), windowRect, WindowFunction, $"Mass Shader Editor v{Version}", newSkin.window, GUILayout.MaxWidth(defaultSize[2] * UIScale.Value));
                        DrawTooltip(tooltip[0]);
                        IMGUIUtils.EatInputInRect(windowRect);
                        Redraw(WinNum("main"), windowRect, redrawNum);

                        // Additional window rendering conditional calculations
                        bool isMixWindow = tab == SettingType.Color && setModeColor == 1;
                        bool isRandWindow = (new SettingType[] { SettingType.Float, SettingType.Color }).Contains(tab) && setRandom;

                        // Below main window
                        infoRect.position = windowRect.position + new Vector2(0, windowRect.size.y + 3);

                        // To the right of main window
                        helpRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        setRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);

                        // To the left of main window
                        texRect.position = windowRect.position - new Vector2(texRect.size.x + 3, 0);
                        mixRect.position = windowRect.position - new Vector2(mixRect.size.x + 3, 0);
                        mixRect.size = new Vector2(mixRect.size.x, windowRect.size.y);
                        randRect.position = windowRect.position - new Vector2(randRect.size.x + 3 + (isMixWindow ? mixRect.size.x + 3 : 0), 0);

                        if (isMixWindow) {
                            Vector2 oldPos = mixRect.position;
                            mixRect = GUILayout.Window(WinNum("mix"), mixRect, MixFunction, "", newSkin.box);
                            IMGUIUtils.EatInputInRect(mixRect);
                            Redraw(WinNum("mix"), mixRect, redrawNum-1, true);
                            if ((mixRect.position - oldPos) != Vector2.zero) {
                                windowRect.position += mixRect.position - oldPos;
                            }
                        }
                        if (isRandWindow) {
                            Vector2 oldPos = randRect.position;
                            randRect = GUILayout.Window(WinNum("random"), randRect, RandFunction, "Noise Generation", newSkin.window);
                            DrawTooltip(tooltip[0]);
                            IMGUIUtils.EatInputInRect(randRect);
                            Redraw(WinNum("random"), randRect, redrawNum - 1, true);
                            if ((randRect.position - oldPos) != Vector2.zero) {
                                windowRect.position += randRect.position - oldPos;
                            }
                        }
                        if (tab == SettingType.Texture) {
                            Vector2 oldPos = texRect.position;
                            texRect = GUILayout.Window(WinNum("texture"), texRect, TexFunction, "Selected Texture", newSkin.window);
                            IMGUIUtils.EatInputInRect(texRect);
                            Redraw(WinNum("texture"), texRect, redrawNum);
                            if ((texRect.position - oldPos) != Vector2.zero) {
                                windowRect.position += texRect.position - oldPos;
                            }
                        }
                        if (isHelp) {
                            Vector2 oldPos = helpRect.position;
                            helpRect = GUILayout.Window(WinNum("help"), helpRect, HelpFunction, "How to use?", newSkin.window, GUILayout.MaxWidth(defaultSize[2] * UIScale.Value * 1.1f));
                            IMGUIUtils.EatInputInRect(helpRect);
                            Redraw(WinNum("help"), helpRect, redrawNum);
                            if ((helpRect.position - oldPos) != Vector2.zero) {
                                windowRect.position += helpRect.position - oldPos;
                            }
                        }
                        if (isSetting) {
                            Vector2 oldPos = setRect.position;
                            setRect = GUILayout.Window(WinNum("settings"), setRect, SettingFunction, "Settings ۞", newSkin.window, GUILayout.MaxWidth(defaultSize[2] * UIScale.Value * 0.9f));
                            DrawTooltip(tooltip[0]);
                            IMGUIUtils.EatInputInRect(setRect);
                            Redraw(WinNum("settings"), setRect, redrawNum);
                            if ((setRect.position - oldPos) != Vector2.zero) {
                                windowRect.position += setRect.position - oldPos;
                            }
                        }
                        if (showMessage) {
                            var boxStyle = new GUIStyle(newSkin.box) {
                                fontSize = 1
                            };
                            infoRect = GUILayout.Window(WinNum("info"), infoRect, InfoFunction, "", boxStyle);
                            IMGUIUtils.EatInputInRect(infoRect);
                            Redraw(WinNum("info"), infoRect, redrawNum, true);
                        }
                        if (shaderDrop > 0) {
                            shaderRect.position = windowRect.position + new Vector2(commonWidth + newSkin.window.border.left + 4, GUI.skin.label.CalcSize(new GUIContent("TEST")).y + (newSkin.label.CalcSize(new GUIContent("TEST")).y + 4) * (shaderDrop + 1));
                            shaderRect = GUILayout.Window(WinNum("shaderDrop"), shaderRect, ShaderDropFunction, "", newSkin.box, GUILayout.MaxWidth(defaultSize[2]));
                            IMGUIUtils.EatInputInRect(shaderRect);
                            Redraw(WinNum("shaderDrop"), shaderRect, redrawNum, true);
                            GUI.BringWindowToFront(WinNum("shaderDrop"));
                        }
                        if (historyDrop == true) {
                            historyRect.position = windowRect.position + new Vector2(commonWidth + newSkin.window.border.left + 4, GUI.skin.label.CalcSize(new GUIContent("TEST")).y + (newSkin.label.CalcSize(new GUIContent("TEST")).y + 4) * 3);
                            historyRect = GUILayout.Window(WinNum("historyDrop"), historyRect, HistoryDropFunction, "", newSkin.box, GUILayout.MaxWidth(defaultSize[2]));
                            IMGUIUtils.EatInputInRect(historyRect);
                            Redraw(WinNum("historyDrop"), historyRect, redrawNum, true);
                            GUI.BringWindowToFront(WinNum("historyDrop"));
                        }
                    }
                    if (showWarning) {
                        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                        GUI.Box(screenRect, "");
                        warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                        warnRect = GUILayout.Window(WinNum("warn"), warnRect, WarnFunction, "", newSkin.window);
                        IMGUIUtils.EatInputInRect(screenRect);
                        Redraw(WinNum("warn"), warnRect, redrawNum);
                    }
                } else {
                    Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
                    GUI.Box(screenRect, "");
                    warnRect.position = new Vector2((Screen.width - warnRect.size.x) / 2, (Screen.height - warnRect.size.y) / 2);
                    warnRect = GUILayout.Window(WinNum("intro"), warnRect, IntroFunction, "", newSkin.window);
                    IMGUIUtils.EatInputInRect(screenRect);
                    Redraw(WinNum("intro"), warnRect, redrawNum);
                }
            }

        }

        private bool SetAllProperties<T>(T _value) {
            if (!(_value is float) && !(_value is Color) && !(_value is string) && !(_value is ScaledTex)) return false;
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                if (IsDebug.Value) Log($"{(setReset?"Res":"S")}etting ALL items' {(tab == SettingType.Shader ? "shaders" : "properties")}!");
                SetStudioProperties(studio.dicObjectCtrl.Values.ToList(), _value);
            }
            else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (MakerGetType(out ObjectType type)) {
                    var chaCtrl = KKAPI.Maker.MakerAPI.GetCharacterControl();
                    int limit = 1;
                    if (type == ObjectType.Hair) limit = chaCtrl.objHair.Length;
                    if (type == ObjectType.Clothing) limit = chaCtrl.objClothes.Length;
                    if (type == ObjectType.Accessory) limit = chaCtrl.objAccessory.Length;

                    Predicate<Material> matFilter = (x) => true;
                    Predicate<Renderer> rendFilter = (x) => true;

                    if (type == ObjectType.Character && !AffectMiscBodyParts.Value) {
                        rendFilter = (Renderer x) => new List<string> { "cf_o_face", "o_body_a" }.Contains(x.NameFormatted());
                    }
                    if (type == ObjectType.Accessory && HairAccIsHair.Value) {
                        matFilter = (Material x) => !x.shader.NameFormatted().ToLower().Contains("hair");
                    }

                    for (int i = 0; i < limit; i++) SetCharaProperties(chaCtrl.GetController(), null, i, type, _value, matFilter, rendFilter);
                    
                    if (type == ObjectType.Hair && HairAccIsHair.Value)
                        for (int i = 0; i < chaCtrl.objAccessory.Length; i++)
                            SetCharaProperties(chaCtrl.GetController(), null, i, ObjectType.Accessory, _value, (Material x) => x.shader.NameFormatted().ToLower().Contains("hair"));

                    if (MaterialEditorUI.MaterialEditorWindow.gameObject.activeSelf) MEMaker.Instance.RefreshUI();
                } else ShowMessage("Please select a valid item category.");
            }
            HistoryAppend(_value);
            return false;
        }

        private bool SetSelectedProperties<T>(T _value) {
            if (!(_value is float) && !(_value is Color) && !(_value is string) && !(_value is ScaledTex)) return false;
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                if (setReset && _value is ScaledTex) MaterialEditorPluginBase.Logger.LogMessage("Save and reload scene to refresh textures.");
                if (IsDebug.Value) Log($"{(setReset ? "Res" : "S")}etting selected items' properties!");
                var ociList = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                if (ociList.Count > 0) {
                    var iterateList = new List<ObjectCtrlInfo>(ociList);
                    if (IsDebug.Value) Log("Checking for dives...");
                    var diveList = new List<Type>();
                    if (DiveFolders.Value) diveList.Add(typeof(OCIFolder));
                    if (DiveItems.Value) diveList.Add(typeof(OCIItem));
                    foreach (var oci in iterateList)
                        if (diveList.Contains(oci.GetType())) {
                            if (IsDebug.Value) Log($"Found diveable item: {oci.treeNodeObject.textName}");
                            oci.AddChildrenRecursive(ociList);
                        }
                    if (SetStudioProperties(ociList, _value) && fetchValue) return true;
                } else ShowMessage("Please select at least one item!");
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (MakerGetType(out ObjectType type)) {
                    if (setReset && _value is ScaledTex) MaterialEditorPluginBase.Logger.LogMessage("Save and reload character or change outfits to refresh textures.");
                    var chaCtrl = KKAPI.Maker.MakerAPI.GetCharacterControl();
                    int slot = 0;
                    if (type == ObjectType.Hair) slot = makerMenu.ccHairMenu.GetSelectIndex();
                    if (type == ObjectType.Clothing) slot = makerMenu.ccClothesMenu.GetSelectIndex();
                    if (type == ObjectType.Accessory) slot = makerMenu.ccAcsMenu.GetSelectIndex();

                    Predicate<Material> matFilter = (x) => true;
                    Predicate<Renderer> rendFilter = (x) => true;

                    if (makerTabID == 0) {
                        rendFilter = (Renderer x) => x.NameFormatted() == "cf_o_face";
                    } else if (makerTabID == 1) {
                        matFilter = (Material x) => x.NameFormatted() == "cf_m_body" || x.NameFormatted() == "cm_m_body";
                    }
                    
                    SetCharaProperties(chaCtrl.GetController(), null, slot, type, _value, matFilter, rendFilter);

                    if (MaterialEditorUI.MaterialEditorWindow.gameObject.activeSelf) MEMaker.Instance.RefreshUI();
                } else ShowMessage("Please select a valid item category.");
            }
            if (!fetchValue) {
                HistoryAppend(_value);
            }
            return !fetchValue;
        }

        private bool SetStudioProperties<T>(List<ObjectCtrlInfo> _ociList, T _value) {
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                foreach (ObjectCtrlInfo oci in _ociList) {
                    if (oci is OCIItem item) {
                        if (SetItemProperties(controller, item, _value) && fetchValue) return true;
                    } else if (oci is OCIChar ociChar && AffectCharacters.Value) {
                        if (IsDebug.Value) Log($"Looking into character: {ociChar.treeNodeObject.textName}");
                        var ctrl = KKAPI.Studio.StudioObjectExtensions.GetChaControl(ociChar);
                        if (AffectChaBody.Value && AffectMiscBodyParts.Value) SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value);
                        else if (AffectChaBody.Value) if (SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value,
                            (Renderer x) => new List<string> { "cf_o_face", "o_body_a" }.Contains(x.NameFormatted().ToLower())) && fetchValue) return true;
                        else if (AffectMiscBodyParts.Value) if (SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value,
                            (Renderer x) => !new List<string> { "cf_o_face", "o_body_a" }.Contains(x.NameFormatted().ToLower())) && fetchValue) return true;
                        if (AffectChaHair.Value) for(int i = 0; i<ctrl.objHair.Length; i++) if (SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Hair, _value) && fetchValue) return true;
                        if (AffectChaClothes.Value) for (int i = 0; i < ctrl.objClothes.Length; i++) if (SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Clothing, _value) && fetchValue) return true;
                        for (int i = 0; i < ctrl.objAccessory.Length; i++)
                            if (
                                SetCharaProperties(
                                    ctrl.GetController(), ociChar, i, ObjectType.Accessory, _value, (Material x) =>
                                    (x.shader.NameFormatted().ToLower().Contains("hair") && AffectChaHair.Value) ||
                                    (!x.shader.NameFormatted().ToLower().Contains("hair") && AffectChaAccs.Value)
                                ) &&
                                fetchValue
                            ) return true;
                    }
                }
                if (MaterialEditorUI.MaterialEditorWindow.gameObject.activeSelf && !fetchValue) MEStudio.Instance.RefreshUI();
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                Log("SetStudioProperties should not be called inside Maker!", 3);
            }
            return false;
        }

        private bool SetItemProperties<T>(SceneController ctrl, OCIItem item, T _value) {
            var editedMatList = new List<string>();

            if (IsDebug.Value) Log($"Looking into {item.NameFormatted()}...");
            foreach (var rend in GetRendererList(item.objectItem)) {
                if (ApplyFilter(rend.NameFormatted(), filters[0]))
                    foreach (var mat in GetMaterials(item.objectItem, rend)) {
                        if (!editedMatList.Contains(mat.NameFormatted()) && ApplyFilter(mat.NameFormatted(), filters[1]) && ApplyFilter(mat.shader.NameFormatted(), filters[2])) {
                            if (tab == SettingType.Float || tab == SettingType.Color || tab == SettingType.Texture)
                                if (mat.HasProperty("_" + setName)) {
                                    try {
                                        if (setReset) {
                                            if (_value is float || _value is Color) {
                                                ctrl.RemoveMaterialFloatProperty(item.objectInfo.dicKey, mat, setName);
                                                ctrl.RemoveMaterialColorProperty(item.objectInfo.dicKey, mat, setName);
                                                if (IsDebug.Value) Log($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} reset!");
                                            } else if (_value is ScaledTex) {
                                                if (setTexAffectTex) {
                                                    ctrl.RemoveMaterialTexture(item.objectInfo.dicKey, mat, setName, false);
                                                    if (IsDebug.Value) Log($"Texture {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} reset!");
                                                }
                                                if (setTexAffectDims) {
                                                    ctrl.RemoveMaterialTextureOffset(item.objectInfo.dicKey, mat, setName);
                                                    ctrl.RemoveMaterialTextureScale(item.objectInfo.dicKey, mat, setName);
                                                    if (IsDebug.Value) Log($"Texture {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} scale/offset reset!");
                                                }
                                            } else { if (IsDebug.Value) Log($"Tried resetting property with erroneous identifier type: {_value.GetType()}, {_value}"); }
                                        } else {
                                            if (_value is float floatval) {
                                                if (mat.TryGetFloat(setName, out float current)) {
                                                    if (fetchValue) {
                                                        ModifyFloatTarget(current);
                                                        return true;
                                                    }
                                                    ctrl.SetMaterialFloatProperty(item.objectInfo.dicKey, mat, setName, GetModifiedFloat(current, floatval));
                                                    if (IsDebug.Value) Log($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedFloat(current, floatval)}!");
                                                } else { if (IsDebug.Value) Log($"Tried setting non-float property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} to ({floatval}) value!"); }
                                            } else if (_value is Color colval) {
                                                if (mat.TryGetColor(setName, out Color current)) {
                                                    if (fetchValue) {
                                                        setCol = current;
                                                        return true;
                                                    }
                                                    ctrl.SetMaterialColorProperty(item.objectInfo.dicKey, mat, setName, GetModifiedColor(current, colval));
                                                    if (IsDebug.Value) Log($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedColor(current, colval)}!");
                                                } else { if (IsDebug.Value) Log($"Tried setting non-color property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} to ({colval}) value!"); }
                                            } else if (_value is ScaledTex texval && !fetchValue) {
                                                if (mat.TryGetTex(setName, out Texture current)) {
                                                    if (setTexAffectTex) {
                                                        ctrl.SetMaterialTexture(item.objectInfo.dicKey, mat, setName, texval.data);
                                                        if (IsDebug.Value) Log($"Texture {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {texval.TexString()}!");
                                                    }
                                                    if (setTexAffectDims) {
                                                        ctrl.SetMaterialTextureOffset(item.objectInfo.dicKey, mat, setName, new Vector2(texval.offset[0], texval.offset[1]));
                                                        ctrl.SetMaterialTextureScale(item.objectInfo.dicKey, mat, setName, new Vector2(texval.scale[0], texval.scale[1]));
                                                        if (IsDebug.Value) Log($"Texture {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} scale/offset set to {texval.DimString()}!");
                                                    }
                                                } else { if (IsDebug.Value) Log($"Tried setting non-texture property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} to ({texval}) value!"); }
                                            } else { if (IsDebug.Value) Log($"Tried setting an item property or shader to erroneous type: {_value.GetType()}, {_value}"); }
                                        }
                                        editedMatList.Add(mat.NameFormatted());
                                    } catch (Exception e) {
                                        Logger.LogError($"Unknown error during property value assignment of {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName}: {e}");
                                    }
                                } else if ((setName.ToLower().Contains("render queue") || setName.ToLower().Contains("renderqueue") || setName.ToLower().Trim() == "rq") && _value is float floatval) {
                                    if (setReset) {
                                        ctrl.RemoveMaterialShaderRenderQueue(item.objectInfo.dicKey, mat);
                                        if (IsDebug.Value) Log($"Render queue of {item.NameFormatted()}\\{mat.NameFormatted()} reset!");
                                    } else {
                                        ctrl.SetMaterialShaderRenderQueue(item.objectInfo.dicKey, mat, (int)Math.Floor(floatval));
                                        if (IsDebug.Value) Log($"Render queue of {item.NameFormatted()}\\{mat.NameFormatted()} set to {floatval}!");
                                    }
                                } else {
                                    if (IsDebug.Value) Log($"Material {item.NameFormatted()}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()} did not have the {setName} property...");
                                }
                            else if (tab == SettingType.Shader)
                                if (shaders.Contains(setShader) || setReset) {
                                    try {
                                        if (setReset) {
                                            if (_value is string) {
                                                ctrl.RemoveMaterialShader(item.objectInfo.dicKey, mat);
                                                ctrl.RemoveMaterialShaderRenderQueue(item.objectInfo.dicKey, mat);
                                                if (IsDebug.Value) Log($"Shader of {item.NameFormatted()}\\{mat.NameFormatted()} reset!");
                                            } else { if (IsDebug.Value) Log($"Tried resetting shader of {item.NameFormatted()}\\{mat.NameFormatted()} with erroneous identifier type: {_value.GetType()}"); }
                                        } else if (_value is string stringval) {
                                            ctrl.SetMaterialShader(item.objectInfo.dicKey, mat, stringval);
                                            if (setQueue != 0) ctrl.SetMaterialShaderRenderQueue(item.objectInfo.dicKey, mat, setQueue);
                                            if (IsDebug.Value) Log($"Shader of {item.NameFormatted()}\\{mat.NameFormatted()} set to {_value}!");
                                        } else { if (IsDebug.Value) Log($"Tried setting shader of {item.NameFormatted()}\\{mat.NameFormatted()} with erroneous identifier: {_value.GetType()}"); }
                                    } catch (Exception e) {
                                        Logger.LogError($"Unknown error during shader assignment of {item.NameFormatted()}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()}\\{setName}: {e}");
                                    }
                                } else { if (IsDebug.Value) Log($"Tried setting {item.NameFormatted()}\\{mat.NameFormatted()} to nonexistent shader!"); }
                            else { throw new ArgumentException("Erroneous / unimplemented tab type!"); }
                        }
                    }
            }
            return false;
        }

        private bool SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value) {
            return SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, x => true, x => true);
        }

        private bool SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<Material> materialFilter) {
            return SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, materialFilter, x => true);
        }

        private bool SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<Renderer> rendererFilter) {
            return SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, x => true, rendererFilter);
        }

        private bool SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<Material> materialFilter, Predicate<Renderer> rendererFilter) {
            var chaCtrl = ctrl.ChaControl;

            string chaName;
            if (ociChar != null) chaName = ociChar.NameFormatted();
            else chaName = $"{chaCtrl.fileParam.lastname} {chaCtrl.fileParam.firstname}";

            GameObject go = GetChaGO(ctrl, type, slot);
            var editedMatList = new List<string>();

            foreach (var rend in GetRendererList(go).Where(x => rendererFilter(x))) {
                if (ApplyFilter(rend.NameFormatted(), filters[0]))
                    foreach (var mat in GetMaterials(go, rend).Where(x => materialFilter(x))) {
                        if (ApplyFilter(mat.NameFormatted(), filters[1]) && ApplyFilter(mat.shader.NameFormatted(), filters[2])) {
                            if (tab == SettingType.Float || tab == SettingType.Color || tab == SettingType.Texture)
                                if (mat.HasProperty("_" + setName)) {
                                    try {
                                        if (setReset) {
                                            if (_value is float || _value is Color) {
                                                ctrl.RemoveMaterialFloatProperty(slot, type, mat, setName, go);
                                                ctrl.RemoveMaterialColorProperty(slot, type, mat, setName, go);
                                                if (IsDebug.Value) Log($"Property {chaName}\\{mat.NameFormatted()}\\{setName} reset!");
                                            } else if (_value is ScaledTex) {
                                                if (setTexAffectTex) {
                                                    ctrl.RemoveMaterialTexture(slot, type, mat, setName, go, false);
                                                    if (IsDebug.Value) Log($"Texture {chaName}\\{mat.NameFormatted()}\\{setName} reset!");
                                                }
                                                if (setTexAffectDims) {
                                                    ctrl.RemoveMaterialTextureOffset(slot, type, mat, setName, go);
                                                    ctrl.RemoveMaterialTextureScale(slot, type, mat, setName, go);
                                                    if (IsDebug.Value) Log($"Texture {chaName}\\{mat.NameFormatted()}\\{setName} scale/offset reset!");
                                                }
                                            } else { if (IsDebug.Value) Log($"Tried resetting property with erroneous identifier type: {_value.GetType()}"); }
                                        } else {
                                            if (_value is float floatval) {
                                                if (mat.TryGetFloat(setName, out float current)) {
                                                    if (fetchValue) {
                                                        ModifyFloatTarget(current);
                                                        return true;
                                                    }
                                                    ctrl.SetMaterialFloatProperty(slot, type, mat, setName, GetModifiedFloat(current, floatval), go);
                                                    if (IsDebug.Value) Log($"Property {chaName}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedFloat(current, floatval)}!");
                                                } else { if (IsDebug.Value) Log($"Tried setting non-float property {chaName}\\{mat.NameFormatted()}\\{setName} to color ({floatval}) value!"); }
                                            } else if (_value is Color colval) {
                                                if (mat.TryGetColor(setName, out Color current)) {
                                                    if (fetchValue) {
                                                        setCol = current;
                                                        return true;
                                                    }
                                                    ctrl.SetMaterialColorProperty(slot, type, mat, setName, GetModifiedColor(current, colval), go);
                                                    if (IsDebug.Value) Log($"Property {chaName}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedColor(current, colval)}!");
                                                } else { if (IsDebug.Value) Log($"Tried setting non-color property {chaName}\\{mat.NameFormatted()}\\{setName} to float ({colval}) value!"); }
                                            } else if (_value is ScaledTex texval) {
                                                if (mat.TryGetTex(setName, out Texture current)) {
                                                    if (setTexAffectTex) {
                                                        ctrl.SetMaterialTexture(slot, type, mat, setName, texval.data, go);
                                                        if (IsDebug.Value) Log($"Texture {chaName}\\{mat.NameFormatted()}\\{setName} set to {texval.TexString()}!");
                                                    }
                                                    if (setTexAffectDims) {
                                                        ctrl.SetMaterialTextureOffset(slot, type, mat, setName, new Vector2(texval.offset[0], texval.offset[1]), go);
                                                        ctrl.SetMaterialTextureScale(slot, type, mat, setName, new Vector2(texval.scale[0], texval.scale[1]), go);
                                                        if (IsDebug.Value) Log($"Texture {chaName}\\{mat.NameFormatted()}\\{setName} scale/offset set to {texval.DimString()}!");
                                                    }
                                                } else { if (IsDebug.Value) Log($"Tried setting non-texture property {chaName}\\{mat.NameFormatted()}\\{setName} to ({texval}) value!"); }
                                            } else { if (IsDebug.Value) Log($"Tried setting a character property to erroneous type: {_value.GetType()}"); }
                                        }
                                        editedMatList.Add(mat.NameFormatted());
                                    } catch (Exception e) {
                                        Logger.LogError($"Unknown error during property value assignment of {chaName}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()}\\{setName}: {e}");
                                    }
                                } else {
                                    if (IsDebug.Value) Log($"{chaName}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()} did not have the {setName} property...");
                                }
                            else if (tab == SettingType.Shader)
                                if (shaders.Contains(setShader.Trim()) || setReset) {
                                    try {
                                        if (setReset) {
                                            if (_value is string) {
                                                ctrl.RemoveMaterialShader(slot, type, mat, go);
                                                ctrl.RemoveMaterialShaderRenderQueue(slot, type, mat, go);
                                                if (IsDebug.Value) Log($"Shader of {chaName}\\{mat.NameFormatted()} reset!");
                                            } else { if (IsDebug.Value) Log($"Tried resetting shader of {chaName}\\{mat.NameFormatted()} with erroneous identifier type: {_value.GetType()}"); }
                                        } else if (_value is string stringval) {
                                            ctrl.SetMaterialShader(slot, type, mat, stringval, go);
                                            if (setQueue != 0) ctrl.SetMaterialShaderRenderQueue(slot, type, mat, setQueue, go);
                                            if (IsDebug.Value) Log($"Shader of {chaName}\\{mat.NameFormatted()} set to {_value}!");
                                        } else { if (IsDebug.Value) Log($"Tried setting shader of {chaName}\\{mat.NameFormatted()} with erroneous identifier: {_value.GetType()}"); }
                                    } catch (Exception e) {
                                        Logger.LogError($"Unknown error during shader assignment of {chaName}\\{mat.NameFormatted()}\\{mat.shader.NameFormatted()}\\{setName}: {e}");
                                    }
                                } else { if (IsDebug.Value) Log($"Tried setting {chaName}\\{mat.NameFormatted()} to nonexistent shader!"); }
                            else { throw new ArgumentException("Erroneous / unimplemented tab type!"); }
                        }
                    }
            }
            return false;
        }

        private bool GetTargetValue<T>(T value) {
            fetchValue = true;
            setReset = false;
            bool found = SetSelectedProperties(value);
            fetchValue = false;
            return found;
        }

        private bool GetTargetTexture(ref byte[] data, ref Texture tex, ref float[] offset, ref float[] scale) {
            if (IsDebug.Value) Log("Copying target texture...");
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                var ociList = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                if (ociList.Count > 0) {
                    var oci = ociList[0];
                    if (oci is OCIItem item) {
                        foreach (var rend in GetRendererList(item.objectItem))
                            if (ApplyFilter(rend.NameFormatted(), filters[0]))
                                foreach (var mat in GetMaterials(item.objectItem, rend))
                                    if (ApplyFilter(mat.NameFormatted(), filters[1]) && ApplyFilter(mat.shader.NameFormatted(), filters[2]))
                                        if (mat.HasProperty("_" + setName))
                                            return GetItemTexture(oci.objectInfo.dicKey, mat, setName, ref data, ref tex, ref offset, ref scale);
                    } else if (oci is OCIChar ociChar) {
                        List<byte[]> bytes = new List<byte[]>();
                        List<Texture> textures = new List<Texture>();
                        List<float[]> offsets = new List<float[]>();
                        List<float[]> scales = new List<float[]>();
                        var ctrl = KKAPI.Studio.StudioObjectExtensions.GetChaControl(ociChar);
                        if (AffectChaBody.Value && AffectMiscBodyParts.Value) GetCharaTexture(ctrl.GetController(), 0, ObjectType.Character, setName, (x) => true, (x) => true, ref bytes, ref textures, ref offsets, ref scales);
                        else if (AffectChaBody.Value) GetCharaTexture(ctrl.GetController(), 0, ObjectType.Character, setName,
                            (x) => true, (Renderer x) => new List<string> { "cf_o_face", "o_body_a" }.Contains(x.NameFormatted().ToLower()), ref bytes, ref textures, ref offsets, ref scales);
                        else if (AffectMiscBodyParts.Value) GetCharaTexture(ctrl.GetController(), 0, ObjectType.Character, setName,
                            (x) => true, (Renderer x) => !new List<string> { "cf_o_face", "o_body_a" }.Contains(x.NameFormatted().ToLower()), ref bytes, ref textures, ref offsets, ref scales);
                        if (AffectChaHair.Value) for (int i = 0; i < ctrl.objHair.Length; i++) GetCharaTexture(ctrl.GetController(), i, ObjectType.Hair, setName, (x) => true, (x) => true, ref bytes, ref textures, ref offsets, ref scales);
                        if (AffectChaClothes.Value) for (int i = 0; i < ctrl.objClothes.Length; i++) GetCharaTexture(ctrl.GetController(), i, ObjectType.Clothing, setName, (x) => true, (x) => true, ref bytes, ref textures, ref offsets, ref scales);
                        for (int i = 0; i < ctrl.objAccessory.Length; i++)
                            GetCharaTexture(ctrl.GetController(), i, ObjectType.Accessory, setName, (Material x) => (x.shader.NameFormatted().ToLower().Contains("hair") && AffectChaHair.Value) ||
                                (!x.shader.NameFormatted().ToLower().Contains("hair") && AffectChaAccs.Value), (x) => true, ref bytes, ref textures, ref offsets, ref scales);
                        for (int i = 0; i < textures.Count; i++)
                            if (textures[i] != null) {
                                data = bytes[i];
                                tex = textures[i];
                                return true;
                            }
                        return false;
                    }
                }
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (MakerGetType(out ObjectType type)) {
                    var chaCtrl = KKAPI.Maker.MakerAPI.GetCharacterControl();
                    int slot = 0;
                    if (type == ObjectType.Hair) slot = makerMenu.ccHairMenu.GetSelectIndex();
                    if (type == ObjectType.Clothing) slot = makerMenu.ccClothesMenu.GetSelectIndex();
                    if (type == ObjectType.Accessory) slot = makerMenu.ccAcsMenu.GetSelectIndex();

                    Predicate<Renderer> filter = (Renderer x) => true;
                    if (type == ObjectType.Character)
                        if (makerTabID == 0)
                            filter = (Renderer x) => x.NameFormatted() == "cf_o_face";
                        else if (makerTabID == 1)
                            filter = (Renderer x) => x.NameFormatted() == "o_body_a";
                    List<byte[]> bytes = new List<byte[]>();
                    List<Texture> textures = new List<Texture>();
                    List<float[]> offsets = new List<float[]>();
                    List<float[]> scales = new List<float[]>();
                    bool result = GetCharaTexture(chaCtrl.GetController(), slot, type, setName, (x) => true, filter, ref bytes, ref textures, ref offsets, ref scales);

                    if (!result) return false;
                    if (setTexAffectTex) {
                        data = bytes[0];
                        tex = textures[0];
                    }
                    if (setTexAffectDims) {
                        offset = offsets[0];
                        scale = scales[0];
                    }
                    return true;
                } else ShowMessage("Please select a valid item category.");
            }

            return false;
        }

        private bool GetItemTexture(int id, Material mat, string propName, ref byte[] data, ref Texture tex, ref float[] offset, ref float[] scale) {
            try {
                if (typeof(SceneController).GetPrivateProperty("MaterialTexturePropertyList", controller, out object value1)) {
                    var matPropList = (value1 as List<SceneController.MaterialTextureProperty>);

                    var textureProperty = matPropList.FirstOrDefault(x => x.ID == id && x.MaterialName == mat.NameFormatted() && x.Property == propName);
                    if (textureProperty?.TexID != null)
                        if (typeof(SceneController).GetPrivateProperty("TextureDictionary", null, out object value2)) {
                            var texDict = (value2 as Dictionary<int, TextureContainer>);

                            if (IsDebug.Value) Log("ME item texture data found!");
                            if (setTexAffectTex) {
                                data = texDict[(int)textureProperty.TexID].Data;
                                tex = new Texture2D(1, 1);
                                (tex as Texture2D).LoadImage(data);
                            }
                            if (setTexAffectDims) {
                                var offsetTemp = controller.GetMaterialTextureOffset(id, mat, propName) ?? new Vector2(0, 0);
                                offset = new float[] { offsetTemp.x, offsetTemp.y };
                                var scaleTemp = controller.GetMaterialTextureScale(id, mat, propName) ?? new Vector2(1, 1);
                                scale = new float[] { scaleTemp.x, scaleTemp.y };
                            }
                            return true;
                        }
                }
            } catch (Exception e) {
                Logger.LogError(e.Message);
                return false;
            }

            if (IsDebug.Value) Log("ME item texture data NOT found, defaulting to material texture.");
            if (setTexAffectTex) {
                tex = mat.GetTexture("_" + propName);
                data = tex?.ToTexture2D().EncodeToPNG();
            }
            if (setTexAffectDims) {
                var offsetTemp = controller.GetMaterialTextureOffset(id, mat, propName) ?? new Vector2(0, 0);
                offset = new float[] { offsetTemp.x, offsetTemp.y };
                var scaleTemp = controller.GetMaterialTextureScale(id, mat, propName) ?? new Vector2(1, 1);
                scale = new float[] { scaleTemp.x, scaleTemp.y };
            }
            return true;
        }

        private bool GetCharaTexture(MaterialEditorCharaController ctrl, int slot, ObjectType type, string propName, Predicate<Material> materialFilter, Predicate<Renderer> rendererFilter, ref List<byte[]> bytes, ref List<Texture> textures, ref List<float[]> offsets, ref List<float[]> scales) {
            GameObject go = GetChaGO(ctrl, type, slot);

            foreach (var rend in GetRendererList(go).Where(x => rendererFilter(x))) {
                if (ApplyFilter(rend.NameFormatted(), filters[0]))
                    foreach (var mat in GetMaterials(go, rend).Where(x => materialFilter(x))) {
                        if (ApplyFilter(mat.NameFormatted(), filters[1]) && ApplyFilter(mat.shader.NameFormatted(), filters[2])) {
                            if (mat.HasProperty("_" + propName)) {
                                try {
                                    if (typeof(MaterialEditorCharaController).GetPrivateProperty("MaterialTexturePropertyList", ctrl, out object value1)) {
                                        var matPropList = (value1 as List<MaterialTextureProperty>);

                                        var textureProperty = matPropList.FirstOrDefault(x => x.ObjectType == type && x.CoordinateIndex == GetCoordinateIndex() && x.Slot == slot && x.Property == setName && x.MaterialName == mat.NameFormatted());
                                        if (textureProperty?.TexID != null)
                                            if (typeof(MaterialEditorCharaController).GetPrivateProperty("TextureDictionary", null, out object value2)) {
                                                var texDict = (value2 as Dictionary<int, TextureContainer>);
                                                var texBytes = texDict[(int)textureProperty.TexID].Data;
                                                var newTex = new Texture2D(1, 1);
                                                newTex.LoadImage(texBytes);

                                                var offsetTemp = ctrl.GetMaterialTextureOffset(slot, type, mat, propName, go) ?? new Vector2(0, 0);
                                                var scaleTemp = ctrl.GetMaterialTextureScale(slot, type, mat, propName, go) ?? new Vector2(1, 1);

                                                if (IsDebug.Value) Log("ME Character texture data found!");
                                                bytes.Add(texBytes);
                                                textures.Add(newTex);
                                                offsets.Add(new float[] { offsetTemp.x, offsetTemp.y });
                                                scales.Add(new float[] { scaleTemp.x, scaleTemp.y });
                                                return true;
                                            }
                                    }
                                } catch (Exception e) {
                                    Logger.LogError(e.Message);
                                }
                                if (IsDebug.Value) Log("ME Character texture data NOT found, defaulting to material texture.");
                                var tex = mat.GetTexture("_" + setName);
                                var data = tex?.ToTexture2D().EncodeToPNG();

                                bytes.Add(data);
                                textures.Add(tex);
                                offsets.Add(new float[] { 0, 0 });
                                scales.Add(new float[] { 1, 1 });
                                return false;
                            }
                        }
                    }
            }
            return false;

            int GetCoordinateIndex() {
                if (type == ObjectType.Accessory || type == ObjectType.Clothing)
                    return ctrl.ChaControl.fileStatus.coordinateType;
                return 0;
            }
        }

        private GameObject GetChaGO(MaterialEditorCharaController ctrl, ObjectType type, int slot) {
            var chaCtrl = ctrl.ChaControl;

            switch (type) {
                case ObjectType.Character:
                    return ctrl.gameObject;
                case ObjectType.Hair:
                    return chaCtrl.objHair[slot];
                case ObjectType.Clothing:
                    return chaCtrl.objClothes[slot];
                case ObjectType.Accessory:
                    return KKAPI.Maker.AccessoriesApi.GetAccessoryObject(chaCtrl, slot);
                default:
                    return null;
            }
        }

        private bool ApplyFilter(string _name, string _rawFilter) {
            _name = _name.Trim().ToLower();
            _rawFilter = _rawFilter.Trim().ToLower();

            List<string> filterListBlocks = _rawFilter.Split(new char[] { ';', '|' }).ToList();
            foreach (string filterBlock in filterListBlocks) {
                List<string> filterList = filterBlock.Split(new char[] { ',', '&' }).ToList();
                bool output = true;
                for (int i = 0; i < filterList.Count; i++) {
                    string filter = filterList[i].Trim();
                    if (filter.Length == 0) continue;

                    if (filter[0] == '-') {
                        if (_name.Contains(filter.Substring(1))) {
                            output = false;
                            break;
                        }
                    } else if (filter[0] == '+') {
                        if (!_name.Contains(filter.Substring(1))) {
                            output = false;
                            break;
                        }
                    } else {
                        if (!_name.Contains(filter)) {
                            output = false;
                            break;
                        }
                    }
                }
                if (output) return true;
            }
            return false;
        }

        private float GetModifiedFloat(float current, float floatVal) {
            float newVal = 0;
            switch (setModeFloat) {
                case 0:
                    newVal = floatVal; break;
                case 1:
                    newVal = current + floatVal; break;
                case 2:
                    newVal = current * floatVal; break;
                case 3:
                    newVal = Math.Max(current, floatVal); break;
                case 4:
                    newVal = Math.Min(current, floatVal); break;
            }

            if (setRandom) {
                newVal = GetRandomisedFloat(newVal);
            }

            return newVal;
        }

        private Color GetModifiedColor(Color current, Color colval) {
            Color newCol = new Color();
            switch (setModeColor) {
                case 0:
                    newCol = colval; break;
                case 1:
                    newCol = current * (1 - setMix) + colval * setMix; break;
                case 2:
                    newCol = current.AddClamp(colval, 0f, 1f); break;
                case 3:
                    newCol = current + colval; break;
                case 4:
                    newCol = current - colval; break;
            }

            if (setRandom) {
                Color toRandomise;
                if (randColSpace == 0) {
                    toRandomise = newCol;
                } else {
                    toRandomise = newCol.RGBToHSV();
                    toRandomise.r *= 4;
                }

                float r = (randColComps & (1 << 0)) > 0 ? GetRandomisedFloat(toRandomise.r) : toRandomise.r;
                float g = (randColComps & (1 << 1)) > 0 ? GetRandomisedFloat(toRandomise.g) : toRandomise.g;
                float b = (randColComps & (1 << 2)) > 0 ? GetRandomisedFloat(toRandomise.b) : toRandomise.b;
                float a = (randColComps & (1 << 3)) > 0 ? GetRandomisedFloat(toRandomise.a) : toRandomise.a;

                a = Mathf.Clamp(a, 0, 1);

                if (randColSpace == 0) {
                    newCol = new Color(r, g, b, a);
                } else {
                    r = (r / 4f) % 1;
                    if (r < 0) r++;
                    g = Mathf.Clamp(g, 0.005f, 1);
                    b = Mathf.Clamp(b, 0, 1);
                    newCol = new Color(r, g, b, a).HSVToRGB();
                }
            }

            return newCol;
        }

        private float GetRandomisedFloat(float original) {
            if (randDistType == 0) {
                double u1 = 1.0 - rand.NextDouble();
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                return original + randStdDev * (float)randStdNormal;
            } else {
                return original + randStdDev * 2 * (float)(0.5 - rand.NextDouble());
            }
        }

        private bool MakerGetType(out ObjectType type) {
            switch (makerTabID) {
                case 0:
                    type = ObjectType.Character; break;
                case 1:
                    type = ObjectType.Character; break;
                case 2:
                    type = ObjectType.Hair; break;
                case 3:
                    type = ObjectType.Clothing; break;
                case 4:
                    type = ObjectType.Accessory; break;
                case 5:
                    type = ObjectType.Unknown; break;
                case 6:
                    type = ObjectType.Unknown; break;
                default:
                    type = ObjectType.Unknown; break;
            }
            return type != ObjectType.Unknown;
        }

        private void LoadTextureFromFile() {
            if (IsDebug.Value) Log("Opening file dialog...");
            string filter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
            OpenFileDialog.Show(OnFileAccept, "Open image", Application.dataPath, filter, "png");

            void OnFileAccept(string[] strings) {
                if (IsDebug.Value) Log("File chosen!");
                if (strings == null || strings.Length == 0 || strings[0].IsNullOrEmpty()) {
                    return;
                }
                fileToOpen = strings[0];
            }
        }

        public void Log(object data, int level = 0) {
            switch (level) {
                case 0:
                    Logger.LogInfo(data); return;
                case 1:
                    Logger.LogDebug(data); return;
                case 2:
                    Logger.LogWarning(data); return;
                case 3:
                    Logger.LogError(data); return;
                case 4:
                    Logger.LogFatal(data); return;
                default: return;
            }
        }
    }
}
