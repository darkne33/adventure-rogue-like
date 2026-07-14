using System.Collections.Generic;
using UnityEngine;

internal sealed class CharacterOutlineController
{
    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");

    private readonly Renderer[] _renderers;
    private readonly List<MaterialState> _materialStates = new();
    private readonly MaterialPropertyBlock _propertyBlock = new();

    public CharacterOutlineController(Renderer[] renderers) =>
        _renderers = renderers;

    public void Hide()
    {
        if (_materialStates.Count > 0 || _renderers == null)
            return;

        foreach (Renderer meshRenderer in _renderers)
        {
            if (meshRenderer == null)
                continue;

            Material[] materials = meshRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || material.HasProperty(OutlineThicknessId) == false)
                    continue;

                _propertyBlock.Clear();
                meshRenderer.GetPropertyBlock(_propertyBlock, materialIndex);
                float thickness = _propertyBlock.HasFloat(OutlineThicknessId)
                    ? _propertyBlock.GetFloat(OutlineThicknessId)
                    : material.GetFloat(OutlineThicknessId);

                _materialStates.Add(new MaterialState(meshRenderer, materialIndex, thickness));
                _propertyBlock.SetFloat(OutlineThicknessId, 0f);
                meshRenderer.SetPropertyBlock(_propertyBlock, materialIndex);
            }
        }
    }

    public void Restore()
    {
        foreach (MaterialState state in _materialStates)
        {
            if (state.Renderer == null)
                continue;

            _propertyBlock.Clear();
            state.Renderer.GetPropertyBlock(_propertyBlock, state.MaterialIndex);
            _propertyBlock.SetFloat(OutlineThicknessId, state.Thickness);
            state.Renderer.SetPropertyBlock(_propertyBlock, state.MaterialIndex);
        }

        _materialStates.Clear();
    }

    private readonly struct MaterialState
    {
        public Renderer Renderer { get; }
        public int MaterialIndex { get; }
        public float Thickness { get; }

        public MaterialState(Renderer renderer, int materialIndex, float thickness)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            Thickness = thickness;
        }
    }
}
