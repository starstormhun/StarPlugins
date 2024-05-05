using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Linq;
using HarmonyLib;
using Studio;
using ChaCustom;
using KKAPI;
using static ADV.Info;

namespace LightSettings.Koikatu {
    internal static class UIHandler {
        private const string backgroundImage = "iVBORw0KGgoAAAANSUhEUgAAAJoAAAEnCAYAAABR3FoIAAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kT1Iw0AcxV9TiyIVQYuIOGSoTnZREXEqVSyChdJWaNXB5NIvaNKQpLg4Cq4FBz8Wqw4uzro6uAqC4AeIs4OToouU+L+k0CLGg+N+vLv3uHsHCI0KU82uKKBqlpGKx8RsblXsfkUAAgYwhDmJmXoivZiB5/i6h4+vdxGe5X3uz9Gn5E0G+ETiKNMNi3iDeGbT0jnvE4dYSVKIz4knDLog8SPXZZffOBcdFnhmyMik5olDxGKxg+UOZiVDJZ4mDiuqRvlC1mWF8xZntVJjrXvyFwbz2kqa6zRHEccSEkhChIwayqjAQoRWjRQTKdqPefhHHH+SXDK5ymDkWEAVKiTHD/4Hv7s1C1OTblIwBgRebPtjDOjeBZp12/4+tu3mCeB/Bq60tr/aAGY/Sa+3tfAR0L8NXFy3NXkPuNwBhp90yZAcyU9TKBSA9zP6phwweAv0rrm9tfZx+gBkqKvlG+DgEBgvUva6x7t7Onv790yrvx/DqXLHF0sh1QAAAAZiS0dEAP8A/wD/oL2nkwAAAAlwSFlzAAAuIwAALiMBeKU/dgAAAAd0SU1FB+gFAwAfO5DH9GsAAAAZdEVYdENvbW1lbnQAQ3JlYXRlZCB3aXRoIEdJTVBXgQ4XAAADQ0lEQVR42u3cwWoaURSA4VNnkIG8gNs+XiEboS8hBNwU8nbpoovOIkVBAmK8pgs1oRRKbWe8kzvftxICkVx+zmg43A+LxeIhTpqbJqAr2+3b67pt24iImM1mTobOte23Y2jfH1fH+vYRVV05GTqT9k+xWh/7mjgOrkFoCA2hgdAQGkIDoSE0EBpCY0xqR0BfqkkTKZloeHQiNPAZjSFJh4iqakw0PDoRGggNoSE0EBpCA6EhNIQGQkNoIDSEhtBAaAgNhIbQEBoIDaGB0BAaQgOhITSEBkJDaCA0hIbQQGgIDYSG0BAaCA2hgdAQGkIDoSE0EBpCQ2ggNISG0EBoCA2EhtAQGggNoYHQEBpCA6EhNBAaQkNoIDSEBkJDaAgNhIbQGLnaEeSTIl31/aqoTDRMNHp2v1x+7PP3f5rPv/qMhi8DIDSEBkJDaAgNhIbQQGgIDaGB0BAaQoOe2UcbgCHsi5lomGj8n5w7/CYaQgOhITQQGkJDaCA0hIbQQGgIDYSG0BAadMw+WkbusAUTrTzusAWhITQQGkJDaCA0hAZCQ2gIDYSG0BAaCI0C2EcbAHfYgon2/rnDFoSG0EBoCA2hgdAQGggNoSE0EBpCA6EhNApiHy0jd9iCiVYed9iC0BAaCA2hITQQGkIDoSE0hAZCQ2gIDYRGAeyjDYA7bMFEe//cYQtCQ2ggNISG0EBoCA2EhtAQGggNoYHQEBoFsY+WkTtswUQrx3nCfFne9XqH7e38szts8WUAhIbQQGgIDaGB0BAaCA2hITQQGkJDaNA3+2gZnTds+77D9vw+OTdshZaRq0VBaAgNhIbQEBoIDaGB0BAaQgOhITQQGkKjIPbRMnK1KJho5bBhC0JDaCA0hIbQQGgIDYSG0BAaCA2hgdAQGkIDoSE0hAZCQ2ggNISG0EBoCA2EhtAQGggNocEv6ng5vtjtdzGtp6f69Me/O8Thtal4iajiWVF4dFLUo5PBPGr6myb554mJhok2ns8vkxH8jSA0hAZCQ2gIDYSG0EBoDCW05BQw0RAaCA2hMTIpktC4DvtoA2DDFrqaaJv1JiJSbNY/Yrc/yO8vPO/87/Ey26iftuuIaH77AX/SOIIL/QS+oWVeHiG58wAAAABJRU5ErkJggg==";
        private static int frameCounter = 0;

        internal static Transform containerLight = null;
        internal static Transform containerChara = null;
        internal static Transform containerItem = null;
        internal static bool charaToggleMade = false;
        internal static bool syncing = false;

        private static Transform itemPanelToggle;

        internal static void Init() {
            // Setup the item control panel for the extra settings then create settings
            Transform itemLightSettingsPanel = SetupItemPanel();
            MakeGUI(ref containerItem, itemLightSettingsPanel);
            containerItem.localPosition = new Vector2(0, -20);

            // Create item light settings GUI
            MakeGUI(ref containerLight, Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform);

            // Create chara light settings GUI
            MakeGUI(ref containerChara, Studio.Studio.Instance.cameraLightCtrl.transform);
            containerChara.localPosition = new Vector2(0, -40);
        }

        private static Transform SetupItemPanel() {
            // Studio reference
            var itemCtrl = Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.transform;
            var lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;

            // Setup displacer and container
            Transform displacer = new GameObject("LightSettings Panel Container").transform;
            displacer.SetParent(itemCtrl);
            displacer.localPosition = new Vector2(0, -30);
            displacer.localScale = Vector3.one;
            displacer.SetAsFirstSibling();

            Transform container = GameObject.Instantiate(lightCtrl, displacer);
            container.name = "LightSettings Panel";
            container.localPosition = new Vector2(280, 30);
            container.localScale = Vector3.one;

            // Setup copied light panel element positions and background
            GameObject.DestroyImmediate(container.Find("Image Directional").gameObject);
            GameObject.DestroyImmediate(container.Find("Image Spot").gameObject);
            GameObject.DestroyImmediate(container.Find("Spot Angle").gameObject);
            container.Find("Range").gameObject.SetActive(true);
            Image bg = container.Find("Image Point").GetComponent<Image>();
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
        }

        internal static void SyncGUI(ref Transform container, Light _light, bool syncExtra = false) {
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

            // State / Color / Intensity / Range
            if (syncExtra) {
                var parent = container.parent;
                parent.Find("Image Color Sample").GetComponentInChildren<Image>(true).color = _light.color;
                parent.Find("Light OnOff").GetComponentInChildren<Toggle>(true).isOn = _light.enabled;
                parent.Find("Strength").GetComponentInChildren<InputField>(true).text = _light.intensity.ToString("0.000");
                parent.Find("Intensity").GetComponentInChildren<InputField>(true).text = _light.range.ToString("0.000");
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
        }

        internal static void TogglePanelToggler(bool state) {
            if (!state) containerItem.parent.gameObject.SetActive(false);
            if (state && itemPanelToggle.GetComponent<Toggle>().isOn) containerItem.parent.gameObject.SetActive(true);
            itemPanelToggle.gameObject.SetActive(state);
            itemPanelToggle.parent.GetComponent<LayoutElement>().minHeight = state ? 78 : 55;
        }

        private static Transform MakeDropDown(Transform _parent, string _name, Vector2 _pos, List<string> _options, UnityAction<int> _callback) {
            Transform lutCtrl = Studio.Studio.Instance.systemButtonCtrl.transform.Find("01_Screen Effect").GetChild(0).GetChild(0).GetChild(0).GetChild(1);
            Transform newDrop = (new GameObject(_name.Split('/').Join((x) => x, ""))).transform;

            newDrop.SetParent(_parent);
            newDrop.localScale = Vector3.one;
            newDrop.localPosition = _pos;

            Transform text = GameObject.Instantiate(lutCtrl.GetChild(0), newDrop);
            text.localPosition = new Vector2(0, 0);
            text.GetComponent<TextMeshProUGUI>().text = _name;
            Transform dropDown = GameObject.Instantiate(lutCtrl.GetChild(1), newDrop);
            dropDown.localPosition = new Vector2(15, -20);
            dropDown.GetComponent<Dropdown>().AddOptions(MakeOptions(_options));
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
                if (float.TryParse(newSlider.GetChild(2).GetComponent<InputField>().text, out float field)) {
                    if (
                        !Mathf.Approximately(field, f) && (
                            (Mathf.Clamp(field, _sliderMin, _sliderMax) == field || !_allowOutOfBounds) ||
                            newSlider.GetChild(1).GetComponent<Slider>().currentSelectionState != Selectable.SelectionState.Normal
                        )
                    )
                        newSlider.GetChild(2).GetComponent<InputField>().text = f.ToString("0.000");
                }
            });
            newSlider.GetChild(1).GetComponent<Slider>().m_Value = _default;

            // Field config
            UnityAction<string> fieldCallback = (s) => {
                if (float.TryParse(s, out float f)) {
                    _callback.Invoke(f);
                    float slider = newSlider.GetChild(1).GetComponent<Slider>().value;
                    if (!Mathf.Approximately(slider, f)) newSlider.GetChild(1).GetComponent<Slider>().value = Mathf.Clamp(f, _sliderMin, _sliderMax);
                }
            };
            newSlider.GetChild(2).GetComponent<InputField>().onValueChanged.AddListener(fieldCallback);
            newSlider.GetChild(2).GetComponent<InputField>().m_Text = _default.ToString("0.000");

            // Button config
            newSlider.GetChild(3).GetComponent<Button>().onClick.AddListener(() => {
                newSlider.GetChild(2).GetComponent<InputField>().text = _default.ToString("0.000");
            });

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

        private static List<Dropdown.OptionData> MakeOptions(List<string> options) {
            var optionList = new List<Dropdown.OptionData>();
            foreach (string option in options) {
                optionList.Add(new Dropdown.OptionData(option));
            }
            return optionList;
        }

        private static void ColorPicker(Color col, Action<Color> act) {
            var studio = Studio.Studio.Instance;
            studio.colorPalette.Setup("Lighting", col, act, false);
            studio.colorPalette.visible = true;
        }
    }
}
