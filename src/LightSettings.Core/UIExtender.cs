using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LightSettings.Koikatu {
    public static class UIExtender {
        private static bool initialised = false;
        private static Sprite refBg;
        private static Sprite refBlack;
        private static Dictionary<ControlType, object> refControls = new Dictionary<ControlType, object>();

        private enum ControlType {
            Label,
            Toggle,
            Slider,
            Color,
            Text,
            Choice,
            Dropdown
        }

        private static void Init() {
            if (!initialised) {


                // Dummy container for reference elements
                Transform mgr = GameObject.Find("BepInEx_Manager").transform;
                GameObject templates = new GameObject("UIElement Templates");
                templates.transform.SetParent(mgr);
                templates.SetActive(false);

                // Studio hierarchy object accesses grouped together for clarity
                Transform itemCtrl = Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.transform;
                Transform chaCtrl = Studio.Studio.Instance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.transform;
                Transform folderCtrl = Studio.Studio.Instance.manipulatePanelCtrl.folderPanelInfo.mpFolderCtrl.transform;

                // Setting up the reference background and black sprite
                for (int i = 0; i < itemCtrl.childCount; i++) {
                    if (itemCtrl.GetChild(i).name == "Image Shadow") {
                        Sprite spr = itemCtrl.GetChild(i).GetComponent<Image>().sprite;
                        refBg = Sprite.Create(spr.texture, spr.textureRect, spr.pivot, 80f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 141));
                        break;
                    }
                }
                Texture2D texBlack = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texBlack.SetPixel(0, 0, new Color(0, 0, 0, 1));
                texBlack.Apply();
                refBlack = Sprite.Create(texBlack, new Rect(0, 0, 1, 1), new Vector2(0, 0));

                // Setting up the reference control dicitonary to be cloned for custom UI elements
                // Label
                GameObject refLabel = Object.Instantiate(chaCtrl.Find("01_State/Viewport/Content/Cos"), templates.transform).gameObject;
                refLabel.name = "Template_Label";
                DelUnneeded(refLabel.transform, new List<string> { "Text" });
                refControls.Add(ControlType.Label, refLabel);

                // Toggle
                GameObject refToggle = Object.Instantiate(chaCtrl.Find("01_State/Viewport/Content/Etc/Son"), templates.transform).gameObject;
                refToggle.name = "Template_Toggle";
                refControls.Add(ControlType.Toggle, refToggle);

                // Slider
                GameObject refSlider = Object.Instantiate(itemCtrl.Find("Image Alpha"), templates.transform).gameObject;
                Object.DestroyImmediate(refSlider.GetComponent<Image>());
                refSlider.name = "Template_Slider";
                refControls.Add(ControlType.Toggle, refSlider);

                // Color
                GameObject refColor = Object.Instantiate(itemCtrl.GetChild(0), templates.transform).gameObject;
                Object.DestroyImmediate(refColor.GetComponent<Image>());
                Object.DestroyImmediate(refColor.transform.GetChild(refColor.transform.childCount - 1));
                refColor.name = "Template_Color";
                refControls.Add(ControlType.Color, refColor);

                // Text
                GameObject refText = Object.Instantiate(folderCtrl.GetChild(0), templates.transform).gameObject;
                Object.Instantiate(refText.transform.GetChild(2), refText.transform);
                for (int i = 0; i < refText.transform.childCount; i++) {
                    refText.transform.GetChild(i).localPosition
                }

                initialised = true;
            }

            void DelUnneeded(Transform tf, List<string> keep) {
                for (int k = tf.childCount - 1; k >= 0; k--) {
                    if (!keep.Contains(tf.GetChild(k).name)) Object.DestroyImmediate(tf.GetChild(k));
                }
            }
        }

        public static List<object> AddUIElements(PanelType panel, List<Controls> list) {
            if (!initialised) Init();

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
