public interface IPauseService
{
    void Register(PauseEntity pauseEntity);
    void HandlePause();
    void CancelPause();
}