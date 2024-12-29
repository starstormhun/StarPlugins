using BepInEx;
using ChaCustom;
using UnityEngine;
using KKAPI.Utilities;
using BepInEx.Configuration;
using System.Collections.Generic;

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
        public static ConfigEntry<bool> IsTest { get; private set; }

        internal static CvsAccessoryChange _cvsAccessoryChange;

        internal static HashSet<int> selected = new HashSet<int>();

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("General", "Debug", false, new ConfigDescription("Log debug messages", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            IsTest = Config.Bind("General", "Test", false, new ConfigDescription("This shouldn't be here, please report", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => {
                _cvsAccessoryChange = GameObject.Find("04_AccessoryTop").GetComponentInChildren<CvsAccessoryChange>(true);
            };

            HookPatch.Init();

            if (IsDebug.Value) Log("Awoken!");
        }

        // TODO modify for loops with custom slot selection
        private static void DoTransfer() {
            var transfer = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange/ChangeTop").GetComponent<CvsAccessoryChange>();
            HookPatch.Conditionals.savedMoveParentage.Clear();
            // Disable heavy functions on acc copying
            HookPatch.Hooks.disableTransferFuncs = true;
            // Setup dictionary of slot movement
            // TODO
            var dicMovement = new Dictionary<int, int>();
            for (int i = 0; i <= 3; i++) {
                dicMovement[i] = i + 5;
            }
            // Prepare data for handling after accessory copy / movement
            foreach (int i in dicMovement.Keys) {
                if (HookPatch.Conditionals.A12) HookPatch.Conditionals.HandleA12Before(i, dicMovement, IsTest.Value);
            }
            // Move accessories
            foreach (var kvp in dicMovement) {
                transfer.selSrc = kvp.Key;
                transfer.selDst = kvp.Value;
                transfer.CopyAcs();
            }
            // Reenable heavy functions
            HookPatch.Hooks.disableTransferFuncs = false;
            // Perform copying post-work
            transfer.chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)transfer.chaCtrl.fileStatus.coordinateType);
            transfer.chaCtrl.Reload(false, true, true, true);
            transfer.CalculateUI();
            transfer.cmpAcsChangeSlot.UpdateSlotNames();
            // Handle prepared data
            foreach (int i in dicMovement.Keys) {
                Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(i + 5, true);
            }
            foreach (int i in dicMovement.Keys) {
                if (HookPatch.Conditionals.A12) HookPatch.Conditionals.HandleA12After(i);
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
