using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Gameplay;
using static Ship_Game.AI.ThreatMatrix;

namespace Ship_Game.Debug.Page;

public class RelationshipDebug : DebugPage
{
    public RelationshipDebug(DebugInfoScreen parent) : base(parent, DebugModes.Relationship)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.NonPlayerMajorEmpires)
        {
            if (!e.IsDefeated)
            {
                DrawRelationships(e, column);
                ++column;
            }
        }

        base.Draw(batch, elapsed);
    }

    void DrawRelationships(Empire e, int column)
    {
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String("--------------------------");
        Text.String(e.Name);
        Text.String($"{e.Personality}");
        Text.String($"Average War Grade: {e.GetAverageWarGrade()}");
        Text.String("----------------------------");

        foreach (Relationship rel in e.AllRelations)
        {
            if (rel.Them.IsFaction || GlobalStats.RestrictAIPlayerInteraction && rel.Them.isPlayer || rel.Them.IsDefeated)
                continue;

            Text.String(rel.Them.EmpireColor, $"{rel.Them.Name}");
            Text.String(rel.Them.EmpireColor, $"Posture: {rel.Posture}");
            Text.String(rel.Them.EmpireColor, $"Trust (A/U/T)   : {rel.AvailableTrust.String(2)}/{rel.TrustUsed.String(2)}/{rel.Trust.String(2)}");
            Text.String(rel.Them.EmpireColor, $"Anger Diplomatic: {rel.Anger_DiplomaticConflict.String(2)}");
            Text.String(rel.Them.EmpireColor, $"Anger Border    : {rel.Anger_FromShipsInOurBorders.String(2)}");
            Text.String(rel.Them.EmpireColor, $"Anger Military  : {rel.Anger_MilitaryConflict.String(2)}");
            Text.String(rel.Them.EmpireColor, $"Anger Territory : {rel.Anger_TerritorialConflict.String(2)}");
            string nap   = rel.Treaty_NAPact      ? "NAP "      : "";
            string trade = rel.Treaty_Trade       ? ",Trade "   : "";
            string open  = rel.Treaty_OpenBorders ? ",Borders " : "";
            string ally  = rel.Treaty_Alliance    ? ",Allied "  : "";
            string peace = rel.Treaty_Peace       ? "Peace"     : "";
            Text.String(rel.Them.EmpireColor, $"Treaties: {nap}{trade}{open}{ally}{peace}");
            if (rel.NumTechsWeGave > 0)
                Text.String(rel.Them.EmpireColor, $"Techs We Gave Them: {rel.NumTechsWeGave}");

            if (rel.ActiveWar != null)
                Text.String(rel.Them.EmpireColor, "*** At War! ***");

            if (rel.PreparingForWar)
                Text.String(rel.Them.EmpireColor, "*** Preparing for War! ***");
            if (rel.PreparingForWar)
                Text.String(rel.Them.EmpireColor, $"*** {rel.PreparingForWarType} ***");

            Text.String(e.EmpireColor, "----------------------------");
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
