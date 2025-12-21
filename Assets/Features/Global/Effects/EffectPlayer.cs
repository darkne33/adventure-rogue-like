using System;
using System.Collections;
using System.Linq;
using System.Threading;
using CustomPackages.Package.Extensions.ObjectPool;
using Cysharp.Threading.Tasks;
using Package.Logging.CustomPackages.Package.Logging.Runtime.Scripts.Core;
using UnityEngine;

namespace Core
{
    public class EffectPlayer : MonoBehaviour, IPoolable<EffectPlayer>
    {
        public event Action<EffectPlayer> EffectFinished;
        public event Action<EffectPlayer> UnexpectedDestroy;
        public ParticleSystem[] ParticleSystems => _particleSystems;

        [SerializeField] private ParticleSystem[] _particleSystems;

        private IGameObjectPool<EffectPlayer> _pool;

        public float SpeedDivider { get; set; }
        private float _mainSpeed;
        
        //private UIParticle _uiParticle;

        private void Awake()
        {
            var fxMain = ParticleSystems.First().main;
            _mainSpeed = fxMain.simulationSpeed;
          //  _uiParticle = GetComponent<UIParticle>();
        }

        public void Play()
        {
            ParticleSystem.MainModule fxMain = ParticleSystems.First().main;
            fxMain.simulationSpeed = _mainSpeed;
            ParticleSystems[0].Play(true);
            StartCoroutine(WaitForComplete());
        }

        public void PlayWithoutRelease()
        {
            ParticleSystems[0].Play(true);
        }

        public async UniTask PlayAsync()
        {
            var fxMain = ParticleSystems.First().main;
            fxMain.simulationSpeed = _mainSpeed;
            ParticleSystems[0].Play(true);
            await UniTask.WaitWhile(() => _particleSystems.Any(x => x.isPlaying),
                cancellationToken: this.GetCancellationTokenOnDestroy());
            EffectFinished?.Invoke(this);
            Release();
        }

        public async UniTask PlayAsync(CancellationToken token)
        {
            ParticleSystems[0].Play(true);
            var fxMain = ParticleSystems.First().main;
            fxMain.simulationSpeed = _mainSpeed;

            var destroyToken = this.GetCancellationTokenOnDestroy();
            try
            {
                await UniTask.WaitWhile(() => _particleSystems.Any(x => x.isPlaying),
                    cancellationToken: token).AttachExternalCancellation(destroyToken);
            }
            catch (Exception)
            {
                if (destroyToken.IsCancellationRequested)
                {
                    Log.Gameplay.Warn($"Interrupted cancel of effect {gameObject.name} on {transform.parent.name}");
                }

                if (token.IsCancellationRequested)
                {
                    Log.Gameplay.Debug($"Correct ending of effect {gameObject.name} on {transform.parent.name}");
                }
            }

            EffectFinished?.Invoke(this);
            Release();
        }

        private IEnumerator WaitForComplete()
        {
            int i = 0;
            while (i < ParticleSystems.Length)
            {
                ParticleSystem particle = ParticleSystems[i];
                if (!particle.isPlaying)
                {
                    i++;
                    continue;
                }

                yield return null;
            }

            EffectFinished?.Invoke(this);
            Release();
        }

        public void SetPool(IGameObjectPool<EffectPlayer> pool)
        {
            _pool = pool;
        }

        public void OnTake()
        {
        }

        public void Release()
        {
            StopPlay();
            _pool.Release(this);
            /*
            if (_uiParticle != null)
            {
                gameObject.transform.localScale = Vector3.one;
            }
            */
        }

        public void StopPlay()
        {
            if (_particleSystems[0] != null)
                ParticleSystems[0].Stop(true);
        }

        public void StopEmit()
        {
            if (_particleSystems[0] != null)
                ParticleSystems[0].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDestroy() => UnexpectedDestroy?.Invoke(this);

#if UNITY_EDITOR
        private void OnValidate() => _particleSystems = ParticleSystems ?? GetComponentsInChildren<ParticleSystem>();
#endif
    }
}