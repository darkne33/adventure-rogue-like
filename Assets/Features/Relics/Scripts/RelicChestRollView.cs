using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Features.Relics.Scripts
{
    [Serializable]
    public sealed class RelicChestRollView
    {
        private const float PreviewRiseLoopDistance = 1f;
        private static readonly int OpenTrigger = Animator.StringToHash("Open");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private Transform _rewardRoot;
        [SerializeField] private Transform _coinSilverFountain;
        [SerializeField] private ParticleSystem[] _treasureRaysParticles;
        [SerializeField] private ParticleSystem _treasureOpenParticle;

        private GameObject _owner;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private Vector3 _initialRewardRootLocalPosition;
        private Tween _previewRiseTween;

        public Transform RewardRoot => _rewardRoot;
        public bool IsConfigured => _animator != null && _shakeTarget != null &&
                                    _rewardRoot != null && _coinSilverFountain != null;

        public void Initialize(GameObject owner)
        {
            _owner = owner;

            if (_shakeTarget != null)
            {
                _initialLocalPosition = _shakeTarget.localPosition;
                _initialLocalRotation = _shakeTarget.localRotation;
                _initialLocalScale = _shakeTarget.localScale;
            }

            if (_rewardRoot != null)
                _initialRewardRootLocalPosition = _rewardRoot.localPosition;

            StopParticle(_treasureOpenParticle);
        }

        public async UniTask PlayOpenAnimationAsync(CancellationToken cancellationToken)
        {
            int initialStateHash = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

            _animator.ResetTrigger(OpenTrigger);
            _animator.SetTrigger(OpenTrigger);

            await UniTask.WaitUntil(() =>
            {
                if (_animator.IsInTransition(0))
                    return false;

                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                return state.fullPathHash != initialStateHash && state.normalizedTime >= 1f;
            }, cancellationToken: cancellationToken);
        }

        public void SetRarity(RelicRarity rarity)
        {
            Color color = RelicRarityPalette.GetColor(rarity);
            SetParticleColor(_treasureRaysParticles, color);
            SetParticleColor(_treasureOpenParticle, color);
        }

        public void Begin(float previewRiseSpeed)
        {
            ResetShakeTarget();
            StartPreviewRise(previewRiseSpeed);
            PlayParticles(_treasureRaysParticles, true);
        }

        public void PulsePreview(Transform preview)
        {
            if (preview == null)
                return;

            _ = preview.DOPunchScale(preview.localScale * 0.16f, 0.12f, 3, 0.45f)
                .SetLink(_owner);
        }

        public void UpgradeRarity(RelicRarity rarity, float pumpDuration, float pumpStrength)
        {
            SetRarity(rarity);
            PlayParticles(_treasureRaysParticles, true);

            if (_shakeTarget == null)
                return;

            _shakeTarget.localScale = _initialLocalScale;
            _ = _shakeTarget.DOPunchScale(_initialLocalScale * Mathf.Max(0f, pumpStrength),
                    Mathf.Max(0.01f, pumpDuration), 7, 0.65f)
                .SetEase(Ease.OutQuad)
                .SetLink(_owner);
        }

        public void Reveal(Transform preview)
        {
            StopPreviewRise(false);
            ResetShakeTarget();
            PlayParticle(_treasureOpenParticle);

            if (preview != null)
            {
                _ = preview.DOPunchScale(preview.localScale * 0.35f, 0.32f, 5, 0.6f)
                    .SetLink(_owner);
            }
        }

        public void CompleteReward()
        {
            _animator.enabled = false;
            _coinSilverFountain.gameObject.SetActive(false);
        }

        public void End()
        {
            StopPreviewRise(true);
            ResetShakeTarget();
            StopParticles(_treasureRaysParticles);
            StopParticle(_treasureOpenParticle);
        }

        private void StartPreviewRise(float speed)
        {
            StopPreviewRise(true);

            if (_rewardRoot == null || speed <= 0f)
                return;

            float duration = PreviewRiseLoopDistance / speed;
            _previewRiseTween = _rewardRoot
                .DOLocalMoveY(_initialRewardRootLocalPosition.y + PreviewRiseLoopDistance, duration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(_owner);
        }

        private void StopPreviewRise(bool resetPosition)
        {
            _previewRiseTween?.Kill();
            _previewRiseTween = null;

            if (resetPosition && _rewardRoot != null)
                _rewardRoot.localPosition = _initialRewardRootLocalPosition;
        }

        private void ResetShakeTarget()
        {
            if (_shakeTarget == null)
                return;

            _shakeTarget.DOKill();
            _shakeTarget.localPosition = _initialLocalPosition;
            _shakeTarget.localRotation = _initialLocalRotation;
            _shakeTarget.localScale = _initialLocalScale;
        }

        private static void SetParticleColor(ParticleSystem[] particles, Color color)
        {
            if (particles == null)
                return;

            foreach (ParticleSystem particle in particles)
            {
                if (particle == null)
                    continue;

                ParticleSystem.MainModule main = particle.main;
                ParticleSystem.MinMaxGradient currentColor = main.startColor;
                Color minimumColor = color;
                Color maximumColor = color;
                minimumColor.a = currentColor.colorMin.a;
                maximumColor.a = currentColor.colorMax.a;
                main.startColor = new ParticleSystem.MinMaxGradient(minimumColor, maximumColor);
            }
        }

        private static void SetParticleColor(ParticleSystem particle, Color color)
        {
            if (particle == null)
                return;

            SetParticleColor(particle.GetComponentsInChildren<ParticleSystem>(true), color);
        }

        private static void PlayParticles(ParticleSystem[] particles, bool restart)
        {
            if (particles == null)
                return;

            foreach (ParticleSystem particle in particles)
            {
                if (particle == null)
                    continue;

                if (restart)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

                if (restart || particle.isPlaying == false)
                    particle.Play(false);
            }
        }

        private static void StopParticles(ParticleSystem[] particles)
        {
            if (particles == null)
                return;

            foreach (ParticleSystem particle in particles)
            {
                if (particle != null)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static void PlayParticle(ParticleSystem particle)
        {
            if (particle == null)
                return;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        private static void StopParticle(ParticleSystem particle)
        {
            if (particle != null)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
