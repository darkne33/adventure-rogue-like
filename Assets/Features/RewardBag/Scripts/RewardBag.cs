using System;
using System.Threading;
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
            Key,
            Heart
        }

        [Inject] private DiContainer _container;
        [Inject] private HeartDropper _heartDropper;
        [Inject] private CharacterStats _characterStats;
        [Inject] private LevelsConfiguration _levelsConfiguration;
        [Inject] private ITimeScaleService _timeScaleService;

        [SerializeField] private RelicChestInteractionView _interactionView = new();
        [SerializeField] private GameObject _silverRewardPrefab;
        [SerializeField] private GameObject _keyRewardPrefab;
        [SerializeField] private Transform _lootRayRoot;
        [SerializeField, Min(0f)] private float _interactDistance = 4f;
        [SerializeField, Min(1)] private int _rewardAmount = 1;
        [SerializeField, Range(0f, 1f)] private float _keyDropChance = 0.33f;
        [SerializeField, Range(0f, 1f)] private float _heartDropChance = 0.33f;
        [SerializeField, Min(0.01f)] private float _rewardScale = 2f;
        [SerializeField, Min(0f)] private float _rewardExtraDropHeight = 0.5f;
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
            if (_timeScaleService.IsPaused || _isReady == false || _isOpened ||
                _characterProvider?.CharacterFacade == null || _characterWallet == null)
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
            Action collectedCallback = _collectedCallback;
            _collectedCallback = null;

            try
            {
                DropReward(RollReward(), collectedCallback);

                await transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .SetLink(gameObject)
                    .ToUniTask(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (this != null)
                    Destroy(gameObject);
            }
        }

        private RewardType RollReward()
        {
            float keyChance = Mathf.Clamp01(_keyDropChance);
            float heartChance = Mathf.Clamp(_heartDropChance, 0f, 1f - keyChance);
            float roll = UnityEngine.Random.value;

            if (roll < keyChance)
                return RewardType.Key;

            return roll < keyChance + heartChance
                ? RewardType.Heart
                : RewardType.Silver;
        }

        private void DropReward(RewardType rewardType, Action collectedCallback)
        {
            if (rewardType == RewardType.Heart)
            {
                if (_heartDropper == null ||
                    _heartDropper.DropHeart(transform.position, collectedCallback, true,
                        _rewardExtraDropHeight) == false)
                    collectedCallback?.Invoke();

                return;
            }

            if (TryDropCurrencyReward(rewardType, collectedCallback))
                return;

            GrantCurrencyReward(rewardType);
            collectedCallback?.Invoke();
        }

        private bool TryDropCurrencyReward(RewardType rewardType, Action collectedCallback)
        {
            GameObject rewardPrefab = rewardType == RewardType.Silver
                ? _silverRewardPrefab
                : _keyRewardPrefab;
            if (rewardPrefab == null)
                return false;

            GameObject rewardObject = _container.InstantiatePrefab(rewardPrefab,
                GetRewardSpawnPosition(), Quaternion.identity, null);
            rewardObject.layer = gameObject.layer;
            rewardObject.transform.localScale = Vector3.one * _rewardScale;

            RewardBagPickup pickup = rewardObject.GetComponent<RewardBagPickup>();
            if (pickup == null)
                pickup = rewardObject.AddComponent<RewardBagPickup>();

            CharacterWallet characterWallet = _characterWallet;
            int rewardAmount = Mathf.Max(1, _rewardAmount);
            Action grantReward = rewardType == RewardType.Silver
                ? () => characterWallet.Silver.Add(rewardAmount)
                : () => characterWallet.Keys.Add(rewardAmount);
            pickup.Construct(_characterProvider, _characterStats, GetRewardLandPosition(),
                grantReward, collectedCallback);
            return true;
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

        private void GrantCurrencyReward(RewardType rewardType)
        {
            if (rewardType == RewardType.Silver)
                _characterWallet.Silver.Add(_rewardAmount);
            else
                _characterWallet.Keys.Add(_rewardAmount);
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

            return new Vector3(transform.position.x,
                top + 0.2f + _rewardExtraDropHeight, transform.position.z);
        }

        private Vector3 GetRewardLandPosition()
        {
            const float scatterRadius = 1.2f;
            const float groundOffset = 0.35f;
            const float rayStartHeight = 4f;
            const float rayDistance = 12f;

            Vector2 scatter = UnityEngine.Random.insideUnitCircle * scatterRadius;
            Vector3 position = transform.position + new Vector3(scatter.x, 0f, scatter.y);
            Vector3 rayOrigin = position + Vector3.up * rayStartHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance,
                    GetGroundLayerMask(), QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * groundOffset;

            return position + Vector3.up * groundOffset;
        }

        private LayerMask GetGroundLayerMask() =>
            _levelsConfiguration != null && _levelsConfiguration.GroundLayer.value != 0
                ? _levelsConfiguration.GroundLayer
                : Physics.DefaultRaycastLayers;
    }
}
