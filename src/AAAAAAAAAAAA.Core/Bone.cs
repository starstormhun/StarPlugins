using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

namespace AAAAAAAAAAAA {
    public class Bone {
        public Transform bone;
        public Bone parent = null;
        public List<Bone> children = new List<Bone>();
        public CardDataController controller;

        public string Hash { get; private set; }

        public Bone(Transform _bone, Bone _parent = null, IEnumerable<Bone> _children = null, CardDataController _controller = null) {
            bone = _bone;
            if (_parent != null) {
                parent = _parent;
                parent.children.Add(this);
            }
            if (_children != null) {
                children = new List<Bone>(_children);
            }
            if (_controller != null) {
                controller = _controller;
            }
            if (!KKAPI.Maker.MakerAPI.InsideMaker && controller == null) {
                AAAAAAAAAAAA.Instance.Log("Did not provide CardDataController for Bone constructor in Studio!", 3);
                AAAAAAAAAAAA.Instance.Log("[AAAAAAAAAAAA] ERROR: No CardDataController found for Bone constructor in Studio!", 5);
            }
            Hash = MakeHash();
            if (KKAPI.Maker.MakerAPI.InsideMaker) {
                AAAAAAAAAAAA.dicMakerTfBones.Add(_bone, this);
                AAAAAAAAAAAA.dicMakerHashBones.Add(Hash, this);
            } else {
                controller.dicTfBones.Add(_bone, this);
                controller.dicHashBones.Add(Hash, this);
            }
        }

        internal void SetParent(Bone newParent) {
            if (newParent != parent) {
                if (parent != null) {
                    parent.children.Remove(this);
                }
                if (newParent != null) {
                    newParent.children.Add(this);
                }
                parent = newParent;
            }
        }

        internal void Destroy() {
            foreach (Bone child in children) {
                child.Destroy();
            }
            if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (bone != null) AAAAAAAAAAAA.dicMakerTfBones.Remove(bone);
                if (Hash != "") AAAAAAAAAAAA.dicMakerHashBones.Remove(Hash);
            } else {
                if (bone != null) controller.dicTfBones.Remove(bone);
                if (Hash != "") controller.dicHashBones.Remove(Hash);
            }
            if (children.Count > 0) children.Clear();
            parent = null;
            bone = null;
            Hash = "";
        }

        internal bool IsChildOf(Bone bone) {
            Bone check = this;
            while (check != null) {
                if (check == bone) return true;
                check = check.parent;
            }
            return false;
        }

        internal void PerformBoneUpdate() {
            if (parent != null && bone.parent != parent.bone) {
                var parentDB0 = GetAncestralComponent<DynamicBone>(parent.bone);
                parentDB0?.m_Exclusions.Add(bone);
                var parentDB1 = GetAncestralComponent<DynamicBone_Ver01>(parent.bone);
                var parentDB2 = GetAncestralComponent<DynamicBone_Ver02>(parent.bone);
                if (parent.bone.childCount == 0 && (parentDB1 != null || parentDB2 != null)) {
                    var dummy = new GameObject("dummy");
                    dummy.transform.SetParent(parent.bone);
                    dummy.transform.localPosition = Vector3.zero;
                    parentDB0?.m_Exclusions.Add(dummy.transform);
                }
                var oldScale = bone.localScale;
                bone.SetParent(parent.bone);
                bone.localPosition = Vector3.zero;
                bone.localRotation = Quaternion.identity;
                bone.localScale = oldScale;
            }

            T GetAncestralComponent<T>(Transform root) where T : Component {
                while (root.parent != null) {
                    T comp = root.parent.GetComponent<T>();
                    if (comp != null) {
                        return comp;
                    }
                    if (root.parent.GetComponent<ChaControl>() || root.parent.GetComponent<ChaAccessoryComponent>()) {
                        break;
                    }
                    root = root.parent;
                }
                return null;
            }
        }

        private string MakeHash(bool tryKeepOld = false) {
            List<byte> bytes = Encoding.UTF8.GetBytes(bone.name).ToList();
            var previous = bone.parent;
            while (previous != null && previous.GetComponent<ChaControl>() == null) {
                bytes.AddRange(Encoding.UTF8.GetBytes(previous.name));
                previous = previous.parent;
            }

            using (var sha = new System.Security.Cryptography.SHA1CryptoServiceProvider()) {
                byte[] hash = sha.ComputeHash(bytes.ToArray());
                string result = string.Empty;
                foreach (byte x in hash) {
                    result += string.Format("{0:x2}", x);
                }
                if (tryKeepOld && result.Split('/')[0] == Hash) return Hash;
                if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    if (AAAAAAAAAAAA.dicMakerHashBones.ContainsKey(result)) {
                        int i = 1;
                        while (AAAAAAAAAAAA.dicMakerHashBones.ContainsKey(result + $"/{i}")) i++;
                        result += $"/{i}";
                    }
                } else {
                    if (controller.dicHashBones.ContainsKey(result)) {
                        int i = 1;
                        while (controller.dicHashBones.ContainsKey(result + $"/{i}")) i++;
                        result += $"/{i}";
                    }
                }
                return result;
            }
        }

        internal string ReHash() {
            Hash = MakeHash(true);
            return Hash;
        }
    }
}
