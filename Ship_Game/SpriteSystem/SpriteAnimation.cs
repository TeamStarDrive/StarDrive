using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SpriteAnimation : UIElementV2
    {
        readonly TextureAtlas Atlas;
        int CurrentFrame;
        public bool Looping { get; set; }
        public bool IsAnimating { get; private set; }

        // if true, this animation will freeze at the last frame
        public bool StopAtLastFrame;

        public SpriteAnimation(UIElementV2 parent, string atlasPath, bool autoStart = true)
            : base(parent, Vector2.Zero)
        {
            Atlas = ContentManager.Load<TextureAtlas>(atlasPath);
            if (autoStart) Start();
        }

        public void Start(int startFrame = 0)
        {
            int numFrames = Atlas.Count;
            CurrentFrame = startFrame.Clamped(0, numFrames);
            IsAnimating = numFrames != 0;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!IsAnimating) return;
            if (CurrentFrame >= Atlas.Count)
            {
                if (Looping)
                {
                    CurrentFrame = 0;
                }
                else if (StopAtLastFrame)
                {
                    CurrentFrame = Atlas.Count-1;
                }
                else
                {
                    IsAnimating = false;
                    return;
                }
            }

            SubTexture frame = Atlas[CurrentFrame];
            batch.Draw(frame, Rect, Color.White);
            ++CurrentFrame;
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}
