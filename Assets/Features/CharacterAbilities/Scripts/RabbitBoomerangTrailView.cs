using System.Collections.Generic;
using UnityEngine;

// The prefab owns the renderer; the flight advances its history with the gameplay clock.
[DisallowMultipleComponent]
public sealed class RabbitBoomerangTrailView : MonoBehaviour
{
    [SerializeField] private LineRenderer _line;
    [SerializeField, Min(0.01f)] private float _duration = 0.2f;
    [SerializeField, Min(0.01f)] private float _width = 0.16f;

    private readonly Queue<Sample> _samples = new(32);
    private float _elapsed;

    public void Initialize()
    {
        _samples.Clear();
        _elapsed = 0f;
        if (_line == null)
            return;

        Vector3 scale = transform.lossyScale;
        _line.widthMultiplier = _width *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        _line.positionCount = 0;
        // The prepared child starts inactive so the model's Outline does not cache it.
        _line.gameObject.SetActive(true);
        _samples.Enqueue(new Sample(transform.position, 0f));
    }

    public void Tick(float deltaTime)
    {
        if (_line == null || !_line.gameObject.activeInHierarchy || deltaTime <= 0f)
            return;

        _elapsed += deltaTime;
        float oldestTime = _elapsed - Mathf.Max(0.01f, _duration);
        while (_samples.Count > 0 && _samples.Peek().Time < oldestTime)
            _samples.Dequeue();

        _samples.Enqueue(new Sample(transform.position, _elapsed));
        _line.positionCount = _samples.Count;
        int index = _samples.Count - 1;
        foreach (Sample sample in _samples)
            _line.SetPosition(index--, sample.Position);
    }

    private void OnDisable()
    {
        _samples.Clear();
        if (_line != null)
            _line.positionCount = 0;
    }

    private readonly struct Sample
    {
        public readonly Vector3 Position;
        public readonly float Time;

        public Sample(Vector3 position, float time)
        {
            Position = position;
            Time = time;
        }
    }
}
