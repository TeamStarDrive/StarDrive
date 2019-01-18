using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class Node
    {
        public TechEntry tech;
        public Rectangle NodeRect;
        public Vector2 NodePosition;
        public bool isResearched;

        public virtual bool HandleInput(InputState input)
        {
            return false;
        }
    }
}