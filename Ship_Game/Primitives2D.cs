using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public static class Primitives2D
    {
        private static Texture2D Pixel;

        public static void BracketRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, int bracketSize)
        {
            float x = rect.X;
            float y = rect.Y;
            float r = x + rect.Width;
            float b = y + rect.Height;
            float o = bracketSize;
            DrawLine(spriteBatch, new Vector2(x-1, y),   new Vector2(x+o-1, y), color);
            DrawLine(spriteBatch, new Vector2(x,   y+1), new Vector2(x, y+o),   color);
            DrawLine(spriteBatch, new Vector2(r-o, y),   new Vector2(r, y),     color);
            DrawLine(spriteBatch, new Vector2(r, y+1),   new Vector2(r, y+o),   color);
            DrawLine(spriteBatch, new Vector2(r-o+1, b), new Vector2(r+1, b),   color);
            DrawLine(spriteBatch, new Vector2(r, b),     new Vector2(r, b-o+1), color);
            DrawLine(spriteBatch, new Vector2(x, b),     new Vector2(x+o, b),   color);
            DrawLine(spriteBatch, new Vector2(x, b),     new Vector2(x, b-o+1), color);
        }

        public static void BracketRectangle(SpriteBatch spriteBatch, Vector2 pos, float radius, Color color)
        {
            Vector2 tl = pos + new Vector2(-(radius + 3f), -(radius + 3f));
            Vector2 bl = pos + new Vector2(-(radius + 3f), radius);
            Vector2 tr = pos + new Vector2(radius, -(radius + 3f));
            Vector2 br = pos + new Vector2(radius, radius);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_TR"), tr, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_TL"), tl, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_BR"), br, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_BL"), bl, color);
        }

        private static void CreateThePixel(SpriteBatch spriteBatch)
        {
            Pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Pixel.SetData(new []{ Color.White });
        }

        public static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, int sides, Color color, float thickness = 1f)
        {
            float step = 6.28318530717959f / sides;

            var start = new Vector2(center.X + radius, center.Y); // 0 angle is horizontal right
            Vector2 previous = start;

            for (float theta = step; theta < 6.28318530717959f; theta += step)
            {
                var current = new Vector2(center.X + radius * (float)Math.Cos(theta), center.Y + radius * (float)Math.Sin(theta));

                DrawLine(spriteBatch, previous, current, color, thickness);
                previous = current;
            }

            // connect back to start
            DrawLine(spriteBatch, previous, start, color, thickness);
        }

        public static void DrawCircle(SpriteBatch spriteBatch, float x, float y, float radius, int sides, Color color, float thickness = 1f)
            => DrawCircle(spriteBatch, new Vector2(x, y), radius, sides, color, thickness);

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color)
            => DrawLine(spriteBatch, point1, point2, color, 1f);

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness)
        {
            float distance = Vector2.Distance(point1, point2);
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            DrawLine(spriteBatch, point1, distance, angle, color, thickness);
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness)
        {
            if (Pixel == null) CreateThePixel(spriteBatch);

            // some hack here - the 1px texture is rotated and scaled to proper width/height
            var scale = new Vector2(length, thickness);
            spriteBatch.Draw(Pixel, point, null, color, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness = 1.0f)
        {
            DrawLine(spriteBatch, new Vector2(rect.X, rect.Y), new Vector2(rect.Right, rect.Y), color, thickness);
            DrawLine(spriteBatch, new Vector2(rect.X, rect.Y), new Vector2(rect.X, rect.Bottom), color, thickness);
            DrawLine(spriteBatch, new Vector2(rect.X - thickness, rect.Bottom), new Vector2(rect.Right, rect.Bottom), color, thickness);
            DrawLine(spriteBatch, new Vector2(rect.Right, rect.Y), new Vector2(rect.Right, rect.Bottom), color, thickness);
        }

        public static void DrawRectangleGlow(SpriteBatch spriteBatch, Rectangle r)
        {
            r = new Rectangle(r.X - 13, r.Y - 12, r.Width + 25, r.Height + 25);
            var tl = new Rectangle(r.X, r.Y, 20, 20);
            var tr = new Rectangle(r.X + r.Width - 20, r.Y, 20, 20);
            var bl = new Rectangle(r.X, r.Y + r.Height - 20, 20, 20);
            var br = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 20, 20, 20);
            var ht = new Rectangle(tl.X + 20, tl.Y, r.Width - 40, 20);
            var hb = new Rectangle(tl.X + 20, bl.Y, r.Width - 40, 20);
            var vl = new Rectangle(tl.X, tl.Y + 20, 20, r.Height - 40);
            var vr = new Rectangle(tl.X + r.Width - 20, tl.Y + 20, 20, r.Height - 40);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_TL"), tl, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_TR"), tr, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_BL"), bl, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_BR"), br, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_horiz_T"), ht, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_horiz_B"), hb, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_verti_L"), vl, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_verti_R"), vr, Color.White);
        }

        public static void DrawResearchLineHorizontal(SpriteBatch spriteBatch, Vector2 leftPoint, Vector2 rightPoint, bool complete)
        {
            var r = new Rectangle((int)leftPoint.X + 5, (int)leftPoint.Y - 2, (int)Vector2.Distance(leftPoint, rightPoint) - 5, 5);
            var small = new Rectangle((int)leftPoint.X, (int)leftPoint.Y, 5, 1);
            FillRectangle(spriteBatch, small, (complete ? new Color(110, 171, 227) : new Color(194, 194, 194)));
            if (complete)
            {
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/grid_horiz_complete"), r, Color.White);
                return;
            }
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/grid_horiz"), r, Color.White);
        }

        public static void DrawResearchLineHorizontalGradient(SpriteBatch spriteBatch, Vector2 left, Vector2 right, bool complete)
        {
            var r = new Rectangle((int)left.X + 5, (int)left.Y - 2, (int)Vector2.Distance(left, right) - 5, 5);
            var small = new Rectangle((int)left.X, (int)left.Y, 5, 1);
            FillRectangle(spriteBatch, small, (complete ? new Color(110, 171, 227) : new Color(194, 194, 194)));
            if (complete)
            {
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/grid_horiz_gradient_complete"), r, Color.White);
                return;
            }
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/grid_horiz_gradient"), r, Color.White);
        }

        public static void DrawResearchLineVertical(SpriteBatch spriteBatch, Vector2 top, Vector2 bottom, bool complete)
        {
            var r = new Rectangle((int)top.X - 2, (int)top.Y, 5, (int)Vector2.Distance(top, bottom));
            if (complete)
            {
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/grid_vert_complete"), r, Color.White);
                return;
            }
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/grid_vert"), r, Color.White);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            if (Pixel == null)
                CreateThePixel(spriteBatch);

            spriteBatch.Draw(Pixel, rect, color);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color, float angle)
        {
            if (Pixel == null)
                CreateThePixel(spriteBatch);

            spriteBatch.Draw(Pixel, location, null, color, angle, Vector2.Zero, size, SpriteEffects.None, 0f);
        }
    }
}