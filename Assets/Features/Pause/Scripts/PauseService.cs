using System.Collections.Generic;

public class PauseService : IPauseService
{
    private List<PauseEntity> _pauseEntities = new List<PauseEntity>();

    public void Register(PauseEntity pauseEntity) => 
        _pauseEntities.Add(pauseEntity);

    public void HandlePause() => 
        SetupPauseEntity(true);

    public void CancelPause() => 
        SetupPauseEntity(false);

    private void SetupPauseEntity(bool state)
    {
        foreach (var pauseEntity in _pauseEntities) 
            pauseEntity.IsPauseEntity = state;
    }
}