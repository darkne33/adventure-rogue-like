using System;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using Zenject;

public class UpgradeOfferPanel : MonoBehaviour
{
    [field: SerializeField] public Transform UpgradesRoot { get; private set; }

    public event Action<bool> VisibilityChanged;

    [Inject] private ICursorService _cursorService;

    private PanelAnimationsMonoComponent _panelAnimationsMonoComponent;

    private void Awake()
    {
        _panelAnimationsMonoComponent = GetComponent<PanelAnimationsMonoComponent>();
    }

    public UniTask Show()
    {
        gameObject.SetActive(true);
        _panelAnimationsMonoComponent.ForceShow();
        _cursorService.ShowUiCursor();
        VisibilityChanged?.Invoke(true);
        return UniTask.CompletedTask;
    }

    public async UniTask Hide()
    {
        await _panelAnimationsMonoComponent.Hide();
        gameObject.SetActive(false);
        _cursorService.ShowGameplayCursor();
        VisibilityChanged?.Invoke(false);
    }
}
