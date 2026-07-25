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
    [field: SerializeField]
    public float MinVerticalAngle { get; private set; } = -30f;

    [field: SerializeField] public float MaxVerticalAngle { get; private set; } = 50f;

    [field: SerializeField] public float CameraSmoothness { get; private set; } = 0.2f;

    [Header("Cinemachine Follow")]
    [field: SerializeField] public Vector3 FollowDamping { get; private set; } = new(0.14f, 0.26f, 0.18f);
    [field: SerializeField] public Vector3 FollowShoulderOffset { get; private set; } = new(0f, 0.18f, 0f);
    [field: SerializeField] public float FollowVerticalArmLength { get; private set; } = 0f;
    [field: SerializeField] public float FollowCameraDistance { get; private set; } = 14f;

    [Header("Landing Pivot Movement")]
    [field: SerializeField] public float LandingShakeDuration { get; private set; } = 0.26f;
    [field: SerializeField] public float LandingShakeStrength { get; private set; } = 0.32f;

    [Header("Damage Shake")]
    [field: SerializeField] public float DamageShakeDuration { get; private set; } = 0.2f;
    [field: SerializeField] public float DamageShakeStrength { get; private set; } = 0.12f;
    [field: SerializeField] public float DamageShakeRotationStrength { get; private set; } = 1.4f;
    [field: SerializeField] public float DamageShakeFrequency { get; private set; } = 30f;
}
