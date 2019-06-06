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

        /// <summary>
        /// Default display rectangle. Reset to video dimensions every time `PlayVideo` is called.
        /// </summary>
        public Rectangle Rect;

        // Extra music associated with the video.
        // For example, diplomacy screen uses WAR music if WarDeclared
        AudioHandle Music = AudioHandle.Dummy;

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
            Stop();
            Video = null;
            Player.Dispose();
        }

        public void PlayVideo(string videoPath, bool looping = true)
        {
            try
            {
                Video = ResourceManager.LoadVideo(Content, videoPath);
                Rect = new Rectangle(0, 0, Video.Width, Video.Height);
                Player.IsLooped = looping;

                if (Player.Volume.NotEqual(GlobalStats.MusicVolume))
                    Player.Volume = GlobalStats.MusicVolume;

                Player.Play(Video);
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

        public void Stop()
        {
            Frame = null;

            if (Video != null)
            {
                Player.Stop();
            }

            if (Music.IsPlaying)
            {
                Music.Stop();
                GameAudio.SwitchBackToGenericMusic();
            }
        }

        public void Update(GameScreen screen)
        {
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
            if (Video != null && Player.State != MediaState.Stopped)
            {
                // don't grab lo-fi default video thumbnail while video is looping around
                if (Player.PlayPosition.TotalMilliseconds > 0)
                    Frame = Player.GetTexture();

                if (Frame != null)
                    batch.Draw(Frame, Rect, color);
            }
        }
    }
}