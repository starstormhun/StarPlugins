using Studio;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Illusion.Extensions;
using System.Collections.Generic;
using DynamicBoneDistributionEditor;

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

            internal const int frameAllowance = 3;

            private static Dictionary<Transform, GuideObject> dicGuideObjects = new Dictionary<Transform, GuideObject>();

            private static Dictionary<GuideObject, Dictionary<string, Vector3>> dicGuideObjectVals = new Dictionary<GuideObject, Dictionary<string, Vector3>>();
            internal static Dictionary<MonoBehaviour, Dictionary<string, object>> dicDynBoneVals = new Dictionary<MonoBehaviour, Dictionary<string, object>>();
            private static Dictionary<MonoBehaviour, ChaControl> dicDynBoneCharas = new Dictionary<MonoBehaviour, ChaControl>();

            private static Dictionary<GuideObject, bool> dicGuideObjectsToUpdate = new Dictionary<GuideObject, bool>();
            internal static Dictionary<MonoBehaviour, int> dicDynBonesToUpdate = new Dictionary<MonoBehaviour, int>();

            private static List<Transform> iterateList = new List<Transform>();

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            internal static void ClearDynBoneDics() {
                dicDynBoneVals.Clear();
                dicDynBonesToUpdate.Clear();
            }

            private static bool IsThisDBDE(MonoBehaviour mb) {
                return
                    ConditionalHooks.DBDEUI.referencedChara != null &&
                    dicDynBoneCharas.TryGetValue(mb, out var val) &&
                    val == ConditionalHooks.DBDEUI.referencedChara.ChaControl;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
            private static bool GuideObjectBeforeLateUpdate(ref GuideObject __instance) {
                if (!dicGuideObjects.ContainsKey(__instance.transformTarget)) {
                    dicGuideObjects[__instance.transformTarget] = __instance;
                }

                bool result;
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
                // Third, we check if we're supposed to update this GO (because a parent object was changed)
                } else if (dicGuideObjectsToUpdate.TryGetValue(__instance, out bool dicVal) && dicVal) {
                    result = true;
                    dicGuideObjectsToUpdate[__instance] = false;
                // If all else fails, we check if the GO was changed since last frame
                } else {
                    var vals = dicGuideObjectVals[__instance];
                    result =
                        vals["pos"]   != __instance.m_ChangeAmount.pos ||
                        vals["rot"]   != __instance.m_ChangeAmount.rot ||
                        vals["scale"] != __instance.m_ChangeAmount.scale;

                    // GuideObject has changed, therefore we need to update the attached object's children
                    if (result) {
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
                                        dicGuideObjectsToUpdate[dicGuideObjects[curr]] = true;
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
                }

                var dicValue = dicGuideObjectVals[__instance];
                dicValue["pos"]   = __instance.m_ChangeAmount.pos;
                dicValue["rot"]   = __instance.m_ChangeAmount.rot;
                dicValue["scale"] = __instance.m_ChangeAmount.scale;
                dicValue = null;

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
                    { "damping", 0f },
                    { "dampingD", new AnimationCurve() },
                    { "elasticity", 0f },
                    { "elasticityD", new AnimationCurve() },
                    { "inertia", 0f },
                    { "inertiaD", new AnimationCurve() },
                    { "radius", 0f },
                    { "radiusD", new AnimationCurve() },
                    { "stiffness", 0f },
                    { "stiffnessD", new AnimationCurve() },
                };

                Transform go = __instance.transform;
                ChaControl ctrl = null;
                while (go != null) {
                    ctrl = go.GetComponent<ChaControl>();
                    if (ctrl != null) {
                        dicDynBoneCharas.Add(__instance, ctrl);
                        break;
                    }
                    go = go.parent;
                }

                // Allow bones to settle when spawned
                dicDynBonesToUpdate[__instance] = frameAllowance;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver01), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
            private static bool DynamicBonesBeforeLateUpdate(ref MonoBehaviour __instance) {
                var dicVal = dicDynBoneVals[__instance];
                bool result;
                // If we don't optimise, then always run the Update scripts
                if (!Performancer.OptimiseGuideObjectLate.Value || !Performancer.OptimiseDynamicBones.Value) {
                    result = true;
                // If there's leeway time left, continue running
                } else if (dicDynBonesToUpdate.TryGetValue(__instance, out int framesLeft) && framesLeft > 0) {
                    result = true;
                // If some value has changed, start running
                } else if (
                    (__instance is DynamicBone db && (
                        (db.m_Particles.Count > 0 && dicVal["tfPos"] is Vector3 tfPos0 && tfPos0 != db.m_Particles[Mathf.Max(db.m_Particles.Count - 2, 0)].m_Transform.position) ||
                        (dicVal["force"] is Vector3 force0 && force0 != db.m_Force) ||
                        (dicVal["gravity"] is Vector3 gravity0 && gravity0 != db.m_Gravity) ||
                        (dicVal["weight"] is float weight0 && weight0 != db.m_Weight) ||
                        (IsThisDBDE(__instance) && (
                            (dicVal["damping"] is float damping0 && damping0 != db.m_Damping) ||
                            (dicVal["dampingD"] is AnimationCurve dampingD0 && !dampingD0.IsSame(db.m_DampingDistrib)) ||
                            (dicVal["elasticity"] is float elasticity0 && elasticity0 != db.m_Elasticity) ||
                            (dicVal["elasticityD"] is AnimationCurve elasticityD0 && !elasticityD0.IsSame(db.m_ElasticityDistrib)) ||
                            (dicVal["inertia"] is float inertia0 && inertia0 != db.m_Inert) ||
                            (dicVal["inertiaD"] is AnimationCurve inertiaD0 && !inertiaD0.IsSame(db.m_InertDistrib)) ||
                            (dicVal["radius"] is float radius0 && radius0 != db.m_Radius) ||
                            (dicVal["radiusD"] is AnimationCurve radiusD0 && !radiusD0.IsSame(db.m_RadiusDistrib)) ||
                            (dicVal["stiffness"] is float stiffness0 && stiffness0 != db.m_Stiffness) ||
                            (dicVal["stiffnessD"] is AnimationCurve stiffnessD0 && !stiffnessD0.IsSame(db.m_StiffnessDistrib))
                        ))
                    )) ||
                    (__instance is DynamicBone_Ver01 db1 && (
                        (db1.m_Particles.Count > 0 && dicVal["tfPos"] is Vector3 tfPos1 && tfPos1 != db1.m_Particles[Mathf.Max(db1.m_Particles.Count - 2, 0)].m_Transform.position) ||
                        (dicVal["force"] is Vector3 force1 && force1 != db1.m_Force) ||
                        (dicVal["gravity"] is Vector3 gravity1 && gravity1 != db1.m_Gravity) ||
                        (dicVal["weight"] is float weight1 && weight1 != db1.m_Weight) ||
                        (IsThisDBDE(__instance) && (
                            (dicVal["damping"] is float damping1 && damping1 != db1.m_Damping) ||
                            (dicVal["dampingD"] is AnimationCurve dampingD1 && !dampingD1.IsSame(db1.m_DampingDistrib)) ||
                            (dicVal["elasticity"] is float elasticity1 && elasticity1 != db1.m_Elasticity) ||
                            (dicVal["elasticityD"] is AnimationCurve elasticityD1 && !elasticityD1.IsSame(db1.m_ElasticityDistrib)) ||
                            (dicVal["inertia"] is float inertia1 && inertia1 != db1.m_Inert) ||
                            (dicVal["inertiaD"] is AnimationCurve inertiaD1 && !inertiaD1.IsSame(db1.m_InertDistrib)) ||
                            (dicVal["radius"] is float radius1 && radius1 != db1.m_Radius) ||
                            (dicVal["radiusD"] is AnimationCurve radiusD1 && !radiusD1.IsSame(db1.m_RadiusDistrib)) ||
                            (dicVal["stiffness"] is float stiffness1 && stiffness1 != db1.m_Stiffness) ||
                            (dicVal["stiffnessD"] is AnimationCurve stiffnessD1 && !stiffnessD1.IsSame(db1.m_StiffnessDistrib))
                        ))
                    )) ||
                    (__instance is DynamicBone_Ver02 db2 && (
                        (db2.Particles.Count > 0 && dicVal["tfPos"] is Vector3 tfPos2 && tfPos2 != db2.Particles[Mathf.Max(db2.Particles.Count - 2, 0)].Transform.position) ||
                        (dicVal["force"] is Vector3 force2 && force2 != db2.Force) ||
                        (dicVal["gravity"] is Vector3 gravity2 && gravity2 != db2.Gravity) ||
                        (dicVal["weight"] is float weight2 && weight2 != db2.Weight) ||
                        (IsThisDBDE(__instance) && (
                            (dicVal["dampingD"] is AnimationCurve dampingD2 && !dampingD2.IsSame(db2.Particles, CurveType.Damping)) ||
                            (dicVal["elasticityD"] is AnimationCurve elasticityD2 && !elasticityD2.IsSame(db2.Particles, CurveType.Elasticity)) ||
                            (dicVal["inertiaD"] is AnimationCurve inertiaD2 && !inertiaD2.IsSame(db2.Particles, CurveType.Inertia)) ||
                            (dicVal["radiusD"] is AnimationCurve radiusD2 && !radiusD2.IsSame(db2.Particles, CurveType.Radius)) ||
                            (dicVal["stiffnessD"] is AnimationCurve stiffnessD2 && !stiffnessD2.IsSame(db2.Particles, CurveType.Stiffness))
                        ))
                    ))
                ) {
                    dicDynBonesToUpdate[__instance] = frameAllowance;
                    result = true;
                } else {
                    result = false;
                }

                // Update saved values
                switch (__instance) {
                    case DynamicBone db:
                        dicVal["pos"] = db.m_Particles.Count > 0 ? db.m_Particles[db.m_Particles.Count - 1].m_Position : Vector3.zero;
                        dicVal["tfPos"] = db.m_Particles.Count > 0 ? db.m_Particles[Mathf.Max(db.m_Particles.Count - 2, 0)].m_Transform.position : Vector3.zero;
                        dicVal["force"] = db.m_Force;
                        dicVal["gravity"] = db.m_Gravity;
                        dicVal["weight"] = db.m_Weight;
                        if (IsThisDBDE(__instance)) {
                            dicVal["damping"] = db.m_Damping;
                            (dicVal["dampingD"] as AnimationCurve).Copy(db.m_DampingDistrib);
                            dicVal["elasticity"] = db.m_Elasticity;
                            (dicVal["elasticityD"] as AnimationCurve).Copy(db.m_ElasticityDistrib);
                            dicVal["inertia"] = db.m_Inert;
                            (dicVal["inertiaD"] as AnimationCurve).Copy(db.m_InertDistrib);
                            dicVal["radius"] = db.m_Radius;
                            (dicVal["radiusD"] as AnimationCurve).Copy(db.m_RadiusDistrib);
                            dicVal["stiffness"] = db.m_Stiffness;
                            (dicVal["stiffnessD"] as AnimationCurve).Copy(db.m_StiffnessDistrib);
                        }
                        break;
                    case DynamicBone_Ver01 db:
                        dicVal["pos"] = db.m_Particles.Count > 0 ? db.m_Particles[db.m_Particles.Count - 1].m_Position : Vector3.zero;
                        dicVal["tfPos"] = db.m_Particles.Count > 0 ? db.m_Particles[Mathf.Max(db.m_Particles.Count - 2, 0)].m_Transform.position : Vector3.zero;
                        dicVal["force"] = db.m_Force;
                        dicVal["gravity"] = db.m_Gravity;
                        dicVal["weight"] = db.m_Weight;
                        if (IsThisDBDE(__instance)) {
                            (dicVal["dampingD"] as AnimationCurve).Copy(db.m_DampingDistrib);
                            dicVal["elasticity"] = db.m_Elasticity;
                            (dicVal["elasticityD"] as AnimationCurve).Copy(db.m_ElasticityDistrib);
                            dicVal["inertia"] = db.m_Inert;
                            (dicVal["inertiaD"] as AnimationCurve).Copy(db.m_InertDistrib);
                            dicVal["radius"] = db.m_Radius;
                            (dicVal["radiusD"] as AnimationCurve).Copy(db.m_RadiusDistrib);
                            dicVal["stiffness"] = db.m_Stiffness;
                            (dicVal["stiffnessD"] as AnimationCurve).Copy(db.m_StiffnessDistrib);
                        }
                        break;
                    case DynamicBone_Ver02 db:
                        dicVal["pos"] = db.Particles.Count > 0 ? db.Particles[db.Particles.Count - 1].Position : Vector3.zero;
                        dicVal["tfPos"] = db.Particles.Count > 0 ? db.Particles[Mathf.Max(db.Particles.Count - 2, 0)].Transform.position : Vector3.zero;
                        dicVal["force"] = db.Force;
                        dicVal["gravity"] = db.Gravity;
                        dicVal["weight"] = db.Weight;
                        if (IsThisDBDE(__instance)) {
                            (dicVal["dampingD"] as AnimationCurve).Copy(db.Particles, CurveType.Damping);
                            (dicVal["elasticityD"] as AnimationCurve).Copy(db.Particles, CurveType.Elasticity);
                            (dicVal["inertiaD"] as AnimationCurve).Copy(db.Particles, CurveType.Inertia);
                            (dicVal["radiusD"] as AnimationCurve).Copy(db.Particles, CurveType.Radius);
                            (dicVal["stiffnessD"] as AnimationCurve).Copy(db.Particles, CurveType.Stiffness);
                        }
                        break;
                }

                // Have to call these methods when disabling the calculations, otherwise the bones won't function properly
                if (!result) {
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
                Vector3 pos = Vector3.zero;
                switch (__instance) {
                    case DynamicBone db:
                        if (db.m_Particles.Count > 0) pos = db.m_Particles[db.m_Particles.Count - 1].m_Position;
                        break;
                    case DynamicBone_Ver01 db:
                        if (db.m_Particles.Count > 0) pos = db.m_Particles[db.m_Particles.Count - 1].m_Position;
                        break;
                    case DynamicBone_Ver02 db:
                        if (db.Particles.Count > 0) pos = db.Particles[db.Particles.Count - 1].Position;
                        break;
                }

                // If it didn't change during the update, subtract from the frame allowance
                if (dicDynBoneVals[__instance]["pos"] is Vector3 posVal && posVal.IsSame(pos)) {
                    if (dicDynBonesToUpdate.TryGetValue(__instance, out int frames) && frames > 0) {
                        dicDynBonesToUpdate[__instance] = frames - 1;
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
        }

        internal static class ConditionalHooks {
            private static Harmony _harmony;

            internal static DBDEUI DBDEUI = null;

            // Enable conditional patches
            public static void SetupHooks() {
                // Not really a patch here but oh well, we need to get the DBDEUI reference
                var plugins = Performancer.Instance.gameObject.GetComponents<MonoBehaviour>();
                foreach (var plugin in plugins) {
                    if (plugin is DBDEUI dbdeui) {
                        _harmony = Harmony.CreateAndPatchAll(typeof(ConditionalHooks), null);
                        DBDEUI = dbdeui;
                        break;
                    }
                }
            }

            // Disable conditional patches
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }
        }
    }
}
