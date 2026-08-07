using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Leaderboard
{
    public sealed class LeaderboardRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _playerText;
        [SerializeField] private TMP_Text _roomsText;
        [SerializeField] private Image _background;
        [SerializeField] private Color _normalBackground =
            new(0.035f, 0.075f, 0.06f, 0.42f);
        [SerializeField] private Color _alternateBackground =
            new(0.07f, 0.13f, 0.105f, 0.52f);
        [SerializeField] private Color _currentPlayerBackground =
            new(0.16f, 0.38f, 0.29f, 0.72f);
        [SerializeField] private Color _normalText =
            new(0.9f, 0.93f, 0.84f, 1f);
        [SerializeField] private Color _currentPlayerText =
            new(1f, 0.84f, 0.35f, 1f);

        public void Show(int rank, string playerName, int rooms,
            bool isCurrentPlayer, bool isAlternate)
        {
            _rankText.text = rank.ToString();
            _playerText.text = playerName;
            _roomsText.text = rooms.ToString();
            _background.color = isCurrentPlayer
                ? _currentPlayerBackground
                : isAlternate
                    ? _alternateBackground
                    : _normalBackground;

            Color textColor = isCurrentPlayer ? _currentPlayerText : _normalText;
            _rankText.color = textColor;
            _playerText.color = textColor;
            _roomsText.color = textColor;
        }
    }
}
