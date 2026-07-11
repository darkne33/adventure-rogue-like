public class PauseEntity
{
    private bool _isPausedByPauseService;
    private bool _isPausedByTransition;

    public bool IsPauseEntity
    {
        get => _isPausedByPauseService || _isPausedByTransition;
        set => _isPausedByPauseService = value;
    }

    public PauseEntity(IPauseService pauseService) =>
        pauseService.Register(this);

    public void SetTransitionPaused(bool state) =>
        _isPausedByTransition = state;
}
