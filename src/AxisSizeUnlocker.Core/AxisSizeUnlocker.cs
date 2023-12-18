using BepInEx;
using BepInEx.Configuration;
#if KKS
using Common.KoikatsuSunshine;
#else
using Common.Koikatu;
#endif
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(AxisSizeUnlocker.AxisSizeUnlocker.Version)]

namespace AxisSizeUnlocker {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, "Axis Size Unlocker", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class AxisSizeUnlocker : BaseUnityPlugin {
        public const string GUID = "starstorm.axissizeunlocker";
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
