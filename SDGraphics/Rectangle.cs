using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using XnaRect = Microsoft.Xna.Framework.Rectangle;

namespace SDGraphics
{
    public struct Rectangle : IEquatable<Rectangle>
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public readonly int Left => X;
        public readonly int Right => X + Width;
        public readonly int Top => Y;
        public readonly int Bottom => Y + Height;

        public Point Location
        {
            get => new Point(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public readonly Point Center => new Point(X + Width / 2, Y + Height / 2);
        public readonly Vector2 CenterF => new Vector2(X + Width / 2, Y + Height / 2);

        public static readonly Rectangle Empty = new Rectangle();

        public readonly bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rectangle(Vector2 pos, Vector2 size)
        {
            X = (int)pos.X;
            Y = (int)pos.Y;
            Width = (int)size.X;
            Height = (int)size.Y;
        }

        public static implicit operator XnaRect(in Rectangle r)
        {
            return new XnaRect(r.X, r.Y, r.Width, r.Height);
        }

        public void Offset(Point amount)
        {
            X += amount.X;
            Y += amount.Y;
        }

        public void Offset(int offsetX, int offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        public void Inflate(int horizontalAmount, int verticalAmount)
        {
            X -= horizontalAmount;
            Y -= verticalAmount;
            Width += horizontalAmount * 2;
            Height += verticalAmount * 2;
        }

        [Pure]
        public readonly bool Contains(int x, int y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        [Pure]
        public readonly bool Contains(Point value) => X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

        [Pure]
        public readonly void Contains(ref Point value, out bool result) => result = X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

        [Pure]
        public readonly bool Contains(Rectangle value) => X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y && value.Y + value.Height <= Y + Height;

        [Pure]
        public readonly bool Intersects(Rectangle value) => value.X < X + Width && X < value.X + value.Width && value.Y < Y + Height && Y < value.Y + value.Height;

        public static Rectangle Intersect(Rectangle value1, Rectangle value2)
        {
            int num1 = value1.X + value1.Width;
            int num2 = value2.X + value2.Width;
            int num3 = value1.Y + value1.Height;
            int num4 = value2.Y + value2.Height;
            int num5 = value1.X > value2.X ? value1.X : value2.X;
            int num6 = value1.Y > value2.Y ? value1.Y : value2.Y;
            int num7 = num1 < num2 ? num1 : num2;
            int num8 = num3 < num4 ? num3 : num4;
            Rectangle rectangle;
            if (num7 > num5 && num8 > num6)
            {
                rectangle.X = num5;
                rectangle.Y = num6;
                rectangle.Width = num7 - num5;
                rectangle.Height = num8 - num6;
            }
            else
            {
                rectangle.X = 0;
                rectangle.Y = 0;
                rectangle.Width = 0;
                rectangle.Height = 0;
            }
            return rectangle;
        }

        public static void Intersect(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
        {
            int num1 = value1.X + value1.Width;
            int num2 = value2.X + value2.Width;
            int num3 = value1.Y + value1.Height;
            int num4 = value2.Y + value2.Height;
            int num5 = value1.X > value2.X ? value1.X : value2.X;
            int num6 = value1.Y > value2.Y ? value1.Y : value2.Y;
            int num7 = num1 < num2 ? num1 : num2;
            int num8 = num3 < num4 ? num3 : num4;
            if (num7 > num5 && num8 > num6)
            {
                result.X = num5;
                result.Y = num6;
                result.Width = num7 - num5;
                result.Height = num8 - num6;
            }
            else
            {
                result.X = 0;
                result.Y = 0;
                result.Width = 0;
                result.Height = 0;
            }
        }

        public static Rectangle Union(Rectangle value1, Rectangle value2)
        {
            int num1 = value1.X + value1.Width;
            int num2 = value2.X + value2.Width;
            int num3 = value1.Y + value1.Height;
            int num4 = value2.Y + value2.Height;
            int num5 = value1.X < value2.X ? value1.X : value2.X;
            int num6 = value1.Y < value2.Y ? value1.Y : value2.Y;
            int num7 = num1 > num2 ? num1 : num2;
            int num8 = num3 > num4 ? num3 : num4;
            Rectangle rectangle;
            rectangle.X = num5;
            rectangle.Y = num6;
            rectangle.Width = num7 - num5;
            rectangle.Height = num8 - num6;
            return rectangle;
        }

        public static void Union(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
        {
            int num1 = value1.X + value1.Width;
            int num2 = value2.X + value2.Width;
            int num3 = value1.Y + value1.Height;
            int num4 = value2.Y + value2.Height;
            int num5 = value1.X < value2.X ? value1.X : value2.X;
            int num6 = value1.Y < value2.Y ? value1.Y : value2.Y;
            int num7 = num1 > num2 ? num1 : num2;
            int num8 = num3 > num4 ? num3 : num4;
            result.X = num5;
            result.Y = num6;
            result.Width = num7 - num5;
            result.Height = num8 - num6;
        }

        public bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Rectangle other)
                flag = Equals(other);
            return flag;
        }

        public override string ToString()
        {
            CultureInfo c = CultureInfo.CurrentCulture;
            return string.Format(c, "{{X:{0} Y:{1} Width:{2} Height:{3}}}",
                X.ToString(c), Y.ToString(c), Width.ToString(c), Height.ToString(c));
        }

        public override int GetHashCode() => X.GetHashCode() + Y.GetHashCode() + Width.GetHashCode() + Height.GetHashCode();

        public static bool operator ==(Rectangle a, Rectangle b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

        public static bool operator !=(Rectangle a, Rectangle b) => a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;

        [Pure]
        public readonly bool HitTest(Vector2 pos)
        {
            return pos.X > X && pos.Y > Y
                && pos.X < X + Width
                && pos.Y < Y + Height;
        }

        [Pure]
        public readonly bool HitTest(int x, int y)
        {
            return x > X && y > Y
                && x < X + Width
                && y < Y + Height;
        }

        [Pure] public readonly Point Pos() => new Point(X, Y);
        [Pure] public readonly Vector2 PosVec() => new Vector2(X, Y);
        [Pure] public readonly Vector2 Size() => new Vector2(Width, Height);
        [Pure] public readonly int CenterX() => X + Width / 2;
        [Pure] public readonly int CenterY() => Y + Height / 2;
        [Pure] public readonly int Area() => Width * Height;

        // Example: r.RelativeX(0.5) == r.CenterX()
        //          r.RelativeX(1.0) == r.Right
        [Pure] public readonly int RelativeX(float percent) => X + (int)(Width * percent);
        [Pure] public readonly int RelativeY(float percent) => Y + (int)(Height * percent);

        [Pure]
        public readonly Vector2 RelPos(float relX, float relY)
            => new Vector2(RelativeX(relX), RelativeY(relY));

        [Pure]
        public readonly Rectangle Bevel(int bevel)
            => new Rectangle(X - bevel, Y - bevel, Width + bevel * 2, Height + bevel * 2);

        [Pure]
        public readonly Rectangle Bevel(int bevelX, int bevelY)
            => new Rectangle(X - bevelX, Y - bevelY, Width + bevelX * 2, Height + bevelY * 2);

        [Pure]
        public readonly Rectangle Widen(int widen)
            => new Rectangle(X - widen, Y, Width + widen * 2, Height);

        [Pure]
        public readonly Rectangle Move(int dx, int dy)
            => new Rectangle(X + dx, Y + dy, Width, Height);

        // Cut a chunk off the top of the rectangle
        [Pure]
        public readonly Rectangle CutTop(int amount)
            => new Rectangle(X, Y + amount, Width, Height - amount);

        [Pure]
        public readonly Rectangle ScaledBy(float scale)
        {
            if (scale.AlmostEqual(1f))
                return this;
            float extrude = scale - 1f;
            int extrudeX = (int)(Width * extrude);
            int extrudeY = (int)(Height * extrude);
            return new Rectangle(X - extrudeX,
                Y - extrudeY,
                Width + extrudeX * 2,
                Height + extrudeY * 2);
        }

        // Given 2 rectangles, returns out the intersecting rectangle area,
        // or returns false if no intersection
        [Pure]
        public readonly bool GetIntersectingRect(in Rectangle b, out Rectangle intersection)
        {
            int leftX = Math.Max(X, b.X);
            int rightX = Math.Min(X + Width, b.X + b.Width);
            int topY = Math.Max(Y, b.Y);
            int bottomY = Math.Min(Y + Height, b.Y + b.Height);

            if (leftX < rightX && topY < bottomY)
            {
                intersection = new Rectangle(leftX, topY, rightX - leftX, bottomY - topY);
                return true;
            }
            intersection = Rectangle.Empty;
            return false;
        }
    }
}
