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

        private static class Hooks {
            private static Harmony _harmony;

            private static Dictionary<Transform, GuideObject> dicGuideObjects = new Dictionary<Transform, GuideObject>();
            private static Dictionary<GuideObject, Dictionary<string, Vector3>> dicGuideObjectVals = new Dictionary<GuideObject, Dictionary<string, Vector3>>();
            private static Dictionary<GuideObject, bool> dicGuideObjectsToUpdate = new Dictionary<GuideObject, bool>();

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
        }
    }
}
