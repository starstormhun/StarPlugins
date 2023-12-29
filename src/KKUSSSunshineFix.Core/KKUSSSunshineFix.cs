using BepInEx;
using KK_Plugins.MaterialEditor;
using static KK_Plugins.MaterialEditor.MaterialEditorCharaController;
using static MaterialEditorAPI.MaterialAPI;
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(KKUSSFix.Sunshine.KKUSSSunshineFix.Version)]

namespace KKUSSFix.Sunshine {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
    [BepInPlugin(GUID, "KKUSS Sunshine Fix", Version)]
    /// <info>
    /// Plugin structure thanks to Keelhauled
    /// </info>
    public class KKUSSSunshineFix : BaseUnityPlugin {
        public const string GUID = "starstorm.kkussfix";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public static KKUSSSunshineFix Instance {get; private set; }

        private void Awake() {
            Instance = this;
            HookPatch.Init();
            Log.SetLogSource(Logger);
        }
    }
}
