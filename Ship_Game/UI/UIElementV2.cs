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
        
        public void SetPos(float x, float y)           => Pos  = new Vector2(x, y);
        public void SetSize(float width, float height) => Size = new Vector2(width, height);

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract bool HandleInput(InputState input);
    }
}