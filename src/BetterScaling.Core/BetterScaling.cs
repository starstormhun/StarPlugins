using BepInEx;
using UnityEngine;
using KKAPI.Utilities;
using BepInEx.Configuration;
using KKAPI.Studio.SaveLoad;
using Studio;

[assembly: System.Reflection.AssemblyFileVersion(BetterScaling.BetterScaling.Version)]

namespace BetterScaling {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(Performancer.Performancer.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Better Scaling", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class BetterScaling : BaseUnityPlugin {
        public const string GUID = "starstorm.betterscaling";
        public const string Version = "0.2.0." + BuildNumber.Version;

        public static BetterScaling Instance { get; private set; }

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> FolderScaling { get; private set; }
        public static ConfigEntry<bool> LogarithmicScaling { get; private set; }
        public static ConfigEntry<bool> HierarchyScaling { get; private set; }
        public static ConfigEntry<bool> IsDebug { get; private set; }

        private void Awake() {
            Instance = this;

            Enabled = Config.Bind("General", "Enable plugin", true, new ConfigDescription("REQUIRES RESTART! Enable/disable the plugin entirely. DO NOT save / modify current scene after changing this setting!", null, new ConfigurationManagerAttributes { Order = 10 }));
            FolderScaling = Config.Bind("General", "Scale folders", true, "Makes it possible to scale folders. You need to save/reload the scene after changing this.");
            LogarithmicScaling = Config.Bind("General", "Logaritchmic guideobject scaling", false, "The bigger the scale, the faster it scales! And the smaller the scale, the slower it goes. Allows better control across all scales.");
            HierarchyScaling = Config.Bind("General", "Hierarchy scaling", true, "REQUIRES RESTART! Enable toggling of hierarchy scaling. Mark objects in the Workspace window. Marked objects will scale their direct children by their own scale. DO NOT save / modify current scene after changing this setting!");
            IsDebug = Config.Bind("Debug", "Logging", false, new ConfigDescription("Enable verbose logging", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            if (IsDebug.Value) Log("Awoken!");

            if (!Enabled.Value) return;

            StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(SceneDataController.SaveID);

            HookPatch.Init();
        }

        public static bool ToggleScaling(TreeNodeObject tno) {
            if (HookPatch.Hierarchy.dicTNOScaleHierarchy.TryGetValue(tno, out bool val)) {
                HookPatch.Hierarchy.dicTNOScaleHierarchy[tno] = !val;
                return true;
            } else {
                return false;
            }
        }

        public static bool SetScaling(TreeNodeObject tno, bool state) {
            if (HookPatch.Hierarchy.dicTNOScaleHierarchy.ContainsKey(tno)) {
                HookPatch.Hierarchy.dicTNOScaleHierarchy[tno] = state;
                return true;
            } else {
                return false;
            }
        }

        public static bool IsScaled(TreeNodeObject tno) {
            if (HookPatch.Hierarchy.dicTNOScaleHierarchy.TryGetValue(tno, out bool val)) {
                return val;
            } else {
                return false;
            }
        }

        public static bool IsHierarchyScalable(TreeNodeObject tno) {
            return HookPatch.Hierarchy.dicTNOScaleHierarchy.ContainsKey(tno);
        }

        internal void Log(object data, int level = 0) {
            switch (level) {
                case 0:
                    Logger.LogInfo(data); return;
                case 1:
                    Logger.LogDebug(data); return;
                case 2:
                    Logger.LogWarning(data); return;
                case 3:
                    Logger.LogError(data); return;
                case 4:
                    Logger.LogFatal(data); return;
                case 5:
                    Logger.LogMessage(data); return;
                default: return;
            }
        }
    }
}
