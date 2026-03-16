using Infrastructure.SceneProvider;
using Unity.AI.Navigation;
using UnityEngine;

public class RogueLikeSceneProvider : GameSceneComponentsProvider
{
    [field: SerializeField] public Transform LevelSpawnPoint { get; private set; }
    [field: SerializeField] public NavMeshSurface NavMeshSurface { get; private set; }
    
    public LevelView CurrentLevel { get; set; }
}