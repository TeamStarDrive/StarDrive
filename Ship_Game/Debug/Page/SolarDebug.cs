using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page;

internal class SolarDebug : DebugPage
{
    SolarSystem System;
    public SolarDebug(DebugInfoScreen parent) : base(parent, DebugModes.Solar)
    {
    }
        
    public override void Update(float fixedDeltaTime)
    {
        System = Screen.UState.FindClosestSystem(Screen.CursorWorldPosition2D);
        base.Update(fixedDeltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        if (System != null)
        {
            foreach (Planet p in System.PlanetList)
            {
                Screen.DrawCircleProjected(p.Position3D, p.Radius, Color.Green);
            }
            foreach (Moon m in System.MoonList)
            {
                Screen.DrawCircleProjected(m.Position3D, m.Radius, Color.LightGray);
            }
        }

        Spatial.VisualizerOptions sysOpt = new() { ObjectText = true };
        Universe.SystemsTree.DebugVisualize(Screen, sysOpt);

        Spatial.VisualizerOptions planetsOpt = new() { ObjectText = true, ZPlane = 2500 };
        Universe.PlanetsTree.DebugVisualize(Screen, planetsOpt);

        base.Draw(spriteBatch, elapsed);
    }
}
