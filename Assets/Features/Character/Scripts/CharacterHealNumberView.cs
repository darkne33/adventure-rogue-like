using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

public sealed class CharacterHealNumberView : MonoBehaviour
{
    private const float FontSize = 1.05f;
    private const float TargetScale = 0.6f;
    private const float StartScale = TargetScale * 0.45f;
    private static readonly Color32 HealColor = new(84, 255, 126, 255);
    private static readonly Color32 OutlineColor = new(0, 0, 0, 255);

    private Canvas _sourceCanvas;
    private Transform _cameraTransform;
    private RectTransform _root;

    public void ShowHeal(float amount, TMP_FontAsset fontAsset, Transform cameraTransform)
    {
        if (amount <= 0f)
            return;

        _cameraTransform = cameraTransform;
        EnsureRoot();
        SyncRootTransform();

        GameObject numberObject = new("HealNumber", typeof(RectTransform), typeof(CanvasGroup),
            typeof(TextMeshProUGUI));
        RectTransform rectTransform = numberObject.GetComponent<RectTransform>();
        rectTransform.SetParent(_root, false);
        rectTransform.sizeDelta = new Vector2(3.5f, 1.3f);
        rectTransform.anchoredPosition = new Vector2(Random.Range(-0.35f, 0.35f), 0.55f);
        rectTransform.localScale = Vector3.one * StartScale;

        TextMeshProUGUI text = numberObject.GetComponent<TextMeshProUGUI>();
        text.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
        text.text = $"+{amount.ToString("0.#", CultureInfo.InvariantCulture)}";
        text.fontSize = FontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = HealColor;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.18f;
        text.outlineColor = OutlineColor;

        CanvasGroup canvasGroup = numberObject.GetComponent<CanvasGroup>();
        Vector2 endPosition = rectTransform.anchoredPosition + new Vector2(0f, 1.35f);

        DOTween.Sequence()
            .SetLink(numberObject)
            .Append(rectTransform.DOScale(TargetScale, 0.18f).SetEase(Ease.OutBack))
            .Join(rectTransform.DOAnchorPos(endPosition, 0.8f).SetEase(Ease.OutCubic))
            .AppendInterval(0.12f)
            .Append(canvasGroup.DOFade(0f, 0.28f).SetEase(Ease.InQuad))
            .Join(rectTransform.DOAnchorPosY(endPosition.y + 0.35f, 0.28f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(numberObject));
    }

    private void LateUpdate() =>
        SyncRootTransform();

    private void OnDestroy()
    {
        if (_root != null)
            Destroy(_root.gameObject);
    }

    private void EnsureRoot()
    {
        if (_root != null)
            return;

        _sourceCanvas = GetComponentInChildren<Canvas>();
        GameObject rootObject = new("CharacterHealNumbers", typeof(RectTransform), typeof(Canvas));
        _root = rootObject.GetComponent<RectTransform>();
        _root.sizeDelta = _sourceCanvas != null
            ? ((RectTransform)_sourceCanvas.transform).sizeDelta
            : new Vector2(4f, 2f);

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = _sourceCanvas != null ? _sourceCanvas.sortingOrder + 1 : 1;
    }

    private void SyncRootTransform()
    {
        if (_root == null)
            return;

        if (_sourceCanvas != null)
        {
            Transform source = _sourceCanvas.transform;
            _root.position = source.position;

            Vector3 sourceScale = source.lossyScale;
            _root.localScale = new Vector3(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y),
                Mathf.Abs(sourceScale.z));
        }
        else
        {
            _root.position = transform.position + Vector3.up * 1.5f;
            _root.localScale = Vector3.one * 0.01f;
        }

        Transform cameraTransform = _cameraTransform;
        if (cameraTransform == null)
            cameraTransform = Camera.main != null ? Camera.main.transform : null;

        if (cameraTransform == null)
            return;

        Vector3 directionAwayFromCamera = _root.position - cameraTransform.position;
        if (directionAwayFromCamera.sqrMagnitude > 0.001f)
            _root.rotation = Quaternion.LookRotation(directionAwayFromCamera.normalized, Vector3.up);
    }
}
