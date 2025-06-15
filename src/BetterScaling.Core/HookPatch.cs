using System;
using Studio;
using BepInEx;
using HarmonyLib;
using System.Xml;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
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

            // Prevent objects being able to be scaled to 0
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
            private static void GuideObjectBeforeLateUpdate(GuideObject __instance) {
                if (BetterScaling.Enabled.Value && BetterScaling.PreventZeroScale.Value) {
                    if (__instance.changeAmount == null) return;
                    var nowScale = __instance.changeAmount.scale;
                    if (Math.Abs(nowScale.x) < 1E-06f) {
                        nowScale.x = 1E-06f;
                    }
                    if (Math.Abs(nowScale.y) < 1E-06f) {
                        nowScale.y = 1E-06f;
                    }
                    if (Math.Abs(nowScale.z) < 1E-06f) {
                        nowScale.z = 1E-06f;
                    }
                    __instance.changeAmount.scale = nowScale;
                }
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
                            RegisterTimelineBehaviour();
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

            private static void RegisterTimelineBehaviour() {
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

            internal static bool HandleSetScaling(TreeNodeObject tno, bool newVal) {
                if (dicTNOButtons.TryGetValue(tno, out var extraToggle) && dicTNOScaleHierarchy.TryGetValue(tno, out bool oldVal) && newVal != oldVal) {
                    // Adjust scaling of children to remain approximately the same size
                    if (BetterScaling.AdjustScales.Value) {
                        var oci = Studio.Studio.Instance.dicInfo[tno];
                        if (oci is OCIChar) {
                            foreach (var child1 in tno.child) {
                                foreach (var child2 in child1.child) {
                                    Vector3 parentScale;
                                    Vector3 scale;
                                    Quaternion rotation;
                                    foreach (var child in child2.child) {
                                        // Get relevant geometry data
                                        var childOCI = Studio.Studio.Instance.dicInfo[child];
                                        parentScale = childOCI.guideObject.transformTarget.parent.lossyScale;
                                        scale = childOCI.guideObject.m_ChangeAmount.scale;
                                        rotation = childOCI.guideObject.transformTarget.localRotation;

                                        // Adjust parentScale by local rotation;
                                        parentScale = new Vector3(
                                            Vector3.Dot(rotation * Vector3.right, (rotation * Vector3.right).ScaleImmut(parentScale)),
                                            Vector3.Dot(rotation * Vector3.up, (rotation * Vector3.up).ScaleImmut(parentScale)),
                                            Vector3.Dot(rotation * Vector3.forward, (rotation * Vector3.forward).ScaleImmut(parentScale))
                                        );

                                        // Apply scale adjustment
                                        scale = scale.ScaleImmut(newVal ? parentScale.Invert() : parentScale);
                                        childOCI.guideObject.m_ChangeAmount.scale = scale;
                                    }
                                }
                            }
                        } else {
                            Vector3 parentScale = oci.guideObject.transformTarget.lossyScale;
                            var childList = tno.child;
                            foreach (var child in childList) {
                                // Get relevant geometry data
                                var childOCI = Studio.Studio.Instance.dicInfo[child];
                                Vector3 scale = childOCI.guideObject.m_ChangeAmount.scale;
                                Quaternion rotation = childOCI.guideObject.transformTarget.localRotation;

                                // Adjust parentScale by local rotation
                                parentScale = new Vector3(
                                    Vector3.Dot(rotation * Vector3.right, (rotation * Vector3.right).ScaleImmut(parentScale)),
                                    Vector3.Dot(rotation * Vector3.up, (rotation * Vector3.up).ScaleImmut(parentScale)),
                                    Vector3.Dot(rotation * Vector3.forward, (rotation * Vector3.forward).ScaleImmut(parentScale))
                                );

                                // Apply scale adjustment
                                scale = scale.ScaleImmut(newVal ? parentScale.Invert() : parentScale);
                                childOCI.guideObject.m_ChangeAmount.scale = scale;
                            }
                        }
                    }

                    // Activate children to ensure scale updates as intended even after loading
                    foreach (var child in tno.child) {
                        TNOAfterStart(child);
                    }

                    // Save new hierarchy scaling setting
                    dicTNOScaleHierarchy[tno] = newVal;
                    extraToggle.GetComponent<Image>().sprite = newVal ? toggleOn : toggleOff;
                    MakePerformancerUpdate(tno);

                    return true;
                }
                return false;
            }

            // Register TNO in dictionaries and create toggle button
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TreeNodeObject), "Start")]
            internal static void TNOAfterStart(TreeNodeObject __instance) {
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(__instance, out ObjectCtrlInfo oci)) return;
                if (dicGuideToTNO.ContainsKey(oci.guideObject)) return;
                if ((oci is OCIItem || oci is OCIFolder || oci is OCIChar) && oci.guideObject.enableScale) {
                    // Register TNO in dictionaries
                    dicGuideToTNO[oci.guideObject] = __instance;
                    if (!dicTNOScaleHierarchy.ContainsKey(__instance)) {
                        bool contains = BetterScalingDataController.listScaledTNO.Contains(__instance);
                        dicTNOScaleHierarchy[__instance] = contains;
                        if (contains) {
                            BetterScalingDataController.listScaledTNO.Remove(__instance);
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
                        HandleSetScaling(__instance, newVal);

                        var selected = Studio.Studio.Instance.treeNodeCtrl.selectNodes;
                        if (selected.Contains(__instance)) {
                            foreach (var tno in selected) {
                                if (tno != __instance) {
                                    HandleSetScaling(tno, newVal);
                                }
                            }
                        }
                    });
                    toggle.transform.localPosition = new Vector3(__instance.m_ButtonVisible.gameObject.activeSelf ? 40f : 20f, 0, 0);
                    toggle.name = "BS_ScaleChildren";
                    dicTNOButtons[__instance] = toggle;

                    // Recalculate text position
                    __instance.RecalcSelectButtonPos();

                    // If hierarchy scaled, call self on all children to ensure scaling works on load
                    BetterScaling.Instance.StartCoroutine(CallLater());
                    IEnumerator CallLater() {
                        yield return null;
                        if (dicTNOScaleHierarchy[__instance]) {
                            if (BetterScaling.IsDebug.Value) {
                                BetterScaling.Instance.Log($"Scaled TNO ({__instance.textName}) initialising {__instance.child.Count} children!");
                            }
                            foreach (TreeNodeObject child in __instance.child) {
                                TNOAfterStart(child);
                            }
                        }
                    }
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
                    tno.parent != null && (
                        (dicTNOScaleHierarchy.TryGetValue(tno.parent, out bool isScale) && isScale) || (
                            tno.parent?.parent?.parent != null && Studio.Studio.Instance.dicInfo.TryGetValue(tno.parent.parent.parent, out var ociChar) &&
                            ociChar is OCIChar && dicTNOScaleHierarchy.TryGetValue(ociChar.treeNodeObject, out bool isScaleChar) && isScaleChar
                        )
                    )
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

            // Handle auto-scaling upon parenting
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TreeNodeObject), "SetParent")]
            private static bool TNOBeforeSetParent(TreeNodeObject __instance, TreeNodeObject _parent) {
                if (!BetterScaling.AdjustScales.Value || KKAPI.Studio.SaveLoad.StudioSaveLoadApi.LoadInProgress || KKAPI.Studio.SaveLoad.StudioSaveLoadApi.ImportInProgress) return true;
                if (__instance == null || !Studio.Studio.Instance.dicInfo.TryGetValue(__instance, out var oci)) return true;

                bool scaled = false;
                Vector3 scale = oci.guideObject.m_ChangeAmount.scale;
                // Current parent is scaled and a normal item
                if (__instance.parent != null && dicTNOScaleHierarchy.TryGetValue(__instance.parent, out bool oldScaled) && oldScaled) {
                    Vector3 oldScale = Studio.Studio.Instance.dicInfo[__instance.parent].guideObject.transformTarget.lossyScale;
                    scale.x *= oldScale.x; scale.y *= oldScale.y; scale.z *= oldScale.z;
                    scaled = true;
                }
                // Current parent is scaled and a character
                if (
                    __instance.parent?.parent?.parent != null && Studio.Studio.Instance.dicInfo.TryGetValue(__instance.parent.parent.parent, out var ociCharOld) &&
                    ociCharOld is OCIChar && dicTNOScaleHierarchy.TryGetValue(ociCharOld.treeNodeObject, out bool oldScaledChar) && oldScaledChar
                ) {
                    Vector3 oldScale = Studio.Studio.Instance.dicInfo[__instance].guideObject.transformTarget.parent.lossyScale;
                    scale.x *= oldScale.x; scale.y *= oldScale.y; scale.z *= oldScale.z;
                    scaled = true;
                }
                // New parent is scaled and a normal item
                if (_parent != null && dicTNOScaleHierarchy.TryGetValue(_parent, out bool newScaled) && newScaled) {
                    Vector3 newScale = Studio.Studio.Instance.dicInfo[_parent].guideObject.transformTarget.lossyScale;
                    scale.x /= newScale.x; scale.y /= newScale.y; scale.z /= newScale.z;
                    scaled = true;
                }
                // New parent is scaled and (part of) a character
                if (
                    _parent?.parent?.parent != null && Studio.Studio.Instance.dicInfo.TryGetValue(_parent.parent.parent, out var ociCharNew_oci) &&
                    ociCharNew_oci is OCIChar ociCharNew && dicTNOScaleHierarchy.TryGetValue(ociCharNew.treeNodeObject, out bool newScaledChar) && newScaledChar &&
                    ociCharNew.dicAccessoryPoint.TryGetValue(_parent, out var key) && Singleton<Info>.Instance.dicAccessoryPointInfo.TryGetValue(key, out var info)
                ) {
                    GameObject referenceInfo = ociCharNew.charReference.GetReferenceInfo((ChaReference.RefObjKey)Enum.Parse(typeof(ChaReference.RefObjKey), info.key));
                    Vector3 newScale = referenceInfo.transform.lossyScale;
                    scale.x /= newScale.x; scale.y /= newScale.y; scale.z /= newScale.z;
                    scaled = true;
                }

                if (scaled) {
                    oci.guideObject.m_ChangeAmount.scale = scale;
                }

                return true;
            }
        }
    }
}
