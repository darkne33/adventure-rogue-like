using Cysharp.Threading.Tasks;

namespace Core
{
    public interface IGameAddressableService
    {
        UniTask InitializeAddressables();
    }
}