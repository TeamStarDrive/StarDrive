using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game
{
    public static class SpriteExtensions
    {
        [Conditional("DEBUG")] static void CheckTextureDisposed(Texture2D texture)
        {
            if (texture.IsDisposed)
                throw new ObjectDisposedException($"Texture2D '{texture.Name}'");
        }
        [Conditional("DEBUG")] static void CheckSubTextureDisposed(SubTexture texture)
        {
            if (texture.Texture.IsDisposed)
                throw new ObjectDisposedException($"SubTexture '{texture.Name}' in Texture2D '{texture.Texture.Name}'");
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, float x, float y)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, new Vector2(x, y), texture.Rect, Color.White);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, in Rectangle destRect)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, destRect, texture.Rect, Color.White);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, 
                                Vector2 position, Color color)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, position, texture.Rect, color);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, 
                                in Rectangle destRect, Color color)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, destRect, texture.Rect, color);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, in Rectangle destRect,
            Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, destRect, texture.Rect,
                       color, rotation, origin, effects, layerDepth);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, position, texture.Rect, color, 
                       rotation, origin, scale, effects, layerDepth);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, Vector2 position, Vector2 size)
        {
            CheckSubTextureDisposed(texture);
            var r = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            batch.Draw(texture.Texture, r, texture.Rect, Color.White);
        }

        public static void Draw(this SpriteBatch batch, Texture2D texture, Vector2 position, Vector2 size)
        {
            CheckTextureDisposed(texture);
            var r = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            batch.Draw(texture, r, Color.White);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, Vector2 position, Vector2 size, Color color)
        {
            CheckSubTextureDisposed(texture);
            var r = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            batch.Draw(texture.Texture, r, texture.Rect, color);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, in Rectangle rect, 
                                float rotation)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, rect, texture.Rect, Color.White, 
                       rotation, texture.CenterF, SpriteEffects.None, 1f);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, in Rectangle rect, 
                                float rotation, float scale, float z)
        {
            CheckSubTextureDisposed(texture);
            Rectangle r = rect.ScaledBy(scale);
            batch.Draw(texture.Texture, r, texture.Rect, Color.White, 
                       rotation, texture.CenterF, SpriteEffects.None, z);
        }

        static Rectangle AdjustedToSubTexture(SubTexture texture, Rectangle srcRect)
        {
            Rectangle subRect = texture.Rect;
            return new Rectangle(
                subRect.X + srcRect.X,
                subRect.Y + srcRect.Y,
                srcRect.Width,
                srcRect.Height
            );
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, Rectangle destRect,
                                Rectangle srcRect, Color color)
        {
            CheckSubTextureDisposed(texture);
            Rectangle adjustedSrcRect = AdjustedToSubTexture(texture, srcRect);
            batch.Draw(texture.Texture, destRect, adjustedSrcRect, color);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, Rectangle destRect, Rectangle srcRect,
            Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            CheckSubTextureDisposed(texture);
            Rectangle adjustedSrcRect = AdjustedToSubTexture(texture, srcRect);
            batch.Draw(texture.Texture, destRect, adjustedSrcRect,
                       color, rotation, origin, effects, layerDepth);
        }

        public static void DrawString(this SpriteBatch batch, Font font,
                                      in LocalizedText text, Vector2 position, Color color)
        {
            batch.DrawString(font.XnaFont, text.Text, position, color);
        }

        public static void DrawString(this SpriteBatch batch, Font font,
                                      string text, float x, float y)
        {
            batch.DrawString(font.XnaFont, text, new Vector2(x, y), Color.White);
        }

        public static void DrawString(this SpriteBatch batch, Font font,
                                      string text, float x, float y, Color color)
        {
            batch.DrawString(font.XnaFont, text, new Vector2(x, y), color);
        }

        public static void DrawString(this SpriteBatch batch, Font font,
                                      string text, Vector2 pos, Color color, 
                                      float rotation, Vector2 origin, float scale = 1f)
        {
            batch.DrawString(font.XnaFont, text, pos, color, 
                             rotation, origin, scale, SpriteEffects.None, 1f);
        }

        // Special Multi-Colored line draw
        // batch.DrawLine(Fonts.Arial12, X, Y, ("A: ", Color.White), ("100", Color.Red));
        public static void DrawLine(this SpriteBatch batch, Font font, float x, float y,
                                    params (string Text, Color Color)[] textSequence)
        {
            for (int i = 0; i < textSequence.Length; ++i)
            {
                batch.DrawString(font.XnaFont, textSequence[i].Text, new Vector2(x, y), textSequence[i].Color);
                x += font.TextWidth(textSequence[i].Text);
            }
        }
    }
}
