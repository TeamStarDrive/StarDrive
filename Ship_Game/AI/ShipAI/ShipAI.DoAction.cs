using System;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Ship_Game.Commands;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class ShipAI
    {

        
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
            if (!boarding && (OurOutStrength > 0 || OurTroopStrength > 0))
            {
                if (Owner.System?.OwnerList.Count > 0)
                {
                    Planet x = Owner.System.PlanetList.FindMinFiltered(
                        filter: p => p.Owner != null && p.Owner != Owner.loyalty || p.RecentCombat,
                        selector: p => Owner.Center.SqDist(p.Center));
                    if (x == null) return;
                    Owner.ScrambleAssaultShips(0);
                    OrderAssaultPlanet(x);
                }
            }
        }

        private void DoAttackRun(float elapsedTime)
        {
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            float adjustedWeaponRange = Owner.maxWeaponsRange * .35f;
            float spacerdistance = Owner.Radius * 3 + Target.Radius;
            if (spacerdistance > adjustedWeaponRange)
                spacerdistance = adjustedWeaponRange;

            if (distanceToTarget > spacerdistance && distanceToTarget > adjustedWeaponRange)
            {
                RunTimer = 0f;
                AttackRunStarted = false;

                Vector2 interceptPoint = Owner.Center.ProjectImpactPoint(
                                            Vector2.Zero, Owner.Speed, Target.Center, Target.Velocity);

                ThrustTowardsPosition(interceptPoint, elapsedTime, Owner.Speed);
                return;
            }

            if (distanceToTarget < adjustedWeaponRange)
            {                
                RunTimer += elapsedTime;
                if (RunTimer > 7f)
                {
                    ThrustTowardsPosition(Target.FindVectorBehindTarget(Owner.maxWeaponsRange), elapsedTime, Owner.Speed);
                    //DoNonFleetArtillery(elapsedTime);
                    return;
                }
                //when close enough veer away
                if (distanceToTarget < (Owner.Radius + Target.Radius) * 3f && !AttackRunStarted)
                {
                    AttackRunStarted = true;
                    int ran = RandomMath.IntBetween(0, 1);
                    ran = ran == 1 ? 1 : -1;
                    AttackRunAngle = ran * RandomMath.RandomBetween(75f, 100f) + Owner.Rotation.ToDegrees();                    
                }
                AttackVector = Owner.Center.PointFromAngle(AttackRunAngle, (Owner.Radius + Target.Radius) * 3f);
                MoveInDirection(AttackVector, elapsedTime);
                if (RunTimer > 2)
                {
                    DoNonFleetArtillery(elapsedTime);
                    return;
                }
            }
        }

        private void DoBoardShip(float elapsedTime)
        {
            HasPriorityTarget = true;
            State = AIState.Boarding;
            if ((!EscortTarget?.Active ?? true)
                || EscortTarget.loyalty == Owner.loyalty)
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
                return;
            }
            ThrustTowardsPosition(EscortTarget.Center, elapsedTime, Owner.Speed);
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
                     && Owner.Mothership.AI.CombatState == CombatState.AssaultShip) OrderReturnToHangar();
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
            AwaitClosest = null;
            State = AIState.Combat;
            Owner.InCombat = true;
            Owner.InCombatTimer = 15f;
            if (Owner.Mothership?.Active == true)
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
                !HasPriorityTarget)
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
                if (Owner.engineState != Ship.MoveState.Warp && Owner.GetHangars().Count > 0 &&
                    !Owner.ManualHangarOverride)
                    if (!Owner.FightersOut) Owner.FightersOut = true;
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
            }
            else if (CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }
            if (!HasPriorityOrder && !HasPriorityTarget && Owner.Weapons.Count == 0 && Owner.GetHangars().Count == 0)
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
                case CombatState.Artillery:
                    DoNonFleetArtillery(elapsedTime);
                    break;
                case CombatState.OrbitLeft:
                    OrbitShipLeft(Target as Ship, elapsedTime);
                    break;
                case CombatState.BroadsideLeft:
                    DoNonFleetBroadsideLeft(elapsedTime);
                    break;
                case CombatState.OrbitRight:
                    OrbitShip(Target as Ship, elapsedTime);
                    break;
                case CombatState.BroadsideRight:
                    DoNonFleetBroadsideRight(elapsedTime);
                    break;
                case CombatState.AttackRuns:
                    DoAttackRun(elapsedTime);
                    break;
                case CombatState.HoldPosition:
                    DoHoldPositionCombat(elapsedTime);
                    break;
                case CombatState.Evade:
                    DoEvadeCombat(elapsedTime);
                    break;
                case CombatState.AssaultShip:
                    DoAssaultShipCombat(elapsedTime);
                    break;
                case CombatState.ShortRange:
                    DoNonFleetArtillery(elapsedTime);
                    break;
            }

            if (Target != null)
                return;
            Owner.InCombat = false;
        }

        private void DoDeploy(ShipGoal shipgoal)
        {
            if (shipgoal.goal == null)
                return;
            Planet target = shipgoal.TargetPlanet;
            if (shipgoal.goal.TetherTarget != Guid.Empty)
            {
                if (target == null)
                    UniverseScreen.PlanetsDict.TryGetValue(shipgoal.goal.TetherTarget, out target);
                shipgoal.goal.BuildPosition = target.Center + shipgoal.goal.TetherOffset;
            }
            if (target != null && (target.Center + shipgoal.goal.TetherOffset).Distance(Owner.Center) > 200f)
            {
                shipgoal.goal.BuildPosition = target.Center + shipgoal.goal.TetherOffset;
                OrderDeepSpaceBuild(shipgoal.goal);
                return;
            }
            Ship platform = Ship.CreateShipAtPoint(shipgoal.goal.ToBuildUID, Owner.loyalty,
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
                platform.TetherToPlanet(UniverseScreen.PlanetsDict[shipgoal.goal.TetherTarget]);
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
                if (ship.Ship.loyalty == Owner.loyalty ||
                    !ship.Ship.loyalty.isFaction && !Owner.loyalty.GetRelations(ship.Ship.loyalty).AtWar)
                    continue;
                AverageDirection = AverageDirection + Owner.Center.DirectionToTarget(ship.Ship.Center);
                count++;
            }
            if (count != 0)
            {
                AverageDirection = AverageDirection / count;
                AverageDirection = Vector2.Normalize(AverageDirection);
                AverageDirection = Vector2.Negate(AverageDirection);
                AverageDirection = AverageDirection * 7500f; //@WHY 7500?
                ThrustTowardsPosition(AverageDirection + Owner.Center, elapsedTime, Owner.Speed);
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
                    var message = new StringBuilder(system.Name); //@todo create global string builder
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
                PatrolTarget = PatrolRoute[StopNumber];
                if (PatrolTarget.IsExploredBy(Owner.loyalty))
                {
                    StopNumber += 1;
                    if (StopNumber == PatrolRoute.Count)
                    {
                        StopNumber = 0;
                        PatrolRoute.Clear();
                        return true;
                    }
                }
                else
                {
                    MovePosition = PatrolTarget.Center;
                    float Distance = Owner.Center.Distance(MovePosition);
                    if (Distance < 75000f) PatrolTarget.ParentSystem.SetExploredBy(Owner.loyalty);
                    if (Distance > 15000f)
                    {
//@todo this should take longer to explore any planet. the explore speed should be based on sensors and such. 
                        if (Owner.velocityMaximum > Distance && Owner.Speed >= Owner.velocityMaximum
                        ) //@todo fix this speed limiter. it makes little sense as i think it would limit the speed by a very small aoumt. 
                            Owner.Speed = Distance;
                        ThrustTowardsPosition(MovePosition, elapsedTime, Owner.Speed);
                    }
                    else if (Distance >= 5500f)
                    {
                        if (Owner.velocityMaximum > Distance && Owner.Speed >= Owner.velocityMaximum)
                            Owner.Speed = Distance;
                        ThrustTowardsPosition(MovePosition, elapsedTime, Owner.Speed);
                    }
                    else
                    {
                        ThrustTowardsPosition(MovePosition, elapsedTime, Owner.Speed);
                        if (Distance < 500f)
                        {
                            PatrolTarget.SetExploredBy(Owner.loyalty);
                            StopNumber += 1;
                            if (StopNumber == PatrolRoute.Count)
                            {
                                StopNumber = 0;
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
                Stop(elapsedTime);
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
                var angleDiff = Owner.AngleDiffTo(Target.Center.Normalized(), out Vector2 right, out Vector2 forward);
                var facing = Owner.Velocity.Facing(right);                
                if (angleDiff <= 0.2f)
                {
                    
                    return;
                }
                RotateToFacing(elapsedTime, angleDiff, facing);
                return;
            }
            else
            {
                Vector2 VectorToTarget = Owner.Center.DirectionToTarget(Target.Center);
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
            float distCenter = goal.TargetPlanet.Center.Distance(Owner.Center);

            if (Owner.shipData.Role == ShipData.RoleName.troop && Owner.TroopList.Count > 0)
            {
                if (Owner.engineState == Ship.MoveState.Warp && distCenter < 7500f)
                    Owner.HyperspaceReturn();
                if (distCenter < radius)
                    ThrustTowardsPosition(goal.TargetPlanet.Center, elapsedTime,
                        Owner.Speed > 200 ? Owner.Speed * .90f : Owner.velocityMaximum);
                else
                    ThrustTowardsPosition(goal.TargetPlanet.Center, elapsedTime, Owner.Speed);
                if (distCenter < goal.TargetPlanet.ObjectRadius &&
                    goal.TargetPlanet.AssignTroopToTile(Owner.TroopList[0]))
                    Owner.QueueTotalRemoval();
                return;
            }
            if (Owner.loyalty == goal.TargetPlanet.Owner || goal.TargetPlanet.GetGroundLandingSpots() == 0
                || Owner.TroopList.Count <= 0)
                //||( Owner.shipData.Role != ShipData.RoleName.troop
                //     && Owner.HasTroopBay !Owner.GetHangars().Any(hangar => hangar.IsTroopBay && hangar.hangarTimer <= 0)
                //     && !Owner.hasTransporter))
            {
                if (Owner.loyalty.isPlayer)
                    HadPO = true;
                HasPriorityOrder = false;
                State = DefaultAIState;
                OrderQueue.Clear();
                Log.Info("Do Land Troop: Troop Assault Canceled with {0} troops and {1} Landing Spots ",
                    Owner.TroopList.Count, goal.TargetPlanet.GetGroundLandingSpots());
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
            Vector2 vectorToTarget = Owner.Center.DirectionToTarget(Target.Center);
            var angleDiff = Owner.AngleDiffTo(vectorToTarget, out Vector2 right, out Vector2 forward);
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            float adjustedRange = (Owner.maxWeaponsRange - Owner.Radius) * 0.85f;

            if (distanceToTarget > adjustedRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }

            if (distanceToTarget < Owner.Radius)
            {
                Owner.Velocity = Owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Owner.GetSTLSpeed());
            }
            else
            {
                Owner.Velocity *= 0.995f; //Small propensity to not drift
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
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }
            if (distanceToTarget < Owner.maxWeaponsRange * 0.70f &&
                Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Target.Center) < distanceToTarget)
            {
                Owner.Velocity = Vector2.Zero;
            }
            Vector2 vectorToTarget = Owner.Center.DirectionToTarget(Target.Center);
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
            var forward = new Vector2((float) Math.Sin(Owner.Rotation), -(float) Math.Cos(Owner.Rotation));
            var left = new Vector2(forward.Y, -forward.X);
            Vector2 vectorToTarget = Owner.Center.DirectionToTarget(Target.Center);
            var angleDiff = (float) Math.Acos(Vector2.Dot(vectorToTarget, left));
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            if (distanceToTarget > Owner.maxWeaponsRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
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
            RotateToFacing(elapsedTime, angleDiff, vectorToTarget.Facing(forward));
        }

        private void DoOrbit(Planet orbitTarget, float elapsedTime) //fbedard: my version of DoOrbit, fastest possible?
        {
            if (Owner.velocityMaximum < 1)
                return;
            float distance = orbitTarget.Center.Distance(Owner.Center);
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian && distance < Empire.ProjectorRadius * 2)
            {
                OrbitPos = orbitTarget.Center;
                OrderMoveTowardsPosition(OrbitPos, 0, Vector2.Zero, false, OrbitTarget);
                return;
            }

            if (distance > 15000f)
            {
                ThrustTowardsPosition(orbitTarget.Center, elapsedTime, Owner.Speed);
                OrbitPos = orbitTarget.Center;
                return;
            }
            FindNewPosTimer -= elapsedTime;
            if (FindNewPosTimer <= 0f)
            {
                float radius = orbitTarget.ObjectRadius + Owner.Radius + 1200f;
                float distanceToOrbitSpot = Owner.Center.Distance(OrbitPos);
                if (distanceToOrbitSpot <= radius || Owner.Speed < 1f)
                {
                    OrbitalAngle += ((float) Math.Asin(Owner.yBankAmount * 10f)).ToDegrees();
                    if (OrbitalAngle >= 360f)
                        OrbitalAngle -= 360f;
                }
                FindNewPosTimer = elapsedTime * 10f;
                OrbitPos = orbitTarget.Center.PointOnCircle(OrbitalAngle, radius);
            }

            if (distance < 7500f)
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();             
            }
            if (distance < 1500f + orbitTarget.ObjectRadius)
            {
                ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.Speed > 300f ? 300f : Owner.Speed);
                if (State != AIState.Bombard)
                    HasPriorityOrder = false;
            }
            else
                ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.Speed);
        }

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

        private void DoRefit(float elapsedTime, ShipGoal goal)
        {
            QueueItem qi = new BuildShip(goal);
            if (qi.sData == null)
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
            }
            var cost = (int) (ResourceManager.ShipsDict[goal.VariableString].GetCost(Owner.loyalty) -
                              Owner.GetCost(Owner.loyalty));
            if (cost < 0)
                cost = 0;
            cost = cost + 10 * (int) UniverseScreen.GamePaceStatic;
            if (Owner.loyalty.isFaction)
                qi.Cost = 0;
            else
                qi.Cost = (float) cost;
            qi.isRefit = true;
            //Added by McShooterz: refit keeps name and level
            if (Owner.VanityName != Owner.Name)
                qi.RefitName = Owner.VanityName;
            qi.sData.Level = (byte) Owner.Level;
            if (Owner.fleet != null)
            {
                Goal refitgoal = new FleetRequisition(goal, this);
                FleetNode.GoalGUID = refitgoal.guid;
                Owner.loyalty.GetGSAI().Goals.Add(refitgoal);
                qi.Goal = refitgoal;
            }
            OrbitTarget.ConstructionQueue.Add(qi);
            Owner.QueueTotalRemoval();
        }

        private void DoRepairDroneLogic(Weapon w)
        {
            // @todo Refactor this bloody mess
            //Turns out the ship was used to get a vector to the target ship and not actually used for any kind of targeting. 

            Ship friendliesNearby = null;
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
                Vector2 target = w.Center.DirectionToTarget(friendliesNearby.Center);
                target.Y = target.Y * -1f;
                w.FireDrone(target);
            }
        }

        private void DoRepairBeamLogic(Weapon w)
        {
            Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ship.Active && ship != w.Owner
                                 && ship.Health / ship.HealthMax < .9f
                                 && Owner.Center.Distance(ship.Center) <= w.Range + 500f,
                    selector: ship => ship.Health);

            if (repairMe != null) w.FireTargetedBeam(repairMe);
        }

        private void DoOrdinanceTransporterLogic(ShipModule module)
        {
            Ship repairMe = module.GetParent()
                    .loyalty.GetShips()
                    .FindMinFiltered(
                        filter: ship => Owner.Center.Distance(ship.Center) <= module.TransporterRange + 500f
                                        && ship.Ordinance < ship.OrdinanceMax && !ship.hasOrdnanceTransporter,
                        selector: ship => ship.Ordinance);
            if (repairMe != null)
            {
                module.TransporterTimer = module.TransporterTimerConstant;
                float transferAmount = module.TransporterOrdnance > module.GetParent().Ordinance
                                     ? module.GetParent().Ordinance : module.TransporterOrdnance;
                //check how much can be given
                if (transferAmount > repairMe.OrdinanceMax - repairMe.Ordinance)
                    transferAmount = repairMe.OrdinanceMax - repairMe.Ordinance;
                //Transfer
                repairMe.Ordinance += transferAmount;
                module.GetParent().Ordinance -= transferAmount;
                module.GetParent().PowerCurrent -= module.TransporterPower *
                                                   (transferAmount / module.TransporterOrdnance);

                if (Owner.InFrustum)
                {
                    GameAudio.PlaySfxAsync("transporter", module.GetParent().SoundEmitter);
                }
            }
        }

        private void DoAssaultTransporterLogic(ShipModule module)
        {
            var ship = NearbyShips.FindMinFiltered(
                    filter:
                    s => s.Ship.loyalty != null && s.Ship.loyalty != Owner.loyalty && s.Ship.shield_power <= 0
                         && Owner.Center.Distance(s.Ship.Center) <= module.TransporterRange + 500f,
                    selector: Ship => Owner.Center.SqDist(Ship.Ship.Center));
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
                        ship.Ship.TroopList.Add(Owner.TroopList[i]);
                        Owner.TroopList.Remove(Owner.TroopList[i]);
                        TroopCount++;
                        Transported = true;
                    }
                    if (TroopCount == module.TransporterTroopAssault)
                        break;
                }
                if (Transported) //@todo audio should not be here
                {
                    module.TransporterTimer = module.TransporterTimerConstant;
                    if (Owner.InFrustum)
                        GameAudio.PlaySfxAsync("transporter");
                }
            }
        }

        private void DoReturnToHangar(float elapsedTime)
        {
            if (Owner.Mothership == null || !Owner.Mothership.Active)
            {
                OrderQueue.Clear();
                return;
            }
            ThrustTowardsPosition(Owner.Mothership.Center, elapsedTime, Owner.Speed);
            if (Owner.Center.InRadius(Owner.Mothership.Center, Owner.Mothership.Radius + 300f))
            {
                if (Owner.Mothership.TroopCapacity > Owner.Mothership.TroopList.Count && Owner.TroopList.Count == 1)
                    Owner.Mothership.TroopList.Add(Owner.TroopList[0]);
                if (Owner.shipData.Role == ShipData.RoleName.supply) //fbedard: Supply ship return with Ordinance
                    Owner.Mothership.AlterOrdinance(Owner.Ordinance);
                Owner.ApplyFighterLaunchCost(false); //fbedard: New spawning cost                
             
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
                    rearmTime += Owner.Ordinance * .1f;
                    rearmTime += Owner.PowerCurrent * .1f;
                    rearmTime += Owner.shield_power * .1f;
                    rearmTime /= Owner.HealthMax + ammoReloadTime + shieldrechargeTime + powerRechargeTime;
                    rearmTime = (1.01f - rearmTime) * (hangar.hangarTimerConstant *
                                                       (1.01f - (Owner.Level + hangar.GetParent().Level) /
                                                        10)); // fbedard: rearm time from 50% to 150%
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
            ThrustTowardsPosition(EscortTarget.Center, elapsedTime, Owner.Speed);
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

        private void DoSystemDefense(float elapsedTime)
        {
            SystemToDefend = SystemToDefend ?? Owner.System;
            if (SystemToDefend == null || AwaitClosest?.Owner == Owner.loyalty)
                AwaitOrders(elapsedTime);
            else
                OrderSystemDefense(SystemToDefend);
        }

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
    }
}