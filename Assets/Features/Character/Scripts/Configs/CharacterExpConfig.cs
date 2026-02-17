using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/CharacterExpConfig",
    fileName = "CharacterExpConfig", order = 0)]
public class CharacterExpConfig : ScriptableObject
{
    [SerializeField] private SerializedDictionary<int, int> _maxExpiriences = new SerializedDictionary<int, int>();

    public int GetMaxExpByLevel(int level) =>
        _maxExpiriences[level];
}