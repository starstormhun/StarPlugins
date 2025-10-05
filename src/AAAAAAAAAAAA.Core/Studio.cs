using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Studio;

namespace AAAAAAAAAAAA {
    public partial class AAAAAAAAAAAA : BaseUnityPlugin {
        internal static void InitStudio() {
            KKAPI.Studio.SaveLoad.StudioSaveLoadApi.ObjectDeleted += (x, y) => { ClearNullBones(y.DeletedObject); };
        }

        internal static void ClearNullBones(ObjectCtrlInfo oci) {
            if (!(oci is OCIChar ociChar)) return;
            var controller = ociChar.charInfo.transform.GetComponent<CardDataController>();
            var dicCopy = controller.dicTfBones.ToList();
            controller.dicTfBones.Clear();
            foreach (var kvp in dicCopy) {
                if (kvp.Key != null) controller.dicTfBones[kvp.Key] = kvp.Value;
                else kvp.Value.Destroy();
            }
        }

        internal static bool TryGetStudioAccBone(CardDataController controller, int slot, out Bone bone) {
            bone = null;
            if (controller.ChaControl.objAccessory.Length < slot + 1) {
                Instance.Log($"Fewer accessory slots found on character than being requested! ({slot + 1})", 3);
                return false;
            }
            var acc = controller.ChaControl.objAccessory?[slot]?.transform;
            if (acc == null) return false;
            if (controller.dicTfBones.TryGetValue(acc, out bone)) return true;
            Instance.Log($"Could not get accessory bone for slot {slot + 1}!", 3);
            return false;
        }

        internal static Bone BuildStudioTree(CardDataController controller) {
            return BuildBoneTree(controller.transform, controller.dicTfBones, controller);
        }

        internal static Bone ApplyStudioData(CardDataController controller) {
            Bone result = BuildStudioTree(controller);
            if (controller.customAccParents.TryGetValue((int)controller.CurrentCoordinate.Value, out var dicChanges)) {
                foreach (var kvp in dicChanges) {
                    if (TryGetStudioAccBone(controller, kvp.Key, out var accBone) && controller.dicHashBones.TryGetValue(kvp.Value, out var parentBone)) {
                        accBone.SetParent(parentBone);
                        accBone.PerformBoneUpdate();
                    } else {
                        Instance.Log($"Invalid parentage found for [{(int)controller.CurrentCoordinate.Value}][{kvp.Key}] = \"{kvp.Value}\"!", 2);
                    }
                }
            }
            return result;
        }
    }
}
