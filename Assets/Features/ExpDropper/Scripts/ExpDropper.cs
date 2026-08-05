using UnityEngine;
using Zenject;

public sealed class ExpDropper
{
    private readonly ExpDropperConfiguration _configuration;
    private readonly ICharacterProvider _characterProvider;
    private readonly ICharacterLevelService _characterLevelService;
    private readonly DiContainer _container;

    public ExpDropper(ExpDropperConfiguration configuration, ICharacterProvider characterProvider,
        ICharacterLevelService characterLevelService, DiContainer container)
    {
        _configuration = configuration;
        _characterProvider = characterProvider;
        _characterLevelService = characterLevelService;
        _container = container;
    }

    public void DropExp(Vector3 position, int amount)
    {
        if (amount <= 0)
            return;

        if (_configuration == null || _configuration.ExpRupeePrefab == null)
        {
            _characterLevelService.AddExp(amount);
            return;
        }

        GameObject rupeeObject = _container.InstantiatePrefab(_configuration.ExpRupeePrefab, position,
            Quaternion.identity, null);
        ExpRupee expRupee = rupeeObject.GetComponent<ExpRupee>();
        if (expRupee == null)
            expRupee = rupeeObject.AddComponent<ExpRupee>();

        expRupee.Construct(amount, _configuration, _characterProvider, _characterLevelService,
            position + GetBurstOffset());
    }

    private Vector3 GetBurstOffset()
    {
        Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, _configuration.BurstScatterRadius);
        return new Vector3(scatter.x, Mathf.Max(0f, _configuration.BurstHeight), scatter.y);
    }
}
