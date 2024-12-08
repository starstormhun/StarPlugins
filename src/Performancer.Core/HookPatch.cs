using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Studio;
using Illusion.Extensions;

namespace Performancer {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        internal static class Hooks {
            private static Harmony _harmony;

            private const int frameAllowance = 6;

            private static Dictionary<Transform, GuideObject> dicGuideObjects = new Dictionary<Transform, GuideObject>();
            private static Dictionary<GuideObject, Dictionary<string, Vector3>> dicGuideObjectVals = new Dictionary<GuideObject, Dictionary<string, Vector3>>();
            internal static Dictionary<MonoBehaviour, Dictionary<string, object>> dicDynBoneVals = new Dictionary<MonoBehaviour, Dictionary<string, object>>();

            private static Dictionary<GuideObject, bool> dicGuideObjectsToUpdate = new Dictionary<GuideObject, bool>();
            private static Dictionary<MonoBehaviour, int> dicDynBonesToUpdate = new Dictionary<MonoBehaviour, int>();

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
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
                    var newDict = new Dictionary<string, Vector3> {
                        { "pos", __instance.m_ChangeAmount.pos },
                        { "rot", __instance.m_ChangeAmount.rot },
                        { "scale", __instance.m_ChangeAmount.scale }
                    };
                    dicGuideObjectVals.Add(__instance, newDict);
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
                    result = vals["pos"] != __instance.m_ChangeAmount.pos || vals["rot"] != __instance.m_ChangeAmount.rot || vals["scale"] != __instance.m_ChangeAmount.scale;

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
                                var iterateList = new List<Transform> { oci.GetObject().transform };
                                while (iterateList.Count > 0) {
                                    var curr = iterateList.Pop();
                                    if (dicGuideObjects.ContainsKey(curr)) {
                                        dicGuideObjectsToUpdate[dicGuideObjects[curr]] = true;
                                    }
                                    iterateList.AddRange(curr.Children());

                                    if (Performancer.OptimiseDynamicBones.Value) {
                                        var dynBones = new List<MonoBehaviour>();
                                        dynBones.AddRange(curr.GetComponents<DynamicBone>());
                                        dynBones.AddRange(curr.GetComponents<DynamicBone_Ver02>());
                                        foreach (var dynBone in dynBones) {
                                            // I leave some leeway for the bone to start/stop moving
                                            dicDynBonesToUpdate[dynBone] = frameAllowance;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                dicGuideObjectVals[__instance]["pos"] = __instance.m_ChangeAmount.pos;
                dicGuideObjectVals[__instance]["rot"] = __instance.m_ChangeAmount.rot;
                dicGuideObjectVals[__instance]["scale"] = __instance.m_ChangeAmount.scale;

                if (result) {
                    Performancer.numGuideObjectLateUpdates++;
                }
                return result;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DynamicBone), "Start")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "Awake")]
            private static void DynamicBonesAfterCreate(ref MonoBehaviour __instance) {
                // Register bones when spawned
                dicDynBoneVals[__instance] = new Dictionary<string, object> {
                    { "enabled", __instance.isActiveAndEnabled }
                };
                switch (__instance) {
                    case DynamicBone db:
                        dicDynBoneVals[__instance]["pos"] = db.m_Particles[db.m_Particles.Count - 1].m_Position;
                        break;
                    case DynamicBone_Ver02 db:
                        dicDynBoneVals[__instance]["pos"] = db.Particles[db.Particles.Count - 1].Position;
                        break;
                }

                // Allow bones to settle when spawned
                dicDynBonesToUpdate[__instance] = 2*frameAllowance;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
            [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
            private static bool DynamicBonesBeforeLateUpdate(ref MonoBehaviour __instance) {
                bool result;
                // If we don't optimise, then always run the Update scripts
                if (!Performancer.OptimiseGuideObjectLate.Value || !Performancer.OptimiseDynamicBones.Value) {
                    result = true;
                // If there's leeway time left, continue running
                } else if (dicDynBonesToUpdate.TryGetValue(__instance, out int framesLeft) && framesLeft > 0) {
                    result = true;
                // If the component was just turned on, add leeway frames
                } else if (dicDynBoneVals[__instance]["enabled"] is bool enabled && !enabled && __instance.isActiveAndEnabled) {
                    dicDynBonesToUpdate[__instance] = frameAllowance;
                    result = true;
                } else {
                    result = false;
                }

                // Update saved values
                dicDynBoneVals[__instance]["enabled"] = __instance.isActiveAndEnabled;
                switch (__instance) {
                    case DynamicBone db:
                        if (db.m_Particles.Count > 0) {
                            dicDynBoneVals[__instance]["pos"] = db.m_Particles[db.m_Particles.Count - 1].m_Position;
                        } else {
                            dicDynBoneVals[__instance]["pos"] = Vector3.zero;
                        }
                        break;
                    case DynamicBone_Ver02 db:
                        if (db.Particles.Count > 0) {
                            dicDynBoneVals[__instance]["pos"] = db.Particles[db.Particles.Count - 1].Position;
                        } else {
                            dicDynBoneVals[__instance]["pos"] = Vector3.zero;
                        }
                        break;
                }

                if (!result) {
                    switch (__instance) {
                        case DynamicBone db: db.ApplyParticlesToTransforms(); break;
                        case DynamicBone_Ver02 db: db.ApplyParticlesToTransforms(); break;
                    }
                }

                return result;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
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
                    case DynamicBone_Ver02 db:
                        if (db.Particles.Count > 0) pos = db.Particles[db.Particles.Count - 1].Position;
                        break;
                }

                // If it didn't change during the update, subtract from the frame allowance
                if (dicDynBoneVals[__instance]["pos"] is Vector3 dicPos && dicPos == pos) {
                    if (dicDynBonesToUpdate.TryGetValue(__instance, out int frames) && frames > 0) {
                        dicDynBonesToUpdate[__instance] = frames - 1;
                    }
                }
            }
        }
    }
}
