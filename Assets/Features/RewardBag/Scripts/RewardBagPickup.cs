using System;
using DG.Tweening;
using UnityEngine;

namespace Features.RewardBag
{
    public sealed class RewardBagPickup : MonoBehaviour
    {
        private const float DropJumpPower = 1.1f;
        private const float DropDuration = 0.35f;
        private const float AttractionStartDelay = 0.2f;
        private const float AttractionRadius = 4f;
        private const float CollectDistance = 0.45f;
        private const float AttractionSpeed = 12f;
        private const float AttractionScaleMultiplier = 0.05f;
        private const float CollectionTargetHeight = 1f;

        private ICharacterProvider _characterProvider;
        private CharacterStats _characterStats;
        private Action _grantReward;
        private Action _collectedCallback;
        private float _attractionEnabledTime;
        private Vector3 _startScale;
        private bool _isCollecting;

        public void Construct(ICharacterProvider characterProvider, CharacterStats characterStats,
            Vector3 landPosition, Action grantReward, Action collectedCallback)
        {
            _characterProvider = characterProvider;
            _characterStats = characterStats;
            _grantReward = grantReward;
            _collectedCallback = collectedCallback;
            _attractionEnabledTime = Time.time + DropDuration + AttractionStartDelay;
            _startScale = transform.localScale;

            PlayDropAnimation(landPosition);
        }

        private void Update()
        {
            if (_isCollecting || Time.time < _attractionEnabledTime)
                return;

            Transform character = _characterProvider?.CharacterFacade != null
                ? _characterProvider.CharacterFacade.transform
                : null;
            if (character == null)
                return;

            Vector3 targetPosition = character.position + Vector3.up * CollectionTargetHeight;
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance > GetAttractionRadius())
                return;

            MoveToTarget(targetPosition);
            UpdateAttractionScale(distance);

            if (distance <= CollectDistance)
                Collect();
        }

        private void OnDestroy() =>
            transform.DOKill();

        private void PlayDropAnimation(Vector3 landPosition)
        {
            transform.DOKill();

            _ = transform.DOJump(landPosition, DropJumpPower, 1, DropDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
            _ = transform.DOPunchScale(Vector3.one * 0.2f, DropDuration, 4, 0.6f)
                .SetLink(gameObject);
        }

        private void MoveToTarget(Vector3 targetPosition)
        {
            float speedMultiplier = 1f + Mathf.Clamp01(
                Vector3.Distance(transform.position, targetPosition) /
                Mathf.Max(0.01f, GetAttractionRadius()));
            transform.position = Vector3.MoveTowards(transform.position, targetPosition,
                AttractionSpeed * speedMultiplier * Time.deltaTime);
        }

        private void UpdateAttractionScale(float distance)
        {
            float attractionRadius = Mathf.Max(CollectDistance, GetAttractionRadius());
            float progress = 1f - Mathf.InverseLerp(CollectDistance, attractionRadius, distance);
            float scaleMultiplier = Mathf.Lerp(1f, AttractionScaleMultiplier, progress);
            transform.localScale = _startScale * scaleMultiplier;
        }

        private void Collect()
        {
            if (_isCollecting)
                return;

            _isCollecting = true;
            transform.DOKill();

            Action grantReward = _grantReward;
            Action collectedCallback = _collectedCallback;
            _grantReward = null;
            _collectedCallback = null;

            grantReward?.Invoke();
            collectedCallback?.Invoke();
            Destroy(gameObject);
        }

        private float GetAttractionRadius()
        {
            float pickupRange = _characterStats != null
                ? Mathf.Max(0f, _characterStats.PickupRange)
                : 0f;
            return AttractionRadius * (1f + pickupRange * 0.01f);
        }
    }
}
