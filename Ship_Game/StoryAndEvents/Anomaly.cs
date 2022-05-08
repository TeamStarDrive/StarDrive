using SDGraphics.Sprites;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public class Anomaly
    {
        public Vector2 Position;
        public string type;

        public virtual void Draw(SpriteRenderer renderer)
        {
        }

        public virtual void Update(FixedSimTime timeStep)
        {
        }
    }
}