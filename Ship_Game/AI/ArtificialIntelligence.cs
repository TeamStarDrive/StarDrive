using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algorithms;
using Microsoft.Xna.Framework;
using Ship_Game.Commands;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class ArtificialIntelligence : IDisposable
    {       
        public bool UseSensorsForTargets =true;
        public bool ClearOrdersNext;
        private Vector2 aiNewDir;
        public static UniverseScreen universeScreen;
        public Ship Owner;
        public GameplayObject Target;
        public AIState State = AIState.AwaitingOrders;
        public CombatState CombatState = CombatState.AttackRuns;
        public Guid OrbitTargetGuid;
        public CombatAI CombatAI = new CombatAI();
        public BatchRemovalCollection<ShipWeight> NearbyShips = new BatchRemovalCollection<ShipWeight>();
        public BatchRemovalCollection<Ship> PotentialTargets = new BatchRemovalCollection<Ship>();
        public Planet resupplyTarget;
        public Planet start;
        public Planet end;
        private SolarSystem SystemToPatrol;
        private readonly Array<Planet> PatrolRoute = new Array<Planet>();
        private int stopNumber;
        private Planet PatrolTarget;
        public SolarSystem SystemToDefend;
        public Guid SystemToDefendGuid;
        public SolarSystem ExplorationTarget;
        public Ship EscortTarget;
        public Guid EscortTargetGuid;
        private float findNewPosTimer;
        private Goal ColonizeGoal;
        private Planet awaitClosest;
        private Vector2 OrbitPos;
        private float DistanceLast;
        public bool HasPriorityOrder;
        public int GotoStep;
        private bool AttackRunStarted;
        private float AttackRunAngle;
        private float runTimer;
        private Vector2 AttackVector = Vector2.Zero;
        public AIState DefaultAIState = AIState.AwaitingOrders;
        private FleetDataNode node;
        public bool HadPO;
        private float ScanForThreatTimer;
        public Vector2 MovePosition;
        private float DesiredFacing;
        private Vector2 FinalFacingVector;
        public SafeQueue<ShipGoal> OrderQueue = new SafeQueue<ShipGoal>();
        public Queue<Vector2> ActiveWayPoints = new Queue<Vector2>();
        public Planet ExterminationTarget;
        public string FoodOrProd;
        public bool hasPriorityTarget;
        public bool Intercepting;
        public Array<Ship> TargetQueue = new Array<Ship>();
        private float TriggerDelay = 0;
        public Guid TargetGuid;
        public Planet ColonizeTarget;
        public bool ReadyToWarp = true;
        public Planet OrbitTarget;
        private float OrbitalAngle = RandomMath.RandomBetween(0f, 360f);
        public bool IgnoreCombat;
        public BatchRemovalCollection<Ship> FriendliesNearby = new BatchRemovalCollection<Ship>();
        public bool BadGuysNear;        
        public bool troopsout = false;
        private float UtilityModuleCheckTimer;
        public object WayPointLocker;
        public Ship TargetShip;
        public Array<Projectile> TrackProjectiles = new Array<Projectile>();
        private static float[] DmgLevel = { 0.25f, 0.85f, 0.65f, 0.45f, 0.45f, 0.45f, 0.0f };  //fbedard: dmg level for repair
                
        public ArtificialIntelligence(Ship owner)
        {
            Owner = owner;
            State = AIState.AwaitingOrders;
            WayPointLocker = new object();
        }

        private void AwaitOrders(float elapsedTime)
        {
            if (State != AIState.Resupply)
                HasPriorityOrder = false;
            if (awaitClosest != null)
            {
                DoOrbit(awaitClosest, elapsedTime);
                return;
            }
            SolarSystem home = null;
            if (Owner.System == null)
            {
                if (SystemToDefend != null)
                {
                    DoOrbit(SystemToDefend.PlanetList[0], elapsedTime);
                    awaitClosest = SystemToDefend.PlanetList[0];
                    return;
                }

                if (!Owner.loyalty.isFaction) //for empire find whatever is close. might add to this for better logic. 
                {
                    home = (Owner.loyalty.GetOwnedSystems() as Array<SolarSystem>)
                        .FindMin(s => Owner.Center.SqDist(s.Position));
                }
                else //for factions look for ships in a system so they group up. 
                {
                    home = Owner.loyalty.GetShips()
                        .FindMinFiltered(inSystem => inSystem.System != null,
                            inSystem => Owner.Center.SqDist(inSystem.Center))?.System;
                }

                if (home == null) //Find any system with no owners and planets.
                {
                    home =
                        Empire.Universe.SolarSystemDict.Values.ToArrayList()
                            .FindMinFiltered(o => o.OwnerList.Count == 0 && o.PlanetList.Count > 0,
                                ss => Owner.Center.SqDist(ss.Position));
                }
            }

            if (home != null)
            {
                var closestD = float.MaxValue;
                var closestOurs = false;
                float distance;
                foreach (Planet p in home.PlanetList)
                {
                    if (awaitClosest == null) awaitClosest = p;
                    var ours = false;
                    if (Owner.loyalty.isFaction)
                        ours = p.Owner != null || p.habitable; //for factions it just has to be habitable

                    else ours = p.Owner == Owner.loyalty;

                    if (closestOurs && !ours) // if we already have an owned planet and the current isnt. forget it. 
                        continue;
                    distance = Owner.Center.SqDist(p.Position);

                    if (ours && closestOurs)
                        if (distance >= closestD)
                            continue;

                    closestOurs = true;
                    closestD = distance;
                    awaitClosest = p;
                }
            }
        }

        private void AwaitOrdersPlayer(float elapsedTime)
        {
            HasPriorityOrder = false;
            if (Owner.InCombatTimer > elapsedTime * -5 && ScanForThreatTimer < 2 - elapsedTime * 5)
                ScanForThreatTimer = 0;
            if (EscortTarget != null)
            {
                State = AIState.Escort;
                return;
            }
            if (!HadPO)
            {
                if (SystemToDefend != null)
                {
                    Planet p = Owner.loyalty.GetGSAI().DefensiveCoordinator.AssignIdleShips(Owner);
                    DoOrbit(p, elapsedTime);
                    awaitClosest = p;
                    return;
                }
                else
                if (awaitClosest != null)
                {
                    DoOrbit(awaitClosest, elapsedTime);
                    return;
                }
                awaitClosest =
                    Owner.loyalty.GetGSAI()
                        .GetKnownPlanets()
                        .FindMin(
                            planet => planet.Position.SqDist(Owner.Center) + (Owner.loyalty != planet.Owner ? 300000 : 0));
                return;
            }	        
            if (Owner.System?.OwnerList.Contains(Owner.loyalty) ?? false)
            {
                HadPO = false;
                return;
            }
            Stop(elapsedTime);
        }

        private void Colonize(Planet TargetPlanet)
        {
            if (Owner.Center.OutsideRadius(TargetPlanet.Position, 2000f))
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
            ColonizeTarget.system.OwnerList.Add(Owner.loyalty);
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
            ColonizeTarget.ExploredDict[Owner.loyalty] = true;
            var BuildingsAdded = new Array<string>();
            foreach (ShipModule slot in Owner.ModuleSlotList)//@TODO create building placement methods in planet.cs that take into account the below logic. 
            {
                if (slot == null || slot.ModuleType != ShipModuleType.Colony || slot.DeployBuildingOnColonize == null || BuildingsAdded.Contains(slot.DeployBuildingOnColonize))
                    continue;
                Building building = ResourceManager.CreateBuilding(slot.DeployBuildingOnColonize);
                var ok = true;
                if (building.Unique)
                    foreach (Building b in ColonizeTarget.BuildingList)
                    {
                        if (b.Name != building.Name)
                            continue;
                        ok = false;
                        break;
                    }
                if (!ok)
                    continue;
                BuildingsAdded.Add(slot.DeployBuildingOnColonize);
                ColonizeTarget.BuildingList.Add(building);
                ColonizeTarget.AssignBuildingToTileOnColonize(building);
            }
            ColonizeTarget.TerraformPoints += Owner.loyalty.data.EmpireFertilityBonus;
            ColonizeTarget.Crippled_Turns = 0;
            StatTracker.StatAddColony(ColonizeTarget, Owner.loyalty, universeScreen);		
                
            foreach (Goal g in Owner.loyalty.GetGSAI().Goals)
            {
                if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != ColonizeTarget)
                    continue;
                Owner.loyalty.GetGSAI().Goals.QueuePendingRemoval(g);
                break;
            }
            Owner.loyalty.GetGSAI().Goals.ApplyPendingRemovals();
            if (ColonizeTarget.system.OwnerList.Count > 1)
                foreach (Planet p in ColonizeTarget.system.PlanetList)
                {
                    if (p.Owner == ColonizeTarget.Owner || p.Owner == null)
                        continue;
                    if (p.Owner.TryGetRelations(Owner.loyalty, out Relationship rel) && !rel.Treaty_OpenBorders)
                        p.Owner.DamageRelationship(Owner.loyalty, "Colonized Owned System", 20f, p);
                }
            foreach (ShipModule slot in Owner.ModuleSlotList)
            {
                if (slot.ModuleType != ShipModuleType.Colony)
                    continue;			    
                ColonizeTarget.FoodHere += slot.numberOfFood;				
                ColonizeTarget.ProductionHere += slot.numberOfEquipment;				
                ColonizeTarget.Population += slot.numberOfColonists;
            }
            var TroopsRemoved = false;
            var PlayerTroopsRemoved = false;

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
                TroopsRemoved = true;
                if (t.GetOwner().isPlayer)
                    PlayerTroopsRemoved = true;
            }
            toLaunch.Clear();
            if (TroopsRemoved)
                if (PlayerTroopsRemoved)
                    universeScreen.NotificationManager.AddTroopsRemovedNotification(ColonizeTarget);
                else if (ColonizeTarget.Owner.isPlayer)
                    universeScreen.NotificationManager.AddForeignTroopsRemovedNotification(ColonizeTarget);
            Owner.QueueTotalRemoval();
        }

        private void DeRotate()
        {
            if (Owner.yRotation > 0f)
            {
                Owner.yRotation -= Owner.yBankAmount;
                if (Owner.yRotation < 0f)
                {
                    Owner.yRotation = 0f;
                    return;
                }
            }
            else if (Owner.yRotation < 0f)
            {
                Owner.yRotation += Owner.yBankAmount;
                if (Owner.yRotation > 0f)
                    Owner.yRotation = 0f;
            }
        }

        private void DoAssaultShipCombat(float elapsedTime)
        {
            if (Owner.isSpooling)
                return;
            DoNonFleetArtillery(elapsedTime);
            if (!Owner.loyalty.isFaction && (Target as Ship).shipData.Role < ShipData.RoleName.drone ||
                Owner.GetHangars().Count == 0)
                return;
            var OurTroopStrength = 0f;
            var OurOutStrength = 0f;
            var tcount = 0;
            for (var i = 0; i < Owner.GetHangars().Count; i++)
            {
                ShipModule s = Owner.GetHangars()[i];
                if (s.IsTroopBay)
                {
                    if (s.GetHangarShip() != null)
                        foreach (Troop st in s.GetHangarShip().TroopList)
                        {
                            OurTroopStrength += st.Strength;
                            Ship escShip = s.GetHangarShip().AI.EscortTarget;
                            if (escShip != null && escShip != Target && escShip != Owner)
                                continue;

                            OurOutStrength += st.Strength;
                        }
                    if (s.hangarTimer <= 0)
                        tcount++;
                }
            }
            for (var i = 0; i < Owner.TroopList.Count; i++)
            {
                Troop t = Owner.TroopList[i];
                if (tcount <= 0)
                    break;
                OurTroopStrength = OurTroopStrength + t.Strength;
                tcount--;
            }

            if (OurTroopStrength <= 0) return;
            
            bool boarding = false;
            Ship shipTarget = Target as Ship;
            if (shipTarget != null)
            {
                float EnemyStrength = shipTarget?.BoardingDefenseTotal ?? 0;

                if (OurTroopStrength + OurOutStrength > EnemyStrength &&
                    (Owner.loyalty.isFaction || shipTarget.GetStrength() > 0f))
                {
                    if (OurOutStrength < EnemyStrength)
                        Owner.ScrambleAssaultShips(EnemyStrength);
                    for (var i = 0; i < Owner.GetHangars().Count; i++)
                    {
                        ShipModule hangar = Owner.GetHangars()[i];
                        if (!hangar.IsTroopBay || hangar.GetHangarShip() == null)
                            continue;
                        hangar.GetHangarShip().AI.OrderTroopToBoardShip(shipTarget);
                    }
                    boarding = true;
                }
            }
            if (!boarding && (OurOutStrength >0 || OurTroopStrength >0))
            {
                if (Owner.System?.OwnerList.Count > 0)
                {
                    Planet x = Owner.System.PlanetList.FindMinFiltered(
                        filter: p => p.Owner != null && p.Owner != Owner.loyalty || p.RecentCombat,
                        selector: p => Owner.Center.SqDist(p.Position));
                    if (x == null) return;
                    Owner.ScrambleAssaultShips(0);
                    OrderAssaultPlanet(x);

                }
            }
        }

        //@TODO FIX ATTACK RUNS. This is all goofy.
        private void DoAttackRun(float elapsedTime)
        {            
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            float adjustedWeaponRange = Owner.maxWeaponsRange * .35f;
            float spacerdistance = Owner.Radius * 3 + Target.Radius;
            if (spacerdistance > adjustedWeaponRange)
                spacerdistance = adjustedWeaponRange;

            if (distanceToTarget > spacerdistance && distanceToTarget > adjustedWeaponRange)
            {
                runTimer = 0f;
                AttackRunStarted = false;
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.speed);
                return;
            }

            if (distanceToTarget < adjustedWeaponRange)
            {                
                runTimer += elapsedTime;
                if (runTimer > 7f) 
                {
                    DoNonFleetArtillery(elapsedTime);
                    return;
                }               
                aiNewDir += Owner.Center.FindVectorToTarget(Target.Center + Target.Velocity) * 0.35f;
                if (distanceToTarget < (Owner.Radius + Target.Radius) * 3f && !AttackRunStarted)
                {
                    AttackRunStarted = true;
                    int ran = RandomMath.IntBetween(0, 1);
                    ran = ran == 1 ? 1 : -1;
                    AttackRunAngle = ran * RandomMath.RandomBetween(75f, 100f) + Owner.Rotation.ToDegrees();
                    AttackVector = Owner.Center.PointFromAngle(AttackRunAngle, 1500f); //@why 1500
                }
                AttackVector = Owner.Center.PointFromAngle(AttackRunAngle, 1500f);
                MoveInDirection(AttackVector, elapsedTime);
                if (runTimer > 2)
                {
                    DoNonFleetArtillery(elapsedTime);
                    return;
                }

            }
        }

        private void DoBoardShip(float elapsedTime)
        {
            hasPriorityTarget = true;
            State = AIState.Boarding;
            if ((!EscortTarget?.Active ?? true)
                ||EscortTarget.loyalty==Owner.loyalty)
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
                return;
            }			
            ThrustTowardsPosition(EscortTarget.Center, elapsedTime, Owner.speed);
            float Distance = Owner.Center.Distance(EscortTarget.Center);			
            if (Distance < EscortTarget.Radius + 300f)
            {
                if (Owner.TroopList.Count > 0)
                {
                    EscortTarget.TroopList.Add(Owner.TroopList[0]);
                    Owner.QueueTotalRemoval();
                    return;
                }
            }
            else if (Distance > 10000f 
                &&  Owner.Mothership.AI.CombatState == CombatState.AssaultShip) OrderReturnToHangar();
        }

        private void DoCombat(float elapsedTime)
        {
            var ctarget = Target as Ship;
            if ((!Target?.Active ?? true) || ctarget.engineState == Ship.MoveState.Warp)
            {
                Intercepting = false;
                Target = PotentialTargets.FirstOrDefault(t => t.Active && t.engineState != Ship.MoveState.Warp &&
                                                         t.Center.InRadius(Owner.Center, Owner.SensorRange));
                if (Target == null)
                {
                    if (OrderQueue.NotEmpty) OrderQueue.RemoveFirst();
                    State = DefaultAIState;
                    return;
                }
            }
            awaitClosest = null;
            State = AIState.Combat;
            Owner.InCombat = true;
            Owner.InCombatTimer = 15f;
            if (Owner.Mothership?.Active ?? false)
                if (Owner.shipData.Role != ShipData.RoleName.troop
                    &&
                    (Owner.Health / Owner.HealthMax < DmgLevel[(int) Owner.shipData.ShipCategory] ||
                     (Owner.shield_max > 0 && Owner.shield_percent <= 0))
                    || (Owner.OrdinanceMax > 0 && Owner.Ordinance / Owner.OrdinanceMax <= .1f)
                    || (Owner.PowerCurrent <= 1f && Owner.PowerDraw / Owner.PowerFlowMax <= .1f)
                )
                {
                    OrderReturnToHangar();
                }

            if (State != AIState.Resupply && Owner.OrdinanceMax > 0f && Owner.OrdinanceMax * 0.05 > Owner.Ordinance &&
                !hasPriorityTarget)
                if (!FriendliesNearby.Any(supply => supply.HasSupplyBays && supply.Ordinance >= 100))
                {
                    OrderResupplyNearest(false);
                    return;
                }
            if (State != AIState.Resupply && !Owner.loyalty.isFaction && State == AIState.AwaitingOrders &&
                Owner.TroopCapacity > 0 &&
                Owner.TroopList.Count < Owner.GetHangars().Count(hangar => hangar.IsTroopBay) * .5f)
            {
                OrderResupplyNearest(false);
                return;
            }
            if (State != AIState.Resupply && Owner.Health > 0 &&
                Owner.HealthMax * DmgLevel[(int) Owner.shipData.ShipCategory] > Owner.Health
                && Owner.shipData.Role >= ShipData.RoleName.supply) //fbedard: repair level
                if (Owner.fleet == null || !Owner.fleet.HasRepair)
                {
                    OrderResupplyNearest(false);
                    return;
                }
            if (Target.Center.Distance(Owner.Center) < 10000f)
            {
                if (Owner.engineState != Ship.MoveState.Warp && Owner.GetHangars().Count > 0 && !Owner.ManualHangarOverride)
                    if (!Owner.FightersOut) Owner.FightersOut = true;
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
            }
            else if (CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.speed);
                return;
            }
            if (!HasPriorityOrder && !hasPriorityTarget && Owner.Weapons.Count == 0 && Owner.GetHangars().Count == 0)
                CombatState = CombatState.Evade;
            if (!Owner.loyalty.isFaction && Owner.System != null && Owner.TroopsOut == false &&
                Owner.GetHangars().Any(troops => troops.IsTroopBay) || Owner.hasTransporter)
                if (Owner.TroopList.Count(troop => troop.GetOwner() == Owner.loyalty) == Owner.TroopList.Count)
                {
                    Planet invadeThis =
                        Owner.System.PlanetList.FindMinFiltered(
                            owner =>
                                owner.Owner != null && owner.Owner != Owner.loyalty &&
                                Owner.loyalty.GetRelations(owner.Owner).AtWar,
                            troops => troops.TroopsHere.Count);

                    if (!Owner.TroopsOut && !Owner.hasTransporter)
                    {
                        Ship shipTarget = Target as Ship;
                        if (invadeThis != null)
                        {
                            Owner.TroopsOut = true;
                            foreach (
                                Ship troop in
                                Owner.GetHangars()
                                    .Where(
                                        troop =>
                                            troop.IsTroopBay && troop.GetHangarShip() != null &&
                                            troop.GetHangarShip().Active)
                                    .Select(ship => ship.GetHangarShip()))
                                troop.AI.OrderAssaultPlanet(invadeThis);
                        }
                        else if (shipTarget?.shipData.Role >= ShipData.RoleName.drone)
                        {
                            if (Owner.GetHangars().Count(troop => troop.IsTroopBay) * 60 >=
                                shipTarget.MechanicalBoardingDefense)
                            {
                                Owner.TroopsOut = true;
                                foreach (ShipModule hangar in Owner.GetHangars())
                                {
                                    if (hangar.GetHangarShip() == null || Target == null ||
                                        hangar.GetHangarShip().shipData.Role != ShipData.RoleName.troop ||
                                        shipTarget.shipData.Role < ShipData.RoleName.drone)
                                        continue;
                                    hangar.GetHangarShip().AI.OrderTroopToBoardShip(shipTarget);
                                }
                            }
                        }
                        else
                        {
                            Owner.TroopsOut = false;
                        }
                    }
                }

            

            switch (CombatState)
            {
                case CombatState.Artillery:                             DoNonFleetArtillery(elapsedTime); break;
                case CombatState.OrbitLeft:                             OrbitShipLeft(Target as Ship, elapsedTime); break;
                case CombatState.BroadsideLeft:                         DoNonFleetBroadsideLeft(elapsedTime); break;
                case CombatState.OrbitRight:                            OrbitShip(Target as Ship, elapsedTime); break;
                case CombatState.BroadsideRight:                        DoNonFleetBroadsideRight(elapsedTime); break;
                case CombatState.AttackRuns:                            DoAttackRun(elapsedTime); break;
                case CombatState.HoldPosition:                          DoHoldPositionCombat(elapsedTime); break;
                case CombatState.Evade:                                 DoEvadeCombat(elapsedTime); break;
                case CombatState.AssaultShip:                           DoAssaultShipCombat(elapsedTime); break;
                case CombatState.ShortRange:                            DoNonFleetArtillery(elapsedTime);break;
            }

            if (Target != null)
                return;
            Owner.InCombat = false;
            
        }

        //added by gremlin : troop asssault planet
        public void OrderAssaultPlanet(Planet p)
        {
            State = AIState.AssaultPlanet;
            OrbitTarget = p;
            var shipGoal = new ShipGoal(Plan.LandTroop, Vector2.Zero, 0f, OrbitTarget);
            OrderQueue.Clear();
            OrderQueue.Enqueue(shipGoal);            
        }

        private void DoDeploy(ShipGoal shipgoal)
        {
            if (shipgoal.goal == null)
                return;
            Planet target = shipgoal.TargetPlanet;
            if (shipgoal.goal.TetherTarget != Guid.Empty)
            {
                if (target == null)
                    universeScreen.PlanetsDict.TryGetValue(shipgoal.goal.TetherTarget, out target);
                shipgoal.goal.BuildPosition = target.Position + shipgoal.goal.TetherOffset;
            }
            if (target != null && (target.Position + shipgoal.goal.TetherOffset).Distance(Owner.Center) > 200f)
            {
                shipgoal.goal.BuildPosition = target.Position + shipgoal.goal.TetherOffset;
                OrderDeepSpaceBuild(shipgoal.goal);
                return;
            }
            Ship platform = ResourceManager.CreateShipAtPoint(shipgoal.goal.ToBuildUID, Owner.loyalty,
                shipgoal.goal.BuildPosition);
            if (platform == null)
                return;

            foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                bool found = false;
                foreach (RoadNode node in road.RoadNodesList)
                {
                    if (node.Position != shipgoal.goal.BuildPosition)
                        continue;
                    node.Platform = platform;
                    StatTracker.StatAddRoad(node, Owner.loyalty);
                    found = true;
                    break;
                }
                if (found) break;
            }
            if (shipgoal.goal.TetherTarget != Guid.Empty)
            {
                platform.TetherToPlanet(universeScreen.PlanetsDict[shipgoal.goal.TetherTarget]);
                platform.TetherOffset = shipgoal.goal.TetherOffset;
            }
            Owner.loyalty.GetGSAI().Goals.Remove(shipgoal.goal);
            Owner.QueueTotalRemoval();
        }

        private void DoEvadeCombat(float elapsedTime)
        {
            var AverageDirection = new Vector2();
            var count = 0;
            foreach (ShipWeight ship in NearbyShips)
            {
                if (ship.ship.loyalty == Owner.loyalty ||
                    !ship.ship.loyalty.isFaction && !Owner.loyalty.GetRelations(ship.ship.loyalty).AtWar)
                    continue;
                AverageDirection = AverageDirection + Owner.Center.FindVectorToTarget(ship.ship.Center);
                count++;
            }
            if (count != 0)
            {
                AverageDirection = AverageDirection / count;
                AverageDirection = Vector2.Normalize(AverageDirection);
                AverageDirection = Vector2.Negate(AverageDirection);
                AverageDirection = AverageDirection * 7500f; //@WHY 7500?
                ThrustTowardsPosition(AverageDirection + Owner.Center, elapsedTime, Owner.speed);
            }
        }

        public void DoExplore(float elapsedTime)
        {
            HasPriorityOrder = true;
            IgnoreCombat = true;
            if (ExplorationTarget == null)
            {
                ExplorationTarget = Owner.loyalty.GetGSAI().AssignExplorationTarget(Owner);
                if (ExplorationTarget == null)
                {
                    OrderQueue.Clear();
                    State = AIState.AwaitingOrders;
                    return;
                }
            }
            else if (DoExploreSystem(elapsedTime)) //@Notification
            {
                if (Owner.loyalty.isPlayer)
                {
                    //added by gremlin  add shamatts notification here
                    SolarSystem system = ExplorationTarget;
                    var message = new StringBuilder(system.Name);//@todo create global string builder
                    message.Append(" system explored.");

                    var planetsTypesNumber = new Map<string, int>();
                    if (system.PlanetList.Count > 0)
                    {
                        foreach (Planet planet in system.PlanetList)
                        {
                            //some planets don't have Type set and it is null
                            planet.Type = planet.Type ?? "Other";
                            planetsTypesNumber.AddToValue(planet.Type, 1);
                        }

                        foreach (var pair in planetsTypesNumber)
                            message.Append('\n').Append(pair.Value).Append(' ').Append(pair.Key);
                    }

                    foreach (Planet planet in system.PlanetList)
                    {
                        Building tile = planet.BuildingList.Find(t => t.IsCommodity);
                        if (tile != null)
                            message.Append('\n').Append(tile.Name).Append(" on ").Append(planet.Name);
                    }

                    if (system.combatTimer > 0)
                        message.Append("\nCombat in system!!!");

                    if (system.OwnerList.Count > 0 && !system.OwnerList.Contains(Owner.loyalty))
                        message.Append("\nContested system!!!");

                    Empire.Universe.NotificationManager.AddNotification(new Notification
                    {
                        Pause = false,
                        Message = message.ToString(),
                        ReferencedItem1 = system,
                        IconPath = "Suns/" + system.SunPath,
                        Action = "SnapToExpandSystem"
                    }, "sd_ui_notification_warning");
                }
                ExplorationTarget = null;                            
            }
        }

        private bool DoExploreSystem(float elapsedTime)
        {
            SystemToPatrol = ExplorationTarget;
            if (PatrolRoute.Count == 0) 
            {                
                foreach (Planet p in SystemToPatrol.PlanetList) PatrolRoute.Add(p);
                                                
                if (SystemToPatrol.PlanetList.Count == 0) return ExploreEmptySystem(elapsedTime, SystemToPatrol);
            }
            else
            {
                PatrolTarget = PatrolRoute[stopNumber];
                if (PatrolTarget.ExploredDict[Owner.loyalty])
                {
                    stopNumber += 1;
                    if (stopNumber == PatrolRoute.Count)
                    {
                        stopNumber = 0;
                        PatrolRoute.Clear();                       
                        return true;
                    }
                }
                else
                {
                    MovePosition = PatrolTarget.Position;
                    float Distance = Owner.Center.Distance(MovePosition);
                    if (Distance < 75000f) PatrolTarget.system.ExploredDict[Owner.loyalty] = true;
                    if (Distance > 15000f)
                    {//@todo this should take longer to explore any planet. the explore speed should be based on sensors and such. 
                        if (Owner.velocityMaximum > Distance && Owner.speed >= Owner.velocityMaximum)//@todo fix this speed limiter. it makes little sense as i think it would limit the speed by a very small aoumt. 
                            Owner.speed = Distance;
                        ThrustTowardsPosition(MovePosition, elapsedTime, Owner.speed);
                    }
                    else if (Distance >= 5500f)
                    {
                        if (Owner.velocityMaximum > Distance && Owner.speed >= Owner.velocityMaximum)
                            Owner.speed = Distance;
                        ThrustTowardsPosition(MovePosition, elapsedTime, Owner.speed);
                    }
                    else
                    {
                        ThrustTowardsPosition(MovePosition, elapsedTime, Owner.speed);
                        if (Distance < 500f)
                        {
                            PatrolTarget.ExploredDict[Owner.loyalty] = true;							
                            stopNumber += 1;
                            if (stopNumber == PatrolRoute.Count)
                            {
                                stopNumber = 0;
                                PatrolRoute.Clear();
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void DoHoldPositionCombat(float elapsedTime)
        {		  
            
            if (Owner.Velocity.Length() > 0f)
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
                var angleDiff = Owner.AngleDiffTo(Owner.Velocity, out Vector2 right, out Vector2 forward);
                var facing = Owner.Velocity.Facing(right);
                if (angleDiff <= 0.2f)
                {
                    Stop(elapsedTime);
                    return;
                }
                RotateToFacing(elapsedTime, angleDiff, facing);
                return;
            }
            else
            {
                Vector2 VectorToTarget = Owner.Center.FindVectorToTarget(Target.Center);
                var angleDiff = Owner.AngleDiffTo(VectorToTarget, out Vector2 right, out Vector2 forward);
                if (angleDiff <= 0.02f)
                {
                    DeRotate();
                    return;
                }
                RotateToFacing(elapsedTime, angleDiff, VectorToTarget.Facing(right));
            }
        }
        
        private void DoLandTroop(float elapsedTime, ShipGoal goal)
        {
            if (Owner.shipData.Role != ShipData.RoleName.troop || Owner.TroopList.Count == 0)
                DoOrbit(goal.TargetPlanet, elapsedTime); //added by gremlin.

            float radius = goal.TargetPlanet.ObjectRadius + Owner.Radius * 2;
            float distCenter = goal.TargetPlanet.Position.Distance(Owner.Center);

            if (Owner.shipData.Role == ShipData.RoleName.troop && Owner.TroopList.Count > 0)
            {
                if (Owner.engineState == Ship.MoveState.Warp && distCenter < 7500f)
                    Owner.HyperspaceReturn();
                if (distCenter < radius  )
                    ThrustTowardsPosition(goal.TargetPlanet.Position, elapsedTime, Owner.speed > 200 ? Owner.speed*.90f : Owner.velocityMaximum);
                else
                    ThrustTowardsPosition(goal.TargetPlanet.Position, elapsedTime, Owner.speed);
                if (distCenter < goal.TargetPlanet.ObjectRadius && goal.TargetPlanet.AssignTroopToTile(Owner.TroopList[0]))
                        Owner.QueueTotalRemoval();
                return;
            }
            else if (Owner.loyalty == goal.TargetPlanet.Owner || goal.TargetPlanet.GetGroundLandingSpots() == 0
                     || Owner.TroopList.Count <= 0 || Owner.shipData.Role != ShipData.RoleName.troop
                     && !Owner.GetHangars().Any(hangar => hangar.IsTroopBay && hangar.hangarTimer <= 0)
                     && !Owner.hasTransporter)
            {
                if (Owner.loyalty.isPlayer)
                    HadPO = true;
                HasPriorityOrder = false;
                State = DefaultAIState;
                OrderQueue.Clear();
                Log.Info("Do Land Troop: Troop Assault Canceled");
            }
            else if (distCenter < radius)
            {
                var toRemove = new Array<Troop>();
                {
                    //Get limit of troops to land
                    int landLimit = Owner.GetHangars().Count(hangar => hangar.IsTroopBay && hangar.hangarTimer <= 0);
                    foreach (ShipModule module in Owner.Transporters.Where(module => module.TransporterTimer <= 1f))
                        landLimit += module.TransporterTroopLanding;
                    //Land troops
                    foreach (Troop troop in Owner.TroopList)
                    {
                        if (troop == null || troop.GetOwner() != Owner.loyalty)
                            continue;
                        if (goal.TargetPlanet.AssignTroopToTile(troop))
                        {
                            toRemove.Add(troop);
                            landLimit--;
                            if (landLimit < 1)
                                break;
                        }
                        else
                        {
                            break;
                        }
                    }
                    //Clear out Troops
                    if (toRemove.Count > 0)
                    {
                        bool flag; // = false;                        
                        foreach (Troop to in toRemove)
                        {
                            flag = false;
                            foreach (ShipModule module in Owner.GetHangars())
                                if (module.hangarTimer < module.hangarTimerConstant)
                                {
                                    module.hangarTimer = module.hangarTimerConstant;
                                    flag = true;
                                    break;
                                }
                            if (!flag)
                                foreach (ShipModule module in Owner.Transporters)
                                    if (module.TransporterTimer < module.TransporterTimerConstant)
                                    {
                                        module.TransporterTimer = module.TransporterTimerConstant;
                                        break;
                                    }
                            Owner.TroopList.Remove(to);
                        }   
                    }
                }
            }
        }

        private void DoNonFleetArtillery(float elapsedTime)
        {
            //Heavily modified by Gretman
            Vector2 vectorToTarget = Owner.Center.FindVectorToTarget(Target.Center);
            var angleDiff = Owner.AngleDiffTo(vectorToTarget, out Vector2 right, out Vector2 forward);
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            float adjustedRange = (Owner.maxWeaponsRange - Owner.Radius) * 0.85f;

            if (distanceToTarget > adjustedRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.speed);
                return;
            }
            else if (distanceToTarget < Owner.Radius)
            {
                Owner.Velocity = Owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Owner.GetSTLSpeed());
            }
            else
            {
                Owner.Velocity *= 0.995f;        //Small propensity to not drift
            }

            if (angleDiff <= 0.02f)
            {
                DeRotate();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, vectorToTarget.Facing(right));
        }

        private void DoNonFleetBroadsideRight(float elapsedTime)
        {   
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            if (distanceToTarget > Owner.maxWeaponsRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.speed);
                return;
            }
            if (distanceToTarget < Owner.maxWeaponsRange * 0.70f && Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Target.Center) < distanceToTarget)
            {
                Owner.Velocity = Vector2.Zero;
            }
            Vector2 vectorToTarget = Owner.Center.FindVectorToTarget(Target.Center);
            var angleDiff = Owner.AngleDiffTo(vectorToTarget, out Vector2 right, out Vector2 forward);
            if (angleDiff <= 0.02f)
            {
                DeRotate();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, vectorToTarget.Facing(right));
        }

        private void DoNonFleetBroadsideLeft(float elapsedTime)
        {
            var forward = new Vector2((float)Math.Sin(Owner.Rotation), -(float)Math.Cos(Owner.Rotation));
            var left = new Vector2(forward.Y, -forward.X);
            Vector2 vectorToTarget = Owner.Center.FindVectorToTarget(Target.Center); 
            var angleDiff = (float)Math.Acos(Vector2.Dot(vectorToTarget, left));            
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            if (distanceToTarget > Owner.maxWeaponsRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.speed);
                return;
            }
            if (distanceToTarget < Owner.maxWeaponsRange * 0.70f &&
                (Owner.Center + Owner.Velocity * elapsedTime).InRadius(Target.Center, distanceToTarget))
                Owner.Velocity = Vector2.Zero;
            if (angleDiff <= 0.02f)
            {
                DeRotate();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, vectorToTarget.Facing(forward)) ;
        }
        
        private void DoOrbit(Planet orbitTarget, float elapsedTime)  //fbedard: my version of DoOrbit, fastest possible?
        {            
            if (Owner.velocityMaximum < 1)
                return;
            float distance = orbitTarget.Position.Distance(Owner.Center);
            if(Owner.shipData.ShipCategory == ShipData.Category.Civilian && distance < Empire.ProjectorRadius * 2)
            {
                OrderMoveTowardsPosition(OrbitPos, 0, Vector2.Zero, false, OrbitTarget);
                OrbitPos = orbitTarget.Position;
                return;
            }

            if (distance > 15000f)
            {
                ThrustTowardsPosition(orbitTarget.Position, elapsedTime, Owner.speed);
                OrbitPos = orbitTarget.Position;
                return;
            }
            findNewPosTimer -= elapsedTime;
            if (findNewPosTimer <= 0f)
            {
                float radius = orbitTarget.ObjectRadius + Owner.Radius + 1200f;
                float distanceToOrbitSpot = Owner.Center.Distance(OrbitPos);
                if (distanceToOrbitSpot <= radius || Owner.speed < 1f)
                {                    
                    OrbitalAngle += ((float)Math.Asin(Owner.yBankAmount * 10f)).ToDegrees();
                    if (OrbitalAngle >= 360f)
                        OrbitalAngle -= 360f;
                }
                findNewPosTimer =  elapsedTime * 10f;
                OrbitPos = orbitTarget.Position.PointOnCircle(OrbitalAngle, radius);
            }            

            if (distance < 7500f)
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
                if (State != AIState.Bombard)
                    HasPriorityOrder = false;
            }
            if (distance < 1500f + orbitTarget.ObjectRadius)
                ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.speed > 300f ? 300f : Owner.speed);
            else
                ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.speed);
        }
        
        //do troop rebase
        private void DoRebase(ShipGoal Goal)
        {
            if (Owner.TroopList.Count == 0)
            {
                Owner.QueueTotalRemoval();
            }
            else if (Goal.TargetPlanet.AssignTroopToTile(Owner.TroopList[0]))
            {
                Owner.TroopList.Clear();
                Owner.QueueTotalRemoval();
                return;
            }
            else
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
            }
        }

        //added by gremlin refit while in fleet
        //do refit 
        private void DoRefit(float elapsedTime, ShipGoal goal)
        {
            QueueItem qi = new BuildShip(goal);
            if (qi.sData == null)
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
            }
            var cost = (int)(ResourceManager.ShipsDict[goal.VariableString].GetCost(Owner.loyalty) - Owner.GetCost(Owner.loyalty));
            if (cost < 0)
                cost = 0;
            cost = cost + 10 * (int)UniverseScreen.GamePaceStatic;
            if (Owner.loyalty.isFaction)
                qi.Cost = 0;
            else
                qi.Cost = (float)cost;
            qi.isRefit = true;
            //Added by McShooterz: refit keeps name and level
            if(Owner.VanityName != Owner.Name)
                qi.RefitName = Owner.VanityName;
            qi.sData.Level = (byte)Owner.Level;
            if (Owner.fleet != null)
            {
                Goal refitgoal = new FleetRequisition(goal,this);
                node.GoalGUID = refitgoal.guid;
                Owner.loyalty.GetGSAI().Goals.Add(refitgoal);
                qi.Goal = refitgoal;
            }
            OrbitTarget.ConstructionQueue.Add(qi);
            Owner.QueueTotalRemoval();
        }
        //do repair drone
        private void DoRepairDroneLogic(Weapon w)
        {
            // @todo Refactor this bloody mess
            //Turns out the ship was used to get a vector to the target ship and not actually used for any kind of targeting. 
            
            Ship friendliesNearby =null;
            using (FriendliesNearby.AcquireReadLock())
            {
                foreach (Ship ship in FriendliesNearby)
                {
                    if (!ship.Active || ship.Health > ship.HealthMax * 0.95f
                        || !Owner.Center.InRadius(ship.Center, 20000f))
                        continue;
                    friendliesNearby = ship;
                    break;
                }
                if (friendliesNearby == null) return;
                Vector2 target = w.Center.FindVectorToTarget(friendliesNearby.Center);
                target.Y = target.Y * -1f;
                w.FireDrone(Vector2.Normalize(target));
            }            
        }
        //do repair beam
        private void DoRepairBeamLogic(Weapon w)
        {
            var repairMe = FriendliesNearby.FindMinFiltered
            (filter: ship => ship.Active && ship != w.Owner
                             && ship.Health / ship.HealthMax < .9f
                             && Owner.Center.Distance(ship.Center) <= w.Range + 500f,
                selector: ship => ship.Health);
            
            if(repairMe != null) w.FireTargetedBeam(repairMe);            
        }
        //do ordinance transporter @TODO move to module and cleanup. this is a mod only method. Low priority
        private void DoOrdinanceTransporterLogic(ShipModule module)
        {
            Ship repairMe =
                module.GetParent()
                    .loyalty.GetShips()
                    .FindMinFiltered(
                        filter: ship => Owner.Center.Distance(ship.Center) <= module.TransporterRange + 500f
                                        && ship.Ordinance < ship.OrdinanceMax && !ship.hasOrdnanceTransporter,
                        selector: ship => ship.Ordinance);
            if (repairMe != null)
            {
                module.TransporterTimer = module.TransporterTimerConstant;
                var TransferAmount = 0f;
                //check how much can be taken
                if (module.TransporterOrdnance > module.GetParent().Ordinance)
                    TransferAmount = module.GetParent().Ordinance;
                else
                    TransferAmount = module.TransporterOrdnance;
                //check how much can be given
                if (TransferAmount > repairMe.OrdinanceMax - repairMe.Ordinance)
                    TransferAmount = repairMe.OrdinanceMax - repairMe.Ordinance;
                //Transfer
                repairMe.Ordinance += TransferAmount;
                module.GetParent().Ordinance -= TransferAmount;
                module.GetParent().PowerCurrent -= module.TransporterPower * (TransferAmount / module.TransporterOrdnance);
                if (Owner.InFrustum && ResourceManager.SoundEffectDict.ContainsKey("transporter"))
                {
                    GameplayObject.audioListener.Position = Empire.Universe.camPos;
                    AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict["transporter"], GameplayObject.audioListener, module.GetParent().emitter, 0.5f);
                }
                return;
            }                
        }
        //do transporter assault  @TODO move to module and cleanup. this is a mod only method. Low priority
        private void DoAssaultTransporterLogic(ShipModule module)
        {
            var ship =
                NearbyShips.FindMinFiltered(
                    filter:
                    Ship =>
                        Ship.ship.loyalty != null && Ship.ship.loyalty != Owner.loyalty && Ship.ship.shield_power <= 0
                        && Owner.Center.Distance(Ship.ship.Center) <= module.TransporterRange + 500f,
                    selector: Ship => Owner.Center.SqDist(Ship.ship.Center));            
                if (ship != null)
                {
                    byte TroopCount = 0;
                    var Transported = false;
                    for (byte i = 0; i < Owner.TroopList.Count(); i++)
                    {
                        if (Owner.TroopList[i] == null)
                            continue;
                        if (Owner.TroopList[i].GetOwner() == Owner.loyalty)
                        {
                            ship.ship.TroopList.Add(Owner.TroopList[i]);
                            Owner.TroopList.Remove(Owner.TroopList[i]);
                            TroopCount++;
                            Transported = true;
                        }
                        if (TroopCount == module.TransporterTroopAssault)
                            break;
                    }
                    if (Transported)//@todo audio should not be here
                    {
                        module.TransporterTimer = module.TransporterTimerConstant;
                        if (Owner.InFrustum && ResourceManager.SoundEffectDict.ContainsKey("transporter"))
                        {
                            GameplayObject.audioListener.Position = Empire.Universe.camPos;
                            AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict["transporter"], GameplayObject.audioListener, module.GetParent().emitter, 0.5f);
                        }
                        return;
                    }
                }
        }

        //do hangar return
        private void DoReturnToHangar(float elapsedTime)
        {
            if (Owner.Mothership == null || !Owner.Mothership.Active)
            {
                OrderQueue.Clear();
                return;
            }
            ThrustTowardsPosition(Owner.Mothership.Center, elapsedTime, Owner.speed);			
            if (Owner.Center.InRadius(Owner.Mothership.Center, Owner.Mothership.Radius + 300f))
            {
                if (Owner.Mothership.TroopCapacity > Owner.Mothership.TroopList.Count && Owner.TroopList.Count == 1)
                    Owner.Mothership.TroopList.Add(Owner.TroopList[0]);
                if (Owner.shipData.Role == ShipData.RoleName.supply)  //fbedard: Supply ship return with Ordinance
                    Owner.Mothership.Ordinance += Owner.Ordinance;
                Owner.Mothership.Ordinance += Owner.Mass / 5f;        //fbedard: New spawning cost
                if (Owner.Mothership.Ordinance > Owner.Mothership.OrdinanceMax)
                    Owner.Mothership.Ordinance = Owner.Mothership.OrdinanceMax;
                Owner.QueueTotalRemoval();
                foreach (ShipModule hangar in Owner.Mothership.GetHangars())
                {
                    if (hangar.GetHangarShip() != Owner)
                        continue;
                    //added by gremlin: prevent fighters from relaunching immediatly after landing.
                    float ammoReloadTime = Owner.OrdinanceMax * .1f;
                    float shieldrechargeTime = Owner.shield_max * .1f;
                    float powerRechargeTime = Owner.PowerStoreMax * .1f;
                    float rearmTime = Owner.Health;
                    rearmTime += Owner.Ordinance*.1f;
                    rearmTime += Owner.PowerCurrent * .1f;
                    rearmTime += Owner.shield_power * .1f;
                    rearmTime /= Owner.HealthMax + ammoReloadTime + shieldrechargeTime + powerRechargeTime;                    
                    rearmTime = (1.01f - rearmTime) * (hangar.hangarTimerConstant *(1.01f- (Owner.Level + hangar.GetParent().Level)/10 ));  // fbedard: rearm time from 50% to 150%
                    if (rearmTime < 0)
                        rearmTime = 1;
                    //CG: if the fighter is fully functional reduce rearm time to very little. The default 5 minute hangar timer is way too high. It cripples fighter usage.
                    //at 50% that is still 2.5 minutes if the fighter simply launches and returns. with lag that can easily be 10 or 20 minutes. 
                    //at 1.01 that should be 3 seconds for the default hangar.
                    hangar.SetHangarShip(null);
                    hangar.hangarTimer = rearmTime;
                    hangar.HangarShipGuid = Guid.Empty;                   
                }
            }
        }
        //do supply ship
        private void DoSupplyShip(float elapsedTime, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                OrderQueue.Clear();
                OrderResupplyNearest(false);
                return;
            }
            if (EscortTarget.AI.State == AIState.Resupply || EscortTarget.AI.State == AIState.Scrap ||
                EscortTarget.AI.State == AIState.Refit)
            {
                OrderQueue.Clear();
                OrderResupplyNearest(false);
                return;
            }
            ThrustTowardsPosition(EscortTarget.Center, elapsedTime, Owner.speed);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))                
            {
                if (EscortTarget.Ordinance + Owner.Ordinance > EscortTarget.OrdinanceMax)
                    Owner.Ordinance = EscortTarget.OrdinanceMax - EscortTarget.Ordinance;
                EscortTarget.Ordinance += Owner.Ordinance;
                Owner.Ordinance -= Owner.Ordinance;
                OrderQueue.Clear();
                if (Owner.Ordinance > 0)
                    State = AIState.AwaitingOrders;
                else
                    Owner.ReturnToHangar();
            }
        }
        // do system defense
        private void DoSystemDefense(float elapsedTime)
        {
            SystemToDefend=SystemToDefend ?? Owner.System;                 
            if (SystemToDefend == null ||  awaitClosest?.Owner == Owner.loyalty)
                AwaitOrders(elapsedTime);
            else
                OrderSystemDefense(SystemToDefend);              
        }
        //do troop board ship
        private void DoTroopToShip(float elapsedTime)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                OrderQueue.Clear();
                return;
            }
            MoveTowardsPosition(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity > EscortTarget.TroopList.Count)
                {
                    EscortTarget.TroopList.Add(Owner.TroopList[0]);
                    Owner.QueueTotalRemoval();
                    return;
                }
                OrbitShip(EscortTarget, elapsedTime);
            }
        }

        //trade goods drop off
        private void DropoffGoods()
        {
            if (end != null)
            {
                if (Owner.loyalty.data.Traits.Mercantile > 0f)
                    Owner.loyalty.AddTradeMoney(Owner.CargoSpaceUsed * Owner.loyalty.data.Traits.Mercantile);

                end.FoodHere       += Owner.UnloadFood(end.MAX_STORAGE - end.FoodHere);
                end.ProductionHere += Owner.UnloadProduction(end.MAX_STORAGE - end.ProductionHere);
                end = null;
            }
            start = null;
            OrderQueue.RemoveFirst();
            OrderTrade(5f);
        }

        //trade passengers drop off
        private void DropoffPassengers()
        {
            if (end == null)
            {
                OrderQueue.RemoveFirst();
                OrderTransportPassengers(0.1f);
                return;
            }

            float maxPopulation = end.MaxPopulation + end.MaxPopBonus;
            end.Population += Owner.UnloadColonists(maxPopulation - end.Population);

            OrderQueue.RemoveFirst();
            start = null;
            end   = null;
            OrderTransportPassengers(5f);
        }

        //explore system
        private bool ExploreEmptySystem(float elapsedTime, SolarSystem system)
        {
            if (system.ExploredDict[Owner.loyalty])
                return true;
            MovePosition = system.Position;
            if (Owner.Center.InRadius(MovePosition, 75000f))
            {
                system.ExploredDict[Owner.loyalty] = true;
                return true;
            }
            ThrustTowardsPosition(MovePosition, elapsedTime, Owner.speed);
            return false;
        }

        // fire on target
        public void FireOnTarget()
        {
            try
            {
                TargetShip = Target as Ship;
                //Relationship enemy =null;
                //base reasons not to fire. @TODO actions decided by loyalty like should be the same in all areas. 
                if (!Owner.hasCommand || Owner.engineState == Ship.MoveState.Warp || Owner.disabled || Owner.Weapons.Count == 0)
                    return;

                var hasPD = false;
                //Determine if there is something to shoot at
                if (BadGuysNear)
                {
                    //Target is dead or dying, will need a new one.
                    if ((Target?.Active ?? false)==false ||  (TargetShip?.dying ??false))
                    {
                        foreach (Weapon purge in Owner.Weapons)
                        {
                            if (purge.Tag_PD || purge.TruePD)
                                hasPD = true;
                            if (purge.PrimaryTarget)
                            {
                                purge.PrimaryTarget = false;
                                purge.fireTarget = null;
                                purge.SalvoTarget = null;
                            }
                        }
                        Target = null;
                        TargetShip = null;
                    }
                    foreach (Weapon purge in Owner.Weapons)
                    {
                        if (purge.Tag_PD || purge.TruePD)
                            hasPD = true;
                        else continue;
                        break;
                    }
                    TrackProjectiles.Clear(); 
                    if (Owner.Mothership != null)
                        TrackProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
                    if (Owner.TrackingPower > 0 && hasPD) //update projectile list                         
                    {
                        foreach (Projectile missile in Owner.GetNearby<Projectile>())
                        {
                            if (missile.Loyalty != Owner.loyalty && missile.Weapon.Tag_Intercept)
                                TrackProjectiles.Add(missile);
                        }
                        TrackProjectiles.Sort(missile => Owner.Center.SqDist(missile.Center));
                    }
       
                    float lag = Empire.Universe.Lag;
                    //Go through each weapon
                    float index = 0; //count up weapons.
                    //save target ship if it is a ship.
                    TargetShip = Target as Ship;
                    //group of weapons into chunks per thread available
                    //var source = Enumerable.Range(0, Owner.Weapons.Count).ToArray();
                    //        var rangePartitioner = Partitioner.Create(0, source.Length);
                    //handle each weapon group in parallel
                            //System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (range, loopState) =>
                    //Parallel.For(Owner.Weapons.Count, (start, end) =>
                    {
                        //standard for loop through each weapon group.
                        for (int T = 0; T < Owner.Weapons.Count; T++)
                        {
                            Weapon weapon = Owner.Weapons[T];
                            weapon.TargetChangeTimer -= 0.0167f;
                            //Reasons for this weapon not to fire 
                            if ( !weapon.moduleAttachedTo.Active 
                                || weapon.timeToNextFire > 0f 
                                || !weapon.moduleAttachedTo.Powered || weapon.IsRepairDrone || weapon.isRepairBeam
                                || weapon.PowerRequiredToFire > Owner.PowerCurrent
                                || weapon.TargetChangeTimer >0
                                )
                                continue;
                            if ((!weapon.TruePD || !weapon.Tag_PD) && Owner.isPlayerShip())
                                continue;
                            var moduletarget = weapon.fireTarget as ShipModule;
                            //if firing at the primary target mark weapon as firing on primary.
                            if (!(weapon.fireTarget is Projectile) && weapon.fireTarget != null && (weapon.fireTarget == Target || moduletarget != null && moduletarget.GetParent() as GameplayObject == Target))
                                weapon.PrimaryTarget = true;                                                   
                            //check if weapon target as a gameplay object is still a valid target    
                            if (weapon.fireTarget !=null )
                                if (weapon.fireTarget !=null && !Owner.CheckIfInsideFireArc(weapon, weapon.fireTarget)                                                           
                                    //check here if the weapon can fire on main target.                                                           
                                    || Target != null && weapon.SalvoTimer <=0 && weapon.BeamDuration <=0 && !weapon.PrimaryTarget && !(weapon.fireTarget is Projectile) && Owner.CheckIfInsideFireArc(weapon, Target)                                                         
                                )
                                {
                                    weapon.TargetChangeTimer = .1f * weapon.moduleAttachedTo.XSIZE * weapon.moduleAttachedTo.YSIZE;
                                    weapon.fireTarget = null;
                                    if (weapon.isTurret)
                                        weapon.TargetChangeTimer *= .5f;
                                    if(weapon.Tag_PD)
                                        weapon.TargetChangeTimer *= .5f;
                                    if (weapon.TruePD)
                                        weapon.TargetChangeTimer *= .25f;
                                }
                            //if weapon target is null reset primary target and decrement target change timer.
                            if (weapon.fireTarget == null && !Owner.isPlayerShip())
                                weapon.PrimaryTarget = false;
                            //Reasons for this weapon not to fire                    
                            if (weapon.fireTarget == null && weapon.TargetChangeTimer >0 ) 
                                continue;
                            //main targeting loop. little check here to disable the whole thing for debugging.
                            if (true)
                            {
                                //Can this weapon fire on ships
                                if (BadGuysNear && !weapon.TruePD)
                                {
                                    //if there are projectile to hit and weapons that can shoot at them. do so. 
                                    if (TrackProjectiles.Count > 0 && weapon.Tag_PD)
                                        for (var i = 0;
                                            i < TrackProjectiles.Count &&
                                            i < Owner.TrackingPower + Owner.Level;
                                            i++)
                                        {
                                            Projectile proj;
                                            {
                                                proj = TrackProjectiles[i];
                                            }

                                            if (proj == null || !proj.Active || proj.Health <= 0 ||
                                                !proj.Weapon.Tag_Intercept)
                                                continue;
                                            if (Owner.CheckIfInsideFireArc(weapon,
                                                proj as GameplayObject))
                                            {
                                                weapon.fireTarget = proj;
                                                //AddTargetsTracked++;
                                                break;
                                            }
                                        }
                                    //Is primary target valid
                                    if (weapon.fireTarget == null)
                                        if (Owner.CheckIfInsideFireArc(weapon, Target))
                                        {
                                            weapon.fireTarget = Target;
                                            weapon.PrimaryTarget = true;
                                        }

                                    //Find alternate target to fire on
                                    //this seems to be very expensive code. 
                                    if (true)
                                        if (weapon.fireTarget == null && Owner.TrackingPower > 0)
                                        {
                                            //limit to one target per level.
                                            int tracking = Owner.TrackingPower;
                                            for (var i = 0;
                                                i < PotentialTargets.Count &&
                                                i < tracking + Owner.Level;
                                                i++) //
                                            {
                                                Ship potentialTarget = PotentialTargets[i];
                                                if (potentialTarget == TargetShip)
                                                {
                                                    tracking++;
                                                    continue;
                                                }
                                                if (
                                                    !Owner.CheckIfInsideFireArc(weapon,
                                                        potentialTarget))
                                                    continue;
                                                weapon.fireTarget = potentialTarget;
                                                //AddTargetsTracked++;
                                                break;

                                            }
                                        }
                                    //If a ship was found to fire on, change to target an internal module if target is visible  || weapon.Tag_Intercept
                                    if (weapon.fireTarget is Ship &&
                                        (GlobalStats.ForceFullSim || Owner.InFrustum ||
                                        (weapon.fireTarget as Ship).InFrustum))
                                        weapon.fireTarget =
                                            (weapon.fireTarget as Ship).GetRandomInternalModule(
                                                weapon);
                                }
                                //No ship to target, check for projectiles
                                if (weapon.fireTarget == null && weapon.Tag_PD)

                                    for (var i = 0;
                                        i < TrackProjectiles.Count &&
                                        i < Owner.TrackingPower + Owner.Level;
                                        i++)
                                    {
                                        Projectile proj;
                                        proj = TrackProjectiles[i];


                                        if (proj == null || !proj.Active || proj.Health <= 0 ||
                                            !proj.Weapon.Tag_Intercept)
                                            continue;
                                        if (Owner.CheckIfInsideFireArc(weapon,
                                            proj as GameplayObject))
                                        {
                                            weapon.fireTarget = proj;
                                            break;
                                        }
                                    }
                            }
                        }
                    }//);
                    //this section actually fires the weapons. This whole firing section can be moved to some other area of the code. This code is very expensive. 
                    if(true)
                    foreach (Weapon weapon in Owner.Weapons)
                        if (weapon.fireTarget != null && weapon.moduleAttachedTo.Active && weapon.timeToNextFire <= 0f && weapon.moduleAttachedTo.Powered)
                            if (!(weapon.fireTarget is Ship))
                            {
                                GameplayObject target = weapon.fireTarget;
                                if (weapon.isBeam)
                                {
                                    weapon.FireTargetedBeam(target);
                                }
                                else if (weapon.Tag_Guided)
                                {
                                    if (index > 10 && lag > .05 && !GlobalStats.ForceFullSim && !weapon.Tag_Intercept && weapon.fireTarget is ShipModule)
                                        FireOnTargetNonVisible(weapon, (weapon.fireTarget as ShipModule).GetParent());
                                    else
                                        weapon.Fire(new Vector2((float)Math.Sin((double)Owner.Rotation + weapon.moduleAttachedTo.Facing.ToRadians()), -(float)Math.Cos((double)Owner.Rotation + weapon.moduleAttachedTo.Facing.ToRadians())), target);
                                    index++;
                                }
                                else
                                {
                                    if (index > 10 && lag > .05 && !GlobalStats.ForceFullSim && weapon.fireTarget is ShipModule)
                                        FireOnTargetNonVisible(weapon, (weapon.fireTarget as ShipModule).GetParent());
                                    else
                                        CalculateAndFire(weapon, target, false);
                                    index++;
                                }
                            }
                            else
                            {
                                FireOnTargetNonVisible(weapon, weapon.fireTarget);
                            }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "FireOnTarget() crashed");
            }
            TargetShip = null;
        }

        // fire on calculate and fire
        public void CalculateAndFire(Weapon weapon, GameplayObject target, bool SalvoFire)
        {
            GameplayObject realTarget = (target is ShipModule sm) ? sm.GetParent() : target;

            Vector2 predictedPos = weapon.Center.FindPredictedTargetPosition(
                weapon.Owner.Velocity, weapon.ProjectileSpeed, target.Center, realTarget.Velocity);

            if (Owner.CheckIfInsideFireArc(weapon, predictedPos))
            {
                Vector2 direction = (predictedPos - weapon.Center).Normalized(); 
                if (SalvoFire) weapon.FireSalvo(direction, target);
                else           weapon.Fire(direction, target);
            }
        }

        // fire on non visible
        private void FireOnTargetNonVisible(Weapon w, GameplayObject fireTarget)
        {
            if (Owner.Ordinance < w.OrdinanceRequiredToFire || Owner.PowerCurrent < w.PowerRequiredToFire)
                return;
            w.timeToNextFire = w.fireDelay;
            if (w.IsRepairDrone)
                return;
            if (TargetShip == null || !TargetShip.Active || TargetShip.dying || !w.TargetValid(TargetShip.shipData.Role)
                || TargetShip.engineState == Ship.MoveState.Warp || !Owner.CheckIfInsideFireArc(w, TargetShip))
                return;

            Owner.Ordinance    -= w.OrdinanceRequiredToFire;
            Owner.PowerCurrent -= w.PowerRequiredToFire;
            Owner.PowerCurrent -= w.BeamPowerCostPerSecond * w.BeamDuration;
            Owner.InCombatTimer = 15f;

            if (fireTarget is Projectile)
            {
                fireTarget.Damage(w.Owner, w.DamageAmount);
                return;
            }
            if (!(fireTarget is Ship targetShip))
            {
                if (fireTarget is ShipModule shipModule)
                {
                    w.timeToNextFire = w.fireDelay;
                    shipModule.GetParent().FindClosestExternalModule(Owner.Center).Damage(Owner, w.InvisibleDamageAmount);
                }
                return;
            }
            w.timeToNextFire = w.fireDelay;
            if (targetShip.ExternalSlots.IsEmpty)
            {
                targetShip.Die(null, true);
                return;
            }//@todo invisible ecm and such should match visible

            if ((fireTarget as Ship).AI.CombatState == CombatState.Evade)   //fbedard: firing on evading ship can miss !
                if (RandomMath.RandomBetween(0f, 100f) < 5f + targetShip.experience)
                    return;


            if (targetShip.shield_power > 0f)
            {
                Array<ShipModule> shields = targetShip.GetShields();
                for (int i = 0; i < shields.Count; ++i)
                {
                    ShipModule shield = shields[i];
                    if (shield.Active && shield.ShieldPower > 0f)
                    {
                        shield.Damage(Owner, w.InvisibleDamageAmount);
                        return;
                    }
                }
                return;
            }

            ShipModule closestExtSlot = targetShip.FindClosestExternalModule(Owner.Center);
            if (closestExtSlot == null)
                return;
            ShipModule unshieldedModule = targetShip.FindUnshieldedExternalModule(closestExtSlot.quadrant);
            unshieldedModule?.Damage(Owner, w.InvisibleDamageAmount);
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
            findNewPosTimer = 0f;
            GotoStep = 0;
            HasPriorityOrder = true;
            MovePosition.X = p.Position.X;
            MovePosition.Y = p.Position.Y;
        }
        //movement goto
        public void GoTo(Vector2 movePos, Vector2 facing)
        {
            GotoStep = 0;
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            MovePosition.X = movePos.X;
            MovePosition.Y = movePos.Y;
            FinalFacingVector = facing;
            State = AIState.MoveTo;
        }
        //movement hold posistion
        public void HoldPosition()
        {
                if (Owner.isSpooling || Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
            State = AIState.HoldPosition;
                Owner.isThrusting = false;
        }
        //movement final approach
        private void MakeFinalApproach(float elapsedTime, ShipGoal Goal)
        {
            if (Goal.TargetPlanet != null)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Last().Equals(Goal.TargetPlanet.Position);
                    Goal.MovePosition = Goal.TargetPlanet.Position;
                }
            Owner.HyperspaceReturn();
            Vector2 velocity = Owner.Velocity;
            if (Goal.TargetPlanet != null)
                velocity += Goal.TargetPlanet.Position;
            float timetostop = velocity.Length() / Goal.SpeedLimit;
            float Distance = Owner.Center.Distance(Goal.MovePosition);
            if (Distance / (Goal.SpeedLimit + 0.001f) <= timetostop)
            {
                OrderQueue.RemoveFirst();
            }
            else
            {
                if (DistanceLast == Distance)
                    Goal.SpeedLimit++;
                ThrustTowardsPosition(Goal.MovePosition, elapsedTime, Goal.SpeedLimit);
            }
            DistanceLast = Distance;
        }
        //added by gremlin Deveksmod MakeFinalApproach
        private void MakeFinalApproachDev(float elapsedTime, ShipGoal Goal)
        {
            float speedLimit = (int)Goal.SpeedLimit;

            Owner.HyperspaceReturn();
            Vector2 velocity = Owner.Velocity;
            float distance = Vector2.Distance(Owner.Center, Goal.MovePosition);

            float timetostop = velocity.Length() / speedLimit;

            if (distance / velocity.Length() <= timetostop)
            {
                OrderQueue.RemoveFirst();
            }
            else
            {
                Goal.SpeedLimit = speedLimit;

                ThrustTowardsPosition(Goal.MovePosition, elapsedTime, speedLimit);
            }
            DistanceLast = distance;
        }
        //movement final approach
        private void MakeFinalApproachFleet(float elapsedTime, ShipGoal Goal)
        {
            float distance = Owner.Center.Distance(Goal.fleet.Position + Owner.FleetOffset);
            if (distance < 100f || DistanceLast > distance)
                OrderQueue.RemoveFirst();
            else
                MoveTowardsPosition(Goal.fleet.Position + Owner.FleetOffset, elapsedTime, Goal.fleet.Speed);
            DistanceLast = distance;
        }
        //movement in direction
        private void MoveInDirection(Vector2 direction, float elapsedTime)
        {
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Vector2.Normalize(direction);			
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);			    
                float facing = wantedForward.Facing(right);
                if (angleDiff > 0.22f)
                {
                    Owner.isTurning = true;
                    float rotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (Math.Abs(rotAmount) > angleDiff)
                        rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
                    if (rotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (rotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship ship = Owner;
                        ship.yRotation = ship.yRotation + Owner.yBankAmount;
                    }
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + rotAmount;
                }
                else if (Owner.yRotation > 0f)
                {
                    Ship owner1 = Owner;
                    owner1.yRotation = owner1.yRotation - Owner.yBankAmount;
                    if (Owner.yRotation < 0f)
                        Owner.yRotation = 0f;
                }
                else if (Owner.yRotation < 0f)
                {
                    Ship ship1 = Owner;
                    ship1.yRotation = ship1.yRotation + Owner.yBankAmount;
                    if (Owner.yRotation > 0f)
                        Owner.yRotation = 0f;
                }
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * Owner.speed);
                if (Owner.Velocity.Length() > Owner.velocityMaximum)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Owner.velocityMaximum;
            }
        }
    
        //movement to posisiton
        private void MoveTowardsPosition(Vector2 Position, float elapsedTime)
        {
            if (Owner.Center.Distance(Position) < 50f)
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            Position = Position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Owner.Center.FindVectorToTarget(Position);				
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
                float facing = wantedForward.Facing(right);
                float distance = Vector2.Distance(Position, Owner.Center);

                if (angleDiff > 0.02f)
                {
                    float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (RotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship ship = Owner;
                        ship.yRotation = ship.yRotation + Owner.yBankAmount;
                    }
                    Owner.isTurning = true;
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + RotAmount;
                }
                float speedLimit = Owner.speed;
                if (Owner.isSpooling)
                    speedLimit = speedLimit * Owner.loyalty.data.FTLModifier;
                else if (distance < speedLimit)
                    speedLimit = distance * 0.75f;
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                if (Owner.Velocity.Length() > speedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
            }
        }
        /// <summary>
        /// movement to posistion
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="speedLimit"></param>
        private void MoveTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit)
        {
            if (speedLimit < 1f)
                speedLimit = 200f;
            Position = Position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Owner.Center.FindVectorToTarget(Position);
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
                float facing = wantedForward.Facing(right);
                if (angleDiff > 0.02f)
                {
                    float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (RotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship ship = Owner;
                        ship.yRotation = ship.yRotation + Owner.yBankAmount;
                    }
                    Owner.isTurning = true;
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + RotAmount;
                }
                if (Owner.isSpooling)
                    speedLimit = speedLimit * Owner.loyalty.data.FTLModifier;
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                if (Owner.Velocity.Length() > speedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
            }
        }
        //order movement 1000
        private void MoveToWithin1000(float elapsedTime, ShipGoal goal)
        {

            var distWaypt = 15000f; //fbedard
            if (ActiveWayPoints.Count > 1)  
                distWaypt = Empire.ProjectorRadius / 2f;

            if (OrderQueue.NotEmpty && OrderQueue[1].Plan != Plan.MoveToWithin1000 && goal.TargetPlanet != null)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Last().Equals(goal.TargetPlanet.Position);
                    goal.MovePosition = goal.TargetPlanet.Position;
                }
            float speedLimit =  (int)Owner.speed  ;
            float distance = Owner.Center.Distance(goal.MovePosition);
            if (ActiveWayPoints.Count <= 1)
                if (distance  < Owner.speed)
                    speedLimit = distance;
            ThrustTowardsPosition(goal.MovePosition, elapsedTime, speedLimit);
            if (ActiveWayPoints.Count <= 1)
            {
                if (distance <= 1500f)
                    lock (WayPointLocker)
                    {
                        if (ActiveWayPoints.Count > 1)
                            ActiveWayPoints.Dequeue();
                        if (OrderQueue.NotEmpty)
                            OrderQueue.RemoveFirst();
                    }
            }
            else if (Owner.engineState == Ship.MoveState.Warp)
            {
                if (distance <= distWaypt)
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Dequeue();
                        if (OrderQueue.NotEmpty)
                            OrderQueue.RemoveFirst();
                    }
            }
            else if (distance <= 1500f)
            {
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Dequeue();
                    if (OrderQueue.NotEmpty)
                        OrderQueue.RemoveFirst();
                }
            }
        }
        //order movement fleet 1000
        private void MoveToWithin1000Fleet(float elapsedTime, ShipGoal goal)
        {
            float Distance = Vector2.Distance(Owner.Center, goal.fleet.Position + Owner.FleetOffset);
            float speedLimit = goal.SpeedLimit;
            if (Owner.velocityMaximum >= Distance)
                speedLimit = Distance;

            if (Distance > 10000f)
            {
                Owner.EngageStarDrive();
            }
            else if (Distance < 1000f)
            {
                Owner.HyperspaceReturn();
                OrderQueue.RemoveFirst();
                return;
            }
            MoveTowardsPosition(goal.fleet.Position + Owner.FleetOffset, elapsedTime, speedLimit);
        }
        //order orbit ship
        private void OrbitShip(Ship ship, float elapsedTime)
        {
            OrbitPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
            if (Vector2.Distance(OrbitPos, Owner.Center) < 1500f)
            {
                ArtificialIntelligence orbitalAngle = this;
                orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle + 15f;
                if (OrbitalAngle >= 360f)
                {
                    ArtificialIntelligence artificialIntelligence = this;
                    artificialIntelligence.OrbitalAngle = artificialIntelligence.OrbitalAngle - 360f;
                }
                OrbitPos = ship.Position.PointOnCircle(OrbitalAngle, 2500f);
            }
            ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.speed);
        }
        //order orbit ship
        private void OrbitShipLeft(Ship ship, float elapsedTime)
        {
            OrbitPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
            if (Vector2.Distance(OrbitPos, Owner.Center) < 1500f)
            {
                ArtificialIntelligence orbitalAngle = this;
                orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle - 15f;
                if (OrbitalAngle >= 360f)
                {
                    ArtificialIntelligence artificialIntelligence = this;
                    artificialIntelligence.OrbitalAngle = artificialIntelligence.OrbitalAngle - 360f;
                }
                OrbitPos = ship.Position.PointOnCircle(OrbitalAngle, 2500f);
            }
            ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.speed);
        }
        //order movement stop
        public void OrderAllStop()
        {
            OrderQueue.Clear();
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.HoldPosition;
            HasPriorityOrder = false;
            var stop = new ShipGoal(Plan.Stop, Vector2.Zero, 0f);            
            OrderQueue.Enqueue(stop);
        }

    
        //order target ship
        public void OrderAttackSpecificTarget(Ship toAttack)
        {
            TargetQueue.Clear();
            
            if (toAttack == null)
                return;

            if (Owner.loyalty.TryGetRelations(toAttack.loyalty, out Relationship relations))
            {
                if (!relations.Treaty_Peace)
                {
                    if (State == AIState.AttackTarget && Target == toAttack)
                        return;
                    if (State == AIState.SystemDefender && Target == toAttack)
                        return;
                    if (Owner.Weapons.Count == 0 || Owner.shipData.Role == ShipData.RoleName.troop)
                    {
                        OrderInterceptShip(toAttack);
                        return;
                    }
                    Intercepting = true;
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Clear();
                    }
                    State = AIState.AttackTarget;
                    Target = toAttack;
                    Owner.InCombatTimer = 15f;
                    OrderQueue.Clear();
                    IgnoreCombat = false;
                    TargetQueue.Add(toAttack);
                    hasPriorityTarget = true;
                    HasPriorityOrder = false;
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    OrderQueue.Enqueue(combat);
                    return;
                }
                OrderInterceptShip(toAttack);
            }
        }
        //order bomb planet
        public void OrderBombardPlanet(Planet toBombard)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState. Bombard;
            Owner.InCombatTimer = 15f;
            OrderQueue.Clear();
            HasPriorityOrder = true;
            var combat = new ShipGoal(Plan.Bombard, Vector2.Zero, 0f)
            {
                TargetPlanet = toBombard
            };
            OrderQueue.Enqueue(combat);
        }
        //order colinization
        public void OrderColonization(Planet toColonize)
        {
            if (toColonize == null)
                return;
            ColonizeTarget = toColonize;
            OrderMoveTowardsPosition(toColonize.Position, 0f, new Vector2(0f, -1f), true, toColonize);
            var colonize = new ShipGoal(Plan.Colonize, toColonize.Position, 0f)
            {
                TargetPlanet = ColonizeTarget
            };
            OrderQueue.Enqueue(colonize);
            State = AIState.Colonize;
        }
        //order build platform no planet
        public void OrderDeepSpaceBuild(Goal goal)
        {
            OrderQueue.Clear();
            OrderMoveTowardsPosition(goal.BuildPosition, Owner.Center.RadiansToTarget(goal.BuildPosition), Owner.Center.FindVectorToTarget( goal.BuildPosition), true,null);
            var Deploy = new ShipGoal(Plan.DeployStructure, goal.BuildPosition, Owner.Center.RadiansToTarget(goal.BuildPosition))
            {
                goal = goal,
                VariableString = goal.ToBuildUID                
            };            
            OrderQueue.Enqueue(Deploy);
          
        }
        //order explore
        public void OrderExplore()
        {
            if (State == AIState.Explore && ExplorationTarget != null)
                return;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            OrderQueue.Clear();
            State = AIState.Explore;
            var Explore = new ShipGoal(Plan.Explore, Vector2.Zero, 0f);
            OrderQueue.Enqueue(Explore);
        }
        //order remenant exterminate planet
        public void OrderExterminatePlanet(Planet toBombard)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Exterminate;
            OrderQueue.Clear();
            var combat = new ShipGoal(Plan.Exterminate, Vector2.Zero, 0f)
            {
                TargetPlanet = toBombard
            };
            OrderQueue.Enqueue(combat);
        }
        //order remnant exterminate target
        public void OrderFindExterminationTarget(bool ClearOrders)
        {
            if (ExterminationTarget == null || ExterminationTarget.Owner == null)
            {
                var plist = new Array<Planet>();
                foreach (var planetsDict in universeScreen.PlanetsDict)
                {
                    if (planetsDict.Value.Owner == null)
                        continue;
                    plist.Add(planetsDict.Value);
                }
                var sortedList = 
                    from planet in plist
                    orderby Vector2.Distance(Owner.Center, planet.Position)
                    select planet;
                if (sortedList.Any())
                {
                    ExterminationTarget = sortedList.First<Planet>();
                    OrderExterminatePlanet(ExterminationTarget);
                    return;
                }
            }
            else if (ExterminationTarget != null && OrderQueue.IsEmpty)
            {
                OrderExterminatePlanet(ExterminationTarget);
            }
        }
        //order movement fleet 
        public void OrderFormationWarp(Vector2 destination, float facing, Vector2 fvec)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            OrderQueue.Clear();
            OrderMoveDirectlyTowardsPosition(destination, facing, fvec, true, Owner.fleet.Speed);
            State = AIState.FormationWarp;
        }
        //order movement fleet queued
        public void OrderFormationWarpQ(Vector2 destination, float facing, Vector2 fvec)
        {
            OrderMoveDirectlyTowardsPosition(destination, facing, fvec, false, Owner.fleet.Speed);
            State = AIState.FormationWarp;
        }
        //order intercept
        public void OrderInterceptShip(Ship toIntercept)
        {
            Intercepting = true;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Intercept;
            Target = toIntercept;
            hasPriorityTarget = true;
            HasPriorityOrder = false;
            OrderQueue.Clear();
        }
        //order troops landall
        public void OrderLandAllTroops(Planet target)
        {
            if ((Owner.shipData.Role == ShipData.RoleName.troop || Owner.HasTroopBay || Owner.hasTransporter) && Owner.TroopList.Count > 0 && target.GetGroundLandingSpots() > 0)
            {
                HasPriorityOrder = true;
                State = AIState.AssaultPlanet;
                OrbitTarget = target;
                OrderQueue.Clear();
                lock (ActiveWayPoints)
                {
                    ActiveWayPoints.Clear();
                }
                var goal = new ShipGoal(Plan.LandTroop, Vector2.Zero, 0f)
                {
                    TargetPlanet = target
                };
                OrderQueue.Enqueue(goal);
            }
            //else if (this.Owner.BombBays.Count > 0 && target.GetGroundStrength(this.Owner.loyalty) ==0)  //universeScreen.player == this.Owner.loyalty && 
            //{
            //    this.State = AIState.Bombard;
            //    this.OrderBombardTroops(target);
            //}
        }

        public void AddShipGoal(Plan plan, Vector2 waypoint, float desiredFacing)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, waypoint, desiredFacing));
        }
        public void AddShipGoal(Plan plan, Vector2 waypoint, float desiredFacing, Planet targetPlanet)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, waypoint, desiredFacing, targetPlanet));
        }
        public void AddShipGoal(Plan plan, Vector2 waypoint, float desiredFacing, Planet targetPlanet, float speedLimit)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, waypoint, desiredFacing, targetPlanet){SpeedLimit = speedLimit});
        }

        //order movement no pathing
        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders)
        {
            Target = null;
            hasPriorityTarget = false;
            Vector2 wantedForward = Owner.Center.FindVectorToTarget(position);
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
            Vector2.Dot(wantedForward, right);
            if (angleDiff > 0.2f)
                Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (ClearOrders)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Enqueue(position);
            }
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            lock (WayPointLocker)
            {
                for (var i = 0; i < ActiveWayPoints.Count; i++)
                {
                    Vector2 waypoint = ActiveWayPoints.ToArray()[i];
                    if (i != 0)
                    {
                        var to1k = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.speed
                        };
                        OrderQueue.Enqueue(to1k);
                    }
                    else
                    {
                        AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                        var to1k = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.speed
                        };
                        OrderQueue.Enqueue(to1k);
                    }
                    if (i == ActiveWayPoints.Count - 1)
                    {
                        var finalApproach = new ShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.speed
                        };
                        OrderQueue.Enqueue(finalApproach);
                        var slow = new ShipGoal(Plan.StopWithBackThrust, waypoint, 0f)
                        {
                            SpeedLimit = Owner.speed
                        };
                        OrderQueue.Enqueue(slow);
                        AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
                    }
                }
            }
        }

        //order movement no pathing
        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders, float speedLimit)
        {
            Target = null;
            hasPriorityTarget = false;
            Vector2 wantedForward = Owner.Center.FindVectorToTarget(position);
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
            Vector2.Dot(wantedForward, right);
            if (angleDiff > 0.2f)
                Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (ClearOrders)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Enqueue(position);
            }
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;

            Vector2[] waypoints;
            lock (WayPointLocker) waypoints = ActiveWayPoints.ToArray();

            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector2 waypoint = waypoints[i];
                if (i != 0)
                {
                    var to1K = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(to1K);
                }
                else
                {
                    AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                    var to1K = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(to1K);
                }
                if (i == waypoints.Length - 1)
                {
                    var finalApproach = new ShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(finalApproach);
                    var slow = new ShipGoal(Plan.StopWithBackThrust, waypoint, 0f)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(slow);
                    AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
                }
            }
        }
        //order movement fleet to posistion
        public void OrderMoveToFleetPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders, float SpeedLimit, Fleet fleet)
        {
            SpeedLimit = Owner.speed;
            if (ClearOrders)
            {
                OrderQueue.Clear();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            }
            State = AIState.MoveTo;
            MovePosition = position;
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            bool inCombat = Owner.InCombat;
            AddShipGoal(Plan.RotateToFaceMovePosition, MovePosition, 0f);
            var to1k = new ShipGoal(Plan.MoveToWithin1000Fleet, MovePosition, desiredFacing)
            {
                SpeedLimit = SpeedLimit,
                fleet = fleet
            };
            OrderQueue.Enqueue(to1k);
            var finalApproach = new ShipGoal(Plan.MakeFinalApproachFleet, MovePosition, desiredFacing)
            {
                SpeedLimit = SpeedLimit,
                fleet = fleet
            };
            OrderQueue.Enqueue(finalApproach);
            AddShipGoal(Plan.RotateInlineWithVelocity, Vector2.Zero, 0f);
            var slow = new ShipGoal(Plan.StopWithBackThrust, position, 0f)
            {
                SpeedLimit = Owner.speed
            };
            OrderQueue.Enqueue(slow);
            AddShipGoal(Plan.RotateToDesiredFacing, MovePosition, desiredFacing);
        }
        // order movement to posiston
        public void OrderMoveTowardsPosition( Vector2  position , float desiredFacing, Vector2 fVec, bool clearOrders, Planet targetPlanet)
        {
            DistanceLast = 0f;
            Target = null;
            hasPriorityTarget = false;
         //   Vector2 wantedForward = Owner.Center.FindVectorToTarget(position);
   //         Vector2 forward       = Owner.Rotation.RotationToForwardVec();
            //float angleDiff = (float)Math.Acos(Vector2.Dot(wantedForward, forward));

            //if (angleDiff > 0.2f)
            //    Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (clearOrders)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            if (universeScreen != null && Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;

            PlotCourseToNew(position, ActiveWayPoints.Count > 0 ? ActiveWayPoints.Last() : Owner.Center);

            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;

            Vector2[] waypoints;
            lock (WayPointLocker) waypoints = ActiveWayPoints.ToArray();

            for (int i = 0; i < waypoints.Length; ++i)
            {
                Vector2 waypoint = waypoints[i];
                bool isLast = waypoints.Length - 1 == i;
                Planet p = isLast ? targetPlanet : null;

                if (i != 0)
                {
                    AddShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing, p, Owner.speed);
                }
                else
                {
                    AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                    AddShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing, p, Owner.speed);
                }
                if (isLast)
                {
                    AddShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing, p, Owner.speed);
                    AddShipGoal(Plan.StopWithBackThrust, waypoint, 0f, targetPlanet, Owner.speed);
                    AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
                }
            }
        }

        //order orbit nearest
        public void OrderOrbitNearest(bool ClearOrders)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (ClearOrders)
                OrderQueue.Clear();
            var sortedList = 
                from toOrbit in Owner.loyalty.GetPlanets()
                orderby Vector2.Distance(Owner.Center, toOrbit.Position)
                select toOrbit;
            if (sortedList.Any())
            {
                var planet = sortedList.First<Planet>();
                OrbitTarget = planet;
                var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = planet
                };
                resupplyTarget = planet;
                OrderQueue.Enqueue(orbit);
                State = AIState.Orbit;
                return;
            }

            if (Owner.loyalty.GetOwnedSystems().Any())
            {
                var systemList = from solarsystem in Owner.loyalty.GetOwnedSystems()
                                 orderby Owner.Center.SqDist(solarsystem.Position)
                                 select solarsystem;
                Planet item = systemList.First().PlanetList[0];
                OrbitTarget = item;
                var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = item
                };
                resupplyTarget = item;
                OrderQueue.Enqueue(orbit);
                State = AIState.Orbit;
            }
        }
        //added by gremlin to run away
        //order flee
        public void OrderFlee(bool ClearOrders)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (ClearOrders)
                OrderQueue.Clear();

            var systemList =
                from solarsystem in Owner.loyalty.GetOwnedSystems()
                where solarsystem.combatTimer <= 0f && Vector2.Distance(solarsystem.Position, Owner.Position) > 200000f
                orderby Vector2.Distance(Owner.Center, solarsystem.Position)
                select solarsystem;
            if (systemList.Any())
            {
                Planet item = systemList.First<SolarSystem>().PlanetList[0];
                OrbitTarget = item;
                var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = item
                };
                resupplyTarget = item;
                OrderQueue.Enqueue(orbit);
                State = AIState.Flee;
            }
        }
        //order orbit planet
        public void OrderOrbitPlanet(Planet p)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            OrbitTarget = p;
            OrderQueue.Clear();
            var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };
            resupplyTarget = p;
            OrderQueue.Enqueue(orbit);
            State = AIState.Orbit;
        }

        public void OrderQueueSpecificTarget(Ship toAttack)
        {
            if (TargetQueue.Count == 0 && Target != null && Target.Active && Target != toAttack)
            {
                OrderAttackSpecificTarget(Target as Ship);
                TargetQueue.Add(Target as Ship);
            }
            if (TargetQueue.Count == 0)
            {
                OrderAttackSpecificTarget(toAttack);
                return;
            }
            if (toAttack == null)
                return;
            //targetting relation
            if (Owner.loyalty.TryGetRelations(toAttack.loyalty, out Relationship relations))
            {
                if (!relations.Treaty_Peace)
                {
                    if (State == AIState.AttackTarget && Target == toAttack)
                        return;
                    if (State == AIState.SystemDefender && Target == toAttack)
                        return;
                    if (Owner.Weapons.Count == 0 || Owner.shipData.Role == ShipData.RoleName.troop)
                    {
                        OrderInterceptShip(toAttack);
                        return;
                    }
                    Intercepting = true;
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Clear();
                    }
                    State = AIState.AttackTarget;
                    TargetQueue.Add(toAttack);
                    hasPriorityTarget = true;
                    HasPriorityOrder = false;
                    return;
                }
                OrderInterceptShip(toAttack);
            }
        }
        //order rebase target
        public void OrderRebase(Planet p, bool ClearOrders)
        {

            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            if (ClearOrders)
                OrderQueue.Clear();
            int troops = Owner.loyalty
                .GetShips().Where(troop => troop.TroopList.Count > 0)
                .Count(troopAi => troopAi.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == p));

            if (troops >= p.GetGroundLandingSpots())
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
                return;
            }

            OrderMoveTowardsPosition(p.Position, 0f, new Vector2(0f, -1f), false,p);
            IgnoreCombat = true;
            var rebase = new ShipGoal(Plan.Rebase, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };
            OrderQueue.Enqueue(rebase);
            State = AIState.Rebase;
            HasPriorityOrder = true;
        }
        //order rebase nearest
        public void OrderRebaseToNearest()
        {
            ////added by gremlin if rebasing dont rebase.
            //if (this.State == AIState.Rebase && this.OrbitTarget.Owner == this.Owner.loyalty)
            //    return;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            
            var sortedList = 
                from planet in Owner.loyalty.GetPlanets()
                //added by gremlin if the planet is full of troops dont rebase there. RERC2 I dont think the about looking at incoming troops works.
                where Owner.loyalty.GetShips( )
                .Where(troop => troop.TroopList.Count > 0).Count(troopAi => troopAi.AI.OrderQueue
                .Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == planet)) <= planet.GetGroundLandingSpots()


                /*where planet.TroopsHere.Count + this.Owner.loyalty.GetShips()
                .Where(troop => troop.Role == ShipData.RoleName.troop 
                    
                    && troop.GetAI().State == AIState.Rebase 
                    && troop.GetAI().OrbitTarget == planet).Count() < planet.TilesList.Sum(space => space.number_allowed_troops)*/
                orderby Vector2.Distance(Owner.Center, planet.Position)
                select planet;

           


            if (!sortedList.Any())
            {
                State = AIState.AwaitingOrders;
                return;
            }
            var p = sortedList.First();
            OrderMoveTowardsPosition(p.Position, 0f, new Vector2(0f, -1f), false,p);
            IgnoreCombat = true;
            var rebase = new ShipGoal(Plan.Rebase, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };

           
            OrderQueue.Enqueue(rebase);
        
            State = AIState.Rebase;
            HasPriorityOrder = true;
        }
        //order refit
        public void OrderRefitTo(string toRefit)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            HasPriorityOrder = true;
            IgnoreCombat = true;
           
            OrderQueue.Clear();
          
            var sortedList = 
                from planet in Owner.loyalty.GetPlanets()
                orderby Vector2.Distance(Owner.Center, planet.Position)
                select planet;
            OrbitTarget = null;
            foreach (Planet Planet in sortedList)
            {
                if (!Planet.HasShipyard && !Owner.loyalty.isFaction)
                    continue;
                OrbitTarget = Planet;
                break;
            }
            if (OrbitTarget == null)
            {
                State = AIState.AwaitingOrders;
                return;
            }
            OrderMoveTowardsPosition(OrbitTarget.Position, 0f, Vector2.One, true,OrbitTarget);
            var refit = new ShipGoal(Plan.Refit, Vector2.Zero, 0f)
            {
                TargetPlanet = OrbitTarget,
                VariableString = toRefit
            };
            OrderQueue.Enqueue(refit);
            State = AIState.Refit;
        }
        //resupply order
        public void OrderResupply(Planet toOrbit, bool ClearOrders)
        {
          
            if (ClearOrders)
            {
                OrderQueue.Clear();
                HadPO = true;
            }
            else
            {
                HadPO = false;
            }
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            OrbitTarget = toOrbit;
            awaitClosest = toOrbit;
            OrderMoveTowardsPosition(toOrbit.Position, 0f, Vector2.One, ClearOrders, toOrbit);
            State = AIState.Resupply;
            HasPriorityOrder = true;
        }

        //fbedard: Added dont retreat to a near planet in combat, and flee if nowhere to go
        //resupply order
        public void OrderResupplyNearest(bool ClearOrders)
        {
            if (Owner.Mothership != null && Owner.Mothership.Active && (Owner.shipData.Role != ShipData.RoleName.supply 
                || Owner.Ordinance > 0 || Owner.Health / Owner.HealthMax < DmgLevel[(int)Owner.shipData.ShipCategory]))
            {
                OrderReturnToHangar();
                return;
            }
            var shipyards = new Array<Planet>();
            if(Owner.loyalty.isFaction)
                return;
            foreach (Planet planet in Owner.loyalty.GetPlanets())
            {
                if (!planet.HasShipyard || Owner.InCombat && Vector2.Distance(Owner.Center, planet.Position) < 15000f)
                    continue;
                shipyards.Add(planet);
            }
            IOrderedEnumerable<Planet> sortedList = null;
            if(Owner.NeedResupplyTroops)
                sortedList =
                from p in shipyards
                orderby p.TroopsHere.Count > Owner.TroopCapacity,
                Vector2.Distance(Owner.Center, p.Position)                
                select p;
            else
            sortedList = 
                from p in shipyards
                orderby Vector2.Distance(Owner.Center, p.Position)
                select p;
            if (sortedList.Count<Planet>() > 0)
                OrderResupply(sortedList.First<Planet>(), ClearOrders);
            else
                OrderFlee(true);

        }
        //hangar order return
        public void OrderReturnToHangar()
        {
            var g = new ShipGoal(Plan.ReturnToHangar, Vector2.Zero, 0f);
            
            OrderQueue.Clear();
            OrderQueue.Enqueue(g);
            
            HasPriorityOrder = true;
            State = AIState.ReturnToHangar;
        }
        //SCRAP Order
        public void OrderScrapShip()
        {
#if SHOWSCRUB
            //Log.Info(string.Concat(this.Owner.loyalty.PortraitName, " : ", this.Owner.Role)); 
#endif

            if (Owner.shipData.Role <= ShipData.RoleName.station && Owner.ScuttleTimer < 1)
            {
                Owner.ScuttleTimer = 1;
                State = AIState.Scuttle;
                HasPriorityOrder = true;
                Owner.QueueTotalRemoval();  //fbedard
                return;
            }
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Owner.loyalty.ForcePoolRemove(Owner);

            Owner.fleet?.RemoveShip(Owner);


            HasPriorityOrder = true;
            IgnoreCombat = true;
            OrderQueue.Clear();
            var sortedList = 
                from planet in Owner.loyalty.GetPlanets()
                orderby Vector2.Distance(Owner.Center, planet.Position)
                select planet;
            OrbitTarget = null;
            foreach (Planet Planet in sortedList)
            {
                if (!Planet.HasShipyard)
                    continue;
                OrbitTarget = Planet;
                break;
            }
            if (OrbitTarget == null)
            {
                State = AIState.AwaitingOrders;
            }
            else
            {
                OrderMoveTowardsPosition(OrbitTarget.Position, 0f, Vector2.One, true,OrbitTarget);
                var scrap = new ShipGoal(Plan.Scrap, Vector2.Zero, 0f)
                {
                    TargetPlanet = OrbitTarget
                };
                OrderQueue.Enqueue(scrap);
                State = AIState.Scrap;
            }
            State = AIState.Scrap;
        }

        private void OrderSupplyShip(Ship tosupply, float ord_amt)
        {
            var g = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
            EscortTarget = tosupply;
            g.VariableNumber = ord_amt;
            IgnoreCombat = true;
            OrderQueue.Clear();
            OrderQueue.Enqueue(g);
            State = AIState.Ferrying;
        }
        /// <summary>
        /// sysdefense order defend system
        /// </summary>
        /// <param name="system"></param>
        public void OrderSystemDefense(SolarSystem system)
        {
            //if (this.State == AIState.Intercept || this.Owner.InCombatTimer > 0)
            //    return;
            //bool inSystem = true;
            //if (this.Owner.BaseCanWarp && Vector2.Distance(system.Position, this.Owner.Position) / this.Owner.velocityMaximum > 11)
            //    inSystem = false;
            //else 
            //    inSystem = this.Owner.GetSystem() == this.SystemToDefend;
            //if (this.SystemToDefend == null)
            //{
            //    this.HasPriorityOrder = false;
            //    this.SystemToDefend = system;
            //    this.OrderQueue.Clear();
            //}
            //else

            ShipGoal goal = OrderQueue.PeekLast;

            if (SystemToDefend == null || SystemToDefend != system || awaitClosest == null || awaitClosest.Owner == null || awaitClosest.Owner != Owner.loyalty || Owner.System!= system && goal != null && OrderQueue.PeekLast.Plan != Plan.DefendSystem)
            {

#if SHOWSCRUB
                if (this.Target != null && (this.Target as Ship).Name == "Subspace Projector")
                    Log.Info(string.Concat("Scrubbed", (this.Target as Ship).Name)); 
#endif
                SystemToDefend = system;
                HasPriorityOrder = false;
                SystemToDefend = system;
                OrderQueue.Clear();
                OrbitTarget = (Planet)null;
                if (SystemToDefend.PlanetList.Count > 0)
                {
                    var Potentials = new Array<Planet>();
                    foreach (Planet p in SystemToDefend.PlanetList)
                    {
                        if (p.Owner == null || p.Owner != Owner.loyalty)
                            continue;
                        Potentials.Add(p);
                    }
                    //if (Potentials.Count == 0)
                    //    foreach (Planet p in this.SystemToDefend.PlanetList)
                    //        if (p.Owner == null)
                    //            Potentials.Add(p);

                    if (Potentials.Count > 0)
                    {
                        awaitClosest = Potentials[UniverseRandom.InRange(Potentials.Count)];
                        OrderMoveTowardsPosition(awaitClosest.Position, 0f, Vector2.One, true, null);
                        AddShipGoal(Plan.DefendSystem, Vector2.Zero, 0f);
                        State = AIState.SystemDefender;                   
                    }
                    else
                    {
                        OrderResupplyNearest(true);
                    }
                }
                //this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DefendSystem, Vector2.Zero, 0f));
            }
        
            //this.State = AIState.SystemDefender;                   
        }
        //movement create goals from waypoints
        public void OrderThrustTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders)
        {
            if (ClearOrders)
            {
                OrderQueue.Clear();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            }
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            lock (WayPointLocker)
            {
                for (var i = 0; i < ActiveWayPoints.Count; i++)
                {
                    Vector2 waypoint = ActiveWayPoints.ToArray()[i];
                    if (i == 0)
                    {
                        AddShipGoal(Plan.RotateInlineWithVelocity, Vector2.Zero, 0f);
                        var stop = new ShipGoal(Plan.Stop, Vector2.Zero, 0f);
                        OrderQueue.Enqueue(stop);
                        AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                        var to1k = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.speed
                        };
                        OrderQueue.Enqueue(to1k);
                    }
                }
            }
        }

        public void OrderToOrbit(Planet toOrbit, bool ClearOrders)
        {
            if (ClearOrders)
                OrderQueue.Clear();
            HasPriorityOrder = true;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Orbit;
            OrbitTarget = toOrbit;
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian)  //fbedard: civilian ship will use projectors
                OrderMoveTowardsPosition(toOrbit.Position, 0f, new Vector2(0f, -1f), false, toOrbit);
            var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
            {
                TargetPlanet = toOrbit
            };
            
            OrderQueue.Enqueue(orbit);
            
        }
        public float TimeToTarget(Planet target)
        {
            float test = 0;
            test = Vector2.Distance(target.Position, Owner.Center) / Owner.GetmaxFTLSpeed;
            return test;
        }
        //added by: Gremalin. returns roughly the number of turns to a target planet restricting to targets that can use the freighter. 
        private float TradeSort(Ship ship, Planet PlanetCheck, string ResourceType, float cargoCount,bool Delivery)
        {
            /*here I am trying to predict the planets need versus the ships speed.
             * I am returning a weighted value that is based on this but primarily the returned value is the time it takes the freighter to get to the target in a straight line
             * 
             * 
             */
            //cargoCount = cargoCount > PlanetCheck.MAX_STORAGE ? PlanetCheck.MAX_STORAGE : cargoCount;
            float resourceRecharge =0;
            float resourceAmount =0;
            if (ResourceType == "Food")
            {
                resourceRecharge = PlanetCheck.NetFoodPerTurn;
                resourceAmount = PlanetCheck.FoodHere;
            }
            else if(ResourceType == "Production")
            {
                resourceRecharge =  PlanetCheck.NetProductionPerTurn;
                resourceAmount = PlanetCheck.ProductionHere;
            }
            float timeTotarget = ship.AI.TimeToTarget(PlanetCheck);
            float Effeciency =  resourceRecharge * timeTotarget;
            
            // return PlanetCheck.MAX_STORAGE / (PlanetCheck.MAX_STORAGE -(Effeciency + resourceAmount));

            if (Delivery)
            {
                // return Effeciency;// * ((PlanetCheck.MAX_STORAGE + cargoCount) / ((PlanetCheck.MAX_STORAGE - resourceAmount + 1)));
                // Effeciency =  (PlanetCheck.MAX_STORAGE - cargoCount) / (cargoCount + Effeciency + resourceAmount) ;
                //return timeTotarget * Effeciency;
                bool badCargo = Effeciency + resourceAmount > PlanetCheck.MAX_STORAGE ;
                //bool badCargo = (cargoCount + Effeciency + resourceAmount) > PlanetCheck.MAX_STORAGE - cargoCount * .5f; //cargoCount + Effeciency < 0 ||
                if (!badCargo)
                    return timeTotarget * (badCargo ? PlanetCheck.MAX_STORAGE / (Effeciency + resourceAmount) : 1);// (float)Math.Ceiling((double)timeTotarget);                
            }
            else
            {
                //return Effeciency * (PlanetCheck.MAX_STORAGE / ((PlanetCheck.MAX_STORAGE - resourceAmount + 1)));
                // Effeciency = (ship.CargoSpace_Max) / (PlanetCheck.MAX_STORAGE);
                //return timeTotarget * Effeciency;
                Effeciency = PlanetCheck.MAX_STORAGE * .5f < ship.CargoSpaceMax ? resourceAmount + Effeciency < ship.CargoSpaceMax * .5f ? ship.CargoSpaceMax*.5f / (resourceAmount + Effeciency) :1:1;
                //bool BadSupply = PlanetCheck.MAX_STORAGE * .5f < ship.CargoSpace_Max && PlanetCheck.FoodHere + Effeciency < ship.CargoSpace_Max * .5f;
                //if (!BadSupply)
                    return timeTotarget * Effeciency;// (float)Math.Ceiling((double)timeTotarget);
            }
            return timeTotarget + universeScreen.Size.X;
        }
        //trade pick trade targets
        public void OrderTrade(float elapsedTime)
        {            
            //trade timer is sent but uses arbitrary timer just to delay the routine.
            Owner.TradeTimer -= elapsedTime;
            if (Owner.TradeTimer > 0f)
                return;

            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }

            OrderQueue.Clear();
            if (Owner.GetColonists() > 0.0f)
                return;

            if(start != null && end != null)  //resume trading
            {
                Owner.TradeTimer = 5f;
                if (Owner.GetFood() > 0f || Owner.GetProduction() > 0f)
                {
                    OrderMoveTowardsPosition(end.Position, 0f, new Vector2(0f, -1f), true, end);
                    
                    AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                   
                    State = AIState.SystemTrader;
                    return;
                }
                else
                {
                    OrderMoveTowardsPosition(start.Position, 0f, new Vector2(0f, -1f), true, start);
                  
                    AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
                    
                    State = AIState.SystemTrader;
                    return;
                }
            }
            Planet potential = null;//<-unused
            var planets = new Array<Planet>();
            IOrderedEnumerable<Planet> sortPlanets;
            bool flag;
            var secondaryPlanets = new Array<Planet>();
            //added by gremlin if fleeing keep fleeing
            if (Math.Abs(Owner.CargoSpaceMax) < 1 || State == AIState.Flee || Owner.isConstructor || Owner.isColonyShip)
                return;

            //try
            {
                var allincombat = true;
                var noimport = true;
                foreach (Planet p in Owner.loyalty.GetPlanets())
                {
                    if (p.ParentSystem.combatTimer <= 0)
                        allincombat = false;
                    if (p.ps == Planet.GoodState.IMPORT || p.fs == Planet.GoodState.IMPORT)
                        noimport = false;
                }

                if (allincombat || noimport && Owner.CargoSpaceUsed > 0)
                {
                    Owner.TradeTimer = 5f;
                    return;
                }
                if (Owner.loyalty.data.Traits.Cybernetic > 0)
                    Owner.TradingFood = false;

                //bool FoodFirst = true;
                //if ((Owner.GetProduction() > 0f || !Owner.TradingFood || RandomMath.RandomBetween(0f, 1f) < 0.5f) && Owner.TradingProd && Owner.GetFood() == 0f)
                //    FoodFirst = false;
                

                if (end == null && Owner.CargoSpaceUsed <1 ) //  && FoodFirst  && ( this.Owner.GetFood() > 0f))
                    foreach (Planet planet in Owner.loyalty.GetPlanets())
                    {
                        if (planet.ParentSystem.combatTimer > 0)
                            continue;
                        if (planet.fs == Planet.GoodState.IMPORT && InsideAreaOfOperation(planet))
                            planets.Add(planet);
                    }

                if (planets.Count > 0)
                {
                    // if (this.Owner.GetFood() > 0f)
                        //sortPlanets = planets.OrderBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                        {
                            return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceUsed, true);
                        }
                    );
                    foreach (Planet p in sortPlanets)
                    {
                        flag = false;
                        float cargoSpaceMax = p.MAX_STORAGE - p.FoodHere;                            
                        var faster = true ;
                        float mySpeed = TradeSort(Owner, p, "Food", Owner.CargoSpaceMax, true); 
                        cargoSpaceMax += p.NetFoodPerTurn * mySpeed;
                        cargoSpaceMax = cargoSpaceMax > p.MAX_STORAGE ? p.MAX_STORAGE : cargoSpaceMax;
                        cargoSpaceMax = cargoSpaceMax < 0 ? 0 : cargoSpaceMax;
                        //Planet with negative food production need more food:
                        //cargoSpaceMax = (cargoSpaceMax - (p.NetFoodPerTurn * 5f)) / 2f;  //reduced cargoSpacemax on first try!

                        using (Owner.loyalty.GetShips().AcquireReadLock())
                        {
                            for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = Owner.loyalty.GetShips()[k];
                                if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                {
                                    if (s.AI.State == AIState.SystemTrader && s.AI.end == p && s.AI.FoodOrProd == "Food" && s.CargoSpaceUsed > 0)
                                    {

                                        float currenTrade = TradeSort(s, p, "Food", s.CargoSpaceMax, true);                                        
                                        if (currenTrade < mySpeed)
                                            faster = false;
                                        if (currenTrade !=0 )
                                        {
                                            flag = true;
                                            break;
                                        }
                                        float efficiency = currenTrade - mySpeed;
                                        if(mySpeed * p.NetFoodPerTurn < p.FoodHere && faster)
                                            continue;
                                        if(p.NetFoodPerTurn <=0)
                                            efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;                                        
                                        else
                                            efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;                                        
                                        if (efficiency > 0)
                                        {
                                            if (efficiency > s.CargoSpaceMax)
                                                efficiency = s.CargoSpaceMax;
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                        //ca

                                    }
                                    if (cargoSpaceMax <= 0f)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag )
                        {
                            end = p;
                            break;
                        }
                        if (faster)
                            potential = p;
                    }
                    if (end != null)
                    {
                        FoodOrProd = "Food";
                        if (Owner.GetFood() > 0f)
                        {
                            OrderMoveTowardsPosition(end.Position, 0f, new Vector2(0f, -1f), true, end);
                            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                            State = AIState.SystemTrader;
                            return;
                        }
                    }
                }

                #region deliver Production (return if already loaded)
                if (end == null && (Owner.TradingProd || Owner.GetProduction() > 0f))
                {
                    planets.Clear();
                    secondaryPlanets.Clear();
                    foreach (Planet planet in Owner.loyalty.GetPlanets())
                    {
                        if (planet.ParentSystem.combatTimer > 0)
                            continue;

                        if (planet.ps == Planet.GoodState.IMPORT && InsideAreaOfOperation(planet))
                            planets.Add(planet);
                        else if (planet.MAX_STORAGE - planet.ProductionHere > 0)
                            secondaryPlanets.Add(planet);
                    }
              
                    if (Owner.CargoSpaceUsed > 0.01f &&  planets.Count == 0 )
                        planets.AddRange(secondaryPlanets);

                    if (planets.Count > 0)
                    {
                        if (Owner.GetProduction() > 0.01f)
                            //sortPlanets = planets.OrderBy(PlanetCheck=> (PlanetCheck.MAX_STORAGE - PlanetCheck.ProductionHere) >= this.Owner.CargoSpace_Max)
                            //    .ThenBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));
                            sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Production", Owner.CargoSpaceUsed, true);
                                
                            }
                   );//.ThenByDescending(f => f.ProductionHere / f.MAX_STORAGE);
                        else
                            //sortPlanets = planets.OrderBy(PlanetCheck=> (PlanetCheck.MAX_STORAGE - PlanetCheck.ProductionHere) >= this.Owner.CargoSpace_Max)
                            //    .ThenBy(dest => (dest.ProductionHere));
                            sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Production", Owner.CargoSpaceMax, true);
                            }
                   );//.ThenByDescending(f => f.ProductionHere / f.MAX_STORAGE);
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.MAX_STORAGE - p.ProductionHere;
                            var faster = true;
                            float thisTradeStr = TradeSort(Owner, p, "Production", Owner.CargoSpaceMax, true);
                            if (thisTradeStr >= universeScreen.Size.X && p.ProductionHere >= 0)
                                continue;

                            using (Owner.loyalty.GetShips().AcquireReadLock())
                            {
                                for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                                {
                                    Ship s = Owner.loyalty.GetShips()[k];
                                    if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                    {
                                        if (s.AI.State == AIState.SystemTrader && s.AI.end == p && s.AI.FoodOrProd == "Prod")
                                        {

                                            float currenTrade = TradeSort(s, p, "Production", s.CargoSpaceMax, true);
                                            if (currenTrade < thisTradeStr)
                                                faster = false;
                                            if (currenTrade > UniverseData.UniverseWidth && !faster)
                                            {
                                                flag = true;
                                                break;
                                            }
                                            cargoSpaceMax = cargoSpaceMax - s.CargoSpaceMax;
                                        }

                                        if (cargoSpaceMax <= 0f)
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!flag)
                            {
                                end = p;
                                break;
                            }
                            if (faster)
                                potential = p;
                        }
                        if (end != null)
                        {
                            FoodOrProd = "Prod";
                            if (Owner.GetProduction() > 0f)
                            {
                                OrderMoveTowardsPosition(end.Position, 0f, new Vector2(0f, -1f), true, end);
                                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                                State = AIState.SystemTrader;
                                return;
                            }
                        }
                    }
                }
                #endregion

                #region Deliver Food LAST (return if already loaded)
                if (end == null && (Owner.TradingFood || Owner.GetFood() > 0.01f) && Owner.GetProduction() == 0.0f)
                {
                    planets.Clear();
                    foreach (Planet planet in Owner.loyalty.GetPlanets())
                    {
                        if (planet.ParentSystem.combatTimer > 0f)
                            continue;
                        if (planet.fs == Planet.GoodState.IMPORT && InsideAreaOfOperation(planet))
                            planets.Add(planet);
                    }

                    if (planets.Count > 0)
                    {
                        if (Owner.GetFood() > 0.01f)
                          //  sortPlanets = planets.OrderBy(PlanetCheck => (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                        {
                            return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceUsed, true);   
                        }
                            );//.ThenByDescending(f => f.FoodHere / f.MAX_STORAGE);
                        else
                            //sortPlanets = planets.OrderBy(PlanetCheck => (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                            //    .ThenBy(dest => (dest.FoodHere + (dest.NetFoodPerTurn - dest.consumption) * GoodMult));

                        sortPlanets = planets.OrderBy(PlanetCheck =>
                        {
                            return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceMax, true);   
                        }
                            );//.ThenByDescending(f => f.FoodHere / f.MAX_STORAGE);
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.MAX_STORAGE - p.FoodHere;
                            var faster = true;
                            float mySpeed = TradeSort(Owner, p, "Food", Owner.CargoSpaceMax, true);
                            if (mySpeed >= universeScreen.Size.X)
                                continue;
                            cargoSpaceMax += p.NetFoodPerTurn * mySpeed;
                            cargoSpaceMax = cargoSpaceMax > p.MAX_STORAGE ? p.MAX_STORAGE : cargoSpaceMax;
                            cargoSpaceMax = cargoSpaceMax < 0.0f ? 0.0f : cargoSpaceMax;

                            using (Owner.loyalty.GetShips().AcquireReadLock())
                            {
                                for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                                {
                                    Ship s = Owner.loyalty.GetShips()[k];
                                    if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                    {
                                        if (s.AI.State == AIState.SystemTrader && s.AI.end == p && s.AI.FoodOrProd == "Food")
                                        {

                                            float currenTrade = TradeSort(s, p, "Food", s.CargoSpaceMax, true);
                                            if (currenTrade < mySpeed)
                                                faster = false;
                                            if (currenTrade > UniverseData.UniverseWidth && !faster)
                                                continue;
                                            float efficiency = Math.Abs(currenTrade - mySpeed);
                                            if (mySpeed * p.NetFoodPerTurn < p.FoodHere && faster)
                                                continue;
                                            if (p.NetFoodPerTurn == 0.0f)
                                                efficiency = s.CargoSpaceMax + efficiency * p.NetFoodPerTurn;
                                            else
                                            if (p.NetFoodPerTurn < 0.0f)
                                                efficiency = s.CargoSpaceMax + efficiency * p.NetFoodPerTurn;
                                            else
                                                efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;
                                            if (efficiency > 0.0f)
                                            {
                                                if (efficiency > s.CargoSpaceMax)
                                                    efficiency = s.CargoSpaceMax;
                                                cargoSpaceMax = cargoSpaceMax - efficiency;
                                            }
                                            //ca

                                        }
                                        if (cargoSpaceMax <= 0.0f)
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!flag)
                            {
                                end = p;
                                break;
                            }
                        }
                        if (end != null)
                        {
                            FoodOrProd = "Food";
                            if (Owner.GetFood() > 0f)
                            {
                                OrderMoveTowardsPosition(end.Position, 0f, new Vector2(0f, -1f), true, end);
                                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                                State = AIState.SystemTrader;
                                return;
                            }
                        }
                    }
                }
                #endregion
                
                #region Get Food
                if (start == null && end != null && FoodOrProd == "Food"
                    && (Owner.CargoSpaceUsed == 0 || Owner.CargoSpaceUsed / Owner.CargoSpaceMax < .2f))
                {
                    planets.Clear();
                    foreach (Planet planet in Owner.loyalty.GetPlanets())
                    {
                        if (planet.ParentSystem.combatTimer > 0)
                            continue;

                        float distanceWeight = TradeSort(Owner, planet, "Food", Owner.CargoSpaceMax, false);
                        planet.ExportFSWeight = distanceWeight < planet.ExportFSWeight ? distanceWeight : planet.ExportFSWeight;
                        if (planet.fs == Planet.GoodState.EXPORT && InsideAreaOfOperation(planet))
                            planets.Add(planet);
                    }

                    if (planets.Count > 0)
                    {
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceMax, false);
                                    //+ this.TradeSort(this.Owner, this.end, "Food", this.Owner.CargoSpace_Max)
                                    ;
                                //weight += this.Owner.CargoSpace_Max / (PlanetCheck.FoodHere + 1);
                                //weight += Vector2.Distance(PlanetCheck.Position, this.Owner.Position) / this.Owner.GetmaxFTLSpeed;
                                //return weight;
                            });
                        foreach (Planet p in sortPlanets)
                        {
                            float cargoSpaceMax = p.FoodHere; 
                            flag = false;
                            float mySpeed = TradeSort(Owner, p, "Food", Owner.CargoSpaceMax, false);                            
                            //cargoSpaceMax = cargoSpaceMax + p.NetFoodPerTurn * mySpeed;
                            using (Owner.loyalty.GetShips().AcquireReadLock())
                            {
                                for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                                {
                                    Ship s = Owner.loyalty.GetShips()[k];
                                    if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                    {
                                        ShipGoal plan = s.AI.OrderQueue.PeekLast;

                                        if (plan != null && s.AI.State == AIState.SystemTrader && s.AI.start == p && plan.Plan == Plan.PickupGoods && s.AI.FoodOrProd == "Food")
                                        {

                                            float currenTrade = TradeSort(s, p, "Food", s.CargoSpaceMax, false);
                                            if (currenTrade > 1000)
                                                continue;

                                            float efficiency = Math.Abs(currenTrade - mySpeed);
                                            efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;
                                            if (efficiency > 0)
                                            {
                                                if (efficiency > s.CargoSpaceMax)
                                                    efficiency = s.CargoSpaceMax;
                                                cargoSpaceMax = cargoSpaceMax - efficiency;
                                            }
                                            //cargoSpaceMax = cargoSpaceMax - s.CargoSpace_Max;
                                        }
                                    
                                        if (cargoSpaceMax <=0+p.MAX_STORAGE*.1f)// < this.Owner.CargoSpace_Max)
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!flag)
                            {
                                start = p;
                                //this.Owner.TradingFood = true;
                                //this.Owner.TradingProd = false;
                                break;
                            }
                        }
                    }
                }
                #endregion

                #region Get Production
                if (start == null && end != null && FoodOrProd == "Prod" 
                    && (Owner.CargoSpaceUsed ==0 || Owner.CargoSpaceUsed / Owner.CargoSpaceMax <.2f  ))
                {
                    planets.Clear();
                    foreach (Planet planet in Owner.loyalty.GetPlanets())
                        if (planet.ParentSystem.combatTimer <= 0)
                        {
                            float distanceWeight = TradeSort(Owner, planet, "Production", Owner.CargoSpaceMax, false);
                            planet.ExportPSWeight = distanceWeight < planet.ExportPSWeight ? distanceWeight : planet.ExportPSWeight;

                            if (planet.ps == Planet.GoodState.EXPORT && InsideAreaOfOperation(planet))
                                planets.Add(planet);
                        }
                    if (planets.Count > 0)
                    {
                        sortPlanets = planets.OrderBy(PlanetCheck => {//(PlanetCheck.ProductionHere > this.Owner.CargoSpace_Max))
                                //.ThenBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));

                            return TradeSort(Owner, PlanetCheck, "Production", Owner.CargoSpaceMax, false);
                                  // + this.TradeSort(this.Owner, this.end, "Production", this.Owner.CargoSpace_Max);
                            
                        });
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.ProductionHere;
                            
                            float mySpeed = TradeSort(Owner, p, "Production", Owner.CargoSpaceMax, false);
                            //cargoSpaceMax = cargoSpaceMax + p.NetProductionPerTurn * mySpeed;

                            //+ this.TradeSort(this.Owner, this.end, "Production", this.Owner.CargoSpace_Max);
                            
                            ShipGoal plan;
                            using (Owner.loyalty.GetShips().AcquireReadLock())
                            {
                                for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                                {
                                    Ship s = Owner.loyalty.GetShips()[k];
                                    if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                    {
                                        plan = s.AI.OrderQueue.PeekLast;
                                        if (plan != null && s.AI.State == AIState.SystemTrader && s.AI.start == p && plan.Plan == Plan.PickupGoods && s.AI.FoodOrProd == "Prod")
                                        {

                                            float currenTrade = TradeSort(s, p, "Production", s.CargoSpaceMax, false);      
                                            if (currenTrade > 1000)
                                                continue;

                                            float efficiency = Math.Abs(currenTrade - mySpeed);
                                            efficiency = s.CargoSpaceMax - efficiency * p.NetProductionPerTurn;
                                            if(efficiency >0)
                                                cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                    
                                        if (cargoSpaceMax <= 0 + p.MAX_STORAGE * .1f) // this.Owner.CargoSpace_Max)
                                        {
                                            flag = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!flag)
                            {
                                start = p;
                                //this.Owner.TradingFood = false;
                                //this.Owner.TradingProd = true;
                                break;
                            }
                        }
                    }
                }
                #endregion

                if (start != null && end != null && start != end )
                {
                    //if (this.Owner.CargoSpace_Used == 00 && this.start.Population / this.start.MaxPopulation < 0.2 && this.end.Population > 2000f && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                    //    this.PickupAnyPassengers();
                    //if (this.Owner.CargoSpace_Used == 00 && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                    //    this.PickupAnyGoods();
                    OrderMoveTowardsPosition(start.Position + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true, start);
                    
                    AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
                   
                }
                else
                {                    
                    awaitClosest = start ?? end;
                    start = null;
                    end = null;
                    if(Owner.CargoSpaceUsed >0)
                        Owner.ClearCargo();
                }
                State = AIState.SystemTrader;
                Owner.TradeTimer = 5f;
                if (FoodOrProd.IsEmpty())
                    FoodOrProd = Owner.TradingFood ? "Food" : "Prod";
            }
            //catch { }
        }

        private bool ShouldSuspendTradeDueToCombat()
        {
            return Owner.loyalty.GetOwnedSystems().All(combat => combat.combatTimer > 0);
        }

        public void OrderTradeFromSave(bool hasCargo, Guid startGUID, Guid endGUID)
        {
            if (Owner.CargoSpaceMax <= 0 || State == AIState.Flee || ShouldSuspendTradeDueToCombat())
                return;

            if (start == null && end == null)
                foreach (Planet p in Owner.loyalty.GetPlanets())
                {
                    if (p.guid == startGUID)
                        start = p;
                    if (p.guid != endGUID)
                        continue;
                    end = p;
                }
            if (!hasCargo && start != null)
            {
                OrderMoveTowardsPosition(start.Position + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true, start);
                AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
                State = AIState.SystemTrader;
            }
            if (!hasCargo || end == null)
            {
                if (!hasCargo && (start == null || end == null))
                    OrderTrade(5f);
                return;
            }
            OrderMoveTowardsPosition(end.Position + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true, end);
            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            State = AIState.SystemTrader;
        }



        private bool InsideAreaOfOperation(Planet planet)
        {
            if (Owner.AreaOfOperation.Count == 0)
                return true;
            foreach (Rectangle AO in Owner.AreaOfOperation)
                if (HelperFunctions.CheckIntersection(AO, planet.Position))
                    return true;
            return false;
        }

        private float RelativePlanetFertility(Planet p)
        {
            return p.Owner.data.Traits.Cybernetic > 0 ? p.MineralRichness : p.Fertility;
        }

        private bool PassengerDropOffTarget(Planet p)
        {
            return p != start && p.MaxPopulation > 2000f && p.Population / p.MaxPopulation < 0.5f
                && RelativePlanetFertility(p) >= 0.5f;
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

                float dist = Owner.Center.SqDist(p.Position);
                if (dist >= minSqDist)
                    continue;

                minSqDist = dist;
                targetPlanet = p;
            }
            return targetPlanet != null;
        }



        //trade pick passenger targets
        public void OrderTransportPassengers(float elapsedTime)
        {
            Owner.TradeTimer -= elapsedTime;
            if (Owner.TradeTimer > 0f || Owner.CargoSpaceMax <= 0f || State == AIState.Flee || Owner.isConstructor)
                return;

            if (ShouldSuspendTradeDueToCombat())
            {
                Owner.TradeTimer = 5f;
                return;
            }

            Planet[] safePlanets = Owner.loyalty.GetPlanets().Where(combat => combat.ParentSystem.combatTimer <= 0f).ToArray();
            OrderQueue.Clear();

            // RedFox: Where to drop nearest Population
            if (Owner.GetColonists() > 0f)
            {
                if (SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget))
                {
                    OrderMoveTowardsPosition(end.Position, 0f, new Vector2(0f, -1f), true, end);
                    State = AIState.PassengerTransport;
                    FoodOrProd = "Pass";
                    AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
                }
                return;
            }

            // RedFox: Where to load & drop nearest Population
            SelectPlanetByFilter(safePlanets, out start, p => p.MaxPopulation > 1000 && p.Population > 1000);
            SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget);

            if (start != null && end != null)
            {
                OrderMoveTowardsPosition(start.Position + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true, start);
                AddShipGoal(Plan.PickupPassengers, Vector2.Zero, 0f);
            }
            else
            {
                awaitClosest = start ?? end;
                start = null;
                end   = null;
            }
            Owner.TradeTimer = 5f;
            State            = AIState.PassengerTransport;
            FoodOrProd       = "Pass";
        }

        public void OrderTransportPassengersFromSave()
        {
            OrderTransportPassengers(0.33f);
        }

        public void OrderTroopToBoardShip(Ship s)
        {
            HasPriorityOrder = true;
            EscortTarget = s;
            OrderQueue.Clear();
            AddShipGoal(Plan.BoardShip, Vector2.Zero, 0f);
        
        }

        public void OrderTroopToShip(Ship s)
        {
            EscortTarget = s;
            OrderQueue.Clear();
            AddShipGoal(Plan.TroopToShip, Vector2.Zero, 0f);
        }

        private void PickupGoods()
        {
            if (start == null)
            {
                OrderTrade(0.1f);
                return;
            }

            if (FoodOrProd == "Food")
            {
                start.ProductionHere += Owner.UnloadProduction();
                start.Population     += Owner.UnloadColonists();

                float maxFoodLoad = (start.MAX_STORAGE * 0.10f).Clamp(0f, start.MAX_STORAGE - start.FoodHere);
                start.FoodHere -= Owner.LoadFood(maxFoodLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Position + UniverseRandom.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true, end);
                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            }
            else if (FoodOrProd == "Prod")
            {
                start.FoodHere   += Owner.UnloadFood();
                start.Population += Owner.UnloadColonists();

                float maxProdLoad = (start.MAX_STORAGE * .10f).Clamp(0f, start.MAX_STORAGE - start.ProductionHere);
                start.ProductionHere -= Owner.LoadProduction(maxProdLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Position + UniverseRandom.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true, end);
                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            }
            else 
            {
                OrderTrade(0.1f);
            }
            State = AIState.SystemTrader;
        }

        // trade pickup passengers
        private void PickupPassengers()
        {
            start.ProductionHere += Owner.UnloadProduction();
            start.FoodHere       += Owner.UnloadFood();

            // load everyone we can :P
            start.Population -= Owner.LoadColonists(start.Population * 0.2f);

            OrderQueue.RemoveFirst();
            OrderMoveTowardsPosition(end.Position, 0f, new Vector2(0f, -1f), true, end);
            State = AIState.PassengerTransport;
            AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
        }

        // movement cachelookup
        private bool PathCacheLookup(Point startp, Point endp, Vector2 startv, Vector2 endv)
        {            
            if (!Owner.loyalty.PathCache.TryGetValue(startp, out Map<Point, Empire.PatchCacheEntry> pathstart)
                || !pathstart.TryGetValue(endp, out Empire.PatchCacheEntry pathend))
                return false;

            lock (WayPointLocker)
            {
                if (pathend.Path.Count > 2)
                {
                    int n = pathend.Path.Count - 2;
                    for (var x = 1; x < n; ++x)
                    {
                        Vector2 point = pathend.Path[x];
                        if (point != Vector2.Zero)
                            ActiveWayPoints.Enqueue(point);
                    }
                }
                ActiveWayPoints.Enqueue(endv);
            }
            ++pathend.CacheHits;
            return true;
        }


        // movement pathing       
        private void PlotCourseToNew(Vector2 endPos, Vector2 startPos)
        {
            if (Owner.loyalty.grid != null && Vector2.Distance(startPos,endPos) > Empire.ProjectorRadius *2)
            {
                int reducer = Empire.Universe.reducer;//  (int)(Empire.ProjectorRadius );
                int granularity = Owner.loyalty.granularity; // (int)Empire.ProjectorRadius / 2;

                var startp = new Point((int)startPos.X, (int)startPos.Y);
                startp.X /= reducer;
                startp.Y /= reducer;
                startp.X += granularity;
                startp.Y += granularity;
                startp.X = startp.X < 0 ? 0 : startp.X;
                startp.Y = startp.Y < 0 ? 0 : startp.Y;
                startp.X = startp.X > granularity * 2 ? granularity * 2 : startp.X;
                startp.Y = startp.Y > granularity *2 ? granularity *2 : startp.Y;
                var endp = new Point((int)endPos.X, (int)endPos.Y);
                endp.X /= reducer;
                endp.Y /= reducer;
                endp.Y += granularity;
                endp.X += granularity;
                endp.X = endp.X < 0 ? 0 : endp.X;
                endp.Y = endp.Y < 0 ? 0 : endp.Y;
                endp.X = endp.X > granularity * 2 ? granularity * 2 : endp.X;
                endp.Y = endp.Y > granularity * 2 ? granularity * 2 : endp.Y;
                //@Bug Add sanity correct to prevent start and end from getting posistions off the map
                using (Owner.loyalty.LockPatchCache.AcquireReadLock())
                    if (PathCacheLookup(startp, endp, startPos, endPos))
                        return;

                var path = new PathFinderFast(Owner.loyalty.grid)
                {
                    Diagonals = true,
                    HeavyDiagonals = false,
                    PunishChangeDirection = true,
                    Formula = HeuristicFormula.EuclideanNoSQR, // try with HeuristicFormula.MaxDXDY?
                    HeuristicEstimate = 1, // try with 2?
                    SearchLimit = 999999
                };

                var pathpoints = path.FindPath(startp, endp);
                lock (WayPointLocker)
                {
                    if (pathpoints != null)
                    {
                        var cacheAdd = new Array<Vector2>();
                        //byte lastValue =0;
                        int y = pathpoints.Count() - 1;                                                        
                        for (int x =y; x >= 0; x-=2)                            
                        {
                            PathFinderNode pnode = pathpoints[x];
                            //var value = this.Owner.loyalty.grid[pnode.X, pnode.Y];
                            //if (value != 1 && lastValue >1)
                            //{
                            //    lastValue--;
                            //    continue;
                            //}
                            //lastValue = value ==1 ?(byte)1 : (byte)2;
                            var translated = new Vector2((pnode.X - granularity) * reducer, (pnode.Y - granularity) * reducer);
                            if (translated == Vector2.Zero)
                                continue;
                            cacheAdd.Add(translated);
                                
                            if (Vector2.Distance(translated, endPos) > Empire.ProjectorRadius *2 
                                && Vector2.Distance(translated, startPos) > Empire.ProjectorRadius *2)
                                ActiveWayPoints.Enqueue(translated);
                        }

                        var cache = Owner.loyalty.PathCache;
                        if (!cache.ContainsKey(startp))
                        {
                            using (Owner.loyalty.LockPatchCache.AcquireWriteLock())
                            {
                                var endValue = new Empire.PatchCacheEntry(cacheAdd);
                                cache[startp] = new Map<Point, Empire.PatchCacheEntry> { { endp, endValue } };
                                Owner.loyalty.pathcacheMiss++;
                            }
                        }
                        else if (!cache[startp].ContainsKey(endp))
                        {
                            using (Owner.loyalty.LockPatchCache.AcquireWriteLock())
                            {
                                var endValue = new Empire.PatchCacheEntry(cacheAdd);
                                cache[startp].Add(endp, endValue);
                                Owner.loyalty.pathcacheMiss++;
                            }
                        }
                        else
                        {
                            using (Owner.loyalty.LockPatchCache.AcquireReadLock())
                            {
                                PathCacheLookup(startp, endp, startPos, endPos);
                            }
                        }
                    }
                    ActiveWayPoints.Enqueue(endPos);
                    return;
                }
                   
                    
            }
            ActiveWayPoints.Enqueue(endPos);

            #if false
                Array<Vector2> goodpoints = new Array<Vector2>();
                //Grid path = new Grid(this.Owner.loyalty, 36, 10f);
                if (Empire.Universe != null && this.Owner.loyalty.SensorNodes.Count != 0)
                    goodpoints = this.Owner.loyalty.pathhMap.Pathfind(startPos, endPos, false);
                if (goodpoints != null && goodpoints.Count > 0)
                {
                    lock (this.WayPointLocker)
                    {
                        foreach (Vector2 wayp in goodpoints.Skip(1))
                        {

                            this.ActiveWayPoints.Enqueue(wayp);
                        }
                        //this.ActiveWayPoints.Enqueue(endPos);
                    }
                    //this.Owner.loyalty.lockPatchCache.EnterWriteLock();
                    //int cache;
                    //if (!this.Owner.loyalty.pathcache.TryGetValue(goodpoints, out cache))
                    //{

                    //    this.Owner.loyalty.pathcache.Add(goodpoints, 0);

                    //}
                    //cache++;
                    this.Owner.loyalty.lockPatchCache.ExitWriteLock();

                }
                else
                {
                    if (startPos != Vector2.Zero && endPos != Vector2.Zero)
                    {
                        // this.ActiveWayPoints.Enqueue(startPos);
                        this.ActiveWayPoints.Enqueue(endPos);
                    }
                    else
                        this.ActiveWayPoints.Clear();
                }
            #endif
        }

        private Array<Vector2> GoodRoad(Vector2 endPos, Vector2 startPos)
        {
            SpaceRoad targetRoad =null;
            var StartRoads = new Array<SpaceRoad>();
            var endRoads = new Array<SpaceRoad>();
            var nodePos = new Array<Vector2>();
            foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                Vector2 start = road.GetOrigin().Position;
                Vector2 end = road.GetDestination().Position;
                if (Vector2.Distance(start, startPos) < Empire.ProjectorRadius)
                    if (Vector2.Distance(end, endPos) < Empire.ProjectorRadius)
                        targetRoad = road;
                    else
                        StartRoads.Add(road);
                else if (Vector2.Distance(end, startPos) < Empire.ProjectorRadius)
                    if (Vector2.Distance(start, endPos) < Empire.ProjectorRadius)
                        targetRoad = road;
                    else
                        endRoads.Add(road);

                if (  targetRoad !=null)
                    break;
            }
            

            if(targetRoad != null)
            {
                foreach(RoadNode node in targetRoad.RoadNodesList)
                    nodePos.Add(node.Position);
                nodePos.Add(endPos);
                nodePos.Add(targetRoad.GetDestination().Position);
                nodePos.Add(targetRoad.GetOrigin().Position);
            }
            return nodePos;
            


        }
        
        private Array<Vector2> PlotCourseToNewViaRoad(Vector2 endPos, Vector2 startPos)
        {
            //return null;
            var goodPoints = new Array<Vector2>();
            var potentialEndRoads = new Array<SpaceRoad>();
            var potentialStartRoads = new Array<SpaceRoad>();
            RoadNode nearestNode = null;
            var distanceToNearestNode = 0f;
            foreach(SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                if (Vector2.Distance(road.GetOrigin().Position, endPos) < 300000f || Vector2.Distance(road.GetDestination().Position, endPos) < 300000f)
                    potentialEndRoads.Add(road);
                foreach(RoadNode projector in road.RoadNodesList)
                    if (nearestNode == null || Vector2.Distance(projector.Position, startPos) < distanceToNearestNode)
                    {
                        potentialStartRoads.Add(road);
                        nearestNode = projector;
                        distanceToNearestNode = Vector2.Distance(projector.Position, startPos);
                    }
            }

            var targetRoads = potentialStartRoads.Intersect(potentialEndRoads).ToList();
            if (targetRoads.Count == 1)
            {
                SpaceRoad targetRoad = targetRoads[0];
                bool startAtOrgin = Vector2.Distance(endPos, targetRoad.GetOrigin().Position) > Vector2.Distance(endPos, targetRoad.GetDestination().Position);
                var foundstart = false;
                if (startAtOrgin)
                    foreach (RoadNode node in targetRoad.RoadNodesList)
                    {
                        if (!foundstart && node != nearestNode)
                            continue;
                        else if (!foundstart)
                            foundstart = true;
                        goodPoints.Add(node.Position);
                        goodPoints.Add(targetRoad.GetDestination().Position);
                        goodPoints.Add(targetRoad.GetOrigin().Position);

                    }
                else
                    foreach (RoadNode node in targetRoad.RoadNodesList.Reverse<RoadNode>())
                    {
                        if (!foundstart && node != nearestNode)
                            continue;
                        else if (!foundstart)
                            foundstart = true;
                        goodPoints.Add(node.Position);
                        goodPoints.Add(targetRoad.GetDestination().Position);
                        goodPoints.Add(targetRoad.GetOrigin().Position);

                    }
          
            }
            else if (true)
            {
                while (potentialStartRoads.Intersect(potentialEndRoads).Count() == 0)
                {
                    var test = false;
                    foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
                    {
                        var flag = false;

                        if (!potentialStartRoads.Contains(road))
                            foreach (SpaceRoad proad in potentialStartRoads)
                                if (proad.GetDestination() == road.GetOrigin() || proad.GetOrigin() == road.GetDestination())
                                    flag = true;
                        if (flag)
                        {
                            potentialStartRoads.Add(road);
                            test = true;
                        }
                        
                    }
                    if(!test)
                    {
                        Log.Info("failed to find road path for {0}", Owner.loyalty.PortraitName);
                        return new Array<Vector2>();
                    }
                }
                while (!potentialEndRoads.Contains(potentialStartRoads[0]))
                {
                    var test = false;
                    foreach (SpaceRoad road in potentialStartRoads)
                    {
                        var flag = false;

                        if (!potentialEndRoads.Contains(road))
                            foreach (SpaceRoad proad in potentialEndRoads)
                                if (proad.GetDestination() == road.GetOrigin() || proad.GetOrigin() == road.GetDestination())
                                    flag = true;
                        if (flag)
                        {

                            test = true;
                            potentialEndRoads.Add(road);
                            
                        }
                        
                    }
                    if(!test)
                    {
                        Log.Info("failed to find road path for {0}", Owner.loyalty.PortraitName);
                        return new Array<Vector2>();
                    }


                }
                targetRoads = potentialStartRoads.Intersect(potentialEndRoads).ToList();
                if (targetRoads.Count >0)
                {
                    SpaceRoad targetRoad = null;
                    RoadNode targetnode = null;
                    float distance = -1f;
                    foreach (SpaceRoad road in targetRoads)
                    foreach (RoadNode node in road.RoadNodesList)
                        if (distance == -1f || Vector2.Distance(node.Position, startPos) < distance)
                        {
                            targetRoad = road;
                            targetnode = node;
                            distance = Vector2.Distance(node.Position, startPos);

                        }
                    var orgin = false;
                    var startnode = false;
                    foreach (SpaceRoad road in targetRoads)
                        if (road.GetDestination() == targetRoad.GetDestination() || road.GetDestination() == targetRoad.GetOrigin())
                            orgin = true;
                    if (orgin)
                        foreach (RoadNode node in targetRoad.RoadNodesList)
                            if (!startnode || node != targetnode)
                            {
                                continue;
                            }
                            else
                            {
                                startnode = true;
                                goodPoints.Add(node.Position);
                                goodPoints.Add(targetRoad.GetDestination().Position);
                                goodPoints.Add(targetRoad.GetOrigin().Position);
                            }
                    else
                        foreach (RoadNode node in targetRoad.RoadNodesList.Reverse<RoadNode>())
                            if (!startnode || node != targetnode)
                            {
                                continue;
                            }
                            else
                            {
                                startnode = true;
                                goodPoints.Add(node.Position);
                                goodPoints.Add(targetRoad.GetDestination().Position);
                                goodPoints.Add(targetRoad.GetOrigin().Position);
                            }
                    while (Vector2.Distance(targetRoad.GetOrigin().Position,endPos)>300000 
                        &&  Vector2.Distance(targetRoad.GetDestination().Position,endPos)>300000)
                    {
                        targetRoads.Remove(targetRoad);
                        if(orgin)
                        {
                            var test = false;
                            foreach(SpaceRoad road in targetRoads)
                                if(road.GetOrigin()==targetRoad.GetDestination())
                                {
                                    foreach(RoadNode node in road.RoadNodesList)
                                    {
                                        goodPoints.Add(node.Position);
                                        goodPoints.Add(targetRoad.GetDestination().Position);
                                        goodPoints.Add(targetRoad.GetOrigin().Position);
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                                else if(road.GetDestination() == targetRoad.GetDestination())
                                {
                                    orgin = false;
                                    if (road.GetOrigin() == targetRoad.GetDestination())
                                        foreach (RoadNode node in road.RoadNodesList.Reverse<RoadNode>())
                                        {
                                            goodPoints.Add(node.Position);
                                            goodPoints.Add(targetRoad.GetDestination().Position);
                                            goodPoints.Add(targetRoad.GetOrigin().Position);
                                        }
                                    test = true;
                                    targetRoad = road;
                                    break;
                                }
                            if (!test)
                                orgin = false;
                        }
                        else
                        {
                            var test = false;
                            foreach (SpaceRoad road in targetRoads)
                                if (road.GetOrigin() == targetRoad.GetOrigin())
                                {
                                    foreach (RoadNode node in road.RoadNodesList)
                                    {
                                        goodPoints.Add(node.Position);
                                        goodPoints.Add(targetRoad.GetDestination().Position);
                                        goodPoints.Add(targetRoad.GetOrigin().Position);
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                                else if (road.GetDestination() == targetRoad.GetOrigin())
                                {
                                    orgin = true;
                                    if (road.GetOrigin() == targetRoad.GetDestination())
                                        foreach (RoadNode node in road.RoadNodesList.Reverse<RoadNode>())
                                        {
                                            goodPoints.Add(node.Position);
                                            goodPoints.Add(targetRoad.GetDestination().Position);
                                            goodPoints.Add(targetRoad.GetOrigin().Position);
                                        }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                            if (!test)
                                break;
                        }

                    }
                }
            }
            return goodPoints;
        }

        private void RotateInLineWithVelocity(float elapsedTime, ShipGoal Goal)
        {
            if (Owner.Velocity == Vector2.Zero)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(Vector2.Normalize(Owner.Velocity), forward));
            float facing = Vector2.Dot(Vector2.Normalize(Owner.Velocity), right) > 0f ? 1f : -1f;
            if (angleDiff <= 0.2f)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, facing);
        }

        private void RotateToDesiredFacing(float elapsedTime, ShipGoal goal)
        {
            Vector2 p = MathExt.PointFromRadians(Vector2.Zero, goal.DesiredFacing, 1f);
            Vector2 fvec = Vector2.Zero.FindVectorToTarget(p);
            Vector2 wantedForward = Vector2.Normalize(fvec);
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
            float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;
            if (angleDiff <= 0.02f)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, facing);
        }

        private bool RotateToFaceMovePosition(float elapsedTime, ShipGoal goal)
        {
            var turned = false;
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = Owner.Center.FindVectorToTarget(goal.MovePosition);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward));
            if (angleDiff > 0.2f)
            {
                Owner.HyperspaceReturn();
                RotateToFacing(elapsedTime, angleDiff, Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f);
                turned = true;
            }
            else if (OrderQueue.NotEmpty)
            {
                OrderQueue.RemoveFirst();
                
            }
            return turned;
        }
        private bool RotateToFaceMovePosition(float elapsedTime, Vector2 MovePosition)
        {
            var turned = false;
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = Owner.Center.FindVectorToTarget( MovePosition);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward));
            if (angleDiff > Owner.rotationRadiansPerSecond*elapsedTime )
            {
                Owner.HyperspaceReturn();
                RotateToFacing(elapsedTime, angleDiff, Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f);
                turned = true;
            }
 
            return turned;
        }
        //movement rotate
        private void RotateToFacing(float elapsedTime, float angleDiff, float facing)
        {
            Owner.isTurning = true;
            float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
            if (Math.Abs(RotAmount) > angleDiff)
                RotAmount = RotAmount <= 0f ? -angleDiff : angleDiff;
            if (RotAmount > 0f)
            {
                if (Owner.yRotation > -Owner.maxBank)
                {
                    Ship owner = Owner;
                    owner.yRotation = owner.yRotation - Owner.yBankAmount;
                }
            }
            else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
            {
                Ship ship = Owner;
                ship.yRotation = ship.yRotation + Owner.yBankAmount;
            }
            if (!float.IsNaN(RotAmount))
            {
                Ship rotation = Owner;
                rotation.Rotation = rotation.Rotation + RotAmount;
            }
        }

        //targetting get targets
        public GameplayObject ScanForCombatTargets(Vector2 Position, float Radius)
        {

            BadGuysNear = false;
            FriendliesNearby.Clear();
            PotentialTargets.Clear();
            NearbyShips.Clear();
            //this.TrackProjectiles.Clear();

            if (hasPriorityTarget && Target == null)
            {
                hasPriorityTarget = false;
                if (TargetQueue.Count > 0)
                {
                    hasPriorityTarget = true;
                    Target = TargetQueue.First<Ship>();
                }
            }
            if (Target != null)
                if ((Target as Ship).loyalty == Owner.loyalty)
                {
                    Target = null;
                    hasPriorityTarget = false;
                }


                else if (
                    !Intercepting && (Target as Ship).engineState == Ship.MoveState.Warp) //||((double)Vector2.Distance(Position, this.Target.Center) > (double)Radius ||
                {
                    Target = (GameplayObject)null;
                    if (!HasPriorityOrder && Owner.loyalty != universeScreen.player)
                        State = AIState.AwaitingOrders;
                    return (GameplayObject)null;
                }
            //Doctor: Increased this from 0.66f as seemed slightly on the low side. 
            CombatAI.PreferredEngagementDistance = Owner.maxWeaponsRange * 0.75f;
            SolarSystem thisSystem = Owner.System;
            if(thisSystem != null)
                foreach (Planet p in thisSystem.PlanetList)
                {
                    BadGuysNear =BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) && Owner.Center.InRadius(p.Position, Radius);
                    //@TODO remove below once new logic is checked
                    //Empire emp = p.Owner;
                    //if (emp !=null && emp != Owner.loyalty)
                    //{
                    //    Relationship test = null;
                    //    Owner.loyalty.TryGetRelations(emp, out test);
                    //    if (!test.Treaty_OpenBorders || !test.Treaty_NAPact || Vector2.Distance(Owner.Center, p.Position) >Radius)
                    //        BadGuysNear = true;
                    //    break;
                    //}
                }
            {
                if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
                {
                    var sw = new ShipWeight
                    {
                        ship = EscortTarget.AI.Target as Ship,
                        weight = 2f
                    };
                    NearbyShips.Add(sw);
                }
                Ship[] nearbyShips = Owner.GetNearby<Ship>();
                foreach (Ship nearbyShip in nearbyShips)
                {
                    if (!nearbyShip.Active || nearbyShip.dying
                        || Owner.Center.OutsideRadius(nearbyShip.Center, Radius + (Radius < 0.01f ? 10000 : 0)))
                        continue;

                    Empire empire = nearbyShip.loyalty;
                    if (empire == Owner.loyalty)
                    {
                        FriendliesNearby.Add(nearbyShip);
                        continue;
                    }
                    bool isAttackable = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                    if (!isAttackable) continue;
                    BadGuysNear = true;
                    if (Radius < 1)
                        continue;
                    var sw = new ShipWeight
                    {
                        ship = nearbyShip,
                        weight = 1f
                    };
                    NearbyShips.Add(sw);

                    if (BadGuysNear && nearbyShip.AI.Target is Ship targetShip && 
                        targetShip == EscortTarget && nearbyShip.engineState != Ship.MoveState.Warp)
                    {
                        sw.weight = 3f;
                    }
                }
            }


            #region supply ship logic   //fbedard: for launch only
            if (Owner.GetHangars().Find(hangar => hangar.IsSupplyBay) !=null && Owner.engineState != Ship.MoveState.Warp)  // && !this.Owner.isSpooling
            {
                IOrderedEnumerable<Ship> sortedList = null;
                {
                    sortedList = FriendliesNearby.Where(ship => ship != Owner 
                        && ship.engineState != Ship.MoveState.Warp
                        && ship.AI.State != AIState.Scrap
                        && ship.AI.State != AIState.Resupply
                        && ship.AI.State != AIState.Refit
                        && ship.Mothership == null 
                        && ship.OrdinanceMax > 0 
                        && ship.Ordinance / ship.OrdinanceMax < 0.5f
                        && !ship.IsTethered())
                        .OrderBy(ship => Math.Truncate(Vector2.Distance(Owner.Center, ship.Center) + 4999) / 5000).ThenByDescending(ship => ship.OrdinanceMax - ship.Ordinance);
//                      .OrderBy(ship => ship.HasSupplyBays).ThenBy(ship => ship.OrdAddedPerSecond).ThenBy(ship => Math.Truncate((Vector2.Distance(this.Owner.Center, ship.Center) + 4999)) / 5000).ThenBy(ship => ship.OrdinanceMax - ship.Ordinance);
                }

                    if (sortedList.Count() > 0)
                    {
                        var skip = 0;
                        var inboundOrdinance = 0f;
                    if(Owner.HasSupplyBays)
                        foreach (ShipModule hangar in Owner.GetHangars().Where(hangar => hangar.IsSupplyBay))
                        {
                            if (hangar.GetHangarShip() != null && hangar.GetHangarShip().Active)
                            {
                                if (hangar.GetHangarShip().AI.State != AIState.Ferrying && hangar.GetHangarShip().AI.State != AIState.ReturnToHangar && hangar.GetHangarShip().AI.State != AIState.Resupply && hangar.GetHangarShip().AI.State != AIState.Scrap)
                                {
                                    if (sortedList.Skip(skip).Count() > 0)
                                    {
                                        var g1 = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
                                        hangar.GetHangarShip().AI.EscortTarget = sortedList.Skip(skip).First();

                                        hangar.GetHangarShip().AI.IgnoreCombat = true;
                                        hangar.GetHangarShip().AI.OrderQueue.Clear();
                                        hangar.GetHangarShip().AI.OrderQueue.Enqueue(g1);
                                        hangar.GetHangarShip().AI.State = AIState.Ferrying;
                                        continue;
                                    }
                                    else
                                    {
                                        //hangar.GetHangarShip().QueueTotalRemoval();
                                        hangar.GetHangarShip().AI.State = AIState.ReturnToHangar;  //shuttle with no target
                                        continue;
                                    }
                                }
                                else if (sortedList.Skip(skip).Count() > 0 && hangar.GetHangarShip().AI.EscortTarget == sortedList.Skip(skip).First() && hangar.GetHangarShip().AI.State == AIState.Ferrying)
                                {
                                    inboundOrdinance = inboundOrdinance + 100f;
                                    if ((inboundOrdinance + sortedList.Skip(skip).First().Ordinance) / sortedList.First().OrdinanceMax > 0.5f)
                                    {
                                        skip++;
                                        inboundOrdinance = 0;
                                        continue;
                                    }
                                }
                                continue;
                            }
                            if (!hangar.Active || hangar.hangarTimer > 0f || Owner.Ordinance >= 100f && sortedList.Skip(skip).Any() )
                                continue;                            
                            if (ResourceManager.ShipsDict["Supply_Shuttle"].Mass / 5f > Owner.Ordinance)  //fbedard: New spawning cost
                                continue;
                            Ship shuttle = ResourceManager.CreateShipFromHangar("Supply_Shuttle", Owner.loyalty, Owner.Center, Owner);
                            shuttle.VanityName = "Supply Shuttle";
                            //shuttle.shipData.Role = ShipData.RoleName.supply;
                            //shuttle.GetAI().DefaultAIState = AIState.Flee;
                            shuttle.Velocity = UniverseRandom.RandomDirection() * shuttle.speed + Owner.Velocity;
                            if (shuttle.Velocity.Length() > shuttle.velocityMaximum)
                                shuttle.Velocity = Vector2.Normalize(shuttle.Velocity) * shuttle.speed;
                            Owner.Ordinance -= shuttle.Mass / 5f;

                            if (Owner.Ordinance >= 100f)
                            {
                                inboundOrdinance = inboundOrdinance + 100f;
                                Owner.Ordinance = Owner.Ordinance - 100f;
                                hangar.SetHangarShip(shuttle);
                                var g = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
                                shuttle.AI.EscortTarget = sortedList.Skip(skip).First();
                                shuttle.AI.IgnoreCombat = true;
                                shuttle.AI.OrderQueue.Clear();
                                shuttle.AI.OrderQueue.Enqueue(g);
                                shuttle.AI.State = AIState.Ferrying;
                            }
                            else  //fbedard: Go fetch ordinance when mothership is low on ordinance
                            {
                                shuttle.Ordinance = 0f;
                                hangar.SetHangarShip(shuttle);
                                shuttle.AI.IgnoreCombat = true;
                                shuttle.AI.State = AIState.Resupply;
                                shuttle.AI.OrderResupplyNearest(true);
                            }
                            break;
                        }
                    }
    
            } 
            if (Owner.shipData.Role == ShipData.RoleName.supply && Owner.Mothership == null)
                OrderScrapShip();   //Destroy shuttle without mothership

            #endregion

            //}           
            foreach (ShipWeight nearbyShip in NearbyShips )
                // Doctor: I put modifiers for the ship roles Fighter and Bomber in here, so that when searching for targets they prioritise their targets based on their selected ship role.
                // I'll additionally put a ScanForCombatTargets into the carrier fighter code such that they use this code to select their own weighted targets.
            //Parallel.ForEach(this.NearbyShips, nearbyShip =>
                if (nearbyShip.ship.loyalty != Owner.loyalty)
                {
                    if (Target as Ship == nearbyShip.ship)
                        nearbyShip.weight += 3;
                    if (nearbyShip.ship.Weapons.Count ==0)
                    {
                        ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.weight = vultureWeight.weight + CombatAI.PirateWeight;
                    }
                    
                    if (nearbyShip.ship.Health / nearbyShip.ship.HealthMax < 0.5f)
                    {
                        ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.weight = vultureWeight.weight + CombatAI.VultureWeight;
                    }
                    if (nearbyShip.ship.Size < 30)
                    {
                        ShipWeight smallAttackWeight = nearbyShip;
                        smallAttackWeight.weight = smallAttackWeight.weight + CombatAI.SmallAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                            smallAttackWeight.weight *= 2f;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            smallAttackWeight.weight /= 2f;
                    }
                    if (nearbyShip.ship.Size > 30 && nearbyShip.ship.Size < 100)
                    {
                        ShipWeight mediumAttackWeight = nearbyShip;
                        mediumAttackWeight.weight = mediumAttackWeight.weight + CombatAI.MediumAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            mediumAttackWeight.weight *= 1.5f;
                    }
                    if (nearbyShip.ship.Size > 100)
                    {
                        ShipWeight largeAttackWeight = nearbyShip;
                        largeAttackWeight.weight = largeAttackWeight.weight + CombatAI.LargeAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                            largeAttackWeight.weight /= 2f;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            largeAttackWeight.weight *= 2f;
                    }
                    float rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, Owner.Center);
                    if (rangeToTarget <= CombatAI.PreferredEngagementDistance) 
                        // && Vector2.Distance(nearbyShip.ship.Center, this.Owner.Center) >= this.Owner.maxWeaponsRange)
                    {
                        ShipWeight shipWeight = nearbyShip;
                        shipWeight.weight = (int)Math.Ceiling(shipWeight.weight + 5 *
                                                              ((CombatAI.PreferredEngagementDistance -Vector2.Distance(Owner.Center,nearbyShip.ship.Center))
                                                               / CombatAI.PreferredEngagementDistance  ))
                            
                            ;
                    }
                    else if (rangeToTarget > CombatAI.PreferredEngagementDistance + Owner.velocityMaximum * 5)
                    {
                        ShipWeight shipWeight1 = nearbyShip;
                        shipWeight1.weight = shipWeight1.weight - 2.5f * (rangeToTarget / (CombatAI.PreferredEngagementDistance + Owner.velocityMaximum * 5));
                    }
                    if(Owner.Mothership !=null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, Owner.Mothership.Center);
                        if (rangeToTarget < CombatAI.PreferredEngagementDistance)
                            nearbyShip.weight += 1;

                    }
                    if (EscortTarget != null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, EscortTarget.Center);
                        if( rangeToTarget <5000) // / (this.CombatAI.PreferredEngagementDistance +this.Owner.velocityMaximum ))
                            nearbyShip.weight += 1;
                        else
                            nearbyShip.weight -= 2;
                        if (nearbyShip.ship.AI.Target == EscortTarget)
                            nearbyShip.weight += 1;

                    }
                    if(nearbyShip.ship.Weapons.Count <1)
                        nearbyShip.weight -= 3;

                    foreach (ShipWeight otherShip in NearbyShips)
                        if (otherShip.ship.loyalty != Owner.loyalty)
                        {
                            if (otherShip.ship.AI.Target != Owner)
                                continue;
                            ShipWeight selfDefenseWeight = nearbyShip;
                            selfDefenseWeight.weight = selfDefenseWeight.weight + 0.2f * CombatAI.SelfDefenseWeight;
                        }
                        //else if (otherShip.ship.GetAI().Target != nearbyShip.ship)
                        //{
                        //    continue;
                        //}
                }
                else
                {
                    NearbyShips.QueuePendingRemoval(nearbyShip);
                }
            //this.PotentialTargets = this.NearbyShips.Where(loyalty=> loyalty.ship.loyalty != this.Owner.loyalty) .OrderBy(weight => weight.weight).Select(ship => ship.ship).ToList();
            //if (this.Owner.Role == ShipData.RoleName.platform)
            //{
            //    this.NearbyShips.ApplyPendingRemovals();
            //    IEnumerable<ArtificialIntelligence.ShipWeight> sortedList =
            //        from potentialTarget in this.NearbyShips
            //        orderby Vector2.Distance(this.Owner.Center, potentialTarget.ship.Center)
            //        select potentialTarget;
            //    if (sortedList.Count<ArtificialIntelligence.ShipWeight>() > 0)
            //    {
            //        this.Target = sortedList.ElementAt<ArtificialIntelligence.ShipWeight>(0).ship;
            //    }
            //    return this.Target;
            //}
            NearbyShips.ApplyPendingRemovals();
            IEnumerable<ShipWeight> sortedList2 =
                from potentialTarget in NearbyShips
                orderby potentialTarget.weight descending //, Vector2.Distance(potentialTarget.ship.Center,this.Owner.Center) 
                select potentialTarget;
            
            {
                //this.PotentialTargets.ClearAdd() ;//.ToList() as BatchRemovalCollection<Ship>;

                //trackprojectiles in scan for targets.

                PotentialTargets.ClearAdd(sortedList2.Select(ship => ship.ship));
                   // .Where(potentialTarget => Vector2.Distance(potentialTarget.Center, this.Owner.Center) < this.CombatAI.PreferredEngagementDistance));
                    
            }
            if (Target != null && !Target.Active)
            {
                Target = null;
                hasPriorityTarget = false;
            }
            else if (Target != null && Target.Active && hasPriorityTarget)
            {
                var ship = Target as Ship;
                if (Owner.loyalty.GetRelations(ship.loyalty).AtWar || Owner.loyalty.isFaction || ship.loyalty.isFaction)
                    BadGuysNear = true;
                return Target;
            }
            if (sortedList2.Count<ShipWeight>() > 0)
                Target = sortedList2.ElementAt<ShipWeight>(0).ship;

            if (Owner.Weapons.Count > 0 || Owner.GetHangars().Count > 0)
                return Target;          
            return null;
        }
        //Targeting SetCombatStatus
        private void SetCombatStatus(float elapsedTime)
        {
            //if(this.State==AIState.Scrap)
            //{
            //    this.Target = null;
            //    this.Owner.InCombatTimer = 0f;
            //    this.Owner.InCombat = false;
            //    this.TargetQueue.Clear();
            //    return;
                
            //}
            var radius = 30000f;
            Vector2 senseCenter = Owner.Center;
            if (UseSensorsForTargets)
                if (Owner.Mothership != null)
                {
                    if (Vector2.Distance(Owner.Center, Owner.Mothership.Center) <= Owner.Mothership.SensorRange - Owner.SensorRange)
                    {
                        senseCenter = Owner.Mothership.Center;
                        radius = Owner.Mothership.SensorRange;
                    }
                }
                else
                {
                    radius = Owner.SensorRange;
                    if (Owner.inborders) radius += 10000;
                }
            else if (Owner.Mothership != null )
                senseCenter = Owner.Mothership.Center;


            if (Owner.fleet != null)
            {
                if (!hasPriorityTarget)
                    Target = ScanForCombatTargets(senseCenter, radius);
                else
                    ScanForCombatTargets(senseCenter, radius);
            }
            else if (!hasPriorityTarget)
            {
                //#if DEBUG
                //                if (this.State == AIState.Intercept && this.Target != null)
                //                    Log.Info(this.Target); 
                //#endif
                if (Owner.Mothership != null)
                {
                    Target = ScanForCombatTargets(senseCenter, radius);

                    if (Target == null)
                        Target = Owner.Mothership.AI.Target;
                }
                else
                {
                    Target = ScanForCombatTargets(senseCenter, radius);
                }
            }
            else
            {

                if (Owner.Mothership != null)
                    Target = ScanForCombatTargets(senseCenter, radius) ?? Owner.Mothership.AI.Target;
                else
                    ScanForCombatTargets(senseCenter, radius);
            }
            if (State == AIState.Resupply)
                return;
            if ((Owner.shipData.Role == ShipData.RoleName.freighter || Owner.shipData.ShipCategory == ShipData.Category.Civilian) && Owner.CargoSpaceMax > 0 || Owner.shipData.Role == ShipData.RoleName.scout || Owner.isConstructor || Owner.shipData.Role == ShipData.RoleName.troop || IgnoreCombat || State == AIState.Resupply || State == AIState.ReturnToHangar || State == AIState.Colonize || Owner.shipData.Role == ShipData.RoleName.supply)
                return;
            if (Owner.fleet != null && State == AIState.FormationWarp)
            {
                bool doreturn = !(Owner.fleet != null && State == AIState.FormationWarp && Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset) < 15000f);
                if (doreturn)
                    return;
            }
            if (Owner.fleet != null)
                foreach (FleetDataNode datanode in Owner.fleet.DataNodes)
                {
                    if (datanode.Ship!= Owner)
                        continue;
                    node = datanode;
                    break;
                }
            if (Target != null && !Owner.InCombat)
            {
                Owner.InCombatTimer = 15f;
                if (!HasPriorityOrder && OrderQueue.NotEmpty && OrderQueue.PeekFirst.Plan != Plan.DoCombat)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                }
                else if (!HasPriorityOrder)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                }
                else 
                {
                    if (CombatState == CombatState.HoldPosition || OrderQueue.NotEmpty)
                        return;
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                }
            }
        }

        private void ScrapShip(float elapsedTime, ShipGoal goal)
        {
            if (Vector2.Distance(goal.TargetPlanet.Position, Owner.Center) >= goal.TargetPlanet.ObjectRadius + Owner.Radius)   //2500f)   //OrbitTarget.ObjectRadius *15)
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
            Owner.loyalty.GetGSAI().recyclepool++;
        }

        private void SetCombatStatusorig(float elapsedTime)
        {
            if (Owner.fleet != null)
                if (!hasPriorityTarget)
                    Target = ScanForCombatTargets(Owner.Center, 30000f);
                else
                    ScanForCombatTargets(Owner.Center, 30000f);
            else if (!hasPriorityTarget)
                Target = ScanForCombatTargets(Owner.Center, 30000f);
            else
                ScanForCombatTargets(Owner.Center, 30000f);
            if (State == AIState.Resupply)
                return;
            if ((Owner.shipData.Role == ShipData.RoleName.freighter || Owner.shipData.ShipCategory == ShipData.Category.Civilian || Owner.shipData.Role == ShipData.RoleName.scout || Owner.isConstructor || Owner.shipData.Role == ShipData.RoleName.troop || IgnoreCombat || State == AIState.Resupply || State == AIState.ReturnToHangar) && !Owner.IsSupplyShip)
                return;
            if (Owner.fleet != null && State == AIState.FormationWarp)
            {
                var doreturn = true;
                if (Owner.fleet != null && State == AIState.FormationWarp && Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset) < 15000f)
                    doreturn = false;
                if (doreturn)
                    return;
            }
            if (Owner.fleet != null)
                foreach (FleetDataNode datanode in Owner.fleet.DataNodes)
                {
                    if (datanode.Ship!= Owner)
                        continue;
                    node = datanode;
                    break;
                }
            if (Target != null && !Owner.InCombat)
            {
                Owner.InCombat = true;
                Owner.InCombatTimer = 15f;
                if (!HasPriorityOrder && OrderQueue.NotEmpty && OrderQueue.PeekFirst.Plan != Plan.DoCombat)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                    return;
                }
                if (!HasPriorityOrder)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                    return;
                }
                if (HasPriorityOrder && CombatState != CombatState.HoldPosition && OrderQueue.IsEmpty)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                    return;
                }
            }
            else if (Target == null)
            {
                Owner.InCombat = false;
            }
        }

        public void SetPriorityOrder()
        {
            OrderQueue.Clear();
            HasPriorityOrder = true;
            Intercepting = false;
            hasPriorityTarget = false;
        }

        private void Stop(float elapsedTime)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity == Vector2.Zero || Owner.Velocity.Length() > Owner.VelocityLast.Length())
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            if (Owner.Velocity.Length() / Owner.velocityMaximum <= elapsedTime || (forward.X <= 0f || Owner.Velocity.X <= 0f) && (forward.X >= 0f || Owner.Velocity.X >= 0f))
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            Ship owner = Owner;
            owner.Velocity = owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Owner.velocityMaximum);
        }

        private void Stop(float elapsedTime, ShipGoal Goal)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity == Vector2.Zero || Owner.Velocity.Length() > Owner.VelocityLast.Length())
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                return;
            }
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            if (Owner.Velocity.Length() / Owner.velocityMaximum <= elapsedTime || (forward.X <= 0f || Owner.Velocity.X <= 0f) && (forward.X >= 0f || Owner.Velocity.X >= 0f))
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            Ship owner = Owner;
            owner.Velocity = owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Owner.velocityMaximum);
        }
        //movement StopWithBackwardsThrust
        private void StopWithBackwardsThrust(float elapsedTime, ShipGoal Goal)
        {
            if(Goal.TargetPlanet !=null)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Last().Equals(Goal.TargetPlanet.Position);
                    Goal.MovePosition = Goal.TargetPlanet.Position;
                }
            if (Owner.loyalty == EmpireManager.Player)
                HadPO = true;
            HasPriorityOrder = false;
            float Distance = Vector2.Distance(Owner.Center, Goal.MovePosition);
            //if (Distance < 100f && Distance < 25f)
            if (Distance < 200f)  //fbedard
            {
                OrderQueue.RemoveFirst();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
                Owner.Velocity = Vector2.Zero;
                if (Owner.loyalty == EmpireManager.Player)
                    HadPO = true;
                HasPriorityOrder = false;
            }
            Owner.HyperspaceReturn();
            //Vector2 forward2 = Quaternion
            //Quaternion.AngleAxis(_angle, Vector3.forward) * normalizedDirection1
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            if (Owner.Velocity == Vector2.Zero || Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Goal.MovePosition) > Vector2.Distance(Owner.Center, Goal.MovePosition))
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                if (ActiveWayPoints.Count > 0)
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Dequeue();
                    }
                return;
            }
            Vector2 velocity = Owner.Velocity;
            float timetostop = velocity.Length() / Goal.SpeedLimit;
            //added by gremlin devekmod timetostopfix
            if (Vector2.Distance(Owner.Center, Goal.MovePosition) / Goal.SpeedLimit <= timetostop + .005) 
            //if (Vector2.Distance(this.Owner.Center, Goal.MovePosition) / (this.Owner.Velocity.Length() + 0.001f) <= timetostop)
            {
                Ship owner = Owner;
                owner.Velocity = owner.Velocity + Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
            }
            else
            {
                Ship ship = Owner;
                ship.Velocity = ship.Velocity + Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                {
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
                    return;
                }
            }
        }
        private void StopWithBackwardsThrustbroke(float elapsedTime, ShipGoal Goal)
        {
            
            if (Owner.loyalty == EmpireManager.Player)
                HadPO = true;
            HasPriorityOrder = false;
            float Distance = Vector2.Distance(Owner.Center, Goal.MovePosition);
            if (Distance < 200 )//&& Distance > 25f)
            {
                OrderQueue.RemoveFirst();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
                Owner.Velocity = Vector2.Zero;
                if (Owner.loyalty == EmpireManager.Player)
                    HadPO = true;
                HasPriorityOrder = false;
            }
            Owner.HyperspaceReturn();
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            if (Owner.Velocity == Vector2.Zero || Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Goal.MovePosition) > Vector2.Distance(Owner.Center, Goal.MovePosition))
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                if (ActiveWayPoints.Count > 0)
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Dequeue();
                    }
                return;
            }
            Vector2 velocity = Owner.Velocity;
            float timetostop = (int)velocity.Length() / Goal.SpeedLimit;
            if (Vector2.Distance(Owner.Center, Goal.MovePosition) / Goal.SpeedLimit <= timetostop + .005) //(this.Owner.Velocity.Length() + 1)
                if (Math.Abs((int)(DistanceLast - Distance)) < 10)
                {
                    var to1K = new ShipGoal(Plan.MakeFinalApproach, Goal.MovePosition, 0f)
                    {
                        SpeedLimit = Owner.speed > Distance ? Distance : Owner.GetSTLSpeed()
                    };
                    lock (WayPointLocker)
                    {
                        OrderQueue.PushToFront(to1K);
                    }
                    DistanceLast = Distance;
                    return;
                }
            if (Vector2.Distance(Owner.Center, Goal.MovePosition) / (Owner.Velocity.Length() + 0.001f) <= timetostop)
            {
                Ship owner = Owner;
                owner.Velocity = owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
            }
            else
            {
                Ship ship = Owner;
                ship.Velocity = ship.Velocity + Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                {
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
                    return;
                }
            }

            DistanceLast = Distance;
        }
        // bookmark : Main Movement Code
                
        private void ThrustTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit)        //Gretman's Version
        {
            if (speedLimit == 0f) speedLimit = Owner.speed;
            float Distance = Vector2.Distance(Position, Owner.Center);
            if (Owner.engineState != Ship.MoveState.Warp) Position = Position - Owner.Velocity;
            if (Owner.EnginesKnockedOut) return;

            Owner.isThrusting = true;
            Vector2 wantedForward = Owner.Center.FindVectorToTarget(Position);
            var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
            float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;

            float TurnRate = Owner.TurnThrust / Owner.Mass / 700f;

            #region Warp

            if (angleDiff * 1.25f > TurnRate && Distance > 2500f && Owner.engineState == Ship.MoveState.Warp)      //Might be a turning issue
            {
                if (angleDiff > 1.0f)
                {
                    Owner.HyperspaceReturn();      //Too sharp of a turn. Drop out of warp
                }
                else {
                    float WarpSpeed = (Owner.WarpThrust / Owner.Mass + 0.1f) * Owner.loyalty.data.FTLModifier;
                    if (Owner.inborders && Owner.loyalty.data.Traits.InBordersSpeedBonus > 0) WarpSpeed *= 1 + Owner.loyalty.data.Traits.InBordersSpeedBonus;

                    if (Owner.VanityName == "MerCraft") Log.Info("AngleDiff: " + angleDiff + "     TurnRate = " + TurnRate + "     WarpSpeed = " + WarpSpeed + "     Distance = " + Distance);
                    //AngleDiff: 1.500662     TurnRate = 0.2491764     WarpSpeed = 26286.67     Distance = 138328.4

                    if (ActiveWayPoints.Count >= 2 && Distance > Empire.ProjectorRadius / 2 && Vector2.Distance(Owner.Center, ActiveWayPoints.ElementAt(1)) < Empire.ProjectorRadius * 5)
                    {
                        Vector2 wantedForwardNext = Owner.Center.FindVectorToTarget(ActiveWayPoints.ElementAt(1));
                        var angleDiffNext = (float)Math.Acos((double)Vector2.Dot(wantedForwardNext, forward));
                        if (angleDiff > angleDiffNext || angleDiffNext < TurnRate * 0.5) //Angle to next waypoint is better than angle to this one, just cut the corner.
                        {
                            lock (WayPointLocker)
                            {
                                ActiveWayPoints.Dequeue();
                            }
                            if (OrderQueue.NotEmpty)      OrderQueue.RemoveFirst();
                            return;
                        }
                    }
                    //                          Turn per tick         ticks left          Speed per tic
                    else if (angleDiff > TurnRate / elapsedTime * (Distance / (WarpSpeed / elapsedTime) ) )       //Can we make the turn in the distance we have remaining?
                    {
                        Owner.WarpThrust -= Owner.NormalWarpThrust * 0.02f;   //Reduce warpthrust by 2 percent every frame until this is an acheivable turn
                    }
                    else if (Owner.WarpThrust < Owner.NormalWarpThrust)
                    {
                        Owner.WarpThrust += Owner.NormalWarpThrust * 0.01f;   //Increase warpthrust back to normal 1 percent at a time
                        if (Owner.WarpThrust > Owner.NormalWarpThrust) Owner.WarpThrust = Owner.NormalWarpThrust;    //Make sure we dont accidentally go over
                    }
                }
            }
            else if (Owner.WarpThrust < Owner.NormalWarpThrust && angleDiff < TurnRate) //Intentional allowance of the 25% added to angle diff in main if, so it wont accelerate too soon
            {
                Owner.WarpThrust += Owner.NormalWarpThrust * 0.01f;   //Increase warpthrust back to normal 1 percent at a time
                if (Owner.WarpThrust > Owner.NormalWarpThrust) Owner.WarpThrust = Owner.NormalWarpThrust;    //Make sure we dont accidentally go over
            }

            #endregion

            if (hasPriorityTarget && Distance < Owner.maxWeaponsRange * 0.85f)        //If chasing something, and within weapons range
            {
                if (Owner.engineState == Ship.MoveState.Warp) Owner.HyperspaceReturn();
            }
            else if (!HasPriorityOrder && !hasPriorityTarget && Distance < 1000f && ActiveWayPoints.Count <= 1 && Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn();
            }

            if (angleDiff > 0.025f)     //Stuff for the ship visually banking on the Y axis when turning
            {
                float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                if (RotAmount > 0f && Owner.yRotation > -Owner.maxBank) Owner.yRotation = Owner.yRotation - Owner.yBankAmount;
                else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank) Owner.yRotation = Owner.yRotation + Owner.yBankAmount;
                Owner.isTurning = true;
                Owner.Rotation = Owner.Rotation + (RotAmount > angleDiff ? angleDiff : RotAmount);
                return;       //I'm not sure about the return statement here. -Gretman
            }

            if (State != AIState.FormationWarp || Owner.fleet == null)        //not in a fleet
            {
                if (Distance > 7500f && !Owner.InCombat && angleDiff < 0.25f) Owner.EngageStarDrive();
                else if (Distance > 15000f && Owner.InCombat && angleDiff < 0.25f) Owner.EngageStarDrive();
            }
            else        //In a fleet
            {
                if (Distance > 7500f)   //Not near destination
                {
                    var fleetReady = true;
                    
                    using (Owner.fleet.Ships.AcquireReadLock())
                    {
                        foreach (Ship ship in Owner.fleet.Ships)
                        {
                            if (ship.AI.State != AIState.FormationWarp) continue;
                            if (ship.AI.ReadyToWarp && (ship.PowerCurrent / (ship.PowerStoreMax + 0.01f) >= 0.2f || ship.isSpooling))
                            {
                                if (Owner.FightersOut) Owner.RecoverFighters();       //Recall Fighters
                                continue;
                            }
                            fleetReady = false;
                            break;
                        }
                    }

                    float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove;
                    speedLimit = Owner.fleet.Speed;

                #region FleetGrouping
                #if true
                    if (Distance <= distanceFleetCenterToDistance)
                    {
                        float speedreduction = distanceFleetCenterToDistance - Distance;
                        speedLimit = Owner.fleet.Speed - speedreduction;

                        if (speedLimit > Owner.fleet.Speed) speedLimit = Owner.fleet.Speed;
                    }
                    else if (Distance > distanceFleetCenterToDistance && Distance > Owner.speed)
                    {
                        float speedIncrease = Distance - distanceFleetCenterToDistance;
                        speedLimit = Owner.fleet.Speed + speedIncrease;
                    }
                #endif
                #endregion

                    if (fleetReady) Owner.EngageStarDrive();   //Fleet is ready to Go into warp
                    else if (Owner.engineState == Ship.MoveState.Warp) Owner.HyperspaceReturn(); //Fleet is not ready for warp
                }
                else if (Owner.engineState == Ship.MoveState.Warp)
                {
                    Owner.HyperspaceReturn(); //Near Destination
                }
            }

            if (speedLimit > Owner.velocityMaximum) speedLimit = Owner.velocityMaximum;
            else if (speedLimit < 0) speedLimit = 0;

            Owner.Velocity = Owner.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
            if (Owner.Velocity.Length() > speedLimit) Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
        }

        private void ThrustTowardsPositionOld(Vector2 Position, float elapsedTime, float speedLimit)
        {
            if (speedLimit == 0f)
                speedLimit = Owner.speed;
            float Ownerspeed = Owner.speed;
            if (Ownerspeed > speedLimit)
                Ownerspeed = speedLimit;
            float Distance = Position.Distance(Owner.Center);
 
            if (Owner.engineState != Ship.MoveState.Warp )
                Position = Position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;

                Vector2 wantedForward = Vector2.Normalize(Owner.Center.FindVectorToTarget(Position));
                var forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
                var right = new Vector2(-forward.Y, forward.X);
                double angleDiff = Math.Acos(Vector2.Dot(wantedForward, forward));
                double facing = Vector2.Dot(wantedForward, right)> 0f ? 1f : -1f;
#region warp
                if (angleDiff > 0.25f && Owner.engineState == Ship.MoveState.Warp)
                {
                    if (Owner.VanityName == "MerCraftA") Log.Info("angleDiff: " + angleDiff);
                    if (ActiveWayPoints.Count > 1)
                    {
                        if (angleDiff > 1.0f)
                        {
                            Owner.HyperspaceReturn();
                            if (Owner.VanityName == "MerCraft") Log.Info("Dropped out of warp:  Master Angle too large for warp." 
                                + "   angleDiff: " + angleDiff);
                        }
                        if (Distance <= Empire.ProjectorRadius / 2f)
                            if (angleDiff > 0.25f) //Gretman tinkering with fbedard's 2nd attempt to smooth movement around waypoints
                            {
                                if (Owner.VanityName == "MerCraft") Log.Info("Pre Dequeue Queue size:  " + ActiveWayPoints.Count);
                                lock (WayPointLocker)
                                {
                                    ActiveWayPoints.Dequeue();
                                }
                                if (Owner.VanityName == "MerCraft") Log.Info("Post Dequeue Pre Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                if (OrderQueue.NotEmpty)
                                    OrderQueue.RemoveFirst();
                                if (Owner.VanityName == "MerCraft") Log.Info("Post Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                Position = ActiveWayPoints.First();
                                Distance = Vector2.Distance(Position, Owner.Center);
                                wantedForward = Owner.Center.FindVectorToTarget(Position);
                                forward = new Vector2((float)Math.Sin((double)Owner.Rotation), -(float)Math.Cos((double)Owner.Rotation));
                                angleDiff = Math.Acos((double)Vector2.Dot(wantedForward, forward));

                                speedLimit = speedLimit * 0.75f;
                                if (Owner.VanityName == "MerCraft") Log.Info("Rounded Corner:  Slowed down.   angleDiff: {0}", angleDiff);
                            }
                            else
                            {
                                if (Owner.VanityName == "MerCraft") Log.Info("Pre Dequeue Queue size:  " + ActiveWayPoints.Count);
                                lock (WayPointLocker)
                                {
                                    ActiveWayPoints.Dequeue();
                                }
                                if (Owner.VanityName == "MerCraft") Log.Info("Post Dequeue Pre Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                if (OrderQueue.NotEmpty)
                                    OrderQueue.RemoveFirst();
                                if (Owner.VanityName == "MerCraft") Log.Info("Post Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                Position = ActiveWayPoints.First();
                                Distance = Vector2.Distance(Position, Owner.Center);
                                wantedForward = Owner.Center.FindVectorToTarget(Position);
                                forward = new Vector2((float)Math.Sin(Owner.Rotation), -(float)Math.Cos(Owner.Rotation));
                                angleDiff = Math.Acos(Vector2.Dot(wantedForward, forward));
                                if (Owner.VanityName == "MerCraft") Log.Info("Rounded Corner:  Did not slow down." + "   angleDiff: " + angleDiff);
                            }
                    }
                    else if (Target != null)
                    {
                        float d = Vector2.Distance(Target.Center, Owner.Center);
                        if (angleDiff > 0.400000005960464f)
                            Owner.HyperspaceReturn();
                        else if (d > 25000f)
                            Owner.HyperspaceReturn();
                    }
                    else if (State != AIState.Bombard && State != AIState.AssaultPlanet && State != AIState.BombardTroops && !IgnoreCombat || OrderQueue.IsEmpty)
                    {
                        Owner.HyperspaceReturn();
                    }
                    else if (OrderQueue.PeekLast.TargetPlanet != null)
                    {
                        float d = OrderQueue.PeekLast.TargetPlanet.Position.Distance(Owner.Center);
                        wantedForward = Owner.Center.FindVectorToTarget(OrderQueue.PeekLast.TargetPlanet.Position);
                        angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));                        
                        if (angleDiff > 0.400000005960464f)
                            Owner.HyperspaceReturn();
                        else if (d > 25000f)
                            Owner.HyperspaceReturn();
                    }
                    else if (angleDiff > .25)
                    {
                        Owner.HyperspaceReturn();
                    }
                }
#endregion

                if (hasPriorityTarget && Distance < Owner.maxWeaponsRange)
                {
                    if (Owner.engineState == Ship.MoveState.Warp)
                        Owner.HyperspaceReturn();
                }
                else if (!HasPriorityOrder && !hasPriorityTarget && Distance < 1000f && ActiveWayPoints.Count <= 1 && Owner.engineState == Ship.MoveState.Warp)
                {
                    Owner.HyperspaceReturn();
                }
                float TurnSpeed = 1;
                if (angleDiff > Owner.yBankAmount*.1) 
                {
                    double RotAmount = Math.Min(angleDiff, facing *  Owner.yBankAmount); 
                    if (RotAmount > 0f)
                    {                        
                        if (Owner.yRotation > -Owner.maxBank)
                        {                            
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {                        
                        Ship owner1 = Owner;
                        owner1.yRotation = owner1.yRotation + Owner.yBankAmount;                        
                    }                
                    Owner.isTurning = true;
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + (RotAmount > angleDiff ? (float)angleDiff: (float)RotAmount);
                    {
                        float nimble = Owner.rotationRadiansPerSecond;
                        if (angleDiff < nimble)
                            TurnSpeed = (float)((nimble * 1.5 - angleDiff) / (nimble * 1.5));

                    }                   
                }
                if (State != AIState.FormationWarp || Owner.fleet == null)
                {
                    if (Distance > 7500f && !Owner.InCombat && angleDiff < 0.25f)
                        Owner.EngageStarDrive();
                    else if (Distance > 15000f && Owner.InCombat && angleDiff < 0.25f)
                        Owner.EngageStarDrive();
                    if (Owner.engineState == Ship.MoveState.Warp)
                        if (angleDiff > .1f)
                            speedLimit = Ownerspeed; 
                        else
                            speedLimit = (int)Owner.velocityMaximum;
                    else if (Distance > Ownerspeed * 10f)
                        speedLimit = Ownerspeed;
                    speedLimit *= TurnSpeed;
                    Ship velocity = Owner;
                    velocity.Velocity = velocity.Velocity +   Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                    if (Owner.Velocity.Length() > speedLimit)
                        Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit; 
                }
                else
                {
                    if (Distance > 7500f)                    
                    {
                        var fleetReady = true;
                        using (Owner.fleet.Ships.AcquireReadLock())
                        {
                            foreach (Ship ship in Owner.fleet.Ships)
                            {
                                if(ship.AI.State != AIState.FormationWarp)
                                    continue;
                                if (ship.AI.ReadyToWarp
                                
                                    && (ship.PowerCurrent / (ship.PowerStoreMax + 0.01f) >= 0.2f || ship.isSpooling ) 
                                )
                                {
                                    if (Owner.FightersOut)
                                        Owner.RecoverFighters();                                
                                    continue;
                                }
                                fleetReady = false;
                                break;
                            }
                        }

                        float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove; //
                            speedLimit = Owner.fleet.Speed;
#region FleetGrouping
                            float fleetPosistionDistance = Distance;
                            if (fleetPosistionDistance <= distanceFleetCenterToDistance )
                            {
                                float speedreduction = distanceFleetCenterToDistance - Distance;
                                speedLimit = (int)( Owner.fleet.Speed - speedreduction);
                                if (speedLimit < 0)
                                    speedLimit = 0;
                                else if (speedLimit > Owner.fleet.Speed)
                                    speedLimit = (int)Owner.fleet.Speed;
                            }
                            else if (fleetPosistionDistance > distanceFleetCenterToDistance && Distance > Ownerspeed)
                            {
                                float speedIncrease = Distance - distanceFleetCenterToDistance ;                             
                                speedLimit = (int)(Owner.fleet.Speed + speedIncrease);
  
                            }
#endregion
                            if (fleetReady)
                                Owner.EngageStarDrive();
                            else if (Owner.engineState == Ship.MoveState.Warp)
                                Owner.HyperspaceReturn();
                    }
                    else if (Owner.engineState == Ship.MoveState.Warp)
                    {
                        Owner.HyperspaceReturn();
                    }

                    if (speedLimit > Owner.velocityMaximum)
                        speedLimit = Owner.velocityMaximum;
                    else if (speedLimit < 0)
                        speedLimit = 0;
                    Ship velocity1 = Owner;
                    velocity1.Velocity = velocity1.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                    if (Owner.Velocity.Length() > speedLimit)
                    {
                        Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
                        return;
                    }
                }
            }
        }



        //added by gremlin Devekmod AuUpdate(fixed)
        public void Update(float elapsedTime)
        {

            ShipGoal toEvaluate;
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate)
                State = AIState.Exterminate;
            if (ClearOrdersNext)
            {
                OrderQueue.Clear();
                ClearOrdersNext = false;
                awaitClosest = null;
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
            if (!hasPriorityTarget)
                TargetQueue.Clear();
            if (Owner.loyalty == universeScreen.player &&
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
                            if (module.InstalledWeapon.timeToNextFire <= 0f &&
                                module.InstalledWeapon.moduleAttachedTo.Powered &&
                                Owner.Ordinance >= module.InstalledWeapon.OrdinanceRequiredToFire &&
                                Owner.PowerCurrent >= module.InstalledWeapon.PowerRequiredToFire)
                                DoRepairBeamLogic(module.InstalledWeapon);
                    if (Owner.HasRepairModule)
                        foreach (Weapon weapon in Owner.Weapons)
                        {
                            if (weapon.timeToNextFire > 0f || !weapon.moduleAttachedTo.Powered ||
                                Owner.Ordinance < weapon.OrdinanceRequiredToFire ||
                                Owner.PowerCurrent < weapon.PowerRequiredToFire || !weapon.IsRepairDrone)
                            {
                                //Gretman -- Added this so repair drones would cooldown outside combat (+15s)
                                if (weapon.timeToNextFire > 0f)
                                    weapon.timeToNextFire = MathHelper.Max(weapon.timeToNextFire - 1, 0f);
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
                                        if (Owner.loyalty != universeScreen.player)
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
                                            orderby Vector2.Distance(Owner.Center, p.Position)
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
                        Vector2 vector2 = MathExt.PointFromRadians(Vector2.Zero, Owner.fleet.Facing, 1f);
                        Vector2 fvec = Vector2.Zero.FindVectorToTarget(vector2);
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
            if (BadGuysNear)
            {
                using (OrderQueue.AcquireWriteLock())
                {
                    var docombat = false;
                    ShipGoal firstgoal = OrderQueue.PeekFirst;
                    if (Owner.Weapons.Count > 0 || Owner.GetHangars().Count > 0 || Owner.Transporters.Count > 0)
                    {

                        if (Target != null)
                            docombat = !HasPriorityOrder && !IgnoreCombat && State != AIState.Resupply &&
                                       (OrderQueue.IsEmpty ||
                                        firstgoal != null && firstgoal.Plan != Plan.DoCombat && firstgoal.Plan != Plan.Bombard &&
                                        firstgoal.Plan != Plan.BoardShip);

                        if (docombat)
                            OrderQueue.PushToFront(new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f));
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
                {
                    if (purge.fireTarget != null)
                    {
                        purge.PrimaryTarget = false;
                        purge.fireTarget = null;
                        purge.SalvoTarget = null;
                    }
                    if (purge.AttackerTargetting != null)
                        purge.AttackerTargetting.Clear();
                }
                if (Owner.GetHangars().Count > 0 && Owner.loyalty != universeScreen.player)
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
                            || hangar.GetHangarShip().AI.hasPriorityTarget
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

        public void DropBombsAtGoal(ShipGoal goal, float radius)
        {
            if (Owner.Center.InRadius(goal.TargetPlanet.Position, radius))
            {
                foreach (ShipModule bombBay in Owner.BombBays)
                {
                    if (bombBay.BombTimer > 0f)
                        continue;
                    var bomb = new Bomb(new Vector3(Owner.Center, 0f), Owner.loyalty)
                    {
                        WeaponName = bombBay.BombType
                    };
                    var wepTemplate = ResourceManager.WeaponsDict[bombBay.BombType];

                    if (Owner.Ordinance > wepTemplate.OrdinanceRequiredToFire)
                    {
                        Owner.Ordinance -= wepTemplate.OrdinanceRequiredToFire;
                        bomb.SetTarget(goal.TargetPlanet);
                        universeScreen.BombList.Add(bomb);
                        bombBay.BombTimer = wepTemplate.fireDelay;
                    }
                }
            }
        }

        public enum Plan
        {
            Stop,
            Scrap,
            HoldPosition,
            Bombard,
            Exterminate,
            RotateToFaceMovePosition,
            RotateToDesiredFacing,
            MoveToWithin1000,
            MakeFinalApproachFleet,
            MoveToWithin1000Fleet,
            MakeFinalApproach,
            RotateInlineWithVelocity,
            StopWithBackThrust,
            Orbit,
            Colonize,
            Explore,
            Rebase,
            DoCombat,
            MoveTowards,
            Trade,
            DefendSystem,
            TransportPassengers,
            PickupPassengers,
            DropoffPassengers,
            DeployStructure,
            PickupGoods,
            DropOffGoods,
            ReturnToHangar,
            TroopToShip,
            BoardShip,
            SupplyShip,
            Refit,
            LandTroop,
            MoveToWithin7500,
            BombTroops
        }

        public class ShipGoal
        {
            public Plan Plan;

            public Goal goal;

            public float VariableNumber;

            public string VariableString;

            public Fleet fleet;

            public Vector2 MovePosition;

            public float DesiredFacing;

            public float FacingVector;

            public Planet TargetPlanet;

            public float SpeedLimit = 1f;

            public ShipGoal(Plan p, Vector2 pos, float facing)
            {
                Plan = p;
                MovePosition = pos;
                DesiredFacing = facing;
            }
            public ShipGoal(Plan p, Vector2 pos, float facing, Planet targetPlanet)
            {
                Plan = p;
                MovePosition = pos;
                DesiredFacing = facing;
                TargetPlanet = targetPlanet;
            }
        }

        public class ShipWeight
        {
            public Ship ship;

            public float weight;
            public bool defendEscort;

            public ShipWeight() {}
        }

        public class WayPoints
        {
            public Planet planet { get; set; }
            public Ship ship { get; set; }            
            public Vector2 location { get ; set; }
        }
        private enum transportState
        {
            ChoosePickup,
            GoToPickup,
            ChooseDropDestination,
            GotoDrop,
            DoDrop
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ArtificialIntelligence() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            NearbyShips?.Dispose(ref NearbyShips);
            FriendliesNearby?.Dispose(ref FriendliesNearby);
            OrderQueue?.Dispose(ref OrderQueue);
            PotentialTargets?.Dispose(ref PotentialTargets);
        }        
    }
}