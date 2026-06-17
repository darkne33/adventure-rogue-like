using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using Zenject;

public class UpgradeOfferPanel : MonoBehaviour
{
    [field: SerializeField] public Transform UpgradesRoot { get; private set; }

    [Inject] private ICursorService _cursorService;

    private PanelAnimationsMonoComponent _panelAnimationsMonoComponent;

    private void Awake()
    {
        _panelAnimationsMonoComponent = GetComponent<PanelAnimationsMonoComponent>();
    }

    public async UniTask Show()
    {
        gameObject.SetActive(true);
        _panelAnimationsMonoComponent.ForceHide();
        _cursorService.ShowUiCursor();
        await _panelAnimationsMonoComponent.Show();
        _cursorService.ShowUiCursor();
    }

    public async UniTask Hide()
    {
        await _panelAnimationsMonoComponent.Hide();
        gameObject.SetActive(false);
        _cursorService.ShowGameplayCursor();
    }
}
