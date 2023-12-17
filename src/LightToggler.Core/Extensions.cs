using UnityEngine;
using Studio;

namespace LightToggler.Koikatu {
    public static class Extensions {
        public static void SetAllLightsState(this GameObject _gameObject, bool _state) {
            foreach (Light childLight in _gameObject.GetComponentsInChildren<Light>()) {
                childLight.enabled = _state;
            }
            LightToggler.updateLightPanel = true;
        }

        public static GameObject GetObject(this ObjectCtrlInfo _objectCtrlInfo) {
            GameObject objectItem;
            switch (_objectCtrlInfo) {
                case OCIItem t1:
                    OCIItem OCI1 = (OCIItem)_objectCtrlInfo;
                    objectItem = OCI1.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Item: " + objectItem.name);
#endif
                    break;
                case OCIFolder t2:
                    OCIFolder OCI2 = (OCIFolder)_objectCtrlInfo;
                    objectItem = OCI2.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Folder: " + objectItem.name);
#endif
                    break;
                case OCILight t3:
                    OCILight OCI3 = (OCILight)_objectCtrlInfo;
                    objectItem = OCI3.objectLight;
#if DEBUG
                    LightToggler.LogThis("Toggled Light: " + objectItem.name);
#endif
                    break;
                case OCICamera t4:
                    OCICamera OCI4 = (OCICamera)_objectCtrlInfo;
                    objectItem = OCI4.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Camera: " + objectItem.name);
#endif
                    break;
                case OCIChar t5:
                    OCIChar OCI5 = (OCIChar)_objectCtrlInfo;
                    objectItem = OCI5.transSon.gameObject;
#if DEBUG
                    LightToggler.LogThis("Toggled Character: " + objectItem.name);
#endif
                    break;
                case OCIRoute t6:
                    OCIRoute OCI6 = (OCIRoute)_objectCtrlInfo;
                    objectItem = OCI6.objectItem;
#if DEBUG
                    LightToggler.LogThis("Toggled Route: " + objectItem.name);
#endif
                    break;
                default:
                    return null;
            }
            return objectItem;
        }
    }
}
