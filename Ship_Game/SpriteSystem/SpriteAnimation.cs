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

        public void Draw(SpriteBatch batch, Rectangle rect)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            batch.Draw(frame, rect, Color.White);
        }

        public void Draw(SpriteBatch batch, Rectangle rect, Color color)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            batch.Draw(frame, rect, color);
        }

        public void Draw(SpriteBatch batch, Rectangle rect, 
                         float rotation, float scale, float z = 0f)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            batch.Draw(frame, rect, rotation, scale, z);
        }
        
        public void Draw(SpriteBatch batch, Vector2 pos, Vector2 size, 
                         float rotation, float scale, float z = 0f)
        {
            if (!IsVisible) return;
            var r = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
            SubTexture frame = Atlas[CurrentFrameId];
            batch.Draw(frame, r, rotation, scale, z);
        }

        public void Draw(SpriteBatch batch, Vector2 pos, Color c, float rotation, float scale, SpriteEffects effects = SpriteEffects.None)
        {
            if (!IsVisible) return;
            SubTexture frame = Atlas[CurrentFrameId];
            batch.Draw(frame, pos, c, rotation, frame.CenterF, scale, effects, 0.9f);
        }
    }

    public class UISpriteElement : UIElementV2
    {
        public SpriteAnimation Animation;

        public UISpriteElement(UIElementV2 parent, string atlasPath, bool autoStart = true)
            : base(parent, Vector2.Zero)
        {
            Animation = new SpriteAnimation(parent.ContentManager, atlasPath, autoStart);
            Size = new Vector2(Animation.Width, Animation.Height);
        }

        public override void Update(float deltaTime)
        {
            Animation.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            Animation.Draw(batch, Rect);
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}
