using HarmonyLib;
using UnityEngine;
using Studio;

namespace LightSettings.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        private static class Hooks {
            private static Harmony _harmony;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Patch extra settings panel movement according to light type
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TreeNodeCtrl), "SelectSingle")]
            private static void PostfixPlaceholder(TreeNodeObject _node, bool _deselect = true) {
                if (_deselect && Studio.Studio.Instance.dicInfo.TryGetValue(_node, out ObjectCtrlInfo _info)) {
                    if (_info is OCILight _ociLight) {
                        switch (_ociLight.lightType) {
                            case UnityEngine.LightType.Directional:
                                UIHandler.containerItem.localPosition = new Vector2(0, 0);
                                break;
                            case UnityEngine.LightType.Point:
                                UIHandler.containerItem.localPosition = new Vector2(0, -50f);
                                break;
                            case UnityEngine.LightType.Spot:
                                UIHandler.containerItem.localPosition = new Vector2(0, -90f);
                                break;
                        }
                    }
                }
            }
        }
    }
}
