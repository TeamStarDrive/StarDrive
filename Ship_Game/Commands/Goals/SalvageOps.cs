using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.Commands.Goals
{
    // an empire's one salvage station in a system: ordered and built by a constructor, or taken over, then kept while it lasts
    [StarDataType]
    public class SalvageOps : Goal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public Vector2 StaticBuildPos;
        [StarData] IShipDesign StationToBuild;
        [StarData] public float StationCost; // as built, all of it refunded when the station comes apart
        Ship SalvageStation => TargetShip;
        public override IShipDesign ToBuild => StationToBuild;

        public override bool IsSalvageOpsGoal(SolarSystem system) => system != null && TargetSystem == system;

        [StarDataConstructor]
        public SalvageOps(Empire owner) : base(GoalType.SalvageOps, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildStationConstructor,
                WaitForConstructor,
                Salvage
            };
        }

        public SalvageOps(Empire owner, SolarSystem system, Vector2 buildPos, IShipDesign stationToBuild) : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            StaticBuildPos = buildPos;
            StationToBuild = stationToBuild;
        }

        // a station that changes hands skips the build, its notification and its trust cost, and keeps the cost it was built for
        public SalvageOps(Empire owner, Ship station, SolarSystem system, float stationCost) : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            TargetShip = station;
            StationCost = stationCost;
            ChangeToStep(Salvage);
        }

        // only in a system with a derelict left for the empire to salvage, and one per empire per system
        public static bool CanOrderIn(Empire empire, SolarSystem system)
        {
            return system != null && empire.Universe.P.SalvageOps
                && AnyToSalvageIn(empire, system)
                && !empire.AI.HasGoal(g => g?.IsSalvageOpsGoal(system) == true);
        }

        // from any thread: a salvageable derelict the empire knows of in the system that no other empire works from its station there, as an empire's claims end with its station
        public static bool AnyToSalvageIn(Empire empire, SolarSystem system)
        {
            Derelict[] derelicts = empire.Universe.Derelicts.Current;
            for (int i = 0; i < derelicts.Length; ++i)
            {
                Derelict d = derelicts[i];
                Empire claimant = d.Claimant;
                if (d.System == system && d.CanBeSalvaged && d.IsKnownBy(empire)
                    && (claimant == null || claimant == empire || StationIn(claimant, system) == null))
                    return true;
            }
            return false;
        }

        // from any thread: a salvageable derelict in the system that the empire knows of, with enough turns left, and that no other empire works from its station there
        public static bool WorthAStationFor(Empire empire, Derelict d, SolarSystem system, int lifetime)
        {
            Empire claimant = d.Claimant;
            return d.System == system && d.CanBeSalvaged && d.IsKnownBy(empire) && d.TurnsLeft * 100 >= lifetime * SalvageOpsPlanner.MinTurnsLeftPercent
                   && (claimant == null || claimant == empire || StationIn(claimant, system) == null);
        }

        // from any thread: the derelicts in the system a new station of the empire's would work
        public static int DerelictsToWorkIn(Empire empire, SolarSystem system)
        {
            int lifetime = Derelict.LifetimeTurns(empire.Universe);
            int count = 0;
            Derelict[] derelicts = empire.Universe.Derelicts.Current;
            for (int i = 0; i < derelicts.Length; ++i)
                if (WorthAStationFor(empire, derelicts[i], system, lifetime))
                    ++count;
            return count;
        }

        public enum OrderBlock { None, StationThere, NotHere, AllWorked, NoDesign }

        // why the derelict panel's button and the derelicts window cannot order a station in the system, or None when they can
        public static OrderBlock WhyNoOrderIn(Empire empire, SolarSystem system)
        {
            if (system != null && empire.AI.HasGoal(g => g?.IsSalvageOpsGoal(system) == true))
                return OrderBlock.StationThere;
            if (!CanOrderIn(empire, system))
                return system != null && empire.Universe.P.SalvageOps && empire.Universe.Derelicts.AnyKnownIn(empire, system)
                     ? OrderBlock.AllWorked : OrderBlock.NotHere;
            return ShipBuilder.PickSalvageStation(empire) == null ? OrderBlock.NoDesign : OrderBlock.None;
        }

        // the derelict panel's button and the derelicts window's Deploy: the picked station at a free site near the system's derelicts; false when it cannot order or finds no site
        public static bool OrderNearDerelicts(Empire empire, SolarSystem system, Ship[] ships, RandomBase random)
        {
            if (WhyNoOrderIn(empire, system) != OrderBlock.None
                || !SalvageMission.FindSite(system, SalvageMission.SitesSeenBy(empire, ships), random, out Vector2 site))
                return false;

            empire.AI.AddGoalAndEvaluate(new SalvageOps(empire, system, site, ShipBuilder.PickSalvageStation(empire, system)));
            return true;
        }

        // a ship is placed in its system a step after it spawns, so until then its position tells
        static SolarSystem SystemOf(Ship station)
        {
            if (station.System != null)
                return station.System;
            SolarSystem closest = station.Universe.FindClosestSystem(station.Position);
            return closest != null && station.Position.InRadius(closest.Position, closest.Radius) ? closest : null;
        }

        static bool HasStationIn(Empire empire, SolarSystem system)
            => empire.AI.HasGoal(g => g?.IsSalvageOpsGoal(system) == true && g.TargetShip is { Active: true });

        // from any thread: the empire's built salvage station in the system, or null, as one coming apart is no longer at work
        public static Ship StationIn(Empire empire, SolarSystem system)
        {
            Ship station = empire.AI.FindGoal(g => g?.IsSalvageOpsGoal(system) == true)?.TargetShip;
            return station is { Active: true, Dismantling: false } && station.Loyalty == empire ? station : null;
        }

        // as a station changes hands: it works for its new owner, whose order there is dropped; where the new owner has a station already it is evacuated, a boarded one's troops leaving first and a player told
        public static void TakeOver(Ship station, Empire from, Empire to, bool boarded)
        {
            if (station.Universe.Screen is { IsOnSimThread: false } screen) // a save or the UI can apply the change of hands
            {
                screen.RunOnSimThread(() => TakeOver(station, from, to, boarded));
                return;
            }
            // a queued call that the station's fate or its own order has overtaken does nothing
            if (!station.Active || station.Loyalty != to || to.AI.HasGoal(g => g is SalvageOps && g.TargetShip == station))
                return;

            var held = from.AI.FindGoal(g => g is SalvageOps && g.TargetShip == station) as SalvageOps;
            if (held != null)
                from.AI.RemoveGoal(held);
            SolarSystem system = SystemOf(station);
            if (system == null)
                return;

            if (HasStationIn(to, system))
            {
                if (boarded)
                {
                    station.DisengageExcessTroops(station.TroopCount);
                    if (to.isPlayer)
                        to.Universe.Notifications?.AddExcessSalvageStationRemoved(station, system);
                }
                station.QueueTotalRemoval();
                return;
            }

            to.AI.CancelSalvageStation(system);
            to.AI.AddGoal(new SalvageOps(to, station, system, held?.StationCost ?? 0f)); // its first turn waits for the goal pass, once the change of hands has shown its new owner the derelicts there
        }

        // an order ends, with any constructor it has, when salvage ops are off (a save loaded under a mod without salvage content), and before its station is built once its system has no derelict left for it to salvage
        protected override GoalStep? PreEvaluate()
        {
            if (Owner.Universe.P.SalvageOps && (SalvageStation != null || AnyToSalvageIn(Owner, TargetSystem)))
                return null;

            Owner.AI.CancelSalvageStationConstruction(TargetSystem);
            return GoalStep.GoalFailed;
        }

        GoalStep BuildStationConstructor()
        {
            // orders placed together from the UI reach the sim thread together, and the first one holds the system
            if (Owner.AI.FindGoal(g => g.IsSalvageOpsGoal(TargetSystem)) != this)
                return GoalStep.GoalFailed;

            StationToBuild ??= ShipBuilder.PickSalvageStation(Owner, TargetSystem);
            if (StationToBuild == null)
                return GoalStep.GoalFailed;

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, StationToBuild, out Planet planetToBuildAt, portQuality: 1f))
                return GoalStep.TryAgain;

            Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetSystem, StationToBuild.Name, Owner, StaticBuildPos));
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForConstructor()
        {
            if (SalvageStation != null)
            {
                StationCost = SalvageStation.GetCost(Owner);
                if (Owner.isPlayer)
                    Owner.Universe.Notifications?.AddSalvageStationBuiltNotification(SalvageStation, TargetSystem);
                SalvageMission.CostTrustOfColonyOwners(Owner, TargetSystem);
                return GoalStep.GoToNextStep;
            }

            return ConstructionGoalInProgress ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep Salvage()
        {
            if (SalvageStation is not { Active: true })
                return GoalStep.GoalFailed;

            // a change of hands this order missed, such as one a save applied, is taken over now
            if (SalvageStation.Loyalty != Owner)
            {
                TakeOver(SalvageStation, Owner, SalvageStation.Loyalty, boarded: false);
                return GoalStep.GoalComplete;
            }

            // one counting down to its scuttling, coming apart, or about to change hands, calls no freighters and does not end; one pirates hold only keeps its ordnance up
            if (!SalvageStation.Dying && SalvageStation.ScuttleTimer < 0f && !SalvageStation.Dismantling && !SalvageStation.LoyaltyTracker.ChangesFrom(Owner))
            {
                if (Owner.WeArePirates)
                {
                    ReplenishOrdnance(SalvageStation);
                    return GoalStep.TryAgain;
                }
                bool workDone = CanEnd(SalvageStation) && NothingLeftToSalvage();
                // what it holds goes with the station of an empire with nowhere to send it
                if (workDone && (SalvageStation.GetProduction() < 1f || SalvageMission.NowhereToSend(Owner)))
                    return ScrapStation(SalvageStation);
                Haul(SalvageStation, workDone);
            }
            return GoalStep.TryAgain;
        }

        // nothing resupplies a station pirates hold, so one that makes no ordnance of its own regains a tenth of its store a turn out of combat, as a pirate base does
        static void ReplenishOrdnance(Ship station)
        {
            if (station.OrdAddedPerSecond <= 0f && !station.InCombat)
                station.ChangeOrdnance(station.OrdinanceMax / 10f);
        }

        // one not yet placed in its system has yet to show its owner the derelicts there
        static bool CanEnd(Ship station) => station.System != null;

        // no salvage or recovery order of the empire's is left in the system, the platforms of those ended gone too, and no derelict left there for it to salvage
        bool NothingLeftToSalvage()
            => !Owner.AI.HasGoal(g => g is SalvageDerelict s && s.TargetSystem == TargetSystem) && !AnyToSalvageIn(Owner, TargetSystem);

        // it comes apart as a station scrapped does, this order waiting for its end
        GoalStep ScrapStation(Ship station)
        {
            station.AI.OrderScrapShip();
            return GoalStep.TryAgain;
        }

        // the most freighters on their way to load at one station
        public const int MaxHauls = 3;

        // a station calls an idle freighter for each average freighter load it holds beyond the holds of those on their way, and for all of it once it has no room for another platform load or nothing left to salvage
        void Haul(Ship station, bool workDone)
        {
            if (HaulsBlocked(station))
                return;

            int coming = HaulsComing(station, out float holds);
            float uncalled = station.GetProduction() - holds;
            float callAt = workDone || station.CargoRoom < ShipResupply.SalvageLoad ? 1f : Owner.AverageFreighterCargoCap;
            while (coming < MaxHauls && uncalled >= callAt
                   && Owner.TryDispatchHaul(station, out Planet colony, out Ship freighter))
            {
                uncalled -= freighter.CargoRoom;
                freighter.AI.SetupFreighterPlan(station, colony, Goods.Production);
                colony.UpdateIncomingTradeGoods();
                ++coming;
            }
        }

        // armed hostiles in a station's sensor range hold off its hauls
        public static bool HaulsBlocked(Ship station)
        {
            if (!station.AI.BadGuysNear)
                return false;

            return station.AI.PotentialTargets.Any(s => s.Active && !s.Dying && s.TotalDps > 0f);
        }

        // from any thread: the freighters on their way to load at the station, those landing on it included, and the room in their holds
        public static int HaulsComing(Ship station, out float holds)
        {
            holds = 0f;
            int coming = 0;
            var ships = station.Loyalty.OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = ships[i];
                if (ship.IsFreighter && ship.AI.FindGoal(ShipAI.Plan.PickupGoodsFromStation, out ShipAI.ShipGoal g) && g.Trade?.FromStation == station)
                {
                    ++coming;
                    holds += ship.CargoRoom;
                }
            }
            return coming;
        }

        bool ConstructionGoalInProgress
            => Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetSystem) && g.ToBuild?.IsSalvageStation == true);
    }
}
