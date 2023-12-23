using BepInEx;
using System;
using System.Collections.Generic;
using KKAPI;
using ChaCustom;
using UnityEngine;

namespace MassShaderEditor.Koikatu {
    public partial class MassShaderEditor : BaseUnityPlugin {
        private bool isShown = false;
        private Rect windowRect = new Rect(500, 40, 240, 170);
        private Rect helpRect = new Rect(0, 0, 0, 0);
        private SettingType tab = SettingType.Slider;
        private bool isHelp = false;
        private bool isSetting = false;
        private float prevScale = 1;
        private float newScale;
        private GUISkin newSkin;

        private string setName = "";
        private float leftLim = -1;
        private float rightLim = 1;
        private string setValInputString = "";
        private float setValInput = 0;
        private float setValSlider = 0;
        private float setVal = 0;
        private List<float> setColNum = new List<float> { 1f, 1f, 1f, 1f };
        private string setColString = "ffffffff";
        private Color setCol = Color.white;

        private const string helpText = "To use, first choose whether the property you want to edit is a value, or a color. Afterwards you can input its name, and set the desired value/color. Clicking 'Set Selected' will modify shaders with an appropriately named property only on items you currently have selected. 'Set ALL' will modify all items in the scene / on the current outfit. You have been warned.";
        private void WindowFunction(int WindowID) {
            GUILayout.BeginVertical();

            // Changing tabs
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Value", newSkin.button))
                tab = SettingType.Slider;
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
            if (tab == SettingType.Slider) {
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
                            setCol = ColorConverter.ConvertFromString(setColString);
                            if (Mathf.Abs(setColNum[0] - setCol.r) >= 1.2f / 255f ||
                                Mathf.Abs(setColNum[1] - setCol.g) >= 1.2f / 255f ||
                                Mathf.Abs(setColNum[2] - setCol.b) >= 1.2f / 255f ||
                                Mathf.Abs(setColNum[3] - setCol.a) >= 1.2f / 255f) {
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
                            if (IsDebug.Value) Log.Info("Could not convert color code!");
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
            GUILayout.Button("Set ALL", allStyle);
            GUILayout.Button("Set Selected", newSkin.button);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
            if (isHelp || isSetting) windowRect.position = helpRect.position - new Vector2(windowRect.size.x+3, 0);
            if (windowRect.position.x < 0) windowRect.position -= new Vector2(windowRect.position.x, 0);
            if (windowRect.position.y < 0) windowRect.position -= new Vector2(0, windowRect.position.y);
            if (windowRect.position.x + windowRect.size.x > Screen.width) windowRect.position -= new Vector2(windowRect.position.x + windowRect.size.x - Screen.width, 0);
            if (windowRect.position.y + windowRect.size.y > Screen.height) windowRect.position -= new Vector2(0, windowRect.position.y + windowRect.size.y - Screen.height);
        }

        private void HelpFunction(int WindowID) {
            GUILayout.BeginVertical(); GUILayout.FlexibleSpace();
            GUILayout.Label(helpText, newSkin.label);
            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void SettingFunction(int WindowID) {
            GUILayout.BeginVertical();
            GUILayout.Label("GUI Scale", newSkin.label);

            GUILayout.Space(-4); GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(); GUILayout.Space(8);
            newScale = GUILayout.HorizontalSlider(newScale, 1, 3, newSkin.horizontalSlider, newSkin.horizontalSliderThumb);
            GUILayout.EndVertical();
            GUILayout.Label(newScale.ToString("0.00"), newSkin.label, GUILayout.ExpandWidth(false));
            if (GUILayout.Button("Set", newSkin.button, GUILayout.ExpandWidth(false))) {
                UIScale.Value = newScale;
                //scaled = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace(); GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private bool ColorPicker(Color col, Action<Color> act = null) {
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Studio) {
                if (act == null) {
                    return studio.colorPalette.visible;
                }
                if (studio.colorPalette.visible) {
                    studio.colorPalette.visible = false;
                } else {
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
                if (component.isOpen) {
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
            prevScale = scale;
            newScale = scale;
            int newSize = (int)(GUI.skin.font.fontSize * scale);

            newSkin.label.fontSize = newSize;
            newSkin.button.fontSize = newSize;
            newSkin.textField.fontSize = newSize;
            newSkin.horizontalSlider.fixedHeight = newSize;
            newSkin.horizontalSliderThumb.fixedHeight = newSize;
        }

        private enum SettingType {
            Color,
            Slider
        }
    }
}
