using Features.Leaderboard;
using Features.Quests.Scripts;
using UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuPanel : PanelBase
{
    [field: SerializeField] public Button PlayButton { get; private set; }
    [field: SerializeField] public Button QuestsButton { get; private set; }
    [field: SerializeField] public Button UnlocksButton { get; private set; }
    [field: SerializeField] public Button SettingsButton { get; private set; }
    [field: SerializeField] public Button ExitButton { get; private set; }
    [field: SerializeField] public CharacterSelectionView CharacterSelection { get; private set; }
    [field: SerializeField] public LeaderboardView Leaderboard { get; private set; }
    [field: SerializeField] public QuestsPanelView QuestsPanelPrefab { get; private set; }
    [field: SerializeField] public UnlocksPanelView UnlocksPanelPrefab { get; private set; }

    [SerializeField] private CharacterConfiguration _characterConfiguration;
    [SerializeField] private GameObject _titleLogo;
    [SerializeField] private MainMenuRoomController _roomPrefab;

    private MainMenuRoomController _room;

    public CharacterConfiguration CharacterConfiguration => _characterConfiguration;
    public MainMenuRoomController Room => _room;

    public void CreateRoom()
    {
        if (_room == null)
            _room = MainMenuRoomController.CreateInstance(_roomPrefab, (RectTransform)transform);
    }

    public void CloseRoom()
    {
        if (_room == null)
            return;

        _room.Close();
        _room = null;
    }

    private void OnDestroy() => CloseRoom();

    public void SetHomeVisible(bool visible)
    {
        PlayButton.transform.parent.gameObject.SetActive(visible);

        if (_titleLogo != null)
            _titleLogo.SetActive(visible);

        if (Leaderboard != null)
            Leaderboard.gameObject.SetActive(visible);

        if (ExitButton != null)
            ExitButton.gameObject.SetActive(visible);
    }

    public void SetButtonsInteractable(bool interactable)
    {
        PlayButton.interactable = interactable;
        QuestsButton.interactable = interactable;
        UnlocksButton.interactable = interactable;
        SettingsButton.interactable = interactable;
        ExitButton.interactable = interactable;
    }
}
