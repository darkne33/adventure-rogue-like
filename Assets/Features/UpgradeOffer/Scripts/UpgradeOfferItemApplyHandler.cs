using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UpgradeOfferItemApplyHandler : MonoBehaviour
{
    [SerializeField] private Button _applyButton;

    [Inject] private ICharacterProvider _characterProvider;
    [Inject] private CharacterStats _characterStats;
    [Inject] private IUpgradeOfferHandler _upgradeOfferHandler;

    public void Initialize(CharacterAbility characterAbility) =>
        _applyButton.onClick.AddListener(() =>
            _upgradeOfferHandler.ApplyAbilityToCharacter(characterAbility));

    private void OnDisable() =>
        _applyButton.onClick.RemoveAllListeners();
}