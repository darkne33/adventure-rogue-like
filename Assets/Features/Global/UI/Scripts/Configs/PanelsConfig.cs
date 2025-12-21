using System.Collections.Generic;
using CustomPackages.Package.Extensions.Other;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(menuName = "Configs/UI/Panels")]
    public class PanelsConfig : ScriptableObject
    {
        [field: SerializeField] public List<PanelData> GamePanelsAssetReferences { get; private set; }

        public void Validate()
        {
            foreach (var panelData in GamePanelsAssetReferences)
            {
                panelData.Validate(name);
            }
        }
    }
}