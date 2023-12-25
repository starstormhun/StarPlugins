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

        private Rect windowRect = new Rect(200, 40, 240, 170);
        private Rect helpRect = new Rect();
        private Rect setRect = new Rect();
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
        private List<float> setColNum = new List<float> { 1f, 1f, 1f, 1f };
        private string setColStringMemory = "ffffffff";
        private string setColString = "ffffffff";

        private float newScale = 1;
        private float newScalePrev = 1;
        private string newScaleText = "1.00";
        private float newScaleSlider = 1;

        private int helpPage = 0;

        private List<string> helpText = new List<string>{"To use, first choose whether the property you want to edit is a value, or a color using the buttons at the top of the UI. Afterwards you can input its name, and set the desired value/color using the fields below those.",
            "You can either type in the name of the property you want to edit, or you can click its name in the MaterialEditor UI, or click the timeline integration button that you can enable in the ME settings. Clicking these things will autofill the property name.",
            "After you have the edited-to-be property named, and its desired value set, you can click'Set Selected', or 'Set ALL'. In Studio, 'Set Selected' will modify items you currently have selected in the Workspace. Also in Studio, 'Set ALL' will modify EVERYTHING in the scene.",
            "In Character Maker, 'Set Selected' will affect only the currently edited clothing piece or accessory. When in the face or body menus, the appropriate body part will be affected instead. The 'Set ALL' button in Maker affects all of the currently edited category.",
            "Right-clicking either of these two buttons will reset the specified property of the appropriate items to the default value instead of setting the one you have currently inputted."};
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

            setColNum.FindAll(x => (x > 0));

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
                if (ColorPicker(setCol)) {
                    GUILayout.TextField(setColString, newSkin.textField);
                } else {
                    if (setCol.maxColorComponent <= 1) {
                        setColString = GUILayout.TextField(setColString, newSkin.textField);
                        try {
                            Color buffer = new Color(setCol.r, setCol.g, setCol.b, setCol.a);
                            Color colConvert = ColorConverter.ConvertFromString(setColString);
                            if ((new Vector4(setColNum[0], setColNum[1], setColNum[2], setColNum[3]) - new Vector4(colConvert.r, colConvert.g, colConvert.b, colConvert.a)).magnitude >= 1.2f / 255f) {
                                setCol = colConvert;
                                setColNum[0] = setCol.r;
                                setColNum[1] = setCol.g;
                                setColNum[2] = setCol.b;
                                setColNum[3] = setCol.a;
                                if (IsDebug.Value) {
                                    Log.Info("Color changed from: " + buffer.ToString());
                                    Log.Info("Color changed to: " + setCol.ToString());
                                }
                            }
                        } catch {
                            if (IsDebug.Value && setColStringMemory != setColString) {
                                Log.Info("Could not convert color code!");
                            }
                            setColStringMemory = setColString;
                        }
                    } else {
                        GUILayout.TextField("########", newSkin.textField);
                    }
                }
                GUILayout.Label("RGBA →", newSkin.label, GUILayout.ExpandWidth(false));
                if (GUILayout.Button("Click", Colorbutton(setCol))) {
                    setCol.r = Mathf.Clamp(setCol.r, 0, 1);
                    setCol.g = Mathf.Clamp(setCol.g, 0, 1);
                    setCol.b = Mathf.Clamp(setCol.b, 0, 1);
                    setCol.a = Mathf.Clamp(setCol.a, 0, 1);
                    void act4(Color c) {
                        setCol = c;
                        setColString = ColorConverter.ConvertToString(setCol);
                        setColNum[0] = setCol.r;
                        setColNum[1] = setCol.g;
                        setColNum[2] = setCol.b;
                        setColNum[3] = setCol.a;
                    }
                    ColorPicker(setCol, act4);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (ColorPicker(setCol)) {
                    GUILayout.Label("R", newSkin.label); GUILayout.TextField(setCol.r.ToString("0.000"), newSkin.textField);
                    GUILayout.Label("G", newSkin.label); GUILayout.TextField(setCol.g.ToString("0.000"), newSkin.textField);
                    GUILayout.Label("B", newSkin.label); GUILayout.TextField(setCol.b.ToString("0.000"), newSkin.textField);
                    GUILayout.Label("A", newSkin.label); GUILayout.TextField(setCol.a.ToString("0.000"), newSkin.textField);
                } else {
                    GUILayout.Label("R", newSkin.label); setColNum[0] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[0].ToString("0.000"), newSkin.textField));
                    GUILayout.Label("G", newSkin.label); setColNum[1] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[1].ToString("0.000"), newSkin.textField));
                    GUILayout.Label("B", newSkin.label); setColNum[2] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[2].ToString("0.000"), newSkin.textField));
                    GUILayout.Label("A", newSkin.label); setColNum[3] = Studio.Utility.StringToFloat(GUILayout.TextField(setColNum[3].ToString("0.000"), newSkin.textField));
                    setCol = new Color(setColNum[0], setColNum[1], setColNum[2], setColNum[3]);
                    if (setCol.maxColorComponent <= 1) {
                        setColString = ColorConverter.ConvertToString(setCol);
                    }
                }
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
                }
            }
            if (GUILayout.Button("Set Selected", newSkin.button)) {
                if (setName != "") {
                    if (tab == SettingType.Color)
                        SetSelectedProperties(setCol);
                    else if (tab == SettingType.Float)
                        SetSelectedProperties(setVal);
                }
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
                CalcHelpSize();
            }
            GUILayout.FlexibleSpace(); GUILayout.Label($"Page {helpPage+1}/{helpText.Count}", newSkin.label); GUILayout.FlexibleSpace();
            if (GUILayout.Button(" > ", newSkin.button)) {
                if (++helpPage == helpText.Count) helpPage--;
                CalcHelpSize();
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
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            GUI.DragWindow();
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

        private bool ColorPicker(Color col, Action<Color> act = null, bool forceClose = false) {
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Studio) {
                if (act == null) {
                    return studio.colorPalette.visible;
                }
                if (studio.colorPalette.visible || forceClose) {
                    studio.colorPalette.visible = false;
                } else {
                    studio.colorPalette._outsideVisible = true;
                    studio.colorPalette.Setup("ColorPicker", col, act, true);
                    studio.colorPalette.visible = true;
                }
                return studio.colorPalette.visible;
            }
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Maker) {
                CvsColor component = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsColor/Top").GetComponent<CvsColor>();
                if (act == null) {
                    return component.isOpen;
                }
                if (component.isOpen || forceClose) {
                    component.Close();
                } else {
                    component.Setup("ColorPicker", CvsColor.ConnectColorKind.None, col, act, true);
                }
                return component.isOpen;
            }
            return false;
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

        private void CalcHelpSize() {
            helpRect.size = new Vector2(windowRect.size.x, newSkin.label.CalcHeight(new GUIContent(helpText[helpPage]), windowRect.size.x) + newSkin.label.CalcHeight(new GUIContent("temp"), windowRect.size.x) + 10 * UIScale.Value);
        }

        private void CalcSettingSize() {
            setRect.size = new Vector2(windowRect.size.x, 2.5f * newSkin.label.CalcHeight(new GUIContent("TEST"), setRect.size.x) + 10);
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

            CalcHelpSize();
            CalcSettingSize();

        }

        private enum SettingType {
            Color,
            Float
        }
    }
}
