using static ViewpointSwitching.ViewpointSwitching;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ViewpointSwitching {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        private static class Hooks {
            private static Harmony _harmony;
            
            // Setup functionality on launch / enable
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            // Disable functionality when disabled in settings
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Modify FOV limit
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Studio.CameraControl), "InputKeyProc")]
            public static bool InputKeyProc_patch(ref float ___limitFov) {
                ___limitFov = FovLimit.Value;
                return !IsReplacingDefaultFunc();
            }

            // 
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaseCameraControl_Ver2), "InputKeyProc")]
            public static bool InputKeyProc_patchBCC2() {
                return !KKAPI.Maker.MakerAPI.InsideMaker || !IsReplacingDefaultFunc();
            }

            private static bool IsReplacingDefaultFunc() {
                return (KeyRotLeft.Value.Equals(new KeyboardShortcut(KeyCode.Keypad4)) && KeyRotLeft.Value.IsPressed()) ||
                    (KeyRotRight.Value.Equals(new KeyboardShortcut(KeyCode.Keypad6)) && KeyRotRight.Value.IsPressed()) ||
                    (KeyRotUp.Value.Equals(new KeyboardShortcut(KeyCode.Keypad8)) && KeyRotUp.Value.IsPressed()) ||
                    (KeyRotDown.Value.Equals(new KeyboardShortcut(KeyCode.Keypad2)) && KeyRotDown.Value.IsPressed()) ||
                    (KeyZoomIn.Value.Equals(new KeyboardShortcut(KeyCode.KeypadPlus)) && KeyZoomIn.Value.IsPressed()) ||
                    (KeyZoomOut.Value.Equals(new KeyboardShortcut(KeyCode.KeypadMinus)) && KeyZoomOut.Value.IsPressed());
            }
        }
    }
}
