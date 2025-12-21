using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Scripting;
using Zenject;

namespace UI
{
    [Preserve]
    public interface IPanelStorage
    {
        UniTask WarmUp(CancellationToken cts);
        UniTask Add(PanelData assetReference, DiContainer container, CancellationToken token);
        void CleanPanel(PanelName panelName);
        UniTask<StoragePanelData> Get(PanelName panelName, CancellationToken token);
        void Remove(PanelName panelName);
        PanelLocation GetPanelLocation(PanelName panelName);
        bool Exist(PanelName panelName);
    }
}