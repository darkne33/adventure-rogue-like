using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UpgradeOfferItemApplyHandler : MonoBehaviour
{
    [SerializeField] private Button _applyButton;

    [Inject] private IUpgradeOfferHandler _upgradeOfferHandler;

    public void Initialize(UpgradeOffer upgradeOffer) =>
        _applyButton.onClick.AddListener(() =>
            _upgradeOfferHandler.ApplyUpgradeOffer(upgradeOffer));

    private void OnDisable() =>
        _applyButton.onClick.RemoveAllListeners();
}
