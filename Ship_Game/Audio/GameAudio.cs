using System;
using System.Threading;
using SDGraphics;
using SDUtils;
using System.IO;
using Ship_Game.Utils;
using NAudio.CoreAudioApi;
using Ship_Game.Audio.NAudio;

namespace Ship_Game.Audio;

/// <summary>
/// StarDrive game audio state is managed here
/// </summary>
public static class GameAudio
{
    static bool AudioDisabled;
    static bool EffectsDisabled;
    static bool MusicDisabled;
    static bool AudioEngineGood;
    
    static NAudioPlaybackEngine AudioEngine;
    static string ConfigFile;
    static AudioConfig Config;
    static AudioCategory Default;
    static AudioCategory Weapons;
    static AudioCategory Music;
    static AudioCategory RacialMusic;

    static readonly RandomBase Random = new ThreadSafeRandom();
    
    static readonly object SfxQueueLock = new();

    struct AsyncSfx
    {
        public string EffectId;
        public AudioEmitter Emitter;
        public AudioHandle Handle;
    }

    static Array<AsyncSfx> AsyncSfxQueue;
    static Thread SfxThread;

    public static AudioDevices Devices;

    public static void DisableAudio(bool disabled)
    {
        AudioDisabled = disabled;
        if (disabled)
            Destroy();
        else
            ReloadAfterDeviceChange(null);
    }

    public static void ReloadAfterDeviceChange(MMDevice newDevice)
    {
        Initialize(newDevice, ConfigFile);
    }

    public static void Initialize(MMDevice device, string configFile)
    {
        try
        {
            Destroy(); // just in case

            Devices = new();
            AudioEngineGood = true;

            // try selecting an audio device if no argument given
            if (device == null && !Devices.PickAudioDevice(out device))
            {
                Log.Warning("GameAudio is disabled since audio device selection failed.");
                AudioDisabled = true;
                AudioEngineGood = false;
                return;
            }

            Log.Info($"GameAudio Initialize Device: {device.FriendlyName}");
            Devices.CurrentDevice = device; // make sure it's always properly in sync

            ConfigFile = configFile;
            Config = new(configFile);
            Default = Config.GetCategory("Default");
            Weapons = Config.GetCategory("Weapons");
            Music = Config.GetCategory("Music");
            RacialMusic = Config.GetCategory("RacialMusic");

            AudioEngine = new(device);

            AsyncSfxQueue = new(16);
            SfxThread = new(SfxEnqueueThread) { Name = "GameAudioSfx" };
            SfxThread.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AudioEngine init failed. Make sure that Speakers/Headphones are attached");
            Destroy();
            AudioEngineGood = false;
        }
    }

    // called from GameBase.Dispose()
    public static void Destroy()
    {
        SfxThread = null;
        if (AsyncSfxQueue != null)
        {
            lock (SfxQueueLock)
            {
                AsyncSfxQueue?.Clear();
                AsyncSfxQueue = null;
            }
        }

        Mem.Dispose(ref Config);
        Default = null;
        Weapons = null;
        Music = null;
        RacialMusic = null;

        Mem.Dispose(ref AudioEngine);
        Mem.Dispose(ref Devices);
    }

    public static void Update3DSound(in Vector3 listenerPos)
    {
        Config?.SetListenerPos(listenerPos);
    }

    // this is called from Game1.Update() every frame
    public static void Update()
    {
        Devices.HandleEvents();

        if (Devices.ShouldReloadAudioDevice)
        {
            ReloadAfterDeviceChange(null);
            return;
        }

        Config?.Update();
    }

    // Configures GameAudio from GlobalStats MusicVolume and EffectsVolume
    public static void ConfigureAudioSettings()
    {
        // TODO: we can add a global volume control for all audio too
        float music   = GlobalStats.MusicVolume;
        float effects = GlobalStats.EffectsVolume;

        MusicDisabled = music <= 0.001f;
        EffectsDisabled = effects <= 0.001f;
        AudioDisabled = AudioEngineGood && MusicDisabled && EffectsDisabled;
        if (!AudioEngineGood)
            return;

        Default.Volume = AudioDisabled ? 0.0f : 1.0f;
        Music.Volume = music;
        RacialMusic.Volume = music;
        Weapons.Volume = effects;
        Default.Volume = effects;
    }

    /// <summary>
    /// Stops all music in the Music category
    /// </summary>
    /// <param name="fadeout">Use category fadeout time?</param>
    public static void StopGenericMusic(bool fadeout)
    {
        if (AudioEngineGood)
            Music.Stop(fadeout);
    }

    public static void PauseGenericMusic()  { if (!IsMusicDisabled) Music.Pause(); }
    public static void ResumeGenericMusic() { if (!IsMusicDisabled) Music.Resume(); }
    public static void MuteGenericMusic()   { if (!IsMusicDisabled) Music.Volume = 0f; }
    public static void UnMuteGenericMusic() { if (!IsMusicDisabled) Music.Volume = GlobalStats.MusicVolume; }
    public static void MuteRacialMusic()    { if(!IsMusicDisabled) RacialMusic.Volume = 0f;}
    public static void UnMuteRacialMusic()  { if (!IsMusicDisabled) RacialMusic.Volume = GlobalStats.MusicVolume; }

    public static void NegativeClick()    => PlaySfxAsync("UI_Misc20"); // "eek-eek"
    public static void AffirmativeClick() => PlaySfxAsync("echo_affirm1"); // soft "bubble" affirm
    public static void MouseOver()        => PlaySfxAsync("mouse_over4");  // very soft "bumble"
    public static void ShipClicked()      => PlaySfxAsync("techy_affirm1"); // "chu-duk"
    public static void FleetClicked()     => PlaySfxAsync("techy_affirm1");
    public static void PlanetClicked()    => PlaySfxAsync("techy_affirm1");
    public static void BuildItemClicked() => PlaySfxAsync("techy_affirm1");
    public static void DesignSoftBeep()   => PlaySfxAsync("simple_beep"); // "blup"

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

    // General danger notification sound
    public static void NotifyAlert() => PlaySfxAsync("sd_notify_alert");

    // this is used for the DiplomacyScreen
    public static void SwitchToRacialMusic()
    {
        if (IsMusicDisabled) return;
        Music.Pause();
        RacialMusic.Volume = GlobalStats.MusicVolume;
    }

    public static void SwitchBackToGenericMusic()
    {
        if (IsMusicDisabled) return;            
        Music.Resume();
        RacialMusic.Stop(fadeout: false);
    }

    static void SfxEnqueueThread()
    {
        for (;;)
        {
            Thread.Sleep(1); // suspend for 1-15ms
            if (SfxThread == null)
                break;
            if (AsyncSfxQueue.IsEmpty)
                continue;

            AsyncSfx[] items;
            lock (SfxQueueLock)
            {
                items = AsyncSfxQueue.ToArray();
                AsyncSfxQueue.Clear();
            }

            for (int i = 0; i < items.Length; ++i)
            {
                SoundEffect effect = Config.GetSoundEffect(items[i].EffectId);
                IAudioInstance instance = PlayEffect(effect, items[i].Emitter);
                if (instance != null)
                {
                    effect.Category.TrackInstance(instance, items[i].Handle);
                }
            }
        }
    }

    static IAudioInstance PlayEffect(SoundEffect effect, AudioEmitter emitter)
    {
        if (effect == null || effect.Category.IsQueueFull)
            return null;

        float volume = effect.Category.Volume * effect.Volume;
        if (volume <= 0.0001f)
            return null; // this effect is muted

        string sfxFile = effect.GetNextSfxFile(Random);
        FileInfo file = ResourceManager.GetModOrVanillaFile("Audio/" + sfxFile);
        if (file == null)
        {
            Log.Warning($"Could not find SFX file: {sfxFile} for SoundEffect: {effect.Id}");
            return null;
        }
        return AudioEngine.Play(effect.Category, emitter, file.FullName, volume);
    }

    static IAudioInstance PlayFromFile(AudioCategory category, string audioFile)
    {
        FileInfo file = ResourceManager.GetModOrVanillaFile(audioFile);
        if (file == null)
        {
            Log.Warning($"Could not find audio file: {audioFile}");
            return null;
        }
        return AudioEngine.Play(category, null, file.FullName, category.Volume);
    }

    public static bool CantPlaySfx(string cueName)
    {
        return !AudioEngineGood || AudioDisabled || EffectsDisabled || cueName.IsEmpty();
    }

    public static void PlaySfxAsync(string effectId, AudioEmitter emitter = null)
    {
        if (CantPlaySfx(effectId))
            return;
        lock (SfxQueueLock)
        {
            AsyncSfxQueue.Add(new()
            {
                EffectId = effectId,
                Emitter = emitter,
            });
        }
    }

    internal static void PlaySfxAsync(string effectId, AudioEmitter emitter, AudioHandle handle)
    {
        lock (SfxQueueLock)
        {
            AsyncSfxQueue.Add(new()
            {
                EffectId = effectId,
                Emitter = emitter,
                Handle = handle,
            });
        }
    }
        
    public static bool IsMusicDisabled => !AudioEngineGood || AudioDisabled || MusicDisabled;

    static bool CantPlayMusic(string music) // returns true if music is muted
    {
        return IsMusicDisabled || music.IsEmpty();
    }

    /// <summary>
    /// Play music from an effect id
    /// </summary>
    public static AudioHandle PlayMusic(string effectId)
    {
        if (CantPlayMusic(effectId))
            return AudioHandle.DoNotPlay;
        
        SoundEffect effect = Config.GetSoundEffect(effectId);
        IAudioInstance instance = PlayEffect(effect, emitter: null);
        if (instance == null)
            return AudioHandle.DoNotPlay;

        AudioHandle handle = new();
        effect.Category.TrackInstance(instance, handle);
        return handle;
    }

    /// <summary>
    /// Play music from a content file
    /// </summary>
    public static AudioHandle PlayMusicFile(string audioFile)
    {
        if (CantPlayMusic(audioFile))
            return AudioHandle.DoNotPlay;

        IAudioInstance instance = PlayFromFile(Music, audioFile);
        if (instance == null)
            return AudioHandle.DoNotPlay;
            
        AudioHandle handle = new();
        Music.TrackInstance(instance, handle);
        return handle;
    }
}
