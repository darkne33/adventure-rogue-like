using UnityEngine;

public class CharacterFxSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem _movementTrailFx;
    [SerializeField] private ParticleSystem _stepFx;
    [SerializeField] private ParticleSystem _jumpFx;
    [SerializeField] private ParticleSystem _dashFx;
    [SerializeField] private ParticleSystem _completedJumpFx;

    public void ActivateMovementTrail(bool state) =>
        _movementTrailFx.gameObject.SetActive(state);

    public void ActivateJump() =>
        _jumpFx.Play(true);

    public void ActivateDash() =>
        _dashFx.Play(true);

    public void ActivateStep() =>
        _stepFx.Play(true);

    public void CompletedJump() => 
        _completedJumpFx.Play(true);
}