using System;

namespace Ship_Game.Audio;

internal readonly struct TrackedHandle
{
    public readonly AudioHandle Handle;
    public readonly IAudioInstance Instance;

    public TrackedHandle(IAudioInstance instance)
    {
        Instance = instance;
    }
    public TrackedHandle(IAudioInstance instance, AudioHandle handle)
    {
        Instance = instance;
        Handle = handle;
    }
        
    public bool IsDisposed => Instance.IsDisposed;
    public bool IsPlaying => Handle?.IsPlaying ?? Instance.IsPlaying;
    public bool IsPaused => Handle?.IsPaused ?? Instance.IsPaused;
    public bool IsStopped => Handle?.IsStopped ?? Instance.IsStopped;

    public void Stop(bool fadeout)
    {
        if (Handle != null) Handle.Stop(fadeout);
        else Instance.Stop(fadeout);
    }
    public void Dispose()
    {
        if (Handle != null) Handle.Destroy();
        else Instance.Dispose();
    }
}

public class AudioHandle
{
    DateTime StartedAt;
    float ReplayTimeout;
    bool Loading;

    // NOTE: no need to dispose the instance, the Audio engine will do it for us
    IAudioInstance Audio;

    // This is a special AudioHandle which can never be played and is always stopped
    public static readonly AudioHandle DoNotPlay = new();

    public AudioHandle() {}
    internal AudioHandle(IAudioInstance audio) => Audio = audio;
     
    public bool IsPlaying => Loading || Audio?.IsPlaying == true;
    public bool IsPaused  => Audio?.IsPaused == true;

    // Returns TRUE if audio is not loading &&
    // has finished playback || has been stopped by user,
    // || if audio system has gone over max cue limit,
    //    the audio system automatically stopped the CUE
    public bool IsStopped
    {
        get
        {
            if (Loading)
                return false;
            IAudioInstance audio = Audio; // flimsy thread safety
            return audio == null || audio.IsStopped;
        }
    }

    // This prevents SFX from instantly replaying after being forcefully stopped
    // Used to circumvent CUE limit issue, where AudioEngine force stops oldest CUE's.
    public bool IsReadyToReplay => ReplayTimeout < 0.0001f // almost 0?
                                   || (DateTime.UtcNow-StartedAt).TotalMilliseconds >= ReplayTimeout;

    public void Pause() => Audio?.Pause();
    public void Resume() => Audio?.Resume();

    // Stops the Audio Cue and resets replay timeout
    public void Stop(bool fadeout = true)
    {
        StartedAt = default;
        ReplayTimeout = 0f;
        IAudioInstance audio = Audio; // flimsy thread safety
        if (audio != null) { Audio = null; audio.Stop(fadeout); }
    }

    // Signals that this audio handle is being destroyed, so all sounds should be stopped
    public void Destroy()
    {
        StartedAt = default;
        ReplayTimeout = 0f;
        IAudioInstance audio = Audio; // flimsy thread safety
        if (audio != null) { Audio = null; audio.Stop(fadeout: false); }
    }

    internal void OnLoaded(IAudioInstance audio)
    {
        Audio = audio;
        Loading = false;
    }

    /// <summary>
    /// NOTE: XNA Sound Banks have hardcoded CUE limits which are unique to each CUE.
    /// If your limit is 8 and you try to play more, then the oldest CUE is instantly stopped.
    /// To work around this issue, you can prevent additional CUE's from being spawned by using
    /// the replayTimeout. This will prevent CUE's from re-firing instantly after they are stopped.
    /// </summary>
    /// <param name="cueName"></param>
    /// <param name="emitter"></param>
    /// <param name="replayTimeout">Minimum seconds before this SFX can be played again. 0: disabled</param>
    public void PlaySfxAsync(string cueName, AudioEmitter emitter, float replayTimeout = 0f)
    {
        if (this == DoNotPlay || Loading || GameAudio.CantPlaySfx(cueName))
            return;

        // prevent SFX from playing before X amount of seconds has elapsed since last start
        if (replayTimeout > 0f)
        {
            DateTime now = DateTime.UtcNow;
            if ((now-StartedAt).TotalSeconds < replayTimeout)
                return; // not ready yet

            StartedAt = now;
            ReplayTimeout = replayTimeout;
        }

        Loading = true;
        GameAudio.PlaySfxAsync(cueName, emitter, this);
    }
}
