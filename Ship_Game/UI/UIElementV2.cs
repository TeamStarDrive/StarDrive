using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class UIElementV2 : IInputHandler
    {
        public Vector2 Pos;
        public Vector2 Size;
        public Rectangle Rect
        {
            get => new Rectangle((int)Pos.X, (int)Pos.Y, (int)Size.X, (int)Size.Y);
            set
            {
                Pos  = new Vector2(value.X, value.Y);
                Size = new Vector2(value.Width, value.Height);
            }
        }

        public float X      { get => Pos.X;  set => Pos.X  = value; }
        public float Y      { get => Pos.Y;  set => Pos.Y  = value; }
        public float Width  { get => Size.X; set => Size.X = value; }
        public float Height { get => Size.Y; set => Size.Y = value; }
        
        public void SetPos(float x, float y)           => Pos  = new Vector2(x, y);
        public void SetSize(float width, float height) => Size = new Vector2(width, height);

        protected UIElementV2(Vector2 pos)
        {
            Pos = pos;
        }

        protected UIElementV2(Vector2 pos, Vector2 size)
        {
            Pos = pos;
            Size = size;
        }

        protected UIElementV2(Rectangle rect)
        {
            Pos  = new Vector2(rect.X, rect.Y);
            Size = new Vector2(rect.Width, rect.Height);
        }

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract bool HandleInput(InputState input);

        public bool HitTest(Vector2 pos)
        {
            return pos.X > Pos.X && pos.Y > Pos.Y && pos.X < Pos.X + Size.X && pos.Y < Pos.Y + Size.Y;
        }

    }
}