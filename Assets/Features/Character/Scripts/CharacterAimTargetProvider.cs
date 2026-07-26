using Core;
using Features.Enemies.Scripts;
using Unity.Cinemachine;
using UnityEngine;

public sealed class CharacterAimTargetProvider : ICharacterAimTargetProvider
{
    private const float MaxRaycastDistance = 200f;
    private const int MaxRaycastHits = 64;

    private readonly ICameraService _cameraService;
    private readonly ICharacterProvider _characterProvider;
    private readonly IEnemiesProvider _enemiesProvider;
    private readonly RaycastHit[] _raycastHits = new RaycastHit[MaxRaycastHits];

    public float TargetingDistance => 100f;

    public CharacterAimTargetProvider(ICameraService cameraService, ICharacterProvider characterProvider,
        IEnemiesProvider enemiesProvider)
    {
        _cameraService = cameraService;
        _characterProvider = characterProvider;
        _enemiesProvider = enemiesProvider;
    }

    public EnemyFacade GetAimedEnemy()
    {
        CharacterFacade character = _characterProvider.CharacterFacade;
        Camera outputCamera = GetOutputCamera();
        if (character == null || outputCamera == null || outputCamera.isActiveAndEnabled == false)
            return null;

        Ray aimRay = outputCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        int hitCount = Physics.RaycastNonAlloc(aimRay, _raycastHits, MaxRaycastDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        SortHitsByDistance(hitCount);

        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = _raycastHits[index].collider;
            if (hitCollider == null)
                continue;

            if (hitCollider.transform.IsChildOf(character.transform))
                continue;

            EnemyFacade enemy = hitCollider.GetComponentInParent<EnemyFacade>();
            if (enemy == null)
                return null;

            return IsValidTarget(enemy, character) ? enemy : null;
        }

        return null;
    }

    private Camera GetOutputCamera()
    {
        CinemachineVirtualCameraBase mainCamera = _cameraService.MainCamera;
        if (mainCamera == null)
            return null;

        CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(mainCamera);
        return brain != null ? brain.OutputCamera : null;
    }

    private bool IsValidTarget(EnemyFacade enemy, CharacterFacade character)
    {
        if (enemy == null || enemy.gameObject.activeInHierarchy == false || enemy.IsDead)
            return false;

        float maxSqrDistance = TargetingDistance * TargetingDistance;
        if ((enemy.transform.position - character.transform.position).sqrMagnitude >= maxSqrDistance)
            return false;

        for (int index = 0; index < _enemiesProvider.ActiveEnemies.Count; index++)
        {
            if (_enemiesProvider.ActiveEnemies[index] == enemy)
                return true;
        }

        return false;
    }

    private void SortHitsByDistance(int hitCount)
    {
        for (int index = 1; index < hitCount; index++)
        {
            RaycastHit currentHit = _raycastHits[index];
            int insertionIndex = index - 1;

            while (insertionIndex >= 0 && _raycastHits[insertionIndex].distance > currentHit.distance)
            {
                _raycastHits[insertionIndex + 1] = _raycastHits[insertionIndex];
                insertionIndex--;
            }

            _raycastHits[insertionIndex + 1] = currentHit;
        }
    }
}
