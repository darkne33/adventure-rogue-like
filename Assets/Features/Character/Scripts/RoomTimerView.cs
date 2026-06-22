using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class RoomTimerView : MonoBehaviour
{
    [SerializeField] private TMP_Text _timerText;

    public void Show(int seconds)
    {
        if (_timerText == null)
            return;

        KillTweens();
        _timerText.text = FormatTime(seconds);
        _timerText.alpha = 1f;
        _timerText.rectTransform.localScale = Vector3.one;
        _timerText.rectTransform
            .DOPunchScale(Vector3.one * 0.18f, 0.25f, 4, 0.35f)
            .SetLink(_timerText.gameObject);
    }

    public void UpdateValue(int seconds)
    {
        if (_timerText == null)
            return;

        DOTween.Kill(_timerText.rectTransform);
        _timerText.text = FormatTime(seconds);
        _timerText.rectTransform.localScale = Vector3.one;
        _timerText.rectTransform
            .DOPunchScale(Vector3.one * 0.12f, 0.18f, 3, 0.4f)
            .SetLink(_timerText.gameObject);
    }

    public void Hide()
    {
        if (_timerText == null)
            return;

        KillTweens();
        _timerText.DOFade(0f, 0.25f)
            .SetLink(_timerText.gameObject);
    }

    public void HideImmediate()
    {
        if (_timerText == null)
            return;

        KillTweens();
        _timerText.alpha = 0f;
        _timerText.rectTransform.localScale = Vector3.one;
    }

    private void KillTweens()
    {
        DOTween.Kill(_timerText);
        DOTween.Kill(_timerText.rectTransform);
    }

    private static string FormatTime(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
