using BepInEx;
using BepInEx.Configuration;
using KK_Plugins.MaterialEditor;

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
        public const string Version = "1.2.0." + BuildNumber.Version;

        public static ShaderFixer Instance {get; private set; }

        public static ConfigEntry<string> Filter { get; set; }

        public static ConfigEntry<string> Properties { get; set; }

        private void Awake() {
            Instance = this;

            const string shaderFilterDescription =
                "Specify filters for which shaders (partial name) should be affected. Separate tokens with commas. Spaces and case" +
                "don't matter. A '-' sign in the front denotes a negative filter. Changes take effect on scene reload and on newly" +
                "added or changed items (shaders).";
            const string propertyNameDescription =
                "The texture properties to be affected. Must be the full name of the property as it appears in Material Editor." +
                "Separate tokens with commas. Spaces don't matter, but case does.";

            Filter = Config.Bind("General", "Shader filter", "KKUSS, KKUTS", new ConfigDescription(shaderFilterDescription, null));
            Properties = Config.Bind("General", "Properties", "NormalMap, BumpMap", new ConfigDescription(propertyNameDescription, null));

            HookPatch.Init();
            Log.SetLogSource(Logger);
        }
    }
}
