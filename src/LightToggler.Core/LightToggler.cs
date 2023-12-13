using BepInEx;
using BepInEx.Configuration;
using StarPlugins.Koikatu;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(LightToggler.Koikatu.LightToggler.Version)]

/// <info>
/// Plugin structure thanks to Keelhauled
/// </info>
namespace LightToggler.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, "Light Toggler", Version)]
    public class LightToggler : BaseUnityPlugin {
        public const string GUID = "starstorm.lighttoggler";
        public const string Version = "0.1.1." + BuildNumber.Version;
        public static bool updateLightPanel = false;
        private static string toLog = "";
        private static bool logNew = false;

        private void Awake() {
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(null);
            HookPatch.Init();

#if DEBUG
            Logger.LogInfo($"Plugin {GUID} is loaded!");
#endif
        }

        private void Update() {
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
            }
        }

#if DEBUG
        public static void LogThis(string str) {
            toLog = str;
            logNew = true;
        }
#endif
    }
}
