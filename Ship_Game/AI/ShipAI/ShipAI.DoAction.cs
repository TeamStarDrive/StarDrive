using Microsoft.Xna.Framework;
using Ship_Game.Commands;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;
using System.Text;
using Ship_Game.Audio;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        void RestoreYBankRotation()
        {
            if (Owner.yRotation > 0f)
            {
                Owner.yRotation -= Owner.yBankAmount;
                if (Owner.yRotation < 0f)
                    Owner.yRotation = 0f;
            }
            else if (Owner.yRotation < 0f)
            {
                Owner.yRotation += Owner.yBankAmount;
                if (Owner.yRotation > 0f)
                    Owner.yRotation = 0f;
            }
        }

        void DoAssaultShipCombat(float elapsedTime)
        {
            if (Owner.isSpooling || !Owner.Carrier.HasTroopBays || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
                return;

            DoNonFleetArtillery(elapsedTime);
            if (!Owner.loyalty.isFaction && (Target as Ship).shipData.Role <= ShipData.RoleName.drone)
                return;

            float totalTroopStrengthToCommit = Owner.Carrier.MaxTroopStrengthInShipToCommit + Owner.Carrier.MaxTroopStrengthInSpaceToCommit;
            if (totalTroopStrengthToCommit <= 0)
                return;

            bool boarding = false;
            if (Target is Ship shipTarget)
            {
                float enemyStrength = shipTarget.BoardingDefenseTotal * 1.5f; // FB: assume the worst, ensure boarding success!

                if (totalTroopStrengthToCommit > enemyStrength &&
                    (Owner.loyalty.isFaction || shipTarget.GetStrength() > 0f))
                {
                    if (Owner.Carrier.MaxTroopStrengthInSpaceToCommit < enemyStrength && Target.Center.InRadius(Owner.Center, Owner.maxWeaponsRange))
                        Owner.Carrier.ScrambleAssaultShips(enemyStrength); // This will launch salvos of assault shuttles if possible

                    for (int i = 0; i < Owner.Carrier.AllTroopBays.Length; i++)
                    {
                        ShipModule hangar = Owner.Carrier.AllTroopBays[i];
                        if (hangar.GetHangarShip() == null)
                            continue;
                        hangar.GetHangarShip().AI.OrderTroopToBoardShip(shipTarget);
                    }
                    boarding = true;
                }
            }
            //This is the auto invade feature. FB: this should be expanded to check for building stength and compare troops in ship vs planet
            if (boarding || Owner.TroopsAreBoardingShip)
                return;

            Planet invadeThis = Owner.System?.PlanetList.FindMinFiltered(
                                owner => owner.Owner != null && owner.Owner != Owner.loyalty && Owner.loyalty.GetRelations(owner.Owner).AtWar,
                                troops => troops.TroopsHere.Count);
            if (invadeThis != null)
                Owner.Carrier.AssaultPlanet(invadeThis);
        }

        void DrawDebugTarget(Vector2 pip, float radius)
        {
            if (DebugInfoScreen.Mode == DebugModes.Targeting && Empire.Universe?.DebugWin?.IgnoreThisShip(Owner) == true)
            {
                Empire.Universe.DebugWin.DrawCircle(DebugModes.Targeting, pip, radius, Owner.loyalty.EmpireColor, 0.033f);
                Empire.Universe.DebugWin.DrawLine(DebugModes.Targeting, Target.Center, pip, 1f, Owner.loyalty.EmpireColor, 0.033f);
            }
        }


        void DoAttackRun(float elapsedTime)
        {
            float spacerdistance = Owner.Radius + Target.Radius;
            float adjustedWeaponRange = Owner.maxWeaponsRange * 0.35f;
            if (spacerdistance > adjustedWeaponRange)
                spacerdistance = adjustedWeaponRange;

            Vector2 interceptPoint = Owner.PredictImpact(Target);
            float distanceToTarget = Owner.Center.Distance(interceptPoint);

            if (distanceToTarget > Owner.maxWeaponsRange * 2f) //spacerdistance && distanceToTarget > adjustedWeaponRange)
            {
                RunTimer = 0f;
                AttackRunStarted = false;

                if (distanceToTarget > 7500 ) //|| distanceToTarget < Owner.maxWeaponsRange)
                {
                    ThrustTowardsPosition(interceptPoint, elapsedTime, Owner.Speed);
                    return;

                }
                Vector2 direction = Owner.Center.DirectionToTarget(interceptPoint);
                MoveInDirection(direction, elapsedTime);
                DrawDebugTarget(interceptPoint, spacerdistance);
                return;
            }
            RunTimer -= elapsedTime;
            AttackRunStarted |= RunTimer < 0;
            if (AttackRunStarted)
            {
                if (distanceToTarget > spacerdistance)
                {
                    Vector2 direction = Owner.Center.DirectionToTarget(interceptPoint);
                    MoveInDirection(direction, elapsedTime);
                    DrawDebugTarget(interceptPoint, Owner.Radius);
                    return;
                }
                AttackRunStarted = false;
                int ran = RandomMath.IntBetween(0, 1);
                ran = ran == 1 ? 1 : -1;
                AttackRunAngle = ran * RandomMath.RandomBetween(75f, 100f) + Owner.Rotation.ToDegrees();
                RunTimer = 20; // Owner.Speed * elapsedTime ;
            }
            if (distanceToTarget < Owner.maxWeaponsRange)
            {
                //var behind     = Target.FindVectorBehindTarget(Owner.maxWeaponsRange);
                //AttackVector   = behind.PointFromAngle(AttackRunAngle, spacerdistance);
                var strafeVector = Target.FindStrafeVectorFromTarget(Owner.maxWeaponsRange, 180);
                AttackVector     = strafeVector.PointFromAngle(AttackRunAngle, spacerdistance);
                var attackSetup  = Owner.Center.DirectionToTarget(AttackVector);
                MoveInDirection(attackSetup, elapsedTime);
                //DrawDebugTarget(AttackVector, spacerdistance);
                //if (RunTimer < 3)
                return;
            }

            //if (RunTimer > 5)
                //DoNonFleetArtillery(elapsedTime);
            RunTimer = 0;
            //DoNonFleetArtillery(elapsedTime);

        }

        void DoBoardShip(float elapsedTime)
        {
            HasPriorityTarget = true;
            State = AIState.Boarding;
            if ((!EscortTarget?.Active ?? true)
                || EscortTarget.loyalty == Owner.loyalty)
            {
                OrderQueue.Clear();
                //State = AIState.AwaitingOrders;
                if (Owner.Mothership != null)
                {
                    if (Owner.Mothership.TroopsOut)
                        Owner.DoEscort(Owner.Mothership);
                    else
                        OrderReturnToHangar();
                }
                return;
            }
            ThrustTowardsPosition(EscortTarget.Center, elapsedTime, Owner.Speed);
            float distance = Owner.Center.Distance(EscortTarget.Center);
            if (distance < EscortTarget.Radius + 300f)
            {
                if (Owner.TroopList.Count > 0)
                {
                    EscortTarget.TroopList.Add(Owner.TroopList[0]);
                    Owner.QueueTotalRemoval();
                }
            }
            else if (distance > 10000f
                     && Owner.Mothership?.AI.CombatState == CombatState.AssaultShip) OrderReturnToHangar();
        }

        void DoCombat(float elapsedTime)
        {
            var ctarget = Target as Ship;
            if (Target?.Active != true || ctarget?.engineState != Ship.MoveState.Sublight || !Owner.loyalty.IsEmpireAttackable(Target.GetLoyalty(), ctarget))
            {
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

            if (!HasPriorityOrder && !HasPriorityTarget && Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                CombatState = CombatState.Evade;

            if (!Owner.loyalty.isFaction && Owner.System != null && Owner.Carrier.HasTroopBays) //|| Owner.hasTransporter)
                CombatState = CombatState.AssaultShip;

            if (Target?.Center.InRadius(Owner.Center, 10000) ?? false)
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
                if (Owner.Carrier.HasHangars && !Owner.ManualHangarOverride)
                    Owner.Carrier.ScrambleFighters();
            }
            else if (FleetNode != null && Owner.fleet != null)
            {
                if (Target == null)
                    Log.Error($"doCombat: Target was null? : https://sentry.io/blackboxmod/blackbox/issues/628107403/");
                var fleetPositon = Owner.fleet.FindAveragePosition() + FleetNode.FleetOffset;
                if (Target.Center.OutsideRadius(fleetPositon, FleetNode.OrdersRadius))
                {
                    if (Owner.Center.OutsideRadius(fleetPositon,1000))
                    {
                        ThrustTowardsPosition(fleetPositon, elapsedTime, Owner.Speed);
                        return;
                    }
                    DoHoldPositionCombat(elapsedTime);
                    return;
                }
            }
            else if (CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }
            if (Intercepting && CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade
                && Owner.Center.OutsideRadius(Target.Center, Owner.maxWeaponsRange))
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }
            switch (CombatState)
            {
                case CombatState.Artillery:
                    DoNonFleetArtillery(elapsedTime);
                    break;
                case CombatState.OrbitLeft:
                    OrbitShip(Target as Ship, elapsedTime, Orbit.Left);
                    break;
                case CombatState.BroadsideLeft:
                    DoNonFleetBroadsideLeft(elapsedTime);
                    break;
                case CombatState.OrbitRight:
                    OrbitShip(Target as Ship, elapsedTime, Orbit.Right);
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

        void DoDeploy(ShipGoal shipgoal)
        {
            if (shipgoal.goal == null)
                return;
            Planet target = shipgoal.TargetPlanet;
            if (target == null && shipgoal.goal.TetherTarget != Guid.Empty)
            {
                Empire.Universe.PlanetsDict.TryGetValue(shipgoal.goal.TetherTarget, out target);
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
                platform.TetherToPlanet(Empire.Universe.PlanetsDict[shipgoal.goal.TetherTarget]);
                platform.TetherOffset = shipgoal.goal.TetherOffset;
            }
            Owner.loyalty.GetEmpireAI().Goals.Remove(shipgoal.goal);
            Owner.QueueTotalRemoval();
        }

        void DoEvadeCombat(float elapsedTime)
        {
            var AverageDirection = new Vector2();
            var count = 0;
            foreach (ShipWeight ship in NearByShips)
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
                ExplorationTarget = Owner.loyalty.GetEmpireAI().AssignExplorationTarget(Owner);
                if (ExplorationTarget == null)
                {
                    OrderQueue.Clear();
                    State = AIState.AwaitingOrders;
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
                            planetsTypesNumber.AddToValue(planet.CategoryName, 1);
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
                        Icon = system.Sun.Icon,
                        Action = "SnapToExpandSystem"
                    }, "sd_ui_notification_warning");
                }
                ExplorationTarget = null;
            }
        }

        bool DoExploreSystem(float elapsedTime)
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

        void DoHoldPositionCombat(float elapsedTime)
        {
            if (Owner.Velocity.Length() > 0f)
            {
                Vector2 interceptPoint = Owner.PredictImpact(Target);

                Stop(elapsedTime);

                float angleDiff = Owner.AngleDiffTo(interceptPoint, out Vector2 right, out Vector2 forward);
                float facing = Owner.Velocity.Facing(right);
                if (angleDiff <= 0.2f)
                    return;
                RotateToFacing(elapsedTime, angleDiff, facing);
            }
            else
            {
                Vector2 dir = Owner.Center.DirectionToTarget(Target.Center);
                float angleDiff = Owner.AngleDiffTo(dir, out Vector2 right, out Vector2 forward);
                if (angleDiff <= 0.02f)
                    return;
                RotateToFacing(elapsedTime, angleDiff, dir.Facing(right));
            }
        }

        void DoLandTroop(float elapsedTime, ShipGoal goal)
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
                    Owner.TroopList[0].AssignTroopToTile(goal.TargetPlanet))
                    Owner.QueueTotalRemoval();
                return;
            }
            if (Owner.loyalty == goal.TargetPlanet.Owner || goal.TargetPlanet.GetGroundLandingSpots() == 0
                || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
            {
                if (Owner.loyalty.isPlayer)
                    HadPO = true;
                HasPriorityOrder = false;
                State = DefaultAIState;
                OrderQueue.Clear();
                Log.Info($"Do Land Troop: Troop Assault Canceled with {Owner.TroopList.Count} troops and {goal.TargetPlanet.GetGroundLandingSpots()} Landing Spots ");
            }
            else if (!Owner.Carrier.HasTransporters) // FB: use regular invade
            {
                if (distCenter > 7500f)
                    return;
                Owner.Carrier.AssaultPlanet(goal.TargetPlanet);
                Owner.DoOrbit(goal.TargetPlanet);
            }
            else if (distCenter < radius)  // STSA with transpoters - this should be checked wit STSA active
                Owner.Carrier.AssaultPlanetWithTransporters(goal.TargetPlanet);
        }

        void DoNonFleetArtillery(float elapsedTime)
        {
            //Heavily modified by Gretman
            Vector2 vectorToTarget = Owner.Center.DirectionToTarget(Owner.PredictImpact(Target));
            var angleDiff = Owner.AngleDiffTo(vectorToTarget, out Vector2 right, out Vector2 forward);
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            float adjustedRange = (Owner.maxWeaponsRange - Owner.Radius);// * 0.85f;
            float minDistance = Math.Max(adjustedRange * .25f + Target.Radius, adjustedRange *.5f);

            if (distanceToTarget > adjustedRange)
            {
                if (distanceToTarget > 7500)
                {
                    ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                    return;
                }
                //if (angleDiff > .22f)
                //{
                //    Owner.Velocity *= angleDiff / .44f;

                //    //var rnd = RandomMath.IntBetween(1, 100);
                //    //if (rnd < 10)
                //    //{
                //    //    RotateToFaceMovePosition(elapsedTime, Target.Center);
                //    //    return;
                //    //}

                //}
                MoveInDirection(vectorToTarget, elapsedTime );
                return;
            }
            if (distanceToTarget < minDistance)
            {
                float aceel = elapsedTime * Owner.GetSTLSpeed();
                Owner.Velocity -= Vector2.Normalize(forward) * aceel;
            }
            else
            {
                Owner.Velocity *= 0.95f; //Small propensity to not drift
            }

            if (angleDiff <= 0.02f)
            {
                RestoreYBankRotation();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, vectorToTarget.Facing(right));
        }

        void DoNonFleetBroadsideRight(float elapsedTime)
        {
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            if (distanceToTarget > Owner.maxWeaponsRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }

            if (distanceToTarget < Owner.maxWeaponsRange * 0.70f &&
                (Owner.Center + Owner.Velocity*elapsedTime).InRadius(Target.Center, distanceToTarget))
            {
                Owner.Velocity = Vector2.Zero;
            }

            Vector2 direction = Owner.Center.DirectionToTarget(Target.Center);
            float angleDiff = Owner.AngleDiffTo(direction, out Vector2 right, out Vector2 forward);
            if (angleDiff > 0.02f)
            {
                RotateToFacing(elapsedTime, angleDiff, direction.Facing(right));
            }
            else
            {
                RestoreYBankRotation();
            }
        }

        void DoNonFleetBroadsideLeft(float elapsedTime)
        {
            float distanceToTarget = Owner.Center.Distance(Target.Center);
            if (distanceToTarget > Owner.maxWeaponsRange)
            {
                ThrustTowardsPosition(Target.Center, elapsedTime, Owner.Speed);
                return;
            }

            if (distanceToTarget < Owner.maxWeaponsRange * 0.70f &&
                (Owner.Center + Owner.Velocity*elapsedTime).InRadius(Target.Center, distanceToTarget))
            {
                Owner.Velocity = Vector2.Zero; // @todo This stopping ability is insane
            }

            
            var forward = new Vector2((float) Math.Sin(Owner.Rotation), 
                                     -(float) Math.Cos(Owner.Rotation));
            var left = new Vector2(forward.Y, -forward.X);
            Vector2 direction = Owner.Center.DirectionToTarget(Target.Center);
            float angleDiff = (float) Math.Acos(Vector2.Dot(direction, left));

            if (angleDiff > 0.02f)
            {
                RotateToFacing(elapsedTime, angleDiff, direction.Facing(forward));
            }
            else
            {
                RestoreYBankRotation();
            }
        }

        const float OrbitalSpeedLimit = 500f;

        enum Orbit { Left, Right }

        // Orbit drones around a ship
        void OrbitShip(Ship ship, float elapsedTime, Orbit direction)
        {
            OrbitPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
            if (OrbitPos.Distance(Owner.Center) < 1500f)
            {
                float deltaAngle = direction == Orbit.Left ? -15f : +15f;
                OrbitalAngle = (OrbitalAngle + deltaAngle).NormalizedAngle();
                OrbitPos = ship.Position.PointOnCircle(OrbitalAngle, 2500f);
            }
            ThrustTowardsPosition(OrbitPos, elapsedTime, OrbitalSpeedLimit);
        }

        // orbit around a planet
        void DoOrbit(Planet orbitTarget, float elapsedTime)
        {
            if (Owner.velocityMaximum < 1)
                return;

            float distance = orbitTarget.Center.Distance(Owner.Center);
            if (distance > 15000f)
            {
                ThrustTowardsPosition(orbitTarget.Center, elapsedTime, Owner.velocityMaximum);
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
                    OrbitalAngle = OrbitalAngle.NormalizedAngle();
                }
                FindNewPosTimer = elapsedTime * 10f;
                OrbitPos = orbitTarget.Center.PointOnCircle(OrbitalAngle, radius);
            }

            if (Owner.engineState == Ship.MoveState.Warp && distance < 7500f) // exit warp if we're getting close
                Owner.HyperspaceReturn();

            // precision move, this fixes uneven thrusting while orbiting
            float precisionSpeed = Owner.velocityMaximum * 0.5f;

            // We are within orbit radius, so do actual orbiting:
            if (distance < (1500f + orbitTarget.ObjectRadius))
            {
                var direction = Owner.Center.DirectionToTarget(OrbitPos);
                MoveInDirection(direction, elapsedTime, precisionSpeed);
                if (State != AIState.Bombard)
                    HasPriorityOrder = false;
            }
            else // we are still not there yet, so find a meaningful orbit position
            {
                ThrustTowardsPosition(OrbitPos, elapsedTime, precisionSpeed);
            }
        }

        void DoRebase(ShipGoal Goal)
        {
            if (Owner.TroopList.Count == 0)
            {
                Owner.QueueTotalRemoval();
            }
            else if (Owner.TroopList[0].AssignTroopToTile(Goal.TargetPlanet))
            {
                Owner.TroopList.Clear();
                Owner.QueueTotalRemoval();
            }
            else
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
            }
        }

        void DoRefit(float elapsedTime, ShipGoal goal)
        {
            QueueItem qi = new BuildShip(goal, OrbitTarget);
            if (qi.sData == null)
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
            }
            int cost = (int) (ResourceManager.ShipsDict[goal.VariableString].GetCost(Owner.loyalty) -
                              Owner.GetCost(Owner.loyalty));
            if (cost < 0)
                cost = 0;
            cost += (int)(10 * CurrentGame.Pace); // extra refit cost: accord for GamePace
            qi.Cost = Owner.loyalty.isFaction ? 0 : cost;
            qi.isRefit = true;
            //Added by McShooterz: refit keeps name and level
            if (Owner.VanityName != Owner.Name)
                qi.RefitName = Owner.VanityName;
            if (qi.sData != null)
                qi.sData.Level = (byte)Owner.Level;
            if (Owner.fleet != null)
            {
                var refitgoal = new FleetRequisition(goal, this);
                FleetNode.GoalGUID = refitgoal.guid;
                Owner.loyalty.GetEmpireAI().Goals.Add(refitgoal);
                qi.Goal = refitgoal;
            }
            OrbitTarget.ConstructionQueue.Add(qi);
            Owner.QueueTotalRemoval();
        }

        void DoRepairDroneLogic(Weapon w)
        {
            using (FriendliesNearby.AcquireReadLock())
            {
                Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ShipNeedsRepair(ship, ShipResupply.RepairDroneRange),
                    selector: ship => ship.InternalSlotsHealthPercent);

                if (repairMe == null) return;
                Vector2 target = w.Center.DirectionToTarget(repairMe.Center);
                target.Y = target.Y * -1f;
                w.FireDrone(target);
            }
        }

        void DoRepairBeamLogic(Weapon w)
        {
            Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ShipNeedsRepair(ship, w.Range + 500f, Owner),
                    selector: ship => ship.InternalSlotsHealthPercent);

            if (repairMe != null) w.FireTargetedBeam(repairMe);
        }

        bool ShipNeedsRepair(Ship target, float maxDistance, Ship dontHealSelf = null)
        {
            return target.Active && target != dontHealSelf
                    && target.HealthPercent < ShipResupply.RepairDroneThreshold
                    && Owner.Center.Distance(target.Center) <= maxDistance;
        }

        void DoOrdinanceTransporterLogic(ShipModule module)
        {
            Ship repairMe = module.GetParent()
                    .loyalty.GetShips()
                    .FindMinFiltered(
                        filter: ship => Owner.Center.Distance(ship.Center) <= module.TransporterRange + 500f
                                        && ship.Ordinance < ship.OrdinanceMax && !ship.Carrier.HasOrdnanceTransporters,
                        selector: ship => ship.Ordinance);
            if (repairMe == null)
                return;

            module.TransporterTimer = module.TransporterTimerConstant;

            float transferAmount    = module.TransporterOrdnance > module.GetParent().Ordinance
                ? module.GetParent().Ordinance : module.TransporterOrdnance;
            float ordnanceLeft = repairMe.ChangeOrdnance(transferAmount);
            module.GetParent().ChangeOrdnance(ordnanceLeft - transferAmount);
            module.GetParent().AddPower(module.TransporterPower * ((ordnanceLeft - transferAmount) / module.TransporterOrdnance));

            if (Owner.InFrustum)
                GameAudio.PlaySfxAsync("transporter", module.GetParent().SoundEmitter);
        }

        void DoAssaultTransporterLogic(ShipModule module)
        {
            ShipWeight ship = NearByShips.Where(
                    s => s.Ship.loyalty != null && s.Ship.loyalty != Owner.loyalty && s.Ship.shield_power <= 0
                         && Owner.Center.Distance(s.Ship.Center) <= module.TransporterRange + 500f)
                .OrderBy(Ship => Owner.Center.SqDist(Ship.Ship.Center)).First();
            if (ship.Ship == null) return;

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

        void DoReturnToHangar(float elapsedTime)
        {
            if (Owner.Mothership == null || !Owner.Mothership.Active)
            {
                OrderQueue.Clear();
                if (Owner.shipData.Role == ShipData.RoleName.supply)
                    OrderScrapShip();
                else
                    GoOrbitNearestPlanetAndResupply(true);
                return;
            }
            ThrustTowardsPosition(Owner.Mothership.Center, elapsedTime, Owner.Speed);
            //this looks to need refactor. some of these formulas are... weird
            if (Owner.Center.InRadius(Owner.Mothership.Center, Owner.Mothership.Radius + 300f))
            {
                if (Owner.TroopList.Count == 1)
                    Owner.Mothership.TroopList.Add(Owner.TroopList[0]);
                if (Owner.shipData.Role == ShipData.RoleName.supply) //fbedard: Supply ship return with Ordinance
                    Owner.Mothership.ChangeOrdnance(Owner.Ordinance);
                Owner.Mothership.ChangeOrdnance(Owner.ShipRetrievalOrd);

                Owner.QueueTotalRemoval();
                foreach (ShipModule hangar in Owner.Mothership.Carrier.AllActiveHangars)
                {
                    if (hangar.GetHangarShip() != Owner)
                        continue;
                    //added by gremlin: prevent fighters from relaunching immediately after landing.
                    float ammoReloadTime = Owner.OrdinanceMax * .1f;
                    float shieldRechargeTime = Owner.shield_max * .1f;
                    float powerRechargeTime = Owner.PowerStoreMax * .1f;
                    float rearmTime = Owner.Health;
                    rearmTime += Owner.Ordinance * .1f;
                    rearmTime += Owner.PowerCurrent * .1f;
                    rearmTime += Owner.shield_power * .1f;
                    rearmTime /= Owner.HealthMax + ammoReloadTime + shieldRechargeTime + powerRechargeTime;
                    //this was broken now im not sure.
                    float rearmModifier = hangar.hangarTimerConstant *
                                           (1.01f - (Owner.Level + hangar.GetParent().Level) /10f);
                    // fbedard: rearm time from 50% to 150%
                    rearmTime = (1.01f - rearmTime) * rearmModifier;
                    if (rearmTime < 0)
                        rearmTime = 1;
                    /*CG: if the fighter is fully functional reduce rearm time to very little.
                    The default 5 minute hangar timer is way too high. It cripples fighter usage.
                    at 50% that is still 2.5 minutes if the fighter simply launches and returns.
                    with lag that can easily be 10 or 20 minutes.
                    at 1.01 that should be 3 seconds for the default hangar.
                    */
                    hangar.SetHangarShip(null);
                    hangar.hangarTimer = rearmTime;
                    hangar.HangarShipGuid = Guid.Empty;
                }
            }
        }

        void DoReturnHome(float elapsedTime)
        {
            if (Owner.HomePlanet.Owner != Owner.loyalty)
            {
                // find another friendly planet to land at
                Owner.UpdateHomePlanet(Owner.loyalty.RallyShipYardNearestTo(Owner.Center));
                if (Owner.HomePlanet == null)
                {
                    // Nowhere to land, bye bye.
                    Owner.ScuttleTimer = 1;
                    return;
                }
            }
            ThrustTowardsPosition(Owner.HomePlanet.Center, elapsedTime, Owner.Speed);
            if (Owner.Center.InRadius(Owner.HomePlanet.Center, Owner.HomePlanet.ObjectRadius + 150f))
            {
                Owner.HomePlanet.LandDefenseShip(Owner.DesignRole, Owner.GetCost(Owner.loyalty), Owner.HealthPercent);
                Owner.QueueTotalRemoval();
            }
            if (Owner.InCombat)
            {
                OrderQueue.Clear();
                HasPriorityOrder = false;
                State = AIState.AwaitingOrders;
            }
        }

        void DoSupplyShip(float elapsedTime, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Resupply
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit
                                     || EscortTarget.OrdnancePercent >= 0.99f)
            {
                OrderReturnToHangar();
                return;
            }
            ThrustTowardsPosition(EscortTarget.Center, elapsedTime, Owner.Speed);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                Owner.ChangeOrdnance(EscortTarget.ChangeOrdnance(Owner.Ordinance) - Owner.Ordinance);
                EscortTarget.AI.TerminateResupplyIfDone();
                OrderReturnToHangar();
            }
        }

        void DoResupplyEscort(float elapsedTime, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Resupply
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit
                                     || !EscortTarget.SupplyShipCanSupply)
            {
                State = AIState.AwaitingOrders;
                IgnoreCombat = false;
                return;
            }

            var escortVector = EscortTarget.FindStrafeVectorFromTarget(goal.VariableNumber, (int)goal.Direction.ToDegrees());
            DrawDebugTarget(escortVector, Owner.Radius);
            float distanceToEscortSpot = Owner.Center.Distance(escortVector);
            float supplyShipVelocity   = EscortTarget.Velocity.Length();
            float escortVelocity       = Owner.velocityMaximum;
            if (distanceToEscortSpot < 2000) // ease up thrust on approach to escort spot
                escortVelocity  = distanceToEscortSpot / 2000 * Owner.velocityMaximum + supplyShipVelocity + 25;

            if (distanceToEscortSpot > 50)
                ThrustTowardsPosition(escortVector, elapsedTime, escortVelocity.Clamped(0, Owner.velocityMaximum));
            else
                Owner.Velocity = Vector2.Zero;

            switch (goal.VariableString)
            {
                default:       TerminateResupplyIfDone(); break;
                case "Rearm":  TerminateResupplyIfDone(SupplyType.Rearm); break;
                case "Repair": TerminateResupplyIfDone(SupplyType.Repair); break;
                case "Troops": TerminateResupplyIfDone(SupplyType.Troops); break;
            }
        }

        void DoSystemDefense(float elapsedTime)
        {
            SystemToDefend = SystemToDefend ?? Owner.System;
            if (SystemToDefend == null || AwaitClosest?.Owner == Owner.loyalty)
                AwaitOrders(elapsedTime);
            else
                OrderSystemDefense(SystemToDefend);
        }

        void DoTroopToShip(float elapsedTime)
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
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
            }
        }
    }
}