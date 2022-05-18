using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

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
        readonly Vector3 Center;
        readonly float Radius;
        public DebugCircle(Vector3 centerInWorld, float radius,
            Color color, float lifeTime) : base(color, lifeTime)
        {
            Radius = radius;
            Center = centerInWorld;
        }
        public override void Draw(UniverseScreen screen)
        {
            Vector2d screenPos = screen.ProjectToScreenPosition(Center);
            double radius = screen.ProjectToScreenSize(Radius);
            screen.DrawCircle(screenPos, radius, Color, 2);
        }
    }

    public class DebugRect : DebugPrimitive
    {
        readonly Vector3 Center;
        readonly Vector2 Size;
        readonly float Rotation;
        public DebugRect(in RectF worldRect, float rotation, Color color, float lifeTime)
            : base(color, lifeTime)
        {
            Center = worldRect.Center.ToVec3();
            Size = worldRect.Size;
            Rotation = rotation;
        }
        public DebugRect(in Vector3 center, float radius, float rotation, Color color, float lifeTime)
            : base(color, lifeTime)
        {
            Center = center;
            Size = new Vector2(radius*2f);
            Rotation = rotation;
        }
        public override void Draw(UniverseScreen screen)
        {
            RectF screenRect = screen.ProjectToScreenRectF(Center, Size);
            screen.ScreenManager.SpriteBatch.DrawRectangle(screenRect, Rotation, Color, 2);
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

    public class DebugArrow : DebugPrimitive
    {
        readonly Vector2 StartInWorld;
        readonly Vector2 EndInWorld;
        readonly float Width;
        public DebugArrow(Vector2 startInWorld, Vector2 endInWorld, float width,
            Color color, float lifeTime) : base(color, lifeTime)
        {
            StartInWorld = startInWorld;
            EndInWorld = endInWorld;
            Width = width;
        }
        public override void Draw(UniverseScreen screen)
        {
            Vector2d screenA = screen.ProjectToScreenPosition(StartInWorld);
            Vector2d screenB = screen.ProjectToScreenPosition(EndInWorld);
            screen.DrawLine(screenA, screenB, Color, Width);

            Vector2d screenDir = screenA.DirectionToTarget(screenB);
            Vector2d rightDir = screenDir.RightVector();

            double arrowSize = screenA.Distance(screenB) * 0.1;
            arrowSize = arrowSize.Clamped(5f, screen.ScreenWidth * 0.05f);

            Vector2d thickOffset = rightDir*Width;
            Vector2d arrowTip = screenB + thickOffset;
            Vector2d arrowButt = screenB - screenDir*arrowSize;
            Vector2d left  = arrowButt + screenDir.LeftVector()*arrowSize;
            Vector2d right = arrowButt + thickOffset + rightDir*arrowSize;

            screen.DrawLine(arrowTip, left, Color, Width);
            screen.DrawLine(arrowTip, right, Color, Width);
        }
    }

    // A little special, since the position changes every frame
    public class DebugGameObject : DebugPrimitive
    {
        readonly GameObject Obj;
        public DebugGameObject(GameObject obj, Color color, float lifeTime) : base(color, lifeTime)
        {
            Obj = obj;
        }
        public override bool Update(float gameDeltaTime)
        {
            if (!Obj.Active)
                return true; // REMOVE
            return base.Update(gameDeltaTime);
        }
        public override void Draw(UniverseScreen screen)
        {
            screen.DrawCircleProjected(Obj.Position, Obj.Radius, Color, 2);
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