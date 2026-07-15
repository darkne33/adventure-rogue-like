using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts.Level.Scripts;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.Relics.Scripts
{
    public sealed class RelicChest : MonoBehaviour
    {
        [Inject] private IPanelService _panelService;
        [Inject] private IRoomTransitionService _roomTransitionService;
        [Inject] private CharacterChestOpeningService _chestOpeningService;

        [SerializeField] private RelicChestInteractionView _interactionView = new();
        [SerializeField] private RelicChestOpeningView _openingView = new();

        private InputSystem_Actions _inputActions;
        private RelicDefinition _relic;
        private RelicChestConfiguration _configuration;
        private ICharacterProvider _characterProvider;
        private RoomData _roomData;
        private Room _room;
        private RelicChestOpeningSequence _openingSequence;
        private bool _isOpened;

        public bool IsOpened => _isOpened;
        public Room Room => _room;
        public RoomData RoomData => _roomData;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _interactionView.Initialize(gameObject);
        }

        public void Construct(RelicDefinition relic, RelicChestConfiguration configuration,
            RelicManager relicManager, RelicEventBus eventBus, ICharacterProvider characterProvider,
            DiContainer container, RoomData roomData, Room room)
        {
            _relic = relic;
            _configuration = configuration;
            _characterProvider = characterProvider;
            _roomData = roomData;
            _room = room;

            RelicChestRewardPresenter rewardPresenter = new(configuration, relicManager, eventBus,
                container, roomData, room);
            _openingSequence = new RelicChestOpeningSequence(_openingView, configuration, eventBus,
                _panelService, _roomTransitionService, _chestOpeningService, rewardPresenter);
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
            if (_isOpened || _configuration == null || _characterProvider?.CharacterFacade == null)
                return false;

            CharacterFacade character = _characterProvider.CharacterFacade;
            return _chestOpeningService.IsOpening == false &&
                   Vector3.Distance(transform.position, character.transform.position) <=
                   _configuration.InteractDistance;
        }

        private void Open()
        {
            if (_isOpened)
                return;

            if (_openingSequence == null || _openingView.IsConfigured == false)
            {
                Debug.LogError($"{name} is missing chest opening sequence references.", this);
                return;
            }

            CharacterFacade character = _characterProvider?.CharacterFacade;
            if (character == null || _chestOpeningService.IsOpening)
                return;

            _openingSequence.PlayAsync(character, _relic, transform.position, HandleOpeningStarted,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void HandleOpeningStarted()
        {
            _isOpened = true;
            _interactionView.SetAvailable(false);
        }
    }
}
