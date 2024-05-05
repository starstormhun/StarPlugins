using ADV;
using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace LightSettings.Koikatu {
    internal class SceneDataController : SceneCustomFunctionController {
        // These CANNOT be changed without breaking existing saved files
        internal const string SaveID = "SavedLightSettings";
        internal const int chaLightID = -10;

        internal static LightSaveData charaLightData;
        internal static List<LightSaveData> itemLightDatas;
        internal static List<OCIItem> listDisabledLights = new List<OCIItem>();
        internal static Dictionary<Light, OCIItem> dicDisabledLights = new Dictionary<Light, OCIItem>();

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
            Light charaLight = Singleton<Studio.Studio>.Instance.gameObject.GetComponentInChildren<Light>(true);
            UIHandler.SyncGUI(ref UIHandler.containerChara, charaLight);

            var data = GetExtendedData();
            if (data == null)
                return;

            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Loading saved data...");
            if ((operation == SceneOperationKind.Load || operation == SceneOperationKind.Import) && data.data.TryGetValue(SaveID, out var saveDataBytes)) {
                var saveData = MessagePackSerializer.Deserialize<List<LightSaveData>>((byte[])saveDataBytes);
                foreach (var lightData in saveData) {
                    if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"----- Setting loaded data for item {lightData.ObjectId}... -----");
                    if (loadedItems.TryGetValue(lightData.ObjectId, out var oci)) {
                        if (oci is OCILight ociLight) {
                            SetLoadedData(lightData, new List<Light> { ociLight.light });
                        } else if (oci is OCIItem ociItem) {
                            SetLoadedData(lightData, ociItem.objectItem.GetComponentsInChildren<Light>(true).ToList(), true, true);
                        }
                    } else if (lightData.ObjectId == -10) {
                        charaLightData = lightData;
                        LightSettings.charaLightSetCountDown = 5;
                    }
                }
                if (LightSettings.charaLightSetCountDown < 0) {
                    charaLightData = new LightSaveData {
                        ObjectId = chaLightID,

                        state = charaLight.enabled,
                        shadows = LightShadows.Soft,
                        shadowResolution = LightShadowResolution.FromQualitySettings,
                        shadowStrength = 0.8f,
                        shadowBias = 0.0075f,
                        shadowNormalBias = 0.4f,
                        shadowNearPlane = 0.1f,
                        renderMode = LightRenderMode.Auto,
                        cullingMask = 1<<10 + 23,
                    };
                    LightSettings.charaLightSetCountDown = 5;
                }
            }
        }

        protected override void OnSceneSave() {
            var saveData = new List<LightSaveData>();

            // Add chara light
            saveData.Add(charaLightData);

            // Add item lights
            foreach (var item in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCILight>()) {
                if (item.light) AddSaveData(saveData, KKAPI.Studio.StudioObjectExtensions.GetSceneId(item), item.light);
            }

            // Add lights attached to items
            saveData.AddRange(itemLightDatas);
            /* foreach (var item in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIItem>()) {
                var light = item.objectItem.GetComponentInChildren<Light>();
                if (light != null) AddSaveData(saveData, KKAPI.Studio.StudioObjectExtensions.GetSceneId(item), light);
            } */

            // Save
            if (saveData.Count > 0) {
                var data = new PluginData();
                data.data.Add(SaveID, MessagePackSerializer.Serialize(saveData));
                SetExtendedData(data);
            }
        }

        internal static void SetLoadedData(LightSaveData lightData, List<Light> lights, bool setState = false, bool setColIntRange = false) {
            foreach (var light in lights) {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Loading data for {light.name}!");
                if (setState) {
                    if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Light {(lightData.state ? "enabled" : "disabled")}!");
                    light.enabled = lightData.state;
                }
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Shadow type set to {lightData.shadows}");
                light.shadows = lightData.shadows;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Shadow resolution set to {lightData.shadowResolution}");
                light.shadowResolution = lightData.shadowResolution;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Shadow strength set to {lightData.shadowStrength}");
                light.shadowStrength = lightData.shadowStrength;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Shadow bias set to {lightData.shadowBias}");
                light.shadowBias = lightData.shadowBias;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Shadow normal bias set to {lightData.shadowNormalBias}");
                light.shadowNormalBias = lightData.shadowNormalBias;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Shadow near plane set to {lightData.shadowNearPlane}");
                light.shadowNearPlane = lightData.shadowNearPlane;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Light render mode set to {lightData.renderMode}");
                light.renderMode = lightData.renderMode;
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Light culling mask set to {lightData.cullingMask}");
                light.cullingMask = lightData.cullingMask;
                if (setColIntRange) {
                    if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Light color set to {lightData.color}!");
                    light.color = lightData.color;
                    if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Light intensity set to {lightData.intensity}!");
                    light.intensity = lightData.intensity;
                    if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Light range set to {lightData.range}!");
                    light.range = lightData.range;
                }
            }
        }

        internal static void AddSaveData(List<LightSaveData> saveData, int key, Light light) {
            saveData.Add(new LightSaveData {
                ObjectId = key,

                state = light.enabled,
                shadows = light.shadows,
                shadowResolution = light.shadowResolution,
                shadowStrength = light.shadowStrength,
                shadowBias = light.shadowBias,
                shadowNormalBias = light.shadowNormalBias,
                shadowNearPlane = light.shadowNearPlane,
                renderMode = light.renderMode,
                cullingMask = light.cullingMask,

                color = light.color,
                intensity = light.intensity,
                range = light.range,
            });
        }
    }
}
