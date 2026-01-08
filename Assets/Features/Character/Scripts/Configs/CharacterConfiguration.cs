using Core;
using UnityEngine;

[CreateAssetMenu(menuName = "Create PlayerConfiguration", fileName = "Configs/Character/PlayerConfiguration", order = 0)]
public class CharacterConfiguration : ScriptableObject
{
    [field: SerializeField] public AddressableLoadContainerGameObject CharacterContainer  { get; private set; }
}