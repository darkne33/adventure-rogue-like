using System;

public interface ICharacterLevelService
{
    public event Action<int, int> OnUpdateAddExpView;
    public event Action<int> OnExpAdded;
    public event Action<int> OnLevelUp;
    public int GetCurrentExp { get; }
    public int GetMaxExp { get; }
    public int GetLevel { get; }
    public void AddExp(int amount);
    public void Reset();
}
