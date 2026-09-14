using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public abstract class BossAttackConfiguration : ScriptableObject
    {
        [field: SerializeField] public bool IsEnabled { get; private set; } = true;

        public abstract IBossAttack CreateAttack(BossFacade boss, CharacterFacade character);

        public virtual void DrawPreview(Vector3 origin, Quaternion rotation)
        {
        }
    }

    public interface IBossAttack
    {
        UniTask Execute(CancellationToken cancellationToken, bool animateBoss = true,
            Func<bool> ownsAnimation = null);
    }
}
