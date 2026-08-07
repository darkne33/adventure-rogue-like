using Features.Leaderboard;
using UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuPanel : PanelBase
{
    [field: SerializeField] public Button PlayButton { get; private set; }
    [field: SerializeField] public Button QuestsButton { get; private set; }
    [field: SerializeField] public Button UnlocksButton { get; private set; }
    [field: SerializeField] public Button SettingsButton { get; private set; }

    [SerializeField] private LeaderboardView _leaderboardPrefab;

    public LeaderboardView Leaderboard { get; private set; }

    private void Awake() =>
        EnsureLeaderboardView();

    public LeaderboardView EnsureLeaderboardView()
    {
        if (Leaderboard != null)
            return Leaderboard;

        Leaderboard = Instantiate(_leaderboardPrefab, transform);
        return Leaderboard;
    }

    public void SetButtonsInteractable(bool interactable)
    {
        PlayButton.interactable = interactable;
        QuestsButton.interactable = interactable;
        UnlocksButton.interactable = interactable;
        SettingsButton.interactable = interactable;
    }
}
