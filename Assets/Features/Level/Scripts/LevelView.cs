using UnityEngine;

public class LevelView : MonoBehaviour
{
    [field: SerializeField] public LevelDoor MainDoor { get; private set; }
    [field: SerializeField] public Room StartRoom { get; private set; }
}