using UnityEngine;

public sealed class CharacterDeathSoulView : MonoBehaviour
{
    [SerializeField] private ParticleSystem _soul;

    private readonly ParticleSystem.Particle[] _particle = new ParticleSystem.Particle[1];
    private bool _isPlaying;

    public void Begin(Vector3 origin, float sizeMultiplier)
    {
        Stop();
        transform.SetPositionAndRotation(origin, Quaternion.identity);
        transform.localScale = Vector3.one * sizeMultiplier;
        _isPlaying = true;
    }

    public void Sample(float elapsed, Vector3 worldOffset,
        CharacterDeathConfiguration.SoulPose pose)
    {
        if (!_isPlaying || _soul == null)
            return;

        // Simulate pauses afterwards, so the particle cannot also advance automatically.
        // The prepared prefab uses fixed seeds for stable clouds and sparks between samples.
        _soul.Simulate(Mathf.Max(0.0001f, elapsed), true, true, false);
        if (_soul.GetParticles(_particle) == 0)
            return;

        ParticleSystem.Particle particle = _particle[0];
        particle.position = _soul.main.simulationSpace == ParticleSystemSimulationSpace.World
            ? _soul.transform.position + worldOffset
            : _soul.transform.InverseTransformVector(worldOffset);
        float size = particle.startSize;
        // The prepared soul texture is rotated a quarter turn, so its local size axes swap.
        particle.startSize3D = new Vector3(size * pose.Scale.y, size * pose.Scale.x, size);
        Color32 color = particle.startColor;
        color.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(pose.Opacity) * 255f);
        particle.startColor = color;
        _particle[0] = particle;
        _soul.SetParticles(_particle, 1);
    }

    public void Stop()
    {
        _isPlaying = false;
        if (_soul != null)
            _soul.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDisable() => Stop();
}
