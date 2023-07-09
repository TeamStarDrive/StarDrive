using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.Serialization;
using Ship_Game.Utils;

namespace Ship_Game.Audio;

/// <summary>
/// Main audio configuration file and state manager
/// </summary>
[StarDataType]
public class AudioConfig : IDisposable
{
    /// <summary>
    /// Contains all the audio categories which are used to track all audio instances
    /// </summary>
    [StarData] public readonly AudioCategory[] Categories;

    /// <summary>
    /// Helper mapping from SoundEffect.Id to SoundEffect instances
    /// </summary>
    readonly Map<string, SoundEffect> SoundEffects = new();

    public AudioConfig(string configFile = "Audio/AudioConfig.yaml")
    {
        using YamlParser parser = new(configFile);
        Categories = parser.DeserializeArray<AudioCategory>().ToArr();

        // create a mapping of Id to SoundEffect
        foreach (AudioCategory category in Categories)
        {
            foreach (SoundEffect effect in category.SoundEffects)
            {
                if (SoundEffects.ContainsKey(effect.Id))
                    throw new($"Duplicate sound effect={effect.Id} in category={category.Name}");

                effect.Category = category;
                SoundEffects[effect.Id] = effect;
            }
        }
    }

    /// <summary>
    /// Perform updates on all audio categories
    /// </summary>
    public void Update(in Vector3 listenerPosition)
    {
        foreach (AudioCategory category in Categories)
            category.Update(listenerPosition);
    }

    public SoundEffect GetSoundEffect(string id)
    {
        if (SoundEffects.TryGetValue(id, out SoundEffect effect))
            return effect;
        Log.Warning($"Could not find SoundEffect: {id}");
        return null;
    }   

    public AudioCategory GetCategory(string name)
    {
        foreach (AudioCategory category in Categories)
            if (category.Name == name)
                return category;
        throw new($"AudioConfig category not found: {name}");
    }

    public void Dispose()
    {
        foreach (AudioCategory category in Categories)
            category.Dispose();
    }
}

[StarDataType]
public class AudioCategory : IDisposable
{
    /// <summary>
    /// Name of this category
    /// </summary>
    [StarData] public readonly string Name;

    /// <summary>
    /// Global volume for this category
    /// </summary>
    [StarData] public float Volume = 1.0f;

    /// <summary>
    /// Useful for Music playback, this is the time it takes to fade out the audio
    /// when Stop() is called
    /// </summary>
    [StarData] public readonly float FadeOutTime = 0.0f;


    /// <summary>
    /// Sound effects in this category can be cached to memory to speed up the audio playback
    /// </summary>
    [StarData] public readonly bool MemoryCache;

    /// <summary>
    /// The maximum number of sounds that can be played at the same time, 0 for unlimited
    /// </summary>
    [StarData] public readonly int MaxConcurrentSounds;

    /// <summary>
    /// List of sound effects in this category
    /// </summary>
    [StarData] public readonly SoundEffect[] SoundEffects;

    /// <summary>
    /// Back-Reference to ALL audio instances that this category is currently playing
    /// They are automatically removed when they stop playing
    /// </summary>
    readonly Array<TrackedHandle> TrackedInstances = new();

    /// <summary>
    /// Can another sound be played in this category?
    /// </summary>
    public bool IsQueueFull => MaxConcurrentSounds != 0 && TrackedInstances.Count >= MaxConcurrentSounds;

    public void Dispose()
    {
        lock (TrackedInstances)
        {
            for (int i = 0; i < TrackedInstances.Count; i++)
            {
                if (!TrackedInstances[i].IsDisposed)
                    TrackedInstances[i].Dispose();
            }
            TrackedInstances.Clear();
        }
    }

    public void Update(in Vector3 listenerPosition)
    {
        // remove disposed handles
        lock (TrackedInstances)
        {
            for (int i = 0; i < TrackedInstances.Count; i++)
            {
                TrackedHandle tracked = TrackedInstances[i];
                if (tracked.Instance.IsDisposed)
                {
                    TrackedInstances.RemoveAtSwapLast(i--);
                }
                else if (tracked.Instance.CanBeDisposed)
                {
                    tracked.Dispose();
                    TrackedInstances.RemoveAtSwapLast(i--);
                }
            }
        }
    }

    /// <summary>
    /// Stop all audio instances in this category
    /// </summary>
    public void Stop(bool fadeout)
    {
        lock (TrackedInstances)
        {
            for (int i = 0; i < TrackedInstances.Count; ++i)
            {
                TrackedHandle tracked = TrackedInstances[i];
                if (!tracked.IsStopped)
                    tracked.Stop(fadeout);
            }
        }
    }

    /// <summary>
    /// Pauses all audio instances in this category
    /// </summary>
    public void Pause()
    {
        lock (TrackedInstances)
        {
            for (int i = 0; i < TrackedInstances.Count; ++i)
            {
                TrackedHandle tracked = TrackedInstances[i];
                if (tracked.IsPlaying)
                    tracked.Instance.Pause();
            }
        }
    }

    /// <summary>
    /// Resumes all paused audio instances in this category.
    /// Does not resume any stopped instances.
    /// </summary>
    public void Resume()
    {
        lock (TrackedInstances)
        {
            for (int i = 0; i < TrackedInstances.Count; ++i)
            {
                TrackedHandle tracked = TrackedInstances[i];
                if (tracked.IsPaused)
                    tracked.Instance.Resume();
            }
        }
    }

    internal void TrackInstance(IAudioInstance instance, AudioHandle handle)
    {
        TrackedHandle tracked;
        if (handle != null)
        {
            handle.OnLoaded(instance);
            tracked = new(instance, handle);
        }
        else
        {
            tracked = new(instance);
        }

        lock (TrackedInstances)
        {
            TrackedInstances.Add(tracked);
        }
    }
}

[StarDataType]
public class SoundEffect
{
    /// <summary>
    /// Unique ID of the sound effect, used when playing
    /// </summary>
    [StarData] public readonly string Id;

    /// <summary>
    /// Volume modifier for this sound effect
    /// </summary>
    [StarData] public readonly float Volume = 1.0f;

    /// <summary>
    /// A single sound effect
    /// </summary>
    [StarData]
    public readonly string Sound;

    /// <summary>
    /// Multiple sound effects.
    /// The effect to play is chosen randomly.
    /// </summary>
    [StarData]
    public readonly string[] Sounds;

    public AudioCategory Category;

    /// <summary>
    /// Selects the next sound file to play
    /// </summary>
    public string GetNextSfxFile(RandomBase random)
    {
        if (Sound != null)
            return Sound;
        return Sounds.Length == 1 ? Sounds[0] : random.Item(Sounds);
    }
}
