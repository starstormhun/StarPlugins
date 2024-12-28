using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(AccMover.AccMover.Version)]

namespace AccMover {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "Acc Mover", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class AccMover : BaseUnityPlugin {
        public const string GUID = "starstorm.accmover";
        public const string Version = "1.0.0." + BuildNumber.Version;

        private void Awake() {
#if DEBUG
            Logger.LogInfo($"Plugin {GUID} is loaded!");
#endif
        }

        private void Update() {
			
        }
    }
}
