using System.Collections.Generic;
using Core;
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
        [Header("Pickup Fountain")]
        [SerializeField] private ParticleSystem[] _currencyParticles;
        [SerializeField] private Vector2 _currencyIconOffset;
        [SerializeField, Min(0f)] private float _currencyDisappearTime = 23f / 24f;
        [SerializeField, Min(0f)] private float _fountainDissolveStart = 0.75f;
        [SerializeField] private AnimationCurve _currencyPopScale = AnimationCurve.Constant(0f, 1f, 1f);
        [SerializeField] private AnimationCurve _currencyRise = AnimationCurve.Constant(0f, 1f, 0f);
        [SerializeField] private FountainParticle[] _fountainPattern;
        [SerializeField] private Vector2 _fountainAcceleration = new(0f, -241.92f);
        [SerializeField, Min(0.01f)] private float _fountainParticleLifetime = 15f / 24f;
        [SerializeField, Min(0.01f)] private float _fountainStarDuration = 13f / 24f;
        [SerializeField, Min(0.01f)] private float _fountainStarSize = 10f;
        [SerializeField] private AnimationCurve _fountainSquareSize;
        [SerializeField, Min(0f)] private float _fountainTrailStart = 3f / 24f;
        [SerializeField, Min(0f)] private float _fountainTrailEnd = 12f / 24f;
        [SerializeField, Min(0.001f)] private float _fountainDetailFade = 1f / 24f;
        [SerializeField, Min(0f)] private float _fountainTrailOffset = 1.2f / 24f;
        [SerializeField, Min(0.01f)] private float _fountainDetailSize = 2f;
        [SerializeField] private Vector2[] _fountainHaloPoints;
        [SerializeField, Min(0.01f)] private float _fountainHaloStep = 4f / 24f;
        [SerializeField, Min(0f)] private float _fountainHaloEnd = 19f / 24f;
        [SerializeField] private Vector2 _fountainLaunchPosition = new(0f, -8f);
        [SerializeField] private AnimationCurve _fountainLaunchWidth;
        [SerializeField] private AnimationCurve _fountainLaunchHeight;

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
        private float _elapsed;
        private float _sparkPhase;
        private int _pulseIndex;
        private bool _playing;
        private ParticleSystemRenderer[] _currencyParticleRenderers;
        private ParticleSystem.Particle[] _fountainSquares;
        private ParticleSystem.Particle[] _fountainStars;
        private Material _iconMaterial;

        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int BlockCountId = Shader.PropertyToID("_BlockCount");
        private static readonly int SpriteUvRectId = Shader.PropertyToID("_SpriteUVRect");

        private bool IsPaused => _timeScaleService.IsPaused || _character.IsCinematicPaused;
        private bool UsesFountainParticles => (IsCurrency(_kind) || _kind == PickupFeedbackKind.Relic) &&
                                              _currencyParticles != null && _currencyParticles.Length >= 2 &&
                                              _fountainPattern != null && _fountainPattern.Length > 0;

        private void Awake()
        {
            _iconOrigin = _icon.rectTransform.anchoredPosition;
            _iconMaterial = new Material(_icon.material);
            _icon.material = _iconMaterial;
            _group.alpha = 0f;
            int count = _currencyParticles?.Length ?? 0;
            _currencyParticleRenderers = new ParticleSystemRenderer[count];
            for (int i = 0; i < count; i++)
                _currencyParticleRenderers[i] = _currencyParticles[i].GetComponent<ParticleSystemRenderer>();
            int patternCount = _fountainPattern?.Length ?? 0;
            _fountainSquares = new ParticleSystem.Particle[
                Mathf.Max(1, patternCount * 2 + (_fountainHaloPoints?.Length ?? 0) + 1)];
            _fountainStars = new ParticleSystem.Particle[Mathf.Max(1, patternCount)];
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

            var request = new PickupRequest(kind, icon);
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
                SetFountainParticlesVisible(false);
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
                StopFountainParticles();
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

        private void OnDestroy()
        {
            if (_iconMaterial != null)
                Destroy(_iconMaterial);
        }

        private void Begin(PickupRequest request)
        {
            StopFountainParticles();
            _kind = request.Kind;
            _style = _configuration.GetStyle(_kind);
            _elapsed = 0f;
            _playing = true;
            // Keep cosmetic variation independent of the gameplay random sequence.
            _sparkPhase = ++_pulseIndex * 0.61803399f;
            _icon.sprite = request.Icon;
            _icon.rectTransform.sizeDelta = Vector2.one * _style.IconSize;
            _icon.color = Color.white;
            _icon.enabled = true;
            _iconMaterial.SetFloat(DissolveId, 0f);
            Vector4 spriteUv = UnityEngine.Sprites.DataUtility.GetOuterUV(request.Icon);
            _iconMaterial.SetVector(SpriteUvRectId,
                new Vector4(spriteUv.x, spriteUv.y, spriteUv.z - spriteUv.x, spriteUv.w - spriteUv.y));
            _glow.enabled = !UsesFountainParticles;
            _ring.enabled = !UsesFountainParticles;
            _flash.enabled = !UsesFountainParticles;
            if (UsesFountainParticles)
            {
                foreach (UnityEngine.UI.Image spark in _sparks)
                    spark.enabled = false;
                foreach (ParticleSystem particles in _currencyParticles)
                {
                    particles.gameObject.SetActive(true);
                    particles.Play(false);
                    particles.Pause(false);
                }
            }
            SyncTransform();
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (UsesFountainParticles)
            {
                RenderFountainFrame();
                return;
            }

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

        private void RenderFountainFrame()
        {
            // The prefab stores the selected preview's trajectories and timing.
            // Evaluate the real Particle Systems continuously without random emission.
            float duration = Mathf.Max(0.01f, _style.Duration);
            float sampleTime = Mathf.Clamp01(_elapsed / duration);
            float rise = _currencyRise.Evaluate(sampleTime);
            float size = Mathf.Max(0.01f, _style.IconSize * _currencyPopScale.Evaluate(sampleTime));

            _group.alpha = 1f;
            _visualRoot.localScale = Vector3.one;
            _icon.rectTransform.localScale = Vector3.one;
            _icon.rectTransform.sizeDelta = Vector2.one * size;
            _icon.rectTransform.anchoredPosition = _iconOrigin + _currencyIconOffset + Vector2.up * rise;
            _icon.enabled = sampleTime < _currencyDisappearTime;
            _iconMaterial.SetFloat(DissolveId,
                Mathf.InverseLerp(_fountainDissolveStart, _currencyDisappearTime, sampleTime));
            Vector2 iconSize = Vector2.one * size;
            Rect spriteRect = _icon.sprite.rect;
            if (spriteRect.width > spriteRect.height)
                iconSize.y *= spriteRect.height / spriteRect.width;
            else
                iconSize.x *= spriteRect.width / spriteRect.height;
            _iconMaterial.SetVector(BlockCountId, new Vector4(iconSize.x / 4f, iconSize.y / 4f, 0f, 0f));

            int squareCount = 0;
            int starCount = 0;
            foreach (FountainParticle particle in _fountainPattern)
            {
                float age = sampleTime - particle.Delay;
                if (age < 0f || age >= _fountainParticleLifetime)
                    continue;

                Vector2 position = particle.Velocity * age + _fountainAcceleration * (0.5f * age * age);
                bool star = particle.Star && age < _fountainStarDuration;
                float particleSize = star
                    ? Mathf.Lerp(_fountainStarSize, _fountainDetailSize,
                        Mathf.InverseLerp(_fountainStarDuration - _fountainDetailFade, _fountainStarDuration, age))
                    : _fountainSquareSize.Evaluate(age);
                Color particleColor = _style.GetFountainColor(star ? 3 : particle.Shade);
                if (star)
                    _fountainStars[starCount++] = CreateFountainParticle(position,
                        Vector2.one * particleSize, particleColor);
                else
                    WriteFountainSquare(ref squareCount, position, Vector2.one * particleSize, particleColor);

                if (age > _fountainTrailStart && age < _fountainTrailEnd)
                {
                    Vector2 velocity = particle.Velocity + _fountainAcceleration * age;
                    float trailFade = Mathf.InverseLerp(_fountainTrailStart,
                                          _fountainTrailStart + _fountainDetailFade, age) *
                                      (1f - Mathf.InverseLerp(_fountainTrailEnd - _fountainDetailFade,
                                          _fountainTrailEnd, age));
                    Color trailColor = Color.Lerp(_style.GetFountainColor(1), _style.GetFountainColor(0),
                        Mathf.InverseLerp(8f / 24f, 9f / 24f, age));
                    trailColor.a *= trailFade;
                    WriteFountainSquare(ref squareCount, position - velocity * _fountainTrailOffset,
                        Vector2.one * _fountainDetailSize, trailColor);
                }
            }

            float haloPhase = sampleTime / Mathf.Max(0.01f, _fountainHaloStep);
            int haloGroup = Mathf.FloorToInt(haloPhase);
            float haloBlend = Mathf.SmoothStep(0f, 1f, haloPhase - haloGroup);
            float haloFade = 1f - Mathf.InverseLerp(_fountainHaloEnd - _fountainDetailFade,
                _fountainHaloEnd, sampleTime);
            for (int i = 0; i < _fountainHaloPoints.Length && haloFade > 0f; i++)
            {
                float opacity = (i + haloGroup) % 3 == 0 ? 1f - haloBlend :
                    (i + haloGroup + 1) % 3 == 0 ? haloBlend : 0f;
                if (opacity <= 0f)
                    continue;
                Color haloColor = _style.GetFountainColor(0);
                haloColor.a *= opacity * haloFade;
                WriteFountainSquare(ref squareCount, _fountainHaloPoints[i],
                    Vector2.one * _fountainDetailSize, haloColor);
            }

            float lineHeight = _fountainLaunchHeight.Evaluate(sampleTime);
            if (lineHeight > 0f)
                WriteFountainSquare(ref squareCount, _fountainLaunchPosition,
                    new Vector2(_fountainLaunchWidth.Evaluate(sampleTime), lineHeight),
                    _style.GetFountainColor(2));

            _currencyParticles[0].SetParticles(_fountainSquares, squareCount);
            _currencyParticles[1].SetParticles(_fountainStars, starCount);
            SetFountainParticlesVisible(true);
        }

        private void WriteFountainSquare(ref int count, Vector2 position, Vector2 size, Color color) =>
            _fountainSquares[count++] = CreateFountainParticle(position, size, color);

        private static ParticleSystem.Particle CreateFountainParticle(Vector2 position, Vector2 size,
            Color color) => new()
        {
            position = new Vector3(position.x, position.y, 0f),
            startSize3D = new Vector3(size.x, size.y, 1f),
            startColor = color,
            startLifetime = 1f,
            remainingLifetime = 1f
        };

        private void SetFountainParticlesVisible(bool visible)
        {
            if (_currencyParticleRenderers == null)
                return;

            foreach (ParticleSystemRenderer particleRenderer in _currencyParticleRenderers)
                particleRenderer.enabled = visible;
        }

        private void StopFountainParticles()
        {
            if (_currencyParticles == null)
                return;

            foreach (ParticleSystem particles in _currencyParticles)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.gameObject.SetActive(false);
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
            StopFountainParticles();
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

        [System.Serializable]
        private struct FountainParticle
        {
            public float Delay;
            public Vector2 Velocity;
            public bool Star;
            public int Shade;
        }

        private readonly struct PickupRequest
        {
            public PickupFeedbackKind Kind { get; }
            public Sprite Icon { get; }

            public PickupRequest(PickupFeedbackKind kind, Sprite icon)
            {
                Kind = kind;
                Icon = icon;
            }
        }
    }
}
