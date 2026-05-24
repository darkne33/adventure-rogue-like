public interface IUpgradeOfferHandler
{
    public void Handle();
    public void ApplyAbilityToCharacter(CharacterAbility characterAbility);
    public void RefreshItems();
    public void SkipUpgrades();
}