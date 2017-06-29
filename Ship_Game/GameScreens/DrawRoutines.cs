using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public static class DrawRoutines
    {
        public static Circle ProjectCircleWorldToScreen(Vector2 worldPos, float worldRadius)
        {
            UniverseScreen screen = Empire.Universe;
            Viewport viewport = screen.Viewport;

            Vector2 center    = viewport.Project(worldPos.ToVec3(), screen.projection, screen.view, Matrix.Identity).ToVec2();
            Vector2 projected = viewport.Project(worldPos.PointOnCircle(90f, worldRadius).ToVec3(), screen.projection, screen.view, Matrix.Identity).ToVec2();
            float screenRadius = Vector2.Distance(projected, center) + 10f;
            return new Circle(center, screenRadius);
        }
    }
}
