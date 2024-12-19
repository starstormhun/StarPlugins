using System;
using System.Collections.Generic;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BetterScaling.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            Hooks.UnregisterHooks();
        }

        private static class Hooks {
            private static Harmony _harmony;

            // Setup functionality on launch / enable
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Hooks), null);
            }

            // Disable functionality when disabled in settings
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Makes folder scalable
            [HarmonyPostfix]
            [HarmonyPatch(typeof(AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            private static void AddObjectFolderAfterLoad(ref OCIFolder __result) {
                if (BetterScaling.FolderScaling.Value) {
                    __result.guideObject.enableScale = true;
                    __result.guideObject.isActiveFunc += new GuideObject.IsActiveFunc(__result.OnSelect);
                }
            }

            // Makes object scaling via handles logarithmic instead of linear
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GuideScale), "OnDrag")]
            private static bool GuideScaleReplaceOnDrag(GuideScale __instance, UnityEngine.EventSystems.PointerEventData _eventData) {
                if (BetterScaling.Enabled.Value && BetterScaling.LogarithmicScaling.Value) {
                    Vector3 b;
                    if (__instance.axis == GuideScale.ScaleAxis.XYZ) {
                        Vector2 delta = _eventData.delta;
                        float d = (delta.x + delta.y) * __instance.speed;
                        b = Vector3.one * d;
                    } else {
                        b = __instance.AxisMove(_eventData.delta);
                    }
                    foreach (KeyValuePair<int, ChangeAmount> keyValuePair in __instance.dicChangeAmount) {
                        Vector3 vector = keyValuePair.Value.scale;
                        vector = (vector.ToDb() + (Vector3.one + b).ToDb()).FromDb();
                        keyValuePair.Value.scale = vector;
                    }
                    return false;
                } else return true;
            }
        }
    }
}
