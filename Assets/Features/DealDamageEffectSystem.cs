using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class DealDamageEffectSystem
    {
        private static readonly int HitBlend = Shader.PropertyToID("_HitPower");
        private static readonly int FadeAmount = Shader.PropertyToID("_FadeAmount");
        
        private readonly Material[] _materials;
        private Tweener _hitTweener;
        private Tweener _deathFadeTweener;

        public DealDamageEffectSystem(Renderer[] meshRenderers)
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

        public Tween PlayDeathFade(float duration)
        {
            _hitTweener?.Kill();
            _deathFadeTweener?.Kill();

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
    }
}
