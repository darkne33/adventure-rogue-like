using System;

public class HealthSystem
{
    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsDead => _isDead;

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

    public int GetDamage(int damage, bool isCritical = false)
    {
        if (_isDead || damage <= 0)
            return 0;

        int appliedDamage = (int)Math.Ceiling(Math.Min(_currentHealth, damage));
        _currentHealth = Math.Max(0f, _currentHealth - appliedDamage);
        UpdateViews();
        UpdateDamageViews(appliedDamage, isCritical);

        if (_currentHealth > 0)
            return appliedDamage;

        _isDead = true;
        _deathSystem.HandleDeath();
        return appliedDamage;
    }

    public void IncreaseCurrentHealth(float increase)
    {
        if (_isDead || increase <= 0f || _currentHealth >= _maxHealth)
            return;

        _currentHealth = Math.Min(_maxHealth, _currentHealth + increase);
        UpdateViews();
    }

    public void SetMaxHealth(float maxHealth, bool healIncrease = true)
    {
        maxHealth = Math.Max(1f, maxHealth);
        if (Math.Abs(_maxHealth - maxHealth) < 0.001f)
            return;

        float difference = maxHealth - _maxHealth;
        _maxHealth = maxHealth;

        if (healIncrease && difference > 0f)
            _currentHealth += difference;

        _currentHealth = Math.Min(_currentHealth, _maxHealth);
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
