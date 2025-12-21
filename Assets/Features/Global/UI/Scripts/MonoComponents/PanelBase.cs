using CustomPackages.Package.Extensions.Other;
using NaughtyAttributes;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup)),
     RequireComponent(typeof(IDMonoComponent))]
    public abstract class PanelBase : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public PanelName PanelName { get; private set; }

        private void OnValidate()
        {
            if (PanelName == PanelName.None)
            {
                PanelName = (PanelName)GetComponent<IDMonoComponent>().Id;
            }
        }
    }
}