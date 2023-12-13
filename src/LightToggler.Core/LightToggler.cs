using BepInEx;
using BepInEx.Configuration;
using KeelPlugins.Koikatu;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(LightToggler.Koikatu.LightToggler.Version)]

namespace LightToggler.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, "Light Toggler", Version)]
    public class LightToggler : BaseUnityPlugin {
        public const string GUID = "starstorm.lighttoggler";
        public const string Version = "0.1.0." + BuildNumber.Version;
        public static bool updateLightPanel = false;
        private static string toLog = "";
        private static bool logNew = false;

        private void Awake() {
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(null);
            HookPatch.Init();

            //Logger.LogInfo($"Plugin {GUID} is loaded!");
        }

        private void Update() {
            if (logNew) {
                logNew = false;
                Logger.LogInfo(toLog);
            }

            if (updateLightPanel) {
                //Appears useless but triggers the info update that is patched onto the end of the get method
                ManipulatePanelCtrl.LightPanelInfo read = Singleton<Studio.Studio>.Instance.manipulatePanelCtrl.lightPanelInfo;
                if (read != null) {
                    OCILight light = read.mpLightCtrl.ociLight;
                }
            }
        }

        public static void LogThis(string str) {
            toLog = str;
            logNew = true;
        }
    }
}
