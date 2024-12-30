using BepInEx;
using UnityEngine;
using KKAPI.Utilities;
using BepInEx.Configuration;
using KKAPI.Chara;

[assembly: System.Reflection.AssemblyFileVersion(AAAAAAAAAAAA.AAAAAAAAAAAA.Version)]

namespace AAAAAAAAAAAA {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(KK_MoreAccessoryParents.MoreAccParents.GUID, KK_MoreAccessoryParents.MoreAccParents.Version)]
    [BepInDependency(KKABMX.Core.KKABMX_Core.GUID, KKABMX.Core.KKABMX_Core.Version)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "AAAAAAAAAAAA", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public partial class AAAAAAAAAAAA : BaseUnityPlugin {
        // Actual plugin name: Attach All Accessories Anywhere, Anytime, At Any Angle And Artistic Arrangement, Allegedly
        public const string GUID = "starstorm.aaaaaaaaaaaa";
        public const string Version = "1.0.1." + BuildNumber.Version;

        public static AAAAAAAAAAAA Instance {  get; private set; }

        public static ConfigEntry<bool> IsDebug { get; private set; }

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("General", "Debug", false, new ConfigDescription("Log debug messages", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            CharacterApi.RegisterExtraBehaviour<CardDataController>(CardDataController.SaveID);

            if (KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.Studio) {
                HookPatch.InitMaker();
            } else {
                HookPatch.InitStudio();
            }

            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => { InitMaker(); };
            KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => { InitStudio(); };
        }

        private void LateUpdate() {
            MakerLateUpdate();
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
