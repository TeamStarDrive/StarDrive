using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page;

public class SpaceRoadsDebug : DebugPage
{

    int Down;
    int InProgress;
    int Online;
    float Maint;
    float timer;
    public SpaceRoadsDebug(DebugInfoScreen parent) : base(parent, DebugModes.Tasks)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.NonPlayerMajorEmpires)
        {
            if (!e.data.Defeated)
            {
                DrawSpaceRoads(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }

    void DrawSpaceRoads(Empire e, int column)
    {
        Text.SetCursor(Parent.Win.X + 10 + 150 * column, Parent.Win.Y + 200, e.EmpireColor);
        Text.String("--------------------------");
        Text.String(e.Name);
        if (--timer < 0)
        {
            (Down, InProgress, Online, Maint) = CountRoads(e.AI.SpaceRoads);
            timer = 60;
        }
        Text.String($"Number of Roads: {e.AI.SpaceRoads.Count} - (d{Down}, ip{InProgress}, o{Online}");
        Text.String($"Total Maintenance/Budget: {Maint.String(2)}/{e.AI.SSPBudget.String(2)}");
        Text.String("----------------------------");
        Text.NewLine();

        var spaceRoads = e.AI.SpaceRoads.SortedDescending(r => r.Heat);
        foreach (SpaceRoad road in spaceRoads)
        {
            Text.String($"{road.System1.Name}-{road.System2.Name}, (SSPs {road.NumProjectors}), (Heat {road.Heat}), {road.Status}");
        }
    }

    (int down, int inProgress, int active, float maint) CountRoads(Array<SpaceRoad>roads)
    {
        int down = 0;
        int inProgress = 0;
        int online = 0;
        float maint = 0;

        foreach (SpaceRoad road in roads)
        {
            maint += road.Maintenance;
            switch (road.Status)
            {
                case SpaceRoad.SpaceRoadStatus.Down:       down++;       break;
                case SpaceRoad.SpaceRoadStatus.InProgress: inProgress++; break;
                case SpaceRoad.SpaceRoadStatus.Online:     online++;     break;
            }
        }

        return (down, inProgress, online, maint);
    }
    void DrawTask(MilitaryTask t, Empire e)
    {
        Color color = t.TargetEmpire?.EmpireColor ?? e.EmpireColor;
        string target = t.TargetPlanet?.Name ?? t.TargetSystem?.Name ?? "";
        string fleet = t.Fleet != null ? $"Fleet Step: {t.Fleet.TaskStep}" : "";
        float str = t.Fleet?.GetStrength() ?? t.MinimumTaskForceStrength;
        Text.String(color, $"({t.Priority}) {t.Type}, {target}, str: {(int)str}, {fleet}");
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