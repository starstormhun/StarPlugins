using BepInEx;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using KKAPI;
using ChaCustom;
using UnityEngine;
using HarmonyLib;
using KKAPI.Utilities;

namespace MassShaderEditor.Koikatu {
    public partial class MassShaderEditor : BaseUnityPlugin {
        public bool IsShown { get; private set; } = false;
        private bool isHelp = false;
        private bool isSetting = false;
        private bool showWarning = false;
        private bool showMessage = false;

        private string message = "";
        private float messageDur = 1;
        private float messageTime = 0;

        private static readonly float[] defaultSize = new float[] { 337f, 40f, 305f, 170f };
        private Rect windowRect = new Rect(defaultSize[0], defaultSize[1], defaultSize[2], defaultSize[3]);
        private Rect mixRect = new Rect();
        private Rect helpRect = new Rect();
        private Rect setRect = new Rect();
        private Rect infoRect = new Rect();
        private Rect texRect = new Rect();
        private Rect warnRect = new Rect(0, 0, 360, 200);
        private Rect shaderRect = new Rect(defaultSize[0], defaultSize[1], defaultSize[2] * 0.9f, defaultSize[3] * 1.2f);
        private Rect historyRect = new Rect(defaultSize[0], defaultSize[1], defaultSize[2] * 0.9f, defaultSize[3] * 1.2f);
        private Vector2 shaderScrollPos;
        private Vector2 historyScrollPos;
        internal SettingType tab = SettingType.Float;
        private float prevScale = 1;
        private const float maxScale = 3;
        private const int redrawNum = 2;
        private GUISkin newSkin;

        private string fileToOpen = "";
        private bool setReset = true;
        internal List<string> filters = new List<string>{"", "", ""};
        internal string setName = "";
        private float setVal = 0;
        private int setModeFloat = 0;
        private int setModeColor = 0;
        private Color setCol = Color.white;
        internal string setShader = "";
        internal ScaledTex setTex = new ScaledTex();
        private bool setTexAffectTex = true;
        private bool setTexAffectDims = false;
        private int setQueue = 0;
        private float setMix = 0.5f;

        private float leftLim = -1;
        private float rightLim = 1;
        private string setValInputString = "";
        private float setValInput = 0;
        private float setValSlider = 0;
        private int shaderDrop = 0;
        private bool historyDrop = false;
        private float shaderDropWidth = 0;
        private float historyDropWidth = 0;
        private float commonWidth = 0;
        private float commonHeight = 0;
        private string setQueueInput = "";
        public readonly List<HistoryItem> floatHist = new List<HistoryItem>();
        public readonly List<HistoryItem> colHist = new List<HistoryItem>();
        public readonly List<HistoryItem> texHist = new List<HistoryItem>();
        private readonly Dictionary<HistoryItem, GUIContent> dicHistContent = new Dictionary<HistoryItem, GUIContent>();

        internal int currentFilter = 2;
        internal string filterInput = "";
        internal string setNameInput = "";
        internal string setShaderInput = "";
        private float[] setColNum = new float[]{ 1f, 1f, 1f, 1f };
        private string setColStringInputMemory = "ffffffff";
        private string setColStringInput = "ffffffff";
        private string setColString = "ffffffff";
        internal string pickerName = "Mass Shader Editor Color";
        private bool pickerChanged = false;
        internal bool isPicker = false;
        private Color setColPicker = Color.white;
        private MassShaderEditor.OnShaderSelectFunc onShaderSelect;
        private MassShaderEditor.OnHistorySelectFunc onHistorySelect;

        private float newScale = 1;
        private string newScaleTextInput = "1.00";
        private string newScaleText = "1.00";
        private float newScaleSlider = 1;

        private int helpSection = 0;
        private int helpPage = 0;
        private readonly string[] tooltip = new string[]{ "","" };
        private delegate void OnShaderSelectFunc(string s);
        private delegate void OnHistorySelectFunc(int i);

        private readonly List<string> helpSections = new List<string> { "General", "Filtering", "Values", "Settings", "Info" };
        private readonly List<List<string>> helpText = new List<List<string>>{
            new List<string> {
                "Mass Shader Editor can do four different things with shaders:\n- Edit numeric properties\n- Edit color properties\n- Copy and paste textures\n- Swap shaders\nIt works on items and characters both, and it's possible to filter exactly which items and properties it will modify.",
                "To use the plugin, you have to choose one of the four tabs from the top bar, optionally set up filters, pick a property name to edit, and give it's value.\nOnce all of those are set up, you can click either \"Modify Selected\" or \"Modify ALL\". The way these buttons work is slightly different in Studio and in Maker.",
                "In Maker, when you click \"Modify Selected\" the plugin will look at the currently selected item and look for shaders to edit in that item.\nFor example, if you're editing the Legwear of a character, it will only affect that.",
                "In contrast, \"Modify ALL\" will affect the entire category of items you're currently editing: in the previous example, it would affect every piece of clothing on the current outfit.\nThe different categories of items that can be affected are: Body, Hair, Clothing, Accessories.",
                "In Studio, \"Modify Selected\" will affect only the items / characters that are currently selected in the Workspace, whereas \"Modify ALL\" will affect every single item and character in the entire scene.",
                "Right-clicking \"Modify Selected\" or \"Modify ALL\" in either Maker or Studio will RESET the specified shader properties instead of setting them.",
                "Next to the property selector there is a dropdown arrow, which will let you restore previously used values and property names. The value/color histories will always be saved, but saving the texture history has to be toggled from the settings."
            },
            new List<string> {
                "There are three kinds of filters you can set up: Renderer, Material, Shader.\nThey will filter which shaders to affect based on the names of their respective renderers and materials, and their own names.",
                "To choose which shader to set up, click the R/M/S buttons next to the filter input.\nAn asterisk (*) after the letter indicates that the filter is active. All active filters will be applied when modifying.\nTo clear all filters, click the × button.",
                "When setting up a shader name filter, a dropdown will appear with all MaterialEditor-available shader names to choose from.\nYou can filter different shaders as well, but you have to input their names manually.",
                "Every filter is a wildcard, meaning that if you want to affect all \"KKUTShair', \"KKUTS', and \"KKUTSeye\" shaders you can simply input \"KKUTS\" or \"UTS\" in the shader filter.\nYou can auto-fill filters by clicking their names in Material Editor. For shaders you have to click the \"Shader:\" label to the left of the dropdown.",
                "The property field specifies which property to affect. This is case-sensitive and you have to type the full name of the property here.\nThese can also be auto-filled by clicking the appropriate labels in Material Editor. If you shift+click a \"Shader:\" label then its value will be put into the \"Shader\" field on the \"Shader\" tab, instead of the \"Filter\" field."
            },
            new List<string> {
                "On the \"Value\" tab you can specify the value in two ways: Manual input or slider. The limits of the slider are configurable.\nAfter choosing a value, you also have to choose what way the value will be applied to the shader property.",
                "There are five options for choosing what to do with the specified value:\n- \"=\": Replace with the specified value\n- \"+\": Add to the current value\n- \"×\": Multiply the current value\n- \"Min\": Replace values lower than the specified\n- \"Max\": Replace values higher than the specified",
                "On the \"Color\" tab you can specify the color in three ways: Hex input, color picker, or value input. To open the color picker, click the color display.\nYou can manually set components above 1 or below 0, but if you do then the text input will be set to ########.",
                "There are five options for choosing what to do with the specified color:\n- \"Set\": Replace the current color\n- \"Mix\": Mix with the current color\n- \"Add1\": Add to the color, capping at 1\n- \"Add2\": Add to the color, no cap\n- \"Sub\": Subtract from the color, floor 0\nThe mix ratio can be set by the slider that pops up on the left. (Higher = more effect)",
                "On the \"Texture\" tab you can specify the texture in two ways: By selecting a file from the disk, or by copying an existing texture from a material. When copying, the first texture that matches the filters and the property name will be selected.",
                "On this same tab you can also specify texture scales/offsets to be set when modifying a texture.\nWhether the texture or the scale/offset will be modified can be toggled with two checkboxes at the end of the respective lines.",
                "On the \"Shader\" tab you can swap shaders for other shaders by filtering similar to the other tabs and choosing a shader to be set. The shader name to be set has to match the shader name EXACTLY. For this reason it is recommended to use the dropdown.",
                "Together with shader swapping, the plugin is also able to modify filtered shaders\" render queue, which can be set on this tab. If left empty, 0, or non-numeric, it will not be modified.\nThe render queue can also be modified on the \"Value\" tab by inputting \"RQ', \"Render Queue', \"RenderQueue', or differently capitalised versions of those in the Property selector."
            },
            new List<string> {
                "The settings tab can be opened via the cogwheel icon in the top right next to the \"?\" button, and it lets you toggle most settings of the plugin.\nThese settings are different for Maker and Studio, but there are some common elements.",
                "In both Maker and Studio, you can set the UI Scale, which will modify the size of the plugin's UI. You can also toggle whether tooltips will be shown and whether you want to save texture edit history to disk.",
                "In Maker, you can choose whether to affect miscellaneous body parts such as eyes/tongue/noseline/penis/etc. If set to Yes, then they will only be affected when you're on the Face or Body tabs in the editor, and if you hit \"Modify ALL\".",
                "The other setting in Maker is \"Hair accs are hair\". When using \"Modify Selected', this setting has no effect, but when using \"Modify ALL', and if this setting is turned on to Yes, then hair accessories will ONLY be modified if you're on the Hair tab in the editor.",
                "In Studio, you can set whether to \"Dive\" folders and items. This modifies the behaviour of the \"Modify Selected\" button: If the selected items include an item which is set as diveable, then its children will also be modified. This is recursive, so if there are diveable items among the children, then their children are included as well.",
                "Furthermore, you can choose if you want characters to be affected at all, and if yes, then which parts of them. \"Misc parts\" refers to parts of characters such as tongue, eyebrows, and such, like in Maker, but unlike in Maker, \"Modify Selected\" will affect them as well."
            },
            new List<string> {
                $"MassShaderEditor Version {Version}\nPlugin made by Starstorm"
            }
        };
        private const string introText = "Welcome to Mass Shader Editor! To get started, I first recommend checking out the Help section, which will tell you how to best use this plugin, and any specifics on what each of the buttons and options do.\n\nTo access the help section, click the yellow '?' symbol in the top right corner of the plugin window.\n\nAfterwards, you should check out the various settings of the plugin, accessible either in the F1 menu or by clicking the cogwheel icon next to the help button. The available settings are different in Maker and in Studio!\n\nHappy creating!";
        private const string shaderNameWrongMessage = "You need to input the full name (CASE-sensitive) of the shader to be set! The name has to be from the dropdown list.";
        private const string missingPropertyMessage = "You need to input the name of the property to be modified!";
        private const string texNoAffectChecksMessage = "Please choose to affect either at least texture data or offset / scale values!";
        private const string texEmptyMessage = "Please specify a texture to use!";
        private const string saveTexExplainText = "Whether to save texture edit history to disk. Note: In-session the edit history will always be remembered.";
        private const string valueExplainText = "The value to be used in the modification, according to the method chosen below.";
        private const string colorExplainText = "The color to be assigned to the specified propety.";
        private const string textureExplainText = "The texture to be assigned to the specified property.";
        private const string queueExplainText = "The render queue to set. If left empty, 0, or invalid, the render queue won't be modified.";
        private const string textureSelectText = "Select an image from the disk.";
        private const string textureCopyText = "Copy the specified texture from the currently selected item.";
        private const string diveFoldersText = "Whether 'Modify Selected' will affect items that are inside selected folders.";
        private const string diveItemsText = "Whether 'Modify Selected' will affect items that are the children of selected items.";
        private const string affectCharactersText = "Whether 'Modify Selected' and 'Modify ALL' will affect characters at all.";
        private static string AffectChaPartsText(string part) => $"Whether the 'Set ...' buttons will affect characters' {part}.";
        private readonly string affectChaBodyText = AffectChaPartsText("faces and bodies");
        private readonly string affectChaHairText = AffectChaPartsText("hair, including hair accs");
        private readonly string affectChaClothesText = AffectChaPartsText("clothes");
        private readonly string affectChaAccsText = AffectChaPartsText("accessories, excluding any hair");

        private const string hairAccIsHairText = "Whether 'Modify ALL' will affect hair accessories while editing hair and skip them while editing accessories.";
        private const string affectMiscBodyPartsText = "Whether the miscellaneous body parts like eyes/tongue/noseline/penis/etc should be affected. In Maker and if enabled, only 'Modify ALL' will affect them.";

        private void WindowFunction(int WindowID) {
            GUILayout.BeginVertical();

            // Label setup
            string filterText = "Filter"; float filterWidth = newSkin.label.CalcSize(new GUIContent(filterText)).x;
            string propertyText = "Property"; float propertyWidth = newSkin.label.CalcSize(new GUIContent(propertyText)).x;
            string shaderText = "Shader"; float shaderWidth = newSkin.label.CalcSize(new GUIContent(shaderText)).x;
            string valueText = "Value"; float valueWidth = newSkin.label.CalcSize(new GUIContent(valueText)).x;
            string queueText = "Queue"; float queueWidth = newSkin.label.CalcSize(new GUIContent(queueText)).x;
            string colorText = "Color   #"; float colorWidth = newSkin.label.CalcSize(new GUIContent(colorText)).x;
            string textureText = "Texture"; float textureWidth = newSkin.label.CalcSize(new GUIContent(textureText)).x;
            commonWidth = Mathf.Max(new float[] { filterWidth, propertyWidth, valueWidth, shaderWidth, queueWidth, colorWidth, textureWidth });
            commonHeight = newSkin.textField.CalcSize(new GUIContent("")).y;
            float halfWidth = (windowRect.width - newSkin.window.border.left - newSkin.window.border.right) / 2;
            var filterButtons = new GUIContent[] {
                new GUIContent(filters[0].Trim() == "" ? "R" : "R*", "Filter by renderer name." + (filters[0].Trim() == "" ? "" : " (Filter is set)")),
                new GUIContent(filters[1].Trim() == "" ? "M" : "M*", "Filter by material name." + (filters[1].Trim() == "" ? "" : " (Filter is set)")),
                new GUIContent(filters[2].Trim() == "" ? "S" : "S*", "Filter by shader name." + (filters[2].Trim() == "" ? "" : " (Filter is set)")),
            };
            var filterStyle = new GUIStyle(newSkin.button);
            filterStyle.fontSize = (int)(filterStyle.fontSize * 0.67);

            // Menu bar
            {
                // Changing tabs
                GUILayout.BeginHorizontal();
                var activeStyle = new GUIStyle(newSkin.button);
                activeStyle.normal = activeStyle.active;
                if (GUILayout.Button("Value", (tab == SettingType.Float ? activeStyle : newSkin.button)))
                    tab = SettingType.Float;
                if (GUILayout.Button("Color", (tab == SettingType.Color ? activeStyle : newSkin.button)))
                    tab = SettingType.Color;
                if (GUILayout.Button("Texture", (tab == SettingType.Texture ? activeStyle : newSkin.button)))
                    tab = SettingType.Texture;
                if (GUILayout.Button("Shader", (tab == SettingType.Shader ? activeStyle : newSkin.button)))
                    tab = SettingType.Shader;
                if (GUILayout.Button(new GUIContent("۞", "Show settings"), newSkin.button, GUILayout.ExpandWidth(false))) {
                    isSetting = !isSetting;
                    isHelp = false;
                }
                var helpStyle = new GUIStyle(newSkin.button);
                helpStyle.normal.textColor = Color.yellow;
                helpStyle.hover.textColor = Color.yellow;
                if (GUILayout.Button(new GUIContent("?", "Show help"), helpStyle, GUILayout.ExpandWidth(false))) {
                    isHelp = !isHelp;
                    isSetting = false;
                }
                GUILayout.EndHorizontal();
            }

            // Filters
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(filterText, "Only shaders matching these filters will be edited"), newSkin.label, GUILayout.Width(commonWidth));
                var filterInputStyle = new GUIStyle(newSkin.textField);
                filterInputStyle.fontSize = (int)(filterInputStyle.fontSize * 0.8);
                filterInputStyle.alignment = TextAnchor.MiddleLeft;
                filterInput = GUILayout.TextField(filterInput, filterInputStyle, GUILayout.Height(commonHeight));
                if (currentFilter == 2) {
                    if (GUILayout.Button("▼", newSkin.button, GUILayout.ExpandWidth(false))) {
                        onShaderSelect = (s) => { filters[2] = s; filterInput = s; };
                        CalcShaderDropSize();
                        shaderScrollPos = shaderRect.position;
                        shaderDrop = 1;
                    }
                }
                int prevFilter = currentFilter;
                currentFilter = GUILayout.SelectionGrid(
                    currentFilter,
                    filterButtons,
                    filterButtons.Length,
                    filterStyle,
                    new GUILayoutOption[] {
                            GUILayout.ExpandWidth(false),
                            GUILayout.Height(newSkin.button.CalcHeight(new GUIContent("TEST"), 100))
                    }
                );
                if (prevFilter != currentFilter) {
                    filterInput = filters[currentFilter].Trim();
                }
                filters[currentFilter] = filterInput.Trim();
                if (GUILayout.Button(new GUIContent("×", "Clear filters"), newSkin.button, GUILayout.ExpandWidth(false))) {
                    filters = new List<string> { "", "", "" };
                    filterInput = "";
                }
                GUILayout.EndHorizontal();
            }

            // Float / color / texture property editing
            if (new List<SettingType> { SettingType.Float, SettingType.Color, SettingType.Texture }.Contains(tab)) {
                // Property name
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(propertyText, "The name of the shader property to be edited"), newSkin.label, GUILayout.Width(commonWidth));
                    setNameInput = GUILayout.TextField(setNameInput, newSkin.textField, GUILayout.Height(commonHeight));
                    setName = setNameInput.Trim();
                    if (GUILayout.Button("▼", newSkin.button, GUILayout.ExpandWidth(false))) {
                        if ((tab == SettingType.Float && floatHist.Count > 0) || (tab == SettingType.Color && colHist.Count > 0) || (tab == SettingType.Texture && texHist.Count > 0)) {
                            onHistorySelect = (i) => {
                                if (tab == SettingType.Float) {
                                    setName = floatHist[i].name;
                                    setNameInput = setName;
                                    setValInputString = floatHist[i].val.ToString("0.000");
                                    if (IsDebug.Value) Log($"Restoring history item: {floatHist[i].name} to {floatHist[i].val}");
                                } else if (tab == SettingType.Color) {
                                    setName = colHist[i].name;
                                    setNameInput = setName;
                                    setCol = colHist[i].col.ToColor();
                                    if (IsDebug.Value) Log($"Restoring history item: {colHist[i].name} to {colHist[i].col}");
                                } else if (tab == SettingType.Texture) {
                                    setName = texHist[i].name;
                                    setNameInput = setName;
                                    setTex.data = texHist[i].texData;
                                    if (setTex.data != null) {
                                        var tex = new Texture2D(1, 1);
                                        tex.LoadImage(setTex.data);
                                        setTex.tex = tex;
                                    } else setTex.tex = null;
                                    if (texHist[i].texScale != null && texHist[i].texOffset != null) {
                                        setTex.scale = texHist[i].texScale;
                                        setTex.offset = texHist[i].texOffset;
                                    }
                                    setTexAffectTex = texHist[i].texData != null;
                                    setTexAffectDims = texHist[i].texScale != null;
                                    if (IsDebug.Value) Log($"Restoring history item: {texHist[i].name} with{((texHist[i].texData == null) ? "out" : "")} a texture{((texHist[i].texScale == null) ? "" : $" - Scale ({texHist[i].texScale[0]},{texHist[i].texScale[1]}) Offset ({texHist[i].texOffset[0]},{texHist[i].texOffset[1]})")}");
                                }
                            };
                            CalcHistoryDropSize();
                            historyScrollPos = shaderRect.position;
                            historyDrop = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                // Float value
                if (tab == SettingType.Float) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(valueText, valueExplainText), newSkin.label, GUILayout.Width(commonWidth));
                    setValInputString = GUILayout.TextField(setValInputString, newSkin.textField, GUILayout.Height(commonHeight));
                    setValInput = Studio.Utility.StringToFloat(setValInputString);
                    string indicatorText = "→ " + setVal.ToString("0.000");
                    GUILayout.Label(new GUIContent(indicatorText, valueExplainText), newSkin.label, GUILayout.Width(newSkin.label.CalcSize(new GUIContent(indicatorText)).x));
                    if (GUILayout.Button(new GUIContent("To 0", "Set target value to 0"), newSkin.button, GUILayout.ExpandWidth(false))) {
                        setValInputString = "0";
                        setValInput = 0;
                        setVal = 0;
                        setValSlider = 0;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(-4);
                    GUILayout.BeginHorizontal();
                    leftLim = Studio.Utility.StringToFloat(GUILayout.TextField(leftLim.ToString("0.0"), newSkin.textField, GUILayout.ExpandWidth(false)));
                    GUILayout.BeginVertical(); GUILayout.Space(8);
                    setValSlider = GUILayout.HorizontalSlider(setValSlider, leftLim, rightLim, newSkin.horizontalSlider, newSkin.horizontalSliderThumb);
                    GUILayout.EndVertical();
                    rightLim = Studio.Utility.StringToFloat(GUILayout.TextField(rightLim.ToString("0.0"), newSkin.textField, GUILayout.ExpandWidth(false)));
                    if (Mathf.Abs(setValInput - setVal) > 1E-06) {
                        if (float.TryParse(setValInputString, out _)) {
                            setVal = setValInput;
                            setValSlider = setValInput;
                        }
                    } else if (Mathf.Abs(setValSlider - setVal) > 1E-06) {
                        setVal = setValSlider;
                        setValInput = setValSlider;
                        setValInputString = setVal.ToString();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();

                    var buttonContents = new GUIContent[] {
                        new GUIContent(" = ", "Set the property to this value."),
                        new GUIContent(" + ", "Add to the property's existing value."),
                        new GUIContent(" × ", "Multiply the property's existing value."),
                        new GUIContent("Min", "Set any property lower than the set value to the value."),
                        new GUIContent("Max", "Set any property higher than the set value to the value.")
                    };
                    setModeFloat = GUILayout.SelectionGrid(setModeFloat, buttonContents, buttonContents.Length, newSkin.button);

                    GUILayout.EndHorizontal();
                }

                // Color value
                if (tab == SettingType.Color) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(colorText, colorExplainText), newSkin.label, GUILayout.Width(commonWidth));

                    // Text input
                    {
                        if (!setCol.Matches(setColString.ToColor()) && (setCol.ToArray().Max() <= 1) && (setCol.ToArray().Min() >= 0)) {
                            setColString = setCol.ToHex();
                            setColStringInput = setColString;
                            setColStringInputMemory = setColStringInput;
                        }
                        float colInputWidth = newSkin.textField.CalcSize(new GUIContent("########")).x * 1.2f;
                        setColStringInput = GUILayout.TextField(setColStringInput, newSkin.textField, new GUILayoutOption[] { GUILayout.Height(commonHeight), GUILayout.Width(colInputWidth) });
                        if (setColStringInputMemory != setColStringInput) {
                            try {
                                Color colConvert = setColStringInput.ToColor(); // May throw exception if hexcode is faulty
                                setColString = setColStringInput; // Since hexcode is valid, we store it
                                if (!colConvert.Matches(setCol)) {
                                    if (IsDebug.Value && !pickerChanged) Log($"Color changed from {setCol} to {colConvert} based on text input!");
                                    pickerChanged = false;
                                    setCol = colConvert;
                                }
                            } catch {
                                Log("Could not convert color code!");
                            }
                        }
                        setColStringInputMemory = setColStringInput;
                    } // End text input
                    colorText = "RGBA →";
                    GUILayout.Label(colorText, newSkin.label, GUILayout.Width(newSkin.label.CalcSize(new GUIContent(colorText)).x));

                    // Color picker
                    {
                        if (!setCol.Matches(setColPicker)) {
                            setColPicker = setCol;
                            if (isPicker) ColorPicker(setColPicker, actPicker, true);
                        }
                        if (GUILayout.Button(new GUIContent("Click", colorExplainText), Colorbutton(setColPicker))) {
                            isPicker = !isPicker;
                            if (isPicker) {
                                setColPicker = setColPicker.Clamp();
                                setCol = setColPicker;
                                setColString = setCol.ToHex();
                                setColStringInput = setColString;
                                setColStringInputMemory = setColString;
                            }
                            ColorPicker(setColPicker, actPicker);
                        }
                        void actPicker(Color c) {
                            if (IsDebug.Value) Log($"Color changed from {setCol} to {c} based on picker!");
                            setCol = c;
                            setColPicker = c;
                            pickerChanged = true;
                        }
                    } // End Color picker

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();

                    // Value input
                    {
                        if (!setCol.Matches(setColNum.ToColor())) {
                            setColNum = setCol.ToArray();
                        }
                        float[] buffer = (float[])setColNum.Clone();
                        for (int i = 0; i < 4; i++) {
                            buffer[i] = Mathf.Round(buffer[i] * 1000) / 1000f;
                        }
                        GUILayout.Label("R", newSkin.label, GUILayout.ExpandWidth(false)); setColNum[0] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[0].ToString("0.000"), newSkin.textField));
                        GUILayout.Label("  G", newSkin.label, GUILayout.ExpandWidth(false)); setColNum[1] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[1].ToString("0.000"), newSkin.textField));
                        GUILayout.Label("  B", newSkin.label, GUILayout.ExpandWidth(false)); setColNum[2] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[2].ToString("0.000"), newSkin.textField));
                        GUILayout.Label("  A", newSkin.label, GUILayout.ExpandWidth(false)); setColNum[3] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[3].ToString("0.000"), newSkin.textField));
                        if (!buffer.Matches(setColNum)) {
                            if (IsDebug.Value) Log($"Color changed from {setCol} to {setColNum.ToColor()} based on value input!");
                            setCol = setColNum.ToColor();
                            if (setCol.ToArray().Max() > 1 || setCol.ToArray().Min() < 0) {
                                if (isPicker) {
                                    isPicker = false;
                                    ColorPicker(Color.black, null);
                                }
                                setColStringInput = "########";
                                setColStringInputMemory = "########";
                            }
                            if ((buffer.Max() > 1 || buffer.Min() < 0) && (setCol.ToArray().Max() <= 1 && setCol.ToArray().Min() >=0)) {
                                setColString = setCol.ToHex();
                                setColStringInput = setColString;
                                setColStringInputMemory = setColStringInput;
                            }
                        }
                    } // End value input

                    GUILayout.EndHorizontal();

                    var buttonContents = new GUIContent[] {
                        new GUIContent("Set", "Replace the color."),
                        new GUIContent("Mix", "Average the existing and set color. The ratio is controlled by the slider that appears to the left."),
                        new GUIContent("Add1", "Add the set color, capping values at 1."),
                        new GUIContent("Add2", "Add the set color, without capping values."),
                        new GUIContent("Sub", "Subtract the set color, but not below 0.")
                    };
                    setModeColor = GUILayout.SelectionGrid(setModeColor, buttonContents, buttonContents.Length, newSkin.button);
                }

                // Texture value
                if (tab == SettingType.Texture) {
                    float checkWidth = newSkin.button.CalcSize(new GUIContent("X")).x;

                    // Texture selection
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label(new GUIContent(textureText, textureExplainText), newSkin.label, GUILayout.Width(commonWidth));
                        if (GUILayout.Button(new GUIContent("Select", textureSelectText), newSkin.button)) {
                            LoadTextureFromFile();
                        }
                        if (GUILayout.Button(new GUIContent("Copy", textureCopyText), newSkin.button)) {
                            GetTargetTexture(out setTex.data, out setTex.tex);
                        }

                        if (GUILayout.Button(new GUIContent(setTexAffectTex ? "X" : "", "Check this to affect the texture data of texture properties!"), newSkin.button, new GUILayoutOption[] { GUILayout.Width(checkWidth), GUILayout.Height(commonHeight - UIScale.Value / 1.5f) }))
                            setTexAffectTex = !setTexAffectTex;

                        GUILayout.EndHorizontal();
                    }

                    // Offset - Scale input
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label(new GUIContent("Off X", "X offset"), newSkin.label, GUILayout.ExpandWidth(false));
                        setTex.offset[0] = Studio.Utility.StringToFloat(GUILayout.TextField(setTex.offset[0].ToString("0.000"), newSkin.textField));
                        GUILayout.Label(new GUIContent("Y", "Y offset"), newSkin.label, GUILayout.ExpandWidth(false));
                        setTex.offset[1] = Studio.Utility.StringToFloat(GUILayout.TextField(setTex.offset[1].ToString("0.000"), newSkin.textField));
                        GUILayout.Label(new GUIContent("Sc X", "X scale"), newSkin.label, GUILayout.ExpandWidth(false));
                        setTex.scale[0] = Studio.Utility.StringToFloat(GUILayout.TextField(setTex.scale[0].ToString("0.000"), newSkin.textField));
                        GUILayout.Label(new GUIContent("Y", "Y scale"), newSkin.label, GUILayout.ExpandWidth(false));
                        setTex.scale[1] = Studio.Utility.StringToFloat(GUILayout.TextField(setTex.scale[1].ToString("0.000"), newSkin.textField));

                        if (GUILayout.Button(new GUIContent(setTexAffectDims ? "X" : "", "Check this to affect the offset and scale of texture properties!"), newSkin.button, new GUILayoutOption[] { GUILayout.Width(checkWidth), GUILayout.Height(commonHeight - UIScale.Value / 1.5f) }))
                            setTexAffectDims = !setTexAffectDims;

                        GUILayout.EndHorizontal();
                    }

                    // Info label to point out checkboxes
                    {
                        var infoLabelStyle = new GUIStyle(newSkin.label);
                        infoLabelStyle.alignment = TextAnchor.MiddleRight;
                        GUILayout.Label("Choose what to affect!  ↑  ", infoLabelStyle);
                    }
                }
            }

            // Shader swapping
            if (tab == SettingType.Shader) {
                // Shader
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(shaderText, "Full name of the shader to be set"), newSkin.label, GUILayout.Width(commonWidth));
                    setShaderInput = GUILayout.TextField(setShaderInput, newSkin.textField);
                    setShader = setShaderInput.Trim();
                    if (GUILayout.Button("▼", newSkin.button, GUILayout.ExpandWidth(false))) {
                        onShaderSelect = (s) => { setShader = s; setShaderInput = s; };
                        CalcShaderDropSize();
                        shaderScrollPos = shaderRect.position;
                        shaderDrop = 2;
                    }
                    GUILayout.EndHorizontal();
                }

                // Render queue
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(queueText, queueExplainText), newSkin.label, GUILayout.Width(commonWidth));
                    setQueueInput = GUILayout.TextField(setQueueInput, newSkin.textField);
                    setQueue = setQueueInput.ToInt();
                    string indicatorText = "→ " + (setQueue == 0 ? "####" : setQueue.ToString("0000"));
                    GUILayout.Label(new GUIContent(indicatorText, queueExplainText), newSkin.label, GUILayout.Width(newSkin.label.CalcSize(new GUIContent(indicatorText)).x));
                    GUILayout.EndHorizontal();
                }

                SpacerVert(2);
                GUILayout.Space(-UIScale.Value / 1.5f);
            }

            // Action buttons
            {
                GUILayout.BeginHorizontal();
                GUIStyle allStyle = new GUIStyle(newSkin.button);
                allStyle.normal.textColor = Color.red;
                allStyle.hover.textColor = Color.red;
                if (GUILayout.Button(new GUIContent("Modify ALL", "Right click to reset ALL"), allStyle, GUILayout.MaxWidth(halfWidth))) {
                    if (new List<SettingType> { SettingType.Float, SettingType.Color, SettingType.Texture }.Contains(tab))
                        if (setName != "") {
                            if (!DisableWarning.Value) {
                                showWarning = true;
                            } else {
                                if (tab == SettingType.Color)
                                    SetAllProperties(setCol);
                                else if (tab == SettingType.Float)
                                    SetAllProperties(setVal);
                                else if (tab == SettingType.Texture)
                                    TrySetTexture(SetAllProperties);
                            }
                        } else ShowMessage(missingPropertyMessage);
                    else if (tab == SettingType.Shader)
                        if (shaders.Contains(setShader) || setReset) {
                            if (!DisableWarning.Value) {
                                showWarning = true;
                            } else {
                                SetAllProperties(setShader);
                            }
                        } else ShowMessage(shaderNameWrongMessage, 6);
                }
                if (GUILayout.Button(new GUIContent("Modify Selected", "Right click to reset selected"), newSkin.button, GUILayout.MaxWidth(halfWidth))) {
                    if (new List<SettingType> { SettingType.Float, SettingType.Color, SettingType.Texture }.Contains(tab))
                        if (setName != "") {
                            if (tab == SettingType.Color)
                                SetSelectedProperties(setCol);
                            else if (tab == SettingType.Float)
                                SetSelectedProperties(setVal);
                            else if (tab == SettingType.Texture)
                                TrySetTexture(SetSelectedProperties);
                        } else ShowMessage(missingPropertyMessage);
                    else if (tab == SettingType.Shader)
                        if (shaders.Contains(setShader) || setReset) {
                            SetSelectedProperties(setShader);
                        } else ShowMessage(shaderNameWrongMessage, 6);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUI.DragWindow();

            if (windowRect.position.x < -windowRect.size.x * 0.9f) windowRect.position -= new Vector2(windowRect.position.x + windowRect.size.x * 0.9f, 0);
            if (windowRect.position.y < 0) windowRect.position -= new Vector2(0, windowRect.position.y);
            if (windowRect.position.x + windowRect.size.x * 0.1f > Screen.width) windowRect.position -= new Vector2(windowRect.position.x + windowRect.size.x * 0.1f - Screen.width, 0);
            if (windowRect.position.y + 18 > Screen.height) windowRect.position -= new Vector2(0, windowRect.position.y + 18 - Screen.height);

            PushTooltip(GUI.tooltip);
        }

        private void HelpFunction(int WindowID) {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", newSkin.button)) {
                helpPage = 0;
                if (--helpSection < 0) helpSection = helpSections.Count - 1;
                CalcSizes();
            }

            GUILayout.FlexibleSpace(); GUILayout.Label($"Section {helpSection + 1}/{helpSections.Count} ({helpSections[helpSection]})", newSkin.label); GUILayout.FlexibleSpace();

            if (GUILayout.Button(">>", newSkin.button)) {
                helpPage = 0;
                if (++helpSection == helpSections.Count) helpSection = 0;
                CalcSizes();
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(" < ", newSkin.button)) {
                if (--helpPage < 0) {
                    if (--helpSection < 0) helpSection = helpSections.Count - 1;
                    helpPage = helpText[helpSection].Count - 1;
                }
                CalcSizes();
            }

            GUILayout.FlexibleSpace(); GUILayout.Label($"Page {helpPage + 1}/{helpText[helpSection].Count}", newSkin.label); GUILayout.FlexibleSpace();

            if (GUILayout.Button(" > ", newSkin.button)) {
                if (++helpPage == helpText[helpSection].Count) {
                    if (++helpSection == helpSections.Count) helpSection = 0;
                    helpPage = 0;
                }
                CalcSizes();
            }

            var helpStyle = new GUIStyle(newSkin.label);
            if (helpSection == helpSections.Count - 1)
                helpStyle.alignment = TextAnchor.MiddleCenter;
            else
                helpStyle.stretchWidth = true;

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(helpText[helpSection][helpPage], helpStyle);
            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void SettingFunction(int WindowID) {
            GUILayout.BeginVertical();

            float halfWidth = (setRect.width - newSkin.window.border.left - newSkin.window.border.right) / 2;

            // General
            {
                // UI Scale
                {
                    GUILayout.Space(-4); GUILayout.BeginHorizontal();
                    GUILayout.Label("UI Scale", newSkin.label, GUILayout.ExpandWidth(false));

                    // Text input
                    {
                        if (Mathf.Abs(newScale - Studio.Utility.StringToFloat(newScaleText)) > 1E-06) {
                            newScaleText = newScale.ToString("0.00");
                            newScaleTextInput = newScaleText;
                        }
                        GUI.SetNextControlName("MSE_Interface_UIScaleText");
                        Vector2 textSize = newSkin.textField.CalcSize(new GUIContent("5.55"));
                        newScaleTextInput = GUILayout.TextArea(newScaleTextInput, newSkin.textField, new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MaxHeight(textSize.y), GUILayout.MaxWidth(textSize.x) });
                        
                        // The only way I could find to detect enter/return was via making it a TextArea and searching the string
                        if (newScaleTextInput.Contains("\n")) {
                            int i = newScaleTextInput.IndexOf('\n');
                            if (UIScale.Value == newScale)
                                newScaleTextInput = newScaleTextInput.Remove(i, 1);
                            UIScale.Value = newScale;
                        }

                        float newScaleTemp = Studio.Utility.StringToFloat(newScaleTextInput.Trim());
                        if (Mathf.Abs(newScale - newScaleTemp) > 1E-06 && float.TryParse(newScaleTextInput, out _)) {
                            newScale = Mathf.Clamp(newScaleTemp, 1, maxScale);
                            newScaleText = newScaleTextInput;
                        }
                    } // End text input

                    // Slider
                    {
                        GUILayout.BeginVertical(); GUILayout.Space(8);
                        if (Mathf.Abs(newScaleSlider - newScale) > 1E-06) newScaleSlider = newScale;
                        newScaleSlider = GUILayout.HorizontalSlider(newScaleSlider, 1, maxScale, newSkin.horizontalSlider, newSkin.horizontalSliderThumb);
                        if (Mathf.Abs(newScaleSlider - newScale) > 1E-06) newScale = newScaleSlider;
                        GUILayout.EndVertical();
                    } // End slider

                    GUILayout.Label("→ " + newScale.ToString("0.00"), newSkin.label, GUILayout.ExpandWidth(false));

                    // Button
                    if (GUILayout.Button(new GUIContent("Set", "Right click to reset to 1.5"), newSkin.button, GUILayout.ExpandWidth(false)))
                        UIScale.Value = setReset ? 1.5f : newScale;

                    GUILayout.EndHorizontal(); GUILayout.Space(8);
                } // End UI Scale

                // Misc
                {
                    
                    GUILayout.BeginHorizontal();
                    // Tooltips
                    if (GUILayout.Button($"Tooltips: {(ShowTooltips.Value ? "Yes" : "No")}", newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        ShowTooltips.Value = !ShowTooltips.Value;
                    // Save textures
                    if (GUILayout.Button(new GUIContent($"Save textures: {(SaveTextures.Value ? "Yes" : "No")}", saveTexExplainText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        SaveTextures.Value = !SaveTextures.Value;
                    GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
                }
            } // End general

            Spacer();

            // Studio settings
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent($"Dive folders: {(DiveFolders.Value ? "Yes" : "No")}", diveFoldersText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                    DiveFolders.Value = !DiveFolders.Value;
                if (GUILayout.Button(new GUIContent($"Dive items: {(DiveItems.Value ? "Yes" : "No")}", diveItemsText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                    DiveItems.Value = !DiveItems.Value;
                GUILayout.EndHorizontal(); Spacer();
                if (GUILayout.Button(new GUIContent($"Affect characters: {(AffectCharacters.Value ? "Yes" : "No")}", affectCharactersText), newSkin.button)) {
                    AffectCharacters.Value = !AffectCharacters.Value;
                    CalcSizes();
                }
                if (AffectCharacters.Value) {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent($"Body: {(AffectChaBody.Value ? "Yes" : "No")}", affectChaBodyText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        AffectChaBody.Value = !AffectChaBody.Value;
                    if (GUILayout.Button(new GUIContent($"Hair: {(AffectChaHair.Value ? "Yes" : "No")}", affectChaHairText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        AffectChaHair.Value = !AffectChaHair.Value;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent($"Clothes: {(AffectChaClothes.Value ? "Yes" : "No")}", affectChaClothesText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        AffectChaClothes.Value = !AffectChaClothes.Value;
                    if (GUILayout.Button(new GUIContent($"Accs: {(AffectChaAccs.Value ? "Yes" : "No")}", affectChaAccsText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        AffectChaAccs.Value = !AffectChaAccs.Value;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent($"Misc parts: {(AffectMiscBodyParts.Value ? "Yes" : "No")}", affectMiscBodyPartsText), newSkin.button, GUILayout.MaxWidth(halfWidth)))
                        AffectMiscBodyParts.Value = !AffectMiscBodyParts.Value;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            } // End studio settings

            // Maker settings
            if (KKAPI.Maker.MakerAPI.InsideMaker) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent($"Misc parts: {(AffectMiscBodyParts.Value ? "Yes" : "No")}", affectMiscBodyPartsText), newSkin.button))
                    AffectMiscBodyParts.Value = !AffectMiscBodyParts.Value;
                if (GUILayout.Button(new GUIContent($"Hair accs are hair: {(HairAccIsHair.Value ? "Yes" : "No")}", hairAccIsHairText), newSkin.button))
                    HairAccIsHair.Value = !HairAccIsHair.Value;
                GUILayout.EndHorizontal();
            } // End maker settings

            GUILayout.EndVertical();
            GUI.DragWindow();

            PushTooltip(GUI.tooltip);
        }

        private void WarnFunction(int WindowID) {
            GUILayout.BeginHorizontal(); GUILayout.Space(15 * UIScale.Value); GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
            var warnStyle = new GUIStyle(newSkin.label);
            warnStyle.fontSize = (int)(warnStyle.fontSize*2);
            warnStyle.normal.textColor = Color.red;
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label("WARNING !", warnStyle); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            string items = "";
            if (KKAPI.Studio.StudioAPI.InsideStudio) items = "items in the scene";
            if (KKAPI.Maker.MakerAPI.InsideMaker) items = "items in the currently edited category";
            string value = "";
            if (tab == SettingType.Color) value = setCol.ToString();
            if (tab == SettingType.Float) value = setVal.ToString();
            string propString = $"Are you sure you want to {(setReset ? "re" : "")}set the \"{setName}\" property of ALL {items}{(setReset ? "" : $" to {value}")}?";
            string shaderString = $"Are you sure you want to {(setReset ? "re" : "")}set the shaders of ALL {items}{(setReset ? "" : $" to {setShader}")}";
            GUILayout.BeginHorizontal(); GUILayout.Label((tab == SettingType.Shader ? shaderString : propString), newSkin.label); GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            var buttonStyle = new GUIStyle(newSkin.button);
            buttonStyle.fontSize = (int)(buttonStyle.fontSize*1.5f);
            buttonStyle.fixedHeight = buttonStyle.CalcHeight(new GUIContent("test"), 150) * 1.5f;
            if (GUILayout.Button("No", buttonStyle))
                showWarning = false;
            GUILayout.Space(10 * UIScale.Value);
            if (GUILayout.Button("Yes", buttonStyle)) {
                showWarning = false;
                if (tab == SettingType.Color)
                    SetAllProperties(setCol);
                if (tab == SettingType.Float)
                    SetAllProperties(setVal);
                if (tab == SettingType.Texture)
                    TrySetTexture(SetAllProperties);
                if (tab == SettingType.Shader)
                    SetAllProperties(setShader);
            }
            GUILayout.EndHorizontal();
            var infoStyle = new GUIStyle(newSkin.label);
            infoStyle.normal.textColor = Color.yellow;
            infoStyle.fontSize = Math.Max((int)(infoStyle.fontSize / 2 * 0.75), GUI.skin.font.fontSize);
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label("(You can disable this warning in the F1 menu)", infoStyle); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace(); GUILayout.EndVertical(); GUILayout.Space(15 * UIScale.Value); GUILayout.EndHorizontal();
        }

        private void IntroFunction(int WindowID) {
            GUILayout.BeginHorizontal(); GUILayout.Space(5 * UIScale.Value); GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
            var warnStyle = new GUIStyle(newSkin.label);
            warnStyle.fontSize = (int)(warnStyle.fontSize*1.25f);
            warnStyle.normal.textColor = Color.yellow;
            warnStyle.fontStyle = FontStyle.Bold;
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); GUILayout.Label($"Mass Shader Editor v{Version}", warnStyle); GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(introText, newSkin.label);
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            var buttonStyle = new GUIStyle(newSkin.button);
            buttonStyle.fontSize = (int)(buttonStyle.fontSize * 1.25f);
            buttonStyle.fixedHeight = buttonStyle.CalcHeight(new GUIContent("TEST"), 150) * 1.5f;
            buttonStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button(new GUIContent("<  OK  >"), buttonStyle))
                IntroShown.Value = true;
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace(); GUILayout.EndVertical(); GUILayout.Space(5 * UIScale.Value); GUILayout.EndHorizontal();
        }

        private void InfoFunction(int windowID) {
            var msgStyle = new GUIStyle(newSkin.label);
            msgStyle.normal.textColor = Color.yellow;
            msgStyle.fontSize = Math.Max(GUI.skin.font.fontSize, (int)(msgStyle.fontSize * 0.8));
            GUILayout.Label(message, msgStyle);
        }

        private void ShaderDropFunction(int windowID) {
            shaderScrollPos = GUILayout.BeginScrollView(shaderScrollPos, false, true, GUIStyle.none, newSkin.verticalScrollbar, GUILayout.Height(windowRect.height * 1.2f), GUILayout.Width(shaderDropWidth));
            var scrollBtnStyle = new GUIStyle(newSkin.button) {
                alignment = TextAnchor.MiddleLeft
            };
            foreach (string shader in shaders)
                if (GUILayout.Button(shader, scrollBtnStyle)) {
                    onShaderSelect.Invoke(shader);
                    shaderDrop = 0;
                }
            GUILayout.EndScrollView();
        }

        private void HistoryDropFunction(int windowID) {
            historyScrollPos = GUILayout.BeginScrollView(historyScrollPos, false, true, GUIStyle.none, newSkin.verticalScrollbar,
                new GUILayoutOption[] { GUILayout.Height(windowRect.height * 1.2f), GUILayout.Width(historyDropWidth) });
            var scrollBtnStyle = new GUIStyle(newSkin.button) {
                alignment = TextAnchor.MiddleLeft
            };
            List<HistoryItem> items = null;
            if (tab == SettingType.Float) items = floatHist;
            else if (tab == SettingType.Color) items = colHist;
            else if (tab == SettingType.Texture) items = texHist;
            items.Reverse();
            foreach (HistoryItem item in items) {
                if (GUILayout.Button(GetHistoryContent(item), scrollBtnStyle)) {
                    onHistorySelect.Invoke(items.IndexOf(item));
                    historyDrop = false;
                }
            }
            items.Reverse();
            GUILayout.EndScrollView();
        }

        private void MixFunction(int windowID) {
            var numStyle = new GUIStyle(newSkin.label) {
                alignment = TextAnchor.MiddleCenter,
            };
            GUILayout.BeginVertical();
            GUILayout.Label("1",numStyle);
            setMix = GUILayout.VerticalSlider(setMix, 1, 0, newSkin.verticalSlider, newSkin.verticalSliderThumb);
            GUILayout.Label("0", numStyle);
            GUILayout.EndVertical();
        }

        private void TexFunction(int windowID) {
            var imgLabelStyle = new GUIStyle(newSkin.label);
            imgLabelStyle.alignment = TextAnchor.MiddleCenter;
            float imgSize = defaultSize[2] * 0.6f * UIScale.Value;

            if (setTex.tex != null) {
                GUILayout.Label(new GUIContent(setTex.tex), imgLabelStyle, new GUILayoutOption[] { GUILayout.Width(imgSize), GUILayout.Height(imgSize) });
            } else {
                GUILayout.Label(new GUIContent("No Texture"), imgLabelStyle, new GUILayoutOption[] { GUILayout.Width(imgSize), GUILayout.Height(imgSize) });
            }

            GUI.DragWindow();
        }

        private void TrySetTexture(Action<ScaledTex> setFunction) {
            if (((setTex.tex != null && setTexAffectTex) || (setReset && setTexAffectTex)) || setTexAffectDims) {
                setFunction.Invoke(setTex);
            } else if (!setTexAffectTex) {
                ShowMessage(texNoAffectChecksMessage);
            } else ShowMessage(texEmptyMessage);
        }

        private void ColorPicker(Color col, Action<Color> act, bool update = false) {
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Studio) {
                if (studio.colorPalette.visible && !update && (studio.colorPalette.textWinTitle.m_text == pickerName)) {
                    studio.colorPalette.visible = false;
                } else {
                    studio.colorPalette._outsideVisible = true;
                    studio.colorPalette.Setup(pickerName, col, act, true);
                    studio.colorPalette.visible = true;
                }
            }
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Maker) {
                CvsColor component = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsColor/Top").GetComponent<CvsColor>();
                if (component.isOpen && !update) {
                    component.Close();
                } else {
                    component.Setup("ColorPicker", CvsColor.ConnectColorKind.None, col, act, true);
                }
            }
        }

        private GUIStyle Colorbutton(Color col) {
            GUIStyle guiStyle = new GUIStyle(newSkin.button);
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            float max = Mathf.Max(col.maxColorComponent, 1);
            Color newCol = new Color(col.r / max, col.g / max, col.b / max, col.a);
            texture2D.SetPixel(0, 0, newCol);
            texture2D.Apply();
            guiStyle.normal.background = texture2D;
            guiStyle.normal.textColor = ((col.maxColorComponent * col.a) > 0.7) ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.9f, 0.9f, 0.9f);
            guiStyle.hover = guiStyle.normal;
            guiStyle.onHover = guiStyle.normal;
            guiStyle.onActive = guiStyle.normal;
            return guiStyle;
        }

        private void CalcSizes() {
            helpRect.size = new Vector2(windowRect.size.x, newSkin.label.CalcHeight(new GUIContent(helpText[helpSection][helpPage]), windowRect.size.x) + newSkin.label.CalcHeight(new GUIContent("temp"), windowRect.size.x) + 10 * UIScale.Value);
            setRect.size = new Vector2(windowRect.size.x, newSkin.label.CalcHeight(new GUIContent("TEST"), setRect.size.x) + 10);
            infoRect.size = new Vector2(windowRect.size.x, 10);
        }

        private void CalcShaderDropSize() {
            shaderRect.width = 1;
            List<float> widths = new List<float>();
            foreach (var shader in shaders) widths.Add(newSkin.button.CalcSize(new GUIContent(shader)).x);
            shaderDropWidth = widths.Max() + newSkin.button.CalcSize(new GUIContent("TEST")).y;
        }

        private void CalcHistoryDropSize() {
            historyRect.width = 1;
            List<float> widths = new List<float>();
            List<HistoryItem> items = null;
            if (tab == SettingType.Float) items = floatHist;
            else if (tab == SettingType.Color) items = colHist;
            else if (tab == SettingType.Texture) items = texHist;
            foreach (var item in items) {
                widths.Add(newSkin.button.CalcSize(GetHistoryContent(item)).x);
            }
            historyDropWidth = widths.Max() + newSkin.button.CalcSize(new GUIContent("TEST")).y * 2;
        }

        private void InitUI() {
            newSkin = ScriptableObject.CreateInstance<GUISkin>();
            newSkin.box = new GUIStyle(GUI.skin.box);
            newSkin.label = new GUIStyle(GUI.skin.label);
            newSkin.button = new GUIStyle(GUI.skin.button);
            newSkin.window = new GUIStyle(GUI.skin.window);
            newSkin.textField = new GUIStyle(GUI.skin.textField);
            newSkin.scrollView = new GUIStyle(GUI.skin.scrollView);
            newSkin.verticalSlider = new GUIStyle(GUI.skin.verticalSlider);
            newSkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
            newSkin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar);
            newSkin.verticalSliderThumb = new GUIStyle(GUI.skin.verticalSliderThumb);
            newSkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);

            newSkin.window.stretchWidth = false;
            Texture2D newBg = new Texture2D(1,1, TextureFormat.RGBAFloat, false);
            newBg.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f, 0.3f));
            newBg.Apply();
            newSkin.verticalScrollbar.normal.background = newBg;
            newSkin.verticalScrollbar.active.background = newBg;
            newSkin.verticalScrollbar.focused.background = newBg;
            newSkin.verticalScrollbar.hover.background = newBg;
        }

        private void ScaleUI(float scale) {
            windowRect.size = new Vector2(windowRect.size.x * scale / prevScale, 1);
            shaderRect.size = new Vector2(shaderRect.size.x, windowRect.size.y * 1.3f);
            mixRect.size = new Vector2(1, windowRect.size.y);
            texRect.size = new Vector2(10, 10);
            warnRect.size *= scale / prevScale;
            prevScale = scale;
            newScale = scale;
            newScaleSlider = scale;
            newScaleText = scale.ToString("0.00");
            newScaleTextInput = newScaleText;

            int newSize = (int)(GUI.skin.font.fontSize * scale);
            newSkin.box.fontSize = newSize;
            newSkin.label.fontSize = newSize;
            newSkin.button.fontSize = newSize;
            newSkin.textField.fontSize = newSize;
            newSkin.scrollView.fontSize = newSize;
            newSkin.verticalSlider.fixedWidth = newSize;
            newSkin.horizontalSlider.fixedHeight = newSize;
            newSkin.verticalSliderThumb.fixedWidth = newSize;
            newSkin.horizontalSliderThumb.fixedHeight = newSize;
            newSkin.verticalSliderThumb.fixedHeight = Math.Max(newSkin.verticalSliderThumb.fixedWidth * 0.65f, 15);
            newSkin.horizontalSliderThumb.fixedWidth = Math.Max(newSkin.horizontalSliderThumb.fixedHeight * 0.65f, 15);

            CalcSizes();
        }

        private void ShowMessage(string _msg, float _dur = 3f) {
            infoRect.size = new Vector2(windowRect.size.x, 1);
            messageTime = Time.time;
            messageDur = _dur;
            message = _msg;
            showMessage = true;
        }

        private void PushTooltip(string _tip) {
            if (_tip.IsNullOrEmpty()) {
                if (tooltip[1] == "") tooltip[0] = "";
                tooltip[1] = "";
            } else {
                tooltip[0] = _tip;
                tooltip[1] = _tip;
            }
        }

        private void DrawTooltip(string _tip) {
            if (!_tip.IsNullOrEmpty() && ShowTooltips.Value && (shaderDrop == 0 || !shaderRect.Contains(Input.mousePosition.InvertScreenY()))) {
                var tipStyle = new GUIStyle(newSkin.box) {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                var winStyle = new GUIStyle(GUIStyle.none);
                winStyle.normal.background = newSkin.box.normal.background;
                winStyle.border = newSkin.box.border;
                winStyle.padding.top = 4;

                float defaultWidth = 270f * UIScale.Value;
                Vector2 defaultSize = tipStyle.CalcSize(new GUIContent(_tip));
                float width = defaultSize.x > defaultWidth ? defaultWidth : defaultSize.x;
                float height = tipStyle.CalcHeight(new GUIContent(_tip), width) + 10f;

                float x = Input.mousePosition.x;
                float y = Screen.height - Input.mousePosition.y + 30f;
                Rect draw = new Rect(x, y, width+2*winStyle.border.left, height);
                GUILayout.Window(592, draw, (int id) => { GUILayout.Box(_tip, tipStyle); }, new GUIContent(), winStyle);
                Redraw(1592, draw, redrawNum - 1, true);
            }
        }

        private void Redraw(int id, Rect rect, int num, bool box = false) {
            for (int i = 0; i<num; i++)
                GUI.Window(id+9277*i, rect, (x) => { }, "", (box ? newSkin.box : newSkin.window));
        }

        private void Spacer(float multiplied = 1) => GUILayout.Space(6 * multiplied * UIScale.Value);

        private void SpacerVert(int num = 1) {
            for (int i = 0; i < num; i++) GUILayout.Label("", GUILayout.Height(commonHeight));
        }

        private void SaveHistory() {
            string floatString = "";
            for (int i = 0; i < floatHist.Count; i++) {
                floatString += floatHist[i].name + "," + floatHist[i].val.ToString("0.000") + ";";
            }
            FloatHistory.Value = floatString;
            string colString = "";
            for (int i = 0; i < colHist.Count; i++) {
                colString += colHist[i].name + "," +
                    colHist[i].col.ToColor().r.ToString("0.000") + "," +
                    colHist[i].col.ToColor().g.ToString("0.000") + "," +
                    colHist[i].col.ToColor().b.ToString("0.000") + "," +
                    colHist[i].col.ToColor().a.ToString("0.000") + ";";
            }
            ColorHistory.Value = colString;
            if (SaveTextures.Value) {
                WriteToBinaryFile(GetDataPath(), texHist, false);
            }
        }

        private void ReadHistory() {
            floatHist.Clear();
            var floatParts = FloatHistory.Value.Split(';');
            for (int i = 0; i < floatParts.Length; i++) {
                if (floatParts[i].Length > 0) {
                    var currentParts = floatParts[i].Split(',');
                    floatHist.Add(new HistoryItem {
                        name = currentParts[0],
                        val = Studio.Utility.StringToFloat(currentParts[1])
                    });
                }
            }
            colHist.Clear();
            var colParts = ColorHistory.Value.Split(';');
            for (int i = 0; i < colParts.Length; i++) {
                if (colParts[i].Length > 0) {
                    var currentParts = colParts[i].Split(',');
                    colHist.Add(new HistoryItem {
                        name = currentParts[0],
                        col = new Color(
                            Studio.Utility.StringToFloat(currentParts[1]),
                            Studio.Utility.StringToFloat(currentParts[2]),
                            Studio.Utility.StringToFloat(currentParts[3]),
                            Studio.Utility.StringToFloat(currentParts[4])
                        ).ToArray()
                    });
                }
            }
            if (File.Exists(GetDataPath())) {
                texHist.Clear();
                var textures = (List<HistoryItem>)ReadFromBinaryFile(GetDataPath());
                for (int i = 0; i < textures.Count; i++) {
                    texHist.Add(textures[i]);
                }
            }
        }

        private void HistoryAppend<T>(T _value) {
            if (_value is float floatval) {
                if (setReset) {
                    HistoryAppend(setName, 0, null, null);
                } else {
                    HistoryAppend(setName, floatval, null, null);
                }
            } else if (_value is Color colval) {
                if (setReset) {
                    HistoryAppend(setName, null, new Color(0, 0, 0, 0), null);
                } else {
                    HistoryAppend(setName, null, colval, null);
                }
            } else if (_value is ScaledTex texval) {
                if (setReset) {
                    HistoryAppend(setName, null, null, new ScaledTex());
                } else {
                    HistoryAppend(setName, null, null, texval);
                }
            }
        }

        private void HistoryAppend(string name, float? val, Color? col, ScaledTex tex) {
            if (val != null) {
                if (floatHist.Count == 10) floatHist.RemoveAt(0);
                floatHist.Add(new HistoryItem { name = name, val = (float)val });
            }
            if (col != null) {
                if (colHist.Count == 10) colHist.RemoveAt(0);
                colHist.Add(new HistoryItem { name = name, col = col?.ToArray() });
            }
            if (tex != null) {
                if (texHist.Count == 10) texHist.RemoveAt(0);
                texHist.Add(new HistoryItem {
                    name = name,
                    texData = setTexAffectTex ? tex.data : null,
                    texScale = setTexAffectDims ? tex.scale : null,
                    texOffset = setTexAffectDims ? tex.offset : null
                });
            }
            SaveHistory();
            dicHistContent.Clear();
        }

        private string GetDataPath() {
            var parts = Paths.BepInExConfigPath.Split(Path.DirectorySeparatorChar).ToList();
            parts[parts.Count - 1] = $"{GUID}.data";
            return parts.Join(x => x, Path.DirectorySeparatorChar.ToString());
        }

        private GUIContent GetHistoryContent(HistoryItem item) {
            if (dicHistContent.ContainsKey(item)) return dicHistContent[item];
            GUIContent content = null;
            if (tab == SettingType.Float) {
                content = new GUIContent(item.name + ": " + item.val.ToString("0.000"));
            } else if (tab == SettingType.Color) {
                var img = new Texture2D(3 * newSkin.label.fontSize, newSkin.label.fontSize);
                for (int i = 0; i < 3 * newSkin.label.fontSize; i++)
                    for (int j = 0; j < newSkin.label.fontSize; j++)
                        img.SetPixel(i, j, item.col.ToColor());
                img.Apply();
                content = new GUIContent(" " + item.name, img);
            } else if (tab == SettingType.Texture) {
                string text = " " + item.name;
                if (item.texScale != null && item.texOffset != null) {
                    text += $" - Offset: ({item.texOffset[0]},{item.texOffset[1]}) Scale: ({item.texScale[0]},{item.texScale[1]})";
                }
                Texture2D img;
                if (item.texData != null) {
                    img = new Texture2D(1, 1);
                    img.LoadImage(item.texData);
                    float scale = (2f * newSkin.label.fontSize) / Mathf.Max(img.height, img.width);
                    img = img.ResizeTexture(TextureUtils.ImageFilterMode.Biliner, scale);
                } else {
                    img = new Texture2D(2 * newSkin.label.fontSize, newSkin.label.fontSize, TextureFormat.RGBAHalf, false);
                }
                content = new GUIContent(text, img);
            }
            if (content != null) {
                dicHistContent[item] = content;
                return content;
            }
            throw new Exception("Tried generating history item for unknown tab!");
        }

        private void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false) {
            try {
                using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create)) {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    binaryFormatter.Serialize(stream, objectToWrite);
                }
            } catch (Exception e) {
                Log(e.Source + e.Message, 3);
            }
        }

        private object ReadFromBinaryFile(string filePath) {
            try {
                using (Stream stream = File.Open(filePath, FileMode.Open)) {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    return binaryFormatter.Deserialize(stream);
                }
            } catch (Exception e) {
                Log(e.Source + e.Message, 3);
                return null;
            }
        }

        [Serializable]
        public class HistoryItem {
            public string name = "";
            public float val = 0;
            public float[] col = null;
            public byte[] texData = null;
            public float[] texScale = null;
            public float[] texOffset = null;
        }

        public class ScaledTex {
            public byte[] data = null;
            public Texture tex = null;
            public float[] offset = new float[] { 0, 0 };
            public float[] scale = new float[] { 1, 1 };

            public override string ToString() {
                return $"Texture{tex.dimension}D ({tex.width}×{tex.height}), Scale/Offset ({offset[0]},{offset[1]},{scale[0]},{scale[1]})";
            }

            public string TexString() {
                return $"Texture{tex.dimension} ({tex.width}×{tex.height})";
            }

            public string DimString() {
                return $"Scale/Offset ({offset[0]},{offset[1]},{scale[0]},{scale[1]})";
            }
        }

        public enum SettingType {
            Float,
            Color,
            Texture,
            Shader
        }
    }
}
