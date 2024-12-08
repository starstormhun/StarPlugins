using BepInEx;
using BepInEx.Configuration;
using KKAPI.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(Performancer.Performancer.Version)]

namespace Performancer {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Performancer", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class Performancer : BaseUnityPlugin {
        public const string GUID = "starstorm.performancer";
        public const string Version = "0.1.0." + BuildNumber.Version;

        public static Performancer Instance { get; private set; }

        public static ConfigEntry<bool> OptimiseGuideObjectLate {  get; private set; }
        public static ConfigEntry<bool> DoLogs {  get; private set; }

        internal static bool isLogCoroutine = false;
        internal static int numGuideObjectLateUpdates = 0;

        private void Awake() {
            Instance = this;

            DoLogs = Config.Bind("Advanced", "Log Performance Numbers", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimiseGuideObjectLate = Config.Bind("General", "Optimise GuideObject LateUpdate", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            HookPatch.Init();
        }

        private void Update() {
            if (!isLogCoroutine && DoLogs.Value) {
                isLogCoroutine = true;
                StartCoroutine(LogCoroutine());

                IEnumerator LogCoroutine() {
                    yield return new WaitForSeconds(1f);
                    Log($"GuideObject LateUpdates this second: {numGuideObjectLateUpdates}");
                    numGuideObjectLateUpdates = 0;
                    isLogCoroutine = false;
                }
            }
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
                default: return;
            }
        }
    }
}
