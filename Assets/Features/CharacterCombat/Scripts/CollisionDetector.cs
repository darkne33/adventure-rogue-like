using System;
using Features.Enemies.Scripts;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public Action<EnemyFacade> OnCollisionEnter;
        
    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyFacade>();
        if (enemy != null)
            OnCollisionEnter?.Invoke(enemy);
    }
}
