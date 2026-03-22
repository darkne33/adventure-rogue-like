using DG.Tweening;
using TMPro;
using UnityEngine;
public class CharacterGoldView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text _goldText;

    [Header("Animation")]
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private float _pumpDuration = 0.2f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private Color _activeColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color _fadeColor = new Color(1f, 0.9f, 0.3f, 0.3f);
    [SerializeField] private float _durationBeforeHide = 0.8f;

    private int _currentAmount = 0;
    private Sequence _currentSequence;
    private bool _isActive = false;

    private void Awake()
    {
        if (_goldText == null)
        {
            Debug.LogError("GoldPopupManager: goldText is not assigned!");
            return;
        }
        _goldText.gameObject.SetActive(false);
    }
    
    public void ShowGold(int amount)
    {
        _currentAmount += amount;

        if (!_isActive)
        {
            StartDisplay();
        }
        else
        {
            UpdateText();
            RestartPump();
        }
    }

    private void StartDisplay()
    {
        _isActive = true;
        _goldText.gameObject.SetActive(true);
        UpdateText();
        
        _goldText.rectTransform.localScale = Vector3.one;
        _goldText.color = _activeColor;

        PlaySequence();
    }

    private void UpdateText()
    {
        _goldText.text = $"+${_currentAmount}";
    }

    private void RestartPump()
    {
        _currentSequence?.Kill();

        _goldText.rectTransform.localScale = Vector3.one;
        _goldText.color = _activeColor;

        PlaySequence();
    }

    private void PlaySequence()
    {
        _currentSequence = DOTween.Sequence()
            .Append(_goldText.rectTransform.DOScale(_maxScale, _pumpDuration).SetEase(Ease.OutBack))
            .AppendInterval(_pumpDuration * 0.5f)
            .Append(DOTween.Sequence()
                .Join(_goldText.rectTransform.DOScale(1f, _duration - _pumpDuration * 1.5f))
                .Join(_goldText.DOColor(_fadeColor, _duration - _pumpDuration * 1.5f).SetDelay(_durationBeforeHide))
            )
            .OnComplete(Hide);
    }

    private void Hide()
    {
        _isActive = false;
        _currentAmount = 0;
        _currentSequence = null;
        _goldText.gameObject.SetActive(false);
    }
    
    public void ForceHide()
    {
        _currentSequence?.Kill();
        Hide();
    }
}