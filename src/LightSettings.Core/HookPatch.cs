using HarmonyLib;
using UnityEngine;
using Studio;
using System.Linq;

namespace LightSettings.Koikatu {
    public static class HookPatch {
        internal static void Init() {
            HookPatch.Hooks.SetupHooks();
        }

        internal static void Deactivate() {
            HookPatch.Hooks.UnregisterHooks();
        }

        private static class Hooks {
            private static Harmony _harmony;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(HookPatch.Hooks), null);
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
                        UIHandler.SyncGUI(ref UIHandler.containerLight, _ociLight.light);
                    } else if (_info is OCIItem _ociItem) {
                        var lights = _ociItem.objectItem.GetComponentsInChildren<Light>(true).ToList();
                        UIHandler.TogglePanelToggler(lights.Count > 0);
                        if (lights.Count > 0) {
                            UIHandler.SyncGUI(ref UIHandler.containerItem, lights[0], true);
                        }
                    }
                }
            }

            // Save data of lights attached to items
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Studio.Studio), "SaveScene")]
            private static bool BeforeStudioSaveScene() {
                LightSettings.logger.LogInfo("Saving data");
                SceneDataController.itemLightDatas = new System.Collections.Generic.List<LightSaveData>();
                foreach (OCIItem ociItem in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIItem>()) {
                    var lights = ociItem.objectItem.GetComponentsInChildren<Light>(true).ToList();
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
            private static void AfterStudioSaveScene() {
                LightSettings.logger.LogInfo("Restoring data");
                foreach (LightSaveData data in SceneDataController.itemLightDatas) {
                    if (Studio.Studio.Instance.dicObjectCtrl.TryGetValue(data.ObjectId, out ObjectCtrlInfo oci)) {
                        if (oci is OCIItem ociItem) {
                            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Restoring data for {ociItem.treeNodeObject.textName}!");
                            var lights = ociItem.objectItem.GetComponentsInChildren<Light>(true).ToList();
                            lights[0].enabled = false;
                            SceneDataController.SetLoadedData(data, lights, true, true);
                        }
                    }
                }
            }

            // Patch AutoSave to save/restore data correctly
            [HarmonyPrefix]
            [HarmonyPatch(typeof(KK_Plugins.Autosave), "SetText")]
            private static bool BeforeAutoSaveSetText(string text) {
                if (text.ToLower().Contains("saving.")) BeforeStudioSaveScene();
                return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(KK_Plugins.Autosave), "SetText")]
            private static void AfterAutoSaveSetText(string text) {
                if (text.ToLower().Contains("saved")) AfterStudioSaveScene();
            }

            // Patch light enabled property
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Behaviour), "enabled", MethodType.Setter)]
            private static void AfterLightEnabledSet(Behaviour __instance, bool value) {
                if (__instance is Light light && value) {
                    if (SceneDataController.dicDisabledLights.ContainsKey(light)) light.enabled = false;
                }
            }
        }
    }
}
