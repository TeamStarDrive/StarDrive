using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public struct FRect
    {
        public Vector2 Pos;
        public Vector2 Size;

        public float X      { get => Pos.X;  set => Pos.X  = value; }
        public float Y      { get => Pos.Y;  set => Pos.Y  = value; }
        public float Width  { get => Size.X; set => Size.X = value; }
        public float Height { get => Size.Y; set => Size.Y = value; }

        public FRect(float x, float y, float width, float height)
        {
            Pos.X = x;
            Pos.Y = y;
            Size.X = width;
            Size.Y = height;
        }

        public bool HitTest(Vector2 pos)
        {
            return pos.X > Pos.X && pos.Y > Pos.Y && pos.X < Pos.X + Size.X && pos.Y < Pos.Y + Size.Y;
        }

        public Rectangle XnaRect => new Rectangle((int)Pos.X, (int)Pos.Y, (int)Size.X, (int)Size.Y);
    }
}
