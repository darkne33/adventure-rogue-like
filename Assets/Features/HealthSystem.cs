using System;

public class HealthSystem
{
    public event Action<float> DamageReceived;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsDead => _isDead;

    private float _maxHealth;
    private float _currentHealth;
    private bool _isDead;
    private float _pendingHealing;

    private readonly float _startHealth;
    private readonly IHealthView[] _characterHealthViews;
    private readonly IDamageView[] _damageViews;
    private readonly IDeathSystem _deathSystem;
    private readonly bool _isInvulnerable;
    private readonly float _healthIncrement;
    private readonly float _maximumHealthLimit;

    public HealthSystem(float startHealth, IHealthView[] characterHealthViews, IDeathSystem deathSystem,
        IDamageView[] damageViews = null, bool isInvulnerable = false,
        float healthIncrement = 0f, float maximumHealthLimit = float.PositiveInfinity)
    {
        _startHealth = startHealth;
        _characterHealthViews = characterHealthViews;
        _deathSystem = deathSystem;
        _damageViews = damageViews;
        _isInvulnerable = isInvulnerable;
        _healthIncrement = Math.Max(0f, healthIncrement);
        _maximumHealthLimit = maximumHealthLimit;
    }

    public void Initialize()
    {
        _maxHealth = NormalizeMaximumHealth(_startHealth);
        _currentHealth = _maxHealth;
        _isDead = false;
        _pendingHealing = 0f;

        UpdateViews();
    }

    public int GetDamage(int damage, bool isCritical = false, UnityEngine.Color? damageColor = null) =>
        (int)Math.Ceiling(GetDamage((float)damage, isCritical, damageColor));

    public float GetDamage(float damage, bool isCritical = false, UnityEngine.Color? damageColor = null)
    {
        if (_isInvulnerable || _isDead || damage <= 0)
            return 0;

        if (_healthIncrement > 0f)
            damage = (float)Math.Ceiling(damage / _healthIncrement - 0.0001f) * _healthIncrement;

        float appliedDamage = _healthIncrement > 0f
            ? Math.Min(_currentHealth, damage)
            : (float)Math.Ceiling(Math.Min(_currentHealth, damage));
        _currentHealth = Math.Max(0f, _currentHealth - appliedDamage);
        _pendingHealing = 0f;
        UpdateViews();
        UpdateDamageViews(appliedDamage, isCritical, damageColor);

        // Publish the real health loss before death ends and saves the current run.
        DamageReceived?.Invoke(appliedDamage);

        if (_currentHealth > 0)
            return appliedDamage;

        _isDead = true;
        _deathSystem.HandleDeath();
        return appliedDamage;
    }

    public float IncreaseCurrentHealth(float increase)
    {
        if (_isDead || increase <= 0f || _currentHealth >= _maxHealth)
            return 0f;

        float previousHealth = _currentHealth;
        if (_healthIncrement > 0f)
        {
            _pendingHealing += increase;
            increase = (float)Math.Floor((_pendingHealing + 0.00001f) / _healthIncrement) * _healthIncrement;
            if (increase <= 0f)
                return 0f;

            _pendingHealing = Math.Max(0f, _pendingHealing - increase);
        }

        _currentHealth = Math.Min(_maxHealth, _currentHealth + increase);
        if (_currentHealth >= _maxHealth)
            _pendingHealing = 0f;
        UpdateViews();
        return _currentHealth - previousHealth;
    }

    public void SetCurrentHealth(float health)
    {
        if (_isDead)
            return;

        _currentHealth = Math.Clamp(NormalizeHealth(health), 0f, _maxHealth);
        _pendingHealing = 0f;
        UpdateViews();
        if (_currentHealth <= 0f)
        {
            _isDead = true;
            _deathSystem.HandleDeath();
        }
    }

    public void SetMaxHealth(float maxHealth, bool healIncrease = true)
    {
        maxHealth = NormalizeMaximumHealth(maxHealth);
        if (Math.Abs(_maxHealth - maxHealth) < 0.001f)
            return;

        float difference = maxHealth - _maxHealth;
        _maxHealth = maxHealth;

        if (healIncrease && difference > 0f)
            _currentHealth += difference;

        _currentHealth = Math.Min(_currentHealth, _maxHealth);
        if (_currentHealth >= _maxHealth)
            _pendingHealing = 0f;
        UpdateViews();
    }

    private float NormalizeMaximumHealth(float health) =>
        Math.Clamp(NormalizeHealth(health), _healthIncrement > 0f ? _healthIncrement : 1f,
            _maximumHealthLimit);

    private float NormalizeHealth(float health) => _healthIncrement > 0f
        ? (float)Math.Round(health / _healthIncrement, MidpointRounding.AwayFromZero) * _healthIncrement
        : health;

    private void UpdateViews()
    {
        foreach (var healthView in _characterHealthViews)
            healthView.UpdateHealth(_currentHealth, _maxHealth);
    }

    private void UpdateDamageViews(float damage, bool isCritical, UnityEngine.Color? damageColor)
    {
        if (_damageViews == null)
            return;

        foreach (var damageView in _damageViews)
            damageView.ShowDamage(damage, _maxHealth, isCritical, damageColor);
    }
}
