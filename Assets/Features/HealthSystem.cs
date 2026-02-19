using System;

public class HealthSystem
{
    private int _maxHealth;
    private int _currentHealth;

    private readonly int _startHealth;
    private readonly IHealthView[] _characterHealthViews;
    private readonly IDeathSystem _deathSystem;

    public HealthSystem(int startHealth, IHealthView[] characterHealthViews, IDeathSystem deathSystem)
    {
        _startHealth = startHealth;
        _characterHealthViews = characterHealthViews;
        _deathSystem = deathSystem;
    }

    public void Initialize()
    {
        _maxHealth = _startHealth;
        _currentHealth = _maxHealth;

        UpdateViews();
    }

    public void GetDamage(int damage)
    {
        _currentHealth -= damage;
        UpdateViews();

        if (_currentHealth <= 0)
        {
            _deathSystem.HandleDeath();
        }
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
}