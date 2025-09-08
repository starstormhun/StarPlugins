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
        public const string Version = "1.1.0." + BuildNumber.Version;

        public static ConfigEntry<bool> Enabled {  get; private set; }

        bool needInit = true;
        GUIStyle styleOuter;
        GUIStyle styleInner;
        Texture2D bgOuter;
        Texture2D bgInner;
        Rect rectOuter;
        Rect rectInner;

        private void Awake() {
            Enabled = Config.Bind("General", "Enabled", false, "Whether to show the cursor position on screen. Useful in case your cursor won't show on recordings but you'd really rather it did.");

            rectOuter = new Rect(Vector2.zero, new Vector2(12, 12));
            rectInner = new Rect(Vector2.zero, new Vector2(8, 8));
        }

        private void Update() {
            if (Enabled.Value && Event.current?.mousePosition != null) {
                rectOuter.position = Event.current.mousePosition - new Vector2(6, 6);
                rectInner.position = Event.current.mousePosition - new Vector2(4, 4);
            }
        }

        private void OnGUI() {
            if (needInit) {
                needInit = false;

                styleOuter = new GUIStyle(GUI.skin.box);
                styleInner = new GUIStyle(GUI.skin.box);

                bgOuter = new Texture2D(1, 1);
                bgOuter.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
                bgOuter.Apply();
                styleOuter.normal.background = bgOuter;

                bgInner = new Texture2D(1, 1);
                bgInner.SetPixel(0, 0, new Color(1, 1, 1, 0.75f));
                bgInner.Apply();
                styleInner.normal.background = bgInner;
            }

            if (Enabled.Value) {
                GUI.Box(rectOuter, "", styleOuter);
                GUI.Box(rectInner, "", styleInner);
            }
        }
    }
}
