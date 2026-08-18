using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiOutline = UnityEngine.UI.Outline;

public sealed class CharacterPortraitSlotView : MonoBehaviour
{
    [SerializeField] private CharacterSelectionView _owner;
    [SerializeField] private Button _button;
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private UiOutline _outline;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private LayoutElement _layout;

    [Header("Selection Style")]
    [SerializeField] private Color _normalOutlineColor = new(0.12f, 0.105f, 0.09f, 1f);
    [SerializeField] private Color _selectedOutlineColor = new(0.43f, 0.12f, 0.09f, 1f);
    [SerializeField] private Color _normalNameColor = new(0.12f, 0.105f, 0.09f, 1f);
    [SerializeField] private Color _selectedNameColor = new(0.43f, 0.12f, 0.09f, 1f);
    [SerializeField] private Vector2 _normalOutlineDistance = new(3f, -3f);
    [SerializeField] private Vector2 _selectedOutlineDistance = new(7f, -7f);
    [SerializeField] private float _selectedScale = 1.08f;

    private int _characterIndex;

    public void Bind(int characterIndex, CharacterDefinition character, Sprite portraitPlaceholder, float width)
    {
        _characterIndex = characterIndex;
        _layout.preferredWidth = width;
        _rectTransform.sizeDelta = new Vector2(width, _rectTransform.sizeDelta.y);
        _portrait.sprite = character.Portrait != null ? character.Portrait : portraitPlaceholder;
        _portrait.enabled = _portrait.sprite != null;
        _name.text = character.DisplayName.ToUpperInvariant();
        gameObject.SetActive(true);
    }

    public void Clear() =>
        gameObject.SetActive(false);

    public void SetInteractable(bool interactable) =>
        _button.interactable = interactable;

    public void SetSelected(bool selected)
    {
        _outline.enabled = selected;
        _outline.effectColor = selected ? _selectedOutlineColor : _normalOutlineColor;
        _outline.effectDistance = selected ? _selectedOutlineDistance : _normalOutlineDistance;
        _rectTransform.localScale = selected ? Vector3.one * _selectedScale : Vector3.one;
        _name.color = selected ? _selectedNameColor : _normalNameColor;
    }

    public void RequestSelection() =>
        _owner.RequestSelectionFromSlot(_characterIndex);
}
