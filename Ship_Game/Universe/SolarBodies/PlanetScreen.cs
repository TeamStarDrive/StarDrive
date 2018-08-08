using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class PlanetScreen : GameScreen
    {
        public Texture2D Panel;
        public Texture2D SliderBar;
        public Array<ToolTip> tips = new Array<ToolTip>();

        public PlanetScreen(GameScreen parent) : base(parent, pause: false)
        {
        }

        public override void Update(float elapsedTime)
        {
            base.Update(elapsedTime);
        }
    }
}