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
            if (!e.data.Defeated)
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
        SetTextCursor(Parent.Win.X + 10 + 255 * column, Parent.Win.Y + 95, e.EmpireColor);
        DrawString(e.data.Traits.Name);

        if (e.data.DiplomaticPersonality != null)
        {
            DrawString(e.data.DiplomaticPersonality.Name);
            DrawString(e.data.EconomicPersonality.Name);
        }
        DrawString($"Money: {e.Money.String()} A:({e.GetActualNetLastTurn().String()}) T:({e.GrossIncome.String()})");
       
        DrawString($"Treasury Goal: {(int)eAI.ProjectedMoney} {(int)( e.AI.CreditRating * 100)}%");
        float taxRate = e.data.TaxRate * 100f;
        
        var ships = e.OwnedShips;
        DrawString($"Threat : av:{eAI.ThreatLevel:#0.00} $:{eAI.EconomicThreat:#0.00} " +
                   $"b:{eAI.BorderThreat:#0.00} e:{eAI.EnemyThreat:#0.00}");
        DrawString("Tax Rate:     "+taxRate.ToString("#.0")+"%");
        DrawString($"War Maint:  ({(int)e.AI.BuildCapacity}) Shp:{(int)e.TotalWarShipMaintenance} " +
                   $"Trp:{(int)(e.TotalTroopShipMaintenance + e.TroopCostOnPlanets)}");

        var warShips = ships.Filter(s => s.DesignRoleType == RoleType.Warship ||
                                         s.DesignRoleType == RoleType.WarSupport ||
                                         s.DesignRoleType == RoleType.Troop);
        DrawString($"   #:({warShips.Length})" +
                   $" f{warShips.Count(warship => warship?.DesignRole == RoleName.fighter || warship?.DesignRole == RoleName.corvette)}" +
                   $" g{warShips.Count(warship => warship?.DesignRole == RoleName.frigate || warship.DesignRole == RoleName.prototype)}" +
                   $" c{warShips.Count(warship => warship?.DesignRole == RoleName.cruiser)}" +
                   $" b{warShips.Count(warship => warship?.DesignRole == RoleName.battleship)}" +
                   $" c{warShips.Count(warship => warship?.DesignRole == RoleName.capital)}" +
                   $" v{warShips.Count(warship => warship?.DesignRole == RoleName.carrier)}" +
                   $" m{warShips.Count(warship => warship?.DesignRole == RoleName.bomber)}"
                   );
        DrawString("Civ Maint:  " +
                   $"({(int)e.AI.CivShipBudget}) {(int)e.TotalCivShipMaintenance} " +
                   $"#{ships.Count(freighter => freighter?.DesignRoleType == RoleType.Civilian)} " +
                   $"Inc({e.AverageTradeIncome})");
        DrawString($"Other Ship Maint:  Orb:{(int)e.TotalOrbitalMaintenance} - Sup:{(int)e.TotalEmpireSupportMaintenance}" +
                   $" #{ships.Count(warship => warship?.DesignRole == RoleName.platform || warship?.DesignRole == RoleName.station)}");
        DrawString($"Scrap:  {(int)e.TotalMaintenanceInScrap}");

        DrawString($"Build Maint:   ({(int)e.data.ColonyBudget}) {(int)e.TotalBuildingMaintenance}");
        DrawString($"Spy Count:     ({(int)e.data.SpyBudget}) {e.data.AgentList.Count}");
        DrawString("Spy Defenders: "+e.data.AgentList.Count(defenders => defenders.Mission == AgentMission.Defending));
        DrawString("Planet Count:  "+e.GetPlanets().Count);
        if (e.Research.HasTopic)
        {
            DrawString($"Research: {e.Research.Current.Progress:0}/{e.Research.Current.TechCost:0}({e.Research.NetResearch.String()})");
            DrawString("   --" + e.Research.Topic);
        }
        else
        {
            NewLine(2);
        }

        NewLine(3);
        DrawString("Total Pop: "+ e.TotalPopBillion.String(1) 
                                + "/" + e.MaxPopBillion.String(1) 
                                + "/" + e.GetTotalPopPotential().String(1));

        DrawString("Gross Food: "+ e.GetGrossFoodPerTurn().String());
        DrawString("Military Str: "+ (int)e.CurrentMilitaryStrength);
        DrawString("Offensive Str: " + (int)e.OffensiveStrength);
        DrawString($"Fleets: Str: {(int)e.AIManagedShips.InitialStrength} Avail: {e.AIManagedShips.InitialReadyFleets}");
        
        for (int x = 0; x < e.AI.Goals.Count; x++)
        {
            Goal g = e.AI.Goals[x];
            if (g is MarkForColonization)
            {
                NewLine();
                DrawString($"{g.TypeName} {g.TargetPlanet.Name}" +
                           $" (x{e.GetFleetStrEmpireMultiplier(g.TargetEmpire).String(1)})");

                DrawString(15f, $"Step: {g.StepName}");
                if (g.FinishedShip != null && g.FinishedShip.Active)
                    DrawString(15f, "Has ship");
            }
        }

        MilitaryTask[] tasks = e.AI.GetTasks().ToArr();
        for (int j = 0; j < tasks.Length; j++)
        {
            MilitaryTask task = tasks[j];
            string sysName = "Deep Space";
            for (int i = 0; i < e.Universe.Systems.Count; i++)
            {
                SolarSystem sys = e.Universe.Systems[i];
                if (task.AO.InRadius(sys.Position, sys.Radius))
                    sysName = sys.Name;
            }

            NewLine();
            var planet =task.TargetPlanet?.Name ?? "";
            DrawString($"FleetTask: {task.Type} {sysName} {planet}");
            DrawString(15f, $"Priority:{task.Priority}");
            float ourStrength = task.Fleet?.GetStrength() ?? task.MinimumTaskForceStrength;
            string strMultiplier = $" (x{e.GetFleetStrEmpireMultiplier(task.TargetEmpire).String(1)})";
            
            DrawString(15f, $"Strength: Them: {(int)task.EnemyStrength} Us: {(int)ourStrength} {strMultiplier}");
            if (task.WhichFleet != -1)
            {
                DrawString(15f, "Fleet: " + task.Fleet?.Name);
                DrawString(15f, $" Ships: {task.Fleet?.Ships.Count} CanWin: {task.Fleet?.CanTakeThisFight(task.EnemyStrength, task,true)}");
            }
        }

        NewLine();
        foreach (Relationship rel in e.AllRelations)
        {
            string plural = rel.Them.data.Traits.Plural;
            TextColor = rel.Them.EmpireColor;
            if (rel.Treaty_NAPact)
                DrawString(15f, "NA Pact with " + plural);

            if (rel.Treaty_Trade)
                DrawString(15f, "Trade Pact with " + plural);

            if (rel.Treaty_OpenBorders)
                DrawString(15f, "Open Borders with " + plural);

            if (rel.AtWar)
                DrawString(15f, $"War with {plural} ({rel.ActiveWar?.WarType})");
        }

        if (Screen.SelectedSystem != null)
        {
            SetTextCursor(Parent.Win.X + 10, 600f, Color.White);
            foreach (Ship ship in Screen.SelectedSystem.ShipList)
            {
                DrawString(ship?.Active == true ? ship.Name : ship?.Name + " (inactive)");
            }

            SetTextCursor(Parent.Win.X + 300, 600f, Color.White);
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