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
    // volatile: published by Initialize on the main thread after Music/RacialMusic/AudioEngine
    // fields are assigned; read by worker threads (e.g. LoadGame.SetupUniverseScreen) that gate
    // Music access on it. Without volatile the JIT/CPU could reorder so a reader sees the flag
    // true while still seeing stale nulls in the dependent fields.
    static volatile bool AudioEngineGood;
    
    static NAudioPlaybackEngine AudioEngine;
    static string ConfigFile;
    static AudioConfig Config;
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
    // volatile: cross-thread stop signal. Destroy() writes null on the main thread; the
    // SfxEnqueueThread worker reads it each poll and exits when it sees null.
    static volatile Thread SfxThread;

    public static AudioDevices Devices;

    public static void DisableAudio(bool disabled)
    {
        AudioDisabled = disabled;
        if (disabled)
            Destroy();
        else
            Reload();
    }

    /// <summary>
    /// Mod path the current AudioConfig was built from
    /// </summary>
    static string ConfigModPath;

    /// <summary>
    /// True when AudioConfig was built for a different mod than the one now active.
    /// Only meaningful after ResourceManager.InitContentDir has run for that mod.
    /// </summary>
    public static bool ConfigIsStale => AudioEngineGood && ConfigModPath != GlobalStats.ModPath;

    /// <summary>
    /// Rebuilds the whole audio stack: the device, the engine and the AudioConfig,
    /// which is re-read from the currently active mod.
    /// Pass null for the device to re-pick the one the user configured.
    /// </summary>
    public static void Reload(MMDevice device = null)
    {
        Initialize(device, ConfigFile);
    }

    public static void Initialize(MMDevice device, string configFile)
    {
        try
        {
            Destroy(); // just in case
            AudioEngineGood = false; // reset; only flip true once Music/RacialMusic/AudioEngine are all set
            ConfigFile = configFile;

            Devices = new();

            // try selecting an audio device if no argument given
            if (device == null && !Devices.PickAudioDevice(out device))
            {
                Log.Warning("GameAudio is disabled since audio device selection failed.");
                AudioDisabled = true;
                return;
            }

            Log.Info($"GameAudio Initialize Device: {device.FriendlyName}");
            Devices.CurrentDevice = device; // make sure it's always properly in sync

            Config = new(configFile);
            ConfigModPath = ResourceManager.ModContentDir;
            Music = Config.GetCategory("Music");
            RacialMusic = Config.GetCategory("RacialMusic");

            AudioEngine = new(device);

            AsyncSfxQueue = new(16);
            SfxThread = new(SfxEnqueueThread) { Name = "GameAudioSfx" };
            SfxThread.Start();

            // Invariant for callers: AudioEngineGood == true implies Music, RacialMusic and
            // AudioEngine are non-null. Worker threads (e.g. LoadGame.SetupUniverseScreen calling
            // StopGenericMusic) may observe this flag concurrently with a re-init, so flipping
            // it early — before fields are populated — caused NREs on Music.Stop.
            AudioEngineGood = true;
        }
        catch (Exception ex)
        {
            // Almost always user-environment: WASAPI AUDCLNT_E_UNSUPPORTED_FORMAT (0x88890008) from
            // Bluetooth headsets in handsfree/mono, USB DACs with restricted format support, virtual
            // audio devices, or a device-switch race. Game gracefully degrades to silent mode.
            Log.Warning($"AudioEngine init failed (game will run without sound): {ex.Message}");
            Destroy();
            AudioEngineGood = false;
        }
    }

    // called from GameBase.Dispose()
    public static void Destroy()
    {
        AudioEngineGood = false;

        // Stop and join the SFX consumer BEFORE disposing the fields it reads (Config/AudioEngine),
        // so it can't deref torn-down audio state mid-batch. It breaks on SfxThread==null at its
        // next poll and its body is non-blocking, so this returns well within the timeout.
        Thread sfxThread = SfxThread;
        SfxThread = null;
        sfxThread?.Join(1000);

        if (AsyncSfxQueue != null)
        {
            lock (SfxQueueLock)
            {
                AsyncSfxQueue?.Clear();
                AsyncSfxQueue = null;
            }
        }

        Mem.Dispose(ref Config);
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
        // Devices is null when Initialize() failed (AudioEngineGood=false) or after Destroy()/DisableAudio(true).
        if (Devices == null)
            return;

        Devices.HandleEvents();

        if (Devices.ShouldReloadAudioDevice)
        {
            Reload();
            return;
        }

        Config?.Update();
    }

    // Configures GameAudio from GlobalStats MusicVolume and EffectsVolume
    public static void ConfigureAudioSettings(float music, float effects)
    {
        MusicDisabled = music <= 0.001f;
        EffectsDisabled = effects <= 0.001f;
        AudioDisabled = AudioEngineGood && MusicDisabled && EffectsDisabled;
        if (AudioEngineGood)
        {
            Config.SetVolume(music, effects);
        }
    }

    /// <summary>
    /// Stops all music in the Music category
    /// </summary>
    /// <param name="fadeout">Use category fadeout time?</param>
    public static void StopGenericMusic(bool fadeout)
    {
        if (AudioEngineGood)
            Music?.Stop(fadeout);
    }

    public static void PauseGenericMusic()  { if (!IsMusicDisabled) Music.Pause(); }
    public static void ResumeGenericMusic() { if (!IsMusicDisabled) Music.Resume(); }
    public static void MuteGenericMusic()   { if (!IsMusicDisabled) Music.Volume = 0f; }
    public static void UnMuteGenericMusic() { if (!IsMusicDisabled) Music.Volume = GlobalStats.MusicVolume; }

    /// <summary>
    /// Silences all NAudio mixer output (music + SFX) at the mixer level, before it reaches
    /// the WasapiOut device. Per-instance and per-category volumes are untouched, so the
    /// per-sample auto-Stop (which would cause ScreenManager.StartMusic to detect
    /// Music.IsStopped and re-call ConfigureAudioSettings, undoing our mute) does NOT fire.
    /// MonoGame VideoPlayer audio (MediaFoundation, not routed through this mixer) is
    /// unaffected. Pair with RestoreMixerOutput().
    /// <para>
    /// NOTE: Mute is not reference counted — overlapping callers (e.g. two ScreenMediaPlayer
    /// instances opting into MuteGameAudioWhilePlaying at once) are NOT supported. The first
    /// one to stop will RestoreMixerOutput() and the second caller will hear game audio
    /// despite still expecting silence. If a future feature needs concurrent mute requests,
    /// add a counter here or store/restore per-caller volume.
    /// </para>
    /// </summary>
    public static void MuteMixerOutput()
    {
        if (AudioEngineGood)
            AudioEngine.MixerMasterVolume = 0f;
    }

    public static void RestoreMixerOutput()
    {
        if (AudioEngineGood)
            AudioEngine.MixerMasterVolume = 1f;
    }
    public static void MuteRacialMusic()    { if(!IsMusicDisabled) RacialMusic.Volume = 0f;}
    public static void UnMuteRacialMusic()  { if (!IsMusicDisabled) RacialMusic.Volume = GlobalStats.MusicVolume; }

    /// <summary>
    /// Audio distance for projectile sound effects. This uses linear falloff.
    /// </summary>
    public const float ProjectileSfxDistance = 35_000f;

    /// <summary>
    /// Audio distance for all ship sound effects. This uses linear falloff.
    /// Explosion, Detonation, Warp, DieSfx, ShieldImpact
    /// </summary>
    public const float ShipSfxDistance = 35_000f;

    /// <summary>
    /// Audio distance for all planet sound effects. This uses linear falloff.
    /// Planetary bomb impacts, 
    /// </summary>
    public const float PlanetSfxDistance = 50_000f;

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
            // Snapshot: Destroy()/Initialize() can null the queue concurrently during a re-init.
            Array<AsyncSfx> queue = AsyncSfxQueue;
            if (queue == null || queue.IsEmpty)
                continue;

            AsyncSfx[] items;
            lock (SfxQueueLock)
            {
                if (AsyncSfxQueue == null) // re-check under the lock
                    continue;
                items = AsyncSfxQueue.ToArray();
                AsyncSfxQueue.Clear();
            }

            AudioConfig config = Config;
            if (config == null)
                continue; // audio torn down (re-init); drop this batch
            for (int i = 0; i < items.Length; ++i)
            {
                ref AsyncSfx sfx = ref items[i];
                SoundEffect effect = config.GetSoundEffect(sfx.EffectId);
                IAudioInstance instance = PlayEffect(effect, sfx.Emitter);
                if (instance != null)
                {
                    effect.Category.TrackInstance(effect, instance, sfx.Handle);
                }

                // notify AudioHandle that the operation is complete
                AudioHandle handle = sfx.Handle;
                if (handle != null && handle.AsyncPlayStarted)
                    handle.OnAsyncPlayComplete();
            }
        }
    }

    static IAudioInstance PlayEffect(SoundEffect effect, AudioEmitter emitter)
    {
        if (effect == null || !effect.Category.CanPlayEffect(effect))
            return null;

        float volume = effect.GetEffectiveVolume();
        if (volume <= 0.0001f)
            return null; // this effect is muted

        string sfxFile = effect.GetNextSfxFile(Random);
        FileInfo file = ResourceManager.GetModOrVanillaFile("Audio/" + sfxFile);
        if (file == null)
        {
            Log.Warning($"Could not find SFX file: {sfxFile} for SoundEffect: {effect.Id}");
            return null;
        }
        NAudioPlaybackEngine engine = AudioEngine;
        if (engine == null)
            return null; // engine torn down concurrently (re-init)
        return engine.Play(effect.Category, emitter, file.FullName, volume);
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
            // Null-safe under the lock: Destroy()/Initialize() can null the queue concurrently
            // (under this same lock) during an audio re-init; dropping the SFX is correct then.
            AsyncSfxQueue?.Add(new()
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
            // See note above: queue may be nulled mid re-init under this same lock.
            AsyncSfxQueue?.Add(new()
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
        effect.Category.TrackInstance(effect,instance, handle);
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
        Music.TrackInstance(null, instance, handle);
        return handle;
    }
}
