using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Ships;
using System.Linq;

namespace Ship_Game.Debug.Page;

public class TechDebug : DebugPage
{
    public TechDebug(DebugInfoScreen parent) : base(parent, DebugModes.Tech)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        TextCursor.Y -= (float)(Fonts.Arial20Bold.LineSpacing + 2) * 4;
        int column = 0;
        foreach (Empire e in Universe.Empires)
        {
            if (!e.IsFaction && !e.data.Defeated)
            {
                DrawEmpireTech(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }
    void DrawEmpireTech(Empire e, int column)
    {
        SetTextCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 10, e.EmpireColor);
        DrawString(e.data.Traits.Name);

        if (e.data.DiplomaticPersonality != null)
        {
            DrawString(e.data.DiplomaticPersonality.Name);
            DrawString(e.data.EconomicPersonality.Name);
        }

        DrawString($"Corvettes: {e.canBuildCorvettes}");
        DrawString($"Frigates: {e.canBuildFrigates}");
        DrawString($"Cruisers: {e.canBuildCruisers}");
        DrawString($"Battleships: {e.CanBuildBattleships}");
        DrawString($"Capitals: {e.canBuildCapitals}");
        DrawString($"Bombers: {e.canBuildBombers}");
        DrawString($"Carriers: {e.canBuildCarriers}");
        DrawString($"Troopships: {e.canBuildTroopShips}");
        NewLine();
        if (e.Research.HasTopic)
        {
            DrawString($"Research: {e.Research.Current.Progress:0}/{e.Research.Current.TechCost:0} ({e.Research.NetResearch.String()} / {e.Research.MaxResearchPotential.String()})");
            DrawString("   --" + e.Research.Topic);
            Ship bestShip = e.AI.TechChooser.LineFocus.BestCombatShip;
            if (bestShip != null)
            {
                var neededTechs = bestShip.ShipData.TechsNeeded.Except(e.ShipTechs);
                float techCost = 0;
                foreach(var tech in neededTechs)
                    techCost += e.TechCost(tech);

                DrawString($"Ship : {bestShip.Name}");
                DrawString($"Hull : {bestShip.BaseHull.Role}");
                DrawString($"Role : {bestShip.DesignRole}");
                DrawString($"Str : {(int)bestShip.BaseStrength} - Tech : {techCost}");
            }
        }
        DrawString("");
        if (Parent.GetResearchLog(e, out var empireLog))
        {
            for (int x = 0; x < empireLog.Count - 1; x++)
            {
                var text = empireLog[x];
                DrawString(text ?? "Error");
            }
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