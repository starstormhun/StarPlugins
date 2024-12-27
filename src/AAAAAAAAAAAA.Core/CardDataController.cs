using KKAPI;
using KKAPI.Chara;
using MessagePack;
using System.Collections;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using UnityEngine;

namespace AAAAAAAAAAAA {
    public class CardDataController : CharaCustomFunctionController {
        // These strings should not be modified unless save versions are increased, and old save versions should be handled when increasing
        internal const int version = 1;
        internal const string SaveID = "AAAAAAAAAAAA_Data";
        internal const string cardDataID = "accParents";
        internal const string coordDataID = "accParentsCoord";

        // Only for Studio
        public Dictionary<int, Dictionary<int, string>> customAccParents = new Dictionary<int, Dictionary<int, string>>();
        public Dictionary<Transform, Bone> dicTfBones = new Dictionary<Transform, Bone>();
        public Dictionary<string, Bone> dicHashBones = new Dictionary<string, Bone>();
        internal Bone chaRoot = null;
        internal bool loading = false;

        // Only for Studio
        protected override void OnDestroy() {
            if (KKAPI.Studio.StudioAPI.InsideStudio) {
                chaRoot.Destroy();
                customAccParents.Clear();
                dicTfBones.Clear();
                dicHashBones.Clear();
            }
            base.OnDestroy();
        }

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
            if (maintainState) {
                if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.UpdateMakerTree(true);
                if (KKAPI.Studio.StudioAPI.InsideStudio) LoadData();
                return;
            }
            if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerModifiedParents[(int)CurrentCoordinate.Value] = new Dictionary<int, string>();
            if (KKAPI.Studio.StudioAPI.InsideStudio) customAccParents[(int)CurrentCoordinate.Value] = new Dictionary<int, string>();
            PluginData data = GetCoordinateExtendedData(coordinate);
            if (data == null || data.data == null) return;
            if (data.data.TryGetValue(coordDataID, out var accData)) {
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Loading custom data into coord#{(int)CurrentCoordinate.Value}...");
                var dicCoord = MessagePackSerializer.Deserialize<Dictionary<int, string>>((byte[])accData);
                if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerModifiedParents[(int)CurrentCoordinate.Value] = dicCoord;
                if (KKAPI.Studio.StudioAPI.InsideStudio) customAccParents[(int)CurrentCoordinate.Value] = dicCoord;
                LoadData();
            }
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState) {
            if (maintainState) {
                if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.UpdateMakerTree(true);
                if (KKAPI.Studio.StudioAPI.InsideStudio) LoadData();
                return;
            }
            if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerModifiedParents = new Dictionary<int, Dictionary<int, string>>();
            if (KKAPI.Studio.StudioAPI.InsideStudio) customAccParents = new Dictionary<int, Dictionary<int, string>>();
            PluginData data = GetExtendedData();
            if (data == null || data.data == null) return;
            if (data.data.TryGetValue(cardDataID, out object cardData)) {
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Loading card data...");
                var dicCoord = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, string>>>((byte[])cardData);
                if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerModifiedParents = dicCoord;
                if (KKAPI.Studio.StudioAPI.InsideStudio) customAccParents = dicCoord;
                LoadData();
            }
        }

        internal void LoadData() {
            if (loading) return;
            loading = true;
            AAAAAAAAAAAA.Instance.StartCoroutine(LoadDataCoroutine());
            IEnumerator LoadDataCoroutine() {
                for (int i = 0; i < 6; i++) yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                if (KKAPI.Maker.MakerAPI.InsideMaker) {
                    AAAAAAAAAAAA.MakeMakerTree();
                    AAAAAAAAAAAA.UpdateMakerTree(true);
                }
                if (KKAPI.Studio.StudioAPI.InsideStudio) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Loading data for {ChaFileControl.parameter.fullname}...");
                    chaRoot = AAAAAAAAAAAA.ApplyStudioData(this);
                }
                loading = false;
            }
        }
    }
}
