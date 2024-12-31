using BepInEx;
using ChaCustom;
using HarmonyLib;
using MessagePack;
using System.Linq;
using UnityEngine;
using AAAAAAAAAAAA;
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

            // Disable vanilla CopyAcs() in favor of self-made one
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CvsAccessoryChange), "CopyAcs")]
            private static IEnumerable<CodeInstruction> CvsAccessoryChangeCopyAcsTranspiler(IEnumerable<CodeInstruction> instructions) {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Hooks), "CvsAccessoryChangeCopyAcsReplacement"));
                yield return new CodeInstruction(OpCodes.Ret);
            }
            private static void CvsAccessoryChangeCopyAcsReplacement() {
                CvsAccessoryChange __instance = AccMover._cvsAccessoryChange;
                var bytes = MessagePackSerializer.Serialize<ChaFileAccessory.PartsInfo>(__instance.accessory.parts[__instance.selSrc]);
                __instance.accessory.parts[__instance.selDst] = MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(bytes);
                if (!disableTransferFuncs) {
                    if (__instance.tglReverse.isOn) {
                        string reverseParent = ChaAccessoryDefine.GetReverseParent(__instance.accessory.parts[__instance.selDst].parentKey);
                        if (string.Empty != reverseParent) {
                            __instance.accessory.parts[__instance.selDst].parentKey = reverseParent;
                        }
                    }
                    __instance.chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)__instance.chaCtrl.fileStatus.coordinateType);
                    __instance.chaCtrl.Reload(false, true, true, true);
                    __instance.CalculateUI();
                    __instance.cmpAcsChangeSlot.UpdateSlotNames();
                    Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(__instance.selDst, true);
                }
            }
        }

        public static class Conditionals {
            private static Harmony _harmony;
            public static bool A12 { get; private set; } = false;
            public static bool ObjImp { get; private set; } = false;
            public static bool DBDE { get; private set; } = false;

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
                            DBDE = true;
                            if (_harmony == null) _harmony = Harmony.CreateAndPatchAll(typeof(DBDEHooks));
                            else _harmony.PatchAll(typeof(DBDEHooks));
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
                [HarmonyPatch(typeof(DBDECharaController), "AccessoryTransferedEvent")]
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
                        for (int i = 0; i < sourceEdits.Count; i++) {
                            sourceEdits[i] = new DBDEDynamicBoneEdit(() => sourceEdits[i].DynamicBones, sourceEdits[i]) {
                                ReidentificationData = sourceEdits[i].ReidentificationData
                            };
                        }
                        yield return new WaitForSeconds(0.25f);
                        DynamicBone[] destDBs = __instance.ChaControl.GetAccessoryComponent(destination).GetComponentsInChildren<DynamicBone>();
                        for (int i = 0; i < destDBs.Length; i++) {
                            sourcDBs[i].TryGetAccessoryQualifiedName(out string name);
                            int newSlot = destination;
                            DBDEDynamicBoneEdit sourceEdit = sourceEdits.Find(dbde => dbde.ReidentificationData is KeyValuePair<int, string> kvp && kvp.Value == name);
                            if (sourceEdit == null) continue;
                            __instance.DistributionEdits[__instance.ChaControl.fileStatus.coordinateType].Add(new DBDEDynamicBoneEdit(() => __instance.WouldYouBeSoKindTohandMeTheDynamicBonePlease(name, newSlot), sourceEdit) { ReidentificationData = new KeyValuePair<int, string>(newSlot, name) });
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
