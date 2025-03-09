using Studio;
using MessagePack;
using UnityEngine.UI;
using KKAPI.Utilities;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using System.Collections.Generic;

namespace BetterScaling {
    internal class SceneDataController : SceneCustomFunctionController {
        // These CANNOT be changed without breaking existing saved files
        internal const string SaveID = "BetterScalingData";

        internal static List<TreeNodeObject> listScaledTNO = new List<TreeNodeObject>();

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
            var data = GetExtendedData();

            if (data != null) {
                if ((operation == SceneOperationKind.Load || operation == SceneOperationKind.Import) && data.data.TryGetValue(SaveID + "_scaledOCI", out var saveDataBytes)) {
                    var saveData = MessagePackSerializer.Deserialize<List<int>>((byte[])saveDataBytes);

                    listScaledTNO.Clear();
                    foreach (var dicKey in saveData) {
                        if (loadedItems.TryGetValue(dicKey, out var oci)) {
                            if (HookPatch.Hierarchy.dicTNOScaleHierarchy.ContainsKey(oci.treeNodeObject)) {
                                HookPatch.Hierarchy.dicTNOScaleHierarchy[oci.treeNodeObject] = true;
                                if (HookPatch.Hierarchy.dicTNOButtons.TryGetValue(oci.treeNodeObject, out var toggle)) {
                                    toggle.GetComponent<Image>().sprite = HookPatch.Hierarchy.toggleOn;
                                }
                            } else {
                                listScaledTNO.Add(oci.treeNodeObject);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnSceneSave() {
            // Transform TNO dictionary into OCI list
            var saveList = new List<int>();
            foreach (var kvp in HookPatch.Hierarchy.dicTNOScaleHierarchy) {
                if (kvp.Value) {
                    saveList.Add(KKAPI.Studio.StudioObjectExtensions.GetSceneId(Studio.Studio.Instance.dicInfo[kvp.Key]));
                }
            }

            // Save
            if (saveList.Count > 0) {
                var data = new PluginData();
                data.data.Add(SaveID + "_scaledOCI", MessagePackSerializer.Serialize(saveList));
                SetExtendedData(data);
            }
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
