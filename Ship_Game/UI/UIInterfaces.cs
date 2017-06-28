using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public interface IInputHandler
    {
        // @return TRUE if input was handled by the UI Control
        bool HandleInput(InputState input);
    }

    public interface IElement : IInputHandler
    {
        Rectangle Rect { get; }

        void Layout(Vector2 pos);
        void Draw(SpriteBatch spriteBatch);
    }
}
