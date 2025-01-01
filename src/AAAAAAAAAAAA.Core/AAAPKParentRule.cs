using System;
using MessagePack;

namespace AAAAAAAAAAAA {
    [MessagePackObject(false)]
    [Serializable]
    public class AAAPKParentRule {
        [Key("Coordinate")]
        public int Coordinate { get; set; }

        [Key("Slot")]
        public int Slot { get; set; }

        [Key("ParentPath")]
        public string ParentPath { get; set; }

        [Key("ParentType")]
        public ParentType ParentType { get; set; }

        [Key("ParentSlot")]
        public int ParentSlot { get; set; }
    }

    public enum ParentType {
        Unknown,
        Clothing,
        Accessory,
        Hair,
        Character,
    }
}
