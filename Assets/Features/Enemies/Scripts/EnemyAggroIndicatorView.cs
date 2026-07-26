using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [DisallowMultipleComponent]
    public sealed class EnemyAggroIndicatorView : MonoBehaviour
    {
        private const float IndicatorWorldScale = 0.01f;
        private const float IndicatorHeightOffset = 0.55f;
        private const float PopDuration = 0.14f;
        private const float SettleDuration = 0.08f;
        private const float FadeDuration = 0.18f;

        private static readonly Color32 IndicatorColor = new(255, 68, 45, 255);
        private static readonly Color32 OutlineColor = new(55, 7, 4, 255);

        private RectTransform _root;
        private RectTransform _textRoot;
        private CanvasGroup _canvasGroup;
        private Collider _collider;
        private Camera _mainCamera;
        private Sequence _sequence;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            CreateIndicator();
        }

        private void LateUpdate()
        {
            if (_root == null || _root.gameObject.activeSelf == false)
                return;

            SyncPosition();
            FaceCamera();
        }

        public void Play(float totalDuration)
        {
            if (_root == null)
                return;

            _sequence?.Kill();
            SyncPosition();
            FaceCamera();

            _root.gameObject.SetActive(true);
            _root.localScale = Vector3.one * (IndicatorWorldScale * 0.2f);
            _textRoot.anchoredPosition = Vector2.zero;
            _canvasGroup.alpha = 1f;

            float holdDuration = Mathf.Max(
                0f,
                totalDuration - PopDuration - SettleDuration - FadeDuration);

            _sequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(_root.DOScale(
                        Vector3.one * (IndicatorWorldScale * 1.25f),
                        PopDuration)
                    .SetEase(Ease.OutBack))
                .Append(_root.DOScale(
                        Vector3.one * IndicatorWorldScale,
                        SettleDuration)
                    .SetEase(Ease.OutQuad))
                .AppendInterval(holdDuration)
                .Append(_canvasGroup.DOFade(0f, FadeDuration).SetEase(Ease.InQuad))
                .Join(_textRoot.DOAnchorPosY(20f, FadeDuration).SetEase(Ease.InQuad))
                .OnComplete(() => _root.gameObject.SetActive(false));
        }

        private void CreateIndicator()
        {
            GameObject rootObject = new(
                "AggroIndicator",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            _root = rootObject.GetComponent<RectTransform>();
            _root.sizeDelta = new Vector2(100f, 120f);

            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            _canvasGroup = rootObject.GetComponent<CanvasGroup>();

            GameObject textObject = new(
                "ExclamationMark",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            _textRoot = textObject.GetComponent<RectTransform>();
            _textRoot.SetParent(_root, false);
            _textRoot.anchorMin = Vector2.zero;
            _textRoot.anchorMax = Vector2.one;
            _textRoot.offsetMin = Vector2.zero;
            _textRoot.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "!";
            text.fontSize = 96f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = IndicatorColor;
            text.outlineWidth = 0.24f;
            text.outlineColor = OutlineColor;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            _root.gameObject.SetActive(false);
        }

        private void SyncPosition()
        {
            float top = _collider != null
                ? _collider.bounds.max.y
                : transform.position.y + 1.5f;

            _root.position = new Vector3(
                transform.position.x,
                top + IndicatorHeightOffset,
                transform.position.z);
        }

        private void FaceCamera()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            Vector3 directionAwayFromCamera =
                _root.position - _mainCamera.transform.position;
            if (directionAwayFromCamera.sqrMagnitude > 0.001f)
                _root.rotation = Quaternion.LookRotation(
                    directionAwayFromCamera.normalized,
                    Vector3.up);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();

            if (_root != null)
                Destroy(_root.gameObject);
        }

        private void OnDisable()
        {
            _sequence?.Kill();

            if (_root != null)
                _root.gameObject.SetActive(false);
        }
    }
}
