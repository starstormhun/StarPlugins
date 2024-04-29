using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LightSettings.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "UI Extender", Version)]

    public class UIExtender : BaseUnityPlugin {
        public const string GUID = "starstorm.uiextender";
        public const string Version = "0.1.0." + BuildNumber.Version;

        internal static ManualLogSource logger;
        private static int hello = 0;
        private static bool initialised = false;
        private static Sprite refBg;
        private static Sprite refBlack;
        private static Dictionary<ControlType, object> refControls = new Dictionary<ControlType, object>();

        private void Awake() {
            logger = Logger;

            // KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x,y) => Init();

            Log.Info($"Plugin {GUID} has awoken!");
        }

        private static void Init() {
            if (!initialised) {
                // Dummy container for reference elements
                Transform mgr = GameObject.Find("BepInEx_Manager").transform;
                GameObject templates = Instantiate(GameObject.Find("StudioScene/Canvas Main Menu"), mgr);
                templates.name = "UIElement Templates";
                DelUnneeded(templates.transform, new List<string>());
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
                {
                    GameObject refLabel = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Cos"), templates.transform).gameObject;
                    refLabel.name = "Template_Label";
                    DelUnneeded(refLabel.transform, new List<string> { "Text" });
                    refControls.Add(ControlType.Label, refLabel);
                }
                /*
                // Toggle
                {
                    GameObject refToggle = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Etc/Son"), templates.transform).gameObject;
                    refToggle.name = "Template_Toggle";
                    refControls.Add(ControlType.Toggle, refToggle);
                }

                // Slider
                {
                    GameObject refSlider = Instantiate(itemCtrl.Find("Image Alpha"), templates.transform).gameObject;
                    Object.DestroyImmediate(refSlider.GetComponent<Image>());
                    refSlider.name = "Template_Slider";
                    refControls.Add(ControlType.Slider, refSlider);
                }

                // Color
                {
                    GameObject refColor = Instantiate(itemCtrl.GetChild(0), templates.transform).gameObject;
                    Object.DestroyImmediate(refColor.GetComponent<Image>());
                    Object.DestroyImmediate(refColor.transform.GetChild(refColor.transform.childCount - 1).gameObject);
                    refColor.name = "Template_Color";
                    refControls.Add(ControlType.Color, refColor);
                }

                // Text
                {
                    GameObject refText = Instantiate(folderCtrl.GetChild(0), templates.transform).gameObject;
                    GameObject refName = Instantiate(refText.transform.GetChild(1), refText.transform).gameObject;
                    refText.name = "Template_Text";
                    refName.name = "Name";
                    refName.GetComponent<RectTransform>().offsetMin = new Vector2(-35f, 0);
                    refName.GetComponent<RectTransform>().offsetMax = new Vector2(-78f, 0);
                    refText.transform.GetChild(1).GetComponent<RectTransform>().offsetMin = new Vector2(42f, 0);
                    refText.transform.GetChild(1).GetComponent<RectTransform>().offsetMin = new Vector2(105f, 0);
                    refControls.Add(ControlType.Text, refText);
                }

                // Choice
                {
                    var refChoice = new Dictionary<ChoiceControls.ChoiceType, GameObject>();
                    var refChoiceContainer = new GameObject("Template_Choice");
                    refChoiceContainer.transform.SetParent(templates.transform);

                    var refChoiceOnOff = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Clothing Details/Gloves"), refChoiceContainer.transform).gameObject;
                    refChoiceOnOff.name = "Choice_OnOff";
                    refChoice.Add(ChoiceControls.ChoiceType.OnOff, refChoiceOnOff);

                    var refChoiceOnHalfOff = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Clothing Details/Top"), refChoiceContainer.transform).gameObject;
                    refChoiceOnHalfOff.name = "Choice_OnHalfOff";
                    refChoice.Add(ChoiceControls.ChoiceType.OnHalfOff, refChoiceOnHalfOff);

                    var refChoiceOnHalfHalfOff = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Clothing Details/Shorts"), refChoiceContainer.transform).gameObject;
                    refChoiceOnHalfHalfOff.name = "Choice_OnHalfHalfOff";
                    refChoice.Add(ChoiceControls.ChoiceType.OnHalfHalfOff, refChoiceOnHalfHalfOff);

                    var refChoiceOffOneTwo = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Liquid"), refChoiceContainer.transform).gameObject;
                    DelUnneeded(refChoiceOffOneTwo.transform, new List<string> { "Text Face", "Button Face 1", "Button Face 2", "Button Face 3" });
                    refChoiceOffOneTwo.name = "Choice_OffOneTwo";
                    refChoice.Add(ChoiceControls.ChoiceType.OffOneTwo, refChoiceOffOneTwo);

                    var refChoiceOffOneTwoTwo = Instantiate(refChoiceOffOneTwo, refChoiceContainer.transform).gameObject;
                    var extraTwo = Instantiate(refChoiceOffOneTwoTwo.transform.GetChild(3), refChoiceOffOneTwoTwo.transform).gameObject;
                    extraTwo.transform.localPosition += new Vector3(30f, 0, 0);
                    refChoiceOffOneTwoTwo.name = "Choice_OffOneTwoTwo";
                    refChoice.Add(ChoiceControls.ChoiceType.OffOneTwoTwo, refChoiceOffOneTwoTwo);

                    var refChoiceNullOneTwoThree = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Etc/Tears"), refChoiceContainer.transform).gameObject;
                    refChoiceNullOneTwoThree.name = "Choice_NullOneTwoThree";
                    refChoice.Add(ChoiceControls.ChoiceType.NullOneTwoThree, refChoiceNullOneTwoThree);

                    refControls.Add(ControlType.Choice, refChoice);
                }

                // Dropdown
                {
                    var refDropdown = Instantiate(chaCtrl.Find("01_State/Viewport/Content/Cos"), templates.transform).gameObject;
                    DelUnneeded(refDropdown.transform, new List<string> { "Text Type", "Dropdown" });
                    refDropdown.name = "Template_Dropdown";
                    refControls.Add(ControlType.Dropdown, refDropdown);
                }
                */

                initialised = true;
            }

            void DelUnneeded(Transform tf, List<string> keep) {
                for (int k = tf.childCount - 1; k >= 0; k--) {
                    if (!keep.Contains(tf.GetChild(k).name)) DestroyImmediate(tf.GetChild(k).gameObject);
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
                OnHalfHalfOff,
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

        public enum ControlType {
            Label,
            Toggle,
            Slider,
            Color,
            Text,
            Choice,
            Dropdown
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

        private static void Hello() {
            logger.LogInfo($"Hello {++hello}!");
        }
    }
}
