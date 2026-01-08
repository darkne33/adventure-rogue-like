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
        await _characterConfiguration.CharacterContainer.Load(cancellationToken);
        var character =
            _container.InstantiatePrefabForComponent<CharacterFacade>(_characterConfiguration.CharacterContainer.Get(), spawnPoint);
        return character;
    }
}