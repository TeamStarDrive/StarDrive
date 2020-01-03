using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SubTexture
    {
        // name="sprite1" x="461" y="1317" width="28" height="41"
        public readonly string Name;        // name of the sprite for name-based lookup
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public readonly Texture2D Texture;

        public Rectangle Rect => new Rectangle(X, Y, Width, Height);
        public int Right  => X + Width;
        public int Bottom => Y + Height;

        public SubTexture(string name, int x, int y, int w, int h, Texture2D texture)
        {
            Name = name;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            Texture = texture;
        }

        // special case: SubTexture is a container for a full texture
        public SubTexture(string name, Texture2D fullTexture)
        {
            Name = name;
            Width = fullTexture.Width;
            Height = fullTexture.Height;
            Texture = fullTexture;
        }

        // UV-coordinates
        public float CoordLeft   => X / (float)Texture.Width;
        public float CoordTop    => Y / (float)Texture.Height;
        public float CoordRight  => (X + (Width  - 1)) / (float)Texture.Width;
        public float CoordBottom => (Y + (Height - 1)) / (float)Texture.Height;

        public Vector2 CoordUpperLeft  => new Vector2(CoordLeft,  CoordTop);
        public Vector2 CoordLowerLeft  => new Vector2(CoordLeft,  CoordBottom);
        public Vector2 CoordLowerRight => new Vector2(CoordRight, CoordBottom);
        public Vector2 CoordUpperRight => new Vector2(CoordRight, CoordTop);

        public Vector2 CenterF => new Vector2(Width/2f, Height/2f);
        public Vector2 SizeF => new Vector2(Width, Height);
        public Point Center => new Point(Width/2, Height/2);
        public int CenterX => Width/2;
        public int CenterY => Height/2;

        public float AspectRatio => Width / (float)Height;

        public override string ToString()
            => $"sub-tex  {Name} {X},{Y} {Width}x{Height}  texture:{Texture.Width}x{Texture.Height}";
    }
}
