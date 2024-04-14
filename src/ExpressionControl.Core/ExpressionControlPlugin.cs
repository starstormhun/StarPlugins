using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
[assembly: System.Reflection.AssemblyFileVersion(ExpressionControl.ExpressionControlPlugin.Version)]
namespace ExpressionControl
{
	[BepInDependency(KKAPI.KoikatuAPI.GUID)]
	[BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessName)]
#if KK
	[BepInProcess(KKAPI.KoikatuAPI.GameProcessNameSteam)]
#endif
	[BepInPlugin(GUID, Name, Version)]
	/// <info>
	/// Plugin structure thanks to Keelhauled
	/// </info>
	public class ExpressionControlPlugin : BaseUnityPlugin
	{
		public const string GUID = "ExpressionControl";
        public const string Version = "0.2.6." + BuildNumber.Version;
		public const string Name = GUID;
		
		public void Start()
		{
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoad);
			ExpressionControlPlugin.goMain = new GameObject(ExpressionControlPlugin.Name);
			ExpressionControlPlugin.inst = ExpressionControlPlugin.goMain.AddComponent<ExpressionControl>();
			ExpressionControlPlugin.hotkey = ExpressionControlPlugin.goMain.AddComponent<ShortCutKeyMgr>();
            UnityEngine.Object.DontDestroyOnLoad(ExpressionControlPlugin.goMain);
			Harmony.CreateAndPatchAll(typeof(Hooks), null);
		}
		public void OnSceneLoad(Scene scene, LoadSceneMode mode)
		{
			ExpressionControlPlugin.inst.ReStart();
		}
		private static GameObject goMain;
		internal static ExpressionControl inst;
		internal static ShortCutKeyMgr hotkey;
	}
}