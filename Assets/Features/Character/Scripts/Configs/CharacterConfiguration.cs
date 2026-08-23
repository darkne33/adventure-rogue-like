using Core;
using Features.Relics.Scripts;
using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Configs/Character/PlayerConfiguration", fileName = "PlayerConfiguration", order = 0)]
public class CharacterConfiguration : ScriptableObject
{
    [SerializeField] private List<CharacterDefinition> _characters = new();
    [SerializeField, Min(0)] private int _defaultCharacterIndex;

    private int _selectedCharacterIndex = -1;

    public IReadOnlyList<CharacterDefinition> Characters => _characters;
    public bool HasCharacters => _characters is { Count: > 0 };

    public int SelectedCharacterIndex
    {
        get
        {
            EnsureCharactersConfigured();

            if (_selectedCharacterIndex < 0 || _selectedCharacterIndex >= _characters.Count)
                _selectedCharacterIndex = GetDefaultCharacterIndex();

            return _selectedCharacterIndex;
        }
    }

    public CharacterDefinition SelectedCharacter => _characters[SelectedCharacterIndex];
    public AddressableLoadContainerGameObject CharacterContainer => GetConfiguredSelectedCharacter().CharacterContainer;
    public CharacterSettingsConfiguration CharacterSettings => GetConfiguredSelectedCharacter().CharacterSettings;

    private void OnEnable() =>
        ResetSelectionToDefault();

    public void ResetSelectionToDefault() =>
        _selectedCharacterIndex = HasCharacters ? GetDefaultCharacterIndex() : -1;

    public void SelectCharacter(int index)
    {
        EnsureCharactersConfigured();

        if (index < 0 || index >= _characters.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Character index is outside the roster.");

        _selectedCharacterIndex = index;
    }

    public void ValidateRosterEntries()
    {
        EnsureCharactersConfigured();

        for (int i = 0; i < _characters.Count; i++)
        {
            if (_characters[i] == null)
                throw new InvalidOperationException($"{name} contains an empty character entry at index {i}.");
        }
    }

    public int GetWrappedCharacterIndex(int index)
    {
        EnsureCharactersConfigured();
        return (index % _characters.Count + _characters.Count) % _characters.Count;
    }

    public CharacterDefinition GetConfiguredSelectedCharacter()
    {
        CharacterDefinition character = SelectedCharacter;
        if (character == null)
            throw new InvalidOperationException($"{name} contains an empty character entry at index {SelectedCharacterIndex}.");

        if (!character.IsConfigured)
            throw new InvalidOperationException(character.ConfigurationError);

        return character;
    }

    private int GetDefaultCharacterIndex() =>
        Mathf.Clamp(_defaultCharacterIndex, 0, _characters.Count - 1);

    private void EnsureCharactersConfigured()
    {
        if (!HasCharacters)
            throw new InvalidOperationException($"{name} does not contain any configured characters.");
    }
}

[Serializable]
public sealed class CharacterDefinition
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea(2, 5)] private string _description;
    [SerializeField] private Sprite _portrait;
    [SerializeField] private AddressableLoadContainerGameObject _characterContainer = new();
    [SerializeField] private CharacterSettingsConfiguration _characterSettings;
    [SerializeField] private AbilityName _startingAbility;
    [SerializeField] private AbilityConfiguration _startingAbilityDetails;
    [SerializeField] private RelicDefinition _startingRelic;

    public string Id => _id;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
        ? string.IsNullOrWhiteSpace(_id) ? "UNNAMED" : _id
        : _displayName;
    public string Description => _description;
    public Sprite Portrait => _portrait;
    public AddressableLoadContainerGameObject CharacterContainer => _characterContainer;
    public CharacterSettingsConfiguration CharacterSettings => _characterSettings;
    public AbilityName StartingAbility => _startingAbility;
    public AbilityConfiguration StartingAbilityDetails => _startingAbilityDetails;
    public RelicDefinition StartingRelic => _startingRelic;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_id) &&
        _characterSettings != null &&
        _characterContainer?.AssetReference != null &&
        _characterContainer.AssetReference.RuntimeKeyIsValid() &&
        Enum.IsDefined(typeof(AbilityName), _startingAbility);

    public string ConfigurationError
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_id))
                return "Character entry does not have an ID.";

            if (_characterSettings == null)
                return $"Character '{DisplayName}' does not have Character Settings assigned.";

            if (_characterContainer?.AssetReference == null ||
                !_characterContainer.AssetReference.RuntimeKeyIsValid())
            {
                return $"Character '{DisplayName}' does not have a valid addressable prefab assigned.";
            }

            if (!Enum.IsDefined(typeof(AbilityName), _startingAbility))
                return $"Character '{DisplayName}' does not have a valid starting ability.";

            return string.Empty;
        }
    }
}
