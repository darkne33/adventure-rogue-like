using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts.Level.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicChest : MonoBehaviour
    {
        [Inject] private RelicChestRollService _rollService;
        [Inject] private ITimeScaleService _timeScaleService;

        [SerializeField] private RelicChestInteractionView _interactionView = new();
        [SerializeField] private RelicChestRollView _rollView = new();

        private InputSystem_Actions _inputActions;
        private RelicChestConfiguration _configuration;
        private RelicPool _relicPool;
        private RelicManager _relicManager;
        private ICharacterProvider _characterProvider;
        private RoomData _roomData;
        private Room _room;
        private RelicChestRollSequence _rollSequence;
        private bool _isOpened;

        public bool IsOpened => _isOpened;
        public Room Room => _room;
        public RoomData RoomData => _roomData;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _interactionView.Initialize(gameObject);
            _rollView.Initialize(gameObject);
        }

        public void Construct(RelicChestConfiguration configuration, RelicPool relicPool,
            RelicManager relicManager, RelicEventBus eventBus,
            ICharacterProvider characterProvider, DiContainer container, RoomData roomData,
            Room room)
        {
            _configuration = configuration;
            _relicPool = relicPool;
            _relicManager = relicManager;
            _characterProvider = characterProvider;
            _roomData = roomData;
            _room = room;

            RelicChestRewardPresenter rewardPresenter = new(configuration, relicManager, eventBus,
                characterProvider, container, roomData, room);
            _rollSequence = new RelicChestRollSequence(_rollView, configuration, eventBus,
                rewardPresenter);
        }

        private void OnEnable()
        {
            _inputActions ??= new InputSystem_Actions();
            _inputActions.Player.Interact.Enable();
        }

        private void OnDisable()
        {
            _inputActions?.Player.Interact.Disable();
            _interactionView.SetAvailable(false, true);
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void Update()
        {
            bool canInteract = CanInteract();
            _interactionView.SetAvailable(canInteract);

            if (canInteract && _inputActions != null &&
                _inputActions.Player.Interact.WasPressedThisFrame())
                Open();
        }

        private bool CanInteract()
        {
            if (_timeScaleService.IsPaused || _isOpened || _configuration == null ||
                _characterProvider?.CharacterFacade == null)
                return false;

            CharacterFacade character = _characterProvider.CharacterFacade;
            return _rollService.IsRolling == false &&
                   Vector3.Distance(transform.position, character.transform.position) <=
                   _configuration.InteractDistance;
        }

        private void Open()
        {
            if (_isOpened)
                return;

            if (_rollSequence == null || _rollView.IsConfigured == false)
            {
                Debug.LogError($"{name} is missing relic roll references.", this);
                return;
            }

            if (_characterProvider?.CharacterFacade == null || _relicPool == null ||
                _relicManager == null)
                return;

            List<RelicDefinition> availableRelics = _relicPool
                .GetAvailable(_relicManager.ActiveRelics)
                .ToList();
            if (availableRelics.Count == 0)
            {
                Debug.LogWarning($"{name} has no available relic rewards.", this);
                return;
            }

            if (_rollService.TryBegin() == false)
                return;

            _isOpened = true;
            _interactionView.SetAvailable(false);
            _rollSequence.PlayAsync(availableRelics, transform.position,
                _rollService.Finish, this.GetCancellationTokenOnDestroy()).Forget();
        }
    }
}
