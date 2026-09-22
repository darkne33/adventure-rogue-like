using System;
using UnityEngine.EventSystems;

namespace Features.Quests.Scripts
{
    public sealed class ProgressionSelectionHandler : UIBehaviour, ISelectHandler
    {
        public Action Selected { get; set; }

        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }
}
