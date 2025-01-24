using HSPE;
using Studio;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using Illusion.Extensions;
using System.Collections.Generic;
using DynamicBoneDistributionEditor;
using UniRx.Triggers;
using System.Linq;

namespace Performancer {
    public static class HookPatch {
        internal static void Init() {
            Hooks.SetupHooks();
            ConditionalHooks.SetupHooks();
        }

        internal static void Deactivate() {
            Hooks.UnregisterHooks();
            ConditionalHooks.UnregisterHooks();
        }

        internal static class Hooks {
            private static Harmony _harmony;

            internal const int frameAllowance = 9;

            private static Dictionary<Transform, GuideObject> dicGuideObjects = new Dictionary<Transform, GuideObject>();

            private static Dictionary<GuideObject, Dictionary<string, Vector3>> dicGuideObjectVals = new Dictionary<GuideObject, Dictionary<string, Vector3>>();
            internal static Dictionary<MonoBehaviour, Dictionary<string, object>> dicDynBoneVals = new Dictionary<MonoBehaviour, Dictionary<string, object>>();
            internal static Dictionary<MonoBehaviour, ChaControl> dicDynBoneCharas = new Dictionary<MonoBehaviour, ChaControl>();
            internal static Dictionary<MonoBehaviour, MonoBehaviour> dicDynBonePoseCtrls = new Dictionary<MonoBehaviour, MonoBehaviour>();

            internal static Dictionary<GuideObject, int> dicGuideObjectsToUpdate = new Dictionary<GuideObject, int>();
            internal static Dictionary<MonoBehaviour, int> dicDynBonesToUpdate = new Dictionary<MonoBehaviour, int>();

            private static List<Transform> iterateList = new List<Transform>();

            private static bool enableAllGuideObjects = false;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
            private static void StudioAfterDuplicate() {
                // After duplicating an item, briefly enable all guideobjects to fix scaling issues
                enableAllGuideObjects = true;
                Performancer.Instance.StartCoroutine(DisableLater());
                IEnumerator DisableLater() {
                    yield return new WaitForSeconds(1);
                    enableAllGuideObjects = false;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
            private static bool GuideObjectBeforeLateUpdate(ref GuideObject __instance) {
                if (!dicGuideObjects.ContainsKey(__instance.transformTarget)) {
                    dicGuideObjects[__instance.transformTarget] = __instance;
                }

                bool result;
                bool skipChildren = false;

                // First we check if we have added the GO to the dict, so that things don't break later
                if (!dicGuideObjectVals.ContainsKey(__instance)) {
                    dicGuideObjectVals.Add(__instance, new Dictionary<string, Vector3> {
                        { "pos", __instance.m_ChangeAmount.pos },
                        { "rot", __instance.m_ChangeAmount.rot },
                        { "scale", __instance.m_ChangeAmount.scale }
                    });
                    result = true;
                // Second check is whether we want to optimise the LateUpdate or not
                } else if (!Performancer.OptimiseGuideObjectLate.Value) {
                    result = true;
                // Whether we have decided it's time to update all guide objects
                } else if (enableAllGuideObjects) {
                    result = true;
                    skipChildren = true;
                // We check if we're supposed to update this GO (because a parent object was changed)
                } else if (dicGuideObjectsToUpdate.TryGetValue(__instance, out int dicVal) && dicVal > 0) {
                    result = true;
                    // If skipChildren is left false too many times it will slow down the game considerably
                    if (dicVal != 2) skipChildren = true;
                    dicGuideObjectsToUpdate[__instance] = dicVal - 1;
                // Check if the GO was changed since last frame
                } else if (
                    dicGuideObjectVals[__instance] is var vals && (
                        vals["pos"] != __instance.m_ChangeAmount.pos ||
                        vals["rot"] != __instance.m_ChangeAmount.rot ||
                        vals["scale"] != __instance.m_ChangeAmount.scale
                    )
                ) {
                    result = true;
                // If the GuideObject is currently visible, it needs to always be updated
                } else if (__instance.layer == 28 || Studio.Studio.optionSystem.selectedState == 0) {
                    result = true;
                    skipChildren = true;
                } else {
                    result = false;
                }

                if (result && !skipChildren) {
                    // GuideObject has been updated in some way, therefore we need to update the attached object's children
                    int id = int.MaxValue;
                    foreach (var kvp in Studio.Studio.Instance.dicChangeAmount) {
                        if (kvp.Value == __instance.m_ChangeAmount) {
                            id = kvp.Key;
                            break;
                        }
                    }
                    if (id != int.MaxValue) {
                        if (Studio.Studio.Instance.dicObjectCtrl.TryGetValue(id, out var oci)) {
                            iterateList.Clear();
                            iterateList.Add(oci.GetObject().transform);
                            while (iterateList.Count > 0) {
                                var curr = iterateList.Pop();
                                if (dicGuideObjects.ContainsKey(curr)) {
                                    dicGuideObjectsToUpdate[dicGuideObjects[curr]] = 1;
                                }
                                iterateList.AddRange(curr.Children());

                                if (Performancer.OptimiseDynamicBones.Value) {
                                    foreach (var bone in curr.GetComponents<DynamicBone>()) {
                                        dicDynBonesToUpdate[bone] = frameAllowance;
                                    };
                                    foreach (var bone in curr.GetComponents<DynamicBone_Ver01>()) {
                                        dicDynBonesToUpdate[bone] = frameAllowance;
                                    };
                                    foreach (var bone in curr.GetComponents<DynamicBone_Ver02>()) {
                                        dicDynBonesToUpdate[bone] = frameAllowance;
                                    };
                                }
                            }
                        }
                    }
                }

                var dicValue = dicGuideObjectVals[__instance];
                dicValue["pos"]   = __instance.m_ChangeAmount.pos;
                dicValue["rot"]   = __instance.m_ChangeAmount.rot;
                dicValue["scale"] = __instance.m_ChangeAmount.scale;

                if (result) {
                    Performancer.numGuideObjectLateUpdates++;
                }
                return result;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GuideObject), "OnDestroy")]
            private static void GuideObjectAfterOnDestroy(ref GuideObject __instance) {
                dicGuideObjectVals.Remove(__instance);
                dicGuideObjectsToUpdate.Remove(__instance);
                Transform key = null;
                foreach (var kvp in dicGuideObjects) {
                    if (kvp.Value == __instance) {
                        key = kvp.Key;
                        break;
                    }
                }
                if (key != null) {
                    dicGuideObjects.Remove(key);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DynamicBone), "Start")]
            [HarmonyPatch(typeof(DynamicBone_Ver01), "Start")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "Awake")]
            private static void DynamicBonesAfterCreate(ref MonoBehaviour __instance) {
                // Register bones when spawned
                dicDynBoneVals[__instance] = new Dictionary<string, object> {
                    { "pos", Vector3.zero },
                    { "tfPos", Vector3.zero },
                    { "force", Vector3.zero },
                    { "gravity", Vector3.zero },
                    { "weight", 0f },
                };

                Performancer.Instance.StartCoroutine(GetRefs(__instance));

                // Allow bones to settle when spawned
                dicDynBonesToUpdate[__instance] = frameAllowance;

                IEnumerator GetRefs(MonoBehaviour _instance) {
                    yield return null;
                    yield return null;
                    Transform go = _instance.transform;
                    ChaControl chaCtrl = null;
                    MonoBehaviour poseCtrl = null;
                    while (go != null && (chaCtrl == null || (ConditionalHooks.isKKPE && poseCtrl == null))) {
                        if (chaCtrl == null) {
                            chaCtrl = go.GetComponent<ChaControl>();
                            if (chaCtrl != null) {
                                dicDynBoneCharas.Add(_instance, chaCtrl);
                            }
                        }
                        if (ConditionalHooks.isKKPE && poseCtrl == null) {
                            poseCtrl = ConditionalHooks.GetPoseControl(go);
                            if (poseCtrl != null) {
                                dicDynBonePoseCtrls.Add(_instance, poseCtrl);
                            }
                        }
                        go = go.parent;
                    }
                    if (ConditionalHooks.isKKPE && poseCtrl == null) {
                        Performancer.Instance.Log($"No PoseController found for {_instance.name}!", 1);
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver01), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
            private static bool DynamicBonesBeforeLateUpdate(ref MonoBehaviour __instance) {
                var dicVal = dicDynBoneVals[__instance];
                bool result;
                bool skip = false;
                // If we don't optimise, then always run the Update scripts
                if (!Performancer.OptimiseGuideObjectLate.Value || !Performancer.OptimiseDynamicBones.Value) {
                    result = true;
                    // If there's leeway time left, continue
                } else if (dicDynBonesToUpdate.TryGetValue(__instance, out int framesLeft) && framesLeft > 0) {
                    result = true;
                    // If there are no particles, stop running altogether
                } else if (
                    (__instance is DynamicBone db_00 && (db_00.m_Particles.Count == 0)) ||
                    (__instance is DynamicBone_Ver01 db_01 && (db_01.m_Particles.Count == 0)) ||
                    (__instance is DynamicBone_Ver02 db_02 && (db_02.Particles.Count == 0))
                ) {
                    result = false;
                    skip = true;
                    // If the weight is zero, then continue
                } else if (
                    (__instance is DynamicBone db_10 && (db_10.m_Weight == 0)) ||
                    (__instance is DynamicBone_Ver01 db_11 && (db_11.m_Weight == 0)) ||
                    (__instance is DynamicBone_Ver02 db_12 && (db_12.Weight == 0))
                ) {
                    result = true;
                    // If the item / character is edited by KKPE / DBDE, always run
                } else if (ConditionalHooks.IsThisDBDE(__instance) || ConditionalHooks.IsThisKKPE(__instance)) {
                    dicDynBonesToUpdate[__instance] = frameAllowance;
                    result = true;
                    // If a relevant collider has changed, start running
                } else if (
                    (__instance is DynamicBone db_20 && db_20.m_Colliders.Any(x => Performancer.dicColliderVals.TryGetValue(x, out var dicColls) && dicColls["moved"] is bool moved && moved)) ||
                    (__instance is DynamicBone_Ver01 db_21 && db_21.m_Colliders.Any(x => Performancer.dicColliderVals.TryGetValue(x, out var dicColls) && dicColls["moved"] is bool moved && moved)) ||
                    (__instance is DynamicBone_Ver02 db_22 && db_22.Colliders.Any(x => Performancer.dicColliderVals.TryGetValue(x, out var dicColls) && dicColls["moved"] is bool moved && moved))
                ) {
                    dicDynBonesToUpdate[__instance] = frameAllowance;
                    result = true;
                    // If some value has changed, start running
                } else if (
                    __instance.GetDBPos() is Vector3[] positions && (
                        (__instance is DynamicBone db_30 && (
                            (dicVal["tfPos"] is Vector3 tfPos0 && !tfPos0.IsSame(positions[1])) ||
                            (dicVal["force"] is Vector3 force0 && !force0.IsSame(db_30.m_Force)) ||
                            (dicVal["gravity"] is Vector3 gravity0 && !gravity0.IsSame(db_30.m_Gravity)) ||
                            (dicVal["weight"] is float weight0 && weight0 != db_30.m_Weight)
                        )) ||
                        (__instance is DynamicBone_Ver01 db_31 && (
                            (dicVal["tfPos"] is Vector3 tfPos1 && !tfPos1.IsSame(positions[1])) ||
                            (dicVal["force"] is Vector3 force1 && !force1.IsSame(db_31.m_Force)) ||
                            (dicVal["gravity"] is Vector3 gravity1 && !gravity1.IsSame(db_31.m_Gravity)) ||
                            (dicVal["weight"] is float weight1 && weight1 != db_31.m_Weight)
                        )) ||
                        (__instance is DynamicBone_Ver02 db_32 && (
                            (dicVal["tfPos"] is Vector3 tfPos2 && !tfPos2.IsSame(positions[1])) ||
                            (dicVal["force"] is Vector3 force2 && !force2.IsSame(db_32.Force)) ||
                            (dicVal["gravity"] is Vector3 gravity2 && !gravity2.IsSame(db_32.Gravity)) ||
                            (dicVal["weight"] is float weight2 && weight2 != db_32.Weight)
                        ))
                    )
                ) {
                    dicDynBonesToUpdate[__instance] = frameAllowance;
                    result = true;
                } else {
                    result = false;
                }

                // If running, update saved values
                if (result) {
                    Vector3[] positions = __instance.GetDBPos();
                    switch (__instance) {
                        case DynamicBone db:
                            dicVal["pos"] = positions[0];
                            dicVal["tfPos"] = positions[1];
                            dicVal["force"] = db.m_Force;
                            dicVal["gravity"] = db.m_Gravity;
                            dicVal["weight"] = db.m_Weight;
                            break;
                        case DynamicBone_Ver01 db:
                            dicVal["pos"] = positions[0];
                            dicVal["tfPos"] = positions[1];
                            dicVal["force"] = db.m_Force;
                            dicVal["gravity"] = db.m_Gravity;
                            dicVal["weight"] = db.m_Weight;
                            break;
                        case DynamicBone_Ver02 db:
                            dicVal["pos"] = positions[0];
                            dicVal["tfPos"] = positions[1];
                            dicVal["force"] = db.Force;
                            dicVal["gravity"] = db.Gravity;
                            dicVal["weight"] = db.Weight;
                            break;
                    }
                }

                // Have to call these methods when disabling the calculations, otherwise the bones won't function properly
                if (!result && !skip) {
                    switch (__instance) {
                        case DynamicBone db:
                            db.InitTransforms();
                            db.ApplyParticlesToTransforms();
                            break;
                        case DynamicBone_Ver01 db:
                            db.InitTransforms();
                            db.ApplyParticlesToTransforms();
                            break;
                        case DynamicBone_Ver02 db:
                            db.InitTransforms();
                            db.ApplyParticlesToTransforms();
                            break;
                    }
                }

                return result;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver01), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
            private static void DynamicBonesAfterUpdate(ref MonoBehaviour __instance) {
                // If we don't optimise, then skip the postfix
                if (!Performancer.OptimiseGuideObjectLate.Value || !Performancer.OptimiseDynamicBones.Value) {
                    return;
                }

                // Get current position of bone
                var positions = __instance.GetDBPos(true);

                // If it's running and didn't change during the update, subtract from the frame allowance, otherwise refresh
                if (dicDynBonesToUpdate.TryGetValue(__instance, out int frames) && frames > 0) {
                    if (dicDynBoneVals[__instance]["pos"] is Vector3 pos && pos.IsSame(positions[0])) {
                        dicDynBonesToUpdate[__instance] = frames - 1;
                    } else {
                        dicDynBonesToUpdate[__instance] = frameAllowance;
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DynamicBone), "OnEnable")]
            [HarmonyPatch(typeof(DynamicBone_Ver01), "OnEnable")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "OnEnable")]
            private static void DynamicBonesAfterOnEnable(ref MonoBehaviour __instance) {
                // If we don't optimise, then skip the postfix
                if (!Performancer.OptimiseGuideObjectLate.Value || !Performancer.OptimiseDynamicBones.Value) {
                    return;
                }

                // Else allow the bone to update
                dicDynBonesToUpdate[__instance] = frameAllowance;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DynamicBoneCollider), "", MethodType.Constructor)]
            private static void DynamicBoneColliderAfterCreated(ref DynamicBoneCollider __instance) {
                Performancer.dicColliderVals.Add(__instance, new Dictionary<string, object> {
                    { "moved", false },
                    { "pos", Vector3.zero },
                    { "rot", Quaternion.identity },
                    { "scale", Vector3.zero },
                    { "center", Vector3.zero },
                    { "radius", 0f },
                    { "height", 0f },
                    { "bound", DynamicBoneCollider.Bound.Inside },
                    { "direction", DynamicBoneCollider.Direction.X },
                });
            }
        }

        internal static class ConditionalHooks {
            private static Harmony _harmony;

            internal static bool isKKPE = false;
            internal static bool isDBDE = false;

            internal static MonoBehaviour DBDEUI = null;

            // Enable conditional patches
            public static void SetupHooks() {
                // Not really a patch here but oh well, we need to get the DBDEUI reference and KKPE existence verified
                var mbs = Performancer.Instance.gameObject.GetComponents<MonoBehaviour>();
                foreach (var mb in mbs) {
                    if (mb == null) continue;
                    if (mb.GetType().ToString() == "DynamicBoneDistributionEditor.DBDE" && mb is BaseUnityPlugin dbde) {
                        var ver = dbde.Info.Metadata.Version;
                        if (ver.Major > 1 || ver.Minor >= 5) {
                            isDBDE = true;
                        } else {
                            Performancer.Instance.Log("[Performancer] You have an outdated version of DynamicBoneDistributionEditor! Please update to v1.5.0 or later.", 5);
                        }
                    }
                    if (mb.GetType().ToString() == "DynamicBoneDistributionEditor.DBDEUI") {
                        DBDEUI = mb;
                    }
                    if (mb.GetType().ToString() == "HSPE.HSPE") {
                        isKKPE = true;
                    }
                }
            }

            // Disable conditional patches
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            public static bool IsThisDBDE(MonoBehaviour mb) {
                if (!isDBDE) return false;
                return DoIsThisDBDE();
                bool DoIsThisDBDE() {
                    return
                        (DBDEUI as DBDEUI).referencedChara != null &&
                        Hooks.dicDynBoneCharas.TryGetValue(mb, out var val) &&
                        val == (DBDEUI as DBDEUI).referencedChara.ChaControl;
                }
            }

            public static MonoBehaviour GetPoseControl(Transform tf) {
                if (!isKKPE) return null;
                return DoGetPoseControl();
                MonoBehaviour DoGetPoseControl() {
                    return tf.GetComponent<PoseController>();
                }
            }

            public static bool IsThisKKPE(MonoBehaviour mb) {
                if (!isKKPE) return false;
                return DoIsThisKKPE();
                bool DoIsThisKKPE() {
                    return Hooks.dicDynBonePoseCtrls.TryGetValue(mb, out var val) && val.enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null &&
                        Studio.Studio.Instance.dicInfo.TryGetValue(Studio.Studio.Instance.treeNodeCtrl.selectNode, out var oci) &&
                        oci.GetObject() is GameObject go && go.GetComponent<PoseController>() == val;
                }
            }

            public static bool IsKKPEOpen() {
                if (!isKKPE) return false;
                return doIsKKPEOpen();
                bool doIsKKPEOpen() {
                    return PoseController._drawAdvancedMode;
                }
            }
        }
    }
}
