using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using MessagePack;
using Studio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LightSettings.Koikatu {
    internal class SceneDataController : SceneCustomFunctionController {
        private const string SaveId = "SavedLightSettings";

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
            var data = GetExtendedData();
            if (data == null)
                return;

            if ((operation == SceneOperationKind.Load || operation == SceneOperationKind.Import) && data.data.TryGetValue(SaveId, out var saveDataBytes)) {
                var saveData = MessagePackSerializer.Deserialize<List<LightSaveData>>((byte[])saveDataBytes);
                foreach (var lightData in saveData) {
                    if (loadedItems.TryGetValue(lightData.ObjectId, out var oci) && oci is OCIItem item) {
                        SetLoadedData(lightData, item.objectItem.GetComponentInChildren<Light>(true));
                    } else if (lightData.ObjectId == -1) {
                        SetLoadedData(lightData, Singleton<Studio.Studio>.Instance.gameObject.GetComponentInChildren<Light>(true));
                    }
                }

                void SetLoadedData(LightSaveData lightData, Light light) {
                    light.shadows = lightData.shadows;
                    light.shadowResolution = lightData.shadowResolution;
                    light.shadowStrength = lightData.shadowStrength;
                    light.shadowBias = lightData.shadowBias;
                    light.shadowNormalBias = lightData.shadowNormalBias;
                    light.shadowNearPlane = lightData.shadowNearPlane;
                    light.renderMode = lightData.renderMode;
                    light.cullingMask = lightData.cullingMask;
                }
            }
        }

        protected override void OnSceneSave() {
            var saveData = new List<LightSaveData>();
            // Add chara light
            var charLightData = Singleton<Studio.Studio>.Instance.gameObject.GetComponentInChildren<Light>();
            AddSaveData(-1, charLightData);

            // Add object lights
            foreach (var item in Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIItem>()) {
                var itemData = item.objectItem.GetComponent<Light>();
                if (itemData) AddSaveData(item.objectInfo.dicKey, itemData);
            }

            // Save
            if (saveData.Count > 0) {
                var data = new PluginData();
                data.data.Add(SaveId, MessagePackSerializer.Serialize(saveData));
                SetExtendedData(data);
            }

            void AddSaveData(int key, Light light) {
                saveData.Add(new LightSaveData {
                    ObjectId = key,

                    shadows = light.shadows,
                    shadowResolution = light.shadowResolution,
                    shadowStrength = light.shadowStrength,
                    shadowBias = light.shadowBias,
                    shadowNormalBias = light.shadowNormalBias,
                    shadowNearPlane = light.shadowNearPlane,
                    renderMode = light.renderMode,
                    cullingMask = light.cullingMask
                });
            }
        }
    }
}
