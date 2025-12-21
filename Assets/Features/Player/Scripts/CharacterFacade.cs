using Core;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    [Header("Configurations")]
    
    [Inject] private CharacterSettingsConfiguration _characterSettingsConfiguration;
    [Inject] private CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;

    [HorizontalLine]
    
    [SerializeField] private PlayerInput _input;

    private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;

    private Rigidbody _rigidbody;

    [Inject] private ICameraService _cameraService;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _moveSystem =
            new CharacterMoveSystem(_rigidbody, _input, _cameraService, _characterSettingsConfiguration);
        _cameraSystem = new CharacterCameraMoveSystem(_cameraService.MainCamera, transform, _characterCameraSettingsConfiguration);
    }

    private void FixedUpdate()
    {
        _moveSystem.Move();
        _moveSystem.Rotate();
        _cameraSystem.Move();
    }
}