using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Ship_Game
{
    public struct AudioHandle
    {
        private Cue CueInst;
        private SoundEffectInstance SfxInst;

        public AudioHandle(Cue cue)
        {
            CueInst = cue;
            SfxInst = null;
        }
        public AudioHandle(SoundEffectInstance sfx)
        {
            CueInst = null;
            SfxInst = sfx;
        }
        public bool IsPlaying  => CueInst?.IsPlaying ?? SfxInst?.State == SoundState.Playing;
        public bool IsPaused   => CueInst?.IsPaused  ?? SfxInst?.State == SoundState.Paused;
        public bool NotPlaying
        {
            get
            {
                if (CueInst != null) return CueInst.IsStopped;
                if (SfxInst != null) return SfxInst.State == SoundState.Stopped;
                return true;
            }
        }
        public void Stop()
        {
            if (SfxInst != null)
            {
                SfxInst.Stop(immediate:true);
                SfxInst = null;
            }
            else if (CueInst != null)
            {
                CueInst.Stop(AudioStopOptions.Immediate);
                CueInst = null;
            }
        }
        public void Resume()
        {
            SfxInst?.Resume();
            CueInst?.Resume();
        }
        public void Pause()
        {
            SfxInst?.Pause();
            CueInst?.Pause();
        }
        public void Destroy()
        {
            SfxInst?.Dispose();
            CueInst?.Dispose();
        }
    }

    // Whole StarDrive game audio state is managed here
    public static class GameAudio
    {
        private static AudioEngine AudioEngine;
        private static SoundBank SoundBank;
        private static WaveBank WaveBank;

        private static bool AudioDisabled;
        private static bool EffectsDisabled;
        private static bool MusicDisabled;

        private static AudioCategory Global;
        private static AudioCategory Default;
        private static AudioCategory Weapons;
        private static AudioCategory Music;
        private static AudioCategory RacialMusic;
        private static AudioCategory CombatMusic;

        private static Array<AudioHandle> AudioHandles;

        private static int ThisFrameCueCount; // Limit the number of Cues that can be loaded per frame. 
        private static bool FrameCueLimitExceeded => ThisFrameCueCount > 2; // @ 60fps, this is max 120 samples per minute

        public static void Initialize(string settingsFile, string waveBankFile, string soundBankFile)
        {
            try
            {
                AudioHandles = new Array<AudioHandle>();
                AudioEngine = new AudioEngine(settingsFile);
                WaveBank    = new WaveBank(AudioEngine, waveBankFile, 0, 16);
                SoundBank   = new SoundBank(AudioEngine, soundBankFile);

                while (!WaveBank.IsPrepared)
                    AudioEngine.Update();

                // these are specific to "Audio/ShipGameProject.xgs"
                Global      = AudioEngine.GetCategory("Global");
                Default     = AudioEngine.GetCategory("Default");
                Music       = AudioEngine.GetCategory("Music");
                Weapons     = AudioEngine.GetCategory("Weapons");
                RacialMusic = AudioEngine.GetCategory("RacialMusic");
                CombatMusic = AudioEngine.GetCategory("CombatMusic");
            }
            catch (Exception ex)
            {
                AudioEngine = null;
                WaveBank    = null;
                SoundBank   = null;
                Log.Error(ex, "AudioEngine init failed");
            }
        }

        // called from Game1 Dispose()
        public static void Destroy()
        {
            lock (AudioHandles)
            {
                for (int i = 0; i < AudioHandles.Count; ++i)
                    AudioHandles[i].Destroy();
                AudioHandles.Clear();
            }
            SoundBank?.Dispose(ref SoundBank);
            WaveBank?.Dispose(ref WaveBank);
            AudioEngine?.Dispose(ref AudioEngine);
        }


        // this is called from Game1.Update() every frame
        public static void Update()
        {
            ThisFrameCueCount = 0;
            DisposeStoppedHandles();

            AudioEngine?.Update();
        }

        // Configures GameAudio from GlobalStats MusicVolume and EffectsVolume
        public static void ConfigureAudioSettings()
        {
            float music   = GlobalStats.MusicVolume;
            float effects = GlobalStats.EffectsVolume;

            MusicDisabled   = music   <= 0.001f;
            EffectsDisabled = effects <= 0.001f;
            AudioDisabled = MusicDisabled && EffectsDisabled;

            Global.SetVolume(AudioDisabled ? 0.0f : 1.0f);
            Music.SetVolume(music);
            RacialMusic.SetVolume(music);
            CombatMusic.SetVolume(music);
            Weapons.SetVolume(effects);
            Default.SetVolume(effects * 0.5f);
        }

        public static void StopGenericMusic(bool immediate = true)
        {
            Music.Stop(immediate ? AudioStopOptions.Immediate : AudioStopOptions.AsAuthored);
        }

        public static void PauseGenericMusic()  => Music.Pause();
        public static void ResumeGenericMusic() => Music.Resume();
        public static void MuteGenericMusic()   => Music.SetVolume(0f);
        public static void UnmuteGenericMusic() => Music.SetVolume(GlobalStats.MusicVolume);
        public static void MuteRacialMusic()    => RacialMusic.SetVolume(0f);
        public static void UnmuteRacialMusic()  => RacialMusic.SetVolume(GlobalStats.MusicVolume);

        // this is used for the DiplomacyScreen
        public static void SwitchToRacialMusic()
        {
            Music.Stop(AudioStopOptions.Immediate);
            MuteGenericMusic();
            UnmuteRacialMusic();
        }

        public static void SwitchBackToGenericMusic()
        {
            UnmuteGenericMusic();
            Music.Resume();
            RacialMusic.Stop(AudioStopOptions.Immediate);
        }


        private static AudioHandle Play(string cueName, AudioEmitter emitter = null)
        {
            AudioHandle handle;

            if (ResourceManager.GetModSoundEffect(cueName, out SoundEffect sfx))
            {
                SoundEffectInstance sfxi = sfx.CreateInstance();
                handle = new AudioHandle(sfxi);
                if (emitter != null)
                    sfxi.Apply3D(Empire.Universe.Listener, emitter);

                sfxi.Volume = GlobalStats.EffectsVolume;
                sfxi.Play();
            }
            else
            {
                Cue cue = SoundBank.GetCue(cueName);
                handle = new AudioHandle(cue);
                if (emitter != null)
                    cue.Apply3D(Empire.Universe.Listener, emitter);
                cue.Play();
                ThisFrameCueCount++;
            }

            lock (AudioHandles) AudioHandles.Add(handle);
            return handle;
        }

        public static AudioHandle PlaySfx(string cueName, AudioEmitter emitter = null)
        {
            if (AudioDisabled || EffectsDisabled || FrameCueLimitExceeded || cueName.IsEmpty())
                return default(AudioHandle);
            return Play(cueName, emitter);
        }

        public static void PlaySfxAsync(string cueName)
        {
            if (AudioDisabled || EffectsDisabled || FrameCueLimitExceeded || cueName.IsEmpty())
                return;
            Parallel.Run(() => Play(cueName));
        }

        public static void PlaySfxAsync(string cueName, Action<AudioHandle> complete)
        {
            if (AudioDisabled || EffectsDisabled || FrameCueLimitExceeded || cueName.IsEmpty())
                return;
            Parallel.Run(() => complete(Play(cueName)));
        }

        public static void PlaySfxAt(string cueName, Vector3 position)
        {
            if (AudioDisabled || EffectsDisabled || FrameCueLimitExceeded || cueName.IsEmpty())
                return; // avoid creating emitter

            Parallel.Run(() => Play(cueName, new AudioEmitter { Position = position }));
        }

        public static AudioHandle PlayMusic(string cueName)
        {
            if (AudioDisabled || MusicDisabled || cueName.IsEmpty())
                return default(AudioHandle);

            Cue cue = SoundBank.GetCue(cueName);
            cue.Play();
            return new AudioHandle(cue);
        }

        private static void DisposeStoppedHandles()
        {
            lock (AudioHandles)
            {
                for (int i = 0; i < AudioHandles.Count; i++)
                {
                    AudioHandle handle = AudioHandles[i];
                    if (handle.NotPlaying)
                    {
                        handle.Destroy();
                        AudioHandles.RemoveAtSwapLast(i--);
                    }
                }
            }
        }
    }
}