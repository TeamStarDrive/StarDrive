using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class PlanetScreen
    {
        public Texture2D Panel;
        public Texture2D SliderBar;
        public Array<ToolTip> tips = new Array<ToolTip>();
        protected ScreenManager ScreenManager;

        public PlanetScreen()
        {
        }

        public PlanetScreen(Planet p, ScreenManager screenManager)
        {
            ScreenManager = screenManager;
        }

        public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
        }

        public virtual void HandleInput(InputState input)
        {
        }

        public virtual void Update(float elapsedTime)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 center, float radius, int sides, Color color, float thickness = 1.0f)
            => ScreenManager.SpriteBatch.DrawCircle(center, radius, sides, color, thickness);
    }
}