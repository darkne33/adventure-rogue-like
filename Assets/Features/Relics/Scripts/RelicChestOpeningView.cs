using System;
using UnityEngine;

namespace Features.Relics.Scripts
{
    [Serializable]
    public sealed class RelicChestOpeningView
    {
        private static readonly int OpeningTrigger = Animator.StringToHash("ChestCameraOpening");
        private static readonly int ClaimTrigger = Animator.StringToHash("Claim");

        [SerializeField] private ParticleSystem[] _treasureVerticalRaysParticles;
        [SerializeField] private ParticleSystem _treasureOpenParticle;
        [SerializeField] private Transform _characterPosition;
        [SerializeField] private GameObject _camera;
        [SerializeField] private Animator _cameraAnimator;

        public Transform CharacterPosition => _characterPosition;
        public bool IsConfigured => _characterPosition != null && _camera != null &&
                                    _cameraAnimator != null && _treasureOpenParticle != null;

        public void Begin()
        {
            _camera.SetActive(true);
            _cameraAnimator.SetTrigger(OpeningTrigger);
        }

        public void BeginClaimCamera() =>
            _cameraAnimator.SetTrigger(ClaimTrigger);

        public void PlayTreasureOpen() =>
            _treasureOpenParticle.Play(true);

        public void End()
        {
            if (_camera != null)
                _camera.SetActive(false);
        }

        public void StopTreasureRays()
        {
            if (_treasureVerticalRaysParticles == null)
                return;

            foreach (ParticleSystem particle in _treasureVerticalRaysParticles)
            {
                if (particle != null)
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
