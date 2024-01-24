using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(LightSettings.Koikatu.LightSettings.Version)]

namespace LightSettings.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Light Settings", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class LightSettings : BaseUnityPlugin {
        public const string GUID = "starstorm.lightsettings";
        public const string Version = "1.0.0." + BuildNumber.Version;

        private void Awake() {
#if DEBUG
            Logger.LogInfo($"Plugin {GUID} is loaded!");
#endif
        }

        private void Update() {
			
        }
    }
}
