using Studio;
using UnityEngine;
using MessagePack;
using UnityEngine.UI;
using KKAPI.Utilities;
using System.Collections;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using System.Collections.Generic;

namespace BetterScaling {
    internal class BetterScalingDataController : SceneCustomFunctionController {
        // These CANNOT be changed without breaking existing saved files
        internal const string SaveID = "BetterScalingData";
        internal const string HierarchyID = "_scaledOCI";
        internal const string FolderScalingID = "_scaledFoldersFlag";

        internal static List<TreeNodeObject> listScaledTNO = new List<TreeNodeObject>();

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
            if (BetterScaling.IsDebug.Value) BetterScaling.Instance.Log("[BetterScaling] Loading data...");

            if (operation == SceneOperationKind.Load || operation == SceneOperationKind.Import) {
                // Get data specific to the plugin
                var data = GetExtendedData();

                // Handle folder scaling tag
                if (!BetterScaling.DisableFolderScalingWarning.Value) {
                    string folderWarning = "";
                    if (data != null && data.data.TryGetValue(SaveID + FolderScalingID, out var folderScalingTag)) {
                        if ((int)folderScalingTag != 0 != BetterScaling.FolderScaling.Value) {
                            folderWarning = "[BetterScaling] This scene was saved with Scale Folders " +
                                (((int)folderScalingTag != 0) ? "ON" : "OFF") +
                                ", but current setting is " +
                                (BetterScaling.FolderScaling.Value ? "ON" : "OFF") +
                                ". If something seems broken, change this setting and reload.";
                        }
                    } else {
                        folderWarning = "[BetterScaling] Scaled folders found in old scene! If something is displaced, check for folders with scales other than [1, 1, 1], or change the Scale Folders setting and reload!";
                    }
                    if (folderWarning.Length > 0) {
                        foreach (var kvpLoadedItem in loadedItems) {
                            if (
                                kvpLoadedItem.Value is OCIFolder ociFolder &&
                                ociFolder.guideObject.changeAmount.scale != Vector3.one
                            ) {
                                BetterScaling.Instance.Log(folderWarning, 5);
                                break;
                            }
                        }
                    }
                }

                if (data != null) {
                    // Handle hierarchy scaled items
                    if (data.data.TryGetValue(SaveID + HierarchyID, out var saveDataBytes)) {
                        var loadedData = MessagePackSerializer.Deserialize<List<int>>((byte[])saveDataBytes);

                        if (BetterScaling.IsDebug.Value) BetterScaling.Instance.Log($"[BetterScaling] Loading {loadedData.Count} hierarchy-scaled items...");

                        listScaledTNO.Clear();
                        foreach (var dicKey in loadedData) {
                            if (loadedItems.TryGetValue(dicKey, out var oci)) {
                                if (HookPatch.Hierarchy.dicTNOScaleHierarchy.ContainsKey(oci.treeNodeObject)) {
                                    HookPatch.Hierarchy.dicTNOScaleHierarchy[oci.treeNodeObject] = true;
                                    HookPatch.Hierarchy.TNOAfterStart(oci.treeNodeObject);
                                    if (HookPatch.Hierarchy.dicTNOButtons.TryGetValue(oci.treeNodeObject, out var toggle)) {
                                        toggle.GetComponent<Image>().sprite = HookPatch.Hierarchy.toggleOn;
                                    }
                                } else {
                                    listScaledTNO.Add(oci.treeNodeObject);
                                    BetterScaling.Instance.StartCoroutine(RunLater(oci.treeNodeObject));
                                    IEnumerator RunLater(TreeNodeObject tno) {
                                        yield return null;
                                        HookPatch.Hierarchy.TNOAfterStart(tno);
                                    }
                                }
                            }
                        }
                    }
                } else {
                    if (BetterScaling.IsDebug.Value) BetterScaling.Instance.Log("[BetterScaling] No data to load!");
                }
            } 
        }

        protected override void OnSceneSave() {
            if (BetterScaling.IsDebug.Value) BetterScaling.Instance.Log("[BetterScaling] Saving data...", 5);
            // Transform TNO dictionary into OCI ID list
            var saveList = new List<int>();
            foreach (var kvp in HookPatch.Hierarchy.dicTNOScaleHierarchy) {
                if (kvp.Key != null && kvp.Value) {
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(kvp.Key, out var oci)) {
                        int id = KKAPI.Studio.StudioObjectExtensions.GetSceneId(oci);
                        if (id != -1) {
                            saveList.Add(id);
                        } else {
                            const string msg = "[BetterScaling] Couldn't find hierarchy-scaled OCI ID to save!";
                            BetterScaling.Instance.Log(msg, 2);
                            BetterScaling.Instance.Log(msg, 5);
                        }
                    }
                }
            }

            var data = new PluginData();

            // Add folder scaling tag
            data.data.Add(SaveID + FolderScalingID, BetterScaling.FolderScaling.Value ? 1 : 0);

            // Add hierarchy scaled items
            if (saveList.Count > 0) {
                data.data.Add(SaveID + HierarchyID, MessagePackSerializer.Serialize(saveList));
                if (BetterScaling.IsDebug.Value) BetterScaling.Instance.Log($"[BetterScaling] Saved {saveList.Count} hierarchy-scaled items!", 5);
            } else {
                if (BetterScaling.IsDebug.Value) BetterScaling.Instance.Log("[BetterScaling] NO hierarchy-scaled items found!", 5);
            }

            // Save
            SetExtendedData(data);
        }

        protected override void OnObjectsCopied(ReadOnlyDictionary<int, ObjectCtrlInfo> copiedItems) {
            foreach (var kvp in copiedItems) {
                if (Studio.Studio.Instance.dicObjectCtrl.TryGetValue(kvp.Key, out var originalOCI)) {
                    if (HookPatch.Hierarchy.dicTNOScaleHierarchy.TryGetValue(originalOCI.treeNodeObject, out var scaled) && scaled) {
                        HookPatch.Hierarchy.dicTNOScaleHierarchy[kvp.Value.treeNodeObject] = true;
                    }
                }
            }

            base.OnObjectsCopied(copiedItems);
        }
    }
}
