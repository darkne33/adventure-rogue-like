using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;
using Zenject;

public class RoomDoor : MonoBehaviour
{
    private bool _isOpen;

    [SerializeField] private Transform _leftDoor;
    [SerializeField] private Transform _rightDoor;
    [SerializeField] private RoomDirection _direction;
    [SerializeField] private Room _nextRoom;

    [Inject] private ITransitToRoomService _transitToRoomService;
    [Inject] private ILevelProgressionService _levelProgressionService;

    public RoomDirection Direction => _direction;

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
        gameObject.SetActive(true);
        Close();
    }

    public void ClearDestination()
    {
        _nextRoom = null;
        _nextRoomEntryDoor = null;
        _isLevelExit = false;
        _isOpen = false;
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
        _leftDoor.gameObject.SetActive(true);
        _rightDoor.gameObject.SetActive(true);
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
        _leftDoor.gameObject.SetActive(false);
        _rightDoor.gameObject.SetActive(false);
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
