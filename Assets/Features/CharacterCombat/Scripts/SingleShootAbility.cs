using CustomPackages.Package.Extensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Enemies.Scripts;
using UI;
using UnityEngine;

public class SingleShootAbility : CharacterActiveAbility
{
    private int _damage;

    private ShootableAbilityConfiguration _abilityConfig;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IPanelService _panelService;
    private readonly CharacterWallet _characterWallet;

    private CharacterPanel _characterPanel;

    public SingleShootAbility(IEnemiesProvider enemiesProvider, IPanelService panelService,
        CharacterWallet characterWallet)
    {
        _enemiesProvider = enemiesProvider;
        _panelService = panelService;
        _characterWallet = characterWallet;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _abilityConfig = (ShootableAbilityConfiguration)abilityConfig;
        _damage = _abilityConfig.StartDamage;
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
        playerCollisionDetector.OnCollisionEnter = enemyFacade => DamageDeal(shootObj, enemyFacade);
    }

    private void DamageDeal(GameObject shootObj, EnemyFacade enemyFacade)
    {
        enemyFacade.HealthSystem.GetDamage(_damage);

        _characterPanel.CharacterGoldView.ShowGold(1);
        _characterWallet.Money.Add(1);

        enemyFacade.EffectsSystem.DealDamage();
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
            
            DestroyExtensions.DestroyAfterDelay(explosion, 1, explosion.GetCancellationTokenOnDestroy()).Forget();
            DestroyExtensions.DestroyAfterDelay(muzzle, 1, explosion.GetCancellationTokenOnDestroy()).Forget();
        }
    }
}