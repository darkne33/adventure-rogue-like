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

    public UniTask Show() =>
        _panelAnimationsMonoComponent.Show();

    public UniTask Hide() =>
        _panelAnimationsMonoComponent.Hide();
}