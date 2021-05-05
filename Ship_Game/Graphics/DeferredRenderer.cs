using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Graphics
{
    /// <summary>
    /// Thread-Safe deferred renderer for game screens
    /// </summary>
    public class DeferredRenderer
    {
        enum PrimitiveType { Point, Circle, Line, Rect }

        struct Primitive
        {
            public PrimitiveType Type;
            public Vector2 A; // A = Point, Circle Center, Line A, or Rectangle TopLeft
            public Vector2 B; // B = Circle radius(X), Line B, or Rectangle BottomLeft
            public Color Color;
            public Primitive(PrimitiveType type, Vector2 a, Vector2 b, Color color)
            {
                Type = type; A = a; B = b; Color = color;
            }
        }

        readonly GameScreen Screen;
        object Locker = new object();
        Array<Primitive> PrimitivesQueue = new Array<Primitive>();
        Array<Primitive> PrimitivesDrawing = new Array<Primitive>();
        int LastSimTurnId;

        public DeferredRenderer(GameScreen screen)
        {
            Screen = screen;
        }

        public void Draw(SpriteBatch batch)
        {
            lock (Locker)
            {
                // swap Queued items with Drawing
                var tmp = PrimitivesQueue;
                PrimitivesQueue = PrimitivesDrawing;
                PrimitivesDrawing = tmp;
                PrimitivesQueue.Clear();
                LastSimTurnId = Empire.Universe.SimTurnId;
            }

            int count = PrimitivesDrawing.Count;
            Primitive[] primitives = PrimitivesDrawing.GetInternalArrayItems();
            Vector2 posA, posB;
            float size;

            for (int i = 0; i < count; ++i)
            {
                ref Primitive p = ref primitives[i];
                switch (p.Type)
                {
                    case PrimitiveType.Point:
                        posA = Screen.ProjectToScreenPosition(p.A);
                        batch.Draw(ResourceManager.WhitePixel, posA, null, p.Color);
                        break;
                    case PrimitiveType.Circle:
                        Screen.ProjectToScreenCoords(p.A, p.B.X, out posA, out size);
                        batch.DrawCircle(posA, size, p.Color);
                        break;
                    case PrimitiveType.Line:
                        posA = Screen.ProjectToScreenPosition(p.A);
                        posB = Screen.ProjectToScreenPosition(p.B);
                        batch.DrawLine(posA, posB, p.Color);
                        break;
                    case PrimitiveType.Rect:
                        posA = Screen.ProjectToScreenPosition(p.A);
                        posB = Screen.ProjectToScreenPosition(p.B);
                        batch.DrawRectangle(new AABoundingBox2D(posA, posB), p.Color);
                        break;
                }
            }
        }

        void CheckDeferredPrimitives()
        {
            // simulation has already elapsed to a new frame
            int simTurnId = Empire.Universe.SimTurnId;
            if (simTurnId > LastSimTurnId)
            {
                LastSimTurnId = simTurnId;
                PrimitivesQueue.Clear();
            }
        }
        
        public void DrawPointDeferred(Vector2 center, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Point, center, Vector2.Zero, color));
            }
        }

        public void DrawCircleDeferred(Vector2 center, float radius, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Circle, center, new Vector2(radius), color));
            }
        }

        public void DrawLineDeferred(Vector2 a, Vector2 b, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Circle, a, b, color));
            }
        }

        public void DrawRectDeferred(in Rectangle rect, Color color)
        {
            var a = new Vector2(rect.X, rect.Y);
            var b = new Vector2(rect.X + rect.Width, rect.Y + rect.Height);
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Rect, a, b, color));
            }
        }

        public void DrawRectDeferred(in RectF rect, Color color)
        {
            var a = new Vector2(rect.X, rect.Y);
            var b = new Vector2(rect.X + rect.W, rect.Y + rect.H);
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Rect, a, b, color));
            }
        }
    }
}
