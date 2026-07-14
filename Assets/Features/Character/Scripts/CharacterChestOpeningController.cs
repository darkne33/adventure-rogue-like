using System;
using UnityEngine;

internal sealed class CharacterChestOpeningController
{
    private readonly Transform _characterTransform;
    private readonly Transform _characterModel;
    private readonly Rigidbody _rigidbody;
    private readonly PauseEntity _pauseEntity;
    private readonly CharacterAnimationSystem _animationSystem;
    private readonly CharacterOutlineController _outlineController;

    public bool IsOpening { get; private set; }

    public CharacterChestOpeningController(Transform characterTransform, Transform characterModel,
        Renderer[] renderers, Rigidbody rigidbody, PauseEntity pauseEntity,
        CharacterAnimationSystem animationSystem)
    {
        _characterTransform = characterTransform;
        _characterModel = characterModel;
        _rigidbody = rigidbody;
        _pauseEntity = pauseEntity;
        _animationSystem = animationSystem;
        _outlineController = new CharacterOutlineController(renderers);
    }

    public bool TryBegin(bool isTransitionPaused, Action<bool> refreshControlLock)
    {
        if (IsOpening || isTransitionPaused || _rigidbody == null || _pauseEntity.IsPauseEntity)
            return false;

        bool wasControlLocked = isTransitionPaused || IsOpening;
        IsOpening = true;
        _pauseEntity.SetCinematicPaused(true);
        refreshControlLock(wasControlLocked);
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

    public void EndAnimation() =>
        _animationSystem.EndChestOpening();

    public void Finish(bool isTransitionPaused, Action<bool> refreshControlLock)
    {
        if (IsOpening == false)
            return;

        try
        {
            _animationSystem.FinishChestOpening();
        }
        finally
        {
            _outlineController.Restore();

            bool wasControlLocked = isTransitionPaused || IsOpening;
            IsOpening = false;
            _pauseEntity.SetCinematicPaused(false);
            refreshControlLock(wasControlLocked);
        }
    }
}
