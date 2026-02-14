using System.Linq;
using System.Threading;
using AYellowpaper.SerializedCollections;
using CustomPackages.Package.Extensions.Other;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services
{
    [CreateAssetMenu(menuName = "Data/VFX collection")]
    public class EffectsConfig : ScriptableObject
    {
        [SerializedDictionary("Name", "Effect")]
        public SerializedDictionary<EffectName, AddressableLoadContainerEffectPlayer> Effects = new();

        public UniTask Load(CancellationToken cts) =>
            UniTask.WhenAll(Enumerable.Select(Effects.Values, effect => effect.Load(cts)).ToList());

        public void CleanUp()
        {
            foreach (var effect in Effects.Values)
                effect.CleanUp();
        }

        public void Validate()
        {
            Effects.Validate(name);
        }
    }

    public enum EffectName
    {
        EnemyPortal = 1,
    }
}