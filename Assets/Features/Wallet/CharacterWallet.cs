public class CharacterWallet
{
    public Currency Gold { get; } = new();
    public Currency Silver { get; } = new();
    public Currency Keys { get; } = new();

    public Currency Money => Gold;
}
