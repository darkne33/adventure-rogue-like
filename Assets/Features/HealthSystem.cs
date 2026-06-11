using System;

public class HealthSystem
{
    private float _maxHealth;
    private float _currentHealth;
    private bool _isDead;

    private readonly float _startHealth;
    private readonly IHealthView[] _characterHealthViews;
    private readonly IDamageView[] _damageViews;
    private readonly IDeathSystem _deathSystem;

    public HealthSystem(float startHealth, IHealthView[] characterHealthViews, IDeathSystem deathSystem,
        IDamageView[] damageViews = null)
    {
        _startHealth = startHealth;
        _characterHealthViews = characterHealthViews;
        _deathSystem = deathSystem;
        _damageViews = damageViews;
    }

    public void Initialize()
    {
        _maxHealth = _startHealth;
        _currentHealth = _maxHealth;
        _isDead = false;

        UpdateViews();
    }

    public void GetDamage(int damage, bool isCritical = false)
    {
        if (_isDead || damage <= 0)
            return;

        _currentHealth = Math.Max(0f, _currentHealth - damage);
        UpdateViews();
        UpdateDamageViews(damage, isCritical);

        if (_currentHealth > 0)
            return;

        _isDead = true;
        _deathSystem.HandleDeath();
    }

    public void IncreaseCurrentHealth(int increase)
    {
        _currentHealth += increase;
        UpdateViews();
    }

    private void UpdateViews()
    {
        foreach (var healthView in _characterHealthViews)
            healthView.UpdateHealth(_currentHealth, _maxHealth);
    }

    private void UpdateDamageViews(int damage, bool isCritical)
    {
        if (_damageViews == null)
            return;

        foreach (var damageView in _damageViews)
            damageView.ShowDamage(damage, _maxHealth, isCritical);
    }
}
