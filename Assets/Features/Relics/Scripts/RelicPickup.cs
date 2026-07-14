using System;
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
        private const float AutoCollectDuration = 0.45f;
        private const float AutoCollectArcHeight = 1.1f;
        private const float AutoCollectTargetHeight = 1.2f;

        [Inject] private ICameraService _cameraService;
        [Inject] private CharacterStats _characterStats;

        private InputSystem_Actions _inputActions;

        private RelicDefinition _relic;
        private RelicChestConfiguration _configuration;
        private RelicManager _relicManager;
        private RelicEventBus _eventBus;
        private ICharacterProvider _characterProvider;
        private RoomData _roomData;
        private Room _room;
        private SpriteRenderer _spriteRenderer;
        private Action _collectedCallback;
        private bool _isPicked;

        private void Awake() =>
            _inputActions = new InputSystem_Actions();

        public void Construct(RelicDefinition relic, RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, ICharacterProvider characterProvider,
            RoomData roomData, Room room, bool collectImmediately = false, Action collectedCallback = null)
        {
            Initialize(relic, configuration, relicManager, eventBus, characterProvider, roomData, room,
                collectedCallback);

            if (collectImmediately)
                AutoCollect().Forget();
            else
                AnimateDrop();
        }

        private void Initialize(RelicDefinition relic, RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, ICharacterProvider characterProvider,
            RoomData roomData, Room room, Action collectedCallback)
        {
            _relic = relic;
            _configuration = configuration;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _roomData = roomData;
            _room = room;
            _collectedCallback = collectedCallback;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = relic.Icon;
                _spriteRenderer.sortingOrder = 10;
            }

            transform.localScale = Vector3.one * 1.15f;
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
            if (_isPicked || _configuration == null || _characterProvider?.CharacterFacade == null)
                return;

            Transform character = _characterProvider.CharacterFacade.transform;
            if (Vector3.Distance(transform.position, character.position) > GetPickupDistance())
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
            await ActivateAndDestroy();
        }

        private async UniTaskVoid AutoCollect()
        {
            if (_isPicked)
                return;

            _isPicked = true;
            await FlyToCharacter();
            await ActivateAndDestroy();
        }

        private async UniTask FlyToCharacter()
        {
            Transform character = _characterProvider?.CharacterFacade != null
                ? _characterProvider.CharacterFacade.transform
                : null;

            if (character == null)
                return;

            Vector3 startPosition = transform.position;
            Tween rotateTween = transform.DORotate(new Vector3(0f, 360f, 0f), AutoCollectDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(gameObject);

            Tween scaleTween = transform.DOScale(Vector3.one * 1.45f, AutoCollectDuration * 0.45f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);

            Tween flyTween = DOVirtual.Float(0f, 1f, AutoCollectDuration, progress =>
                {
                    if (this == null)
                        return;

                    Vector3 targetPosition = character.position + Vector3.up * AutoCollectTargetHeight;
                    Vector3 position = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
                    position.y += Mathf.Sin(progress * Mathf.PI) * AutoCollectArcHeight;
                    transform.position = position;
                })
                .SetEase(Ease.InCubic)
                .SetLink(gameObject);

            try
            {
                await flyTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            finally
            {
                rotateTween.Kill();
                scaleTween.Kill();
            }
        }

        private async UniTask ActivateAndDestroy()
        {
            if (TryActivate() == false)
            {
                _isPicked = false;
                return;
            }

            transform.DOKill();

            await transform.DOScale(Vector3.one * 1.7f, 0.12f)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            await transform.DOScale(Vector3.zero, 0.14f)
                .SetEase(Ease.InBack)
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            Destroy(gameObject);
        }

        private bool TryActivate()
        {
            if (_relicManager.AddRelic(_relic) == false)
                return false;

            _collectedCallback?.Invoke();
            _collectedCallback = null;
            _eventBus.PublishChestCollected(_roomData, _room);
            return true;
        }

        private float GetPickupDistance()
        {
            float pickupRangeMultiplier = 1f + Mathf.Max(0f, _characterStats?.PickupRange ?? 0f) * 0.01f;
            return _configuration.RelicPickupDistance * pickupRangeMultiplier;
        }
    }
}
