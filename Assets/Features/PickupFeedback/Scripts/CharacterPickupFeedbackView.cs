using System.Collections.Generic;
using Core;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Features.PickupFeedback
{
    public sealed class CharacterPickupFeedbackView : MonoBehaviour
    {
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private UnityEngine.UI.Image _icon;
        [SerializeField] private UnityEngine.UI.Image _glow;
        [SerializeField] private UnityEngine.UI.Image _ring;
        [SerializeField] private UnityEngine.UI.Image _flash;
        [SerializeField] private UnityEngine.UI.Image[] _sparks;
        [SerializeField] private TMP_Text _amount;

        [Inject] private ICameraService _cameraService;
        [Inject] private ITimeScaleService _timeScaleService;

        private readonly Queue<PickupRequest> _pending = new();
        private CharacterFacade _character;
        private Vector3 _anchorLocalPosition;
        private PickupFeedbackConfiguration _configuration;
        private PickupFeedbackStyle _style;
        private PickupFeedbackKind _kind;
        private Transform _cameraTransform;
        private Vector2 _iconOrigin;
        private Vector2 _amountOrigin;
        private float _elapsed;
        private float _sparkPhase;
        private int _pulseIndex;
        private bool _playing;

        private bool IsPaused => _timeScaleService.IsPaused || _character.IsCinematicPaused;

        private void Awake()
        {
            _iconOrigin = _icon.rectTransform.anchoredPosition;
            _amountOrigin = _amount.rectTransform.anchoredPosition;
            _group.alpha = 0f;
        }

        private void OnEnable() =>
            Canvas.preWillRenderCanvases += SyncBeforeCanvasRendering;

        public void Initialize(CharacterFacade character, PickupFeedbackConfiguration configuration)
        {
            _character = character;
            _configuration = configuration;
            CacheAnchor(character.GetComponent<Collider>());
            CacheCamera();
            SyncTransform();
        }

        public void Show(PickupFeedbackKind kind, Sprite icon = null, int amount = 0)
        {
            if (_configuration == null || _character == null || !_character.isActiveAndEnabled)
                return;

            icon = kind switch
            {
                PickupFeedbackKind.Silver => _configuration.SilverIcon,
                PickupFeedbackKind.Key => _configuration.KeyIcon,
                _ => icon
            };
            if (icon == null)
                return;

            var request = new PickupRequest(kind, icon, amount);
            // Repeated coins pulse immediately, as in the approved preview. Different
            // pickups wait their turn so an item or relic icon is not lost in a burst.
            bool refreshCurrency = _playing && IsCurrency(kind) && _kind == kind && _pending.Count == 0;
            if (!IsPaused && (!_playing || refreshCurrency))
                Begin(request);
            else
                _pending.Enqueue(request);
        }

        private void Update()
        {
            if (_character == null || _configuration == null)
                return;

            if (!_character.isActiveAndEnabled || _character.HealthSystem?.IsDead == true ||
                _character.IsTransitionPaused)
            {
                Clear();
                return;
            }

            if (IsPaused)
            {
                _group.alpha = 0f;
                return;
            }

            if (!_playing)
            {
                if (_pending.Count > 0)
                    Begin(_pending.Dequeue());
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _style.Duration)
            {
                _playing = false;
                _group.alpha = 0f;
                if (_pending.Count > 0)
                    Begin(_pending.Dequeue());
                else
                    _icon.sprite = null;
                return;
            }

            RenderFrame();
        }

        private void SyncBeforeCanvasRendering()
        {
            if (_configuration == null || _character == null || !_playing)
                return;

            // Cinemachine has finished moving its output camera. Apply the final
            // UI pose before Canvas rebuilds, regardless of LateUpdate ordering.
            CacheCamera();
            SyncTransform();
        }

        private void OnDisable()
        {
            Canvas.preWillRenderCanvases -= SyncBeforeCanvasRendering;
            Clear();
        }

        private void Begin(PickupRequest request)
        {
            _kind = request.Kind;
            _style = _configuration.GetStyle(_kind);
            _elapsed = 0f;
            _playing = true;
            // Keep cosmetic variation independent of the gameplay random sequence.
            _sparkPhase = ++_pulseIndex * 0.61803399f;
            _icon.sprite = request.Icon;
            _icon.rectTransform.sizeDelta = Vector2.one * _style.IconSize;
            _icon.color = Color.white;
            _amount.gameObject.SetActive(IsCurrency(_kind) && request.Amount > 0);
            if (request.Amount > 0)
                _amount.text = $"+{request.Amount}";
            _amount.color = _style.Color;
            SyncTransform();
            RenderFrame();
        }

        private void RenderFrame()
        {
            float duration = Mathf.Max(0.01f, _style.Duration);
            float fadeDuration = Mathf.Clamp(_configuration.FadeDuration, 0.01f, duration);
            float fadeProgress = Mathf.Clamp01((_elapsed - (duration - fadeDuration)) / fadeDuration);
            _group.alpha = 1f - fadeProgress;
            _visualRoot.localScale = Vector3.one;

            float popDuration = Mathf.Max(0.01f, _configuration.PopDuration);
            float entryDuration = popDuration * 4f / 7f;
            float iconScale = _elapsed < entryDuration
                ? Mathf.Lerp(0.3f, 1.12f, EaseOut(_elapsed / entryDuration))
                : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((_elapsed - entryDuration) /
                    Mathf.Max(0.01f, popDuration - entryDuration)));
            _icon.rectTransform.localScale = Vector3.one * iconScale;
            Vector2 rise = Vector2.up * (_configuration.RiseDistance * EaseOut(fadeProgress));
            _icon.rectTransform.anchoredPosition = _iconOrigin + rise;
            _amount.rectTransform.anchoredPosition = _amountOrigin + rise;

            bool silver = _kind == PickupFeedbackKind.Silver;
            SetColor(_glow, _style.Color, silver ? 0.25f : 0.34f);
            _glow.rectTransform.localScale = Vector3.one * (silver ? 0.72f : 1f);

            float ringProgress = Mathf.Clamp01(_elapsed /
                Mathf.Max(0.01f, _configuration.RingDuration));
            _ring.rectTransform.localScale = Vector3.one *
                Mathf.Lerp(0.28f, silver ? 0.73f : 1f, EaseOut(ringProgress));
            SetColor(_ring, _style.Color, 0.8f * (1f - ringProgress));

            float flashProgress = Mathf.Clamp01(_elapsed /
                Mathf.Max(0.01f, _configuration.FlashDuration));
            _flash.rectTransform.localScale = Vector3.one * (1f - flashProgress);
            _flash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            SetColor(_flash, _style.Color, 0.7f * (1f - flashProgress));

            int sparkCount = Mathf.Clamp(_style.SparkCount, 0, _sparks.Length);
            float sparkLifetime = Mathf.Min(duration, silver ? 0.6f : 1f);
            float sparkProgress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, sparkLifetime));
            float spread = 22f + EaseOut(_elapsed / (silver ? 0.6f : 0.9f)) * _style.SpreadRadius;
            for (int i = 0; i < _sparks.Length; i++)
            {
                UnityEngine.UI.Image spark = _sparks[i];
                spark.enabled = i < sparkCount;
                if (i >= sparkCount)
                    continue;

                float angle = i * Mathf.PI * 2f / sparkCount + _sparkPhase;
                RectTransform sparkTransform = spark.rectTransform;
                sparkTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * spread,
                    Mathf.Sin(angle) * spread * 0.8f - _elapsed * 11f);
                float size = (i % 3 == 0 ? 9.6f : 5.2f) * Mathf.Max(0f, 1f - 0.4f * _elapsed);
                sparkTransform.sizeDelta = Vector2.one * size;
                sparkTransform.localRotation = Quaternion.Euler(0f, 0f,
                    (angle + _elapsed * 0.25f) * Mathf.Rad2Deg);
                SetColor(spark, _style.Color, 1f - sparkProgress);
            }
        }

        private void SyncTransform()
        {
            // Follow the interpolated visual Transform, not the fixed-step physics AABB.
            Vector3 position = _character.transform.TransformPoint(_anchorLocalPosition) +
                               Vector3.up * _configuration.HeadClearance;
            transform.position = position;

            if (_cameraTransform == null)
                CacheCamera();
            if (_cameraTransform != null)
                transform.rotation = _cameraTransform.rotation;

            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float scale = _configuration.WorldScale;
            transform.localScale = new Vector3(scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
                scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        }

        private void CacheCamera()
        {
            CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(_cameraService.MainCamera);
            Camera renderingCamera = brain != null ? brain.OutputCamera : Camera.main;
            _cameraTransform = renderingCamera != null ? renderingCamera.transform : null;
        }

        private void CacheAnchor(Collider characterCollider)
        {
            Transform characterRoot = _character.transform;
            if (characterCollider is CapsuleCollider capsule && capsule.direction == 1)
            {
                // The authored player capsules are upright. Read their local geometry
                // once so physics synchronization cannot move the visual anchor.
                Vector3 localTop = capsule.center +
                                   Vector3.up * Mathf.Max(capsule.radius, capsule.height * 0.5f);
                _anchorLocalPosition = characterRoot.InverseTransformPoint(
                    capsule.transform.TransformPoint(localTop));
            }
            else if (characterCollider != null)
            {
                Bounds bounds = characterCollider.bounds;
                _anchorLocalPosition = characterRoot.InverseTransformPoint(
                    new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            }
            else
                _anchorLocalPosition = Vector3.zero;
        }

        private void Clear()
        {
            _pending.Clear();
            _playing = false;
            if (_group != null)
                _group.alpha = 0f;
            if (_icon != null)
                _icon.sprite = null;
        }

        private static float EaseOut(float progress) =>
            1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 3f);

        private static bool IsCurrency(PickupFeedbackKind kind) =>
            kind is PickupFeedbackKind.Silver or PickupFeedbackKind.Key;

        private static void SetColor(UnityEngine.UI.Image image, Color color, float alpha)
        {
            color.a *= Mathf.Clamp01(alpha);
            image.color = color;
        }

        private readonly struct PickupRequest
        {
            public PickupFeedbackKind Kind { get; }
            public Sprite Icon { get; }
            public int Amount { get; }

            public PickupRequest(PickupFeedbackKind kind, Sprite icon, int amount)
            {
                Kind = kind;
                Icon = icon;
                Amount = amount;
            }
        }
    }
}
