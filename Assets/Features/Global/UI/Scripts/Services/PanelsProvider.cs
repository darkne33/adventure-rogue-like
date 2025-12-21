using System.Collections.Generic;
using System.Linq;
using CustomPackages.Package.Extensions.AsyncAction;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace UI
{
    [Preserve]
    public sealed class PanelsProvider : IPanelsProvider
    {
        public AsyncAction<PanelName> PanelOpened { get; } = new();
        public AsyncAction<PanelName> PanelClosed { get; } = new();

        private readonly List<PanelBase> _openedPanels = new();
        private Dictionary<PanelLocation, Transform> _panelLocations;

        public void ConstructPanelLocations(Dictionary<PanelLocation, Transform> panelLocations) =>
            _panelLocations = panelLocations;

        public Transform GetRootFor(PanelLocation panelLocation) =>
            _panelLocations[panelLocation];

        public bool AnyPanelOpenedIn(PanelLocation panelLocation) =>
            _panelLocations[panelLocation].childCount > 0;

        public List<PanelBase> GetOpenedPanels() =>
            _openedPanels;

        public T GetOpenedPanel<T>() where T : PanelBase =>
            _openedPanels.FirstOrDefault(x => x is T) as T;

        private PanelBase GetOpenedPanel(PanelName panelName) =>
            _openedPanels.FirstOrDefault(x => x.PanelName == panelName);

        public UniTask AddOpenedPanel(PanelBase panel)
        {
            _openedPanels.Add(panel);
            return PanelOpened.Invoke(panel.PanelName);
        }

        public UniTask RemoveOpenedPanel(PanelBase panel)
        {
            _openedPanels.Remove(panel);
            return PanelClosed.Invoke(panel.PanelName);
        }

        public bool IsOpen(PanelName panelName) =>
            GetOpenedPanel(panelName) != null;
    }
}