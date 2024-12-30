using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace AccMover {
    internal class Selector : MonoBehaviour {
        private int Slot { get; set; } = 0;
        private Image Image { get; set; }

        private static Color defCol = Color.clear;
        private static Color inSetCol = Color.clear;

        private static Event e = null;
        private static bool shift = false;
        private static bool control = false;

        private static int prevSel = 0;

        private void Start() {
            if (int.TryParse(transform.parent?.parent?.name.Replace("kind", ""), out int parsedSlot)) {
                Slot = parsedSlot;
            }
            Image = gameObject.GetComponent<Image>();
            if (defCol == Color.clear) {
                defCol = Image.color;
                inSetCol = defCol;
                inSetCol.a /= 4;
            }
            transform.parent.gameObject.GetComponent<Toggle>().onValueChanged.AddListener(newVal => {
                if (!newVal) return;
                StartCoroutine(UpdateSelection());
                IEnumerator UpdateSelection() {
                    for (int i = 0; i < 1; i++) yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                    if (shift) {
                        if (Slot < prevSel) {
                            for (int i = Slot; i <= prevSel; i++) {
                                if (!AccMover.selected.Contains(i)) AccMover.selected.Add(i);
                            }
                        } else {
                            for (int i = prevSel; i <= Slot; i++) {
                                if (!AccMover.selected.Contains(i)) AccMover.selected.Add(i);
                            }
                        }
                        AccMover._cvsAccessoryChange.selSrc = Slot;
                    } else if (control) {
                        if (AccMover.selected.Contains(Slot)) {
                            if (AccMover.selected.Count > 1) {
                                AccMover.selected.Remove(Slot);
                                if (prevSel == Slot) {
                                    var iter = AccMover.selected.GetEnumerator();
                                    iter.MoveNext();
                                    AccMover._cvsAccessoryChange.selSrc = iter.Current;
                                } else {
                                    AccMover._cvsAccessoryChange.selSrc = prevSel;
                                }
                            }
                        } else {
                            if (!AccMover.selected.Contains(Slot)) AccMover.selected.Add(Slot);
                            AccMover._cvsAccessoryChange.selSrc = Slot;
                        }
                    } else {
                        AccMover.selected.Clear();
                        AccMover.selected.Add(Slot);
                    }
                    prevSel = AccMover._cvsAccessoryChange.selSrc;
                }
            });
        }

        private void Update() {
            if (Slot == 0) {
                e = Event.current;
                shift = e.shift;
                control = e.control;
            }
            if (Image != null) {
                if (AccMover.selected.Contains(Slot)) {
                    Image.color = AccMover._cvsAccessoryChange.selSrc == Slot ? defCol : inSetCol;
                } else {
                    Image.color = Color.clear;
                }
                Image.OnDisable();
                Image.OnEnable();
            }
        }
    }
}
