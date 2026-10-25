using Features.RunResults.Scripts;

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CharacterDeathSystem : IDeathSystem
{
    private readonly CharacterFacade _characterFacade;
    private readonly RunResultsController _runResultsController;
    private readonly RunResultsTracker _runResultsTracker;
    private bool _handled;

    public CharacterDeathSystem(CharacterFacade characterFacade, RunResultsController runResultsController,
        RunResultsTracker runResultsTracker)
    {
        _characterFacade = characterFacade;
        _runResultsController = runResultsController;
        _runResultsTracker = runResultsTracker;
    }

    public void HandleDeath()
    {
        if (_handled)
            return;

        _handled = true;
        // Preserve the original fatal-hit snapshot while delaying only the screen's appearance.
        _runResultsTracker.CaptureResults();
        PlayDeath().Forget();
    }

    private async UniTask PlayDeath()
    {
        string sceneName = _characterFacade.gameObject.scene.name;

        try
        {
            _characterFacade.PlayCombatSound(Features.Sounds.SoundId.PlayerDeath);
            float groundHeight = _characterFacade.GetDeathGroundHeight();
            _characterFacade.DisableAfterDeath();
            await _characterFacade.DeathPresentation.Play(groundHeight, _characterFacade.destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        if (_characterFacade != null && _characterFacade.gameObject.activeInHierarchy)
            _runResultsController.Show(sceneName);
    }
}
