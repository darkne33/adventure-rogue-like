using Infrastructure.SceneProvider;
using UnityEngine;

public class RogueLikeSceneProvider : GameSceneComponentsProvider
{
    [field: SerializeField] public Transform CharacterSpawnPoint { get; private set; }
}