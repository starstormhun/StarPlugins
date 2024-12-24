using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

namespace AAAAAAAAAAAA {
    public class Bone {
        public Transform bone;
        public Bone parent = null;
        public List<Bone> children = new List<Bone>();

        public string Hash { get; private set; }

        public Bone(Transform _bone) : this(_bone, null, null) { }

        public Bone(Transform _bone, Bone _parent) : this(_bone, _parent, null) { }

        public Bone(Transform _bone, Bone _parent, IEnumerable<Bone> _children) {
            bone = _bone;
            if (_parent != null) {
                parent = _parent;
                parent.children.Add(this);
            }
            if (_children != null) {
                children = new List<Bone>(_children);
            }
            Hash = MakeHash();
            AAAAAAAAAAAA.dicTfBones.Add(_bone, this);
            AAAAAAAAAAAA.dicHashBones.Add(Hash, this);
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
            if (bone != null) AAAAAAAAAAAA.dicTfBones.Remove(bone);
            if (Hash != "") AAAAAAAAAAAA.dicHashBones.Remove(Hash);
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
                var oldScale = bone.localScale;
                bone.SetParent(parent.bone);
                bone.localPosition = Vector3.zero;
                bone.localRotation = Quaternion.identity;
                bone.localScale = oldScale;
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
                if (AAAAAAAAAAAA.dicHashBones.ContainsKey(result)) {
                    int i = 1;
                    while (AAAAAAAAAAAA.dicHashBones.ContainsKey(result + $"/{i}")) i++;
                    result += $"/{i}";
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
