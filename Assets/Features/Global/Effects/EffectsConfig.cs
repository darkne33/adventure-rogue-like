using System.Linq;
using System.Threading;
using AYellowpaper.SerializedCollections;
using CustomPackages.Package.Extensions.Other;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    [CreateAssetMenu(menuName = "Data/VFX collection")]
    public class EffectsConfig : ScriptableObject
    {
        [SerializedDictionary("Name", "Effect")]
        public SerializedDictionary<EffectName, AddressableLoadContainerEffectPlayer> Effects = new();

        public UniTask Load(CancellationToken cts) =>
            UniTask.WhenAll(Enumerable.Select(Effects.Values, effect => effect.Load(cts)).ToList());

        public void CleanUp()
        {
            foreach (var effect in Effects.Values)
                effect.CleanUp();
        }

        public void Validate()
        {
            Effects.Validate(name);
        }
    }

    public enum EffectName
    {
        MoneyWithPoof = 0,
        Poof = 1,
        SmallBuildCloud = 2,
        BigBuildCloud = 3,
        Confetti = 4,
        ConfettiOnce = 5,
        ConfettiCircle = 6,
        Explosion = 7,
        ChestWinPoof = 8,
        ChestWinCoins = 9,
        ChestLosePoof = 10,
        ChestLoseCoal = 11,
        EnergyBonusPoof = 12,
        EventPoof = 13,
        MoveMetaFirework = 14,
        EnergyCardBonus = 15,
        BuildDestroySmoke = 16,
        ChestWinEnergy = 17,
        MoneyWithPoofFast = 18,
        SafeOpen = 19,
        MegaSafeMoney = 20,
        PoofShining = 21,
        DuckHuntPartyFirework = 22,
        BombEffect = 23,
        MiniSlotEnergyAndCoins = 24,
        MiniSlotEnergy = 25,
        MiniSlotCoins = 26,
        HighBuildCloud = 27,
        WideBuildCloud = 28,
        SuccessBuildingEffect = 29,
        AutoBattleMergeEffect = 30,
        AutoBattleSwordAttackEffect = 31,
        AutoBattleSwordComboAttackEffect = 32,
        AutoBattleDoubleSwordsComboAttackEffect = 33,
        BrokenBuildSmoke = 34,
        TournamentProgressBarRewardEffect = 35,
        EventButtonCompleteEffect = 36,
        CompleteFlashEffect = 37,
        EventButtonCompleteCircleEffect = 38,
    }
}