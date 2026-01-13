using UnityEngine;

[CreateAssetMenu(menuName = "Create CharacterSettingsConfiguration", fileName = "Configs/Character/CharacterSettingsConfiguration", order = 0)]
public class CharacterSettingsConfiguration : ScriptableObject
{
    [Header("Stats Settings")]
    [field:SerializeField] public int StartHealth { get; private set; } 
    
    [field: Header("Movement Settings")]
    [field: SerializeField] public float MoveSpeed { get; private set; }
    [field: SerializeField] public float RotationSpeed { get; private set; }
    [field: SerializeField]  public float Acceleration { get; private set; }
    [field: SerializeField]  public float Deceleration { get; private set; }
    [field: SerializeField]  public float JumpForce { get; private set; }
    [field: SerializeField]  public float GravityMultiplier { get; private set; }
}