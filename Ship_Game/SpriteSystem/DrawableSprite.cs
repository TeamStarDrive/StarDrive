using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.GameScreens;

namespace Ship_Game.SpriteSystem
{
    // Encapsulates SubTextures or SpriteAnimations
    // Within a single abstract entity
    public class DrawableSprite
    {
        readonly SpriteAnimation Anim;
        readonly SubTexture Tex;
        readonly ScreenMediaPlayer Vid;

        public SpriteEffects Effects;
        public float Rotation;
        public string Name => Tex?.Name ?? Anim?.Name ?? Vid?.Name ?? "";

        public bool IsAnimation => Anim != null;
        public bool IsTexture   => Tex  != null;
        public bool IsVideo     => Vid  != null;

        // Actual size of the Sprite resource
        public readonly Vector2 Size;

        public DrawableSprite(GameContentManager content, string animation, bool looping)
        {
            Anim = new SpriteAnimation(content, "Textures/" + animation)
            {
                Looping = looping
            };
            Size = Anim.Size;
        }

        public DrawableSprite(GameContentManager content, string texture)
        {
            var tex = content.Load<Texture2D>("Textures/" + texture);
            Tex = new SubTexture(texture, tex);
            Size = Tex.SizeF;
        }

        public DrawableSprite(SubTexture texture)
        {
            Tex = texture;
            Size = Tex.SizeF;
        }

        public DrawableSprite(SpriteAnimation spriteAnim)
        {
            Anim = spriteAnim;
            Size = Anim.Size;
        }

        public DrawableSprite(ScreenMediaPlayer video)
        {
            Vid = video;
            Vid.EnableInteraction = false;
            Size = Vid.Size;
            if (Size.AlmostZero())
                Log.Error($"You must call PlayVideo before passing video {video.Name} into a DrawableSprite");
        }

        public static DrawableSprite Animation(GameContentManager content, string animation, bool looping)
        {
            return new DrawableSprite(content, animation, looping);
        }

        public static DrawableSprite Texture2D(GameContentManager content, string texture)
        {
            return new DrawableSprite(content, texture);
        }

        public static DrawableSprite SubTex(GameContentManager content, string texture)
        {
            return new DrawableSprite(content.LoadTextureOrDefault("Textures/" + texture));
        }

        public static DrawableSprite Video(GameContentManager content, string video, bool looping)
        {
            var player = new ScreenMediaPlayer(content);
            player.PlayVideo(video, looping);
            return new DrawableSprite(player);
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
            else if (Vid != null)
            {
                Vector2 s = Vid.Size * scale;
                var rect = new Rectangle((int)(screenPos.X-s.X*0.5f),
                                         (int)(screenPos.Y-s.Y*0.5f), (int)s.X, (int)s.Y);
                Vid.Draw(batch, rect, color, Rotation, Effects);
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
            else if (Vid != null)
            {
                Vid.Draw(batch, rect, color, Rotation, Effects);
            }
            else
            {
                batch.Draw(Tex, rect, color, Rotation, Vector2.Zero, Effects, 0.9f);
            }
        }
    }
}
