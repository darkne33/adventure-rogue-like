using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace UI
{
    public class PanelLocationsMonoComponent : MonoBehaviour
    {
        [SerializedDictionary] public SerializedDictionary<PanelLocation, Transform> PanelLocationRoots;
    }
}