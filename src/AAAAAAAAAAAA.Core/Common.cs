using BepInEx;
using UnityEngine;
using KKAPI.Utilities;
using System.Diagnostics;
using Illusion.Extensions;
using System.Collections.Generic;

namespace AAAAAAAAAAAA {
    public partial class AAAAAAAAAAAA : BaseUnityPlugin {
        internal static Bone BuildBoneTree(Transform start, Dictionary<Transform, Bone> tfBones, CardDataController controller) {
            Stopwatch sw = null;
            if (IsDebug.Value) {
                Instance.Log($"Building bone tree!");
                sw = Stopwatch.StartNew();
            }
            if (start == null) return null;
            if (tfBones.TryGetValue(start, out Bone bone)) {
                BuildBoneTreeRecursive(bone);
                return bone;
            } else {
                var newBone = new Bone(start, null, null, controller);
                BuildBoneTreeRecursive(newBone);
                return newBone;
            }

            void BuildBoneTreeRecursive(Bone _bone) {
                List<Transform> transforms = _bone.bone.Children();
                Bone parent = null;
                while (transforms.Count > 0) {
                    Transform tf = transforms.Pop();
                    if (parent == null || tf.parent != parent.bone) {
                        parent = tfBones[tf.parent];
                        foreach (Bone child in parent.children) {
                            if (child.bone.IsDestroyed()) {
                                child.Destroy();
                            }
                        }
                    }
                    if (tfBones.TryGetValue(tf, out Bone existingBone)) {
                        if (existingBone.bone.IsDestroyed()) {
                            existingBone.Destroy();
                            continue;
                        } else {
                            existingBone.SetParent(parent);
                        }
                    } else if (tf.GetComponent<ChaControl>() == null) {
                        var newBone = new Bone(tf, parent, null, controller);
                        if (KKAPI.Maker.MakerAPI.InsideMaker && tf.name == "a_n_hair_pony") {
                            ponyBone = newBone;
                        }
                    }
                    transforms.AddRange(tf.Children());
                }
                if (IsDebug.Value) {
                    Instance.Log($"Built tree in {sw.ElapsedMilliseconds} ms!");
                }
            }
        }
    }
}
