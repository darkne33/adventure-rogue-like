using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;
using Zenject;

public class RoomDoor : MonoBehaviour
{
    private bool _isOpen;
    
    [SerializeField] private Transform _leftDoor;
    [SerializeField] private Transform _rightDoor;

    [SerializeField] private Room _nextRoom;

    [Inject] private ITransitToRoomService _transitToRoomService;
    
    public void Close()
    {
        _isOpen = false;
        _leftDoor.gameObject.SetActive(true);
        _rightDoor.gameObject.SetActive(true);
    }

    public void Open()
    {
        _isOpen = true;
        _leftDoor.gameObject.SetActive(false);
        _rightDoor.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterFacade characterFacade = other.GetComponent<CharacterFacade>();
        if (characterFacade != null && _isOpen)
        {
            _transitToRoomService.Transit(_nextRoom);
        }
    }
}