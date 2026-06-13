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
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        return _panelAnimationsMonoComponent.Show();
    }

    public async UniTask Hide()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        await _panelAnimationsMonoComponent.Hide();
        gameObject.SetActive(false);
    }
}