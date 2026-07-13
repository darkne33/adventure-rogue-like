using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicChest : MonoBehaviour
    {
        private static readonly int ChestCameraOpeningTrigger =
            Animator.StringToHash("ChestCameraOpening");
        private static readonly int ClaimTrigger = Animator.StringToHash("Claim");
        private static readonly int OpenTrigger = Animator.StringToHash("Open");

        private const float MinimumOpeningDuration = 1f;
        private const float MinimumClaimHoldDuration = 0.3f;

        [Inject] private IPanelService _panelService;

        private InputSystem_Actions _inputActions;

        public bool IsOpened => _isOpened;
        public Room Room => _room;
        public RoomData RoomData => _roomData;

        [SerializeField] private Outline _outline;
        [SerializeField] private CanvasGroup _interactionPromptCanvasGroup;
        [SerializeField] private Transform _interactionPromptTransform;
        [SerializeField, Min(0f)] private float _promptShowDuration = 0.14f;
        [SerializeField, Min(0f)] private float _promptHideDuration = 0.12f;
        [SerializeField] private ParticleSystem[] _treasureVerticalRaysParticles;
        [SerializeField] private Transform _characterPosition;
        [SerializeField] private GameObject _chestCamera;
        [SerializeField] private Animator _chestCameraAnimator;
        [SerializeField] private Animator _chestAnimator;

        private RelicDefinition _relic;
        private RelicChestConfiguration _configuration;
        private RelicManager _relicManager;
        private RelicEventBus _eventBus;
        private ICharacterProvider _characterProvider;
        private DiContainer _container;
        private RoomData _roomData;
        private Room _room;
        private bool _isOpened;
        private bool _isInteractionAvailable;
        private Vector3 _promptVisibleScale = Vector3.one;
        private Vector3 _promptHiddenScale = Vector3.one * 0.82f;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            if (_interactionPromptTransform != null)
            {
                _promptVisibleScale = _interactionPromptTransform.localScale;
                _promptHiddenScale = _promptVisibleScale * 0.82f;
            }

            SetInteractionVisuals(false, true);
        }

        public void Construct(RelicDefinition relic, RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, ICharacterProvider characterProvider,
            DiContainer container, RoomData roomData, Room room)
        {
            _relic = relic;
            _configuration = configuration;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _container = container;
            _roomData = roomData;
            _room = room;
        }

        private void OnEnable()
        {
            _inputActions ??= new InputSystem_Actions();
            _inputActions.Player.Interact.Enable();
        }

        private void OnDisable()
        {
            _inputActions?.Player.Interact.Disable();
            SetInteractionVisuals(false, true);
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void Update()
        {
            bool canInteract = CanInteract();
            SetInteractionVisuals(canInteract);

            if (!canInteract)
                return;

            if (_inputActions != null && _inputActions.Player.Interact.WasPressedThisFrame())
                Open();
        }

        private bool CanInteract()
        {
            if (_isOpened || _configuration == null || _characterProvider?.CharacterFacade == null)
                return false;

            if (_characterProvider.CharacterFacade.IsChestOpening)
                return false;

            return Vector3.Distance(transform.position,
                _characterProvider.CharacterFacade.transform.position) <= _configuration.InteractDistance;
        }

        private void SetInteractionVisuals(bool isAvailable, bool instantly = false)
        {
            if (_isInteractionAvailable == isAvailable && !instantly)
                return;

            _isInteractionAvailable = isAvailable;

            if (_outline != null)
                _outline.enabled = isAvailable;

            if (_interactionPromptCanvasGroup == null || _interactionPromptTransform == null)
                return;

            _interactionPromptCanvasGroup.DOKill();
            _interactionPromptTransform.DOKill();

            if (instantly)
            {
                _interactionPromptCanvasGroup.alpha = isAvailable ? 1f : 0f;
                _interactionPromptTransform.localScale = isAvailable ? _promptVisibleScale : _promptHiddenScale;
                return;
            }

            float duration = isAvailable ? _promptShowDuration : _promptHideDuration;
            _ = _interactionPromptCanvasGroup.DOFade(isAvailable ? 1f : 0f, duration)
                .SetEase(isAvailable ? Ease.OutQuad : Ease.InQuad)
                .SetLink(gameObject);
            _ = _interactionPromptTransform.DOScale(isAvailable ? _promptVisibleScale : _promptHiddenScale, duration)
                .SetEase(isAvailable ? Ease.OutBack : Ease.InQuad)
                .SetLink(gameObject);
        }

        private void Open()
        {
            if (_isOpened)
                return;

            if (_characterPosition == null || _chestCamera == null || _chestCameraAnimator == null ||
                _chestAnimator == null)
            {
                Debug.LogError($"{name} is missing chest opening sequence references.", this);
                return;
            }

            OpenSequenceAsync().Forget();
        }

        private async UniTask OpenSequenceAsync()
        {
            CharacterFacade character = _characterProvider?.CharacterFacade;
            if (character == null || character.IsChestOpening)
                return;

            bool characterLockAcquired = false;
            CancellationToken cancellationToken = default;
            CharacterPanelPresenter characterPanelPresenter = null;
            CharacterPanel characterPanel = null;
            CanvasGroup characterPanelCanvasGroup = null;
            bool wasCharacterPanelStateCaptured = false;
            float characterPanelAlpha = 0f;
            bool wasCharacterPanelInteractable = false;
            bool wasCharacterPanelBlockingRaycasts = false;

            try
            {
                if (character.TryEnterChestOpening(_characterPosition) == false)
                    return;

                characterLockAcquired = true;
                _isOpened = true;
                SetInteractionVisuals(false);
                cancellationToken = this.GetCancellationTokenOnDestroy();

                characterPanelPresenter = _panelService?
                    .GetPanelPresenter<CharacterPanelPresenter>(PanelName.CharacterPanel);
                characterPanel = characterPanelPresenter?.Panel;
                if (characterPanel != null)
                {
                    characterPanelCanvasGroup = characterPanel.GetComponent<CanvasGroup>();
                    if (characterPanelCanvasGroup != null)
                    {
                        characterPanelAlpha = characterPanelCanvasGroup.alpha;
                        wasCharacterPanelInteractable = characterPanelCanvasGroup.interactable;
                        wasCharacterPanelBlockingRaycasts = characterPanelCanvasGroup.blocksRaycasts;
                        wasCharacterPanelStateCaptured = true;
                        characterPanelPresenter.ForceHide();
                    }
                }

                _chestCamera.SetActive(true);
                _chestCameraAnimator.SetTrigger(ChestCameraOpeningTrigger);
                character.StartChestOpeningAnimation();
                _eventBus.PublishChestOpened(transform.position);

                float openingDuration = Mathf.Max(MinimumOpeningDuration, _configuration.OpeningDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(openingDuration),
                    cancellationToken: cancellationToken);

                character.EndChestOpeningAnimation();
                _chestCameraAnimator.SetTrigger(ClaimTrigger);
                _chestAnimator.enabled = true;
                _chestAnimator.SetTrigger(OpenTrigger);
                SpawnPickup();

                float claimHoldDuration = Mathf.Max(MinimumClaimHoldDuration,
                    _configuration.ClaimHoldDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(claimHoldDuration),
                    cancellationToken: cancellationToken);

                _chestCamera.SetActive(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                try
                {
                    try
                    {
                        if (_chestCamera != null)
                            _chestCamera.SetActive(false);
                    }
                    finally
                    {
                        bool isSamePanel = characterPanelPresenter != null &&
                                           ReferenceEquals(characterPanelPresenter.Panel, characterPanel);
                        bool isStillHiddenBySequence = characterPanelCanvasGroup != null &&
                                                       Mathf.Approximately(characterPanelCanvasGroup.alpha, 0f) &&
                                                       characterPanelCanvasGroup.interactable == false &&
                                                       characterPanelCanvasGroup.blocksRaycasts == false;

                        if (wasCharacterPanelStateCaptured && characterPanel != null &&
                            isSamePanel && isStillHiddenBySequence)
                        {
                            characterPanelCanvasGroup.alpha = characterPanelAlpha;
                            characterPanelCanvasGroup.interactable = wasCharacterPanelInteractable;
                            characterPanelCanvasGroup.blocksRaycasts = wasCharacterPanelBlockingRaycasts;
                        }
                    }
                }
                finally
                {
                    bool ownsCharacterLock = characterLockAcquired ||
                                             (character != null && character.IsChestOpening);
                    if (ownsCharacterLock && character != null)
                        character.FinishChestOpening();
                }
            }
        }

        private void SpawnPickup()
        {
            RelicPickup pickup = _container.InstantiatePrefabForComponent<RelicPickup>(
                _configuration.RelicPickupPrefab,
                transform.position + Vector3.up * _configuration.RelicDropHeight,
                Quaternion.identity,
                transform.parent);
            pickup.name = $"RelicPickup_{_relic.Id}";
            pickup.Construct(_relic, _configuration, _relicManager, _eventBus, _characterProvider,
                _roomData, _room, true, HandlePickupCollected);
        }

        private void HandlePickupCollected()
        {
            if (this == null)
                return;

            StopTreasureVerticalRays();
        }

        private void StopTreasureVerticalRays()
        {
            if (_treasureVerticalRaysParticles == null)
                return;

            foreach (ParticleSystem particle in _treasureVerticalRaysParticles)
            {
                if (particle != null)
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
