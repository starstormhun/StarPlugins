using UnityEngine;
using Studio;

namespace AxisUnlocker.Koikatu {
    public static class Extensions {
        public static float TodB(this float val) {
            return Mathf.Log10(val) * 20;
        }
        public static float FromdB(this float val) {
            return Mathf.Pow(10, val / 20);
        }
    }

public abstract class ICLogValBase : OptionCtrl.InputCombination {
        public abstract bool GetToggle();

        new public float value {
            get {
                return this.slider.value;
            }
            set {
                this.slider.value = value;
                this.input.text = (GetToggle() ? value.FromdB() : value).ToString("0.00");
            }
        }
    }

    public class ICLogValSpeed : ICLogValBase {
        public override bool GetToggle() {
            return AxisUnlocker.UseLogMove.Value;
        }
    }

    public class ICLogValSize : ICLogValBase {
        public override bool GetToggle() {
            return AxisUnlocker.UseLogSize.Value;
        }
    }
}
