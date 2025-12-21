using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CustomPackages.Package.Extensions.Animations
{
    public class AnimationMonoComponent : MonoBehaviour
    {
        public bool IsPlaying => _animation.isPlaying;

        [SerializeField] protected Animation _animation;

        protected async UniTask PlayAnimation(AnimationClip clip, CancellationToken cancellationToken)
        {
            _animation.clip = clip;
            _animation.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
        }
    }
}