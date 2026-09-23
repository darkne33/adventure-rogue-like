using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.RunResults.Scripts
{
    public sealed class RunResultsData
    {
        public float SurvivedSeconds { get; }
        public int LevelReached { get; }
        public int EnemiesDefeated { get; }
        public int GoldEarned { get; }
        public int SilverEarned { get; }
        public IReadOnlyList<RunWeaponResult> Weapons { get; }

        public RunResultsData(float survivedSeconds, int levelReached, int enemiesDefeated,
            int goldEarned, int silverEarned, IReadOnlyList<RunWeaponResult> weapons)
        {
            SurvivedSeconds = Mathf.Max(0f, survivedSeconds);
            LevelReached = Math.Max(1, levelReached);
            EnemiesDefeated = Math.Max(0, enemiesDefeated);
            GoldEarned = Math.Max(0, goldEarned);
            SilverEarned = Math.Max(0, silverEarned);
            Weapons = new List<RunWeaponResult>(weapons).AsReadOnly();
        }
    }
}
