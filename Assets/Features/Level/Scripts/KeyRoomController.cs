using System;
using System.Collections;
using DG.Tweening;
using Features.Relics.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

[DisallowMultipleComponent]
public sealed class KeyRoomController : MonoBehaviour
{
    [Inject] private ICharacterProvider _characterProvider;
    [Inject] private ITimeScaleService _timeScaleService;
    [Inject] private CharacterWallet _characterWallet;
    [Inject] private RelicChestSpawner _relicChestSpawner;

    [Header("References")]
    [SerializeField] private Transform _directionDoor;
    [SerializeField] private Transform _door;
    [SerializeField] private Renderer _doorRenderer;
    [SerializeField] private Collider _doorCollider;
    [SerializeField] private Transform _chestSpawnPoint;
    [SerializeField] private RelicChestInteractionView _interactionView = new();

    [Header("Interaction")]
    [SerializeField, Min(1)] private int _keyPrice = 1;
    [SerializeField, Min(0f)] private float _interactDistance = 4f;

    [Header("Door Fade")]
    [SerializeField, Min(0f)] private float _doorFadeDuration = 0.5f;

    private InputSystem_Actions _inputActions;
    private Room _ownerRoom;
    private MaterialPropertyBlock _doorPropertyBlock;
    private Color _doorColor = Color.white;
    private Tween _doorFadeTween;
    private float _doorAlpha = 1f;
    private RelicChest _spawnedChest;
    private bool _isInitialized;
    private bool _isOpening;
    private bool _isOpen;

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _interactionView ??= new RelicChestInteractionView();
        _interactionView.Initialize(gameObject);
    }

    private void OnEnable()
    {
        _inputActions ??= new InputSystem_Actions();
        _inputActions.Player.Interact.Enable();
    }

    private void OnDisable()
    {
        _inputActions?.Player.Interact.Disable();
        _interactionView?.SetAvailable(false, true);
    }

    private void OnDestroy()
    {
        _doorFadeTween?.Kill();
        _inputActions?.Dispose();
        _inputActions = null;
    }

    private void Update()
    {
        bool canInteract = CanInteract();
        _interactionView.SetAvailable(canInteract);

        if (canInteract && _inputActions != null &&
            _inputActions.Player.Interact.WasPressedThisFrame())
        {
            TryOpen();
        }
    }

    public void Initialize(Room ownerRoom, Vector3 entryForward)
    {
        _ownerRoom = ownerRoom != null
            ? ownerRoom
            : throw new ArgumentNullException(nameof(ownerRoom));

        ValidateConfiguration();
        AlignWithEntryDirection(entryForward);
        InitializeDoorVisual();

        _door.gameObject.SetActive(true);
        _doorRenderer.enabled = true;
        _doorCollider.enabled = true;
        SetDoorAlpha(1f);

        _isOpening = false;
        _isOpen = false;
        _isInitialized = true;

        SpawnChest();
    }

    public void ValidateConfiguration()
    {
        if (_directionDoor == null)
            throw new InvalidOperationException($"{name} does not have a DirectionDoor reference.");
        if (_door == null)
            throw new InvalidOperationException($"{name} does not have a Door reference.");
        if (_doorRenderer == null)
            throw new InvalidOperationException($"{name} does not have a Door renderer reference.");
        if (_doorCollider == null)
            throw new InvalidOperationException($"{name} does not have a Door collider reference.");
        if (_chestSpawnPoint == null)
            throw new InvalidOperationException($"{name} does not have a chest spawn point.");
    }

    private bool CanInteract()
    {
        if (!_isInitialized || _isOpening || _isOpen ||
            _timeScaleService == null || _timeScaleService.IsPaused ||
            _characterProvider?.CharacterFacade == null)
        {
            return false;
        }

        CharacterFacade character = _characterProvider.CharacterFacade;
        Vector3 offset = _door.position - character.transform.position;
        offset.y = 0f;
        float interactDistance = Mathf.Max(0f, _interactDistance);
        return offset.sqrMagnitude <= interactDistance * interactDistance;
    }

    private void TryOpen()
    {
        int keyPrice = Mathf.Max(1, _keyPrice);
        if (_characterWallet == null || _characterWallet.Keys.Count < keyPrice)
            return;

        _characterWallet.Keys.Remove(keyPrice);
        _isOpening = true;
        _interactionView.SetAvailable(false);
        _doorCollider.enabled = false;

        if (_doorFadeDuration <= 0f)
        {
            SetDoorAlpha(0f);
            CompleteOpening();
            return;
        }

        _doorFadeTween?.Kill();
        _doorFadeTween = DOTween.To(() => _doorAlpha, SetDoorAlpha, 0f,
                _doorFadeDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject)
            .OnComplete(CompleteOpening);
    }

    private void CompleteOpening()
    {
        if (_isOpen)
            return;

        _isOpening = false;
        _isOpen = true;
        _doorRenderer.enabled = false;

        StartCoroutine(EnableChestInteractionNextFrame());
    }

    private void SpawnChest()
    {
        if (_spawnedChest != null)
            return;

        if (_ownerRoom == null ||
            !_relicChestSpawner.TrySpawnAt(_ownerRoom, _chestSpawnPoint,
                out RelicChest chest))
        {
            Debug.LogWarning($"Could not spawn a relic chest in {name}.", this);
            return;
        }

        _spawnedChest = chest;
        _spawnedChest.enabled = false;
    }

    private IEnumerator EnableChestInteractionNextFrame()
    {
        yield return null;

        if (_spawnedChest != null)
            _spawnedChest.enabled = true;
    }

    private void InitializeDoorVisual()
    {
        _doorPropertyBlock ??= new MaterialPropertyBlock();

        Material material = _doorRenderer.sharedMaterial;
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            _doorColor = material.GetColor("_BaseColor");
        else if (material.HasProperty("_Color"))
            _doorColor = material.GetColor("_Color");
    }

    private void SetDoorAlpha(float alpha)
    {
        _doorAlpha = Mathf.Clamp01(alpha);
        if (_doorRenderer == null)
            return;

        _doorPropertyBlock ??= new MaterialPropertyBlock();
        _doorRenderer.GetPropertyBlock(_doorPropertyBlock);

        Color color = _doorColor;
        color.a = _doorAlpha;

        Material material = _doorRenderer.sharedMaterial;
        if (material != null && material.HasProperty("_BaseColor"))
            _doorPropertyBlock.SetColor("_BaseColor", color);
        if (material != null && material.HasProperty("_Color"))
            _doorPropertyBlock.SetColor("_Color", color);

        _doorRenderer.SetPropertyBlock(_doorPropertyBlock);
    }

    private void AlignWithEntryDirection(Vector3 entryForward)
    {
        Vector3 markerForward = Vector3.ProjectOnPlane(_directionDoor.forward, Vector3.up);
        Vector3 targetForward = Vector3.ProjectOnPlane(entryForward, Vector3.up);

        if (markerForward.sqrMagnitude <= Mathf.Epsilon ||
            targetForward.sqrMagnitude <= Mathf.Epsilon)
        {
            throw new InvalidOperationException(
                "Key_Room and entry door directions must have a horizontal component.");
        }

        float yaw = Vector3.SignedAngle(markerForward, targetForward, Vector3.up);
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * transform.rotation;
    }
}
