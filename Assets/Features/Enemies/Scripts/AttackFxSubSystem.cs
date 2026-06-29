using System;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class AttackFxSubSystem : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _fx;

        public void PlayFx() => 
            _fx.Play();
    }
}