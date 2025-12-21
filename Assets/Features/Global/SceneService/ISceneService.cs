namespace Core
{
    public interface ISceneService<out T>
    {
        T GameSceneComponentsService { get; }
    }
}