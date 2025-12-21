using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CustomPackages.Package.Extensions.Animations
{
    public static class DoTweenExtensions
    {
        public static Sequence DoPump(this Transform transform, float maxSize = 1.2f, float duration = .3f,
            CancellationToken token = default)
        {
            var sequence = DOTween.Sequence();
            var startScale = transform.localScale;
            sequence.Append(transform.DOScale(maxSize, duration / 2));
            sequence.Append(transform.DOScale(startScale, duration / 2));
            sequence.SetLink(transform.gameObject).SetId($"DoPump {transform.name}");
            if (token == default)
                token = transform.gameObject.GetCancellationTokenOnDestroy();
            sequence.ToUniTask(cancellationToken: token);
            return sequence;
        }

        public static void DoPumpWithReset(this Transform transform, float maxSize = 1.2f, float duration = .3f,
            CancellationToken token = default)
        {
            if (token == default)
                token = transform.gameObject.GetCancellationTokenOnDestroy();
            
            DOTween.Kill(transform, complete: true);
            transform.DOScale(maxSize, duration * 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(transform.gameObject).SetId($"DoPumpWithReset {transform.name}")
                .Play().ToUniTask(cancellationToken: token);
        }

        public static UniTask DoPumpAsync(this Transform transform, float maxSize = 1.2f, float duration = .3f,
            CancellationToken token = default)
        {
            var sequence = DOTween.Sequence();
            var startScale = transform.localScale;
            sequence.Append(transform.DOScale(maxSize, duration / 2));
            sequence.Append(transform.DOScale(startScale, duration / 2));
            sequence.SetLink(transform.gameObject).SetId($"DoPumpAsync {transform.name}");
            if (token == default)
                token = transform.gameObject.GetCancellationTokenOnDestroy();
            return sequence.ToUniTask(cancellationToken: token);
        }

        public static Sequence DoCustomJump(this Transform transform, Vector3 position)
        {
            var seq = DOTween.Sequence();
            seq.Append(transform.transform.DOJump(position, 3, 1, .5f));
            seq.Join(transform.transform.DOScale(.5f, .5f));
            seq.SetLink(transform.gameObject).SetId($"DoCustomJump {transform.name}");
            return seq;
        }

        public static void DoShake(this Transform transform, CancellationToken token = default, float duration = 0.8f,
            float strength = 0.2f,
            int vibrato = 14)
        {
            var defaultPos = transform.position;
            Tween shakeTween = transform.DOShakePosition(duration, Vector2.one * strength, vibrato, 90f,
                    false, true, ShakeRandomnessMode.Harmonic)
                .SetId("DoTweenExtensions DoShake")
                .SetLink(transform.gameObject)
                .OnKill(() => transform.position = defaultPos);
            shakeTween.ToUniTask(cancellationToken: token)
                .SuppressCancellationThrow();
        }

        public static void DoShakeX(this Transform transform, float duration = 0.8f, float strength = 0.2f,
            int vibrato = 14)
        {
            var defaultPos = transform.position;
            transform.DOShakePosition(duration, Vector2.right * strength, vibrato, 90f,
                    false, true, ShakeRandomnessMode.Harmonic)
                .SetId("DoTweenExtensions DoShakeX")
                .SetLink(transform.gameObject)
                .OnKill(() => transform.position = defaultPos);
        }

        public static UniTask DoShakeAsync(this Transform transform, CancellationToken token,
            float duration = 0.3f, float strength = 0.1f, int vibrato = 15)
        {
            var defaultPos = transform.position;
            Tween shakeTween = transform
                .DOShakePosition(duration, Vector2.one * strength, vibrato, 90f,
                    false, true, ShakeRandomnessMode.Harmonic)
                .OnKill(() => transform.position = defaultPos)
                .SetLink(transform.gameObject)
                .SetId("DoShakeAsync");
            return shakeTween.ToUniTask(cancellationToken: token);
        }
    }
}