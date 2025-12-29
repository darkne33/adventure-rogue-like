using UnityEngine;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : MonoBehaviour
    {
        [Inject] private ICharacterProvider _characterProvider;

        public void Update()
        {
            Vector3 direction = _characterProvider.CharacterFacade.transform.position - transform.position;
            
            direction.y = 0f;

            float distance = direction.magnitude;
            
            if (distance <= 10)
            {
                return;
            }
            
            Vector3 moveDirection = direction.normalized;
            Vector3 newPosition = transform.position + moveDirection * 10 * Time.deltaTime;
            
            transform.position = newPosition;
        }
    }
}