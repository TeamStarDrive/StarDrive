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
    float Timer;

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
        Text.SetCursor(Parent.Win.X + 10 + 400 * column, Parent.Win.Y + 50, e.EmpireColor);
        Text.String("--------------------------");
        Text.String(e.Name);
        if (--Timer < 0)
        {
            (Down, InProgress, Online, Maint) = CountRoads(e.AI.SpaceRoads);
            Timer = 60;
        }
        Text.String($"Number of Roads: {e.AI.SpaceRoads.Count} - (Dn {Down}, Ip {InProgress}, On {Online})");
        Text.String($"Total Maintenance/Budget: {Maint.String(2)}/{e.AI.SSPBudget.String(2)}");
        Text.String("----------------------------");
        Text.NewLine();

        var spaceRoads = e.AI.SpaceRoads.SortedDescending(r => r.Heat);
        for (int i = 0; i < spaceRoads.Length; i++)
        {
            SpaceRoad road = spaceRoads[i];
            Text.String($"{i+1}. {road.System1.Name}-{road.System2.Name}, (maint {road.Maintenance.String(2)}), " +
                        $"(SSPs {road.NumProjectors}), (Heat {road.Heat.String(2)}), {road.Status}");
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

    public override bool HandleInput(InputState input)
    {
        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        base.Update(fixedDeltaTime);
    }
}