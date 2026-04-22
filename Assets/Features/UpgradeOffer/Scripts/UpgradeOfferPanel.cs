using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class UpgradeOfferPanel : MonoBehaviour
{
    [field: SerializeField] public Transform UpgradesRoot { get; private set; }

    private PanelAnimationsMonoComponent _panelAnimationsMonoComponent;

    private void Awake()
    {
        _panelAnimationsMonoComponent = GetComponent<PanelAnimationsMonoComponent>();
    }

    public UniTask Show()
    {
        gameObject.SetActive(true);
        _panelAnimationsMonoComponent.ForceHide();
        return _panelAnimationsMonoComponent.Show();
    }

    public async UniTask Hide()
    {
        await _panelAnimationsMonoComponent.Hide();
        gameObject.SetActive(true);
    }
}