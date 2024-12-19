using System;
using System.Collections.Generic;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BetterScaling {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
            Hierarchy.SetupHooks();
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
                if (BetterScaling.Enabled.Value && BetterScaling.FolderScaling.Value) {
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

        private static class Hierarchy {
            private static Harmony _harmony;

            private static Dictionary<GuideObject, TreeNodeObject> dicGuideToTNO = new Dictionary<GuideObject, TreeNodeObject>();
            private static Dictionary<TreeNodeObject, bool> dicTNOScaleHierarchy = new Dictionary<TreeNodeObject, bool>();
            private static Dictionary<GuideObject, bool> dicGuideObjectCalcScale = new Dictionary<GuideObject, bool>();

            // Setup functionality on launch / enable
            public static void SetupHooks() {
                if (BetterScaling.HierarchyScaling.Value) {
                    _harmony = Harmony.CreateAndPatchAll(typeof(Hierarchy), null);
                }
            }

            // Disable functionality when disabled in settings
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Register GuideObject -> TNO connection on TNO creation
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TreeNodeObject), "Start")]
            private static void TNOAfterStart(TreeNodeObject __instance) {
                dicGuideToTNO[Studio.Studio.Instance.dicInfo[__instance].guideObject] = __instance;
            }

            // Prefix LateUpdate to potentially disable scale calculation
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
            private static bool GuideObjectBeforeLateUpdate(GuideObject __instance) {
                // If the hierarchy scaling feature is disabled, do nothing
                if (!BetterScaling.HierarchyScaling.Value) return true;

                // Disable default scaling calculations if necessary
                if (
                    __instance.calcScale &&
                    __instance.enableScale &&
                    dicGuideToTNO.TryGetValue(__instance, out TreeNodeObject tno) &&
                    tno.parent != null &&
                    dicTNOScaleHierarchy.TryGetValue(tno.parent, out bool isScale) && isScale
                ) {
                    dicGuideObjectCalcScale[__instance] = true;
                    __instance.calcScale = false;
                } else {
                    dicGuideObjectCalcScale[__instance] = false;
                }

                return true;
            }

            // Postfix LateUpdate to restore stored scale calculation
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
            private static void GuideObjectAfterLateUpdate(GuideObject __instance) {
                // If the hierarchy scaling feature is disabled, do nothing
                if (!BetterScaling.HierarchyScaling.Value) return;

                // Make adjusted calculations, then restore saved value, if necessary
                if (dicGuideObjectCalcScale[__instance]) {
                    // Adjust scale
                    __instance.transformTarget.localScale = __instance.changeAmount.scale;

                    // Restore calcScale value to not break other code
                    __instance.calcScale = true;
                }
            }
        }
    }
}
