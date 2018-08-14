using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class PrimitiveQuad
    {
        public static GraphicsDevice Device;
        private static Texture2D White;

        public int X;
        public int Y;
        public int W;
        public int H;
        public bool Filled;

        public Vector2 Position => new Vector2(X, Y);
        public Rectangle Rect => new Rectangle(X, Y, W, H);

        public PrimitiveQuad(float x, float y, float w, float h)
        {
            X = (int)x;
            Y = (int)y;
            W = (int)w;
            H = (int)h;
            LoadContent();
        }

        public PrimitiveQuad(Rectangle rect)
        {
            X = rect.X;
            Y = rect.Y;
            W = rect.Width;
            H = rect.Height;
            LoadContent();
        }

        public bool Contains(Vector2 pos)
        {
            return pos.X > X && pos.X < (X + W)
                && pos.Y > Y && pos.Y < (Y + H);
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Draw(White, new Rectangle(X,   Y,   W, 1), Color.White);
            sb.Draw(White, new Rectangle(X,   Y,   1, H), Color.White);
            sb.Draw(White, new Rectangle(X+W, Y,   1, H), Color.White);
            sb.Draw(White, new Rectangle(X,   Y+H, W, 1), Color.White);
        }

        public void Draw(SpriteBatch sb, Color color)
        {
            sb.Draw(White, new Rectangle(X,   Y,   W, 1), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
            sb.Draw(White, new Rectangle(X,   Y,   1, H), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
            sb.Draw(White, new Rectangle(X+W, Y,   1, H), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
            sb.Draw(White, new Rectangle(X,   Y+H, W, 1), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
        }

        public void Draw(SpriteBatch sb, Color color, int thickness)
        {
            int t  = thickness;
            int t2 = thickness / 2;
            sb.Draw(White, new Rectangle(X - t2, Y - t2, W, t),   null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
            sb.Draw(White, new Rectangle(X - t2, Y - t2, t, H),   null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
            sb.Draw(White, new Rectangle(X+W-t2, Y - t2, t, H+t), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
            sb.Draw(White, new Rectangle(X - t2, Y+H-t2, W, t),   null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
        }

        public void LoadContent()
        {
            if (White != null)
                return;
            White = new Texture2D(Device, 1, 1);
            White.SetData(new[]{ Color.White });
        }
    }
}