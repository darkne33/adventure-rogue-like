using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyDamageNumberView : MonoBehaviour, IDamageView
    {
        private const int POOL_SIZE = 6;
        private static readonly Color32 TextOutlineColor = new(35, 8, 8, 230);

        [SerializeField] private Canvas _worldCanvas;
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Inject] private DiContainer _container;

        private readonly Queue<DamageNumber> _availableNumbers = new();
        private readonly List<DamageNumber> _numbers = new();

        private RectTransform _root;

        private void Awake()
        {
            _worldCanvas ??= GetComponentInChildren<Canvas>();
            CreatePool();
        }

        private void LateUpdate() =>
            SyncRootTransform();

        public void ShowDamage(int damage, float maximumHealth, bool isCritical)
        {
            SyncRootTransform();
            DamageNumber number = GetNumber();
            float damageRatio = maximumHealth > 0f ? damage / maximumHealth : 0f;

            number.Sequence?.Kill();
            number.Text.text = isCritical ? $"{damage}!" : damage.ToString();
            number.Text.fontSize = isCritical ? 1.25f : 1.05f;
            number.Text.color = Color.white;
            number.Text.outlineColor = TextOutlineColor;
            number.CanvasGroup.alpha = 1f;

            float scale = Mathf.Lerp(0.85f, 1.35f, Mathf.InverseLerp(0.02f, 0.35f, damageRatio));
            if (isCritical)
                scale *= 1.4f;

            float horizontalOffset = Random.Range(-0.45f, 0.45f);
            Vector2 startPosition = new(horizontalOffset, 0.55f);
            float riseDistance = isCritical ? 1.65f : 1.25f;
            Vector2 endPosition = startPosition + new Vector2(horizontalOffset * 0.35f, riseDistance);

            number.RectTransform.anchoredPosition = startPosition;
            number.RectTransform.localRotation = Quaternion.identity;
            number.RectTransform.localScale = Vector3.one * scale * (isCritical ? 0.22f : 0.45f);
            number.GameObject.SetActive(true);

            number.Sequence = DOTween.Sequence().SetLink(number.GameObject);

            if (isCritical)
            {
                number.Sequence
                    .Append(number.RectTransform.DOScale(scale, 0.24f).SetEase(Ease.OutElastic, 1.15f, 0.35f))
                    .Join(number.RectTransform.DOPunchRotation(
                        new Vector3(0f, 0f, Random.Range(-14f, 14f)), 0.28f, 7, 0.55f));
            }
            else
            {
                number.Sequence.Append(number.RectTransform.DOScale(scale, 0.16f).SetEase(Ease.OutBack));
            }

            number.Sequence
                .Join(number.RectTransform.DOAnchorPos(endPosition, isCritical ? 0.9f : 0.75f)
                    .SetEase(Ease.OutCubic))
                .AppendInterval(0.12f)
                .Append(number.CanvasGroup.DOFade(0f, 0.28f).SetEase(Ease.InQuad))
                .Join(number.RectTransform.DOAnchorPosY(endPosition.y + 0.35f, 0.28f).SetEase(Ease.InQuad))
                .OnComplete(() => Release(number));
        }

        private void CreatePool()
        {
            GameObject rootObject = new("EnemyDamageNumbers", typeof(RectTransform), typeof(Canvas),
                typeof(ViewToCamera));
            _root = rootObject.GetComponent<RectTransform>();
            _root.sizeDelta = ((RectTransform)_worldCanvas.transform).sizeDelta;

            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = _worldCanvas.sortingOrder + 1;
            SyncRootTransform();

            ViewToCamera viewToCamera = rootObject.GetComponent<ViewToCamera>();
            viewToCamera.Initialize(_root, true);
            _container.Inject(viewToCamera);

            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject numberObject = new($"DamageNumber_{i}", typeof(RectTransform), typeof(CanvasGroup),
                    typeof(TextMeshProUGUI));
                RectTransform rectTransform = numberObject.GetComponent<RectTransform>();
                rectTransform.SetParent(_root, false);
                rectTransform.sizeDelta = new Vector2(3.5f, 1.3f);

                TextMeshProUGUI text = numberObject.GetComponent<TextMeshProUGUI>();
                text.font = _fontAsset;
                text.fontSize = 1.05f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.outlineWidth = 0.18f;
                text.outlineColor = TextOutlineColor;

                DamageNumber number = new(numberObject, rectTransform, numberObject.GetComponent<CanvasGroup>(), text);
                _numbers.Add(number);
                Release(number);
            }
        }

        private void SyncRootTransform()
        {
            if (_root == null || _worldCanvas == null)
                return;

            Transform source = _worldCanvas.transform;
            _root.position = source.position;

            Vector3 sourceScale = source.lossyScale;
            _root.localScale = new Vector3(
                Mathf.Abs(sourceScale.x),
                Mathf.Abs(sourceScale.y),
                Mathf.Abs(sourceScale.z));
        }

        private DamageNumber GetNumber()
        {
            if (_availableNumbers.Count > 0)
                return _availableNumbers.Dequeue();

            DamageNumber oldestNumber = _numbers[0];
            oldestNumber.Sequence?.Kill();
            return oldestNumber;
        }

        private void Release(DamageNumber number)
        {
            if (!number.GameObject.activeSelf && _availableNumbers.Contains(number))
                return;

            number.GameObject.SetActive(false);
            number.Sequence = null;
            _availableNumbers.Enqueue(number);
        }

        private void OnDestroy()
        {
            if (_root != null)
                Destroy(_root.gameObject, 1.5f);
        }

        private sealed class DamageNumber
        {
            public readonly GameObject GameObject;
            public readonly RectTransform RectTransform;
            public readonly CanvasGroup CanvasGroup;
            public readonly TextMeshProUGUI Text;
            public Sequence Sequence;

            public DamageNumber(GameObject gameObject, RectTransform rectTransform, CanvasGroup canvasGroup,
                TextMeshProUGUI text)
            {
                GameObject = gameObject;
                RectTransform = rectTransform;
                CanvasGroup = canvasGroup;
                Text = text;
            }
        }
    }
}
