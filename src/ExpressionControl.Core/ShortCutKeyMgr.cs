using System;
using System.Collections.Generic;
using UnityEngine;
namespace ExpressionControl
{
	public class ShortCutKeyMgr : MonoBehaviour
	{
		public ShortCutKeyMgr.ShortCutKey this[string name]
		{
			get
			{
				bool flag = !this.dict.ContainsKey(name);
				ShortCutKeyMgr.ShortCutKey result;
				if (flag)
				{
					result = null;
				}
				else
				{
					result = this.dict[name];
				}
				return result;
			}
		}
		public bool Set(string name, string key)
		{
			bool flag = string.IsNullOrEmpty(name);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = this.dict.ContainsKey(name);
				if (flag2)
				{
					this.dict[name].SetKey(key);
				}
				else
				{
					this.dict.Add(name, new ShortCutKeyMgr.ShortCutKey(key));
				}
				result = true;
			}
			return result;
		}
		public bool Remove(string name)
		{
			bool flag = string.IsNullOrEmpty(name);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = this.dict.ContainsKey(name);
				if (flag2)
				{
					this.dict.Remove(name);
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}
		public void Update()
		{
			ShortCutKeyMgr.chkModKeyOnCurrentFrame = false;
		}
		private static bool chkModKeyOnCurrentFrame;
		private static bool _ctrl;
		private static bool _alt;
		private static bool _shift;
		private Dictionary<string, ShortCutKeyMgr.ShortCutKey> dict = new Dictionary<string, ShortCutKeyMgr.ShortCutKey>();
		public class ShortCutKey
		{
			public ShortCutKey(string s)
			{
				ShortCutKeyMgr.ShortCutKey.StringToKey(this, s);
			}
			public bool GetKeyState()
			{
				bool flag = !ShortCutKeyMgr.chkModKeyOnCurrentFrame;
				if (flag)
				{
					ShortCutKeyMgr._ctrl = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
					ShortCutKeyMgr._shift = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
					ShortCutKeyMgr._alt = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
					ShortCutKeyMgr.chkModKeyOnCurrentFrame = true;
				}
				return (this.ctrl ? ShortCutKeyMgr._ctrl : (!ShortCutKeyMgr._ctrl)) && (this.shift ? ShortCutKeyMgr._shift : (!ShortCutKeyMgr._shift)) && (this.alt ? ShortCutKeyMgr._alt : (!ShortCutKeyMgr._alt)) && Input.GetKeyDown(this.key);
			}
			public bool GetKeyState_Move()
			{
				bool flag = !ShortCutKeyMgr.chkModKeyOnCurrentFrame;
				if (flag)
				{
                    ShortCutKeyMgr._ctrl = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
                    ShortCutKeyMgr._shift = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                    ShortCutKeyMgr._alt = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
                    ShortCutKeyMgr.chkModKeyOnCurrentFrame = true;
				}
				return (this.ctrl ? ShortCutKeyMgr._ctrl : (!ShortCutKeyMgr._ctrl)) && (this.shift ? ShortCutKeyMgr._shift : (!ShortCutKeyMgr._shift)) && Input.GetKey(this.key);
			}
			public void SetKey(string s)
			{
				ShortCutKeyMgr.ShortCutKey.StringToKey(this, s);
			}
			private static void StringToKey(ShortCutKeyMgr.ShortCutKey hotkey, string s)
			{
				bool flag = string.IsNullOrEmpty(s);
				if (!flag)
				{
					hotkey.ctrl = (hotkey.alt = (hotkey.shift = false));
					s = s.ToLower();
					bool flag2 = s.Contains("ctrl+");
					if (flag2)
					{
						hotkey.ctrl = true;
						s = s.Replace("ctrl+", string.Empty);
					}
					bool flag3 = s.Contains("alt+");
					if (flag3)
					{
						hotkey.alt = true;
						s = s.Replace("alt+", string.Empty);
					}
					bool flag4 = s.Contains("shift+");
					if (flag4)
					{
						hotkey.shift = true;
						s = s.Replace("shift+", string.Empty);
					}
					hotkey.key = s;
				}
			}
			private string key = string.Empty;
			private bool ctrl;
			private bool alt;
			private bool shift;
		}
	}
}