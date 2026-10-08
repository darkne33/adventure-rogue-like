using System;
using Features.Enemies.Scripts.Level.Scripts;
using Features.Relics.Scripts;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class RoomDoor : MonoBehaviour
{
    private bool _isOpen;
    private DoorType _doorType;
    private RoomDoor _nextRoomEntryDoor;
    private bool _isLevelExit;
    private int _roomRotationQuarterTurns;
    private Room _ownerRoom;
    private InputSystem_Actions _inputActions;

    [SerializeField] private DoorAnimator _doorAnimator;
    [SerializeField] private Color _outlineColor = Color.yellow;
    [SerializeField] private RoomDirection _direction;
    [SerializeField] private Room _nextRoom;
    [SerializeField] private RelicChestInteractionView _interactionView = new();
    [SerializeField] private Transform _interactionPoint;
    [SerializeField, Min(0f)] private float _interactDistance = 4f;

    [Inject] private ITransitToRoomService _transitToRoomService;
    [Inject] private ILevelProgressionService _levelProgressionService;
    [Inject] private IRogueLikeRuntimeDataService _runtimeDataService;
    [Inject] private ICharacterProvider _characterProvider;
    [Inject] private ITimeScaleService _timeScaleService;
    [Inject] private CharacterWallet _characterWallet;

    public RoomDirection AuthoredDirection => _direction;
    public RoomDirection Direction => _direction.RotateClockwise(_roomRotationQuarterTurns);
    public Room NextRoom => _nextRoom;
    public bool IsRewardGate =>
        _nextRoom != null && _nextRoom.RoomData is RewardRoomData or OnlyRelicRoomData or ShopRoomData or BloodRoomData;
    public bool HasConfiguredVisuals => _doorAnimator != null && _doorAnimator.IsConfigured;
    public bool HasRoomDestination => _nextRoom != null;

    private bool HasDestination => HasRoomDestination || _isLevelExit;
    private bool RequiresKey => _nextRoom?.RoomData is RewardRoomData { IsUnlocked: false };

    private void Awake()
    {
        _ownerRoom = GetComponentInParent<Room>();
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

    private void Update()
    {
        bool canInteract = CanInteract();
        if (canInteract && _nextRoom.RoomData is RewardRoomData rewardRoomData)
        {
            int keyPrice = Mathf.Max(1, rewardRoomData.KeyPrice);
            _interactionView.SetKeyPrice(keyPrice,
                _characterWallet != null && _characterWallet.Keys.Count >= keyPrice);
        }

        _interactionView.SetAvailable(canInteract);

        if (canInteract && _inputActions != null &&
            _inputActions.Player.Interact.WasPressedThisFrame())
        {
            TryUnlock();
        }
    }

    private void Start()
    {
        if (_runtimeDataService == null)
            return;

        _runtimeDataService.RoomChanged += OnRoomChanged;
        RefreshOutline();
    }

    private void OnDestroy()
    {
        if (_runtimeDataService != null)
            _runtimeDataService.RoomChanged -= OnRoomChanged;

        _inputActions?.Dispose();
        _inputActions = null;
    }

    public void Configure(Room nextRoom, RoomDoor nextRoomEntryDoor)
    {
        _nextRoom = nextRoom != null
            ? nextRoom
            : throw new ArgumentNullException(nameof(nextRoom));
        _nextRoomEntryDoor = nextRoomEntryDoor != null
            ? nextRoomEntryDoor
            : throw new ArgumentNullException(nameof(nextRoomEntryDoor));
        _isLevelExit = false;
        DoorType destinationType = GetDoorType(nextRoom.RoomData);
        _doorType = destinationType != DoorType.Enemy
            ? destinationType
            : GetDoorType(GetComponentInParent<Room>()?.RoomData);

        Close();
    }

    public void SetDirection(RoomDirection direction)
    {
        _direction = direction;
        _roomRotationQuarterTurns = 0;
    }

    public void SetRoomRotation(int quarterTurns) =>
        _roomRotationQuarterTurns = ((quarterTurns % 4) + 4) % 4;

    public void ConfigureLevelExit()
    {
        _nextRoom = null;
        _nextRoomEntryDoor = null;
        _isLevelExit = true;
        _doorType = GetDoorType(GetComponentInParent<Room>()?.RoomData);

        Close();
    }

    public void ClearDestination()
    {
        ResetDestination();
        _doorAnimator.Hide();
        gameObject.SetActive(false);
    }

    private static DoorType GetDoorType(RoomData roomData) => roomData switch
    {
        OnlyRelicRoomData => DoorType.Item,
        RewardRoomData => DoorType.Reward,
        ShopRoomData => DoorType.Shop,
        BossRoomData => DoorType.Boss,
        BloodRoomData => DoorType.Blood,
        _ => DoorType.Enemy
    };

    public void Close() =>
        SetOpenState(isOpen: false);

    public void Open() =>
        SetOpenState(isOpen: true);

    private void ResetDestination()
    {
        _nextRoom = null;
        _nextRoomEntryDoor = null;
        _isLevelExit = false;
        _isOpen = false;
    }

    private void SetOpenState(bool isOpen)
    {
        _isOpen = isOpen && HasDestination;

        if (!HasDestination)
        {
            RefreshOutline();
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        RefreshDoorVisual();
        RefreshOutline();
    }

    private void OnRoomChanged(RoomData previousRoom, RoomData currentRoom)
    {
        if (_isOpen)
            RefreshDoorVisual();

        RefreshOutline();
    }

    private void RefreshDoorVisual()
    {
        if (_isOpen && !RequiresKey)
            _doorAnimator.Open(_doorType);
        else
            _doorAnimator.Close(_doorType);
    }

    private bool CanInteract()
    {
        if (!_isOpen || !RequiresKey || _ownerRoom == null ||
            _runtimeDataService?.CurrentRoomData != _ownerRoom.RoomData ||
            _timeScaleService == null || _timeScaleService.IsPaused ||
            _characterProvider?.CharacterFacade == null)
        {
            return false;
        }

        Vector3 interactionPosition = _interactionPoint != null
            ? _interactionPoint.position
            : transform.position;
        Vector3 offset = interactionPosition - _characterProvider.CharacterFacade.transform.position;
        offset.y = 0f;
        float interactDistance = Mathf.Max(0f, _interactDistance);
        return offset.sqrMagnitude <= interactDistance * interactDistance;
    }

    private void TryUnlock()
    {
        if (!CanInteract() || _nextRoom.RoomData is not RewardRoomData rewardRoomData)
            return;

        int keyPrice = Mathf.Max(1, rewardRoomData.KeyPrice);
        if (_characterWallet == null || _characterWallet.Keys.Count < keyPrice)
            return;

        _characterWallet.Keys.Remove(keyPrice);
        rewardRoomData.Unlock();
        _interactionView.SetAvailable(false);
        RefreshDoorVisual();
        RefreshOutline();
    }

    private void RefreshOutline()
    {
        bool isHighlighted = _isOpen && _nextRoom != null && _nextRoom.RoomData != null &&
                             _runtimeDataService != null &&
                             !_runtimeDataService.HasVisitedRoom(_nextRoom.RoomData);
        _doorAnimator.SetHighlight(isHighlighted, _outlineColor);
    }

    private void OnTriggerEnter(Collider other) =>
        TryTransit(other);

    private void OnTriggerStay(Collider other) =>
        TryTransit(other);

    private void TryTransit(Collider other)
    {
        if (!_isOpen || RequiresKey)
            return;

        CharacterFacade characterFacade = other.GetComponentInParent<CharacterFacade>();
        if (characterFacade == null)
            return;

        _isOpen = false;
        RefreshOutline();

        if (_isLevelExit)
            _levelProgressionService.TransitToNextLevel();
        else
            _transitToRoomService.Transit(_nextRoom, _nextRoomEntryDoor);
    }
}
