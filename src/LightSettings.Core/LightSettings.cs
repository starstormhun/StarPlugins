using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        public static LightSettings Instance { get; private set; }

        public const string GUID = "starstorm.lightsettings";
        public const string Version = "0.1.0." + BuildNumber.Version;

        internal static ManualLogSource logger;
        internal static int hello = 0;

        public ConfigEntry<bool> IsDebug { get; private set; }
        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<string> CharaLightToggleType { get; private set; }

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            Enabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("Enable/disable the plugin entirely. You need to save/reload the scene after changing this. Changes take effect on Studio restart.", null, new ConfigurationManagerAttributes { Order = 10 }));
            CharaLightToggleType = Config.Bind("General", "Character light toggle", "Cramped", new ConfigDescription("How the character light on/off toggle will be handled. Changes take effect on Studio restart.", new AcceptableValueList<string>(new string[] {"None", "Cramped", "Below Vanilla"}), new ConfigurationManagerAttributes { Order = 10 }));

            Log.SetLogSource(Logger);
            logger = Logger;

            if (Enabled.Value) {
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => UIHandler.Init();
                HookPatch.Init();
            }

            if (IsDebug.Value) Log.Info($"Plugin {GUID} has awoken!");
        }

        private void Update() {
            if (!UIHandler.charaToggleMade && Enabled.Value) {
                UIHandler.MakeCharaToggle();
            }
        }

        internal static void Hello() {
            logger.LogInfo($"Hello {++hello}!");
        }

        internal static void ChaLightToggle(bool state) {
            // TODO
        }
    }
}
