using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using System;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI : IDisposable
    {
        private Vector2 AINewDir;
        private Goal ColonizeGoal;
        private Planet AwaitClosest;
        private Planet PatrolTarget;
        private Vector2 OrbitPos;
        private float FindNewPosTimer;
        private float DistanceLast;
        private float UtilityModuleCheckTimer;
        private SolarSystem SystemToPatrol;
        private readonly Array<Planet> PatrolRoute = new Array<Planet>();
        private int StopNumber;
        public FleetDataNode FleetNode { get;  set; }

        public static UniverseScreen UniverseScreen;
        public Ship Owner;
        public AIState State;// = AIState.AwaitingOrders;
        public Guid OrbitTargetGuid;
        public Planet ColonizeTarget;
        public Planet ResupplyTarget;
        public Guid SystemToDefendGuid;
        public SolarSystem SystemToDefend;
        public SolarSystem ExplorationTarget;
        public AIState DefaultAIState                         = AIState.AwaitingOrders;
        public SafeQueue<ShipGoal> OrderQueue                 = new SafeQueue<ShipGoal>();
        public Array<ShipWeight> NearByShips = new Array<ShipWeight>();
        public BatchRemovalCollection<Ship> FriendliesNearby  = new BatchRemovalCollection<Ship>();


        public ShipAI(Ship owner)
        {
            Owner = owner;
            State = AIState.AwaitingOrders;
            WayPoints = new WayPoints(Owner);
        }

        private void Colonize(Planet TargetPlanet)
        {
            if (Owner.Center.OutsideRadius(TargetPlanet.Center, 2000f))
            {
                OrderQueue.RemoveFirst();
                OrderColonization(TargetPlanet);
                State = AIState.Colonize;
                return;
            }
            if (TargetPlanet.Owner != null || !TargetPlanet.Habitable)
            {
                ColonizeGoal?.NotifyMainGoalCompleted();
                State = AIState.AwaitingOrders;
                OrderQueue.Clear();
                return;
            }
            ColonizeTarget = TargetPlanet;
            ColonizeTarget.Owner = Owner.loyalty;
            ColonizeTarget.ParentSystem.OwnerList.Add(Owner.loyalty);
            if (Owner.loyalty.isPlayer)
            {
                if (!Owner.loyalty.AutoColonize)
                {
                    ColonizeTarget.colonyType = Planet.ColonyType.Colony;
                }
                else ColonizeTarget.colonyType = Owner.loyalty.AssessColonyNeeds(ColonizeTarget);
                Empire.Universe.NotificationManager.AddColonizedNotification(ColonizeTarget, EmpireManager.Player);
            }
            else ColonizeTarget.colonyType = Owner.loyalty.AssessColonyNeeds(ColonizeTarget);
            Owner.loyalty.AddPlanet(ColonizeTarget);
            ColonizeTarget.InitializeSliders(Owner.loyalty);
            ColonizeTarget.SetExploredBy(Owner.loyalty);

            Owner.CreateColonizationBuildingFor(ColonizeTarget);

            ColonizeTarget.TerraformPoints += Owner.loyalty.data.EmpireFertilityBonus;
            ColonizeTarget.CrippledTurns = 0;
            StatTracker.StatAddColony(ColonizeTarget, Owner.loyalty, UniverseScreen);

            foreach (Goal g in Owner.loyalty.GetEmpireAI().Goals)
            {
                if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != ColonizeTarget)
                    continue;
                Owner.loyalty.GetEmpireAI().Goals.QueuePendingRemoval(g);
                break;
            }
            Owner.loyalty.GetEmpireAI().Goals.ApplyPendingRemovals();
            if (ColonizeTarget.ParentSystem.OwnerList.Count > 1)
                foreach (Planet p in ColonizeTarget.ParentSystem.PlanetList)
                {
                    if (p.Owner == ColonizeTarget.Owner || p.Owner == null)
                        continue;
                    if (p.Owner.TryGetRelations(Owner.loyalty, out Relationship rel) && !rel.Treaty_OpenBorders)
                        p.Owner.DamageRelationship(Owner.loyalty, "Colonized Owned System", 20f, p);
                }

            Owner.UnloadColonizationResourcesAt(ColonizeTarget);

            var troopsRemoved = false;
            var playerTroopsRemoved = false;

            var toLaunch = new Array<Troop>();
            foreach (Troop t in TargetPlanet.TroopsHere)
            {
                Empire owner = t?.GetOwner();
                if (owner != null && !owner.isFaction && owner.data.DefaultTroopShip != null && owner != ColonizeTarget.Owner &&
                    ColonizeTarget.Owner.TryGetRelations(owner, out Relationship rel) && !rel.AtWar)
                    toLaunch.Add(t);
            }
            foreach (Troop t in toLaunch)
            {
                t.Launch();
                troopsRemoved = true;
                if (t.GetOwner().isPlayer)
                    playerTroopsRemoved = true;
            }
            toLaunch.Clear();
            if (troopsRemoved)
                if (playerTroopsRemoved)
                    UniverseScreen.NotificationManager.AddTroopsRemovedNotification(ColonizeTarget);
                else if (ColonizeTarget.Owner.isPlayer)
                    UniverseScreen.NotificationManager.AddForeignTroopsRemovedNotification(ColonizeTarget);
            Owner.QueueTotalRemoval();
        }

        private bool ExploreEmptySystem(float elapsedTime, SolarSystem system)
        {
            if (system.IsExploredBy(Owner.loyalty))
                return true;
            MovePosition = system.Position;
            if (Owner.Center.InRadius(MovePosition, 75000f))
            {
                system.SetExploredBy(Owner.loyalty);
                return true;
            }
            ThrustTowardsPosition(MovePosition, elapsedTime, Owner.Speed);
            return false;
        }

        //go colonize
        public void GoColonize(Planet p)
        {
            State = AIState.Colonize;
            ColonizeTarget = p;
            GotoStep = 0;
        }
        //go colonize
        public void GoColonize(Planet p, Goal g)
        {
            State = AIState.Colonize;
            ColonizeTarget = p;
            ColonizeGoal = g;
            GotoStep = 0;
            OrderColonization(p);
        }
        //go rebase
        public void GoRebase(Planet p)
        {
            HasPriorityOrder = true;
            State = AIState.Rebase;
            OrbitTarget = p;
            FindNewPosTimer = 0f;
            GotoStep = 0;
            HasPriorityOrder = true;
            MovePosition.X = p.Center.X;
            MovePosition.Y = p.Center.Y;
        }

        public float TimeToTarget(Planet target)
        {
            if (target == null) return 0;
            float test = Math.Max(1,Vector2.Distance(target.Center, Owner.Center) / Owner.GetmaxFTLSpeed);
            return test;
        }

        private bool InsideAreaOfOperation(Planet planet)
        {
            if (Owner.AreaOfOperation.Count == 0)
                return true;
            foreach (Rectangle ao in Owner.AreaOfOperation)
                if (ao.HitTest(planet.Center))
                    return true;
            return false;
        }

        private static float RelativePlanetFertility(Planet p)
        {
            return p.Owner.data.Traits.Cybernetic > 0 ? p.MineralRichness : p.Fertility;
        }

        private bool SelectPlanetByFilter(Planet[] safePlanets, out Planet targetPlanet, Func<Planet, bool> predicate)
        {
            float minSqDist = float.MaxValue;
            targetPlanet = null;

            for (int i = 0; i < safePlanets.Length; ++i)
            {
                Planet p = safePlanets[i];
                if (!predicate(p) || !InsideAreaOfOperation(p))
                    continue;

                float dist = Owner.Center.SqDist(p.Center);
                if (dist >= minSqDist)
                    continue;

                minSqDist = dist;
                targetPlanet = p;
            }
            return targetPlanet != null;
        }

        private void ScrapShip(float elapsedTime, ShipGoal goal)
        {
            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius * 3)
            {
                DoOrbit(goal.TargetPlanet, elapsedTime);
                return;
            }

            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius)
            {
                ThrustTowardsPosition(goal.TargetPlanet.Center, elapsedTime, 200);
                return;
            }
            OrderQueue.Clear();
            Planet targetPlanet = goal.TargetPlanet;
            targetPlanet.ProdHere = targetPlanet.ProdHere + Owner.GetCost(Owner.loyalty) / 2f;
            Owner.QueueTotalRemoval();
            Owner.loyalty.GetEmpireAI().Recyclepool++;
        }

        public void Update(float elapsedTime)
        {
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate)
                State = AIState.Exterminate;
            if (ClearOrdersNext)
            {
                OrderQueue.Clear();
                ClearOrdersNext = false;
                AwaitClosest = null;
                State = AIState.AwaitingOrders;
            }
            CheckTargetQueue();

            PrioritizePlayerCommands();
            if (HadPO && State != AIState.AwaitingOrders)
                HadPO = false;
            if (State == AIState.Resupply)
            {
                HasPriorityOrder = true;
                if (Owner.Ordinance >= Owner.OrdinanceMax && Owner.Health >= Owner.HealthMax)
                    //fbedard: consider health also
                {
                    HasPriorityOrder = false;
                    State = AIState.AwaitingOrders;
                }
            }

            ResetStateFlee();

            ScanForThreat(elapsedTime);

            Owner.loyalty.data.Traits.ApplyTraitToShip(Owner);

            UpdateUtilityModuleAI(elapsedTime);

            if (State == AIState.ManualControl)
                return;

            Owner.isThrusting = false;
            Owner.isTurning = false;

            if (UpdateFreightAI()) return;

            if (UpdateOrderQueueAI(elapsedTime)) return;

            AIStateRebase();

            UpdateCombatStateAI(elapsedTime);

            UpdateResupplyAI();

            if (!Owner.isTurning)
            {
                DeRotate();
            }
        }

        private void UpdateResupplyAI()
        {
            if (Owner.Health < 0.1f)
                return;

            ResupplyReason resupplyReason = Owner.Supply.Resupply();
            if (resupplyReason != ResupplyReason.NotNeeded && Owner.Mothership?.Active == true)
            {
                OrderReturnToHangar(); // dealing with hangar ships needing resupply
                return;
            }

            if (Owner.loyalty.isFaction)
                return;

            ProcessResupply(resupplyReason);
        }

        private void ProcessResupply(ResupplyReason resupplyReason)
        {
            Planet nearestRallyPoint = null;
            switch (resupplyReason)
            {
                case ResupplyReason.LowOrdnanceCombat:
                    if (FriendliesNearby.Any(supply => supply.SupplyShipCanSupply))
                    {
                        Ship supplyShip = FriendliesNearby.FindMinFiltered(supply => supply.Carrier.HasSupplyBays,
                                                                           supply => -supply.Center.SqDist(Owner.Center));

                        SetUpSupplyEscort(supplyShip, supplyType: "Rearm");
                        return;
                    }
                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.LowOrdnanceNonCombat:
                    if (FriendliesNearby.Any(supply => supply.SupplyShipCanSupply))
                    {
                        State = AIState.ResupplyEscort; // FB: this will signal supply carriers to dispatch a supply shuttle
                        return;
                    }
                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.NoCommand:
                case ResupplyReason.LowHealth:
                    if (Owner.fleet != null && Owner.fleet.HasRepair)
                    {
                        Ship supplyShip =  Owner.fleet.GetShips.First(supply => supply.hasRepairBeam || supply.HasRepairModule);
                        SetUpSupplyEscort(supplyShip, supplyType: "Repair");
                        return;
                    }
                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.LowTroops:
                    nearestRallyPoint = Owner.loyalty.RallyShipYards.FindMax(p => p.TroopsHere.Count);
                    break;
                case ResupplyReason.NotNeeded:
                    return;
            }
            HasPriorityOrder = true;
            DecideWhereToResupply(nearestRallyPoint);
        }

        private void SetUpSupplyEscort(Ship supplyShip, string supplyType = "All")
        {
            EscortTarget       = supplyShip;
            float minDistance  = Owner.Radius + supplyShip.Radius;
            var goal           = new ShipGoal(Plan.ResupplyEscort, Vector2.Zero, 0f)
            {
                FacingVector   = UniverseRandom.RandomBetween(0, 360),
                VariableNumber = minDistance + UniverseRandom.RandomBetween(200, 1000),
                VariableString = supplyType
            };

            State = AIState.ResupplyEscort;
            IgnoreCombat = true;
            OrderQueue.Clear();
            OrderQueue.Enqueue(goal);
        }

        private void DecideWhereToResupply(Planet nearestRallyPoint, bool cancelOrders = false)
        {
            if (nearestRallyPoint != null)
                OrderResupply(nearestRallyPoint, false);
            else
            {
                nearestRallyPoint = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                if (nearestRallyPoint != null)
                    OrderResupply(nearestRallyPoint, false);
                else
                    OrderFlee(true);
            }
        }

        public void TerminateResupplyIfDone(SupplyType supplyType = SupplyType.All)
        {
            if (Owner.AI.State != AIState.Resupply && Owner.AI.State != AIState.ResupplyEscort)
                return;

            if (!Owner.Supply.DoneResupplying(supplyType))
                return;

            Owner.AI.HasPriorityOrder = false;
            if (OrderQueue.NotEmpty)
                OrderQueue.RemoveFirst();

            Owner.AI.State = AIState.AwaitingOrders;
            Owner.AI.IgnoreCombat = false;
        }

        private void UpdateCombatStateAI(float elapsedTime)
        {
            TriggerDelay -= elapsedTime;
            if (BadGuysNear && !IgnoreCombat)
            {
                using (OrderQueue.AcquireWriteLock())
                {
                    ShipGoal firstgoal = OrderQueue.PeekFirst;
                    if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters)
                    {
                        if (Target != null && !HasPriorityOrder && State != AIState.Resupply &&
                            (OrderQueue.IsEmpty ||
                             firstgoal != null && firstgoal.Plan != Plan.DoCombat && firstgoal.Plan != Plan.Bombard &&
                             firstgoal.Plan != Plan.BoardShip))
                        {
                            OrderQueue.PushToFront(new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f));
                        }
                        if (TriggerDelay < 0)
                        {
                            TriggerDelay = elapsedTime * 2;
                            FireOnTarget();
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < Owner.Weapons.Count; x++)
                    Owner.Weapons[x].ClearFireTarget();


                if (Owner.Carrier.HasHangars && Owner.loyalty != UniverseScreen.player)
                {
                    foreach (ShipModule hangar in Owner.Carrier.AllFighterHangars)
                    {
                        Ship hangarShip = hangar.GetHangarShip();
                        if (hangarShip != null && hangarShip.Active)
                            hangarShip.AI.OrderReturnToHangar();
                    }
                }
                else if (Owner.Carrier.HasHangars)
                {
                    foreach (ShipModule hangar in Owner.Carrier.AllFighterHangars)
                    {
                        Ship hangarShip = hangar.GetHangarShip();
                        if (hangarShip == null
                            || hangarShip.AI.State == AIState.ReturnToHangar
                            || hangarShip.AI.HasPriorityTarget
                            || hangarShip.AI.HasPriorityOrder)
                                continue;

                        if (Owner.FightersLaunched)
                            hangarShip.DoEscort(Owner);
                        else
                            hangarShip.AI.OrderReturnToHangar();
                    }
                }
            }
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian && BadGuysNear) //fbedard: civilian will evade
                CombatState = CombatState.Evade;
        }

        private void AIStateRebase()
        {
            if (State != AIState.Rebase) return;
            if (OrderQueue.IsEmpty)
            {
                OrderRebaseToNearest();
                return;
            }
            for (int x = 0; x < OrderQueue.Count; x++)
            {
                ShipGoal goal = OrderQueue[x];
                if (goal.Plan != Plan.Rebase || goal.TargetPlanet == null || goal.TargetPlanet.Owner == Owner.loyalty)
                    continue;
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
                return;
            }
        }

        private bool UpdateOrderQueueAI(float elapsedTime)
        {
            ShipGoal toEvaluate;
            if (OrderQueue.IsEmpty)
            {
                if (Owner.fleet == null)
                {

                    WayPoints.Clear();

                    AIState state = State;
                    if (state <= AIState.MoveTo)
                    {
                        if (state <= AIState.SystemTrader)
                        {
                            if (state == AIState.DoNothing)
                                AwaitOrders(elapsedTime);
                            else
                                switch (state)
                                {
                                    case AIState.AwaitingOrders:
                                    {
                                        AIStateAwaitingOrders(elapsedTime);
                                        break;
                                    }
                                    case AIState.Escort:
                                    {
                                        AIStateEscort(elapsedTime);
                                        break;
                                    }
                                    case AIState.SystemTrader:
                                    {
                                        AIStateOrderTrade(elapsedTime);
                                        break;
                                    }
                                }
                        }
                        else if (state == AIState.PassengerTransport)
                        {
                            AIStatePassengersTransport(elapsedTime);
                        }
                    }
                    else if (state <= AIState.ReturnToHangar)
                    {
                        switch (state)
                        {
                            case AIState.SystemDefender:
                            {
                                AwaitOrders(elapsedTime);
                                break;
                            }
                            case AIState.AwaitingOffenseOrders:
                            {
                                break;
                            }
                            case AIState.Resupply:
                            {
                                AwaitOrders(elapsedTime);
                                break;
                            }
                            default:
                            {
                                if (state == AIState.ReturnToHangar)
                                {
                                    DoReturnToHangar(elapsedTime);
                                }

                                break;
                            }
                        }
                    }
                    else if (state != AIState.Intercept)
                    {
                        if (state == AIState.Exterminate)
                            OrderFindExterminationTarget(true);
                    }
                    else if (Target != null)
                    {
                        OrbitShip(Target as Ship, elapsedTime, Orbit.Right);
                    }
                }
                else
                {
                    if (HasPriorityOrder)
                        HasPriorityOrder = false;
                    using (Owner.fleet.Ships.AcquireReadLock())
                        IdleFleetAI(elapsedTime);
                }
            }
            else if (OrderQueue.NotEmpty && (toEvaluate = OrderQueue.PeekFirst) != null)
            {
                Planet targetPlanet = toEvaluate.TargetPlanet;
                switch (toEvaluate.Plan)
                {
                    case Plan.HoldPosition:
                        HoldPosition();
                        break;
                    case Plan.Stop:
                        Stop(elapsedTime, toEvaluate);
                        break;
                    case Plan.Scrap:
                    {
                        ScrapShip(elapsedTime, toEvaluate);
                        break;
                    }
                    case Plan.Bombard: //Modified by Gretman
                        if (Owner.Ordinance < 0.05 * Owner.OrdinanceMax //'Aint Got no bombs!
                            || targetPlanet.TroopsHere.Count == 0 && targetPlanet.Population <= 0f //Everyone is dead
                            || (targetPlanet.GetGroundStrengthOther(Owner.loyalty) + 1) * 1.5
                            <= targetPlanet.GetGroundStrength(Owner.loyalty))
                            //This will tilt the scale just enough so that if there are 0 troops, a planet can still be bombed.

                        {
                            //As far as I can tell, if there were 0 troops on the planet, then GetGroundStrengthOther and GetGroundStrength would both return 0,
                            //meaning that the planet could not be bombed since that part of the if statement would always be true (0 * 1.5 <= 0)
                            //Adding +1 to the result of GetGroundStrengthOther tilts the scale just enough so a planet with no troops at all can still be bombed
                            //but having even 1 allied troop will cause the bombine action to abort.

                            OrderQueue.Clear();
                            State = AIState.AwaitingOrders;
                            var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                            {
                                TargetPlanet = toEvaluate.TargetPlanet
                            };

                            OrderQueue.Enqueue(orbit); //Stay in Orbit

                            HasPriorityOrder = false;
                            //Log.Info("Bombardment info! " + target.GetGroundStrengthOther(this.Owner.loyalty) + " : " + target.GetGroundStrength(this.Owner.loyalty));
                        } //Done -Gretman

                        DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                        float radius = toEvaluate.TargetPlanet.ObjectRadius + Owner.Radius + 1500;
                        if (toEvaluate.TargetPlanet.Owner == Owner.loyalty)
                        {
                            OrderQueue.Clear();
                            return true;
                        }
                        DropBombsAtGoal(toEvaluate, radius);
                        break;
                    case Plan.Exterminate:
                    {
                        DoOrbit(targetPlanet, elapsedTime);
                        radius = targetPlanet.ObjectRadius + Owner.Radius + 1500;
                        if (targetPlanet.Owner == Owner.loyalty || targetPlanet.Owner == null)
                        {
                            OrderQueue.Clear();
                            OrderFindExterminationTarget(true);
                            return true;
                        }
                        DropBombsAtGoal(toEvaluate, radius);
                        break;
                    }
                    case Plan.RotateToFaceMovePosition:
                        RotateToFaceMovePosition(elapsedTime, toEvaluate);
                        break;
                    case Plan.RotateToDesiredFacing:
                        RotateToDesiredFacing(elapsedTime, toEvaluate);
                        break;
                    case Plan.MoveToWithin1000:
                        MoveToWithin1000(elapsedTime, toEvaluate);
                        break;

                    case Plan.MakeFinalApproachFleet:
                        if (Owner.fleet != null)
                        {
                            MakeFinalApproachFleet(elapsedTime, toEvaluate);
                            break;
                        }
                        State = AIState.AwaitingOrders;
                        break;

                    case Plan.MoveToWithin1000Fleet:
                        if (Owner.fleet != null)
                        {
                            MoveToWithin1000Fleet(elapsedTime, toEvaluate);
                            break;
                        }
                        State = AIState.AwaitingOrders;
                        break;
                    case Plan.MakeFinalApproach:
                        MakeFinalApproach(elapsedTime, toEvaluate);
                        break;
                    case Plan.RotateInlineWithVelocity:
                        RotateInLineWithVelocity(elapsedTime, toEvaluate);
                        break;
                    case Plan.StopWithBackThrust:
                        StopWithBackwardsThrust(elapsedTime, toEvaluate);
                        break;
                    case Plan.Orbit:
                        DoOrbit(targetPlanet, elapsedTime);
                        break;
                    case Plan.Colonize:
                        Colonize(targetPlanet);
                        break;
                    case Plan.Explore:
                        DoExplore(elapsedTime);
                        break;
                    case Plan.Rebase:
                        DoRebase(toEvaluate);
                        break;
                    case Plan.DefendSystem:
                        DoSystemDefense(elapsedTime);
                        break;
                    case Plan.DoCombat:
                        DoCombat(elapsedTime);
                        break;
                    case Plan.MoveTowards:
                        MoveTowardsPosition(MovePosition, elapsedTime);
                        break;
                    case Plan.PickupPassengers:
                    {
                        if (start != null)
                            PickupPassengers();
                        else
                            State = AIState.AwaitingOrders;
                        break;
                    }
                    case Plan.DropoffPassengers:
                        DropoffPassengers();
                        break;
                    case Plan.DeployStructure:
                        DoDeploy(toEvaluate);
                        break;
                    case Plan.PickupGoods:
                        PickupGoods();
                        break;
                    case Plan.DropOffGoods:
                        DropOffGoods();
                        break;
                    case Plan.ReturnToHangar:
                        DoReturnToHangar(elapsedTime);
                        break;
                    case Plan.TroopToShip:
                        DoTroopToShip(elapsedTime);
                        break;
                    case Plan.BoardShip:
                        DoBoardShip(elapsedTime);
                        break;
                    case Plan.SupplyShip:
                        DoSupplyShip(elapsedTime, toEvaluate);
                        break;
                    case Plan.Refit:
                        DoRefit(elapsedTime, toEvaluate);
                        break;
                    case Plan.LandTroop:
                        DoLandTroop(elapsedTime, toEvaluate);
                        break;
                    case Plan.ResupplyEscort:
                        DoResupplyEscort(elapsedTime, toEvaluate);
                        break;
                    default:
                        break;
                }
            }
            return false;
        }

        private void IdleFleetAI(float elapsedTime)
        {
            if (!OrderQueue.IsEmpty)
                return;
            bool nearFleetOffSet = Owner.Center.InRadius(Owner.fleet.Position + Owner.FleetOffset, 75);
            if (nearFleetOffSet)
            {
                Owner.Velocity = Vector2.Zero;
                Vector2 vector2 = Vector2.Zero.PointFromRadians(Owner.fleet.Facing, 1f);
                Vector2 fvec = Vector2.Zero.DirectionToTarget(vector2);
                Vector2 wantedForward = Vector2.Normalize(fvec);
                var forward = new Vector2((float) Math.Sin(Owner.Rotation),
                    -(float) Math.Cos(Owner.Rotation));
                var right = new Vector2(-forward.Y, forward.X);
                var angleDiff = (float) Math.Acos(Vector2.Dot(wantedForward, forward));
                float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;
                if (angleDiff > 0.02f)
                    RotateToFacing(elapsedTime, angleDiff, facing);
            }
            else if (State == AIState.FormationWarp || State == AIState.Orbit || State == AIState.AwaitingOrders
                     || !HasPriorityOrder && !HadPO
                                          && State != AIState.HoldPosition)
            {
                //if (State == AIState.FormationWarp)
                //{
                //    //OrderMoveToFleetPosition(Owner.fleet.Position + Owner.FleetOffset, 0f, Vector2.Zero, true, Owner.velocityMaximum, Owner.fleet);
                //    Log.Warning($"Fleet formation warp should not be possible with nothing in order queue.");
                //    ClearOrdersNext = true;
                //}

                    if (Owner.fleet.Position.InRadius(Owner.Center, 7500))
                        ThrustTowardsPosition(Owner.fleet.Position + Owner.FleetOffset, elapsedTime, Owner.Speed);
                    else
                    {
                        WayPoints.Clear();
                        WayPoints.Enqueue(Owner.fleet.Position + Owner.FleetOffset);
                        //fbedard: set new order for ship returning to fleet
                        State = AIState.AwaitingOrders;
                    if (Owner.fleet?.GetStack().Count > 0)
                        WayPoints.Enqueue(Owner.fleet.GetStack().Peek().MovePosition + Owner.FleetOffset);
                    else
                        OrderMoveTowardsPosition(Owner.fleet.Position + Owner.FleetOffset, DesiredFacing, true, null);
                        //(Owner.fleet.Position + Owner.FleetOffset, 0f, Vector2.Zero, true,
                          //      Owner.velocityMaximum, Owner.fleet);
                    }
            }
        }


        private bool UpdateFreightAI()
        {
            if (State == AIState.SystemTrader && start != null && end != null &&
                (start.Owner != Owner.loyalty || end.Owner != Owner.loyalty))
            {
                start = null;
                end = null;
                OrderTrade(5f);
                return true;
            }
            if (State == AIState.PassengerTransport && start != null && end != null &&
                (start.Owner != Owner.loyalty || end.Owner != Owner.loyalty))
            {
                start = null;
                end = null;
                OrderTransportPassengers(5f);
                return true;
            }
            return false;
        }
        public bool ClearOrderIfCombat() => ClearOrdersConditional(Plan.DoCombat);
        public bool ClearOrdersConditional(Plan plan)
        {
            bool clearOrders = false;

                foreach (var order in OrderQueue)
                {
                    if (order.Plan != plan)
                        continue;
                    clearOrders = true;
                    break;


                }
            if (clearOrders)
                OrderQueue.Clear();
            return clearOrders;
        }

        private void UpdateUtilityModuleAI(float elapsedTime)
        {
            UtilityModuleCheckTimer -= elapsedTime;
            if (Owner.engineState != Ship.MoveState.Warp && UtilityModuleCheckTimer <= 0f)
            {
                UtilityModuleCheckTimer = 1f;
                //Added by McShooterz: logic for transporter modules
                if (Owner.Carrier.HasTransporters)
                    for (int x = 0; x < Owner.Carrier.AllTransporters.Length; x++) // FB:change to foreach
                    {
                        ShipModule module = Owner.Carrier.AllTransporters[x];
                        if (module.TransporterTimer > 0f || !module.Active || !module.Powered ||
                            module.TransporterPower >= Owner.PowerCurrent) continue;
                        if (FriendliesNearby.Count > 0 && module.TransporterOrdnance > 0 && Owner.Ordinance > 0)
                            DoOrdinanceTransporterLogic(module);
                        if (module.TransporterTroopAssault > 0 && Owner.TroopList.Any())
                            DoAssaultTransporterLogic(module);
                    }

                //Do repair check if friendly ships around
                if (FriendliesNearby.Count <= 0)
                    return;
                //Added by McShooterz: logic for repair beams
                if (Owner.hasRepairBeam)
                    for (int x = 0; x < Owner.RepairBeams.Count; x++)
                    {
                        ShipModule module = Owner.RepairBeams[x];
                        if (module.InstalledWeapon.CooldownTimer <= 0f &&
                            module.InstalledWeapon.Module.Powered &&
                            Owner.Ordinance >= module.InstalledWeapon.OrdinanceRequiredToFire &&
                            Owner.PowerCurrent >= module.InstalledWeapon.PowerRequiredToFire)
                            DoRepairBeamLogic(module.InstalledWeapon);
                    }

                if (!Owner.HasRepairModule) return;
                for (int x = 0; x < Owner.Weapons.Count; x++)
                {
                    Weapon weapon = Owner.Weapons[x];
                    if (weapon.CooldownTimer > 0f || !weapon.Module.Powered ||
                        Owner.Ordinance < weapon.OrdinanceRequiredToFire ||
                        Owner.PowerCurrent < weapon.PowerRequiredToFire || !weapon.IsRepairDrone)
                    {
                        //Gretman -- Added this so repair drones would cooldown outside combat (+15s)
                        if (weapon.CooldownTimer > 0f)
                            weapon.CooldownTimer = MathHelper.Max(weapon.CooldownTimer - 1, 0f);
                        continue;
                    }

                    DoRepairDroneLogic(weapon);
                }
            }
        }


        private void ScanForThreat(float elapsedTime)
        {
            ScanForThreatTimer -= elapsedTime;
            if (!(ScanForThreatTimer < 0f)) return;
            SetCombatStatus(elapsedTime);
            ScanForThreatTimer = 2f;
        }

        private void ResetStateFlee()
        {
            if (State != AIState.Flee || BadGuysNear || State == AIState.Resupply || HasPriorityOrder) return;
            if (OrderQueue.NotEmpty)
                OrderQueue.RemoveLast();
            switch (FoodOrProd) {
                case Goods.Colonists:  State = AIState.PassengerTransport; break;
                case Goods.Food:
                case Goods.Production: State = AIState.SystemTrader; break;
                default:
                    State = DefaultAIState;
                    break;
            }
        }

        private void PrioritizePlayerCommands()
        {
            if (Owner.loyalty == UniverseScreen.player &&
                (State == AIState.MoveTo && Vector2.Distance(Owner.Center, MovePosition) > 100f || State == AIState.Orbit ||
                 State == AIState.Bombard || State == AIState.AssaultPlanet || State == AIState.BombardTroops ||
                 State == AIState.Rebase || State == AIState.Scrap || State == AIState.Resupply || State == AIState.Refit ||
                 State == AIState.FormationWarp))
            {
                HasPriorityOrder = true;
                HadPO = false;
                EscortTarget = null;
            }
        }

        private void CheckTargetQueue()
        {
            if (!HasPriorityTarget)
                TargetQueue.Clear();
            for (int x = TargetQueue.Count - 1; x >= 0; x--)
            {
                Ship target = TargetQueue[x];
                if (target.Active)
                    continue;
                TargetQueue.RemoveAtSwapLast(x);

            }


        }

        private void AIStatePassengersTransport(float elapsedTime)
        {
            OrderTransportPassengers(elapsedTime);
            if (start == null || end == null)
                AwaitOrders(elapsedTime);
        }

        private void AIStateOrderTrade(float elapsedTime)
        {
            OrderTrade(elapsedTime);
            if (start == null || end == null)
            {
                AwaitOrders(elapsedTime);
                State = AIState.SystemTrader;
            }
        }

        private void AIStateEscort(float elapsedTime)
        {
            Owner.AI.HasPriorityOrder = false;
            if (EscortTarget == null || !EscortTarget.Active)
            {
                EscortTarget = null;
                OrderQueue.Clear();
                ClearOrdersNext = false;
                if (Owner.Mothership != null && Owner.Mothership.Active)
                {
                    OrderReturnToHangar();
                    return;
                }
                State = AIState.AwaitingOrders; //fbedard
                return;
            }
            if (Owner.GetStrength() <=0 ||
                Owner.Mothership == null &&
                EscortTarget.Center.InRadius(Owner.Center, Owner.SensorRange) ||
                Owner.Mothership == null || !Owner.Mothership.AI.BadGuysNear ||
                EscortTarget != Owner.Mothership)
            {
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
                return;
            }
            // Doctor: This should make carrier-launched fighters scan for their own combat targets, except using the mothership's position
            // and a standard 30k around it instead of their own. This hopefully will prevent them flying off too much, as well as keeping them
            // in a carrier-based role while allowing them to pick appropriate target types depending on the fighter type.
            //gremlin Moved to setcombat status as target scan is expensive and did some of this already. this also shortcuts the UseSensorforTargets switch. Im not sure abuot the using the mothership target.
            // i thought i had added that in somewhere but i cant remember where. I think i made it so that in the scan it takes the motherships target list and adds it to its own.
            if(!Owner.InCombat )
            {
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
                return;
            }

            if (Owner.InCombat && Owner.Center.OutsideRadius(EscortTarget.Center, Owner.AI.CombatAI.PreferredEngagementDistance))
            {
                Owner.AI.HasPriorityOrder = true;
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
            }
        }

        private void AIStateAwaitingOrders(float elapsedTime)
        {
            if (Owner.loyalty != UniverseScreen.player)
                AwaitOrders(elapsedTime);
            else
                AwaitOrdersPlayer(elapsedTime);
            if (Owner.loyalty.isFaction)
                return;

            if (Owner.OrdnanceStatus > ShipStatus.Average)
                return;
            if (FriendliesNearby.Any(supply => supply.Carrier.HasSupplyBays && supply.OrdnanceStatus > ShipStatus.Poor))
                return;
            var resupplyPlanet = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
            if (resupplyPlanet == null)
                return;
            OrderResupply(resupplyPlanet, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShipAI() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            NearByShips = null;
            FriendliesNearby?.Dispose(ref FriendliesNearby);
            OrderQueue?.Dispose(ref OrderQueue);
            PotentialTargets?.Dispose(ref PotentialTargets);
        }
    }
}