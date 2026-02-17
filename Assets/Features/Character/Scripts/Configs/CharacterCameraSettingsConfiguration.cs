using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/CharacterCameraSettingsConfiguration",
    fileName = "CharacterCameraSettingsConfiguration", order = 0)]
public class CharacterCameraSettingsConfiguration : ScriptableObject
{
    [Header("Camera Settings")]
    [field: SerializeField]
    public float DistanceToTarget { get; private set; } = 5f;

    [field: SerializeField] public float Height { get; private set; } = 2f;
    [field: SerializeField] public float MouseSensitivity { get; private set; } = 0.1f;
    [field: SerializeField] public float MinDistanceToTarget { get; private set; } = 0.7f;
    [field: SerializeField] public LayerMask CameraCollisionLayers { get; private set; } = Physics.AllLayers;

    [Header("Vertical Limits")]
    [field: SerializeField] public float MinVerticalAngle { get; private set; } = -30f;
    [field: SerializeField] public float MaxVerticalAngle { get; private set; } = 50f;
    
    [field: SerializeField]  public float CameraSmoothness { get; private set; } = 0.2f;
}