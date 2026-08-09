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
        private enum RewardType
        {
            None,
            Heart,
            Key,
            Silver,
            ImprovedRelic,
            PremiumRelic,
            PremiumKey
        }

        private enum WheelTier
        {
            Basic,
            Improved,
            Premium
        }

        private static readonly int[] SpinCosts = { 1, 3, 10 };

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

        [SerializeField] private RelicChestInteractionView _interactionView = new();

        [Header("Wheel")]
        [SerializeField] private Transform _wheelTransform;
        [SerializeField] private Transform _slotsRoot;
        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();

        [Header("Reward Views")]
        [SerializeField] private GameObject _noneRewardPrefab;
        [SerializeField] private GameObject _heartRewardPrefab;
        [SerializeField] private GameObject _keyRewardPrefab;
        [SerializeField] private GameObject _silverRewardPrefab;
        [SerializeField] private GameObject[] _relicRewardPrefabs = Array.Empty<GameObject>();

        [Header("Dropped Rewards")]
        [SerializeField] private GameObject _silverDropPrefab;
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
        [SerializeField, ColorUsage(true, true)] private Color _rewardFxColor = Color.yellow;

        [Header("Reward Amounts")]
        [SerializeField, Min(1)] private int _silverAmount = 1;
        [SerializeField, Min(1)] private int _keyAmount = 1;

        private InputSystem_Actions _inputActions;
        private Transform _spinRoot;
        private readonly Dictionary<RelicDefinition, GameObject> _relicPrefabsByDefinition = new();
        private RewardType[] _baseRewards = Array.Empty<RewardType>();
        private RewardType[] _rewards = Array.Empty<RewardType>();
        private int[][] _tierSlotsBySource = Array.Empty<int[]>();
        private int[] _displayedSourceIndices = Array.Empty<int>();
        private GameObject[] _rewardViews = Array.Empty<GameObject>();
        private bool[] _availableSources = Array.Empty<bool>();
        private RelicDefinition _improvedRelic;
        private RelicDefinition _premiumRelic;
        private WheelTier _selectedTier;
        private bool _improvedRelicConsumed;
        private bool _premiumRelicConsumed;
        private bool _premiumKeyConsumed;
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
            InitializeRewards();
            InitializeGradeFx();

            if (_betSelectionHintText != null)
                _betSelectionHintText.text = "Q  <  BET  >  R";
        }

        private void Start()
        {
            _improvedRelic = RollRelic(RelicRarity.Common, RelicRarity.Uncommon);
            _premiumRelic = RollRelic(RelicRarity.Rare, RelicRarity.Legendary);
            RefreshDisplayedRewards(false);
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
            if (_isSpinning || _spinRoot == null || HasAvailableRewards() == false ||
                _characterProvider?.CharacterFacade == null)
                return false;

            return Vector3.Distance(transform.position,
                       _characterProvider.CharacterFacade.transform.position) <= _interactDistance;
        }

        private int CurrentSpinCost =>
            SpinCosts[Mathf.Clamp((int)_selectedTier, 0, SpinCosts.Length - 1)];

        private void ChangeTier(int direction)
        {
            int tierIndex = Mathf.Clamp((int)_selectedTier + direction,
                0, SpinCosts.Length - 1);
            if (tierIndex == (int)_selectedTier)
                return;

            _selectedTier = (WheelTier)tierIndex;
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
                WheelTier.Improved => _grade2FxColor,
                WheelTier.Premium => _grade3FxColor,
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
            _baseRewards = CreateShuffledRewards(_slots.Length);
            _rewards = (RewardType[])_baseRewards.Clone();
            InitializeTierSlotMappings(_slots.Length);
            _displayedSourceIndices = Array.Empty<int>();
            _rewardViews = new GameObject[_slots.Length];
            _availableSources = new bool[_slots.Length];

            for (int i = 0; i < _slots.Length; i++)
                _availableSources[i] = true;

            RefreshDisplayedRewards(false);
        }

        private void InitializeTierSlotMappings(int slotCount)
        {
            _tierSlotsBySource = new int[SpinCosts.Length][];
            int[] shuffledSlots = new int[Mathf.Max(0, slotCount)];
            for (int i = 0; i < shuffledSlots.Length; i++)
                shuffledSlots[i] = i;

            for (int i = shuffledSlots.Length - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (shuffledSlots[i], shuffledSlots[swapIndex]) =
                    (shuffledSlots[swapIndex], shuffledSlots[i]);
            }

            int[] tierShifts = new int[SpinCosts.Length];
            if (slotCount > 1)
                tierShifts[(int)WheelTier.Improved] = UnityEngine.Random.Range(1, slotCount);

            if (slotCount > 2)
            {
                int improvedShift = tierShifts[(int)WheelTier.Improved];
                tierShifts[(int)WheelTier.Premium] = improvedShift % (slotCount - 1) + 1;
            }

            for (int tierIndex = 0; tierIndex < _tierSlotsBySource.Length; tierIndex++)
            {
                int[] slotsBySource = new int[slotCount];
                for (int sourceIndex = 0; sourceIndex < slotCount; sourceIndex++)
                {
                    int shiftedIndex = (sourceIndex + tierShifts[tierIndex]) % slotCount;
                    slotsBySource[sourceIndex] = shuffledSlots[shiftedIndex];
                }

                _tierSlotsBySource[tierIndex] = slotsBySource;
            }
        }

        private void RefreshDisplayedRewards(bool animateChanges)
        {
            RewardType[] previousRewards = _rewards;
            int[] previousSourceIndices = _displayedSourceIndices;
            BuildDisplayedRewards(out RewardType[] displayedRewards,
                out int[] displayedSourceIndices);

            for (int i = 0; i < _slots.Length; i++)
            {
                Transform slot = _slots[i];
                RewardType previousReward = i < previousRewards.Length
                    ? previousRewards[i]
                    : displayedRewards[i];
                int previousSourceIndex = i < previousSourceIndices.Length
                    ? previousSourceIndices[i]
                    : -1;
                int displayedSourceIndex = displayedSourceIndices[i];
                RewardType displayedReward = displayedRewards[i];
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

        private void BuildDisplayedRewards(out RewardType[] displayedRewards,
            out int[] displayedSourceIndices)
        {
            RewardType[] rewardsBySource = (RewardType[])_baseRewards.Clone();
            bool[] reservedSources = new bool[rewardsBySource.Length];

            if (_selectedTier == WheelTier.Premium)
            {
                if (_premiumRelicConsumed == false && _premiumRelic != null)
                {
                    TryAssignUpgrade(rewardsBySource, reservedSources,
                        RewardType.PremiumRelic, RewardType.None, RewardType.Heart,
                        RewardType.Silver, RewardType.Key);
                }

                if (_improvedRelicConsumed == false && _improvedRelic != null)
                {
                    TryAssignUpgrade(rewardsBySource, reservedSources,
                        RewardType.ImprovedRelic, RewardType.Heart, RewardType.Silver,
                        RewardType.None, RewardType.Key);
                }

                if (_premiumKeyConsumed == false)
                {
                    TryAssignUpgrade(rewardsBySource, reservedSources,
                        RewardType.PremiumKey, RewardType.Heart, RewardType.Silver,
                        RewardType.None, RewardType.Key);
                }
            }
            else if (_selectedTier == WheelTier.Improved &&
                     _improvedRelicConsumed == false && _improvedRelic != null)
            {
                TryAssignUpgrade(rewardsBySource, reservedSources,
                    RewardType.ImprovedRelic, RewardType.Heart, RewardType.Silver,
                    RewardType.None, RewardType.Key);
            }

            displayedRewards = new RewardType[rewardsBySource.Length];
            displayedSourceIndices = new int[rewardsBySource.Length];
            for (int i = 0; i < displayedSourceIndices.Length; i++)
                displayedSourceIndices[i] = -1;

            int tierIndex = Mathf.Clamp((int)_selectedTier, 0,
                _tierSlotsBySource.Length - 1);
            int[] slotsBySource = _tierSlotsBySource[tierIndex];

            for (int sourceIndex = 0; sourceIndex < rewardsBySource.Length; sourceIndex++)
            {
                if (_availableSources[sourceIndex] == false)
                    continue;

                int slotIndex = slotsBySource[sourceIndex];
                if (slotIndex < 0 || slotIndex >= _slots.Length ||
                    _slots[slotIndex] == null)
                    continue;

                displayedRewards[slotIndex] = rewardsBySource[sourceIndex];
                displayedSourceIndices[slotIndex] = sourceIndex;
            }
        }

        private void TryAssignUpgrade(RewardType[] rewardsBySource, bool[] reservedSources,
            RewardType upgradeReward, params RewardType[] sourcePriority)
        {
            foreach (RewardType sourceReward in sourcePriority)
            {
                for (int i = 0; i < _baseRewards.Length; i++)
                {
                    if (_availableSources[i] == false || reservedSources[i] ||
                        _baseRewards[i] != sourceReward)
                        continue;

                    rewardsBySource[i] = upgradeReward;
                    reservedSources[i] = true;
                    return;
                }
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

        private static RewardType[] CreateShuffledRewards(int slotCount)
        {
            RewardType[] fixedRewards =
            {
                RewardType.None,
                RewardType.None,
                RewardType.Heart,
                RewardType.Heart,
                RewardType.Key,
                RewardType.Silver
            };
            RewardType[] rewards = new RewardType[Mathf.Max(0, slotCount)];

            for (int i = 0; i < rewards.Length; i++)
                rewards[i] = i < fixedRewards.Length
                    ? fixedRewards[i]
                    : RewardType.Silver;

            for (int i = rewards.Length - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (rewards[i], rewards[swapIndex]) = (rewards[swapIndex], rewards[i]);
            }

            return rewards;
        }

        private RelicDefinition RollRelic(RelicRarity firstRarity,
            RelicRarity secondRarity)
        {
            if (_relicPool == null)
                return null;

            List<RelicDefinition> firstCandidates = new();
            List<RelicDefinition> secondCandidates = new();

            foreach (RelicDefinition relic in
                     _relicPool.GetAvailable(_relicManager?.ActiveRelics))
            {
                if (relic == null || _relicPrefabsByDefinition.ContainsKey(relic) == false)
                    continue;

                if (relic.Rarity == firstRarity)
                    firstCandidates.Add(relic);
                else if (relic.Rarity == secondRarity)
                    secondCandidates.Add(relic);
            }

            bool rollFirstRarity = UnityEngine.Random.value < 0.5f;
            List<RelicDefinition> candidates = rollFirstRarity
                ? firstCandidates
                : secondCandidates;

            if (candidates.Count == 0)
                candidates = rollFirstRarity ? secondCandidates : firstCandidates;

            return candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : null;
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

                RewardType rewardType = _rewards[winningSlotIndex];
                int winningSourceIndex = _displayedSourceIndices[winningSlotIndex];
                await PlayWinningRewardAnimationAsync(winningSlotIndex, cancellationToken);
                MarkUpgradeConsumed(rewardType);
                if (winningSourceIndex >= 0 && winningSourceIndex < _availableSources.Length)
                    _availableSources[winningSourceIndex] = false;
                DropReward(rewardType);
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
            foreach (int sourceIndex in _displayedSourceIndices)
            {
                if (sourceIndex >= 0)
                    return true;
            }

            return false;
        }

        private void MarkUpgradeConsumed(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.ImprovedRelic:
                    _improvedRelicConsumed = true;
                    break;
                case RewardType.PremiumRelic:
                    _premiumRelicConsumed = true;
                    break;
                case RewardType.PremiumKey:
                    _premiumKeyConsumed = true;
                    break;
            }
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

        private void DropReward(RewardType rewardType)
        {
            if (rewardType == RewardType.None)
                return;

            PlayGradeFx(_rewardFxColor);

            switch (rewardType)
            {
                case RewardType.Heart:
                    _heartDropper?.DropHeart(GetRewardSpawnPosition(),
                        collectWhenHealthFull: true, additionalDropHeight: 0.5f);
                    return;
                case RewardType.Key:
                case RewardType.PremiumKey:
                    if (TryDropCurrencyReward(_keyDropPrefab,
                            () => _characterWallet?.Keys.Add(Mathf.Max(1, _keyAmount))) == false)
                        _characterWallet?.Keys.Add(Mathf.Max(1, _keyAmount));
                    return;
                case RewardType.Silver:
                    if (TryDropCurrencyReward(_silverDropPrefab,
                            () => _characterWallet?.Silver.Add(Mathf.Max(1, _silverAmount))) == false)
                        _characterWallet?.Silver.Add(Mathf.Max(1, _silverAmount));
                    return;
                case RewardType.ImprovedRelic:
                    DropRelic(_improvedRelic);
                    return;
                case RewardType.PremiumRelic:
                    DropRelic(_premiumRelic);
                    return;
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

        private GameObject GetRewardPrefab(RewardType rewardType) => rewardType switch
        {
            RewardType.None => _noneRewardPrefab,
            RewardType.Heart => _heartRewardPrefab,
            RewardType.Key => _keyRewardPrefab,
            RewardType.PremiumKey => _keyRewardPrefab,
            RewardType.Silver => _silverRewardPrefab,
            RewardType.ImprovedRelic => GetRelicRewardPrefab(_improvedRelic),
            RewardType.PremiumRelic => GetRelicRewardPrefab(_premiumRelic),
            _ => null
        };

        private GameObject GetRelicRewardPrefab(RelicDefinition relic) =>
            relic != null && _relicPrefabsByDefinition.TryGetValue(relic, out GameObject prefab)
                ? prefab
                : null;

    }
}
