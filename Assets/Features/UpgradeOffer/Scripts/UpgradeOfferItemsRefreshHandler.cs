using Core;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UpgradeOfferItemsRefreshHandler : MonoBehaviour
{
    [SerializeField] private Button _refreshButton;
    
    [Inject] private IGameModeService _gameModeService;

    public void Start()
    {
        var rogueLikeStateMachine = _gameModeService.Get<RogueLikeStateMachine>();
        var upgradesOfferHandler =
            rogueLikeStateMachine.Resolve<IUpgradeOfferHandler>();
        _refreshButton.onClick.AddListener(upgradesOfferHandler.RefreshItems);
    }

    private void OnDestroy() =>
        _refreshButton.onClick.RemoveAllListeners();
}