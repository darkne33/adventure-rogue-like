using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Character/CharacterExpConfig",
    fileName = "CharacterExpConfig", order = 0)]
public class CharacterExpConfig : ScriptableObject
{
    [SerializeField] private SerializedDictionary<int, int> _maxExpiriences = new();
    [SerializeField, Min(1)] private int _linearFormulaStartLevel = 20;
    [SerializeField, Min(1)] private int _linearFormulaExpPerLevel = 4;

    public int MaxLevel => int.MaxValue;
    public int MaxConfiguredLevel => _maxExpiriences.Count == 0 ? 1 : _maxExpiriences.Keys.Max();
    public int LinearFormulaStartLevel => Mathf.Max(1, _linearFormulaStartLevel);
    public int LinearFormulaExpPerLevel => Mathf.Max(1, _linearFormulaExpPerLevel);

    public int GetMaxExpByLevel(int level) =>
        TryGetMaxExpByLevel(level, out int maxExperience) ? maxExperience : 1;

    public bool TryGetMaxExpByLevel(int level, out int maxExperience)
    {
        if (level < 1)
        {
            maxExperience = 0;
            return false;
        }

        if (_maxExpiriences.TryGetValue(level, out maxExperience))
        {
            maxExperience = Mathf.Max(1, maxExperience);
            return true;
        }

        maxExperience = GetLinearMaxExpByLevel(level);
        return true;
    }

    private int GetLinearMaxExpByLevel(int level)
    {
        int anchorLevel = GetLinearFormulaAnchorLevel();
        int anchorExperience = _maxExpiriences.TryGetValue(anchorLevel, out int configuredExperience)
            ? Mathf.Max(1, configuredExperience)
            : 1;
        int levelOffset = Mathf.Max(0, level - anchorLevel);
        long maxExperience = (long)anchorExperience + (long)levelOffset * LinearFormulaExpPerLevel;

        return maxExperience >= int.MaxValue ? int.MaxValue : (int)maxExperience;
    }

    private int GetLinearFormulaAnchorLevel()
    {
        if (_maxExpiriences.Count == 0)
            return LinearFormulaStartLevel;

        if (_maxExpiriences.ContainsKey(LinearFormulaStartLevel))
            return LinearFormulaStartLevel;

        return MaxConfiguredLevel;
    }
}
