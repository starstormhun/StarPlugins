using MaterialEditorClipboard.JetPack;
using System.Collections.Generic;
using KK_Plugins.MaterialEditor;
using KKAPI.Maker.UI.Sidebar;
using BepInEx.Configuration;
using MaterialEditorAPI;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using BepInEx.Logging;
using KKAPI.Studio;
using KKAPI.Maker;
using MessagePack;
using UnityEngine;
using System.Linq;
using HarmonyLib;
using System.Xml;
using System.IO;
using BepInEx;
using System;
using UniRx;

[assembly: System.Reflection.AssemblyFileVersion(MaterialEditorClipboard.MaterialEditorClipboard.Version)]

namespace MaterialEditorClipboard {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInIncompatibility("madevil.kk.mec")]
    [BepInPlugin(GUID, "Material Editor Clipboard", Version)]
    public partial class MaterialEditorClipboard : BaseUnityPlugin {
        public const string GUID = "starstorm.madevil.meclipboard";
        public const string Version = "1.0.0." + BuildNumber.Version;
        public const int XMLSaveVersion = 1;

        public static ConfigEntry<bool> ConfMonitor { get; private set; }
        public static ConfigEntry<bool> ConfResScale { get; private set; }
        public static ConfigEntry<bool> ConfExportXML { get; private set; }
        public static ConfigEntry<string> ConfExportPath { get; private set; }
        public static ConfigEntry<bool> ConfDebug { get; private set; }

        public static MaterialEditorClipboard Instance { get; private set; }
        public static ManualLogSource LoggerStat { get; private set; }
        public static SidebarToggle SideBarButton { get; private set; }
        public static ToolbarToggle ToolbarButton { get; private set; }
        public static MaterialEditorClipboardUI WindowInstance { get; private set; }

        internal static Harmony harmony;
        internal static List<ClipboardEntry> _listCopyContainer = new List<ClipboardEntry>();
        internal static string exportFolder;

        private void Awake() {
            LoggerStat = Logger;
            Instance = this;
        }

        private void Start() {
            ConfDebug = Config.Bind("Advanced", "Debug", false, new ConfigDescription("Log debug information", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            ConfMonitor = Config.Bind("General", "Monitor clipboard", true, new ConfigDescription("When enabled, the plugin monitors when new data is copied and adds it to the clipboard", null, new KKAPI.Utilities.ConfigurationManagerAttributes { Order = 1 }));
            ConfMonitor.SettingChanged += (x, y) => {
                if (WindowInstance == null) {
                    return;
                }
            };
            ConfResScale = Config.Bind("General", "Config Window Resolution Adjust", false, new ConfigDescription("", null, new KKAPI.Utilities.ConfigurationManagerAttributes { Order = 0 }));
            ConfResScale.SettingChanged += (x, y) => {
                if (WindowInstance == null) {
                    return;
                }
                WindowInstance.SetResScale(ConfResScale.Value);
            };
            ConfExportXML = Config.Bind("General", "Export as XML", false, new ConfigDescription("Export clipboard entries as XML files instead of binary files"));
            string defaultExportPath = Path.Combine(Path.Combine("UserData", "MaterialEditor"), "Clipboard");
            ConfExportPath = Config.Bind("General", "Export Path", defaultExportPath, new ConfigDescription("Path to export the clipboard entries", null, new KKAPI.Utilities.ConfigurationManagerAttributes {
                CustomDrawer = new Action<ConfigEntryBase>(ExportPathDrawer)
            }));
            MakerAPI.RegisterCustomSubCategories += (x, y) => {
                WindowInstance = Instance.gameObject.AddComponent<MaterialEditorClipboardUI>();
                harmony = Harmony.CreateAndPatchAll(typeof(Hooks), null);
                SideBarButton = y.AddSidebarControl(new SidebarToggle("Clipboard", false, Instance));
                SideBarButton.ValueChanged.Subscribe(delegate (bool _value) {
                    if (WindowInstance.enabled != _value) {
                        WindowInstance.enabled = _value;
                    }
                });
            };
            MakerAPI.MakerExiting += delegate (object _sender, EventArgs _args) {
                harmony.UnpatchSelf();
                harmony = null;
                Destroy(WindowInstance);
                SideBarButton = null;
            };
            StudioAPI.StudioLoadedChanged += delegate (object _sender, EventArgs _args) {
                WindowInstance = gameObject.AddComponent<MaterialEditorClipboardUI>();
                Harmony.CreateAndPatchAll(typeof(Hooks), null);
                ToolbarButton = CustomToolbarButtons.AddLeftToolbarToggle(TextureUtils.LoadTexture(ResourceUtils.GetEmbeddedResource("toolbar_icon.png", null), TextureFormat.ARGB32, false), false, delegate (bool _value) {
                    WindowInstance.enabled = _value;
                });
            };
        }

        internal static CopyContainer CopyContainerClone(CopyContainer _src) {
            CopyContainer copyContainer = new CopyContainer();
            foreach (CopyContainer.MaterialShader materialShader in _src.MaterialShaderList) {
                copyContainer.MaterialShaderList.Add(new CopyContainer.MaterialShader(materialShader.ShaderName, materialShader.RenderQueue));
            }
            foreach (CopyContainer.MaterialFloatProperty materialFloatProperty in _src.MaterialFloatPropertyList) {
                copyContainer.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(materialFloatProperty.Property, materialFloatProperty.Value));
            }
            foreach (CopyContainer.MaterialColorProperty materialColorProperty in _src.MaterialColorPropertyList) {
                copyContainer.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(materialColorProperty.Property, materialColorProperty.Value));
            }
            foreach (CopyContainer.MaterialTextureProperty materialTextureProperty in _src.MaterialTexturePropertyList) {
                if (materialTextureProperty.Data != null) {
                    copyContainer.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, materialTextureProperty.Data, materialTextureProperty.Offset, materialTextureProperty.Scale));
                } else {
                    copyContainer.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(materialTextureProperty.Property, null, materialTextureProperty.Offset, materialTextureProperty.Scale));
                }
            }
            foreach (CopyContainer.MaterialKeywordProperty materialKeywordProperty in _src.MaterialKeywordPropertyList) {
                copyContainer.MaterialKeywordPropertyList.Add(new CopyContainer.MaterialKeywordProperty(materialKeywordProperty.Property, materialKeywordProperty.Value));
            }

            return copyContainer;
        }

        private void ExportPathDrawer(ConfigEntryBase configEntry) {
            GUILayout.BeginHorizontal();
            {
                string newPath = GUILayout.TextField(ConfExportPath.Value, GUILayout.Width(220f));
                if (newPath != ConfExportPath.Value) {
                    ConfExportPath.Value = newPath;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse")) {
                    GetExportPath();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void GetExportPath() {
            BrowseForFolder browser = new BrowseForFolder();
            string path = browser.SelectFolder(
                "Select export folder",
                Path.Combine(Path.Combine(Paths.GameRootPath, "UserData"), "MaterialEditor"),
                BrowseForFolder.GetActiveWindow()
            );
            OnExportPathAccept(path);
        }

        private void OnExportPathAccept(string path) {
            if (path.IsNullOrEmpty()) {
                return;
            }
            string newPath;
            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                newPath = path;
            } else {
                newPath = Path.GetDirectoryName(path);
            }
            if (Path.IsPathRooted(newPath) && newPath.StartsWith(Paths.GameRootPath)) {
                newPath = newPath.Replace(Paths.GameRootPath + Path.DirectorySeparatorChar, "");
            }
            ConfExportPath.Value = newPath;
        }

        public class ClipboardEntry {
            public string Label = "";

            public CopyContainer Data = new CopyContainer();
        }

        [MessagePackObject(false)]
        [Serializable]
        public class MsgPackMaterialFloatProperty {
            [Key("Property")]
            public string Property;

            [Key("Value")]
            public float Value;
        }

        [MessagePackObject(false)]
        [Serializable]
        public class MsgPackMaterialColorProperty {
            [Key("Property")]
            public string Property;

            [Key("Value")]
            public Color Value;
        }

        [MessagePackObject(false)]
        [Serializable]
        public class MsgPackMaterialTextureProperty {
            [Key("Property")]
            public string Property;

            [Key("Data")]
            public byte[] Data;

            [Key("Offset")]
            public Vector2? Offset;

            [Key("Scale")]
            public Vector2? Scale;
        }

        [MessagePackObject(false)]
        [Serializable]
        public class MsgPackMaterialShader {
            [Key("ShaderName")]
            public string ShaderName;

            [Key("RenderQueue")]
            public int? RenderQueue;
        }

        [MessagePackObject(false)]
        [Serializable]
        public class MsgPackMaterialKeywordProperty {
            [Key("Property")]
            public string Property;

            [Key("Value")]
            public bool Value;
        }

        [MessagePackObject(false)]
        [Serializable]
        public class MsgPackClipboardEntry {
            public MsgPackClipboardEntry Import(ClipboardEntry x) {
                Label = x.Label;
                List<CopyContainer.MaterialFloatProperty> materialFloatPropertyList = x.Data.MaterialFloatPropertyList;
                if (materialFloatPropertyList != null && materialFloatPropertyList.Count > 0) {
                    foreach (CopyContainer.MaterialFloatProperty materialFloatProperty in x.Data.MaterialFloatPropertyList) {
                        MaterialFloatPropertyList.Add(new MsgPackMaterialFloatProperty {
                            Property = materialFloatProperty.Property,
                            Value = materialFloatProperty.Value
                        });
                    }
                }
                List<CopyContainer.MaterialColorProperty> materialColorPropertyList = x.Data.MaterialColorPropertyList;
                if (materialColorPropertyList != null && materialColorPropertyList.Count > 0) {
                    foreach (CopyContainer.MaterialColorProperty materialColorProperty in x.Data.MaterialColorPropertyList) {
                        MaterialColorPropertyList.Add(new MsgPackMaterialColorProperty {
                            Property = materialColorProperty.Property,
                            Value = materialColorProperty.Value
                        });
                    }
                }
                List<CopyContainer.MaterialTextureProperty> materialTexturePropertyList = x.Data.MaterialTexturePropertyList;
                if (materialTexturePropertyList != null && materialTexturePropertyList.Count > 0) {
                    foreach (CopyContainer.MaterialTextureProperty materialTextureProperty in x.Data.MaterialTexturePropertyList) {
                        MaterialTexturePropertyList.Add(new MsgPackMaterialTextureProperty {
                            Property = materialTextureProperty.Property,
                            Data = materialTextureProperty.Data,
                            Offset = materialTextureProperty.Offset,
                            Scale = materialTextureProperty.Scale
                        });
                    }
                }
                List<CopyContainer.MaterialShader> materialShaderList = x.Data.MaterialShaderList;
                if (materialShaderList != null && materialShaderList.Count > 0) {
                    foreach (CopyContainer.MaterialShader materialShader in x.Data.MaterialShaderList) {
                        MaterialShaderList.Add(new MsgPackMaterialShader {
                            ShaderName = materialShader.ShaderName,
                            RenderQueue = materialShader.RenderQueue
                        });
                    }
                }
                List<CopyContainer.MaterialKeywordProperty> materialKeywordList = x.Data.MaterialKeywordPropertyList;
                if (materialKeywordList != null && materialKeywordList.Count > 0) {
                    foreach (CopyContainer.MaterialKeywordProperty materialKeyword in x.Data.MaterialKeywordPropertyList) {
                        MaterialKeywordPropertyList.Add(new MsgPackMaterialKeywordProperty {
                            Property = materialKeyword.Property,
                            Value = materialKeyword.Value
                        });
                    }
                }
                return this;
            }

            public ClipboardEntry Export() {
                ClipboardEntry clipboardEntry = new ClipboardEntry {
                    Label = Label
                };
                List<MsgPackMaterialFloatProperty> materialFloatPropertyList = MaterialFloatPropertyList;
                if (materialFloatPropertyList != null && materialFloatPropertyList.Count > 0) {
                    clipboardEntry.Data.MaterialFloatPropertyList = new List<CopyContainer.MaterialFloatProperty>();
                    foreach (MsgPackMaterialFloatProperty msgPackMaterialFloatProperty in MaterialFloatPropertyList) {
                        clipboardEntry.Data.MaterialFloatPropertyList.Add(new CopyContainer.MaterialFloatProperty(msgPackMaterialFloatProperty.Property, msgPackMaterialFloatProperty.Value));
                    }
                }
                List<MsgPackMaterialColorProperty> materialColorPropertyList = MaterialColorPropertyList;
                if (materialColorPropertyList != null && materialColorPropertyList.Count > 0) {
                    clipboardEntry.Data.MaterialColorPropertyList = new List<CopyContainer.MaterialColorProperty>();
                    foreach (MsgPackMaterialColorProperty msgPackMaterialColorProperty in MaterialColorPropertyList) {
                        clipboardEntry.Data.MaterialColorPropertyList.Add(new CopyContainer.MaterialColorProperty(msgPackMaterialColorProperty.Property, msgPackMaterialColorProperty.Value));
                    }
                }
                List<MsgPackMaterialTextureProperty> materialTexturePropertyList = MaterialTexturePropertyList;
                if (materialTexturePropertyList != null && materialTexturePropertyList.Count > 0) {
                    clipboardEntry.Data.MaterialTexturePropertyList = new List<CopyContainer.MaterialTextureProperty>();
                    foreach (MsgPackMaterialTextureProperty msgPackMaterialTextureProperty in MaterialTexturePropertyList) {
                        clipboardEntry.Data.MaterialTexturePropertyList.Add(new CopyContainer.MaterialTextureProperty(msgPackMaterialTextureProperty.Property, msgPackMaterialTextureProperty.Data, msgPackMaterialTextureProperty.Offset, msgPackMaterialTextureProperty.Scale));
                    }
                }
                List<MsgPackMaterialShader> materialShaderList = MaterialShaderList;
                if (materialShaderList != null && materialShaderList.Count > 0) {
                    clipboardEntry.Data.MaterialShaderList = new List<CopyContainer.MaterialShader>();
                    foreach (MsgPackMaterialShader msgPackMaterialShader in MaterialShaderList) {
                        clipboardEntry.Data.MaterialShaderList.Add(new CopyContainer.MaterialShader(msgPackMaterialShader.ShaderName, msgPackMaterialShader.RenderQueue));
                    }
                }
                List<MsgPackMaterialKeywordProperty> materialKeywordPropertyList = MaterialKeywordPropertyList;
                if (materialKeywordPropertyList != null && materialKeywordPropertyList.Count > 0) {
                    clipboardEntry.Data.MaterialKeywordPropertyList = new List<CopyContainer.MaterialKeywordProperty>();
                    foreach (MsgPackMaterialKeywordProperty msgPackMaterialKeywordProperty in MaterialKeywordPropertyList) {
                        clipboardEntry.Data.MaterialKeywordPropertyList.Add(new CopyContainer.MaterialKeywordProperty(msgPackMaterialKeywordProperty.Property, msgPackMaterialKeywordProperty.Value));
                    }
                }
                return clipboardEntry;
            }

            [Key("Label")]
            public string Label = "";

            [Key("MaterialFloatPropertyList")]
            public List<MsgPackMaterialFloatProperty> MaterialFloatPropertyList = new List<MsgPackMaterialFloatProperty>();

            [Key("MaterialColorPropertyList")]
            public List<MsgPackMaterialColorProperty> MaterialColorPropertyList = new List<MsgPackMaterialColorProperty>();

            [Key("MaterialTexturePropertyList")]
            public List<MsgPackMaterialTextureProperty> MaterialTexturePropertyList = new List<MsgPackMaterialTextureProperty>();

            [Key("MaterialShaderList")]
            public List<MsgPackMaterialShader> MaterialShaderList = new List<MsgPackMaterialShader>();

            [Key("MaterialKeywordPropertyList")]
            public List<MsgPackMaterialKeywordProperty> MaterialKeywordPropertyList = new List<MsgPackMaterialKeywordProperty>();
        }

        public class MaterialEditorClipboardUI : UI.Template {
            private Vector2 _listScrollPos = Vector2.zero;
            private readonly GUILayoutOption _buttonElem = GUILayout.Width(60f);
            private ClipboardEntry _current;
            private string _renameLabel = "";

            protected override void Awake() {
                _windowSize = new Vector2(400f, 525f);
                _windowInitPos.x = 525f;
                _windowInitPos.y = 80f;
                _cfgResScaleEnable = ConfResScale;
                base.Awake();
            }

            internal void SetResScale(bool _value) {
                _cfgResScaleEnable.Value = _value;
                ChangeRes();
            }

            protected override void CloseWindow() {
                SetNewPos();
                base.CloseWindow();
            }

            protected override void OnDisable() {
                SideBarButton?.SetValue(false, false);
                ToolbarButton?.SetValue(false, false);
                base.OnDisable();
            }

            protected override void DrawDragWindow(int _windowID) {
                _windowTitle = "Material Editor Clipboard";
                base.DrawDragWindow(_windowID);
            }

            protected override void DragWindowContent() {
                GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandHeight(true));
                GUILayout.BeginVertical();
                _listScrollPos = GUILayout.BeginScrollView(_listScrollPos, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
                List<ClipboardEntry> listCopyContainer = _listCopyContainer;
                if (listCopyContainer != null && listCopyContainer.Count > 0) {
                    int num = 0;
                    foreach (ClipboardEntry clipboardEntry in _listCopyContainer.ToList()) {
                        GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
                        if (clipboardEntry == _current) {
                            _renameLabel = GUILayout.TextField(_renameLabel, new[] {GUILayout.Width(120f), GUILayout.ExpandWidth(false)});
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(new GUIContent("Save", "Save entry label"), _buttonElem)) {
                                clipboardEntry.Label = _renameLabel;
                                _current = null;
                                _renameLabel = "";
                            }
                            if (GUILayout.Button(new GUIContent("Back", "Cancel renaming"), _buttonElem)) {
                                _current = null;
                                _renameLabel = "";
                            }
                        } else {
                            string tooltip = string.Format("Shader: {0} | Float: {1} | Color: {2} | Texture: {3} | Keyword: {4}", new object[]
                            {
                                clipboardEntry.Data.MaterialShaderList.Count,
                                clipboardEntry.Data.MaterialFloatPropertyList.Count,
                                clipboardEntry.Data.MaterialColorPropertyList.Count,
                                clipboardEntry.Data.MaterialTexturePropertyList.Count,
                                clipboardEntry.Data.MaterialKeywordPropertyList.Count
                            });
                            if (GUILayout.Button(new GUIContent(clipboardEntry.Label, tooltip), _label)) {
                                _current = clipboardEntry;
                                _renameLabel = clipboardEntry.Label;
                            }
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(new GUIContent("Use", "Use this entry as clipboard content"), _buttonElem)) {
                                MaterialEditorPluginBase.CopyData.ClearAll();
                                MaterialEditorPluginBase.CopyData = CopyContainerClone(clipboardEntry.Data);
                                LoggerStat.LogMessage("[" + clipboardEntry.Label + "] assigned as clipboard content");
                                if (MaterialEditorUI.Visible) {
                                    FindObjectOfType<MaterialEditorUI>().RefreshUI();
                                }
                            }
                            if (GUILayout.Button(new GUIContent("Del", "Delete entry"), _buttonElem)) {
                                clipboardEntry.Data.ClearAll();
                                _listCopyContainer.RemoveAt(num);
                            }
                        }
                        GUILayout.EndHorizontal();
                        num++;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (ConfMonitor.Value != GUILayout.Toggle(ConfMonitor.Value, new GUIContent(" Monitor", "Monitoring clipboard change"))) {
                    ConfMonitor.Value = !ConfMonitor.Value;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Clear", ""), _buttonElem)) {
                    foreach (ClipboardEntry clipboardEntry2 in _listCopyContainer) {
                        clipboardEntry2.Data.ClearAll();
                    }
                    _listCopyContainer.Clear();
                }
                if (GUILayout.Button(new GUIContent("Export", "Export to external binary file"), _buttonElem)) {
                    if (_listCopyContainer.Count == 0) {
                        LoggerStat.LogMessage("Nothing to export");
                        return;
                    }

                    if (Path.IsPathRooted(ConfExportPath.Value)) {
                        exportFolder = ConfExportPath.Value;
                    } else {
                        exportFolder = Path.Combine(Paths.GameRootPath, ConfExportPath.Value);
                    }
                    if (Directory.Exists(exportFolder) || File.Exists(exportFolder)) {
                        var attr = File.GetAttributes(exportFolder);
                        if ((attr & FileAttributes.Directory) != FileAttributes.Directory) {
                            exportFolder = Path.GetDirectoryName(exportFolder);
                        }
                    } else {
                        Directory.CreateDirectory(exportFolder);
                    }

                    string exportFile = Path.Combine(exportFolder, $"MEClipboard-{DateTime.Now:yy-MM-dd-HH-mm-ss}.");
                    if (ConfExportXML.Value) {
                        ExportXML(exportFile + "xml");
                    } else {
                        ExportBinary(exportFile + "bin");
                    }

                    LoggerStat.LogMessage(string.Format("{0} entries exported", _listCopyContainer.Count));
                }
                if (GUILayout.Button(new GUIContent("Import", "Import from external binary file"), _buttonElem)) {
                    GetImportFile();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(GUI.tooltip);
                GUILayout.EndHorizontal();
            }

            private void GetImportFile() {
                string defaultRoute = File.Exists(exportFolder) ? exportFolder : Paths.GameRootPath;
                OpenFileDialog.Show(
                    (path) => { OnImportFileAccept(path); },
                    "Select MEClipboard file",
                    defaultRoute,
                    "MEClipboard file (*.bin;*.xml)|*.bin;*.xml",
                    ".bin"
                );
            }

            private void OnImportFileAccept(string[] path) {
                if (path.IsNullOrEmpty()) {
                    LoggerStat.LogMessage("No file selected!");
                    return;
                }
                if (File.Exists(path[0])) {
                    int countBefore = _listCopyContainer.Count;

                    if (path[0].ToLower().EndsWith("xml")) {
                        ImportXML(path[0]);
                    } else {
                        ImportBinary(path[0]);
                    }
                    
                    LoggerStat.LogMessage(string.Format("{0} entries imported", _listCopyContainer.Count - countBefore));
                } else {
                    LoggerStat.LogMessage("Invalid file selected!");
                    return;
                }
            }

            private void ExportBinary(string path) {
                List<MsgPackClipboardEntry> list = new List<MsgPackClipboardEntry>();
                foreach (ClipboardEntry x in _listCopyContainer) {
                    list.Add(new MsgPackClipboardEntry().Import(x));
                }
                if (Path.IsPathRooted(ConfExportPath.Value)) {
                    exportFolder = ConfExportPath.Value;
                } else {
                    exportFolder = Path.Combine(Paths.GameRootPath, ConfExportPath.Value);
                }
                if (!Directory.Exists(exportFolder)) {
                    Directory.CreateDirectory(exportFolder);
                }
                File.WriteAllBytes(path, MessagePackSerializer.Serialize(list));
            }

            private void ImportBinary(string path) {
                List<MsgPackClipboardEntry> list2;
                try {
                    list2 = MessagePackSerializer.Deserialize<List<MsgPackClipboardEntry>>(File.ReadAllBytes(path));
                } catch {
                    LoggerStat.LogMessage("Non-MEClipboard file selected!");
                    return;
                }
                if (list2 != null && list2.Count == 0) {
                    LoggerStat.LogMessage("Nothing to import!");
                    return;
                }
                foreach (MsgPackClipboardEntry msgPackClipboardEntry in list2) {
                    _listCopyContainer.Add(msgPackClipboardEntry.Export());
                }
            }

            private void ExportXML(string path) {
                XmlWriter w = new XmlTextWriter(path, null);
                w.WriteStartDocument();
                w.WriteStartElement("MEClipboard");
                {
                    w.WriteStartElement("Version");
                    {
                        w.WriteString(XMLSaveVersion.ToString());
                    }
                    w.WriteEndElement();
                    foreach (ClipboardEntry entry in _listCopyContainer) {
                        w.WriteStartElement("Entry");
                        {
                            w.WriteStartElement("Label");
                            {
                                w.WriteString(entry.Label);
                            }
                            w.WriteEndElement();
                            foreach (var shaderProp in entry.Data.MaterialShaderList) {
                                w.WriteStartElement("Shader");
                                {
                                    if (shaderProp.ShaderName != "") {
                                        w.WriteStartElement("Name");
                                        {
                                            w.WriteString(shaderProp.ShaderName);
                                        }
                                        w.WriteEndElement();
                                    }
                                    if (shaderProp.RenderQueue.HasValue) {
                                        w.WriteStartElement("RenderQueue");
                                        {
                                            w.WriteString(shaderProp.RenderQueue.ToString());
                                        }
                                        w.WriteEndElement();
                                    }
                                }
                                w.WriteEndElement();
                            }
                            foreach (var floatProp in entry.Data.MaterialFloatPropertyList) {
                                w.WriteStartElement("Float");
                                {
                                    w.WriteStartElement("Name");
                                    {
                                        w.WriteString(floatProp.Property);
                                    }
                                    w.WriteEndElement();
                                    w.WriteStartElement("Value");
                                    {
                                        w.WriteString(floatProp.Value.ToString("0.00000"));
                                    }
                                    w.WriteEndElement();
                                }
                                w.WriteEndElement();
                            }
                            foreach (var colorProp in entry.Data.MaterialColorPropertyList) {
                                w.WriteStartElement("Color");
                                {
                                    w.WriteStartElement("Name");
                                    {
                                        w.WriteString(colorProp.Property);
                                    }
                                    w.WriteEndElement();
                                    w.WriteStartElement("R");
                                    {
                                        w.WriteString(colorProp.Value.r.ToString("0.000"));
                                    }
                                    w.WriteEndElement();
                                    w.WriteStartElement("G");
                                    {
                                        w.WriteString(colorProp.Value.g.ToString("0.000"));
                                    }
                                    w.WriteEndElement();
                                    w.WriteStartElement("B");
                                    {
                                        w.WriteString(colorProp.Value.b.ToString("0.000"));
                                    }
                                    w.WriteEndElement();
                                    w.WriteStartElement("A");
                                    {
                                        w.WriteString(colorProp.Value.a.ToString("0.000"));
                                    }
                                    w.WriteEndElement();
                                }
                                w.WriteEndElement();
                            }
                            foreach (var textureProp in entry.Data.MaterialTexturePropertyList) {
                                w.WriteStartElement("Texture");
                                {
                                    w.WriteStartElement("Name");
                                    {
                                        w.WriteString(textureProp.Property);
                                    }
                                    w.WriteEndElement();
                                    if (textureProp.Offset.HasValue) {
                                        w.WriteStartElement("OffsetX");
                                        {
                                            w.WriteString(textureProp.Offset.Value.x.ToString("0.00000"));
                                        }
                                        w.WriteEndElement();
                                        w.WriteStartElement("OffsetY");
                                        {
                                            w.WriteString(textureProp.Offset.Value.y.ToString("0.00000"));
                                        }
                                        w.WriteEndElement();
                                    }
                                    if (textureProp.Scale.HasValue) {
                                        w.WriteStartElement("ScaleX");
                                        {
                                            w.WriteString(textureProp.Scale.Value.x.ToString("0.00000"));
                                        }
                                        w.WriteEndElement();
                                        w.WriteStartElement("ScaleY");
                                        {
                                            w.WriteString(textureProp.Scale.Value.y.ToString("0.00000"));
                                        }
                                        w.WriteEndElement();
                                    }
                                    if (!textureProp.Data.IsNullOrEmpty()) {
                                        w.WriteStartElement("Data");
                                        {
                                            w.WriteValue(textureProp.Data);
                                        }
                                        w.WriteEndElement();
                                    }
                                }
                                w.WriteEndElement();
                            }
                            foreach (var keywordProp in entry.Data.MaterialKeywordPropertyList) {
                                w.WriteStartElement("Keyword");
                                {
                                    w.WriteStartElement("Name");
                                    {
                                        w.WriteString(keywordProp.Property);
                                    }
                                    w.WriteEndElement();
                                    w.WriteStartElement("Value");
                                    {
                                        w.WriteString(keywordProp.Value ? "1" : "0");
                                    }
                                    w.WriteEndElement();
                                }
                                w.WriteEndElement();
                            }
                        }
                        w.WriteEndElement();
                    }
                }
                w.WriteEndElement();
                w.WriteEndDocument();
                w.Close();
            }

            private void ImportXML(string path) {
                var reader = new XmlTextReader(path);

                bool foundXML = false;
                bool foundMEC = false;
                bool foundVersion = false;
                int version = 0;

                while (reader.Read()) {
                    if (reader.Name.ToLower() == "xml") {
                        foundXML = true;
                    } else if (reader.Name == "MEClipboard") {
                        foundMEC = true;
                    } else if (reader.Name == "Version") {
                        foundVersion = true;
                        if (!int.TryParse(reader.ReadString(), out version)) {
                            LoggerStat.LogMessage("Invalid Version detected, aborting!");
                            reader.Close();
                            return;
                        }
                    }
                    if (foundXML && foundMEC && foundVersion) {
                        break;
                    }
                }
                if (!foundXML) {
                    LoggerStat.LogMessage("Invalid XML file!");
                    reader.Close();
                    return;
                }
                if (!foundMEC) {
                    LoggerStat.LogMessage("Non-MEC XML file loaded!");
                    reader.Close();
                    return;
                }
                if (!foundVersion) {
                    LoggerStat.LogMessage("Version missing from XML, aborting!");
                    reader.Close();
                    return;
                }

                // Add version logic here if implementing new XML save format
                switch (version) {
                    case 1:
                        int reading = 0; // 0 = Nothing, 1 = Shader, 2 = Float, 3 = Color, 4 = Texture, 5 = Keyword
                        ClipboardEntry constructedEntry = null;
                        CopyContainer.MaterialShader shaderProp = null;
                        CopyContainer.MaterialFloatProperty floatProp = null;
                        CopyContainer.MaterialColorProperty colorProp = null;
                        CopyContainer.MaterialTextureProperty textureProp = null;
                        CopyContainer.MaterialKeywordProperty keywordProp = null;

                        while (reader.Read()) {
                            switch (reading) {
                                case 0: // Between properties
                                    switch (reader.Name) {
                                        case "":
                                            continue;
                                        case "Entry":
                                            if (constructedEntry != null) {
                                                _listCopyContainer.Add(constructedEntry);
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Entry finished! ({constructedEntry.Label})");
                                                constructedEntry = null;
                                            } else {
                                                constructedEntry = new ClipboardEntry {
                                                    Data = new CopyContainer()
                                                };
                                                if (ConfDebug.Value) LoggerStat.LogDebug("Adding new entry!");
                                            }
                                            break;
                                        case "Label":
                                            constructedEntry.Label = reader.ReadString();
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Set label! ({constructedEntry.Label})");
                                            break;
                                        case "Shader":
                                            shaderProp = new CopyContainer.MaterialShader("", null);
                                            reading = 1;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Reading Shader prop!");
                                            break;
                                        case "Float":
                                            floatProp = new CopyContainer.MaterialFloatProperty("", 0);
                                            reading = 2;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Reading Float prop!");
                                            break;
                                        case "Color":
                                            colorProp = new CopyContainer.MaterialColorProperty("", Color.clear);
                                            reading = 3;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Reading Color prop!");
                                            break;
                                        case "Texture":
                                            textureProp = new CopyContainer.MaterialTextureProperty("");
                                            reading = 4;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Reading Texture prop!");
                                            break;
                                        case "Keyword":
                                            keywordProp = new CopyContainer.MaterialKeywordProperty("", false);
                                            reading = 5;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Reading Keyword prop!");
                                            break;
                                    }
                                    break;
                                case 1: // Shader
                                    switch (reader.Name) {
                                        case "Name":
                                            shaderProp.ShaderName = reader.ReadString();
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Got shader name! ({shaderProp.ShaderName})");
                                            break;
                                        case "RenderQueue":
                                            if (int.TryParse(reader.ReadString(), out int rq)) {
                                                shaderProp.RenderQueue = rq;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got render queue! ({shaderProp.RenderQueue})");
                                            }
                                            break;
                                        case "Shader":
                                            reading = 0;
                                            constructedEntry.Data.MaterialShaderList.Add(shaderProp);
                                            shaderProp = null;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Shader done!");
                                            break;
                                        case "":
                                            continue;
                                        default:
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Unexpected tag found while reading Shader: {reader.Name}");
                                            break;
                                    }
                                    break;
                                case 2: // Float
                                    switch (reader.Name) {
                                        case "Name":
                                            floatProp.Property = reader.ReadString();
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Got float name! ({floatProp.Property})");
                                            break;
                                        case "Value":
                                            if (float.TryParse(reader.ReadString(), out float val)) {
                                                floatProp.Value = val;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got float value! ({floatProp.Value})");
                                            }
                                            break;
                                        case "Float":
                                            reading = 0;
                                            constructedEntry.Data.MaterialFloatPropertyList.Add(floatProp);
                                            floatProp = null;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Float done!");
                                            break;
                                        case "":
                                            continue;
                                        default:
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Unexpected tag found while reading Float: {reader.Name}");
                                            break;
                                    }
                                    break;
                                case 3: // Color
                                    switch (reader.Name) {
                                        case "Name":
                                            colorProp.Property = reader.ReadString();
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Got color name! ({colorProp.Property})");
                                            break;
                                        case "R":
                                            if (float.TryParse(reader.ReadString(), out float red)) {
                                                colorProp.Value.r = red;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got color R! ({colorProp.Value.r})");
                                            }
                                            break;
                                        case "G":
                                            if (float.TryParse(reader.ReadString(), out float green)) {
                                                colorProp.Value.g = green;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got color G! ({colorProp.Value.g})");
                                            }
                                            break;
                                        case "B":
                                            if (float.TryParse(reader.ReadString(), out float blue)) {
                                                colorProp.Value.b = blue;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got color B! ({colorProp.Value.b})");
                                            }
                                            break;
                                        case "A":
                                            if (float.TryParse(reader.ReadString(), out float alpha)) {
                                                colorProp.Value.a = alpha;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got color A! ({colorProp.Value.a})");
                                            }
                                            break;
                                        case "Color":
                                            reading = 0;
                                            constructedEntry.Data.MaterialColorPropertyList.Add(colorProp);
                                            colorProp = null;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Color done!");
                                            break;
                                        case "":
                                            continue;
                                        default:
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Unexpected tag found while reading Color: {reader.Name}");
                                            break;
                                    }
                                    break;
                                case 4: // Texture
                                    switch (reader.Name) {
                                        case "Name":
                                            textureProp.Property = reader.ReadString();
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Got texture name! ({textureProp.Property})");
                                            break;
                                        case "OffsetX":
                                            if (float.TryParse(reader.ReadString(), out float offX)) {
                                                Vector2 offset = textureProp.Offset ?? Vector2.zero;
                                                offset.x = offX;
                                                textureProp.Offset = offset;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got texture Offset X! ({textureProp.Offset.Value.x})");
                                            }
                                            break;
                                        case "OffsetY":
                                            if (float.TryParse(reader.ReadString(), out float offY)) {
                                                Vector2 offset = textureProp.Offset ?? Vector2.zero;
                                                offset.y = offY;
                                                textureProp.Offset = offset;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got texture Offset Y! ({textureProp.Offset.Value.y})");
                                            }
                                            break;
                                        case "ScaleX":
                                            if (float.TryParse(reader.ReadString(), out float sclX)) {
                                                Vector2 scale = textureProp.Scale ?? Vector2.zero;
                                                scale.x = sclX;
                                                textureProp.Scale = scale;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got texture Scale X! ({textureProp.Scale.Value.x})");
                                            }
                                            break;
                                        case "ScaleY":
                                            if (float.TryParse(reader.ReadString(), out float sclY)) {
                                                Vector2 scale = textureProp.Scale ?? Vector2.zero;
                                                scale.y = sclY;
                                                textureProp.Scale = scale;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got texture Scale Y! ({textureProp.Scale.Value.y})");
                                            }
                                            break;
                                        case "Data":
                                            textureProp.Data = Convert.FromBase64String(reader.ReadString());
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Got texture Data! ({textureProp.Data.Length} bytes)");
                                            break;
                                        case "Texture":
                                            reading = 0;
                                            constructedEntry.Data.MaterialTexturePropertyList.Add(textureProp);
                                            textureProp = null;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Texture done!");
                                            break;
                                        case "":
                                            continue;
                                        default:
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Unexpected tag found while reading Texture: {reader.Name}");
                                            break;
                                    }
                                    break;
                                case 5: // Keyword
                                    switch (reader.Name) {
                                        case "Name":
                                            keywordProp.Property = reader.ReadString();
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Got keyword name! ({keywordProp.Property})");
                                            break;
                                        case "Value":
                                            if (int.TryParse(reader.ReadString(), out int val)) {
                                                keywordProp.Value = val == 1;
                                                if (ConfDebug.Value) LoggerStat.LogDebug($"Got keyword value! ({keywordProp.Value})");
                                            }
                                            break;
                                        case "Keyword":
                                            reading = 0;
                                            constructedEntry.Data.MaterialKeywordPropertyList.Add(keywordProp);
                                            keywordProp = null;
                                            if (ConfDebug.Value) LoggerStat.LogDebug("Keyword done!");
                                            break;
                                        case "":
                                            continue;
                                        default:
                                            if (ConfDebug.Value) LoggerStat.LogDebug($"Unexpected tag found while reading Keyword: {reader.Name}");
                                            break;
                                    }
                                    break;
                            }
                        }
                        break;
                }
                reader.Close();
            }
        }
    }
}

