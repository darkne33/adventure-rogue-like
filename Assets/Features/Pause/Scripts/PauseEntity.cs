public class PauseEntity
{
    public bool IsPauseEntity { get; set; }

    public PauseEntity(IPauseService pauseService) =>
        pauseService.Register(this);
}