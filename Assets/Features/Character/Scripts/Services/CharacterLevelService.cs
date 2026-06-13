using System;

public class CharacterLevelService : ICharacterLevelService
{
    public event Action<int, int> OnUpdateAddExpView;
    public event Action<int> OnLevelUp;
    
    public int GetCurrentExp => _characterExpData.CurrentExp;
    public int GetMaxExp => _characterExpData.MaxExp;
    public int GetLevel => _characterExpData.Level;

    private readonly CharacterExpConfig _characterExpConfig;
    
    private CharacterExpData _characterExpData;

    public CharacterLevelService(CharacterExpConfig characterExpConfig)
    {
        _characterExpConfig = characterExpConfig;

        _characterExpData = new CharacterExpData
        {
            CurrentExp = 0,
            Level = 1,
            MaxExp = characterExpConfig.GetMaxExpByLevel(1)
        };
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        _characterExpData.CurrentExp += amount;

        while (_characterExpData.CurrentExp >= _characterExpData.MaxExp &&
               _characterExpData.Level < _characterExpConfig.MaxLevel)
        {
            _characterExpData.CurrentExp -= _characterExpData.MaxExp;
            _characterExpData.Level++;
            _characterExpData.MaxExp = _characterExpConfig.GetMaxExpByLevel(_characterExpData.Level);
            OnLevelUp?.Invoke(_characterExpData.Level);
        }

        if (_characterExpData.Level >= _characterExpConfig.MaxLevel)
            _characterExpData.CurrentExp =
                Math.Min(_characterExpData.CurrentExp, _characterExpData.MaxExp);

        OnUpdateAddExpView?.Invoke(_characterExpData.CurrentExp, _characterExpData.MaxExp);
    }
}

[Serializable]
public struct CharacterExpData
{
    public int Level;
    public int CurrentExp;
    public int MaxExp;
    
    public CharacterExpData(int level, int currentExp, int maxExp)
    {
        Level = level;
        CurrentExp = currentExp;
        MaxExp = maxExp;
    }
}
