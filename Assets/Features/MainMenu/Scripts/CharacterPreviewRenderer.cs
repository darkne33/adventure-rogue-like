using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.Universal;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CharacterPreviewRenderer : MonoBehaviour
{
    private const int PreviewLayer = 31;
    private const int PreviewRendererIndex = 1;
    private const int PortraitResolution = 512;
    private const int MinimumPreviewResolution = 256;
    private const int MaximumPreviewResolution = 1024;
    private const float PreviewFieldOfView = 28f;
    private const float MainStagePadding = 1.14f;
    private const float PortraitStagePadding = 1.2f;
    private const float PortraitStageOffset = 100f;
    private const float AppearanceScaleMultiplier = 1.018f;
    private const float AppearanceGrowDuration = 0.07f;
    private const float AppearanceSettleDuration = 0.09f;
    private const int EmptyPreviewMilliseconds = 50;

    private static readonly Vector3 PreviewWorldPosition = new(10000f, -10000f, 10000f);
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");

    private readonly Dictionary<string, Sprite> _portraitCache =
        new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _portraitRenderGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();

    private RawImage _viewport;
    private GameObject _previewWorld;
    private Transform _mainStage;
    private Transform _portraitStage;
    private Camera _previewCamera;
    private Camera _portraitCamera;
    private RenderTexture _previewRenderTexture;
    private RenderTexture _portraitRenderTexture;

    private GameObject _currentPreview;
    private float _currentPreviewOffsetY;
    private float _currentPreviewZoom = 1f;
    private AsyncOperationHandle<GameObject> _currentCharacterHandle;
    private bool _hasCurrentCharacterHandle;
    private Sequence _appearanceSequence;
    private CancellationTokenSource _switchCancellation;
    private int _switchVersion;
    private bool _isInitialized;
    private bool _isDestroyed;

    public event Action<string, Sprite> PortraitRendered;

    public IReadOnlyDictionary<string, Sprite> Portraits => _portraitCache;

    public void Initialize(RawImage viewport)
    {
        if (viewport == null)
            throw new ArgumentNullException(nameof(viewport));

        if (_isDestroyed)
            throw new ObjectDisposedException(nameof(CharacterPreviewRenderer));

        if (_viewport != null && _viewport != viewport &&
            _viewport.texture == _previewRenderTexture)
        {
            _viewport.texture = null;
        }

        _viewport = viewport;
        CreatePreviewWorldIfNeeded();
        EnsurePreviewRenderTexture(forceRecreate: false);

        _viewport.texture = _previewRenderTexture;
        _viewport.color = Color.white;
        _viewport.uvRect = new Rect(0f, 0f, 1f, 1f);
        _previewCamera.enabled = isActiveAndEnabled;
        _isInitialized = true;
    }

    public async UniTask ShowCharacterAsync(CharacterDefinition character,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ValidateCharacter(character);

        CancelSwitchOperation();
        int switchVersion = ++_switchVersion;
        ClearCurrentPreview();

        var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token, cancellationToken);
        _switchCancellation = operationCancellation;
        CancellationToken operationToken = operationCancellation.Token;

        AsyncOperationHandle<GameObject> loadHandle = default;
        bool ownsLoadHandle = false;

        try
        {
            loadHandle = Addressables.LoadAssetAsync<GameObject>(
                character.CharacterContainer.AssetReference.RuntimeKey);
            ownsLoadHandle = true;

            await UniTask.Delay(TimeSpan.FromMilliseconds(EmptyPreviewMilliseconds),
                ignoreTimeScale: true, cancellationToken: operationToken);

            GameObject characterPrefab = await loadHandle.ToUniTask(
                cancellationToken: operationToken);

            ThrowIfSwitchIsStale(switchVersion, operationToken);

            GameObject preview = CreateVisualInstance(characterPrefab, _mainStage,
                $"CharacterPreview_{character.Id}");

            try
            {
                ThrowIfSwitchIsStale(switchVersion, operationToken);
                _currentPreview = preview;
                _currentCharacterHandle = loadHandle;
                _hasCurrentCharacterHandle = true;
                ownsLoadHandle = false;
                _currentPreviewOffsetY = character.PreviewOffsetY;
                _currentPreviewZoom = character.PreviewZoom;

                FrameCamera(_previewCamera, _currentPreview, MainStagePadding,
                    _currentPreviewOffsetY, _currentPreviewZoom);
                PlayAppearanceAccent(_currentPreview.transform);

                await RenderAndCachePortraitAsync(character, characterPrefab,
                    operationToken);
            }
            catch
            {
                if (_currentPreview != preview)
                {
                    preview.SetActive(false);
                    DestroyUnityObject(preview);
                }

                throw;
            }
        }
        finally
        {
            if (ownsLoadHandle)
                ReleaseHandle(loadHandle);

            if (ReferenceEquals(_switchCancellation, operationCancellation))
                _switchCancellation = null;

            operationCancellation.Dispose();
        }
    }

    public async UniTask PrewarmPortraitsAsync(
        IReadOnlyList<CharacterDefinition> characters,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        if (characters == null)
            throw new ArgumentNullException(nameof(characters));

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token, cancellationToken);
        CancellationToken operationToken = operationCancellation.Token;

        for (int index = 0; index < characters.Count; index++)
        {
            operationToken.ThrowIfCancellationRequested();
            CharacterDefinition character = characters[index];
            ValidateCharacter(character);

            if (_portraitCache.ContainsKey(character.Id))
                continue;

            AsyncOperationHandle<GameObject> loadHandle = default;
            bool ownsLoadHandle = false;

            try
            {
                loadHandle = Addressables.LoadAssetAsync<GameObject>(
                    character.CharacterContainer.AssetReference.RuntimeKey);
                ownsLoadHandle = true;
                GameObject characterPrefab = await loadHandle.ToUniTask(
                    cancellationToken: operationToken);

                await RenderAndCachePortraitAsync(character, characterPrefab,
                    operationToken);
            }
            finally
            {
                if (ownsLoadHandle)
                    ReleaseHandle(loadHandle);
            }
        }
    }

    public bool TryGetPortrait(string characterId, out Sprite portrait)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            portrait = null;
            return false;
        }

        return _portraitCache.TryGetValue(characterId, out portrait);
    }

    public void ClearPreview()
    {
        CancelSwitchOperation();
        _switchVersion++;
        ClearCurrentPreview();
    }

    private async UniTask RenderAndCachePortraitAsync(CharacterDefinition character,
        GameObject characterPrefab, CancellationToken cancellationToken)
    {
        if (_portraitCache.ContainsKey(character.Id))
            return;

        bool enteredGate = false;
        GameObject portraitInstance = null;

        try
        {
            await _portraitRenderGate.WaitAsync(cancellationToken);
            enteredGate = true;

            if (_portraitCache.ContainsKey(character.Id))
                return;

            cancellationToken.ThrowIfCancellationRequested();
            portraitInstance = CreateVisualInstance(characterPrefab, _portraitStage,
                $"CharacterPortrait_{character.Id}");
            FrameCamera(_portraitCamera, portraitInstance, PortraitStagePadding,
                character.PreviewOffsetY, character.PreviewZoom);

            _portraitCamera.enabled = true;
            try
            {
                await UniTask.WaitForEndOfFrame(cancellationToken);
            }
            finally
            {
                if (_portraitCamera != null)
                    _portraitCamera.enabled = false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Texture2D portraitTexture = ReadPortraitTexture();
            Sprite portrait = null;

            try
            {
                portrait = Sprite.Create(
                    portraitTexture,
                    new Rect(0f, 0f, portraitTexture.width, portraitTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                portrait.name = $"{character.Id}_PreviewPortrait";
                _portraitCache.Add(character.Id, portrait);
            }
            catch
            {
                if (portrait != null)
                    DestroyUnityObject(portrait);

                DestroyUnityObject(portraitTexture);
                throw;
            }

            PortraitRendered?.Invoke(character.Id, portrait);
        }
        finally
        {
            if (portraitInstance != null)
            {
                portraitInstance.SetActive(false);
                DestroyUnityObject(portraitInstance);
            }

            if (enteredGate)
                _portraitRenderGate.Release();
        }
    }

    private GameObject CreateVisualInstance(GameObject characterPrefab, Transform stage,
        string instanceName)
    {
        CharacterFacade facade = characterPrefab != null
            ? characterPrefab.GetComponent<CharacterFacade>()
            : null;
        GameObject visualSource = facade?.CharacterModel;

        if (visualSource == null)
        {
            throw new InvalidOperationException(
                $"Character prefab '{characterPrefab?.name ?? "NULL"}' does not expose " +
                "CharacterFacade.CharacterModel for preview rendering.");
        }

        GameObject instance = Instantiate(visualSource, stage, false);
        instance.name = instanceName;
        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.SetActive(true);

        PrepareVisualHierarchy(instance);
        return instance;
    }

    private static void PrepareVisualHierarchy(GameObject visualRoot)
    {
        Transform[] transforms = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform child in transforms)
        {
            child.gameObject.layer = PreviewLayer;
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        Behaviour[] behaviours = visualRoot.GetComponentsInChildren<Behaviour>(includeInactive: true);
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour is Animator)
                continue;

            behaviour.enabled = false;
        }

        Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (Collider collider in colliders)
            collider.enabled = false;

        Rigidbody[] rigidbodies = visualRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true);
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.detectCollisions = false;
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        ParticleSystem[] particleSystems =
            visualRoot.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            var main = particleSystem.main;
            main.playOnAwake = false;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (particleSystem.TryGetComponent(out ParticleSystemRenderer particleRenderer))
                particleRenderer.enabled = false;
        }

        TrailRenderer[] trails =
            visualRoot.GetComponentsInChildren<TrailRenderer>(includeInactive: true);
        foreach (TrailRenderer trail in trails)
            trail.enabled = false;

        LineRenderer[] lines =
            visualRoot.GetComponentsInChildren<LineRenderer>(includeInactive: true);
        foreach (LineRenderer line in lines)
            line.enabled = false;

        Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(includeInactive: true);
        foreach (Animator animator in animators)
        {
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.applyRootMotion = false;
            animator.speed = 1f;
            animator.Rebind();

            if (animator.runtimeAnimatorController != null &&
                animator.HasState(0, IdleStateHash))
            {
                animator.Play(IdleStateHash, 0, 0f);
            }

            animator.Update(0f);
        }

        SkinnedMeshRenderer[] skinnedRenderers =
            visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
            skinnedRenderer.updateWhenOffscreen = true;
    }

    private void PlayAppearanceAccent(Transform previewTransform)
    {
        _appearanceSequence?.Kill();

        Vector3 baseScale = previewTransform.localScale;
        Vector3 accentScale = baseScale * AppearanceScaleMultiplier;
        previewTransform.localScale = baseScale;

        _appearanceSequence = DOTween.Sequence()
            .SetUpdate(isIndependentUpdate: true)
            .SetLink(previewTransform.gameObject)
            .Append(previewTransform.DOScale(accentScale, AppearanceGrowDuration)
                .SetEase(Ease.OutQuad))
            .Append(previewTransform.DOScale(baseScale, AppearanceSettleDuration)
                .SetEase(Ease.InOutSine))
            .OnComplete(() => _appearanceSequence = null);
    }

    private static void FrameCamera(Camera camera, GameObject visualRoot, float padding,
        float previewOffsetY, float previewZoom)
    {
        Bounds bounds = CalculateVisualBounds(visualRoot);
        Vector3 extents = bounds.extents;
        extents.x = Mathf.Max(extents.x, 0.05f);
        extents.y = Mathf.Max(extents.y, 0.05f);
        extents.z = Mathf.Max(extents.z, 0.05f);

        float aspect = camera.targetTexture != null
            ? camera.targetTexture.width / (float)camera.targetTexture.height
            : Mathf.Max(0.1f, camera.aspect);
        float verticalHalfFov = camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * aspect);
        float verticalDistance = extents.y / Mathf.Tan(verticalHalfFov);
        float horizontalDistance = extents.x / Mathf.Tan(horizontalHalfFov);
        float zoom = Mathf.Max(0.1f, previewZoom);
        float distance = (Mathf.Max(verticalDistance, horizontalDistance) + extents.z) *
                         padding / zoom;

        Vector3 focus = bounds.center + Vector3.up * extents.y * 0.035f;
        float viewportWorldHeight = 2f * distance * Mathf.Tan(verticalHalfFov);
        focus -= Vector3.up * previewOffsetY * viewportWorldHeight;
        camera.transform.SetPositionAndRotation(
            focus + Vector3.forward * distance,
            Quaternion.LookRotation(Vector3.back, Vector3.up));

        float boundsRadius = extents.magnitude;
        camera.nearClipPlane = Mathf.Max(0.01f, distance - boundsRadius * 1.5f);
        camera.farClipPlane = Mathf.Max(camera.nearClipPlane + 1f,
            distance + boundsRadius * 2f);
    }

    private static Bounds CalculateVisualBounds(GameObject visualRoot)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer.enabled == false || renderer.gameObject.activeInHierarchy == false ||
                renderer is ParticleSystemRenderer or
                TrailRenderer or LineRenderer)
            {
                continue;
            }

            if (hasBounds == false)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds
            ? bounds
            : new Bounds(visualRoot.transform.position, Vector3.one * 2f);
    }

    private Texture2D ReadPortraitTexture()
    {
        RenderTexture previousActiveTexture = RenderTexture.active;

        try
        {
            RenderTexture.active = _portraitRenderTexture;
            var texture = new Texture2D(PortraitResolution, PortraitResolution,
                TextureFormat.RGBA32, mipChain: false, linear: false)
            {
                name = "CharacterPreviewPortraitTexture",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.ReadPixels(new Rect(0f, 0f, PortraitResolution, PortraitResolution),
                0, 0, recalculateMipMaps: false);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }
        finally
        {
            RenderTexture.active = previousActiveTexture;
        }
    }

    private void CreatePreviewWorldIfNeeded()
    {
        if (_previewWorld != null)
            return;

        _previewWorld = new GameObject($"CharacterPreviewWorld_{GetEntityId()}")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _previewWorld.transform.position = PreviewWorldPosition;

        _mainStage = CreateStage("MainPreviewStage", Vector3.zero);
        _portraitStage = CreateStage("PortraitPreviewStage",
            Vector3.right * PortraitStageOffset);
        _previewCamera = CreateCamera("CharacterPreviewCamera");
        _portraitCamera = CreateCamera("CharacterPortraitCamera");
        _portraitCamera.enabled = false;
        _portraitRenderTexture = CreateRenderTexture(PortraitResolution,
            PortraitResolution, "CharacterPortraitRenderTexture");
        _portraitCamera.targetTexture = _portraitRenderTexture;

        CreateDirectionalLight("CharacterPreviewKeyLight",
            new Color(1f, 0.91f, 0.8f), 1.35f, new Vector3(32f, -28f, 0f));
        CreateDirectionalLight("CharacterPreviewFillLight",
            new Color(0.55f, 0.7f, 1f), 0.75f, new Vector3(18f, 145f, 0f));
    }

    private Transform CreateStage(string stageName, Vector3 localPosition)
    {
        var stage = new GameObject(stageName)
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = PreviewLayer
        };
        stage.transform.SetParent(_previewWorld.transform, worldPositionStays: false);
        stage.transform.localPosition = localPosition;
        return stage.transform;
    }

    private Camera CreateCamera(string cameraName)
    {
        var cameraObject = new GameObject(cameraName)
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = PreviewLayer
        };
        cameraObject.transform.SetParent(_previewWorld.transform, worldPositionStays: false);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.cameraType = CameraType.Game;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << PreviewLayer;
        camera.orthographic = false;
        camera.fieldOfView = PreviewFieldOfView;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.depthTextureMode = DepthTextureMode.None;
        camera.useOcclusionCulling = false;
        camera.forceIntoRenderTexture = true;
        camera.depth = 100f;

        UniversalAdditionalCameraData cameraData =
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.SetRenderer(PreviewRendererIndex);
        cameraData.renderType = CameraRenderType.Base;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.renderShadows = false;
        cameraData.allowXRRendering = false;
        cameraData.allowHDROutput = false;
        cameraData.volumeLayerMask = 0;
        return camera;
    }

    private void CreateDirectionalLight(string lightName, Color color, float intensity,
        Vector3 eulerAngles)
    {
        var lightObject = new GameObject(lightName)
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = PreviewLayer
        };
        lightObject.transform.SetParent(_previewWorld.transform, worldPositionStays: false);
        lightObject.transform.localRotation = Quaternion.Euler(eulerAngles);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << PreviewLayer;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    private void EnsurePreviewRenderTexture(bool forceRecreate)
    {
        GetPreviewResolution(out int width, out int height);
        if (forceRecreate == false && _previewRenderTexture != null &&
            _previewRenderTexture.width == width && _previewRenderTexture.height == height)
        {
            return;
        }

        RenderTexture replacement = CreateRenderTexture(width, height,
            "CharacterPreviewRenderTexture");
        _previewCamera.targetTexture = replacement;

        if (_viewport != null)
            _viewport.texture = replacement;

        DestroyRenderTexture(_previewRenderTexture);
        _previewRenderTexture = replacement;

        if (_currentPreview != null)
        {
            FrameCamera(_previewCamera, _currentPreview, MainStagePadding,
                _currentPreviewOffsetY, _currentPreviewZoom);
        }
    }

    private void GetPreviewResolution(out int width, out int height)
    {
        Rect viewportRect = _viewport.rectTransform.rect;
        float requestedWidth = Mathf.Abs(viewportRect.width);
        float requestedHeight = Mathf.Abs(viewportRect.height);

        if (requestedWidth < 1f || requestedHeight < 1f)
        {
            width = MaximumPreviewResolution;
            height = MaximumPreviewResolution;
            return;
        }

        float resolutionScale = MaximumPreviewResolution /
                                Mathf.Max(requestedWidth, requestedHeight);
        width = Mathf.Clamp(Mathf.RoundToInt(requestedWidth * resolutionScale),
            MinimumPreviewResolution, MaximumPreviewResolution);
        height = Mathf.Clamp(Mathf.RoundToInt(requestedHeight * resolutionScale),
            MinimumPreviewResolution, MaximumPreviewResolution);
    }

    private static RenderTexture CreateRenderTexture(int width, int height, string textureName)
    {
        var renderTexture = new RenderTexture(width, height, 24,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
        {
            name = textureName,
            hideFlags = HideFlags.HideAndDontSave,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
        return renderTexture;
    }

    private void LateUpdate()
    {
        if (_isInitialized && _viewport != null)
            EnsurePreviewRenderTexture(forceRecreate: false);
    }

    private void OnEnable()
    {
        if (_previewCamera != null)
            _previewCamera.enabled = true;
    }

    private void OnDisable()
    {
        if (_previewCamera != null)
            _previewCamera.enabled = false;
    }

    private void ClearCurrentPreview()
    {
        _appearanceSequence?.Kill();
        _appearanceSequence = null;

        if (_currentPreview != null)
        {
            _currentPreview.SetActive(false);
            DestroyUnityObject(_currentPreview);
            _currentPreview = null;
        }

        _currentPreviewOffsetY = 0f;
        _currentPreviewZoom = 1f;

        if (_hasCurrentCharacterHandle)
        {
            ReleaseHandle(_currentCharacterHandle);
            _currentCharacterHandle = default;
            _hasCurrentCharacterHandle = false;
        }
    }

    private void CancelSwitchOperation()
    {
        CancellationTokenSource cancellation = _switchCancellation;
        _switchCancellation = null;

        if (cancellation == null)
            return;

        cancellation.Cancel();
        cancellation.Dispose();
    }

    private void ThrowIfSwitchIsStale(int switchVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (switchVersion != _switchVersion)
            throw new OperationCanceledException(cancellationToken);
    }

    private static void ValidateCharacter(CharacterDefinition character)
    {
        if (character == null)
            throw new ArgumentNullException(nameof(character));

        if (string.IsNullOrWhiteSpace(character.Id))
            throw new InvalidOperationException("A preview character must have a non-empty ID.");

        if (character.CharacterContainer?.AssetReference == null ||
            character.CharacterContainer.AssetReference.RuntimeKeyIsValid() == false)
        {
            throw new InvalidOperationException(
                $"Character '{character.DisplayName}' does not have a valid preview prefab.");
        }
    }

    private void EnsureInitialized()
    {
        if (_isDestroyed)
            throw new ObjectDisposedException(nameof(CharacterPreviewRenderer));

        if (_isInitialized == false || _viewport == null || _previewWorld == null)
        {
            throw new InvalidOperationException(
                "CharacterPreviewRenderer.Initialize must be called before rendering.");
        }
    }

    private static void ReleaseHandle(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.IsValid())
            Addressables.Release(handle);
    }

    private static void DestroyRenderTexture(RenderTexture renderTexture)
    {
        if (renderTexture == null)
            return;

        if (renderTexture.IsCreated())
            renderTexture.Release();

        DestroyUnityObject(renderTexture);
    }

    private static void DestroyUnityObject(Object unityObject)
    {
        if (unityObject == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(unityObject);
        else
            Object.DestroyImmediate(unityObject);
    }

    private void OnDestroy()
    {
        _isDestroyed = true;
        _lifetimeCancellation.Cancel();
        CancelSwitchOperation();
        ClearCurrentPreview();

        if (_viewport != null && _viewport.texture == _previewRenderTexture)
            _viewport.texture = null;

        foreach (Sprite portrait in _portraitCache.Values)
        {
            if (portrait == null)
                continue;

            Texture2D texture = portrait.texture;
            DestroyUnityObject(portrait);
            DestroyUnityObject(texture);
        }

        _portraitCache.Clear();
        PortraitRendered = null;
        DestroyRenderTexture(_previewRenderTexture);
        DestroyRenderTexture(_portraitRenderTexture);
        _previewRenderTexture = null;
        _portraitRenderTexture = null;

        if (_previewWorld != null)
        {
            DestroyUnityObject(_previewWorld);
            _previewWorld = null;
        }

        _lifetimeCancellation.Dispose();
    }
}
