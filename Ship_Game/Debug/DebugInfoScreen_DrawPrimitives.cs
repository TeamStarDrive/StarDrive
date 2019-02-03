using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.Debug
{
    public sealed partial class DebugInfoScreen
    {
        void AddPrimitive(DebugPrimitive primitive)
        {
            lock (Primitives) Primitives.Add(primitive);
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, float lifeTime)
        {
            if (mode != Mode) return;
            AddPrimitive(new DebugCircle(worldPos, radius, Color.Yellow, lifeTime));
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, Color color, float lifeTime)
        {
            if (mode != Mode) return;
            AddPrimitive(new DebugCircle(worldPos, radius, color, lifeTime));
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, Color color)
        {
            if (mode != Mode) return;
            AddPrimitive(new DebugCircle(worldPos, radius, color, 0f));
        }

        public bool IgnoreThisShip(Ship ship)
        {
            return ship != null && Screen.SelectedShip != null && Screen.SelectedShip != ship;
        }

        public void DrawLine(DebugModes mode, Vector2 startInWorld, Vector2 endInWorld,
            float width, Color color, float lifeTime)
        {
            if (mode != Mode) return;
            AddPrimitive(new DebugLine(startInWorld, endInWorld, width, color, lifeTime));
        }

        public void DrawGameObject(DebugModes mode, GameplayObject obj)
        {
            if (mode != Mode || !obj.IsInFrustum) return;
            AddPrimitive(new DebugGameObject(obj, Color.Red, 0f /*transient*/));
        }

        void DrawDebugPrimitives(float gameDeltaTime)
        {
            lock (Primitives)
            {
                for (int i = Primitives.Count - 1; i >= 0; --i)
                {
                    DebugPrimitive primitive = Primitives[i];
                    primitive.Draw(Screen);
                    if (!Screen.Paused && primitive.Update(gameDeltaTime))
                    {
                        Primitives.RemoveAtSwapLast(i);
                    }
                }
            }
        }

        // This will draw immediately using the current SpriteBatch
        public void DrawCircleImmediate(Vector2 worldPos, float radius, Color color, float thickness = 1f)
        {
            Empire.Universe.DrawCircleProjected(worldPos, radius, color, thickness);
        }

        public void DrawLineImmediate(Vector2 worldA, Vector2 worldB, Color color, float thickness = 1f)
        {
            Vector2 screenA = Empire.Universe.ProjectToScreenPosition(worldA);
            Vector2 screenB = Empire.Universe.ProjectToScreenPosition(worldB);
            DrawLine(screenA, screenB, color, thickness);
        }

        public void DrawArrowImm(Vector2 worldA, Vector2 worldB, Color color, float thickness = 1f)
        {
            Vector2 screenA = Empire.Universe.ProjectToScreenPosition(worldA);
            Vector2 screenB = Empire.Universe.ProjectToScreenPosition(worldB);
            DrawLine(screenA, screenB, color, thickness);

            Vector2 screenDir = screenA.DirectionToTarget(screenB);
            Vector2 rightDir = screenDir.RightVector();

            float arrowSize = screenA.Distance(screenB) * 0.1f;
            arrowSize = arrowSize.Clamped(5f, ScreenWidth * 0.05f);

            Vector2 thickOffset = rightDir*thickness;
            Vector2 arrowTip = screenB + thickOffset;
            Vector2 arrowButt = screenB - screenDir*arrowSize;
            Vector2 left  = arrowButt + screenDir.LeftVector()*arrowSize;
            Vector2 right = arrowButt + thickOffset + rightDir*arrowSize;

            DrawLine(arrowTip, left, color, thickness);
            DrawLine(arrowTip, right, color, thickness);
        }

    }
}