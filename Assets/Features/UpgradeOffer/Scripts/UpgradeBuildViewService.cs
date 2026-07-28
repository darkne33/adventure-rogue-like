using System;

public sealed class UpgradeBuildViewService : IDisposable
{
    private readonly UpgradeBuildService _upgradeBuildService;

    private CharacterBuildView _view;

    public UpgradeBuildViewService(UpgradeBuildService upgradeBuildService)
    {
        _upgradeBuildService = upgradeBuildService;
        _upgradeBuildService.Changed += Refresh;
    }

    public void Attach(CharacterPanel panel)
    {
        if (panel == null || _view != null)
            return;

        _view = panel.CharacterBuildView;
        Refresh();
    }

    public void Detach()
    {
        _view?.Refresh(null);
        _view = null;
    }

    public void Dispose()
    {
        _upgradeBuildService.Changed -= Refresh;
        Detach();
    }

    private void Refresh() =>
        _view?.Refresh(_upgradeBuildService.SelectedUpgrades);
}
