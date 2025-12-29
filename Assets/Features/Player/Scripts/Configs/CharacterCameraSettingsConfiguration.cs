using UnityEngine;

[CreateAssetMenu(menuName = "Create CharacterCameraSettingsConfiguration",
    fileName = "Configs/Character/CharacterCameraSettingsConfiguration", order = 0)]
public class CharacterCameraSettingsConfiguration : ScriptableObject
{
    [Header("Camera Settings")]
    [field: SerializeField] public float DistanceToTarget { get; private set; } = 5f;

    [field: SerializeField] public float Height { get; private set; } = 2f;
    [field: SerializeField] public float MouseSensitivity { get; private set; } = 0.1f;
    
    [Header("Vertical Limits")] 
    [field: SerializeField] public float MinVerticalAngle { get; private set; } = -30f;
    [field: SerializeField] public float MaxVerticalAngle { get; private set; }= 50f;
}