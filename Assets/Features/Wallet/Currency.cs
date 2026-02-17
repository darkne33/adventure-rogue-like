public class Currency
{
    public int Count { get; private set; }

    public void Add(int amount)
    {
        Count += amount;
    }

    public void Remove(int amount) => 
        Count -= amount;
}