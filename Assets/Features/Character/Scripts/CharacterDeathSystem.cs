using Cysharp.Threading.Tasks;

public class CharacterDeathSystem : IDeathSystem
{
    private readonly CharacterFacade _characterFacade;
    private readonly RunRestartService _runRestartService;

    public CharacterDeathSystem(CharacterFacade characterFacade, RunRestartService runRestartService)
    {
        _characterFacade = characterFacade;
        _runRestartService = runRestartService;
    }

    public void HandleDeath()
    {
        string sceneName = _characterFacade.gameObject.scene.name;
        _characterFacade.DisableAfterDeath();
        _runRestartService.ReturnToMainMenu(sceneName).Forget();
    }
}
