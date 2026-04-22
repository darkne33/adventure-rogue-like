using UnityEngine;
using Zenject;

public class UpgradeOfferItemFactory : IUpgradeOfferItemFactory
{ 
    private readonly DiContainer _container;
    private readonly UpgradeOfferConfiguration _upgradeOfferConfiguration;

    public UpgradeOfferItemFactory(DiContainer container, UpgradeOfferConfiguration upgradeOfferConfiguration)
    {
        _container = container;
        _upgradeOfferConfiguration = upgradeOfferConfiguration;
    }

    public UpgradeOfferItemView Create(Transform root)
    {
        var upgradeItemView = _upgradeOfferConfiguration.UpgradeOfferItemView;
        return _container.InstantiatePrefabForComponent<UpgradeOfferItemView>(upgradeItemView, root);
    }
}

public interface IUpgradeOfferItemFactory
{
    UpgradeOfferItemView Create(Transform root);
}