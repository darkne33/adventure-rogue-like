using UnityEngine;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicPickup : MonoBehaviour
    {
        [Inject] private ICameraService _cameraService;

        private InputSystem_Actions _inputActions;

        private RelicDefinition _relic;
        private RelicChestConfiguration _configuration;
        private RelicManager _relicManager;
        private RelicEventBus _eventBus;
        private ICharacterProvider _characterProvider;
        private RoomData _roomData;
        private SpriteRenderer _spriteRenderer;
        private bool _isPicked;

        private void Awake() =>
            _inputActions = new InputSystem_Actions();

        public void Construct(RelicDefinition relic, RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, ICharacterProvider characterProvider,
            RoomData roomData)
        {
            _relic = relic;
            _configuration = configuration;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _roomData = roomData;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = relic.Icon;
                _spriteRenderer.sortingOrder = 10;
            }

            transform.localScale = Vector3.one * 1.15f;

            AnimateDrop();
        }

        private void OnEnable()
        {
            _inputActions ??= new InputSystem_Actions();
            _inputActions.Player.Interact.Enable();
        }

        private void OnDisable() =>
            _inputActions?.Player.Interact.Disable();

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void Update()
        {
            if (_isPicked || _characterProvider?.CharacterFacade == null)
                return;

            Transform character = _characterProvider.CharacterFacade.transform;
            if (Vector3.Distance(transform.position, character.position) > _configuration.RelicPickupDistance)
                return;

            if (_inputActions != null && _inputActions.Player.Interact.WasPressedThisFrame())
                PickUp().Forget();
        }

        private void LateUpdate()
        {
            Transform cameraTransform = _cameraService?.MainCamera != null
                ? _cameraService.MainCamera.transform
                : Camera.main != null
                    ? Camera.main.transform
                    : null;

            if (cameraTransform == null)
                return;

            Vector3 direction = transform.position - cameraTransform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void AnimateDrop()
        {
            Vector3 endPosition = transform.position + Vector3.down * 0.75f;
            _ = transform.DOMove(endPosition, 0.45f).SetEase(Ease.OutBounce).SetLink(gameObject);
            _ = transform.DORotate(new Vector3(0f, 360f, 0f), 1.4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(gameObject);
            _ = transform.DOPunchScale(Vector3.one * 0.25f, 0.5f, 4, 0.6f).SetLink(gameObject);
        }

        private async UniTaskVoid PickUp()
        {
            if (_isPicked)
                return;

            _isPicked = true;
            if (_relicManager.AddRelic(_relic) == false)
            {
                _isPicked = false;
                return;
            }

            _eventBus.PublishChestCollected(_roomData);
            await transform.DOScale(Vector3.one * 1.7f, 0.12f)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            await transform.DOScale(Vector3.zero, 0.14f)
                .SetEase(Ease.InBack)
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            Destroy(gameObject);
        }
    }
}
