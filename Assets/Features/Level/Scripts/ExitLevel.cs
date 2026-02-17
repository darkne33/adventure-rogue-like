using System;
using UnityEngine;
using Zenject;

public class ExitLevel : MonoBehaviour
{
    [Inject] private ICharacterProvider _characterProvider;

    private bool _isActive;

    private void OnTriggerEnter(Collider other)
    {
        
    }
}