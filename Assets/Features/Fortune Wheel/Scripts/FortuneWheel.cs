using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.Relics.Scripts;
using Features.RewardBag;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace Features.FortuneWheel
{
    public sealed class FortuneWheel : MonoBehaviour
    {
        private sealed class RewardState
        {
            public FortuneWheelReward Definition { get; }
            public RelicDefinition Relic { get; }
            public bool IsAvailable { get; set; } = true;

            public RewardState(FortuneWheelReward definition, RelicDefinition relic)
            {
                Definition = definition;
                Relic = relic;
            }
        }

        [Inject] private ICharacterProvider _characterProvider;
        [Inject] private CharacterWallet _characterWallet;
        [Inject] private HeartDropper _heartDropper;
        [Inject] private CharacterStats _characterStats;
        [Inject] private LevelsConfiguration _levelsConfiguration;
        [Inject] private DiContainer _container;
        [Inject] private RelicPool _relicPool;
        [Inject] private RelicManager _relicManager;
        [Inject] private RelicEventBus _relicEventBus;
        [Inject] private RelicChestConfiguration _relicChestConfiguration;
        [Inject] private ITimeScaleService _timeScaleService;
        [Inject] private GoldDropperConfiguration _goldDropperConfiguration;
        [Inject] private UI.IPanelService _panelService;

        [SerializeField] private RelicChestInteractionView _interactionView = new();
        [SerializeField] private FortuneWheelConfiguration _configuration;

        [Header("Wheel")]
        [SerializeField] private Transform _wheelTransform;
        [SerializeField] private Transform _slotsRoot;
        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();

        [Header("Reward Views")]
        [SerializeField] private GameObject _noneRewardPrefab;
        [SerializeField] private GameObject _heartRewardPrefab;
        [SerializeField] private GameObject _keyRewardPrefab;
        [SerializeField] private GameObject _goldRewardPrefab;
        [SerializeField] private GameObject _relicRewardPrefab;
        [SerializeField] private GameObject[] _relicRewardPrefabs = Array.Empty<GameObject>();

        [Header("Dropped Rewards")]
        [SerializeField] private GameObject _keyDropPrefab;
        [SerializeField, Min(0.01f)] private float _rewardDropScale = 2f;
        [SerializeField, Min(0f)] private float _rewardDropForwardOffset = 0.45f;

        [Header("Interaction")]
        [SerializeField, Min(0f)] private float _interactDistance = 4f;
        [SerializeField] private Text _priceText;
        [SerializeField] private Text _betSelectionHintText;
        [SerializeField] private Color _affordablePriceColor = new(1f, 0.92f, 0.62f, 1f);
        [SerializeField] private Color _unaffordablePriceColor = new(1f, 0.3f, 0.2f, 1f);

        [Header("Spin")]
        [SerializeField, Min(0.1f)] private float _spinDuration = 2f;
        [SerializeField, Min(1)] private int _minFullRotations = 4;
        [SerializeField, Min(1)] private int _maxFullRotations = 6;
        [SerializeField, Min(0f)] private float _anticipationAngle = 8f;
        [SerializeField, Min(1)] private int _slowdownSlotCount = 5;

        [Header("Grade FX")]
        [SerializeField] private Transform _gradeFxRoot;
        [SerializeField, ColorUsage(true, true)] private Color _grade1FxColor = Color.green;
        [SerializeField, ColorUsage(true, true)] private Color _grade2FxColor = Color.blue;
        [SerializeField, ColorUsage(true, true)] private Color _grade3FxColor = new(0.6f, 0.15f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color _grade4FxColor = new(1f, 0.75f, 0.12f);
        [SerializeField, ColorUsage(true, true)] private Color _rewardFxColor = Color.yellow;

        private InputSystem_Actions _inputActions;
        private Transform _spinRoot;
        private readonly Dictionary<RelicDefinition, GameObject> _relicPrefabsByDefinition = new();
        private RewardState[][] _tierRewards = Array.Empty<RewardState[]>();
        private RewardState[] _rewards = Array.Empty<RewardState>();
        private int[][] _tierSlotsBySource = Array.Empty<int[]>();
        private int[] _displayedSourceIndices = Array.Empty<int>();
        private GameObject[] _rewardViews = Array.Empty<GameObject>();
        private int _selectedTier;
        private bool _isSpinning;
        private ParticleSystem[] _gradeFxSystems = Array.Empty<ParticleSystem>();
        private Vector3 _wheelBaseScale = Vector3.one;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _interactionView.Initialize(gameObject);
            CacheRelicRewardPrefabs();
            CreateSpinRoot();
            if (_wheelTransform != null)
                _wheelBaseScale = _wheelTransform.localScale;
            InitializeGradeFx();

            if (_betSelectionHintText != null)
                _betSelectionHintText.text = "Q  <  BET  >  R";
        }

        private void Start()
        {
            InitializeRewards();
        }

        private void OnEnable()
        {
            _inputActions ??= new InputSystem_Actions();
            _inputActions.Player.Interact.Enable();
            _inputActions.Player.Previous.Enable();
            _inputActions.Player.Next.Enable();
        }

        private void OnDisable()
        {
            _inputActions?.Player.Interact.Disable();
            _inputActions?.Player.Previous.Disable();
            _inputActions?.Player.Next.Disable();
            _interactionView.SetAvailable(false, true);
        }

        private void OnDestroy()
        {
            _spinRoot?.DOKill();
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void Update()
        {
            UpdatePriceView();
            bool canInteract = CanInteract();
            _interactionView.SetAvailable(canInteract);

            if (canInteract == false || _inputActions == null)
                return;

            if (_inputActions.Player.Previous.WasPressedThisFrame())
                ChangeTier(-1);

            if (_inputActions.Player.Next.WasPressedThisFrame())
                ChangeTier(1);

            if (_inputActions.Player.Interact.WasPressedThisFrame())
                SpinAsync().Forget();
        }

        private bool CanInteract()
        {
            if (_timeScaleService.IsPaused || _isSpinning || _spinRoot == null ||
                HasAvailableRewards() == false ||
                _characterProvider?.CharacterFacade == null)
                return false;

            return Vector3.Distance(transform.position,
                       _characterProvider.CharacterFacade.transform.position) <= _interactDistance;
        }

        private int CurrentSpinCost => _configuration != null &&
                                       _selectedTier < _configuration.RewardSets.Count
            ? _configuration.RewardSets[_selectedTier].SpinCost
            : 0;

        private void ChangeTier(int direction)
        {
            int tierIndex = Mathf.Clamp(_selectedTier + direction,
                0, _tierRewards.Length - 1);
            if (tierIndex == _selectedTier)
                return;

            _selectedTier = tierIndex;
            RefreshDisplayedRewards(true);
            UpdatePriceView();
            PlayGradeFx();
            PlayWheelGradePunch();

            if (_priceText == null)
                return;

            Transform priceTransform = _priceText.transform;
            priceTransform.DOKill();
            priceTransform.localScale = Vector3.one;
            priceTransform.DOPunchScale(Vector3.one * 0.12f, 0.22f, 5, 0.55f)
                .SetLink(_priceText.gameObject);
        }

        private void InitializeGradeFx()
        {
            if (_gradeFxRoot == null)
                return;

            _gradeFxSystems = _gradeFxRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in _gradeFxSystems)
                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void PlayGradeFx()
        {
            Color color = _selectedTier switch
            {
                1 => _grade2FxColor,
                2 => _grade3FxColor,
                3 => _grade4FxColor,
                _ => _grade1FxColor
            };

            PlayGradeFx(color);
        }

        private void PlayGradeFx(Color color)
        {

            foreach (ParticleSystem particleSystem in _gradeFxSystems)
            {
                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = color;
                particleSystem.Play(false);
            }
        }

        private void PlayWheelGradePunch()
        {
            if (_wheelTransform == null)
                return;

            _wheelTransform.DOKill();
            _wheelTransform.localScale = _wheelBaseScale;
            _wheelTransform.DOPunchScale(_wheelBaseScale * 0.12f,
                    0.28f, 6, 0.5f)
                .SetLink(_wheelTransform.gameObject);
        }

        private void CreateSpinRoot()
        {
            if (_wheelTransform == null || _slotsRoot == null)
                return;

            _spinRoot = new GameObject("SpinRoot").transform;
            _spinRoot.SetParent(transform, false);
            _spinRoot.localPosition = _wheelTransform.localPosition;

            _wheelTransform.SetParent(_spinRoot, true);
            _slotsRoot.SetParent(_spinRoot, true);
        }

        private void CacheRelicRewardPrefabs()
        {
            _relicPrefabsByDefinition.Clear();

            foreach (GameObject rewardPrefab in _relicRewardPrefabs)
            {
                if (rewardPrefab == null)
                    continue;

                FortuneWheelRelicRewardView rewardView =
                    rewardPrefab.GetComponent<FortuneWheelRelicRewardView>();
                if (rewardView?.Relic != null)
                    _relicPrefabsByDefinition[rewardView.Relic] = rewardPrefab;
            }
        }

        private void InitializeRewards()
        {
            int tierCount = _configuration != null ? _configuration.RewardSets.Count : 0;
            _tierRewards = new RewardState[tierCount][];
            for (int tierIndex = 0; tierIndex < tierCount; tierIndex++)
            {
                IReadOnlyList<FortuneWheelReward> definitions =
                    _configuration.RewardSets[tierIndex].Rewards;
                RewardState[] rewards = new RewardState[Mathf.Min(definitions.Count, _slots.Length)];
                HashSet<string> selectedRelicIds = new();
                for (int sourceIndex = 0; sourceIndex < rewards.Length; sourceIndex++)
                {
                    FortuneWheelReward definition = definitions[sourceIndex];
                    RelicDefinition relic = definition.Type == FortuneWheelRewardType.Relic
                        ? RollRelic(definition.Rarity, selectedRelicIds)
                        : null;
                    if (relic != null)
                        selectedRelicIds.Add(relic.Id);
                    rewards[sourceIndex] = new RewardState(definition, relic);
                }

                _tierRewards[tierIndex] = rewards;
            }

            InitializeTierSlotMappings();
            _displayedSourceIndices = Array.Empty<int>();
            _rewardViews = new GameObject[_slots.Length];
            RefreshDisplayedRewards(false);
        }

        private void InitializeTierSlotMappings()
        {
            _tierSlotsBySource = new int[_tierRewards.Length][];
            for (int tierIndex = 0; tierIndex < _tierSlotsBySource.Length; tierIndex++)
            {
                int[] shuffledSlots = new int[_slots.Length];
                for (int i = 0; i < shuffledSlots.Length; i++)
                    shuffledSlots[i] = i;

                for (int i = shuffledSlots.Length - 1; i > 0; i--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, i + 1);
                    (shuffledSlots[i], shuffledSlots[swapIndex]) =
                        (shuffledSlots[swapIndex], shuffledSlots[i]);
                }

                _tierSlotsBySource[tierIndex] = shuffledSlots;
            }
        }

        private void RefreshDisplayedRewards(bool animateChanges)
        {
            RewardState[] previousRewards = _rewards;
            int[] previousSourceIndices = _displayedSourceIndices;
            BuildDisplayedRewards(out RewardState[] displayedRewards,
                out int[] displayedSourceIndices);

            for (int i = 0; i < _slots.Length; i++)
            {
                Transform slot = _slots[i];
                RewardState previousReward = i < previousRewards.Length
                    ? previousRewards[i]
                    : displayedRewards[i];
                int previousSourceIndex = i < previousSourceIndices.Length
                    ? previousSourceIndices[i]
                    : -1;
                int displayedSourceIndex = displayedSourceIndices[i];
                RewardState displayedReward = displayedRewards[i];
                GameObject rewardPrefab = GetRewardPrefab(displayedReward);

                if (displayedSourceIndex < 0 || slot == null || rewardPrefab == null)
                {
                    _rewardViews[i]?.SetActive(false);
                    continue;
                }

                GameObject rewardView = _rewardViews[i];
                if (rewardView == null)
                {
                    rewardView = Instantiate(rewardPrefab, slot, false);
                    _rewardViews[i] = rewardView;
                }
                else
                {
                    rewardView.transform.DOKill();
                    rewardView.transform.localScale = rewardPrefab.transform.localScale;
                    CopyRewardView(rewardPrefab, rewardView);
                    rewardView.SetActive(true);
                }

                rewardView.transform.SetParent(slot, false);
                rewardView.transform.SetLocalPositionAndRotation(Vector3.zero,
                    Quaternion.identity);
                rewardView.transform.localScale = rewardPrefab.transform.localScale;

                if (displayedReward.Relic != null)
                {
                    SpriteRenderer relicRenderer = rewardView.GetComponent<SpriteRenderer>();
                    if (relicRenderer != null)
                        relicRenderer.sprite = displayedReward.Relic.Icon;
                }

                if (animateChanges &&
                    (previousSourceIndex != displayedSourceIndex ||
                     previousReward != displayedReward))
                {
                    Transform rewardTransform = rewardView.transform;
                    Vector3 targetScale = rewardPrefab.transform.localScale;
                    rewardTransform.DOKill();
                    rewardTransform.localScale = targetScale * 0.35f;
                    rewardTransform.DOScale(targetScale, 0.24f)
                        .SetDelay(i * 0.025f)
                        .SetEase(Ease.OutBack)
                        .SetLink(rewardView);
                }
            }

            _rewards = displayedRewards;
            _displayedSourceIndices = displayedSourceIndices;
        }

        private void BuildDisplayedRewards(out RewardState[] displayedRewards,
            out int[] displayedSourceIndices)
        {
            displayedRewards = new RewardState[_slots.Length];
            displayedSourceIndices = new int[_slots.Length];
            for (int i = 0; i < displayedSourceIndices.Length; i++)
                displayedSourceIndices[i] = -1;

            if (_tierRewards.Length == 0)
                return;

            RewardState[] rewardsBySource = _tierRewards[_selectedTier];
            int[] slotsBySource = _tierSlotsBySource[_selectedTier];

            for (int sourceIndex = 0; sourceIndex < rewardsBySource.Length; sourceIndex++)
            {
                if (rewardsBySource[sourceIndex].IsAvailable == false)
                    continue;

                int slotIndex = slotsBySource[sourceIndex];
                if (slotIndex < 0 || slotIndex >= _slots.Length ||
                    _slots[slotIndex] == null)
                    continue;

                displayedRewards[slotIndex] = rewardsBySource[sourceIndex];
                displayedSourceIndices[slotIndex] = sourceIndex;
            }
        }

        private static void CopyRewardView(GameObject sourcePrefab, GameObject targetView)
        {
            SpriteRenderer sourceRenderer = sourcePrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer targetRenderer = targetView.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null || targetRenderer == null)
                return;

            targetRenderer.sprite = sourceRenderer.sprite;
            targetRenderer.color = sourceRenderer.color;
            targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            targetRenderer.sortingOrder = sourceRenderer.sortingOrder;
            targetRenderer.maskInteraction = sourceRenderer.maskInteraction;
            targetRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
            targetRenderer.flipX = sourceRenderer.flipX;
            targetRenderer.flipY = sourceRenderer.flipY;
            targetRenderer.drawMode = sourceRenderer.drawMode;
            targetRenderer.size = sourceRenderer.size;
            targetView.name = sourcePrefab.name;
        }

        private RelicDefinition RollRelic(RelicRarity rarity,
            IReadOnlyCollection<string> selectedRelicIds)
        {
            if (_relicPool == null)
                return null;

            List<RelicDefinition> candidates = GetRelicCandidates(rarity,
                _relicPool.GetAvailable(_relicManager?.ActiveRelics, selectedRelicIds));

            // The wheel may offer locked relics when the player has no eligible
            // unlocked option of this rarity. This does not unlock the collection entry.
            if (candidates.Count == 0)
                candidates = GetRelicCandidates(rarity,
                    _relicPool.GetAvailable(_relicManager?.ActiveRelics, selectedRelicIds,
                        includeLocked: true));

            if (candidates.Count == 0)
                candidates = GetRelicCandidates(rarity,
                    _relicPool.GetAvailable(_relicManager?.ActiveRelics, includeLocked: true));

            return candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : null;
        }

        private static List<RelicDefinition> GetRelicCandidates(RelicRarity rarity,
            IEnumerable<RelicDefinition> availableRelics)
        {
            List<RelicDefinition> candidates = new();
            foreach (RelicDefinition relic in availableRelics)
            {
                if (relic == null || relic.Rarity != rarity || relic.Icon == null)
                    continue;

                candidates.Add(relic);
            }

            return candidates;
        }

        private async UniTaskVoid SpinAsync()
        {
            int winningSlotIndex = RollAvailableSlot();
            if (winningSlotIndex < 0)
                return;

            if (TryPayForSpin() == false)
            {
                PlayCannotAffordAnimation();
                return;
            }

            _isSpinning = true;
            _interactionView.SetAvailable(false);

            int minRotations = Mathf.Max(1, _minFullRotations);
            int maxRotations = Mathf.Max(minRotations, _maxFullRotations);
            int fullRotations = UnityEngine.Random.Range(minRotations, maxRotations + 1);
            float slotAngle = 360f / _slots.Length;
            float currentAngle = _spinRoot.localEulerAngles.z;
            float winningAngle = Mathf.Repeat(-winningSlotIndex * slotAngle, 360f);
            float counterClockwiseOffset = Mathf.Repeat(winningAngle - currentAngle, 360f);
            float targetAngle = currentAngle +
                                fullRotations * 360f + counterClockwiseOffset;
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                await CreateSpinSequence(targetAngle, winningSlotIndex, slotAngle)
                    .SetLink(gameObject)
                    .ToUniTask(cancellationToken: cancellationToken);

                _spinRoot.localRotation = Quaternion.Euler(
                    0f, 0f, -winningSlotIndex * slotAngle);
                _spinRoot.localScale = Vector3.one;

                RewardState reward = _rewards[winningSlotIndex];
                await PlayWinningRewardAnimationAsync(winningSlotIndex, cancellationToken);
                reward.IsAvailable = false;
                DropReward(reward);
                RefreshDisplayedRewards(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                _isSpinning = false;
            }
        }

        private int RollAvailableSlot()
        {
            int availableCount = 0;
            foreach (int sourceIndex in _displayedSourceIndices)
            {
                if (sourceIndex >= 0)
                    availableCount++;
            }

            if (availableCount == 0)
                return -1;

            int roll = UnityEngine.Random.Range(0, availableCount);
            for (int i = 0; i < _displayedSourceIndices.Length; i++)
            {
                if (_displayedSourceIndices[i] < 0)
                    continue;

                if (roll == 0)
                    return i;

                roll--;
            }

            return -1;
        }

        private bool HasAvailableRewards()
        {
            foreach (RewardState[] rewards in _tierRewards)
            {
                foreach (RewardState reward in rewards)
                {
                    if (reward.IsAvailable)
                        return true;
                }
            }

            return false;
        }

        private Sequence CreateSpinSequence(float targetAngle, int winningSlotIndex,
            float slotAngle)
        {
            const float anticipationDuration = 0.12f;

            float totalDuration = Mathf.Max(0.5f, _spinDuration);
            float spinDuration = totalDuration - anticipationDuration;
            int slowdownSlotCount = Mathf.Clamp(_slowdownSlotCount, 1,
                Mathf.Max(1, _slots.Length));
            float firstTickAngle = targetAngle -
                                   (slowdownSlotCount - 1) * slotAngle;
            float anticipatedAngle = _spinRoot.localEulerAngles.z -
                                     Mathf.Max(0f, _anticipationAngle);
            int nextTickIndex = 0;

            _spinRoot.DOKill();
            _spinRoot.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence().SetTarget(_spinRoot);
            sequence.Append(_spinRoot.DOLocalRotate(
                    Vector3.forward * (_spinRoot.localEulerAngles.z -
                                       Mathf.Max(0f, _anticipationAngle)),
                    anticipationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic));
            sequence.Append(DOVirtual.Float(anticipatedAngle, targetAngle, spinDuration,
                    angle =>
                    {
                        _spinRoot.localRotation = Quaternion.Euler(0f, 0f, angle);

                        while (nextTickIndex < slowdownSlotCount &&
                               angle >= firstTickAngle + nextTickIndex * slotAngle)
                        {
                            int remainingTicks = slowdownSlotCount - 1 - nextTickIndex;
                            int slotIndex = (winningSlotIndex + remainingTicks) % _slots.Length;
                            PlaySlotTickVisual(slotIndex);
                            nextTickIndex++;
                        }
                    })
                .SetEase(Ease.OutQuart));

            return sequence;
        }

        private void PlaySlotTickVisual(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _rewardViews.Length ||
                _rewardViews[slotIndex] == null)
                return;

            Transform rewardTransform = _rewardViews[slotIndex].transform;
            rewardTransform.DOKill();
            rewardTransform.DOPunchRotation(Vector3.forward * -5f,
                    0.1f, 3, 0.35f)
                .SetLink(_rewardViews[slotIndex]);
        }

        private bool TryPayForSpin()
        {
            int cost = CurrentSpinCost;
            if (_characterWallet == null || _characterWallet.Gold.Count < cost)
                return false;

            _characterWallet.Gold.Remove(cost);
            UpdatePriceView();
            return true;
        }

        private void UpdatePriceView()
        {
            if (_priceText == null)
                return;

            int cost = CurrentSpinCost;
            bool canAfford = _characterWallet != null &&
                             _characterWallet.Gold.Count >= cost;
            _priceText.text = cost.ToString();
            _priceText.color = canAfford
                ? _affordablePriceColor
                : _unaffordablePriceColor;
        }

        private void PlayCannotAffordAnimation()
        {
            if (_priceText == null)
                return;

            Transform priceTransform = _priceText.transform;
            priceTransform.DOKill();
            priceTransform.localScale = Vector3.one;
            priceTransform.DOPunchScale(Vector3.one * 0.18f, 0.35f, 7, 0.45f)
                .SetLink(_priceText.gameObject);
        }

        private async UniTask PlayWinningRewardAnimationAsync(int winningSlotIndex,
            CancellationToken cancellationToken)
        {
            if (winningSlotIndex < 0 || winningSlotIndex >= _rewardViews.Length)
                return;

            GameObject rewardView = _rewardViews[winningSlotIndex];
            if (rewardView == null)
                return;

            Transform rewardTransform = rewardView.transform;
            Vector3 visibleScale = rewardTransform.localScale;
            await rewardTransform
                .DOPunchScale(visibleScale * 0.25f, 0.38f, 6, 0.55f)
                .SetLink(rewardView)
                .ToUniTask(cancellationToken: cancellationToken);

            await rewardTransform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetLink(rewardView)
                .ToUniTask(cancellationToken: cancellationToken);
            rewardView.SetActive(false);
        }

        private void DropReward(RewardState reward)
        {
            if (reward.Definition.Type == FortuneWheelRewardType.None)
                return;

            PlayGradeFx(_rewardFxColor);

            switch (reward.Definition.Type)
            {
                case FortuneWheelRewardType.Heart:
                    _heartDropper?.DropHeart(GetRewardSpawnPosition(),
                        collectWhenHealthFull: true, additionalDropHeight: 0.5f);
                    return;
                case FortuneWheelRewardType.Key:
                    if (TryDropCurrencyReward(_keyDropPrefab,
                            () => _characterWallet?.Keys.Add(reward.Definition.Amount)) == false)
                        _characterWallet?.Keys.Add(reward.Definition.Amount);
                    return;
                case FortuneWheelRewardType.Gold:
                    DropGold(reward.Definition.Amount);
                    return;
                case FortuneWheelRewardType.Relic:
                    DropRelic(reward.Relic);
                    return;
            }
        }

        private void DropGold(int amount)
        {
            GameObject coinPrefab = _goldDropperConfiguration?.CoinGoldPrefab;
            if (coinPrefab == null || _container == null || _characterProvider == null)
            {
                _characterWallet?.Gold.Add(amount);
                return;
            }

            Vector3 spawnPosition = GetRewardSpawnPosition();
            for (int i = 0; i < amount; i++)
            {
                GameObject coinObject = _container.InstantiatePrefab(coinPrefab,
                    spawnPosition, Quaternion.identity, null);
                CoinGold coin = coinObject.GetComponent<CoinGold>();
                if (coin == null)
                {
                    Destroy(coinObject);
                    _characterWallet?.Gold.Add(1);
                    continue;
                }

                coinObject.layer = gameObject.layer;
                coinObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, _rewardDropScale);
                coin.Construct(1, _goldDropperConfiguration, _characterWallet,
                    _characterProvider, _characterStats, _panelService, spawnPosition, null,
                    flyToCharacter: true);
            }
        }

        private void DropRelic(RelicDefinition relic)
        {
            if (relic == null)
                return;

            GameObject pickupPrefab = _relicChestConfiguration?.RelicPickupPrefab;
            if (pickupPrefab == null || _container == null || _relicManager == null ||
                _relicEventBus == null || _characterProvider == null)
            {
                _relicManager?.AddRelic(relic);
                return;
            }

            GameObject rewardObject = _container.InstantiatePrefab(pickupPrefab,
                GetRewardSpawnPosition(), Quaternion.identity, null);
            RelicPickup pickup = rewardObject.GetComponent<RelicPickup>();
            if (pickup == null)
            {
                Destroy(rewardObject);
                _relicManager.AddRelic(relic);
                return;
            }

            rewardObject.layer = gameObject.layer;
            pickup.Construct(relic, _relicChestConfiguration, _relicManager,
                _relicEventBus, _characterProvider, null, null,
                collectImmediately: true);
        }

        private bool TryDropCurrencyReward(GameObject rewardPrefab, Action grantReward)
        {
            if (rewardPrefab == null || _container == null || _characterProvider == null)
                return false;

            GameObject rewardObject = _container.InstantiatePrefab(rewardPrefab,
                GetRewardSpawnPosition(), Quaternion.identity, null);
            rewardObject.layer = gameObject.layer;
            rewardObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, _rewardDropScale);

            RewardBagPickup pickup = rewardObject.GetComponent<RewardBagPickup>();
            if (pickup == null)
                pickup = rewardObject.AddComponent<RewardBagPickup>();

            pickup.Construct(_characterProvider, _characterStats, GetRewardLandPosition(),
                grantReward, null);
            return true;
        }

        private Vector3 GetRewardSpawnPosition()
        {
            Vector3 center = _wheelTransform != null
                ? _wheelTransform.position
                : transform.position + Vector3.up * 1.35f;
            return center + transform.forward * Mathf.Max(0f, _rewardDropForwardOffset);
        }

        private Vector3 GetRewardLandPosition()
        {
            const float scatterRadius = 1.1f;
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

        private GameObject GetRewardPrefab(RewardState reward) => reward?.Definition.Type switch
        {
            FortuneWheelRewardType.None => _noneRewardPrefab,
            FortuneWheelRewardType.Heart => _heartRewardPrefab,
            FortuneWheelRewardType.Key => _keyRewardPrefab,
            FortuneWheelRewardType.Gold => _goldRewardPrefab,
            FortuneWheelRewardType.Relic => GetRelicRewardPrefab(reward.Relic),
            _ => null
        };

        private GameObject GetRelicRewardPrefab(RelicDefinition relic) => relic == null
            ? _noneRewardPrefab
            : _relicPrefabsByDefinition.TryGetValue(relic, out GameObject prefab)
                ? prefab
                : _relicRewardPrefab;

    }
}
