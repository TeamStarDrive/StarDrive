using System;
using NAudio.Wave;

namespace Ship_Game.Audio.NAudio;

/// <summary>
/// A simple playing sample instance
/// </summary>
internal class NAudioSampleInstance : ISampleProvider, IAudioInstance, IDisposable
{
    readonly AudioCategory Category;
    readonly AudioEmitter Emitter;
    readonly ISampleProvider Provider;
    public float Volume { get; set; }
    public WaveFormat WaveFormat => Provider.WaveFormat;

    PlaybackState State;
    float FadeOutTimer;
    
    public bool IsPlaying => State == PlaybackState.Playing;
    public bool IsPaused  => State == PlaybackState.Paused;
    public bool IsStopped => IsDisposed || (!IsPaused && !IsPlaying);
    public bool IsDisposed { get; private set; }
    public bool CanBeDisposed => State == PlaybackState.Stopped && FadeOutTimer <= 0f;

    public override string ToString() => $"NAudioSampleInstance {Provider}";

    public NAudioSampleInstance(AudioCategory category, AudioEmitter emitter, ISampleProvider provider, float volume)
    {
        Category = category;
        Emitter = emitter;
        Provider = provider;
        Volume = volume;
        State = PlaybackState.Playing;
    }

    public void Dispose()
    {
        State = PlaybackState.Stopped;
        if (!IsDisposed)
        {
            IsDisposed = true;
            if (Provider is IDisposable disposableProvider)
                disposableProvider.Dispose();
        }
    }
    
    public void Pause()
    {
        if (State == PlaybackState.Playing)
        {
            State = PlaybackState.Paused;
            FadeOutTimer = Category.FadeOutTime;
        }
    }
    public void Resume()
    {
        if (State == PlaybackState.Paused)
        {
            State = PlaybackState.Playing;
        }
    }
    public void Stop(bool fadeout)
    {
        State = PlaybackState.Stopped;
        FadeOutTimer = fadeout ? Category.FadeOutTime : 0f;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (IsDisposed)
            return 0;

        // once the volume goes to zero due to distance, we can dispose the sample
        float volume = Emitter?.GetEffectiveVolume(Category, Volume) ?? Volume;
        if (volume <= 0.0001f)
        {
            Dispose();
            return 0;
        }

        // fade out timer has been set
        if (FadeOutTimer > 0f)
        {
            int read2 = ReadSamples(buffer, offset, count);
            return read2 <= 0 ? 0 : FadeOut(volume, buffer, offset, read2);
        }

        // pausing is very easy, we simply generate empty (0.0) samples
        if (State == PlaybackState.Paused)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }

        if (State == PlaybackState.Stopped)
        {
            Dispose();
            return 0;
        }

        int read = ReadSamples(buffer, offset, count);
        return ApplyVolume(volume, buffer, offset, read);
    }

    int ReadSamples(float[] buffer, int offset, int count)
    {
        int read = Provider.Read(buffer, offset, count);
        // we're out of buffers so we can dispose this instance
        if (read < count)
        {
            Dispose();
        }
        return read <= 0 ? 0 : read;
    }

    int FadeOut(float volume, float[] buffer, int offset, int count)
    {
        // fade goes from 1.0 to 0.0
        float fade = 1f - (FadeOutTimer / Category.FadeOutTime);

        // step is how much to decrease volume per sample
        float step = 1f / NAudioPlaybackEngine.SampleRate;
        
        for (int i = 0; i < count; ++i)
        {
            // apply volume directly to fade multiplier
            buffer[offset + i] *= fade * volume;
            fade -= step;
        }

        // update the timer
        if (fade <= 0f)
            FadeOutTimer = 0f;
        else
            FadeOutTimer -= step * count;
        
        return ApplyVolume(volume, buffer, offset, count);
    }

    static int ApplyVolume(float volume, float[] buffer, int offset, int count)
    {
        if (volume != 1.0f)
        {
            for (int index = 0; index < count; ++index)
                buffer[offset + index] *= volume;
        }
        return count;
    }
}
