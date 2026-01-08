using Unity.AI.Navigation;
using UnityEngine;

public class LevelView : MonoBehaviour
{
    [SerializeField] private NavMeshSurface _navMeshSurface;
    
    public void BakeNavMeshSurface() => 
        _navMeshSurface.BuildNavMesh();
}
