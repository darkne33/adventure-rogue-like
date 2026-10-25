using System;
using System.Collections.Generic;

namespace Features.RunResults.Scripts
{
    public sealed class RunResultsData
    {
        public int RoomsVisited { get; }
        public int LevelReached { get; }
        public int EnemiesDefeated { get; }
        public int GoldEarned { get; }
        public int SilverEarned { get; }
        public IReadOnlyList<RunWeaponResult> Weapons { get; }

        public RunResultsData(int roomsVisited, int levelReached, int enemiesDefeated,
            int goldEarned, int silverEarned, IReadOnlyList<RunWeaponResult> weapons)
        {
            RoomsVisited = Math.Max(0, roomsVisited);
            LevelReached = Math.Max(1, levelReached);
            EnemiesDefeated = Math.Max(0, enemiesDefeated);
            GoldEarned = Math.Max(0, goldEarned);
            SilverEarned = Math.Max(0, silverEarned);
            Weapons = new List<RunWeaponResult>(weapons).AsReadOnly();
        }
    }
}
