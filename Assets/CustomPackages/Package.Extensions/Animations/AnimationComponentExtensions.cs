using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CustomPackages.Package.Extensions.Animations
{
    public static class AnimationComponentExtensions
    {
        public static void PlayAnimation(this Animation animation, AnimationClip clip)
        {
            animation.clip = clip;
            animation.Play();
        }

        public static UniTask PlayAsync(this Animation animation, CancellationToken token)
        {
            animation.Play();
            return UniTask.Delay(TimeSpan.FromSeconds(animation.clip.length), cancellationToken: token);
        }
    }
}