using System;
using System.Threading;
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
    /// Sets the 3D audio listener position
    /// </summary>
    public void SetListenerPos(in Vector3 listenerPos)
    {
        foreach (AudioCategory category in Categories)
            category.SetListenerPos(listenerPos);
    }

    /// <summary>
    /// Perform updates on all audio categories
    /// </summary>
    public void Update()
    {
        foreach (AudioCategory category in Categories)
            category.Update();
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
    /// The maximum number of sounds that can be played at the same time for each sound effect, 0 for unlimited
    /// </summary>
    [StarData] public readonly int MaxConcurrentSoundsPerEffect;

    /// <summary>
    /// The maximum number of sounds that can be played per frame, 0 for unlimited
    /// </summary>
    [StarData] public readonly int MaxSoundsPerFrame;

    /// <summary>
    /// List of sound effects in this category
    /// </summary>
    [StarData] public readonly SoundEffect[] SoundEffects;

    /// <summary>
    /// Back-Reference to ALL audio instances that this category is currently playing
    /// They are automatically removed when they stop playing
    /// </summary>
    readonly Array<TrackedHandle> TrackedInstances = new();

    readonly struct TrackedHandle
    {
        public readonly SoundEffect Effect;
        public readonly IAudioInstance Instance; // can be NAudioSampleInstance or AudioHandle
        public TrackedHandle(SoundEffect effect, IAudioInstance instance)
        {
            Effect = effect;
            Instance = instance;
        }
    }

    public Vector3 ListenerPos;
    int CurrentSoundsPerFrame;

    /// <summary>
    /// Can this category play the following effect right now?
    /// It can be rejected because of maximum concurrent sounds or max concurrent sound effects limits
    /// </summary>
    public bool CanPlayEffect(SoundEffect effect)
    {
        //if (MaxSoundsPerFrame != 0 && CurrentSoundsPerFrame >= MaxSoundsPerFrame)
        //    return false;
        if (MaxConcurrentSounds != 0 && TrackedInstances.Count >= MaxConcurrentSounds)
            return false;
        if (MaxConcurrentSoundsPerEffect != 0 && effect.NumActiveInstances >= MaxConcurrentSoundsPerEffect)
            return false;
        return true;
    }

    static void RemoveTrackedInstance(ref TrackedHandle tracked)
    {
        if (tracked.Effect != null)
        {
            Interlocked.Decrement(ref tracked.Effect.NumActiveInstances);
        }
        if (!tracked.Instance.IsDisposed)
        {
            tracked.Instance.Dispose();
        }
    }

    public void Dispose()
    {
        lock (TrackedInstances)
        {
            foreach (ref TrackedHandle tracked in TrackedInstances.AsSpan())
            {
                RemoveTrackedInstance(ref tracked);
            }
            TrackedInstances.Clear();
        }

        // add some error checking here for debugging purposes
        foreach (SoundEffect effect in SoundEffects)
        {
            if (effect.NumActiveInstances != 0)
                Log.Error($"AudioCategory {Name}.{effect.Id} has {effect.NumActiveInstances} instances - this is a tracking bug");
            effect.NumActiveInstances = 0;
        }
    }

    public void SetListenerPos(in Vector3 listenerPos)
    {
        ListenerPos = listenerPos;
    }

    public void Update()
    {
        CurrentSoundsPerFrame = 0;

        // remove disposed handles
        lock (TrackedInstances)
        {
            for (int i = 0; i < TrackedInstances.Count; i++)
            {
                TrackedHandle tracked = TrackedInstances[i];
                if (tracked.Instance.IsDisposed || tracked.Instance.CanBeDisposed)
                {
                    RemoveTrackedInstance(ref tracked);
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
            foreach (ref TrackedHandle tracked in TrackedInstances.AsSpan())
            {
                if (!tracked.Instance.IsStopped)
                    tracked.Instance.Stop(fadeout);
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
            foreach (ref TrackedHandle tracked in TrackedInstances.AsSpan())
            {
                if (tracked.Instance.IsPlaying)
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
            foreach (ref TrackedHandle tracked in TrackedInstances.AsSpan())
            {
                if (tracked.Instance.IsPaused)
                    tracked.Instance.Resume();
            }
        }
    }

    internal void TrackInstance(SoundEffect effect, IAudioInstance instance, AudioHandle handle)
    {
        ++CurrentSoundsPerFrame;

        TrackedHandle tracked;
        if (handle != null)
        {
            handle.OnLoaded(instance);
            tracked = new(effect, handle);
        }
        else
        {
            tracked = new(effect, instance);
        }

        if (effect != null)
        {
            Interlocked.Increment(ref effect.NumActiveInstances);
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
    [StarData] public readonly string Sound;

    /// <summary>
    /// Multiple sound effects.
    /// The effect to play is chosen randomly.
    /// </summary>
    [StarData] public readonly string[] Sounds;

    public AudioCategory Category;
    public int NumActiveInstances;

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
