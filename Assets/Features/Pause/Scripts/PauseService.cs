using System.Collections.Generic;
using UnityEngine;

public class PauseService : IPauseService
{
    private readonly List<PauseEntity> _pauseEntities = new List<PauseEntity>();
    private float _timeScaleBeforePause = 1f;
    private bool _isPaused;

    public void Register(PauseEntity pauseEntity) =>
        _pauseEntities.Add(pauseEntity);

    public void HandlePause()
    {
        if (_isPaused)
        {
            SetupPauseEntity(true);
            return;
        }

        _timeScaleBeforePause = Time.timeScale;
        _isPaused = true;
        SetupPauseEntity(true);
        Time.timeScale = 0f;
    }

    public void CancelPause()
    {
        if (_isPaused == false)
        {
            SetupPauseEntity(false);
            return;
        }

        Time.timeScale = _timeScaleBeforePause;
        _isPaused = false;
        SetupPauseEntity(false);
    }

    private void SetupPauseEntity(bool state)
    {
        foreach (PauseEntity pauseEntity in _pauseEntities)
            pauseEntity.IsPauseEntity = state;
    }
}
