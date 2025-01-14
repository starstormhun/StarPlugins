using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Illusion.Extensions;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

[assembly: System.Reflection.AssemblyFileVersion(LightSettings.Koikatu.LightSettings.Version)]

namespace LightSettings.Koikatu {
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(KK_Plugins.Autosave.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess(KKAPI.KoikatuAPI.StudioProcessName)]
    [BepInPlugin(GUID, "Light Settings", Version)]
    /// <info>
    /// Plugin structure thanks to Keelhauled
    /// </info>
    public partial class LightSettings : BaseUnityPlugin {
        public static LightSettings Instance { get; private set; }

        public const string GUID = "starstorm.lightsettings";
        public const string Version = "1.2.0." + BuildNumber.Version;

        internal static Dictionary<string, byte[]> cookieDict = new Dictionary<string, byte[]>();
        internal static Dictionary<string, Texture> cookieDirectionalDict = new Dictionary<string, Texture>();
        internal static Dictionary<string, Texture> cookieSpotDict = new Dictionary<string, Texture>();
        internal static Dictionary<string, Cubemap> cookiePointDict = new Dictionary<string, Cubemap>();

        internal static ManualLogSource logger;
        internal static int hello = 0;

        internal static int charaLightSetCountDown = 0;
        private static string fileToRead = "";

        public ConfigEntry<bool> IsDebug { get; private set; }
        public ConfigEntry<bool> Enabled { get; private set; }
        public ConfigEntry<bool> ControlMapLights { get; private set; }
        public ConfigEntry<string> CharaLightToggleType { get; private set; }
        public ConfigEntry<int> MaxShadowResDirectional { get; private set; }
        public ConfigEntry<int> MaxShadowResSpot { get; private set; }
        public ConfigEntry<int> MaxShadowResPoint { get; private set; }

        private void Awake() {
            Instance = this;

            IsDebug = Config.Bind("0. Advanced", "Logging", false, new ConfigDescription("Enable verbose logging for debugging purposes", null, new KKAPI.Utilities.ConfigurationManagerAttributes { IsAdvanced = true }));
            Enabled = Config.Bind("1. General", "Enable plugin", true, new ConfigDescription("Enable/disable the plugin entirely. You need to save/reload the scene after changing this. Changes take effect on Studio restart.", null, new ConfigurationManagerAttributes { Order = 10 }));
            ControlMapLights = Config.Bind("1. General", "Control Map Lights", false, "Enable unified control over map lights. This causes issues in maps with multiple lights. Changes take effect on Studio restart.");

            CharaLightToggleType = Config.Bind("1. General", "Character light toggle", "Cramped", new ConfigDescription("How the character light on/off toggle will be handled. Changes take effect on Studio restart.", new AcceptableValueList<string>(new string[] { "None", "Cramped", "Below Vanilla" }), new ConfigurationManagerAttributes { Order = 5 }));
            
            MaxShadowResDirectional = Config.Bind("2. Default Shadow Resolutions", "Directional", 4096, new ConfigDescription("Set the shadow resolution of directional lights to this value if they spawn with automatic resolution. Set to -1 to disable.", new AcceptableValueList<int>(new int[] { -1, 512, 1024, 2048, 4096, 8192, 16384 }), new ConfigurationManagerAttributes { Order = 2 }));
            MaxShadowResSpot = Config.Bind("2. Default Shadow Resolutions", "Spot", 2048, new ConfigDescription("Set the shadow resolution of spot lights to this value if they spawn with automatic resolution. Set to -1 to disable.", new AcceptableValueList<int>(new int[] { -1, 512, 1024, 2048, 4096, 8192, 16384 }), new ConfigurationManagerAttributes { Order = 1 }));
            MaxShadowResPoint = Config.Bind("2. Default Shadow Resolutions", "Point", 1024, new ConfigDescription("Set the shadow resolution of point lights to this value if they spawn with automatic resolution. Set to -1 to disable.", new AcceptableValueList<int>(new int[] { -1, 512, 1024, 2048, 4096, 8192, 16384 }), new ConfigurationManagerAttributes { Order = 0 }));

            Log.SetLogSource(Logger);
            logger = Logger;

            if (Enabled.Value) {
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => UIHandler.Init();
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => charaLightSetCountDown = 5;
                KKAPI.Studio.StudioAPI.StudioLoadedChanged += (x, y) => {
                    var charaLight = Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true);
                    SceneDataController.charaLightData = new LightSaveData {
                        ObjectId = SceneDataController.chaLightID,
                        state = charaLight.enabled,
                        shadows = charaLight.shadows,
                        shadowResolution = charaLight.shadowResolution,
                        shadowStrength = charaLight.shadowStrength,
                        shadowBias = charaLight.shadowBias,
                        shadowNormalBias = charaLight.shadowNormalBias,
                        shadowNearPlane = charaLight.shadowNearPlane,
                        renderMode = charaLight.renderMode,
                        cullingMask = charaLight.cullingMask | (1 << 28),
                    };
                    SetMaxShadowRes(charaLight);
                };
                StudioSaveLoadApi.RegisterExtraBehaviour<SceneDataController>(SceneDataController.SaveID);
                StudioSaveLoadApi.SceneSave += (x, y) => charaLightSetCountDown = 5;
                HookPatch.Init();
            }

            if (IsDebug.Value) Log.Info($"Plugin {GUID} has awoken!");
        }

        private void Update() {
            // Try making chara toggle until it's made
            if (!UIHandler.charaToggleMade && Enabled.Value) {
                UIHandler.MakeCharaToggle();
            }

            // Load chara light data
            if (charaLightSetCountDown > 0) {
                charaLightSetCountDown--;
                if (charaLightSetCountDown == 0) {
                    if (IsDebug.Value) Logger.LogInfo("Loading character light settings...");
                    var charaLight = Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true);
                    SceneDataController.SetLoadedData(SceneDataController.charaLightData, new List<Light> { charaLight }, true);
                    UIHandler.SyncGUI(UIHandler.containerChara, charaLight);
                }
            }

            // Load cookie
            if (fileToRead != "") {
                if (Instance.IsDebug.Value) logger.LogInfo("Loading file...");
                var lights = GetCurrentLights(SettingType.None, "", out bool isChaLight);
                if (lights.Count > 0) {
                    var data = System.IO.File.ReadAllBytes(fileToRead);
                    fileToRead = "";

                    foreach (Light light in lights) {
                        if (Instance.IsDebug.Value) logger.LogInfo($"Setting up light {light.name}");
                        Texture cookie = LightCookieFromBytes(data, light);
                        light.cookie = cookie;
                    }
                    if (isChaLight) {
                        SceneDataController.charaLightData.cookieHash = GetHashSHA1(data);
                    }
                    UIHandler.DisplayCookie(LightCookieFromBytes(data, lights[0]));
                }
            }
        }

        internal static void Hello() {
            logger.LogInfo($"Hello {++hello}!");
        }

        internal static void ChaLightToggle(bool state) {
            if (UIHandler.syncing) return;
            if (Instance.IsDebug.Value) logger.LogInfo($"Character light {(state ? "enabled" : "disabled")}!");
            Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true).enabled = state;
            SceneDataController.charaLightData.state = state;
        }

        internal static void SetLightSetting<T>(SettingType _type, T _value, Light lightToModify = null) {
            if (Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.isUpdateInfo) return;
            if (UIHandler.syncing) return;

            bool isChaLight;
            var lights = new List<Light>();

            if (lightToModify == null) {
                lights = GetCurrentLights(_type, _value, out isChaLight);
            } else {
                lights.Add(lightToModify);
                isChaLight = lightToModify == Singleton<Studio.Studio>.Instance.gameObject.GetComponentInChildren<Light>(true);
            }

            foreach (Light light in lights) {
                switch (_type) {
                    case SettingType.ShadowType:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow type set to {_value}");
                        light.shadows = EnumParser<LightShadows>((_value as string));
                        if (isChaLight) SceneDataController.charaLightData.shadows = light.shadows;
                        break;
                    case SettingType.Resolution:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow resolution set to {_value}");
                        light.shadowResolution = EnumParser<UnityEngine.Rendering.LightShadowResolution>((_value as string));
                        if (isChaLight) SceneDataController.charaLightData.shadowResolution = light.shadowResolution;
                        break;
                    case SettingType.CustomResolution:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow custom resolution set to {_value}");
                        light.shadowCustomResolution = int.Parse(_value as string);
                        if (isChaLight) SceneDataController.charaLightData.shadowCustomResolution = light.shadowCustomResolution;
                        break;
                    case SettingType.ShadowStrength:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow strength set to {_value}");
                        if (_value is float strVal) light.shadowStrength = strVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowStrength = light.shadowStrength;
                        break;
                    case SettingType.Bias:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow bias set to {_value}");
                        if (_value is float biasVal) light.shadowBias = biasVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowBias = light.shadowBias;
                        break;
                    case SettingType.NormalBias:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow normal bias set to {_value}");
                        if (_value is float normBiasVal) light.shadowNormalBias = normBiasVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowNormalBias = light.shadowNormalBias;
                        break;
                    case SettingType.NearPlane:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Shadow near plane set to {_value}");
                        if (_value is float nearPlaneVal) light.shadowNearPlane = nearPlaneVal;
                        if (isChaLight) SceneDataController.charaLightData.shadowNearPlane = light.shadowNearPlane;
                        break;
                    case SettingType.RenderMode:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light render mode set to {_value}");
                        light.renderMode = EnumParser<LightRenderMode>((_value as string));
                        if (isChaLight) SceneDataController.charaLightData.renderMode = light.renderMode;
                        break;
                    case SettingType.CullingMask:
                        if (_value is int maskVal) {
                            if (Instance.IsDebug.Value) logger.LogInfo($"Light culling mask set to {light.cullingMask}");
                            if ((light.cullingMask & maskVal) == 0) light.cullingMask |= maskVal;
                            else light.cullingMask &= ~maskVal;
                            if (isChaLight) SceneDataController.charaLightData.cullingMask = light.cullingMask;
                        }
                        break;
                    case SettingType.CookieSize:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Cookie size set to {_value}");
                        if (_value is float cookieSizeVal) light.cookieSize = cookieSizeVal;
                        if (isChaLight) SceneDataController.charaLightData.cookieSize = light.cookieSize;
                        break;

                    // These are exclusive to lights attached to items, since those controls needed to be recreated
                    case SettingType.LightStrength:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light intensity set to {_value}");
                        if (_value is float intensityVal) light.intensity = intensityVal;
                        break;
                    case SettingType.LightRange:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light range set to {_value}");
                        if (_value is float rangeVal) light.range = rangeVal;
                        break;
                    case SettingType.SpotAngle:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Spot angle set to {_value}");
                        if (_value is float spotRangeVal) light.spotAngle = spotRangeVal;
                        break;
                    case SettingType.Color:
                        if (Instance.IsDebug.Value) logger.LogInfo($"Light color set to {_value}");
                        if (_value is Color colVal) light.color = colVal;
                        break;
                    case SettingType.State:
                        if (_value is bool stateVal) {
                            if (Instance.IsDebug.Value) logger.LogInfo($"Light {(stateVal ? "enabled" : "disabled")}!");
                            light.enabled = stateVal;
                        }
                        break;
                }
            }
        }

        internal static void SetCookie(bool on) {
            var lights = GetCurrentLights(SettingType.None, "", out bool isChaLight);

            if (on) {
                if (Instance.IsDebug.Value) logger.LogInfo("Opening file dialog...");
                string filter = "Images (*.png;.jpg)|*.png;*.jpg|All files|*.*";
                OpenFileDialog.Show(OnFileAccept, "Open image", Application.dataPath, filter, "png");
            } else {
                foreach (var light in lights) {
                    light.cookie = null;
                }
                if (isChaLight) SceneDataController.charaLightData.cookieHash = "";
            }

            void OnFileAccept(string[] strings) {
                if (strings == null || strings.Length == 0 || strings[0].IsNullOrEmpty()) {
                    return;
                }
                if (Instance.IsDebug.Value) logger.LogInfo("File chosen!");
                fileToRead = strings[0];
            }
        }

        internal static List<Light> GetCurrentLights<T>(SettingType _type, T _value, out bool isChaLight) {
            List<Light> lights = new List<Light>();
            isChaLight = false;
            if (Studio.Studio.Instance.manipulatePanelCtrl.lightPanelInfo.mpLightCtrl.gameObject.activeSelf) {
                var list = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                if (list.Count == 0) return null;
                lights.Add((list[0] as OCILight).light);
            } else if (Studio.Studio.Instance.manipulatePanelCtrl.itemPanelInfo.mpItemCtrl.gameObject.activeSelf) {
                var list = KKAPI.Studio.StudioAPI.GetSelectedObjects().ToList();
                if (list.Count == 0) return null;
                lights = (list[0] as OCIItem).objectItem.GetComponentsInChildren<Light>(true).ToList();
                if (_type == SettingType.State && list[0] is OCIItem ociItem && _value is bool stateVal) {
                    if (stateVal) {
                        if (SceneDataController.listDisabledLights.Remove(ociItem))
                            foreach (Light light in lights)
                                SceneDataController.dicDisabledLights.Remove(light);
                    } else {
                        if (!SceneDataController.listDisabledLights.Contains(ociItem)) {
                            SceneDataController.listDisabledLights.Add(ociItem);
                            foreach (Light light in lights) SceneDataController.dicDisabledLights.Add(light, ociItem);
                        }
                    }
                }
            } else if (Studio.Studio.Instance.transform.Find("Canvas Main Menu/01_Add").gameObject.activeSelf) {
                var map = Singleton<Map>.Instance.mapRoot;
                if (map == null) return null;
                lights = map.GetComponentsInChildren<Light>(true).ToList();
            } else {
                lights.Add(Studio.Studio.Instance.gameObject.GetComponentInChildren<Light>(true));
                isChaLight = true;
            }
            return lights;
        }

        private static T EnumParser<T>(string _val) {
            return (T)Enum.Parse(typeof(T), _val.Split(' ').Join((x) => x, ""), true);
        }

        private static string GetHashSHA1(byte[] data) {
            using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider()) {
                return sha1.ComputeHash(data).Select(x => x.ToString("X2")).ToList().Join(x => x, "");
            }
        }

        internal static Texture LightCookieFromBytes(byte[] bytes, Light light) {
            var hash = GetHashSHA1(bytes);
            Texture tex;

            if (!cookieDict.ContainsKey(hash)) {
                cookieDict.Add(hash, bytes);
            }

            if (!cookieDirectionalDict.TryGetValue(hash, out tex) || light.type != LightType.Directional) {
                if (!cookieSpotDict.TryGetValue(hash, out tex) || light.type != LightType.Spot) {
                    if (!cookiePointDict.TryGetValue(hash, out Cubemap cubeMap) || light.type != LightType.Point) {
                        var loadedTex = new Texture2D(1, 1);
                        loadedTex.LoadImage(bytes);

                        var newTex = new Texture2D(loadedTex.width, loadedTex.height, TextureFormat.ARGB32, false, false);
                        newTex.filterMode = FilterMode.Trilinear;
                        var pixels = loadedTex.GetPixels();
                        var min_alpha = 1f;
                        foreach (Color pixel in pixels) if (pixel.a < min_alpha) min_alpha = pixel.a;
                        if (min_alpha > 0.99) {
                            if (Instance.IsDebug.Value) logger.LogInfo($"Converting brightness to alpha... (Minimum alpha was: {min_alpha})");
                            for (int i = 0; i< pixels.Count(); i++) {
                                float gray = 0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b;
                                pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, gray);
                            }
                        } else if (Instance.IsDebug.Value) logger.LogInfo($"Skipping alpha conversion! (Minimum alpha was: {min_alpha})");
                        newTex.SetPixels(pixels);
                        newTex.Apply();

                        if (light.type == LightType.Directional) {
                            if (Instance.IsDebug.Value) logger.LogInfo("Adding directional cookie...");
                            newTex.wrapMode = TextureWrapMode.Repeat;
                            cookieDirectionalDict.Add(hash, newTex);
                            tex = newTex;
                        } else if (light.type == LightType.Spot) {
                            if (Instance.IsDebug.Value) logger.LogInfo("Adding spot cookie...");
                            newTex.wrapMode = TextureWrapMode.Clamp;
                            cookieSpotDict.Add(hash, newTex);
                            tex = newTex;
                        } else if (light.type == LightType.Point) {
                            if (Instance.IsDebug.Value) logger.LogInfo("Adding point cookie...");
                            cubeMap = CubemapFromTexture2D(newTex);
                            cubeMap.wrapMode = TextureWrapMode.Repeat;
                            cookiePointDict.Add(hash, cubeMap);
                            tex = cubeMap;
                        }
                    } else {
                        tex = cubeMap;
                    }
                }
            }
            return tex;
        }

        internal static Cubemap CubemapFromTexture2D(Texture2D tex) {
            int w = tex.width / 4;
            Cubemap cubeMap = new Cubemap(w, TextureFormat.ARGB32, false);
            cubeMap.SetPixels(tex.GetPixels(0, w, w, w), CubemapFace.NegativeX);
            cubeMap.SetPixels(tex.GetPixels(w, 2 * w, w, w), CubemapFace.NegativeY);
            cubeMap.SetPixels(tex.GetPixels(w, w, w, w), CubemapFace.PositiveZ);
            cubeMap.SetPixels(tex.GetPixels(w, 0, w, w), CubemapFace.PositiveY);
            cubeMap.SetPixels(tex.GetPixels(2 * w, w, w, w), CubemapFace.PositiveX);
            cubeMap.SetPixels(tex.GetPixels(3 * w, w, w, w), CubemapFace.NegativeZ);
            cubeMap.Apply();
            return cubeMap;
        }

        internal static void FilterCookies(List<string> hashes) {
            var keys = cookieDict.Keys.ToList();
            foreach (string key in keys) {
                if (!hashes.Contains(key)) {
                    if (cookieDict.ContainsKey(key)) cookieDict.Remove(key);
                    if (cookieDirectionalDict.ContainsKey(key)) cookieDirectionalDict.Remove(key);
                    if (cookiePointDict.ContainsKey(key)) cookiePointDict.Remove(key);
                }
            }
        }

        internal static void SetMaxShadowRes(Light light) {
            if (light.shadowCustomResolution == -1) {
                int defShadowRes = -1;
                switch (light.type) {
                    case LightType.Directional:
                        defShadowRes = Instance.MaxShadowResDirectional.Value; break;
                    case LightType.Spot:
                        defShadowRes = Instance.MaxShadowResSpot.Value; break;
                    case LightType.Point:
                        defShadowRes = Instance.MaxShadowResPoint.Value; break;
                }
                if (defShadowRes > -1) {
                    SetLightSetting(SettingType.CustomResolution, $"{defShadowRes}", light);
                }
            }
        }

        internal static List<Light> GetOwnLights(OCIItem ociItem) {
            var allLights = ociItem.objectItem.GetComponentsInChildren<Light>(true).ToList();
            if (Instance.IsDebug.Value) Log.Info($"Item '{ociItem.treeNodeObject.textName}' Light count: " + allLights.Count);
            if (allLights.Count == 0) return allLights;
            var children = new List<TreeNodeObject>(ociItem.treeNodeObject.child);
            TreeNodeObject child;
            while (children.Count > 0) {
                child = children.Pop();
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out var ociChild)) {
                    if (ociChild is OCILight childLight) {
                        var childLights = childLight.objectLight.GetComponentsInChildren<Light>(true).ToList();
                        if (Instance.IsDebug.Value) Log.Info($"Child lights '{childLight.treeNodeObject.textName}' count: " + childLights.Count);
                        foreach (var light in childLights) {
                            allLights.Remove(light);
                        }
                    }
                }
                children.AddRange(child.child);
            }
            return allLights;
        }

        internal enum SettingType {
            None,
            ShadowType,
            Resolution,
            CustomResolution,
            ShadowStrength,
            Bias,
            NormalBias,
            NearPlane,
            RenderMode,
            CullingMask,
            LightStrength,
            LightRange,
            SpotAngle,
            Color,
            State,
            CookieSize,
        }
    }
}
