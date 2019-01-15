using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public static class SpriteExtensions
    {
        public static void Draw(this SpriteBatch batch, SubTexture texture, 
                                Vector2 position, Color color)
        {
            batch.Draw(texture.Texture, position, texture.Rect, color);
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, 
                                in Rectangle destinationRect, Color color)
        {
            batch.Draw(texture.Texture, destinationRect, texture.Rect, color);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, Rectangle destinationRectangle,
            Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            batch.Draw(texture.Texture, destinationRectangle, texture.Rect,
                       color, rotation, origin, effects, layerDepth);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, Vector2 position, Color color,
            float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            batch.Draw(texture.Texture, position, texture.Rect, color, 
                       rotation, origin, scale, effects, layerDepth);
        }

        static Rectangle AdjustedToSubTexture(SubTexture texture, Rectangle sourceRectangle)
        {
            Rectangle subRect = texture.Rect;
            return new Rectangle(
                subRect.X + sourceRectangle.X,
                subRect.Y + sourceRectangle.Y,
                sourceRectangle.Width,
                sourceRectangle.Height
            );
        }

        public static void Draw(this SpriteBatch batch, SubTexture texture, Rectangle destinationRectangle,
                                Rectangle sourceRectangle, Color color)
        {
            Rectangle adjustedSrcRect = AdjustedToSubTexture(texture, sourceRectangle);
            batch.Draw(texture.Texture, destinationRectangle, adjustedSrcRect, color);
        }

        public static void Draw(
            this SpriteBatch batch, SubTexture texture, Rectangle destinationRectangle, Rectangle sourceRectangle,
            Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            Rectangle adjustedSrcRect = AdjustedToSubTexture(texture, sourceRectangle);
            batch.Draw(texture.Texture, destinationRectangle, adjustedSrcRect,
                       color, rotation, origin, effects, layerDepth);
        }


        public static void DrawString(
            this SpriteBatch batch, SpriteFont font, string text, float x, float y)
        {
            batch.DrawString(font, text, new Vector2(x, y), Color.White);
        }
    }
}
