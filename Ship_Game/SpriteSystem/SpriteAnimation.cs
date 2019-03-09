using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SpriteAnimation
    {
        readonly TextureAtlas Atlas;
        public bool Looping { get; set; }
        public bool IsAnimating { get; private set; }

        // if true, this animation will freeze at the last frame
        // this only applies if animation is not looping
        public bool FreezeAtLastFrame;
        public bool VisibleBeforeDelay;

        public float Delay;
        public float CurrentTime;
        public float Duration;

        public string Name => Atlas?.Name ?? "";
        public int NumFrames => Atlas?.Count ?? 0;
        public int Width  { get; private set; }
        public int Height { get; private set; }

        public SpriteAnimation(GameContentManager content, string atlasPath, bool autoStart = true)
        {
            Duration = 1f;
            Atlas = content.LoadTextureAtlas(atlasPath);
            if (Atlas == null)
                return;
            if (autoStart)
            {
                Start();
            }
            else if (Atlas.Count > 0)
            {
                Width  = Atlas[0].Width;
                Height = Atlas[0].Height;
            }
        }

        public SpriteAnimation(TextureAtlas atlas, float duration)
        {
            Atlas = atlas;
            Start(duration);
        }

        public void Start(float duration = 1f, float startAt = 0f, float delay = 0f)
        {
            CurrentTime = startAt.Clamped(startAt, duration);
            Duration    = duration;
            Delay       = delay;
            IsAnimating = Atlas?.Count > 0;
            if (Atlas != null && IsAnimating)
            {
                Width  = Atlas[0].Width;
                Height = Atlas[0].Height;
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsAnimating)
                return;

            if (Delay > 0f)
            {
                Delay -= deltaTime;
                return;
            }

            CurrentTime += deltaTime;
            if (CurrentTime > Duration)
            {
                if (Looping)
                {
                    CurrentTime -= Duration; // wrap around
                }
                else if (FreezeAtLastFrame)
                {
                    CurrentTime = Duration;
                }
                else
                {
                    IsAnimating = false;
                }
            }
        }

        public int CurrentFrameId
        {
            get
            {
                float framePos = (CurrentTime / Duration) * Atlas.Count;
                int frameIdx = ((int)framePos).Clamped(0, Atlas.Count - 1);
                return frameIdx;
            }
        }

        bool IsVisible => IsAnimating && (Delay <= 0f || VisibleBeforeDelay);

        public void Draw(SpriteBatch sb, in Rectangle r)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            sb.Draw(frame, r, Color.White);
        }

        public void Draw(SpriteBatch sb, in Rectangle r, Color color)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            sb.Draw(frame, r, color);
        }

        public void Draw(SpriteBatch sb, in Rectangle r, float rotation, float scale, float z = 0f)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            sb.Draw(frame, r, rotation, scale, z);
        }
        
        public void Draw(SpriteBatch sb, Vector2 pos, Vector2 size, float rot, float scale, float z = 0f)
        {
            if (!IsVisible) return;
            var r = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
            SubTexture frame = Atlas[CurrentFrameId];
            sb.Draw(frame, r, rot, scale, z);
        }

        public void Draw(SpriteBatch sb, Vector2 pos, Color c, float rot, float scale, SpriteEffects e = SpriteEffects.None)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            sb.Draw(frame, pos, c, rot, frame.CenterF, scale, e, 0.9f);
        }

        public void Draw(SpriteBatch sb, in Rectangle r, Color c, float rot, SpriteEffects e = SpriteEffects.None)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            sb.Draw(frame, r, c, rot, Vector2.Zero, e, 0.9f);
        }
    }
}
