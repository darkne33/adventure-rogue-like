using System;
using System.Collections.Generic;
using Features.Relics.Scripts;
using UnityEngine;

namespace Features.Quests.Scripts
{
    [CreateAssetMenu(menuName = "Configs/Progression/Quests and Unlocks", fileName = "DemoProgressionConfiguration")]
    public sealed class ProgressionConfiguration : ScriptableObject
    {
        public const string ResourcePath = "Progression/DemoProgressionConfiguration";

        [Header("Available from the start")]
        [SerializeField] private string[] _defaultCharacters = { "rabbit" };
        [SerializeField] private AbilityName[] _defaultAbilities = Array.Empty<AbilityName>();
        [SerializeField] private RelicDefinition[] _defaultRelics = Array.Empty<RelicDefinition>();

        [Header("Quests and purchasable unlocks — stable IDs are used in saves")]
        [SerializeField] private QuestDefinition[] _quests = Array.Empty<QuestDefinition>();
        [SerializeField] private UnlockDefinition[] _unlocks = Array.Empty<UnlockDefinition>();

        [Header("Fallback content icons — window styling lives in prefabs")]
        [SerializeField] private Sprite _silverIcon;
        [SerializeField] private Sprite _portraitPlaceholder;

        public IReadOnlyList<string> DefaultCharacters => _defaultCharacters ?? Array.Empty<string>();
        public IReadOnlyList<AbilityName> DefaultAbilities => _defaultAbilities ?? Array.Empty<AbilityName>();
        public IReadOnlyList<RelicDefinition> DefaultRelics => _defaultRelics ?? Array.Empty<RelicDefinition>();
        public IReadOnlyList<QuestDefinition> Quests => _quests ?? Array.Empty<QuestDefinition>();
        public IReadOnlyList<UnlockDefinition> Unlocks => _unlocks ?? Array.Empty<UnlockDefinition>();
        public Sprite SilverIcon => _silverIcon;
        public Sprite PortraitPlaceholder => _portraitPlaceholder;
    }
}
