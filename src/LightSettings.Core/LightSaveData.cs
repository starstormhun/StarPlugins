using MessagePack;
using UnityEngine;

namespace LightSettings.Koikatu {
    internal class LightDataContainer : MonoBehaviour {
        public int DefaultLayer;
    }

    [MessagePackObject(true)]
    public class LightSaveData {
        public int DefaultLayer;
        public int NewLayer;
        public int ObjectId;
    }
}
