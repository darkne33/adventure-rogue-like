using System;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyCollisionDetector : MonoBehaviour
    {
        public Action OnCollisionEnterEvent;
        
        private void OnTriggerEnter(Collider other)
        {
            var characterFacade = other.GetComponent<CharacterFacade>();
            if (characterFacade != null)
                OnCollisionEnterEvent?.Invoke();
        }
        
        private void OnCollisionEnter(Collision other)
        {
            var characterFacade = other.gameObject.GetComponent<CharacterFacade>();
            if (characterFacade != null)
                OnCollisionEnterEvent?.Invoke();
        }
    }
}