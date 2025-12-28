using UnityEngine;

[CreateAssetMenu(menuName = "Create CharacterCameraSettingsConfiguration", fileName = "Configs/Character/CharacterCameraSettingsConfiguration", order = 0)]
public class CharacterCameraSettingsConfiguration : ScriptableObject
{
    [field: SerializeField] public Vector3 LocalOffset = new(2.5f, 4.4f, 5.4f);
    [field: SerializeField] public  Vector3 LocalRotation = new(25, -135, 0);

    [field: SerializeField] public float SmoothTime = 0.1f;
}