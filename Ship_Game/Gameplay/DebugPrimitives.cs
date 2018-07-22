using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    // @note This is the only scene graph interface in this
    //       engine that actually has a structure and makes sense.
    public abstract class DebugPrimitive
    {
        protected Color Color;
        private float LifeTime;
        protected DebugPrimitive(Color color, float lifeTime)
        {
            Color = color;
            LifeTime  = lifeTime;
        }
        public bool Update(float gameDeltaTime)
        {
            LifeTime -= gameDeltaTime;
            return LifeTime <= 0f;
        }
        public abstract void Draw(UniverseScreen screen);
    }

	public class DebugCircle : DebugPrimitive
    {
        private readonly Vector2 Center;
        private readonly float Radius;
		public DebugCircle(Vector2 centerInWorld, float radius, 
		                   Color color, float lifeTime) : base(color, lifeTime)
		{
			Radius = radius;
			Center = centerInWorld;
		}
        public override void Draw(UniverseScreen screen)
        {
            screen.DrawCircleProjected(Center, Radius, Color, 2);
        }
    }

    public class DebugLine : DebugPrimitive
    {
        private readonly Vector2 StartInWorld;
        private readonly Vector2 EndInWorld;
        private readonly float Width;
        public DebugLine(Vector2 startInWorld, Vector2 endInWorld, float width, 
                         Color color, float lifeTime) : base(color, lifeTime)
        {
            StartInWorld = startInWorld;
            EndInWorld   = endInWorld;
            Width = width;
        }
        public override void Draw(UniverseScreen screen)
        {
            screen.DrawLineWideProjected(StartInWorld, EndInWorld, Color, Width);
        }
    }
}