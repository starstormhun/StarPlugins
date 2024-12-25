using KKAPI;
using KKAPI.Chara;
using MessagePack;
using System.Collections;
using ExtensibleSaveFormat;
using System.Collections.Generic;

namespace AAAAAAAAAAAA {
    internal class CardDataController : CharaCustomFunctionController {
        // These strings should not be modified unless save versions are increased, and old save versions should be handled when increasing
        internal const int version = 1;
        internal const string SaveID = "AAAAAAAAAAAA_Data";
        internal const string cardDataID = "accParents";
        internal const string coordDataID = "accParentsCoord";

        // Only possible in Maker
        protected override void OnCardBeingSaved(GameMode currentGameMode) {
            if (AAAAAAAAAAAA.dicMakerModifiedParents.Count == 0) return;
            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Saving custom card data...");
            var data = new PluginData { version = version };
            data.data.Add(cardDataID, MessagePackSerializer.Serialize(AAAAAAAAAAAA.dicMakerModifiedParents));
            SetExtendedData(data);
        }

        // Only possible in Maker
        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate) {
            if (!AAAAAAAAAAAA.dicMakerModifiedParents.TryGetValue((int)CurrentCoordinate.Value, out var dicCoord) || dicCoord.Count == 0) return;
            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Saving custom data to coordinate file from coord#{(int)CurrentCoordinate.Value}...");
            var data = new PluginData { version = version };
            data.data.Add(coordDataID, MessagePackSerializer.Serialize(dicCoord));
            SetCoordinateExtendedData(coordinate, data);
        }

        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) {
            if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (maintainState) {
                    AAAAAAAAAAAA.UpdateMakerTree(true);
                    return;
                }
                PluginData data = GetCoordinateExtendedData(coordinate);
                if (data == null || data.data == null) return;
                if (data.data.TryGetValue(coordDataID, out var accData)) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Loading custom data into coord#{(int)CurrentCoordinate.Value}...");
                    var dicCoord = MessagePackSerializer.Deserialize<Dictionary<int, string>>((byte[])accData);
                    AAAAAAAAAAAA.dicMakerModifiedParents[(int)CurrentCoordinate.Value] = dicCoord;
                    LoadData();
                }
            }
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState) {
            if (KKAPI.Maker.MakerAPI.InsideMaker) {
                if (maintainState) {
                    AAAAAAAAAAAA.UpdateMakerTree(true);
                    return;
                }
                PluginData data = GetExtendedData();
                if (data == null || data.data == null) return;
                if (data.data.TryGetValue(cardDataID, out object cardData)) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Loading card data...");
                    var dicCoord = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, string>>>((byte[])cardData);
                    AAAAAAAAAAAA.dicMakerModifiedParents = dicCoord;
                    LoadData();
                }
            }
        }

        private void LoadData() {
            AAAAAAAAAAAA.Instance.StartCoroutine(LoadDataCoroutine());
            IEnumerator LoadDataCoroutine() {
                for (int i = 0; i < 3; i++) yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    AAAAAAAAAAAA.MakeMakerTree();
                    AAAAAAAAAAAA.UpdateMakerTree(true);
                }
            }
        }
    }
}
