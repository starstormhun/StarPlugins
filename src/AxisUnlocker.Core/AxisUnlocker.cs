using BepInEx;
using BepInEx.Configuration;
#if KKS
using Common.KoikatsuSunshine;
#else
using Common.Koikatu;
#endif
using KKAPI.Utilities;
using UnityEngine;
using Studio;

[assembly: System.Reflection.AssemblyFileVersion(AxisUnlocker.Koikatu.AxisUnlocker.Version)]

namespace AxisUnlocker.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, "Axis Unlocker", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class AxisUnlocker : BaseUnityPlugin {
        public const string GUID = "starstorm.axisunlocker";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public static ConfigEntry<float> NewMinSize { get; set; }
        public static ConfigEntry<float> NewMaxSize { get; set; }
        public static ConfigEntry<bool> UseLogSize { get; set; }
        public static ConfigEntry<float> NewMinMove { get; set; }
        public static ConfigEntry<float> NewMaxMove { get; set; }
        public static ConfigEntry<bool> UseLogMove { get; set; }
        public static ConfigEntry<KeyboardShortcut> Hotkey { get; private set; }

        private void Awake() {
            // Options
            UseLogSize = Config.Bind("General", "Logarithmic axis size", true, new ConfigDescription("Use a logarithmic slider for the guideobject scaling factor. This enables finer control across very different sizes.", null, new ConfigurationManagerAttributes { Order = 4 }));
            UseLogSize.SettingChanged += (x, y) => UpdateSliders(_logSizeChanged:true);

            NewMinSize = Config.Bind("General", "New min size", 0.1f, new ConfigDescription("New cap value for the System/Options/Size slider", new AcceptableValueRange<float>(0.01f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            NewMinSize.SettingChanged += (x, y) => UpdateSliders();
            NewMaxSize = Config.Bind("General", "New max size", 10f, new ConfigDescription("New cap value for the System/Options/Size slider", new AcceptableValueRange<float>(4f,100f), new ConfigurationManagerAttributes { Order = 3 }));
            NewMaxSize.SettingChanged += (x, y) => UpdateSliders();

            UseLogMove = Config.Bind("General", "Logarithmic move speed", true, new ConfigDescription("Use a logarithmic slider for the object move speed factor. This enables finer control for very different speeds.", null, new ConfigurationManagerAttributes { Order = 2 }));
            UseLogMove.SettingChanged += (x, y) => UpdateSliders(_logMoveChanged:true);

            NewMinMove = Config.Bind("General", "New min speed", 0.01f, new ConfigDescription("New minimum value for the System/Options/Speed slider", new AcceptableValueRange<float>(0.001f, 0.1f), new ConfigurationManagerAttributes { Order = 1 }));
            NewMinMove.SettingChanged += (x, y) => UpdateSliders();
            NewMaxMove = Config.Bind("General", "New max speed", 10f, new ConfigDescription("New cap value for the System/Options/Speed slider", new AcceptableValueRange<float>(5f, 100f), new ConfigurationManagerAttributes { Order = 1 }));
            NewMaxMove.SettingChanged += (x, y) => UpdateSliders();

            Hotkey = Config.Bind("Hidden", "Move change hotkey", new KeyboardShortcut(KeyCode.B), new ConfigDescription("B", null, new ConfigurationManagerAttributes { Order = 0, Browsable = false }));

            // Setup Harmony hooks
            HookPatch.Init();
#if DEBUG
            Logger.LogInfo($"Plugin {GUID} is loaded!");
#endif
        }

        private void Update() {
            if (Hotkey.Value.IsPressed()) {
                if (Input.GetMouseButton(0)) {
                    ChangeMove();
                }
            }
        }

        internal static void ChangeMove() {
            float num = Input.GetAxis("Mouse X") * 0.1f;
            OptionCtrl.InputCombination inputSpeed = Singleton<Studio.Studio>.Instance.systemButtonCtrl.gameObject.GetComponentInChildren<Studio.OptionCtrl>().inputSpeed;
            float value = UseLogMove.Value ? Studio.Studio.optionSystem.manipuleteSpeed.TodB() : Studio.Studio.optionSystem.manipuleteSpeed;
            value = Mathf.Clamp(value + num, inputSpeed.min, inputSpeed.max);
            Studio.Studio.optionSystem.manipuleteSpeed = UseLogMove.Value ? value.FromdB() : value;
            inputSpeed.value = UseLogMove.Value ? Studio.Studio.optionSystem.manipuleteSpeed.TodB() : Studio.Studio.optionSystem.manipuleteSpeed;
        }

        internal static void UpdateSliders(bool _logSizeChanged = false, bool _logMoveChanged = false) {
            OptionCtrl _ctrlComponent = Singleton<Studio.Studio>.Instance.systemButtonCtrl.gameObject.GetComponentInChildren<OptionCtrl>();
            float bufferedSize = _ctrlComponent._inputSize.slider.value;
            if (UseLogSize.Value) {
                _ctrlComponent._inputSize.slider.minValue = NewMinSize.Value.TodB();
                _ctrlComponent._inputSize.slider.maxValue = NewMaxSize.Value.TodB();
                if (_logSizeChanged) _ctrlComponent._inputSize.slider.value = bufferedSize.TodB();
            } else {
                _ctrlComponent._inputSize.slider.minValue = NewMinSize.Value;
                _ctrlComponent._inputSize.slider.maxValue = NewMaxSize.Value;
                if (_logSizeChanged) _ctrlComponent._inputSize.slider.value = bufferedSize.FromdB();
            }

            float bufferedMove = _ctrlComponent.inputSpeed.slider.value;
            if (UseLogMove.Value) {
                _ctrlComponent.inputSpeed.slider.minValue = NewMinMove.Value.TodB();
                _ctrlComponent.inputSpeed.slider.maxValue = NewMaxMove.Value.TodB();
                if (_logMoveChanged) _ctrlComponent.inputSpeed.slider.value = bufferedMove.TodB();
            } else {
                _ctrlComponent.inputSpeed.slider.minValue = NewMinMove.Value;
                _ctrlComponent.inputSpeed.slider.maxValue = NewMaxMove.Value;
                if (_logMoveChanged) _ctrlComponent.inputSpeed.slider.value = bufferedMove.FromdB();
            }
        }
    }
}
