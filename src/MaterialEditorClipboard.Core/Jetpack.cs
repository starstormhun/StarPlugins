using BepInEx.Configuration;
using UnityEngine;
using ChaCustom;

namespace MaterialEditorClipboard.JetPack {
    public class UI {
        public static Texture2D MakePlainTex(int _width, int _height, Color _color) {
            Color[] array = new Color[_width * _height];
            for (int i = 0; i < array.Length; i++) {
                array[i] = _color;
            }
            Texture2D texture2D = new Texture2D(_width, _height);
            texture2D.SetPixels(array);
            texture2D.Apply();
            return texture2D;
        }

        public static Rect GetResizedRect(Rect _rect) {
            Vector2 vector = GUI.matrix.MultiplyVector(new Vector2(_rect.x, _rect.y));
            Vector2 vector2 = GUI.matrix.MultiplyVector(new Vector2(_rect.width, _rect.height));
            return new Rect(vector.x, vector.y, vector2.x, vector2.y);
        }

        public class Template : MonoBehaviour {
            public int _windowRectID;
            public Rect _windowRect;
            public Rect _dragWindowRect;
            public Vector2 _windowSize = Vector2.zero;
            public Vector2 _windowPos = Vector2.zero;
            public Vector2 _windowInitPos = Vector2.zero;
            public Texture2D _windowBGtex;
            public string _windowTitle;
            public bool _hasFocus;
            public bool _passThrough;
            public bool _onAccTab;
            public int _slotIndex = -1;
            public Vector2 _ScreenRes = Vector2.zero;
            public ConfigEntry<bool> _cfgResScaleEnable;
            public float _cfgScaleFactor = 1f;
            public Vector2 _resScaleFactor = Vector2.one;
            public Matrix4x4 _resScaleMatrix;
            public bool _initStyle = true;
            public GUIStyle _windowSolid;
            public GUIStyle _buttonActive;
            public GUIStyle _label;
            public GUIStyle _labelDisabled;
            public GUIStyle _labelAlignCenter;
            public GUIStyle _labelAlignCenterCyan;
            public readonly Color _windowBG = new Color(0.5f, 0.5f, 0.5f, 1f);

            protected virtual void Awake() {
                DontDestroyOnLoad(this);
                enabled = false;
                _windowRectID = GUIUtility.GetControlID(FocusType.Passive);
                _windowPos.x = _windowInitPos.x;
                _windowPos.y = _windowInitPos.y;
                _windowBGtex = UI.MakePlainTex((int)_windowSize.x, (int)_windowSize.y, _windowBG);
                _windowRect = new Rect(_windowPos.x, _windowPos.y, _windowSize.x, _windowSize.y);
                ChangeRes();
            }

            protected virtual bool OnGUIshow() {
                return true;
            }

            protected virtual void OnGUI() {
                if (!KKAPI.Studio.StudioAPI.InsideStudio) {
                    if (Singleton<CustomBase>.Instance.customCtrl.hideFrontUI) {
                        return;
                    }
                    if (Toolbox.SceneIsOverlap()) {
                        return;
                    }
                    if (!Toolbox.SceneAddSceneName().IsNullOrEmpty() && Toolbox.SceneAddSceneName() != "CustomScene") {
                        return;
                    }
                }
                if (!OnGUIshow()) {
                    return;
                }
                if (_ScreenRes.x != (float)Screen.width || _ScreenRes.y != (float)Screen.height) {
                    ChangeRes();
                }
                if (_initStyle) {
                    ChangeRes();
                    InitStyle();
                }
                GUI.matrix = _resScaleMatrix;
                _dragWindowRect = GUILayout.Window(_windowRectID, _windowRect, new GUI.WindowFunction(DrawDragWindow), "", _windowSolid, new GUILayoutOption[0]);
                _windowRect.x = _dragWindowRect.x;
                _windowRect.y = _dragWindowRect.y;
                Event current = Event.current;
                if (current.type == EventType.MouseDown || EventType.MouseUp == current.type || EventType.MouseDrag == current.type || EventType.MouseMove == current.type) {
                    _hasFocus = false;
                }
                if ((!_passThrough || _hasFocus) && UI.GetResizedRect(_windowRect).Contains(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y))) {
                    Input.ResetInputAxes();
                }
            }

            protected virtual void DrawDragWindow(int _windowID) {
                Event current = Event.current;
                if (current.type == EventType.MouseDown || EventType.MouseUp == current.type || EventType.MouseDrag == current.type || EventType.MouseMove == current.type) {
                    _hasFocus = true;
                }
                GUI.Box(new Rect(0f, 0f, _windowSize.x, _windowSize.y), _windowBGtex);
                GUI.Box(new Rect(0f, 0f, _windowSize.x, 30f), _windowTitle, new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleCenter
                });
                if (GUI.Button(new Rect(_windowSize.x - 27f, 4f, 23f, 23f), new GUIContent("X", "Close this window"))) {
                    CloseWindow();
                }
                if (GUI.Button(new Rect(4f, 4f, 23f, 23f), new GUIContent("<", "Reset window position"))) {
                    ChangeRes();
                }
                if (GUI.Button(new Rect(27f, 4f, 23f, 23f), new GUIContent("T", "Use current window position when reset"))) {
                    SetNewPos();
                }
                DrawExtraTitleButtons();
                GUILayout.BeginVertical(new GUILayoutOption[0]);
                GUILayout.Space(10f);
                DragWindowContent();
                GUILayout.EndVertical();
                GUI.DragWindow();
            }

            protected virtual void DrawExtraTitleButtons() {
            }

            protected virtual void DragWindowContent() {
            }

            protected virtual void InitStyle() {
                _windowSolid = new GUIStyle(GUI.skin.window);
                _windowSolid.normal.background = _windowSolid.onNormal.background;
                _buttonActive = new GUIStyle(GUI.skin.button);
                _buttonActive.normal.textColor = Color.cyan;
                _buttonActive.hover.textColor = Color.cyan;
                _buttonActive.fontStyle = FontStyle.Bold;
                _label = new GUIStyle(GUI.skin.label) {
                    clipping = TextClipping.Clip,
                    wordWrap = false
                };
                _label.normal.textColor = Color.white;
                _labelDisabled = new GUIStyle(_label);
                _labelDisabled.normal.textColor = Color.grey;
                _labelAlignCenter = new GUIStyle(_label) {
                    alignment = TextAnchor.MiddleCenter
                };
                _labelAlignCenterCyan = new GUIStyle(_labelAlignCenter);
                _labelAlignCenterCyan.normal.textColor = Color.cyan;
                _initStyle = false;
            }

            protected virtual void OnEnable() {
                _hasFocus = true;
            }

            protected virtual void OnDisable() {
                _initStyle = true;
                _hasFocus = false;
            }

            protected virtual void ChangeRes() {
                _ScreenRes.x = Screen.width;
                _ScreenRes.y = Screen.height;
                _resScaleFactor.x = _ScreenRes.x / 1600f;
                _resScaleFactor.y = _ScreenRes.y / 900f;
                if (_cfgResScaleEnable.Value) {
                    _resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_resScaleFactor.x * _cfgScaleFactor, _resScaleFactor.y * _cfgScaleFactor, 1f));
                } else {
                    _resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_cfgScaleFactor, _cfgScaleFactor, 1f));
                }
                ResetPos();
            }

            protected virtual void SetNewPos() {
                if (_cfgResScaleEnable.Value) {
                    _windowPos.x = _windowRect.x * _cfgScaleFactor;
                    _windowPos.y = _windowRect.y * _cfgScaleFactor;
                } else {
                    _windowPos.x = _windowRect.x / _resScaleFactor.x * _cfgScaleFactor;
                    _windowPos.y = _windowRect.y / _resScaleFactor.y * _cfgScaleFactor;
                }
                _windowInitPos.x = _windowPos.x;
                _windowInitPos.y = _windowPos.y;
            }

            protected virtual void ResetPos() {
                _windowPos.x = _windowInitPos.x;
                _windowPos.y = _windowInitPos.y;
                if (_cfgResScaleEnable.Value) {
                    _windowRect.x = _windowPos.x / _cfgScaleFactor;
                    _windowRect.y = _windowPos.y / _cfgScaleFactor;
                    return;
                }
                _windowRect.x = _windowPos.x * _resScaleFactor.x / _cfgScaleFactor;
                _windowRect.y = _windowPos.y * _resScaleFactor.y / _cfgScaleFactor;
            }

            protected virtual void CloseWindow() {
                enabled = false;
            }
        }
    }

    public static class Toolbox {
        public static string SceneAddSceneName() {
#if KK
            return Singleton<Manager.Scene>.Instance.AddSceneName;
#else
            return Manager.Scene.AddSceneName;
#endif
        }

        public static bool SceneIsOverlap() {
#if KK
            return Singleton<Manager.Scene>.Instance.IsOverlap;
#else
            return Manager.Scene.IsOverlap;
#endif
        }
    }
}
