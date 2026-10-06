using UnityEngine;

using Features.Sounds;
using Zenject;

public class CharacterFxSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem _movementTrailFx;
    [SerializeField] private ParticleSystem _stepFx;
    [SerializeField] private ParticleSystem _jumpFx;
    [SerializeField] private ParticleSystem _dashFx;
    [SerializeField] private ParticleSystem _completedJumpFx;
    [SerializeField] private AudioClip[] _stepSounds = System.Array.Empty<AudioClip>();
    [SerializeField] private AudioClip _jumpSound;

    private ISoundsService _soundsService;
    private int _stepSoundIndex;

    [Inject]
    private void Construct(ISoundsService soundsService) =>
        _soundsService = soundsService;

    public void ActivateMovementTrail(bool state) =>
        _movementTrailFx.gameObject.SetActive(state);

    public void ActivateJump() =>
        _jumpFx.Play(true);

    public void ActivateJumpSound() =>
        _soundsService?.PlaySfx(_jumpSound);

    public void ActivateDash() =>
        _dashFx.Play(true);

    public void ActivateStep()
    {
        _stepFx.Play(true);

        if (_soundsService == null || _stepSounds == null || _stepSounds.Length == 0)
            return;

        _soundsService.PlaySfx(_stepSounds[_stepSoundIndex]);
        _stepSoundIndex = (_stepSoundIndex + 1) % _stepSounds.Length;
    }

    public void CompletedJump() => 
        _completedJumpFx.Play(true);
}
