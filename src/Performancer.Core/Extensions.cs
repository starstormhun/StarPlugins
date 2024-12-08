using UnityEngine;
using Studio;

namespace Performancer {
    public static class Extensions {
        public static GameObject GetObject(this ObjectCtrlInfo _objectCtrlInfo) {
            GameObject objectItem;
            switch (_objectCtrlInfo) {
                case OCIItem t1:
                    OCIItem OCI1 = (OCIItem)_objectCtrlInfo;
                    objectItem = OCI1.objectItem;
                    break;
                case OCIFolder t2:
                    OCIFolder OCI2 = (OCIFolder)_objectCtrlInfo;
                    objectItem = OCI2.objectItem;
                    break;
                case OCILight t3:
                    OCILight OCI3 = (OCILight)_objectCtrlInfo;
                    objectItem = OCI3.objectLight;
                    break;
                case OCICamera t4:
                    OCICamera OCI4 = (OCICamera)_objectCtrlInfo;
                    objectItem = OCI4.objectItem;
                    break;
                case OCIChar t5:
                    OCIChar OCI5 = (OCIChar)_objectCtrlInfo;
                    objectItem = OCI5.charInfo.gameObject;
                    break;
                case OCIRoute t6:
                    OCIRoute OCI6 = (OCIRoute)_objectCtrlInfo;
                    objectItem = OCI6.objectItem;
                    break;
                default:
                    return null;
            }
            return objectItem;
        }
    }
}
