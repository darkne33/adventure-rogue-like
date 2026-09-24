using System.Linq;
using System.Threading;
using Core.Services;
using Cysharp.Threading.Tasks;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class ExplosivoRelicRuntime
    {
        private const int MaximumPendingBombs = 64;
        private readonly RelicEffectCollection<StickyBomb> _stickyBombs = new();
        private readonly IRelicVisualEffectService _visualEffectService;
        private readonly RelicCombatService _combat;
        private readonly CancellationToken _lifetimeToken;

        public ExplosivoRelicRuntime(IRelicVisualEffectService visuals, RelicCombatService combat,
            CancellationToken lifetimeToken)
        {
            _visualEffectService = visuals;
            _combat = combat;
            _lifetimeToken = lifetimeToken;
        }

        public void Remove(RelicRuntimeState state) => _stickyBombs.RemoveOwner(state);
        public void Clear() => _stickyBombs.Clear();

        public void Attach(RelicRuntimeState state, RelicEffectDefinition effect,
            RelicHitEvent hitEvent)
        {
            if (_stickyBombs.Count >= MaximumPendingBombs || hitEvent.Target == null)
                return;
            int targetCap = Mathf.Max(1, Mathf.RoundToInt(effect.Cap));
            if (_stickyBombs.Count(bomb => bomb.Target == hitEvent.Target) >= targetCap)
                return;

            Vector3 position = RelicCombatService.GetTargetPosition(hitEvent.Target);
            _stickyBombs.Add(new StickyBomb
            {
                State = state,
                Effect = effect,
                Target = hitEvent.Target,
                Position = position,
                ExpiresAt = Time.time + Mathf.Max(0.1f, effect.Duration),
                Damage = Mathf.Max(1, Mathf.RoundToInt(effect.Value * state.StackCount +
                    hitEvent.Damage * Mathf.Max(0f, effect.BossValue))),
                Visual = _visualEffectService.BeginPersistent(EffectName.RelicExplosivoCharge,
                    position, 0.6f)
            });
        }

        public void Tick()
        {
            int version = _stickyBombs.Version;
            for (int i = _stickyBombs.Count - 1; i >= 0; i--)
            {
                StickyBomb bomb = _stickyBombs[i];
                if (bomb.Target != null && !bomb.Target.IsDead)
                    bomb.Position = RelicCombatService.GetTargetPosition(bomb.Target);
                if (bomb.Visual != null)
                {
                    bomb.Visual.transform.position = bomb.Position;
                    float remaining = Mathf.Max(0f, bomb.ExpiresAt - Time.time);
                    float progress = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.1f, bomb.Effect.Duration));
                    bomb.Visual.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.2f, progress);
                }
                if (Time.time < bomb.ExpiresAt)
                    continue;

                // Remove before publishing kills; callbacks may change the inventory.
                _stickyBombs.RemoveAt(i);
                _visualEffectService.PlayImpact(EffectName.RelicExplosivoExplosion, bomb.Position,
                    Mathf.Max(0.1f, bomb.Effect.Radius) * 0.5f, _lifetimeToken).Forget();
                _combat.DealAreaDamage(bomb.Position, Mathf.Max(0.1f, bomb.Effect.Radius),
                    bomb.Damage, RelicBuildEffects.Explosivo);
                if (version != _stickyBombs.Version)
                    return;
            }
        }

        private sealed class StickyBomb : RelicVisualInstance
        {
            public CombatTarget Target;
            public int Damage;
            public float ExpiresAt;
        }
    }
}
