using Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UpgradeOfferItemsSkipHandler : MonoBehaviour
{
    [SerializeField] private Button _skipButton;
    
    [Inject] private IGameModeService _gameModeService;

    public void Start()
    {
        var rogueLikeStateMachine = _gameModeService.Get<RogueLikeStateMachine>();
        var upgradesOfferHandler =
            rogueLikeStateMachine.Resolve<IUpgradeOfferHandler>();
        _skipButton.onClick.AddListener(upgradesOfferHandler.SkipUpgrades);
    }

    private void OnDestroy() =>
        _skipButton.onClick.RemoveAllListeners();
}