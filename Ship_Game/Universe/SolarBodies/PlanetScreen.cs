using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class PlanetScreen : GameScreen
    {
        protected PlanetScreen(GameScreen parent) : base(parent, pause: false)
        {
        }
    }
}