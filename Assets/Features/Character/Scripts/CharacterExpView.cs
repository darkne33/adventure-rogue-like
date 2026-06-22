using DG.Tweening;
using TMPro;
using UnityEngine;

public class CharacterExpView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text _expText;

    [Header("Animation")]
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private float _pumpDuration = 0.2f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private Color _activeColor = new(0.18f, 0.56f, 0.93f, 1f);
    [SerializeField] private Color _fadeColor = new(0.18f, 0.56f, 0.93f, 0.035f);
    [SerializeField] private float _durationBeforeHide = 0.8f;

    private int _currentAmount;
    private Sequence _currentSequence;
    private bool _isActive;

    private void Awake()
    {
        if (_expText == null)
        {
            Debug.LogError("CharacterExpView: expText is not assigned!");
            return;
        }

        _expText.gameObject.SetActive(false);
    }

    public void ShowExp(int amount)
    {
        if (amount <= 0)
            return;

        _currentAmount += amount;

        if (_isActive)
        {
            RestartPump();
            return;
        }

        StartDisplay();
    }

    private void StartDisplay()
    {
        _isActive = true;
        _expText.gameObject.SetActive(true);
        UpdateText();

        _expText.rectTransform.localScale = Vector3.one;
        _expText.color = _activeColor;

        PlaySequence();
    }

    private void UpdateText()
    {
        _expText.text = $"+{_currentAmount}";
    }

    private void RestartPump()
    {
        _currentSequence?.Kill();

        UpdateText();
        _expText.rectTransform.localScale = Vector3.one;
        _expText.color = _activeColor;

        PlaySequence();
    }

    private void PlaySequence()
    {
        _currentSequence = DOTween.Sequence()
            .Append(_expText.rectTransform.DOScale(_maxScale, _pumpDuration).SetEase(Ease.OutBack))
            .AppendInterval(_pumpDuration * 0.5f)
            .Append(DOTween.Sequence()
                .Join(_expText.rectTransform.DOScale(1f, _duration - _pumpDuration * 1.5f))
                .Join(_expText.DOColor(_fadeColor, _duration - _pumpDuration * 1.5f)
                    .SetDelay(_durationBeforeHide)))
            .OnComplete(Hide);
    }

    private void Hide()
    {
        _isActive = false;
        _currentAmount = 0;
        _currentSequence = null;
        _expText.gameObject.SetActive(false);
    }

    public void ForceHide()
    {
        _currentSequence?.Kill();
        Hide();
    }
}
