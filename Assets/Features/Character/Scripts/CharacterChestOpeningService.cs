using System;
using UnityEngine;

public sealed class CharacterChestOpeningService
{
    private CharacterFacade _character;
    private Transform _characterTransform;
    private Transform _characterModel;
    private Rigidbody _rigidbody;
    private PauseEntity _pauseEntity;
    private CharacterAnimationSystem _animationSystem;
    private CharacterOutlineController _outlineController;

    public bool IsOpening { get; private set; }

    public void Initialize(CharacterFacade character, Rigidbody rigidbody, PauseEntity pauseEntity,
        CharacterAnimationSystem animationSystem)
    {
        if (character == null)
            throw new ArgumentNullException(nameof(character));

        if (rigidbody == null)
            throw new ArgumentNullException(nameof(rigidbody));

        _character = character;
        _characterTransform = character.transform;
        _characterModel = character.CharacterModel.transform;
        _rigidbody = rigidbody;
        _pauseEntity = pauseEntity ?? throw new ArgumentNullException(nameof(pauseEntity));
        _animationSystem = animationSystem ?? throw new ArgumentNullException(nameof(animationSystem));
        _outlineController = new CharacterOutlineController(character.Outline);
    }

    public bool TryBegin()
    {
        if (IsOpening || _character == null || _rigidbody == null ||
            _character.IsTransitionPaused || _pauseEntity.IsPauseEntity)
            return false;

        _character.SetCinematicPaused(true);
        IsOpening = true;
        _outlineController.Hide();

        return true;
    }

    public void Prepare(Transform target)
    {
        if (IsOpening == false || target == null)
            throw new InvalidOperationException("Chest opening must be started before it is prepared.");

        Vector3 targetPosition = target.position;
        _rigidbody.position = targetPosition;
        _characterTransform.position = targetPosition;
        _characterModel.localRotation = Quaternion.identity;

        Physics.SyncTransforms();
    }

    public void StartAnimation() =>
        _animationSystem.StartChestOpening();

    public void FinishAnimation() =>
        _animationSystem.FinishChestOpeningAnimation();

    public void EndAnimation() =>
        _animationSystem.EndChestOpening();

    public void Finish()
    {
        if (IsOpening == false)
            return;

        try
        {
            _animationSystem.ResetChestOpening();
        }
        finally
        {
            _outlineController.Restore();
            IsOpening = false;

            if (_character != null)
                _character.SetCinematicPaused(false);
            else
                _pauseEntity.SetCinematicPaused(false);
        }
    }
}
