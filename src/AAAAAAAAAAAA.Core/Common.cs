using BepInEx;
using UnityEngine;
using KKAPI.Utilities;
using System.Diagnostics;
using Illusion.Extensions;
using System.Collections.Generic;

namespace AAAAAAAAAAAA {
    public partial class AAAAAAAAAAAA : BaseUnityPlugin {
        public static Dictionary<Transform, Bone> dicTfBones = new Dictionary<Transform, Bone>();
        public static Dictionary<string, Bone> dicHashBones = new Dictionary<string, Bone>();

        internal static Bone BuildBoneTree(Transform start) {
            Stopwatch sw = null;
            if (IsDebug.Value) {
                Instance.Log($"Building bone tree!");
                sw = Stopwatch.StartNew();
            }
            if (start == null) return null;
            if (dicTfBones.TryGetValue(start, out Bone bone)) {
                BuildBoneTreeRecursive(bone);
                return bone;
            } else {
                var newBone = new Bone(start);
                BuildBoneTreeRecursive(newBone);
                return newBone;
            }

            void BuildBoneTreeRecursive(Bone _bone) {
                List<Transform> transforms = _bone.bone.Children();
                Bone parent = null;
                while (transforms.Count > 0) {
                    Transform tf = transforms.Pop();
                    if (parent == null || tf.parent != parent.bone) {
                        parent = dicTfBones[tf.parent];
                        foreach (Bone child in parent.children) {
                            if (child.bone.IsDestroyed()) {
                                child.Destroy();
                            }
                        }
                    }
                    if (dicTfBones.TryGetValue(tf, out Bone existingBone)) {
                        if (existingBone.bone.IsDestroyed()) {
                            existingBone.Destroy();
                            continue;
                        } else {
                            existingBone.SetParent(parent);
                        }
                    } else {
                        var newBone = new Bone(tf, parent);
                        if (KKAPI.Maker.MakerAPI.InsideMaker) {
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
