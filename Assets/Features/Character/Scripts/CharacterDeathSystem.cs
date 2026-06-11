public class CharacterDeathSystem : IDeathSystem
{
    private readonly CharacterFacade _characterFacade;

    public CharacterDeathSystem(CharacterFacade characterFacade) =>
        _characterFacade = characterFacade;

    public void HandleDeath() =>
        _characterFacade.DisableAfterDeath();
}
