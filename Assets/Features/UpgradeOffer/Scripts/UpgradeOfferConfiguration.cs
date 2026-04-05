using UnityEngine;

[CreateAssetMenu(menuName = "Create UpgradeOfferConfiguration", fileName = "UpgradeOfferConfiguration")]
public class UpgradeOfferConfiguration : ScriptableObject
{
    [field: SerializeField] public UpgradeOfferItemView UpgradeOfferItemView { get; private set; }
}