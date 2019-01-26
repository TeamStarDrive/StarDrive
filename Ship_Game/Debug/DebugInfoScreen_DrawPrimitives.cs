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
    }
}