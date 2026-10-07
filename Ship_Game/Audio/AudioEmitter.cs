using SDGraphics;

namespace Ship_Game.Audio;

/// <summary>
/// 3D Positional Audio Emitter
/// </summary>
public class AudioEmitter
{
    public Vector3 Position;
    public readonly float MaxDistance;
    public readonly float VolumeScale;

    /// <summary>
    /// Initializes a new AudioEmitter associated with any number of Audio instances
    /// </summary>
    /// <param name="maxDistance">
    /// Maximum distance from the listener that this emitter can be heard.
    /// At maxDistance or beyond, the emitter will be silent and will be stopped.
    /// </param>
    /// <param name="volumeScale">Multiplies the volume of every sound played through this emitter</param>
    public AudioEmitter(float maxDistance, float volumeScale = 1f)
    {
        MaxDistance = maxDistance;
        VolumeScale = volumeScale;
    }

    /// <summary>
    /// Based on the current AudioCategory ListenerPosition,
    /// calculates the effective volume of this emitter.
    /// </summary>
    /// <returns>Reduced `volume` based on linear falloff</returns>
    public float GetEffectiveVolume(AudioCategory category, float volume)
    {
        float distance = Position.Distance(category.ListenerPos);
        if (distance >= MaxDistance)
            return 0f;

        float falloff = 1f - (distance / MaxDistance);
        return volume * falloff * VolumeScale;
    }
}
