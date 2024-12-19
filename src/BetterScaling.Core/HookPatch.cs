﻿using System;
using Studio;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

            private static Sprite toggleOn;
            private static Sprite toggleOff;

            private static Dictionary<GuideObject, TreeNodeObject> dicGuideToTNO = new Dictionary<GuideObject, TreeNodeObject>();
            private static Dictionary<TreeNodeObject, bool> dicTNOScaleHierarchy = new Dictionary<TreeNodeObject, bool>();
            private static Dictionary<GuideObject, bool> dicGuideObjectCalcScale = new Dictionary<GuideObject, bool>();

            // Setup functionality on launch / enable
            public static void SetupHooks() {
                if (BetterScaling.HierarchyScaling.Value) {
                    // Load toggle image and create sprites
                    Texture2D toggleIcon = new Texture2D(1, 1);
                    toggleIcon.LoadImage(Convert.FromBase64String(IMG.selectorIcon));
                    toggleOff = Sprite.Create(toggleIcon, new Rect(new Vector2(0, 0), new Vector2(64f, 64f)), new Vector2(32, 32));
                    toggleOn = Sprite.Create(toggleIcon, new Rect(new Vector2(64f, 0), new Vector2(64f, 64f)), new Vector2(32, 32));

                    // Patch methods
                    _harmony = Harmony.CreateAndPatchAll(typeof(Hierarchy), null);
                }
            }

            // Disable functionality when disabled in settings
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Register TNO in dictionaries and create toggle button
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TreeNodeObject), "Start")]
            private static void TNOAfterStart(TreeNodeObject __instance) {
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(__instance, out ObjectCtrlInfo oci)) return;
                if ((oci is OCIItem || oci is OCIFolder || oci is OCIChar) && oci.guideObject.enableScale) {
                    // Register TNO in dictionaries
                    dicGuideToTNO[oci.guideObject] = __instance;
                    if (!dicTNOScaleHierarchy.ContainsKey(__instance)) {
                        dicTNOScaleHierarchy[__instance] = false;
                    }

                    // Create scaling toggle
                    bool isScale = dicTNOScaleHierarchy[__instance];
                    GameObject toggle = UnityEngine.Object.Instantiate(__instance.transform.GetChild(1).gameObject, __instance.transform);
                    UnityEngine.Object.DestroyImmediate(toggle.GetComponent<Button>());
                    Image img = toggle.GetComponent<Image>();
                    var btn = toggle.gameObject.AddComponent<Button>();
                    img.sprite = isScale ? toggleOn : toggleOff;
                    btn.onClick.AddListener(() => {
                        bool newVal = !dicTNOScaleHierarchy[__instance];
                        dicTNOScaleHierarchy[__instance] = newVal;
                        img.sprite = newVal ? toggleOn : toggleOff;
                    });
                    toggle.transform.localPosition = new Vector3(__instance.m_ButtonVisible.gameObject.activeSelf ? 40f : 20f, 0, 0);

                    // Recalculate text position
                    __instance.RecalcSelectButtonPos();
                }
            }

            // Indent TNO name in Workspace
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TreeNodeObject), "RecalcSelectButtonPos")]
            private static void TNOAfterRecalcSelectButtonPos(TreeNodeObject __instance) {
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(__instance, out ObjectCtrlInfo oci)) return;
                if ((oci is OCIItem || oci is OCIFolder || oci is OCIChar) && oci.guideObject.enableScale && __instance.m_ButtonVisible.gameObject.activeSelf) {
                    __instance.m_TransSelect.anchoredPosition += new Vector2(__instance.textPosX * 0.5f, 0);
                }
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
