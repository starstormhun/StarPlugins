using MessagePack;
using UnityEngine;

namespace LightSettings.Koikatu {
    internal class LightDataContainer : MonoBehaviour {
        public int DefaultLayer;
        public int defaultCullingMask; // 1024 = chara, 2048 = map
    }

    [MessagePackObject(true)]
    public class LightSaveData {
        public int DefaultLayer;
        public int NewLayer;
        public int ObjectId;
    }
}
