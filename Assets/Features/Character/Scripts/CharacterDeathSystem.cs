using Features.RunResults.Scripts;

public class CharacterDeathSystem : IDeathSystem
{
    private readonly CharacterFacade _characterFacade;
    private readonly RunResultsController _runResultsController;

    public CharacterDeathSystem(CharacterFacade characterFacade, RunResultsController runResultsController)
    {
        _characterFacade = characterFacade;
        _runResultsController = runResultsController;
    }

    public void HandleDeath()
    {
        string sceneName = _characterFacade.gameObject.scene.name;
        _runResultsController.Show(sceneName);
        _characterFacade.DisableAfterDeath();
    }
}
