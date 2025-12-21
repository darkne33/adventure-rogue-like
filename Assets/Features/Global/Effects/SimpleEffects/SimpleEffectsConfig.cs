using System.Threading;
using CustomPackages.Package.Extensions.Other;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    [CreateAssetMenu(menuName = "Data/Effects/SimpleEffects")]
    public class SimpleEffectsConfig : ScriptableObject
    {
        [field: SerializeField] public AddressableLoadContainerGameObject PileOfAsh { get; private set; }
        
        public UniTask Load(CancellationToken cts) => 
            PileOfAsh.Load(cts);

        public void Validate()
        {
            PileOfAsh.Validate(name);
        }
    }
}