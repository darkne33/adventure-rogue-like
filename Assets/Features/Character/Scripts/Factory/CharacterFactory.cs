using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class CharacterFactory : ICharacterFactory
{
    private readonly DiContainer _container;
    private readonly CharacterConfiguration _characterConfiguration;
    
    public CharacterFactory(DiContainer container, CharacterConfiguration characterConfiguration)
    {
        _container = container;
        _characterConfiguration = characterConfiguration;
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
        return character;
    }
}
