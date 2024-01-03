using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SDUtils;
using System;

#nullable enable

namespace Ship_Game.Audio.NAudio;

internal class NAudioPlaybackEngine : IDisposable
{
    public static readonly int SampleRate = 44100;
    public static readonly int Channels = 2;

    readonly IWavePlayer OutputDevice;
    readonly NAudioSampleMixer Mixer;

    // pre-sampled cache for Weapon and Warp effects
    readonly Map<string, CachedSoundEffect> SfxCache = new();

    public WaveFormat WaveFormat { get; }

    public NAudioPlaybackEngine(MMDevice device)
    {
        // useEventSync: if true, waits for Wasapi signal, otherwise sleeps for (latency/2) ms
        // latency: buffer duration in ms
        OutputDevice = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: false, latency: 50);
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
        Mixer = new(WaveFormat) { ReadFully = true };

        OutputDevice.Init(Mixer);
        OutputDevice.Play();
    }

    public void Dispose()
    {
        Mixer.Dispose();
        OutputDevice.Dispose(); // automatically calls Stop()
    }

    /// <summary>
    /// Global volume of the mixer
    /// </summary>
    public float Volume
    {
        get => OutputDevice.Volume;
        set => OutputDevice.Volume = value;
    }

    /// <summary>
    /// Adds a new sound to the mixer
    /// The sound is automatically removed when it finishes playing
    /// </summary>
    public IAudioInstance? Play(AudioCategory category, AudioEmitter? emitter, string audioFile, float volume)
    {
        try
        {
            float? effectiveVolume = emitter?.GetEffectiveVolume(category, volume);
            if (effectiveVolume < 0.0001f)
                return null; // this sound can't be heard anyway, ignore it

            ISampleProvider provider;

            if (category.MemoryCache)
            {
                CachedSoundEffect cached;
                lock (SfxCache)
                {
                    SfxCache.TryGetValue(audioFile, out cached);
                }
                if (cached == null)
                {
                    // generating the cache will be sloooow, even on a very fast system it can take 300ms+
                    //PerfTimer t = new();
                    cached = new(WaveFormat, audioFile);
                    //double elapsedMs = t.ElapsedMillis;
                    //Log.Write(ConsoleColor.Green, $"Caching {audioFile} elapsed:{elapsedMs:0.1}ms");

                    lock (SfxCache)
                        SfxCache.Add(audioFile, cached);
                }
                provider = cached.CreateReader();
            }
            else
            {
                provider = new NAudioFileReader(WaveFormat, audioFile);
            }

            //Log.Write(ConsoleColor.Green, $"Start {audioFile} volume={volume}");
            NAudioSampleInstance instance = new(category, emitter, provider, volume);
            Mixer.AddMixerInput(instance);
            return instance;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to play audio file: {ex}");
            return null;
        }
    }
}
