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
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Performancer", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class Performancer : BaseUnityPlugin {
        public const string GUID = "starstorm.performancer";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public static Performancer Instance { get; private set; }

        public static ConfigEntry<bool> OptimiseGuideObjectLate {  get; private set; }
        public static ConfigEntry<bool> OptimiseDynamicBones {  get; private set; }
        public static ConfigEntry<bool> DoLogs {  get; private set; }

        internal static bool isLogCoroutine = false;
        internal static int numGuideObjectLateUpdates = 0;

        private static MonoBehaviour selectDynBone = null;
        private static Dictionary<DynamicBoneCollider, Dictionary<string, object>> dicColliderVals = new Dictionary<DynamicBoneCollider, Dictionary<string, object>>();

        private void Awake() {
            Instance = this;

            DoLogs = Config.Bind("Advanced", "Log Performance Numbers", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimiseGuideObjectLate = Config.Bind("General", "Optimise GuideObject LateUpdate", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimiseDynamicBones = Config.Bind("General", "Optimise Dynamic Bones", true, new ConfigDescription("REQUIRES GuideObject LateUpdate optimisation", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            HookPatch.Init();
        }

        private void Update() {
            DoLogging();
            if (OptimiseGuideObjectLate.Value && OptimiseDynamicBones.Value && HookPatch.Hooks.dicDynBoneVals.Count > 0) {
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
            // Make sure we have a valid, active bone selected, if one exists in the scene
            if (selectDynBone == null || selectDynBone.IsDestroyed()) {
                selectDynBone = FindObjectOfType<DynamicBone_Ver02>();
                if (selectDynBone == null) {
                    selectDynBone = FindObjectOfType<DynamicBone>();
                }
                if (selectDynBone == null) {
                    selectDynBone = FindObjectOfType<DynamicBone_Ver01>();
                }
                if (selectDynBone == null) {
                    HookPatch.Hooks.ClearDynBoneDics();
                }
            }

            // Check the colliders
            if (selectDynBone != null) {
                List<DynamicBoneCollider> colliders = null;
                switch (selectDynBone) {
                    case DynamicBone db: colliders = db.m_Colliders; break;
                    case DynamicBone_Ver01 db: colliders = db.m_Colliders; break;
                    case DynamicBone_Ver02 db: colliders = db.Colliders; break;
                }
                if (colliders != null && colliders.Count > 0) {
                    foreach (DynamicBoneCollider collider in colliders) {
                        if (collider.isActiveAndEnabled) {
                            // Add collider to dict if it doesn't exist
                            if (!dicColliderVals.ContainsKey(collider)) {
                                dicColliderVals.Add(collider, new Dictionary<string, object> {
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

                            // Check if the collider has changed
                            var vals = dicColliderVals[collider];
                            if (
                                (vals["pos"] is Vector3 pos && pos != collider.transform.position) ||
                                (vals["rot"] is Quaternion rot && rot != collider.transform.rotation) ||
                                (vals["scale"] is Vector3 scale && scale != collider.transform.lossyScale) ||
                                (vals["center"] is Vector3 center && center != collider.m_Center) ||
                                (vals["radius"] is float radius && radius != collider.m_Radius) ||
                                (vals["height"] is float height && height != collider.m_Height) ||
                                (vals["bound"] is DynamicBoneCollider.Bound bound && bound != collider.m_Bound) ||
                                (vals["direction"] is DynamicBoneCollider.Direction direction && direction != collider.m_Direction)
                            ) {
                                // Check each bone if they're within relevant distance, and activate them if so
                                var destroyedBones = new List<MonoBehaviour>();
                                float collR = Mathf.Max(collider.m_Height, collider.m_Radius) * collider.transform.lossyScale.magnitude;
                                foreach (var bone in HookPatch.Hooks.dicDynBoneVals.Keys) {
                                    if (bone.IsDestroyed()) {
                                        destroyedBones.Add(bone);
                                    } else {
                                        if (HookPatch.Hooks.dicDynBonesToUpdate[bone] == 0) {
                                            float boneR = 0f;
                                            Vector3 bonePos = Vector3.zero;
                                            switch (bone) {
                                                case DynamicBone db:
                                                    boneR = 2 * db.m_Radius;
                                                    if (db.m_Particles.Count > 0) bonePos = db.m_Particles[Mathf.Max(db.m_Particles.Count - 2, 0)].m_Transform.position;
                                                    break;
                                                case DynamicBone_Ver01 db:
                                                    boneR = 2 * db.m_Radius;
                                                    if (db.m_Particles.Count > 0) bonePos = db.m_Particles[Mathf.Max(db.m_Particles.Count - 2, 0)].m_Transform.position;
                                                    break;
                                                case DynamicBone_Ver02 db:
                                                    if (db.Particles.Count > 0) boneR = 2 * db.Particles[db.Particles.Count - 1].Radius;
                                                    if (db.Particles.Count > 0) bonePos = db.Particles[Mathf.Max(db.Particles.Count - 2, 0)].Transform.position;
                                                    break;
                                            }
                                            if ((collider.transform.position + collider.m_Center - bonePos).sqrMagnitude <= (collR + boneR)*(collR + boneR)) {
                                                HookPatch.Hooks.dicDynBonesToUpdate[bone] = HookPatch.Hooks.frameAllowance;
                                            }
                                        }
                                    }
                                }
                                foreach (var bone in destroyedBones) {
                                    HookPatch.Hooks.dicDynBoneVals.Remove(bone);
                                    HookPatch.Hooks.dicDynBonesToUpdate.Remove(bone);
                                }
                            }

                            // Save current collider values
                            dicColliderVals[collider]["pos"] = collider.transform.position;
                            dicColliderVals[collider]["rot"] = collider.transform.rotation;
                            dicColliderVals[collider]["scale"] = collider.transform.lossyScale;
                            dicColliderVals[collider]["center"] = collider.m_Center;
                            dicColliderVals[collider]["radius"] = collider.m_Radius;
                            dicColliderVals[collider]["height"] = collider.m_Height;
                            dicColliderVals[collider]["bound"] = collider.m_Bound;
                            dicColliderVals[collider]["direction"] = collider.m_Direction;
                        }
                    }
                }
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
                default: return;
            }
        }
    }
}
