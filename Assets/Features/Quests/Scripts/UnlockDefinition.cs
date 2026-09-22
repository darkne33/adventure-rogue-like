using System;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.Quests.Scripts
{
    public enum ProgressionCategory
    {
        Characters = 0,
        Weapons = 1,
        Scrolls = 2,
        Relics = 3,
        General = 4
    }

    [Serializable]
    public sealed class UnlockDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private ProgressionCategory _category;
        [SerializeField] private string _characterId;
        [SerializeField] private AbilityConfiguration _ability;
        [SerializeField] private RelicDefinition _relic;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _unlockedByDefault;
        [SerializeField] private string _requiredQuestId;
        [SerializeField, Min(0)] private int _silverCost = 1;

        public string Id => _id;
        public ProgressionCategory Category => _category;
        public string CharacterId => _characterId;
        public AbilityConfiguration Ability => _ability;
        public RelicDefinition Relic => _relic;
        public bool UnlockedByDefault => _unlockedByDefault;
        public string RequiredQuestId => _requiredQuestId;
        public int SilverCost => Mathf.Max(0, _silverCost);
        public string DisplayName => !string.IsNullOrWhiteSpace(_displayName) ? _displayName
            : _ability != null ? _ability.DisplayName : _relic != null ? _relic.DisplayName : _characterId;
        public string Description => !string.IsNullOrWhiteSpace(_description) ? _description
            : _ability != null ? _ability.Description : _relic != null ? _relic.Description : string.Empty;
        public Sprite Icon => _icon != null ? _icon
            : _ability != null ? _ability.Icon : _relic != null ? _relic.Icon : null;
    }
}
