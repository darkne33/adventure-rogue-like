namespace Core.Sounds
{
    public enum AudioClipName
    {
        //Attack Game mode 1-300
        AttackGameModeAmbient = 1,
        AttackRadioSpawn = 2,
        AttackAirshipFlyAway = 3,
        AttackDropBomb = 4,
        AttackExplosionBombVictory = 5,
        AttackExplosionBombLose = 6,
        AttackCardCombo = 7,
        AttackAnnouncement = 8,
        AttackArmageddon = 9,
        AttackCoefficientJump = 10,
        
        //Robbery Game mode 301 - 600
        RobberyGameModeAmbient = 301,
        RobberyDragonLose = 302,
        RobberyDragonVictory = 303,
        RobberyGateOpen = 304,
        RobberyComplete = 305,
        RobberyAnnouncement = 306,
        RobberyCoefficientJump = 307,
        
        //RoyalHunt Game mode 601 - 900
        RoyalHuntGameModeAmbient = 601,
        RoyalHuntHitEmptyDuck = 602,
        RoyalHuntHitRewardDuck = 603,
        RoyalHuntHit = 604,
        RoyalHuntMissDuckDown = 605,
        RoyalHuntRewardDuckDown = 606,
        RoyalHuntAnnouncement = 607,
        RoyalHuntBrokenDuckUI = 608,
        RoyalCoefficientJump = 609,

        //Build meta 901 = 1200
        BuildMetaUpgrade = 901,
        BuildMetaSpawnBuild = 902,
        BuildMetaMoveCrown = 903,
        BuildMetaComplete = 904,
        BuildMetaMoveInMap = 905,
        BuildMetaOpenBiomeInMap = 906,
         
        //Mini Slot Ga mode 1201 = 1500
        MiniSlotAmbient = 1201,
        MiniSlotMultiplierButton = 1202,
        MiniSlotMainRewardJoker = 1203,
        MiniSlotMainRewardCoins = 1204,
        MiniSlotMainRewardEnergy = 1205,
        MiniSlotSpinButton = 1206,
        MiniSlotSpinning = 1207,
        MiniSlotStopSpinning = 1208,
        MiniSlotTakeWinButton = 1209,
        MiniSlotEnergyProgress = 1210,
        MiniSlotCoinsProgress = 1211,
        MiniSlotEntryBonus = 1212,
        
        //Main Game Play 1501 - 1800
        MainGamePlayAmbient = 1501,
        MainGamePlayMoveCollectEnergy = 1504,
        MainGamePlayClickCommon = 1511,
        MainGamePlayCollectCoins = 1512,
        MainGamePlayMoveCollectCoins = 1513,
        MainGamePlayClouds = 1514,
        MainGamePlayChestOpen = 1515,
        MainGamePlayJingle = 1516,
        MainGamePlayResourceCollect = 1517,
        MainGamePlayOfferMovePlaces = 1518,
        MainGamePlayOfferOpenLock = 1519,
        MainGamePlayOpenOfferEventsPanels = 1520,
        MainGamePlayShopPurchaseNotify = 1521,
        MainGamePlayExitPanel = 1522,
        MainGamePlayChestCardShow = 1523,
        MainGamePlayToolTip = 1524,
        
        //Plinko Game mode events 1801 - 2100
        PlinkoAmbient = 1801,
        PlinkoBallSpawn = 1802,
        PlinkoBonusComplete = 1803,
        PlinkoBallDropInBucket = 1804,
        PlinkoHitBonus = 1805,
        PlinkoHitBoost = 1806,
        PlinkoHitCommon = 1807,
        PlinkoOpenBag = 1808,
        PlinkoOpenGameMode = 1809,
        PlinkoBallDropInCenterHole = 1810,
        PlinkoCurrencyCollect = 1811,
        PlinkoBoostActivated = 1812,
        PlinkoProgressReward = 1813,
        PlinkoCurrencyCollectHalf = 1814,
        
        //Achievements 2101 - 2400
        AchievementsDaySwap = 2101,
        AchievementProgressBarRewardCollect = 2102,
        
        //Kingdom advancement 2401 - 2700
        KingdomAdvancementCollectCard = 2401,
        
        //AutoBattle GameMode event 2701- 3000
        AutoBattleAttackDoubleSwords = 2701,
        AutoBattleAttackRegular = 2702,
        AutoBattleAttackSingleSword = 2703,
        AutoBattleBigWin = 2704,
        AutoBattleBlockerBushDestruction = 2705,
        AutoBattleBlockerBushTap0 = 2706,
        AutoBattleBlockerBushTap1 = 2707,
        AutoBattleCardCoins = 2708,
        AutoBattleCardCoinsCollect = 2709,
        AutoBattleCardSword = 2710,
        AutoBattleEnemyAttack = 2711,
        AutoBattleEnemyBushGrowth = 2712,
        AutoBattleMusic = 2713,
        AutoBattleNextLevel = 2714,
        AutoBattleWheelPottyDeath = 2715,
        AutoBattleWheelPottyMoneyCollect = 2716,
        AutoBattleWheelPottySpawnMoney = 2717,
        AutoBattleStack = 2718,
        AutoBattleWheelPottySpinActivate = 2719,
        AutoBattleWheelPottySpinWin = 2720,
        AutoBattleOpenBag = 2721,
        AutoBattlePumpButton = 2722,
        AutoBattleFire = 2723,
        
        //BotsAttack 3001 - 3100
        BotsAttackAlertRobbery = 3001,
        BotsAttackAlertAttack = 3002,
        BotsAttackAlertShield = 3003,
        
        //BlackJack 3101 - 3400
        BlackJackBigWin = 3101,
        BlackJackPlayMultiplierButton = 3102,
        BlackJackCardCollectEnergy = 3103,
        BlackJackCardComboEnergy = 3104,
        BlackJackPlayCollectShield = 3105,
        BlackJackCardComboShield = 3106,
        BlackJackStartMoveCard = 3107,
        BlackJackEndMoveCard = 3108,
        BlackJackShuffleCards = 3109,
        BlackJackFoldCards = 3110,
        BlackJackMergeMegaShields = 3111,
        BlackJackMergeJumpEnergy = 3112,
        
        //DailyCalendar 3401 - 3700
        DailyCalendarAnnounce = 3401,
        DailyCalendarProgressReward = 3402,
        DailyCalendarCollect = 3403,
        DailyCalendarProgressMovement = 3404,
        
        //Tournament 3701 - 4001
        TournamentCurrencyCollect = 3701,
        TournamentCurrencySpawn = 3702,
        TournamentCurrencyCollectLast = 3703,
        TournamentProgressBarMovement = 3704,
        TournamentProgressReward = 3705,
        
        //Kingdom Boom 4001 - 4100
        KingdomBoomButtonActivation = 4001,
        
        //Magic Number 4101 - 4200
        MagicNumberButtonActivation = 4101,
        MagicNumberCollectSpawn = 4102,
        MagicNumberCollect = 4103,
        MagicNumberProgressBarMovement = 4104,
        MagicNumberProgressReward = 4105,
        MagicNumberCollectLast = 4106,
        MagicNumberFinalEventProgressBarMovement = 4107,
        
        //Event Bar 4201 - 4500
        EventBarProgressMovement = 4201,
        EventBarProgressReward = 4202,
        
        //Fish Rush 4501 - 4600
        FishRushPath = 4501,
        FishRushReward = 4502,
        FishRushCurrencyCollectOne = 4503,
        FishRushCurrencyCollectALot = 4504,
        FishRushCurrencySpawn = 4505,
        FishRushIconActivation = 4506,
    }
}