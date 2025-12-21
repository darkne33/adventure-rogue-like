using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Services
{
    public interface IDynamicEffectsService
    {
        UniTask Initialize(ContentType contentType, DynamicEffectsConfig config, CancellationToken token);
        void Cleanup(ContentType contentType);
    }
}