using TMPro;
using BepInEx;
using ChaCustom;
using UnityEngine;
using KKAPI.Utilities;
using System.Collections.Generic;
using Illusion.Extensions;
using System.Linq;

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

        internal static void InitMaker() {
            makerBoneRoot?.Destroy();
            makerBoneRoot = null;
            dicMakerModifiedParents.Clear();
            dicTfBones.Clear();
            dicHashBones.Clear();
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

            // Set to ponytail parent to prevent other plugins from interfering
            HookPatch.Maker.ponytailToggle.isOn = true;

            // Check ABMX for selected bone
            Transform selected = KKABMX.GUI.KKABMX_AdvancedGUI._selectedTransform.Value;
            if (selected == null) {
                Instance.Log("[AAAAAAAAAAAA] Please select a bone in ABMX!", 5);
                return;
            }

            // Lazy building the maker tree
            MakeMakerTree();

            // Parent current accessory to selected bone
            int selectedAcc = mainMenu.ccAcsMenu.GetSelectIndex();
            if (TryGetAccBone(selectedAcc, out var accBone) && dicTfBones.TryGetValue(selected, out var parentBone)) {
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
                customAcsChangeSlot.cvsAccessory[selectedAcc].textAcsParent.text = "AAAAAAAAAAAA";
            }
        }

        internal static void MakeMakerTree() {
            if (makerBoneRoot == null) {
                makerBoneRoot = BuildBoneTree(chaCtrl.transform);
            }
        }

        internal static void UpdateMakerTree(bool performParenting = false, bool clearNullTransforms = false) {
            if (makerBoneRoot != null) {
                if (clearNullTransforms) {
                    var dicCopy = dicTfBones.ToList();
                    dicTfBones.Clear();
                    foreach (var kvp in  dicCopy) {
                        if (kvp.Key != null) dicTfBones[kvp.Key] = kvp.Value;
                        else kvp.Value.Destroy();
                    }
                }
                BuildBoneTree(makerBoneRoot.bone);
                if (performParenting && dicMakerModifiedParents.TryGetValue(coordinateDropdown.value, out var dicChanges)) {
                    foreach (var kvp in dicChanges) {
                        if (TryGetAccBone(kvp.Key, out var accBone) && dicHashBones.TryGetValue(kvp.Value, out var parentBone)) {
                            accBone.SetParent(parentBone);
                            accBone.PerformBoneUpdate();
                        }
                    }
                }
            }
        }

        internal static bool TryGetAccBone(int slot, out Bone bone) {
            bone = null;
            var acc = chaCtrl.objAccessory[slot]?.transform;
            if (acc == null) return false;
            if (dicTfBones.TryGetValue(acc, out bone)) return true;
            return false;
        }

        internal static void UpdateHash(int slot) {
            if (makerBoneRoot == null) return;
            var accBones = new List<Bone>();
            for (int i = 0; i < customAcsChangeSlot.cvsAccessory.Length; i++) {
                if (i == slot) continue;
                if (TryGetAccBone(i, out Bone acc_i)) accBones.Add(acc_i);
            }
            if (TryGetAccBone(slot, out var acc)) {
                // Update the accessory bone and all children, but NOT other accessories
                if (IsDebug.Value) Instance.Log($"{acc.bone.name} has changed parent, updating hashes...");
                var bonesToCheck = new List<Bone> { acc };
                while (bonesToCheck.Count > 0) {
                    var current = bonesToCheck.Pop();
                    if (accBones.Contains(current)) {
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
                        if (dicHashBones.ContainsKey(oldHash)) {
                            dicHashBones[newHash] = dicHashBones[oldHash];
                            dicHashBones.Remove(oldHash);
                        }
                    }
                    bonesToCheck.AddRange(current.children);
                }
            }
        }

        internal static void RemoveParentedChildren(int slot) {
            if (TryGetAccBone(slot, out Bone accBone) && dicMakerModifiedParents.TryGetValue(coordinateDropdown.value, out var dicCoord)) {
                if (IsDebug.Value) Instance.Log($"{accBone.bone.name} is being removed, scanning for children...");
                var toDelete = new List<int>();
                var rootBone = dicTfBones[chaCtrl.transform.Find("BodyTop/p_cf_body_bone/cf_j_root")];
                var backupParent = ponyBone ?? rootBone;
                foreach (var kvp in dicCoord) {
                    if (IsDebug.Value) Instance.Log($"Looking at #{kvp.Key}...");
                    if (dicHashBones.TryGetValue(kvp.Value, out var bone) && bone.IsChildOf(accBone)) {
                        if (IsDebug.Value) Instance.Log($"#{kvp.Key} was child of {accBone.bone.name}!");
                        toDelete.Add(kvp.Key);
                        if (TryGetAccBone(kvp.Key, out Bone childAcc)) {
                            if (IsDebug.Value) Instance.Log("Reparenting...");
                            childAcc.SetParent(backupParent);
                            childAcc.PerformBoneUpdate();
                        }
                    }
                }
                foreach (var key in toDelete) {
                    dicCoord.Remove(key);
                }
            }
        }
    }
}
