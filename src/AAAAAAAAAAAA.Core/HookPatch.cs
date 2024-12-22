using TMPro;
using Studio;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AAAAAAAAAAAA {
    public static class HookPatch {
        internal static void InitMaker() {
            if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Initialising AAAAAAAAAAAA for Maker!");
            Maker.SetupHooks();
        }

        internal static void Deactivate() {
            Maker.UnregisterHooks();
        }

        internal static class Maker {
            private static Harmony _harmony;

            internal static TMP_Dropdown parentDropdown;
            internal static Toggle ponytailToggle;
            internal static Toggle parentToggle;
            internal static int aaaaaaaaaaaaOptionIdx = -1;

            // Setup Harmony and patch methods
            public static void SetupHooks() {
                _harmony = Harmony.CreateAndPatchAll(typeof(Maker), null);
            }

            // Disable Harmony patches of this plugin
            public static void UnregisterHooks() {
                _harmony.UnpatchSelf();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KK_MoreAccessoryParents.Interface), "CreateInterface")]
            private static void KKMoreAccessoryParentsInterfaceAfterCreateInterface() {
                // Get the root of all ~problems~ accessory settings
                Transform uiAccRoot = GameObject.Find("04_AccessoryTop").transform;

                // Extend background
                RectTransform parentBg = uiAccRoot.Find("AcsParentWindow/BasePanel/imgWindowBack").GetComponent<RectTransform>();
                parentBg.offsetMin += new Vector2(0, -30);

                // Add button
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Adding parent updater button...");
                Transform btnToCopy = uiAccRoot.Find("Slots/Viewport/Content/tglSlot01/Slot01Top/Scroll View/Viewport/Content/grpBtn");
                Transform btnParent = uiAccRoot.Find("AcsParentWindow/grpParent");
                var newBtn = GameObject.Instantiate(btnToCopy, btnParent);
                newBtn.name = "AAAAAAAAAAAA_Parent_Btn";
                newBtn.localPosition = btnParent.GetChild(btnParent.childCount - 2).localPosition + new Vector3(0, -35, 0);
                newBtn.GetComponentInChildren<TextMeshProUGUI>(true).text = "Update AAAAAAAAAAAA parent";
                newBtn.GetComponentInChildren<Button>(true).onClick.AddListener(() => AAAAAAAAAAAA.OnParentButtonPressed());
                var newBtnTf = newBtn.GetChild(0).GetComponent<RectTransform>();
                newBtnTf.anchorMax = new Vector2(0, 0);
                newBtnTf.offsetMin = new Vector2(0, 0);
                newBtnTf.offsetMax = new Vector2(367, 24);
                newBtnTf.localPosition = new Vector2(8, -6);

                // Add AAAAAAAAAAAA entry to MoreAccessoryParents "Other" dropdown
                if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("Adding parent dropdown entry...");
                parentToggle = btnParent.GetChild(btnParent.childCount - 3).GetComponentInChildren<Toggle>(true);
                parentDropdown = btnParent.GetChild(btnParent.childCount - 2).GetComponentInChildren<TMP_Dropdown>(true);
                parentDropdown.options.Add(new TMP_Dropdown.OptionData("AAAAAAAAAAAA"));
                aaaaaaaaaaaaOptionIdx = parentDropdown.options.Count - 1;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(KK_MoreAccessoryParents.Interface), "OnSelectionChanged")]
            private static bool KKMoreAccessoryParentsInterfaceBeforeOnSelectionChanged() {
                if (parentToggle.isOn && parentDropdown.value == aaaaaaaaaaaaOptionIdx) {
                    if (AAAAAAAAAAAA.IsDebug.Value) AAAAAAAAAAAA.Instance.Log("AAAAAAAAAAAA Parentage detected!");
                    AAAAAAAAAAAA.RegisterParent();
                    return false;
                }
                return true;
            }
        }
    }
}
