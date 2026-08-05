using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Relics.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.RewardBag
{
    public sealed class RewardBag : MonoBehaviour
    {
        private enum RewardType
        {
            Silver,
            Key
        }

        [Inject] private DiContainer _container;
        [InjectOptional] private ICameraService _cameraService;

        [SerializeField] private RelicChestInteractionView _interactionView = new();
        [SerializeField] private GameObject _silverRewardPrefab;
        [SerializeField] private GameObject _keyRewardPrefab;
        [SerializeField] private Transform _lootRayRoot;
        [SerializeField, Min(0f)] private float _interactDistance = 4f;
        [SerializeField, Min(1)] private int _rewardAmount = 1;
        [SerializeField, Min(0f)] private float _riseHeight = 3f;
        [SerializeField, Min(0.01f)] private float _riseDuration = 0.3f;
        [SerializeField, Min(0.01f)] private float _flyDuration = 0.45f;
        [SerializeField, Min(0f)] private float _targetHeight = 1.2f;
        [SerializeField, Min(0.01f)] private float _rewardScale = 2f;
        [SerializeField, Min(0f)] private float _bagDropHeight = 2f;
        [SerializeField, Min(0f)] private float _bagDropJumpPower = 0.8f;
        [SerializeField, Min(0.01f)] private float _bagDropDuration = 0.55f;

        private InputSystem_Actions _inputActions;
        private ICharacterProvider _characterProvider;
        private CharacterWallet _characterWallet;
        private Action _collectedCallback;
        private bool _isReady;
        private bool _isOpened;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _interactionView.Initialize(gameObject);
            _lootRayRoot?.gameObject.SetActive(false);
        }

        public void Construct(ICharacterProvider characterProvider, CharacterWallet characterWallet,
            Action collectedCallback)
        {
            _characterProvider = characterProvider;
            _characterWallet = characterWallet;
            _collectedCallback = collectedCallback;
            PlayBagDropAsync().Forget();
        }

        private void OnEnable()
        {
            _inputActions ??= new InputSystem_Actions();
            _inputActions.Player.Interact.Enable();
        }

        private void OnDisable()
        {
            _inputActions?.Player.Interact.Disable();
            _interactionView.SetAvailable(false, true);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void Update()
        {
            bool canInteract = CanInteract();
            _interactionView.SetAvailable(canInteract);

            if (canInteract && _inputActions != null &&
                _inputActions.Player.Interact.WasPressedThisFrame())
                Open();
        }

        private bool CanInteract()
        {
            if (_isReady == false || _isOpened || _characterProvider?.CharacterFacade == null ||
                _characterWallet == null)
                return false;

            return Vector3.Distance(transform.position,
                       _characterProvider.CharacterFacade.transform.position) <= _interactDistance;
        }

        private void Open()
        {
            if (_isOpened)
                return;

            _isOpened = true;
            _interactionView.SetAvailable(false);
            _lootRayRoot?.gameObject.SetActive(false);

            foreach (Collider bagCollider in GetComponentsInChildren<Collider>())
                bagCollider.enabled = false;

            OpenAsync().Forget();
        }

        private async UniTaskVoid OpenAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            GameObject rewardObject = null;

            try
            {
                RewardType rewardType = UnityEngine.Random.value < 0.5f
                    ? RewardType.Silver
                    : RewardType.Key;
                rewardObject = CreateRewardVisual(rewardType);
                _ = transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .SetLink(gameObject);

                if (rewardObject != null)
                {
                    await RiseReward(rewardObject.transform, cancellationToken);
                    await FlyRewardToCharacter(rewardObject.transform, cancellationToken);
                }

                GrantReward(rewardType);

                if (rewardObject != null)
                    await DismissReward(rewardObject.transform, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (rewardObject != null)
                    Destroy(rewardObject);

                if (this != null)
                    Destroy(gameObject);
            }
        }

        private GameObject CreateRewardVisual(RewardType rewardType)
        {
            GameObject rewardPrefab = rewardType == RewardType.Silver
                ? _silverRewardPrefab
                : _keyRewardPrefab;
            if (rewardPrefab == null)
                return null;

            GameObject rewardObject = _container.InstantiatePrefab(rewardPrefab,
                GetRewardSpawnPosition(), Quaternion.identity, null);
            rewardObject.layer = gameObject.layer;
            rewardObject.transform.localScale = Vector3.one * _rewardScale;
            FaceCamera(rewardObject.transform);
            return rewardObject;
        }

        private async UniTaskVoid PlayBagDropAsync()
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            Vector3 landPosition = transform.position;
            Vector3 visibleScale = transform.localScale;
            transform.position = landPosition + Vector3.up * _bagDropHeight;
            transform.localScale = visibleScale * 0.8f;

            try
            {
                Tween dropTween = transform
                    .DOJump(landPosition, _bagDropJumpPower, 1, _bagDropDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
                _ = transform.DOScale(visibleScale, _bagDropDuration * 0.7f)
                    .SetEase(Ease.OutBack)
                    .SetLink(gameObject);

                await dropTween.ToUniTask(cancellationToken: cancellationToken);
                transform.SetPositionAndRotation(landPosition, transform.rotation);
                transform.localScale = visibleScale;
                _isReady = true;
                ActivateLootRay();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private void ActivateLootRay()
        {
            if (_lootRayRoot == null)
                return;

            _lootRayRoot.gameObject.SetActive(true);
            foreach (ParticleSystem particleSystem in
                     _lootRayRoot.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Play(true);
        }

        private async UniTask RiseReward(Transform reward, CancellationToken cancellationToken)
        {
            Tween riseTween = reward
                .DOMoveY(reward.position.y + _riseHeight, _riseDuration)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => FaceCamera(reward))
                .SetLink(reward.gameObject);
            _ = reward.DOPunchScale(Vector3.one * (_rewardScale * 0.2f), _riseDuration, 4, 0.5f)
                .SetLink(reward.gameObject);

            await riseTween.ToUniTask(cancellationToken: cancellationToken);
        }

        private async UniTask FlyRewardToCharacter(Transform reward,
            CancellationToken cancellationToken)
        {
            Transform character = _characterProvider?.CharacterFacade != null
                ? _characterProvider.CharacterFacade.transform
                : null;
            if (character == null)
                return;

            Vector3 startPosition = reward.position;
            Tween flyTween = DOVirtual.Float(0f, 1f, _flyDuration, progress =>
                {
                    if (reward == null || character == null)
                        return;

                    Vector3 targetPosition = character.position + Vector3.up * _targetHeight;
                    reward.position = Vector3.Lerp(startPosition, targetPosition, progress);
                    FaceCamera(reward);
                })
                .SetEase(Ease.InCubic)
                .SetLink(reward.gameObject);

            await flyTween.ToUniTask(cancellationToken: cancellationToken);
        }

        private async UniTask DismissReward(Transform reward,
            CancellationToken cancellationToken)
        {
            await reward.DOScale(Vector3.one * (_rewardScale * 1.25f), 0.12f)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: cancellationToken);
            await reward.DOScale(Vector3.zero, 0.14f)
                .SetEase(Ease.InBack)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        private void GrantReward(RewardType rewardType)
        {
            if (rewardType == RewardType.Silver)
                _characterWallet.Silver.Add(_rewardAmount);
            else
                _characterWallet.Keys.Add(_rewardAmount);

            _collectedCallback?.Invoke();
            _collectedCallback = null;
        }

        private Vector3 GetRewardSpawnPosition()
        {
            float top = transform.position.y + 0.5f;
            foreach (Renderer bagRenderer in GetComponentsInChildren<Renderer>())
            {
                if (bagRenderer is ParticleSystemRenderer)
                    continue;

                top = Mathf.Max(top, bagRenderer.bounds.max.y);
            }

            return new Vector3(transform.position.x, top + 0.2f, transform.position.z);
        }

        private void FaceCamera(Transform reward)
        {
            Transform cameraTransform = _cameraService?.MainCamera != null
                ? _cameraService.MainCamera.transform
                : Camera.main != null
                    ? Camera.main.transform
                    : null;
            if (cameraTransform == null)
                return;

            Vector3 directionAwayFromCamera = reward.position - cameraTransform.position;
            if (directionAwayFromCamera.sqrMagnitude > 0.001f)
                reward.rotation = Quaternion.LookRotation(directionAwayFromCamera.normalized, Vector3.up);
        }
    }
}
