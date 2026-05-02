using UnityEngine;

public class UpgradeOfferItemFacade : MonoBehaviour
{
    [field: SerializeField] public UpgradeOfferItemView UpgradeOfferItemView { get; private set; }
    [field: SerializeField] public UpgradeOfferItemApplyHandler UpgradeOfferItemApplyHandler { get; private set; }
}