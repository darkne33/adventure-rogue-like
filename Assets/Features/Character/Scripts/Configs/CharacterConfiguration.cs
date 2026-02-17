using Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/PlayerConfiguration", fileName = "PlayerConfiguration", order = 0)]
public class CharacterConfiguration : ScriptableObject
{
    [field: SerializeField] public AddressableLoadContainerGameObject CharacterContainer  { get; private set; }
}