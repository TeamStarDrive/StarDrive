using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public static class DrawRoutines
    {
        static ScreenManager ScreenManager;
        private static UniverseScreen Screen;

        public static void Init(ScreenManager screenManager, UniverseScreen screen)
        {
            ScreenManager = screenManager;
            Screen = screen;
        }
        public static void Clear()
        {
            Screen = null;
            ScreenManager = null;
        }
        public static Circle DrawSelectionCircles(Vector2 worldPos, float worldRadius)
        {
            float radius = worldRadius;
            Vector3 project = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(worldPos, 0.0f), Screen.projection, Screen.view, Matrix.Identity);
            Vector2 center = new Vector2(project.X, project.Y);
            Vector3 projectPoint = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(worldPos.PointOnCircle(90f, radius), 0.0f), Screen.projection, Screen.view, Matrix.Identity);
            float Radius = Vector2.Distance(new Vector2(projectPoint.X, projectPoint.Y), center) + 10f;
            return new Circle(center, Radius);
        }

    }
}
