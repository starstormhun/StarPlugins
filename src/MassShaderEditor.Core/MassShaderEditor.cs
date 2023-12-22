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
    public class MassShaderEditor : BaseUnityPlugin {
        public const string GUID = "starstorm.massshadereditor";
        public const string Version = "1.0.0." + BuildNumber.Version;

        private static ConfigEntry<KeyboardShortcut> VisibleHotkey { get; set; }

        private static bool isShown = false;
        private Rect windowRect = new Rect(500, 40, 240, 170);

        private void Start() {
            VisibleHotkey = Config.Bind("General", "UI Toggle", new KeyboardShortcut(KeyCode.M), new ConfigDescription("The key used to toggle the plugin's UI",null,new KKAPI.Utilities.ConfigurationManagerAttributes{ Order = 10}));
        }

        private void Update() {
            if (VisibleHotkey.Value.IsDown())
                isShown = !isShown;
            if (!KKAPI.Maker.MakerAPI.InsideMaker && !KKAPI.Studio.StudioAPI.InsideStudio)
                isShown = false;
        }

        private void OnGUI() {
            if (isShown) {
                windowRect = GUI.Window(587, windowRect, WindowFunction, "Mass Shader Editor v" + Version);
                KKAPI.Utilities.IMGUIUtils.EatInputInRect(windowRect);
            }
        }

        private void WindowFunction(int WindowID) {


            GUI.DragWindow();
        }
    }
}
