using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(CursorIndicator.CursorIndicator.Version)]

namespace CursorIndicator {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "Cursor Indicator", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class CursorIndicator : BaseUnityPlugin {
        public const string GUID = "starstorm.cursorindicator";
        public const string Version = "1.0.0." + BuildNumber.Version;

        public static ConfigEntry<bool> Enabled {  get; private set; }

        private void Awake() {
            Enabled = Config.Bind("General", "Enabled", false, "Whether to show the cursor position on screen. Useful in case your cursor won't show on recordings but you'd really rather it did.");
        }

        private void Update() {
        }

        private void OnGUI() {
            if (Enabled.Value) {
                var rect = new Rect(Event.current.mousePosition - new Vector2(5, 5), new Vector2(10, 10));
                GUI.Box(rect, "");
            }
        }
    }
}
