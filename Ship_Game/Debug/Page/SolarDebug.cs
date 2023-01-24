using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page;

internal class SolarDebug : DebugPage
{
    SolarSystem System;
    public SolarDebug(DebugInfoScreen parent) : base(parent, DebugModes.Trade)
    {
    }
        
    public override void Update(float fixedDeltaTime)
    {
        System = Screen.UState.Systems.FirstOrDefault(s => s.InFrustum);
        base.Update(fixedDeltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        Universe.Influence.DebugVisualize(Screen);

        if (System != null)
        {
            foreach (Planet p in System.PlanetList)
            {
                Screen.DrawCircleProjected(p.Position3D, p.Radius, Color.Green);
                Screen.DrawStringProjected(p.Position, p.Radius*0.2f, Color.Green, $"Scale={p.Scale}\nRadius={p.Radius}");
            }
            foreach (Moon m in System.MoonList)
            {
                Screen.DrawCircleProjected(m.Position3D, m.Radius, Color.LightGray);
                Screen.DrawStringProjected(m.Position, m.Radius*0.2f, Color.Green, $"Scale={m.MoonScale}\nRadius={m.Radius}");
            }
        }

        Spatial.VisualizerOptions sysOpt = new() { ObjectText = true };
        Universe.SystemsTree.DebugVisualize(Screen, sysOpt);

        Spatial.VisualizerOptions planetsOpt = new() { ObjectText = true, ZPlane = 2500 };
        Universe.PlanetsTree.DebugVisualize(Screen, planetsOpt);

        base.Draw(spriteBatch, elapsed);
    }
}