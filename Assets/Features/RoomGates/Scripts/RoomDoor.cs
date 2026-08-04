using System;
using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public sealed class RoomDoor : MonoBehaviour
{
    private bool _isOpen;
    private DoorType _doorType;
    private RoomDoor _nextRoomEntryDoor;
    private bool _isLevelExit;

    [SerializeField] private DoorAnimator _doorAnimator;
    [SerializeField] private RoomDirection _direction;
    [SerializeField] private Room _nextRoom;

    [Inject] private ITransitToRoomService _transitToRoomService;
    [Inject] private ILevelProgressionService _levelProgressionService;

    public RoomDirection Direction => _direction;
    public Room NextRoom => _nextRoom;
    public bool IsRewardGate => _doorType == DoorType.Reward;
    public bool HasConfiguredVisuals => _doorAnimator != null && _doorAnimator.IsConfigured;
    public bool HasRoomDestination => _nextRoom != null;

    private bool HasDestination => HasRoomDestination || _isLevelExit;

    public void Configure(Room nextRoom, RoomDoor nextRoomEntryDoor)
    {
        _nextRoom = nextRoom != null
            ? nextRoom
            : throw new ArgumentNullException(nameof(nextRoom));
        _nextRoomEntryDoor = nextRoomEntryDoor != null
            ? nextRoomEntryDoor
            : throw new ArgumentNullException(nameof(nextRoomEntryDoor));
        _isLevelExit = false;
        _doorType = nextRoom.RoomData is RewardRoomData or ShopRoomData
            ? DoorType.Reward
            : DoorType.Enemy;

        Close();
    }

    public void SetDirection(RoomDirection direction) =>
        _direction = direction;

    public void ConfigureLevelExit()
    {
        _nextRoom = null;
        _nextRoomEntryDoor = null;
        _isLevelExit = true;
        _doorType = DoorType.Enemy;

        Close();
    }

    public void ClearDestination()
    {
        ResetDestination();
        _doorAnimator.Hide();
        gameObject.SetActive(false);
    }

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
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (_isOpen)
            _doorAnimator.Open(_doorType);
        else
            _doorAnimator.Close(_doorType);
    }

    private void OnTriggerEnter(Collider other) =>
        TryTransit(other);

    private void OnTriggerStay(Collider other) =>
        TryTransit(other);

    private void TryTransit(Collider other)
    {
        if (!_isOpen)
            return;

        CharacterFacade characterFacade = other.GetComponentInParent<CharacterFacade>();
        if (characterFacade == null)
            return;

        _isOpen = false;

        if (_isLevelExit)
            _levelProgressionService.TransitToNextLevel();
        else
            _transitToRoomService.Transit(_nextRoom, _nextRoomEntryDoor);
    }
}
