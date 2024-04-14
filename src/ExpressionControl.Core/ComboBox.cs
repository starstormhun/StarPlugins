using System;
using UnityEngine;
namespace ExpressionControl
{
	public class ComboBox
	{
		private Rect rectItem { get; set; }
		public bool show { get; set; }
		private string[] sItems { get; set; }
		private GUIStyle gsSelectionGrid { get; set; }
		private GUIStyleState gssBlack { get; set; }
		private GUIStyleState gssWhite { get; set; }
		public Action<int> func { get; private set; }
		private bool bScroll { get; set; }
		public ComboBox(int iWIndowID, int fontSize)
		{
			this.WINDOW_ID = iWIndowID;
			this.gsSelectionGrid = new GUIStyle();
			this.gsSelectionGrid.fontSize = fontSize;
			this.gsSelectionGrid.padding = new RectOffset(4, 4, 4, 4);
			this.gssBlack = new GUIStyleState();
			this.gssBlack.textColor = Color.white;
			this.gssBlack.background = GUIUtl.texGray;
			this.gssWhite = new GUIStyleState();
			this.gssWhite.textColor = Color.black;
			this.gssWhite.background = Texture2D.whiteTexture;
			this.gsSelectionGrid.hover = this.gssBlack;
			this.gsSelectionGrid.active = this.gssBlack;
			this.gsSelectionGrid.focused = this.gssBlack;
		}
		public void Set(Rect mainWindow, Rect itemRect, string[] items, Action<int> func)
		{
			Rect r = new Rect(mainWindow.x + itemRect.x, mainWindow.y + itemRect.y + itemRect.height, itemRect.width, itemRect.height * (float)items.Length);
			this.Set(r, itemRect.height, items, func);
		}
		public void Set(Rect mainWindow, Rect subWindow, Rect itemRect, string[] items, Action<int> func)
		{
			Rect r = new Rect(mainWindow.x + subWindow.x + itemRect.x, mainWindow.y + subWindow.y + itemRect.y + itemRect.height, itemRect.width, itemRect.height * (float)items.Length);
			this.Set(r, itemRect.height, items, func);
		}
		public void Set(Rect r, float itemHeight, string[] items, Action<int> func)
		{
			bool flag = r.height > (float)Screen.height * 0.4f;
			if (flag)
			{
				this.rect = new Rect(r.x, r.y, r.width, (float)Screen.height * 0.4f);
				this.bScroll = true;
			}
			else
			{
				this.bScroll = false;
				this.rect = r;
			}
			bool flag2 = this.rect.y + this.rect.height > (float)Screen.height;
			if (flag2)
			{
				this.rect.y = this.rect.y - (this.rect.height + itemHeight);
			}
			this.sItems = items;
			bool bScroll = this.bScroll;
			if (bScroll)
			{
				this.rectItem = new Rect(0f, 0f, r.width - 16f, r.height);
			}
			else
			{
				this.rectItem = new Rect(0f, 0f, r.width, r.height);
			}
			this.func = func;
			this.show = true;
		}
		public void GuiFunc(int winId)
		{
			bool bScroll = this.bScroll;
			if (bScroll)
			{
				this.v2Scroll = GUI.BeginScrollView(new Rect(0f, 0f, this.rect.width, this.rect.height), this.v2Scroll, this.rectItem, false, false);
			}
			int num = GUI.SelectionGrid(this.rectItem, -1, this.sItems, 1, this.gsSelectionGrid);
			bool bScroll2 = this.bScroll;
			if (bScroll2)
			{
				GUI.EndScrollView();
			}
			bool flag = num >= 0;
			if (flag)
			{
				this.func(num);
				this.show = false;
				Input.ResetInputAxes();
			}
			bool anyMouseButtonDown = GUIUtl.GetAnyMouseButtonDown();
			if (anyMouseButtonDown)
			{
				Vector2 vector = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
				bool flag2 = !this.rect.Contains(vector);
				if (flag2)
				{
					this.show = false;
				}
			}
		}
		public readonly int WINDOW_ID;
		public Rect rect;
		private Vector2 v2Scroll;
	}
}