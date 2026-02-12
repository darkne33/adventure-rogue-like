using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyEffectsSystem
    {
        private static readonly int HitBlend = Shader.PropertyToID("_HitBlend");
        
        private readonly Material[] _materials;
        private Tweener _hitTweener;

        public EnemyEffectsSystem(Renderer[] meshRenderers)
        {
            _materials = new Material[meshRenderers.Length];
            _materials = meshRenderers.Select(x  => x.material).ToArray();
        }

        public void DealDamage(float duration = 0.08f)
        {
            _hitTweener?.Kill();
            
            foreach (var mat in _materials)
            {
                if (mat != null) mat.SetFloat(HitBlend, 0f);
            }
            
            _hitTweener = DOTween.To(
                () => 0f,
                value => { foreach (var mat in _materials) if (mat != null) mat.SetFloat(HitBlend, value); },
                1f,
                duration
            ).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
        }
    }
}