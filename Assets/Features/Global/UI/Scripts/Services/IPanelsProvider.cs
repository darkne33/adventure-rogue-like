using System.Collections.Generic;
using CustomPackages.Package.Extensions.AsyncAction;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace UI
{
    [Preserve]
    public interface IPanelsProvider
    {
        AsyncAction<PanelName> PanelOpened { get; }
        AsyncAction<PanelName> PanelClosed { get; }
        void ConstructPanelLocations(Dictionary<PanelLocation, Transform> panelLocations);
        Transform GetRootFor(PanelLocation panelLocation);
        bool AnyPanelOpenedIn(PanelLocation panelLocation);
        List<PanelBase> GetOpenedPanels();
        T GetOpenedPanel<T>() where T : PanelBase;
        UniTask AddOpenedPanel(PanelBase panel);
        bool IsOpen(PanelName panelName);
        UniTask RemoveOpenedPanel(PanelBase panel);
    }
}