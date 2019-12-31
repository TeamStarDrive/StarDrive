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

        // If the game is paused, then we should not add primitives
        // since they are only cleaned up while the game is running
        bool ShouldNotAddPrimitive(DebugModes mode)
        {
            return mode != Mode || !Visible || Empire.Universe.Paused;
        }

        bool ShouldNotAddPrimitive()
        {
            return !Visible || Empire.Universe.Paused;
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugCircle(worldPos, radius, Color.Yellow, lifeTime));
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, Color color, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugCircle(worldPos, radius, color, lifeTime));
        }

        public void DrawCircle(DebugModes mode, Vector2 worldPos, float radius, Color color)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugCircle(worldPos, radius, color, 0f));
        }

        public void DrawLine(DebugModes mode, Vector2 startInWorld, Vector2 endInWorld,
            float width, Color color, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugLine(startInWorld, endInWorld, width, color, lifeTime));
        }

        public void DrawGameObject(DebugModes mode, GameplayObject obj)
        {
            if (ShouldNotAddPrimitive(mode) || !obj.IsInFrustum) return;
            AddPrimitive(new DebugGameObject(obj, Color.Red, 0f /*transient*/));
        }

        public void DrawText(Vector2 posInWorld, string text, Color color, float lifeTime = 0f)
        {
            if (ShouldNotAddPrimitive()) return;
            AddPrimitive(new DebugText(posInWorld, text, color, lifeTime));
        }

        public void DrawText(DebugModes mode, Vector2 posInWorld, string text, Color color, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugText(posInWorld, text, color, lifeTime));
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
        public void DrawCircleImm(Vector2 worldPos, float radius, Color color, float thickness = 1f)
        {
            if (!Visible) return;
            Empire.Universe.DrawCircleProjected(worldPos, radius, color, thickness);
        }

        public void DrawLineImm(Vector2 worldA, Vector2 worldB, Color color, float thickness = 1f)
        {
            if (!Visible) return;
            Vector2 screenA = Empire.Universe.ProjectToScreenPosition(worldA);
            Vector2 screenB = Empire.Universe.ProjectToScreenPosition(worldB);
            DrawLine(screenA, screenB, color, thickness);
        }

        public void DrawArrowImm(Vector2 worldA, Vector2 worldB, Color color, float thickness = 1f)
        {
            if (!Visible) return;
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