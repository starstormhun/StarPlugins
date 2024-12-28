using BepInEx;
using BepInEx.Configuration;
using KKAPI.Utilities;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(AccMover.AccMover.Version)]

namespace AccMover {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "Acc Mover", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class AccMover : BaseUnityPlugin {
        public const string GUID = "starstorm.accmover";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public static AccMover Instance { get; private set; }

        public static ConfigEntry<bool> IsDebug { get; private set; }

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("General", "Debug", false, new ConfigDescription("Log debug messages", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            HookPatch.Init();

            if (IsDebug.Value) Log("Awoken!");
        }

        private void Update() {
        }

        internal void Log(object data, int level = 0) {
            switch (level) {
                case 0:
                    Logger.LogInfo(data); return;
                case 1:
                    Logger.LogDebug(data); return;
                case 2:
                    Logger.LogWarning(data); return;
                case 3:
                    Logger.LogError(data); return;
                case 4:
                    Logger.LogFatal(data); return;
                case 5:
                    Logger.LogMessage(data); return;
                default: return;
            }
        }
    }
}
