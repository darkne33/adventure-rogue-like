using System;
using System.Linq;
using Coffee.UIExtensions;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Effects
{
    public class FlyingElementsMonoComponent : MonoBehaviour
    {
        [SerializeField] private float _pumpScaleValue = 1.1f;
        [SerializeField] private UIParticleAttractor _attractor;
        [SerializeField] private UIParticle _uiParticle;

        private Sequence _sequence;
        private Vector3 _startScale;
        
        public async UniTask Play(Transform from, Transform to, Texture2D texture2D, long rewardValue,
            bool withoutPump = false)
        {
            var mainParticle = _uiParticle.particles.First();
            var renderer = mainParticle.GetComponent<ParticleSystemRenderer>();

            var materialInstance = renderer.material;
            materialInstance.mainTexture = texture2D;
            renderer.material = materialInstance;

            _uiParticle.transform.position = from.position;
            _attractor.transform.position = new Vector3(to.position.x, to.position.y, from.position.z);

            var rewardParticlesCount = (int)Mathf.Clamp(rewardValue, 1, 10);
            var emissionBlock = mainParticle.emission;
            var burst = emissionBlock.GetBurst(0);
            burst.cycleCount = rewardParticlesCount;
            mainParticle.emission.SetBurst(0, burst);

            var firstPump = new UniTaskCompletionSource();
            _startScale = to.localScale;
            _attractor.onAttracted.AddListener(() =>
            {
                firstPump.TrySetResult();
                if (withoutPump == false)
                    Pump(to);
            });

            _uiParticle.RefreshParticles();
            _uiParticle.Play();

            await firstPump.Task;

            DisposeLater().Forget();
        }

        private async UniTaskVoid DisposeLater()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
            _attractor.onAttracted.RemoveAllListeners();
            Destroy(gameObject);
        }

        private void Pump(Transform target)
        {
            if (target == null)
            {
                return;
            }

            _sequence?.Kill(true);
            _sequence = DOTween.Sequence().SetLink(target.gameObject).SetId("FlyingElementsMonoComponent");
            _sequence.Append(target.DOScale(_pumpScaleValue, 0.2f / 2));
            _sequence.Append(target.DOScale(_startScale, 0.2f / 2));
            _sequence.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            var targetEffect = target.GetComponent<ParticlePlayer>();
            if(targetEffect != null)
                targetEffect.PlayParticle();
        }
        
    }
}