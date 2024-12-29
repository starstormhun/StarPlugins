using BepInEx;
using ChaCustom;
using UnityEngine;
using KKAPI.Utilities;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;

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

        internal static HashSet<int> selected = new HashSet<int> { 0 };

        private int prevAccLength = 0;

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("General", "Debug", false, new ConfigDescription("Log debug messages", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            IsTest = Config.Bind("General", "Test", false, new ConfigDescription("This shouldn't be here, please report", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => {
                selected = new HashSet<int> { 0 };
                _cvsAccessoryChange = GameObject.Find("04_AccessoryTop").GetComponentInChildren<CvsAccessoryChange>(true);
                foreach (var toggle in _cvsAccessoryChange.tglSrcKind) {
                    toggle.transform.GetChild(0).gameObject.AddComponent<Selector>();
                }
            };

            HookPatch.Init();

            if (IsDebug.Value) Log("Awoken!");
        }

        private void Update() {
            if (_cvsAccessoryChange != null && _cvsAccessoryChange.tglDstKind.Length != prevAccLength) {
                prevAccLength = _cvsAccessoryChange.tglDstKind.Length;
                selected.Clear();
                selected.Add(_cvsAccessoryChange.selSrc);
            }
        }

        private static void DoTransfer() {
            if (_cvsAccessoryChange.tglDstKind.Length < _cvsAccessoryChange.selDst + selected.Count) {
                Instance.Log("[AccMover] Not enough space to copy/move, please add more slots!", 5);
                return;
            }
            // Prepare
            // TODO moving!!!
            bool moving = IsTest.Value;
            var transfer = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange/ChangeTop").GetComponent<CvsAccessoryChange>();
            HookPatch.Conditionals.savedMoveParentage.Clear();
            // Disable heavy functions on acc copying
            HookPatch.Hooks.disableTransferFuncs = true;
            // Setup dictionary of slot movement
            var dicMovement = new Dictionary<int, int>();
            int next = _cvsAccessoryChange.selDst;
            var sortedSelections = selected.ToList();
            sortedSelections.Sort();
            foreach (int idx in sortedSelections) dicMovement[idx] = next++;
            // Prepare data for handling after accessory copy / movement
            foreach (int i in dicMovement.Keys) {
                if (HookPatch.Conditionals.A12) HookPatch.Conditionals.HandleA12Before(i, dicMovement, moving);
            }
            // Copy accessories
            int bufferedSrc = transfer.selSrc;
            foreach (var kvp in dicMovement) {
                if (_cvsAccessoryChange.chaCtrl.infoAccessory[kvp.Key] == null) {
                    _cvsAccessoryChange.chaCtrl.ChangeAccessory(kvp.Value, 0, 0, "");
                } else {
                    transfer.selSrc = kvp.Key;
                    transfer.selDst = kvp.Value;
                    transfer.CopyAcs();
                }
            }
            transfer.selSrc = bufferedSrc;
            // If moving, remove appropriate accessories
            if (moving) {
                foreach (var idx in dicMovement.Keys.Where(x => !dicMovement.Values.Contains(x))) {
                    _cvsAccessoryChange.chaCtrl.ChangeAccessory(idx, 0, 0, "");
                }
            }
            // Reenable heavy functions
            HookPatch.Hooks.disableTransferFuncs = false;
            // Perform copying post-work
            transfer.chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)transfer.chaCtrl.fileStatus.coordinateType);
            transfer.chaCtrl.Reload(false, true, true, true);
            transfer.CalculateUI();
            transfer.cmpAcsChangeSlot.UpdateSlotNames();
            if (moving) {
                foreach (int i in dicMovement.Keys) {
                    Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(i, true);
                }
            }
            foreach (int i in dicMovement.Values) {
                Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(i, true);
            }
            // Apply prepared data
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
