using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace LightToggler.Koikatu {
    internal class SceneDataController : SceneCustomFunctionController {
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems) {
#if DEBUG
            LightToggler.LogThis("Scene loaded!");
#endif
        }

        protected override void OnSceneSave() {
            if (LightToggler.IsEnabled.Value) {
                GameObject root = GameObject.Find("CommonSpace");
                root.SetAllLightsState(true);
            }
#if DEBUG
            LightToggler.LogThis("Scene saved!");
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
