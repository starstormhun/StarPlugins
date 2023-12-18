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

	/// Plugin structure thanks to Keelhauled

    public class LightToggler : BaseUnityPlugin {
        public const string GUID = "starstorm.lighttoggler";
        public const string Version = "0.2.0." + BuildNumber.Version;

        public static ConfigEntry<bool> IsEnabled { get; set; }

        private static GameObject lightPanel = null;

        protected void Awake() {
            IsEnabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("Enable or disable the plugin entirely.\nRequires reloading the scene to take effect.", null, new ConfigurationManagerAttributes { Order = 10 }));
            IsEnabled.SettingChanged += (x,y) => UpdateState();

            StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(null);
            if (IsEnabled.Value) HookPatch.Init();

#if DEBUG
            Logger.LogInfo($"Plugin {GUID} is loaded!");
#endif
        }

        protected void Update() {
            if (lightPanel == null) {
                Studio.Studio studio = Singleton<Studio.Studio>.Instance;
                if (studio != null) {
                    lightPanel = studio.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.gameObject;
                }
            }

            //Appears useless but triggers the synchronisation update that is patched onto the end of get_ociLight
            if (IsEnabled.Value && lightPanel != null) {
                Studio.Studio read = Singleton<Studio.Studio>.Instance;
                if (read != null && lightPanel.activeSelf) {
                    OCILight light = read.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.ociLight;
                }
            }
        }

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
