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
        public bool IsStopped
        {
            get
            {
                SoundEffectInstance sfx = Sfx; // flimsy thread safety
                return sfx == null || sfx.IsDisposed || sfx.State == SoundState.Stopped;
            }
        }
        public void Pause()  => Sfx?.Pause();
        public void Resume() => Sfx?.Resume();
        public void Stop()
        {
            if (!IsStopped)
                Sfx?.Stop(immediate: true);
        }
        void Destroy()
        {
            // SFX must always be disposed
            SoundEffectInstance sfx = Sfx;
            if (sfx != null) { Sfx = null; sfx.Dispose(); }
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
        public bool IsPlaying
        {
            get
            {
                Cue cue = Cue; // flimsy thread safety
                return cue != null && !cue.IsDisposed && cue.IsPlaying;
            }
        }
        public bool IsStopped
        {
            get
            {
                Cue cue = Cue; // flimsy thread safety
                return cue == null || cue.IsDisposed || cue.IsStopped;

            }
        }
        public bool IsPaused  => Cue?.IsPaused == true;
        public void Pause()   => Cue?.Pause();
        public void Resume()  => Cue?.Resume();
        public void Stop()
        {
            if (!IsStopped)
                Cue.Stop(AudioStopOptions.AsAuthored);
        }
        void Destroy()
        {
            Cue cue = Cue; // flimsy thread safety
            if (cue != null) { Cue = null; cue.Dispose(); }
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
                Player?.Stop();
        }
        void Destroy()
        {
            var player = Player; // flimsy thread safety
            if (player != null) { Player = null; player.Dispose(); }

            var reader = Reader; // flimsy thread safety
            if (reader != null) { Reader = null; reader.Dispose(); }
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~Mp3Instance() { Destroy(); }
    }
}