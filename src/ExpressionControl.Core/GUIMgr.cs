using System;
using UnityEngine;
namespace ExpressionControl
{
	internal class GUIMgr
	{
		public int fontSize { get; private set; }
		public float itemHeight { get; private set; }
		public float margin { get; private set; }
		public GUIStyle gsWindow { get; private set; }
		public GUIStyle gsWindowFold { get; private set; }
		public GUIStyle gsLabel { get; private set; }
		public GUIStyle gsLabel_Middle { get; private set; }
		public GUIStyle gsToggle { get; private set; }
		public GUIStyle gsButton { get; private set; }
		public GUIStyle gsTextArea { get; private set; }
		public GUIStyle gsTextField { get; private set; }
		public GUIStyle gsGroup { get; private set; }
		public GUIStyle gsPanelNormal { get; private set; }
		public GUIStyle gsPanelBorderRed { get; private set; }
		public ComboBox combo { get; private set; }
		public string windowTitle { get; set; }
		public GUIMgr(int winID, Rect rect, string windowTitle)
		{
			this.WINDOW_ID = winID;
			this.windowTitle = windowTitle;
			this.Init(rect);
		}
		public float itemHeightWithMargin
		{
			get
			{
				return this.itemHeight + this.margin;
			}
		}
		private void Init(Rect rect)
		{
			this.inst = ExpressionControlPlugin.inst;
			this.fontSize = GUIUtl.GetPix(12);
			this.itemHeight = (float)this.fontSize * 1.5f;
			this.margin = (float)this.fontSize * 0.3f;
			this.gsWindow = new GUIStyle("window");
			this.gsWindow.fontSize = 12;
			this.gsWindow.alignment = TextAnchor.UpperCenter;
			this.gsWindow.active.background = (this.gsWindow.focused.background = (this.gsWindow.normal.background = (this.gsWindow.hover.background = GUIUtl.texGuiWindowOff)));
			this.gsWindow.onActive.background = (this.gsWindow.onFocused.background = (this.gsWindow.onNormal.background = (this.gsWindow.onHover.background = GUIUtl.texGuiWindowOn)));
			this.gsWindow.active.textColor = (this.gsWindow.focused.textColor = (this.gsWindow.normal.textColor = (this.gsWindow.hover.textColor = (this.gsWindow.onActive.textColor = (this.gsWindow.onFocused.textColor = (this.gsWindow.onNormal.textColor = (this.gsWindow.onHover.textColor = Color.black)))))));
			this.gsWindow.border = new RectOffset(8, 8, 20, 8);
			this.gsWindowFold = new GUIStyle("window");
			this.gsWindowFold.fontSize = 12;
			this.gsWindowFold.alignment = TextAnchor.UpperCenter;
			this.gsWindowFold.active.background = (this.gsWindowFold.focused.background = (this.gsWindowFold.normal.background = (this.gsWindowFold.hover.background = GUIUtl.texGuiWindowFoldOff)));
			this.gsWindowFold.onActive.background = (this.gsWindowFold.onFocused.background = (this.gsWindowFold.onNormal.background = (this.gsWindowFold.onHover.background = GUIUtl.texGuiWindowFoldOn)));
			this.gsWindowFold.active.textColor = (this.gsWindowFold.focused.textColor = (this.gsWindowFold.normal.textColor = (this.gsWindowFold.hover.textColor = (this.gsWindowFold.onActive.textColor = (this.gsWindowFold.onFocused.textColor = (this.gsWindowFold.onNormal.textColor = (this.gsWindowFold.onHover.textColor = Color.black)))))));
			this.gsWindowFold.border = new RectOffset(8, 8, 0, 0);
			this.gsWindowFold.padding.bottom = -20;
			this.gsLabel = new GUIStyle("label");
			this.gsLabel.fontSize = this.fontSize;
			this.gsLabel.alignment = TextAnchor.MiddleLeft;
			this.gsLabel_Middle = new GUIStyle("label");
			this.gsLabel_Middle.fontSize = this.fontSize;
			this.gsLabel_Middle.alignment = TextAnchor.MiddleCenter;
			this.gsToggle = new GUIStyle("toggle");
			this.gsToggle.fontSize = this.fontSize;
			this.gsToggle.alignment = TextAnchor.MiddleLeft;
			this.gsButton = new GUIStyle("button");
			this.gsButton.fontSize = this.fontSize;
			this.gsButton.alignment = TextAnchor.MiddleCenter;
			this.gsButton.normal.background = GUIUtl.texGuiButtonNormal;
			this.gsButton.active.background = GUIUtl.texGuiButtonActive;
			this.gsButton.focused.background = GUIUtl.texGuiButtonHover;
			this.gsButton.hover.background = GUIUtl.texGuiButtonHover;
			this.gsButton.border = new RectOffset(6, 6, 6, 6);
			this.gsTextArea = new GUIStyle("textarea");
			this.gsTextArea.fontSize = this.fontSize;
			this.gsTextArea.alignment = TextAnchor.UpperLeft;
			this.gsTextField = new GUIStyle("textfield");
			this.gsTextField.fontSize = this.fontSize;
			this.gsTextField.alignment = TextAnchor.UpperLeft;
			this.gsTextField.normal.background = GUIUtl.texGuiInputNormal;
			this.gsTextField.active.background = (this.gsTextField.focused.background = (this.gsTextField.hover.background = GUIUtl.texGuiInputActive));
			this.gsTextField.active.textColor = (this.gsTextField.focused.textColor = (this.gsTextField.normal.textColor = (this.gsTextField.hover.textColor = (this.gsTextField.onActive.textColor = (this.gsTextField.onFocused.textColor = (this.gsTextField.onNormal.textColor = (this.gsTextField.onHover.textColor = Color.black)))))));
			this.gsTextField.border = new RectOffset(6, 6, 6, 6);
			this.gsGroup = new GUIStyle("box");
			this.gsGroup.fontSize = this.fontSize;
			this.gsGroup.alignment = 0;
			this.gsGroup.active.background = (this.gsGroup.focused.background = (this.gsGroup.normal.background = (this.gsGroup.hover.background = (this.gsGroup.onActive.background = (this.gsGroup.onFocused.background = (this.gsGroup.onNormal.background = (this.gsGroup.onHover.background = GUIUtl.texGuiBackgroundOn)))))));
			this.gsGroup.active.textColor = (this.gsGroup.focused.textColor = (this.gsGroup.normal.textColor = (this.gsGroup.hover.textColor = (this.gsGroup.onActive.textColor = (this.gsGroup.onFocused.textColor = (this.gsGroup.onNormal.textColor = (this.gsGroup.onHover.textColor = Color.black)))))));
			this.gsGroup.border = new RectOffset(12, 12, 12, 12);
			this.gsPanelNormal = new GUIStyle();
			this.gsPanelNormal.fontSize = this.fontSize;
			this.gsPanelNormal.alignment = TextAnchor.MiddleLeft;
			this.gsPanelNormal.active.background = (this.gsPanelNormal.focused.background = (this.gsPanelNormal.normal.background = (this.gsPanelNormal.hover.background = (this.gsPanelNormal.onActive.background = (this.gsPanelNormal.onFocused.background = (this.gsPanelNormal.onNormal.background = (this.gsPanelNormal.onHover.background = GUIUtl.texGuiPanelNormal)))))));
			this.gsPanelNormal.active.textColor = (this.gsPanelNormal.focused.textColor = (this.gsPanelNormal.normal.textColor = (this.gsPanelNormal.hover.textColor = (this.gsPanelNormal.onActive.textColor = (this.gsPanelNormal.onFocused.textColor = (this.gsPanelNormal.onNormal.textColor = (this.gsPanelNormal.onHover.textColor = Color.black)))))));
			this.gsPanelNormal.border = new RectOffset(2, 2, 2, 2);
			this.gsPanelBorderRed = new GUIStyle();
			this.gsPanelBorderRed.fontSize = this.fontSize;
			this.gsPanelBorderRed.alignment = TextAnchor.MiddleLeft;
			this.gsPanelBorderRed.active.background = (this.gsPanelBorderRed.focused.background = (this.gsPanelBorderRed.normal.background = (this.gsPanelBorderRed.hover.background = (this.gsPanelBorderRed.onActive.background = (this.gsPanelBorderRed.onFocused.background = (this.gsPanelBorderRed.onNormal.background = (this.gsPanelBorderRed.onHover.background = GUIUtl.texGuiPanelBorderRed)))))));
			this.gsPanelBorderRed.active.textColor = (this.gsPanelBorderRed.focused.textColor = (this.gsPanelBorderRed.normal.textColor = (this.gsPanelBorderRed.hover.textColor = (this.gsPanelBorderRed.onActive.textColor = (this.gsPanelBorderRed.onFocused.textColor = (this.gsPanelBorderRed.onNormal.textColor = (this.gsPanelBorderRed.onHover.textColor = Color.black)))))));
			this.gsPanelBorderRed.border = new RectOffset(2, 6, 2, 2);
			this.rectMainWin.Set(rect.x, rect.y, rect.width * (float)this.fontSize, rect.height * (float)this.fontSize);
			this.mainWinHeight = this.rectMainWin.height;
			this.rectProgram.Set(0f, 0f, (float)(this.fontSize * 60), (float)Screen.height * 0.8f);
			this.programWinHeight = this.rectProgram.height;
			this.rectSave.Set(0f, 0f, (float)(this.fontSize * 20), (float)(this.fontSize * 12));
			this.combo = new ComboBox(this.WINDOW_ID + 3, this.fontSize);
		}
		public void Draw()
		{
			GUI.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
			this.rectMainWin = GUI.Window(this.WINDOW_ID, this.rectMainWin, new GUI.WindowFunction(this.inst.GUIFunc), this.windowTitle, this.mainWinFold ? this.gsWindowFold : this.gsWindow);
			this.rectMainWin = this.ClipWindowPosition(this.rectMainWin, !this.mainWinFold);
			bool flag = this.showSave;
			if (flag)
			{
				this.rectSave = GUI.Window(this.WINDOW_ID + 1, this.rectSave, new GUI.WindowFunction(this.inst.GUIFuncSave), this.windowTitle, this.gsWindow);
				this.rectSave = this.ClipWindowPosition(this.rectSave, true);
			}
			bool flag2 = this.showProgram;
			if (flag2)
			{
				this.rectProgram = GUI.Window(this.WINDOW_ID + 2, this.rectProgram, new GUI.WindowFunction(this.inst.GUIFuncProgram), this.windowTitle, this.programWinFold ? this.gsWindowFold : this.gsWindow);
				this.rectProgram = this.ClipWindowPosition(this.rectProgram, !this.programWinFold);
			}
			bool flag3 = this.combo.show;
			if (flag3)
			{
				this.combo.rect = GUI.Window(this.combo.WINDOW_ID, this.combo.rect, new GUI.WindowFunction(this.combo.GuiFunc), string.Empty, this.gsGroup);
			}
			GUI.backgroundColor = new Color(1f, 1f, 1f, 0f);
			GUI.Window(this.WINDOW_ID + 4, new Rect(-100f, -100f, 0f, 0f), new GUI.WindowFunction(this.inst.GUIFuncDummy), string.Empty);
			bool anyMouseButtonDown = GUIUtl.GetAnyMouseButtonDown();
			if (anyMouseButtonDown)
			{
				Vector2 vector = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
				bool flag4 = this.rectMainWin.Contains(vector) || (this.showSave && this.rectSave.Contains(vector)) || (this.showProgram && this.rectProgram.Contains(vector));
				if (flag4)
				{
					GUIMgr.isMouseDownOnWindow = true;
				}
				else
				{
					GUIMgr.isMouseDownOnWindow = false;
				}
			}
			bool flag5 = GUIMgr.isMouseDownOnWindow && GUIUtl.GetAnyMouseButton();
			if (flag5)
			{
				Vector2 vector2 = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
				bool flag6 = this.rectMainWin.Contains(vector2) || (this.showSave && this.rectSave.Contains(vector2)) || (this.showProgram && this.rectProgram.Contains(vector2));
				if (flag6)
				{
					bool flag7 = this.combo.show && !this.combo.rect.Contains(vector2);
					if (flag7)
					{
						this.combo.show = false;
					}
					Input.ResetInputAxes();
				}
			}
		}
		private Rect ClipWindowPosition(Rect rect, bool over)
		{
			if (over)
			{
				bool flag = rect.x < -rect.width * 0.9f;
				if (flag)
				{
					rect.x = -rect.width * 0.9f;
				}
				else
				{
					bool flag2 = rect.x > (float)Screen.width - rect.width * 0.1f;
					if (flag2)
					{
						rect.x = (float)Screen.width - rect.width * 0.1f;
					}
				}
				bool flag3 = rect.y < -rect.height * 0.9f;
				if (flag3)
				{
					rect.y = -rect.height * 0.9f;
				}
				else
				{
					bool flag4 = rect.y > (float)Screen.height - rect.height * 0.1f;
					if (flag4)
					{
						rect.y = (float)Screen.height - rect.height * 0.1f;
					}
				}
			}
			else
			{
				bool flag5 = rect.x < 0f;
				if (flag5)
				{
					rect.x = 0f;
				}
				else
				{
					bool flag6 = rect.x > (float)Screen.width - rect.width;
					if (flag6)
					{
						rect.x = (float)Screen.width - rect.width;
					}
				}
				bool flag7 = rect.y < 0f;
				if (flag7)
				{
					rect.y = 0f;
				}
				else
				{
					bool flag8 = rect.y > (float)Screen.height - rect.height;
					if (flag8)
					{
						rect.y = (float)Screen.height - rect.height;
					}
				}
			}
			return rect;
		}
		public float DrawLine(Vector2 position, float width)
		{
			GUI.DrawTexture(new Rect(position.x, position.y, width, 1f), GUIUtl.texBlack);
			return position.y + 1f + this.margin;
		}
		public float DrawLineV(Vector2 position, float height)
		{
			GUI.DrawTexture(new Rect(position.x, position.y, 1f, height), GUIUtl.texBlack);
			return position.x + 1f + this.margin;
		}
		public static bool Button(Rect rect, string text, GUIStyle style)
		{
			return GUIMgr.Button(rect, text, style, 0.8f);
		}
		public static bool Button(Rect rect, string text, GUIStyle style, float alpha)
		{
			Color backgroundColor = GUI.backgroundColor;
			GUI.backgroundColor = new Color(1f, 1f, 1f, alpha);
			bool result = GUI.Button(rect, text, style);
			GUI.backgroundColor = backgroundColor;
			return result;
		}
		public static string TextField(Rect rect, string val, GUIStyle style)
		{
			Color color = GUI.color;
			GUI.color = Color.black;
			string text = GUI.TextField(rect, val, style);
			GUI.color = color;
			bool flag = text != val;
			if (flag)
			{
				Input.ResetInputAxes();
			}
			return text;
		}
		public static void TextField_FocusCtrl(Rect rect, string text, GUIStyle style, string identifier, Func<string, string> onUnFocus)
		{
			string text2 = GUIMgr.forcusCtrlName + identifier;
			bool flag = GUIMgr.onFocusCtrl == text2;
			if (flag)
			{
				bool flag2 = GUIMgr.onUnfocusCtrl != string.Empty;
				if (flag2)
				{
					GUIMgr.focusTextTmp2 = GUIMgr.focusTextTmp;
				}
				GUIMgr.focusTextTmp = text;
				GUIMgr.onFocusCtrl = string.Empty;
			}
			bool flag3 = GUIMgr.onUnfocusCtrl == text2;
			if (flag3)
			{
				bool flag4 = GUIMgr.focusTextTmp2 == null;
				if (flag4)
				{
					GUIMgr.focusTextTmp = onUnFocus(GUIMgr.focusTextTmp);
				}
				else
				{
					GUIMgr.focusTextTmp2 = onUnFocus(GUIMgr.focusTextTmp2);
				}
				GUIMgr.onUnfocusCtrl = string.Empty;
			}
			Color color = GUI.color;
			GUI.color = Color.black;
			GUI.SetNextControlName(text2);
			bool flag5 = GUIMgr.nowFocus == text2;
			if (flag5)
			{
				GUIMgr.focusTextTmp = GUI.TextField(rect, GUIMgr.focusTextTmp, style);
			}
			else
			{
				GUI.TextField(rect, text, style);
			}
			GUI.color = color;
		}
		public static void GUIFuncDummy()
		{
			bool flag = GUIMgr.nowFocus != GUI.GetNameOfFocusedControl();
			if (flag)
			{
				GUIMgr.onFocusCtrl = GUI.GetNameOfFocusedControl();
				GUIMgr.onUnfocusCtrl = GUIMgr.nowFocus;
				GUIMgr.nowFocus = GUIMgr.onFocusCtrl;
			}
			bool flag2 = GUIMgr.isMouseDownOnWindow || (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl().StartsWith(GUIMgr.forcusCtrlName));
			if (flag2)
			{
				GUIMgr.onUnfocusCtrl = GUIMgr.nowFocus;
				Event.current.keyCode = 0;
				GUIMgr.isMouseDownOnWindow = false;
			}
			GUIMgr.focusTextTmp2 = null;
		}
		public static int StringToInt(string text, int fail)
		{
			float num;
			bool flag = float.TryParse(text, out num);
			int result;
			if (flag)
			{
				result = (int)num;
			}
			else
			{
				result = fail;
			}
			return result;
		}
		private ExpressionControl inst;
		public readonly int WINDOW_ID;
		public bool show;
		public bool showSave;
		public bool showProgram;
		public Rect rectMainWin;
		public Rect rectSave;
		public Rect rectProgram;
		public float mainWinHeight;
		public float programWinHeight;
		public bool mainWinFold;
		public bool programWinFold;
		private static bool isMouseDownOnWindow;
		public string sTmp = string.Empty;
		public Vector2 v2ScrollPos1;
		public Vector2 v2ScrollPos2;
		private static string focusTextTmp = string.Empty;
		private static string focusTextTmp2 = null;
		private static string nowFocus = string.Empty;
		private static string onFocusCtrl = string.Empty;
		private static string onUnfocusCtrl = string.Empty;
		private static readonly string forcusCtrlName = ExpressionControlPlugin.Name + "_FocusCtrl";
	}
}