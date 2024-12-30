using TMPro;
using BepInEx;
using ChaCustom;
using UnityEngine;
using KKAPI.Utilities;
using System.Collections;
using System.Collections.Generic;
using Illusion.Extensions;
using System.Linq;
using System.Diagnostics;

namespace AAAAAAAAAAAA {
    public partial class AAAAAAAAAAAA : BaseUnityPlugin {
        public static CustomChangeMainMenu mainMenu;
        public static ChaControl chaCtrl;
        public static TMP_Dropdown coordinateDropdown;
        public static CustomAcsParentWindow acsParentWindow;
        public static CustomAcsChangeSlot customAcsChangeSlot;

        public static Bone makerBoneRoot = null;
        public static Bone ponyBone = null;
        public static Dictionary<int, Dictionary<int, string>> dicMakerModifiedParents = new Dictionary<int, Dictionary<int, string>>();
        public static Dictionary<Transform, Bone> dicMakerTfBones = new Dictionary<Transform, Bone>();
        public static Dictionary<string, Bone> dicMakerHashBones = new Dictionary<string, Bone>();

        private static Dictionary<Bone, int> makerAccBones = null;

        internal static bool performParenting = false;
        internal static bool clearNullTransforms = false;

        private static void MakerLateUpdate() {
            makerAccBones = null;
        }

        internal static void InitMaker() {
            makerBoneRoot?.Destroy();
            makerBoneRoot = null;
            dicMakerModifiedParents.Clear();
            dicMakerTfBones.Clear();
            dicMakerHashBones.Clear();
            mainMenu = FindObjectOfType<CustomChangeMainMenu>();
            chaCtrl = FindObjectOfType<ChaControl>();
            coordinateDropdown = FindObjectOfType<CustomControl>().ddCoordinate;
            acsParentWindow = FindObjectOfType<CustomAcsParentWindow>();
            customAcsChangeSlot = FindObjectOfType<CustomAcsChangeSlot>();
        }

        internal static void OnParentButtonPressed() {
            if (HookPatch.Maker.aaaaaaaaaaaaOptionIdx != -1 && HookPatch.Maker.parentDropdown.value != HookPatch.Maker.aaaaaaaaaaaaOptionIdx) {
                HookPatch.Maker.parentDropdown.value = HookPatch.Maker.aaaaaaaaaaaaOptionIdx;
                return;
            }
            RegisterParent();
        }

        internal static void RegisterParent() {
            if (acsParentWindow.updateWin) return;
            if (IsDebug.Value) Instance.Log("Registering new or updated parent...");

            // Check ABMX for selected bone
            Transform selected = KKABMX.GUI.KKABMX_AdvancedGUI._selectedTransform.Value;
            if (selected == null) {
                Instance.Log("[AAAAAAAAAAAA] Please select a bone in ABMX!", 5);
                return;
            }

            // Set to ponytail parent to prevent other plugins from interfering
            HookPatch.Maker.ponytailToggle.isOn = true;

            // Lazy building the maker tree
            MakeMakerTree();

            // Parent current accessory to selected bone
            int selectedAcc = mainMenu.ccAcsMenu.GetSelectIndex();
            if (TryGetMakerAccBone(selectedAcc, out var accBone) && dicMakerTfBones.TryGetValue(selected, out var parentBone)) {
                // Make sure not to parent anything to itself or its children
                if (parentBone.IsChildOf(accBone)) {
                    Instance.Log("[AAAAAAAAAAAA] Can't parent accessory to itself or its children!", 5);
                    return;
                }

                // Perform reparenting
                accBone.SetParent(parentBone);
                accBone.PerformBoneUpdate();

                // Save parentage to dictionary
                if (!dicMakerModifiedParents.ContainsKey(coordinateDropdown.value)) {
                    dicMakerModifiedParents[coordinateDropdown.value] = new Dictionary<int, string>();
                }
                dicMakerModifiedParents[coordinateDropdown.value][selectedAcc] = parentBone.Hash;

                // Update UI
                acsParentWindow.updateWin = true;
                HookPatch.Maker.parentToggle.isOn = true;
                acsParentWindow.updateWin = false;
                customAcsChangeSlot.cvsAccessory[selectedAcc].textAcsParent.text = $"A12: {parentBone.bone.name}";
            }
        }

        internal static void MakeMakerTree() {
            if (makerBoneRoot == null) {
                makerBoneRoot = BuildBoneTree(chaCtrl.transform, dicMakerTfBones, null);
            }
        }

        internal static void MakeMakerAccBones() {
            if (makerAccBones == null) {
                Stopwatch sw = null;
                if (IsDebug.Value) {
                    Instance.Log("Building maker acc bone list...");
                    sw = Stopwatch.StartNew();
                }
                makerAccBones = new Dictionary<Bone, int>();
                for (int i = 0; i < chaCtrl.objAccessory.Length; i++) {
                    if (TryGetMakerAccBone(i, out Bone acc_i)) makerAccBones.Add(acc_i, i);
                }
                if (IsDebug.Value) {
                    Instance.Log($"Acc bone list built in {sw.ElapsedMilliseconds} ms!");
                }
            }
        }

        internal static void UpdateMakerTree(bool _performParenting = false, bool _clearNullTransforms = false, bool forced = false) {
            if (_performParenting) performParenting = true;
            if (_clearNullTransforms) clearNullTransforms = true;
            if (forced) {
                DoUpdateData();
                return;
            }
            if (HookPatch.Maker.isLoading) return;
            HookPatch.Maker.isLoading = true;
            Instance.StartCoroutine(UpdateData());
            IEnumerator UpdateData() {
                for (int i = 0; i < 6; i++) yield return CoroutineUtils.WaitForEndOfFrame;
                DoUpdateData();
            }
            void DoUpdateData() {
                if (makerBoneRoot != null) {
                    if (clearNullTransforms) {
                        clearNullTransforms = false;
                        var dicCopy = dicMakerTfBones.ToList();
                        dicMakerTfBones.Clear();
                        foreach (var kvp in dicCopy) {
                            if (kvp.Key != null) dicMakerTfBones[kvp.Key] = kvp.Value;
                            else kvp.Value.Destroy();
                        }
                    }
                    BuildBoneTree(makerBoneRoot.bone, dicMakerTfBones, null);
                    if (performParenting && dicMakerModifiedParents.TryGetValue(coordinateDropdown.value, out var dicChanges)) {
                        performParenting = false;
                        var keysToRemove = new List<int>();
                        foreach (var kvp in dicChanges) {
                            if (TryGetMakerAccBone(kvp.Key, out var accBone) && dicMakerHashBones.TryGetValue(kvp.Value, out var parentBone)) {
                                accBone.SetParent(parentBone);
                                accBone.PerformBoneUpdate();
                            } else {
                                Instance.Log($"Invalid parentage found for [{coordinateDropdown.value}][{kvp.Key}] = \"{kvp.Value}\"! Removing...", 2);
                                keysToRemove.Add(kvp.Key);
                            }
                        }
                        foreach (var key in keysToRemove) {
                            dicChanges.Remove(key);
                        }
                        if (dicChanges.Count == 0) {
                            dicMakerModifiedParents.Remove(coordinateDropdown.value);
                        }
                    }
                }
                HookPatch.Maker.isLoading = false;
            }
        }

        internal static bool TryGetMakerAccBone(int slot, out Bone bone) {
            bone = null;
            if (slot >= chaCtrl.objAccessory.Length) return false;
            var acc = chaCtrl.objAccessory[slot]?.transform;
            if (acc == null) return false;
            if (dicMakerTfBones.TryGetValue(acc, out bone)) return true;
            UpdateMakerTree(false, true, true);
            if (dicMakerTfBones.TryGetValue(acc, out bone)) return true;
            Instance.Log($"Could not get accessory bone for slot {slot}!", 3);
            return false;
        }

        internal static void UpdateHash(int slot) {
            if (makerBoneRoot == null) return;
            if (TryGetMakerAccBone(slot, out var acc)) {
                // Update the accessory bone and all children, but NOT other accessories
                MakeMakerAccBones();
                if (IsDebug.Value) Instance.Log($"{acc.bone.name} has changed parent, updating hashes...");
                var bonesToCheck = new List<Bone> { acc };
                while (bonesToCheck.Count > 0) {
                    var current = bonesToCheck.Pop();
                    if (makerAccBones.TryGetValue(current, out var i) && i != slot) {
                        if (IsDebug.Value) Instance.Log($"Skipping {current.bone.name} because it's a different accessory!");
                        continue;
                    }
                    if (IsDebug.Value) Instance.Log($"Checking {current.bone.name}...");
                    string oldHash = current.Hash;
                    string newHash = current.ReHash();
                    if (oldHash != newHash) {
                        if (IsDebug.Value) Instance.Log($"Old: {oldHash}, new: {newHash}");
                        if (dicMakerModifiedParents.TryGetValue(coordinateDropdown.value, out var dicCoord)) {
                            var keys = dicCoord.Keys.ToList();
                            foreach (var key in keys) {
                                if (dicCoord[key] == oldHash) {
                                    dicCoord[key] = newHash;
                                    if (IsDebug.Value) Instance.Log($"Coordinate [{coordinateDropdown.value}] saved parentage for acc#{key} updated!");
                                }
                            }
                        }
                        if (dicMakerHashBones.ContainsKey(oldHash)) {
                            dicMakerHashBones[newHash] = dicMakerHashBones[oldHash];
                            dicMakerHashBones.Remove(oldHash);
                        }
                    }
                    bonesToCheck.AddRange(current.children);
                }
            }
        }

        internal static List<KeyValuePair<int, string>> RemoveParentedChildren(int slot) {
            var removed = new List<KeyValuePair<int, string>>();
            if (TryGetMakerAccBone(slot, out Bone accBone) && dicMakerModifiedParents.TryGetValue(coordinateDropdown.value, out var dicCoord)) {
                // Traverse bone tree down and look for accessories
                if (IsDebug.Value) Instance.Log($"{accBone.bone.name} is being (re)moved, scanning for children...");
                var rootBone = dicMakerTfBones[chaCtrl.transform.Find("BodyTop/p_cf_body_bone/cf_j_root")];
                var backupParent = ponyBone ?? rootBone;
                MakeMakerAccBones();
                var bonesToCheck = new List<Bone>{accBone};
                while (bonesToCheck.Count > 0) {
                    var current = bonesToCheck.Pop();
                    if (makerAccBones.TryGetValue(current, out var i) && i != slot) {
                        if (IsDebug.Value) Instance.Log($"#{makerAccBones[current]} was child of {accBone.bone.name}! Reparenting...");
                        removed.Add(new KeyValuePair<int, string>(makerAccBones[current], dicCoord[makerAccBones[current]]));
                        dicCoord.Remove(makerAccBones[current]);
                        current.SetParent(backupParent);
                        current.PerformBoneUpdate();
                    } else {
                        bonesToCheck.AddRange(current.children);
                    }
                }
            }
            return removed;
        }
    }
}
