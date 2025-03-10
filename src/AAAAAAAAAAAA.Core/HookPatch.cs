using TMPro;
using ChaCustom;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using ExtensibleSaveFormat;
using MessagePack;

namespace AAAAAAAAAAAA {
    public static class HookPatch {
        internal static void InitMaker() {
            KKAPI.Maker.MakerAPI.MakerStartedLoading += (x, y) => { Maker.makerLoaded = false; };
            KKAPI.Maker.MakerAPI.MakerExiting += (x, y) => { Maker.makerLoaded = false; };
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += (x, y) => { Maker.makerLoaded = true; };
            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Initialising AAAAAAAAAAAA for Maker!");
            Maker.SetupHooks();
        }

        internal static void InitStudio() {
            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Initialising AAAAAAAAAAAA for Studio!");
            Studio.SetupHooks();
        }

        internal static void DeactivateMaker() {
            Maker.UnregisterHooks();
        }

        public static class Maker {
            private static Harmony _harmony;

            internal static bool makerLoaded = false;

            internal static TMP_Dropdown parentDropdown;
            internal static Toggle ponytailToggle;
            internal static Toggle lTwinToggle;
            internal static Toggle parentToggle;
            internal static int aaaaaaaaaaaaOptionIdx = -1;

            internal static bool isLoading = false;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Maker), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Create custom interface elements by piggybacking off of KK_MoreAccessoryParents's method
            [HarmonyPostfix]
            [HarmonyPatch(typeof(KK_MoreAccessoryParents.Interface), "CreateInterface")]
            private static void KKMoreAccessoryParentsInterfaceAfterCreateInterface() {
                // Get the root of all ~problems~ accessory settings
                Transform uiAccRoot = GameObject.Find("04_AccessoryTop").transform;

                // Extend background
                RectTransform parentBg = uiAccRoot.Find("AcsParentWindow/BasePanel/imgWindowBack").GetComponent<RectTransform>();
                parentBg.offsetMin += new Vector2(0, -30);

                // Add button
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Adding parent updater button...");
                Transform btnToCopy = uiAccRoot.Find("Slots/Viewport/Content/tglSlot01/Slot01Top/Scroll View/Viewport/Content/grpBtn");
                Transform btnParent = uiAccRoot.Find("AcsParentWindow/grpParent");
                var newBtn = GameObject.Instantiate(btnToCopy, btnParent);
                newBtn.name = "AAAAAAAAAAAA_Parent_Btn";
                newBtn.localPosition = btnParent.GetChild(btnParent.childCount - 2).localPosition + new Vector3(0, -35, 0);
                newBtn.GetComponentInChildren<TextMeshProUGUI>(true).text = "Update AAAAAAAAAAAA parent";
                newBtn.GetComponentInChildren<Button>(true).onClick.AddListener(() => AAAAAAAAAAAA.OnParentButtonPressed());
                var newBtnTf = newBtn.GetChild(0).GetComponent<RectTransform>();
                newBtnTf.anchorMax = new Vector2(0, 0);
                newBtnTf.offsetMin = new Vector2(0, 0);
                newBtnTf.offsetMax = new Vector2(367, 24);
                newBtnTf.localPosition = new Vector2(8, -6);

                // Add AAAAAAAAAAAA entry to MoreAccessoryParents "Other" dropdown
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Adding parent dropdown entry...");
                parentToggle = btnParent.GetChild(btnParent.childCount - 3).GetComponentInChildren<Toggle>(true);
                parentDropdown = btnParent.GetChild(btnParent.childCount - 2).GetComponentInChildren<TMP_Dropdown>(true);
                parentDropdown.options.Add(new TMP_Dropdown.OptionData("AAAAAAAAAAAA"));
                aaaaaaaaaaaaOptionIdx = parentDropdown.options.Count - 1;

                // Save Toggles for later use
                ponytailToggle = btnParent.GetChild(1).GetComponent<Toggle>();
                lTwinToggle = btnParent.GetChild(2).GetComponent<Toggle>();
            }

            // Prevent KK_MoreAccessoryParents from modifying the accessory when user selects the AAAAAAAAAAAA option
            [HarmonyPrefix]
            [HarmonyPatch(typeof(KK_MoreAccessoryParents.Interface), "OnSelectionChanged")]
            private static bool KKMoreAccessoryParentsInterfaceBeforeOnSelectionChanged() {
                if (!makerLoaded) return true;
                if (AAAAAAAAAAAA.acsParentWindow.updateWin) return false;
                if (parentToggle.isOn && parentDropdown.value == aaaaaaaaaaaaOptionIdx) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("AAAAAAAAAAAA Parentage detected!");
                    AAAAAAAAAAAA.RegisterParent();
                    return false;
                }
                return true;
            }

            // Apply saved data when switching between coordinates
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateTypeAndReload", new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
            private static bool ChaControlBeforeChangeCoordinateTypeAndReload() {
                if (!makerLoaded) return true;
                isLoading = true;
                return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateTypeAndReload", new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
            private static void ChaControlAfterChangeCoordinateTypeAndReload(ChaControl __instance) {
                if (!makerLoaded) return;
                isLoading = false;
                var ctrl = __instance.gameObject.GetComponent<CardDataController>();
                if (ctrl?.listAAAPKData?.Count > 0) {
                    var data = new PluginData();
                    data.data.Add(CardDataController.aaapkKey, MessagePackSerializer.Serialize(ctrl.listAAAPKData));
                    ctrl.AddAAAPKData(data);
                }
                AAAAAAAAAAAA.UpdateMakerTree(true, true);
                AAAAAAAAAAAA.Instance.StartCoroutine(UpdateTextLater());
                IEnumerator UpdateTextLater() {
                    for (int i = 0; i < 3; i++) yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                    if (
                        AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.chaCtrl.fileStatus.coordinateType, out var dicCoord) &&
                        dicCoord.TryGetValue(AAAAAAAAAAAA.mainMenu.ccAcsMenu.GetSelectIndex(), out string hash) &&
                        AAAAAAAAAAAA.dicMakerHashBones.TryGetValue(hash, out var parentBone)
                    ) {
                        AAAAAAAAAAAA.acsParentWindow.cvsAccessory[AAAAAAAAAAAA.mainMenu.ccAcsMenu.GetSelectIndex()].textAcsParent.text = $"A12: {parentBone.bone.name}";
                    }
                }
            }

            // Handling removing / changing accessories
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryParent")]
            private static void CvsAccessoryAfterUpdateSelectAccessoryParent(CvsAccessory __instance, int index) {
                if (!makerLoaded) return;
                if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord) && dicCoord.ContainsKey(__instance.nSlotNo)) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Moving acc#{__instance.nSlotNo} from custom parent to: {index}");
                    DoRemoveSaveBoneParentageFromDict(__instance.nSlotNo);
                    if (index == 0) {
                        lTwinToggle.isOn = true;
                        ponytailToggle.isOn = true;
                    }
                }
                AAAAAAAAAAAA.UpdateHash(__instance.nSlotNo);
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryType")]
            private static bool CvsAccessoryBeforeUpdateSelectAccessoryType(CvsAccessory __instance, int index) {
                if (!makerLoaded) return true;
                if (isLoading || !AAAAAAAAAAAA.mainMenu || !AAAAAAAAAAAA.customAcsChangeSlot) return true;
                if (AAAAAAAAAAAA.chaCtrl?.infoAccessory?[__instance.nSlotNo]?.Category - 120 == index) return true;
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"(C) Acc#{__instance.nSlotNo} changed type!");
                AAAAAAAAAAAA.RemoveParentedChildren(__instance.nSlotNo);
                if (
                    AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord) &&
                    dicCoord.ContainsKey(__instance.nSlotNo) && __instance.ddAcsType.value != 0
                ) {
                    if (__instance.tglTakeOverParent.isOn) {
                        if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{__instance.nSlotNo} has custom parent! Keeping parent due to toggle value.");
                        AAAAAAAAAAAA.UpdateMakerTree(true);
                    } else {
                        if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{__instance.nSlotNo} had custom parent! Removing...");
                        DoRemoveSaveBoneParentageFromDict(__instance.nSlotNo);
                    }
                }
                return true;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TMP_Dropdown), "value", MethodType.Setter)]
            private static bool TMPDropdownBeforeValueChanged(TMP_Dropdown __instance, int value) {
                if (!makerLoaded) return true;
                if (isLoading || !AAAAAAAAAAAA.mainMenu || !AAAAAAAAAAAA.customAcsChangeSlot) return true;
                if (__instance.transform.parent?.name != "ddCategory") return true;
                if (!int.TryParse(__instance.transform.parent?.parent?.parent?.parent?.parent?.parent?.name.Replace("tglSlot", ""), out int slot)) return true;
                if (slot >= AAAAAAAAAAAA.chaCtrl?.infoAccessory?.Length || AAAAAAAAAAAA.chaCtrl?.infoAccessory?[slot]?.Category - 120 == value) return true;
                if (slot >= AAAAAAAAAAAA.customAcsChangeSlot?.cvsAccessory?.Length) return true;
                var dropDown = AAAAAAAAAAAA.customAcsChangeSlot.cvsAccessory[slot].ddAcsType;
                if (__instance == dropDown && (value == 0 || dropDown.value != value)) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"(T) Acc#{slot} changed type!");
                    AAAAAAAAAAAA.RemoveParentedChildren(slot);
                    if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord) && dicCoord.ContainsKey(slot)) {
                        if (AAAAAAAAAAAA.acsParentWindow.cvsAccessory[slot].tglTakeOverParent.isOn) {
                            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{slot} has custom parent! Keeping parent due to toggle value.");
                            AAAAAAAAAAAA.UpdateMakerTree(true);
                        } else {
                            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{slot} had custom parent! Removing...");
                            DoRemoveSaveBoneParentageFromDict(slot);
                        }
                    }
                }
                return true;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), "ChangeAccessory", new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool) })]
            private static bool ChaControlBeforeChangeAccessory(ChaControl __instance, int slotNo, int type, int id) {
                if (!makerLoaded) return true;
                if (__instance.infoAccessory[slotNo] != null) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{slotNo} changed to different type ({type}) or kind ({id})!");
                    AAAAAAAAAAAA.RemoveParentedChildren(slotNo);
                    if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord) && dicCoord.ContainsKey(slotNo)) {
                        if (AAAAAAAAAAAA.acsParentWindow.cvsAccessory[slotNo].tglTakeOverParent.isOn) {
                            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{slotNo} has custom parent! Keeping parent due to toggle value.");
                            AAAAAAAAAAAA.UpdateMakerTree(true);
                        } else {
                            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc#{slotNo} had custom parent! Removing...");
                            DoRemoveSaveBoneParentageFromDict(slotNo);
                        }
                    }
                }
                return true;
            }
            private static void DoRemoveSaveBoneParentageFromDict(int slot) {
                if (AAAAAAAAAAAA.dicMakerModifiedParents.ContainsKey(AAAAAAAAAAAA.coordinateDropdown.value)) {
                    AAAAAAAAAAAA.dicMakerModifiedParents[AAAAAAAAAAAA.coordinateDropdown.value].Remove(slot);
                    if (AAAAAAAAAAAA.dicMakerModifiedParents[AAAAAAAAAAAA.coordinateDropdown.value].Count == 0) {
                        AAAAAAAAAAAA.dicMakerModifiedParents.Remove(AAAAAAAAAAAA.coordinateDropdown.value);
                    }
                }
            }

            // Sync UI if user switches to modified accessory
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomAcsParentWindow), "UpdateCustomUI")]
            private static void CustomAcsParentWindowAfterUpdateCustomUI(CustomAcsParentWindow __instance) {
                if (!makerLoaded) return;
                if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord) && dicCoord.ContainsKey((int)__instance.slotNo)) {
                    __instance.updateWin = true;
                    parentToggle.isOn = true;
                    parentDropdown.value = aaaaaaaaaaaaOptionIdx;
                    __instance.updateWin = false;
                }
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateAccessoryParentInfo")]
            private static void CvsAccessoryAfterUpdateAccessoryParentInfo(CvsAccessory __instance) {
                if (!makerLoaded) return;
                if (
                    AAAAAAAAAAAA.dicMakerModifiedParents != null &&
                    AAAAAAAAAAAA.coordinateDropdown?.value != null &&
                    AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord) &&
                    dicCoord.TryGetValue(__instance.nSlotNo, out var hash) &&
                    AAAAAAAAAAAA.dicMakerHashBones.TryGetValue(hash, out var parentBone)
                ) {
                    int num = __instance.ddAcsType.value - 1;
                    if (0 <= num) {
                        if (null != __instance.cusAcsParentWin) {
                            if (__instance.textAcsParent) {
                                __instance.textAcsParent.text = $"A12: {parentBone.bone.name}";
                            }
                        }
                    }
                }
            }

            // Update Bone tree whenever accessories change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), "ChangeAccessoryAsync", new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool), typeof(bool) })]
            private static void ChaControlAfterChangeAccessoryAsync() {
                if (!makerLoaded) return;
                AAAAAAAAAAAA.UpdateMakerTree();
            }

            // Add support for copying / transfering accessories
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessoryCopy), "CopyAcs")]
            private static void CvsAccessoryCopyAfterCopyAcs(CvsAccessoryCopy __instance) {
                if (!makerLoaded) return;
                // Yes, this isn't a mistake, they're stored in reverse order
                int coord1 = __instance.ddCoordeType[1].value;
                int coord2 = __instance.ddCoordeType[0].value;
                for (int i = 0; i < __instance.tglKind.Length; i++) {
                    if (__instance.tglKind[i].isOn) {
                        if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(coord1, out var dicCoord1) && dicCoord1.TryGetValue(i, out var hash)) {
                            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Copy: Copying acc#{i} data from coord [{coord1}] to [{coord2}]!");
                            if (!AAAAAAAAAAAA.dicMakerModifiedParents.ContainsKey(coord2))
                                AAAAAAAAAAAA.dicMakerModifiedParents[coord2] = new Dictionary<int, string>();
                            AAAAAAAAAAAA.dicMakerModifiedParents[coord2][i] = hash;
                        } else if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(coord2, out var dicCoord2) && dicCoord2.ContainsKey(i)) {
                            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Copy: Overwriting A12-parented acc#{i} in [{coord2}] with non-A12 accessory! Removing parent entry...");
                            dicCoord2.Remove(i);
                        }
                    }
                }
                AAAAAAAAAAAA.UpdateMakerTree(true);
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessoryChange), "CopyAcs")]
            private static bool CvsAccessoryChangeBeforeCopyAcs(CvsAccessoryChange __instance, ref List<KeyValuePair<int, string>> __state) {
                if (!makerLoaded) return true;
                int nowCoord = AAAAAAAAAAAA.coordinateDropdown.value;
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Moving acc children to ponytail slot before transfering...");
                __state = AAAAAAAAAAAA.RemoveParentedChildren(__instance.selSrc);
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Moved {__state.Count} children!");
                if (
                    AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(nowCoord, out var dicCoord) &&
                    dicCoord.TryGetValue(__instance.selSrc, out var src) &&
                    AAAAAAAAAAAA.TryGetMakerAccBone(__instance.selSrc, out var srcBone)
                ) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Moving acc to ponytail slot before transfering...");
                    var rootBone = AAAAAAAAAAAA.dicMakerTfBones[AAAAAAAAAAAA.chaCtrl.transform.Find("BodyTop/p_cf_body_bone/cf_j_root")];
                    var backupParent = AAAAAAAAAAAA.ponyBone ?? rootBone;
                    dicCoord.Remove(__instance.selSrc);
                    srcBone.SetParent(backupParent);
                    srcBone.PerformBoneUpdate();
                    __state.Add(new KeyValuePair<int, string>(__instance.selSrc, src));
                    __state.Add(new KeyValuePair<int, string>(__instance.selDst, src));
                }
                return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessoryChange), "CopyAcs")]
            private static void CvsAccessoryChangeAfterCopyAcs(CvsAccessoryChange __instance, ref List<KeyValuePair<int, string>> __state) {
                if (!makerLoaded) return;
                if (AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord)) {
                    foreach (var kvp in __state) {
                        dicCoord[kvp.Key] = kvp.Value;
                    }
                }
                AAAAAAAAAAAA.UpdateMakerTree(true);
            }
        }

        public static class Studio {
            private static Harmony _harmony;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Studio), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Apply saved data when switching between coordinates
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), "ChangeCoordinateType", new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
            private static void ChaControlAfterChangeCoordinateType(ChaControl __instance) {
                var ctrl = __instance.gameObject.GetComponent<CardDataController>();
                if (ctrl?.listAAAPKData?.Count > 0) {
                    var data = new PluginData();
                    data.data.Add(CardDataController.aaapkKey, MessagePackSerializer.Serialize(ctrl.listAAAPKData));
                    ctrl.AddAAAPKData(data);
                }
                ctrl?.LoadData();
            }
        }
    }
}
