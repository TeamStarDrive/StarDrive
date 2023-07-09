using System;
using NAudio.Wave;

namespace Ship_Game.Audio;

internal class NAudioSampleInstance : ISampleProvider, IAudioInstance, IDisposable
{
    readonly AudioCategory Category;
    readonly AudioEmitter Emitter;
    readonly AudioFileReader Reader;
    public WaveFormat WaveFormat => Reader.WaveFormat;

    PlaybackState State;
    float FadeOutTimer;
    
    public bool IsPlaying => State == PlaybackState.Playing;
    public bool IsPaused  => State == PlaybackState.Paused;
    public bool IsStopped => IsDisposed || (!IsPaused && !IsPlaying);
    public bool IsDisposed { get; private set; }
    public bool CanBeDisposed => State == PlaybackState.Stopped && FadeOutTimer <= 0f;

    public NAudioSampleInstance(AudioCategory category, AudioEmitter emitter, string fileName, float volume)
    {
        Category = category;
        Emitter = emitter;
        Reader = new(fileName) { Volume = volume };
        State = PlaybackState.Playing;
    }

    public void Dispose()
    {
        State = PlaybackState.Stopped;
        if (!IsDisposed)
        {
            IsDisposed = true;
            Reader.Dispose();
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

        int read = Reader.Read(buffer, offset, count);
        if (read <= 0 || read < count)
        {
            Dispose();
            return 0;
        }
        return read;
    }

    int FadeOut(float[] buffer, int offset, int count)
    {
        int read = Reader.Read(buffer, offset, count);

        // we're out of buffers so we can dispose this instance
        if (read < count)
            Dispose();

        // is there any final samples to process?
        if (read <= 0)
            return 0;

        // fade goes from 1.0 to 0.0
        float fade = 1f - (FadeOutTimer / Category.FadeOutTime);
        // step is how much to decrease volume per sample
        float step = 1f / AudioPlaybackEngine.SampleRate;
        
        for (int i = 0; i < read; ++i)
        {
            buffer[offset + i] *= fade;
            fade -= step;
            FadeOutTimer -= step;
        }

        return read;
    }
}
