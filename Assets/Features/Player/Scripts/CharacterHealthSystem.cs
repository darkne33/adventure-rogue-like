public class CharacterHealthSystem
{
    private int _maxHealth;
    private int _currentHealth;

    public void Initialize()
    {
        _maxHealth = 100;
        _currentHealth = _maxHealth;
    }

    public void GetDamage(int damage) => 
        _currentHealth -= damage;

    public void IncreaseCurrentHealth(int increase) => 
        _currentHealth += increase;
}