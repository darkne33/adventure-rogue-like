using System;

public class CharacterLevelService : ICharacterLevelService
{
    private const double ExperienceEpsilon = 0.000000001d;
    private const double LevelProgressEpsilon = 0.0000000001d;

    public event Action<int, int> OnUpdateAddExpView;
    public event Action<int> OnExpAdded;
    public event Action<int> OnLevelUp;
    
    public int GetCurrentExp => _characterExpData.CurrentExp;
    public int GetMaxExp => _characterExpData.MaxExp;
    public int GetLevel => _characterExpData.Level;
    public float GetLevelProgress => (float)(_currentExperience / _characterExpData.MaxExp);

    private readonly CharacterExpConfig _characterExpConfig;
    
    private CharacterExpData _characterExpData;
    private double _currentExperience;
    private double _pendingExperienceFeedback;

    public CharacterLevelService(CharacterExpConfig characterExpConfig)
    {
        _characterExpConfig = characterExpConfig;
        ResetData();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        _currentExperience += amount;

        while (_currentExperience + ExperienceEpsilon >= _characterExpData.MaxExp)
        {
            _currentExperience = Math.Max(0d, _currentExperience - _characterExpData.MaxExp);
            AdvanceLevel();
        }

        UpdateCurrentExperience();
        NotifyExperienceAdded(amount);
        OnUpdateAddExpView?.Invoke(_characterExpData.CurrentExp, _characterExpData.MaxExp);
    }

    public void AddLevelProgress(double levelProgress)
    {
        if (levelProgress <= 0d || double.IsNaN(levelProgress) || double.IsInfinity(levelProgress))
            return;

        double remainingProgress = levelProgress;
        double earnedExperience = 0d;

        while (remainingProgress > 0d)
        {
            double progressToNextLevel = 1d - _currentExperience / _characterExpData.MaxExp;
            if (remainingProgress + LevelProgressEpsilon < progressToNextLevel)
            {
                double experience = remainingProgress * _characterExpData.MaxExp;
                _currentExperience += experience;
                earnedExperience += experience;
                break;
            }

            earnedExperience += Math.Max(0d, _characterExpData.MaxExp - _currentExperience);
            remainingProgress = Math.Max(0d, remainingProgress - progressToNextLevel);
            _currentExperience = 0d;
            AdvanceLevel();
        }

        UpdateCurrentExperience();
        NotifyExperienceAdded(earnedExperience);
        OnUpdateAddExpView?.Invoke(_characterExpData.CurrentExp, _characterExpData.MaxExp);
    }

    public void Reset()
    {
        ResetData();
        OnUpdateAddExpView?.Invoke(_characterExpData.CurrentExp, _characterExpData.MaxExp);
    }

    private void ResetData()
    {
        _currentExperience = 0d;
        _pendingExperienceFeedback = 0d;
        _characterExpData = new CharacterExpData
        {
            CurrentExp = 0,
            Level = 1,
            MaxExp = _characterExpConfig.GetMaxExpByLevel(1)
        };
    }

    private void AdvanceLevel()
    {
        _characterExpData.Level++;
        _characterExpData.MaxExp = _characterExpConfig.GetMaxExpByLevel(_characterExpData.Level);
        UpdateCurrentExperience();
        OnLevelUp?.Invoke(_characterExpData.Level);
    }

    private void UpdateCurrentExperience() =>
        _characterExpData.CurrentExp = (int)Math.Min(int.MaxValue,
            Math.Floor(_currentExperience + ExperienceEpsilon));

    private void NotifyExperienceAdded(double experience)
    {
        _pendingExperienceFeedback += experience;

        while (_pendingExperienceFeedback + ExperienceEpsilon >= 1d)
        {
            int amount = (int)Math.Min(int.MaxValue,
                Math.Floor(_pendingExperienceFeedback + ExperienceEpsilon));
            _pendingExperienceFeedback = Math.Max(0d, _pendingExperienceFeedback - amount);
            OnExpAdded?.Invoke(amount);
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
