public interface IUpgradeOfferHandler
{
    public void Handle();
    public void ApplyUpgradeOffer(UpgradeOffer upgradeOffer);
    public void RefreshItems();
    public void SkipUpgrades();
}
