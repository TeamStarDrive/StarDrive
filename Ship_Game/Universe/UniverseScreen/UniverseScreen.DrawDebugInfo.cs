using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        ScrollList2<DebugStatItem> DebugStats;

        class DebugStatItem : ScrollListItem<DebugStatItem>
        {
            string Title;
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
                Title = title;
                HeaderMaxWidth = 800;
                ThisTime = perfTimer;
                Init(title, 0, 80);
            }
            public DebugStatItem(string title, Func<string> dynamicText)
            {
                DynamicText = dynamicText;
                Init(title, 0, 80);
            }
            public DebugStatItem(string title, AggregatePerfTimer perfTimer, AggregatePerfTimer master)
            {
                ThisTime = perfTimer;
                MasterTime = master;
                Init(title, 10, 80);
            }
            void Init(string title, float titleX, float valueX)
            {
                Title = title;
                UILabel lblTitle = Add(new UILabel(title));
                UILabel lblValue = Add(new UILabel(GetText));
                lblTitle.SetRelPos(titleX, 0);
                lblValue.SetRelPos(valueX, 0);
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

        void HideDebugGameInfo()
        {
            if (DebugStats != null)
            {
                DebugStats.RemoveFromParent();
                DebugStats = null;
            }
        }

        void ShowDebugGameInfo()
        {
            if (DebugStats == null)
            {
                DebugStats = Add(new ScrollList2<DebugStatItem>(220f, 40f, 400f, 600f, 20));
                DebugStats.EnableItemEvents = true;

                DebugStats.AddItem(new DebugStatItem("Time",
                    () => $"real {GameBase.Base.TotalElapsed:0.00}s   sim.time {CurrentSimTime:0.00}s/{TargetSimTime:0.00}s  lag:{(TargetSimTime - CurrentSimTime) * 1000:0.0}ms"));
                DebugStats.AddItem(new DebugStatItem("Ships",
                    () => GetMasterShipList().Count.ToString()));

                var sim = DebugStats.AddItem(new DebugStatItem("Sim", ProcessSimTurnsPerf, true));
                sim.AddSubItem(new DebugStatItem("FPS", 
                    () => $"actual:{ActualSimFPS}  target:{CurrentSimFPS}"));
                sim.AddSubItem(new DebugStatItem("NumTurns", 
                    () => ProcessSimTurnsPerf.MeasuredSamples.ToString()));

                var turn = DebugStats.AddItem(new DebugStatItem("Turn", TurnTimePerf, true));
                turn.AddSubItem(new DebugStatItem("PreEmp", PreEmpirePerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("Empire", EmpireUpdatePerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("PostEmp", PostEmpirePerf, TurnTimePerf));

                var objects = DebugStats.AddItem(new DebugStatItem("Objects", Objects.TotalTime, true));
                objects.AddSubItem(new DebugStatItem("List", Objects.ListTime, Objects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Systems", Objects.SysPerf, Objects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Ships", Objects.ShipsPerf, Objects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Projectiles", Objects.ProjPerf, Objects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Visibility", Objects.VisPerf, Objects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Spatial", Spatial.UpdateTime, Objects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Collide", Spatial.CollisionTime, Objects.TotalTime));

                var draw = DebugStats.AddItem(new DebugStatItem("Draw", DrawPerf, true));
                draw.AddSubItem(new DebugStatItem("Sync", DrawSyncPerf, DrawPerf));
            }
        }
    }
}