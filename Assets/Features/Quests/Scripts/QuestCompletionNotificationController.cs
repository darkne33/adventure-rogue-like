using System;
using System.Collections.Generic;
using Features.Sounds;
using UnityEngine;
using Zenject;

namespace Features.Quests.Scripts
{
    public sealed class QuestCompletionNotificationController : ITickable, IDisposable
    {
        public const string Address = "Assets/Features/Quests/Prefabs/QuestCompletionNotification.prefab";

        private const float AppearDuration = 0.2f;
        private const float HoldDuration = 4f;
        private const float FadeDuration = 0.25f;
        private const float GapDuration = 0.15f;
        private const float SlideDistance = 32f;

        private readonly QuestService _quests;
        private readonly ISoundsService _sounds;
        private readonly Transform _root;
        private QuestCompletionNotificationView _view;
        private readonly Queue<QuestDefinition> _pending = new();
        private bool _showing;
        private bool _disposed;
        private float _elapsed;
        private float _gapRemaining;

        public QuestCompletionNotificationController(QuestService quests, ISoundsService sounds,
            Transform root)
        {
            _quests = quests;
            _sounds = sounds;
            _root = root;
        }

        public void Initialize(QuestCompletionNotificationView prefab)
        {
            if (_disposed || _view != null)
                return;
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            _view = UnityEngine.Object.Instantiate(prefab, _root, false);
            _view.Hide();
            // Only new completion events are shown; loading a save never replays old quests.
            _quests.QuestCompleted += HandleQuestCompleted;
        }

        public void Tick()
        {
            if (_disposed || _view == null || !Application.isFocused)
                return;

            float deltaTime = Time.unscaledDeltaTime;
            if (!_showing)
            {
                _gapRemaining = Mathf.Max(0f, _gapRemaining - deltaTime);
                if (_pending.Count > 0 && _gapRemaining <= 0f)
                    ShowNext();
                return;
            }

            // Room slow motion and level-up pauses must not stretch or stall a notification.
            _elapsed += deltaTime;
            if (_elapsed < AppearDuration)
            {
                float progress = Mathf.Clamp01(_elapsed / AppearDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                _view.SetPresentation(eased, SlideDistance * (1f - eased));
            }
            else if (_elapsed < AppearDuration + HoldDuration)
            {
                _view.SetPresentation(1f, 0f);
            }
            else if (_elapsed < AppearDuration + HoldDuration + FadeDuration)
            {
                float progress = (_elapsed - AppearDuration - HoldDuration) / FadeDuration;
                _view.SetPresentation(1f - progress, SlideDistance * 0.5f * progress);
            }
            else
            {
                _view.Hide();
                _showing = false;
                _gapRemaining = GapDuration;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _quests.QuestCompleted -= HandleQuestCompleted;
            _pending.Clear();
            if (_view != null)
                UnityEngine.Object.Destroy(_view.gameObject);
        }

        private void HandleQuestCompleted(QuestDefinition quest)
        {
            if (!_disposed && quest != null)
                _pending.Enqueue(quest);
        }

        private void ShowNext()
        {
            QuestDefinition quest = _pending.Dequeue();
            UnlockDefinition unlock = _quests.GetUnlockForQuest(quest.Id);
            Sprite icon = unlock != null ? unlock.Icon : null;
            if (icon == null)
                icon = unlock != null && unlock.Category == ProgressionCategory.Characters
                    ? _quests.Configuration.PortraitPlaceholder
                    : _quests.Configuration.SilverIcon;

            string reward = quest.SilverReward > 0
                ? $"+{quest.SilverReward} silver - claim in QUESTS"
                : string.Empty;
            if (unlock != null && !_quests.IsOwned(unlock))
            {
                if (reward.Length > 0)
                    reward += "\n";
                reward += $"{unlock.DisplayName} - buy in UNLOCKS";
            }

            _view.SetContent(quest.Title, quest.Description, reward, icon);
            _view.SetPresentation(0f, SlideDistance);
            _elapsed = 0f;
            _showing = true;
            _sounds.Play(SoundId.QuestComplete);
        }
    }
}
