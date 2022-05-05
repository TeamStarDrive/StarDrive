using SDGraphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    public abstract class Node
    {
        public TechEntry Entry;
        public Rectangle NodeRect;
        public Vector2 NodePosition;
        public bool isResearched;

        public virtual bool HandleInput(InputState input)
        {
            return false;
        }
    }
}