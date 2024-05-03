using System.Collections.Generic;
using UnityEngine;
using Studio;
using System.Reflection;

namespace MassShaderEditor.Koikatu {
    public static class Extensions {
        public static string NameFormatted(this ObjectCtrlInfo go) => go == null ? "" : go.treeNodeObject.textName.Trim();
        public static string NameFormatted(this Material go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Renderer go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Shader go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static Vector3 InvertScreenY(this Vector3 _vec) => new Vector3(_vec.x, Screen.height - _vec.y, _vec.z);

        public static bool TryGetFloat(this Material mat, string name, out float property) {
            // Currently impossible to detect wrong type in KK
#if KKS
            var accepted = new List<UnityEngine.Rendering.ShaderPropertyType>{ UnityEngine.Rendering.ShaderPropertyType.Float, UnityEngine.Rendering.ShaderPropertyType.Range };
            if (accepted.Contains(mat.shader.GetPropertyType(mat.shader.FindPropertyIndex("_" + name)))) {
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
            var compType = mat.shader.GetPropertyType(mat.shader.FindPropertyIndex("_" + name));
            if (compType == UnityEngine.Rendering.ShaderPropertyType.Color || compType == UnityEngine.Rendering.ShaderPropertyType.Vector) {
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

        public static bool TryGetTex(this Material mat, string name, out Texture property) {
            // Currently impossible to detect wrong type in KK
#if KKS
            var compType = mat.shader.GetPropertyType(mat.shader.FindPropertyIndex("_" + name));
            if (compType == UnityEngine.Rendering.ShaderPropertyType.Texture) {
                property = mat.GetTexture("_" + name);
                return true;
            } else {
                property = new Texture();
                return false;
            }
#elif KK
            property = mat.GetTexture("_" + name);
            return true;
#endif
        }

        public static void AddChildrenRecursive(this ObjectCtrlInfo _oci, List<ObjectCtrlInfo> _list) {
            Recurse(_oci, _list);

            void Recurse(ObjectCtrlInfo oci, List<ObjectCtrlInfo> list) {
                if (oci == null) return;
                if (!list.Contains(oci)) list.Add(oci);
                foreach (var child in oci.treeNodeObject.child) Recurse(GetOCI(child), list);
            }

            ObjectCtrlInfo GetOCI(TreeNodeObject node) {
                if (!Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo oci)) return null;
                return oci;
            }
        }

        public static KK_Plugins.MaterialEditor.MaterialEditorCharaController GetController(this ChaControl chaControl) {
            if (chaControl == null || chaControl.gameObject == null)
                return null;
            return chaControl.gameObject.GetComponent<KK_Plugins.MaterialEditor.MaterialEditorCharaController>();
        }

        public static int ToInt(this string s) {
            if (!int.TryParse(s, out int i)) return 0;
            return i;
        }

        public static bool GetPrivateProperty(this System.Type type, string name, object instance, out object value) {
            MassShaderEditor MSE = Object.FindObjectOfType<MassShaderEditor>();
            try {
                BindingFlags flags = 0;
                if (instance != null) {
                    flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField;
                } else {
                    flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.GetField;
                }
                MemberInfo info = type.GetField(name, flags);
                if (info == null) {
                    info = type.GetProperty(name, flags);
                    value = (info as PropertyInfo).GetValue(instance, null);
                } else value = (info as FieldInfo).GetValue(instance);
                return true;
            } catch {
                if (MSE.IsDebug.Value) MSE.Log($"Property ({name}) not found on ({instance})!");
                value = null;
                return false;
            }
        }
    }
}
