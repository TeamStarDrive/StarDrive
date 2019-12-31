using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Audio;
using Ship_Game.Data;

namespace Ship_Game.GameScreens
{
    /// <summary>
    /// GameScreen compatible media player which automatically
    /// pauses/resumes video if game screen goes out of focus
    /// and resumes normal game music after media stopped
    /// </summary>
    public class ScreenMediaPlayer : IDisposable
    {
        Video Video;
        readonly VideoPlayer Player;
        readonly GameContentManager Content;
        Texture2D Frame; // last good frame, used for looping video transition delay

        public bool Visible = true;

        /// <summary>
        /// Default display rectangle. Reset to video dimensions every time `PlayVideo` is called.
        /// </summary>
        public Rectangle Rect;

        // Extra music associated with the video.
        // For example, diplomacy screen uses WAR music if WarDeclared
        AudioHandle Music = AudioHandle.DoNotPlay;

        // If TRUE, the video becomes interactive with a Play button
        public bool EnableInteraction = false;
        public bool IsHovered;

        // If TRUE, the video will always capture low-res video thumbnail
        public bool CaptureThumbnail;

        // Video play status changed
        public Action OnPlayStatusChange;

        public string Name { get; private set; } = "";
        public Vector2 Size => Video != null ? new Vector2(Video.Width, Video.Height) : Vector2.Zero;

        // Player.Play() is too slow, so we start it in a background thread
        TaskResult BeginPlayTask;

        public ScreenMediaPlayer(GameContentManager content, bool looping = true)
        {
            Content = content;
            Player = new VideoPlayer
            {
                Volume = GlobalStats.MusicVolume,
                IsLooped = looping
            };
        }

        // Stops audio and music, then disposes any graphics resources
        public void Dispose()
        {
            if (Video != null) // avoid double dispose issue
            {
                Stop();
                Video = null;
            }
            if (Player?.IsDisposed == false)
                Player.Dispose();
        }

        public void PlayVideo(string videoPath, bool looping = true, bool startPaused = false)
        {
            try
            {
                Video = ResourceManager.LoadVideo(Content, videoPath);
                Name = videoPath;
                Rect = new Rectangle(0, 0, Video.Width, Video.Height);
                Player.IsLooped = looping;

                if (Player.Volume.NotEqual(GlobalStats.MusicVolume))
                    Player.Volume = GlobalStats.MusicVolume;

                BeginPlayTask = Parallel.Run(() =>
                {
                    Player.Play(Video);
                    if (startPaused)
                    {
                        CaptureThumbnail = true;
                        Player.Pause();
                    }
                });

                OnPlayStatusChange?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"PlayVideo failed: 'Video/{videoPath}'");
            }
        }

        public void PlayVideoAndMusic(Empire empire, bool warMusic)
        {
            PlayVideo(empire.data.Traits.VideoPath);

            GameAudio.PauseGenericMusic();

            if (empire.data.MusicCue != null)
            {
                GameAudio.PauseGenericMusic();
                string warCue = "Stardrive_Combat 1c_114BPM";
                Music = GameAudio.PlayMusic(warMusic ? warCue : empire.data.MusicCue);
            }
        }

        public bool IsPlaying => Video != null && Player.State == MediaState.Playing;
        public bool IsPaused  => Video != null && Player.State == MediaState.Paused;
        public bool IsStopped => Video == null || Player.IsDisposed ||
                                                  Player.State == MediaState.Stopped;

        public void Stop()
        {
            Frame = null;

            if (!IsStopped)
            {
                Player.Stop();
                OnPlayStatusChange?.Invoke();
            }

            if (Music.IsPlaying)
            {
                Music.Stop();
                GameAudio.SwitchBackToGenericMusic();
            }
        }

        public void Resume()
        {
            if (IsPaused)
            {
                Player.Resume();
                OnPlayStatusChange?.Invoke();
            }
            
            if (Music.IsPaused)
            {
                Music.Resume();
                GameAudio.PauseGenericMusic();
            }
        }

        public void Pause()
        {
            if (IsPlaying)
            {
                Player.Pause();
                OnPlayStatusChange?.Invoke();
            }
            
            if (Music.IsPlaying)
            {
                Music.Pause();
                GameAudio.SwitchBackToGenericMusic();
            }
        }

        public void TogglePlay()
        {
            if       (IsPaused) Resume();
            else if (IsPlaying) Pause();
        }

        public bool HandleInput(InputState input)
        {
            IsHovered = false;
            if (!Visible)
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
            if (BeginPlayTask?.IsComplete != true)
                return;

            if (Video != null && Player.State != MediaState.Stopped)
            {
                // pause video when game screen goes inactive
                if (screen.IsActive && Player.State == MediaState.Paused)
                    Player.Resume();
                else if (!screen.IsActive && Player.State == MediaState.Playing)
                    Player.Pause();
            }

            if (!Music.IsStopped)
            {
                // pause music if needed
                if (screen.IsActive && Music.IsPaused)
                    Music.Resume();
                else if (!screen.IsActive && Music.IsPlaying)
                    Music.Pause();
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
            if (BeginPlayTask?.IsComplete != true)
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
                    Frame = Player.GetTexture();

                if (Frame != null)
                    batch.Draw(Frame, rect, null, color, rotation, Vector2.Zero, effects, 0.9f);
            }
            
            if (EnableInteraction)
            {
                batch.DrawRectangle(rect, new Color(32, 30, 18));
                if (IsHovered && Player.State != MediaState.Playing)
                {
                    var playIcon = new Rectangle(rect.CenterX() - 64, rect.CenterY() - 64, 128, 128);
                    batch.Draw(ResourceManager.Texture("icon_play"), playIcon, new Color(255, 255, 255, 200));
                }
            }
        }
    }
}