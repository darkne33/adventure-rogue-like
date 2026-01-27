public class CharacterDoubleJumpAbility : CharacterPassiveAbility
{
    public override void OnEquip(CharacterFacade character)
    {
        //character.JumpCountMax += 1;
    }

    public override void OnUnequip(CharacterFacade character)
    {
        //character.JumpCountMax -= 1;
    }
}