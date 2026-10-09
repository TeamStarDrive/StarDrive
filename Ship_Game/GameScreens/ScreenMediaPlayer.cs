using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Audio;
using Ship_Game.Data;
using System;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens
{
    /// <summary>
    /// GameScreen compatible media player which automatically
    /// pauses/resumes video if game screen goes out of focus
    /// and resumes normal game music after media stopped
    /// </summary>
    public sealed class ScreenMediaPlayer : IDisposable
    {
        Video Video;
        readonly VideoPlayer Player;
        readonly GameContentManager Content;
        #pragma warning disable CA2213 // managed by VideoPlayer
        Texture2D Frame; // last good frame, used for looping video transition delay
        #pragma warning restore CA2213
        public bool Active = true;
        public bool Visible = true;

        /// <summary>
        /// Default display rectangle. Reset to video dimensions every time `PlayVideo` is called.
        /// </summary>
        public Rectangle Rect;

        // Extra music associated with the video.
        // For example, diplomacy screen uses WAR music if WarDeclared
        AudioHandle ExtraMusic = AudioHandle.DoNotPlay;

        // If TRUE, the video becomes interactive with a Play button
        public bool EnableInteraction = false;
        public bool IsHovered;

        // If TRUE, the video will always capture low-res video thumbnail
        public bool CaptureThumbnail;

        // If TRUE, NAudio mixer output (music + sound effects) is muted while the video is
        // actively playing and restored on any pause/stop/dispose/screen-deactivate transition.
        // Opt-in so callers like DiplomacyScreen (which want their own racial music) are unaffected.
        public bool MuteGameAudioWhilePlaying;
        bool GameAudioMuted;

        // Last Player.State observed by Update — needed to detect the Playing→Stopped
        // transition (natural end-of-stream) without false-firing during the transient
        // Stopped state right after our Resume()'s Stop+Play sequence.
        MediaState LastSeenPlayerState = MediaState.Stopped;

        // Video play status changed
        public Action OnPlayStatusChange;

        public string Name { get; private set; } = "";
        public Vector2 Size => Video != null ? new Vector2(Video.Width, Video.Height) : Vector2.Zero;

        public bool ReadyToPlay => Frame != null || IsPlaying || IsPaused;
        public bool PlaybackFailed { get; private set; }
        public bool PlaybackSuccess { get; private set; }

        // Player.Play() is too slow, so we start it in a background thread
        TaskResult BeginPlayTask;

        public bool IsDisposed { get; private set; }

        public ScreenMediaPlayer(GameContentManager content, bool looping = true)
        {
            Content = content;
            Player = new VideoPlayer();
            Player.Volume = GlobalStats.MusicVolume;
            // Phase 2.6.A / re-verified Phase 3.7: VideoPlayer.IsLooped setter is
            // STILL unimplemented in MonoGame WindowsDX 3.8.1.303 (the framework
            // upgrade fixed Play/GetTexture but not this). Looping requested by
            // callers is silently dropped; "Loading 2" plays once then stops.
            // The `looping` ctor param is kept for API stability and so future
            // MonoGame upgrades can re-enable by uncommenting the line below.
            // Player.IsLooped = looping;
        }

        ~ScreenMediaPlayer() { Dispose(false); }

        void MuteGameAudioIfRequested()
        {
            if (MuteGameAudioWhilePlaying && !GameAudioMuted)
            {
                // Mixer-level mute: silences NAudio output before it reaches the WasapiOut
                // device, so MediaFoundation video audio (which shares the per-process Windows
                // audio session but bypasses this mixer) stays audible.
                GameAudio.MuteMixerOutput();
                GameAudioMuted = true;
            }
        }

        void RestoreGameAudioIfMuted()
        {
            if (GameAudioMuted)
            {
                GameAudio.RestoreMixerOutput();
                GameAudioMuted = false;
            }
        }

        void Dispose(bool disposing)
        {
            IsDisposed = true;
            Active = false;
            Visible = false;
            OnPlayStatusChange = null;
            Frame = null;
            RestoreGameAudioIfMuted();

            if (ExtraMusic is { IsPlaying: true })
            {
                ExtraMusic.Stop();
                ExtraMusic = null;
            }

            if (Video != null) // avoid double dispose issue
            {
                Video = null;
                if (!Player.IsDisposed)
                {
                    if (Player.State != MediaState.Stopped)
                        Player.Stop();
                    Player.Dispose();
                }
            }

            Mem.Dispose(ref BeginPlayTask);
        }

        // Stops audio and music, then disposes any graphics resources
        public void Dispose()
        {
            if (IsDisposed)
                return;
            if (GlobalStats.DebugAssetLoading) Log.Write(ConsoleColor.Magenta, $"Disposing ScreenMediaPlayer {Name}");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void PlayVideo(string videoPath, bool looping = true, bool startPaused = false)
        {
            if (IsPlaying || IsDisposed)
                return; // video has already started

            // Belt-and-suspenders: if a prior call left audio muted (e.g., caller is
            // re-using this instance and the previous restore path never fired), clear it
            // now so a new call with MuteGameAudioWhilePlaying=false can't leak the mute.
            RestoreGameAudioIfMuted();

            try
            {
                Video = ResourceManager.LoadVideo(Content, videoPath);
                Name = videoPath;
                Rect = new Rectangle(0, 0, Video.Width, Video.Height);

                if (Player.Volume.NotEqual(GlobalStats.MusicVolume))
                    Player.Volume = GlobalStats.MusicVolume;
                // IsLooped setter still unimplemented in 3.8.1.303 (see ctor;
                // re-verified Phase 3.7).
                // Player.IsLooped = looping;

                BeginPlayTask = Parallel.Run(() =>
                {
                    try
                    {
                        Player.Play(Video);
                        if (startPaused)
                        {
                            CaptureThumbnail = true;
                            Player.Pause();
                        }
                        else
                        {
                            // active playback begins immediately when not started paused
                            MuteGameAudioIfRequested();
                        }
                        PlaybackSuccess = true;
                        OnPlayStatusChange?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Player.Play failed: 'Video/{videoPath}' reason: {ex.Message}");
                        PlaybackFailed = true;
                    }
                    finally
                    {
                        BeginPlayTask = null;
                    }
                });
            }
            catch (Exception ex)
            {
                // Mark failed so callers (e.g. DiplomacyScreen.Update gates PlayVideoAndMusic on
                // !PlaybackFailed) stop retrying every frame after a definitive load failure.
                PlaybackFailed = true;
                Log.Warning($"PlayVideo failed: 'Video/{videoPath}' reason: {ex.Message}");
            }
        }

        public void PlayVideoAndMusic(Empire empire, bool warMusic)
        {
            if (IsPlaying || IsDisposed)
                return; // video has already started

            PlayVideo(empire.data.Traits.VideoPath);

            if (empire.data.MusicCue != null && Player.State != MediaState.Playing)
            {
                GameAudio.SwitchToRacialMusic(); // before PlayMusic: a muted RacialMusic category refuses to start the theme
                ExtraMusic = GameAudio.PlayMusic(warMusic ? "CombatMusic" : empire.data.MusicCue);
            }
        }

        public bool IsPlaying => BeginPlayTask != null || (Video != null && Player.State == MediaState.Playing);
        public bool IsPaused  => Video != null && Player.State == MediaState.Paused;
        public bool IsStopped => Video == null || Player.IsDisposed ||
                                                  Player.State == MediaState.Stopped;

        public void Stop()
        {
            if (IsDisposed)
                return;

            Frame = null;
            RestoreGameAudioIfMuted();

            if (!IsStopped)
            {
                Player.Stop();
                OnPlayStatusChange?.Invoke();
            }

            if (ExtraMusic.IsPlaying)
            {
                ExtraMusic.Stop();
                GameAudio.SwitchBackToGenericMusic();
            }
        }

        public void Resume()
        {
            if (IsDisposed)
                return;

            // Stop+Play bypasses MediaSession.Resume E_POINTER after startPaused init
            // (MonoGame 3.8.1.303). Restarts the video from t=0.
            // Gate on "not currently playing" rather than IsPaused — the Play→Pause race
            // on the BeginPlayTask worker can leave Player.State as Stopped, not Paused.
            if (Video != null && Player.State != MediaState.Playing)
            {
                try
                {
                    Player.Stop();
                    Player.Play(Video);
                    MuteGameAudioIfRequested();
                    OnPlayStatusChange?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warning($"ScreenMediaPlayer.Resume Stop+Play failed for '{Name}': {ex.Message}");
                    PlaybackFailed = true;
                    RestoreGameAudioIfMuted();
                }
            }

            if (ExtraMusic.IsPaused)
            {
                ExtraMusic.Resume();
                GameAudio.PauseGenericMusic();
            }
        }

        public void Pause()
        {
            if (IsDisposed)
                return;

            if (IsPlaying)
            {
                Player.Pause();
                RestoreGameAudioIfMuted();
                OnPlayStatusChange?.Invoke();
            }

            if (ExtraMusic.IsPlaying)
            {
                ExtraMusic.Pause();
                GameAudio.SwitchBackToGenericMusic();
            }
        }

        public bool HandleInput(InputState input)
        {
            IsHovered = false;
            if (!Visible || IsDisposed)
                return false;

            if (EnableInteraction)
            {
                IsHovered = Rect.HitTest(input.CursorPosition);
                if (IsPlaying && (input.Escaped || input.RightMouseClick))
                {
                    GameAudio.EchoAffirmative();
                    Pause();
                    return true;
                }
                if (IsHovered && input.InGameSelect)
                {
                    if (!IsPlaying)
                    {
                        GameAudio.EchoAffirmative();
                        Resume();
                    }
                    // always capture input if clicked on video
                    return true;
                }
            }
            return false;
        }

        public void Update(GameScreen screen)
        {
            if (!PlaybackSuccess || IsDisposed || PlaybackFailed)
                return;

            MediaState currentState = Player.State;
            try
            {
                if (Video != null && currentState != MediaState.Stopped)
                {
                    // pause video when game screen goes inactive
                    if (screen.IsActive && currentState == MediaState.Paused)
                    {
                        // Player.Resume() here is MonoGame's VideoPlayer.Resume, NOT our
                        // public Resume() wrapper's Stop+Play workaround. This is intentional:
                        // the wedge that the wrapper guards against was a startup-only race
                        // in BeginPlayTask that left MediaSession in a bad state before any
                        // normal transitions. The pause/resume cycle from a screen-deactivate
                        // hits MediaSession in a stable Paused state and Resume works as
                        // designed, with the bonus of resuming from the paused position
                        // rather than restarting from t=0.
                        Player.Resume();
                        MuteGameAudioIfRequested();
                    }
                    else if (!screen.IsActive && currentState == MediaState.Playing)
                    {
                        Player.Pause();
                        RestoreGameAudioIfMuted();
                    }
                }
                else if (Video != null
                         && LastSeenPlayerState == MediaState.Playing
                         && currentState == MediaState.Stopped)
                {
                    // Playing→Stopped transition = natural end of stream. Reached only when a
                    // MuteGameAudioWhilePlaying caller also drives Update() (CodexScreen
                    // does not — left in place for future opt-in callers like DiplomacyScreen).
                    // LastSeenPlayerState gate avoids false-firing during the transient Stopped
                    // state right after Resume()'s Stop+Play sequence.
                    RestoreGameAudioIfMuted();
                }

                if (!ExtraMusic.IsStopped)
                {
                    // pause music if needed
                    if (screen.IsActive && ExtraMusic.IsPaused)
                        ExtraMusic.Resume();
                    else if (!screen.IsActive && ExtraMusic.IsPlaying)
                        ExtraMusic.Pause();
                }
            }
            catch (Exception ex)
            {
                // Underlying MediaSession can transition to an invalid state (alt-tab, device loss,
                // GPU reset, codec hiccup) and throw E_POINTER from Resume/Pause. Video is incidental;
                // mark failed and bail so the game keeps running. DiplomacyScreen and other callers
                // gate further PlayVideo calls on PlaybackFailed.
                Log.Warning($"ScreenMediaPlayer.Update Pause/Resume failed for '{Name}': {ex.Message}");
                PlaybackFailed = true;
                RestoreGameAudioIfMuted();
            }
            finally
            {
                // Always advance — otherwise a thrown catch leaves LastSeenPlayerState stale
                // and the next frame may misclassify the Playing→Stopped transition.
                LastSeenPlayerState = currentState;
            }
        }

        public void Draw(SpriteBatch batch)
        {
            Draw(batch, Color.White);
        }
        
        public void Draw(SpriteBatch batch, Color color)
        {
            Draw(batch, Rect, color, 0f, SpriteEffects.None);
        }

        public void Draw(SpriteBatch batch, in Rectangle rect, Color color, float rotation, SpriteEffects effects)
        {
            if (!PlaybackSuccess || Player.IsDisposed || !Active || IsDisposed || PlaybackFailed)
                return;

            if (!Visible)
            {
                if (IsPlaying)
                    Stop();
                return;
            }

            if (Video != null && Player.State != MediaState.Stopped)
            {
                // don't grab lo-fi default video thumbnail while video is looping around
                if (CaptureThumbnail || Player.PlayPosition.TotalMilliseconds > 0)
                {
                    try
                    {
                        Frame = Player.GetTexture();
                    }
                    catch (Exception ex)
                    {
                        // Same MediaSession failure mode as Update's Pause/Resume: platform layer can
                        // return a null texture / throw when the underlying video session is invalid.
                        Log.Warning($"ScreenMediaPlayer.Draw GetTexture failed for '{Name}': {ex.Message}");
                        PlaybackFailed = true;
                        return;
                    }
                }
            }

            if (Frame != null)
                batch.Draw(Frame, rect, null, color, rotation, Vector2.Zero, effects, 0.9f);

            if (EnableInteraction)
            {
                batch.DrawRectangle(rect, new Color(32, 30, 18));
                if (IsHovered && Player.State != MediaState.Playing)
                {
                    var playIcon = new Rectangle(rect.CenterX() - 64, rect.CenterY() - 64, 128, 128);
                    batch.Draw(ResourceManager.Texture("icon_play"), playIcon, new Color(255, 255, 255, 200).Premultiplied());
                }
            }
        }
    }
}