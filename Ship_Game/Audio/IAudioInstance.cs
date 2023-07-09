using System;

namespace Ship_Game.Audio;

/// <summary>
/// Allows to generalize which audio backend we're using
/// </summary>
internal interface IAudioInstance : IDisposable
{
    /// <summary>
    /// Returns TRUE if audio is playing and isn't paused/stopped/disposed
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Returns TRUE if `Pause()` was called on this audio instance
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Returns TRUE if this audio instance was stopped/disposed(end-of-stream)
    /// </summary>
    bool IsStopped { get; }

    /// <summary>
    /// Returns TRUE if this audio instance was disposed
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Returns TRUE if the audio system is allowed to dispose this instance
    /// </summary>
    bool CanBeDisposed { get; }

    /// <summary>
    /// Pauses the audio instance. Only works if audio is playing.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the audio instance. Only works if audio is paused.
    /// </summary>
    void Resume();

    /// <summary>
    /// Stops the audio instance, optionally gently fading out
    /// </summary>
    /// <param name="fadeout">if true, then SoundEffect.FadeOutTime or Category.FadeOutTime is used</param>
    void Stop(bool fadeout);
}
