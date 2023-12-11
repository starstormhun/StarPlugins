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
        public const string Version = "1.0.0." + BuildNumber.Version;
        private static string toLog = "";

        private void Awake() {
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(null);

            Logger.LogInfo($"Plugin {GUID} is loaded!");
        }

        private void Start() {
            
        }

        private void Update() {
            Logger.LogInfo(toLog);
        }

        public static void LogThis(string str) {
            toLog = str;
        }
    }
}
