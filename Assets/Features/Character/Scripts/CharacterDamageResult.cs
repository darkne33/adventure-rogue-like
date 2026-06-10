public readonly struct CharacterDamageResult
{
    public int Damage { get; }
    public bool IsCritical { get; }

    public CharacterDamageResult(int damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }
}
