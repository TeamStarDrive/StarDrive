using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Debug
{
    // @note This is the only scene graph interface in this
    //       engine that actually has a structure and makes sense.
    public abstract class DebugPrimitive
    {
        protected Color Color;
        float LifeTime;
        protected DebugPrimitive(Color color, float lifeTime)
        {
            Color = color;
            LifeTime = lifeTime;
        }
        public virtual bool Update(float gameDeltaTime)
        {
            LifeTime -= gameDeltaTime;
            return LifeTime <= 0f;
        }
        public abstract void Draw(UniverseScreen screen);
    }

    public class DebugCircle : DebugPrimitive
    {
        readonly Vector2 Center;
        readonly float Radius;
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
        readonly Vector2 StartInWorld;
        readonly Vector2 EndInWorld;
        readonly float Width;
        public DebugLine(Vector2 startInWorld, Vector2 endInWorld, float width,
            Color color, float lifeTime) : base(color, lifeTime)
        {
            StartInWorld = startInWorld;
            EndInWorld = endInWorld;
            Width = width;
        }
        public override void Draw(UniverseScreen screen)
        {
            screen.DrawLineWideProjected(StartInWorld, EndInWorld, Color, Width);
        }
    }

    // A little special, since the position changes every frame
    public class DebugGameObject : DebugPrimitive
    {
        readonly GameplayObject Obj;
        public DebugGameObject(GameplayObject obj,
            Color color, float lifeTime) : base(color, lifeTime)
        {
            Obj = obj;
        }
        public override bool Update(float gameDeltaTime)
        {
            if (!Obj.Active || !Obj.IsInFrustum)
                return true; // REMOVE
            return base.Update(gameDeltaTime);
        }
        public override void Draw(UniverseScreen screen)
        {
            screen.DrawCircleProjected(Obj.Center, Obj.Radius, Color, 2);
        }
    }

    public class DebugText : DebugPrimitive
    {
        readonly Vector2 PosInWorld;
        readonly string Text;
        public DebugText(Vector2 posInWorld, string text, Color color, float lifeTime) : base(color, lifeTime)
        {
            PosInWorld = posInWorld;
            Text = text;
        }
        public override void Draw(UniverseScreen screen)
        {
            screen.DrawStringProjected(PosInWorld, 0f, 1f, Color, Text);
        }
    }

}