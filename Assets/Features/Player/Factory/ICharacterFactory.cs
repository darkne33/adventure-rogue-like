using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ICharacterFactory
{
    public UniTask<CharacterFacade> CreatePlayer(Transform spawnPoint, CancellationToken cancellationToke);
}