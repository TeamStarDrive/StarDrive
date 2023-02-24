using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Input;
using SDGraphics.Rendering;
using SDGraphics.Sprites;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace Ship_Game.Debug.Page;
using Vector2 = SDGraphics.Vector2;

internal class SolarDebug : DebugPage
{
    SolarSystem System;
    public SolarDebug(DebugInfoScreen parent) : base(parent, DebugModes.Solar)
    {
    }

    public override bool HandleInput(InputState input)
    {
        if (input.IsKeyDown(Keys.OemComma))
        {
            if (input.KeyPressed(Keys.Up))
                Screen.BorderBlendSrc = Screen.BorderBlendSrc.IncrementWithWrap(-1);
            else if (input.KeyPressed(Keys.Down))
                Screen.BorderBlendSrc = Screen.BorderBlendSrc.IncrementWithWrap(+1);
        }
        else if (input.IsKeyDown(Keys.OemPeriod))
        {
            if (input.KeyPressed(Keys.Up))
                Screen.BorderBlendDest = Screen.BorderBlendDest.IncrementWithWrap(-1);
            else if (input.KeyPressed(Keys.Down))
                Screen.BorderBlendDest = Screen.BorderBlendDest.IncrementWithWrap(+1);
        }
        return base.HandleInput(input);
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
        
        var frustum = Screen.VisibleWorldRect;
        Empire[] empires = Universe.Empires.Sorted(e=> e.MilitaryScore);

        SpriteRenderer sr = Screen.ScreenManager.SpriteRenderer;

        foreach (Empire empire in empires)
        {
            Empire.InfluenceNode[] nodes = empire.BorderNodeCache.BorderNodes;
            if (nodes.Length == 0)
                continue;

            sr.Begin(Screen.ViewProjection);

            for (int x = 0; x < nodes.Length; x++)
            {
                ref Empire.InfluenceNode inf = ref nodes[x];
                if (inf.KnownToPlayer && frustum.Overlaps(inf.Position, inf.Radius))
                    Screen.DrawCircleProjected(inf.Position, inf.Radius, Color.Orange, 2); // DEBUG
            }
            foreach (InfluenceConnection c in empire.BorderNodeCache.Connections)
            {
                Empire.InfluenceNode a = c.Node1;
                Empire.InfluenceNode b = c.Node2;
                if (frustum.Overlaps(a.Position, a.Radius) || frustum.Overlaps(b.Position, b.Radius))
                {
                    Parent.DrawArrowImm(a.Position, b.Position, Color.Red, 2); // DEBUG

                    float radius = Math.Min(a.Radius, b.Radius);
                    float width = 2.0f * radius;
                    
                    // make a quad by reusing the Quad3D line constructor
                    Quad3D connectLine = new(a.Position, b.Position, width, zValue: 0f);
                    sr.DrawRectLine(connectLine, Color.Brown, thickness: 5000);
                }
            }

            sr.End();
        }
        
        Screen.DrawString(new(300, 200), Color.White, $"SrcBlend: {Screen.BorderBlendSrc}  Change with COMMA+UP/DOWN keys", Fonts.Arial20Bold);
        Screen.DrawString(new(300, 240), Color.White, $"DstBlend: {Screen.BorderBlendDest}  Change with PERIOD+UP/DOWN keys", Fonts.Arial20Bold);

        base.Draw(spriteBatch, elapsed);
    }
}
