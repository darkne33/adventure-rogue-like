using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Enemies.Scripts
{
    public interface IEnemyDamageSystem
    {
        void Initialize();
        UniTask Execute(CancellationToken cancellationToken);
        UniTask Tick(CancellationToken cancellationToken);
    }
}