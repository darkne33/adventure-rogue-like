using System.Collections.Generic;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class CharacterProximityTransparencySystem
{
    private const float NearbyAlpha = 0.3f;
    private const float ProximityRadius = 2.5f;
    private const float FadeDuration = 0.35f;
    private const float ProximityCheckInterval = 0.1f;
    private const float CandidateRefreshInterval = 1f;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int SurfaceProperty = Shader.PropertyToID("_Surface");
    private static readonly int BlendProperty = Shader.PropertyToID("_Blend");
    private static readonly int AlphaClipProperty = Shader.PropertyToID("_AlphaClip");
    private static readonly int CutoffProperty = Shader.PropertyToID("_Cutoff");
    private static readonly int ModeProperty = Shader.PropertyToID("_Mode");
    private static readonly int SrcBlendProperty = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendProperty = Shader.PropertyToID("_DstBlend");
    private static readonly int SrcBlendAlphaProperty = Shader.PropertyToID("_SrcBlendAlpha");
    private static readonly int DstBlendAlphaProperty = Shader.PropertyToID("_DstBlendAlpha");
    private static readonly int BlendSrcProperty = Shader.PropertyToID("_BlendSrc");
    private static readonly int BlendDstProperty = Shader.PropertyToID("_BlendDst");
    private static readonly int ZWriteProperty = Shader.PropertyToID("_ZWrite");

    private readonly Transform _characterRoot;
    private readonly Collider _characterCollider;
    private readonly List<Renderer> _candidates = new();
    private readonly HashSet<Renderer> _nearbyRenderers = new();
    private readonly Dictionary<Renderer, RendererTransparencyState> _rendererStates = new();
    private readonly List<Renderer> _staleRenderers = new();

    private readonly int _fadeableLayer = LayerMask.NameToLayer("Fadeable");
    private readonly int _wallLayer = LayerMask.NameToLayer("Wall");
    private readonly int _enemyLayer = LayerMask.NameToLayer("Enemy");
    private readonly int _doorLayer = LayerMask.NameToLayer("Door");
    private readonly int _characterLayer = LayerMask.NameToLayer("Character");

    private float _candidateRefreshTimer;
    private float _proximityCheckTimer;
    private bool _isDisposed;

    public CharacterProximityTransparencySystem(Transform characterRoot,
        Collider characterCollider)
    {
        _characterRoot = characterRoot;
        _characterCollider = characterCollider;
    }

    public void Tick(float deltaTime)
    {
        if (_isDisposed || _characterRoot == null)
            return;

        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        _candidateRefreshTimer -= safeDeltaTime;
        _proximityCheckTimer -= safeDeltaTime;

        if (_candidateRefreshTimer <= 0f)
        {
            RefreshCandidates();
            _candidateRefreshTimer = CandidateRefreshInterval;
        }

        if (_proximityCheckTimer <= 0f)
        {
            _proximityCheckTimer = ProximityCheckInterval;
            UpdateTransparencyTargets();
        }

        UpdateFadeAnimations(safeDeltaTime);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        foreach (RendererTransparencyState state in _rendererStates.Values)
            state.Dispose();

        _candidates.Clear();
        _nearbyRenderers.Clear();
        _rendererStates.Clear();
        _staleRenderers.Clear();
    }

    private void RefreshCandidates()
    {
        _candidates.Clear();

        Renderer[] sceneRenderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Renderer renderer in sceneRenderers)
        {
            if (IsOnFadeableLayer(renderer.transform) &&
                IsSupportedRenderer(renderer) && IsExcluded(renderer) == false)
            {
                _candidates.Add(renderer);
            }
        }

        RemoveDestroyedStates();
    }

    private void UpdateTransparencyTargets()
    {
        _nearbyRenderers.Clear();

        Vector3 characterPosition = _characterCollider != null
            ? _characterCollider.bounds.center
            : _characterRoot.position;

        foreach (Renderer renderer in _candidates)
        {
            if (renderer == null || renderer.enabled == false ||
                renderer.gameObject.activeInHierarchy == false ||
                IsNearby(renderer.bounds, characterPosition) == false)
            {
                continue;
            }

            RendererTransparencyState state = GetOrCreateState(renderer);
            if (state == null)
                continue;

            state.SetFaded(true);
            _nearbyRenderers.Add(renderer);
        }

        foreach (KeyValuePair<Renderer, RendererTransparencyState> pair in _rendererStates)
        {
            if (pair.Key == null)
            {
                _staleRenderers.Add(pair.Key);
                continue;
            }

            if (_nearbyRenderers.Contains(pair.Key) == false)
                pair.Value.SetFaded(false);
        }

        RemoveStaleStates();
    }

    private void UpdateFadeAnimations(float deltaTime)
    {
        foreach (RendererTransparencyState state in _rendererStates.Values)
            state.Tick(deltaTime);
    }

    private RendererTransparencyState GetOrCreateState(Renderer renderer)
    {
        if (_rendererStates.TryGetValue(renderer, out RendererTransparencyState state))
            return state;

        if (renderer is SpriteRenderer spriteRenderer)
        {
            state = new RendererTransparencyState(spriteRenderer);
            _rendererStates.Add(renderer, state);
            return state;
        }

        Material[] originalMaterials = renderer.sharedMaterials;
        Material[] transparentMaterials = new Material[originalMaterials.Length];
        bool hasTransparentMaterial = false;

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material originalMaterial = originalMaterials[i];
            Material transparentMaterial = CreateTransparentMaterial(originalMaterial);
            transparentMaterials[i] = transparentMaterial;

            if (ReferenceEquals(originalMaterial, transparentMaterial) == false)
                hasTransparentMaterial = true;
        }

        if (hasTransparentMaterial == false)
            return null;

        state = new RendererTransparencyState(
            renderer, originalMaterials, transparentMaterials);
        _rendererStates.Add(renderer, state);
        return state;
    }

    private static Material CreateTransparentMaterial(Material originalMaterial)
    {
        if (originalMaterial == null)
            return null;

        bool hasBaseColor = originalMaterial.HasProperty(BaseColorProperty);
        bool hasColor = originalMaterial.HasProperty(ColorProperty);
        if (hasBaseColor == false && hasColor == false)
            return originalMaterial;

        bool hasConfigurableBlend = originalMaterial.HasProperty(SurfaceProperty) ||
                                    originalMaterial.HasProperty(ModeProperty) ||
                                    originalMaterial.HasProperty(SrcBlendProperty) ||
                                    originalMaterial.HasProperty(BlendSrcProperty);
        bool isAlreadyTransparent =
            originalMaterial.renderQueue >= (int)RenderQueue.Transparent ||
            originalMaterial.GetTag("RenderType", false) == "Transparent";

        if (hasConfigurableBlend == false && isAlreadyTransparent == false)
            return originalMaterial;

        var transparentMaterial = new Material(originalMaterial)
        {
            name = $"{originalMaterial.name} (Nearby Transparent)",
            hideFlags = HideFlags.DontSave
        };

        bool usesAlphaClip = originalMaterial.HasProperty(AlphaClipProperty) &&
                             originalMaterial.GetFloat(AlphaClipProperty) > 0.5f;

        SetFloatIfPresent(transparentMaterial, SurfaceProperty, 1f);
        SetFloatIfPresent(transparentMaterial, BlendProperty, 0f);
        SetFloatIfPresent(transparentMaterial, AlphaClipProperty, usesAlphaClip ? 1f : 0f);
        SetFloatIfPresent(transparentMaterial, ModeProperty, 3f);
        SetFloatIfPresent(transparentMaterial, SrcBlendProperty, (float)BlendMode.SrcAlpha);
        SetFloatIfPresent(transparentMaterial, DstBlendProperty,
            (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(transparentMaterial, SrcBlendAlphaProperty, (float)BlendMode.One);
        SetFloatIfPresent(transparentMaterial, DstBlendAlphaProperty,
            (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(transparentMaterial, BlendSrcProperty, (float)BlendMode.SrcAlpha);
        SetFloatIfPresent(transparentMaterial, BlendDstProperty,
            (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(transparentMaterial, ZWriteProperty, 1f);

        transparentMaterial.SetOverrideTag("RenderType", "Transparent");
        if (usesAlphaClip)
            transparentMaterial.EnableKeyword("_ALPHATEST_ON");
        else
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
        transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        transparentMaterial.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
        transparentMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        transparentMaterial.SetShaderPassEnabled("ShadowCaster", false);
        transparentMaterial.SetShaderPassEnabled("DepthOnly", false);
        transparentMaterial.SetShaderPassEnabled("DepthNormals", false);
        transparentMaterial.renderQueue = (int)RenderQueue.Transparent;

        return transparentMaterial;
    }

    private static void SetFloatIfPresent(Material material, int propertyId, float value)
    {
        if (material.HasProperty(propertyId))
            material.SetFloat(propertyId, value);
    }

    private bool IsExcluded(Renderer renderer)
    {
        if (renderer == null || renderer.transform.IsChildOf(_characterRoot))
            return true;

        if (renderer.GetComponentInParent<TMP_Text>(true) != null ||
            renderer.GetComponentInParent<CharacterFacade>(true) != null ||
            renderer.GetComponentInParent<EnemyFacade>(true) != null ||
            renderer.GetComponentInParent<RelicChest>(true) != null ||
            renderer.GetComponentInParent<Ground>(true) != null ||
            renderer.GetComponentInParent<Wall>(true) != null ||
            renderer.GetComponentInParent<RoomDoor>(true) != null ||
            renderer.GetComponentInParent<DoorView>(true) != null ||
            renderer.GetComponentInParent<KeyRoomController>(true) != null)
        {
            return true;
        }

        for (Transform current = renderer.transform; current != null; current = current.parent)
        {
            int layer = current.gameObject.layer;
            if (layer == _wallLayer || layer == _enemyLayer || layer == _doorLayer ||
                layer == _characterLayer || IsUnmarkedWall(current.name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnmarkedWall(string objectName) =>
        objectName.StartsWith("Forest_Wall_Bricks_",
            System.StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedRenderer(Renderer renderer) =>
        renderer is MeshRenderer or SkinnedMeshRenderer or SpriteRenderer;

    private bool IsOnFadeableLayer(Transform rendererTransform)
    {
        for (Transform current = rendererTransform; current != null; current = current.parent)
        {
            if (current.gameObject.layer == _fadeableLayer)
                return true;
        }

        return false;
    }

    private static bool IsNearby(Bounds bounds, Vector3 characterPosition)
    {
        float distanceX = characterPosition.x < bounds.min.x
            ? bounds.min.x - characterPosition.x
            : characterPosition.x > bounds.max.x
                ? characterPosition.x - bounds.max.x
                : 0f;
        float distanceZ = characterPosition.z < bounds.min.z
            ? bounds.min.z - characterPosition.z
            : characterPosition.z > bounds.max.z
                ? characterPosition.z - bounds.max.z
                : 0f;

        return distanceX * distanceX + distanceZ * distanceZ <=
               ProximityRadius * ProximityRadius;
    }

    private void RemoveDestroyedStates()
    {
        foreach (KeyValuePair<Renderer, RendererTransparencyState> pair in _rendererStates)
        {
            if (pair.Key == null)
                _staleRenderers.Add(pair.Key);
        }

        RemoveStaleStates();
    }

    private void RemoveStaleStates()
    {
        foreach (Renderer staleRenderer in _staleRenderers)
        {
            if (_rendererStates.TryGetValue(staleRenderer,
                    out RendererTransparencyState state))
            {
                state.Dispose();
            }

            _rendererStates.Remove(staleRenderer);
        }

        _staleRenderers.Clear();
    }

    private sealed class RendererTransparencyState
    {
        private readonly Renderer _renderer;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly Material[] _originalMaterials;
        private readonly Material[] _transparentMaterials;
        private readonly MaterialTransparencyData[] _materialTransparencyData;
        private readonly Color _originalSpriteColor;

        private float _fadeProgress;
        private bool _shouldFade;
        private bool _isUsingFadeVisual;
        private bool _isDisposed;

        public RendererTransparencyState(Renderer renderer, Material[] originalMaterials,
            Material[] transparentMaterials)
        {
            _renderer = renderer;
            _originalMaterials = originalMaterials;
            _transparentMaterials = transparentMaterials;
            _materialTransparencyData =
                new MaterialTransparencyData[transparentMaterials.Length];

            for (int i = 0; i < transparentMaterials.Length; i++)
            {
                Material transparentMaterial = transparentMaterials[i];
                if (transparentMaterial != null &&
                    ReferenceEquals(originalMaterials[i], transparentMaterial) == false)
                {
                    _materialTransparencyData[i] =
                        new MaterialTransparencyData(transparentMaterial);
                }
            }
        }

        public RendererTransparencyState(SpriteRenderer spriteRenderer)
        {
            _renderer = spriteRenderer;
            _spriteRenderer = spriteRenderer;
            _originalSpriteColor = spriteRenderer.color;
        }

        public void SetFaded(bool shouldFade)
        {
            if (_isDisposed)
                return;

            _shouldFade = shouldFade;
            if (_shouldFade)
                EnsureFadeVisual();
        }

        public void Tick(float deltaTime)
        {
            if (_isDisposed || _renderer == null)
                return;

            float targetProgress = _shouldFade ? 1f : 0f;
            if (Mathf.Approximately(_fadeProgress, targetProgress))
            {
                if (_shouldFade == false)
                    RestoreOriginalVisual();

                return;
            }

            EnsureFadeVisual();

            float progressStep = FadeDuration <= 0f
                ? 1f
                : Mathf.Max(0f, deltaTime) / FadeDuration;
            _fadeProgress = Mathf.MoveTowards(
                _fadeProgress, targetProgress, progressStep);

            float smoothProgress = Mathf.SmoothStep(0f, 1f, _fadeProgress);
            ApplyFade(smoothProgress);

            if (_shouldFade == false && _fadeProgress <= 0f)
                RestoreOriginalVisual();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            RestoreOriginalVisual();

            if (_materialTransparencyData != null)
            {
                foreach (MaterialTransparencyData materialData in _materialTransparencyData)
                    materialData?.Dispose();
            }

            _isDisposed = true;
        }

        private void EnsureFadeVisual()
        {
            if (_isUsingFadeVisual || _renderer == null)
                return;

            if (_spriteRenderer == null)
                _renderer.sharedMaterials = _transparentMaterials;

            _isUsingFadeVisual = true;
            ApplyFade(Mathf.SmoothStep(0f, 1f, _fadeProgress));
        }

        private void ApplyFade(float progress)
        {
            if (_spriteRenderer != null)
            {
                Color color = _originalSpriteColor;
                color.a = Mathf.Lerp(
                    _originalSpriteColor.a,
                    Mathf.Min(_originalSpriteColor.a, NearbyAlpha),
                    progress);
                _spriteRenderer.color = color;
                return;
            }

            foreach (MaterialTransparencyData materialData in _materialTransparencyData)
                materialData?.SetFade(progress);
        }

        private void RestoreOriginalVisual()
        {
            if (_isUsingFadeVisual == false)
                return;

            if (_renderer != null)
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _originalSpriteColor;
                else
                    _renderer.sharedMaterials = _originalMaterials;
            }

            _fadeProgress = 0f;
            _isUsingFadeVisual = false;
        }
    }

    private sealed class MaterialTransparencyData
    {
        private readonly Material _material;
        private readonly bool _hasBaseColor;
        private readonly bool _hasColor;
        private readonly bool _usesAlphaClip;
        private readonly Color _originalBaseColor;
        private readonly Color _originalColor;
        private readonly float _originalCutoff;

        public MaterialTransparencyData(Material material)
        {
            _material = material;
            _hasBaseColor = material.HasProperty(BaseColorProperty);
            _hasColor = material.HasProperty(ColorProperty);
            _usesAlphaClip = material.HasProperty(AlphaClipProperty) &&
                             material.GetFloat(AlphaClipProperty) > 0.5f &&
                             material.HasProperty(CutoffProperty);

            if (_hasBaseColor)
                _originalBaseColor = material.GetColor(BaseColorProperty);
            if (_hasColor)
                _originalColor = material.GetColor(ColorProperty);
            if (_usesAlphaClip)
                _originalCutoff = material.GetFloat(CutoffProperty);
        }

        public void SetFade(float progress)
        {
            if (_material == null)
                return;

            float originalAlpha = 1f;
            float currentAlpha = 1f;

            if (_hasBaseColor)
            {
                Color color = _originalBaseColor;
                color.a = Mathf.Lerp(
                    _originalBaseColor.a,
                    Mathf.Min(_originalBaseColor.a, NearbyAlpha),
                    progress);
                _material.SetColor(BaseColorProperty, color);
                originalAlpha = _originalBaseColor.a;
                currentAlpha = color.a;
            }

            if (_hasColor)
            {
                Color color = _originalColor;
                color.a = Mathf.Lerp(
                    _originalColor.a,
                    Mathf.Min(_originalColor.a, NearbyAlpha),
                    progress);
                _material.SetColor(ColorProperty, color);

                if (_hasBaseColor == false)
                {
                    originalAlpha = _originalColor.a;
                    currentAlpha = color.a;
                }
            }

            if (_usesAlphaClip)
            {
                float alphaRatio = originalAlpha > Mathf.Epsilon
                    ? currentAlpha / originalAlpha
                    : 1f;
                _material.SetFloat(CutoffProperty, _originalCutoff * alphaRatio);
            }
        }

        public void Dispose()
        {
            if (_material != null)
                Object.Destroy(_material);
        }
    }
}
