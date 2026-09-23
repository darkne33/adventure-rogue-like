using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    public sealed class ProgressionSelectionHandler : UIBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private GameObject _frame;

        public void OnSelect(BaseEventData eventData) => SetVisible(true);

        public void OnDeselect(BaseEventData eventData) => SetVisible(false);

        protected override void OnEnable()
        {
            base.OnEnable();
            SetVisible(EventSystem.current != null &&
                       EventSystem.current.currentSelectedGameObject == gameObject);
        }

        protected override void OnDisable()
        {
            SetVisible(false);
            base.OnDisable();
        }

        private void SetVisible(bool visible)
        {
            if (_frame != null)
                _frame.SetActive(visible);
        }
    }
}
