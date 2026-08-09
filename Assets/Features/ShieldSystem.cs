using UnityEngine;

public sealed class ShieldSystem
{
    private const float ShieldValueEpsilon = 0.001f;

    public int MaxShield => _maxShield + Mathf.CeilToInt(_temporaryShield);
    public float CurrentShield => _currentShield + _temporaryShield;

    private readonly float _regenerationDelay;
    private readonly float _regenerationPerSecond;
    private readonly IShieldView _shieldView;

    private int _maxShield;
    private float _currentShield;
    private float _temporaryShield;
    private float _timeSinceLastDamage;

    public ShieldSystem(float regenerationDelay, float regenerationPerSecond, IShieldView shieldView)
    {
        _regenerationDelay = Mathf.Max(0f, regenerationDelay);
        _regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
        _shieldView = shieldView;
    }

    public void Initialize(float maximumShield)
    {
        _maxShield = NormalizeMaximumShield(maximumShield);
        _currentShield = _maxShield;
        _temporaryShield = 0f;
        _timeSinceLastDamage = _regenerationDelay;
        UpdateView();
    }

    public int AbsorbDamage(int damage)
    {
        if (damage <= 0 || CurrentShield <= 0f)
            return 0;

        _timeSinceLastDamage = 0f;

        int availableShield = Mathf.FloorToInt(CurrentShield + ShieldValueEpsilon);
        int absorbedDamage = Mathf.Min(availableShield, damage);
        if (absorbedDamage <= 0)
            return 0;

        float temporaryAbsorption = Mathf.Min(_temporaryShield, absorbedDamage);
        _temporaryShield -= temporaryAbsorption;
        _currentShield = Mathf.Max(0f, _currentShield - (absorbedDamage - temporaryAbsorption));
        UpdateView();
        return absorbedDamage;
    }

    public float AddTemporaryShield(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (amount <= 0f)
            return 0f;

        _temporaryShield += amount;
        UpdateView();
        return amount;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || _maxShield <= 0 || _currentShield >= _maxShield ||
            _regenerationPerSecond <= 0f)
            return;

        float previousTimeSinceLastDamage = _timeSinceLastDamage;
        _timeSinceLastDamage += deltaTime;

        float regenerationTime = previousTimeSinceLastDamage >= _regenerationDelay
            ? deltaTime
            : Mathf.Max(0f, _timeSinceLastDamage - _regenerationDelay);
        if (regenerationTime <= 0f)
            return;

        _currentShield = Mathf.Min(_maxShield,
            _currentShield + _regenerationPerSecond * regenerationTime);
        UpdateView();
    }

    public void SetMaxShield(float maximumShield, bool restoreIncrease = true)
    {
        int normalizedMaximumShield = NormalizeMaximumShield(maximumShield);
        if (_maxShield == normalizedMaximumShield)
            return;

        int difference = normalizedMaximumShield - _maxShield;
        _maxShield = normalizedMaximumShield;

        if (restoreIncrease && difference > 0)
            _currentShield += difference;

        _currentShield = Mathf.Clamp(_currentShield, 0f, _maxShield);
        UpdateView();
    }

    private static int NormalizeMaximumShield(float maximumShield) =>
        Mathf.Max(0, Mathf.RoundToInt(maximumShield));

    private void UpdateView() =>
        _shieldView?.UpdateShield(CurrentShield, MaxShield);
}
