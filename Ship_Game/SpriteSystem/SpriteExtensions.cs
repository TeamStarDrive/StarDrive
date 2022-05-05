using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Graphics;
using Vector2 = SDGraphics.Vector2;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    public static class SpriteExtensions
    {
        delegate void InternalDrawD(SpriteBatch batch, Texture2D tex, ref XnaVector4 dst, bool scaleDst, ref Rectangle? srcRect,
                                    Color color, float rotation, ref XnaVector2 origin, SpriteEffects effects, float depth);

        static readonly InternalDrawD DrawInternal;
        static readonly Rectangle? NullRectangle = new Rectangle?();

        static SpriteExtensions()
        {
            const BindingFlags anyMethod = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            MethodInfo method = typeof(SpriteBatch).GetMethod("InternalDraw", anyMethod);
            if (method == null)
                throw new InvalidOperationException("Missing InternalDraw from XNA.SpriteBatch");
            DrawInternal = (InternalDrawD)Delegate.CreateDelegate(typeof(InternalDrawD), null, method);
        }

        static void InternalDraw(SpriteBatch batch, Texture2D tex, in RectF dstRect, bool scaleDst, Rectangle? srcRect, 
                                 Color color, float rotation, XnaVector2 origin,  SpriteEffects effects, float depth)
        {
            var dst = new XnaVector4(dstRect.X, dstRect.Y, dstRect.W, dstRect.H);
            DrawInternal.Invoke(batch, tex, ref dst, scaleDst, ref srcRect, color, 
                                rotation, ref origin, effects, depth);
        }

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
                                Vector2d position, Color color)
        {
            CheckSubTextureDisposed(texture);
            Vector2 pos = position.ToVec2f();
            batch.Draw(texture.Texture, pos, texture.Rect, color);
        }
        
        ////// RectF overloads - precise sub-pixel drawing which gives less flickering //////

        public static void Draw(this SpriteBatch batch, SubTexture tex, in RectF destRect, Color color)
        {
            CheckSubTextureDisposed(tex);
            InternalDraw(batch, tex.Texture, destRect, false, tex.Rect, color, 0f, 
                         Vector2.Zero, SpriteEffects.None, 1f);
        }

        public static void Draw(this SpriteBatch batch, SubTexture tex, in RectF destRect, Color color,
                                float rotation, Vector2 origin)
        {
            CheckSubTextureDisposed(tex);
            InternalDraw(batch, tex.Texture, destRect, false, tex.Rect, color,
                         rotation, origin, SpriteEffects.None, 1f);
        }

        public static void Draw(this SpriteBatch batch, SubTexture tex, in RectF destRect, Color color,
                                float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            CheckSubTextureDisposed(tex);
            InternalDraw(batch, tex.Texture, destRect, false, tex.Rect, color,
                         rotation, origin, effects, layerDepth);
        }

        public static void Draw(this SpriteBatch batch, SubTexture tex, in RectF rect,
                                float rotation, float scale, float z)
        {
            CheckSubTextureDisposed(tex);
            RectF r = rect.ScaledBy(scale);
            InternalDraw(batch, tex.Texture, r, false, tex.Rect, Color.White,
                         rotation, tex.CenterF, SpriteEffects.None, z);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture,
                                Vector2 position, Vector2 size)
        {
            var r = new RectF(position, size);
            Draw(batch, texture, r, Color.White);
        }

        public static void Draw(this SpriteBatch batch, Texture2D texture,
                                in RectF r)
        {
            CheckTextureDisposed(texture);
            InternalDraw(batch, texture, r, false, NullRectangle, Color.White,
                         0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        public static void Draw(this SpriteBatch batch, Texture2D texture,
                                in RectF r, Color color)
        {
            CheckTextureDisposed(texture);
            InternalDraw(batch, texture, r, false, NullRectangle, color,
                         0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        public static void Draw(this SpriteBatch batch, Texture2D texture,
                                in RectF r, Color color, float angle)
        {
            CheckTextureDisposed(texture);
            InternalDraw(batch, texture, r, false, NullRectangle, color,
                         angle, Vector2.Zero, SpriteEffects.None, 0f);
        }

        public static void Draw(this SpriteBatch batch, Texture2D texture,
                                Vector2 position, Vector2 size)
        {
            var r = new RectF(position, size);
            Draw(batch, texture, r);
        }

        public static void Draw(this SpriteBatch batch, Texture2D texture,
                                Vector2 position, Vector2 size, Color color, float angle)
        {
            var r = new RectF(position, size);
            Draw(batch, texture, r, color, angle);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture,
                                Vector2 position, Vector2 size, Color color)
        {
            CheckSubTextureDisposed(texture);
            var r = new RectF(position, size);
            InternalDraw(batch, texture.Texture, r, false, texture.Rect, color,
                         0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        ////// Integer Rectangle overloads - only useful for static UI pieces //////

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
            batch.Draw(texture.Texture, destRect, texture.Rect, color,
                       rotation, origin, effects, layerDepth);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            CheckSubTextureDisposed(texture);
            batch.Draw(texture.Texture, position, texture.Rect, color, 
                       rotation, origin, scale, effects, layerDepth);
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

        public static float GetHeightFromWidthAspect(this Texture2D tex, float wantedWidth)
            => SubTexture.GetHeightFromWidthAspect(tex.Width, tex.Height, wantedWidth);

        public static float GetWidthFromHeightAspect(this Texture2D tex, float wantedHeight)
            => SubTexture.GetWidthFromHeightAspect(tex.Width, tex.Height, wantedHeight);
    }
}
