using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;

namespace Ship_Game.AI
{

    public sealed partial class ShipAI
    {
        public bool UseSensorsForTargets = true;
        public CombatState CombatState = CombatState.AttackRuns;
        public CombatAI CombatAI = new CombatAI();
        public BatchRemovalCollection<Ship> PotentialTargets = new BatchRemovalCollection<Ship>();
        public Ship EscortTarget;
        private float ScanForThreatTimer;
        public Planet ExterminationTarget;
        public bool Intercepting { get; private set; }
        public Guid TargetGuid;
        public bool IgnoreCombat;
        public bool BadGuysNear;
        public bool Troopsout = false;
        public Array<Projectile> TrackProjectiles = new Array<Projectile>();
        public Guid EscortTargetGuid;
        public Ship Target;
        public Array<Ship> TargetQueue = new Array<Ship>();
        public bool HasPriorityTarget;
        private float TriggerDelay;

        public void CancelIntercept()
        {
            HasPriorityTarget = false;
            Intercepting = false;
        }

        public void FireOnTarget()
        {
            // base reasons not to fire. @TODO actions decided by loyalty like should be the same in all areas. 
            if (!Owner.hasCommand || Owner.engineState == Ship.MoveState.Warp
                || Owner.EMPdisabled || Owner.Weapons.Count == 0 || !BadGuysNear)
                return;

            int count = Owner.Weapons.Count;
            Weapon[] weapons = Owner.Weapons.GetInternalArrayItems();

            if (Target?.Active == false || Target is Ship ship && ship.dying)
            {
                Target = null;
            }

            for (int i = 0; i < count; ++i)
            {
                weapons[i].UpdateAndFireAtTarget(Target, TrackProjectiles, PotentialTargets);
            }
        }

        void UpdateTrackedProjectiles()
        {
            bool hasPointDefense = OwnerHasPointDefense();

            TrackProjectiles.Clear();
            if (Owner.Mothership != null)
                TrackProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
            if (Owner.TrackingPower <= 0 || !hasPointDefense) return;
            // @todo This usage of GetNearby is slow! Consider creating a specific SpatialManager search function
            foreach (GameplayObject go in Owner.GetObjectsInSensors(GameObjectType.Proj, Owner.maxWeaponsRange))
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

        public Ship ScanForCombatTargets(Ship sensorShip, float radius)
        {
            BadGuysNear = false;
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
                    if (!HasPriorityOrder && Owner.loyalty != Empire.Universe.player)
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
            {
                for (int i = 0; i < thisSystem.PlanetList.Count; i++)
                {
                    Planet p = thisSystem.PlanetList[i];
                    BadGuysNear = BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) &&
                                  Owner.Center.InRadius(p.Center, radius);
                }
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
                if (!isAttackable)
                    continue;

                armorAvg += nearbyShip.armor_max;
                shieldAvg += nearbyShip.shield_max;
                dpsAvg += nearbyShip.GetDPS();
                sizeAvg += nearbyShip.SurfaceArea;
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
            SetTargetWeights(armorAvg, shieldAvg, dpsAvg, sizeAvg);

            ShipWeight[] sortedList2 = NearByShips.Filter(weight => weight.Weight > -100)
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

            Ship targetShip = null;
            if (sortedList2.Any())
                targetShip = sortedList2.ElementAt(0).Ship;

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

                Vector2 fleetPos = Owner.fleet.AveragePosition() + FleetNode.FleetOffset;
                float distanceToFleet = fleetPos.Distance(copyWeight.Ship.Center);
                copyWeight += FleetNode.OrdersRadius <= distanceToFleet ? 0 : -distanceToFleet / FleetNode.OrdersRadius;
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.GetDPS(), dpsAvg, FleetNode.DPSWeight);
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.shield_power, shieldAvg,
                    FleetNode.AttackShieldedWeight);
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.armor_max, armorAvg, FleetNode.ArmoredWeight);
                copyWeight += FleetNode.ApplyWeight(copyWeight.Ship.SurfaceArea, sizeAvg, FleetNode.SizeWeight);
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
            if (Owner.engineState == Ship.MoveState.Warp ||!Owner.Carrier.HasSupplyBays ) return;

            Ship[] sortedList = FriendliesNearby.Filter(ship => ship.shipData.Role != ShipData.RoleName.supply 
                                                                  && ship.AI.State == AIState.ResupplyEscort
                                                                  && ship != Owner)       
                .OrderBy(ship =>
                {
                    var distance = Owner.Center.Distance(ship.Center);
                    distance = (int)distance * 10 / radius;
                    return (int)ship.OrdnanceStatus * distance + (ship.fleet == Owner.fleet ? 0 : 10 );
                }).ToArray();

            if (sortedList.Length <= 0 ) return;            

            var skipShip = 0;
            var inboundOrdinance = 0f;
            //oh crap this is really messed up.  FB: working on it.
            foreach (ShipModule hangar in Owner.Carrier.AllActiveHangars.Filter(hangar => hangar.IsSupplyBay))
            {
                Ship supplyShipInSpace = hangar.GetHangarShip();
                if (supplyShipInSpace != null && supplyShipInSpace.Active)
                {
                    if (SupplyShipIdle(supplyShipInSpace))
                    {
                        if (sortedList[skipShip] != null)
                        {
                            SetSupplyTarget(supplyShipInSpace);
                            supplyShipInSpace.AI.State = AIState.Ferrying;
                            continue;
                        }

                        supplyShipInSpace.AI.OrderReturnToHangar(); //shuttle with no target
                        continue;
                    }
                    // FB: check if the same ship needs more ordnance based on resupply class ordnance threshold
                    if (sortedList[skipShip] != null &&
                        supplyShipInSpace.AI.EscortTarget == sortedList[skipShip] &&
                        supplyShipInSpace.AI.State == AIState.Ferrying)
                    {
                        inboundOrdinance = inboundOrdinance + supplyShipInSpace.OrdinanceMax;
                        if ((inboundOrdinance + sortedList[skipShip].Ordinance) /
                            sortedList[skipShip].OrdinanceMax > ShipResupply.ResupplyShuttleOrdnanceThreshold)
                        {
                            if (skipShip >= sortedList.Length - 1)
                                return;

                            skipShip++;
                            inboundOrdinance = 0;
                        }
                    }
                    continue;
                }

                hangar.hangarShipUID = Ship.GetSupplyShuttleName(Owner.loyalty);
                var supplyShuttle = ResourceManager.GetShipTemplate(hangar.hangarShipUID);
                if (!hangar.Active || hangar.hangarTimer > 0f || sortedList[skipShip] == null)
                    continue;

                if (supplyShuttle.ShipOrdLaunchCost > Owner.Ordinance) //fbedard: New spawning cost
                    continue;

                Ship shuttle = Ship.CreateShipFromHangar(hangar, Owner.loyalty, Owner.Center, Owner);
                shuttle.Velocity = UniverseRandom.RandomDirection() * shuttle.Speed + Owner.Velocity;
                if (shuttle.Velocity.Length() > shuttle.velocityMaximum)
                    shuttle.Velocity = shuttle.Velocity.Normalized() * shuttle.Speed;

                Owner.ChangeOrdnance(-shuttle.ShipOrdLaunchCost);
                Owner.ChangeOrdnance(-shuttle.OrdinanceMax);
                hangar.SetHangarShip(shuttle);
                SetSupplyTarget(shuttle);
                if (Owner.Ordinance >= shuttle.OrdinanceMax)
                    shuttle.AI.State = AIState.Ferrying;
                else // FB: Fatch ordnance from a colony when mothership doesnt have enough ordnance
                {
                    shuttle.ChangeOrdnance(-shuttle.OrdinanceMax);
                    shuttle.AI.GoOrbitNearestPlanetAndResupply(false);
                }
                break;
            }

            void SetSupplyTarget(Ship ship)
            {
                ship.AI.EscortTarget = sortedList[skipShip];
                ship.AI.IgnoreCombat = true;
                ship.AI.ClearOrders();
                ship.AI.AddShipGoal(Plan.SupplyShip);
            }

            bool SupplyShipIdle(Ship supplyShip)
            {
                return supplyShip.AI.State != AIState.Ferrying &&
                       (supplyShip.AI.State != AIState.ReturnToHangar || supplyShip.Ordinance > 0) &&
                       supplyShip.AI.State != AIState.Resupply &&
                       supplyShip.AI.State != AIState.Scrap;
            }
        }

        private void SetCombatStatus(float elapsedTime)
        {
            float radius = GetSensorRadius(out Ship sensorShip);
            //Vector2 senseCenter = sensorShip.Center;
            if (Owner.fleet != null)
            {
                if (!HasPriorityTarget)
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
                EnterCombat();

            else if (!HasPriorityOrder)
                EnterCombat();

            else if ((Owner.IsPlatform || Owner.shipData.Role == ShipData.RoleName.station) 
                     && OrderQueue.PeekFirst.Plan != Plan.DoCombat)
                EnterCombat();
            else
            {
                if (CombatState == CombatState.HoldPosition || OrderQueue.NotEmpty)
                    return;
                EnterCombat();
            }
        }

        private void EnterCombat()
        {
            State = AIState.Combat;
            AddShipGoal(Plan.DoCombat);
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
                if (IgnoreCombat || Owner.IsFreighter
                                 || Owner.DesignRole == ShipData.RoleName.supply 
                                 || Owner.DesignRole == ShipData.RoleName.scout
                                 || Owner.DesignRole == ShipData.RoleName.troop
                                 || State == AIState.Resupply 
                                 || State == AIState.ReturnToHangar 
                                 || State == AIState.Colonize
                                 || Owner.IsConstructor)
                {
                    return true;
                }

                return false;
            }
        }

        public void DropBombsAtGoal(ShipGoal goal, float radius)
        {
            if (!Owner.Center.InRadius(goal.TargetPlanet.Center, radius)) return;
            foreach (ShipModule bombBay in Owner.BombBays)
            {
                if (bombBay.InstalledWeapon.CooldownTimer > 0f)
                    continue;
                var bomb = new Bomb(new Vector3(Owner.Center, 0f), Owner.loyalty, bombBay.BombType);
                if (Owner.Ordinance > bombBay.InstalledWeapon.OrdinanceRequiredToFire)
                {
                    Owner.ChangeOrdnance(-bombBay.InstalledWeapon.OrdinanceRequiredToFire);
                    bomb.SetTarget(goal.TargetPlanet);
                    Empire.Universe.BombList.Add(bomb);
                    bombBay.InstalledWeapon.CooldownTimer = bombBay.InstalledWeapon.fireDelay;
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