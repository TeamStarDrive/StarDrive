using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;

namespace Ship_Game.Debug.Page;

public class EmpireInfoDebug : DebugPage
{
    public EmpireInfoDebug(DebugInfoScreen parent) : base(parent, DebugModes.Empire)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        int column = 0;
        foreach (Empire e in Universe.MajorEmpires)
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
        EmpireAI eAI = e.AI;
        Text.SetCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        Text.String(e.data.Traits.Name);

        if (e.data.DiplomaticPersonality != null)
        {
            Text.String(e.data.DiplomaticPersonality.Name);
            Text.String(e.data.EconomicPersonality.Name);
        }
        Text.String($"Money: {e.Money.String()} A:({e.GetActualNetLastTurn().String()}) T:({e.GrossIncome.String()})");
       
        Text.String($"Treasury Goal: {(int)eAI.ProjectedMoney} ({(int)( e.AI.CreditRating * 100)}%)");
        float taxRate = e.data.TaxRate * 100f;
        
        var ships = e.OwnedShips;
        Text.String($"Threat : av:{eAI.ThreatLevel:#0.00} $:{eAI.EconomicThreat:#0.00} " +
                    $"b:{eAI.BorderThreat:#0.00} e:{eAI.EnemyThreat:#0.00}");
        Text.String("Tax Rate:     "+taxRate.ToString("#.0")+"%");
        Text.String($"War Maint:  ({(int)e.AI.BuildCapacity}) Shp:{(int)e.TotalWarShipMaintenance} " +
                    $"Trp:{(int)(e.TotalTroopShipMaintenance + e.TroopCostOnPlanets)}");

        var warShips = ships.Filter(s => s.DesignRoleType is RoleType.Warship 
                                    or RoleType.WarSupport
                                    or RoleType.Troop);

        Text.String($"Total Ships: {warShips.Length}");
        Text.String($"--- Fi: {warShips.Count(warship => warship?.DesignRole == RoleName.fighter)}   " +
                    $"Cv: {warShips.Count(warship => warship?.DesignRole == RoleName.corvette)}   " +
                    $"Fr: {warShips.Count(warship => warship?.DesignRole == RoleName.frigate)}");

        Text.String($"--- Cr: {warShips.Count(warship => warship?.DesignRole is RoleName.cruiser or RoleName.prototype)}   " +
                    $"Bt: {warShips.Count(warship => warship?.DesignRole == RoleName.battleship)}   " +
                    $"Dr: {warShips.Count(warship => warship?.DesignRole == RoleName.capital)}");

        Text.String($"--- Ca: {warShips.Count(warship => warship?.DesignRole == RoleName.carrier)}   " +
                    $"Bm: {warShips.Count(warship => warship?.DesignRole == RoleName.bomber)}   " +
                    $"Sp: {warShips.Count(warship => warship?.DesignRole == RoleName.support)}");

        Text.String("Civ Maint:  " +
                    $"({(int)e.AI.CivShipBudget}) {(int)e.TotalCivShipMaintenance} " +
                    $"#{ships.Count(freighter => freighter?.DesignRoleType == RoleType.Civilian)} " +
                    $"Inc({e.AverageTradeIncome})");
        Text.String($"Other Ship Maint:  Orb:{(int)e.TotalOrbitalMaintenance} - Sup:{(int)e.TotalEmpireSupportMaintenance}" +
                    $" #{ships.Count(warship => warship?.DesignRole == RoleName.platform || warship?.DesignRole == RoleName.station)}");
        Text.String($"Scrap:  {(int)e.TotalMaintenanceInScrap}");

        Text.String($"Build Maint/Budget:   {(int)e.TotalBuildingMaintenance}/{(int)e.AI.ColonyBudget}");
        Text.String($"Spy Count (Budget):   {e.data.AgentList.Count} ({(int)e.AI.SpyBudget})");
        Text.String("Spy Defenders: "+e.data.AgentList.Count(defenders => defenders.Mission == AgentMission.Defending));
        Text.String("Planet Count:  "+e.GetPlanets().Count);
        if (e.Research.HasTopic)
        {
            Text.String($"Research: {e.Research.Current.Progress:0}/{e.Research.Current.TechCost:0}({e.Research.NetResearch.String()})");
            Text.String("   --" + e.Research.Topic);
        }
        else
        {
            Text.NewLine(2);
        }

        Text.NewLine(3);
        Text.String("Total Pop: "+ e.TotalPopBillion.String(1) 
                                 + "/" + e.MaxPopBillion.String(1) 
                                 + "/" + e.GetTotalPopPotential().String(1));

        Text.String("Gross Food: "+ e.GetGrossFoodPerTurn().String());
        Text.String("Military Str: "+ (int)e.CurrentMilitaryStrength);
        Text.String("Offensive Str: " + (int)e.OffensiveStrength);
        Text.String($"Fleets: Str: {(int)e.AIManagedShips.InitialStrength} Avail: {e.AIManagedShips.InitialReadyFleets}");
        
        for (int x = 0; x < e.AI.Goals.Count; x++)
        {
            Goal g = e.AI.Goals[x];
            if (g is MarkForColonization)
            {
                Text.NewLine();
                Text.String($"{g.TypeName} {g.TargetPlanet.Name}" +
                            $" (x{e.GetFleetStrEmpireMultiplier(g.TargetEmpire).String(1)})");

                Text.String(15f, $"Step: {g.StepName}");
                if (g.FinishedShip != null && g.FinishedShip.Active)
                    Text.String(15f, "Has ship");
            }
        }

        Text.NewLine();
        foreach (Relationship rel in e.AllRelations)
        {
            string plural = rel.Them.data.Traits.Plural;
            Text.Color = rel.Them.EmpireColor;
            if (rel.Treaty_NAPact)
                Text.String(15f, "NA Pact with " + plural);

            if (rel.Treaty_Trade)
                Text.String(15f, "Trade Pact with " + plural);

            if (rel.Treaty_OpenBorders)
                Text.String(15f, "Open Borders with " + plural);

            if (rel.AtWar)
                Text.String(15f, $"War with {plural} ({rel.ActiveWar?.WarType})");
        }

        if (Screen.SelectedSystem != null)
        {
            Text.SetCursor(Parent.Win.X + 10, 600f, Color.White);
            foreach (Ship ship in Screen.SelectedSystem.ShipList)
            {
                Text.String(ship?.Active == true ? ship.Name : ship?.Name + " (inactive)");
            }

            Text.SetCursor(Parent.Win.X + 300, 600f, Color.White);
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