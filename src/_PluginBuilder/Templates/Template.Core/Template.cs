using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion($nsname$.$safeprojectname$.Version)]

namespace $nsname$ {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "$projectname$", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class $safeprojectname$ : BaseUnityPlugin {
        public const string GUID = "$guid$";
        public const string Version = "1.0.0." + BuildNumber.Version;

        private void Awake() {
			
        }

        private void Update() {
			
        }
    }
}
