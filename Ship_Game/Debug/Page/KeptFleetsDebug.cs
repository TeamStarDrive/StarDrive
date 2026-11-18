using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page;

// per AI empire: the fleet cap, the highest cap it has had, and every fleet the cap counts, with its strength and ships by role
public class KeptFleetsDebug : DebugPage
{
    public KeptFleetsDebug(DebugInfoScreen parent) : base(parent, DebugModes.KeptFleets)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.ActiveNonPlayerMajorEmpires)
        {
            if (!e.IsDefeated)
            {
                DrawEmpire(e, column);
                ++column;
            }
        }
        base.Draw(batch, elapsed);
    }

    void DrawEmpire(Empire e, int column)
    {
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String(e.data.Traits.Name);
        Text.String($"Cap: {e.KeptFleetCap()}   Peak Cap: {e.PeakKeptFleetCap}   Kept: {e.KeptFleetCount()}");
        if (e.ShipsReadyForFleet is { } pool)
            Text.String($"Ships per full fleet: {pool.ShipsPerFullFleet:0}");

        foreach (Fleet fleet in e.ActiveFleets)
        {
            MilitaryTask task = fleet.FleetTask; // read once: the sim thread changes it
            if (fleet.Kept || task != null && MilitaryTask.UsesKeptFleets(task.Type))
                DrawFleet(fleet, task);
        }
    }

    void DrawFleet(Fleet fleet, MilitaryTask task)
    {
        Ship[] ships = fleet.Ships.ToArray(); // read once: the sim thread changes the list
        int count = 0;
        float strength = 0f;
        int fi = 0, cv = 0, fr = 0, cr = 0, bt = 0, dr = 0, ca = 0, bm = 0, sp = 0, tr = 0;
        for (int i = 0; i < ships.Length; ++i)
        {
            Ship ship = ships[i];
            if (ship is not { Active: true })
                continue;

            ++count;
            strength += ship.GetStrength();
            switch (ship.DesignRole)
            {
                case RoleName.fighter:   ++fi; break;
                case RoleName.corvette:  ++cv; break;
                case RoleName.frigate:   ++fr; break;
                case RoleName.cruiser:
                case RoleName.prototype: ++cr; break;
                case RoleName.battleship: ++bt; break;
                case RoleName.capital:   ++dr; break;
                case RoleName.carrier:   ++ca; break;
                case RoleName.bomber:    ++bm; break;
                case RoleName.support:   ++sp; break;
                case RoleName.troop:
                case RoleName.troopShip: ++tr; break;
            }
        }

        Text.NewLine();
        Text.String(fleet.Name);
        string doing = task != null ? $"{task.Type} step {fleet.TaskStep}" : "Idle";
        Text.String($"Str: {(int)strength}   Ships: {count}   {doing}");
        Text.String($"--- Fi: {fi}   Cv: {cv}   Fr: {fr}");
        Text.String($"--- Cr: {cr}   Bt: {bt}   Dr: {dr}");
        Text.String($"--- Ca: {ca}   Bm: {bm}   Sp: {sp}   Tr: {tr}");
    }
}
