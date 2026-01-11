public class CharacterHealthSystem
{
    private int _maxHealth;
    private int _currentHealth;
    
    private readonly CharacterSettingsConfiguration _characterSettingsConfiguration;
    private readonly CharacterHealthView _characterHealthView;

    public CharacterHealthSystem(CharacterSettingsConfiguration characterSettingsConfiguration, CharacterHealthView characterHealthView)
    {
        _characterSettingsConfiguration = characterSettingsConfiguration;
        _characterHealthView = characterHealthView;
    }

    public void Initialize()
    {
        _maxHealth = _characterSettingsConfiguration.StartHealth;
        _currentHealth = _maxHealth;
        
        _characterHealthView.UpdateHealth(_currentHealth, _maxHealth);
    }

    public void GetDamage(int damage)
    {
        _currentHealth -= damage;
        _characterHealthView.UpdateHealth(_currentHealth, _maxHealth);
    }

    public void IncreaseCurrentHealth(int increase)
    {
        _currentHealth += increase;
        _characterHealthView.UpdateHealth(_currentHealth, _maxHealth);
    }
}