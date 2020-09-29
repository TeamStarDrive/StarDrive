using System;
using System.Management;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NAudio.CoreAudioApi;
using Cue = Microsoft.Xna.Framework.Audio.Cue;

namespace Ship_Game.Audio
{
    // Whole StarDrive game audio state is managed here
    public static class GameAudio
    {
        static AudioEngine AudioEngine;
        static SoundBank SoundBank;
        static WaveBank WaveBank;
        static AudioListener Listener;
        static bool AudioDisabled;
        static bool EffectsDisabled;
        static bool MusicDisabled;
        static bool AudioEngineGood;

        static AudioCategory Global;
        static AudioCategory Default;
        static AudioCategory Weapons;
        static AudioCategory Music;
        static AudioCategory RacialMusic;
        static AudioCategory CombatMusic;

        struct TrackedHandle
        {
            public IAudioHandle Handle;
            public IAudioInstance Instance;
            public bool IsStopped()
            {
                if (Handle != null) return Handle.IsStopped;
                return Instance.IsStopped;
            }
            public void DestroyInstance()
            {
                if (Handle != null) Handle.Destroy();
                else Instance.Dispose();
            }
        }

        static Array<TrackedHandle> TrackedInstances;
        static int ThisFrameSfxCount; // Limit the number of Cues that can be loaded per frame. 

        struct QueuedSfx
        {
            public string CueName;
            public AudioEmitter Emitter;
            public IAudioHandle Handle;
        }

        static Array<QueuedSfx> SfxQueue;
        static Thread SfxThread;

        static string SettingsFile, WaveBankFile, SoundBankFile;

        public static void ReloadAfterDeviceChange(MMDevice newDevice)
        {
            Initialize(newDevice, SettingsFile, WaveBankFile, SoundBankFile);
        }

        public static void Initialize(MMDevice device, string settingsFile, string waveBankFile, string soundBankFile)
        {
            try
            {
                Destroy(); // just in case
                AudioEngineGood = true;
                // try selecting an audio device if no argument given
                if (device == null && !AudioDevices.PickAudioDevice(out device))
                {
                    Log.Warning("GameAudio is disabled since audio device selection failed.");
                    AudioDisabled = true;
                    AudioEngineGood = false;
                    return;
                }

                Log.Info($"GameAudio.Device: {device.FriendlyName}");
                AudioDevices.CurrentDevice = device; // make sure it's always properly in sync

                SettingsFile  = settingsFile;
                WaveBankFile  = waveBankFile;
                SoundBankFile = soundBankFile;

                AudioEngine      = new AudioEngine(settingsFile, TimeSpan.FromMilliseconds(250), device.ID);
                WaveBank         = new WaveBank(AudioEngine, waveBankFile, 0, 16);
                SoundBank        = new SoundBank(AudioEngine, soundBankFile);
                TrackedInstances = new Array<TrackedHandle>();

                while (!WaveBank.IsPrepared)
                    AudioEngine.Update();

                // these are specific to "Audio/ShipGameProject.xgs"
                Global      = AudioEngine.GetCategory("Global");
                Default     = AudioEngine.GetCategory("Default");
                Music       = AudioEngine.GetCategory("Music");
                Weapons     = AudioEngine.GetCategory("Weapons");
                RacialMusic = AudioEngine.GetCategory("RacialMusic");
                CombatMusic = AudioEngine.GetCategory("CombatMusic");

                SfxQueue  = new Array<QueuedSfx>(16);
                SfxThread = new Thread(SfxEnqueueThread) { Name = "GameAudioSfx" };
                SfxThread.Start();

                Listener = new AudioListener();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AudioEngine init failed. Please make sure that Speakers/Headphones are attached");
                Destroy();
                AudioEngineGood = false;
            }
        }

        // called from Game1 Dispose()
        public static void Destroy()
        {
            SfxThread = null;
            if (SfxQueue != null) lock (SfxQueue)
                {
                    SfxQueue.Clear();
                    SfxQueue = null;
                }

            if (TrackedInstances != null) lock (TrackedInstances)
                {
                    for (int i = 0; i < TrackedInstances.Count; ++i)
                        TrackedInstances[i].DestroyInstance();
                    TrackedInstances.Clear();
                }
            SoundBank?.Dispose(ref SoundBank);
            WaveBank?.Dispose(ref WaveBank);
            AudioEngine?.Dispose(ref AudioEngine);
        }


        // this is called from Game1.Update() every frame
        public static void Update()
        {
            ThisFrameSfxCount = 0;

            AudioDevices.HandleEvents();

            AudioEngine?.Update();

            DisposeStoppedInstances();
        }

        public static void Update3DSound(in Vector3 listenerPosition)
        {
            if (AudioEngineGood)
                Listener.Position = listenerPosition;
        }

        // Configures GameAudio from GlobalStats MusicVolume and EffectsVolume
        public static void ConfigureAudioSettings()
        {
            float music   = GlobalStats.MusicVolume;
            float effects = GlobalStats.EffectsVolume;

            MusicDisabled   = music <= 0.001f;
            EffectsDisabled = effects <= 0.001f;
            AudioDisabled   = AudioEngineGood && MusicDisabled && EffectsDisabled;
            if (!AudioEngineGood) return;

            Global.SetVolume(AudioDisabled ? 0.0f : 1.0f);
            Music.SetVolume(music);
            RacialMusic.SetVolume(music);
            CombatMusic.SetVolume(music);
            Weapons.SetVolume(effects);
            Default.SetVolume(effects);
        }

        public static void StopGenericMusic(bool immediate = true)
        {
            if (AudioEngineGood)
                Music.Stop(immediate ? AudioStopOptions.Immediate : AudioStopOptions.AsAuthored);
        }

        public static void PauseGenericMusic()  { if (!CantPlayMusic()) Music.Pause(); }
        public static void ResumeGenericMusic() { if (!CantPlayMusic()) Music.Resume(); }
        public static void MuteGenericMusic()   { if (!CantPlayMusic()) Music.SetVolume(0f); }
        public static void UnMuteGenericMusic() { if (CantPlayMusic()) Music.SetVolume(GlobalStats.MusicVolume); }
        public static void MuteRacialMusic()    { if(!CantPlayMusic()) RacialMusic.SetVolume(0f);}
        public static void UnMuteRacialMusic()  { if (!CantPlayMusic()) RacialMusic.SetVolume(GlobalStats.MusicVolume); }

        public static void NegativeClick()    => PlaySfxAsync("UI_Misc20"); // "eek-eek"
        public static void AffirmativeClick() => PlaySfxAsync("echo_affirm1"); // soft "bubble" affirm
        public static void MouseOver()        => PlaySfxAsync("mouse_over4");  // very soft "bumble"
        public static void ShipClicked()      => PlaySfxAsync("techy_affirm1"); // "chu-duk"
        public static void FleetClicked()     => PlaySfxAsync("techy_affirm1");
        public static void PlanetClicked()    => PlaySfxAsync("techy_affirm1");
        public static void BuildItemClicked() => PlaySfxAsync("techy_affirm1");

        public static void AcceptClick()      => PlaySfxAsync("sd_ui_accept_alt3"); // "clihk"
        public static void ButtonMouseOver()  => PlaySfxAsync("sd_ui_mouseover"); // super soft "katik"
        public static void ResearchSelect()   => PlaySfxAsync("sd_ui_research_select");
        public static void EchoAffirmative()  => PlaySfxAsync("echo_affirm"); // barely audible
        public static void BlipClick()        => PlaySfxAsync("blip_click"); // "blop"

        //subbasewoosh
        public static void SubBassWhoosh()        => PlaySfxAsync("sub_bass_whoosh");
        public static void OpenSolarSystemPopUp() => PlaySfxAsync("sub_bass_whoosh");
        public static void SubBassMouseOver()     => PlaySfxAsync("sub_bass_mouseover");

        // badooommg
        public static void TacticalPause() => PlaySfxAsync("sd_ui_tactical_pause");
        public static void TroopTakeOff()  => PlaySfxAsync("sd_troop_takeoff");
        public static void TroopLand()     => PlaySfxAsync("sd_troop_land");
        public static void SmallServo()    => PlaySfxAsync("smallservo"); // module placement sound

        // this is used for the DiplomacyScreen
        public static void SwitchToRacialMusic()
        {
            if (CantPlayMusic()) return;
            Music.Pause();
            RacialMusic.SetVolume(GlobalStats.MusicVolume);
        }

        public static void SwitchBackToGenericMusic()
        {
            if (CantPlayMusic()) return;            
            Music.Resume();
            RacialMusic.Stop(AudioStopOptions.Immediate);
        }

        static void SfxEnqueueThread()
        {
            for (;;)
            {
                Thread.Sleep(1);
                if (SfxThread == null)
                    break;
                if (SfxQueue.IsEmpty)
                    continue;

                QueuedSfx[] items;
                lock (SfxQueue)
                {
                    items = SfxQueue.ToArray();
                    SfxQueue.Clear();
                }
                for (int i = 0; i < items.Length; ++i)
                {
                    PlaySfx(items[i].CueName, items[i].Emitter, items[i].Handle);
                }
            }
        }

        static void PlaySfx(string cueName, AudioEmitter emitter, IAudioHandle handle)
        {
            IAudioInstance instance;
            if (ResourceManager.GetModSoundEffect(cueName, out SoundEffect sfx))
            {
                SoundEffectInstance inst = sfx.CreateInstance();
                if (emitter != null)
                    inst.Apply3D(Listener, emitter);
                inst.Volume = GlobalStats.EffectsVolume;
                instance = new SfxInstance(inst);
            }
            else
            {
                Cue cue = SoundBank.GetCue(cueName);
                if (emitter != null)
                    cue.Apply3D(Listener, emitter);
                instance = new CueInstance(cue);
            }

            TrackedHandle tracked;
            if (handle != null)
            {
                handle.OnLoaded(instance);
                tracked = new TrackedHandle { Handle = handle };
            }
            else
            {
                tracked = new TrackedHandle { Instance = instance };
            }

            lock (TrackedInstances)
            {
                TrackedInstances.Add(tracked);
            }
        }

        public static bool CantPlaySfx(string cueName)
        {
            const int frameSfxLimit = 1; // @ 60fps, this is max 60 SFX per second
            return !AudioEngineGood || AudioDisabled || EffectsDisabled || ThisFrameSfxCount > frameSfxLimit || cueName.IsEmpty();
        }

        public static void PlaySfxAsync(string cueName, AudioEmitter emitter = null)
        {
            if (CantPlaySfx(cueName))
                return;
            ++ThisFrameSfxCount;
            lock (SfxQueue)
            {
                SfxQueue.Add(new QueuedSfx
                {
                    CueName = cueName,
                    Emitter = emitter
                });
            }
        }

        internal static void PlaySfxAsync(string cueName, AudioEmitter emitter, IAudioHandle handle)
        {
            ++ThisFrameSfxCount;
            lock (SfxQueue)
            {
                SfxQueue.Add(new QueuedSfx
                {
                    CueName = cueName,
                    Emitter = emitter,
                    Handle = handle
                });
            }
        }

        static bool CantPlayMusic(string music)
        {
            return !AudioEngineGood || AudioDisabled || MusicDisabled || music.IsEmpty();
        }

        static bool CantPlayMusic() => !AudioEngineGood || AudioDisabled || MusicDisabled;

        public static AudioHandle PlayMusic(string cueName)
        {
            if (CantPlayMusic(cueName))
                return AudioHandle.DoNotPlay;
            return new AudioHandle(new CueInstance(SoundBank.GetCue(cueName)));
        }

        public static AudioHandle PlayMp3(string mp3File)
        {
            if (CantPlayMusic(mp3File))
                return AudioHandle.DoNotPlay;
            return new AudioHandle(new Mp3Instance(mp3File));
        }

        static void DisposeStoppedInstances()
        {
            if (TrackedInstances == null) return;

            lock (TrackedInstances)
            {
                for (int i = 0; i < TrackedInstances.Count; i++)
                {
                    TrackedHandle audio = TrackedInstances[i];
                    if (audio.IsStopped())
                    {
                        audio.DestroyInstance();
                        TrackedInstances.RemoveAtSwapLast(i--);
                    }
                }
            }
        }
    }
}