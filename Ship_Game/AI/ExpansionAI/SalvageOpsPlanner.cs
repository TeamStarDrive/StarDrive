using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.AI.ExpansionAI
{
    // every few turns, as the mining planner, for an AI empire or the player with Auto Salvage on: salvage orders on the derelicts in the systems worth working, the closest to the empire first, for as many as its salvage ships take, with at most one salvage ship built a run and none beyond its cap; then beyond the cap its queued salvage ships with no order leave the queue, and an AI empire scraps its idle ones
    [StarDataType]
    public class SalvageOpsPlanner
    {
        // a derelict with less than this share of its turns left is not ordered, as it may sink before a platform is up
        public const int MinTurnsLeftPercent = 20;
        // an empire's salvage ships, built or queued, number no more than one for every this many planets, at least one and at most MaxShips
        public const int PlanetsPerShip = 5;
        public const int MaxShips = 10;

        [StarData] readonly Empire Owner;
        [StarData] internal int TurnTimer;

        [StarDataConstructor] SalvageOpsPlanner() { }

        public SalvageOpsPlanner(Empire empire)
        {
            Owner = empire;
            TurnTimer = empire.DifficultyModifiers.MiningOpsTurnsPerRun + empire.Id;
        }

        // ignoreDistance is for tests
        public void RunSalvageOpsPlanner(bool ignoreDistance = false)
        {
            if (!ShouldRun())
                return;

            SolarSystem[] systems = Owner.DerelictSystems.ToArr();
            systems.Sort(s => s.Position.SqDist(Owner.WeightedCenter));
            float averageDist = Owner.AverageSystemsSqdistFromCenter;
            bool built = false;
            for (int i = 0; i < systems.Length; ++i)
            {
                SolarSystem system = systems[i];
                if (!Candidate(system, averageDist, ignoreDistance))
                    continue;

                Derelict[] derelicts = ToOrderIn(Owner, system);
                if (derelicts.Length == 0)
                    continue;

                if (Owner.KnownEnemyStrengthNoResearchStationsIn(system) > 0f)
                {
                    if (!Owner.isPlayer && CanSendShipTo(system, built) && !Owner.HasWarTaskTargetingSystem(system)
                        && !Owner.AI.HasGoal(g => g is DefendSystem d && d.TargetSystem == system)) // a fleet another planner asked for this turn has no task yet
                        Owner.AddDefenseSystemGoal(system, Owner.KnownEnemyStrengthIn(system), Tasks.MilitaryTaskImportance.Normal);
                    continue;
                }

                for (int j = 0; j < derelicts.Length && CanSendShipTo(system, built); ++j)
                {
                    bool builds = !SalvageMission.HasShipFor(Owner, system);
                    if (!Order(derelicts[j]))
                        break;
                    built |= builds;
                }
            }

            DropShipsBeyondCap();
        }

        bool ShouldRun()
        {
            if (!SalvageMission.OnAutomatic(Owner) || Owner.NumPlanets == 0
                || !Owner.CanBuildSalvageShips && !SalvageDerelict.HasSalvageShip(Owner))
                return false;

            if (--TurnTimer > 0)
                return false;

            TurnTimer = Owner.DifficultyModifiers.MiningOpsTurnsPerRun;
            return true;
        }

        // safe to work and near enough
        bool Candidate(SolarSystem system, float averageDist, bool ignoreDistance)
            => SafeToWork(Owner, system, out InfluenceStatus influence)
               && (ignoreDistance || HelperFunctions.InGoodDistanceForReseachOrMiningOps(Owner, system, averageDist, influence));

        // explored, outside enemy influence, with no Remnant portal, and with no colony of a rival it is neither at war with nor has open borders or an alliance with
        static bool SafeToWork(Empire empire, SolarSystem system, out InfluenceStatus influence)
        {
            influence = empire.Universe.Influence.GetInfluenceStatus(empire, system.Position);
            if (!system.IsExploredBy(empire) || influence == InfluenceStatus.Enemy)
                return false;

            if (empire.Universe.Remnants?.AI.HasGoal(g => g is RemnantPortal && g.TargetShip?.System == system) == true)
                return false;

            foreach (Empire e in system.OwnerList)
                if (e != empire && !empire.IsAtWarWith(e) && !empire.IsOpenBordersTreaty(e) && !empire.IsAlliedWith(e))
                    return false;
            return true;
        }

        // on the sim thread: where the planner would order salvage wherever the system lies, safe to work with no hostile strength the empire knows of
        public static bool WouldWork(Empire empire, SolarSystem system)
            => SafeToWork(empire, system, out _) && empire.KnownEnemyStrengthNoResearchStationsIn(system) <= 0f;

        // the derelicts there the empire can order salvaged with a fifth of their turns left, the fewest turns left first, as a claim stops a derelict's clock
        public static Derelict[] ToOrderIn(Empire empire, SolarSystem system)
        {
            int lifetime = Derelict.LifetimeTurns(empire.Universe);
            Derelict[] derelicts = empire.Universe.Derelicts.Current.Filter(d => d.System == system && d.TurnsLeft * 100 >= lifetime * MinTurnsLeftPercent
                                                                              && SalvageDerelict.WhyNoOrder(empire, d) == SalvageDerelict.Block.None);
            derelicts.Sort(d => d.TurnsLeft);
            return derelicts;
        }

        // the ships it would recover, salvage for the rest; an order no salvage ship takes, as with no free site in its system, is dropped for a later run
        bool Order(Derelict d)
        {
            if (!SalvageDerelict.Order(Owner, d, recover: SalvageDerelict.WouldRecover(Owner, d)))
                return false;
            if (SalvageDerelict.OrderOf(Owner, d) is { Mission: null } waiting)
            {
                Owner.AI.RemoveGoal(waiting);
                return false;
            }
            return true;
        }

        // a ship of its would take an order there, or it can still build one this run
        bool CanSendShipTo(SolarSystem system, bool built)
            => SalvageMission.HasShipFor(Owner, system) || !built && Owner.CanBuildSalvageShips && Ships(Owner) < Cap(Owner);

        public static int Cap(Empire empire) => (int)Math.Ceiling(empire.NumPlanets / (float)PlanetsPerShip).Clamped(1, MaxShips);

        // from any thread: its salvage ships, with those queued, less those being scrapped
        public static int Ships(Empire empire)
        {
            int ships = 0;
            var owned = empire.OwnedShips;
            for (int i = 0; i < owned.Count; ++i)
                if (owned[i] is { IsSalvageShip: true, Active: true, Dying: false, DoingScrap: false })
                    ++ships;
            return ships + empire.AI.CountGoals(g => g is SalvageMission { Over: false, Building: true });
        }

        // those beyond the cap once the run has sent what it could, as after the player's own orders or a ship taken over from another empire: queued ones with no order leave the queue, then an AI empire scraps those left idle, the player's being its own to scrap; none while an order of the empire's waits for a ship, as one whose ship was lost does until it takes another
        void DropShipsBeyondCap()
        {
            if (Owner.AI.HasGoal(g => g is SalvageDerelict { Mission: null or { Over: true, ShipLost: true } }))
                return;
            int beyond = Ships(Owner) - Cap(Owner);
            for (; beyond > 0 && SalvageMission.Unordered(Owner) is { } queued; --beyond)
                Owner.AI.RemoveGoal(queued);
            if (Owner.isPlayer)
                return;
            var owned = Owner.OwnedShips;
            for (int i = 0; i < owned.Count && beyond > 0; ++i)
            {
                if (SalvageMission.IsIdle(owned[i], Owner))
                {
                    owned[i].AI.OrderScrapShip();
                    --beyond;
                }
            }
        }
    }
}
