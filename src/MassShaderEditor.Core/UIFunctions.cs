using BepInEx;
using System;
using System.Linq;
using System.Collections.Generic;
using KKAPI;
using ChaCustom;
using UnityEngine;

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

        private static readonly float[] defaultSize = new float[] { 200f,40f,250f,170f};
        private Rect windowRect = new Rect(defaultSize[0], defaultSize[1], defaultSize[2], defaultSize[3]);
        private Rect mixRect = new Rect();
        private Rect helpRect = new Rect();
        private Rect setRect = new Rect();
        private Rect infoRect = new Rect();
        private Rect warnRect = new Rect(0,0,360,200);
        private Rect dropRect = new Rect(defaultSize[0], defaultSize[1], defaultSize[2]*0.9f, defaultSize[3]*1.2f);
        private Vector2 scrollPos;
        internal SettingType tab = SettingType.Float;
        private float prevScale = 1;
        private const float maxScale = 3;
        private const int redrawNum = 2;
        private GUISkin newSkin;

        private bool setReset = true;
        internal string filter = "";
        internal string setName = "";
        private float setVal = 0;
        private int setModeFloat = 0;
        private int setModeColor = 0;
        private Color setCol = Color.white;
        internal string setShader = "";
        private int setQueue = 0;
        private float setMix = 0.5f;

        private float leftLim = -1;
        private float rightLim = 1;
        private string setValInputString = "";
        private float setValInput = 0;
        private float setValSlider = 0;
        private int dropdown = 0;
        private float dropWidth = 0;
        private float commonWidth = 0;
        private string setQueueInput = "";
        private bool historyDrop = false;
        public readonly List<HistoryItem> floatHist = new List<HistoryItem>();
        public readonly List<HistoryItem> colHist = new List<HistoryItem>();

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

        private float newScale = 1;
        private string newScaleTextInput = "1.00";
        private string newScaleText = "1.00";
        private float newScaleSlider = 1;

        private int helpPage = 0;
        private readonly string[] tooltip = new string[]{ "",""};
        private delegate void OnShaderSelectFunc(string s);

        private readonly List<string> helpText = new List<string>{"To use, first choose whether the property you want to edit is a value, or a color using the buttons at the top of the UI. Afterwards you can input its name, and set the desired value/color using the fields below those.",
            "You can either type in the name of the property you want to edit, or you can click its name in the MaterialEditor UI, or click the timeline integration button that you can enable in the ME settings. Clicking these things will autofill the property name.",
            "You can also set up a shader name filter by clicking the 'Shader' label or Timeline button in MaterialEditor next to the dropdown list of the shader, but it can also be inputted manually. The filter doesn't have to be the full name of the shader. If left empty, all shaders will be edited.",
            "When modifying slider properties, you can choose from 5 operations to perform on the old value with the button row that appears: Replace, Add, Multiply, Minimum (Every affedted value will be at LEAST the set amount), or Maximum (Every affected value will be at MOST the set amount)",
            "When modifying color properties, you can also choose from 5 operations: Set(Replaces colors), Mix (Averages colors), Add 1 (Adds the individual color values and caps them at 1), Add 2 (Adds the values without capping), or Subtract(Does not subtract below 0)",
            "While mixing colors, an additional slider will appear to the left of the window which controls how much the set value is taken into consideration. At 1, the mix operation is the same as Set, and at 0, it does nothing.",
            "After filtering and naming the property, and choosing the operation, you can click 'Modify Selected', or 'Modify ALL'. In Studio, 'Modify Selected' will modify items you currently have selected in the Workspace. Also in Studio, 'Modify ALL' will modify EVERYTHING in the scene.",
            "In Character Maker, 'Modify Selected' will affect only the currently edited clothing piece or accessory. When in the face or body menus, the appropriate body part will be affected instead. The 'Modify ALL' button in Maker affects all of the currently edited category.",
            "Right-clicking either of these two buttons will reset the specified property of the appropriate items to the default value instead of setting the one you have currently inputted.",
            "Apart from modifying float and color values of shaders, you can also replace shaders with other shaders by choosing the Shader tab in the menu bar. You can filter for shaders to be modified like in the previous cases, and you can select the desired shader from the dropdown list.",
            "Autofilling the shader filter works the same in all three tabs. Additionally, you can autofill the shader to be set by SHIFT + clicking the Shader: label or timeline integration button.",
            "While replacing shaders, you can also specify the render queue of the shader. If left blank, invalid, or 0, the render queue will not be modified. Render queue can also be modified in the Value tab, by inputting its name in the property field, or clicking 'Render Queue:' in Material Editor, which autofills it for you."};
        private const string introText = "Welcome to Mass Shader Editor! To get started, I first recommend checking out the Help section, which will tell you how to best use this plugin, and any specifics on what each of the buttons and options do.\n\nTo access the help section, click the yellow '?' symbol in the top right corner of the plugin window.\n\nAfterwards, you should check out the various settings of the plugin, accessible either in the F1 menu or by clicking the cogwheel icon next to the help button. The available settings are differen in Maker and in Studio!\n\nHappy creating!";
        private const string shaderNameWrongMessage = "You need to input the full name (CASE-sensitive) of the shader to be set! The name has to be from the dropdown list.";
        private const string missingPropertyMessage = "You need to input the name of the property to be modified!";
        private const string valueExplainText = "The value to be used in the modification, according to the method chosen below.";
        private const string colorExplainText = "The color to be assigned to the specified propety.";
        private const string queueExplainText = "The render queue to set. If left empty, 0, or invalid, the render queue won't be modified.";
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
            commonWidth = Mathf.Max(new float[] { filterWidth, propertyWidth, valueWidth, shaderWidth, queueWidth });
            float halfWidth = (windowRect.width - newSkin.window.border.left - newSkin.window.border.right) / 2;

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

            // Shader property editing
            if (new List<SettingType> { SettingType.Float, SettingType.Color }.Contains(tab)) {
                // Filter
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(filterText, "Only shaders with this in their name will be edited"), newSkin.label, GUILayout.Width(commonWidth));
                filterInput = GUILayout.TextField(filterInput, newSkin.textField);
                filter = filterInput.Trim();
                GUILayout.EndHorizontal();

                // Property
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(propertyText, "The name of the shader property to be edited"), newSkin.label, GUILayout.Width(commonWidth));
                setNameInput = GUILayout.TextField(setNameInput, newSkin.textField);
                setName = setNameInput.Trim();
                GUILayout.EndHorizontal();

                // Float value
                if (tab == SettingType.Float) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(valueText, valueExplainText), newSkin.label, GUILayout.Width(commonWidth));
                    setValInputString = GUILayout.TextField(setValInputString, newSkin.textField);
                    setValInput = Studio.Utility.StringToFloat(setValInputString);
                    string indicatorText = "→ " + setVal.ToString("0.000");
                    GUILayout.Label(new GUIContent(indicatorText, valueExplainText), newSkin.label, GUILayout.Width(newSkin.label.CalcSize(new GUIContent(indicatorText)).x));
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

                    var buttonContents = new GUIContent[] { new GUIContent(" = ", "Set the property to this value."), new GUIContent(" + ", "Add to the property's existing value."),
                        new GUIContent(" × ", "Multiply the property's existing value."), new GUIContent("Min", "Set any property lower than the set value to the value."),
                        new GUIContent("Max", "Set any property higher than the set value to the value.")};
                    setModeFloat = GUILayout.SelectionGrid(setModeFloat, buttonContents, buttonContents.Length, newSkin.button);

                    GUILayout.EndHorizontal();
                }

                // Color value
                if (tab == SettingType.Color) {
                    GUILayout.BeginHorizontal();
                    string colorText = "Color #";
                    GUILayout.Label(new GUIContent(colorText, colorExplainText), newSkin.label, GUILayout.Width(newSkin.label.CalcSize(new GUIContent(colorText)).x));

                    // Text input
                    {
                        if (!setCol.Matches(setColString.ToColor()) && setCol.maxColorComponent <= 1 && setCol.a <= 1) {
                            setColString = setCol.ToHex();
                            setColStringInput = setColString;
                            setColStringInputMemory = setColStringInput;
                        }
                        setColStringInput = GUILayout.TextField(setColStringInput, newSkin.textField);
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
                        if (GUILayout.Button(new GUIContent("Click", colorExplainText), Colorbutton(setColPicker.Clamp()))) {
                            if (!isPicker) setColPicker = setColPicker.Clamp();
                            isPicker = !isPicker;
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
                        GUILayout.Label("R", newSkin.label); setColNum[0] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[0].ToString("0.000"), newSkin.textField));
                        GUILayout.Label("G", newSkin.label); setColNum[1] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[1].ToString("0.000"), newSkin.textField));
                        GUILayout.Label("B", newSkin.label); setColNum[2] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[2].ToString("0.000"), newSkin.textField));
                        GUILayout.Label("A", newSkin.label); setColNum[3] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[3].ToString("0.000"), newSkin.textField));
                        if (!buffer.Matches(setColNum)) {
                            if (IsDebug.Value) Log($"Color changed from {setCol} to {setColNum.ToColor()} based on value input!");
                            setCol = setColNum.ToColor();
                            if (setCol.maxColorComponent > 1 || setCol.a > 1) {
                                if (isPicker) {
                                    isPicker = false;
                                    ColorPicker(Color.black, null);
                                }
                                setColStringInput = "########";
                                setColStringInputMemory = "########";
                            }
                            if (buffer.Max() > 1 && setCol.maxColorComponent <= 1 && setCol.a <= 1) {
                                setColString = setCol.ToHex();
                                setColStringInput = setColString;
                                setColStringInputMemory = setColStringInput;
                            }
                        }
                    } // End value input

                    GUILayout.EndHorizontal();

                    var buttonContents = new GUIContent[] { new GUIContent("Set", "Replace the color."), new GUIContent("Mix", "Average the existing and set color. The ratio is controlled by the slider that appears to the left."),
                        new GUIContent("Add1", "Add the set color, capping values at 1."), new GUIContent("Add2", "Add the set color, without capping values."),
                        new GUIContent("Sub", "Subtract the set color, but not below 0.")};
                    setModeColor = GUILayout.SelectionGrid(setModeColor, buttonContents, buttonContents.Length, newSkin.button);

                    GUILayout.Space(1);
                }
            }

            // Shader swapping
            if (tab == SettingType.Shader) {
                // Filter
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(filterText, "Only shaders with this in their name will be edited"), newSkin.label, GUILayout.Width(commonWidth));
                    filterInput = GUILayout.TextField(filterInput, newSkin.textField);
                    filter = filterInput.Trim();
                    if (GUILayout.Button("▼", newSkin.button, GUILayout.ExpandWidth(false))) {
                        onShaderSelect = (s) => { filter = s; filterInput = s; };
                        CalcDropSize();
                        scrollPos = dropRect.position;
                        dropdown = 1;
                    }
                    GUILayout.EndHorizontal();
                }

                // Shader
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(shaderText, "Full name of the shader to be set"), newSkin.label, GUILayout.Width(commonWidth));
                    setShaderInput = GUILayout.TextField(setShaderInput, newSkin.textField);
                    setShader = setShaderInput.Trim();
                    if (GUILayout.Button("▼", newSkin.button, GUILayout.ExpandWidth(false))) {
                        onShaderSelect = (s) => { setShader = s; setShaderInput = s; };
                        CalcDropSize();
                        scrollPos = dropRect.position;
                        dropdown = 2;
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

                GUILayout.Label(" ", newSkin.label); GUILayout.Label(" ", newSkin.label);
            }

            // Action buttons
            {
                GUILayout.BeginHorizontal();
                GUIStyle allStyle = new GUIStyle(newSkin.button);
                allStyle.normal.textColor = Color.red;
                allStyle.hover.textColor = Color.red;
                if (GUILayout.Button(new GUIContent("Modify ALL", "Right click to reset ALL"), allStyle, GUILayout.MaxWidth(halfWidth))) {
                    if (new List<SettingType> { SettingType.Float, SettingType.Color }.Contains(tab))
                        if (setName != "") {
                            if (!DisableWarning.Value) {
                                showWarning = true;
                            } else {
                                if (tab == SettingType.Color)
                                    SetAllProperties(setCol);
                                else if (tab == SettingType.Float)
                                    SetAllProperties(setVal);
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
                    if (new List<SettingType> { SettingType.Float, SettingType.Color }.Contains(tab))
                        if (setName != "") {
                            if (tab == SettingType.Color)
                                SetSelectedProperties(setCol);
                            else if (tab == SettingType.Float)
                                SetSelectedProperties(setVal);
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

            if (isHelp) windowRect.position = helpRect.position - new Vector2(windowRect.size.x + 3, 0);
            if (isSetting) windowRect.position = setRect.position - new Vector2(windowRect.size.x + 3, 0);

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
                CalcSizes();
            }
            Spacer();
            if (GUILayout.Button(" < ", newSkin.button)) {
                if (--helpPage < 0) helpPage++;
                CalcSizes();
            }

            GUILayout.FlexibleSpace(); GUILayout.Label($"Page {helpPage+1}/{helpText.Count}", newSkin.label); GUILayout.FlexibleSpace();

            if (GUILayout.Button(" > ", newSkin.button)) {
                if (++helpPage == helpText.Count) helpPage--;
                CalcSizes();
            }
            Spacer();
            if (GUILayout.Button(">>", newSkin.button)) {
                helpPage = helpText.Count - 1;
                CalcSizes();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(helpText[helpPage], newSkin.label);
            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void SettingFunction(int WindowID) {
            GUILayout.BeginVertical();

            float halfWidth = (windowRect.width - newSkin.window.border.left - newSkin.window.border.right) / 2;

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
                    if (GUILayout.Button("Set", newSkin.button, GUILayout.ExpandWidth(false)))
                        UIScale.Value = newScale;

                    GUILayout.EndHorizontal(); GUILayout.Space(8);
                } // End UI Scale

                // Show tooltips
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Tooltips: {(ShowTooltips.Value ? "Yes" : "No")}", newSkin.button, GUILayout.Width(halfWidth)))
                    ShowTooltips.Value = !ShowTooltips.Value;
                GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
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

        private void DropFunction(int windowID) {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUIStyle.none, newSkin.verticalScrollbar, GUILayout.Height(windowRect.height * 1.2f), GUILayout.Width(dropWidth));
            var scrollBtnStyle = new GUIStyle(newSkin.button) {
                alignment = TextAnchor.MiddleLeft
            };
            foreach (string shader in shaders)
                if (GUILayout.Button(shader, scrollBtnStyle)) {
                    onShaderSelect.Invoke(shader);
                    dropdown = 0;
                }
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

        private void ColorPicker(Color col, Action<Color> act, bool update = false) {
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Studio) {
                if (studio.colorPalette.visible && !update) {
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
            GUIStyle gUIStyle = new GUIStyle(newSkin.button);
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            texture2D.SetPixel(0, 0, col);
            texture2D.Apply();
            gUIStyle.normal.background = texture2D;
            gUIStyle.hover = gUIStyle.normal;
            gUIStyle.onHover = gUIStyle.normal;
            gUIStyle.onActive = gUIStyle.normal;
            return gUIStyle;
        }

        private void CalcSizes() {
            helpRect.size = new Vector2(windowRect.size.x, newSkin.label.CalcHeight(new GUIContent(helpText[helpPage]), windowRect.size.x) + newSkin.label.CalcHeight(new GUIContent("temp"), windowRect.size.x) + 10 * UIScale.Value);
            setRect.size = new Vector2(windowRect.size.x, newSkin.label.CalcHeight(new GUIContent("TEST"), setRect.size.x) + 10);
            infoRect.size = new Vector2(windowRect.size.x, 10);
        }

        private void CalcDropSize() {
            List<float> widths = new List<float>();
            foreach (var shader in shaders) widths.Add(newSkin.button.CalcSize(new GUIContent(shader)).x);
            dropWidth = widths.Max() + newSkin.button.CalcSize(new GUIContent("TEST")).y;
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
            dropRect.size = new Vector2(dropRect.size.x, windowRect.size.y * 1.3f);
            mixRect.size = new Vector2(1, windowRect.size.y);
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
            if (!_tip.IsNullOrEmpty() && ShowTooltips.Value && (dropdown == 0 || !dropRect.Contains(Input.mousePosition.InvertScreenY()))) {
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

        public struct HistoryItem {
            public string name;
            public float val;
            public Color col;
        }

        public enum SettingType {
            Float,
            Color,
            Shader
        }
    }
}
