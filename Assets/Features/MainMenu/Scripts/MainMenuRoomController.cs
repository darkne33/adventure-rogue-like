using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Bosses.Scripts;
using LittleRush.Rendering;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class MainMenuRoomController : MonoBehaviour
{
    // Preserve the room's height so the game's world-space height fog applies correctly.
    private static readonly Vector3 WorldPosition = new(10000f, 0f, 0f);

    [Header("Camera points")]
    [SerializeField] private Transform _menuCameraPoint;
    [SerializeField] private Transform _characterCameraPoint;
    [SerializeField] private Transform _characterSpawnPoint;
    [SerializeField] private Transform _bossSpawnPoint;
    [SerializeField, Min(0.01f)] private float _transitionDuration = 1f;
    [SerializeField, Range(10f, 90f)] private float _fieldOfView = 40f;

    [Header("Menu camera sway")]
    [SerializeField] private Vector3 _menuSwayAngles = new(0.45f, 0.7f, 0.15f);
    [SerializeField, Min(0.01f)] private float _menuSwayPeriod = 8f;
    [SerializeField, Min(0.01f)] private float _menuSwayFadeDuration = 0.5f;

    [Header("Environment")]
    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private Material _fogMaterial;
    [SerializeField] private VolumeProfile _postProcessProfile;

    [Header("Background blur")]
    [SerializeField] private VolumeProfile _blurProfile;
    [SerializeField, Min(0f)] private float _blurFocusPadding = 3f;
    [SerializeField, Min(0.01f)] private float _blurTransitionDistance = 18f;

    private GameObject _worldRoot;
    private GameObject _bossPreview;
    private Camera _camera;
    private UnityEngine.UI.RawImage _background;
    private RenderTexture _renderTexture;
    private CancellationTokenSource _transitionCancellation;
    private VolumeProfile _runtimeBlurProfile;
    private DepthOfField _depthOfField;
    private bool _isMenuSwayActive = true;
    private float _menuSwayTime;

    public Transform CharacterSpawnPoint => _characterSpawnPoint;

    public void ShowBossPreview(Room bossRoom)
    {
        if (bossRoom == null)
            throw new InvalidOperationException("The next boss room is not assigned.");

        BossSpawnPoint spawnPoint = bossRoom.GetComponentInChildren<BossSpawnPoint>(true);
        if (spawnPoint == null)
            throw new InvalidOperationException($"Boss room '{bossRoom.name}' requires a boss spawn point.");
        if (spawnPoint.MenuPreviewPrefab == null)
            throw new InvalidOperationException(
                $"Boss room '{bossRoom.name}' has a missing or unresolved menu preview prefab reference.");
        if (_bossSpawnPoint == null)
            throw new InvalidOperationException("MainMenu_Room requires a boss presentation point.");

        if (_bossPreview != null)
        {
            _bossPreview.SetActive(false);
            Destroy(_bossPreview);
        }

        _bossPreview = Instantiate(spawnPoint.MenuPreviewPrefab, _bossSpawnPoint, false);
    }

    public static MainMenuRoomController CreateInstance(MainMenuRoomController prefab,
        RectTransform menuRoot)
    {
        if (prefab == null)
            throw new InvalidOperationException("MainMenu_Room prefab is not assigned to the menu.");

        var worldRoot = new GameObject("MainMenuRoomWorld");
        // Keep gameplay components inactive until the room is prepared for presentation.
        worldRoot.SetActive(false);
        worldRoot.transform.position = WorldPosition;

        try
        {
            MainMenuRoomController instance = Instantiate(prefab, worldRoot.transform, false);
            instance._worldRoot = worldRoot;
            instance.Initialize(menuRoot);
            worldRoot.SetActive(true);
            return instance;
        }
        catch
        {
            Destroy(worldRoot);
            throw;
        }
    }

    private void Initialize(RectTransform menuRoot)
    {
        if (_menuCameraPoint == null || _characterCameraPoint == null || _characterSpawnPoint == null)
            throw new InvalidOperationException("MainMenu_Room requires camera and character spawn points.");

        if (_postProcessProfile == null)
            throw new InvalidOperationException("MainMenu_Room requires the gameplay post-processing profile.");

        if (_fogMaterial == null)
            throw new InvalidOperationException("MainMenu_Room requires a fog material.");

        if (_blurProfile == null)
            throw new InvalidOperationException("MainMenu_Room requires a background blur profile.");

        // These interactive doors create input actions and prompts in Awake.
        foreach (KeyRoomController keyRoom in GetComponentsInChildren<KeyRoomController>(true))
            keyRoom.gameObject.SetActive(false);

        foreach (RoomDoor door in GetComponentsInChildren<RoomDoor>(true))
            door.enabled = false;

        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (NavMeshObstacle obstacle in GetComponentsInChildren<NavMeshObstacle>(true))
            obstacle.enabled = false;

        var cameraObject = new GameObject("MainMenuRoomCamera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.SetPositionAndRotation(_menuCameraPoint.position, _menuCameraPoint.rotation);
        _camera = cameraObject.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.Skybox;
        _camera.backgroundColor = new Color(0.11f, 0.15f, 0.18f, 1f);

        if (_skyboxMaterial != null)
            cameraObject.AddComponent<Skybox>().material = _skyboxMaterial;

        _camera.cullingMask = ~((1 << 5) | (1 << 31));
        _camera.fieldOfView = _fieldOfView;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 200f;
        _camera.allowHDR = true;
        _camera.allowMSAA = false;
        _camera.useOcclusionCulling = false;

        UniversalAdditionalCameraData cameraData =
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderType = CameraRenderType.Base;
        cameraData.SetRenderer(0);
        cameraData.renderPostProcessing = true;
        cameraData.renderShadows = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraData.volumeLayerMask = 1 << 0;
        cameraData.volumeTrigger = _camera.transform;
        _camera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);

        // Follow the menu camera with the same profile used by the gameplay Global Volume.
        CreateCameraVolume("MainMenuPostProcessing", _postProcessProfile, 1101f);
        Volume blurVolume = CreateCameraVolume("MainMenuBackgroundBlur", _blurProfile, 1102f);
        _runtimeBlurProfile = blurVolume.profile;
        if (!_runtimeBlurProfile.TryGet(out _depthOfField))
            throw new InvalidOperationException("The menu blur profile requires a Depth Of Field override.");
        UpdateBlurFocus();

        var backgroundObject = new GameObject("MainMenuRoomBackground",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
        var backgroundTransform = (RectTransform)backgroundObject.transform;
        backgroundTransform.SetParent(menuRoot, false);
        backgroundTransform.SetAsFirstSibling();
        backgroundTransform.anchorMin = Vector2.zero;
        backgroundTransform.anchorMax = Vector2.one;
        backgroundTransform.offsetMin = Vector2.zero;
        backgroundTransform.offsetMax = Vector2.zero;
        backgroundObject.layer = menuRoot.gameObject.layer;
        _background = backgroundObject.GetComponent<UnityEngine.UI.RawImage>();
        _background.raycastTarget = false;
        _background.color = Color.white;
        ResizeRenderTexture();
        HeightFogRendererFeature.SetCameraMaterial(_camera, _fogMaterial);
    }

    private Volume CreateCameraVolume(string objectName, VolumeProfile profile, float priority)
    {
        var volumeObject = new GameObject(objectName);
        volumeObject.transform.SetParent(_camera.transform, false);
        var volumeBounds = volumeObject.AddComponent<BoxCollider>();
        volumeBounds.isTrigger = true;
        volumeBounds.size = Vector3.one * 2f;
        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = false;
        volume.priority = priority;
        volume.blendDistance = 0f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
        return volume;
    }

    private void UpdateBlurFocus()
    {
        if (_depthOfField == null || _camera == null || _characterSpawnPoint == null)
            return;

        float characterDepth = Vector3.Dot(
            _characterSpawnPoint.position - _camera.transform.position, _camera.transform.forward);
        float blurStart = Mathf.Max(0f, characterDepth) + _blurFocusPadding;
        _depthOfField.gaussianStart.value = blurStart;
        _depthOfField.gaussianEnd.value = blurStart + Mathf.Max(0.01f, _blurTransitionDistance);
    }

    public UniTask MoveToCharacterAsync(CancellationToken cancellationToken) =>
        MoveCameraAsync(_characterCameraPoint, cancellationToken);

    public UniTask MoveToMenuAsync(CancellationToken cancellationToken) =>
        MoveCameraAsync(_menuCameraPoint, cancellationToken);

    private async UniTask MoveCameraAsync(Transform target, CancellationToken cancellationToken)
    {
        CancelCameraTransition();
        _isMenuSwayActive = false;
        using var transition = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, destroyCancellationToken);
        _transitionCancellation = transition;

        try
        {
            Vector3 startPosition = _camera.transform.position;
            Quaternion startRotation = _camera.transform.rotation;
            Vector3 targetPosition = target.position;
            Quaternion targetRotation = target.rotation;
            float duration = Mathf.Max(0.01f, _transitionDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, transition.Token);
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _camera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, progress),
                    Quaternion.Slerp(startRotation, targetRotation, progress));
            }

            _menuSwayTime = 0f;
            _isMenuSwayActive = target == _menuCameraPoint;
        }
        finally
        {
            if (ReferenceEquals(_transitionCancellation, transition))
                _transitionCancellation = null;
        }
    }

    public void CancelCameraTransition()
    {
        CancellationTokenSource transition = _transitionCancellation;
        _transitionCancellation = null;
        transition?.Cancel();
    }

    public void Close()
    {
        CancelCameraTransition();
        HeightFogRendererFeature.SetCameraMaterial(_camera, null);
        ReleaseRenderTexture();

        if (_background != null)
        {
            _background.gameObject.SetActive(false);
            Destroy(_background.gameObject);
            _background = null;
        }

        if (_worldRoot != null)
        {
            _worldRoot.SetActive(false);
            Destroy(_worldRoot);
        }
    }

    private void LateUpdate()
    {
        if (_camera != null && _background != null)
            ResizeRenderTexture();

        UpdateMenuCameraSway();
        UpdateBlurFocus();
    }

    private void UpdateMenuCameraSway()
    {
        if (!_isMenuSwayActive || _camera == null)
            return;

        _menuSwayTime += Time.unscaledDeltaTime;
        float phase = _menuSwayTime * (2f * Mathf.PI / Mathf.Max(0.01f, _menuSwayPeriod));
        float fade = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(_menuSwayTime / Mathf.Max(0.01f, _menuSwayFadeDuration)));
        Vector3 angles = new Vector3(
            Mathf.Sin(phase) * _menuSwayAngles.x,
            Mathf.Sin(phase * 0.73f) * _menuSwayAngles.y,
            Mathf.Sin(phase * 0.51f) * _menuSwayAngles.z) * fade;

        _camera.transform.SetPositionAndRotation(_menuCameraPoint.position,
            _menuCameraPoint.rotation * Quaternion.Euler(angles));
    }

    private void ResizeRenderTexture()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (_renderTexture != null && _renderTexture.width == width && _renderTexture.height == height)
            return;

        ReleaseRenderTexture();
        _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR)
        {
            name = "MainMenuRoomRenderTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
        _camera.aspect = width / (float)height;
        _camera.enabled = true;
        _background.texture = _renderTexture;
    }

    private void ReleaseRenderTexture()
    {
        if (_camera != null)
        {
            _camera.enabled = false;
            _camera.targetTexture = null;
        }

        if (_background != null)
            _background.texture = null;

        if (_renderTexture == null)
            return;

        _renderTexture.Release();
        Destroy(_renderTexture);
        _renderTexture = null;
    }

    private void OnDestroy()
    {
        CancelCameraTransition();
        HeightFogRendererFeature.SetCameraMaterial(_camera, null);
        ReleaseRenderTexture();

        if (_runtimeBlurProfile != null)
        {
            foreach (VolumeComponent component in _runtimeBlurProfile.components)
                Destroy(component);
            Destroy(_runtimeBlurProfile);
            _runtimeBlurProfile = null;
            _depthOfField = null;
        }

        if (_background != null)
            Destroy(_background.gameObject);
    }
}
