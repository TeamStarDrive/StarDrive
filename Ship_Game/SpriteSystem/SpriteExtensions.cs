using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using Ship_Game.Graphics;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaRect = Microsoft.Xna.Framework.Rectangle;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
#pragma warning disable CA1065

namespace Ship_Game
{
    // MonoGame removed XNA 3.1's SpriteBlendMode enum (replaced by BlendState).
    // Kept here so existing call sites (batch.SafeBegin(SpriteBlendMode.Additive)) compile;
    // SafeBegin maps each value to a MonoGame BlendState internally.
    public enum SpriteBlendMode
    {
        None,
        AlphaBlend,
        Additive,
    }

    public static class SpriteExtensions
    {
        static readonly XnaRect? NullRectangle = new();

        // Phase 2: XNA 3.1's internal SpriteBatch.InternalDraw took a Vector4 destination
        // (X, Y, W, H) for sub-pixel-precise quad drawing. MonoGame doesn't expose that
        // method, but its public Draw(Texture2D, Vector2 position, ..., Vector2 scale, ...)
        // overload gives the same sub-pixel precision: position carries the float top-left
        // and scale converts source dimensions into the desired destination size.
        // The legacy `scaleDst` flag is unused by every call site in this codebase
        // (always false); the parameter is preserved only to minimize source churn.
        static void InternalDraw(SpriteBatch batch, Texture2D tex, in RectF dstRect, bool scaleDst, XnaRect? srcRect,
                                 Color color, float rotation, XnaVector2 origin, SpriteEffects effects, float depth)
        {
            XnaVector2 position = new(dstRect.X, dstRect.Y);
            int srcW = srcRect?.Width  ?? tex.Width;
            int srcH = srcRect?.Height ?? tex.Height;
            XnaVector2 scale = new(dstRect.W / srcW, dstRect.H / srcH);
            batch.Draw(tex, position, srcRect, color, rotation, origin, scale, effects, depth);
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
            InternalDraw(batch, texture.Texture, new RectF(destRect), false, adjustedSrcRect, color, 0f,
                         Vector2.Zero, SpriteEffects.None, 1f);
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

        public static bool SafeBegin(this SpriteBatch batch)
        {
            try
            {
                batch.Begin();
                return true;
            }
            catch
            {
                if (batch.SafeEnd())
                {
                    batch.Begin();
                    return true;
                }
                return false;
            }
        }

        // MonoGame removed SpriteBlendMode and SaveStateMode; mapped to BlendState below.
        // The saveState parameter is preserved for source-compat but ignored — MonoGame's
        // SpriteBatch implicitly saves/restores GraphicsDevice render state per Begin/End.
        static BlendState ToBlendState(SpriteBlendMode mode) => mode switch
        {
            SpriteBlendMode.Additive   => BlendState.Additive,
            SpriteBlendMode.AlphaBlend => BlendState.AlphaBlend,
            SpriteBlendMode.None       => BlendState.Opaque,
            _                          => BlendState.AlphaBlend,
        };

        public static bool SafeBegin(this SpriteBatch batch, SpriteBlendMode blendMode)
        {
            BlendState bs = ToBlendState(blendMode);
            try
            {
                batch.Begin(blendState: bs);
                return true;
            }
            catch
            {
                if (batch.SafeEnd())
                {
                    batch.Begin(blendState: bs);
                    return true;
                }
                return false;
            }
        }

        /// <param name="batch"></param>
        /// <param name="blendMode">Sprite blending mode</param>
        /// <param name="sortImmediate">Sorts the sprites immediately. The default is false ("Deferred")</param>
        /// <param name="saveState">Ignored under MonoGame; SpriteBatch handles state save/restore implicitly.</param>
        public static bool SafeBegin(this SpriteBatch batch, SpriteBlendMode blendMode, bool sortImmediate, bool saveState = false)
        {
            SpriteSortMode sortMode = sortImmediate ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;
            BlendState bs = ToBlendState(blendMode);
            try
            {
                batch.Begin(sortMode, bs);
                return true;
            }
            catch
            {
                if (batch.SafeEnd())
                {
                    batch.Begin(sortMode, bs);
                    return true;
                }
                return false;
            }
        }

        // Overload that lets the caller pin a custom RasterizerState (e.g. one with
        // ScissorTestEnable=true). Needed for scroll-list scissor clipping under
        // MonoGame: device.RasterizerState set externally before Begin is fine,
        // but the safest path is to bind it to the SpriteBatch directly so End
        // doesn't lose it after subsequent Begin calls.
        public static bool SafeBegin(this SpriteBatch batch, SpriteBlendMode blendMode, RasterizerState rasterizer)
        {
            BlendState bs = ToBlendState(blendMode);
            try
            {
                batch.Begin(SpriteSortMode.Deferred, bs,
                            samplerState: null, depthStencilState: null,
                            rasterizerState: rasterizer);
                return true;
            }
            catch
            {
                if (batch.SafeEnd())
                {
                    batch.Begin(SpriteSortMode.Deferred, bs,
                                samplerState: null, depthStencilState: null,
                                rasterizerState: rasterizer);
                    return true;
                }
                return false;
            }
        }

        public static bool SafeBegin(this SpriteBatch batch, SpriteBlendMode blendMode, bool sortImmediate, bool saveState, in XnaMatrix transform)
        {
            SpriteSortMode sortMode = sortImmediate ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;
            BlendState bs = ToBlendState(blendMode);
            XnaMatrix t = transform;
            try
            {
                batch.Begin(sortMode, bs, transformMatrix: t);
                return true;
            }
            catch
            {
                if (batch.SafeEnd())
                {
                    batch.Begin(sortMode, bs, transformMatrix: t);
                    return true;
                }
                return false;
            }
        }

        public static bool SafeEnd(this SpriteBatch batch)
        {
            try
            {
                batch.End();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
