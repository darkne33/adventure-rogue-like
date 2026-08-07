using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Features.Leaderboard
{
    public sealed class LeaderboardView : MonoBehaviour
    {
        [SerializeField] private Transform _rowsRoot;
        [SerializeField] private LeaderboardRowView _rowPrefab;
        [SerializeField] private TMP_Text _statusText;

        private readonly List<LeaderboardRowView> _rows = new();

        public void ShowLoading()
        {
            ClearRows();
            ShowStatus("LOADING...");
        }

        public void ShowEntries(IReadOnlyList<LeaderboardEntry> entries,
            string currentPlayerId)
        {
            ClearRows();

            if (entries == null || entries.Count == 0)
            {
                ShowStatus("NO PLAYERS YET");
                return;
            }

            _statusText.gameObject.SetActive(false);
            for (int index = 0; index < entries.Count; index++)
            {
                LeaderboardEntry entry = entries[index];
                bool isCurrentPlayer = string.IsNullOrEmpty(currentPlayerId) == false &&
                                       entry.PlayerId == currentPlayerId;
                LeaderboardRowView row = Instantiate(_rowPrefab, _rowsRoot);
                row.Show(entry.Rank, TruncateName(entry.PlayerName), entry.Score,
                    isCurrentPlayer, index % 2 != 0);
                _rows.Add(row);
            }
        }

        public void ShowError(string message)
        {
            ClearRows();
            ShowStatus(message);
        }

        private void ShowStatus(string message)
        {
            _statusText.text = message;
            _statusText.gameObject.SetActive(true);
        }

        private void ClearRows()
        {
            foreach (LeaderboardRowView row in _rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            _rows.Clear();
        }

        private static string TruncateName(string playerName)
        {
            const int maxLength = 18;
            if (string.IsNullOrWhiteSpace(playerName))
                return "PLAYER";

            string singleLineName = playerName
                .Replace('\r', ' ')
                .Replace('\n', ' ');
            return singleLineName.Length <= maxLength
                ? singleLineName
                : singleLineName.Substring(0, maxLength - 3) + "...";
        }
    }
}
