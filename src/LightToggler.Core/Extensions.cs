using UnityEngine;
using Studio;

namespace LightToggler.Koikatu {
    public static class Extensions {
        public static void SetAllLightsState(this GameObject _gameObject, bool _state) {
            Light light;
            if (light = _gameObject.GetComponent<Light>()) light.enabled = _state;
            foreach (Light childLight in _gameObject.GetComponentsInChildren<Light>()) {
                childLight.enabled = _state;
            }
            LightToggler.updateLightPanel = true;
        }
    }
}
