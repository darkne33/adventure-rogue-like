using System;

public interface ITimeScaleRequest : IDisposable
{
    float TimeScale { get; set; }
}

public interface ITimeScaleService
{
    bool IsPaused { get; }

    ITimeScaleRequest Request(float timeScale);
    void SetPaused(bool isPaused);
}
