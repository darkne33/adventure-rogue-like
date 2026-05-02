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

    public UpgradeOfferItemFacade Create(Transform root)
    {
        var upgradeOfferItemFacade = _upgradeOfferConfiguration.UpgradeOfferItemFacade;
        return _container.InstantiatePrefabForComponent<UpgradeOfferItemFacade>(upgradeOfferItemFacade, root);
    }
}

public interface IUpgradeOfferItemFactory
{
    UpgradeOfferItemFacade Create(Transform root);
}