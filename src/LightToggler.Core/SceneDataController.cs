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
#if DEBUG
            LightToggler.LogThis("Scene saved!");
#endif
        }

        protected override void OnObjectVisibilityToggled(ObjectCtrlInfo _objectCtrlInfo, bool _visible) {
            GameObject toggledObject;
            switch (_objectCtrlInfo) {
                case OCIItem t1:
                    OCIItem OCI1 = (OCIItem)_objectCtrlInfo;
                    toggledObject = OCI1.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Item: " + toggledObject.name);
#endif
                    break;
                case OCIFolder t2:
                    OCIFolder OCI2 = (OCIFolder)_objectCtrlInfo;
                    toggledObject = OCI2.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Folder: " + toggledObject.name);
#endif
                    break;
                case OCILight t3:
                    OCILight OCI3 = (OCILight)_objectCtrlInfo;
                    toggledObject = OCI3.objectLight;
#if DEBUG
                    LightToggler.LogThis("Toggled Light: " + toggledObject.name);
#endif
                    break;
                case OCICamera t4:
                    OCICamera OCI4 = (OCICamera)_objectCtrlInfo;
                    toggledObject = OCI4.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Camera: " + toggledObject.name);
#endif
                    break;
                default:
                    return;
            }
            Extensions.SetAllLightsState(toggledObject, _visible);
        }
    }
}
