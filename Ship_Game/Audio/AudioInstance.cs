using Microsoft.Xna.Framework.Audio;
using System;
using NAudio.Wave;
using Cue = Microsoft.Xna.Framework.Audio.Cue;

namespace Ship_Game.Audio
{
    internal interface IAudioInstance : IDisposable
    {
        bool IsPlaying { get; }
        bool IsPaused  { get; }
        bool IsStopped { get; }
        void Pause();
        void Resume();
        void Stop();
    }

    internal class SfxInstance : IAudioInstance
    {
        SoundEffectInstance Sfx;
        public SfxInstance(SoundEffectInstance sfx)
        {
            Sfx = sfx;
            Sfx.Play();
        }
        public bool IsPlaying => Sfx?.State == SoundState.Playing;
        public bool IsPaused  => Sfx?.State == SoundState.Paused;
        public bool IsStopped => Sfx == null || Sfx.IsDisposed || Sfx.State == SoundState.Stopped;
        public void Pause()   => Sfx?.Pause();
        public void Resume()  => Sfx?.Resume();
        public void Stop()
        {
            if (!IsStopped)
                Sfx.Stop(immediate: true);
        }
        void Destroy()
        {
            if (Sfx != null) { Sfx.Dispose(); Sfx = null; }
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~SfxInstance() { Destroy(); }
    }

    internal class CueInstance : IAudioInstance
    {
        Cue Cue;
        public CueInstance(Cue cue)
        {
            Cue = cue;
            Cue.Play();
        }
        public bool IsPlaying => Cue?.IsPlaying == true;
        public bool IsPaused  => Cue?.IsPaused  == true;
        public bool IsStopped => Cue == null || Cue.IsDisposed || Cue.IsStopped;
        public void Pause()   => Cue?.Pause();
        public void Resume()  => Cue?.Resume();
        public void Stop()
        {
            if (!IsStopped)
                Cue.Stop(AudioStopOptions.Immediate);
        } 
        void Destroy()
        {
            if (Cue != null) { Cue.Dispose(); Cue = null; }
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~CueInstance() { Destroy(); }
    }

    internal class Mp3Instance : IAudioInstance
    {
        IWavePlayer Player;
        Mp3FileReader Reader;
        public Mp3Instance(string mp3File)
        {
            Player = new WaveOut();
            Reader = new Mp3FileReader(mp3File);
            try
            {
                Player.Init(Reader);
                #pragma warning disable CS0618 // Type or member is obsolete
                Player.Volume = GlobalStats.MusicVolume;
                #pragma warning restore CS0618 // Type or member is obsolete
                Player.Play();
                Player.PlaybackStopped += OnPlaybackStopped;
            }
            catch
            {
            }
        }
        void OnPlaybackStopped(object sender, EventArgs e)
        {
            Player?.Dispose(ref Player);
            Reader?.Dispose(ref Reader);
        }
        public bool IsPlaying => Player?.PlaybackState == PlaybackState.Playing;
        public bool IsPaused  => Player?.PlaybackState == PlaybackState.Paused;
        public bool IsStopped => Player == null || (!IsPaused && !IsPlaying);
        public void Pause()   => Player?.Pause();
        public void Resume()  => Player?.Play();
        public void Stop()
        {
            if (!IsStopped)
                Player.Stop();
        }
        void Destroy()
        {
            if (Player != null) { Player.Dispose(); Player = null; }
            if (Reader != null) { Reader.Dispose(); Reader = null; }
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~Mp3Instance() { Destroy(); }
    }
}