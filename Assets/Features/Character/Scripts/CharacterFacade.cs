using Core;
using UI;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Rigidbody))]
public class CharacterFacade : MonoBehaviour
{
    public HealthSystem HealthSystem => _healthSystem;
    public Rigidbody Rigidbody => _rigidbody;
    public CharacterCombatSystem CharacterCombatSystem => _characterCombatSystem;
    public CharacterMoveSystem MoveSystem => _moveSystem;

    [SerializeField] private GameObject _characterModel;

    [Inject] private CharacterSettingsConfiguration _characterSettingsConfiguration;
    [Inject] private CharacterCameraSettingsConfiguration _characterCameraSettingsConfiguration;

    [Inject] private ICameraService _cameraService;
    [Inject] private IPanelService _panelService;
    [Inject] private IAbilityChoiceProvider _abilityChoiceProvider;

    private CharacterMoveSystem _moveSystem;
    private CharacterCameraMoveSystem _cameraSystem;
    private HealthSystem _healthSystem;
    private CharacterFxSystem _characterFxSystem;
    private IHealthView _healthView;
    private CharacterCombatSystem _characterCombatSystem;
    private IDeathSystem _deathSystem;
    private CharacterGoldView _characterGoldView;

    private Rigidbody _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterFxSystem = GetComponent<CharacterFxSystem>();
        _healthView = GetComponent<HealthView>();

        _moveSystem =
            new CharacterMoveSystem(_rigidbody, _cameraService, _characterSettingsConfiguration, _characterFxSystem,
                _characterModel);

        _characterCombatSystem = new CharacterCombatSystem();
        _characterCombatSystem.AddAbility(_abilityChoiceProvider.GetAbility(AbilityName.FireBall), this);

        CharacterPanel characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
        _characterGoldView = characterPanel.CharacterGoldView;
        _healthSystem = new HealthSystem(_characterSettingsConfiguration.StartHealth,
            new[] { characterPanel.CharacterHealthView, _healthView }, _deathSystem
        );

        _healthSystem.Initialize();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        _moveSystem.UpdateDash(Time.deltaTime);
        _characterCombatSystem.TickAbilities(this);
        _moveSystem.Rotate();
    }

    private void FixedUpdate()
    {
        _moveSystem.Move();

        _moveSystem.Jump();
    }

    private void OnCollisionEnter(Collision other)
    {
        var ground = other.gameObject.GetComponent<Ground>();
        if (ground != null)
        {
            _moveSystem.ResetCanJump();
            _moveSystem.CanMove(true);
        }

        var wall = other.gameObject.GetComponent<Wall>();
        if (wall != null)
            _moveSystem.CanMove(true);

        var obstacle = other.gameObject.GetComponent<Obstacle>();
        if (obstacle != null)
            _moveSystem.CanMove(true);
    }
}