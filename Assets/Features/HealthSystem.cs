public class HealthSystem
{
    private int _maxHealth;
    private int _currentHealth;
    
    private readonly int _startHealth;
    private readonly IHealthView[] _characterHealthViews;

    public HealthSystem(int startHealth, IHealthView[] characterHealthViews)
    {
        _startHealth = startHealth;
        _characterHealthViews = characterHealthViews;
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