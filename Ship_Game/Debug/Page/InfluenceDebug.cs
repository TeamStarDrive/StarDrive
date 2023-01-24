using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page;

internal class InfluenceDebug : DebugPage
{
    SolarSystem System;
    public InfluenceDebug(DebugInfoScreen parent) : base(parent, DebugModes.Influence)
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

        base.Draw(spriteBatch, elapsed);
    }
}
