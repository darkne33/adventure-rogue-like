using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Leaderboard
{
    public readonly struct LeaderboardEntry
    {
        public int Rank { get; }
        public string PlayerId { get; }
        public string PlayerName { get; }
        public int Score { get; }

        public LeaderboardEntry(int rank, string playerId, string playerName, int score)
        {
            Rank = rank;
            PlayerId = playerId;
            PlayerName = playerName;
            Score = score;
        }
    }

    public interface ILeaderboardService
    {
        bool IsConfigured { get; }
        string PlayerId { get; }
        string PlayerName { get; }
        string StatisticName { get; }

        UniTask<IReadOnlyList<LeaderboardEntry>> GetTop(CancellationToken cancellationToken);
        UniTask SetPlayerName(string playerName, CancellationToken cancellationToken);
        UniTask StartRun(CancellationToken cancellationToken);
        UniTask ReportRoomVisited(int roomSequence, CancellationToken cancellationToken);
    }
}
