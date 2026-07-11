using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;
using Zenject;

public class RoomDoor : MonoBehaviour
{
    private bool _isOpen;
    private bool _usesRewardDoor;

    [SerializeField] private GameObject _enemyDoor;
    [SerializeField] private GameObject _enemyLeftDoor;
    [SerializeField] private GameObject _enemyRightDoor;
    [SerializeField] private GameObject _rewardDoor;
    [SerializeField] private GameObject _rewardLeftDoor;
    [SerializeField] private GameObject _rewardRightDoor;
    [SerializeField] private RoomDirection _direction;
    [SerializeField] private Room _nextRoom;

    [Inject] private ITransitToRoomService _transitToRoomService;
    [Inject] private ILevelProgressionService _levelProgressionService;

    public RoomDirection Direction => _direction;
    public Room NextRoom => _nextRoom;
    public bool IsRewardGate => _usesRewardDoor;
    public bool HasConfiguredVisuals =>
        _enemyDoor != null &&
        _enemyLeftDoor != null &&
        _enemyRightDoor != null &&
        _rewardDoor != null &&
        _rewardLeftDoor != null &&
        _rewardRightDoor != null;

    private RoomDoor _nextRoomEntryDoor;
    private bool _isLevelExit;

    private bool HasDestination => _nextRoom != null || _isLevelExit;
    public bool HasRoomDestination => _nextRoom != null;

    public void Configure(Room nextRoom, RoomDoor nextRoomEntryDoor)
    {
        _nextRoom = nextRoom != null
            ? nextRoom
            : throw new System.ArgumentNullException(nameof(nextRoom));
        _nextRoomEntryDoor = nextRoomEntryDoor != null
            ? nextRoomEntryDoor
            : throw new System.ArgumentNullException(nameof(nextRoomEntryDoor));
        _isLevelExit = false;
        _usesRewardDoor = nextRoom.RoomData is RewardRoomData;
        gameObject.SetActive(true);
        Close();
    }

    public void SetDirection(RoomDirection direction) =>
        _direction = direction;

    public void ConfigureLevelExit()
    {
        _nextRoom = null;
        _nextRoomEntryDoor = null;
        _isLevelExit = true;
        _usesRewardDoor = false;
        gameObject.SetActive(true);
        Close();
    }

    public void ClearDestination()
    {
        _nextRoom = null;
        _nextRoomEntryDoor = null;
        _isLevelExit = false;
        _isOpen = false;
        SetDoorVariant(enemyVisible: false, rewardVisible: false);
        gameObject.SetActive(false);
    }

    public void Close()
    {
        _isOpen = false;

        if (!HasDestination)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetDoorVariant(
            enemyVisible: !_usesRewardDoor,
            rewardVisible: _usesRewardDoor);
        SetSelectedDoorLeaves(active: true);
    }

    public void Open()
    {
        if (!HasDestination)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _isOpen = true;
        SetDoorVariant(
            enemyVisible: !_usesRewardDoor,
            rewardVisible: _usesRewardDoor);
        SetSelectedDoorLeaves(active: false);
    }

    private void EnsureDoorVisualsConfigured()
    {
        if (!HasConfiguredVisuals)
            throw new MissingReferenceException(
                $"{name} must contain assigned EnemyDoor and RewardDoor roots and two door leaves for each variant.");
    }

    private void SetDoorVariant(bool enemyVisible, bool rewardVisible)
    {
        EnsureDoorVisualsConfigured();
        _enemyDoor.SetActive(enemyVisible);
        _rewardDoor.SetActive(rewardVisible);
    }

    private void SetSelectedDoorLeaves(bool active)
    {
        GameObject leftDoor = _usesRewardDoor ? _rewardLeftDoor : _enemyLeftDoor;
        GameObject rightDoor = _usesRewardDoor ? _rewardRightDoor : _enemyRightDoor;

        leftDoor.SetActive(active);
        rightDoor.SetActive(active);
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

        if (_isLevelExit)
            _levelProgressionService.TransitToNextLevel();
        else
            _transitToRoomService.Transit(_nextRoom, _nextRoomEntryDoor);
    }
}
