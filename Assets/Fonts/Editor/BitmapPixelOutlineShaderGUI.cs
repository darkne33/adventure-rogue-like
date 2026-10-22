using TMPro;
using UnityEditor;
using UnityEngine;

namespace LittleRush.Editor
{
    public class BitmapPixelOutlineShaderGUI : ShaderGUI
    {
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int UseShadowId = Shader.PropertyToID("_UseShadow");
        private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int ShadowOffsetXId = Shader.PropertyToID("_ShadowOffsetX");
        private static readonly int ShadowOffsetYId = Shader.PropertyToID("_ShadowOffsetY");
        private static readonly int ShadowLengthId = Shader.PropertyToID("_ShadowLength");
        private static readonly int ShadowWidthId = Shader.PropertyToID("_ShadowWidth");
        private static readonly int PaddingId = Shader.PropertyToID("_Padding");

        private static int s_UndoRedoCount;
        private static int s_LastSeenUndoRedoCount;

        static BitmapPixelOutlineShaderGUI()
        {
            Undo.undoRedoPerformed += () => s_UndoRedoCount++;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            bool undoRedoPerformed = s_LastSeenUndoRedoCount != s_UndoRedoCount;
            s_LastSeenUndoRedoCount = s_UndoRedoCount;

            EditorGUI.BeginChangeCheck();
            base.OnGUI(materialEditor, properties);
            bool propertiesChanged = EditorGUI.EndChangeCheck();

            foreach (Object target in materialEditor.targets)
            {
                if (!(target is Material material))
                    continue;

                bool paddingChanged = UpdatePadding(material, propertiesChanged);
                if (propertiesChanged || paddingChanged || undoRedoPerformed)
                    TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, material);
            }
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            if (UpdatePadding(material, false))
                TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, material);
        }

        private static bool UpdatePadding(Material material, bool recordUndo)
        {
            if (material == null || !material.HasProperty(PaddingId))
                return false;

            int padding = Mathf.Max(0, Mathf.RoundToInt(material.GetFloat(OutlineWidthId)));
            bool visibleShadow = material.GetFloat(UseShadowId) != 0f &&
                material.GetColor(ShadowColorId).a > 0f;

            if (visibleShadow)
            {
                float offset = Mathf.Max(
                    Mathf.Abs(material.GetFloat(ShadowOffsetXId)),
                    Mathf.Abs(material.GetFloat(ShadowOffsetYId)));
                int length = Mathf.Max(0, Mathf.RoundToInt(material.GetFloat(ShadowLengthId)));
                int width = Mathf.Max(0, Mathf.RoundToInt(material.GetFloat(ShadowWidthId)));
                padding = Mathf.Max(padding, Mathf.CeilToInt(offset + length + width));
            }

            if (material.GetFloat(PaddingId) == padding)
                return false;

            if (recordUndo)
                Undo.RecordObject(material, "Change Bitmap Pixel Outline Padding");

            material.SetFloat(PaddingId, padding);
            EditorUtility.SetDirty(material);
            return true;
        }
    }
}
