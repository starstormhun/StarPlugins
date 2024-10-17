using BepInEx;
using BepInEx.Configuration;
using KK_Plugins.MaterialEditor;
using KKAPI.Utilities;

[assembly: System.Reflection.AssemblyFileVersion(ShaderFixer.ShaderFixer.Version)]

namespace ShaderFixer {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
    [BepInPlugin(GUID, "Koikatsu Shader Fixer", Version)]
    /// <info>
    /// Plugin structure thanks to Keelhauled
    /// </info>
    public class ShaderFixer : BaseUnityPlugin {
        public const string GUID = "starstorm.shaderfixer";
        public const string Version = "1.1.0." + BuildNumber.Version;

        public static ShaderFixer Instance {get; private set; }

        public static ConfigEntry<string> Filter { get; set; }

        public static ConfigEntry<string> Properties { get; set; }

        private void Awake() {
            Instance = this;
      
            Filter = Config.Bind("General", "Shader filter", "KKUSS", new ConfigDescription(
                "Specify filters for which shaders (partial name) should be affected. Separate tokens with commas, " +
                "spaces and case don't matter. A '-' sign in the front denotes a negative filter. Changes " +
                "take effect on scene reload and on newly added / changed items (shaders).", null));

            Properties = Config.Bind("General", "Properties", "NormalMap, BumpMap", new ConfigDescription(
                "The texture properties (full name) to be affected. Separate tokens with commas, spaces don't matter, but case does.", null));

            HookPatch.Init();
            Log.SetLogSource(Logger);
        }
    }
}
