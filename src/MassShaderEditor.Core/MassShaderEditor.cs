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
        public const string Version = "1.0.2." + BuildNumber.Version;

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

        private Studio.Studio studio;
        private bool inited = false;
        private bool scaled = false;
        private SceneController controller;

        private List<string> shaders;

        internal int makerTabID = 0;
        private CustomChangeMainMenu makerMenu;

        private void Awake() {
            UIScale = Config.Bind("General", "UI Scale", 1.5f, new ConfigDescription("Can also be set via the built-in settings panel", new AcceptableValueRange<float>(1f, maxScale), null));
            UIScale.SettingChanged += (x, y) => scaled = false;
            ShowTooltips = Config.Bind("General", "Show tooltips", true, "");
            AffectMiscBodyParts = Config.Bind("General", "Affect misc body parts", false, new ConfigDescription(affectMiscBodyPartsText, null, null));

            DiveFolders = Config.Bind("Studio", "Dive folders", false, new ConfigDescription(diveFoldersText, null, null));
            DiveItems = Config.Bind("Studio", "Dive items", false, new ConfigDescription(diveItemsText, null, null));
            AffectCharacters = Config.Bind("Studio", "Affect characters", false, new ConfigDescription(affectCharactersText, null, new KKAPI.Utilities.ConfigurationManagerAttributes { Order = 10 }));
            AffectChaBody = Config.Bind("Studio", "Affect character bodies", false, new ConfigDescription(affectChaBodyText, null, null));
            AffectChaHair = Config.Bind("Studio", "Affect character hair", false, new ConfigDescription(affectChaHairText, null, null));
            AffectChaClothes = Config.Bind("Studio", "Affect character clothes", false, new ConfigDescription(affectChaClothesText, null, null));
            AffectChaAccs = Config.Bind("Studio", "Affect character accessories", false, new ConfigDescription(affectChaAccsText, null, null));

            HairAccIsHair = Config.Bind("Maker", "Hair accs are hair", false, new ConfigDescription(hairAccIsHairText, null, null));

            VisibleHotkey = Config.Bind("Hotkeys", "UI Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("The key used to toggle the plugin's UI",null,new KKAPI.Utilities.ConfigurationManagerAttributes{ Order = 10}));
            SetSelectedHotkey = Config.Bind("Hotkeys", "Modify Selected", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate left-clicking 'Modify Selected'", null, null));
            ResetSelectedHotkey = Config.Bind("Hotkeys", "Reset Selected", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate right-clicking 'Modify Selected'", null, null));
            SetAllHotkey = Config.Bind("Hotkeys", "Modify ALL", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate left-clicking 'Modify ALL'", null, null));
            ResetAllHotkey = Config.Bind("Hotkeys", "Reset ALL", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Simulate right-clicking 'Modify ALL'", null, null));

            DisableWarning = Config.Bind("Advanced", "Disable warning", false, new ConfigDescription("Disable the warning screen for the 'Modify ALL' function.", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            IsDebug = Config.Bind("Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            IntroShown = Config.Bind("Advanced", "Intro Shown", false, new ConfigDescription("Whether the intro message has been shown already", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => {
                studio = Singleton<Studio.Studio>.Instance;
                controller = MEStudio.GetSceneController();
                shaders = MaterialEditorAPI.MaterialEditorPluginBase.XMLShaderProperties.Keys.Where(z => z != "default").Select(z => z.Trim()).ToList();

            };
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => {
                makerMenu = (FindObjectOfType(typeof(CustomChangeMainMenu)) as CustomChangeMainMenu);
                controller = MEStudio.GetSceneController();
                shaders = MaterialEditorAPI.MaterialEditorPluginBase.XMLShaderProperties.Keys.Where(z => z != "default").Select(z => z.Trim()).ToList();
                makerTabID = 0;
            };

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
                IsShown = false;
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
                if (IsDebug.Value) Log($"RMB detected! setReset: {setReset}");
            }
            if (Input.GetMouseButtonDown(0) && windowRect.Contains(Input.mousePosition.InvertScreenY()) && !showWarning) {
                setReset = false;
                if (IsDebug.Value) Log($"LMB detected! setReset: {setReset}");
            }
            if (dropdown>0 && (Input.GetMouseButton(1) || Input.GetMouseButton(0)) && !dropRect.Contains(Input.mousePosition.InvertScreenY())) {
                dropdown = 0;
            }

            if (IsShown) {
                if (IntroShown.Value) {
                    if (!showWarning) {
                        windowRect = GUILayout.Window(587, windowRect, WindowFunction, $"Mass Shader Editor v{Version}", newSkin.window, GUILayout.MaxWidth(defaultSize[2] * UIScale.Value));
                        DrawTooltip(tooltip[0]);
                        KKAPI.Utilities.IMGUIUtils.EatInputInRect(windowRect);
                        Redraw(1587, windowRect, redrawNum);

                        helpRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        setRect.position = windowRect.position + new Vector2(windowRect.size.x + 3, 0);
                        infoRect.position = windowRect.position + new Vector2(0, windowRect.size.y + 3);

                        mixRect.position = windowRect.position - new Vector2(mixRect.size.x + 3, 0);
                        mixRect.size = new Vector2(mixRect.size.x, windowRect.size.y);

                        if (tab == SettingType.Color && setModeColor == 1) {
                            mixRect = GUILayout.Window(586, mixRect, MixFunction, "", newSkin.box);
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(mixRect);
                            Redraw(1586, mixRect, redrawNum-1, true);
                        }
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
                        if (dropdown > 0) {
                            dropRect.position = windowRect.position + new Vector2(commonWidth + newSkin.window.border.left + 4, GUI.skin.label.CalcSize(new GUIContent("TEST")).y + (newSkin.label.CalcSize(new GUIContent("TEST")).y + 4) * (dropdown+1));
                            dropRect = GUILayout.Window(593, dropRect, DropFunction, "", newSkin.box, GUILayout.MaxWidth(defaultSize[2]));
                            KKAPI.Utilities.IMGUIUtils.EatInputInRect(dropRect);
                            Redraw(1593, dropRect, redrawNum, true);
                            GUI.BringWindowToFront(593);
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
            if (!(_value is float) && !(_value is Color) && !(_value is string)) return;
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

                    Predicate<Material> filter = (x) => true;
                    if (type == ObjectType.Character && !AffectMiscBodyParts.Value) filter =
                            (Material x) => new List<string> { "cf_m_body", "cm_m_body", "cf_m_face_00" }.Contains(x.NameFormatted());
                    if (type == ObjectType.Accessory && HairAccIsHair.Value) filter =
                            (Material x) => !x.shader.NameFormatted().ToLower().Contains("hair");

                    for (int i = 0; i < limit; i++) SetCharaProperties(chaCtrl.GetController(), null, i, type, _value, filter);
                    if (type == ObjectType.Hair && HairAccIsHair.Value)
                        for (int i = 0; i < chaCtrl.objAccessory.Length; i++)
                            SetCharaProperties(chaCtrl.GetController(), null, i, ObjectType.Accessory, _value, (Material x) => x.shader.NameFormatted().ToLower().Contains("hair"));

                    if (MaterialEditorAPI.MaterialEditorUI.MaterialEditorWindow.gameObject.activeSelf) MEMaker.Instance.RefreshUI();
                } else ShowMessage("Please select a valid item category.");
            }
        }

        private void SetSelectedProperties<T>(T _value) {
            if (!(_value is float) && !(_value is Color) && !(_value is string)) return;
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
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
                    SetStudioProperties(ociList, _value);
                } else ShowMessage("Please select at least one item!");
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (MakerGetType(out ObjectType type)) {
                    var chaCtrl = KKAPI.Maker.MakerAPI.GetCharacterControl();
                    int slot = 0;
                    if (type == ObjectType.Hair) slot = makerMenu.ccHairMenu.GetSelectIndex();
                    if (type == ObjectType.Clothing) slot = makerMenu.ccClothesMenu.GetSelectIndex();
                    if (type == ObjectType.Accessory) slot = makerMenu.ccAcsMenu.GetSelectIndex();

                    Predicate<Material> filter = (Material x) => true;
                    if (type == ObjectType.Character)
                        if (makerTabID == 0) filter = (Material x) => x.NameFormatted() == "cf_m_face_00";
                        else if (makerTabID == 1) filter = (Material x) => x.NameFormatted() == "cf_m_body" || x.NameFormatted() == "cm_m_body";

                    SetCharaProperties(chaCtrl.GetController(), null, slot, type, _value, filter);

                    if (MaterialEditorAPI.MaterialEditorUI.MaterialEditorWindow.gameObject.activeSelf) MEMaker.Instance.RefreshUI();
                } else ShowMessage("Please select a valid item category.");
            }
        }

        private void SetStudioProperties<T>(List<ObjectCtrlInfo> _ociList, T _value) {
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                foreach (ObjectCtrlInfo oci in _ociList) {
                    if (oci is OCIItem item) {
                        SetStudioItemProperties(controller, item, _value);
                    } else if (oci is OCIChar ociChar && AffectCharacters.Value) {
                        if (IsDebug.Value) Log($"Looking into character: {ociChar.treeNodeObject.textName}");
                        var ctrl = KKAPI.Studio.StudioObjectExtensions.GetChaControl(ociChar);
                        if (AffectChaBody.Value && AffectMiscBodyParts.Value) SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value);
                        else if (AffectChaBody.Value) SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value,
                            (Material x) => new List<string> { "cf_m_body", "cm_m_body", "cf_m_face_00" }.Contains(x.NameFormatted().ToLower()));
                        else if (AffectMiscBodyParts.Value) SetCharaProperties(ctrl.GetController(), ociChar, 0, ObjectType.Character, _value,
                            (Material x) => !new List<string> { "cf_m_body", "cm_m_body", "cf_m_face_00" }.Contains(x.NameFormatted().ToLower()));
                        if (AffectChaHair.Value) for(int i = 0; i<ctrl.objHair.Length; i++) SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Hair, _value);
                        if (AffectChaClothes.Value) for (int i = 0; i < ctrl.objClothes.Length; i++) SetCharaProperties(ctrl.GetController(), ociChar, i, ObjectType.Clothing, _value);
                        for (int i = 0; i < ctrl.objAccessory.Length; i++)
                            SetCharaProperties(
                                ctrl.GetController(), ociChar, i, ObjectType.Accessory, _value, (Material x) =>
                                (x.shader.NameFormatted().ToLower().Contains("hair") && AffectChaHair.Value) ||
                                (!x.shader.NameFormatted().ToLower().Contains("hair") && AffectChaAccs.Value)
                                );
                    }
                }
                if (MaterialEditorAPI.MaterialEditorUI.MaterialEditorWindow.gameObject.activeSelf) MEStudio.Instance.RefreshUI();
            } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                Log("SetStudioProperties should not be called inside Maker!");
            }
        }

        private void SetStudioItemProperties<T>(SceneController ctrl, OCIItem item, T _value) {
            if (IsDebug.Value) Log($"Looking into {item.NameFormatted()}...");
            foreach (var rend in GetRendererList(item.objectItem)) {
                //if (IsDebug.Value) Log($"Got renderer: {rend.NameFormatted()}");
                foreach (var mat in GetMaterials(item.objectItem, rend)) {
                    //if (IsDebug.Value) Log($"Got material: {mat.NameFormatted()}");
                    if (mat.shader.NameFormatted().ToLower().Contains(filter.ToLower().Trim())) {
                        if (tab == SettingType.Float || tab == SettingType.Color)
                            if (mat.HasProperty("_" + setName)) {
                                try {
                                    if (setReset) {
                                        if (_value is float || _value is Color) {
                                            ctrl.RemoveMaterialFloatProperty(item.objectInfo.dicKey, mat, setName);
                                            ctrl.RemoveMaterialColorProperty(item.objectInfo.dicKey, mat, setName);
                                            if (IsDebug.Value) Log($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} reset!");
                                        } else { if (IsDebug.Value) Log($"Tried resetting property with erroneous identifier type: {_value.GetType()}"); }
                                    } else {
                                        if (_value is float floatval)
                                            if (mat.TryGetFloat(setName, out float current)) {
                                                ctrl.SetMaterialFloatProperty(item.objectInfo.dicKey, mat, setName, GetModifiedFloat(current, floatval));
                                                if (IsDebug.Value) Log($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedFloat(current, floatval)}!");
                                            } else { if (IsDebug.Value) Log($"Tried setting float property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} to color ({_value}) value!"); }
                                        else if (_value is Color colval)
                                            if (mat.TryGetColor(setName, out Color current)) {
                                                ctrl.SetMaterialColorProperty(item.objectInfo.dicKey, mat, setName, GetModifiedColor(current, colval));
                                                if (IsDebug.Value) Log($"Property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedColor(current, colval)}!");
                                            } else { if (IsDebug.Value) Log($"Tried setting color property {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName} to float ({_value}) value!"); }
                                        else { if (IsDebug.Value) Log($"Tried setting a item property or shader to erroneous type: {_value.GetType()}"); }
                                    }
                                } catch (Exception e) {
                                    Logger.LogError($"Unknown error during property value assignment of {item.NameFormatted()}\\{mat.NameFormatted()}\\{setName}: {e}");
                                }
                            } else if ((setName.ToLower().Contains("render queue") || setName.ToLower().Contains("renderqueue") || setName.ToLower().Trim() == "rq") && _value is float floatval) {
                                if (setReset) ctrl.RemoveMaterialShaderRenderQueue(item.objectInfo.dicKey, mat);
                                else ctrl.SetMaterialShaderRenderQueue(item.objectInfo.dicKey, mat, (int)Math.Floor(floatval));
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
        }

        private void SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value) {
            SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, x => true, x => true);
        }

        private void SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<Material> materialFilter) {
            SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, materialFilter, x => true);
        }

        private void SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<Renderer> rendererFilter) {
            SetCharaProperties<T>(ctrl, ociChar, slot, type, _value, x => true, rendererFilter);
        }

        private void SetCharaProperties<T>(MaterialEditorCharaController ctrl, OCIChar ociChar, int slot, ObjectType type, T _value, Predicate<Material> materialFilter, Predicate<Renderer> rendererFilter) {
            var chaCtrl = ctrl.ChaControl;

            string chaName;
            if (ociChar != null) chaName = ociChar.NameFormatted();
            else chaName = $"{chaCtrl.fileParam.lastname} {chaCtrl.fileParam.firstname}";

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
            foreach (var rend in GetRendererList(go).Where(x => rendererFilter(x))) {
                if (IsDebug.Value) Log($"Got renderer: {rend.NameFormatted()}");
                foreach (var mat in GetMaterials(go, rend).Where(x => materialFilter(x))) {
                    if (IsDebug.Value) Log($"Got material: {mat.NameFormatted()}");
                    if (mat.shader.NameFormatted().ToLower().Contains(filter.ToLower().Trim())) {
                        if (tab == SettingType.Float || tab == SettingType.Color)
                            if (mat.HasProperty("_" + setName)) {
                                try {
                                    if (setReset) {
                                        if (_value is float || _value is Color) {
                                            ctrl.RemoveMaterialFloatProperty(slot, type, mat, setName, go);
                                            ctrl.RemoveMaterialColorProperty(slot, type, mat, setName, go);
                                            if (IsDebug.Value) Log($"Property {chaName}\\{mat.NameFormatted()}\\{setName} reset!");
                                        } else { if (IsDebug.Value) Log($"Tried resetting property with erroneous identifier type: {_value.GetType()}"); }
                                    } else {
                                        if (_value is float floatval)
                                            if (mat.TryGetFloat(setName, out float current)) {
                                                ctrl.SetMaterialFloatProperty(slot, type, mat, setName, GetModifiedFloat(current, floatval), go);
                                                if (IsDebug.Value) Log($"Property {chaName}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedFloat(current, floatval)}!");
                                            } else { if (IsDebug.Value) Log($"Tried setting float property {chaName}\\{mat.NameFormatted()}\\{setName} to color ({_value}) value!"); }
                                        else if (_value is Color colval)
                                            if (mat.TryGetColor(setName, out Color current)) {
                                                ctrl.SetMaterialColorProperty(slot, type, mat, setName, GetModifiedColor(current, colval), go);
                                                if (IsDebug.Value) Log($"Property {chaName}\\{mat.NameFormatted()}\\{setName} set to {GetModifiedColor(current, colval)}!");
                                            } else { if (IsDebug.Value) Log($"Tried setting color property {chaName}\\{mat.NameFormatted()}\\{setName} to float ({_value}) value!"); }
                                        else { if (IsDebug.Value) Log($"Tried setting a character property to erroneous type: {_value.GetType()}"); }
                                    }
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
            return newCol;
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

        public void Log(object data) {
            Logger.LogInfo(data);
        }
    }
}
