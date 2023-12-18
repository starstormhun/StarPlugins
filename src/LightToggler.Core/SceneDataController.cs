using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace LightToggler.Koikatu {
    internal class SceneDataController : SceneCustomFunctionController {
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
#if DEBUG
            Debug.Log("Scene loaded!");
#endif
        }

        protected override void OnSceneSave() {
            // When saving the scene, all lights need to be ON, otherwise ones not toggled off will be incorrectly loaded as off if the plugin is disabled / uninstalled
            if (LightToggler.IsEnabled.Value) {
#if KKS
                GameObject root = Manager.Scene.commonSpace;
#else
                GameObject root = Singleton<Manager.Scene>.Instance.commonSpace;
#endif
                root.SetAllLightsState(true);
            }
#if DEBUG
            Debug.Log("Scene saved!");
#endif
        }

        protected override void OnObjectVisibilityToggled(ObjectCtrlInfo _objectCtrlInfo, bool _visible) {
            if (LightToggler.IsEnabled.Value) {
                GameObject toggledObject = _objectCtrlInfo.GetObject();
                toggledObject.SetAllLightsState(_visible);
            }
        }
    }
}
