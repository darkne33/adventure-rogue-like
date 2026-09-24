using System.Collections.Generic;
using System.Linq;
using Core.Services;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class AquariusRelicRuntime
    {
        private readonly IReadOnlyList<RelicRuntimeState> _activeRelics;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly CharacterStats _characterStats;
        private readonly IRelicVisualEffectService _visualEffectService;
        private readonly RelicCombatService _combat;
        private readonly RelicEffectCollection<WaterPatch> _waterPatches = new();
        private readonly RaycastHit[] _groundHits = new RaycastHit[32];

        public AquariusRelicRuntime(IReadOnlyList<RelicRuntimeState> relics, IEnemiesProvider enemies,
            CharacterStats stats, IRelicVisualEffectService visuals, RelicCombatService combat)
        {
            _activeRelics = relics;
            _enemiesProvider = enemies;
            _characterStats = stats;
            _visualEffectService = visuals;
            _combat = combat;
        }

        public void Remove(RelicRuntimeState state) => _waterPatches.RemoveOwner(state);
        public void Clear() => _waterPatches.Clear();

        public void OnMoveDistance(RelicMoveDistanceEvent moveEvent)
        {
            if (moveEvent.Character == null || _enemiesProvider.Count == 0)
                return;
            foreach (RelicRuntimeState state in _activeRelics)
            {
                RelicEffectDefinition effect = RelicBuildEffects.FindActive(state, RelicBuildEffects.Aquarius);
                if (effect == null || !TryGetGround(moveEvent.Character, out Vector3 position))
                    continue;

                int patchCap = Mathf.Max(1, Mathf.RoundToInt(effect.Cap));
                while (_waterPatches.Count(patch => patch.State == state) >= patchCap)
                {
                    int index = _waterPatches.FindIndex(patch => patch.State == state);
                    _waterPatches.RemoveAt(index);
                }
                _waterPatches.Add(new WaterPatch
                {
                    State = state,
                    Effect = effect,
                    Position = position,
                    ExpiresAt = Time.time + Mathf.Max(0.1f, effect.Duration),
                    NextTickAt = Time.time,
                    Visual = _visualEffectService.BeginPersistent(EffectName.RelicAquariusTrail,
                        position, Mathf.Max(0.1f, effect.Radius))
                });
            }
        }

        private bool TryGetGround(CharacterFacade character, out Vector3 position)
        {
            position = default;
            float nearest = float.MaxValue;
            int count = Physics.RaycastNonAlloc(character.transform.position + Vector3.up * 0.5f,
                Vector3.down, _groundHits, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _groundHits[i];
                if (hit.distance >= nearest || hit.normal.y < 0.5f ||
                    hit.collider.GetComponentInParent<CharacterFacade>() != null ||
                    hit.collider.GetComponentInParent<CombatTarget>() != null)
                    continue;
                nearest = hit.distance;
                position = hit.point + Vector3.up * 0.025f;
            }
            return nearest < float.MaxValue;
        }

        public void Tick(CharacterFacade character)
        {
            int version = _waterPatches.Version;
            for (int i = _waterPatches.Count - 1; i >= 0; i--)
            {
                WaterPatch patch = _waterPatches[i];
                if (Time.time >= patch.ExpiresAt)
                {
                    _waterPatches.RemoveAt(i);
                    continue;
                }
                if (Time.time < patch.NextTickAt)
                    continue;
                patch.NextTickAt = Time.time + Mathf.Max(0.05f, patch.Effect.BossValue /
                    Mathf.Max(0.01f, _characterStats.RelicAttackSpeedMultiplier));
                float radiusSquared = patch.Effect.Radius * patch.Effect.Radius;
                foreach (CombatTarget target in _enemiesProvider.ActiveEnemies.ToArray())
                {
                    if (target == null || target.IsDead)
                        continue;
                    Vector3 delta = target.transform.position - patch.Position;
                    if (Mathf.Abs(delta.y) > 1.5f)
                        continue;
                    delta.y = 0f;
                    if (delta.sqrMagnitude > radiusSquared)
                        continue;
                    // Water uses distance from the owner and never rolls on-hit effects.
                    _combat.DealPeriodicDamage(character, target, patch.Effect.Value * patch.State.StackCount,
                        Vector3.Distance(character.transform.position, target.transform.position),
                        RelicBuildEffects.Aquarius);
                    if (version != _waterPatches.Version)
                        return;
                }
            }
        }

        private sealed class WaterPatch : RelicVisualInstance
        {
            public float NextTickAt;
            public float ExpiresAt;
        }
    }
}
