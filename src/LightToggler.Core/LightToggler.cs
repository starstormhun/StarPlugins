using BepInEx;
using BepInEx.Configuration;
#if KKS
using Common.KoikatsuSunshine;
#else
using Common.Koikatu;
#endif
using KKAPI.Studio.SaveLoad;
using Studio;
using Common.Utils;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(LightToggler.Koikatu.LightToggler.Version)]

namespace LightToggler.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, "Light Toggler", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class LightToggler : BaseUnityPlugin {
        public const string GUID = "starstorm.lighttoggler";
        public const string Version = "0.2.0." + BuildNumber.Version;
        public static bool updateLightPanel = false;

        public static ConfigEntry<bool> IsEnabled { get; set; }

#if DEBUG
        private static string toLog = "";
        private static bool logNew = false;
#endif

        protected void Awake() {
            IsEnabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("Enable or disable the plugin entirely. Requires reloading the scene to take effect.", null, new ConfigurationManagerAttributes { Order = 1 }));
            IsEnabled.SettingChanged += (x,y) => UpdateState();

            StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(null);
            if (IsEnabled.Value) HookPatch.Init();

#if DEBUG
            Logger.LogInfo($"Plugin {GUID} is loaded!");
#endif
        }

        protected void Update() {

#if DEBUG
            if (logNew) {
                logNew = false;
                Logger.LogInfo(toLog);
            }
#endif

            if (updateLightPanel) {
                //Appears useless but triggers the info update that is patched onto the end of get_ociLight
                ManipulatePanelCtrl.LightPanelInfo read = Singleton<Studio.Studio>.Instance.manipulatePanelCtrl.lightPanelInfo;
                if (read != null) {
                    OCILight light = read.mpLightCtrl.ociLight;
                }
                updateLightPanel = false;
            }
        }

#if DEBUG
        public static void LogThis(string str) {
            toLog = str;
            logNew = true;
        }
#endif

        private void UpdateState() {
            if (IsEnabled.Value) {
                HookPatch.Init();
            } else {
                HookPatch.Deactivate();
                GameObject root = GameObject.Find("CommonSpace");
                foreach (Light lightComponent in root.GetComponentsInChildren<Light>()) {
                    lightComponent.enabled = true;
                }
            }
        }
    }
}
