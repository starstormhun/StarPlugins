using BepInEx;
using BepInEx.Configuration;
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(MassShaderEditor.Koikatu.MassShaderEditor.Version)]

namespace MassShaderEditor.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "Mass Shader Editor", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public partial class MassShaderEditor : BaseUnityPlugin {
        public const string GUID = "starstorm.massshadereditor";
        public const string Version = "1.0.0." + BuildNumber.Version;

        private ConfigEntry<KeyboardShortcut> VisibleHotkey { get; set; }
        private ConfigEntry<float> UIScale { get; set; }

        private ConfigEntry<bool> IsDebug { get; set; }

        private Studio.Studio studio;
        private bool inited = false;
        private bool scaled = false;

        private void Awake() {
            VisibleHotkey = Config.Bind("General", "UI Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("The key used to toggle the plugin's UI",null,new KKAPI.Utilities.ConfigurationManagerAttributes{ Order = 10}));
            UIScale = Config.Bind("General", "UI Scale", 1f, new ConfigDescription("Can also be set via the built-in settings panel", new AcceptableValueRange<float>(1f, 3f), null));
            UIScale.SettingChanged += (x, y) => scaled = false;

            IsDebug = Config.Bind("Debug", "Logging", false, new ConfigDescription("Enable verbose logging", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));

            KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => studio = Singleton<Studio.Studio>.Instance;

            Log.SetLogSource(Logger);
            if (IsDebug.Value) Log.Info("Awoken!");
        }

        private void Update() {
            if (VisibleHotkey.Value.IsDown())
                isShown = !isShown;
            if (!KKAPI.Maker.MakerAPI.InsideMaker && !KKAPI.Studio.StudioAPI.InsideStudio)
                isShown = false;
        }

        private void OnGUI() {
            if (!inited) {
                inited = true;
                InitUI();
            }
            if (!scaled) {
                scaled = true;
                ScaleUI(UIScale.Value);
            }
            if (isShown) {
                windowRect = GUILayout.Window(587, windowRect, WindowFunction, "Mass Shader Editor", newSkin.window);
                KKAPI.Utilities.IMGUIUtils.EatInputInRect(windowRect);
                helpRect = new Rect(windowRect.position + new Vector2(windowRect.size.x+3, 0), windowRect.size);
                if (isHelp) helpRect.size = new Vector2(helpRect.size.x, newSkin.label.CalcHeight(new GUIContent(helpText),helpRect.size.x) + 2.5f*newSkin.window.padding.top);
                if (isHelp || isSetting) {
                    if (isHelp) helpRect = GUILayout.Window(588, helpRect, HelpFunction, "How to use?", newSkin.window);
                    else if (isSetting) helpRect = GUILayout.Window(588, helpRect, SettingFunction, "Settings ۞", newSkin.window);
                    KKAPI.Utilities.IMGUIUtils.EatInputInRect(helpRect);
                }
            }
        }
    }
}
