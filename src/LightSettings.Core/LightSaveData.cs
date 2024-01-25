using MessagePack;
using UnityEngine;

namespace LightSettings.Koikatu {
    [MessagePackObject(true)]
    public class LightSaveData {
        public int ObjectId;

        public LightShadows shadows;
        public UnityEngine.Rendering.LightShadowResolution shadowResolution;
        public float shadowStrength;
        public float shadowBias;
        public float shadowNormalBias;
        public float shadowNearPlane;
        public UnityEngine.LightRenderMode renderMode;
        public int cullingMask; // 1024 = chara, 2048 = map
    }
}
