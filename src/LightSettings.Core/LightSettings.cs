using BepInEx;
using BepInEx.Configuration;
using KKAPI.Utilities;

[assembly: System.Reflection.AssemblyFileVersion(LightSettings.Koikatu.LightSettings.Version)]

namespace LightSettings.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Light Settings", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public partial class LightSettings : BaseUnityPlugin {
        public const string GUID = "starstorm.lightsettings";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public ConfigEntry<bool> IsDebug { get; private set; }
        public ConfigEntry<bool> Enabled { get; private set; }

        private void Awake() {
            IsDebug = Config.Bind("Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            Enabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("Enable/disable the plugin entirely. You need to save/reload the scene after changing this.", null, new ConfigurationManagerAttributes { Order = 10 }));

            Log.SetLogSource(Logger);

            if (IsDebug.Value) Log.Info($"Plugin {GUID} has awoken!");
        }

        private void Update() {

        }
    }
}
