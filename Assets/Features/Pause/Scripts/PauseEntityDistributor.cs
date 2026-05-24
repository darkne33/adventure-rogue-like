using Zenject;

public class PauseEntityDistributor
{
    private readonly DiContainer _container;

    public PauseEntityDistributor(DiContainer container)
    {
        _container = container;
    }

    public PauseEntity EntityDistribute()
    {
        var pauseEntity = _container.Instantiate<PauseEntity>();
        return pauseEntity;
    }
}