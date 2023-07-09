using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SDUtils;
using System;

namespace Ship_Game.Audio.NAudio;

internal class NAudioPlaybackEngine : IDisposable
{
    public static readonly int SampleRate = 48000;
    public static readonly int Channels = 2;

    readonly IWavePlayer OutputDevice;
    readonly MixingSampleProvider Mixer;

    // pre-sampled cache for Weapon and Warp effects
    readonly Map<string, CachedSoundEffect> SfxCache = new();

    public NAudioPlaybackEngine(MMDevice device)
    {
        OutputDevice = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: true, latency: 100);

        //OutputDevice = new WaveOutEvent()
        //{
        //    DesiredLatency = 200,
        //    NumberOfBuffers = 3,
        //};

        Mixer = new(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels))
        {
            ReadFully = true
        };

        OutputDevice.Init(Mixer);
        OutputDevice.Play();
    }

    public void Dispose()
    {
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
    public IAudioInstance Play(AudioCategory category, AudioEmitter emitter, string audioFile, float volume)
    {
        try
        {
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
                    cached = new(this, audioFile);
                    lock (SfxCache)
                        SfxCache.Add(audioFile, cached);
                }

                provider = cached.CreateReader();
            }
            else
            {
                provider = new NAudioFileReader(this, audioFile);
            }

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

    public ISampleProvider GetCompatibleSampleProvider(ISampleProvider provider)
    {
        ISampleProvider sampler = GetResamplingProvider(provider);
        ISampleProvider stereo = GetStereoSampleProvider(sampler);
        return stereo;
    }

    ISampleProvider GetStereoSampleProvider(ISampleProvider input)
    {
        if (input.WaveFormat.Channels == Mixer.WaveFormat.Channels)
            return input;
        if (input.WaveFormat.Channels == 1 && Mixer.WaveFormat.Channels == 2)
            return new MonoToStereoSampleProvider(input);
        throw new NotImplementedException("SampleProvider not implemented");
    }

    ISampleProvider GetResamplingProvider(ISampleProvider input)
    {
        if (input.WaveFormat.SampleRate == SampleRate)
            return input;
        return new WdlResamplingSampleProvider(input, SampleRate);
    }
}
