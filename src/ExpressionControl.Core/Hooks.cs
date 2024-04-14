using System;
using HarmonyLib;
namespace ExpressionControl
{
	public static class Hooks
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(FaceListCtrl), "SetFace", new Type[]
		{
			typeof(int),
			typeof(ChaControl),
			typeof(int),
			typeof(int)
		})]
		public static bool SetFaceHook(int _idFace, ChaControl _chara, int _voiceKind, int _action, FaceListCtrl __instance, ref bool __result)
		{
			bool flag = _chara == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				Traverse traverse = Traverse.Create(__instance);
				bool flag2 = ExpressionControlPlugin.inst.IsIgnoreSetFace(_idFace, _chara, _voiceKind, _action, __instance, traverse.Field("blendEye").GetValue<GlobalMethod.FloatBlend>(), traverse.Field("blendMouth").GetValue<GlobalMethod.FloatBlend>());
				if (flag2)
				{
					__result = false;
					result = false;
				}
				else
				{
					result = true;
				}
			}
			return result;
		}
	}
}