using System;
using DG.Tweening;
using UnityEngine;

namespace Features.Relics.Scripts
{
    [Serializable]
    public sealed class RelicChestRollView
    {
        private static readonly int OpenTrigger = Animator.StringToHash("Open");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private Transform _rewardRoot;
        [SerializeField] private ParticleSystem[] _treasureRaysParticles;
        [SerializeField] private ParticleSystem[] _treasureOpenParticles;

        private GameObject _owner;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;

        public Transform RewardRoot => _rewardRoot;
        public bool IsConfigured => _animator != null && _shakeTarget != null &&
                                    _rewardRoot != null;

        public void Initialize(GameObject owner)
        {
            _owner = owner;

            if (_shakeTarget == null)
                return;

            _initialLocalPosition = _shakeTarget.localPosition;
            _initialLocalRotation = _shakeTarget.localRotation;
            _initialLocalScale = _shakeTarget.localScale;
        }

        public void PlayOpenAnimation() =>
            _animator.SetTrigger(OpenTrigger);

        public void SetRarity(RelicRarity rarity)
        {
            Color color = RelicRarityPalette.GetColor(rarity);
            SetParticleColor(_treasureRaysParticles, color);
            SetParticleColor(_treasureOpenParticles, color);
        }

        public void Begin(float duration, float positionStrength, float rotationStrength, int vibrato)
        {
            ResetShakeTarget();

            if (_shakeTarget == null || duration <= 0f)
                return;

            _ = _shakeTarget.DOShakePosition(duration, positionStrength, vibrato)
                .SetEase(Ease.Linear)
                .SetLink(_owner);
            _ = _shakeTarget.DOShakeRotation(duration, rotationStrength, vibrato)
                .SetEase(Ease.Linear)
                .SetLink(_owner);

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
            PlayParticles(_treasureOpenParticles, true);

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
            ResetShakeTarget();
            PlayParticles(_treasureOpenParticles, true);

            if (preview != null)
            {
                _ = preview.DOPunchScale(preview.localScale * 0.35f, 0.32f, 5, 0.6f)
                    .SetLink(_owner);
            }
        }

        public void End()
        {
            ResetShakeTarget();
            StopParticles(_treasureRaysParticles);
            StopParticles(_treasureOpenParticles);
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
    }
}
