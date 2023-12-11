using UnityEngine;

namespace LightToggler.Koikatu {
    public static class Extensions {
        public static void SetAllLightsState(this GameObject gameObject, bool state) {
            Light light;
            if (light = gameObject.GetComponent<Light>()) light.enabled = state;
            foreach (Light childLight in gameObject.GetComponentsInChildren<Light>())
                childLight.enabled = state;
        }
    }
}
