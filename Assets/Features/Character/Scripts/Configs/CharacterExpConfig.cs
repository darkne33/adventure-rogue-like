using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/CharacterExpConfig",
    fileName = "CharacterExpConfig", order = 0)]
public class CharacterExpConfig : ScriptableObject
{
    [SerializeField] private SerializedDictionary<int, int> _maxExpiriences = new();

    public int MaxLevel => _maxExpiriences.Count == 0 ? 1 : _maxExpiriences.Keys.Max();

    public int GetMaxExpByLevel(int level) =>
        _maxExpiriences[level];

    public bool TryGetMaxExpByLevel(int level, out int maxExperience) =>
        _maxExpiriences.TryGetValue(level, out maxExperience);
}
