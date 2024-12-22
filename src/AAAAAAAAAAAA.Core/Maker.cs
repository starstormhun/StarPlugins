using BepInEx;
using UnityEngine;

namespace AAAAAAAAAAAA {
    public partial class AAAAAAAAAAAA : BaseUnityPlugin {
        internal static void OnParentButtonPressed() {
            if (HookPatch.Maker.parentDropdown.value != HookPatch.Maker.aaaaaaaaaaaaOptionIdx) {
                HookPatch.Maker.parentDropdown.value = HookPatch.Maker.aaaaaaaaaaaaOptionIdx;
                return;
            }
            if (!HookPatch.Maker.parentToggle.isOn) {
                HookPatch.Maker.parentToggle.isOn = true;
                return;
            }
            RegisterParent();
        }

        internal static void RegisterParent() {
            if (IsDebug.Value) Instance.Log("Registering new or updated parent...");
        }
    }
}
