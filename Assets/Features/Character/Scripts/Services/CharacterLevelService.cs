using System;

public class CharacterLevelService : ICharacterLevelService
{
    public event Action<int, int> OnUpdateAddExpView;
    
    public int GetCurrentExp => _characterExpData.CurrentExp;
    public int GetMaxExp => _characterExpData.MaxExp;

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
        _characterExpData.CurrentExp += amount;
        
        OnUpdateAddExpView?.Invoke(_characterExpData.CurrentExp, _characterExpData.MaxExp);
        
        if (_characterExpData.CurrentExp >= _characterExpData.MaxExp)
        {
            _characterExpData.CurrentExp = 0;
            _characterExpData.Level++;
            _characterExpData.MaxExp = _characterExpConfig.GetMaxExpByLevel(_characterExpData.Level);
        }
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