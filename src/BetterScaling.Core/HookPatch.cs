using System;
using Studio;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Xml;

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

        internal static class Hierarchy {
            private static Harmony _harmony;

            internal static Sprite toggleOn;
            internal static Sprite toggleOff;

            private static int performancerPresence = 0;

            private static Dictionary<GuideObject, TreeNodeObject> dicGuideToTNO = new Dictionary<GuideObject, TreeNodeObject>();
            internal static Dictionary<TreeNodeObject, bool> dicTNOScaleHierarchy = new Dictionary<TreeNodeObject, bool>();
            private static Dictionary<GuideObject, bool> dicGuideObjectCalcScale = new Dictionary<GuideObject, bool>();

            internal static Dictionary<TreeNodeObject, GameObject> dicTNOButtons = new Dictionary<TreeNodeObject, GameObject>();

            // Setup functionality on launch / enable
            public static void SetupHooks() {
                if (BetterScaling.HierarchyScaling.Value) {
                    // Read plugins
                    var plugins = BetterScaling.Instance.gameObject.GetComponents<MonoBehaviour>();
                    foreach (var plugin in plugins) {
                        if (plugin == null) continue;
                        if (plugin.GetType().ToString() == "Performancer.Performancer") {
                            GetPerformancerVersion(plugin as BaseUnityPlugin);
                        } else if (plugin.GetType().ToString() == "Timeline.Timeline") {
                            AddTimelineCompatibility();
                        }
                    }

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

            private static void GetPerformancerVersion(BaseUnityPlugin plugin) {
                if (plugin.Info.Metadata.Version.Major == 1 && plugin.Info.Metadata.Version.Minor < 2) {
                    performancerPresence = 1;
                    BetterScaling.Instance.Log("[BetterScaling] Outdated Performancer version detected! Hierarchy scaling won't work correctly, please update to v1.2.1 or later!", 5);
                } else {
                    performancerPresence = 2;
                }
            }

            internal static void MakePerformancerUpdate(TreeNodeObject tno) {
                switch (performancerPresence) {
                    case 2:
                        MakePerformancerUpdateInternal();
                        break;
                    default:
                        return;
                }

                void MakePerformancerUpdateInternal() {
                    Performancer.Performancer.Instance.EnableGuideObject(Studio.Studio.Instance.dicInfo[tno].guideObject);
                }
            }

            private static void AddTimelineCompatibility() {
                Timeline.Timeline.AddInterpolableModelDynamic(
                    owner: "Better Scaling",
                    id: "hierarchyScaling",
                    name: "Scale Children",
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => BetterScaling.SetScaling(oci?.treeNodeObject, factor < 1 ? (bool)leftValue : (bool)rightValue),
                    interpolateAfter: null,
                    isCompatibleWithTarget: oci => BetterScaling.IsHierarchyScalable(oci?.treeNodeObject),
                    getValue: (oci, parameter) => BetterScaling.IsScaled(oci?.treeNodeObject),
                    readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                    writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                    getParameter: oci => oci.treeNodeObject,
                    readParameterFromXml: (oci, node) => oci.treeNodeObject,
                    getFinalName: (currentName, oci, parameter) => "Scale Children"
                );
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
                        bool contains = SceneDataController.listScaledTNO.Contains(__instance);
                        dicTNOScaleHierarchy[__instance] = contains;
                        if (contains) {
                            SceneDataController.listScaledTNO.Remove(__instance);
                        }
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
                        MakePerformancerUpdate(__instance);

                        var selected = Studio.Studio.Instance.treeNodeCtrl.selectNodes;
                        if (selected.Contains(__instance)) {
                            foreach (var tno in selected) {
                                if (tno != __instance && dicTNOButtons.TryGetValue(tno, out var extraToggle)) {
                                    dicTNOScaleHierarchy[tno] = newVal;
                                    extraToggle.GetComponent<Image>().sprite = newVal ? toggleOn : toggleOff;
                                    MakePerformancerUpdate(tno);
                                }
                            }
                        }
                    });
                    toggle.transform.localPosition = new Vector3(__instance.m_ButtonVisible.gameObject.activeSelf ? 40f : 20f, 0, 0);
                    toggle.name = "BS_ScaleChildren";
                    dicTNOButtons[__instance] = toggle;

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
