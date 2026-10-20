using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Features.Bosses.UI
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class BossIntroScreen : MonoBehaviour
    {
        public const string Address = "Assets/Features/Bosses/UI/Prefabs/BossIntroScreen.prefab";

        [SerializeField] private RectTransform _viewport;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private CanvasGroup _artworkGroup;
        [SerializeField] private UnityEngine.UI.Image _playerImage;
        [SerializeField] private UnityEngine.UI.Image _bossImage;
        [SerializeField] private TMP_Text _playerName;
        [SerializeField] private TMP_Text _bossName;
        [SerializeField] private AnimatedElement _playerPortrait;
        [SerializeField] private AnimatedElement _bossPortrait;
        [SerializeField] private AnimatedElement _playerNameElement;
        [SerializeField] private AnimatedElement _playerLabel;
        [SerializeField] private AnimatedElement _bossCaption;
        [SerializeField] private AnimatedElement _versus;
        [SerializeField] private UnityEngine.UI.Image _versusImage;
        [SerializeField, Min(0.01f)] private float _duration = 3.4f;

        private static readonly float[] SideTimes =
            { 0f, .166f, .333f, .367f, .433f, .5f, .567f, 3f, 3.067f, 3.1f, 3.267f, 3.3f };
        private static readonly float[] SideScaleX =
            { 1.45f, 1.45f, 1.35f, .65f, 1.12f, .96f, 1f, 1f, .72f, 1.1f, 1.35f, 1.35f };
        private static readonly float[] SideScaleY =
            { .42f, .42f, .62f, 1.3f, .88f, 1.05f, 1f, 1f, 1.22f, .78f, .48f, .48f };
        private static readonly float[] SideAlphas =
            { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, .1f, 0f };
        private static readonly float[] VersusTimes =
            { 0f, .599f, .6f, .733f, .8f, .9f, .967f, 1f, 3.067f, 3.3f };
        private static readonly float[] VersusAngles =
            { 35f, 35f, 35f, 90f, 190f, 270f, 350f, 360f, 360f, 360f };
        private static readonly float[] VersusScales =
            { 2.45f, 2.45f, 2.45f, 2.35f, 2.15f, 1.55f, 1.02f, 1f, 1f, 1f };
        private static readonly float[] VersusAlphas =
            { 0f, 0f, .16f, .38f, .55f, .75f, 1f, 1f, 1f, 0f };
        private static readonly float[] VersusBrightness =
            { .2f, .2f, .3f, .45f, .6f, .8f, 1f, 1f, 1f, 1f };
        private static readonly float[] ArtworkTimes = { 0f, .166f, .167f, 3.133f, 3.3f, 3.4f };
        private static readonly float[] ArtworkAlphas = { 0f, 0f, 1f, 1f, 0f, 0f };

        private CancellationTokenSource _playbackCts;
        private Color _versusColor;
        private float _playerNameSize;
        private float _bossNameSize;
        private bool _captured;
        private bool _isPlaying;

        public async UniTask PlayAsync(Sprite playerPortrait, string playerName,
            Sprite bossPortrait, string bossName, Rect bossPortraitRect,
            CancellationToken cancellationToken)
        {
            if (playerPortrait == null)
                throw new ArgumentNullException(nameof(playerPortrait));
            if (bossPortrait == null)
                throw new ArgumentNullException(nameof(bossPortrait));
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("The current character needs an intro name.", nameof(playerName));
            if (string.IsNullOrWhiteSpace(bossName))
                throw new ArgumentException("The room boss needs an intro name.", nameof(bossName));
            if (!(bossPortraitRect.width > 0f) || !(bossPortraitRect.height > 0f))
                throw new ArgumentException("The boss portrait rectangle must have a positive size.", nameof(bossPortraitRect));
            if (_isPlaying)
                throw new InvalidOperationException("A boss intro is already playing on this screen.");

            cancellationToken.ThrowIfCancellationRequested();
            ValidateBindings();
            CaptureRestPose();
            _isPlaying = true;
            using (var playback = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken, this.GetCancellationTokenOnDestroy()))
            {
                _playbackCts = playback;
                try
                {
                    _playerImage.sprite = playerPortrait;
                    _bossImage.sprite = bossPortrait;
                    _playerName.text = playerName.ToUpperInvariant();
                    _bossName.text = bossName.ToUpperInvariant();
                    SetBossRectangle(bossPortraitRect);
                    gameObject.SetActive(true);
                    _viewport.ForceUpdateRectTransforms();
                    FitName(_playerName, _playerNameSize);
                    FitName(_bossName, _bossNameSize);
                    _canvasGroup.alpha = 1f;
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = true;
                    ApplyFrame(0f);

                    float duration = Mathf.Max(.01f, _duration);
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, playback.Token);
                        elapsed += Time.unscaledDeltaTime;
                        ApplyFrame(Mathf.Min(elapsed / duration, 1f) * 3.4f);
                    }
                }
                finally
                {
                    Hide();
                    _playbackCts = null;
                    _isPlaying = false;
                }
            }
        }

        public void Hide()
        {
            _playbackCts?.Cancel();
            if (this == null)
                return;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_artworkGroup != null)
                _artworkGroup.alpha = 0f;
            if (_captured)
            {
                _playerPortrait.Restore();
                _bossPortrait.Restore();
                _playerNameElement.Restore();
                _playerLabel.Restore();
                _bossCaption.Restore();
                _versus.Restore();
                _versusImage.color = _versusColor;
            }
            if (_playerImage != null)
                _playerImage.sprite = null;
            if (_bossImage != null)
                _bossImage.sprite = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void Awake()
        {
            CaptureRestPose();
            Hide();
        }

        private void OnDisable() => _playbackCts?.Cancel();

        private void CaptureRestPose()
        {
            if (_captured || _versusImage == null || _playerName == null || _bossName == null)
                return;
            if (!HasBinding(_playerPortrait) || !HasBinding(_bossPortrait)
                || !HasBinding(_playerNameElement) || !HasBinding(_playerLabel)
                || !HasBinding(_bossCaption) || !HasBinding(_versus))
                return;

            _playerPortrait.Capture();
            _bossPortrait.Capture();
            _playerNameElement.Capture();
            _playerLabel.Capture();
            _bossCaption.Capture();
            _versus.Capture();
            _versusColor = _versusImage.color;
            _playerNameSize = _playerName.fontSize;
            _bossNameSize = _bossName.fontSize;
            _captured = true;
        }

        private void ValidateBindings()
        {
            if (_viewport == null || _canvasGroup == null || _artworkGroup == null
                || _playerImage == null || _bossImage == null || _playerName == null
                || _bossName == null || _versusImage == null
                || !HasBinding(_playerPortrait) || !HasBinding(_bossPortrait)
                || !HasBinding(_playerNameElement) || !HasBinding(_playerLabel)
                || !HasBinding(_bossCaption) || !HasBinding(_versus))
                throw new InvalidOperationException("BossIntroScreen has missing prefab references.");
        }

        private static bool HasBinding(AnimatedElement element) =>
            element != null && element.Rect != null && element.Group != null;

        private static void FitName(TMP_Text text, float maximumSize)
        {
            text.fontSize = maximumSize;
            float width = text.rectTransform.rect.width;
            float preferredWidth = text.GetPreferredValues(text.text).x;
            if (width > 0f && preferredWidth > width)
                text.fontSize = maximumSize * width / preferredWidth;
        }

        private void SetBossRectangle(Rect rectangle)
        {
            RectTransform rect = _bossPortrait.Rect;
            rect.anchorMin = new Vector2(rectangle.x, 1f - rectangle.y - rectangle.height);
            rect.anchorMax = new Vector2(rectangle.x + rectangle.width, 1f - rectangle.y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            _bossPortrait.RestPosition = Vector2.zero;
        }

        private void ApplyFrame(float time)
        {
            _artworkGroup.alpha = Sample(time, ArtworkTimes, ArtworkAlphas);
            ApplySide(_playerPortrait, time, -180f, 3.2f);
            ApplySide(_bossPortrait, time, 170f, -3.2f);
            ApplySide(_playerNameElement, time, -180f, 0f);
            ApplySide(_playerLabel, time, -440f, 0f);
            ApplySide(_bossCaption, time, 170f, 0f);

            float angle = Sample(time, VersusTimes, VersusAngles);
            float scale = Sample(time, VersusTimes, VersusScales);
            float brightness = Sample(time, VersusTimes, VersusBrightness);
            _versus.Rect.localRotation = _versus.RestRotation * Quaternion.Euler(0f, 0f, -angle);
            _versus.Rect.localScale = _versus.RestScale * scale;
            _versus.Group.alpha = Sample(time, VersusTimes, VersusAlphas);
            _versusImage.color = new Color(_versusColor.r * brightness,
                _versusColor.g * brightness, _versusColor.b * brightness, _versusColor.a);
        }

        private void ApplySide(AnimatedElement element, float time, float offscreenPercent, float driftPercent)
        {
            int index = FindSegment(time, SideTimes, out float progress);
            int next = Mathf.Min(index + 1, SideTimes.Length - 1);
            float offscreen = element.Rect.rect.width * offscreenPercent * .01f;
            float drift = _viewport.rect.width * driftPercent * .01f;
            float shift = Mathf.Lerp(SidePosition(index, offscreen, drift),
                SidePosition(next, offscreen, drift), progress);
            element.Rect.anchoredPosition = element.RestPosition + new Vector2(shift, 0f);
            element.Rect.localScale = Vector3.Scale(element.RestScale,
                new Vector3(Mathf.Lerp(SideScaleX[index], SideScaleX[next], progress),
                    Mathf.Lerp(SideScaleY[index], SideScaleY[next], progress), 1f));
            element.Group.alpha = Mathf.Lerp(SideAlphas[index], SideAlphas[next], progress);
        }

        private static float SidePosition(int index, float offscreen, float drift)
        {
            if (index <= 1)
                return offscreen;
            if (index <= 6)
                return 0f;
            return index >= 10 ? offscreen + drift : drift;
        }

        private static float Sample(float time, float[] times, float[] values)
        {
            int index = FindSegment(time, times, out float progress);
            return Mathf.Lerp(values[index], values[Mathf.Min(index + 1, values.Length - 1)], progress);
        }

        private static int FindSegment(float time, float[] times, out float progress)
        {
            for (int i = 0; i < times.Length - 1; i++)
            {
                if (time <= times[i + 1])
                {
                    progress = Mathf.InverseLerp(times[i], times[i + 1], time);
                    return i;
                }
            }
            progress = 0f;
            return times.Length - 1;
        }

        [Serializable]
        private sealed class AnimatedElement
        {
            public RectTransform Rect;
            public CanvasGroup Group;

            [NonSerialized] public Vector2 RestPosition;
            [NonSerialized] public Vector3 RestScale;
            [NonSerialized] public Quaternion RestRotation;

            public void Capture()
            {
                RestPosition = Rect.anchoredPosition;
                RestScale = Rect.localScale;
                RestRotation = Rect.localRotation;
            }

            public void Restore()
            {
                Rect.anchoredPosition = RestPosition;
                Rect.localScale = RestScale;
                Rect.localRotation = RestRotation;
                Group.alpha = 1f;
            }
        }
    }
}
