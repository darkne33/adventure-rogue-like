using System;

public class Currency
{
    public event Action<int> CountChanged;

    public int Count { get; private set; }

    public void Add(int amount)
    {
        if (amount == 0)
            return;

        Count += amount;
        CountChanged?.Invoke(Count);
    }

    public void Remove(int amount) =>
        Add(-amount);

    public void Set(int amount)
    {
        if (Count == amount)
            return;

        Count = amount;
        CountChanged?.Invoke(Count);
    }

    public void Reset() =>
        Set(0);
}
