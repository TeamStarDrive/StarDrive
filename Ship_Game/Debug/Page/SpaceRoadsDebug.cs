using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page;

public class SpaceRoadsDebug : DebugPage
{
    readonly Map<int, RoadStats> EmpireRoadStats = new();
    float Timer;

    class RoadStats
    {
        public int Down, InProgress, Online;
        public float Maint; 

        public void CountRoads(Array<SpaceRoad> roads)
        {
            Down       = 0;
            InProgress = 0;
            Online     = 0;
            Maint      = 0;
            foreach (SpaceRoad road in roads)
            {
                Maint += road.Maintenance;
                switch (road.Status)
                {
                    case SpaceRoad.SpaceRoadStatus.Down:       Down++;       break;
                    case SpaceRoad.SpaceRoadStatus.InProgress: InProgress++; break;
                    case SpaceRoad.SpaceRoadStatus.Online:     Online++;     break;
                }
            }
        }
    }


    public SpaceRoadsDebug(DebugInfoScreen parent) : base(parent, DebugModes.SpaceRoads)
    {
        foreach (Empire e in Universe.MajorEmpires)
            EmpireRoadStats.Add(e.Id, new RoadStats());
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        Timer--;
        int column = 0;
        for (int i = 0; i < Universe.MajorEmpires.Length; i++)
        {
            Empire e = Universe.MajorEmpires[i];
            if (!e.data.Defeated && (!e.isPlayer || e.AutoBuildSpaceRoads))
            {
                DrawSpaceRoads(e, column, Timer < 0);
                ++column;
            }
        }

        if (Timer < 0)
            Timer = 60;

        base.Draw(batch, elapsed);
    }

    void DrawSpaceRoads(Empire e, int column, bool recount)
    {
        Text.SetCursor(Parent.Win.X + 10 + 400 * column, Parent.Win.Y + 50, e.EmpireColor);
        Text.String("--------------------------");
        Text.String(e.Name);
        RoadStats stats = EmpireRoadStats[e.Id];
        if (recount)
            stats.CountRoads(e.AI.SpaceRoadsManager.SpaceRoads);
        
        Text.String($"Number of Roads: {e.AI.SpaceRoadsManager.SpaceRoads.Count} - (Dn {stats.Down}, Ip {stats.InProgress}, On {stats.Online})");
        Text.String($"Total Maintenance/Budget: {stats.Maint.String(2)}/{e.AI.SSPBudget.String(2)}");
        Text.String("----------------------------");
        Text.NewLine();

        var spaceRoads = e.AI.SpaceRoadsManager.SpaceRoads.SortedDescending(r => r.Heat);
        for (int i = 0; i < spaceRoads.Length; i++)
        {
            SpaceRoad road = spaceRoads[i];
            Text.String($"{i+1}. {road.System1.Name}-{road.System2.Name}, maint: {road.Maintenance.String(2)}, " +
                        $"SSPs: {road.NumProjectors}, overlap: {road.RoadNodesList.Count(n => n.Overlapping)}, " +
                        $"Heat: {road.Heat.String(2)}, {road.Status}");
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