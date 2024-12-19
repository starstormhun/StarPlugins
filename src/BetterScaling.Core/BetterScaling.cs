using BepInEx;
using BepInEx.Configuration;
using KKAPI.Utilities;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(BetterScaling.Koikatu.BetterScaling.Version)]

namespace BetterScaling.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Better Scaling", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class BetterScaling : BaseUnityPlugin {
        public const string GUID = "starstorm.betterscaling";
        public const string Version = "0.1.0." + BuildNumber.Version;

        public static BetterScaling Instance { get; private set; }

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> FolderScaling { get; private set; }
        // public static ConfigEntry<bool> ChildScaling { get; private set; }
        public static ConfigEntry<bool> LogarithmicScaling { get; private set; }
        public static ConfigEntry<bool> IsDebug { get; private set; }

        // private GameObject commonSpace;
        // private bool scaled = false;

        private void Awake() {
            Log.SetLogSource(Logger);

            Instance = this;

            Enabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("REQUIRES RESTART! Enable/disable the plugin entirely. DO NOT save / modify current scene after changing this setting!", null, new ConfigurationManagerAttributes { Order = 10 }));
            FolderScaling = Config.Bind("General", "Scale folders", true, "Makes it possible to scale folders. You need to save/reload the scene after changing this.");
            // ChildScaling = Config.Bind("General", "Scale object children", false, "Makes scaling an object also scale its children.");
            LogarithmicScaling = Config.Bind("General", "Logaritchmic guideobject scaling", false, "The bigger the scale, the faster it scales! And the smaller the scale, the slower it goes. Allows better control across all scales.");
            IsDebug = Config.Bind("Debug", "Logging", false, new ConfigDescription("Enable verbose logging", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            if (!Enabled.Value) return;

            HookPatch.Init();

            if (IsDebug.Value) Log.Info("Awoken!");
        }

        /* private void Update() {
            if (commonSpace != null) {
                if (Enabled.Value && ChildScaling.Value) {
                    scaled = true;
                    ScaleChildren(commonSpace.transform, Vector3.one);
                } else if (scaled) {
                    scaled = false;
                    ScaleChildren(commonSpace.transform, Vector3.one, true);
                }
            }
        }

        private void ScaleChildren(Transform tf, Vector3 currentScale, bool reset = false) {
            for (int i = 0; i<tf.childCount; i++) {
                Transform child = tf.GetChild(i);
                Vector3 nextScale = currentScale.ScaleImmut(tf.lossyScale.ScaleImmut(tf.localScale.Invert()));
                if (child.TryGetComponent<MeshRenderer>(out _)) {
                    if (reset) {
                        child.localScale = Vector3.one;
                    } else {
                        child.localScale = nextScale;
                    }
                } else ScaleChildren(child, nextScale, reset);
            }
        } */
    }
}
