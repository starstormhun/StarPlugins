using KKAPI;
using KKAPI.Chara;
using MessagePack;
using System.Collections;
using ExtensibleSaveFormat;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ADV.Commands.Object;

namespace AAAAAAAAAAAA {
    public class CardDataController : CharaCustomFunctionController {
        // These strings should not be modified unless save versions are increased, and old save versions should be handled when increasing
        internal const int version = 1;
        internal const string SaveID = "AAAAAAAAAAAA_Data";
        internal const string cardDataID = "accParents";
        internal const string coordDataID = "accParentsCoord";
        internal const string aaapkID = "madevil.kk.AAAPK";
        internal const string aaapkKey = "ParentRules";

        // Only for Studio
        public Dictionary<int, Dictionary<int, string>> customAccParents = new Dictionary<int, Dictionary<int, string>>();
        public Dictionary<Transform, Bone> dicTfBones = new Dictionary<Transform, Bone>();
        public Dictionary<string, Bone> dicHashBones = new Dictionary<string, Bone>();
        internal Bone chaRoot = null;
        internal bool loading = false;

        // AAAPK compatibility
        public List<AAAPKParentRule> allParentRules;
        public List<AAAPKParentRule> listAAAPKData = null;

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
            if (listAAAPKData != null && listAAAPKData.Count > 0) data.data.Add(aaapkKey, MessagePackSerializer.Serialize(listAAAPKData));
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
            AddAAAPKData(data, false, true);
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
            if (data != null && data.data != null) {
                if (data.data.TryGetValue(cardDataID, out object cardData)) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Loading card data...");
                    var dicCoord = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<int, string>>>((byte[])cardData);
                    if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerModifiedParents = dicCoord;
                    if (KKAPI.Studio.StudioAPI.InsideStudio) customAccParents = dicCoord;
                    LoadData();
                }
                AddAAAPKData(data);
            }
        }

        internal void AddAAAPKData(PluginData data, bool announce = false, bool loadingCoord = false) {
            if (data == null || data.data == null) return;
            StartCoroutine(DoAddAAAPKData());
            IEnumerator DoAddAAAPKData() {
                for (int i = 0; i < 2; i++) yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                if (announce) AAAAAAAAAAAA.Instance.Log("[AAAAAAAAAAAA] AAAPK data found! Converting...", 5);
                if (data.data.TryGetValue(aaapkKey, out object pluginData) && pluginData != null) {
                    var listParentRules = MessagePackSerializer.Deserialize<List<AAAPKParentRule>>((byte[])pluginData);
                    allParentRules = listParentRules;
                    if (listParentRules == null) yield break;
                    if (KKAPI.Maker.MakerAPI.InsideMaker) {
                        if (AAAAAAAAAAAA.makerBoneRoot == null) AAAAAAAAAAAA.MakeMakerTree();
                        else AAAAAAAAAAAA.UpdateMakerTree(_clearNullTransforms: true, forced: true);
                    }
                    if (KKAPI.Studio.StudioAPI.InsideStudio) AAAAAAAAAAAA.BuildStudioTree(this);
                    var listSaveValues = new List<AAAPKParentRule>();
                    Transform hairsRoot = ChaControl.transform.Find("BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head/p_cf_head_bone/cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty");
                    Transform[] hairRoots = new[] {
                        hairsRoot.Find("ct_hairB"),
                        hairsRoot.Find("ct_hairF"),
                        hairsRoot.Find("ct_hairS"),
                        hairsRoot.Find("ct_hairO_01"),
                    };
                    foreach (var rule in  listParentRules) {
                        if (!loadingCoord && rule.Coordinate != (int)CurrentCoordinate.Value) {
                            listSaveValues.Add(rule);
                            continue;
                        }
                        Bone parentBone = null;
                        string parentName = "";
                        switch (rule.ParentType) {
                            case ParentType.Unknown: {
                                    AAAAAAAAAAAA.Instance.Log("Unknown AAAPK parent type detected!", 2);
                                    AAAAAAAAAAAA.Instance.Log("[AAAAAAAAAAAA] Unknown AAAPK parent type detected!", 5);
                                    break;
                                }
                            case ParentType.Clothing: {
                                    AAAAAAAAAAAA.Instance.Log("AAAPK Clothing parents are not currently supported.", 2);
                                    AAAAAAAAAAAA.Instance.Log("[AAAAAAAAAAAA] AAAPK Clothing parents are not currently supported.", 5);
                                    break;
                                }
                            case ParentType.Accessory: {
                                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Converting accessory parent...");
                                    Bone accBone = null;
                                    if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.TryGetMakerAccBone(rule.ParentSlot, out accBone);
                                    if (KKAPI.Studio.StudioAPI.InsideStudio) AAAAAAAAAAAA.TryGetStudioAccBone(this, rule.ParentSlot, out accBone);
                                    if (accBone == null) { if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"No acc bone for acc #{rule.ParentSlot}..."); continue; }
                                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Acc bone found! ({accBone.bone.name})");
                                    Transform parent = accBone.bone.Find(rule.ParentPath);
                                    if (parent == null) { if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"No parent for {accBone.bone.name}, '{rule.ParentPath}'..."); continue; }
                                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Parent found! ({parent.name})");
                                    if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerTfBones.TryGetValue(parent, out parentBone);
                                    if (KKAPI.Studio.StudioAPI.InsideStudio) dicTfBones.TryGetValue(parent, out parentBone);
                                    parentName = parent.name;
                                    break;
                                }
                            case ParentType.Hair: {
                                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Converting hair parent...");
                                    Transform parent = hairRoots[rule.ParentSlot].Find(rule.ParentPath);
                                    if (parent == null) { if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"No parent for {hairRoots[rule.ParentSlot].name}, '{rule.ParentPath}'..."); continue; }
                                    if (KKAPI.Maker.MakerAPI.InsideMaker) AAAAAAAAAAAA.dicMakerTfBones.TryGetValue(parent, out parentBone);
                                    if (KKAPI.Studio.StudioAPI.InsideStudio) dicTfBones.TryGetValue(parent, out parentBone);
                                    parentName = parent.name;
                                    break;
                                }
                            case ParentType.Character: {
                                    AAAAAAAAAAAA.Instance.Log("AAAPK Character parents are not currently supported.", 2);
                                    AAAAAAAAAAAA.Instance.Log("[AAAAAAAAAAAA] AAAPK Character parents are not currently supported.", 5);
                                    break;
                                }
                        }
                        if (parentBone == null) { if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"No parent bone for \"{parentName}\" found..."); continue; }
                        if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log($"Parent bone found!");
                        if (KKAPI.Maker.MakerAPI.InsideMaker) {
                            if (!AAAAAAAAAAAA.dicMakerModifiedParents.ContainsKey(rule.Coordinate))
                                AAAAAAAAAAAA.dicMakerModifiedParents.Add(rule.Coordinate, new Dictionary<int, string>());
                            AAAAAAAAAAAA.dicMakerModifiedParents[rule.Coordinate][rule.Slot] = parentBone.Hash;
                            if (AAAAAAAAAAAA.IsDebug.Value)
                                AAAAAAAAAAAA.Instance.Log($"Added new parent from AAAPK on coord {rule.Coordinate} for acc #{rule.Slot} with hash {parentBone.Hash}!");
                        }
                        if (KKAPI.Studio.StudioAPI.InsideStudio) {
                            if (!customAccParents.ContainsKey(rule.Coordinate)) customAccParents.Add(rule.Coordinate, new Dictionary<int, string>());
                            customAccParents[rule.Coordinate][rule.Slot] = parentBone.Hash;
                            if (AAAAAAAAAAAA.IsDebug.Value)
                                AAAAAAAAAAAA.Instance.Log($"Added new parent from AAAPK on coord {rule.Coordinate} for acc #{rule.Slot} with hash {parentBone.Hash}!");
                        }
                    }
                    if (listSaveValues.Count > 0) listAAAPKData = listSaveValues;
                    LoadData();
                }
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

        internal class A12AAAPKLoader : CharaCustomFunctionController {
            protected override void OnCardBeingSaved(GameMode currentGameMode) {
            }

            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState) {
                if (maintainState) return;
                gameObject.GetComponent<CardDataController>().AddAAAPKData(GetCoordinateExtendedData(coordinate), true, true);
            }

            protected override void OnReload(GameMode currentGameMode, bool maintainState) {
                if (maintainState) return;
                gameObject.GetComponent<CardDataController>().AddAAAPKData(GetExtendedData(), true);
            }
        }
    }
}
