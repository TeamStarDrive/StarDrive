using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace Ship_Game.Audio;

internal class AudioPlaybackEngine : IDisposable
{
    readonly IWavePlayer OutputDevice;
    readonly MixingSampleProvider Mixer;

    public static readonly int SampleRate = 48000;
    public static readonly int Channels = 2;

    public AudioPlaybackEngine(MMDevice device)
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
    public void AddMixerInput(ISampleProvider input)
    {
        ISampleProvider sampler = GetResamplingProvider(input);
        ISampleProvider stereo = GetStereoSampleProvider(sampler);
        Mixer.AddMixerInput(stereo);
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
