using System.Collections.Generic;

public class PauseService : IPauseService
{
    private readonly List<PauseEntity> _pauseEntities = new List<PauseEntity>();
    private readonly ITimeScaleService _timeScaleService;
    private bool _isPaused;

    public PauseService(ITimeScaleService timeScaleService)
    {
        _timeScaleService = timeScaleService;
    }

    public void Register(PauseEntity pauseEntity) =>
        _pauseEntities.Add(pauseEntity);

    public void HandlePause()
    {
        if (_isPaused)
        {
            SetupPauseEntity(true);
            return;
        }

        _isPaused = true;
        SetupPauseEntity(true);
        _timeScaleService.SetPaused(true);
    }

    public void CancelPause()
    {
        if (_isPaused == false)
        {
            SetupPauseEntity(false);
            return;
        }

        _timeScaleService.SetPaused(false);
        _isPaused = false;
        SetupPauseEntity(false);
    }

    private void SetupPauseEntity(bool state)
    {
        foreach (PauseEntity pauseEntity in _pauseEntities)
            pauseEntity.IsPauseEntity = state;
    }
}
