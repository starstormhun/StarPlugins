using static KKAPI.Studio.StudioObjectExtensions;
using System.Collections.Generic;
using UnityEngine;
using Studio;

namespace MassShaderEditor.Koikatu {
    public static class Extensions {
        private static readonly Dictionary<TreeNodeObject, ObjectCtrlInfo> ociDict = new Dictionary<TreeNodeObject, ObjectCtrlInfo>();
        public static string NameFormatted(this ObjectCtrlInfo go) => go == null ? "" : go.treeNodeObject.textName.Trim();
        public static string NameFormatted(this Material go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Renderer go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Shader go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static Vector3 InvertScreenY(this Vector3 _vec) => new Vector3(_vec.x, Screen.height - _vec.y, _vec.z);

        public static bool TryGetFloat(this Material mat, string name, out float property) {
            // Currently impossible to detect wrong type in KK
#if KKS
            if (mat.shader.GetPropertyType(Shader.PropertyToID(name)) == UnityEngine.Rendering.ShaderPropertyType.Float) {
                property = mat.GetFloat("_" + name);
                return true;
            } else {
                property = 0f;
                return false;
            }
#elif KK
            property = mat.GetFloat("_" + name);
            return true;
#endif
        }

        public static bool TryGetColor(this Material mat, string name, out Color property) {
            // Currently impossible to detect wrong type in KK
#if KKS
            if (mat.shader.GetPropertyType(Shader.PropertyToID(name)) == UnityEngine.Rendering.ShaderPropertyType.Float) {
                property = mat.GetColor("_" + name);
                return true;
            } else {
                property = Color.black;
                return false;
            }
#elif KK
            property = mat.GetColor("_" + name);
            return true;
#endif
        }

        public static void AddChildrenRecursive(this ObjectCtrlInfo _oci, List<ObjectCtrlInfo> _list) {
            if(typeof(KKAPI.Studio.StudioObjectExtensions).GetMethod("GetOCI") != null) Recurse(_oci, _list);
            else {
                ConstructDictionary();
                RecurseOld(_oci, _list);
            }

            void Recurse(ObjectCtrlInfo oci, List<ObjectCtrlInfo> list) {
                if (oci == null) return;
                if (!list.Contains(oci)) list.Add(oci);
                foreach (var child in oci.treeNodeObject.child) Recurse(child.GetOCI(), list);
            }

            void RecurseOld(ObjectCtrlInfo oci, List<ObjectCtrlInfo> list) {
                if (oci == null) return;
                if (!list.Contains(oci)) list.Add(oci);
                foreach (var child in oci.treeNodeObject.child) RecurseOld(GetOCI(child), list);
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
