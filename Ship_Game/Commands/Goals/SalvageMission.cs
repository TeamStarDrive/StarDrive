using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    // an empire's salvage ship at work in one system: an idle one, or one built at its empire's safe shipyard nearest the system, flies to a site among the system's derelicts and waits there while its salvage units launch the constructors of its orders, a free unit taking on more of the system's derelicts; with its hold full, or without the ordnance a launch takes, it goes to its empire's nearest safe colony and comes back; once its orders are done and none is left for it there it brings home what it holds and waits at that colony for its next work
    [StarDataType]
    public class SalvageMission : Goal
    {
        // its move to its site over, it parks there once this close
        public const float ArrivalRadius = 300f;
        // a site keeps this far from every salvageable derelict and every other salvage station or ship
        public const float Clearance = 2000f;
        public const float FirstSearchRadius = 1000f;
        public const float SearchRadiusStep = 500f;

        [StarData] public Ship SalvageShip { get; private set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public SolarSystem TargetSystem { get; private set; }
        [StarData] public Vector2 Site { get; private set; }
        // the design built for it, or its idle ship's, whose units its orders hold until the ship is built
        [StarData] public IShipDesign Design { get; private set; }
        // the colony it unloads at, where it waits once its orders are done
        [StarData] public Planet Colony { get; private set; }
        // its ship was destroyed or taken, which its orders tell the player
        [StarData] public bool ShipLost { get; private set; }
        // it has ended, so its orders end too
        [StarData] public bool Over { get; private set; }
        // its ship's icon, kept for the notifications once the ship is gone
        [StarData] public string ShipIcon { get; private set; }

        [StarDataConstructor]
        public SalvageMission(Empire owner) : base(GoalType.SalvageMission, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildShip,
                WaitForShip,
                FlyToSite,
                Work,
                Unload
            };
        }

        SalvageMission(Empire owner, SolarSystem system, Vector2 site, IShipDesign design) : this(owner)
        {
            TargetSystem = system;
            Site = site;
            Design = design;
            ShipIcon = design.IconPath;
        }

        // an idle ship flies out at once
        SalvageMission(Empire owner, SolarSystem system, Vector2 site, Ship ship) : this(owner, system, site, ship.ShipData)
        {
            SalvageShip = ship;
            ChangeToStep(FlyToSite);
        }

        // on the sim thread, once for each order: its system's mission with a free unit takes it, else a mission left with no order while its ship is still to be built moves to the system and takes it, else the empire's idle salvage ship nearest the system starts one, else one is built for it; null with no salvage ship to send or build, or no site for it
        public static SalvageMission Join(Empire empire, SolarSystem system)
        {
            if (WithFreeUnitIn(empire, system) is { } joined)
                return joined;

            SalvageMission unordered = Unordered(empire);
            Ship ship = unordered == null ? NearestIdleShip(empire, system.Position) : null;
            IShipDesign design = unordered == null && ship == null ? ShipBuilder.PickSalvageShip(empire, system) : null;
            if (unordered == null && ship == null && design == null
                || !FindSite(system, SitesSeenBy(empire, empire.Universe.Objects.GetShips()), empire.Random, out Vector2 site))
                return null;

            if (unordered != null)
            {
                unordered.TargetSystem = system;
                unordered.Site = site;
                return unordered;
            }
            SalvageMission mission = ship != null ? new(empire, system, site, ship) : new(empire, system, site, design);
            empire.AI.AddGoal(mission);
            return mission;
        }

        // from the sim thread: an order in the system would join a mission or an idle ship, so none is built for it
        public static bool HasShipFor(Empire empire, SolarSystem system)
            => WithFreeUnitIn(empire, system) != null || Unordered(empire) != null || NearestIdleShip(empire, system.Position) != null;

        static SalvageMission WithFreeUnitIn(Empire empire, SolarSystem system)
        {
            var goals = empire.AI.Goals;
            for (int i = 0; i < goals.Count; ++i)
                if (goals[i] is SalvageMission { Over: false } m && m.TargetSystem == system && m.FreeUnits > 0)
                    return m;
            return null;
        }

        // from the sim thread: one on automatic stays queued with its orders gone, for the next order
        public static SalvageMission Unordered(Empire empire)
        {
            var goals = empire.AI.Goals;
            for (int i = 0; i < goals.Count; ++i)
                if (goals[i] is SalvageMission { Over: false, Building: true } m && !m.HasOrders)
                    return m;
            return null;
        }

        // from any thread: an AI empire, or the player with Auto Salvage on, whose planner orders its salvage
        public static bool OnAutomatic(Empire empire)
            => empire.Universe.P.SalvageOps && (empire.isPlayer ? empire.AutoSalvage : !empire.IsFaction);

        // from any thread: the mission the salvage ship works for, or null; one the queue reported built is its mission's before the mission's turn takes it
        public static SalvageMission OfShip(Ship ship)
            => ship.Loyalty.AI.FindGoal(g => g is SalvageMission s && (s.SalvageShip == ship || s.BuiltForIt(ship))) as SalvageMission;

        // read only once the ship is built, as reading the built ship clears a lost one
        bool BuiltForIt(Ship ship) => SalvageShip == null && !Building && FinishedShip == ship;

        // from any thread: its ship's live salvage units, or before the ship is built its design's, less those its orders hold
        public int FreeUnits => (SalvageShip is { } ship ? LiveUnits(ship) : Design?.NumSalvageModules ?? 0) - NumOrders;

        int NumOrders => Owner.AI.CountGoals(g => g is SalvageDerelict s && s.Mission == this);

        // from any thread: an order of its empire's holds one of its units
        bool HasOrders => Owner.AI.HasGoal(g => g is SalvageDerelict s && s.Mission == this);

        // an order of its waits to launch its constructor
        bool HasOrderToLaunch => Owner.AI.HasGoal(g => g is SalvageDerelict { Launching: true } s && s.Mission == this);

        public static int LiveUnits(Ship ship)
        {
            int units = 0;
            ShipModule[] modules = ship.Modules;
            for (int i = 0; i < modules.Length; ++i)
                if (modules[i].Salvage && modules[i].Active)
                    ++units;
            return units;
        }

        // from any thread: its ship stopped at its site, where its units launch and its platforms' loads reach it
        public bool Parked(out Ship ship)
        {
            ship = StepName == nameof(Work) ? OwnShip() : null;
            return ship != null;
        }

        // from any thread: the steps the derelicts window and the ship's status read
        public bool Building => Step <= 1;
        public bool FlyingOut => StepName == nameof(FlyToSite);
        public bool Unloading => StepName == nameof(Unload);

        // its ship while it is its empire's; one destroyed, boarded or changing hands is not
        Ship OwnShip() => SalvageShip is { Active: true, Dying: false } ship && ship.Loyalty == Owner && !ship.LoyaltyTracker.ChangesFrom(Owner) ? ship : null;

        // boarded, it is another's at its next update
        bool ShipChangesHands => SalvageShip is { Active: true } ship && ship.Loyalty == Owner && ship.LoyaltyTracker.ChangesFrom(Owner);

        // a salvage ship of the empire's with a live unit, in no mission or fleet, orbiting, holding or awaiting orders, and not landing, coming apart, counting down to its scuttling or about to change hands
        public static bool IsIdle(Ship ship, Empire empire)
            => ship is { Active: true, Dying: false, IsSalvageShip: true, Dismantling: false, Fleet: null, IsLanding: false, IsHangarShip: false }
               && ship.Loyalty == empire && ship.ScuttleTimer < 0f && !ship.LoyaltyTracker.ChangesFrom(empire)
               && ship.AI.State is AIState.Orbit or AIState.HoldPosition or AIState.AwaitingOrders
               && LiveUnits(ship) > 0 && OfShip(ship) == null;

        static Ship NearestIdleShip(Empire empire, Vector2 to)
        {
            Ship nearest = null;
            var ships = empire.OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = ships[i];
                if (IsIdle(ship, empire) && (nearest == null || ship.Position.SqDist(to) < nearest.Position.SqDist(to)))
                    nearest = ship;
            }
            return nearest;
        }

        // from any thread: the empire's salvage ships at work for a mission, and those idle
        public static void CountShips(Empire empire, out int working, out int idle)
        {
            working = idle = 0;
            var ships = empire.OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = ships[i];
                if (!ship.IsSalvageShip || !ship.Active)
                    continue;
                if (ship.WorksForSalvage)
                    ++working;
                else if (IsIdle(ship, empire))
                    ++idle;
            }
        }

        // a mission that ends takes its ship out of the queue, and lets go of one still its empire's; its orders see it over and end
        public override void OnRemoved()
        {
            Over = true;
            if (SalvageShip == null && FinishedShip is { IsSalvageShip: true } built) // built since its last turn
                SalvageShip = built;
            if (PlanetBuildingAt is { } shipyard)
                Unqueue(shipyard);
            if (OwnShip() is { } ship)
                LetGo(ship);
        }

        // the design is queued as urgent at the empire's safe shipyard planet nearest the site, first in the player's queue; with none, it waits for one; with its orders gone it ends, unless on automatic
        GoalStep BuildShip()
        {
            if (!HasOrders && !OnAutomatic(Owner))
                return GoalStep.GoalFailed;
            if (NearestShipyard() is not { } shipyard)
                return GoalStep.TryAgain;

            PlanetBuildingAt = shipyard;
            shipyard.Construction.Enqueue(Design, QueueItemType.OrbitalUrgent, this, notifyOnEmpty: false);
            if (Owner.isPlayer)
                shipyard.Construction.MoveToFront(this);
            return GoalStep.GoToNextStep;
        }

        Planet NearestShipyard() => Owner.SafeSpacePorts.FindMin(p => p.Position.SqDist(Site));

        // the queue reports the ship built; with its orders gone it leaves the queue, unless on automatic, where it waits for the next order and, built with none, is a ship like any other; its shipyard lost, or unsafe while a safe one is left, it is queued again at the nearest; taken out of the queue, the mission is called off
        GoalStep WaitForShip()
        {
            if (!HasOrders && !OnAutomatic(Owner))
                return GoalStep.GoalFailed;
            Planet shipyard = PlanetBuildingAt;
            if (shipyard == null || shipyard.Owner != Owner)
                return BuildAgain();
            if (QueuedItem(shipyard) == null)
                return GoalStep.GoalFailed;
            if (!shipyard.Safe && NearestShipyard() is { } safe && safe != shipyard)
            {
                Unqueue(shipyard);
                return BuildAgain();
            }
            return GoalStep.TryAgain;
        }

        QueueItem QueuedItem(Planet shipyard)
        {
            var queue = shipyard.ConstructionQueue;
            for (int i = 0; i < queue.Count; ++i)
                if (queue[i].Goal == this)
                    return queue[i];
            return null;
        }

        // out of the queue at once, as a cancel flag left for the queue's next turn is not saved
        void Unqueue(Planet shipyard)
        {
            if (QueuedItem(shipyard) is { } item)
                shipyard.Construction.Cancel(item);
        }

        GoalStep BuildAgain()
        {
            PlanetBuildingAt = null;
            return GoalStep.RestartGoal;
        }

        // it flies to its site while it has orders or work left there and parks once its move there has stopped it, taking no orders and no fights; one refitted in the queue to another design ends the mission and is a ship like any other
        GoalStep FlyToSite()
        {
            if (SalvageShip == null && FinishedShip is { } built)
            {
                PlanetBuildingAt = null;
                if (!built.IsSalvageShip)
                    return GoalStep.GoalFailed;
                SalvageShip = built;
            }
            if (ShipChangesHands)
                return GoalStep.TryAgain;
            if (OwnShip() is not { } ship)
                return Lose();
            if (!HasOrders && !WorkLeft(ship))
                return End(ship);
            if (ship.AI.State == AIState.MoveTo)
            {
                ship.AI.IgnoreCombat = true; // the end of its launch from the shipyard, or a route redrawn for a newly explored system, clears it
                return GoalStep.TryAgain;
            }
            if (ship.Position.InRadius(Site, ArrivalRadius))
            {
                Park(ship);
                return GoalStep.GoToNextStep;
            }
            ship.AI.OrderMoveTo(Site, ship.Position.DirectionToTarget(Site), AIState.MoveTo, MoveOrder.Regular);
            ship.AI.IgnoreCombat = true;
            return GoalStep.TryAgain;
        }

        // it holds where it stopped, taking no fights, and nothing else orders it off
        static void Park(Ship ship)
        {
            ship.AI.ClearOrdersAndWayPoints(AIState.HoldPosition, priority: true);
            ship.AI.OrderHoldPosition(ship.Position, ship.Direction);
            ship.AI.IgnoreCombat = true;
        }

        // it waits at its site while its orders work, a free unit taking on more of the system's derelicts; with its hold full, or without the ordnance a waiting launch takes that its full load pays for, it leaves for its empire's nearest safe colony once no constructor of its is out; with its orders all done and none left to take it ends
        GoalStep Work()
        {
            if (ShipChangesHands)
                return GoalStep.TryAgain;
            if (OwnShip() is not { } ship)
                return Lose();
            TakeMoreWork();
            if (!HasOrders)
                return End(ship);
            if (ship.AI.State != AIState.HoldPosition)
                Park(ship);
            ship.AI.IgnoreCombat = true; // pushed off its spot, its hold flies it back, which drops these
            ship.AI.SetPriorityOrder(true);
            if (!ConstructorsOut(ship) && (SalvageDerelict.HoldFull(ship) || HasOrderToLaunch && ShortOfLaunch(ship)))
                return Trip(ship);
            return GoalStep.TryAgain;
        }

        // for each free unit, an order in its system that waits for a salvage ship, else an order of its own on a derelict there it would order, recovering on automatic what the planner would
        void TakeMoreWork()
        {
            int free = FreeUnits;
            var goals = Owner.AI.Goals;
            for (int i = 0; i < goals.Count && free > 0; ++i)
                if (goals[i] is SalvageDerelict order && order.TargetSystem == TargetSystem && order.JoinWaiting(this))
                    --free;
            if (free <= 0)
                return;

            bool automatic = OnAutomatic(Owner);
            Derelict[] derelicts = ToOrder();
            for (int i = 0; i < derelicts.Length && free > 0; ++i)
                if (SalvageDerelict.Order(Owner, derelicts[i], recover: automatic && SalvageDerelict.WouldRecover(Owner, derelicts[i]), by: this))
                    --free;
        }

        // the derelicts in its system it would order itself, as the planner picks them, but none its empire called off, and on automatic only where the planner would work
        Derelict[] ToOrder()
            => OnAutomatic(Owner) && !SalvageOpsPlanner.WouldWork(Owner, TargetSystem) ? Empty<Derelict>.Array
             : SalvageOpsPlanner.ToOrderIn(Owner, TargetSystem).Filter(d => !d.CalledOffBy(Owner));

        // its ship has a live unit and a derelict is left in its system that it would order
        bool WorkLeft(Ship ship) => LiveUnits(ship) > 0 && ToOrder().Length > 0;

        static bool ConstructorsOut(Ship ship)
        {
            ShipModule[] bays = ship.Carrier.AllHangars;
            for (int i = 0; i < bays.Length; ++i)
                if (bays[i].IsSalvageBay && bays[i].TryGetHangarShipActive(out Ship launched) && launched.Loyalty == ship.Loyalty)
                    return true;
            return false;
        }

        // short of the ordnance its unit spends to launch its empire's constructor, which its full load pays for
        bool ShortOfLaunch(Ship ship)
        {
            float cost = ResourceManager.GetShipTemplate(SalvageDerelict.ConstructorName(Owner), out Ship constructor) ? constructor.ShipOrdLaunchCost : 0f;
            return ship.Ordinance < cost && cost <= ship.OrdinanceMax;
        }

        // to its empire's safe colony nearest it; with none, what it holds is paid as credits at the tax rate, and it stays
        GoalStep Trip(Ship ship)
        {
            if (NearestSafeColony(ship.Position) is not { } colony)
            {
                PayAsCredits(ship);
                return GoalStep.TryAgain;
            }
            Colony = colony;
            Orbit(ship, colony);
            ChangeToStep(Unload);
            return GoalStep.TryAgain;
        }

        Planet NearestSafeColony(Vector2 from) => Owner.SafePlanets.FindMin(p => p.Position.SqDist(from));

        static void Orbit(Ship ship, Planet colony)
        {
            ship.AI.OrderOrbitPlanet(colony, clearOrders: true);
            ship.AI.IgnoreCombat = true;
            ship.AI.SetPriorityOrder(true);
        }

        void PayAsCredits(Ship ship)
        {
            float load = ship.GetProduction();
            ship.UnloadProduction(load);
            Owner.AddExcessCargoMoney(load * Owner.data.TaxRate);
        }

        // once in orbit at the colony it unloads, what the store has no room for paid as credits at the tax rate, and while an order waits to launch it rearms in orbit; then it flies back to its site, or with its orders all done it stays; a colony lost or turned unsafe on the way is swapped for the nearest safe one
        GoalStep Unload()
        {
            if (ShipChangesHands)
                return GoalStep.TryAgain;
            if (OwnShip() is not { } ship)
                return Lose();

            if (Colony?.Owner != Owner || !Colony.Safe)
            {
                if (NearestSafeColony(ship.Position) is not { } colony)
                {
                    PayAsCredits(ship);
                    return Return(ship);
                }
                Colony = colony;
                Orbit(ship, colony);
                return GoalStep.TryAgain;
            }
            if (!ship.IsOrbiting(Colony))
            {
                if (ship.AI.State != AIState.Orbit || ship.AI.OrbitTarget != Colony)
                    Orbit(ship, Colony);
                return GoalStep.TryAgain;
            }

            float load = ship.GetProduction();
            if (load > 0f)
            {
                ship.UnloadProduction(load);
                DismantleOrbital.Deposit(Owner, Colony, load);
                ship.SendRefundShuttlesDown(Colony, load);
            }
            if (ship.Ordinance < ship.OrdinanceMax && HasOrderToLaunch)
                return GoalStep.TryAgain;
            return Return(ship);
        }

        // back to its site while it has orders or work left there, else a ship like any other
        GoalStep Return(Ship ship)
        {
            if (!HasOrders && !WorkLeft(ship))
                return Release(ship);
            ChangeToStep(FlyToSite);
            return GoalStep.TryAgain;
        }

        // its orders all done, it waits for its constructors, brings home what it holds, and then is a ship like any other
        GoalStep End(Ship ship)
        {
            if (ConstructorsOut(ship))
                return GoalStep.TryAgain;
            return ship.GetProduction() > 0f ? Trip(ship) : Release(ship);
        }

        GoalStep Release(Ship ship)
        {
            LetGo(ship);
            return GoalStep.GoalComplete;
        }

        // in orbit at the colony it last unloaded at or, never unloaded, its empire's nearest safe colony; the ship of an empire that leaves just stops
        void LetGo(Ship ship)
        {
            SalvageShip = null;
            FinishedShip = null; // so its end finds no ship to let go of again
            ship.AI.ClearOrders();
            if (Owner.IsDefeated)
                return;
            Planet home = Colony?.Owner == Owner ? Colony : NearestSafeColony(ship.Position);
            if (home != null)
                ship.AI.OrderOrbitPlanet(home, clearOrders: true);
        }

        // destroyed or in other hands, it ends its orders, each telling the player, who is told too of the production it had aboard
        GoalStep Lose()
        {
            ShipLost = true;
            float aboard = SalvageShip?.GetProduction() ?? 0f;
            if (aboard > 0f && Owner.isPlayer)
                UState.Notifications?.AddSalvageShipLostLoadNotification(aboard, ShipIcon);
            return GoalStep.GoalFailed;
        }

        // from any thread: the salvageable derelicts the empire knows of in the system that no other empire holds
        public static int DerelictsToWorkIn(Empire empire, SolarSystem system)
        {
            int count = 0;
            Derelict[] derelicts = empire.Universe.Derelicts.Current;
            for (int i = 0; i < derelicts.Length; ++i)
            {
                Derelict d = derelicts[i];
                Empire claimant = d.Claimant;
                if (d.System == system && d.CanBeSalvaged && d.IsKnownBy(empire) && (claimant == null || claimant == empire))
                    ++count;
            }
            return count;
        }

        // from any thread: inside the system, clear of its star, radiation and planets' orbits, and of its derelicts and the salvage stations and ships among `ships`
        public static bool IsValidSite(SolarSystem system, Vector2 pos, Ship[] ships)
        {
            if (pos.OutsideRadius(system.Position, system.Radius * DeepSpaceBuildingWindow.SystemPlacementRadius)
                || pos.InRadius(system.Position, DeepSpaceBuildingWindow.MinimumBuildDistanceFromSun)
                || !system.InSafeDistanceFromRadiation(pos))
                return false;

            for (int i = 0; i < system.PlanetList.Count; ++i)
                if (pos.InRadius(system.PlanetList[i].Position, DeepSpaceBuildingWindow.TetherRadius(system.PlanetList[i])))
                    return false;

            Derelict[] derelicts = system.Universe.Derelicts.Current;
            for (int i = 0; i < derelicts.Length; ++i)
                if (derelicts[i].System == system && derelicts[i].CanBeSalvaged && pos.InRadius(derelicts[i].RestPosition, Clearance))
                    return false;

            for (int i = 0; i < ships.Length; ++i)
                if (IsSiteHolder(ships[i]) && ships[i].Active && !ships[i].Dying && pos.InRadius(ships[i].Position, Clearance))
                    return false;

            return true;
        }

        static bool IsSiteHolder(Ship ship) => ship.IsSalvageStation || ship.IsSalvageShip;

        // the salvage stations and ships an empire sees, its own among them, which a new site keeps clear of; the player's sight is read the UI-safe way
        public static Ship[] SitesSeenBy(Empire empire, Ship[] ships)
        {
            return ships.Filter(s => IsSiteHolder(s) && s.Active
                && (s.Loyalty == empire || (empire.isPlayer ? s.KnownByEmpires.KnownByPlayer(empire.Universe)
                                                            : s.KnownByEmpires.KnownBy(empire))));
        }

        // a random point within 1000 of the derelicts' average spot, then within 1500, 2000 and on, until one is a valid site or the system's placement area is covered
        public static bool FindSite(SolarSystem system, Ship[] ships, RandomBase random, out Vector2 site)
        {
            Derelict[] derelicts = system.Universe.Derelicts.Current;
            Vector2 sum = Vector2.Zero;
            int count = 0;
            for (int i = 0; i < derelicts.Length; ++i)
            {
                if (derelicts[i].System == system && derelicts[i].CanBeSalvaged)
                {
                    sum += derelicts[i].RestPosition;
                    ++count;
                }
            }

            if (count > 0)
            {
                Vector2 center = sum / count;
                float lastRadius = center.Distance(system.Position) + system.Radius * DeepSpaceBuildingWindow.SystemPlacementRadius;
                for (float radius = FirstSearchRadius; radius <= lastRadius; radius += SearchRadiusStep)
                {
                    site = center + random.RandomPointInRing(0f, radius);
                    if (IsValidSite(system, site, ships))
                        return true;
                }
            }

            site = Vector2.Zero;
            return false;
        }

        // salvaging in a system costs the salvager trust with each AI empire that has a colony there and knows it, unless they have open borders, an alliance or a war
        public static void CostTrustOfColonyOwners(Empire salvager, SolarSystem system)
        {
            if (salvager.isPlayer && GlobalStats.RestrictAIPlayerInteraction)
                return;

            foreach (Empire owner in system.OwnerList)
            {
                if (owner == salvager || owner.isPlayer || owner.IsFaction || owner.data.DiplomaticPersonality == null
                    || !owner.IsKnown(salvager) || owner.IsOpenBordersTreaty(salvager) || owner.IsAlliedWith(salvager)
                    || owner.IsAtWarWith(salvager))
                    continue;

                float loss = owner.data.DiplomaticPersonality.SalvageStationTrustLoss;
                if (salvager.isPlayer)
                    loss *= owner.DifficultyModifiers.SalvageStationTrustLossVsPlayer;
                owner.GetRelations(salvager).Trust -= loss;
            }
        }
    }
}
