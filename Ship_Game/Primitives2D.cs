using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    using static Math;

    public static class Primitives2D
    {
        public static void BracketRectangle(this SpriteBatch batch, Rectangle rect, Color color, int bracketSize)
        {
            float x = rect.X;
            float y = rect.Y;
            float r = x + rect.Width;
            float b = y + rect.Height;
            float o = bracketSize;
            DrawLine(batch, new Vector2(x-1, y),   new Vector2(x+o-1, y), color);
            DrawLine(batch, new Vector2(x,   y+1), new Vector2(x, y+o),   color);
            DrawLine(batch, new Vector2(r-o, y),   new Vector2(r, y),     color);
            DrawLine(batch, new Vector2(r, y+1),   new Vector2(r, y+o),   color);
            DrawLine(batch, new Vector2(r-o+1, b), new Vector2(r+1, b),   color);
            DrawLine(batch, new Vector2(r, b),     new Vector2(r, b-o+1), color);
            DrawLine(batch, new Vector2(x, b),     new Vector2(x+o, b),   color);
            DrawLine(batch, new Vector2(x, b),     new Vector2(x, b-o+1), color);
        }

        // This is the [ ] selection rectangle you see when selecting planets and ships
        public static void BracketRectangle(this SpriteBatch batch, Vector2d pos, double radius, Color color)
        {
            Vector2d tl = pos + new Vector2d(-(radius + 3f), -(radius + 3f));
            Vector2d bl = pos + new Vector2d(-(radius + 3f), radius);
            Vector2d tr = pos + new Vector2d(radius, -(radius + 3f));
            Vector2d br = pos + new Vector2d(radius, radius);
            batch.Draw(ResourceManager.Texture("UI/bracket_TR"), tr, color);
            batch.Draw(ResourceManager.Texture("UI/bracket_TL"), tl, color);
            batch.Draw(ResourceManager.Texture("UI/bracket_BR"), br, color);
            batch.Draw(ResourceManager.Texture("UI/bracket_BL"), bl, color);
        }

        public static bool IsIntersectingScreen(in Vector2 a, in Vector2 b)
        {
            return Min(a.X, b.X) < GlobalStats.XRES && 0 < Max(a.X, b.X)
                && Min(a.Y, b.Y) < GlobalStats.YRES && 0 < Max(a.Y, b.Y);
        }

        public static bool IsIntersectingScreen(in Vector2d a, in Vector2d b)
        {
            return Min(a.X, b.X) < GlobalStats.XRES && 0 < Max(a.X, b.X)
                && Min(a.Y, b.Y) < GlobalStats.YRES && 0 < Max(a.Y, b.Y);
        }

        public static bool IsIntersectingScreen(in Vector2 pos, float radius)
        {
            return (pos.X-radius) < GlobalStats.XRES && 0 < (pos.X+radius)
                && (pos.Y-radius) < GlobalStats.YRES && 0 < (pos.Y+radius);
        }

        public static bool IsIntersectingScreen(in Vector2d pos, double radius)
        {
            return (pos.X-radius) < GlobalStats.XRES && 0 < (pos.X+radius)
                && (pos.Y-radius) < GlobalStats.YRES && 0 < (pos.Y+radius);
        }

        public static bool IsIntersectingScreenPosSize(in Vector2 pos, in Vector2 size)
        {
            return (pos.X) < GlobalStats.XRES && 0 < (pos.X+size.X)
                && (pos.Y) < GlobalStats.YRES && 0 < (pos.Y+size.Y);
        }

        public static void DrawCircle(this SpriteBatch batch, Vector2 posOnScreen, float radius, Color color, float thickness = 1f)
        {
            double logarithmicReduction = Max(1.0, Log10(radius));
            int sides = (int)(radius / logarithmicReduction);
            batch.DrawCircle(posOnScreen, radius, sides, color, thickness);
        }

        public static void DrawCircle(this SpriteBatch batch, Vector2d posOnScreen, double radius, Color color, float thickness = 1f)
        {
            double logarithmicReduction = Max(1.0, Log10(radius));
            int sides = (int)(radius / logarithmicReduction);
            batch.DrawCircle(posOnScreen, radius, sides, color, thickness);
        }

        /**
         * @param sides This will always be clamped within [3, 256]
         */
        public static void DrawCircle(this SpriteBatch batch, Vector2 posOnScreen, float radius, int sides, Color color, float thickness = 1f)
        {
            // intersection tests will eliminate up to 95% of all lines, leading to much faster performance
            if (!IsIntersectingScreen(posOnScreen, radius))
                return; // nothing to do here!

            sides = sides.Clamped(3, 256);
            float step = 6.28318530717959f / sides;

            var start = new Vector2(posOnScreen.X + radius, posOnScreen.Y); // 0 angle is horizontal right
            Vector2 previous = start;

            for (float theta = step; theta < 6.28318530717959f; theta += step)
            {
                var current = new Vector2(posOnScreen.X + radius * RadMath.Cos(theta), 
                                          posOnScreen.Y + radius * RadMath.Sin(theta));
                DrawLine(batch, previous, current, color, thickness);
                previous = current;
            }
            DrawLine(batch, previous, start, color, thickness); // connect back to start
        }

        public static void DrawCircle(this SpriteBatch batch, Vector2d posOnScreen, double radius, int sides, Color color, float thickness = 1f)
        {
            // intersection tests will eliminate up to 95% of all lines, leading to much faster performance
            if (!IsIntersectingScreen(posOnScreen, radius))
                return; // nothing to do here!

            sides = sides.Clamped(3, 256);
            double step = 6.28318530717959 / sides;

            var start = new Vector2d(posOnScreen.X + radius, posOnScreen.Y); // 0 angle is horizontal right
            Vector2d previous = start;

            for (double theta = step; theta < 6.28318530717959; theta += step)
            {
                var current = new Vector2d(posOnScreen.X + radius * RadMath.Cos(theta), 
                                           posOnScreen.Y + radius * RadMath.Sin(theta));
                DrawLine(batch, previous, current, color, thickness);
                previous = current;
            }
            DrawLine(batch, previous, start, color, thickness); // connect back to start
        }

        public static void DrawCapsule(this SpriteBatch batch, in Capsule capsuleOnScreen,
                                       Color color, float thickness = 1f)
        {
            Vector2 start = capsuleOnScreen.Start;
            Vector2 end   = capsuleOnScreen.End;
            float radius  = capsuleOnScreen.Radius;
            Vector2 leftDir = (end - start).LeftVector().Normalized();
            Vector2 left = leftDir * (radius);
            Vector2 right = leftDir * (thickness - radius);

            DrawLine(batch, start + left, end + left, color, thickness);
            DrawLine(batch, start + right, end + right, color, thickness);
            DrawCircle(batch, start, radius, color, thickness);
            DrawCircle(batch, end, radius, color, thickness);
        }

        public static void DrawLine(this SpriteBatch batch, in Vector2 point1, in Vector2 point2, Color color, float thickness = 1f)
        {
            // intersection tests will eliminate up to 95% of all lines, leading to much faster rendering performance
            if (!IsIntersectingScreen(point1, point2))
                return;
            if (ResourceManager.WhitePixel == null)
                return; // most likely it will reload next frame, ignore it for now

            float distance = point1.Distance(point2);
            float angle = (float)Atan2(point2.Y - point1.Y, point2.X - point1.X);

            // some hack here - the 1px texture is rotated and scaled to proper width/height
            var scale = new Vector2(distance, thickness);
            batch.Draw(ResourceManager.WhitePixel, point1, null, color, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawLine(this SpriteBatch batch, in Vector2d point1, in Vector2d point2, Color color, float thickness = 1f)
        {
            // intersection tests will eliminate up to 95% of all lines, leading to much faster rendering performance
            if (!IsIntersectingScreen(point1, point2))
                return;
            if (ResourceManager.WhitePixel == null)
                return; // most likely it will reload next frame, ignore it for now

            float distance = (float)point1.Distance(point2);
            float angle = (float)Atan2(point2.Y - point1.Y, point2.X - point1.X);

            // some hack here - the 1px texture is rotated and scaled to proper width/height
            var scale = new Vector2(distance, thickness);
            batch.Draw(ResourceManager.WhitePixel, new Vector2((float)point1.X, (float)point1.Y), null, 
                      color, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawCrossHair(this SpriteBatch batch, in Vector2d center, double size, Color color, float thickness = 1f)
        {
            double radius = size*0.5;
            var v = new Vector2d(0.0, radius);
            var h = new Vector2d(radius, 0.0);
            DrawLine(batch, center - v, center + v, color, thickness);
            DrawLine(batch, center - h, center + h, color, thickness);
        }

        public static void DrawRectangle(this SpriteBatch batch, in Rectangle rect, Color color, float thickness = 1f)
        {
            var tl = new Vector2(rect.X, rect.Y);
            var bl = new Vector2(rect.X, rect.Bottom);
            var tr = new Vector2(rect.Right, rect.Y);
            var br = new Vector2(rect.Right, rect.Bottom);
            DrawLine(batch, tl, tr, color, thickness); // ---- top
            DrawLine(batch, tr, br, color, thickness); //    | right
            DrawLine(batch, br, bl, color, thickness); // ____ bottom
            DrawLine(batch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangle(this SpriteBatch batch, in RectF rect, Color color, float thickness = 1f)
        {
            var tl = new Vector2(rect.X, rect.Y);
            var bl = new Vector2(rect.X, rect.Bottom);
            var tr = new Vector2(rect.Right, rect.Y);
            var br = new Vector2(rect.Right, rect.Bottom);
            DrawLine(batch, tl, tr, color, thickness); // ---- top
            DrawLine(batch, tr, br, color, thickness); //    | right
            DrawLine(batch, br, bl, color, thickness); // ____ bottom
            DrawLine(batch, bl, tl, color, thickness); // |    left
        }

        // rotates around TopLeft
        public static void DrawRectangle(this SpriteBatch batch, in RectF rect, float rotation, Color color, float thickness = 1f)
        {
            var tl = new Vector2(rect.X, rect.Y);
            var bl = new Vector2(rect.X, rect.Bottom);
            var tr = new Vector2(rect.Right, rect.Y);
            var br = new Vector2(rect.Right, rect.Bottom);

            if (rotation != 0f)
            {
                var center = tl;
                tl = tl.RotateAroundPoint(center, rotation);
                tr = tr.RotateAroundPoint(center, rotation);
                br = br.RotateAroundPoint(center, rotation);
                bl = bl.RotateAroundPoint(center, rotation);
            }

            DrawLine(batch, tl, tr, color, thickness); // ---- top
            DrawLine(batch, tr, br, color, thickness); //    | right
            DrawLine(batch, br, bl, color, thickness); // ____ bottom
            DrawLine(batch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangle(this SpriteBatch batch, Vector2 center, Vector2 size, float rotation, Color color, float thickness = 1f)
        {
            Vector2 halfSize = size * 0.5f;
            var tl = new Vector2(center.X - halfSize.X, center.Y - halfSize.Y);
            var tr = new Vector2(center.X + halfSize.X, center.Y - halfSize.Y);
            var br = new Vector2(center.X + halfSize.X, center.Y + halfSize.Y);
            var bl = new Vector2(center.X - halfSize.X, center.Y + halfSize.Y);

            if (rotation != 0f)
            {
                tl = tl.RotateAroundPoint(center, rotation);
                tr = tr.RotateAroundPoint(center, rotation);
                br = br.RotateAroundPoint(center, rotation);
                bl = bl.RotateAroundPoint(center, rotation);
            }

            DrawLine(batch, tl, tr, color, thickness); // ---- top
            DrawLine(batch, tr, br, color, thickness); //    | right
            DrawLine(batch, br, bl, color, thickness); // ____ bottom
            DrawLine(batch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangle(this SpriteBatch batch, Vector2d center, Vector2d size, double rotation, Color color, float thickness = 1f)
        {
            Vector2d halfSize = size * 0.5f;
            var tl = new Vector2d(center.X - halfSize.X, center.Y - halfSize.Y);
            var tr = new Vector2d(center.X + halfSize.X, center.Y - halfSize.Y);
            var br = new Vector2d(center.X + halfSize.X, center.Y + halfSize.Y);
            var bl = new Vector2d(center.X - halfSize.X, center.Y + halfSize.Y);

            if (rotation != 0f)
            {
                tl = tl.RotateAroundPoint(center, rotation);
                tr = tr.RotateAroundPoint(center, rotation);
                br = br.RotateAroundPoint(center, rotation);
                bl = bl.RotateAroundPoint(center, rotation);
            }

            DrawLine(batch, tl, tr, color, thickness); // ---- top
            DrawLine(batch, tr, br, color, thickness); //    | right
            DrawLine(batch, br, bl, color, thickness); // ____ bottom
            DrawLine(batch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangle(this SpriteBatch batch, in AABoundingBox2D rect, Color color, float thickness = 1f)
        {
            var tl = new Vector2(rect.X1, rect.Y1);
            var tr = new Vector2(rect.X2, rect.Y1);
            var br = new Vector2(rect.X2, rect.Y2);
            var bl = new Vector2(rect.X1, rect.Y2);
            DrawLine(batch, tl, tr, color, thickness); // ---- top
            DrawLine(batch, tr, br, color, thickness); //    | right
            DrawLine(batch, br, bl, color, thickness); // ____ bottom
            DrawLine(batch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangleGlow(this SpriteBatch batch, Rectangle r)
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
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_TL"), tl, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_TR"), tr, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_BL"), bl, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_BR"), br, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_horiz_T"), ht, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_horiz_B"), hb, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_verti_L"), vl, Color.White);
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_verti_R"), vr, Color.White);
        }

        public static void FillRectangle(this SpriteBatch batch, in Rectangle rect, Color color)
        {
            // TODO: This is the legacy draw with no sub-pixel capabilities
            batch.Draw(ResourceManager.WhitePixel, rect, color);
        }

        public static void FillRectangle(this SpriteBatch batch, in RectF rect, Color color)
        {
            SpriteExtensions.Draw(batch, ResourceManager.WhitePixel, rect, color);
        }

        public static void FillRectangle(this SpriteBatch batch, Vector2 location, Vector2 size, Color color, float angle)
        {
            batch.Draw(ResourceManager.WhitePixel, location, size, color, angle);
        }
    }
}
