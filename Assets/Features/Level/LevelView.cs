using Unity.AI.Navigation;
using UnityEngine;

public class LevelView : MonoBehaviour
{
    [SerializeField] private NavMeshSurface[] _navMeshSurfaces;
    
    public void BakeNavMeshSurface()
    {
        foreach (var navMeshSurface in _navMeshSurfaces)
        {
            navMeshSurface.BuildNavMesh();
        }
    }
}
