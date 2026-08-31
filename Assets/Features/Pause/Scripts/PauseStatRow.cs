using TMPro;
using UnityEngine;

public sealed class PauseStatRow : MonoBehaviour
{
    [SerializeField] private TMP_Text _valueText;

    public void SetValue(string value) =>
        _valueText.text = value;
}
