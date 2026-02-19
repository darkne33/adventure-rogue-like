using DG.Tweening;
using Features.Enemies.Scripts;
using UI;
using UnityEngine;

public class FireballAbility : CharacterActiveAbility
{
    private int _damage;

    private FireballAbilityConfiguration _abilityConfig;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly IPanelService _panelService;
    private readonly CharacterWallet _characterWallet;
    
    private CharacterPanel _characterPanel;

    public FireballAbility(IEnemiesProvider enemiesProvider, IPanelService panelService, CharacterWallet characterWallet)
    {
        _enemiesProvider = enemiesProvider;
        _panelService = panelService;
        _characterWallet = characterWallet;
    }

    public override void Initialize(AbilityConfiguration abilityConfig)
    {
        base.Initialize(abilityConfig);
        _abilityConfig = (FireballAbilityConfiguration)abilityConfig;
        _damage = _abilityConfig.StartDamage;
        Cooldown = _abilityConfig.Cooldown;
        
        _characterPanel = (CharacterPanel)_panelService.GetPanel(PanelName.CharacterPanel);
    }

    protected override void OnUse(CharacterFacade character)
    {
        EnemyFacade randomEnemy = _enemiesProvider.GetRandomClosestEnemyByCharacter(character.transform, 100);
        if (randomEnemy == null)
            return;

        var randomEnemyPosition = randomEnemy.transform.position;

        var fireball = Object.Instantiate(_abilityConfig.Prefab, character.transform.position, Quaternion.identity);
        fireball.transform.rotation =
            Quaternion.LookRotation(fireball.transform.position - randomEnemyPosition);

        fireball.transform.DOMove(randomEnemyPosition, 50).SetSpeedBased().SetLink(fireball).SetId("Fireball Ability")
            .OnComplete(() =>
            {
                if (fireball != null)
                    Object.Destroy(fireball);
            });

        PlayerCollisionDetector playerCollisionDetector = fireball.GetComponent<PlayerCollisionDetector>();
        playerCollisionDetector.OnCollisionEnter = enemyFacade => DamageDeal(fireball, enemyFacade);
    }

    private void DamageDeal(GameObject fireball, EnemyFacade enemyFacade)
    {
        enemyFacade.HealthSystem.GetDamage(_damage);
        
        _characterPanel.CharacterGoldView.ShowGold(1);
        _characterWallet.Money.Add(1);

        enemyFacade.EffectsSystem.DealDamage();
        Object.Destroy(fireball);
    }
}