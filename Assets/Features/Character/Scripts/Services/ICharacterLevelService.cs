using System;

public interface ICharacterLevelService
{
    public event Action<int, int> OnUpdateAddExpView;
    public int GetCurrentExp { get; }
    public int GetMaxExp { get; }
    public void AddExp(int amount);
}