using BepInEx;
using BepInEx.Configuration;
#if KKS
using Common.KoikatsuSunshine;
#else
using Common.Koikatu;
#endif
using System.Linq;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion($nsname$.$safeprojectname$.Version)]

namespace $nsname$ {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, "$projectname$", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class $safeprojectname$ : BaseUnityPlugin {
        public const string GUID = "$guid$";
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
