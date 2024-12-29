using BepInEx;
using ChaCustom;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using AAAAAAAAAAAA;
using UnityEngine.UI;
using Illusion.Extensions;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MessagePack;

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
            public static bool A12 { get; private set; } = false;

            internal static Dictionary<string, List<string>> savedMoveParentage = new Dictionary<string, List<string>>();

            internal static void Setup() {
                var plugins = AccMover.Instance.GetComponents<BaseUnityPlugin>();
                foreach (var plugin in plugins) {
                    switch (plugin.Info.Metadata.GUID) {
                        case "starstorm.aaaaaaaaaaaa": A12 = true; break;
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
                                savedMoveParentage.Add($"{slot1}-c", savedParents);
                                if (AccMover.IsDebug.Value) AccMover.Instance.Log($"Added new data: {string.Join("/", savedParents.ToArray())}");
                            }
                        }
                        if (moving && !dicMovement.Values.Contains(slot1)) {
                            savedMoveParentage[$"{slot1}-clrSrc"] = null;
                        }
                        if (!savedMoveParentage.ContainsKey($"{slot1}-c") && !dicCoord.ContainsKey(slot1)) {
                            savedMoveParentage[$"{slot1}-clrDst"] = null;
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
                                savedMoveParentage.Add($"{slot1}-p", savedParents);
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
                        if (savedMoveParentage.TryGetValue(key, out var entry)) {
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
        }
    }
}
