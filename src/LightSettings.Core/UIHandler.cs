using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Studio;
using System.Linq;
using HarmonyLib;

namespace LightSettings.Koikatu {
    internal static class UIHandler {
        internal const string backgroundImage = "iVBORw0KGgoAAAANSUhEUgAAAJoAAAEnCAYAAABR3FoIAAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kT1Iw0AcxV9TiyIVQYuIOGSoTnZREXEqVSyChdJWaNXB5NIvaNKQpLg4Cq4FBz8Wqw4uzro6uAqC4AeIs4OToouU+L+k0CLGg+N+vLv3uHsHCI0KU82uKKBqlpGKx8RsblXsfkUAAgYwhDmJmXoivZiB5/i6h4+vdxGe5X3uz9Gn5E0G+ETiKNMNi3iDeGbT0jnvE4dYSVKIz4knDLog8SPXZZffOBcdFnhmyMik5olDxGKxg+UOZiVDJZ4mDiuqRvlC1mWF8xZntVJjrXvyFwbz2kqa6zRHEccSEkhChIwayqjAQoRWjRQTKdqPefhHHH+SXDK5ymDkWEAVKiTHD/4Hv7s1C1OTblIwBgRebPtjDOjeBZp12/4+tu3mCeB/Bq60tr/aAGY/Sa+3tfAR0L8NXFy3NXkPuNwBhp90yZAcyU9TKBSA9zP6phwweAv0rrm9tfZx+gBkqKvlG+DgEBgvUva6x7t7Onv790yrvx/DqXLHF0sh1QAAAAZiS0dEAP8A/wD/oL2nkwAAAAlwSFlzAAAuIwAALiMBeKU/dgAAAAd0SU1FB+gFAwAfO5DH9GsAAAAZdEVYdENvbW1lbnQAQ3JlYXRlZCB3aXRoIEdJTVBXgQ4XAAADQ0lEQVR42u3cwWoaURSA4VNnkIG8gNs+XiEboS8hBNwU8nbpoovOIkVBAmK8pgs1oRRKbWe8kzvftxICkVx+zmg43A+LxeIhTpqbJqAr2+3b67pt24iImM1mTobOte23Y2jfH1fH+vYRVV05GTqT9k+xWh/7mjgOrkFoCA2hgdAQGkIDoSE0EBpCY0xqR0BfqkkTKZloeHQiNPAZjSFJh4iqakw0PDoRGggNoSE0EBpCA6EhNIQGQkNoIDSEhtBAaAgNhIbQEBoIDaGB0BAaQgOhITSEBkJDaCA0hIbQQGgIDYSG0BAaCA2hgdAQGkIDoSE0EBpCQ2ggNISG0EBoCA2EhtAQGggNoYHQEBpCA6EhNBAaQkNoIDSEBkJDaAgNhIbQGLnaEeSTIl31/aqoTDRMNHp2v1x+7PP3f5rPv/qMhi8DIDSEBkJDaAgNhIbQQGgIDaGB0BAaQoOe2UcbgCHsi5lomGj8n5w7/CYaQgOhITQQGkJDaCA0hIbQQGgIDYSG0BAadMw+WkbusAUTrTzusAWhITQQGkJDaCA0hAZCQ2gIDYSG0BAaCI0C2EcbAHfYgon2/rnDFoSG0EBoCA2hgdAQGggNoSE0EBpCA6EhNApiHy0jd9iCiVYed9iC0BAaCA2hITQQGkIDoSE0hAZCQ2gIDYRGAeyjDYA7bMFEe//cYQtCQ2ggNISG0EBoCA2EhtAQGggNoYHQEBoFsY+WkTtswUQrx3nCfFne9XqH7e38szts8WUAhIbQQGgIDaGB0BAaCA2hITQQGkJDaNA3+2gZnTds+77D9vw+OTdshZaRq0VBaAgNhIbQEBoIDaGB0BAaQgOhITQQGkKjIPbRMnK1KJho5bBhC0JDaCA0hIbQQGgIDYSG0BAaCA2hgdAQGkIDoSE0hAZCQ2ggNISG0EBoCA2EhtAQGggNocEv6ng5vtjtdzGtp6f69Me/O8Thtal4iajiWVF4dFLUo5PBPGr6myb554mJhok2ns8vkxH8jSA0hAZCQ2gIDYSG0EBoDCW05BQw0RAaCA2hMTIpktC4DvtoA2DDFrqaaJv1JiJSbNY/Yrc/yO8vPO/87/Ey26iftuuIaH77AX/SOIIL/QS+oWVeHiG58wAAAABJRU5ErkJggg==";
        internal static Transform container = null;

        internal static void Init() {
            // Setup studio element references
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;

            // Setup container
            container = new GameObject("LightSettings Container").transform;
            container.SetParent(lightCtrl);
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
            UnityAction<int> typeCallback = (x) => { LightSettings.Hello(); };
            Transform dropType = MakeDropDown(container, "Shadow Type", new Vector2(0, -185f), typeOptions, typeCallback);
            var resolutionOptions = new List<string> { "From Quality Settings", "Low", "Medium", "High", "Very High" };
            UnityAction<int> resolutionCallback = (x) => { LightSettings.Hello(); };
            Transform dropResolution = MakeDropDown(container, "Shadow Resolution", new Vector2(0, -230f), resolutionOptions, resolutionCallback);

            // Create all slider controls
            UnityAction<float> strengthCallback = (x) => { LightSettings.Hello(); };
            Transform sliderStrength = MakeSlider(container, "Shadow Strength", new Vector2(0, -276f), 0, 1, 1, strengthCallback);
            UnityAction<float> biasCallback = (x) => { LightSettings.Hello(); };
            Transform sliderBias = MakeSlider(container, "Shadow Bias", new Vector2(0, -320f), 0, 0.5f, 0.05f, biasCallback);
            UnityAction<float> normalBiasCallback = (x) => { LightSettings.Hello(); };
            Transform sliderNormalBias = MakeSlider(container, "Shadow Normal Bias", new Vector2(0, -365f), 0, 1, 0.4f, normalBiasCallback);
            UnityAction<float> nearPlaneCallback = (x) => { LightSettings.Hello(); };
            Transform sliderNearPlane = MakeSlider(container, "Shadow Near Plane", new Vector2(0, -410f), 0, 1, 0.2f, nearPlaneCallback);

            // Create render mode dropdown control
            var renderModeOptions = new List<string> { "Auto", "Force Pixel", "Force Vertex" };
            UnityAction<int> renderModeCallback = (x) => { LightSettings.Hello(); };
            Transform dropRenderMode = MakeDropDown(container, "Light Render Mode", new Vector2(0, -455f), renderModeOptions, renderModeCallback);

            // Create culling mask toggles
            Transform cullMask = (new GameObject("Culling Mask")).transform;
            cullMask.SetParent(container);
            cullMask.localScale = Vector3.one;
            cullMask.localPosition = new Vector2(0, -500);
            MakeLabel(cullMask, "Culling Mask", Vector2.zero);
            UnityAction<bool> charaToggleCallback = (x) => { LightSettings.Hello(); };
            MakeToggle(cullMask, "Chara", new Vector2(10f, -20f), new Vector2(60f, 0), charaToggleCallback);
            UnityAction<bool> mapToggleCallback = (x) => { LightSettings.Hello(); };
            MakeToggle(cullMask, "Map", new Vector2(100f, -20f), new Vector2(60f, 0), mapToggleCallback);
        }

        private static Transform MakeDropDown(Transform _parent, string _name, Vector2 _pos, List<string> _options, UnityAction<int> _callback) {
            Transform lutCtrl = Studio.Studio.Instance.systemButtonCtrl.transform.Find("01_Screen Effect").GetChild(0).GetChild(0).GetChild(0).GetChild(1);
            Transform newDrop = (new GameObject(_name)).transform;

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

            newSlider.name = _name;
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
                        newSlider.GetChild(2).GetComponent<InputField>().text = f.ToString("0.00");
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
            newSlider.GetChild(2).GetComponent<InputField>().m_Text = _default.ToString("0.00");

            // Button config
            newSlider.GetChild(3).GetComponent<Button>().onClick.AddListener(() => {
                newSlider.GetChild(2).GetComponent<InputField>().text = _default.ToString("0.00");
            });

            return newSlider;
        }

        private static Transform MakeToggle(Transform _parent, string _name, Vector2 _pos, Vector2 _toggleOffset, UnityAction<bool> _callback) {
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;
            Transform choice = GameObject.Instantiate(lightCtrl.GetChild(5), _parent);

            choice.localPosition = _pos;
            choice.GetChild(1).GetComponent<Text>().text = _name;
            choice.GetChild(0).localPosition = _toggleOffset;
            choice.GetComponent<Toggle>().onValueChanged.AddListener(_callback);

            return choice;
        }
    
        private static Transform MakeLabel(Transform _parent, string _name, Vector2 _pos) {
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;
            Transform newLabel = GameObject.Instantiate(lightCtrl.transform.GetChild(3), _parent);

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

        private static T EnumParser<T>(string _val) {
            return (T)Enum.Parse(typeof(T), _val.Split(' ').Join((x) => x, ""), true);
        }
    }
}
