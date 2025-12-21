using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UI
{
    public interface IUIFactory
    {
        Transform UIRoot { get; }
        UniTask Initialize(CancellationToken cts);
    }
}