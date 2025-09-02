using TMPro;
using BepInEx;
using ChaCustom;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KKAPI.Utilities;
using System.Collections;
using BepInEx.Configuration;
using System.Collections.Generic;
using KK_Plugins.MaterialEditor;

[assembly: System.Reflection.AssemblyFileVersion(AccMover.AccMover.Version)]

namespace AccMover {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ObjImport.ObjImport.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(DynamicBoneDistributionEditor.DBDE.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(KK_Plugins.MoreOutfits.Plugin.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
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
        public const string Version = "1.1.1." + BuildNumber.Version;

        public static AccMover Instance { get; private set; }

        public static ConfigEntry<bool> IsDebug { get; private set; }

        internal static CvsAccessoryChange _cvsAccessoryChange;
        internal static CvsAccessoryCopy _cvsAccessoryCopy;
        internal static CvsClothesCopy _cvsClothesCopy;

        internal static HashSet<int> selectedCopyMove = new HashSet<int> { 0 };
        internal static HashSet<int> selectedTransform = new HashSet<int> { 1 };

        private static int prevAccLength = 0;
        internal static bool moving = false;

        private void Awake() {
            var asd = (int) AccessTools.Field(typeof(CvsAccessoryChange), "selDst").GetValue(new CvsAccessoryChange());
            Instance = this;

            IsDebug = Config.Bind("General", "Debug", false, new ConfigDescription("Log debug messages", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Maker.MakerAPI.MakerStartedLoading += (x, y) => { Setup(); };

            HookPatch.Init();

            if (IsDebug.Value) Log("Awoken!");
        }

        private void Update() {
            if (_cvsAccessoryChange != null && _cvsAccessoryChange.tglDstKind.Length != prevAccLength) {
                prevAccLength = _cvsAccessoryChange.tglDstKind.Length;
                selectedCopyMove.Clear();
                selectedCopyMove.Add(_cvsAccessoryChange.selSrc);
            }
        }

        private void Setup() {
            // Reset selections
            selectedCopyMove.Clear();
            selectedCopyMove.Add(0);
            selectedTransform.Clear();
            selectedTransform.Add(1);

            // Get root
            var accRoot = GameObject.Find("04_AccessoryTop");

            // Setup variables
            selectedCopyMove = new HashSet<int> { 0 };
            _cvsAccessoryChange = accRoot.GetComponentInChildren<CvsAccessoryChange>(true);
            _cvsAccessoryCopy = accRoot.GetComponentInChildren<CvsAccessoryCopy>(true);
            _cvsClothesCopy = accRoot.transform.parent.GetComponentInChildren<CvsClothesCopy>(true);

            // Setup selector components
            foreach (var toggle in _cvsAccessoryChange.tglSrcKind) {
                toggle.transform.GetChild(0).gameObject.AddComponent<SelectorAccMove>();
            }
            foreach (var text in Singleton<CustomAcsChangeSlot>.Instance.textSlotNames) {
                text.transform.parent.parent.gameObject.AddComponent<SelectorAccTransform>();
            }

            // Setup transfer UI buttons
            {
                GameObject originalButton = accRoot.transform.Find("tglChange/ChangeTop/rect/btnCopySlot").gameObject;
                var originalTf = originalButton.GetComponent<RectTransform>();
                float width = (originalTf.sizeDelta.x - 14) / 3;

                GameObject copy = Instantiate(originalButton, originalButton.transform.parent);
                DestroyImmediate(copy.GetComponent<Button>());
                copy.GetComponentInChildren<TextMeshProUGUI>().text = "Copy";
                var copyTf = copy.GetComponent<RectTransform>();
                copyTf.sizeDelta = new Vector2(width, copyTf.sizeDelta.y);
                GameObject move = Instantiate(copy, copy.transform.parent);
                move.GetComponentInChildren<TextMeshProUGUI>().text = "Move";
                var moveTf = move.GetComponent<RectTransform>();
                moveTf.localPosition = new Vector3(moveTf.localPosition.x + width + 7, moveTf.localPosition.y, 0);
                GameObject compact = Instantiate(copy, copy.transform.parent);
                compact.GetComponentInChildren<TextMeshProUGUI>().text = "Compact";
                var compactTf = compact.GetComponent<RectTransform>();
                compactTf.localPosition = new Vector3(compactTf.localPosition.x + 2 * (width + 7), compactTf.localPosition.y, 0);
                originalButton.SetActive(false);

                var btnCopy = copy.AddComponent<Button>();
                btnCopy.onClick.AddListener(() => { moving = false; DoTransfer(); });
                var btnMove = move.AddComponent<Button>();
                btnMove.onClick.AddListener(() => { moving = true; DoTransfer(); });
                var btnCompact = compact.AddComponent<Button>();
                btnCompact.onClick.AddListener(() => { moving = true; DoCompact(); });
            }

            // Setup selector compatibility for Copy and Transfer tabs
            {
                var tglCopy = _cvsAccessoryCopy.transform.parent.GetComponent<Toggle>();
                var tglTransfer = _cvsAccessoryChange.transform.parent.GetComponent<Toggle>();

                tglCopy.onValueChanged.AddListener(active => SelectorAccTransform.isCopy = active);
                tglTransfer.onValueChanged.AddListener(active => SelectorAccTransform.isTransfer = active);
            }

            // Setup copy dropdown
            { // Accs
                var ddDst = _cvsAccessoryCopy.ddCoordeType[0];
                var allOpt = new TMP_Dropdown.OptionData("All");
                ddDst.options.Add(allOpt);
                var tglCopy = _cvsAccessoryCopy.transform.parent.GetComponent<Toggle>();
                tglCopy.onValueChanged.AddListener(FixAllOption);
                var tglAccs = accRoot.transform.parent.parent.Find("CvsMainMenu/BaseTop/tglAccessories").GetComponent<Toggle>();
                tglAccs.onValueChanged.AddListener(FixAllOption);
                tglAccs.onValueChanged.AddListener((x) => {
                    selectedTransform.Clear();
                    selectedTransform.Add(1);
                });

                void FixAllOption(bool active) {
                    if (active && ddDst.options.IndexOf(allOpt) != ddDst.options.Count - 1) {
                        ddDst.options.Remove(allOpt);
                        ddDst.options.Add(allOpt);
                    }
                }
            }
            { // Clothes
                var ddDst = _cvsClothesCopy.ddCoordeType[0];
                var allOpt = new TMP_Dropdown.OptionData("All");
                ddDst.options.Add(allOpt);
                var tglCopy = _cvsClothesCopy.transform.parent.GetComponent<Toggle>();
                tglCopy.onValueChanged.AddListener(FixAllOption);
                var tglClothes = accRoot.transform.parent.parent.Find("CvsMainMenu/BaseTop/tglCoordinate").GetComponent<Toggle>();
                tglClothes.onValueChanged.AddListener(FixAllOption);

                void FixAllOption(bool active) {
                    if (active && ddDst.options.IndexOf(allOpt) != ddDst.options.Count - 1) {
                        ddDst.options.Remove(allOpt);
                        ddDst.options.Add(allOpt);
                    }
                }
            }

            // Setup copy button
            { // Accs
                var btnCopyGO = _cvsAccessoryCopy.btnCopy.gameObject;
                DestroyImmediate(_cvsAccessoryCopy.btnCopy);
                _cvsAccessoryCopy.btnCopy = btnCopyGO.AddComponent<Button>();
                _cvsAccessoryCopy.btnCopy.onClick.AddListener(() => {
                    var ddDst = _cvsAccessoryCopy.ddCoordeType[0];
                    if (ddDst.value == ddDst.options.Count - 1) {
                        StartCoroutine(DoAllCopy());
                    } else {
                        _cvsAccessoryCopy.CopyAcs();
                    }
                });

                IEnumerator DoAllCopy() {
                    var ddSrc = _cvsAccessoryCopy.ddCoordeType[1];
                    var ddDst = _cvsAccessoryCopy.ddCoordeType[0];
                    HookPatch.Hooks.disableTransferFuncs = true;
                    for (int i = 0; i < ddDst.options.Count - 1; i++) {
                        if (i == ddSrc.value) continue;
                        ddDst.value = i;
                        _cvsAccessoryCopy.CopyAcs();
                        yield return null;
                    }
                    ddDst.value = ddSrc.value;
                    HookPatch.Hooks.disableTransferFuncs = false;
                    _cvsAccessoryCopy.chaCtrl.ChangeCoordinateType(true);
                    _cvsAccessoryCopy.chaCtrl.Reload(false, true, true, true);
                    _cvsAccessoryCopy.CalculateUI();
                    ddDst.value = ddDst.options.Count - 1;
                }
            }
            { // Clothes
                var btnCopyGO = _cvsClothesCopy.btnCopy.gameObject;
                DestroyImmediate(_cvsClothesCopy.btnCopy);
                _cvsClothesCopy.btnCopy = btnCopyGO.AddComponent<Button>();
                _cvsClothesCopy.btnCopy.onClick.AddListener(() => {
                    var ddDst = _cvsClothesCopy.ddCoordeType[0];
                    if (ddDst.value == ddDst.options.Count - 1) {
                        StartCoroutine(DoAllCopy());
                    } else {
                        _cvsClothesCopy.CopyClothes();
                    }
                });

                IEnumerator DoAllCopy() {
                    var ddSrc = _cvsClothesCopy.ddCoordeType[1];
                    var ddDst = _cvsClothesCopy.ddCoordeType[0];
                    HookPatch.Hooks.disableTransferFuncs = true;
                    for (int i = 0; i < ddDst.options.Count - 1; i++) {
                        if (i == ddSrc.value) continue;
                        ddDst.value = i;
                        _cvsClothesCopy.CopyClothes();
                        yield return null;
                    }
                    ddDst.value = ddSrc.value;
                    HookPatch.Hooks.disableTransferFuncs = false;
                    _cvsClothesCopy.chaCtrl.ChangeCoordinateType(true);
                    _cvsClothesCopy.chaCtrl.Reload(false, true, true, true);
                    _cvsClothesCopy.CalculateUI();
                    ddDst.value = ddDst.options.Count - 1;
                }
            }
        }

        private static void DoCompact() {
            selectedCopyMove.Clear();
            int bufferedDst = _cvsAccessoryChange.selDst;
            _cvsAccessoryChange.selDst = 0;
            var accNum = _cvsAccessoryChange.tglDstKind.Where(x => x.isActiveAndEnabled).Count();
            for (int i = 0; i < accNum; i++) {
                if (_cvsAccessoryChange.chaCtrl.infoAccessory[i] != null) {
                    selectedCopyMove.Add(i);
                }
            }
            DoTransfer();
            _cvsAccessoryChange.selSrc = 0;
            _cvsAccessoryChange.selDst = bufferedDst;
            selectedCopyMove.Clear();
            selectedCopyMove.Add(0);
        }

        private static void DoTransfer() {
            // Prepare
            var accNum = _cvsAccessoryChange.tglDstKind.Where(x => x.isActiveAndEnabled).Count();
            if (accNum < _cvsAccessoryChange.selDst + selectedCopyMove.Count) {
                Instance.Log("[AccMover] Not enough space to copy/move, please add more slots!", 5);
                return;
            }
            HookPatch.Conditionals.savedA12MoveParentage.Clear();
            // Setup support variables for slot movement
            var dicMovement = new Dictionary<int, int>();
            var movements = new HashSet<KeyValuePair<int, int>>();
            var safeSlots = new HashSet<int>();
            int next = _cvsAccessoryChange.selDst;
            for (int idx = 0; idx < accNum; idx++) {
                if (selectedCopyMove.Contains(idx)) {
                    if (idx != next) {
                        dicMovement[idx] = next;
                        movements.Add(new KeyValuePair<int, int>(idx, next));
                    }
                    next++;
                } else {
                    if (idx >= _cvsAccessoryChange.selDst && idx < _cvsAccessoryChange.selDst + selectedCopyMove.Count) safeSlots.Add(idx);
                }
            }
            // Check if there's nothing to do
            if (dicMovement.Count == 0) {
                Instance.Log("[AccMover] Nothing to do!", 5);
                return;
            }
            // Prepare data for handling after accessory copy / movement
            foreach (int i in dicMovement.Keys) {
                if (HookPatch.Conditionals.A12) HookPatch.Conditionals.HandleA12Before(i, dicMovement, moving);
            }
            // Disable heavy functions when copying
            HookPatch.Hooks.disableTransferFuncs = true;
            HookPatch.Conditionals.ObjImpHooks.objImpUpdated = false;
            // Copy accessories
            bool copied = false;
            int bufferedSrc = _cvsAccessoryChange.selSrc;
            int bufferedDst = _cvsAccessoryChange.selDst;

            while (movements.Count > 0) {
                // Select next movement
                var available = movements.Where(x => safeSlots.Contains(x.Value) && !safeSlots.Contains(x.Key)).ToList();
                if (available.Count == 0) {
                    available = movements.Where(x => safeSlots.Contains(x.Value)).ToList();
                }
                available.Sort((kvp1, kvp2) => kvp2.Value - kvp1.Value);
                var kvp = available[0];
                if (kvp.Key == kvp.Value) continue;
                movements.Remove(kvp);
                if ((kvp.Key < bufferedDst + selectedCopyMove.Count) && !safeSlots.Contains(kvp.Key)) safeSlots.Add(kvp.Key);

                // Perform movement
                if (_cvsAccessoryChange.chaCtrl.infoAccessory[kvp.Key] == null) {
                    _cvsAccessoryChange.chaCtrl.ChangeAccessory(kvp.Value, 0, 0, "");
                } else {
                    _cvsAccessoryChange.selSrc = kvp.Key;
                    _cvsAccessoryChange.selDst = kvp.Value;
                    if (HookPatch.Conditionals.ObjImp) HookPatch.Conditionals.HandleObjImportBefore(kvp.Key, kvp.Value, moving);
                    try { // This trycatch courtesy of Preggo+ 7.8 or lower
                        copied = true;
                        _cvsAccessoryChange.CopyAcs();
                    } catch {
                    }
                    if (HookPatch.Conditionals.ObjImp) HookPatch.Conditionals.HandleObjImportAfter(kvp.Value);
                }
            }

            if (HookPatch.Conditionals.ObjImp && !HookPatch.Conditionals.ObjImpHooks.objImpUpdated) {
                HookPatch.Conditionals.ObjImportUpdateMeshes(_cvsAccessoryChange.chaCtrl);
            }
            if (!copied) {
                try {
                    var kvp = dicMovement.ToList()[0];
                    _cvsAccessoryChange.selSrc = kvp.Key;
                    _cvsAccessoryChange.selDst = kvp.Value;
                    _cvsAccessoryChange.CopyAcs();
                } catch {
                }
            }
            _cvsAccessoryChange.selSrc = bufferedSrc;
            _cvsAccessoryChange.selDst = bufferedDst;
            // If moving, remove appropriate accessories
            if (moving) {
                foreach (var idx in dicMovement.Keys.Where(x => !dicMovement.Values.Contains(x))) {
                    _cvsAccessoryChange.chaCtrl.ChangeAccessory(idx, 0, 0, "");
                }
            }
            // Reenable heavy functions
            HookPatch.Hooks.disableTransferFuncs = false;
            // Perform copying post-work
            _cvsAccessoryChange.chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)_cvsAccessoryChange.chaCtrl.fileStatus.coordinateType);
            _cvsAccessoryChange.chaCtrl.Reload(false, true, true, true);
            _cvsAccessoryChange.CalculateUI();
            _cvsAccessoryChange.cmpAcsChangeSlot.UpdateSlotNames();
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
                if (HookPatch.Conditionals.A12) HookPatch.Conditionals.HandleA12After(i, dicMovement);
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
