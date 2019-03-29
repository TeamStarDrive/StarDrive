using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

namespace Ship_Game.SpriteSystem
{
    // Encapsulates SubTextures or SpriteAnimations
    // Within a single abstract entity
    public class DrawableSprite
    {
        SpriteAnimation Anim;
        SubTexture Tex;

        public SpriteEffects Effects;
        public float Rotation;
        public string Name => Tex?.Name ?? Anim?.Name ?? "";

        public DrawableSprite(SpriteEffects effect = SpriteEffects.None)
        {
            Effects = effect;
        }

        public DrawableSprite(GameContentManager content, string animation, bool looping)
        {
            Anim = new SpriteAnimation(content, "Textures/" + animation)
            {
                Looping = looping
            };
        }

        public DrawableSprite(GameContentManager content, string texture)
        {
            var tex = content.Load<Texture2D>("Textures/" + texture);
            Tex = new SubTexture(texture, tex);
        }

        public DrawableSprite(SubTexture texture)
        {
            Tex = texture;
        }

        public DrawableSprite(SpriteAnimation spriteAnim)
        {
            Anim = spriteAnim;
        }

        public static DrawableSprite Animation(GameContentManager content, string animation, bool looping)
        {
            return new DrawableSprite(content, animation, looping);
        }

        public static DrawableSprite Texture2D(GameContentManager content, string texture)
        {
            return new DrawableSprite(content, texture);
        }

        public void Update(float deltaTime)
        {
            Anim?.Update(deltaTime);
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

        public void Draw(SpriteBatch batch, in Rectangle rect, Color color)
        {
            if (Anim != null)
            {
                Anim.Draw(batch, rect, color, Rotation, Effects);
            }
            else
            {
                batch.Draw(Tex, rect, color, Rotation, Vector2.Zero, Effects, 0.9f);
            }
        }
    }
}
