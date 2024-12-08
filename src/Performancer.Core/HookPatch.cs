using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Studio;
using ADV.Commands.Base;
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

            private static Dictionary<GuideObject, Dictionary<string, Vector3>> dicVals = new Dictionary<GuideObject, Dictionary<string, Vector3>>();
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
                bool result;
                if (!dicVals.ContainsKey(__instance)) {
                    var newDict = new Dictionary<string, Vector3> {
                        { "pos", __instance.m_ChangeAmount.pos },
                        { "rot", __instance.m_ChangeAmount.rot },
                        { "scale", __instance.m_ChangeAmount.scale }
                    };
                    dicVals.Add(__instance, newDict);
                    result = true;
                } else if (!Performancer.OptimiseGuideObjectLate.Value) {
                    result = true;
                } else if (dicGuideObjectsToUpdate.TryGetValue(__instance, out bool dicVal) && dicVal) {
                    result = true;
                    dicGuideObjectsToUpdate[__instance] = false;
                } else {
                    var vals = dicVals[__instance];
                    result = vals["pos"] != __instance.m_ChangeAmount.pos || vals["rot"] != __instance.m_ChangeAmount.rot || vals["scale"] != __instance.m_ChangeAmount.scale;

                    // GuideObject has changed, therefore we need to update the object's children
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
                                var iterateList = new List<TreeNodeObject> { oci.treeNodeObject };
                                while (iterateList.Count > 0) {
                                    var curr = iterateList.Pop();
                                    dicGuideObjectsToUpdate[Studio.Studio.Instance.dicInfo[curr].guideObject] = true;
                                    iterateList.AddRange(curr.child);
                                }
                            }
                        }
                    }
                }

                dicVals[__instance]["pos"] = __instance.m_ChangeAmount.pos;
                dicVals[__instance]["rot"] = __instance.m_ChangeAmount.rot;
                dicVals[__instance]["scale"] = __instance.m_ChangeAmount.scale;

                if (result) {
                    Performancer.numGuideObjectLateUpdates++;
                }
                return result;
            }
        }
    }
}
