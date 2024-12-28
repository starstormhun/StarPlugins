using BepInEx;
using BepInEx.Configuration;
using ChaCustom;
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
        public const string Version = "0.1.0." + BuildNumber.Version;

        public static AccMover Instance { get; private set; }

        public static ConfigEntry<bool> IsDebug { get; private set; }

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("General", "Debug", false, new ConfigDescription("Log debug messages", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            HookPatch.Init();

            if (IsDebug.Value) Log("Awoken!");
        }

        private static void DoTransfer() {
            var transfer = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange/ChangeTop").GetComponent<CvsAccessoryChange>();
            HookPatch.Hooks.disableTransferFuncs = true;
            for (int i = 0; i < 3; i++) {
                transfer.selSrc = i;
                transfer.selDst = i + 4;
                transfer.CopyAcs();
            }
            HookPatch.Hooks.disableTransferFuncs = false;
            transfer.chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)transfer.chaCtrl.fileStatus.coordinateType);
            transfer.chaCtrl.Reload(false, true, true, true);
            transfer.CalculateUI();
            transfer.cmpAcsChangeSlot.UpdateSlotNames();
            for (int i = 0; i < 3; i++) {
                Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(i + 4, true);
            }
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
