using System;
using System.Collections.Generic;
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

        public int Left => X;
        public int Right => X + Width;
        public int Top => Y;
        public int Bottom => Y + Height;

        public Point Location
        {
            get => new Point(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Point Center => new Point(X + Width / 2, Y + Height / 2);

        public static readonly Rectangle Empty = new Rectangle();

        public bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
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

        public bool Contains(int x, int y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        public bool Contains(Point value) => X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

        public void Contains(ref Point value, out bool result) => result = X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;

        public bool Contains(Rectangle value) => X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y && value.Y + value.Height <= Y + Height;

        public void Contains(ref Rectangle value, out bool result) => result = X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y && value.Y + value.Height <= Y + Height;

        public bool Intersects(Rectangle value) => value.X < X + Width && X < value.X + value.Width && value.Y < Y + Height && Y < value.Y + value.Height;

        public void Intersects(ref Rectangle value, out bool result) => result = value.X < X + Width && X < value.X + value.Width && value.Y < Y + Height && Y < value.Y + value.Height;

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
    }
}
