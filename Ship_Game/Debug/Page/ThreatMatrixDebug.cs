using Ship_Game.AI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.Debug.Page;

public class ThreatMatrixDebug : DebugPage
{
    readonly DebugEmpireSelectionSubmenu EmpireSelect;

    public ThreatMatrixDebug(DebugInfoScreen parent) : base(parent, DebugModes.ThreatMatrix)
    {
        EmpireSelect = base.Add(new DebugEmpireSelectionSubmenu(parent, parent.ModesTab.ClientArea.CutTop(10)));
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        float baseRadius = (int)Universe.ViewState / 100f;
        Empire e = EmpireSelect.Selected;

        foreach (ThreatCluster c in e.AI.ThreatMatrix.OurClusters)
            DrawCluster(c, e, baseRadius);
        
        foreach (ThreatCluster c in e.AI.ThreatMatrix.RivalClusters)
            DrawCluster(c, e, baseRadius);

        base.Draw(batch, elapsed);
    }

    void DrawCluster(ThreatCluster c, Empire e, float baseRadius)
    {
        float radius = baseRadius + c.Radius;
        if (Screen.IsInFrustum(c.Position, radius))
        {
            // the hexagon marks the observed pin with observed empire's color
            Screen.DrawCircleProjected(c.Position, radius, 6, c.Loyalty.EmpireColor);
            if (Universe.ViewState <= UniverseScreen.UnivScreenState.SystemView)
                Screen.DrawStringProjected(c.Position, radius*0.25f, c.Loyalty.EmpireColor, c.ToString());

            // if it's within our borders, draw "InBorders" using our color
            if (c.InBorders && Universe.ViewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                Screen.DrawStringProjected(c.Position-new Vector2(radius,radius*1.5f), radius*0.5f, e.EmpireColor, "InBorders");
            }

            // put a rectangle around the pin to mark observed empire's color
            Screen.DrawRectangleProjected(c.Position, new(radius*2), 0f, e.EmpireColor);
        }
    }

    public override bool HandleInput(InputState input)
    {
        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        base.Update(fixedDeltaTime);
    }
}