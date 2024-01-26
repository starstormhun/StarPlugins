using System.Collections.Generic;
using UnityEngine;

namespace LightSettings.Koikatu {
    public class UIExtender {
        public List<object> AddUIElements(PanelType panel, List<Controls> list) {
            var output = new List<object>();

            /*
            var img = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>()
            var spr = img.sprite
            var sprnew = UnityEngine.Sprite.Create(spr.texture, spr.textureRect, spr.pivot, 80f, 0, UnityEngine.SpriteMeshType.FullRect, new Vector4(3,3,3,141))
            img.sprite = sprnew
            img.type = UnityEngine.UI.Image.Type.Sliced
            */

            return output;
        }

        public class LabelControls : Controls {
            public LabelControls(string _label) { label = _label; }
        }

        public class ToggleControls : Controls {
            public ToggleControls(string _label) { label = _label; }
        }

        public class SliderControls : Controls {
            public SliderControls(string _label, Vector2 _bounds) {
                label = _label; bounds = _bounds;
                hasDefault = false; def = 0f;
            }
            public SliderControls(string _label, Vector2 _bounds, float _def) {
                label = _label; bounds = _bounds; def = _def;
                hasDefault = true;
            }
            public Vector2 bounds;
            public bool hasDefault;
            public float def;
        }

        public class ColorControls : Controls {
            public ColorControls(string _label) { label = _label; def = Color.black; hasDefault = false; }
            public ColorControls(string _label, Color _def) { label = _label; def = _def; hasDefault = true; }
            public Color def;
            public bool hasDefault;
        }

        public class TextControls : Controls {
            public TextControls(string _label) { label = _label; }
        }

        public class ChoiceControls : Controls {
            public ChoiceControls(string _label, ChoiceType _type) {
                label = _label; type = _type;
            }
            public ChoiceType type;

            public enum ChoiceType {
                OnOff,
                OnHalfOff,
                OnHalfHalfQOff,
                OffOneTwo,
                OffOneTwoTwo,
                NullOneTwoThree
            }
        }

        public class DropdownControls : Controls {
            public DropdownControls(string _label, List<string> _options) { label = _label; options = _options; }
            public List<string> options;
        }

        public abstract class Controls {
            public string label;
        }

        public enum PanelType {
            ChaState,
            Item,
            Light,
            Folder,
            Route,
            Camera,
            Config,
            Options,
            ChaLight,
            SceneEffects
        }
    }
}
