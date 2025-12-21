using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ParticlePlayer : MonoBehaviour
    {
        [SerializeField] private List<ParticleSystem> _animatedParticles;

        public void PlayParticle(int index)
        {
            index = Mathf.Clamp(index, 0, _animatedParticles.Count - 1);
            _animatedParticles[index].Play();
        }

        public void PlayParticle() =>
            PlayParticle(0);
    }
}