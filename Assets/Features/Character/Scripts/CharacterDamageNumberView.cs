using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

public sealed class CharacterDamageNumberView : MonoBehaviour, IDamageView
{
    private const float TargetScale = 0.6f;
    private const float StartScale = TargetScale * 0.45f;

    [SerializeField] private GameObject _damageNumberPrefab;

    [Inject] private DiContainer _container;

    private int _spawnIndex;

    public void ShowDamage(int damage, float maximumHealth, bool isCritical)
    {
        if (damage <= 0)
            return;

        if (_damageNumberPrefab == null)
        {
            Debug.LogError($"{nameof(CharacterDamageNumberView)} requires a damage number prefab.", this);
            return;
        }

        GameObject numberObject = _container.InstantiatePrefab(
            _damageNumberPrefab,
            _container.DefaultParent);
        RectTransform rectTransform = numberObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = numberObject.GetComponent<CanvasGroup>();
        TMP_Text text = numberObject.GetComponentInChildren<TMP_Text>();

        if (rectTransform == null || canvasGroup == null || text == null)
        {
            Debug.LogError(
                $"{_damageNumberPrefab.name} must contain RectTransform, CanvasGroup and TMP text components.",
                _damageNumberPrefab);
            Destroy(numberObject);
            return;
        }

        text.text = $"-{damage}";
        canvasGroup.alpha = 1f;

        int offsetIndex = _spawnIndex++ % 5;
        float horizontalOffset = (offsetIndex - 2) * 0.2f;
        Vector3 startPosition = transform.position + new Vector3(horizontalOffset, 1.55f, 0f);
        Vector3 endPosition = startPosition + new Vector3(horizontalOffset * 0.25f, 1.35f, 0f);

        rectTransform.position = startPosition;
        rectTransform.localScale = Vector3.one * StartScale;

        DOTween.Sequence()
            .SetLink(numberObject)
            .Append(rectTransform.DOScale(TargetScale, 0.18f).SetEase(Ease.OutBack))
            .Join(rectTransform.DOMove(endPosition, 0.8f).SetEase(Ease.OutCubic))
            .AppendInterval(0.1f)
            .Append(canvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad))
            .Join(rectTransform.DOMoveY(endPosition.y + 0.3f, 0.25f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(numberObject));
    }
}
