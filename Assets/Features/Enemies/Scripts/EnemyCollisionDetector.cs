using System;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    public class EnemyCollisionDetector : MonoBehaviour
    {
        public Action OnCollisionEnterEvent;

        private EnemyFacade _enemyFacade;

        private void Awake() =>
            _enemyFacade = GetComponent<EnemyFacade>();
        
        private void OnTriggerEnter(Collider other)
        {
            var characterFacade = other.GetComponentInParent<CharacterFacade>();
            if (characterFacade != null)
                OnCollisionEnterEvent?.Invoke();
        }
        
        private void OnCollisionEnter(Collision other)
        {
            var characterFacade = other.collider.GetComponentInParent<CharacterFacade>();
            if (characterFacade == null)
                return;

            // Let dash attacks apply their hit and knockback before the shared contact hit.
            OnCollisionEnterEvent?.Invoke();
            ApplyContactDamage(characterFacade, other.collider);
        }

        private void OnCollisionStay(Collision other)
        {
            var characterFacade = other.collider.GetComponentInParent<CharacterFacade>();
            if (characterFacade != null)
                ApplyContactDamage(characterFacade, other.collider);
        }

        private void ApplyContactDamage(CharacterFacade character, Collider characterCollider)
        {
            if (!isActiveAndEnabled || _enemyFacade == null || !_enemyFacade.isActiveAndEnabled ||
                _enemyFacade.HealthSystem == null || _enemyFacade.IsDead ||
                character.HealthSystem == null || characterCollider != character.Collider)
                return;

            EnemyConfiguration configuration = _enemyFacade.Configuration;
            // Ricochet hazards already own swept contact damage and its cooldown.
            if (configuration == null || configuration.EnemyDamageType == EnemyDamageType.Ricochet)
                return;

            character.ReceiveDamage(configuration.Damage, _enemyFacade);
        }
    }
}
