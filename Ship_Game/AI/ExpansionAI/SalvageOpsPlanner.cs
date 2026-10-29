using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.AI.ExpansionAI
{
    // every few turns, as the mining planner, for an AI empire or the player with Auto Build Salvage Stations on: a salvage station in a system worth one, the closest to the empire first
    [StarDataType]
    public class SalvageOpsPlanner
    {
        [StarData] readonly Empire Owner;
        [StarData] internal int TurnTimer;

        UniverseState Universe => Owner.Universe;
        [StarDataConstructor] SalvageOpsPlanner() { }

        public SalvageOpsPlanner(Empire empire)
        {
            Owner = empire;
            TurnTimer = empire.DifficultyModifiers.MiningOpsTurnsPerRun + empire.Id;
        }

        InfluenceStatus Influence(Vector2 pos) => Universe.Influence.GetInfluenceStatus(Owner, pos);

        // ignoreDistance is for tests
        public void RunSalvageOpsPlanner(bool ignoreDistance = false)
        {
            if (!ShouldRun() || ShipBuilder.PickSalvageStation(Owner) == null)
                return;

            SolarSystem[] systems = Owner.DerelictSystems.ToArr();
            systems.Sort(s => s.Position.SqDist(Owner.WeightedCenter));
            float averageDist = Owner.AverageSystemsSqdistFromCenter;
            for (int i = 0; i < systems.Length; ++i)
            {
                SolarSystem system = systems[i];
                if (!Candidate(system, averageDist, ignoreDistance))
                    continue;

                IShipDesign station = ShipBuilder.PickSalvageStation(Owner, system);
                if (!WorthAStation(system, station.GetCost(Owner)))
                    continue;

                if (Owner.KnownEnemyStrengthNoResearchStationsIn(system) > 0f)
                {
                    if (!Owner.isPlayer && !Owner.HasWarTaskTargetingSystem(system) && !Owner.AI.HasGoal(g => g is DefendSystem d && d.TargetSystem == system)) // a fleet another planner asked for this turn has no task yet
                        Owner.AddDefenseSystemGoal(system, Owner.KnownEnemyStrengthIn(system), Tasks.MilitaryTaskImportance.Normal);
                    continue;
                }

                if (SalvageMission.FindSite(system, SalvageMission.SitesSeenBy(Owner, Universe.Objects.GetShips()), Owner.Random, out Vector2 site))
                {
                    Owner.AI.AddGoalAndEvaluate(new SalvageOps(Owner, system, site, station));
                    return;
                }
            }
        }

        bool ShouldRun()
        {
            if (!Universe.P.SalvageOps || !Owner.CanBuildSalvageStations || Owner.isPlayer && !Owner.AutoBuildSalvageStations)
                return false;

            if (--TurnTimer > 0)
                return false;

            TurnTimer = Owner.DifficultyModifiers.MiningOpsTurnsPerRun;
            return true;
        }

        // explored, near enough, outside enemy influence, with no Remnant portal, and with no colony of a rival it is neither at war with nor has open borders or an alliance with
        bool Candidate(SolarSystem system, float averageDist, bool ignoreDistance)
        {
            InfluenceStatus influence = Influence(system.Position);
            if (!SalvageOps.CanOrderIn(Owner, system) || !system.IsExploredBy(Owner) || influence == InfluenceStatus.Enemy
                || !ignoreDistance && !HelperFunctions.InGoodDistanceForReseachOrMiningOps(Owner, system, averageDist, influence))
                return false;

            if (Universe.Remnants?.AI.HasGoal(g => g is RemnantPortal && g.TargetShip?.System == system) == true)
                return false;

            foreach (Empire e in system.OwnerList)
                if (e != Owner && !Owner.IsAtWarWith(e) && !Owner.IsOpenBordersTreaty(e) && !Owner.IsAlliedWith(e))
                    return false;
            return true;
        }

        // the production left in the derelicts it can work there pays for the station, or one's design uses a tech it wants, or it is a ship it would recover
        bool WorthAStation(SolarSystem system, float stationCost)
        {
            float production = 0f;
            int lifetime = Derelict.LifetimeTurns(Universe);
            Derelict[] derelicts = Universe.Derelicts.Current;
            for (int i = 0; i < derelicts.Length; ++i)
            {
                Derelict d = derelicts[i];
                if (!SalvageOps.WorthAStationFor(Owner, d, system, lifetime))
                    continue;

                if (SalvageDerelict.DesignHasWantedTech(Owner, d) || SalvageDerelict.WouldRecover(Owner, d))
                    return true;
                production += d.ProductionLeft;
            }
            return production >= stationCost;
        }
    }
}
