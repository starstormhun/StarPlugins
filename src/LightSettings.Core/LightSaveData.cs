﻿using MessagePack;
using UnityEngine;

namespace LightSettings.Koikatu {
    [MessagePackObject(true)]
    public class LightSaveData {
        public int ObjectId;

        public bool state;
        public LightShadows shadows;
        public UnityEngine.Rendering.LightShadowResolution shadowResolution;
        public float shadowStrength;
        public float shadowBias;
        public float shadowNormalBias;
        public float shadowNearPlane;
        public LightRenderMode renderMode;
        public int cullingMask; // 1024 = chara, 2048 = map

        // Cookie
        public string cookieHash = "";
        public float cookieSize;

        // Extra for lights attached to items
        public Color color;
        public float intensity;
        public float range;
        public float spotAngle;
    }
}
