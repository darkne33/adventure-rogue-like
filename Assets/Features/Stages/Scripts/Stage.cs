using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class Stage
{
    public abstract void Initialize();
    public abstract UniTask Play();
}

public class FightingStage : Stage
{
    public override void Initialize()
    {
        Debug.Log("Initializing Fighting Stage");
    }

    public override UniTask Play()
    {
        return UniTask.CompletedTask;
    }
}