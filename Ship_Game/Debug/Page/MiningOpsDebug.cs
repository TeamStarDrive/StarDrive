using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page;

public class MiningOpsDebug : DebugPage
{
    public MiningOpsDebug(DebugInfoScreen parent) : base(parent, DebugModes.MiningOps)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        if (Universe.P.DisableMiningOps)
        {
            Text.SetCursor(Parent.Win.X + 100, Parent.Win.Y+100, Color.White);
            Text.String("Mining Ops disabled. No data available");
            return;
        }

        int column = 0;
        int row = 50;
        foreach (Empire e in Universe.ActiveMajorEmpires)
        {
            DrawExoticResourcesTable(e, column / 5, row);
            ++column;
            if (column % 5 == 0)
                row = 50;
            else 
                row += 180;
        }

        base.Draw(batch, elapsed);
    }

    void DrawExoticResourcesTable(Empire e, int column, int row)
    {
        float cursorColumn = Parent.Win.X + 10 + 800 * column;
        float cursorRow = Parent.Win.Y + row;
        float columnOffset = 100;
        float rowOffset = 100;

        Text.SetCursor(cursorColumn, cursorRow, e.EmpireColor);
        Text.String($"--------------------------------------------------------------{e.Name}--------------------------------------------------------------");
        Text.String(0, "Resource", false);
        Text.String(1*rowOffset, "Bonus", false);
        Text.String(2*rowOffset, "Output", false);
        Text.String(3*rowOffset, "Ref/Cons", false);
        Text.String(4*rowOffset, "Storage", false);
        Text.String(5*rowOffset, "Potential", false);
        Text.String(6*rowOffset, "Num Ops");
        foreach (EmpireExoticBonuses bonus in e.GetExoticBonuses().Values)
        {
            EmpireExoticBonuses resource = e.GetExoticResource(bonus.Good.ExoticBonusType);
            Text.String(0, $"{new LocalizedText(bonus.Good.RefinedNameIndex).Text}", false);
            Text.String(1*rowOffset, $"{resource.DynamicBonusString}", false);
            Text.String(2*rowOffset, $"{resource.CurrentPercentageOutput}", false);
            Text.String(3*rowOffset, $"{resource.RefinedPerTurnForConsumption.String(2)}/{resource.Consumption.String(2)}", false);
            Text.String(4*rowOffset, $"{resource.CurrentStorage.String(1)}/{e.MaxExoticStorage.String(1)}", false);
            Text.String(5*rowOffset, $"{resource.MaxPotentialRefinedPerTurn}", false);
            Text.String(6*rowOffset, $"{resource.ActiveVsTotalOps}");
        }
        Text.String("----------------------------------------------------------------------------------------------------------------------------------------------------------");
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