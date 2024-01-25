using MessagePack;
using UnityEngine;

namespace LightSettings.Koikatu {
    internal class LightDataContainer : MonoBehaviour {
        public UnityEngine.LightShadows defaultShadows;
        public float defaultShadowStrength;
        public UnityEngine.Rendering.LightShadowResolution defaultShadowResolution;
        public float defaultShadowBias;
        public float defaultShadowNormalBias;
        public float defaultShadowNearPlane;
        public UnityEngine.LightRenderMode defaultRenderMode;
        public int cullingMask;
    }

    [MessagePackObject(true)]
    public class LightData {
        public int DefaultLayer;
        public int NewLayer;
        public int ObjectId;
    }
}
