using UnityEngine;

public class CharacterFxSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem _movementTrailFx;
    [SerializeField] private ParticleSystem _jumpFx;
    [SerializeField] private ParticleSystem _dashFx;

    public void ActivateMovementTrail(bool state) => 
        _movementTrailFx.gameObject.SetActive(state);

    public void ActivateJump() =>
        _jumpFx.Play(true);
    
    public void ActivateDash() => 
        _dashFx.Play(true);
}