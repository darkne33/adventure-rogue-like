using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicChest : MonoBehaviour
    {
        private InputSystem_Actions _inputActions;

        [SerializeField] private Outline _outline;
        [SerializeField] private CanvasGroup _interactionPromptCanvasGroup;
        [SerializeField] private Transform _interactionPromptTransform;
        [SerializeField, Min(0f)] private float _promptShowDuration = 0.14f;
        [SerializeField, Min(0f)] private float _promptHideDuration = 0.12f;

        private RelicDefinition _relic;
        private RelicChestConfiguration _configuration;
        private RelicManager _relicManager;
        private RelicEventBus _eventBus;
        private ICharacterProvider _characterProvider;
        private DiContainer _container;
        private RoomData _roomData;
        private bool _isOpened;
        private bool _isInteractionAvailable;
        private Transform _lid;
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
            DiContainer container, RoomData roomData)
        {
            _relic = relic;
            _configuration = configuration;
            _relicManager = relicManager;
            _eventBus = eventBus;
            _characterProvider = characterProvider;
            _container = container;
            _roomData = roomData;
            _lid = transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefaultByName("Chest_cap");
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
                Open().Forget();
        }

        private bool CanInteract()
        {
            if (_isOpened || _configuration == null || _characterProvider?.CharacterFacade == null)
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

        private async UniTaskVoid Open()
        {
            if (_isOpened)
                return;

            _isOpened = true;
            SetInteractionVisuals(false);
            _eventBus.PublishChestOpened(transform.position);

            Sequence sequence = DOTween.Sequence();
            _ = sequence.Append(transform.DOPunchScale(Vector3.one * 0.18f, 0.22f, 5, 0.75f));
            if (_lid != null)
                _ = sequence.Join(_lid.DOLocalRotate(new Vector3(-55f, 0f, 0f), 0.35f,
                    RotateMode.LocalAxisAdd).SetEase(Ease.OutBack));

            await sequence.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            SpawnPickup();
        }

        private void SpawnPickup()
        {
            RelicPickup pickup = _container.InstantiatePrefabForComponent<RelicPickup>(
                _configuration.RelicPickupPrefab,
                transform.position + Vector3.up * _configuration.RelicDropHeight,
                Quaternion.identity,
                transform.parent);
            pickup.name = $"RelicPickup_{_relic.Id}";
            pickup.Construct(_relic, _configuration, _relicManager, _eventBus, _characterProvider, _roomData);
        }
    }

    internal static class TransformSearchExtensions
    {
        public static Transform FirstOrDefaultByName(this Transform[] transforms, string objectName)
        {
            foreach (Transform item in transforms)
            {
                if (item.name == objectName)
                    return item;
            }

            return null;
        }
    }
}
