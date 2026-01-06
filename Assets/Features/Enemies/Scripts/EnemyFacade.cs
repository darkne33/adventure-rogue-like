using System;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace Features.Enemies.Scripts
{
    public class EnemyFacade : MonoBehaviour
    {
        [Inject] private ICharacterProvider _characterProvider;

        [SerializeField] private EnemyConfiguration _configuration;
        
        private NavMeshAgent _navMeshAgent;
        
        private float _currentSpeed = 0f;
        
        private void Start()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void FixedUpdate()
        {
            MoveTowardsPlayerNonPhysics();
        }
        
        private void MoveTowardsPlayerNonPhysics()
        {
            if (_characterProvider.CharacterFacade == null)
                return;

            Vector3 direction = _characterProvider.CharacterFacade.transform.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            
            float desiredSpeed = 0f;

            if (distance > _configuration.DistanceToStop)
            {
                float effectiveDistance = distance - _configuration.DistanceToStop;
                if (effectiveDistance < _configuration.SmoothStopRange)
                {
                    desiredSpeed = _configuration.Speed * (effectiveDistance / _configuration.SmoothStopRange);
                }
                else
                {
                    desiredSpeed = _configuration.Speed;
                }
            }
            
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, desiredSpeed, _configuration.Acceleration * Time.deltaTime);

            if (_currentSpeed > 0.01f && distance > 0.01f)
            {
                _navMeshAgent.SetDestination(_characterProvider.CharacterFacade.transform.position);
                Rotation();
            }
        }

        private void Rotation()
        {
            Vector3 direction = _characterProvider.CharacterFacade.transform.position - transform.position;
            
            if (direction.magnitude > 0.01f)
            {
                transform.forward = Vector3.Slerp(transform.forward, direction.normalized, _configuration.RotationSpeed * Time.deltaTime);
            }
        }
    }
}