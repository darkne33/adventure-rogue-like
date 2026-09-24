using System.Threading;
using Core.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Relics.Scripts
{
    public interface IRelicProjectileContext
    {
        bool HasPiercingProjectiles { get; }
        int ProjectileRicochetCount { get; }
        int RoomSequence { get; }
        CancellationToken LifetimeToken { get; }
        void PlayRicochet(Vector3 position);
    }

    internal sealed class RelicProjectileContext : IRelicProjectileContext
    {
        private readonly RelicBuildModifiers _modifiers;
        private readonly IRelicVisualEffectService _visuals;

        public bool HasPiercingProjectiles => _modifiers.HasPiercingProjectiles;
        public int ProjectileRicochetCount => _modifiers.ProjectileRicochetCount;
        public int RoomSequence { get; private set; }
        public CancellationToken LifetimeToken { get; }

        public RelicProjectileContext(RelicBuildModifiers modifiers, IRelicVisualEffectService visuals,
            CancellationToken lifetimeToken)
        {
            _modifiers = modifiers;
            _visuals = visuals;
            LifetimeToken = lifetimeToken;
        }

        public void BeginRoom() => RoomSequence++;

        public void PlayRicochet(Vector3 position) =>
            _visuals.PlayImpact(EffectName.RelicRicochet, position, 0.35f, LifetimeToken).Forget();
    }
}
