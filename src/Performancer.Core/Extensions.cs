using Studio;
using UnityEngine;
using System.Collections.Generic;

namespace Performancer {
    public static class Extensions {
        internal static GameObject GetObject(this ObjectCtrlInfo _objectCtrlInfo) {
            GameObject objectItem;
            switch (_objectCtrlInfo) {
                case OCIItem t1:
                    OCIItem OCI1 = (OCIItem)_objectCtrlInfo;
                    objectItem = OCI1.objectItem;
                    break;
                case OCIFolder t2:
                    OCIFolder OCI2 = (OCIFolder)_objectCtrlInfo;
                    objectItem = OCI2.objectItem;
                    break;
                case OCILight t3:
                    OCILight OCI3 = (OCILight)_objectCtrlInfo;
                    objectItem = OCI3.objectLight;
                    break;
                case OCICamera t4:
                    OCICamera OCI4 = (OCICamera)_objectCtrlInfo;
                    objectItem = OCI4.objectItem;
                    break;
                case OCIChar t5:
                    OCIChar OCI5 = (OCIChar)_objectCtrlInfo;
                    objectItem = OCI5.charInfo.gameObject;
                    break;
                case OCIRoute t6:
                    OCIRoute OCI6 = (OCIRoute)_objectCtrlInfo;
                    objectItem = OCI6.objectItem;
                    break;
                default:
                    return null;
            }
            return objectItem;
        }

        internal static bool IsSame(this Vector3 v1, Vector3 v2, float delta = 1E-06f) {
            float mDelta = -delta;
            float dX = v1.x - v2.x;
            float dY = v1.y - v2.y;
            float dZ = v1.z - v2.z;
            return dX < delta && dX > mDelta && dY < delta && dY > mDelta && dZ < delta && dZ > mDelta;
        }

        internal static bool IsSame(this AnimationCurve curve1, AnimationCurve curve2, float delta = 1E-06f) {
            if (curve1.keys.Length != curve2.keys.Length) return false;
            float mDelta = -delta;
            for (int i = 0; i < curve1.keys.Length; i++) {
                var c1 = curve1.keys[i];
                var c2 = curve2.keys[i];
                float valDiff = c1.m_Value - c2.m_Value;
                float timeDiff = c1.m_Time - c2.m_Time;
                float inTanDiff = c1.m_InTangent - c2.m_InTangent;
                float outTanDiff = c1.m_OutTangent - c2.m_OutTangent;
                if (
                    valDiff >= delta || valDiff <= mDelta ||
                    timeDiff >= delta || timeDiff <= mDelta ||
                    inTanDiff >= delta || inTanDiff <= mDelta ||
                    outTanDiff >= delta || outTanDiff <= mDelta
                ) return false;
            }
            return true;
        }

        internal static bool IsSame(this AnimationCurve curve1, List<DynamicBone_Ver02.Particle> curve2, CurveType type, float delta = 1E-06f) {
            if (curve1.keys.Length != curve2.Count) return false;
            float mDelta = -delta;
            // The switch is on the outside so it doesn't have to switch for every iteration
            switch (type) {
                case CurveType.Damping:
                    for (int i = 0; i < curve1.keys.Length; i++) {
                        float diff = curve1.keys[i].m_Value - curve2[i].Damping;
                        if (diff >= delta || diff <= mDelta) return false;
                    }
                    break;
                case CurveType.Elasticity:
                    for (int i = 0; i < curve1.keys.Length; i++) {
                        float diff = curve1.keys[i].m_Value - curve2[i].Elasticity;
                        if (diff >= delta || diff <= mDelta) return false;
                    }
                    break;
                case CurveType.Stiffness:
                    for (int i = 0; i < curve1.keys.Length; i++) {
                        float diff = curve1.keys[i].m_Value - curve2[i].Stiffness;
                        if (diff >= delta || diff <= mDelta) return false;
                    }
                    break;
                case CurveType.Radius:
                    for (int i = 0; i < curve1.keys.Length; i++) {
                        float diff = curve1.keys[i].m_Value - curve2[i].Radius;
                        if (diff >= delta || diff <= mDelta) return false;
                    }
                    break;
                case CurveType.Inertia:
                    for (int i = 0; i < curve1.keys.Length; i++) {
                        float diff = curve1.keys[i].m_Value - curve2[i].Inert;
                        if (diff >= delta || diff <= mDelta) return false;
                    }
                    break;
            }
            return true;
        }

        internal static void Copy(this AnimationCurve curve1, AnimationCurve curve2) {
            int keyCount1 = curve1.keys.Length;
            int keyCount2 = curve2.keys.Length;
            if (keyCount1 < keyCount2) {
                for (int i = keyCount1; i < keyCount2; i++) {
                    var c2 = curve2.keys[i];
                    curve1.AddKey(new Keyframe(c2.time, c2.value, c2.m_InTangent, c2.m_OutTangent));
                }
            } else if (keyCount1 > keyCount2) {
                for (int i = keyCount1 - 1; i >= keyCount2; i--) {
                    curve1.RemoveKey(i);
                }
            }
            for (int i = 0; i < keyCount1; i++) {
                var c1 = curve1.keys[i];
                var c2 = curve2.keys[i];
                c1.time = c2.time;
                c1.value = c2.value;
                c1.inTangent = c2.inTangent;
                c1.outTangent = c2.outTangent;
            }
        }

        internal static void Copy(this AnimationCurve curve, List<DynamicBone_Ver02.Particle> particles, CurveType type) {
            int keyCount = curve.keys.Length;
            int partCount = particles.Count;
            if (keyCount > partCount) {
                for (int i = keyCount - 1; i >= partCount; i--) {
                    curve.RemoveKey(i);
                }
            }
            // The switch is on the outside so it doesn't have to switch for every iteration
            switch (type) {
                case CurveType.Damping:
                    if (keyCount < partCount) {
                        for (int i = keyCount; i < partCount; i++) {
                            curve.AddKey(i, particles[i].Damping);
                        }
                    }
                    for (int i = 0; i < keyCount; i++) {
                        curve.keys[i].value = particles[i].Damping;
                    }
                    break;
                case CurveType.Elasticity:
                    if (keyCount < partCount) {
                        for (int i = keyCount; i < partCount; i++) {
                            curve.AddKey(i, particles[i].Elasticity);
                        }
                    }
                    for (int i = 0; i < keyCount; i++) {
                        curve.keys[i].value = particles[i].Elasticity;
                    }
                    break;
                case CurveType.Inertia:
                    if (keyCount < partCount) {
                        for (int i = keyCount; i < partCount; i++) {
                            curve.AddKey(i, particles[i].Inert);
                        }
                    }
                    for (int i = 0; i < keyCount; i++) {
                        curve.keys[i].value = particles[i].Inert;
                    }
                    break;
                case CurveType.Radius:
                    if (keyCount < partCount) {
                        for (int i = keyCount; i < partCount; i++) {
                            curve.AddKey(i, particles[i].Radius);
                        }
                    }
                    for (int i = 0; i < keyCount; i++) {
                        curve.keys[i].value = particles[i].Radius;
                    }
                    break;
                case CurveType.Stiffness:
                    if (keyCount < partCount) {
                        for (int i = keyCount; i < partCount; i++) {
                            curve.AddKey(i, particles[i].Stiffness);
                        }
                    }
                    for (int i = 0; i < keyCount; i++) {
                        curve.keys[i].value = particles[i].Stiffness;
                    }
                    break;
            }
        }

        internal static Vector3[] GetDBPos(this MonoBehaviour bone, bool posOnly = false) {
            Vector3 pos = Vector3.zero;
            Vector3 tfPos = Vector3.zero;
            int pCount;
            switch (bone) {
                case DynamicBone db:
                    pCount = db.m_Particles.Count;
                    if (pCount == 0) break;
                    pos = db.m_Particles[pCount - 1].m_Position;
                    if (posOnly) break;
                    for (int i = pCount - 1; i >= 0; i--) {
                        if (db.m_Particles[i].m_Transform != null) {
                            tfPos = db.m_Particles[i].m_Transform.position;
                            break;
                        }
                    }
                    break;
                case DynamicBone_Ver01 db:
                    pCount = db.m_Particles.Count;
                    if (pCount == 0) break;
                    pos = db.m_Particles[pCount - 1].m_Position;
                    if (posOnly) break;
                    for (int i = pCount - 1; i >= 0; i--) {
                        if (db.m_Particles[i].m_Transform != null) {
                            tfPos = db.m_Particles[i].m_Transform.position;
                            break;
                        }
                    }
                    break;
                case DynamicBone_Ver02 db:
                    pCount = db.Particles.Count;
                    if (pCount == 0) break;
                    pos = db.Particles[pCount - 1].Position;
                    if (posOnly) break;
                    for (int i = pCount - 1; i >= 0; i--) {
                        if (db.Particles[i].Transform != null) {
                            tfPos = db.Particles[i].Transform.position;
                            break;
                        }
                    }
                    break;
            }
            return new Vector3[] { pos, tfPos };
        }
    }

    internal enum CurveType {
        Damping = 0,
        Elasticity = 1,
        Inertia = 2,
        Radius = 3,
        Stiffness = 4,
    }
}
