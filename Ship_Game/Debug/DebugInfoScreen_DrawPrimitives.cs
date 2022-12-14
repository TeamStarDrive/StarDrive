using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Debug
{
    public sealed partial class DebugInfoScreen
    {
        readonly Array<DebugPrimitive> Primitives = new();

        void AddPrimitive(DebugPrimitive primitive)
        {
            lock (Primitives) Primitives.Add(primitive);
        }

        // If the game is paused, then we should not add primitives
        // since they are only cleaned up while the game is running
        bool ShouldNotAddPrimitive(DebugModes mode)
        {
            return mode != Mode || !Visible || Screen.UState.Paused;
        }

        bool ShouldNotAddPrimitive()
        {
            return !Visible || Screen.UState.Paused;
        }

        public void DrawCircle(DebugModes mode, in Vector2 worldPos, float radius, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugCircle(worldPos.ToVec3(), radius, Color.Yellow, lifeTime));
        }

        public void DrawCircle(DebugModes mode, in Vector2 worldPos, float radius, Color color, float lifeTime = 0f)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugCircle(worldPos.ToVec3(), radius, color, lifeTime));
        }

        public void DrawCircle(DebugModes mode, in Vector3 worldPos, float radius, Color color, float lifeTime = 0f)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugCircle(worldPos, radius, color, lifeTime));
        }

        public void DrawRect(DebugModes mode, in Vector3 worldPos, float radius, float rotation, Color color, float lifeTime = 0f)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugRect(worldPos, radius, rotation, color, lifeTime));
        }

        public void DrawRect(DebugModes mode, in Vector2 worldPos, float radius, float rotation, Color color, float lifeTime = 0f)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugRect(worldPos.ToVec3(), radius, rotation, color, lifeTime));
        }

        public void DrawRect(DebugModes mode, in RectF rect, Color color, float rotation, float lifeTime = 0f)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugRect(rect, rotation, color, lifeTime));
        }

        public void DrawLine(DebugModes mode, Vector2 startInWorld, Vector2 endInWorld,
            float width, Color color, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugLine(startInWorld, endInWorld, width, color, lifeTime));
        }

        public void DrawArrow(DebugModes mode, Vector2 startInWorld, Vector2 endInWorld,
            float width, Color color, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode)) return;
            AddPrimitive(new DebugArrow(startInWorld, endInWorld, width, color, lifeTime));
        }

        public void DrawGameObject(DebugModes mode, GameObject obj, UniverseScreen u)
        {
            if (ShouldNotAddPrimitive(mode) || !obj.IsInFrustum(u)) return;
            AddPrimitive(new DebugGameObject(obj, Color.Red, 0f /*transient*/));
        }

        public void DrawGameObject(DebugModes mode, GameObject obj, UniverseScreen u, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode) || !obj.InFrustum) return;
            AddPrimitive(new DebugGameObject(obj, Color.Red, lifeTime));
        }

        public void DrawGameObject(DebugModes mode, GameObject obj, Color color, float lifeTime)
        {
            if (ShouldNotAddPrimitive(mode) || !obj.InFrustum) return;
            AddPrimitive(new DebugGameObject(obj, color, lifeTime));
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
                    if (!Screen.UState.Paused && primitive.Update(gameDeltaTime))
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
            Screen.DrawCircleProjected(worldPos, radius, color, thickness);
        }

        public void DrawLineImm(Vector2 worldA, Vector2 worldB, Color color, float thickness = 1f)
        {
            if (!Visible) return;
            Vector2d screenA = Screen.ProjectToScreenPosition(worldA);
            Vector2d screenB = Screen.ProjectToScreenPosition(worldB);
            DrawLine(screenA, screenB, color, thickness);
        }

        public void DrawArrowImm(Vector2 worldA, Vector2 worldB, Color color, float thickness = 1f)
        {
            if (!Visible) return;
            Vector2d screenA = Screen.ProjectToScreenPosition(worldA);
            Vector2d screenB = Screen.ProjectToScreenPosition(worldB);
            DrawLine(screenA, screenB, color, thickness);

            Vector2d screenDir = screenA.DirectionToTarget(screenB);
            Vector2d rightDir = screenDir.RightVector();

            double arrowSize = screenA.Distance(screenB) * 0.1;
            arrowSize = arrowSize.Clamped(5f, ScreenWidth * 0.05f);

            Vector2d thickOffset = rightDir*thickness;
            Vector2d arrowTip = screenB + thickOffset;
            Vector2d arrowButt = screenB - screenDir*arrowSize;
            Vector2d left  = arrowButt + screenDir.LeftVector()*arrowSize;
            Vector2d right = arrowButt + thickOffset + rightDir*arrowSize;

            DrawLine(arrowTip, left, color, thickness);
            DrawLine(arrowTip, right, color, thickness);
        }
    }
}