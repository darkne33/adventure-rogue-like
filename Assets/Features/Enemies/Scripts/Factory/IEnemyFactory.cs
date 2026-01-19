using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IEnemyFactory
{
    public UniTask<CharacterFacade> CreatePlayer(Transform spawnPoint, CancellationToken cancellationToke);
}