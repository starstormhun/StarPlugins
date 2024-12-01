using HarmonyLib;
using UnityEngine;
using Studio;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;

namespace LightSettings.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            AlwaysHooks.SetupHooks();
            ConditionalHooks.SetupHooks();
        }

        internal static void Deactivate() {
            AlwaysHooks.UnregisterHooks();
            ConditionalHooks.UnregisterHooks();
        }

        private static class AlwaysHooks {
            private static Harmony _harmony;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(AlwaysHooks), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // UI synchronisation on item selection
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TreeNodeCtrl), "SelectSingle")]
            private static void AfterTreeNodeCtrlSelectSingle(TreeNodeObject _node, bool _deselect = true) {
                if (_deselect && (_node != null) && Studio.Studio.Instance.dicInfo.TryGetValue(_node, out ObjectCtrlInfo _info)) {
                    if (_info is OCILight _ociLight) {
                        switch (_ociLight.lightType) {
                            case LightType.Directional:
                                UIHandler.containerLight.localPosition = new Vector2(0, 0);
                                break;
                            case LightType.Point:
                                UIHandler.containerLight.localPosition = new Vector2(0, -50f);
                                break;
                            case LightType.Spot:
                                UIHandler.containerLight.localPosition = new Vector2(0, -90f);
                                break;
                        }
                        UIHandler.SyncGUI(UIHandler.containerLight, _ociLight.light);
                    } else if (_info is OCIItem _ociItem) {
                        var lights = LightSettings.GetOwnLights(_ociItem);
                        UIHandler.TogglePanelToggler(lights.Count > 0);
                        if (lights.Count > 0) {
                            UIHandler.SyncGUI(UIHandler.containerItem, lights[0], true);
                        }
                    }
                }
            }

            // UI synchronisation on map selection
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RootButtonCtrl), "OnClick")]
            private static void AfterRootButtonCtrlOnClick(int _kind) {
                if (_kind == 0) MapLightChecker();
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), "SetupMap")]
            [HarmonyPatch(typeof(MapCtrl), "OnClickSunLightType")]
            private static void AfterMapListOnClick() {
                MapLightChecker();
            }
            private static void MapLightChecker() {
                var map = Singleton<Map>.Instance.mapRoot;
                if (map) {
                    var lights = map.GetComponentsInChildren<Light>(true).ToList();
                    if (lights.Count > 0) {
                        UIHandler.SetMapGUI(true);
                        UIHandler.SyncGUI(UIHandler.containerMap, lights[0], true);
                    } else UIHandler.SetMapGUI(false);
                } else UIHandler.SetMapGUI(false);
            }

            // Save data of lights attached to items
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Studio.Studio), "SaveScene")]
            internal static bool BeforeStudioSaveScene() {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Saving data before save...");
                SceneDataController.itemLightDatas = new List<LightSaveData>();
                foreach (OCIItem ociItem in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIItem>()) {
                    var lights = LightSettings.GetOwnLights(ociItem);
                    if (lights.Count > 0) {
                        if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Saving data for {ociItem.treeNodeObject.textName}!");
                        SceneDataController.AddSaveData(SceneDataController.itemLightDatas, KKAPI.Studio.StudioObjectExtensions.GetSceneId(ociItem), lights[0]);
                    }
                }
                return true;
            }

            // Restore data of lights attached to items
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), "SaveScene")]
            internal static void AfterStudioSaveScene() {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Restoring data after save...");
                foreach (LightSaveData data in SceneDataController.itemLightDatas) {
                    if (Studio.Studio.Instance.dicObjectCtrl.TryGetValue(data.ObjectId, out ObjectCtrlInfo oci)) {
                        if (oci is OCIItem ociItem) {
                            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Restoring data for {ociItem.treeNodeObject.textName}!");
                            var lights = LightSettings.GetOwnLights(ociItem);
                            lights[0].enabled = false;
                            SceneDataController.SetLoadedData(data, lights, true, true);
                        }
                    }
                }
            }

            // Patch light enabled property
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Behaviour), "enabled", MethodType.Setter)]
            private static void AfterLightEnabledSet(Behaviour __instance, bool value) {
                if (__instance is Light light && value) {
                    if (SceneDataController.dicDisabledLights.ContainsKey(light)) light.enabled = false;
                }
            }

            // Bunches of patches to unlock vanilla light intensity controls
            private static float editedIntensity = 0;
            private const float newLimit = 50f;

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(MPLightCtrl), "OnValueChangeIntensity")]
            [HarmonyPatch(typeof(MPLightCtrl), "OnEndEditIntensity")]
            [HarmonyPatch(typeof(CameraLightCtrl.LightCalc), "OnEndEditIntensity")]
            private static IEnumerable<CodeInstruction> UnlockIntensitiesTranspiler(IEnumerable<CodeInstruction> instructions) {
                foreach (var instruction in instructions) {
                    if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 2f) {
                        instruction.operand = newLimit;
                    }
                    yield return instruction;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MPLightCtrl), "OnEndEditIntensity")]
            [HarmonyPatch(typeof(CameraLightCtrl.LightCalc), "OnEndEditIntensity")]
            private static bool IntensityUnlockEditPrefix(object __instance, ref string _text) {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Edited intensity _value: {_text}");
                float.TryParse(_text, out editedIntensity);
                _text = Mathf.Min(newLimit, editedIntensity).ToString("0.00");
                if (__instance is MPLightCtrl mpl) mpl.viIntensity.inputField.text = _text;
                if (__instance is CameraLightCtrl.LightCalc lc) lc.inputIntensity.text = _text;
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MPLightCtrl), "OnValueChangeIntensity")]
            [HarmonyPatch(typeof(CameraLightCtrl.LightCalc), "OnValueChangeIntensity")]
            private static bool IntensityUnlockValueChangePrefix(ref float _value) {
                if (editedIntensity > 2 && _value == 2) return false;
                editedIntensity = _value;
                return true;
            }

            [HarmonyPostfix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(AddObjectLight), "Load", new Type[] { typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            private static void AfterAddObjectLightLoad(OCILight __result) {
                Light light = __result?.objectLight.GetComponentInChildren<Light>(true);
                if (light == null) return;
                LightSettings.SetMaxShadowRes(light);
            }
            [HarmonyPostfix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(AddObjectItem), "Load", new Type[] { typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
            private static void AfterAddObjectItemLoad(OCIItem __result) {
                Light light = __result.objectItem.GetComponentInChildren<Light>(true);
                if (light == null) return;
                LightSettings.SetMaxShadowRes(light);
            }
        }

        private static class ConditionalHooks {
            private static Harmony _harmony;

            // Enable conditional patches
            public static void SetupHooks() {
                var plugins = LightSettings.Instance.gameObject.GetComponents<BaseUnityPlugin>();
                foreach (var plugin in plugins) {
                    if (plugin.GetType().ToString() == "KK_Plugins.Autosave") {
                        _harmony = Harmony.CreateAndPatchAll(typeof(ConditionalHooks), null);
                        break;
                    }
                }
            }

            // Disable conditional patches
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            // Patch AutoSave to save/restore data correctly
            [HarmonyPrefix]
            [HarmonyPatch(typeof(KK_Plugins.Autosave), "SetText")]
            private static bool BeforeAutoSaveSetText(string text) {
                if (text.ToLower().Contains("saving.")) AlwaysHooks.BeforeStudioSaveScene();
                return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(KK_Plugins.Autosave), "SetText")]
            private static void AfterAutoSaveSetText(string text) {
                if (text.ToLower().Contains("saved")) AlwaysHooks.AfterStudioSaveScene();
            }
        }
    }
}
