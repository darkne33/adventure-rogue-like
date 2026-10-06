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
        [Inject] private ITimeScaleService _timeScaleService;
        [Inject] private IRogueLikeRuntimeDataService _runtimeDataService;

        [SerializeField] private ParticleSystem[] _treasureCircleRaysParticles;
        [SerializeField] private bool _autoCollectOnApproach;
        [SerializeField] private bool _levitate;
        [SerializeField, Min(0f)] private float _levitationHeight = 0.25f;
        [SerializeField, Min(0.1f)] private float _levitationHalfPeriod = 1f;

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
        private Action _destroyedCallback;
        private Tween _levitationTween;
        private Vector3 _levitationOrigin;
        private bool _hasLevitationOrigin;
        private bool _isPicked;

        private void Awake() =>
            _inputActions = new InputSystem_Actions();

        public void Construct(RelicDefinition relic, RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, ICharacterProvider characterProvider,
            RoomData roomData, Room room, bool collectImmediately = false,
            Action collectedCallback = null, Action destroyedCallback = null)
        {
            _destroyedCallback = destroyedCallback;
            Initialize(relic, configuration, relicManager, eventBus, characterProvider, roomData, room,
                collectedCallback);

            if (collectImmediately)
                AutoCollect().Forget();
            else if (_levitate)
            {
                _levitationOrigin = transform.position;
                _hasLevitationOrigin = true;
                AnimateLevitation();
            }
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

            SetVisual(relic);
            transform.localScale = Vector3.one * 1.15f;
        }

        public void SetVisual(RelicDefinition relic)
        {
            if (relic == null)
                return;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = relic.Icon;
                _spriteRenderer.sortingOrder = 10;
            }

            ApplyRarityColor(relic.Rarity);
        }

        private void ApplyRarityColor(RelicRarity rarity)
        {
            if (_treasureCircleRaysParticles == null)
                return;

            Color color = RelicRarityPalette.GetColor(rarity);

            foreach (ParticleSystem particleSystem in _treasureCircleRaysParticles)
            {
                if (particleSystem == null)
                    continue;

                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = color;
            }
        }

        private void OnEnable()
        {
            _inputActions ??= new InputSystem_Actions();
            _inputActions.Player.Interact.Enable();
            if (_hasLevitationOrigin && !_isPicked)
                AnimateLevitation();
        }

        private void OnDisable()
        {
            _inputActions?.Player.Interact.Disable();
            StopLevitation();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _inputActions = null;
            _destroyedCallback?.Invoke();
            _destroyedCallback = null;
        }

        private void Update()
        {
            if (_timeScaleService.IsPaused || _isPicked || _configuration == null ||
                _characterProvider?.CharacterFacade == null)
                return;

            Transform character = _characterProvider.CharacterFacade.transform;
            Vector3 offset = (_autoCollectOnApproach ? _levitationOrigin : transform.position) -
                             character.position;
            if (_autoCollectOnApproach)
            {
                if (!ReferenceEquals(_runtimeDataService.CurrentRoomData, _roomData))
                    return;
                offset.y = 0f;
            }
            if (offset.sqrMagnitude > GetPickupDistance() * GetPickupDistance())
                return;

            if (_autoCollectOnApproach)
            {
                AutoCollect().Forget();
                return;
            }

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

        private void AnimateLevitation()
        {
            StopLevitation();
            transform.position = _levitationOrigin;
            _levitationTween = transform.DOMoveY(_levitationOrigin.y + _levitationHeight,
                    _levitationHalfPeriod)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void StopLevitation()
        {
            _levitationTween?.Kill();
            _levitationTween = null;
        }

        private async UniTaskVoid PickUp()
        {
            if (_isPicked)
                return;

            _isPicked = true;
            await OfferAndDestroy();
        }

        private async UniTaskVoid AutoCollect()
        {
            if (_isPicked)
                return;

            _isPicked = true;
            StopLevitation();
            await FlyToCharacter();
            await OfferAndDestroy();
        }

        public async UniTask<bool> CollectImmediatelyAsync(RelicDefinition relic,
            RelicChestConfiguration configuration, RelicManager relicManager,
            RelicEventBus eventBus, ICharacterProvider characterProvider, RoomData roomData,
            Room room, Action collectedCallback = null)
        {
            if (_isPicked)
                return false;

            Initialize(relic, configuration, relicManager, eventBus, characterProvider, roomData,
                room, collectedCallback);
            _isPicked = true;

            await FlyToCharacter();
            return await OfferAndDestroy();
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

        private async UniTask<bool> OfferAndDestroy()
        {
            if (TryConsumePickup() == false)
            {
                _isPicked = false;
                if (_hasLevitationOrigin)
                    AnimateLevitation();
                return false;
            }

            transform.DOKill();

            await transform.DOScale(Vector3.one * 1.7f, 0.12f)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            await transform.DOScale(Vector3.zero, 0.14f)
                .SetEase(Ease.InBack)
                .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            _eventBus.PublishRelicOffered(_relic);
            Destroy(gameObject);
            return true;
        }

        private bool TryConsumePickup()
        {
            if (_relicManager.CanAddRelic(_relic) == false)
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
