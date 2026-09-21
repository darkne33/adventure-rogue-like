using DG.Tweening;
using Features.Enemies.Scripts;
using UnityEngine;

namespace Features.Bosses.Scripts
{
    public sealed class BossDeathSystem : IDeathSystem
    {
        private readonly BossFacade _boss;
        private readonly IEnemiesProvider _targets;
        private readonly CharacterFacade _character;
        private readonly CharacterStats _stats;
        private readonly GoldDropper _goldDropper;
        private readonly ExpDropper _expDropper;

        public BossDeathSystem(BossFacade boss, IEnemiesProvider targets, CharacterFacade character,
            CharacterStats stats, GoldDropper goldDropper, ExpDropper expDropper)
        {
            _boss = boss;
            _targets = targets;
            _character = character;
            _stats = stats;
            _goldDropper = goldDropper;
            _expDropper = expDropper;
        }

        public void HandleDeath()
        {
            _boss.StopCombat();
            foreach (Collider collider in _boss.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            _targets.RemoveEnemy(_boss);
            int experienceReward = _boss.ClaimExperienceReward();
            if (experienceReward > 0)
            {
                float scaledExp = experienceReward * (1f + Mathf.Max(0f, _stats.XPBonus) * 0.01f);
                int exp = Mathf.FloorToInt(scaledExp);
                if (Random.value < scaledExp - exp)
                    exp++;
                _expDropper.DropExp(_boss.transform.position, exp);
            }
            _goldDropper.DropGold(_boss.transform.position);
            if (_character != null && !_character.HealthSystem.IsDead)
                _character.HealthSystem.IncreaseCurrentHealth(Mathf.Max(0f, _stats.GainHp));

            Tween fade = _boss.Config.DeathFadeDuration > 0f
                ? _boss.EffectsSystem.PlayDeathFade(_boss.Config.DeathFadeDuration) : null;
            if (fade != null)
                fade.OnComplete(DestroyBoss);
            else
                DestroyBoss();
        }

        private void DestroyBoss()
        {
            if (_boss != null)
                Object.Destroy(_boss.gameObject);
        }
    }
}
