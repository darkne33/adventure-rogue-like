using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class GameDebugPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private UnityEngine.UI.Button[] _buttons;
    [SerializeField] private TMP_Text[] _labels;
    private GameDebugService _service;
    private readonly List<(string label, Func<string> action)> _entries = new();
    private int _tab, _page;
    private string _result = "F2 / Escape: close";
    public void Initialize(GameDebugService service)
    {
        _service = service;
        for (int i = 0; i < _buttons.Length; i++)
        {
            int index = i;
            _buttons[i].onClick.AddListener(() => Click(index));
        }
        Refresh();
    }
    private void Click(int index)
    {
        try
        {
            if (index < 4) { _tab = index; _page = 0; }
            else if (index == 4) _page = Math.Max(0, _page - 1);
            else if (index == 5) _page = Math.Min(Math.Max(0, (_entries.Count - 1) / 12), _page + 1);
            else
            {
                int entry = _page * 12 + index - 6;
                if (entry < _entries.Count) _result = _entries[entry].action();
            }
        }
        catch (Exception error) { _result = error.Message; Debug.LogException(error); }
        Refresh();
    }
    public void Refresh()
    {
        if (_service == null) return;
        _entries.Clear();
        _service.PopulateDebugMenu(_tab, _entries);
        _page = Math.Min(_page, Math.Max(0, (_entries.Count - 1) / 12));
        _title.text = $"DEBUG  |  F2 / Escape  |  Page {_page + 1}/{Math.Max(1, (_entries.Count + 11) / 12)}";
        _status.text = _service.DebugStatus + "\n" + _result;
        string[] tabs = { "Character / Run", "Relics", "Room prefabs", "Bosses / Enemies", "< Previous", "Next >" };
        for (int i = 0; i < _buttons.Length; i++)
        {
            int entry = _page * 12 + i - 6;
            bool visible = i < 6 || entry < _entries.Count;
            _buttons[i].gameObject.SetActive(visible);
            if (visible) _labels[i].text = i < 6 ? tabs[i] : _entries[entry].label;
        }
    }
}
