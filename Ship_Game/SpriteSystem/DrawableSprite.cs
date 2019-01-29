using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.SpriteSystem
{
    // Encapsulates SubTextures or SpriteAnimations
    // Within a single abstract entity
    public class DrawableSprite
    {
        public SpriteEffects Effects;
        SpriteAnimation Anim;
        SubTexture Tex;
        public float Rotation;

        public DrawableSprite(SpriteEffects effect = SpriteEffects.None)
        {
            Effects = effect;
        }

        public void Animation(GameContentManager content, string animation, bool looping)
        {
            Anim = new SpriteAnimation(content, "Textures/" + animation)
            {
                Looping = looping
            };
        }

        public void Texture2D(GameContentManager content, string texture)
        {
            var tex = content.Load<Texture2D>("Textures/" + texture);
            Tex = new SubTexture(texture, tex);
        }

        public void Update(float deltaTime)
        {
            if (Anim != null)
                Anim.Update(deltaTime);
        }

        public void Draw(SpriteBatch batch, Vector2 screenPos, float scale, Color color)
        {
            if (Anim != null)
            {
                Anim.Draw(batch, screenPos, color, Rotation, scale, Effects);
            }
            else
            {
                batch.Draw(Tex, screenPos, color, Rotation, Tex.CenterF, scale, Effects, 0.9f);
            }
        }
    }
}
