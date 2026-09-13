using UnityEngine;

namespace Features.Bosses.Scripts
{
    public interface IBossFactory
    {
        BossFacade Create(BossFacade prefab, Vector3 position, Quaternion rotation);
    }
}
