using System;
using System.Threading;
using Microsoft.Xna.Framework.Audio;

namespace Ship_Game
{
    public class AudioHandleState
    {
        private Cue CueInst;
        private SoundEffectInstance SfxInst;
        private bool Loading;
        public AudioHandleState(Cue cue, SoundEffectInstance sfx)
        {
            CueInst = cue;
            SfxInst = sfx;
        }
        public bool IsPlaying =>  Loading || (CueInst?.IsPlaying ?? SfxInst?.State == SoundState.Playing);
        public bool IsPaused  => !Loading && (CueInst?.IsPaused  ?? SfxInst?.State == SoundState.Paused);
        public bool IsStopped
        {
            get
            {
                if (Loading) return false; // we consider loading playing, thus IsStopped is false
                if (CueInst != null) return CueInst.IsStopped;
                if (SfxInst != null) return SfxInst.State == SoundState.Stopped;
                return true;
            }
        }
        public void Pause()
        {
            SfxInst?.Pause();
            CueInst?.Pause();
        }
        public void Resume()
        {
            SfxInst?.Resume();
            CueInst?.Resume();
        }
        public void Stop()
        {
            SfxInst?.Stop(immediate: true);
            CueInst?.Stop(AudioStopOptions.Immediate);
            SfxInst = null;
            CueInst = null;
            Loading = false;
        }
        public void Destroy()
        {
            SfxInst?.Dispose();
            CueInst?.Dispose();
            SfxInst = null;
            CueInst = null;
            Loading = false;
        }
        public void Completed(Cue cue, SoundEffectInstance sfx)
        {
            CueInst = cue;
            SfxInst = sfx;
            Loading = false;
        }
        public void PlaySfxAsync(string cueName, AudioEmitter emitter = null)
        {
            if (Loading) return;
            Loading = true;
            GameAudio.PlaySfxAsync(cueName, emitter, this);
        }
    }

    public struct AudioHandle
    {
        private AudioHandleState State;

        public AudioHandle(Cue cue, SoundEffectInstance sfx) => State = new AudioHandleState(cue, sfx);
        public bool IsPlaying => State != null && State.IsPlaying;
        public bool IsPaused  => State != null && State.IsPaused;
        public bool IsStopped => State == null || State.IsStopped;
        public void Pause()   => State?.Pause();
        public void Resume()  => State?.Resume();
        public void Stop()
        {
            if (State != null) { State.Stop(); State = null; }
        }
        public void Destroy()
        {
            if (State != null) { State.Destroy(); State = null; }
        }
        public void PlaySfxAsync(string cueName, AudioEmitter emitter = null)
        {
            if (GameAudio.CantPlaySfx(cueName))
                return;

            if (State == null)
                State = new AudioHandleState(null, null);
            State.PlaySfxAsync(cueName, emitter);
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

        private static Array<AudioHandleState> AudioHandles;
        private static int ThisFrameSfxCount; // Limit the number of Cues that can be loaded per frame. 

        private struct EnqueuedSfx
        {
            public string CueName;
            public AudioEmitter Emitter;
            public AudioHandleState State;
        }

        private static Array<EnqueuedSfx> SfxQueue;
        private static Thread SfxThread;

        public static void Initialize(string settingsFile, string waveBankFile, string soundBankFile)
        {
            try
            {
                AudioHandles = new Array<AudioHandleState>();
                AudioEngine  = new AudioEngine(settingsFile);
                WaveBank     = new WaveBank(AudioEngine, waveBankFile, 0, 16);
                SoundBank    = new SoundBank(AudioEngine, soundBankFile);

                while (!WaveBank.IsPrepared)
                    AudioEngine.Update();

                // these are specific to "Audio/ShipGameProject.xgs"
                Global      = AudioEngine.GetCategory("Global");
                Default     = AudioEngine.GetCategory("Default");
                Music       = AudioEngine.GetCategory("Music");
                Weapons     = AudioEngine.GetCategory("Weapons");
                RacialMusic = AudioEngine.GetCategory("RacialMusic");
                CombatMusic = AudioEngine.GetCategory("CombatMusic");

                SfxQueue  = new Array<EnqueuedSfx>(16);
                SfxThread = new Thread(SfxEnqueueThread) {Name = "GameAudioSfx"};
                SfxThread.Start();
            }
            catch (Exception ex)
            {
                AudioEngine = null;
                WaveBank    = null;
                SoundBank   = null;
                Log.Error(ex, "AudioEngine init failed. Please make sure that Speakers/Headphones are attached");
            }
        }

        // called from Game1 Dispose()
        public static void Destroy()
        {
            SfxThread = null;
            if (SfxQueue != null) lock(SfxQueue)
            {
                SfxQueue.Clear();
                SfxQueue = null;
            }

            if (AudioHandles != null) lock (AudioHandles)
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
            ThisFrameSfxCount = 0;
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
            Default.SetVolume(effects);
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

        public static void NegativeClick()    => PlaySfxAsync("UI_Misc20"); // "eek-eek"
        public static void AffirmativeClick() => PlaySfxAsync("echo_affirm1"); // soft "bubble" affirm
        public static void SystemClick()      => PlaySfxAsync("mouse_over4");  // very soft "bumble"
        public static void ShipClicked()      => PlaySfxAsync("techy_affirm1"); // "chu-duk"
        public static void FleetClicked()     => PlaySfxAsync("techy_affirm1");
        public static void PlanetClicked()    => PlaySfxAsync("techy_affirm1");
        public static void BuildItemClicked() => PlaySfxAsync("techy_affirm1");

        public static void ButtonClick()      => PlaySfxAsync("sd_ui_accept_alt3"); // "clihk"
        public static void MiniMapButton()    => PlaySfxAsync("sd_ui_accept_alt3"); // "clihk"
        public static void MiniMapMouseOver() => PlaySfxAsync("sd_ui_mouseover"); // super soft "katik"
        public static void EchoAffirmative()  => PlaySfxAsync("echo_affirm"); // barely audible
        public static void BlipClick()        => PlaySfxAsync("blip_click"); // "blop"

        //subbasewoosh
        public static void SubBassWhoosh() => PlaySfxAsync("sub_bass_whoosh");
        public static void OpenSolarSystemPopUp() => PlaySfxAsync("sub_bass_whoosh");

        // badooommg
        public static void TacticalPause() => PlaySfxAsync("sd_ui_tactical_pause");





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

        private static void SfxEnqueueThread()
        {
            for (;;)
            {
                Thread.Sleep(1);
                if (SfxThread == null)
                    break;
                if (SfxQueue.IsEmpty)
                    continue;

                EnqueuedSfx[] items;
                lock (SfxQueue)
                {
                    items = SfxQueue.ToArray();
                    SfxQueue.Clear();
                }
                for (int i = 0; i < items.Length; ++i)
                {
                    PlaySfx(items[i].CueName, items[i].Emitter, items[i].State);
                }
            }
        }

        private static void PlaySfx(string cueName, AudioEmitter emitter, AudioHandleState state)
        {
            if (ResourceManager.GetModSoundEffect(cueName, out SoundEffect sfx))
            {
                SoundEffectInstance sfxi = sfx.CreateInstance();
                if (emitter != null)
                    sfxi.Apply3D(Empire.Universe.Listener, emitter);
                sfxi.Volume = GlobalStats.EffectsVolume;
                sfxi.Play();

                state?.Completed(null, sfxi);
                lock (AudioHandles) AudioHandles.Add(state ?? new AudioHandleState(null, sfxi));
            }
            else
            {
                Cue cue = SoundBank.GetCue(cueName);
                if (emitter != null)
                    cue.Apply3D(Empire.Universe.Listener, emitter);
                cue.Play();

                state?.Completed(cue, null);
                lock (AudioHandles) AudioHandles.Add(state ?? new AudioHandleState(cue, null));
            }
        }

        public static bool CantPlaySfx(string cueName)
        {
            const int frameSfxLimit = 1; // @ 60fps, this is max 60 samples per second
            return AudioDisabled || EffectsDisabled || ThisFrameSfxCount > frameSfxLimit || cueName.IsEmpty();
        }

        public static void PlaySfxAsync(string cueName, AudioEmitter emitter = null)
        {
            if (CantPlaySfx(cueName))
                return;
            ++ThisFrameSfxCount;
            lock (SfxQueue)
            {
                SfxQueue.Add(new EnqueuedSfx
                {
                    CueName = cueName,
                    Emitter = emitter
                });
            }
        }

        public static void PlaySfxAsync(string cueName, AudioEmitter emitter, AudioHandleState state)
        {
            if (CantPlaySfx(cueName))
                return;
            ++ThisFrameSfxCount;
            lock (SfxQueue)
            {
                SfxQueue.Add(new EnqueuedSfx
                {
                    CueName = cueName,
                    Emitter = emitter,
                    State = state
                });
            }
        }

        public static AudioHandle PlayMusic(string cueName)
        {
            if (AudioDisabled || MusicDisabled || cueName.IsEmpty())
                return default(AudioHandle);

            Cue cue = SoundBank.GetCue(cueName);
            cue.Play();
            return new AudioHandle(cue, null);
        }

        

        private static void DisposeStoppedHandles()
        {
            lock (AudioHandles)
            {
                for (int i = 0; i < AudioHandles.Count; i++)
                {
                    AudioHandleState handle = AudioHandles[i];
                    if (handle.IsStopped)
                    {
                        handle.Destroy();
                        AudioHandles.RemoveAtSwapLast(i--);
                    }
                }
            }
        }
    }
}