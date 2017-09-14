using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

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
        public FleetDataNode FleetNode { get; private set; }         
        private static float[] DmgLevel = { 0.25f, 0.85f, 0.65f, 0.45f, 0.45f, 0.45f, 0.0f };  //fbedard: dmg level for repair

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
        public BatchRemovalCollection<ShipWeight> NearbyShips = new BatchRemovalCollection<ShipWeight>();
        public BatchRemovalCollection<Ship> FriendliesNearby  = new BatchRemovalCollection<Ship>();        
        public object WayPointLocker;

        

        public ShipAI(Ship owner)
        {
            Owner = owner;
            State = AIState.AwaitingOrders;
            WayPointLocker = new object();
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
            if (TargetPlanet.Owner != null || !TargetPlanet.habitable)
            {                
                if (ColonizeGoal != null)
                {					
                    ColonizeGoal.Step += 1;
                    Owner.loyalty.GetGSAI().Goals.QueuePendingRemoval(ColonizeGoal);
                }
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
                    ColonizeTarget.GovernorOn = false;
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
            ColonizeTarget.Crippled_Turns = 0;
            StatTracker.StatAddColony(ColonizeTarget, Owner.loyalty, UniverseScreen);		
                
            foreach (Goal g in Owner.loyalty.GetGSAI().Goals)
            {
                if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != ColonizeTarget)
                    continue;
                Owner.loyalty.GetGSAI().Goals.QueuePendingRemoval(g);
                break;
            }
            Owner.loyalty.GetGSAI().Goals.ApplyPendingRemovals();
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
                system.SetExploredBy(Owner.loyalty);;
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
            float test = 0;
            test = Vector2.Distance(target.Center, Owner.Center) / Owner.GetmaxFTLSpeed;
            return test;
        }

        private bool InsideAreaOfOperation(Planet planet)
        {
            if (Owner.AreaOfOperation.Count == 0)
                return true;
            foreach (Rectangle AO in Owner.AreaOfOperation)
                if (AO.HitTest(planet.Center))
                    return true;
            return false;
        }

        private float RelativePlanetFertility(Planet p)
        {
            return p.Owner.data.Traits.Cybernetic > 0 ? p.MineralRichness : p.Fertility;
        }

        private bool SelectPlanetByFilter(Planet[] safePlanets, out Planet targetPlanet, Func<Planet, bool> predicate)
        {
            float minSqDist = 999999999f;
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
            if (Vector2.Distance(goal.TargetPlanet.Center, Owner.Center) >= goal.TargetPlanet.ObjectRadius + Owner.Radius)   //2500f)   //OrbitTarget.ObjectRadius *15)
            {
                //goal.MovePosition = goal.TargetPlanet.Position;
                //this.MoveToWithin1000(elapsedTime, goal);
                //goal.SpeedLimit = this.Owner.GetSTLSpeed();
                DoOrbit(goal.TargetPlanet, elapsedTime);
                return;
            }
            OrderQueue.Clear();
            Planet targetPlanet = goal.TargetPlanet;
            targetPlanet.ProductionHere = targetPlanet.ProductionHere + Owner.GetCost(Owner.loyalty) / 2f;
            Owner.QueueTotalRemoval();
            Owner.loyalty.GetGSAI().Recyclepool++;
        }

        public void Update(float elapsedTime)
        {

            ShipGoal toEvaluate;
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate)
                State = AIState.Exterminate;
            if (ClearOrdersNext)
            {
                OrderQueue.Clear();
                ClearOrdersNext = false;
                AwaitClosest = null;
                State = AIState.AwaitingOrders;
            }
            var ToRemove = new Array<Ship>();
            foreach (Ship target in TargetQueue)
            {
                if (target.Active)
                    continue;
                ToRemove.Add(target);
            }
            foreach (Ship ship in ToRemove)
                TargetQueue.Remove(ship);
            if (!HasPriorityTarget)
                TargetQueue.Clear();
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
          
            if (State == AIState.Flee && !BadGuysNear && State != AIState.Resupply && !HasPriorityOrder)
            {
                if (OrderQueue.NotEmpty)
                    OrderQueue.RemoveLast();
                if (FoodOrProd == "Pass")
                    State = AIState.PassengerTransport;
                else if (FoodOrProd == "Food" || FoodOrProd == "Prod")
                    State = AIState.SystemTrader;
                else
                    State = DefaultAIState;
            }
            ScanForThreatTimer -= elapsedTime;
            if (ScanForThreatTimer < 0f)
            {
                SetCombatStatus(elapsedTime);
                ScanForThreatTimer = 2f;
                if (Owner.loyalty.data.Traits.Pack)
                {
                    Owner.DamageModifier = -0.25f;
                    Ship owner = Owner;
                    owner.DamageModifier = owner.DamageModifier + 0.05f * (float) FriendliesNearby.Count;
                    if (Owner.DamageModifier > 0.5f)
                        Owner.DamageModifier = 0.5f;
                }
            }
            UtilityModuleCheckTimer -= elapsedTime;
            if (Owner.engineState != Ship.MoveState.Warp && UtilityModuleCheckTimer <= 0f)
            {
                UtilityModuleCheckTimer = 1f;
                //Added by McShooterz: logic for transporter modules
                if (Owner.hasTransporter)
                    foreach (ShipModule module in Owner.Transporters)
                        if (module.TransporterTimer <= 0f && module.Active && module.Powered &&
                            module.TransporterPower < Owner.PowerCurrent)
                        {
                            if (FriendliesNearby.Count > 0 && module.TransporterOrdnance > 0 && Owner.Ordinance > 0)
                                DoOrdinanceTransporterLogic(module);
                            if (module.TransporterTroopAssault > 0 && Owner.TroopList.Any())
                                DoAssaultTransporterLogic(module);
                        }
                //Do repair check if friendly ships around and no combat
                if (!Owner.InCombat && FriendliesNearby.Count > 0)
                {
                    //Added by McShooterz: logic for repair beams
                    if (Owner.hasRepairBeam)
                        foreach (ShipModule module in Owner.RepairBeams)
                            if (module.InstalledWeapon.CooldownTimer <= 0f &&
                                module.InstalledWeapon.Module.Powered &&
                                Owner.Ordinance >= module.InstalledWeapon.OrdinanceRequiredToFire &&
                                Owner.PowerCurrent >= module.InstalledWeapon.PowerRequiredToFire)
                                DoRepairBeamLogic(module.InstalledWeapon);
                    if (Owner.HasRepairModule)
                        foreach (Weapon weapon in Owner.Weapons)
                        {
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
            if (State == AIState.ManualControl)
                return;
            ReadyToWarp = true;
            Owner.isThrusting = false;
            Owner.isTurning = false;

            if (State == AIState.SystemTrader && start != null && end != null &&
                (start.Owner != Owner.loyalty || end.Owner != Owner.loyalty))
            {
                start = null;
                end = null;
                OrderTrade(5f);
                return;
            }
            if (State == AIState.PassengerTransport && start != null && end != null &&
                (start.Owner != Owner.loyalty || end.Owner != Owner.loyalty))
            {
                start = null;
                end = null;
                OrderTransportPassengers(5f);
                return;
            }
           
            

            if (OrderQueue.IsEmpty)
            {
                if (Owner.fleet == null)
                {
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Clear();
                    }
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
                                        if (Owner.loyalty != UniverseScreen.player)
                                            AwaitOrders(elapsedTime);
                                        else
                                            AwaitOrdersPlayer(elapsedTime);
                                        if (Owner.loyalty.isFaction)
                                            break;

                                        if (Owner.OrdinanceMax < 1 || Owner.Ordinance / Owner.OrdinanceMax >= 0.2f)
                                            break;
                                        if (FriendliesNearby.Any(supply => supply.HasSupplyBays && supply.Ordinance >= 100))
                                            break;
                                        var shipyards = new Array<Planet>();
                                        for (int i = 0; i < Owner.loyalty.GetPlanets().Count; i++)
                                        {
                                            Planet item = Owner.loyalty.GetPlanets()[i];
                                            if (item.HasShipyard)
                                                shipyards.Add(item);
                                        }
                                        var sortedList =
                                            from p in shipyards
                                            orderby Vector2.Distance(Owner.Center, p.Center)
                                            select p;
                                        if (!sortedList.Any())
                                            break;
                                        OrderResupply(sortedList.First(), true);
                                        break;
                                    }
                                    case AIState.Escort:
                                    {
                                        if (EscortTarget == null || !EscortTarget.Active)
                                        {
                                            EscortTarget = null;
                                            OrderQueue.Clear();
                                            ClearOrdersNext = false;
                                            if (Owner.Mothership != null && Owner.Mothership.Active)
                                            {
                                                OrderReturnToHangar();
                                                break;
                                            }
                                            State = AIState.AwaitingOrders; //fbedard
                                            break;
                                        }
                                        if (Owner.BaseStrength == 0 ||
                                            Owner.Mothership == null &&
                                            EscortTarget.Center.InRadius(Owner.Center, Owner.SensorRange) ||
                                            Owner.Mothership == null || !Owner.Mothership.AI.BadGuysNear ||
                                            EscortTarget != Owner.Mothership)
                                        {
                                            OrbitShip(EscortTarget, elapsedTime);
                                            break;
                                        }
                                        // Doctor: This should make carrier-launched fighters scan for their own combat targets, except using the mothership's position
                                        // and a standard 30k around it instead of their own. This hopefully will prevent them flying off too much, as well as keeping them
                                        // in a carrier-based role while allowing them to pick appropriate target types depending on the fighter type.
                                        //gremlin Moved to setcombat status as target scan is expensive and did some of this already. this also shortcuts the UseSensorforTargets switch. Im not sure abuot the using the mothership target. 
                                        // i thought i had added that in somewhere but i cant remember where. I think i made it so that in the scan it takes the motherships target list and adds it to its own. 
                                        else
                                        {
                                            DoCombat(elapsedTime);
                                            break;
                                        }
                                    }
                                    case AIState.SystemTrader:
                                    {
                                        OrderTrade(elapsedTime);
                                        if (start == null || end == null)
                                            AwaitOrders(elapsedTime);
                                        break;
                                    }
                                }
                        }
                        else if (state == AIState.PassengerTransport)
                        {
                            OrderTransportPassengers(elapsedTime);
                            if (start == null || end == null)
                                AwaitOrders(elapsedTime);
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
                                    break;
                                }
                                else
                                {
                                    break;
                                }
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
                        OrbitShip(Target as Ship, elapsedTime);
                    }
                }
                else
                {
                    float DistanceToFleetOffset = Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset);
                    if (DistanceToFleetOffset <= 75f)
                    {
                        Owner.Velocity = Vector2.Zero;
                        Vector2 vector2 = Vector2.Zero.PointFromRadians(Owner.fleet.Facing, 1f);
                        Vector2 fvec = Vector2.Zero.DirectionToTarget(vector2);
                        Vector2 wantedForward = Vector2.Normalize(fvec);
                        var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                            -(float) Math.Cos((double) Owner.Rotation));
                        var right = new Vector2(-forward.Y, forward.X);
                        var angleDiff = (float) Math.Acos((double) Vector2.Dot(wantedForward, forward));
                        float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;
                        if (angleDiff > 0.02f)
                            RotateToFacing(elapsedTime, angleDiff, facing);
                        if (DistanceToFleetOffset <= 75f) //fbedard: dont override high priority resupply
                        {
                            State = AIState.AwaitingOrders;
                            HasPriorityOrder = false;
                        }
                        //add fun idle fleet ship stuff here

                    }
                    else if (State != AIState.HoldPosition && DistanceToFleetOffset > 75f)
                    {
                        ThrustTowardsPosition(Owner.fleet.Position + Owner.FleetOffset, elapsedTime, Owner.fleet.Speed);
                        lock (WayPointLocker)
                        {
                            ActiveWayPoints.Clear();
                            ActiveWayPoints.Enqueue(Owner.fleet.Position + Owner.FleetOffset);
                            if (State != AIState.AwaitingOrders) //fbedard: set new order for ship returning to fleet
                                State = AIState.AwaitingOrders;
                            if (Owner.fleet.GetStack().Count > 0)
                                ActiveWayPoints.Enqueue(Owner.fleet.GetStack().Peek().MovePosition + Owner.FleetOffset);
                        }
                    }


                }
            }
            else if (OrderQueue.NotEmpty)
            {
                toEvaluate = OrderQueue.PeekFirst;
                Planet target = toEvaluate.TargetPlanet;
                switch (toEvaluate.Plan)
                {
                    case Plan.HoldPosition:          HoldPosition(); break;                            
                    case Plan.Stop:	                 Stop(elapsedTime, toEvaluate); break;
                    case Plan.Scrap:
                    {
                        ScrapShip(elapsedTime, toEvaluate);
                        break;
                    }
                    case Plan.Bombard: //Modified by Gretman
                        target = toEvaluate.TargetPlanet; //Stop Bombing if:
                        if (Owner.Ordinance < 0.05 * Owner.OrdinanceMax //'Aint Got no bombs!
                            || target.TroopsHere.Count == 0 && target.Population <= 0f //Everyone is dead
                            || (target.GetGroundStrengthOther(Owner.loyalty) + 1) * 1.5
                            <= target.GetGroundStrength(Owner.loyalty))
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
                            return;
                        }
                        DropBombsAtGoal(toEvaluate, radius);
                        break;
                    case Plan.Exterminate:
                        {
                            DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                            radius = toEvaluate.TargetPlanet.ObjectRadius + Owner.Radius + 1500;
                            if (toEvaluate.TargetPlanet.Owner == Owner.loyalty || toEvaluate.TargetPlanet.Owner == null)
                            {
                                OrderQueue.Clear();
                                OrderFindExterminationTarget(true);
                                return;
                            }
                            DropBombsAtGoal(toEvaluate, radius);
                            break;
                        }
                    case Plan.RotateToFaceMovePosition:    RotateToFaceMovePosition(elapsedTime, toEvaluate);break;                                                
                    case Plan.RotateToDesiredFacing:	   RotateToDesiredFacing(elapsedTime, toEvaluate); break;
                    case Plan.MoveToWithin1000:            MoveToWithin1000(elapsedTime, toEvaluate);break;	                
                        
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
                    case Plan.MakeFinalApproach:	        MakeFinalApproach(elapsedTime, toEvaluate); break;
                    case Plan.RotateInlineWithVelocity:	    RotateInLineWithVelocity(elapsedTime, toEvaluate); break;
                    case Plan.StopWithBackThrust:	        StopWithBackwardsThrust(elapsedTime, toEvaluate); break;
                    case Plan.Orbit:	               	    DoOrbit(toEvaluate.TargetPlanet, elapsedTime); break;
                    case Plan.Colonize:                     Colonize(toEvaluate.TargetPlanet); break;
                    case Plan.Explore: 	                    DoExplore(elapsedTime); break;
                    case Plan.Rebase:                       DoRebase(toEvaluate); break;
                    case Plan.DefendSystem:                 DoSystemDefense(elapsedTime); break;                            
                    case Plan.DoCombat:                     DoCombat(elapsedTime); break;                            
                    case Plan.MoveTowards:	                MoveTowardsPosition(MovePosition, elapsedTime); break;
                    case Plan.PickupPassengers:
                    {
                        if (start != null)
                            PickupPassengers();
                        else
                            State = AIState.AwaitingOrders;
                        break;
                    }
                    case Plan.DropoffPassengers:            DropoffPassengers(); break;                     
                    case Plan.DeployStructure:              DoDeploy(toEvaluate); break;
                    case Plan.PickupGoods:                  PickupGoods();break;	                                               
                    case Plan.DropOffGoods:	                DropoffGoods(); break;                            
                    case Plan.ReturnToHangar:	            DoReturnToHangar(elapsedTime); break;
                    case Plan.TroopToShip:                  DoTroopToShip(elapsedTime); break;                     
                    case Plan.BoardShip:	                DoBoardShip(elapsedTime); break;
                    case Plan.SupplyShip:                   DoSupplyShip(elapsedTime, toEvaluate); break;
                    case Plan.Refit:	                    DoRefit(elapsedTime, toEvaluate); break;                            
                    case Plan.LandTroop:                    DoLandTroop(elapsedTime, toEvaluate); break;
                    default:
                        break;
                }
            }
            if (State == AIState.Rebase)
                foreach (ShipGoal goal in OrderQueue)
                {
                    if (goal.Plan != Plan.Rebase || goal.TargetPlanet == null || goal.TargetPlanet.Owner == Owner.loyalty)
                        continue;
                    OrderQueue.Clear();
                    State = AIState.AwaitingOrders;
                    break;
                }	        
            TriggerDelay -= elapsedTime;
            if (BadGuysNear && !IgnoreCombat )
            {
                using (OrderQueue.AcquireWriteLock())
                {
                    ShipGoal firstgoal = OrderQueue.PeekFirst;
                    if (Owner.Weapons.Count > 0 || Owner.GetHangars().Count > 0 || Owner.Transporters.Count > 0)
                    {
                        if (Target != null && !HasPriorityOrder  && State != AIState.Resupply &&
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
                foreach (Weapon purge in Owner.Weapons)
                    purge.ClearFireTarget();

                if (Owner.GetHangars().Count > 0 && Owner.loyalty != UniverseScreen.player)
                    foreach (ShipModule hangar in Owner.GetHangars())
                    {
                        if (hangar.IsTroopBay || hangar.IsSupplyBay || hangar.GetHangarShip() == null
                            || hangar.GetHangarShip().AI.State == AIState.ReturnToHangar)
                            continue;
                        hangar.GetHangarShip().AI.OrderReturnToHangar();
                    }
                else if (Owner.GetHangars().Count > 0)
                    foreach (ShipModule hangar in Owner.GetHangars())
                    {
                        if (hangar.IsTroopBay
                            || hangar.IsSupplyBay
                            || hangar.GetHangarShip() == null
                            || hangar.GetHangarShip().AI.State == AIState.ReturnToHangar
                            || hangar.GetHangarShip().AI.HasPriorityTarget
                            || hangar.GetHangarShip().AI.HasPriorityOrder

                        )
                            continue;
                        hangar.GetHangarShip().DoEscort(Owner);
                    }
            }
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian && BadGuysNear) //fbedard: civilian will evade
                CombatState = CombatState.Evade;

            if (State != AIState.Resupply && !HasPriorityOrder &&
                Owner.Health / Owner.HealthMax < DmgLevel[(int) Owner.shipData.ShipCategory] &&
                Owner.shipData.Role >= ShipData.RoleName.supply) //fbedard: ships will go for repair
                if (Owner.fleet == null || Owner.fleet != null && !Owner.fleet.HasRepair)
                    OrderResupplyNearest(false);
            if (State == AIState.AwaitingOrders && Owner.NeedResupplyTroops)
                OrderResupplyNearest(false);
            if (State == AIState.AwaitingOrders && Owner.needResupplyOrdnance)
                OrderResupplyNearest(false);
            if (State == AIState.Resupply && !HasPriorityOrder)
                HasPriorityOrder = true;
            if (!Owner.isTurning)
            {
                DeRotate();
                return;
            }
            else
            {
                return;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShipAI() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            NearbyShips?.Dispose(ref NearbyShips);
            FriendliesNearby?.Dispose(ref FriendliesNearby);
            OrderQueue?.Dispose(ref OrderQueue);
            PotentialTargets?.Dispose(ref PotentialTargets);
        }        
    }
}