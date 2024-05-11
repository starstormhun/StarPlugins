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
        private const string backgroundImage = "iVBORw0KGgoAAAANSUhEUgAAAJoAAAEnCAYAAABR3FoIAAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kT1Iw0AcxV9TiyIVQYuIOGSoTnZREXEqVSyChdJWaNXB5NIvaNKQpLg4Cq4FBz8Wqw4uzro6uAqC4AeIs4OToouU+L+k0CLGg+N+vLv3uHsHCI0KU82uKKBqlpGKx8RsblXsfkUAAgYwhDmJmXoivZiB5/i6h4+vdxGe5X3uz9Gn5E0G+ETiKNMNi3iDeGbT0jnvE4dYSVKIz4knDLog8SPXZZffOBcdFnhmyMik5olDxGKxg+UOZiVDJZ4mDiuqRvlC1mWF8xZntVJjrXvyFwbz2kqa6zRHEccSEkhChIwayqjAQoRWjRQTKdqPefhHHH+SXDK5ymDkWEAVKiTHD/4Hv7s1C1OTblIwBgRebPtjDOjeBZp12/4+tu3mCeB/Bq60tr/aAGY/Sa+3tfAR0L8NXFy3NXkPuNwBhp90yZAcyU9TKBSA9zP6phwweAv0rrm9tfZx+gBkqKvlG+DgEBgvUva6x7t7Onv790yrvx/DqXLHF0sh1QAAAAZiS0dEAP8A/wD/oL2nkwAAAAlwSFlzAAAuIwAALiMBeKU/dgAAAAd0SU1FB+gFAwAfO5DH9GsAAAAZdEVYdENvbW1lbnQAQ3JlYXRlZCB3aXRoIEdJTVBXgQ4XAAADQ0lEQVR42u3cwWoaURSA4VNnkIG8gNs+XiEboS8hBNwU8nbpoovOIkVBAmK8pgs1oRRKbWe8kzvftxICkVx+zmg43A+LxeIhTpqbJqAr2+3b67pt24iImM1mTobOte23Y2jfH1fH+vYRVV05GTqT9k+xWh/7mjgOrkFoCA2hgdAQGkIDoSE0EBpCY0xqR0BfqkkTKZloeHQiNPAZjSFJh4iqakw0PDoRGggNoSE0EBpCA6EhNIQGQkNoIDSEhtBAaAgNhIbQEBoIDaGB0BAaQgOhITSEBkJDaCA0hIbQQGgIDYSG0BAaCA2hgdAQGkIDoSE0EBpCQ2ggNISG0EBoCA2EhtAQGggNoYHQEBpCA6EhNBAaQkNoIDSEBkJDaAgNhIbQGLnaEeSTIl31/aqoTDRMNHp2v1x+7PP3f5rPv/qMhi8DIDSEBkJDaAgNhIbQQGgIDaGB0BAaQoOe2UcbgCHsi5lomGj8n5w7/CYaQgOhITQQGkJDaCA0hIbQQGgIDYSG0BAadMw+WkbusAUTrTzusAWhITQQGkJDaCA0hAZCQ2gIDYSG0BAaCI0C2EcbAHfYgon2/rnDFoSG0EBoCA2hgdAQGggNoSE0EBpCA6EhNApiHy0jd9iCiVYed9iC0BAaCA2hITQQGkIDoSE0hAZCQ2gIDYRGAeyjDYA7bMFEe//cYQtCQ2ggNISG0EBoCA2EhtAQGggNoYHQEBoFsY+WkTtswUQrx3nCfFne9XqH7e38szts8WUAhIbQQGgIDaGB0BAaCA2hITQQGkJDaNA3+2gZnTds+77D9vw+OTdshZaRq0VBaAgNhIbQEBoIDaGB0BAaQgOhITQQGkKjIPbRMnK1KJho5bBhC0JDaCA0hIbQQGgIDYSG0BAaCA2hgdAQGkIDoSE0hAZCQ2ggNISG0EBoCA2EhtAQGggNocEv6ng5vtjtdzGtp6f69Me/O8Thtal4iajiWVF4dFLUo5PBPGr6myb554mJhok2ns8vkxH8jSA0hAZCQ2gIDYSG0EBoDCW05BQw0RAaCA2hMTIpktC4DvtoA2DDFrqaaJv1JiJSbNY/Yrc/yO8vPO/87/Ey26iftuuIaH77AX/SOIIL/QS+oWVeHiG58wAAAABJRU5ErkJggg==";
        private const string cookieButton = "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";
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
            bg.LoadImage(Convert.FromBase64String(backgroundImage));
            var newBg = GameObject.Instantiate(lightCtrl.transform.GetChild(0), container);
            newBg.localPosition = new Vector2(0, -180);
            var old = lightCtrl.transform.GetChild(0).GetComponent<Image>().sprite;
            var spr = Sprite.Create(bg, new Rect(0, 0, bg.width, bg.height), old.pivot, old.pixelsPerUnit);
            newBg.GetComponent<Image>().sprite = spr;
            newBg.GetComponent<RectTransform>().sizeDelta = new Vector2(190f / 154f * bg.width, 190f / 154f * bg.height);
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

            // Create all slider controls
            UnityAction<float> strengthCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.ShadowStrength, x);
            Transform sliderStrength = MakeSlider(container, "Shadow Strength", new Vector2(0, -276f), 0, 1, 1, strengthCallback);

            UnityAction<float> biasCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.Bias, x);
            Transform sliderBias = MakeSlider(container, "Shadow Bias", new Vector2(0, -320f), 0, 0.1f, 0.05f, biasCallback);

            UnityAction<float> normalBiasCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.NormalBias, x);
            Transform sliderNormalBias = MakeSlider(container, "Shadow Normal Bias", new Vector2(0, -365f), 0, 1, 0.4f, normalBiasCallback);

            UnityAction<float> nearPlaneCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.NearPlane, x);
            Transform sliderNearPlane = MakeSlider(container, "Shadow Near Plane", new Vector2(0, -410f), 0, 1, 0.2f, nearPlaneCallback);

            // Create render mode dropdown control
            var renderModeOptions = new List<string> { "Auto", "Force Pixel", "Force Vertex" };
            UnityAction<int> renderModeCallback = (x) => LightSettings.SetLightSetting(LightSettings.SettingType.RenderMode, renderModeOptions[x]);
            Transform dropRenderMode = MakeDropDown(container, "Light Render Mode", new Vector2(0, -455f), renderModeOptions, renderModeCallback);

            // Create culling mask toggles
            Transform cullMask = (new GameObject("Culling Mask")).transform;
            cullMask.SetParent(container);
            cullMask.localScale = Vector3.one;
            cullMask.localPosition = new Vector2(0, -500);
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
            cookie.LoadImage(Convert.FromBase64String(cookieButton));

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
            var cookieSizeSlider = MakeSlider(displayContainer, "Cookie Size", new Vector2(0, -207), 0.1f, 10, 10, cookieSizeCallback, false, true, true);

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

        private static Transform MakeSlider(Transform _parent, string _name, Vector2 _pos, float _sliderMin, float _sliderMax, float _default, UnityAction<float> _callback, bool _wholeNumbers = false, bool _allowOutOfBounds = true, bool needsBackground = false) {
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
            if (needsBackground) {
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
            }

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
