using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{

    public sealed partial class ShipAI
    {
        public bool UseSensorsForTargets = true;
        public CombatState CombatState = AI.CombatState.AttackRuns;
        public CombatAI CombatAI = new CombatAI();
        public BatchRemovalCollection<Ship> PotentialTargets = new BatchRemovalCollection<Ship>();
        public Ship EscortTarget;
        private bool AttackRunStarted;
        private float AttackRunAngle;
        private float RunTimer;
        private float ScanForThreatTimer;
        public Planet ExterminationTarget;
        public bool Intercepting;
        public Guid TargetGuid;
        public bool IgnoreCombat;
        public bool BadGuysNear;
        public bool Troopsout = false;
        public Array<Projectile> TrackProjectiles = new Array<Projectile>();
        public Guid EscortTargetGuid;
        public GameplayObject Target;
        private Vector2 AttackVector = Vector2.Zero;
        public Array<Ship> TargetQueue = new Array<Ship>();
        public bool HasPriorityTarget;
        private float TriggerDelay;

        public void FireOnTarget()
        {
            // base reasons not to fire. @TODO actions decided by loyalty like should be the same in all areas. 
            if (!Owner.hasCommand || Owner.engineState == Ship.MoveState.Warp || Owner.EMPdisabled || Owner.Weapons.Count == 0)
                return;

            if (BadGuysNear) // Determine if there is something to shoot at
            {
                RefreshTarget();
                

                for (int i = 0; i < Owner.Weapons.Count; i++)
                {
                    Owner.Weapons[i].UpdatePrimaryFireTarget(Target, TrackProjectiles, PotentialTargets);
                }

                FireWeapons();
            }
        }

        private void UpdateTrackedProjectiles()
        {
            bool hasPointDefense = OwnerHasPointDefense();

            TrackProjectiles.Clear();
            if (Owner.Mothership != null)
                TrackProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
            if (Owner.TrackingPower <= 0 || !hasPointDefense) return;
            // @todo This usage of GetNearby is slow! Consider creating a specific SpatialManager search function
            foreach (GameplayObject go in Owner.GetObjectsInSensors(GameObjectType.Proj, Owner.maxWeaponsRange * 2))
            {
                var missile = (Projectile) go;
                if (missile.Weapon.Tag_Intercept && Owner.loyalty.IsEmpireAttackable(missile.Loyalty))
                    TrackProjectiles.Add(missile);
            }
            TrackProjectiles.Sort(missile => Owner.Center.SqDist(missile.Center));
        }

        private bool OwnerHasPointDefense()
        {
            for (int i = 0; i < Owner.Weapons.Count; ++i)
            {
                Weapon purge = Owner.Weapons[i];
                if (purge.Tag_PD || purge.TruePD)
                    return true;
            }
            return false;
        }

        private void RefreshTarget()
        {
            if (Target?.Active == false || Target is Ship ship && ship.dying)
            {
                for (int i = 0; i < Owner.Weapons.Count; ++i)
                {
                    Weapon weapon = Owner.Weapons[i];

                    // only clear weapons shooting at Primary Target, otherwise we would cripple PD weapons
                    if (weapon.FireTarget == Target) 
                        weapon.ClearFireTarget();
                }
                Target = null;
            }
        }

        //this section actually fires the weapons. This whole firing section can be moved to some other area of the code. 
        // This code is very expensive. 
        private void FireWeapons()
        {
            for (int i = 0; i < Owner.Weapons.Count; ++i)
            {
                Weapon weapon = Owner.Weapons[i];

                // note: these are all optimizations
                if (weapon.FireTarget == null || !weapon.Module.Active || weapon.CooldownTimer > 0f || !weapon.Module.Powered )
                    continue;
                weapon.FireAtAssignedTarget();
            }
        }

        public GameplayObject ScanForCombatTargets(Ship sensorShip, float radius)
        {
            BadGuysNear = false;
            GameplayObject priorityTarget = null;
            FriendliesNearby.Clear();
            PotentialTargets.Clear();
            NearByShips.Clear();


            if (HasPriorityTarget)
            {
                if (Target == null)
                {
                    HasPriorityTarget = false;
                    if (TargetQueue.Count > 0)
                    {
                        HasPriorityTarget = true;
                        Target = TargetQueue.First();
                    }
                }
                else
                    priorityTarget = Target;
            }
            UpdateTrackedProjectiles();
            if (Target is Ship target)
            {
                if (target.loyalty == Owner.loyalty)
                {
                    Target = null;
                    HasPriorityTarget = false;
                }
                else if (!Intercepting && target.engineState == Ship.MoveState.Warp)
                {
                    Target = null;
                    if (!HasPriorityOrder && Owner.loyalty != UniverseScreen.player)
                        State = AIState.AwaitingOrders;
                    return null;
                }
            }

            //Doctor: Increased this from 0.66f as seemed slightly on the low side. 
            //CombatAI.PreferredEngagementDistance =    Owner.maxWeaponsRange * 0.75f ;
            SolarSystem thisSystem = Owner.System;
            float armorAvg = 0;
            float shieldAvg = 0;
            float dpsAvg = 0;
            float sizeAvg = 0;
            if (thisSystem != null)
                for (int i = 0; i < thisSystem.PlanetList.Count; i++)
                {
                    Planet p = thisSystem.PlanetList[i];
                    BadGuysNear = BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) &&
                                  Owner.Center.InRadius(p.Center, radius);
                }

      

            GameplayObject[] nearbyShips = sensorShip.GetObjectsInSensors(GameObjectType.Ship, radius);
            for (int x = 0; x < nearbyShips.Length; x++)
            {
                var go = nearbyShips[x];
                var nearbyShip = (Ship) go;
                if (!nearbyShip.Active || nearbyShip.dying
                                       || Owner.Center.OutsideRadius(nearbyShip.Center,
                                           radius + (radius < 0.01f ? 10000 : 0)))
                    continue;

                Empire empire = nearbyShip.loyalty;
                if (empire == Owner.loyalty)
                {
                    FriendliesNearby.Add(nearbyShip);
                    continue;
                }

                bool isAttackable = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                if (!isAttackable) continue;
                armorAvg += nearbyShip.armor_max;
                shieldAvg += nearbyShip.shield_max;
                dpsAvg += nearbyShip.GetDPS();
                sizeAvg += nearbyShip.Size;
                BadGuysNear = true;
                if (radius < 1)
                    continue;
                var sw = new ShipWeight(nearbyShip, 1);


                if (BadGuysNear && nearbyShip.AI.Target is Ship nearbyShipsTarget &&
                    nearbyShipsTarget == EscortTarget && nearbyShip.engineState != Ship.MoveState.Warp)
                {
                    sw += 3f;
                }

                NearByShips.Add(sw);
            }
            if (Target is Ship shipTarget)
            {

                if (Owner.fleet != null && !HasPriorityOrder && !HasPriorityTarget)
                {
                    var sw = new ShipWeight(shipTarget, 1);
                    NearByShips.AddUnique(sw);
                }

            }
            if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
            {
                var sw = new ShipWeight(EscortTarget.AI.Target, 2f);

                NearByShips.AddUnique(sw);
            }

            SupplyShuttleLaunch(radius);
            if (Owner.shipData.Role == ShipData.RoleName.supply && Owner.Mothership == null)
                Owner.Die(null, true); //Destroy shuttle without mothership

            SetTargetWeights(armorAvg, shieldAvg, dpsAvg, sizeAvg);

            ShipWeight[] sortedList2 = NearByShips.FilterBy(weight => weight.Weight > -100)
                .OrderByDescending(weight => weight.Weight).ToArray();

            PotentialTargets.ClearAdd(sortedList2.Select(ship => ship.Ship));
            if (Owner.fleet != null && State != AIState.FormationWarp)
            {
                foreach (Ship ship in PotentialTargets)
                    Owner.fleet?.FleetTargetList.AddUniqueRef(ship);                
            }

            if (Target?.Active != true)
            {
                Target = null;
                HasPriorityTarget = false;
            }
            else if (Target?.Active == true && HasPriorityTarget && Target is Ship ship)
            {
                if (Owner.loyalty.IsEmpireAttackable(ship.loyalty, ship))
                    BadGuysNear = true;
                return Target;
            }

            GameplayObject targetShip = null;
            if (sortedList2.Any())
                targetShip = sortedList2.ElementAt(0).Ship;

            //if (Owner.Weapons.Count > 0 || Owner.HasActiveHangars)
            if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars)
                return targetShip;
            return null;
        }

        private void SetTargetWeights(float armorAvg, float shieldAvg, float dpsAvg, float sizeAvg)
        {
            armorAvg  /= NearByShips.Count + .01f;
            shieldAvg /= NearByShips.Count + .01f;
            dpsAvg    /= NearByShips.Count + .01f;

            for (int i = NearByShips.Count - 1; i >= 0; i--)
            {

                ShipWeight copyWeight = NearByShips[i]; //Remember we have a copy.
                copyWeight += CombatAI.ApplyWeight(copyWeight.Ship);

                if (Owner.fleet == null || FleetNode == null)
                {
                    NearByShips[i] = copyWeight;//update stored weight from copy
                    continue;
                }
                //if (!Intercepting && copyWeight.Ship.Center.OutsideRadius(Owner.Center, GetSensorRadius()))
                //{
                //    copyWeight.SetWeight(-100); //Hrmm. Dont know how to simply assign a value with operator
                //    NearByShips[i] = copyWeight;
                //    continue;
                //}

                var fleetPosition = Owner.fleet.FindAveragePosition() + FleetNode.FleetOffset;
                var distanceToFleet = fleetPosition.Distance(copyWeight.Ship.Center);
                copyWeight += FleetNode.OrdersRadius <= distanceToFleet ? 0 : -distanceToFleet / FleetNode.OrdersRadius;




                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.GetDPS(), dpsAvg, FleetNode.DPSWeight);
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.shield_power, shieldAvg,
                    FleetNode.AttackShieldedWeight);
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.armor_max, armorAvg, FleetNode.ArmoredWeight);
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.Size, sizeAvg, FleetNode.SizeWeight);
                copyWeight += FleetNode.ApplyFleetWeight(Owner.fleet, copyWeight.Ship);
                //ShipWiegth is a struct so we are working with a copy. Need to overwrite existing value. 
                NearByShips[i] = copyWeight;
            }
        }

        private void SupplyShuttleLaunch(float radius)
        {
            //fbedard: for launch only
            //CG Omergawd. yes i tried getting rid of the orderby and cleaning this up
            //but im not willing to test that change here. I think i did some of this a long while back.  
            //if (Owner.engineState == Ship.MoveState.Warp ||!Owner.HasSupplyBays ) return;
            if (Owner.engineState == Ship.MoveState.Warp ||!Owner.Carrier.HasSupplyBays ) return;

            Ship[] sortedList = FriendliesNearby.FilterBy(ship => ship.shipData.Role != ShipData.RoleName.supply 
                                                                  && ship.OrdnanceStatus < ShipStatus.Good
                                                                  && ship!= Owner
                                                                  //&& (!ship.HasSupplyBays || ship.OrdnanceStatus > ShipStatus.Poor))
                                                                  && (!ship.Carrier.HasSupplyBays || ship.OrdnanceStatus > ShipStatus.Poor ))       
                .OrderBy(ship =>
                {
                    var distance = Owner.Center.Distance(ship.Center);
                    distance = (int)distance * 10 / radius;
                    return (int)ship.OrdnanceStatus * distance + (ship.fleet == Owner.fleet ? 0 : 10 );
                }).ToArray();

            if (sortedList.Length <= 0 ) return;            

            var skip = 0;
            var inboundOrdinance = 0f;
            //oh crap this is really messed up. 
            //foreach (ShipModule hangar in Owner.GetHangars().FilterBy(hangar => hangar.IsSupplyBay))
            foreach (ShipModule hangar in Owner.Carrier.AllActiveHangars.FilterBy(hangar => hangar.IsSupplyBay)) //FB: maybe addd active supplybays to carrierbays
            {
                if (hangar.GetHangarShip() != null && hangar.GetHangarShip().Active)
                {
                    if (hangar.GetHangarShip().AI.State != AIState.Ferrying &&
                        hangar.GetHangarShip().AI.State != AIState.ReturnToHangar &&
                        hangar.GetHangarShip().AI.State != AIState.Resupply &&
                        hangar.GetHangarShip().AI.State != AIState.Scrap)
                    {
                        if (sortedList[skip] != null)
                        {
                            var g1 = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
                            hangar.GetHangarShip().AI.EscortTarget = sortedList[skip];

                            hangar.GetHangarShip().AI.IgnoreCombat = true;
                            hangar.GetHangarShip().AI.OrderQueue.Clear();
                            hangar.GetHangarShip().AI.OrderQueue.Enqueue(g1);
                            hangar.GetHangarShip().AI.State = AIState.Ferrying;
                            continue;
                        }

                        hangar.GetHangarShip().AI.State =
                            AIState.ReturnToHangar; //shuttle with no target
                        continue;
                    }

                    if (sortedList[skip] != null &&
                        hangar.GetHangarShip().AI.EscortTarget == sortedList[skip] &&
                        hangar.GetHangarShip().AI.State == AIState.Ferrying)
                    {
                        inboundOrdinance = inboundOrdinance + 100f;
                        if ((inboundOrdinance + sortedList[skip].Ordinance) /
                            sortedList[skip].OrdinanceMax > 0.5f)
                        {
                            if (skip >= sortedList.Length - 1)
                                return;
                            ;
                            skip++;
                            inboundOrdinance = 0;
                        }
                    }

                    continue;
                }

                if (hangar.hangarShipUID.IsEmpty())                        
                    hangar.hangarShipUID = "Supply_Shuttle";
                        

                var supplyShuttle = ResourceManager.GetShipTemplate(hangar.hangarShipUID);
                if (!hangar.Active || hangar.hangarTimer > 0f ||
                    Owner.Ordinance <= 100f || sortedList[skip] == null)
                    continue;

                if (supplyShuttle.Mass / 5f >
                    Owner.Ordinance) //fbedard: New spawning cost
                    continue;
                Ship shuttle =
                    Ship.CreateShipFromHangar(hangar, Owner.loyalty, Owner.Center, Owner);


                //shuttle.GetAI().DefaultAIState = AIState.Flee;
                shuttle.Velocity = UniverseRandom.RandomDirection() * shuttle.Speed + Owner.Velocity;
                if (shuttle.Velocity.Length() > shuttle.velocityMaximum)
                    shuttle.Velocity = Vector2.Normalize(shuttle.Velocity) * shuttle.Speed;
                Owner.Ordinance -= shuttle.Mass / 5f;

                if (Owner.Ordinance >= 100f && Owner.OrdnanceStatus > ShipStatus.Critical)
                {
                    //inboundOrdinance        = inboundOrdinance + 100f;
                    Owner.Ordinance         = Owner.Ordinance - 100f;
                    hangar.SetHangarShip(shuttle);
                    var g                   = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
                    shuttle.AI.EscortTarget = sortedList[skip];
                    shuttle.AI.IgnoreCombat = true;
                    shuttle.AI.OrderQueue.Clear();
                    shuttle.AI.OrderQueue.Enqueue(g);
                    shuttle.AI.State        = AIState.Ferrying;
                }
                else //fbedard: Go fetch ordinance when mothership is low on ordinance
                {
                    shuttle.Ordinance       = 0f;
                    hangar.SetHangarShip(shuttle);
                    shuttle.AI.IgnoreCombat = true;
                    shuttle.AI.State        = AIState.Resupply;
                    shuttle.AI.OrderResupplyNearest(true);
                }

                break;
            }
        }

        private void SetCombatStatus(float elapsedTime)
        {
            float radius = GetSensorRadius(out Ship sensorShip);
            //Vector2 senseCenter = sensorShip.Center;
            if (Owner.fleet != null)
            {
                if (!HasPriorityTarget )
                    Target = ScanForCombatTargets(sensorShip, radius);
                else
                    ScanForCombatTargets(sensorShip, radius);
            }
            else if (!HasPriorityTarget)
            {
                //#if DEBUG
                //                if (this.State == AIState.Intercept && this.Target != null)
                //                    Log.Info(this.Target); 
                //#endif
                if (Owner.Mothership != null)
                {
                    Target = ScanForCombatTargets(sensorShip, radius);

                    if (Target == null)
                        Target = Owner.Mothership.AI.Target;
                }
                else
                {
                    Target = ScanForCombatTargets(sensorShip, radius);
                }
            }
            else
            {
                if (Owner.Mothership != null)
                    Target = ScanForCombatTargets(sensorShip, radius) ?? Owner.Mothership.AI.Target;
                else
                    ScanForCombatTargets(sensorShip, radius);
            }
            if (State == AIState.Resupply)
                return;
            if (DoNotEnterCombat) return;
            if (Owner.fleet != null && State == AIState.FormationWarp)
            {
                bool doreturn = !(Owner.fleet != null && State == AIState.FormationWarp &&
                                  Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset) < 15000f);
                if (doreturn)
                    return;
            }
            //@TODO Investigate why datanodes can be null here.
            //Also investigate why this is here at all. 
            //it seems to be setting the ships FleetNode value... why?? looks like a bug workaround. 
            //if (Owner.fleet?.DataNodes != null)  
            //    using (Owner.fleet.DataNodes.AcquireReadLock())
            //        foreach (FleetDataNode datanode in Owner.fleet.DataNodes)
            //        {
            //            if (datanode?.Ship != Owner)
            //                continue;
            //            FleetNode = datanode;
            //            break;
                    //}
            
            if (Target == null || Owner.InCombat) return;
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

        public float GetSensorRadius() => GetSensorRadius(out Ship sensorShip);

        public float GetSensorRadius(out Ship sensorShip)
        {
            if (!UseSensorsForTargets) //this is obsolete and not needed anymore. 
            {
                var radius = 30000f;
                sensorShip = Owner.Mothership ?? Owner;
                return radius;
            }


            if (Owner.Mothership != null)
            {
                //get the motherships sensor status. 
                float motherRange = Owner.Mothership.AI.GetSensorRadius(out sensorShip);

                //in radius of the motherships sensors then use that.
                if (Owner.Center.InRadius(sensorShip.Center, motherRange - Owner.SensorRange))
                    return motherRange;               
            }
            sensorShip = Owner;
            float sensorRange = Owner.SensorRange + (Owner.inborders ? 10000 : 0);
            return sensorRange;
            
            
        }

        private bool DoNotEnterCombat
        {
            get
            {
                if (
                    (Owner.shipData.Role == ShipData.RoleName.freighter ||
                     Owner.shipData.ShipCategory == ShipData.Category.Civilian) && Owner.CargoSpaceMax > 0 ||
                    Owner.DesignRole == ShipData.RoleName.scout || Owner.isConstructor ||
                    Owner.DesignRole == ShipData.RoleName.troop || IgnoreCombat || State == AIState.Resupply ||
                    State == AIState.ReturnToHangar || State == AIState.Colonize ||
                    Owner.DesignRole == ShipData.RoleName.supply)
                    return true;
                return false;
            }
        }

        public void DropBombsAtGoal(ShipGoal goal, float radius)
        {
            if (Owner.Center.InRadius(goal.TargetPlanet.Center, radius))
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
                        UniverseScreen.BombList.Add(bomb);
                        bombBay.BombTimer = wepTemplate.fireDelay;
                    }
                }
            }
        }


        public struct ShipWeight
        {
            public Ship Ship;
            public float Weight;

            public ShipWeight(Ship ship, float weight = 0)
            {
                Ship = ship;
                Weight = weight;                
            }
            public ShipWeight(GameplayObject gamePlayObject, float weight = 0) : this(gamePlayObject as Ship, weight) { }            
            
            //We can just say shipWieght += 2 to add 2 the shipweight
            public static ShipWeight operator + (ShipWeight shipWeight, float weight) => new ShipWeight(shipWeight.Ship, shipWeight.Weight + weight);            
            
            //same this for a ship although... seems silly since im not "adding" a ship.
            public static ShipWeight operator +(ShipWeight shipWeight, Ship ship) => new ShipWeight(ship, shipWeight.Weight);

            //i dont know how overload the "=" operator and keep the ship. 
            public void SetWeight(float weight) => Weight = weight;
            


        }
    }
}