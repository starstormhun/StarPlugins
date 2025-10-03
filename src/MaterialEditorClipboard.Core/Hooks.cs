using KK_Plugins.MaterialEditor;
using MaterialEditorAPI;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using System;

namespace MaterialEditorClipboard {
    public partial class MaterialEditorClipboard : BaseUnityPlugin {
        internal static class Hooks {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MaterialEditorCharaController), nameof(MaterialEditorCharaController.MaterialCopyEdits))]
            [HarmonyPatch(typeof(SceneController), nameof(SceneController.MaterialCopyEdits))]
            private static void MaterialEditor_MaterialCopyEdits_Postfix(Material material) {
                if (!ConfMonitor.Value) {
                    return;
                }
                if (MaterialEditorPluginBase.CopyData.MaterialShaderList.Count + MaterialEditorPluginBase.CopyData.MaterialFloatPropertyList.Count + MaterialEditorPluginBase.CopyData.MaterialColorPropertyList.Count + MaterialEditorPluginBase.CopyData.MaterialTexturePropertyList.Count == 0) {
                    return;
                }
                string text = material.NameFormatted();
                _listCopyContainer.Add(new ClipboardEntry {
                    Label = text,
                    Data = CopyContainerClone(MaterialEditorPluginBase.CopyData)
                });
                LoggerStat.LogMessage("Clipboard entry " + text + " created with new copied data");
            }
        }
    }
}
