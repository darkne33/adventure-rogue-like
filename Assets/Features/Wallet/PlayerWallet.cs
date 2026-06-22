public class PlayerWallet
{
    public Currency Gold { get; } = new();
    public Currency Silver { get; } = new();

    public Currency Money => Gold;
}
