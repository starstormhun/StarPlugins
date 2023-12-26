using BepInEx;
using System;
using System.Linq;
using System.Collections.Generic;
using KKAPI;
using ChaCustom;
using UnityEngine;
using UnityEngine.UI;

namespace MassShaderEditor.Koikatu {
    public partial class MassShaderEditor : BaseUnityPlugin {
        private bool isShown = false;
        private bool isHelp = false;
        private bool isSetting = false;
        private bool showWarning = false;
        private bool showMessage = false;

        private string message = "";
        private float messageDur = 1;
        private float messageTime = 0;

        private Rect windowRect = new Rect(200, 40, 240, 170);
        private Rect helpRect = new Rect();
        private Rect setRect = new Rect();
        private Rect infoRect = new Rect();
        private Rect warnRect = new Rect(0,0,360,200);
        private SettingType tab = SettingType.Float;
        private float prevScale = 1;
        private GUISkin newSkin;

        internal string setName = "";
        private bool setReset = true;
        private float setVal = 0;
        private Color setCol = Color.white;

        private float leftLim = -1;
        private float rightLim = 1;
        private string setValInputString = "";
        private float setValInput = 0;
        private float setValSlider = 0;

        private float[] setColNum = new float[]{ 1f, 1f, 1f, 1f };
        private string setColStringInputMemory = "ffffffff";
        private string setColStringInput = "ffffffff";
        private string setColString = "ffffffff";
        internal string pickerName = "Mass Shader Editor Color";
        private bool pickerChanged = false;
        internal bool isPicker = false;
        private Color setColPicker = Color.white;

        private float newScale = 1;
        private float newScalePrev = 1;
        private string newScaleText = "1.00";
        private float newScaleSlider = 1;

        private int helpPage = 0;
        private string[] tooltip = new string[]{ "",""};

        private List<string> helpText = new List<string>{"To use, first choose whether the property you want to edit is a value, or a color using the buttons at the top of the UI. Afterwards you can input its name, and set the desired value/color using the fields below those.",
            "You can either type in the name of the property you want to edit, or you can click its name in the MaterialEditor UI, or click the timeline integration button that you can enable in the ME settings. Clicking these things will autofill the property name.",
            "After you have the edited-to-be property named, and its desired value set, you can click'Set Selected', or 'Set ALL'. In Studio, 'Set Selected' will modify items you currently have selected in the Workspace. Also in Studio, 'Set ALL' will modify EVERYTHING in the scene.",
            "In Character Maker, 'Set Selected' will affect only the currently edited clothing piece or accessory. When in the face or body menus, the appropriate body part will be affected instead. The 'Set ALL' button in Maker affects all of the currently edited category.",
            "Right-clicking either of these two buttons will reset the specified property of the appropriate items to the default value instead of setting the one you have currently inputted."};
        private const string diveFoldersText = "Whether 'Set Selected' will affect items that are inside selected folders.";
        private const string diveItemsText = "Whether 'Set Selected' will affect items that are the children of selected items.";
        private const float maxScale = 3;

        private void WindowFunction(int WindowID) {
            GUILayout.BeginVertical();

            // Changing tabs
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Value", newSkin.button))
                tab = SettingType.Float;
            if (GUILayout.Button("Color", newSkin.button))
                tab = SettingType.Color;
            if (GUILayout.Button("۞", newSkin.button, GUILayout.ExpandWidth(false))) {
                isSetting = !isSetting;
                isHelp = false;
            }
            var helpStyle = new GUIStyle(newSkin.button);
            helpStyle.normal.textColor = Color.yellow;
            helpStyle.hover.textColor = Color.yellow;
            if (GUILayout.Button("?", helpStyle, GUILayout.ExpandWidth(false))) {
                isHelp = !isHelp;
                isSetting = false;
            }
            GUILayout.EndHorizontal();

            // Property name
            GUILayout.BeginHorizontal();
            GUILayout.Label("Property Name", newSkin.label, GUILayout.ExpandWidth(false));
            setName = GUILayout.TextField(setName,newSkin.textField);
            GUILayout.EndHorizontal();

            // Choosing the slider value
            if (tab == SettingType.Float) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Value", newSkin.label, GUILayout.ExpandWidth(false));
                setValInputString = GUILayout.TextField(setValInputString, newSkin.textField);
                setValInput = Studio.Utility.StringToFloat(setValInputString);
                GUILayout.Label("→ " + setVal.ToString("0.000"), newSkin.label, GUILayout.ExpandWidth(false));
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
            }

            // Choosing the color
            if (tab == SettingType.Color) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Color #", newSkin.label, GUILayout.ExpandWidth(false));

                // Text input
                {
                    if (!setCol.Matches(setColString.ToColor()) && setCol.maxColorComponent <= 1) {
                        setColString = setCol.ToHex();
                        setColStringInput = setColString;
                        setColStringInputMemory = setColStringInput;
                    }
                    setColStringInput = GUILayout.TextField(setColStringInput, newSkin.textField);
                    try {
                        Color colConvert = setColStringInput.ToColor(); // May throw exception if hexcode is faulty
                        setColString = setColStringInput; // Since hexcode is valid, we store it
                        if (!colConvert.Matches(setCol)) {
                            if (IsDebug.Value && !pickerChanged) Log.Info($"Color changed from {setCol} to {colConvert} based on text input!");
                            pickerChanged = false;
                            setCol = colConvert;
                        }
                    } catch {
                        if (IsDebug.Value && setColStringInputMemory != setColStringInput) {
                            Log.Info("Could not convert color code!");
                        }
                    }
                    setColStringInputMemory = setColStringInput;
                } // End text input

                GUILayout.Label("RGBA →", newSkin.label, GUILayout.ExpandWidth(false));

                // Color picker
                {
                    if (!setCol.Matches(setColPicker)) {
                        setColPicker = setCol;
                        if (isPicker) ColorPicker(setColPicker, actPicker, true);
                    }
                    if (GUILayout.Button("Click", Colorbutton(setColPicker.Clamp()))) {
                        if (!isPicker) setColPicker = setColPicker.Clamp();
                        isPicker = !isPicker;
                        ColorPicker(setColPicker, actPicker);
                    }
                    void actPicker(Color c) {
                        if (IsDebug.Value) Log.Info($"Color changed from {setCol} to {c} based on picker!");
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
                        if (IsDebug.Value) Log.Info($"Color changed from {setCol} to {setColNum.ToColor()} based on value input!");
                        setCol = setColNum.ToColor();
                        if (setCol.maxColorComponent > 1) {
                            if (isPicker) {
                                isPicker = false;
                                ColorPicker(Color.black, null);
                            }
                            setColStringInput = "########";
                            setColStringInputMemory = "########";
                        }
                        if (buffer.Max() > 1 && setCol.maxColorComponent <= 1) {
                            setColString = setCol.ToHex();
                            setColStringInput = setColString;
                            setColStringInputMemory = setColStringInput;
                        }
                    }
                } // End value input

                GUILayout.EndHorizontal();
            }

            // Action buttons
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUIStyle allStyle = new GUIStyle(newSkin.button);
            allStyle.normal.textColor = Color.red;
            allStyle.hover.textColor = Color.red;
            if (GUILayout.Button("Set ALL", allStyle)) {
                if (setName != "") {
                    if (!DisableWarning.Value) {
                        showWarning = true;
                    } else {
                        if (tab == SettingType.Color)
                            SetAllProperties(setCol);
                        else if (tab == SettingType.Float)
                            SetAllProperties(setVal);
                    }
                } else ShowMessage("You need to set a property name to edit!");
            }
            if (GUILayout.Button("Set Selected", newSkin.button)) {
                if (setName != "") {
                    if (tab == SettingType.Color)
                        SetSelectedProperties(setCol);
                    else if (tab == SettingType.Float)
                        SetSelectedProperties(setVal);
                } else ShowMessage("You need to set a property name to edit!");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();

            if (isHelp) windowRect.position = helpRect.position - new Vector2(windowRect.size.x + 3, 0);
            if (isSetting) windowRect.position = setRect.position - new Vector2(windowRect.size.x + 3, 0);

            if (windowRect.position.x < 0) windowRect.position -= new Vector2(windowRect.position.x, 0);
            if (windowRect.position.y < 0) windowRect.position -= new Vector2(0, windowRect.position.y);
            if (windowRect.position.x + windowRect.size.x > Screen.width) windowRect.position -= new Vector2(windowRect.position.x + windowRect.size.x - Screen.width, 0);
            if (windowRect.position.y + windowRect.size.y > Screen.height) windowRect.position -= new Vector2(0, windowRect.position.y + windowRect.size.y - Screen.height);
        }

        private void HelpFunction(int WindowID) {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(" < ", newSkin.button)) {
                if (--helpPage < 0) helpPage++;
                CalcSizes();
            }
            GUILayout.FlexibleSpace(); GUILayout.Label($"Page {helpPage+1}/{helpText.Count}", newSkin.label); GUILayout.FlexibleSpace();
            if (GUILayout.Button(" > ", newSkin.button)) {
                if (++helpPage == helpText.Count) helpPage--;
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
            GUILayout.Label("GUI Scale", newSkin.label);

            GUILayout.Space(-4); GUILayout.BeginHorizontal();
            newScaleText = GUILayout.TextField(newScaleText, newSkin.textField, GUILayout.ExpandWidth(false));
            float newScaleTemp = Studio.Utility.StringToFloat(newScaleText);
            if (float.TryParse(newScaleText, out _)) {
                newScale = Mathf.Clamp(newScaleTemp, 1, maxScale);
                newScaleSlider = newScale;
                newScaleText = newScale.ToString("0.00");
            }

            GUILayout.BeginVertical(); GUILayout.Space(8);
            if (Mathf.Abs(newScaleSlider - newScale) > 1E-06) newScaleSlider = newScale;
            newScaleSlider = GUILayout.HorizontalSlider(newScaleSlider, 1, maxScale, newSkin.horizontalSlider, newSkin.horizontalSliderThumb);
            if (Mathf.Abs(newScaleSlider - newScale) > 1E-06) newScale = newScaleSlider;
            if (Mathf.Abs(newScale - newScalePrev) > 1E-06) newScaleText = newScale.ToString("0.00");
            newScalePrev = newScale;
            GUILayout.EndVertical();

            GUILayout.Label("→ " + newScale.ToString("0.00"), newSkin.label, GUILayout.ExpandWidth(false));

            if (GUILayout.Button("Set", newSkin.button, GUILayout.ExpandWidth(false)))
                UIScale.Value = newScale;
            GUILayout.EndHorizontal(); GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent($"Dive folders: {(DiveFolders.Value ? "Yes" : "No")}", diveFoldersText), newSkin.button))
                DiveFolders.Value = !DiveFolders.Value;
            if (GUILayout.Button(new GUIContent($"Dive items: {(DiveItems.Value ? "Yes" : "No")}", diveItemsText), newSkin.button))
                DiveItems.Value = !DiveItems.Value;
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
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
            GUILayout.BeginHorizontal(); GUILayout.Label($"Are you sure you want to {(setReset?"re":"")}set the \"{setName}\" property of ALL {items}{(setReset?"":$" to {value}")}?", newSkin.label); GUILayout.EndHorizontal();
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
            GUILayout.Label("Welcome to Mass Shader Editor! To get started, I first recommend checking out the Help section, which will tell you how to best use this plugin, and any specifics on what each of the buttons and options do.\nTo access the help section, click the yellow '?' symbol in the top right corner of the plugin window.\nHappy creating!", newSkin.label);
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            var buttonStyle = new GUIStyle(newSkin.button);
            buttonStyle.fontSize = (int)(buttonStyle.fontSize * 1.25f);
            buttonStyle.fixedHeight = buttonStyle.CalcHeight(new GUIContent("test"), 150) * 1.5f;
            buttonStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("OK", buttonStyle))
                IntroShown.Value = true;
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace(); GUILayout.EndVertical(); GUILayout.Space(5 * UIScale.Value); GUILayout.EndHorizontal();
        }

        private void InfoFunction(int windowID) {
            var msgStyle = new GUIStyle(newSkin.label);
            msgStyle.normal.textColor = Color.yellow;
            GUILayout.Label(message, msgStyle);
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
            setRect.size = new Vector2(windowRect.size.x, 4f * newSkin.label.CalcHeight(new GUIContent("TEST"), setRect.size.x) + 10);
            infoRect.size = new Vector2(windowRect.size.x, newSkin.label.CalcHeight(new GUIContent(message), infoRect.size.x)+10);
        }

        private void InitUI() {
            newSkin = new GUISkin {
                label = new GUIStyle(GUI.skin.label),
                button = new GUIStyle(GUI.skin.button),
                window = new GUIStyle(GUI.skin.window),
                textField = new GUIStyle(GUI.skin.textField),
                horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider),
                horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            };
        }

        private void ScaleUI(float scale) {
            windowRect.size = new Vector2(windowRect.size.x * scale / prevScale, (windowRect.size.y + (prevScale - 1) * 90) * scale / prevScale - (scale - 1) * 90);
            warnRect.size *= scale / prevScale;
            prevScale = scale;
            newScale = scale;
            newScaleSlider = scale;
            newScaleText = scale.ToString("0.00");
            int newSize = (int)(GUI.skin.font.fontSize * scale);

            newSkin.label.fontSize = newSize;
            newSkin.button.fontSize = newSize;
            newSkin.textField.fontSize = newSize;
            newSkin.horizontalSlider.fixedHeight = newSize;
            newSkin.horizontalSliderThumb.fixedHeight = newSize;

            CalcSizes();
        }

        private void ShowMessage(string _msg, float _dur = 2.5f) {
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
            if (!_tip.IsNullOrEmpty()) {
                var tipStyle = new GUIStyle(newSkin.button);
                tipStyle.normal.background = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
                tipStyle.normal.background.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
                tipStyle.normal.background.Apply();
                tipStyle.wordWrap = true;
                tipStyle.alignment = TextAnchor.MiddleCenter;
                float width = 270f * UIScale.Value;
                float height = tipStyle.CalcHeight(new GUIContent(_tip), width) + 10f;
                float x = Input.mousePosition.x;
                float y = Screen.height - Input.mousePosition.y + 30f;
                Rect draw = new Rect(x, y, width, height);
                GUILayout.Window(590, draw, (int id) => GUILayout.Box( _tip, tipStyle), new GUIContent(), GUIStyle.none);
            }
        }

        private enum SettingType {
            Color,
            Float
        }
    }
}
