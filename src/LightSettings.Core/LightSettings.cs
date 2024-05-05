using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KK_Plugins;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using static HSceneProc;
using static Illusion.Utils;

[assembly: System.Reflection.AssemblyFileVersion(LightSettings.Koikatu.LightSettings.Version)]

namespace LightSettings.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(Autosave.PluginGUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Light Settings", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public partial class LightSettings : BaseUnityPlugin {
        public static LightSettings Instance { get; private set; }

        public const string GUID = "starstorm.lightsettings";
        public const string Version = "0.1.0." + BuildNumber.Version;

        internal static ManualLogSource logger;
        internal static int hello = 0;

        internal static int charaLightSetCountDown = -1;

        public ConfigEntry<bool> IsDebug { get; private set; }
        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<string> CharaLightToggleType { get; private set; }

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            Enabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("Enable/disable the plugin entirely. You need to save/reload the scene after changing this. Changes take effect on Studio restart.", null, new ConfigurationManagerAttributes { Order = 10 }));
            CharaLightToggleType = Config.Bind("General", "Character light toggle", "Cramped", new ConfigDescription("How the character light on/off toggle will be handled. Changes take effect on Studio restart.", new AcceptableValueList<string>(new string[] {"None", "Cramped", "Below Vanilla"}), new ConfigurationManagerAttributes { Order = 0 }));

            Log.SetLogSource(Logger);
            logger = Logger;

            if (Enabled.Value) {
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => UIHandler.Init();
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => charaLightSetCountDown = 5;
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => {
                    var charaLight = Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true);
                    SceneDataController.charaLightData = new LightSaveData {
                        ObjectId = SceneDataController.chaLightID,
                        state = charaLight.enabled,
                        shadows = charaLight.shadows,
                        shadowResolution = charaLight.shadowResolution,
                        shadowStrength = charaLight.shadowStrength,
                        shadowBias = charaLight.shadowBias,
                        shadowNormalBias = charaLight.shadowNormalBias,
                        shadowNearPlane = charaLight.shadowNearPlane,
                        renderMode = charaLight.renderMode,
                        cullingMask = charaLight.cullingMask,
                    };
                };
                StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(SceneDataController.SaveID);
                StudioSaveLoadApi.SceneSave += (x, y) => charaLightSetCountDown = 5;
                HookPatch.Init();
            }

            if (IsDebug.Value) Log.Info($"Plugin {GUID} has awoken!");
        }

        private void Update() {
            if (!UIHandler.charaToggleMade && Enabled.Value) {
                UIHandler.MakeCharaToggle();
            }
            if (--charaLightSetCountDown == 0) {
                var charaLight = Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true);
                SceneDataController.SetLoadedData(SceneDataController.charaLightData, new List<Light> { charaLight }, true);
                UIHandler.SyncGUI(ref UIHandler.containerChara, charaLight);
            }
        }

        internal static void Hello() {
            logger.LogInfo($"Hello {++hello}!");
        }

        internal static void ChaLightToggle(bool state) {
            if (UIHandler.syncing) return;
            if (Instance.IsDebug.Value) logger.LogInfo($"Character light {(state ? "enabled" : "disabled")}!");
            Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true).enabled = state;
            SceneDataController.charaLightData.state = state;
        }

        internal static void SetLightSetting<T>(SettingType _type, T _value) {
            if (UIHandler.syncing) return;

            List<Light> lights = new List<Light>();
            bool isChaLight = false;
            if (Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.gameObject.activeSelf) {
                var list = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                if (list.Count == 0) return;
                lights.Add((list[0] as OCILight).light);
            } else if (Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.gameObject.activeSelf) {
                var list = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                if (list.Count == 0) return;
                lights = (list[0] as OCIItem).objectItem.GetComponentsInChildren<Light>(true).ToList();
                if (_type == SettingType.State && list[0] is OCIItem ociItem && _value is bool stateVal) {
                    if (stateVal) {
                        if (SceneDataController.listDisabledLights.Remove(ociItem))
                            foreach (Light light in lights)
                                SceneDataController.dicDisabledLights.Remove(light);
                    } else {
                        if (!SceneDataController.listDisabledLights.Contains(ociItem)) {
                            SceneDataController.listDisabledLights.Add(ociItem);
                            foreach (Light light in lights) SceneDataController.dicDisabledLights.Add(light, ociItem);
                        }
                    }
                }
            } else {
                lights.Add(Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true));
                isChaLight = true;
            }

            foreach (Light light in lights) {
                switch (_type) {
                    case SettingType.Type:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow type set to {_value}");
                        light.shadows = EnumParser<LightShadows>((_value as string));
                        if (isChaLight) SceneDataController.charaLightData.shadows = light.shadows;
                        break;
                    case SettingType.Resolution:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow resolution set to {_value}");
                        light.shadowResolution = EnumParser<UnityEngine.Rendering.LightShadowResolution>((_value as string));
                        if (isChaLight) SceneDataController.charaLightData.shadowResolution = light.shadowResolution;
                        break;
                    case SettingType.ShadowStrength:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow strength set to {_value}");
                        if (_value is float strVal) light.shadowStrength = strVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowStrength = light.shadowStrength;
                        break;
                    case SettingType.Bias:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow bias set to {_value}");
                        if (_value is float biasVal) light.shadowBias = biasVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowBias = light.shadowBias;
                        break;
                    case SettingType.NormalBias:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow normal bias set to {_value}");
                        if (_value is float normBiasVal) light.shadowNormalBias = normBiasVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowNormalBias = light.shadowNormalBias;
                        break;
                    case SettingType.NearPlane:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow near plane set to {_value}");
                        if (_value is float nearPlaneVal) light.shadowNearPlane = nearPlaneVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowNearPlane = light.shadowNearPlane;
                        break;
                    case SettingType.RenderMode:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light render mode set to {_value}");
                        light.renderMode = EnumParser<LightRenderMode>((_value as string));
                        if (isChaLight) SceneDataController.charaLightData.renderMode = light.renderMode;
                        break;
                    case SettingType.CullingMask:
                        if (_value is int maskVal) {
                            if ((light.cullingMask & maskVal) == 0) light.cullingMask |= maskVal;
                            else light.cullingMask &= ~maskVal;
                            if (isChaLight) SceneDataController.charaLightData.cullingMask = light.cullingMask;
                            if (Instance.IsDebug.Value) logger.LogInfo($"Light culling mask set to {light.cullingMask}");
                        }
                        break;
                    // These are exclusive to lights attached to items, since those controls needed to be recreated
                    case SettingType.LightStrength:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light intensity set to {_value}");
                        if (_value is float intensityVal) light.intensity = intensityVal;
                        break;
                    case SettingType.LightRange:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light range set to {_value}");
                        if (_value is float rangeVal) light.range = rangeVal;
                        break;
                    case SettingType.Color:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light color set to {_value}");
                        if (_value is Color colVal) light.color = colVal;
                        break;
                    case SettingType.State:
                        if (_value is bool stateVal) {
                            if (Instance.IsDebug.Value) logger.LogInfo($"Light {(stateVal ? "enabled" : "disabled")}!");
                            light.enabled = stateVal;
                        }
                        break;
                }
            }
        }

        private static T EnumParser<T>(string _val) {
            return (T)Enum.Parse(typeof(T), _val.Split(' ').Join((x) => x, ""), true);
        }

        internal enum SettingType {
            Type,
            Resolution,
            ShadowStrength,
            Bias,
            NormalBias,
            NearPlane,
            RenderMode,
            CullingMask,
            LightStrength,
            LightRange,
            Color,
            State,
        }
    }
}
