using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Studio;

namespace LightSettings.Koikatu {
    internal static class UIHandler {
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
            bg.LoadImage(System.IO.File.ReadAllBytes("d:\\Downloads\\asdminden\\asdmods\\Repos\\StarPlugins\\src\\LightSettings.Core\\img\\bg.png"));
            var lightPanel = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/02_Light");
            var newBg = GameObject.Instantiate(lightPanel.transform.GetChild(0), container);
            newBg.localPosition = new Vector2(0, -180);
            var old = lightPanel.transform.GetChild(0).GetComponent<Image>().sprite;
            var spr = Sprite.Create(bg, new Rect(0, 0, bg.width, bg.height), old.pivot, old.pixelsPerUnit);
            newBg.GetComponent<Image>().sprite = spr;
            newBg.GetComponent<RectTransform>().sizeDelta = new Vector2(190f / 154f * bg.width, 190f / 154f * bg.height);
            newBg.name = "Background";

            // Create type / resolution dropdown controls
            Transform dropType = MakeDropDown(container, "Shadow Type", new Vector2(0, -185f));
            Transform dropResolution = MakeDropDown(container, "Shadow Resolution", new Vector2(0, -230f));

            // Create all slider controls
            Transform sliderStrength = MakeSlider(container, "Shadow Strength", new Vector2(0, -276f));
            Transform sliderBias = MakeSlider(container, "Shadow Bias", new Vector2(0, -320f));
            Transform sliderNormalBias = MakeSlider(container, "Shadow Normal Bias", new Vector2(0, -365f));
            Transform sliderNearPlane = MakeSlider(container, "Shadow Near Plane", new Vector2(0, -410f));

            // Create render mode dropdown control
            Transform dropRenderMode = MakeDropDown(container, "Light Render Mode", new Vector2(0, -455f));

            // Create culling mask toggles
            Transform cullMask = (new GameObject("Culling Mask")).transform;
            cullMask.SetParent(container);
            cullMask.localScale = Vector3.one;
            cullMask.localPosition = new Vector2(0, -500);
            GameObject.Instantiate(lightPanel.transform.GetChild(3), cullMask).localPosition = Vector2.zero;
            cullMask.GetChild(0).GetComponent<Text>().text = "Culling Mask";
            MakeChoice(cullMask, "Chara", new Vector2(10f, -20f), new Vector2(60f, 0));
            MakeChoice(cullMask, "Map", new Vector2(100f, -20f), new Vector2(60f, 0));
        }

        private static Transform MakeDropDown(Transform parent, string _name, Vector2 _pos) {
            Transform lutCtrl = Studio.Studio.Instance.systemButtonCtrl.transform.Find("01_Screen Effect").GetChild(0).GetChild(0).GetChild(0).GetChild(1);
            Transform newDrop = (new GameObject(_name)).transform;

            newDrop.SetParent(parent);
            newDrop.localScale = Vector3.one;
            newDrop.localPosition = _pos;

            Transform text = GameObject.Instantiate(lutCtrl.GetChild(0), newDrop);
            text.localPosition = new Vector2(0, 0);
            text.GetComponent<TextMeshProUGUI>().text = _name;
            Transform dropDown = GameObject.Instantiate(lutCtrl.GetChild(1), newDrop);
            dropDown.localPosition = new Vector2(15, -20);

            return newDrop;
        }

        private static Transform MakeSlider(Transform parent, string _name, Vector2 _pos) {
            Transform lightCtrl = Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.transform;
            Transform newSlider = GameObject.Instantiate(lightCtrl.Find("Spot Angle"), parent);

            newSlider.name = _name;
            newSlider.localPosition = _pos;
            newSlider.GetChild(0).GetComponent<Text>().text = _name;
            newSlider.gameObject.SetActive(true);

            return newSlider;
        }

        private static Transform MakeChoice(Transform parent, string _name, Vector2 _pos, Vector2 _toggleOffset) {
            Transform lightPanel = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/02_Light").transform;
            Transform choice = GameObject.Instantiate(lightPanel.GetChild(5), parent);

            choice.localPosition = _pos;
            choice.GetChild(1).GetComponent<Text>().text = _name;
            choice.GetChild(0).localPosition = _toggleOffset;

            return choice;
        }
    }
}
