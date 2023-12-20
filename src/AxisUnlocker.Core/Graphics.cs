using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AxisUnlocker.Koikatu {
    class SimpleDisplay : MonoBehaviour {
        public bool IsOn { get; private set; }
        public Vector2 Size { get; private set; }
        public Vector2 Pos { get; private set; }

        private string data = "";
        private string dispName = "";
        private Color dispColor = Color.white;
        private readonly GUIStyle textStyle;

        public SimpleDisplay() {
            this.Size = new Vector2(120, 40);
            this.Pos = Vector2.zero;
            textStyle = GUIStyle.none;
            textStyle.alignment = TextAnchor.UpperCenter;
            textStyle.normal.textColor = this.dispColor;
        }

        public void Show(bool _show) {
            this.IsOn = _show;
        }

        public void SetData(float _num) {
            this.data = _num.ToString("0.00");
        }

        public void SetName(string _name) {
            this.dispName = _name;
        }

        public void SetSize(Vector2 _newSize) {
            this.Size = _newSize;
        }

        public void SetPos(Vector2 _newPos) {
            this.Pos = _newPos;
        }

        private void OnGUI() {
            if (IsOn) {
                GUI.Box(new Rect(Pos.x, Pos.y, Size.x, Size.y), this.dispName);
                GUI.Label(new Rect(Pos.x, Pos.y + 25, Size.x, Size.y), this.data, textStyle);
            }
        }
    }
}
