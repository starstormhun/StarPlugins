using System;
using ChaCustom;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace AccMover {
    internal abstract class SelectorBase : MonoBehaviour {
        protected int Slot { get; set; } = 0;
        protected Image Image { get; set; }

        protected abstract void HandleClick(bool newVal);
    }
    
    // Selector for accessory movement within an outfit
    internal class SelectorAccMove : SelectorBase {
        protected static Color defCol = Color.clear;
        protected static Color inSetCol = Color.clear;
        protected static int prevSel = 0;

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

            var tgl = transform.parent.gameObject.GetComponent<Toggle>();
            tgl.onValueChanged.AddListener(newVal => {
                HandleClick(newVal);
            });
        }

        private void Update() {
            if (Image != null) {
                if (AccMover.selectedCopyMove.Contains(Slot)) {
                    Image.color = AccMover._cvsAccessoryChange.selSrc == Slot ? defCol : inSetCol;
                } else {
                    Image.color = Color.clear;
                }
                Image.OnDisable();
                Image.OnEnable();
            }
        }

        protected override void HandleClick(bool newVal) {
            if (!newVal) {
                return;
            } else {
                StartCoroutine(UpdateSelection());
            }

            IEnumerator UpdateSelection() {
                yield return KKAPI.Utilities.CoroutineUtils.WaitForEndOfFrame;
                if (Event.current.shift) {
                    for (int i = Math.Min(Slot, prevSel); i <= Math.Max(Slot, prevSel); i++) {
                        if (!AccMover.selectedCopyMove.Contains(i)) AccMover.selectedCopyMove.Add(i);
                    }
                    AccMover._cvsAccessoryChange.selSrc = Slot;
                } else if (Event.current.control) {
                    if (AccMover.selectedCopyMove.Contains(Slot)) {
                        if (AccMover.selectedCopyMove.Count > 1) {
                            AccMover.selectedCopyMove.Remove(Slot);
                            if (prevSel == Slot) {
                                var iter = AccMover.selectedCopyMove.GetEnumerator();
                                iter.MoveNext();
                                AccMover._cvsAccessoryChange.selSrc = iter.Current;
                            } else {
                                AccMover._cvsAccessoryChange.selSrc = prevSel;
                            }
                        }
                    } else {
                        if (!AccMover.selectedCopyMove.Contains(Slot)) AccMover.selectedCopyMove.Add(Slot);
                        AccMover._cvsAccessoryChange.selSrc = Slot;
                    }
                } else {
                    AccMover.selectedCopyMove.Clear();
                    AccMover.selectedCopyMove.Add(Slot);
                }
                prevSel = AccMover._cvsAccessoryChange.selSrc;
            }
        }
    }

    // Selector for accessory movement in physical space
    internal class SelectorAccTransform : SelectorBase {
        protected static Color defCol = Color.clear;
        protected static Color inSetCol = Color.clear;
        protected static int prevSel = 1;

        private Toggle.ToggleEvent storedEvent;
        private Toggle.ToggleEvent selfEvent;
        private Toggle tgl;

        internal static bool isCopy = false;
        internal static bool isTransfer = false;

        private static readonly Color prevSelCol = new Color(1, 0, 0.5f);

        private void Start() {
            if (int.TryParse(gameObject.name.Replace("tglSlot", ""), out int parsedSlot)) {
                Slot = parsedSlot;
            }

            Image = gameObject.transform.GetChild(0).GetComponent<Image>();
            defCol = Image.color;
            inSetCol = Color.magenta;

            tgl = gameObject.GetComponent<Toggle>();
            storedEvent = tgl.onValueChanged;
            storedEvent.AddListener(newVal => {
                if (!newVal) return;
                AccMover.selectedTransform.Clear();
                AccMover.selectedTransform.Add(Slot);
                prevSel = Slot;
            });
            selfEvent = new Toggle.ToggleEvent();
            selfEvent.AddListener(newVal => {
                HandleClick(newVal);
            });
        }

        private void Update() {
            if ((Event.current.shift || Event.current.control) && tgl.onValueChanged != selfEvent && !isCopy && !isTransfer) {
                tgl.onValueChanged = selfEvent;
            } else if ((isCopy || isTransfer) || (!Event.current.shift && !Event.current.control && tgl.onValueChanged != storedEvent)) {
                tgl.onValueChanged = storedEvent;
            }

            if (Image != null) {
                Image.color = (
                    AccMover.selectedTransform.Contains(Slot) &&
                    Singleton<CustomBase>.Instance.selectSlot + 1 != Slot &&
                    !isCopy && !isTransfer
                ) ? (prevSel == Slot ? prevSelCol : inSetCol) : defCol;
                Image.OnDisable();
                Image.OnEnable();
            }
        }

        protected override void HandleClick(bool newVal) {
            if (!newVal) return;
            int selectSlot = Singleton<ChaCustom.CustomBase>.Instance.selectSlot;
            if (selectSlot == Slot - 1) return;
            transform.parent.GetChild(selectSlot).GetComponent<Toggle>().isOn = true;
            if (Event.current.control) {
                if (selectSlot != Slot - 1 && AccMover.selectedTransform.Contains(Slot)) {
                    AccMover.selectedTransform.Remove(Slot);
                    if (Slot == prevSel) {
                        prevSel = selectSlot;
                    }
                } else {
                    AccMover.selectedTransform.Add(Slot);
                    prevSel = Slot;
                }
            } else if (Event.current.shift) {
                for (int i = Math.Min(Slot, prevSel); i <= Math.Max(Slot, prevSel); i++) {
                    AccMover.selectedTransform.Add(i);
                }
                prevSel = Slot;
            }
        }
    }
}
