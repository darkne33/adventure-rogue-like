using System.Collections.Generic;
using System.Linq;
using Core.Services;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Relics.Scripts
{
    internal sealed class SacrificialDaggerRelicRuntime
    {
        private readonly IReadOnlyList<RelicRuntimeState> _activeRelics;
        private readonly IEnemiesProvider _enemiesProvider;
        private readonly CharacterStats _characterStats;
        private readonly IRelicVisualEffectService _visualEffectService;
        private readonly RelicCombatService _combat;
        private readonly RelicEffectCollection<OrbitingDagger> _orbitingDaggers = new();
        private readonly Collider[] _daggerContacts = new Collider[64];
        private float _daggerAngle;

        public SacrificialDaggerRelicRuntime(IReadOnlyList<RelicRuntimeState> relics, IEnemiesProvider enemies,
            CharacterStats stats, IRelicVisualEffectService visuals, RelicCombatService combat)
        {
            _activeRelics = relics;
            _enemiesProvider = enemies;
            _characterStats = stats;
            _visualEffectService = visuals;
            _combat = combat;
        }

        public void Remove(RelicRuntimeState state) => _orbitingDaggers.RemoveOwner(state);

        public void Clear()
        {
            _orbitingDaggers.Clear();
            _daggerAngle = 0f;
        }

        public void Tick(CharacterFacade character)
        {
            int version = _orbitingDaggers.Version;
            foreach (RelicRuntimeState state in _activeRelics)
            {
                RelicEffectDefinition effect = RelicBuildEffects.FindActive(state, RelicBuildEffects.SacrificialDagger);
                if (effect == null)
                    continue;
                while (_orbitingDaggers.Count(dagger => dagger.State == state) < state.StackCount)
                {
                    _orbitingDaggers.Add(new OrbitingDagger
                    {
                        State = state,
                        Effect = effect,
                        Visual = _visualEffectService.BeginPersistent(EffectName.RelicSacrificialDagger,
                            character.transform.position, 1f)
                    });
                }
            }
            if (_orbitingDaggers.Count == 0)
                return;

            float speedMultiplier = Mathf.Max(0.1f, 1f + _characterStats.AttackSpeed * 0.01f) *
                                    _characterStats.RelicAttackSpeedMultiplier;
            _daggerAngle = Mathf.Repeat(_daggerAngle +
                Mathf.Max(1f, _orbitingDaggers[0].Effect.Cap) * Time.deltaTime, 360f);
            for (int i = 0; i < _orbitingDaggers.Count; i++)
            {
                OrbitingDagger dagger = _orbitingDaggers[i];
                float angle = (_daggerAngle + i * 360f / _orbitingDaggers.Count) * Mathf.Deg2Rad;
                Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 position = character.transform.position + Vector3.up +
                                   radial * Mathf.Max(0.25f, dagger.Effect.Radius);
                Vector3 previous = dagger.HasPosition ? dagger.Position : position;
                if ((position - previous).sqrMagnitude > 25f)
                    previous = position;
                dagger.Position = position;
                dagger.HasPosition = true;
                if (dagger.Visual != null)
                    dagger.Visual.transform.SetPositionAndRotation(position,
                        Quaternion.LookRotation(new Vector3(-radial.z, 0f, radial.x)));

                float contactRadius = Mathf.Max(0.1f, dagger.Effect.BossValue);
                int contacts = Physics.OverlapCapsuleNonAlloc(previous, position, contactRadius,
                    _daggerContacts, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                for (int j = 0; j < contacts; j++)
                {
                    EnemyProjectile projectile = _daggerContacts[j].GetComponentInParent<EnemyProjectile>();
                    if (projectile != null)
                        UnityEngine.Object.Destroy(projectile.gameObject);
                }

                foreach (CombatTarget target in _enemiesProvider.ActiveEnemies.ToArray())
                {
                    if (target == null || target.IsDead ||
                        DistanceToSegmentSquared(RelicCombatService.GetTargetPosition(target), previous, position) >
                        (contactRadius + 0.4f) * (contactRadius + 0.4f))
                        continue;
                    if (dagger.NextHits.TryGetValue(target, out float readyAt) && Time.time < readyAt)
                        continue;
                    dagger.NextHits[target] = Time.time + Mathf.Max(0.05f,
                        dagger.Effect.Duration / Mathf.Max(0.01f, speedMultiplier));
                    _combat.DealDirectHit(character, target,
                        Mathf.Max(1, Mathf.RoundToInt(dagger.Effect.Value)), RelicBuildEffects.SacrificialDagger);
                    if (version != _orbitingDaggers.Version)
                        return;
                }
                foreach (CombatTarget expired in dagger.NextHits.Keys.Where(target =>
                             target == null || target.IsDead).ToArray())
                    dagger.NextHits.Remove(expired);
            }
        }

        private static float DistanceToSegmentSquared(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = to - from;
            float t = segment.sqrMagnitude > 0.0001f
                ? Mathf.Clamp01(Vector3.Dot(point - from, segment) / segment.sqrMagnitude) : 0f;
            return (point - (from + segment * t)).sqrMagnitude;
        }

        private sealed class OrbitingDagger : RelicVisualInstance
        {
            public bool HasPosition;
            public readonly Dictionary<CombatTarget, float> NextHits = new();
        }
    }
}
