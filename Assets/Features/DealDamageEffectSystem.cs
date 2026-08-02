using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class DealDamageEffectSystem
    {
        private const float AttackShakeStrength = 0.12f;
        private const int AttackShakeVibrato = 18;
        private const float AttackTelegraphMaxPower = 0.6f;

        private static readonly int HitBlend = Shader.PropertyToID("_HitPower");
        private static readonly int AttackTelegraphBlend =
            Shader.PropertyToID("_AttackTelegraphPower");
        private static readonly int FadeAmount = Shader.PropertyToID("_FadeAmount");
        
        private readonly Renderer[] _meshRenderers;
        private readonly Material[] _materials;
        private readonly Transform _attackTelegraphTransform;
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private Tweener _hitTweener;
        private Tweener _deathFadeTweener;
        private Sequence _attackTelegraphSequence;
        private Vector3 _attackTelegraphStartLocalPosition;
        private bool _hasAttackTelegraphStartPosition;
        private float _hitPower;
        private float _attackTelegraphPower;

        public DealDamageEffectSystem(Renderer[] meshRenderers,
            Transform attackTelegraphTransform = null)
        {
            _meshRenderers = meshRenderers;
            _attackTelegraphTransform = attackTelegraphTransform;
            _materials = new Material[meshRenderers.Length];
            _materials = meshRenderers.Select(x  => x.material).ToArray();
        }

        public void DealDamage(float duration = 0.08f)
        {
            _hitTweener?.Kill();

            _hitPower = 0f;
            ApplyHitBlend();

            _hitTweener = DOTween.To(
                () => _hitPower,
                value =>
                {
                    _hitPower = value;
                    ApplyHitBlend();
                },
                1f,
                duration
            ).SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    _hitPower = 0f;
                    ApplyHitBlend();
                });
        }

        public void BeginAttackTelegraph(float duration)
        {
            ClearAttackTelegraph();

            float safeDuration = Mathf.Max(0f, duration);
            if (safeDuration <= 0f)
            {
                SetAttackTelegraphProgress(1f);
                return;
            }

            Tween whiteTween = DOTween.To(
                    () => _attackTelegraphPower,
                    SetAttackTelegraphProgress,
                    1f,
                    safeDuration)
                .SetEase(Ease.Linear);

            _attackTelegraphSequence = DOTween.Sequence()
                .Join(whiteTween);

            if (_attackTelegraphTransform != null)
            {
                _attackTelegraphStartLocalPosition =
                    _attackTelegraphTransform.localPosition;
                _hasAttackTelegraphStartPosition = true;
                Tween shakeTween = _attackTelegraphTransform
                    .DOShakePosition(
                        safeDuration,
                        Vector3.one * AttackShakeStrength,
                        AttackShakeVibrato,
                        90f,
                        false,
                        false,
                        ShakeRandomnessMode.Harmonic)
                    .SetEase(Ease.Linear);
                _attackTelegraphSequence.Join(shakeTween);
                _attackTelegraphSequence
                    .SetLink(_attackTelegraphTransform.gameObject)
                    .OnKill(RestoreAttackTelegraphPosition);
            }
        }

        private void SetAttackTelegraphProgress(float progress)
        {
            _attackTelegraphPower = Mathf.SmoothStep(
                0f, AttackTelegraphMaxPower, Mathf.Clamp01(progress));
            ApplyAttackTelegraph();
        }

        public void ClearAttackTelegraph()
        {
            _attackTelegraphSequence?.Kill();
            _attackTelegraphSequence = null;
            RestoreAttackTelegraphPosition();
            _attackTelegraphPower = 0f;
            ApplyAttackTelegraph();
        }

        public async UniTask CompleteAttackTelegraph(CancellationToken cancellationToken)
        {
            SetAttackTelegraphProgress(1f);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            ClearAttackTelegraph();
        }

        public Tween PlayDeathFade(float duration)
        {
            _hitTweener?.Kill();
            _deathFadeTweener?.Kill();
            _hitPower = 0f;
            ClearAttackTelegraph();

            bool hasFadeMaterial = false;
            foreach (var mat in _materials)
            {
                if (mat == null || mat.HasProperty(FadeAmount) == false)
                    continue;

                hasFadeMaterial = true;
                mat.SetFloat(FadeAmount, 0f);

                if (mat.HasProperty(HitBlend))
                    mat.SetFloat(HitBlend, 0f);
            }

            if (hasFadeMaterial == false)
                return null;

            _deathFadeTweener = DOTween.To(
                () => 0f,
                value =>
                {
                    foreach (var mat in _materials)
                    {
                        if (mat != null && mat.HasProperty(FadeAmount))
                            mat.SetFloat(FadeAmount, value);
                    }
                },
                1f,
                Mathf.Max(0.01f, duration)
            ).SetEase(Ease.InQuad);

            return _deathFadeTweener;
        }

        private void ApplyHitBlend()
        {
            foreach (Material material in _materials)
            {
                if (material != null && material.HasProperty(HitBlend))
                    material.SetFloat(HitBlend, _hitPower);
            }
        }

        private void ApplyAttackTelegraph()
        {
            foreach (Renderer meshRenderer in _meshRenderers)
            {
                if (meshRenderer == null)
                    continue;

                _propertyBlock.Clear();
                meshRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(AttackTelegraphBlend, _attackTelegraphPower);
                meshRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void RestoreAttackTelegraphPosition()
        {
            if (_attackTelegraphTransform != null &&
                _hasAttackTelegraphStartPosition)
            {
                _attackTelegraphTransform.localPosition =
                    _attackTelegraphStartLocalPosition;
            }

            _hasAttackTelegraphStartPosition = false;
        }
    }
}
