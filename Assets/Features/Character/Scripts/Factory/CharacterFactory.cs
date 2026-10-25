using System.Threading;
using Core;
using System;
using Cysharp.Threading.Tasks;
using Features.PickupFeedback;
using UnityEngine;
using Zenject;

public class CharacterFactory : ICharacterFactory
{
    private readonly DiContainer _container;
    private readonly CharacterConfiguration _characterConfiguration;
    private readonly GameplayAssetService _gameplayAssets;
    
    public CharacterFactory(DiContainer container, CharacterConfiguration characterConfiguration,
        GameplayAssetService gameplayAssets)
    {
        _container = container;
        _characterConfiguration = characterConfiguration;
        _gameplayAssets = gameplayAssets;
    }
    
    public async UniTask<CharacterFacade> CreatePlayer(Transform spawnPoint, CancellationToken cancellationToken)
    {
        CharacterDefinition definition = _characterConfiguration.GetConfiguredSelectedCharacter();
        var characterContainer = definition.CharacterContainer;
        await characterContainer.Load(cancellationToken);
        var character =
            _container.InstantiatePrefabForComponent<CharacterFacade>(characterContainer.Get(), spawnPoint);
        character.Definition = definition;
        character.transform.SetParent(null, true);
        character.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        CharacterDeathPresentation deathPresentation = character.DeathPresentation;
        if (deathPresentation == null || deathPresentation.SoulPrefab == null)
            throw new InvalidOperationException($"Character '{definition.Id}' is missing its prepared death presentation.");

        CharacterDeathSoulView deathSoul =
            _container.InstantiatePrefabForComponent<CharacterDeathSoulView>(
                deathPresentation.SoulPrefab.gameObject, character.transform);
        deathPresentation.Initialize(character, deathSoul);

        PickupFeedbackConfiguration feedbackConfiguration = _gameplayAssets.PickupFeedback;
        CharacterPickupFeedbackView pickupFeedback =
            _container.InstantiatePrefabForComponent<CharacterPickupFeedbackView>(
                feedbackConfiguration.ViewPrefab.gameObject, character.transform);
        pickupFeedback.Initialize(character, feedbackConfiguration);
        character.SetPickupFeedback(pickupFeedback);
        return character;
    }
}
