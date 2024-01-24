using UnityEngine;

namespace BetterScaling.Koikatu {
    static class Extensions {
        public static Vector3 ToDb(this Vector3 v) => new Vector3(Mathf.Log10(v.x), Mathf.Log10(v.y), Mathf.Log10(v.z))*20;

        public static Vector3 FromDb(this Vector3 v) => new Vector3(Mathf.Pow(10, v.x / 20), Mathf.Pow(10, v.y / 20), Mathf.Pow(10, v.z / 20));

        public static Vector3 ScaleImmut(this Vector3 v, Vector3 s) => new Vector3(v.x * s.x, v.y * s.y, v.z * s.z);

        public static Vector3 Invert(this Vector3 v) => new Vector3(1 / v.x, 1 / v.y, 1 / v.z);

#if KK
        public static bool TryGetComponent<T>(this Component tf, out T component) {
            component = tf.GetComponent<T>();
            return component != null;
        }

        public static bool TryGetComponent<T>(this GameObject go, out T component) => go.transform.TryGetComponent<T>(out component);
#endif
    }
}
