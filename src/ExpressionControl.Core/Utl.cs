using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Manager;
using Studio;
using UnityEngine;
namespace ExpressionControl
{
	internal static class Utl
	{
		public static bool Equals(float a, float b)
		{
			return Mathf.Abs(a - b) < 0.01f;
		}
		public static List<OCIChar> GetOCICharFemaleAll()
		{
			List<OCIChar> list = new List<OCIChar>();
			bool flag = UnityEngine.Object.FindObjectOfType<StudioScene>() == null;
			List<OCIChar> result;
			if (flag)
			{
				result = list;
			}
			else
			{
				foreach (ObjectCtrlInfo objectCtrlInfo in Singleton<Studio.Studio>.Instance.dicObjectCtrl.Values)
				{
					if (objectCtrlInfo is OCICharFemale oci1) {
						list.Add(oci1);
					}
                    if (objectCtrlInfo is OCICharMale oci2) {
                        list.Add(oci2);
                    }
                }
				result = list;
			}
			return result;
		}
		public static List<ChaControl> GetFemaleAll()
		{
			List<ChaControl> list = new List<ChaControl>();
			List<ChaControl> charaList =
#if KKS
				Character.GetCharaList(1);
			charaList.AddRange(Character.GetCharaList(0));
#else
                Singleton<Character>.Instance.GetCharaList(1);
			charaList.AddRange(Singleton<Character>.Instance.GetCharaList(0));
#endif
			bool flag = charaList == null || charaList.Count == 0;
			List<ChaControl> result;
			if (flag)
			{
				Console.WriteLine(ExpressionControlPlugin.Name + ": no charactor");
				result = list;
			}
			else
			{
				for (int i = 0; i <= charaList.Count - 1; i++)
				{
					bool hiPoly = charaList[i].hiPoly;
					if (hiPoly)
					{
						list.Add(charaList[i]);
					}
				}
				result = list;
			}
			return result;
		}
		internal static int GetPix(int i)
		{
			return (int)((1f + ((float)Screen.width / 1280f - 1f) * 0.6f) * (float)i);
		}
		internal static FieldInfo GetFieldInfo<T>(string name)
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
			return typeof(T).GetField(name, bindingAttr);
		}
		internal static TResult GetFieldValue<T, TResult>(T inst, string name)
		{
			bool flag = inst == null;
			TResult result;
			if (flag)
			{
				result = default(TResult);
			}
			else
			{
				FieldInfo fieldInfo = Utl.GetFieldInfo<T>(name);
				bool flag2 = fieldInfo == null;
				if (flag2)
				{
					result = default(TResult);
				}
				else
				{
					result = (TResult)((object)fieldInfo.GetValue(inst));
				}
			}
			return result;
		}
		public static void SetFieldValue<T>(object inst, string name, object val)
		{
			FieldInfo fieldInfo = Utl.GetFieldInfo<T>(name);
			bool flag = fieldInfo != null;
			if (flag)
			{
				fieldInfo.SetValue(inst, val);
			}
		}
		public static IEnumerator CoSmoothUpdate(Action<float, bool> callback, float source, float dest, float time)
		{
			bool flag = time <= 0f || source == dest;
			if (flag)
			{
				yield break;
			}
			float nowTime = time;
			while ((nowTime -= Time.deltaTime) > 0f)
			{
				callback(Mathf.Lerp(source, dest, Mathf.Log10(10f - nowTime / time * 10f)), false);
				yield return null;
			}
			callback(dest, true);
			yield break;
		}
	}
}