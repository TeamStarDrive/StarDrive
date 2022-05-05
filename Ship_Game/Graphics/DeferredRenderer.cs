using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Graphics
{
    /// <summary>
    /// Thread-Safe deferred renderer for game screens
    /// </summary>
    public class DeferredRenderer
    {
        enum PrimitiveType
        {
            Point, Circle, Line, Rect, String,
            ScreenRect, ScreenString,
        }

        struct Primitive
        {
            public PrimitiveType Type;
            public Vector2 A; // A = Point, Circle Center, Line A, or Rectangle TopLeft
            public Vector2 B; // B = Circle radius(X), Line B, or Rectangle BottomLeft
            public Color Color;
            public string String;
            public Primitive(PrimitiveType type, Vector2 a, Vector2 b, Color color, string s = null)
            {
                Type = type; A = a; B = b; Color = color; String = s;
            }
        }

        readonly GameScreen Screen;
        readonly Func<int> SimTurnSource;
        readonly object Locker = new object();
        Array<Primitive> PrimitivesQueue = new Array<Primitive>();
        Array<Primitive> PrimitivesDrawing = new Array<Primitive>();
        int LastSimTurnId;

        public DeferredRenderer(GameScreen screen, Func<int> simTurnSource)
        {
            Screen = screen;
            SimTurnSource = simTurnSource;
        }

        public void Draw(SpriteBatch batch)
        {
            lock (Locker)
            {
                // swap Queued items with Drawing
                (PrimitivesQueue, PrimitivesDrawing) = (PrimitivesDrawing, PrimitivesQueue);
                PrimitivesQueue.Clear();
                LastSimTurnId = SimTurnSource?.Invoke() ?? LastSimTurnId + 1;
            }

            int count = PrimitivesDrawing.Count;
            Primitive[] primitives = PrimitivesDrawing.GetInternalArrayItems();
            Vector2d posA, posB;
            double size;

            for (int i = 0; i < count; ++i)
            {
                ref Primitive p = ref primitives[i];
                switch (p.Type)
                {
                    case PrimitiveType.Point:
                        posA = Screen.ProjectToScreenPosition(p.A);
                        batch.Draw(ResourceManager.WhitePixel, posA.ToVec2f(), null, p.Color);
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
                        batch.DrawRectangle(new AABoundingBox2Dd(posA, posB), p.Color);
                        break;
                    case PrimitiveType.String:
                        posA = Screen.ProjectToScreenPosition(p.A);
                        batch.DrawString(Fonts.Arial12, p.String, posA.ToVec2f(), p.Color);
                        break;
                    case PrimitiveType.ScreenRect:
                        batch.DrawRectangle(new AABoundingBox2D(p.A, p.B), p.Color);
                        break;
                    case PrimitiveType.ScreenString:
                        batch.DrawString(Fonts.Arial12, p.String, p.A, p.Color);
                        break;
                }
            }
        }

        void CheckDeferredPrimitives()
        {
            // simulation has already elapsed to a new frame
            // before any of primitives in the queue were submitted
            // in this case we just discard the queue
            if (SimTurnSource != null)
            {
                int simTurnId = SimTurnSource();
                if (simTurnId > LastSimTurnId)
                {
                    LastSimTurnId = simTurnId;
                    PrimitivesQueue.Clear();
                }
            }
        }
        
        // world coordinates
        public void DrawPointDeferred(Vector2 center, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Point, center, Vector2.Zero, color));
            }
        }
        
        // world coordinates
        public void DrawCircleDeferred(Vector2 center, float radius, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Circle, center, new Vector2(radius), color));
            }
        }
        
        // world coordinates
        public void DrawLineDeferred(Vector2 a, Vector2 b, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.Circle, a, b, color));
            }
        }
        
        // world coordinates
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

        // world coordinates
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
        
        // screen coordinates
        public void DrawScreenRect(in RectF rect, Color color)
        {
            var a = new Vector2(rect.X, rect.Y);
            var b = new Vector2(rect.X + rect.W, rect.Y + rect.H);
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.ScreenRect, a, b, color));
            }
        }

        // screen coordinates
        public void DrawScreenRect(Vector2 center, Vector2 size, Color color)
        {
            var a = new Vector2(center.X - size.X, center.Y - size.Y);
            var b = new Vector2(center.X + size.X, center.Y + size.Y);
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.ScreenRect, a, b, color));
            }
        }

        // world coordinates
        public void DrawStringDeferred(in Vector2 center, string text, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.String, center, Vector2.Zero, color, text));
            }
        }

        // screen coordinates
        public void DrawScreenString(Vector2 center, string text, Color color)
        {
            lock (Locker)
            {
                CheckDeferredPrimitives();
                PrimitivesQueue.Add(new Primitive(PrimitiveType.ScreenString, center, Vector2.Zero, color, text));
            }
        }
    }
}
