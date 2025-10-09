using HarmonyLib;
using Studio;
using UnityEngine;

namespace AxisUnlocker.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        private static class Hooks {
            internal static void SetupHooks() {
                Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
            }

            //////////////////////////////////////////////////////////
            #region Axis size edits
            // Disable the function, then implement my own version
            [HarmonyPrefix]
            [HarmonyPatch(typeof(OptionCtrl), "OnValueChangedSize")]
            private static bool OptionCtrlBeforeOnValueChangedSize(OptionCtrl __instance, float _value) {
                if (AxisUnlocker.UseLogSize.Value) {
                    Studio.Studio.optionSystem.manipulateSize = _value.FromdB();
                } else {
                    _value = _value <= 0 ? 1 : _value;
                    Studio.Studio.optionSystem.manipulateSize = _value;
                }

                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("OptionCtrlBeforeOnValueChangedSize setting manipulate size to: " + Studio.Studio.optionSystem.manipulateSize.ToString());

                __instance._inputSize.value = _value;
                Singleton<GuideObjectManager>.Instance.SetScale();

                // Disables original
                return false;
            }

            // Also have to edit the OnEndEditSize function
            [HarmonyPrefix]
            [HarmonyPatch(typeof(OptionCtrl), "OnEndEditSize")]
            private static bool OptionCtrlBeforeOnEndEditSize(OptionCtrl __instance, string _text) {
                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("Received text input: " + _text);

                float value = Utility.StringToFloat(_text);
                value = Mathf.Max(AxisUnlocker.NewMinSize.Value, value);
                if (AxisUnlocker.UseLogSize.Value) value = value.TodB();
                value = Mathf.Clamp(value, __instance._inputSize.min, __instance._inputSize.max);
                OptionCtrlBeforeOnValueChangedSize(__instance, value);

                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("^ This instance was called from OptionCtrlBeforeOnEndEditSize ^");

                // Disables original
                return false;
            }

            // And add the logarithmic logic to the update as well
            [HarmonyPrefix]
            [HarmonyPatch(typeof(OptionCtrl), "UpdateUIManipulateSize")]
            private static bool OptionCtrlAfterUpdateUIManipulateSize(OptionCtrl __instance) {
                __instance._inputSize.value = AxisUnlocker.UseLogSize.Value ? Studio.Studio.optionSystem.manipulateSize.TodB() : Studio.Studio.optionSystem.manipulateSize;

                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("OptionCtrlAfterUpdateUIManipulateSize setting display to: " + __instance._inputSize.value.ToString());

                // Disables original
                return false;
            }

            // Disable and edit the B+Right click changer
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StudioScene), "ChangeScale")]
            private static bool StudioSceneBeforeChangeScale(StudioScene __instance) {
                float num = Input.GetAxis("Mouse X") * 0.1f;
                OptionCtrl.InputCombination inputSize = __instance.optionCtrl.inputSize;
                float value = AxisUnlocker.UseLogSize.Value ? Studio.Studio.optionSystem.manipulateSize.TodB() : Studio.Studio.optionSystem.manipulateSize;
                value = Mathf.Clamp(value + num, inputSize.min, inputSize.max);
                Studio.Studio.optionSystem.manipulateSize = AxisUnlocker.UseLogSize.Value ? value.FromdB() : value;

                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("StudioSceneBeforeChangeScale setting manipulate size to: " + Studio.Studio.optionSystem.manipulateSize.ToString());

                Singleton<GuideObjectManager>.Instance.SetScale();
                __instance.optionCtrl.UpdateUIManipulateSize();

                // Disables original
                return false;
            }
            #endregion

            //////////////////////////////////////////////////////////
            #region Axis speed edits
            // Disable the function, then implement my own version
            [HarmonyPrefix]
            [HarmonyPatch(typeof(OptionCtrl), "OnValueChangedSpeed")]
            private static bool OptionCtrlBeforeOnValueChangedSpeed(OptionCtrl __instance, float _value) {
                if (AxisUnlocker.UseLogSize.Value) {
                    Studio.Studio.optionSystem.manipuleteSpeed = _value.FromdB();
                } else {
                    _value = _value <= 0 ? 1 : _value;
                    Studio.Studio.optionSystem.manipuleteSpeed = _value;
                }
                __instance.inputSpeed.value = _value;

                // Disables original
                return false;
            }

            // Also have to edit the OnEndEditSpeed function
            [HarmonyPrefix]
            [HarmonyPatch(typeof(OptionCtrl), "OnEndEditSpeed")]
            private static bool OptionCtrlBeforeOnEndEditSpeed(OptionCtrl __instance, string _text) {

                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("Received text input: " + _text);

                float value = Utility.StringToFloat(_text);
                value = Mathf.Max(AxisUnlocker.NewMinMove.Value, value);
                if (AxisUnlocker.UseLogMove.Value) value = value.TodB();
                value = Mathf.Clamp(value, __instance.inputSpeed.min, __instance.inputSpeed.max);
                OptionCtrlBeforeOnValueChangedSpeed(__instance, value);

                if (AxisUnlocker.IsDebug.Value) AxisUnlocker.Log("^ This instance was called from OptionCtrlBeforeOnEndEditSize ^");

                // Disables original
                return false;
            }
            #endregion

            // If the input uses the new classes, we update the display accordingly
            [HarmonyPrefix]
            [HarmonyPatch(typeof(OptionCtrl.InputCombination), "value", MethodType.Setter)]
            private static bool OptionCtrlICValueValueSet(OptionCtrl.InputCombination __instance, ref float value) {
                if (__instance is ICLogValBase) {
                    (__instance as ICLogValBase).value = value;
                    return false;
                } else {
                    if (value == 0f) value = 1f;
                    return true;
                }
            }

            // Replace the two classes in question with identifying classes
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SystemButtonCtrl), "Init")]
            private static void SystemButtonCtrlAfterInit(SystemButtonCtrl __instance) {
                OptionCtrl optCtrl = __instance.gameObject.GetComponentInChildren<OptionCtrl>();

                ICLogValSize newInputSize = new ICLogValSize {
                    input = optCtrl._inputSize.input,
                    slider = optCtrl._inputSize.slider
                };
                optCtrl._inputSize = newInputSize;

                ICLogValSpeed newInputSpeed = new ICLogValSpeed {
                    input = optCtrl.inputSpeed.input,
                    slider = optCtrl.inputSpeed.slider
                };
                optCtrl.inputSpeed = newInputSpeed;
                
                AxisUnlocker.UpdateSliders();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(OptionCtrl), "Start")]
            private static void OptionCtrlAfterStart() {
                AxisUnlocker.UpdateSliders(AxisUnlocker.UseLogSize.Value, AxisUnlocker.UseLogMove.Value);
            }
        }
    }
}
