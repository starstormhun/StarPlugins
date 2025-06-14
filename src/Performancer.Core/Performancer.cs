using Studio;
using BepInEx;
using UnityEngine;
using KKAPI.Utilities;
using System.Collections;
using BepInEx.Configuration;
using System.Collections.Generic;

[assembly: System.Reflection.AssemblyFileVersion(Performancer.Performancer.Version)]

namespace Performancer {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(DynamicBoneDistributionEditor.DBDE.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(HSPE.HSPE.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(VideoExport.VideoExport.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Performancer", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class Performancer : BaseUnityPlugin {
        public const string GUID = "starstorm.performancer";
        public const string Version = "1.2.5." + BuildNumber.Version;

        public static Performancer Instance { get; private set; }

        public static ConfigEntry<bool> OptimiseGuideObjectLate {  get; private set; }
        public static ConfigEntry<bool> OptimiseDynamicBones {  get; private set; }
        public static ConfigEntry<bool> DoLogs {  get; private set; }

        internal static bool isLogCoroutine = false;
        internal static int numGuideObjectLateUpdates = 0;
        internal static int guideObjectAfterLoadFrames = 0;

        internal static Dictionary<DynamicBoneCollider, Dictionary<string, object>> dicColliderVals = new Dictionary<DynamicBoneCollider, Dictionary<string, object>>();
        internal static List<DynamicBoneCollider> movedColliders = new List<DynamicBoneCollider>();

        private void Awake() {
            Instance = this;

            DoLogs = Config.Bind("Advanced", "Log Performance Numbers", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimiseGuideObjectLate = Config.Bind("General", "Optimise GuideObject LateUpdate", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimiseDynamicBones = Config.Bind("General", "Optimise Dynamic Bones", true, new ConfigDescription("REQUIRES GuideObject LateUpdate optimisation", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Studio.SaveLoad.StudioSaveLoadApi.SceneLoad += (x, y) => {
                foreach (var key in new List<GuideObject>(HookPatch.Hooks.dicGuideObjectsToUpdate.Keys)) {
                    HookPatch.Hooks.dicGuideObjectsToUpdate[key] = HookPatch.Hooks.frameAllowance;
                }
            };

            HookPatch.Init();
        }

        private void Update() {
            DoLogging();
            if (OptimiseGuideObjectLate.Value && OptimiseDynamicBones.Value && dicColliderVals.Count > 0) {
                CheckDynBoneColliders();
            }
        }

        public bool EnableDynamicBone(MonoBehaviour bone) {
            if (HookPatch.Hooks.dicDynBonesToUpdate.ContainsKey(bone)) {
                HookPatch.Hooks.dicDynBonesToUpdate[bone] = HookPatch.Hooks.frameAllowance;
                return true;
            } else {
                return false;
            }
        }

        public bool EnableGuideObject(GuideObject guide) {
            if (guide == null) return false;
            try {
                HookPatch.Hooks.dicGuideObjectsToUpdate[guide] = 2;
                return true;
            } catch {
                return false;
            }
        }

        private void DoLogging() {
            if (!isLogCoroutine && DoLogs.Value) {
                isLogCoroutine = true;
                StartCoroutine(LogCoroutine());

                IEnumerator LogCoroutine() {
                    yield return new WaitForSeconds(1f);
                    Log($"GuideObject LateUpdates this second: {numGuideObjectLateUpdates}");
                    numGuideObjectLateUpdates = 0;
                    isLogCoroutine = false;
                }
            }
        }

        private void CheckDynBoneColliders() {
            // Check the colliders
            var collsDestroyed = new List<DynamicBoneCollider>();
            foreach (var kvp in dicColliderVals) {
                var collider = kvp.Key;
                if (collider.IsDestroyed()) {
                    collsDestroyed.Add(collider);
                    continue;
                }
                if (collider.isActiveAndEnabled) {
                    // Check if the collider has changed
                    var vals = dicColliderVals[collider];
                    bool KKPEAdvancedMode = HookPatch.ConditionalHooks.IsKKPEOpen();
                    if (
                        (vals["pos"] is Vector3 pos && pos != collider.transform.position) ||
                        (vals["rot"] is Quaternion rot && rot != collider.transform.rotation) ||
                        (vals["scale"] is Vector3 scale && scale != collider.transform.lossyScale) ||
                        KKPEAdvancedMode && (
                            (vals["center"] is Vector3 center && center != collider.m_Center) ||
                            (vals["radius"] is float radius && radius != collider.m_Radius) ||
                            (vals["height"] is float height && height != collider.m_Height) ||
                            (vals["bound"] is DynamicBoneCollider.Bound bound && bound != collider.m_Bound) ||
                            (vals["direction"] is DynamicBoneCollider.Direction direction && direction != collider.m_Direction)
                        )
                    ) {
                        vals["moved"] = true;
                        movedColliders.Add(collider);

                        // Save changed collider values
                        dicColliderVals[collider]["pos"] = collider.transform.position;
                        dicColliderVals[collider]["rot"] = collider.transform.rotation;
                        dicColliderVals[collider]["scale"] = collider.transform.lossyScale;
                        if (KKPEAdvancedMode) {
                            dicColliderVals[collider]["center"] = collider.m_Center;
                            dicColliderVals[collider]["radius"] = collider.m_Radius;
                            dicColliderVals[collider]["height"] = collider.m_Height;
                            dicColliderVals[collider]["bound"] = collider.m_Bound;
                            dicColliderVals[collider]["direction"] = collider.m_Direction;
                        }
                    }
                } else {
                    dicColliderVals[collider]["moved"] = false;
                }
            }
            foreach (var collider in collsDestroyed) {
                dicColliderVals.Remove(collider);
            }

            StartCoroutine(ClearMovedColliders());
            IEnumerator ClearMovedColliders() {
                yield return CoroutineUtils.WaitForEndOfFrame;
                foreach (var collider in movedColliders) {
                    dicColliderVals[collider]["moved"] = false;
                }
                movedColliders.Clear();
            }
        }

        internal void Log(object data, int level = 0) {
            switch (level) {
                case 0:
                    Logger.LogInfo(data); return;
                case 1:
                    Logger.LogDebug(data); return;
                case 2:
                    Logger.LogWarning(data); return;
                case 3:
                    Logger.LogError(data); return;
                case 4:
                    Logger.LogFatal(data); return;
                case 5:
                    Logger.LogMessage(data); return;
                default: return;
            }
        }
    }
}
