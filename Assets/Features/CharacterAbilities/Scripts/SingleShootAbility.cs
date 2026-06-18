using CustomPackages.Package.Extensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using Features.Relics.Scripts;
using UI;
using UnityEngine;

public class SingleShootAbility : CharacterActiveAbility
{
    private int _damage;

    private ShootableAbilityConfiguration _abilityConfig;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IPanelService _panelService;
    private readonly CharacterWallet _characterWallet;
    private readonly CharacterDamageCalculator _damageCalculator;
    private readonly CharacterStats _characterStats;
    private readonly RelicEventBus _relicEventBus;
    private readonly RelicManager _relicManager;

    private CharacterPanel _characterPanel;

    public SingleShootAbility(IEnemiesProvider enemiesProvider, IPanelService panelService,
        CharacterWallet characterWallet, CharacterDamageCalculator damageCalculator, CharacterStats characterStats,
        RelicEventBus relicEventBus, RelicManager relicManager)
    {
        _enemiesProvider = enemiesProvider;
        _panelService = panelService;
        _characterWallet = characterWallet;
        _damageCalculator = damageCalculator;
        _characterStats = characterStats;
        _relicEventBus = relicEventBus;
        _relicManager = relicManager;
    }

    public override void OnEquip(CharacterStats characterStats)
    {
        base.OnEquip(characterStats);
        
        _damage += _abilityConfig.StartDamage;
        Stat_1 = _damage;
    }

    public override void OnUnequip(CharacterStats characterStats)
    {
        base.OnUnequip(characterStats);
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _abilityConfig = (ShootableAbilityConfiguration)abilityConfig;
       
        StatName_1 = "Damage";
        Cooldown = _abilityConfig.Cooldown;

        _characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
    }

    protected override void OnUse(CharacterFacade character)
    {
        EnemyFacade randomEnemy = _enemiesProvider.GetRandomClosestEnemyByCharacter(character.transform, 100);

        if (randomEnemy == null)
            return;

        var randomEnemyPosition = randomEnemy.TargetToShootDamage.position;

        var shootObj = Object.Instantiate(_abilityConfig.Prefab, character.transform.position, Quaternion.identity);
        shootObj.transform.rotation =
            Quaternion.LookRotation(shootObj.transform.position - randomEnemyPosition);

        shootObj.transform.DOMove(randomEnemyPosition, _abilityConfig.Speed).SetSpeedBased().SetLink(shootObj).SetId($"Shoot Ability {shootObj.name}")
            .OnComplete(() => DestroyShoot(shootObj));

        var playerCollisionDetector = shootObj.GetComponent<PlayerCollisionDetector>();
        playerCollisionDetector.OnCollisionEnter = enemyFacade => DamageDeal(character, shootObj, enemyFacade);
    }

    public override float GetStatFromIncrease() => 
        Stat_1;

    public override float GetStatToIncrease() => 
        GetStatFromIncrease() + _abilityConfig.StartDamage;

    private void DamageDeal(CharacterFacade character, GameObject shootObj, EnemyFacade enemyFacade)
    {
        CharacterDamageResult damageResult = _damageCalculator.Calculate(_damage);
        int finalDamage = _relicManager.ModifyOutgoingDamage(damageResult.Damage, enemyFacade);
        int appliedDamage = enemyFacade.HealthSystem.GetDamage(finalDamage, damageResult.IsCritical);
        bool killedByDirectHit = enemyFacade.HealthSystem.IsDead;

        if (appliedDamage > 0)
        {
            float lifeStealPercent = Mathf.Max(0f, _characterStats.LifeSteal) * 0.01f;
            float healed = character.HealthSystem.IncreaseCurrentHealth(appliedDamage * lifeStealPercent);
            if (healed > 0f)
                _relicEventBus.PublishHeal(new RelicHealEvent(character, healed));

            int goldReward = CalculateGoldReward(1);
            _characterPanel.CharacterGoldView.ShowGold(goldReward);
            _characterWallet.Money.Add(goldReward);

            enemyFacade.EffectsSystem.DealDamage();
            _relicEventBus.PublishHit(new RelicHitEvent(character, enemyFacade, appliedDamage,
                damageResult.IsCritical, _abilityConfig.AbilityName.ToString(), enemyFacade.transform.position));

            if (killedByDirectHit)
                _relicEventBus.PublishKill(new RelicKillEvent(character, enemyFacade,
                    enemyFacade.transform.position));
        }

        DestroyShoot(shootObj);
    }

    private void DestroyShoot(GameObject shootObj)
    {
        if (shootObj != null)
        {
            var explosion = Object.Instantiate(_abilityConfig.ExplosionPrefab, shootObj.transform.position,
                Quaternion.identity);
            
            var muzzle = Object.Instantiate(_abilityConfig.MuzzlePrefab, shootObj.transform.position,
                Quaternion.identity);
            
            Object.Destroy(shootObj);
            
            float effectDuration = AbilityDurationMultiplier;
            DestroyExtensions.DestroyAfterDelay(explosion, effectDuration,
                explosion.GetCancellationTokenOnDestroy()).Forget();
            DestroyExtensions.DestroyAfterDelay(muzzle, effectDuration,
                muzzle.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private int CalculateGoldReward(int baseReward)
    {
        float scaledReward = baseReward * (1f + Mathf.Max(0f, _characterStats.GainGold) * 0.01f);
        int reward = Mathf.FloorToInt(scaledReward);

        if (Random.value < scaledReward - reward)
            reward++;

        float luckChance = Mathf.Clamp(_characterStats.Luck, 0f, 100f) * 0.01f;
        if (Random.value < luckChance)
            reward += baseReward;

        return Mathf.Max(1, reward);
    }
}
