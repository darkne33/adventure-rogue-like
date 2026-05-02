using UnityEngine;

[CreateAssetMenu(menuName = "Create UpgradeOfferConfiguration", fileName = "UpgradeOfferConfiguration")]
public class UpgradeOfferConfiguration : ScriptableObject
{
    [field: SerializeField] public UpgradeOfferItemFacade UpgradeOfferItemFacade { get; private set; }
}