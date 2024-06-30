using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Linq;
using HarmonyLib;
using Studio;

namespace LightSettings.Koikatu {
    internal static class UIHandler {
        private static int frameCounter = 0;

        internal static Transform containerLight = null;
        internal static Transform containerChara = null;
        internal static Transform containerItem = null;
        internal static Transform containerMap = null;
        internal static bool charaToggleMade = false;
        internal static bool syncing = false;
        internal static Sprite noCookie = null;
        internal static Image imageToSync = null;

        private static Transform itemPanelToggle;

        internal static void Init() {
            // Setup the item control panel for the extra settings then create settings
            MakeGUI(ref containerItem, SetupExtendedPanel(Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.transform, new Vector2(0, -30), new Vector2(280, 30)));
            containerItem.localPosition = new Vector2(0, -60);

            // Setup the maplight control panel
            MakeGUI(
                ref containerMap,
                SetupExtendedPanel(
                    Studio.Studio.Instance.transform.Find("Canvas Main Menu/01_Add/03_Map"),
#if KK
                    new Vector2(170, 0),
#else
                    new Vector2(305, 0),
#endif
                    Vector2.zero
                )
            );
            containerMap.localPosition = new Vector2(0, -60);
            
            // Create item light settings GUI
            MakeGUI(ref containerLight, Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform);

            // Create chara light settings GUI
            MakeGUI(ref containerChara, Studio.Studio.Instance.cameraLightCtrl.transform);
            containerChara.localPosition = new Vector2(0, -40);
        }

        private static Transform SetupExtendedPanel(Transform _parent, Vector2 _displacement, Vector2 _pos) {
            // Studio reference
            var itemCtrl = Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.transform;
            var lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;

            // Setup displacer and container
            Transform displacer = new GameObject("LightSettings Panel Container").transform;
            displacer.SetParent(_parent);
            displacer.localPosition = _displacement;
            displacer.localScale = Vector3.one;
            displacer.SetAsFirstSibling();

            Transform container = GameObject.Instantiate(lightCtrl, displacer);
            container.name = "LightSettings Panel";
            container.localPosition = _pos;
            container.localScale = Vector3.one;

            // Setup copied light panel element positions and background
            GameObject.DestroyImmediate(container.Find("Image Directional").gameObject);
            GameObject.DestroyImmediate(container.Find("Image Point").gameObject);
            GameObject.DestroyImmediate(container.Find("Spot Angle").gameObject);
            Image bg = container.Find("Image Spot").GetComponent<Image>();
            bg.gameObject.SetActive(true);
            Rect newRect = new Rect(bg.sprite.textureRect.x, bg.sprite.textureRect.y, bg.sprite.textureRect.width, bg.sprite.textureRect.height - 24);
            Sprite newBg = Sprite.Create(bg.sprite.texture, newRect, bg.sprite.pivot, bg.sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(0, 4, 0, 4));
            bg.sprite = newBg;
            bg.type = Image.Type.Sliced;
            bg.gameObject.GetComponent<RectTransform>().sizeDelta = bg.gameObject.GetComponent<RectTransform>().sizeDelta - new Vector2(0, 30);
            for (int i = 1; i < container.childCount; i++) {
                container.GetChild(i).localPosition += new Vector3(0, 30, 0);
            }

            // Setup copied panel controls
            GameObject.DestroyImmediate(container.GetComponent<MPLightCtrl>());
            GameObject.DestroyImmediate(container.Find("Toggle Target").GetChild(0).GetChild(0).gameObject);
            container.Find("Toggle Target").GetChild(0).GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            container.Find("Toggle Target").GetComponent<Toggle>().m_Interactable = false;
            GameObject.DestroyImmediate(container.Find("Range").gameObject);
            GameObject.DestroyImmediate(container.Find("Text Intensity").gameObject);
            GameObject.DestroyImmediate(container.Find("Slider Intensity").gameObject);
            GameObject.DestroyImmediate(container.Find("InputField Intensity").gameObject);
            GameObject.DestroyImmediate(container.Find("Button Intensity Default").gameObject);

            UnityAction<float> intensityCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.LightStrength, x);
            var sliderIntensity = MakeSlider(container, "Strength", new Vector2(0, -105), 0.1f, 2, 1, intensityCallback);

            UnityAction<float> rangeCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.LightRange, x);
            var sliderRange = MakeSlider(container, "Intensity", new Vector2(0, -150), 0.1f, 100, 15, rangeCallback);

            UnityAction<float> spotAngleCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.SpotAngle, x);
            var sliderSpotAngle = MakeSlider(container, "Spot Angle", new Vector2(0, -195), 0.1f, 179, 30, spotAngleCallback);

            GameObject.DestroyImmediate(container.Find("Toggle Visible").gameObject);
            UnityAction<bool> toggleCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.State, x);
            var toggleOnOff = MakeToggle(container, "Light On/Off", new Vector2(0, -30), new Vector2(110, 0), toggleCallback);

            Image colorImg = container.Find("Image Color Sample").GetComponent<Image>();
            Action<Color> colorCallback = (c) => {
                colorImg.color = c;
                LightSettings.SetLightSetting(LightSettings.SettingType.Color, c);
            };
            colorImg.GetComponent<Button>().onClick.AddListener(() => ColorPicker(colorImg.color, colorCallback));

            // Setup UI toggle Image and Sprite for adjustable background
            Image img = itemCtrl.Find("Image FK").GetComponent<Image>();
            Sprite newSpr = Sprite.Create(img.sprite.texture, img.sprite.textureRect, img.sprite.pivot, img.sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(0, 4, 0, 4));
            img.sprite = newSpr;
            img.type = Image.Type.Sliced;

            // Add new toggle
            itemPanelToggle = MakeToggle(img.transform, "Light controls", new Vector2(0, -53), new Vector2(80, 0), (x) => container.gameObject.SetActive(x));
            itemPanelToggle.gameObject.SetActive(false);
            itemPanelToggle.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(80, 0);

            return container;

            void ColorPicker(Color col, Action<Color> act) {
                var studio = Studio.Studio.Instance;
                studio.colorPalette.Setup("Lighting", col, act, false);
                studio.colorPalette.visible = true;
            }
        }

        internal static void MakeCharaToggle() {
            frameCounter++;
            var lockButton = GameObject.Find("StudioScene/Canvas Main Menu/04_System/03_Light/Chara Light Lock Btn");

            if ((frameCounter >= 3) && ((lockButton != null) || (frameCounter >= 30)) && !charaToggleMade) {
                charaToggleMade = true;

                Transform chaLightPanel = Studio.Studio.Instance.cameraLightCtrl.transform;

                if (LightSettings.Instance.CharaLightToggleType.Value == "Cramped") {
                    // Move existing controls
                    chaLightPanel.Find("Text Color").localPosition = new Vector2(0, -31);
                    chaLightPanel.Find("Image Color Sample").localPosition = new Vector2(70, -31);
                    chaLightPanel.Find("Toggle Shadow").localPosition = new Vector2(0, -72);
                    chaLightPanel.Find("Text Intensity").localPosition = new Vector2(0, -90);
                    if (lockButton != null) lockButton.transform.localPosition = new Vector2(157.5f, -70);

                    // Create toggle
                    var onOff = MakeToggle(chaLightPanel, " Light On/Off", new Vector2(0, -51), new Vector2(110, 0), (state) => LightSettings.ChaLightToggle(state));
                } else if (LightSettings.Instance.CharaLightToggleType.Value == "Below Vanilla") {
                    // Vanilla reference
                    Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;

                    // Create container GO
                    Transform container = new GameObject("LightSettings Character Light Toggle").transform;
                    container.SetParent(chaLightPanel);
                    container.localPosition = new Vector3(0, -220, 0);
                    container.localScale = Vector3.one;

                    // Add background
                    Sprite spr = Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.transform.Find("Image Shadow").GetComponent<Image>().sprite;
                    var newBg = GameObject.Instantiate(lightCtrl.transform.GetChild(0), container);
                    newBg.GetComponent<RectTransform>().sizeDelta = new Vector2(190, 30);
                    newBg.localPosition = Vector2.zero;
                    newBg.GetComponent<Image>().sprite = spr;

                    // Move settings down
                    containerChara.localPosition += new Vector3(0, -30, 0);

                    // Create toggle
                    var toggle = MakeToggle(container, " Light On/Off", new Vector2(0, -4), new Vector2(110, 0), (state) => LightSettings.ChaLightToggle(state));
                }
            }
        }

        private static void MakeGUI(ref Transform container, Transform parent) {
            // Setup studio element references
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;

            // Setup container
            container = new GameObject("LightSettings Container").transform;
            container.SetParent(parent);
            container.localPosition = Vector3.zero;
            container.localScale = Vector3.one;

            // Create background
            var bg = new Texture2D(1, 1);
            bg.LoadImage(Convert.FromBase64String(UITextureData.backgroundImage));
            var newBg = GameObject.Instantiate(lightCtrl.transform.GetChild(0), container);
            newBg.localPosition = new Vector2(0, -180);
            var old = lightCtrl.transform.GetChild(0).GetComponent<Image>().sprite;
            var spr = Sprite.Create(bg, new Rect(0, 0, bg.width, bg.height), old.pivot, old.pixelsPerUnit);
            newBg.GetComponent<Image>().sprite = spr;
            newBg.GetComponent<RectTransform>().sizeDelta = new Vector2(190f / 154f * bg.width, 1.4f * bg.height);
            newBg.name = "Background";

            // Create type / resolution dropdown controls
            var typeOptions = new List<string> { "None", "Soft", "Hard" };
            var toggleShadow = parent.Find("Toggle Shadow");
            UnityAction<int> typeCallback = (x) => {
                toggleShadow.GetComponent<Toggle>().isOn = x != 0;
                LightSettings.SetLightSetting(LightSettings.SettingType.Type, typeOptions[x]);
            };
            Transform dropType = MakeDropDown(container, "Shadow Type", new Vector2(0, -185f), typeOptions, typeCallback);
            toggleShadow.GetComponent<Toggle>().onValueChanged.AddListener((state) => {
                var dropdown = dropType.GetComponentInChildren<Dropdown>(true);
                if (state) { if (dropdown.value == 0) dropdown.value = 1; }
                else dropdown.value = 0;
            });

            var resolutionOptions = new List<string> { "From Quality Settings", "Low", "Medium", "High", "Very High" };
            UnityAction<int> resolutionCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.Resolution, resolutionOptions[x]);
            Transform dropResolution = MakeDropDown(container, "Shadow Resolution", new Vector2(0, -230f), resolutionOptions, resolutionCallback);

            var customResolutionOptions = new List<string> { "-1", "1024", "2048", "4096", "8192", "16384" };
            UnityAction<int> customResolutionCallback = (x) => {
                LightSettings.SetLightSetting(LightSettings.SettingType.CustomResolution, customResolutionOptions[x]);
                dropResolution.GetComponentInChildren<Dropdown>().interactable = x == 0;
            };
            Transform dropCustomResolution = MakeDropDown(container, "Shadow Custom Resolution", new Vector2(0, -275f), customResolutionOptions, customResolutionCallback);

            // Create all slider controls
            UnityAction<float> strengthCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.ShadowStrength, x);
            Transform sliderStrength = MakeSlider(container, "Shadow Strength", new Vector2(0, -320f), 0, 1, 1, strengthCallback);

            UnityAction<float> biasCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.Bias, x);
            Transform sliderBias = MakeSlider(container, "Shadow Bias", new Vector2(0, -365f), 0, 0.1f, 0.05f, biasCallback);

            UnityAction<float> normalBiasCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.NormalBias, x);
            Transform sliderNormalBias = MakeSlider(container, "Shadow Normal Bias", new Vector2(0, -410f), 0, 1, 0.4f, normalBiasCallback);

            UnityAction<float> nearPlaneCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.NearPlane, x);
            Transform sliderNearPlane = MakeSlider(container, "Shadow Near Plane", new Vector2(0, -455), 0, 1, 0.2f, nearPlaneCallback);

            // Create render mode dropdown control
            var renderModeOptions = new List<string> { "Auto", "Force Pixel", "Force Vertex" };
            UnityAction<int> renderModeCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.RenderMode, renderModeOptions[x]);
            Transform dropRenderMode = MakeDropDown(container, "Light Render Mode", new Vector2(0, -500), renderModeOptions, renderModeCallback);

            // Create culling mask toggles
            Transform cullMask = (new GameObject("Culling Mask")).transform;
            cullMask.SetParent(container);
            cullMask.localScale = Vector3.one;
            cullMask.localPosition = new Vector2(0, -545);
            MakeLabel(cullMask, "Culling Mask", Vector2.zero);
            UnityAction<bool> charaToggleCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.CullingMask, 1<<10);
            MakeToggle(cullMask, "Chara", new Vector2(10f, -20f), new Vector2(60f, 0), charaToggleCallback);
            UnityAction<bool> mapToggleCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.CullingMask, 1<<11);
            MakeToggle(cullMask, "Map", new Vector2(100f, -20f), new Vector2(60f, 0), mapToggleCallback);

            // Create cookie interface
            CreateCookie(container, new Vector2(190, -180));
        }

        private static Transform CreateCookie(Transform container, Vector2 pos, bool right = true) {
            var panel = new GameObject("Cookie Container").transform;
            panel.SetParent(container);
            panel.localPosition = pos;
            panel.localScale = Vector3.one;

            // Create and place GameObjects
            var cookieBtn = GameObject.Instantiate(container.GetChild(0), panel);
            cookieBtn.localPosition = new Vector3(right ? 0 : -38, 0, 0);
            cookieBtn.name = "Cookie UI Toggle";

            var displayContainer = new GameObject("Cookie Selector Container").transform;
            displayContainer.SetParent(panel);
            displayContainer.localPosition = new Vector3(right ? 0 : -190, -38, 0);
            displayContainer.localScale = Vector3.one;
            displayContainer.gameObject.SetActive(false);

            var displayBg = GameObject.Instantiate(container.GetChild(0), displayContainer);
            displayBg.localPosition = Vector3.zero;
            displayBg.name = "Background";

            var loadBtn = GameObject.Instantiate(container.GetChild(0), displayContainer);
            loadBtn.localPosition = new Vector3(5, -5, 0);
            loadBtn.name = "Load Cookie Button";

            var clearBtn = GameObject.Instantiate(container.GetChild(0), displayContainer);
            clearBtn.localPosition = new Vector3(98, -5, 0);
            clearBtn.name = "Clear Cookie Button";

            var cookieDisplay = GameObject.Instantiate(container.GetChild(0), displayContainer);
            cookieDisplay.localPosition = new Vector3(5, -27, 0);
            cookieDisplay.name = "Cookie Texture Display";

            // Setup appearance
            Texture2D cookie = new Texture2D(1, 1);
            cookie.LoadImage(Convert.FromBase64String(UITextureData.cookieButton));

            Sprite cookieSpr = Sprite.Create(cookie, new Rect(2, 2, 38, 38), new Vector2(21, 21), 100, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
            SetupImage(cookieBtn, cookieSpr, new Vector2(38, 38));

            Sprite clearSpr = Sprite.Create(cookie, new Rect(43, 7, 80, 16), new Vector2(83, 15), 100, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
            SetupImage(clearBtn, clearSpr, new Vector2(87, 18));

            Sprite loadSpr = Sprite.Create(cookie, new Rect(43, 25, 80, 16), new Vector2(83, 15), 100, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
            SetupImage(loadBtn, loadSpr, new Vector2(87, 18));

            if (noCookie == null) {
                Sprite oldSpr = Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.transform.Find("Image FK").GetComponent<Image>().sprite;
                noCookie = Sprite.Create(oldSpr.texture, oldSpr.textureRect, oldSpr.pivot, oldSpr.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(0, 4, 0, 4));
            }
            SetupImage(displayBg, noCookie, new Vector2(190, 253));
            var cookieDisplayImage = SetupImage(cookieDisplay, noCookie, new Vector2(180, 180));

            // Add functionality
            MakeClickable(cookieBtn, () => displayContainer.gameObject.SetActive(!displayContainer.gameObject.activeSelf));
            MakeClickable(loadBtn, () => {
                imageToSync = cookieDisplayImage;
                LightSettings.SetCookie(true);
            });
            MakeClickable(clearBtn, () => {
                DisplayCookie(null);
                LightSettings.SetCookie(false);
            });

            // Add size slider
            UnityAction<float> cookieSizeCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.CookieSize, x);
            var cookieSizeSlider = MakeSlider(displayContainer, "Cookie Size", new Vector2(0, -207), 0.1f, 10, 10, cookieSizeCallback);

            return panel;

            void MakeClickable(Transform go, Action onClick) {
                var button = go.gameObject.AddComponent<Button>();
                button.image = go.GetComponent<Image>();
                button.transition = Selectable.Transition.ColorTint;
                button.onClick.AddListener(() => onClick.Invoke());
            }

            Image SetupImage(Transform go, Sprite spr, Vector2 size) {
                go.GetComponent<Image>().sprite = spr;
                go.GetComponent<Image>().type = Image.Type.Sliced;
                go.GetComponent<RectTransform>().sizeDelta = size;
                return go.GetComponent<Image>();
            }
        }

        internal static void SyncGUI(Transform container, Light _light, bool syncExtra = false) {
            syncing = true;

            // Dropdowns
            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Syncing dropdowns...");
            var dropdown = container.Find("Shadow Type").GetComponentInChildren<Dropdown>(true);
            dropdown.value = FindOption(dropdown, _light.shadows.ToString());
            dropdown = container.Find("Shadow Resolution").GetComponentInChildren<Dropdown>(true);
            dropdown.value = FindOption(dropdown, _light.shadowResolution.ToString());
            dropdown = container.Find("Shadow Custom Resolution").GetComponentInChildren<Dropdown>(true);
            dropdown.value = FindOption(dropdown, _light.shadowCustomResolution.ToString());
            dropdown = container.Find("Light Render Mode").GetComponentInChildren<Dropdown>(true);
            dropdown.value = FindOption(dropdown, _light.renderMode.ToString());

            // Sliders
            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Syncing sliders...");
            container.Find("Shadow Strength").GetComponentInChildren<InputField>(true).text = _light.shadowStrength.ToString("0.000");
            container.Find("Shadow Bias").GetComponentInChildren<InputField>(true).text = _light.shadowBias.ToString("0.000");
            container.Find("Shadow Normal Bias").GetComponentInChildren<InputField>(true).text = _light.shadowNormalBias.ToString("0.000");
            container.Find("Shadow Near Plane").GetComponentInChildren<InputField>(true).text = _light.shadowNearPlane.ToString("0.000");

            // Culling Mask
            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Syncing culling mask...");
            if (_light.cullingMask == -1) _light.cullingMask = (1 << 10) | (1 << 11) + 23;
            container.Find("Culling Mask").GetChild(1).GetComponentInChildren<Toggle>(true).isOn = (_light.cullingMask & (1 << 10)) != 0;
            container.Find("Culling Mask").GetChild(2).GetComponentInChildren<Toggle>(true).isOn = (_light.cullingMask & (1 << 11)) != 0;

            // State / Color / Intensity / Range / Angle
            if (syncExtra) {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Syncing extra settings...");
                var parent = container.parent;
                parent.Find("Image Color Sample").GetComponentInChildren<Image>(true).color = _light.color;
                parent.Find("Light OnOff").GetComponentInChildren<Toggle>(true).isOn = _light.enabled;
                parent.Find("Strength").GetComponentInChildren<InputField>(true).text = _light.intensity.ToString("0.000");
                parent.Find("Intensity").GetComponentInChildren<InputField>(true).text = _light.range.ToString("0.000");
                parent.Find("Spot Angle").GetComponentInChildren<InputField>(true).text = _light.spotAngle.ToString("0.000");

                SetSliderActivity(parent.Find("Intensity"), _light.type != LightType.Directional);
                SetSliderActivity(parent.Find("Spot Angle"), _light.type == LightType.Spot);
            }

            // Cookie
            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Syncing cookie...");
            var imageTrans = container.Find("Cookie Container/Cookie Selector Container/Cookie Texture Display");
            if (imageTrans != null) {
                imageToSync = imageTrans.GetComponent<Image>();
                DisplayCookie(_light.cookie);
                imageTrans.parent.Find("Cookie Size").GetComponentInChildren<InputField>(true).text = _light.cookieSize.ToString("0.000");
                SetSliderActivity(imageTrans.parent.Find("Cookie Size"), _light.type == LightType.Directional);
            }

            syncing = false;

            int FindOption(Dropdown _dropdown, string value) {
                for (int i=0; i<_dropdown.options.Count; i++) {
                    if (_dropdown.options[i].text.Split(' ').Join((x) => x, "") == value) {
                        return i;
                    }
                }
                return 0;
            }

            void SetSliderActivity(Transform sliderRoot, bool state) {
                sliderRoot.GetComponentInChildren<Button>(true).interactable = state;
                sliderRoot.GetComponentInChildren<InputField>(true).interactable = state;
                sliderRoot.GetComponentInChildren<InputField>(true).transform.Find("Text").GetComponent<Text>().color = state ? Color.white : Color.gray;
                sliderRoot.GetComponentInChildren<Slider>(true).interactable = state;
            }
        }

        internal static void DisplayCookie(Texture tex) {
            if (imageToSync == null || imageToSync.sprite == null || noCookie == null) return;
            if (tex == null) {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Clearing cookie display!");
                imageToSync.sprite = noCookie;
                imageToSync.type = Image.Type.Sliced;
                return;
            }
            if (tex is Cubemap cubeMap) {
                if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Converting cubemap to flat texture...");
                int w = cubeMap.width;
                int h = cubeMap.height;
                var cubeTex = new Texture2D(4 * w, 4 * h, TextureFormat.ARGB32, false, false);

                cubeTex.SetPixels(0, h, w, h, cubeMap.GetPixels(CubemapFace.NegativeX));
                cubeTex.SetPixels(w, 2 * h, w, h, cubeMap.GetPixels(CubemapFace.NegativeY));
                cubeTex.SetPixels(w, h, w, h, cubeMap.GetPixels(CubemapFace.PositiveZ));
                cubeTex.SetPixels(w, 0, w, h, cubeMap.GetPixels(CubemapFace.PositiveY));
                cubeTex.SetPixels(2 * w, h, w, h, cubeMap.GetPixels(CubemapFace.PositiveX));
                cubeTex.SetPixels(3 * w, h, w, h, cubeMap.GetPixels(CubemapFace.NegativeZ));
                cubeTex.Apply();

                tex = cubeTex;
            }
            imageToSync.sprite = Sprite.Create((tex as Texture2D), new Rect(0, 0, tex.width, tex.height), noCookie.pivot, noCookie.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(0, 4, 0, 4));
            imageToSync.type = Image.Type.Simple;
            if (LightSettings.Instance.IsDebug.Value) LightSettings.logger.LogInfo("Cookie display set!");
        }

        internal static void TogglePanelToggler(bool state) {
            if (!state) containerItem.parent.gameObject.SetActive(false);
            if (state && itemPanelToggle.GetComponent<Toggle>().isOn) containerItem.parent.gameObject.SetActive(true);
            itemPanelToggle.gameObject.SetActive(state);
            itemPanelToggle.parent.GetComponent<LayoutElement>().minHeight = state ? 78 : 55;
        }

        internal static void SetMapGUI(bool state) {
            containerMap.parent.gameObject.SetActive(state);
        }

        private static Transform MakeDropDown(Transform _parent, string _name, Vector2 _pos, List<string> _options, UnityAction<int> _callback) {
            Transform lutCtrl = Studio.Studio.Instance.systemButtonCtrl.transform.Find("01_Screen Effect").GetChild(0).GetChild(0).GetChild(0).GetChild(1);
            Transform newDrop = (new GameObject(_name.Split('/').Join((x) => x, ""))).transform;

            newDrop.SetParent(_parent);
            newDrop.localScale = Vector3.one;
            newDrop.localPosition = _pos;

            var optionList = new List<Dropdown.OptionData>();
            foreach (string option in _options) {
                optionList.Add(new Dropdown.OptionData(option));
            }

            Transform text = GameObject.Instantiate(lutCtrl.GetChild(0), newDrop);
            text.localPosition = new Vector2(0, 0);
            text.GetComponent<TextMeshProUGUI>().text = _name;
            Transform dropDown = GameObject.Instantiate(lutCtrl.GetChild(1), newDrop);
            dropDown.localPosition = new Vector2(15, -20);
            dropDown.GetComponent<Dropdown>().AddOptions(optionList);
            dropDown.GetComponent<Dropdown>().onValueChanged.AddListener(_callback);

            return newDrop;
        }

        private static Transform MakeSlider(Transform _parent, string _name, Vector2 _pos, float _sliderMin, float _sliderMax, float _default, UnityAction<float> _callback, bool _wholeNumbers = false, bool _allowOutOfBounds = true) {
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;
            Transform newSlider = GameObject.Instantiate(lightCtrl.Find("Spot Angle"), _parent);

            newSlider.name = _name.Split('/').Join((x) => x, "");
            newSlider.localPosition = _pos;
            newSlider.GetChild(0).GetComponent<Text>().text = _name;
            newSlider.gameObject.SetActive(true);

            // Slider config
            newSlider.GetChild(1).GetComponent<Slider>().minValue = _sliderMin;
            newSlider.GetChild(1).GetComponent<Slider>().maxValue = _sliderMax;
            newSlider.GetChild(1).GetComponent<Slider>().wholeNumbers = _wholeNumbers;
            newSlider.GetChild(1).GetComponent<Slider>().onValueChanged.AddListener((f) => {
                if (float.TryParse(newSlider.GetComponentInChildren<InputField>(true).text, out float field)) {
                    if (
                        !Mathf.Approximately(field, f) && (
                            (Mathf.Clamp(field, _sliderMin, _sliderMax) == field || !_allowOutOfBounds) ||
                            newSlider.GetComponentInChildren<Slider>(true).currentSelectionState != Selectable.SelectionState.Normal
                        )
                    )
                        newSlider.GetComponentInChildren<InputField>(true).text = f.ToString("0.000");
                }
            });
            newSlider.GetChild(1).GetComponent<Slider>().m_Value = _default;

            // Field config
            UnityAction<string> fieldCallback = (s) => {
                if (float.TryParse(s, out float f)) {
                    _callback.Invoke(f);
                    float slider = newSlider.GetComponentInChildren<Slider>(true).value;
                    if (!Mathf.Approximately(slider, f)) newSlider.GetComponentInChildren<Slider>(true).value = Mathf.Clamp(f, _sliderMin, _sliderMax);
                }
            };
            newSlider.GetChild(2).GetComponent<InputField>().onValueChanged.AddListener(fieldCallback);
            newSlider.GetChild(2).GetComponent<InputField>().m_Text = _default.ToString("0.000");

            // Button config
            newSlider.GetChild(3).GetComponent<Button>().onClick.AddListener(() => {
                newSlider.GetComponentInChildren<InputField>(true).text = _default.ToString("0.000");
            });

            // Add background
            var newBg = GameObject.Instantiate(lightCtrl.GetChild(0).gameObject, newSlider).transform;
            newBg.SetAsFirstSibling();
            var tex = new Texture2D(4, 4);
            Color[] cols = new Color[16];
            for (int i = 0; i<16; i++) {
                cols[i] = new Color(0, 0, 0, 1);
            }
            tex.SetPixels(cols);
            tex.Apply();
            var spr = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(2, 2));
            newBg.GetComponent<Image>().sprite = spr;
            newBg.GetComponent<RectTransform>().localPosition = new Vector3(120, -20, 0);
            newBg.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 20);

            return newSlider;
        }

        private static Transform MakeToggle(Transform _parent, string _name, Vector2 _pos, Vector2 _toggleOffset, UnityAction<bool> _callback) {
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;
            Transform choice = GameObject.Instantiate(lightCtrl.GetChild(5), _parent);
            choice.name = _name.Split('/').Join((x)=>x, "");

            choice.localPosition = _pos;
            choice.GetChild(1).GetComponent<Text>().text = _name;
            choice.GetChild(0).localPosition = _toggleOffset;
            choice.GetComponent<Toggle>().onValueChanged.AddListener(_callback);

            return choice;
        }
    
        private static Transform MakeLabel(Transform _parent, string _name, Vector2 _pos) {
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;
            Transform newLabel = GameObject.Instantiate(lightCtrl.transform.GetChild(3), _parent);

            newLabel.name = _name.Split('/').Join((x) => x, "");
            newLabel.localPosition = _pos;
            newLabel.GetComponent<Text>().text = _name;

            return newLabel;
        }
    }
}
