using Ship_Game.AI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Spatial;
using Ship_Game.Ships;
using Ship_Game.UI;

namespace Ship_Game.Debug.Page;

public class ThreatMatrixDebug : DebugPage
{
    readonly DebugEmpireSelectionSubmenu EmpireSelect;
    Empire SelEmpire => EmpireSelect.Selected;
    ThreatMatrix SelThreats => SelEmpire.Threats;

    public ThreatMatrixDebug(DebugInfoScreen parent) : base(parent, DebugModes.ThreatMatrix)
    {
        EmpireSelect = base.Add(new DebugEmpireSelectionSubmenu(parent, parent.ModesTab.ClientArea.CutTop(10)));

        var list = EmpireSelect.Add(new UIList(new LocalPos(50, 100), new Vector2(100), ListLayoutStyle.ResizeList));
        list.Add(new UILabel(_ => $"Us: {SelEmpire.Name}"));
        list.Add(new UILabel(_ => $"OurClusters: {SelThreats.OurClusters.Length}"));
        list.Add(new UILabel(_ => $"RivalClusters: {SelThreats.RivalClusters.Length}"));

        // we shouldn't keep track of any empty clusters, because they should be auto-pruned
        list.Add(new UILabel(_ => $"# of Empty OurClusters (BUG): {SelThreats.OurClusters.Count(c => c.Ships.Length == 0)}"));
        list.Add(new UILabel(_ => $"# of Empty RivalClusters (BUG): {SelThreats.RivalClusters.Count(c => c.Ships.Length == 0)}"));
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        // make the radius a bit bigger if we zoom out, so we can see it
        float baseRadius = 1f;
        if (Universe.ViewState > UniverseScreen.UnivScreenState.SectorView)
            baseRadius = 16;
        else if (Universe.ViewState > UniverseScreen.UnivScreenState.SystemView)
            baseRadius = 8;

        Empire owner = SelEmpire;

        VisualizerOptions opt = new();
        owner.Threats.ClustersMap.DebugVisualize(Screen, opt);

        foreach (ThreatCluster c in owner.Threats.OurClusters)
            DrawCluster(c, owner, baseRadius);
        
        foreach (ThreatCluster c in owner.Threats.RivalClusters)
            DrawCluster(c, owner, baseRadius);

        base.Draw(batch, elapsed);
    }

    void DrawCluster(ThreatCluster c, Empire owner, float baseRadius)
    {
        float radius = c.Radius*baseRadius;
        if (Screen.IsInFrustum(c.Position, radius))
        {
            RectF screenR = Screen.ProjectToScreenRectF(RectF.FromPointRadius(c.Position, radius));
            Color clusterColor = c.Loyalty.EmpireColor;
            Color ownerColor = owner.EmpireColor;

            // the circle marks the observed cluster with observed empire's color
            Screen.DrawCircle(screenR.Center, screenR.Radius, clusterColor, 2);

            if (Universe.ViewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                // draw information text right next to the cluster
                Vector2 cursor = screenR.TopRight.Rounded();
                DrawText(ref cursor, clusterColor, $"Ships={c.Ships.Length}");
                DrawText(ref cursor, clusterColor, $"Strength={c.Strength}");
                DrawText(ref cursor, clusterColor, $"InBorders={c.InBorders}");
                DrawText(ref cursor, clusterColor, $"Loyalty={c.Loyalty}");
                DrawText(ref cursor, clusterColor, $"System={c.System?.Name??"none"}");

                // draw lines from center of cluster to 
                foreach (Ship s in c.Ships)
                {
                    // NOTE: always use the real loyalty color, to find accidental errors
                    Screen.DrawLineProjected(c.Position, s.Position, s.Loyalty.EmpireColor.Alpha(0.33f));
                    Screen.DrawCircleProjected(s.Position, s.Radius, s.Loyalty.EmpireColor);
                }
            }

            // put a rectangle around the cluster to show the observer color
            Screen.DrawRectangle(screenR, ownerColor);
        }
    }

    void DrawText(ref Vector2 cursor, Color color, string text)
    {
        Screen.DrawString(cursor, color, text, Fonts.Arial10);
        cursor.Y += Fonts.Arial10.LineSpacing;
    }
}