using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.UI;

namespace Ship_Game.Debug.Page;

public class PerfDebug : DebugPage
{
    ScrollList<DebugStatItem> Stats;

    public PerfDebug(DebugInfoScreen parent) : base(parent, DebugModes.Perf)
    {
        RectF statsR = new(parent.X + 360, parent.Y + 120, 440, 600);
        Stats = base.Add(new ScrollList<DebugStatItem>(statsR, 20));
        Stats.EnableItemEvents = true;

        var o = Universe.Objects;
        var s = Screen;

        Stats.AddItem(new("Time",
            () => $"real {GameBase.Base.TotalElapsed:0.00}s   sim.time {s.CurrentSimTime:0.00}s/{s.TargetSimTime:0.00}s  lag:{(s.TargetSimTime - s.CurrentSimTime) * 1000:0.0}ms"));
        Stats.AddItem(new("Ships", () => o.NumShips.ToString()));
        Stats.AddItem(new("Proj", () => o.NumProjectiles.ToString()));
        Stats.AddItem(new("DyLights", () => s.ScreenManager.ActiveDynamicLights.ToString()));
        Stats.AddItem(new("Perf", () => "avg-sample  max-sample  total/sec"));

        var sim = Stats.AddItem(new("Sim", s.ProcessSimTurnsPerf, true));
        sim.AddSubItem(new DebugStatItem("FPS", () => $"actual:{s.ActualSimFPS}  target:{s.CurrentSimFPS}"));
        sim.AddSubItem(new DebugStatItem("NumTurns", () => s.ProcessSimTurnsPerf.MeasuredSamples.ToString()));

        var turn = Stats.AddItem(new("Turn", s.TurnTimePerf, true));
        turn.AddSubItem(new DebugStatItem("PreEmp", s.PreEmpirePerf, s.TurnTimePerf));
        turn.AddSubItem(new DebugStatItem("Empire", s.EmpireUpdatePerf, s.TurnTimePerf));
        turn.AddSubItem(new DebugStatItem("Influence", s.EmpireInfluPerf, s.TurnTimePerf));
        turn.AddSubItem(new DebugStatItem(" ResetBorders", s.ResetBordersPerf, s.EmpireInfluPerf));
        turn.AddSubItem(new DebugStatItem(" PlanetScans", s.ScanFromPlanetsPerf, s.EmpireInfluPerf));
        turn.AddSubItem(new DebugStatItem(" ThreatMatrix", s.ThreatMatrixPerf, s.EmpireInfluPerf));
        turn.AddSubItem(new DebugStatItem("Objects", o.TotalTime, s.TurnTimePerf));
        turn.AddSubItem(new DebugStatItem("Misc", s.EmpireMiscPerf, s.TurnTimePerf));
        turn.AddSubItem(new DebugStatItem("PostEmp", s.PostEmpirePerf, s.TurnTimePerf));

        var objects = Stats.AddItem(new("Objects", o.TotalTime, true));
        objects.AddSubItem(new DebugStatItem("List", o.ListTime, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("SysShips", o.SysShipsPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Systems", o.SysPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Ships", o.ShipsPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("ShipAI", o.ShipAiPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Projectiles", o.ProjPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Visibility", o.VisPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Spatial", Universe.Spatial.UpdateTime, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Removal", o.ObjectRemoval, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Collide", Universe.Spatial.CollisionTime, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("Sensors", o.SensorPerf, o.TotalTime));
        objects.AddSubItem(new DebugStatItem("    Sensors", () => $"current:{o.Scans} per/s:{o.ScansPerSec}"));

        Stats.AddItem(new("TotalDraw", s.DrawGroupTotalPerf, true));

        var render = Stats.AddItem(new("Render", s.RenderGroupTotalPerf, true));
        render.AddSubItem(new DebugStatItem("Sunburn.Begin", s.BeginSunburnPerf, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Backdrop", s.BackdropPerf, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Sunburn.Draw", s.SunburnDrawPerf, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Planets", s.DrawPlanetsPerf, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Shields", s.DrawShieldsPerf, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Particles", s.DrawParticles, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Explosions", s.DrawExplosionsPerf, s.RenderGroupTotalPerf));
        render.AddSubItem(new DebugStatItem("Sunburn.End", s.EndSunburnPerf, s.RenderGroupTotalPerf));

        var overlays = Stats.AddItem(new("Overlays", s.OverlaysGroupTotalPerf, true));
        overlays.AddSubItem(new DebugStatItem("Influence", s.DrawFogInfluence, s.OverlaysGroupTotalPerf));
        overlays.AddSubItem(new DebugStatItem("Borders", s.DrawBorders, s.OverlaysGroupTotalPerf));
        overlays.AddSubItem(new DebugStatItem("FogOfWar", s.DrawFogOfWar, s.OverlaysGroupTotalPerf));
        overlays.AddSubItem(new DebugStatItem("OverFog", s.DrawOverFog, s.OverlaysGroupTotalPerf));

        var icons = Stats.AddItem(new("Icons", s.IconsGroupTotalPerf, true));
        icons.AddSubItem(new DebugStatItem("Projectiles", s.DrawProj, s.IconsGroupTotalPerf));
        icons.AddSubItem(new DebugStatItem("ShipOveray", s.DrawShips, s.IconsGroupTotalPerf));
        icons.AddSubItem(new DebugStatItem("Icons", s.DrawIcons, s.IconsGroupTotalPerf));
        icons.AddSubItem(new DebugStatItem("UI", s.DrawUI, s.IconsGroupTotalPerf));
    }

    class DebugStatItem : ScrollListItem<DebugStatItem>
    {
        const float TitleOffset = 120;
        readonly AggregatePerfTimer ThisTime;
        readonly AggregatePerfTimer MasterTime;
        readonly Func<string> DynamicText;
        string GetText(UILabel label)
        {
             if (MasterTime != null)
                 return ThisTime.String(MasterTime);
             if (ThisTime != null)
                 return ThisTime.ToString();
             if (DynamicText != null)
                 return DynamicText();
             return "";
        }
        public override int ItemHeight => IsHeader ? 40 : 16;
        public DebugStatItem(string title, AggregatePerfTimer perfTimer, bool isHeader)
            : base(null)
        {
            HeaderMaxWidth = 800;
            ThisTime = perfTimer;
            Init(title, 0, TitleOffset);
        }
        public DebugStatItem(string title, Func<string> dynamicText)
        {
            DynamicText = dynamicText;
            Init(title, 0, TitleOffset);
        }
        public DebugStatItem(string title, AggregatePerfTimer perfTimer, AggregatePerfTimer master)
        {
            ThisTime = perfTimer;
            MasterTime = master;
            Init(title, 10, TitleOffset);
        }
        void Init(string title, float titleX, float valueX)
        {
            UILabel lblTitle = Add(new UILabel(title));
            UILabel lblValue = Add(new UILabel(GetText));
            lblTitle.SetLocalPos(titleX, 0);
            lblValue.SetLocalPos(valueX, 0);
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (IsHeader)
            {
                // dark blueish transparent background for Headers
                var edgeColor = new Color(75, 99, 125, 100);
                Color bkgColor = Hovered ? edgeColor : new Color(35, 59, 85, 50);
                new Selector(Rect, bkgColor, edgeColor).Draw(batch, elapsed);
            }
            base.Draw(batch, elapsed);
        }
    }
}
