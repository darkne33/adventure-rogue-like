using Infrastructure.SceneProvider;
using UnityEngine;

public class RogueLikeSceneProvider : GameSceneComponentsProvider
{
    [field: SerializeField] public Transform CharacterSpawnPoint { get; private set; }
    [field: SerializeField] public Transform LevelSpawnPoint { get; private set; }
    
    public LevelView CurrentLevel { get; set; }
}