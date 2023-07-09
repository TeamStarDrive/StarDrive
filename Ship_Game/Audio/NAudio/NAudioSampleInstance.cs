using System;
using NAudio.Wave;

namespace Ship_Game.Audio.NAudio;

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

        // fade out timer has been set
        if (FadeOutTimer > 0f)
        {
            return FadeOut(buffer, offset, count);
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

        int read = Provider.Read(buffer, offset, count);
        if (read <= 0 || read < count)
        {
            Dispose();
            return 0;
        }

        return ApplyVolume(buffer, offset, read);
    }

    int ApplyVolume(float[] buffer, int offset, int count)
    {
        float volume = Volume;
        if (volume != 1.0f)
        {
            for (int index = 0; index < count; ++index)
                buffer[offset + index] *= volume;
        }
        return count;
    }

    int FadeOut(float[] buffer, int offset, int count)
    {
        int read = Provider.Read(buffer, offset, count);

        // we're out of buffers so we can dispose this instance
        if (read < count)
            Dispose();

        // is there any final samples to process?
        if (read <= 0)
            return 0;

        // fade goes from 1.0 to 0.0
        float fade = 1f - (FadeOutTimer / Category.FadeOutTime);
        // step is how much to decrease volume per sample
        float step = 1f / NAudioPlaybackEngine.SampleRate;
        
        for (int i = 0; i < read; ++i)
        {
            buffer[offset + i] *= fade;
            fade -= step;
            FadeOutTimer -= step;
        }
        
        return ApplyVolume(buffer, offset, read);
    }
}
