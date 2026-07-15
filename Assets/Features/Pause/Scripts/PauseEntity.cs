public class PauseEntity
{
    private bool _isPausedByPauseService;
    private bool _isPausedByTransition;
    private bool _isPausedByCinematic;

    public bool IsPauseEntity
    {
        get => _isPausedByPauseService || _isPausedByTransition || _isPausedByCinematic;
        set => _isPausedByPauseService = value;
    }

    public bool IsCinematicPaused => _isPausedByCinematic;

    public PauseEntity(IPauseService pauseService) =>
        pauseService.Register(this);

    public void SetTransitionPaused(bool state) =>
        _isPausedByTransition = state;

    public void SetCinematicPaused(bool state) =>
        _isPausedByCinematic = state;
}
