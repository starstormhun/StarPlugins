using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(AAAAAAAAAAAA.AAAAAAAAAAAA.Version)]

namespace AAAAAAAAAAAA {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(KK_MoreAccessoryParents.MoreAccParents.GUID, KK_MoreAccessoryParents.MoreAccParents.Version)]
    [BepInDependency(KKABMX.Core.KKABMX_Core.GUID, KKABMX.Core.KKABMX_Core.Version)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInPlugin(GUID, "AAAAAAAAAAAA", Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
    public class AAAAAAAAAAAA : BaseUnityPlugin {
        // Actual plugin name: Attach All Accessories Anywhere, Anytime, At Any Angle And Artistic Arrangement, Allegedly
        public const string GUID = "starstorm.aaaaaaaaaaaa";
        public const string Version = "0.1.0." + BuildNumber.Version;

        private void Awake() {

        }

        private void Update() {
			
        }
    }
}
