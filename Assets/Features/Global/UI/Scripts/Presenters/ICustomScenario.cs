using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI
{
    public interface ICustomScenario
    {
        UniTask Play(CancellationToken token);
    }
}