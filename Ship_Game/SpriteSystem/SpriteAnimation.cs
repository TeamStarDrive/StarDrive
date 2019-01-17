using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SpriteAnimation
    {
        readonly TextureAtlas Atlas;
        int CurrentFrame;
        public bool Looping { get; set; }
        public bool IsAnimating { get; private set; }

        public SpriteAnimation(TextureAtlas atlas, bool autoStart = true)
        {
            Atlas = atlas;
            if (autoStart) Start();
        }

        public void Start(int startFrame = 0)
        {
            int numFrames = Atlas.Count;
            CurrentFrame = startFrame.Clamped(0, numFrames);
            IsAnimating = numFrames != 0;
        }

        public void Draw(SpriteBatch batch, Rectangle rect)
        {
            if (!IsAnimating) return;
            if (CurrentFrame >= Atlas.Count)
            {
                if (Looping)
                {
                    CurrentFrame = 0;
                }
                else
                {
                    IsAnimating = false;
                    return;
                }
            }

            SubTexture frame = Atlas[CurrentFrame];
            batch.Draw(frame, rect, Color.White);
            ++CurrentFrame;
        }
    }
}
