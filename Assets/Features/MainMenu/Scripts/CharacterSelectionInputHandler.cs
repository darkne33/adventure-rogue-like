using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CharacterSelectionInputHandler : UIBehaviour, IMoveHandler, ICancelHandler
{
    [SerializeField] private CharacterSelectionView _view;

    public void OnMove(AxisEventData eventData) =>
        _view?.HandleMove(eventData);

    public void OnCancel(BaseEventData eventData) =>
        _view?.RequestBackFromInput(eventData);
}
