using BepInEx.Configuration;
using System.Collections;
using UnityEngine;
using BepInEx;
using System;

[assembly: System.Reflection.AssemblyFileVersion(ViewpointSwitching.ViewpointSwitching.Version)]

namespace ViewpointSwitching {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MoarCamz.MoarCamzPlugin.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "ViewpointSwitching", Version)]
    /// <info>
    /// Plugin structure thanks to Keelhauled
    /// </info>
    public class ViewpointSwitching : BaseUnityPlugin {
        public const string GUID = "starstorm.viewpointswitching";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public static ConfigEntry<int> RotateAngle { get; private set; }
        public static ConfigEntry<int> FovLimit { get; private set; }
        public static ConfigEntry<float> Magnification { get; private set; }
        public static ConfigEntry<float> MoveDuration { get; private set; }
        public static ConfigEntry<float> DoubleTapSpeed { get; private set; }

        public static ConfigEntry<KeyboardShortcut> KeyFront { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyLeft { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyTop { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyRotLeft { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyRotRight { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyRotUp { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyRotDown { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyGizmo { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyZoomIn { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyZoomOut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> KeyInvert { get; private set; }

        private Vector3 CameraRotate {
            get {
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    return Singleton<Studio.CameraControl>.Instance.cameraAngle;
                } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    return Singleton<CameraControl_Ver2>.Instance.CameraAngle;
                }
                return Vector3.zero;
            }
            set {
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    if (MoveDuration.Value == 0f) {
                        Singleton<Studio.CameraControl>.Instance.cameraAngle = value;
                    } else {
                        smoothSwitchRotateTarget = value;
                        StartCoroutine(SmoothSwitch(Singleton<Studio.CameraControl>.Instance.cameraAngle, value, (x) => {
                            Singleton<Studio.CameraControl>.Instance.cameraAngle = x;
                        }, true));
                    }
                } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    if (MoveDuration.Value == 0f) {
                        Singleton<CameraControl_Ver2>.Instance.CameraAngle = value;
                    } else {
                        smoothSwitchRotateTarget = value;
                        StartCoroutine(SmoothSwitch(Singleton<CameraControl_Ver2>.Instance.CameraAngle, value, (x) => {
                            Singleton<CameraControl_Ver2>.Instance.CameraAngle = x;
                        }, true));
                    }
                }
            }
        }
        private float CameraZoom {
            get {
                float result = 0f;
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    var studioCameraData = Singleton<Studio.CameraControl>.Instance.Export();
                    result = studioCameraData.distance.z;
                } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    var mainCameraData = Singleton<BaseCameraControl_Ver2>.Instance.GetCameraData();
                    result = mainCameraData.Dir.z;
                }
                return result;
            }
            set {
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    var studioCameraData = Singleton<Studio.CameraControl>.Instance.Export();
                    if (MoveDuration.Value == 0) {
                        studioCameraData.distance.z = value;
                        Singleton<Studio.CameraControl>.Instance.Import(studioCameraData);
                    } else {
                        StartCoroutine(SmoothSwitch(new Vector3(studioCameraData.distance.z, 0, 0), new Vector3(value, 0, 0), (x) => {
                            studioCameraData.distance.z = x.x;
                            Singleton<Studio.CameraControl>.Instance.Import(studioCameraData);
                        }));
                    }
                } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    var mainCameraData = Singleton<BaseCameraControl_Ver2>.Instance.GetCameraData();
                    if (MoveDuration.Value == 0) {
                        mainCameraData.Dir.z = value;
                        Singleton<BaseCameraControl_Ver2>.Instance.SetCameraData(mainCameraData);
                    } else {
                        StartCoroutine(SmoothSwitch(new Vector3(mainCameraData.Dir.z, 0, 0), new Vector3(value, 0, 0), (x) => {
                            mainCameraData.Dir.z = x.x;
                            Singleton<BaseCameraControl_Ver2>.Instance.SetCameraData(mainCameraData);
                        }));
                    }
                }
            }
        }
        private Vector3 CameraTarget {
            get {
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    return Singleton<Studio.CameraControl>.Instance.targetPos;
                } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    return Singleton<CameraControl_Ver2>.Instance.TargetPos;
                }
                return Vector3.zero;
            }
            set {
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    if (MoveDuration.Value == 0) {
                        Singleton<Studio.CameraControl>.Instance.targetPos = value;
                    } else {
                        StartCoroutine(SmoothSwitch(Singleton<Studio.CameraControl>.Instance.targetPos, value, (x) => {
                            Singleton<Studio.CameraControl>.Instance.targetPos = x;
                        }));
                    }
                } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    Singleton<CameraControl_Ver2>.Instance.TargetPos = value;
                    if (MoveDuration.Value == 0) {
                        Singleton<CameraControl_Ver2>.Instance.TargetPos = value;
                    } else {
                        StartCoroutine(SmoothSwitch(Singleton<CameraControl_Ver2>.Instance.TargetPos, value, (x) => {
                            Singleton<CameraControl_Ver2>.Instance.TargetPos = x;
                        }));
                    }
                }
            }
        }

        private bool smoothSwitching = false;
        private Vector3 smoothSwitchRotateTarget = Vector3.zero;
        private Vector3 cameraAngle = Vector3.zero;
        private bool isDoubleType = false;
        private bool axisSwitch = true;
        private Transform invertObj;

        private void Start() {
            CheckMoarCamz();
            BindOptions();
            HookPatch.Init();
        }

        private void Update() {
            if (KKAPI.Maker.MakerAPI.InsideMaker || KKAPI.Studio.StudioAPI.InsideStudio) {
                if (invertObj == null) {
                    GameObject camera;
                    if (KKAPI.Studio.StudioAPI.InsideStudio) {
                        camera = GameObject.Find("StudioScene/Camera/Main Camera");
                    } else {
                        camera = GameObject.Find("CustomScene/CamBase/Camera");
                    }
                    if (camera != null) {
                        invertObj = new GameObject("ViewpointSwitching_InvertTarget").transform;
                        invertObj.transform.SetParent(camera.transform, false);
                        invertObj.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
                    }
                }

                DoHotkeys();
            }
        }
        
        private void OnDestroy() {
            Destroy(invertObj);
            HookPatch.Deactivate();
        }

        private void CheckMoarCamz() {
            var plugins = gameObject.GetComponents<BaseUnityPlugin>();
            foreach (var plugin in plugins) {
                switch (plugin.Info.Metadata.GUID) {
                    case MoarCamz.MoarCamzPlugin.GUID: 
                        StartCoroutine(DoCheckMoarCamz());
                        return;
                }
            }
            IEnumerator DoCheckMoarCamz() {
                yield return null;
                yield return null;
                yield return null;
                if (
                    KKAPI.Studio.StudioAPI.InsideStudio &&
                    (MoarCamz.MoarCamzPlugin.NextCameraButton.Value.Equals(new KeyboardShortcut(KeyCode.KeypadPlus)) ||
                    MoarCamz.MoarCamzPlugin.NextCameraButton.Value.Equals(new KeyboardShortcut(KeyCode.KeypadMinus)) ||
                    MoarCamz.MoarCamzPlugin.PrevCameraButton.Value.Equals(new KeyboardShortcut(KeyCode.KeypadPlus)) ||
                    MoarCamz.MoarCamzPlugin.PrevCameraButton.Value.Equals(new KeyboardShortcut(KeyCode.KeypadMinus))) &&
                    (KeyZoomIn.Value.Equals(new KeyboardShortcut(KeyCode.KeypadPlus)) ||
                    KeyZoomIn.Value.Equals(new KeyboardShortcut(KeyCode.KeypadMinus)) ||
                    KeyZoomOut.Value.Equals(new KeyboardShortcut(KeyCode.KeypadPlus)) ||
                    KeyZoomOut.Value.Equals(new KeyboardShortcut(KeyCode.KeypadMinus)))
                ) {
                    Logger.LogMessage("[ViewpointSwitching] MoarCamz camera switching buttons will interfere with zoom in/out buttons!");
                }
            }
        }

        private void BindOptions() {
            RotateAngle = Config.Bind("General", "View adjustment amount", 15, new ConfigDescription("Amount of degrees to rotate the view by via the view adjustment keys (by default Numpad 2/4/6/8)", new AcceptableValueRange<int>(1, 90)));
            FovLimit = Config.Bind("General", "Field of View limit", 60, new ConfigDescription("Set the limit of the vanilla Field of View adjustment", new AcceptableValueRange<int>(30, 170)));
            Magnification = Config.Bind("General", "Zoom amount", 0.5f, new ConfigDescription("Adjusts how much the zoom keys zoom in / out with each press", new AcceptableValueRange<float>(0.1f, 1.5f)));
            MoveDuration = Config.Bind("General", "Move duration", 0.15f, new ConfigDescription("How long it takes to smoothly switch between views in seconds", new AcceptableValueRange<float>(0f, 0.5f)));
            DoubleTapSpeed = Config.Bind("General", "Double tap speed", 0.3f, new ConfigDescription("Delay in seconds in which pressing the front / left / top view buttons again will instead snap to back / right / bottom respectively (Equivalent to pressing the invert button afterwards)", new AcceptableValueRange<float>(0.1f, 1.0f)));

            KeyFront = Config.Bind("Keys", "Goto front view", new KeyboardShortcut(KeyCode.Keypad1));
            KeyLeft = Config.Bind("Keys", "Goto left view", new KeyboardShortcut(KeyCode.Keypad3));
            KeyTop = Config.Bind("Keys", "Goto top view", new KeyboardShortcut(KeyCode.Keypad7));
            KeyRotLeft = Config.Bind("Keys", "Rotate left", new KeyboardShortcut(KeyCode.Keypad4));
            KeyRotRight = Config.Bind("Keys", "Rotate right", new KeyboardShortcut(KeyCode.Keypad6));
            KeyRotUp = Config.Bind("Keys", "Rotate up", new KeyboardShortcut(KeyCode.Keypad8));
            KeyRotDown = Config.Bind("Keys", "Rotate down", new KeyboardShortcut(KeyCode.Keypad2));
            KeyGizmo = Config.Bind("Keys", "Jump to active gizmo", new KeyboardShortcut(KeyCode.KeypadPeriod));
            KeyZoomIn = Config.Bind("Keys", "Zoom in", new KeyboardShortcut(KeyCode.KeypadPlus));
            KeyZoomOut = Config.Bind("Keys", "Zoom out", new KeyboardShortcut(KeyCode.KeypadMinus));
            KeyInvert = Config.Bind("Keys", "Invert view", new KeyboardShortcut(KeyCode.Keypad9), new ConfigDescription("Rotates the view 180°"));
        }

        private void DoHotkeys() {
            // Rotation
            {
                bool didRotate = false;

                // Front view
                if (KeyFront.Value.IsDown()) {
                    if (isDoubleType) {
                        cameraAngle = new Vector3(0f, 0f, 0f);
                    } else {
                        cameraAngle = new Vector3(0f, 180f, 0f);
                    }
                    if (KeyGizmo.Value.IsPressed() && KKAPI.Studio.StudioAPI.InsideStudio) {
                        GameObject gizmoObj = GizmoObjStudio();
                        if (gizmoObj != null) {
                            cameraAngle = GizmoAngle(gizmoObj);
                        }
                    }
                    if (!isDoubleType) {
                        StartCoroutine(DoubleType());
                    }
                    didRotate = true;
                }

                // Left view
                if (KeyLeft.Value.IsDown()) {
                    if (isDoubleType) {
                        cameraAngle = new Vector3(0f, 90f, 0f);
                    } else {
                        cameraAngle = new Vector3(0f, 270f, 0f);
                    }
                    if (KeyGizmo.Value.IsPressed() && KKAPI.Studio.StudioAPI.InsideStudio) {
                        GameObject gizmoObj = GizmoObjStudio();
                        if (gizmoObj != null) {
                            gizmoObj.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
                            cameraAngle = GizmoAngle(gizmoObj);
                            gizmoObj.transform.localEulerAngles = Vector3.zero;
                        }
                    }
                    if (!isDoubleType) {
                        StartCoroutine(DoubleType());
                    }
                    didRotate = true;
                }

                // Top view
                if (KeyTop.Value.IsDown()) {
                    if (isDoubleType) {
                        cameraAngle = new Vector3(270f, 180f, 0f);
                    } else {
                        cameraAngle = new Vector3(90f, 180f, 0f);
                    }
                    if (KeyGizmo.Value.IsPressed() && KKAPI.Studio.StudioAPI.InsideStudio) {
                        GameObject gizmoObj = GizmoObjStudio();
                        if (gizmoObj != null) {
                            gizmoObj.transform.localEulerAngles = new Vector3(270f, 0f, 0f);
                            cameraAngle = GizmoAngle(gizmoObj);
                            gizmoObj.transform.localEulerAngles = Vector3.zero;
                        }
                    }
                    if (!isDoubleType) {
                        StartCoroutine(DoubleType());
                    }
                    didRotate = true;
                }

                // Adjustments
                {
                    if (KeyRotRight.Value.IsDown()) {
                        cameraAngle = smoothSwitching ? smoothSwitchRotateTarget : CameraRotate;
                        cameraAngle.y = (cameraAngle.y - RotateAngle.Value) % 360f;
                        didRotate = true;
                    } else if (KeyRotLeft.Value.IsDown()) {
                        cameraAngle = smoothSwitching ? smoothSwitchRotateTarget : CameraRotate;
                        cameraAngle.y = (cameraAngle.y + RotateAngle.Value) % 360f;
                        didRotate = true;
                    }
                    if (KeyRotDown.Value.IsDown()) {
                        cameraAngle = smoothSwitching ? smoothSwitchRotateTarget : CameraRotate;
                        cameraAngle.x = (cameraAngle.x - RotateAngle.Value) % 360f;
                        didRotate = true;
                    } else if (KeyRotUp.Value.IsDown()) {
                        cameraAngle = smoothSwitching ? smoothSwitchRotateTarget : CameraRotate;
                        cameraAngle.x = (cameraAngle.x + RotateAngle.Value) % 360f;
                        didRotate = true;
                    }
                }

                // Inversion
                if (KeyInvert.Value.IsDown()) {
                    if (smoothSwitching) {
                        invertObj.parent.eulerAngles = smoothSwitchRotateTarget;
                    }
                    cameraAngle = invertObj.transform.eulerAngles;
                    didRotate = true;
                }

                // Apply changes
                if (didRotate) {
                    CameraRotate = cameraAngle;
                }
            }

            // Jump to gizmo
            {
                if (KeyGizmo.Value.IsDown()) {
                    Vector3 cameraTarget = Vector3.zero;
                    if (KKAPI.Studio.StudioAPI.InsideStudio) {
                        GameObject gameObject = GizmoObjStudio();
                        if (gameObject != null) {
                            cameraTarget = gameObject.transform.position;
                        }
                    } else if (KKAPI.Maker.MakerAPI.InsideMaker) {
                        GameObject gameObject2 = GameObject.Find("customMoveAxis01");
                        GameObject gameObject3 = GameObject.Find("customMoveAxis02");
                        if (gameObject2 != null && gameObject3 != null) {
                            if (axisSwitch) {
                                cameraTarget = gameObject2.transform.position;
                            } else {
                                cameraTarget = gameObject3.transform.position;
                            }
                            axisSwitch = !axisSwitch;
                        } else {
                            axisSwitch = true;
                            if (gameObject2 != null) {
                                cameraTarget = gameObject2.transform.position;
                            }
                            if (gameObject3 != null) {
                                cameraTarget = gameObject3.transform.position;
                            }
                        }
                    }
                    CameraTarget = cameraTarget;
                }
            }

            // Zooming
            {
                if (KeyZoomIn.Value.IsDown()) {
                    CameraZoom /= 1 + Magnification.Value;
                } else if (KeyZoomOut.Value.IsDown()) {
                    CameraZoom *= 1 + Magnification.Value;
                }
            }
        }

        private IEnumerator DoubleType() {
            isDoubleType = true;
            yield return new WaitForSeconds(DoubleTapSpeed.Value);
            isDoubleType = false;
            yield break;
        }

        private Vector3 GizmoAngle(GameObject gizmoObj) {
            Vector3 gizmoRot = gizmoObj.gameObject.transform.eulerAngles;
            if (!isDoubleType) {
                gizmoRot.y = (180f + gizmoRot.y) % 360f;
                gizmoRot.x = (360f - gizmoRot.x) % 360f;
                gizmoRot.z = -gizmoRot.z % 360f;
            }
            return gizmoRot;
        }

        private GameObject GizmoObjStudio() {
            GameObject gizmoObj = GameObject.Find("M Root(Clone)/rotation");
            if (gizmoObj == null) {
                gizmoObj = GameObject.Find("M Root(Clone)/move");
                if (gizmoObj == null) {
                    gizmoObj = GameObject.Find("M Root(Clone)/scale");
                }
            }
            if (gizmoObj != null) {
                gizmoObj = gizmoObj.transform.parent.gameObject;
                gizmoObj = gizmoObj.transform.Find("Sphere").gameObject;
            }
            return gizmoObj;
        }

        private IEnumerator SmoothSwitch(Vector3 initialVal, Vector3 targetVal, Action<Vector3> act, bool isRotate = false) {
            float start = Time.realtimeSinceStartup;
            float timePassed = 0;
            Logger.LogDebug($"Start: {initialVal}");
            Logger.LogDebug($"Target: {targetVal}");
            if (isRotate && smoothSwitching) {
                smoothSwitching = false;
                yield return null;
            }
            if (isRotate) smoothSwitching = true;
            while (timePassed < MoveDuration.Value) {
                if (isRotate && !smoothSwitching) yield break;
                timePassed = Time.realtimeSinceStartup - start;
                float t = timePassed / MoveDuration.Value;
                Vector3 currentVal = new Vector3(
                    Mathf.SmoothStep(initialVal.x, targetVal.x, t),
                    Mathf.SmoothStep(initialVal.y, targetVal.y, t),
                    Mathf.SmoothStep(initialVal.z, targetVal.z, t)
                );
                Logger.LogDebug($"t: {t}");
                act.Invoke(currentVal);
                yield return null;
            }
            if (isRotate) smoothSwitching = false;
        }
    }
}
