using TMPro;
using BepInEx;
using ChaCustom;
using StrayTech;
using HarmonyLib;
using MessagePack;
using System.Linq;
using UnityEngine;
using AAAAAAAAAAAA;
using UnityEngine.UI;
using System.Collections;
using System.Reflection.Emit;
using System.Collections.Generic;
using DynamicBoneDistributionEditor;

namespace AccMover {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
            Conditionals.Setup();
        }

        public static class Hooks {
            private static Harmony _harmony;
            internal static bool disableTransferFuncs = false;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Disable vanilla CopyAcs() of Transfer in favor of adjusted one
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CvsAccessoryChange), "CopyAcs")]
            private static IEnumerable<CodeInstruction> CvsAccessoryChangeCopyAcsTranspiler(IEnumerable<CodeInstruction> instructions) {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(CvsAccessoryChangeCopyAcsReplacement)));
                yield return new CodeInstruction(OpCodes.Ret);
            }
            private static void CvsAccessoryChangeCopyAcsReplacement() {
                CvsAccessoryChange __instance = AccMover._cvsAccessoryChange;
                var bytes = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(__instance.accessory.parts[__instance.selSrc]);
                __instance.accessory.parts[__instance.selDst] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(bytes);
                if (__instance.tglReverse.isOn && !AccMover.moving) {
                    string reverseParent = ChaAccessoryDefine.GetReverseParent(__instance.accessory.parts[__instance.selDst].parentKey);
                    if (string.Empty != reverseParent) {
                        __instance.accessory.parts[__instance.selDst].parentKey = reverseParent;
                    }
                }
                if (!disableTransferFuncs) {
                    __instance.chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)__instance.chaCtrl.fileStatus.coordinateType);
                    __instance.chaCtrl.Reload(false, true, true, true);
                    __instance.CalculateUI();
                    __instance.cmpAcsChangeSlot.UpdateSlotNames();
                    Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(__instance.selDst, true);
                }
            }

            // Disable vanilla CopyAcs() of Copy in favor of adjusted one
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CvsAccessoryCopy), "CopyAcs")]
            private static IEnumerable<CodeInstruction> CvsAccessoryCopyCopyAcsTranspiler(IEnumerable<CodeInstruction> instructions) {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(CvsAccessoryCopyCopyAcsReplacement)));
                yield return new CodeInstruction(OpCodes.Ret);
            }
            private static void CvsAccessoryCopyCopyAcsReplacement() {
                CvsAccessoryCopy __instance = AccMover._cvsAccessoryCopy;
                ChaFileAccessory accessory = __instance.chaCtrl.chaFile.coordinate[__instance.ddCoordeType[0].value].accessory;
                ChaFileAccessory accessory2 = __instance.chaCtrl.chaFile.coordinate[__instance.ddCoordeType[1].value].accessory;
                for (int i = 0; i < __instance.tglKind.Length; i++) {
                    if (__instance.tglKind[i].isOn) {
                        byte[] bytes = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(accessory2.parts[i]);
                        accessory.parts[i] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(bytes);
                    }
                }
                if (!disableTransferFuncs) {
                    __instance.chaCtrl.ChangeCoordinateType(true);
                    __instance.chaCtrl.Reload(false, true, true, true);
                    __instance.CalculateUI();
                }
                Singleton<CustomBase>.Instance.updateCustomUI = true;
            }

            // Disable vanilla CopyClothes() of Copy in favor of adjusted one
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CvsClothesCopy), "CopyClothes")]
            private static IEnumerable<CodeInstruction> CvsClothesCopyCopyAcsTranspiler(IEnumerable<CodeInstruction> instructions) {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(CvsClothesCopyCopyClothesReplacement)));
                yield return new CodeInstruction(OpCodes.Ret);
            }
            private static void CvsClothesCopyCopyClothesReplacement() {
                CvsClothesCopy __instance = AccMover._cvsClothesCopy;
                int num = System.Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length;
                ChaFileClothes clothes = __instance.chaCtrl.chaFile.coordinate[__instance.ddCoordeType[0].value].clothes;
                ChaFileClothes clothes2 = __instance.chaCtrl.chaFile.coordinate[__instance.ddCoordeType[1].value].clothes;
                ListInfoBase listInfo = __instance.chaCtrl.lstCtrl.GetListInfo(__instance.cateNo[0], clothes.parts[0].id);
                if (listInfo == null) {
                    listInfo = __instance.chaCtrl.lstCtrl.GetListInfo(__instance.cateNo[0], __instance.defClothesID[0]);
                }
                ListInfoBase listInfo2 = __instance.chaCtrl.lstCtrl.GetListInfo(__instance.cateNo[0], clothes2.parts[0].id);
                if (listInfo2 == null) {
                    listInfo2 = __instance.chaCtrl.lstCtrl.GetListInfo(__instance.cateNo[0], __instance.defClothesID[0]);
                }
                for (int i = 0; i < num; i++) {
                    if (__instance.tglKind[i].isOn) {
                        byte[] bytes = MessagePackSerializer.Serialize<ChaFileClothes.PartsInfo>(clothes2.parts[i]);
                        clothes.parts[i] = MessagePackSerializer.Deserialize<ChaFileClothes.PartsInfo>(bytes);
                        if (i == 0) {
                            if ((listInfo2.Kind == 1 && listInfo.Kind == 1) || (listInfo2.Kind == 2 && listInfo.Kind == 2)) {
                                for (int j = 0; j < clothes.subPartsId.Length; j++) {
                                    clothes.subPartsId[j] = clothes2.subPartsId[j];
                                }
                            } else if (listInfo2.Kind == 1 || listInfo2.Kind == 2) {
                                for (int k = 0; k < clothes.subPartsId.Length; k++) {
                                    clothes.subPartsId[k] = clothes2.subPartsId[k];
                                }
                            } else {
                                for (int l = 0; l < clothes.subPartsId.Length; l++) {
                                    clothes.subPartsId[l] = 0;
                                }
                            }
                        } else if (i == 2) {
                            clothes.hideBraOpt[0] = clothes2.hideBraOpt[0];
                            clothes.hideBraOpt[1] = clothes2.hideBraOpt[1];
                        } else if (i == 3) {
                            clothes.hideShortsOpt[0] = clothes2.hideShortsOpt[0];
                            clothes.hideShortsOpt[1] = clothes2.hideShortsOpt[1];
                        }
                    }
                }
                if (!disableTransferFuncs) {
                    __instance.chaCtrl.ChangeCoordinateType(true);
                    __instance.chaCtrl.Reload(false, true, true, true);
                    __instance.CalculateUI();
                }
                Singleton<CustomBase>.Instance.updateCustomUI = true;
            }

            // Fix MoreOutfits setting the value to 0 when it shouldn't
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(KK_Plugins.MoreOutfits.Hooks), "CvsAccessoryCopy_ChangeDstDD")]
            [HarmonyPatch(typeof(KK_Plugins.MoreOutfits.Hooks), "CvsClothesCopy_ChangeDstDD")]
            private static IEnumerable<CodeInstruction> KKMoreOutfitsHooksCvsAccessoryClothesCopyChangeDstDDTranspiler(IEnumerable<CodeInstruction> instructions) {
                foreach (CodeInstruction instruction in instructions) {
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Ldlen) {
                        yield return new CodeInstruction(OpCodes.Conv_I4);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Add);
                    }
                }
            }

            // Disable updating the UI when switching to the All option
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessoryCopy), "ChangeDstDD")]
            private static bool CvsAccessoryCopyBeforeChangeDstDD(CvsAccessoryCopy __instance) {
                var dd = __instance.ddCoordeType[0];
                return !(dd.value == dd.options.Count - 1) && !disableTransferFuncs;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsClothesCopy), "ChangeDstDD")]
            private static bool CvsClothesCopyBeforeChangeDstDD(CvsClothesCopy __instance) {
                var dd = __instance.ddCoordeType[0];
                return !(dd.value == dd.options.Count - 1) && !disableTransferFuncs;
            }

            // Fix exception when copy is on "all" and changing clothes type
            private static bool inAllSensitiveFunction = false;
            private static bool inAllSensitiveAccFunc = false;
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsClothesCopy), "ChangeHideSubParts")]
            [HarmonyPatch(typeof(CvsClothesCopy), "ChangeDstDD")]
            [HarmonyPatch(typeof(CvsAccessoryCopy), "ChangeDstDD")]
            private static void BeforeAllSensitiveFunction(object __instance) {
                inAllSensitiveFunction = true;
                inAllSensitiveAccFunc = __instance.GetType() == typeof(CvsAccessoryCopy);
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsClothesCopy), "ChangeHideSubParts")]
            [HarmonyPatch(typeof(CvsClothesCopy), "ChangeDstDD")]
            [HarmonyPatch(typeof(CvsAccessoryCopy), "ChangeDstDD")]
            private static void AfterAllSensitiveFunction() {
                inAllSensitiveFunction = false;
                inAllSensitiveAccFunc = false;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TMP_Dropdown), "value", MethodType.Getter)]
            private static void AfterGetTMPDropdownValue(TMP_Dropdown __instance, ref int __result) {
                if (!inAllSensitiveFunction) return;
                if (__result >= __instance.options.Count() - 1) {
                    if (inAllSensitiveAccFunc) {
                        __result = AccMover._cvsAccessoryCopy?.ddCoordeType?[1]?.m_Value ?? 0;
                    } else {
                        __result = AccMover._cvsClothesCopy?.ddCoordeType?[1]?.m_Value ?? 0;
                    }
                }
            }

            // Fix CvsAccessory LateUpdate lag
            private static Vector3[] cvsAccGuidPos = new[] {Vector3.zero, Vector3.zero};
            private static Vector3[] cvsAccGuidRot = new[] {Vector3.zero, Vector3.zero};
            private static bool[] cvsAccessoryGuidWasDrag = new[] { false, false};
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessory), "LateUpdate")]
            private static bool ReplaceCvsAccessoryLateUpdate(CvsAccessory __instance) {
                if (__instance.cgSlot.alpha != 1f) {
                    return false;
                }
                for (int i = 0; i < 2; i++) {
                    if (!(null == __instance.cmpGuid[i])) {
                        if (__instance.cmpGuid[i].isDrag || __instance.isDrag[i]) {
                            if (
                                __instance.cmpGuid[i].amount.position != cvsAccGuidPos[i] ||
                                __instance.cmpGuid[i].amount.rotation != cvsAccGuidRot[i]
                            ) {
                                __instance.SetAccessoryTransform(i, false);
                                cvsAccGuidPos[i] = __instance.cmpGuid[i].amount.position;
                                cvsAccGuidRot[i] = __instance.cmpGuid[i].amount.rotation;
                                cvsAccessoryGuidWasDrag[i] = true;
                            }
                        } else {
                            __instance.SetControllerTransform(i);
                            if (cvsAccessoryGuidWasDrag[i]) {
                                cvsAccessoryGuidWasDrag[i] = false;
                                __instance.UpdateCustomUI();
                            }
                        }
                        __instance.isDrag[i] = __instance.cmpGuid[i].isDrag;
                    }
                }
                return false;
            }

            // Fix axis movement not appearing
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "Start")]
            private static void AfterCvsAccessoryStart(CvsAccessory __instance) {
                foreach (var tgl in new[] {
                    __instance.tglDrawController01,
                    __instance.tglDrawController02,
                    __instance.tglControllerType01[0],
                    __instance.tglControllerType01[1],
                    __instance.tglControllerType02[0],
                    __instance.tglControllerType02[1]
                }) {
                    if (tgl.isOn) {
                        tgl.isOn = false;
                        __instance.StartCoroutine(ToggleLater());
                        IEnumerator ToggleLater() {
                            yield return null;
                            tgl.isOn = true;
                        }
                    }
                }
            }

            // ===================== Mass Accessory Edit Zone =====================
            private static bool propagating = false;
            private static CustomAcsChangeSlot m_CustomAcsChangeSlot = null;
            private static CustomAcsChangeSlot CustomAcsChangeSlot {
                get {
                    if (m_CustomAcsChangeSlot == null) m_CustomAcsChangeSlot = Singleton<CustomAcsChangeSlot>.Instance;
                    return m_CustomAcsChangeSlot;
                }
            }
            private static CustomBase m_CustomBase = null;
            internal static CustomBase CustomBase {
                get {
                    if (m_CustomBase == null) m_CustomBase = Singleton<CustomBase>.Instance;
                    return m_CustomBase;
                }
            }
            private static bool IsIndependentNMove2(CvsAccessory cvsAcc) {
                Transform nMove2 = cvsAcc.chaCtrl.objAcsMove[cvsAcc.nSlotNo, 1]?.transform;
                if (nMove2 == null) return false;
                Transform nMove = cvsAcc.chaCtrl.objAcsMove[cvsAcc.nSlotNo, 0]?.transform;
                if (nMove == null) return true;
                return !nMove2.IsChildOf(nMove);
            }

            // Propagate accessory transform edits
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "FuncUpdateAcsPosAdd")]
            private static void CvsAccessoryAfterFuncUpdateAcsPosAdd(int xyz, bool add, float val) {
                PropagateAccessoryTransformEdit(0, xyz, add, val);
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "FuncUpdateAcsRotAdd")]
            private static void CvsAccessoryAfterFuncUpdateAcsRotAdd(int xyz, bool add, float val) {
                PropagateAccessoryTransformEdit(1, xyz, add, val);
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "FuncUpdateAcsSclAdd")]
            private static void CvsAccessoryAfterFuncUpdateAcsSclAdd(int xyz, bool add, float val) {
                PropagateAccessoryTransformEdit(2, xyz, add, val);
            }
            private static void PropagateAccessoryTransformEdit(int type, int xyz, bool add, float val) {
                if (propagating) return;
                propagating = true;

                int currentSlot = CustomBase.selectSlot + 1;

                foreach (int Slot in AccMover.selectedTransform.Where(x => x != currentSlot)) {
                    var cvsAccCurr = CustomAcsChangeSlot.cvsAccessory[Slot - 1];
                    if (cvsAccCurr == null) continue;
                    if (IsIndependentNMove2(cvsAccCurr)) {
                        switch (type) {
                            case 0:
                                cvsAccCurr.FuncUpdateAcsPosAdd(0, xyz, add, val);
                                cvsAccCurr.FuncUpdateAcsPosAdd(1, xyz, add, val);
                                break;
                            case 1:
                                cvsAccCurr.FuncUpdateAcsRotAdd(0, xyz, add, val);
                                cvsAccCurr.FuncUpdateAcsRotAdd(1, xyz, add, val);
                                break;
                            case 2:
                                cvsAccCurr.FuncUpdateAcsSclAdd(0, xyz, add, val);
                                cvsAccCurr.FuncUpdateAcsSclAdd(1, xyz, add, val);
                                break;
                        }
                    } else {
                        switch (type) {
                            case 0:
                                cvsAccCurr.FuncUpdateAcsPosAdd(0, xyz, add, val);
                                break;
                            case 1:
                                cvsAccCurr.FuncUpdateAcsRotAdd(0, xyz, add, val);
                                break;
                            case 2:
                                cvsAccCurr.FuncUpdateAcsSclAdd(0, xyz, add, val);
                                break;
                        }
                    }
                }

                propagating = false;
            }

            // Propagate accessory kind / type / parent edits
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryKind")]
            private static bool PropagateAccessoryKindEdit(CvsAccessory __instance, string name, Sprite sp, int index) {
                if (__instance.isDrag[0] || __instance.isDrag[1] || __instance.cmpGuid[0]?.isDrag == true || __instance.cmpGuid[1]?.isDrag == true) return false;
                if (propagating) return true;

                int currentSlot = CustomBase.selectSlot + 1;
                int original = 0;
                if (__instance.nSlotNo < (__instance.accessory?.parts?.Length ?? 0)) original = __instance.accessory.parts[__instance.nSlotNo].id;

                if (AccMover.IsDebug.Value) {
                    AccMover.Instance.Log($"Slot{__instance.nSlotNo + 1} Kind: {original} -> {index}");
                }

                if (original == index) {
                    if (AccMover.IsDebug.Value) AccMover.Instance.Log("Skipping propagation!");
                    return true;
                }

                propagating = true;

                foreach (int Slot in AccMover.selectedTransform.Where(x => x != currentSlot)) {
                    int selfType = __instance.accessory.parts[__instance.nSlotNo].type - 120;
                    var propagateSlot = CustomAcsChangeSlot.cvsAccessory[Slot - 1];
                    if (propagateSlot == null) continue;
                    propagateSlot.UpdateSelectAccessoryType(selfType);
                    propagateSlot.UpdateSelectAccessoryKind(name, sp, index);
                }

                propagating = false;

                return true;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryParent")]
            // Also sets up transform preservation
            private static void PropagateAccessoryParentEdit(CvsAccessory __instance, int index, ref List<Vector3?> __state) {
                // Transform preservation
                if (AccMover.preserveTransforms) {
                    AccMover.Instance.Log("Preserving transforms!");
                    Transform acc = __instance.chaCtrl.objAccessory[__instance.nSlotNo]?.transform;
                    if (acc == null) return;
                    __state = new List<Vector3?>();
                    Transform nMove = acc.FindChildDeep("N_move")?.transform;
                    if (nMove != null) {
                        __state.Add(nMove.position);
                        __state.Add(nMove.eulerAngles);
                        __state.Add(nMove.lossyScale);
                        if (AccMover.IsDebug.Value)
                            AccMover.Instance.Log($"N_move transforms saved: P: {nMove.position}, R: {nMove.eulerAngles}, S: {nMove.lossyScale}");
                    } else {
                        __state.Add(null);
                    }
                    if (IsIndependentNMove2(__instance)) {
                        Transform nMove2 = __instance.chaCtrl.objAcsMove[__instance.nSlotNo, 1].transform;
                        __state.Add(nMove2.position);
                        __state.Add(nMove2.eulerAngles);
                        __state.Add(nMove2.lossyScale);
                        if (AccMover.IsDebug.Value)
                            AccMover.Instance.Log($"N_move2 transforms saved: P: {nMove2.position}, R: {nMove2.eulerAngles}, S: {nMove2.lossyScale}");
                    }
                }

                // Propagation
                if (propagating) return;

                int currentSlot = CustomBase.selectSlot + 1;
                string original = "None";
                if (__instance.nSlotNo < (__instance.accessory?.parts?.Length ?? 0)) original = __instance.accessory.parts[__instance.nSlotNo].parentKey;
                string[] array = (
                    from key in System.Enum.GetNames(typeof(ChaAccessoryDefine.AccessoryParentKey))
                    where key != "none"
                    select key
                ).ToArray();

                if (AccMover.IsDebug.Value) {
                    AccMover.Instance.Log($"Slot{__instance.nSlotNo + 1} Parent: {original} -> {array[index]}");
                }

                if (original == array[index]) {
                    if (AccMover.IsDebug.Value) AccMover.Instance.Log("Skipping propagation!");
                    return;
                }
                    
                propagating = true;

                foreach (int Slot in AccMover.selectedTransform.Where(x => x != currentSlot)) {
                    CustomAcsChangeSlot.cvsAccessory[Slot - 1]?.UpdateSelectAccessoryParent(index);
                }

                propagating = false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryType")]
            private static void PropagateAccessoryTypeEdit(CvsAccessory __instance, int index) {
                if (propagating) return;

                int currentSlot = CustomBase.selectSlot + 1;
                int original = 0;
                if (__instance.nSlotNo < (__instance.accessory?.parts?.Length ?? 0)) original = __instance.accessory.parts[__instance.nSlotNo].type - 120;

                if (AccMover.IsDebug.Value) {
                    AccMover.Instance.Log($"Slot{__instance.nSlotNo + 1} Type: {original} -> {index}");
                }

                if (original == index) {
                    if (AccMover.IsDebug.Value) AccMover.Instance.Log("Skipping propagation!");
                    return;
                }

                propagating = true;

                foreach (int Slot in AccMover.selectedTransform.Where(x => x != currentSlot)) {
                    CustomAcsChangeSlot.cvsAccessory[Slot - 1]?.UpdateSelectAccessoryType(index);
                }

                propagating = false;
            }

            // Propagate axis transforms
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsAccessory), "SetAccessoryTransform")]
            private static bool ReplaceCvsAccessorySetAccessoryTransform(CvsAccessory __instance, int guidNo, bool updateInfo) {
                // Common parts
                Transform guide = __instance.chaCtrl.objAcsMove[__instance.nSlotNo, guidNo]?.transform;
                if (
                    guide == null ||
                    __instance.tglControllerType01 == null ||
                    __instance.tglControllerType01.Length == 0 ||
                    __instance.tglControllerType02 == null ||
                    __instance.tglControllerType02.Length == 0
                ) {
                    return false;
                }

                // AccMover functions
                BeforeCvsAccessorySetAccessoryTransformSelf(__instance, guidNo, guide);

                // Optimised vanilla code
                if (updateInfo) {
                    int posRot = ((guidNo == 0) ? __instance.tglControllerType01[0].isOn : __instance.tglControllerType02[0].isOn) ? 0 : 1;
                    __instance.setAccessory.parts[__instance.nSlotNo].addMove[guidNo, posRot] = __instance.accessory.parts[__instance.nSlotNo].addMove[guidNo, posRot];
                    __instance.chaCtrl.UpdateAccessoryMoveFromInfo(__instance.nSlotNo);
                }

                return false;
            }
            private static void BeforeCvsAccessorySetAccessoryTransformSelf(CvsAccessory __instance, int guidNo, Transform guide) {
                if (propagating) return;
                propagating = true;

                GameObject refGO = new GameObject();
                refGO.transform.parent = guide.parent;
                refGO.transform.position = __instance.cmpGuid[guidNo].amount.position;
                refGO.transform.eulerAngles = __instance.cmpGuid[guidNo].amount.rotation;
                if (guidNo == 0 ? __instance.tglControllerType01[0].isOn : __instance.tglControllerType02[0].isOn) {
                    Vector3 posAdjust = (refGO.transform.localPosition - guide.localPosition) * 100;
                    foreach (int Slot in AccMover.selectedTransform) {
                        var cvsAccCurr = CustomAcsChangeSlot.cvsAccessory[Slot - 1];
                        if (cvsAccCurr == null) continue;
                        cvsAccCurr.FuncUpdateAcsPosAdd(guidNo, 0, true, posAdjust.x);
                        cvsAccCurr.FuncUpdateAcsPosAdd(guidNo, 1, true, posAdjust.y);
                        cvsAccCurr.FuncUpdateAcsPosAdd(guidNo, 2, true, posAdjust.z);
                    }
                } else {
                    Vector3 rotAdjust = refGO.transform.localEulerAngles - guide.localEulerAngles;
                    foreach (int Slot in AccMover.selectedTransform) {
                        var cvsAccCurr = CustomAcsChangeSlot.cvsAccessory[Slot - 1];
                        if (cvsAccCurr == null) continue;
                        cvsAccCurr.FuncUpdateAcsRotAdd(guidNo, 0, true, rotAdjust.x);
                        cvsAccCurr.FuncUpdateAcsRotAdd(guidNo, 1, true, rotAdjust.y);
                        cvsAccCurr.FuncUpdateAcsRotAdd(guidNo, 2, true, rotAdjust.z);
                    }
                }
                Object.DestroyImmediate(refGO);

                propagating = false;
            }

            // Update Preserve toggle on opening new accessory slot
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "CalculateUI")]
            private static void AfterCvsAccessoryCalculateUI(CvsAccessory __instance) {
                __instance.transform.FindChildDeep("tglPreserveTransform").GetComponentInChildren<Toggle>().isOn = AccMover.preserveTransforms;
            }

            // Handle transform presevation
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryParent")]
            private static void AfterCvsAccessoryUpdateSelectAccessoryParent(CvsAccessory __instance, ref List<Vector3?> __state) {
                if (AccMover.preserveTransforms) {
                    AccMover.Instance.Log("Applying preserved transform!");
                    if (__state == null || !(__state.Count == 3 || __state.Count == 4 || __state.Count == 6)) return;
                    Transform acc = __instance.chaCtrl.objAccessory[__instance.nSlotNo]?.transform;
                    if (acc == null) return;
                    Transform nMove;
                    Transform nMove2;
                    switch (__state.Count) {
                        case 3:
                            nMove = __instance.chaCtrl.objAcsMove[__instance.nSlotNo, 0].transform;
                            ApplyTransforms(__state, nMove, 0, 0);
                            break;
                        case 4:
                            nMove2 = __instance.chaCtrl.objAcsMove[__instance.nSlotNo, 1].transform;
                            ApplyTransforms(__state, nMove2, 1, 1);
                            break;
                        case 6:
                            nMove = __instance.chaCtrl.objAcsMove[__instance.nSlotNo, 0].transform;
                            nMove2 = __instance.chaCtrl.objAcsMove[__instance.nSlotNo, 1].transform;
                            ApplyTransforms(__state, nMove, 0, 0);
                            ApplyTransforms(__state, nMove2, 3, 1);
                            break;
                    }
                    __instance.UpdateAccessoryMoveInfo();
                }

                void ApplyTransforms(List<Vector3?> state, Transform tf, int offset, int correctNo) {
                    Vector3 pos = state[offset].Value;
                    Vector3 rot = state[offset + 1].Value;
                    Vector3 scl = state[offset + 2].Value;

                    tf.position = pos;
                    Vector3 posAdjust = tf.localPosition;

                    tf.eulerAngles = rot;
                    Vector3 rotAdjust = tf.localEulerAngles;

                    var tfScl = tf.lossyScale;
                    if (tfScl.x < 1E-02f) tfScl.x = 1E-02f;
                    if (tfScl.y < 1E-02f) tfScl.y = 1E-02f;
                    if (tfScl.z < 1E-02f) tfScl.z = 1E-02f;
                    tf.localScale = new Vector3(scl.x / tfScl.x, scl.y / tfScl.y, scl.z / tfScl.z);
                    Vector3 sclAdjust = tf.localScale;

                    if (AccMover.IsDebug.Value)
                        AccMover.Instance.Log($"Transform adjustments calculated! P: {posAdjust * 100}, R: {rotAdjust}, S: {sclAdjust}");

                    __instance.FuncUpdateAcsMovePaste(correctNo, new[] { posAdjust * 100, rotAdjust, sclAdjust });
                }
            }
        }

        public static class Conditionals {
            private static Harmony _harmony;
            public static bool A12 { get; private set; } = false;
            public static bool ObjImp { get; private set; } = false;
            public static int DBDEPresence { get; private set; } = 0;

            internal static Dictionary<string, List<string>> savedA12MoveParentage = new Dictionary<string, List<string>>();

            internal static void Setup() {
                var plugins = AccMover.Instance.GetComponents<BaseUnityPlugin>();
                foreach (var plugin in plugins) {
                    switch (plugin.Info.Metadata.GUID) {
                        case "starstorm.aaaaaaaaaaaa": A12 = true; break;
                        case "org.njaecha.plugins.objimport": 
                            ObjImp = true;
                            if (_harmony == null) _harmony = Harmony.CreateAndPatchAll(typeof(ObjImpHooks));
                            else _harmony.PatchAll(typeof(ObjImpHooks));
                            break;
                        case "org.njaecha.plugins.dbde":
                            var v = plugin.Info.Metadata.Version;
                            if (v.Major <= 1 && v.Minor <= 5 && v.Build < 1) {
                                AccMover.Instance.Log("[AccMover] Outdated DBDE detected, please update to v1.5.1 or later! DBDE data will fail to copy over until then.", 5);
                            } else {
                                if (v.Major >= 2) {
                                    DBDEPresence = 2;
                                } else {
                                    DBDEPresence = 1;
                                }
                                if (_harmony == null) _harmony = Harmony.CreateAndPatchAll(typeof(DBDEHooks));
                                else _harmony.PatchAll(typeof(DBDEHooks));
                            }
                            break;
                    }
                }
            }

            internal static void HandleA12Before(int slot1, Dictionary<int, int> dicMovement, bool moving) {
                if (!A12) return;
                if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Preparing data for acc #{slot1}...");
                if (!dicMovement.ContainsKey(slot1)) {
                    if (AccMover.IsDebug.Value) AccMover.Instance.Log("This slot was not found in the movement dictionary!", 2);
                    return;
                }
                DoHandleA12Before();
                void DoHandleA12Before() {
                    if (AAAAAAAAAAAA.AAAAAAAAAAAA.makerBoneRoot == null) return;
                    // If we're copying / moving an accessory AND also its parent
                    if (
                        AAAAAAAAAAAA.AAAAAAAAAAAA.TryGetMakerAccBone(slot1, out var currentBone) &&
                        AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(AAAAAAAAAAAA.AAAAAAAAAAAA.coordinateDropdown.value, out var dicCoord)
                    ) {
                        if (dicCoord.TryGetValue(slot1, out var originalHash) && AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerHashBones.TryGetValue(originalHash, out Bone originalParent)) {
                            // Check for a moving parent
                            int idxParent = -1;
                            Bone boneParent = null;
                            foreach (int i in dicMovement.Keys) {
                                if (AAAAAAAAAAAA.AAAAAAAAAAAA.TryGetMakerAccBone(i, out var bone)) {
                                    if (dicMovement.ContainsKey(i)) {
                                        bool isChild = false;
                                        Bone current = originalParent;
                                        while (current != null && !current.bone.name.StartsWith("ca_slot")) {
                                            if (current.parent == bone) {
                                                isChild = true;
                                                break;
                                            }
                                            current = current.parent;
                                        }
                                        if (isChild) {
                                            if (AccMover.IsDebug.Value) AccMover.Instance.Log("Found parent!");
                                            boneParent = bone;
                                            idxParent = i;
                                        }
                                    }
                                }
                            }

                            // If the parent is moving, then save parent structure
                            if (dicMovement.ContainsKey(idxParent) && boneParent != null) {
                                var savedParents = new List<string>();
                                Transform current = originalParent.bone;
                                while (!current.name.StartsWith("ca_slot")) {
                                    savedParents.Add(current.name);
                                    current = current.parent;
                                }
                                // Add as last element the accessory to be parented, and the accessory to be parented to
                                savedParents.Add($"{dicMovement[slot1]},{dicMovement[idxParent]}");
                                savedA12MoveParentage.Add($"{slot1}-c", savedParents);
                                if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Added new data: {string.Join("/", savedParents.ToArray())}");
                            }
                        }
                        if (moving && !dicMovement.Values.Contains(slot1)) {
                            savedA12MoveParentage[$"{slot1}-clrSrc"] = null;
                        }
                        if (!savedA12MoveParentage.ContainsKey($"{slot1}-c") && !dicCoord.ContainsKey(slot1)) {
                            savedA12MoveParentage[$"{slot1}-clrDst"] = null;
                        }

                        // In case we're moving instead of copying, check for stationary, non-overwritten children
                        if (moving) {
                            var stationaryChildren = new Dictionary<Bone, int>();
                            for (int i = 0; i < AAAAAAAAAAAA.AAAAAAAAAAAA.customAcsChangeSlot.cvsAccessory.Length; i++) {
                                if (
                                    dicCoord.ContainsKey(i) && AAAAAAAAAAAA.AAAAAAAAAAAA.TryGetMakerAccBone(i, out var bone) &&
                                    !dicMovement.ContainsKey(i) && !dicMovement.Values.Contains(i)
                                ) {
                                    bool isChild = bone.parent == currentBone;
                                    Bone current = bone.parent;
                                    while (current != null && !current.bone.name.StartsWith("ca_slot")) {
                                        if (current.parent == currentBone) {
                                            isChild = true;
                                            break;
                                        }
                                        current = current.parent;
                                    }
                                    if (isChild) {
                                        if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Found child accessory in slot#{i}");
                                        stationaryChildren.Add(bone, i);
                                    }
                                }
                            }

                            // Save parent structure for all stationary, non-overwritten children
                            foreach (var entry in stationaryChildren) {
                                var savedParents = new List<string>();
                                Transform current = entry.Key.bone.parent;
                                while (!current.name.StartsWith("ca_slot")) {
                                    savedParents.Add(current.name);
                                    current = current.parent;
                                }
                                // Add as last element the accessory to be parented, and the accessory to be parented to
                                savedParents.Add($"{entry.Value},{dicMovement[slot1]}");
                                savedA12MoveParentage.Add($"{slot1}-p", savedParents);
                                if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Added new data: {string.Join("/", savedParents.ToArray())}");
                            }
                        }
                    }
                }
            }

            internal static void HandleA12After(int slot1, Dictionary<int, int> dicMovement) {
                if (!A12) return;
                DoHandleA12After();
                void DoHandleA12After() {
                    if (AAAAAAAAAAAA.AAAAAAAAAAAA.makerBoneRoot == null) return;
                    if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Applying prepared data for acc #{slot1}!");
                    // Apply parenting based on saved data
                    foreach (string key in new[] { $"{slot1}-c", $"{slot1}-p", $"{slot1}-clrSrc", $"{slot1}-clrDst" }) {
                        if (savedA12MoveParentage.TryGetValue(key, out var entry)) {
                            if (entry == null && dicMovement.TryGetValue(slot1, out int slot2)) {
                                int nowCoord = AAAAAAAAAAAA.AAAAAAAAAAAA.coordinateDropdown.value;
                                if (AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue(nowCoord, out var dicCoord)) {
                                    if (key.EndsWith("clrSrc")) dicCoord.Remove(slot1);
                                    if (key.EndsWith("clrDst")) dicCoord.Remove(slot2);
                                    if (dicCoord.Count == 0) {
                                        AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerModifiedParents.Remove(nowCoord);
                                    }
                                }
                                continue;
                            }
                            var entryCopy = new List<string>();
                            entryCopy.AddRange(entry);
                            var parentStrings = entryCopy[entryCopy.Count - 1].Split(',');
                            entryCopy.RemoveAt(entryCopy.Count - 1);
                            if (parentStrings.Length == 2 && int.TryParse(parentStrings[0], out int idxChild) && int.TryParse(parentStrings[1], out int idxParent)) {
                                var tfParent = AAAAAAAAAAAA.AAAAAAAAAAAA.chaCtrl.objAccessory?[idxParent]?.transform;
                                if (tfParent != null) {
                                    string hash;
                                    entryCopy.Reverse();
                                    tfParent = tfParent.Find(string.Join("/", entryCopy.ToArray()));
                                    if (AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerTfBones.ContainsKey(tfParent)) {
                                        AccMover.Instance.Log($"Getting hash from existing bone for slot #{idxChild}...");
                                        hash = AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerTfBones[tfParent].MakeHash(true);
                                    } else {
                                        AccMover.Instance.Log($"Making new bone for slot #{idxChild}...");
                                        var boneParent = new Bone(tfParent);
                                        hash = boneParent.Hash.Replace("/1", "");
                                        boneParent.Destroy();
                                    }
                                    int nowCoord = AAAAAAAAAAAA.AAAAAAAAAAAA.coordinateDropdown.value;
                                    if (!AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerModifiedParents.ContainsKey(nowCoord)) {
                                        AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerModifiedParents[nowCoord] = new Dictionary<int, string>();
                                    }
                                    AAAAAAAAAAAA.AAAAAAAAAAAA.dicMakerModifiedParents[nowCoord][idxChild] = hash;
                                    if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Set new parent of #{idxChild} to {hash}!");
                                }
                            }
                        }
                    }
                }
            }

            internal static void HandleObjImportBefore(int source, int destination, bool moving) {
                if (!ObjImp) return;
                DoHandleObjImportBefore();
                void DoHandleObjImportBefore() {
                    var objImportCtrl = AccMover._cvsAccessoryChange.chaCtrl.gameObject.GetComponent<ObjImport.CharacterController>();
                    int type = objImportCtrl.ChaControl.fileStatus.coordinateType;
                    if (objImportCtrl.remeshData.ContainsKey(type)) {
                        if (objImportCtrl.remeshData[type].ContainsKey(source)) {
                            if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Transfered ObjImport data from #{source} to #{destination}!");
                            objImportCtrl.remeshData[type][destination] = objImportCtrl.remeshData[type][source];
                            if (moving) objImportCtrl.remeshData[type].Remove(source);
                            var newGO = Object.Instantiate(objImportCtrl.ChaControl.GetAccessoryComponent(source).gameObject, null);
                            ObjImpHooks.savedAccReferences.Add(destination, newGO);
                        } else {
                            if (objImportCtrl.remeshData[type].ContainsKey(destination)) {
                                objImportCtrl.remeshData[type].Remove(destination);
                            }
                        }
                    }
                }
            }

            internal static void HandleObjImportAfter(int slot2) {
                if (!ObjImp) return;
                DoHandleObjImportAfter();
                void DoHandleObjImportAfter() {
                    var objImportCtrl = AccMover._cvsAccessoryChange.chaCtrl.gameObject.GetComponent<ObjImport.CharacterController>();
                    if (objImportCtrl != null) {
                        if (
                            objImportCtrl.remeshData.TryGetValue(objImportCtrl.ChaControl.fileStatus.coordinateType, out var dicCoord) &&
                            dicCoord.ContainsKey(slot2)
                        ) {
                            if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Updated ObjImport meshes for #{slot2}!");
                            AccMover.Instance.StartCoroutine(LoadRemoveDelayed());
                            IEnumerator LoadRemoveDelayed() {
                                ObjImpHooks.objImpUpdated = true;
                                yield return new WaitForSeconds(0.25f);
                                objImportCtrl.updateMeshes();
                                yield return new WaitForSeconds(0.25f);
                                Object.DestroyImmediate(ObjImpHooks.savedAccReferences[slot2]);
                                ObjImpHooks.savedAccReferences.Remove(slot2);
                            }
                        }
                    }
                }
            }

            internal static void ObjImportUpdateMeshes(ChaControl chaCtrl) {
                if (!ObjImp) return;
                DoObjImportUpdateMeshes();
                void DoObjImportUpdateMeshes() {
                    var objImportCtrl = chaCtrl.gameObject.GetComponent<ObjImport.CharacterController>();
                    if (objImportCtrl != null) {
                        objImportCtrl.StartCoroutine(LoadDelayed());
                        IEnumerator LoadDelayed() {
                            yield return new WaitForSeconds(0.25f);
                            objImportCtrl.updateMeshes();
                        }
                    }
                }
            }

            public static class ObjImpHooks {
                internal static Dictionary<int, GameObject> savedAccReferences = new Dictionary<int, GameObject>();
                internal static bool getAccFromDic = false;
                internal static bool objImpUpdated = false;

                [HarmonyPrefix]
                [HarmonyPatch(typeof(ObjImport.CharacterController), "accessoryTransferedEvent")]
                private static bool ObjImportCharacterControllerBeforeAccessoryTransferedEvent() {
                    return !Hooks.disableTransferFuncs;
                }

                [HarmonyPrefix]
                [HarmonyPatch(typeof(ObjImport.CharacterController), "updateMeshes")]
                private static void ObjImportCharacterControllerBeforeUpdateMeshes() {
                    ObjImpHooks.getAccFromDic = true;
                }
                [HarmonyPostfix]
                [HarmonyPatch(typeof(ObjImport.CharacterController), "updateMeshes")]
                private static void ObjImportCharacterControllerAfterUpdateMeshes() {
                    ObjImpHooks.getAccFromDic = false;
                }

                [HarmonyPostfix]
                [HarmonyPatch(typeof(ChaInfo), "GetAccessoryComponent")]
                private static void ChaControlAfterGetAccessoryComponent(int parts, ref ChaAccessoryComponent __result) {
                    if (!getAccFromDic || !Hooks.disableTransferFuncs) return;
                    if (savedAccReferences.TryGetValue(parts, out var dicVal)) {
                        __result = dicVal.GetComponent<ChaAccessoryComponent>();
                    }
                }
            }

            public static class DBDEHooks {
                [HarmonyPrefix]
                [HarmonyPatch(typeof(DBDECharaController), "AccessoryTransferredEvent")]
                private static bool DBDECharaControllerBeforeAccessoryTransferedEvent(DBDECharaController __instance, int source, int destination) {
                    DynamicBoneDistributionEditor.DBDE.UI.UpdateUIWhileOpen = false;
                    if (Hooks.disableTransferFuncs) {
                        var acc = __instance.ChaControl.GetAccessoryComponent(source);
                        if (acc == null) return false;
                        GameObject newGOSource = Object.Instantiate(acc.gameObject, null);
                        __instance.StartCoroutine(DelayedTransferEvent(newGOSource));
                        return false;
                    }
                    return true;

                    // Modified form of original method from DBDE
                    IEnumerator DelayedTransferEvent(GameObject newGOSource) {
                        if (newGOSource == null) yield break;
                        DynamicBoneDistributionEditor.DBDE.UI.UpdateUIWhileOpen = false;
                        DynamicBone[] sourcDBs = newGOSource.GetComponentsInChildren<DynamicBone>();
                        List<DBDEDynamicBoneEdit> sourceEdits = __instance.DistributionEdits[__instance.ChaControl.fileStatus.coordinateType].FindAll(
                            dbde => dbde.ReidentificationData is KeyValuePair<int, string> kvp && kvp.Key == source
                        );
                        List<DBDEDynamicBoneEdit> sourceEditsOriginal = new List<DBDEDynamicBoneEdit>(sourceEdits);

                        System.Reflection.ConstructorInfo cons = null;
                        if (DBDEPresence == 1) {
                            cons = AccessTools.Constructor(typeof(DBDEDynamicBoneEdit), new[] {typeof(System.Func<List<DynamicBone>>), typeof(DBDEDynamicBoneEdit)});
                        }

                        // Duplicating edits so they don't get deleted
                        for (int i = 0; i < sourceEdits.Count; i++) {
                            var nowEdit = sourceEdits[i];
                            if (DBDEPresence == 2) {
                                sourceEdits[i] = GetDuplicateEdit();
                                DBDEDynamicBoneEdit GetDuplicateEdit() {
                                    return new DBDEDynamicBoneEdit(() => nowEdit.DynamicBones, nowEdit.holder, nowEdit, false) {
                                        ReidentificationData = nowEdit.ReidentificationData
                                    };
                                }
                            } else if (DBDEPresence == 1) {
                                sourceEdits[i] = (DBDEDynamicBoneEdit)cons.Invoke(new object[] {
                                    (System.Func<List<DynamicBone>>)(() => nowEdit.DynamicBones),
                                    nowEdit
                                });
                                sourceEdits[i].ReidentificationData = nowEdit.ReidentificationData;
                            } else {
                                throw new System.NotImplementedException("DBDE Presence 0 in DBDE Hook???");
                            }
                        }

                        yield return new WaitForSeconds(0.25f);
                        DynamicBone[] destDBs = __instance.ChaControl.GetAccessoryComponent(destination).GetComponentsInChildren<DynamicBone>();
                        for (int i = 0; i < destDBs.Length; i++) {
                            sourcDBs[i].TryGetAccessoryQualifiedName(out string name);
                            int newSlot = destination;
                            DBDEDynamicBoneEdit sourceEdit = sourceEdits.Find(dbdebe => dbdebe.ReidentificationData is KeyValuePair<int, string> kvp && kvp.Value == name);
                            if (sourceEdit == null) continue;

                            if (DBDEPresence == 2) {
                                __instance.DistributionEdits[__instance.ChaControl.fileStatus.coordinateType].Add(GetNewEdit());
                                DBDEDynamicBoneEdit GetNewEdit() {
                                    return new DBDEDynamicBoneEdit(
                                        () => __instance.WouldYouBeSoKindTohandMeTheDynamicBonePlease(name, newSlot), sourceEdit.holder, sourceEdit, false
                                    ) { ReidentificationData = new KeyValuePair<int, string>(newSlot, name) };
                                }
                            } else if (DBDEPresence == 1) {
                                DBDEDynamicBoneEdit newEdit = (DBDEDynamicBoneEdit)cons.Invoke(new object[] {
                                    (System.Func<List<DynamicBone>>)(() => __instance.WouldYouBeSoKindTohandMeTheDynamicBonePlease(name, newSlot)),
                                    sourceEdit
                                });
                                newEdit.ReidentificationData = new KeyValuePair<int, string>(newSlot, name);
                                __instance.DistributionEdits[__instance.ChaControl.fileStatus.coordinateType].Add(newEdit);
                            } else {
                                throw new System.NotImplementedException("DBDE Presence 0 in DBDE Hook???");
                            }
                        }

                        foreach (var edit in sourceEditsOriginal) {
                            if (edit.PrimaryDynamicBone != null) edit.ApplyAll();
                        }
                        __instance.StartCoroutine(__instance.RefreshBoneListDelayed());
                        DynamicBoneDistributionEditor.DBDE.UI.UpdateUIWhileOpen = true;
                        yield return new WaitForSeconds(0.25f);
                        Object.DestroyImmediate(newGOSource);
                    }
                }
            }
        }
    }
}
