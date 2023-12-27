using System.Collections.Generic;
using UnityEngine;
using Studio;

namespace MassShaderEditor.Koikatu {
    public static class Extensions {
        private static readonly Dictionary<TreeNodeObject, ObjectCtrlInfo> ociDict = new Dictionary<TreeNodeObject, ObjectCtrlInfo>();
        public static string NameFormatted(this ObjectCtrlInfo go) => go == null ? "" : go.treeNodeObject.textName.Trim();
        public static string NameFormatted(this Material go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Shader go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();

        public static Vector3 InvertScreenY(this Vector3 _vec) => new Vector3(_vec.x, Screen.height - _vec.y, _vec.z);

        public static void AddChildrenRecursive(this ObjectCtrlInfo _oci, List<ObjectCtrlInfo> _list) {
            ConstructDictionary();
            Recurse(_oci, _list);

            void Recurse(ObjectCtrlInfo oci, List<ObjectCtrlInfo> list) {
                if (oci == null) return;
                if (!list.Contains(oci)) list.Add(oci);
                foreach (var child in oci.treeNodeObject.child) Recurse(GetOCI(child), list);
            }

            void ConstructDictionary() {
                ociDict.Clear();
                foreach (var kvp in Singleton<Studio.Studio>.Instance.dicObjectCtrl) {
                    ociDict.Add(kvp.Value.treeNodeObject, kvp.Value);
                }
            }

            ObjectCtrlInfo GetOCI(TreeNodeObject node) {
                if (!ociDict.TryGetValue(node, out ObjectCtrlInfo oci)) return null;
                return oci;
            }
        }

        public static KK_Plugins.MaterialEditor.MaterialEditorCharaController GetController(this ChaControl chaControl) {
            if (chaControl == null || chaControl.gameObject == null)
                return null;
            return chaControl.gameObject.GetComponent<KK_Plugins.MaterialEditor.MaterialEditorCharaController>();
        }
    }
}
