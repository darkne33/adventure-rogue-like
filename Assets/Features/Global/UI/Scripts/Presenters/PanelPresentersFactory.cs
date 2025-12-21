using System;
using Zenject;

namespace UI
{
    public class PanelPresentersFactory : IPanelPresentersFactory
    {
        public IPanelPresenterLogic Create(PanelName panelName, DiContainer container)
        {
            return (IPanelPresenterLogic)container.Instantiate(ResolveTypeByPanelName(panelName));
        }

        private Type ResolveTypeByPanelName(PanelName panelName)
        {
            switch (panelName)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(panelName), panelName, null);
            }
        }
    }
}