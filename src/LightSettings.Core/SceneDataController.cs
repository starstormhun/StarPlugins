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
        internal const string SaveID = "LightSettingsData";
        internal const int chaLightID = -10;
        internal const int mapLightID = -20;

        internal static LightSaveData charaLightData;
        internal static List<LightSaveData> itemLightDatas;
        internal static List<OCIItem> listDisabledLights = new List<OCIItem>();
        internal static Dictionary<Light, OCIItem> dicDisabledLights = new Dictionary<Light, OCIItem>();

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
            Light charaLight = Singleton<Studio.Studio>.Instance.gameObject.GetComponentInChildren<Light>(true);
            UIHandler.SyncGUI(UIHandler.containerChara, charaLight);

            var data = GetExtendedData();
            if (data == null)
                return;

            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Loading saved data...");
            if (data.data.TryGetValue(SaveID + "_cookies", out var savedCookieDict)) {
                LightSettings.cookieDict = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>((byte[])savedCookieDict);
                LightSettings.cookieSpotDict.Clear();
                LightSettings.cookieDirectionalDict.Clear();
                LightSettings.cookiePointDict.Clear();
            }
            if ((operation == SceneOperationKind.Load || operation == SceneOperationKind.Import) && data.data.TryGetValue(SaveID + "_lights", out var saveDataBytes)) {
                var saveData = MessagePackSerializer.Deserialize<List<LightSaveData>>((byte[])saveDataBytes);
                foreach (var lightData in saveData) {
                    if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"----- Setting loaded data for item {lightData.ObjectId}... -----");
                    if (loadedItems.TryGetValue(lightData.ObjectId, out var oci)) {
                        if (oci is OCILight ociLight) {
                            SetLoadedData(lightData, new List<Light> { ociLight.light });
                        } else if (oci is OCIItem ociItem) {
                            SetLoadedData(lightData, ociItem.objectItem.GetComponentsInChildren<Light>(true).ToList(), true, true);
                        }
                    } else if (lightData.ObjectId == chaLightID) {
                        charaLightData = lightData;
                        LightSettings.charaLightSetCountDown = 5;
                    } else if (lightData.ObjectId == mapLightID) {
                        var map = GameObject.Find("/Map");
                        if (map) {
                            var lights = map.GetComponentsInChildren<Light>(true).ToList();
                            if (lights.Count > 0) SetLoadedData(lightData, lights, true, true);
                        }
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

                        cookieHash = "",
                        cookieSize = 10,
                    };
                    LightSettings.charaLightSetCountDown = 5;
                }
            }
        }

        protected override void OnSceneSave() {
            var saveData = new List<LightSaveData>();

            // Add chara light
            saveData.Add(charaLightData);

            // Add map light
            var map = GameObject.Find("/Map");
            if (map) {
                var light = map.GetComponentsInChildren<Light>(true)[0];
                if (light) AddSaveData(saveData, mapLightID, light);
            }

            // Add item lights
            foreach (var item in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCILight>()) {
                if (item.light) AddSaveData(saveData, KKAPI.Studio.StudioObjectExtensions.GetSceneId(item), item.light);
            }

            // Add lights attached to items
            saveData.AddRange(itemLightDatas);

            // Filter out unused textures
            LightSettings.FilterCookies(saveData.Select((x) => x.cookieHash).ToList());

            // Save
            if (saveData.Count > 0) {
                var data = new PluginData();
                data.data.Add(SaveID + "_lights", MessagePackSerializer.Serialize(saveData));
                data.data.Add(SaveID + "_cookies", MessagePackSerializer.Serialize(LightSettings.cookieDict));
                SetExtendedData(data);
            }
        }

        internal static void SetLoadedData(LightSaveData lightData, List<Light> lights, bool setState = false, bool setExtra = false) {
            foreach (var light in lights) {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo($"Loading data for {light.name}!");
                if (setState) {
                    light.enabled = lightData.state;
                }
                light.shadows = lightData.shadows;
                light.shadowResolution = lightData.shadowResolution;
                light.shadowStrength = lightData.shadowStrength;
                light.shadowBias = lightData.shadowBias;
                light.shadowNormalBias = lightData.shadowNormalBias;
                light.shadowNearPlane = lightData.shadowNearPlane;
                light.renderMode = lightData.renderMode;
                light.cullingMask = lightData.cullingMask;

                // Cookie
                if (LightSettings.cookieDict.TryGetValue(lightData.cookieHash, out byte[] data)) {
                    light.cookie = LightSettings.LightCookieFromBytes(data, light);
                    light.cookieSize = lightData.cookieSize;
                } else {
                    light.cookie = null;
                    light.cookieSize = 0;
                }

                // Exclusive to lights attached to items
                if (setExtra) {
                    light.color = lightData.color;
                    light.intensity = lightData.intensity;
                    light.range = lightData.range;
                    light.spotAngle = lightData.spotAngle;
                }
            }
        }

        internal static void AddSaveData(List<LightSaveData> saveData, int key, Light light) {
            var hash = "";
            if (light.cookie != null) {
                if (light.type == LightType.Point) {
                    foreach (KeyValuePair<string, Cubemap> kvp in LightSettings.cookiePointDict) {
                        if (light.cookie == kvp.Value) {
                            hash = kvp.Key;
                            break;
                        }
                    }
                } else if (light.type == LightType.Directional) {
                    foreach (KeyValuePair<string, Texture> kvp in LightSettings.cookieDirectionalDict) {
                        if (light.cookie == kvp.Value) {
                            hash = kvp.Key;
                            break;
                        }
                    }
                } else if (light.type == LightType.Spot) {
                    foreach (KeyValuePair<string, Texture> kvp in LightSettings.cookieSpotDict) {
                        if (light.cookie == kvp.Value) {
                            hash = kvp.Key;
                            break;
                        }
                    }
                }
            }

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

                // Cookie
                cookieHash = hash,
                cookieSize = light.cookieSize,

                // Exclusive to lights attached to items
                color = light.color,
                intensity = light.intensity,
                range = light.range,
                spotAngle = light.spotAngle,
            });
        }
    }
}
