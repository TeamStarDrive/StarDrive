using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

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
                UpdateTrackedProjectiles();

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
            if (Owner.TrackingPower > 0 && hasPointDefense) // update projectile list                         
            {
                // @todo This usage of GetNearby is slow! Consider creating a specific SpatialManager search function
                foreach (GameplayObject go in Owner.GetObjectsInSensors(GameObjectType.Proj))
                {
                    var missile = (Projectile) go;
                    if (missile.Weapon.Tag_Intercept && Owner.loyalty.IsEmpireAttackable(missile.Loyalty))
                        TrackProjectiles.Add(missile);
                }
                TrackProjectiles.Sort(missile => Owner.Center.SqDist(missile.Center));
            }
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
                if (weapon.FireTarget == null || !weapon.Module.Active || weapon.CooldownTimer > 0f || !weapon.Module.Powered)
                    continue;

                weapon.FireAtAssignedTarget();
            }
        }

        public GameplayObject ScanForCombatTargets(Vector2 position, float radius)
        {
            BadGuysNear = false;
            FriendliesNearby.Clear();
            PotentialTargets.Clear();
            NearbyShips.Clear();
            //this.TrackProjectiles.Clear();



            if (HasPriorityTarget && Target == null)
            {
                HasPriorityTarget = false;
                if (TargetQueue.Count > 0)
                {
                    HasPriorityTarget = true;
                    Target = TargetQueue.First();
                }
            }
            if (Target != null)
            {
                if ((Target as Ship).loyalty == Owner.loyalty)
                {
                    Target = null;
                    HasPriorityTarget = false;
                }
                else if (!Intercepting && (Target as Ship).engineState == Ship.MoveState.Warp)
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
            {
                if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
                {
                    var sw = new ShipWeight
                    {
                        Ship = EscortTarget.AI.Target as Ship,
                        Weight = 2f
                    };
                    NearbyShips.Add(sw);
                }
                GameplayObject[] nearbyShips = Owner.GetObjectsInSensors(GameObjectType.Ship);
                foreach (var go in nearbyShips)
                {
                    var nearbyShip = (Ship)go;
                    if (!nearbyShip.Active || nearbyShip.dying
                        || Owner.Center.OutsideRadius(nearbyShip.Center, radius + (radius < 0.01f ? 10000 : 0)))
                        continue;

                    Empire empire = nearbyShip.loyalty;
                    if (empire == Owner.loyalty)
                    {
                        FriendliesNearby.Add(nearbyShip);
                        continue;
                    }
                    bool isAttackable = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                    if (!isAttackable) continue;
                    armorAvg  += nearbyShip.armor_max;
                    shieldAvg += nearbyShip.shield_max;
                    dpsAvg    += nearbyShip.GetDPS();
                    sizeAvg   += nearbyShip.Size;
                    BadGuysNear       = true;
                    if (radius < 1)
                        continue;
                    var sw            = new ShipWeight
                    {
                        Ship = nearbyShip,
                        Weight = 1f
                    };
                    NearbyShips.Add(sw);

                    if (BadGuysNear && nearbyShip.AI.Target is Ship nearbyShipsTarget &&
                        nearbyShipsTarget == EscortTarget && nearbyShip.engineState != Ship.MoveState.Warp)
                    {
                        sw.Weight = 3f;
                    }
                }
            }
            armorAvg /= NearbyShips.Count + .01f;
            shieldAvg /= NearbyShips.Count + .01f;
            dpsAvg /= NearbyShips.Count + .01f;

            #region supply ship logic   //fbedard: for launch only

            if (Owner.GetHangars().Find(hangar => hangar.IsSupplyBay) != null &&
                Owner.engineState != Ship.MoveState.Warp) // && !this.Owner.isSpooling
            {
                IOrderedEnumerable<Ship> sortedList = null;
                {
                    sortedList = FriendliesNearby.FilterBy(ship => ship != Owner
                                                                && ship.engineState != Ship.MoveState.Warp
                                                                && ship.AI.State != AIState.Scrap
                                                                && ship.AI.State != AIState.Resupply
                                                                && ship.AI.State != AIState.Refit
                                                                && ship.Mothership == null
                                                                && ship.OrdinanceMax > 0
                                                                && ship.Ordinance / ship.OrdinanceMax < 0.5f
                                                                && !ship.IsTethered())
                        .OrderBy(ship => Math.Truncate(Vector2.Distance(Owner.Center, ship.Center) + 4999) / 5000)
                        .ThenByDescending(ship => ship.OrdinanceMax - ship.Ordinance);
//                      .OrderBy(ship => ship.HasSupplyBays).ThenBy(ship => ship.OrdAddedPerSecond).ThenBy(ship => Math.Truncate((Vector2.Distance(this.Owner.Center, ship.Center) + 4999)) / 5000).ThenBy(ship => ship.OrdinanceMax - ship.Ordinance);
                }

                if (sortedList.Any() )
                {
                    var skip = 0;
                    var inboundOrdinance = 0f;
                    if (Owner.HasSupplyBays)
                        foreach (ShipModule hangar in Owner.GetHangars().Where(hangar => hangar.IsSupplyBay))
                        {
                            if (hangar.GetHangarShip() != null && hangar.GetHangarShip().Active)
                            {
                                if (hangar.GetHangarShip().AI.State != AIState.Ferrying &&
                                    hangar.GetHangarShip().AI.State != AIState.ReturnToHangar &&
                                    hangar.GetHangarShip().AI.State != AIState.Resupply &&
                                    hangar.GetHangarShip().AI.State != AIState.Scrap)
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
                                        hangar.GetHangarShip().AI.State =
                                            AIState.ReturnToHangar; //shuttle with no target
                                        continue;
                                    }
                                }
                                else if (sortedList.Skip(skip).Count() > 0 &&
                                         hangar.GetHangarShip().AI.EscortTarget == sortedList.Skip(skip).First() &&
                                         hangar.GetHangarShip().AI.State == AIState.Ferrying)
                                {
                                    inboundOrdinance = inboundOrdinance + 100f;
                                    if ((inboundOrdinance + sortedList.Skip(skip).First().Ordinance) /
                                        sortedList.First().OrdinanceMax > 0.5f)
                                    {
                                        skip++;
                                        inboundOrdinance = 0;
                                        continue;
                                    }
                                }
                                continue;
                            }
                            if (!hangar.Active || hangar.hangarTimer > 0f ||
                                Owner.Ordinance >= 100f && sortedList.Skip(skip).Any())
                                continue;
                            if (ResourceManager.GetShipTemplate(hangar.hangarShipUID).Mass / 5f >
                                Owner.Ordinance) //fbedard: New spawning cost
                                continue;
                            Ship shuttle =
                                Ship.CreateShipFromHangar(hangar.hangarShipUID, Owner.loyalty, Owner.Center, Owner);
                            shuttle.VanityName = "Supply_Shuttle";
                            //shuttle.shipData.Role = ShipData.RoleName.supply;
                            //shuttle.GetAI().DefaultAIState = AIState.Flee;
                            shuttle.Velocity = UniverseRandom.RandomDirection() * shuttle.Speed + Owner.Velocity;
                            if (shuttle.Velocity.Length() > shuttle.velocityMaximum)
                                shuttle.Velocity = Vector2.Normalize(shuttle.Velocity) * shuttle.Speed;
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
                            else //fbedard: Go fetch ordinance when mothership is low on ordinance
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
                OrderScrapShip(); //Destroy shuttle without mothership

            #endregion

            //}           
            for (int i = 0; i < NearbyShips.Count; i++)
            {
                ShipWeight nearbyShip = NearbyShips[i];
                if (nearbyShip.Ship.loyalty != Owner.loyalty)
                {
                    nearbyShip.Weight += CombatAI.ApplyWeight(nearbyShip.Ship);
                    if (Owner.fleet == null || FleetNode == null) continue;

                    nearbyShip.Weight += FleetNode.ApplyWeight(nearbyShip.Ship.GetDPS(), dpsAvg, FleetNode.DPSWeight);                    
                    nearbyShip.Weight += FleetNode.ApplyWeight(nearbyShip.Ship.shield_power, shieldAvg, FleetNode.AttackShieldedWeight);
                    nearbyShip.Weight += FleetNode.ApplyWeight(nearbyShip.Ship.armor_max, armorAvg, FleetNode.ArmoredWeight);
                    nearbyShip.Weight += FleetNode.ApplyWeight(nearbyShip.Ship.Size, sizeAvg, FleetNode.SizeWeight);
                    nearbyShip.Weight += FleetNode.ApplyFleetWeight(Owner.fleet, nearbyShip.Ship);
                    
                }
                else
                {
                    NearbyShips.QueuePendingRemoval(nearbyShip);
                }
            }

            NearbyShips.ApplyPendingRemovals();
            IEnumerable<ShipWeight> sortedList2 =
                from potentialTarget in NearbyShips
                orderby potentialTarget.Weight
                descending //, Vector2.Distance(potentialTarget.ship.Center,this.Owner.Center) 
                select potentialTarget;
            PotentialTargets.ClearAdd(sortedList2.Select(ship => ship.Ship));

            if (Target != null && !Target.Active)
            {
                Target = null;
                HasPriorityTarget = false;
            }
            else if (Target != null && Target.Active && HasPriorityTarget)
            {
                var ship = Target as Ship;
                if (Owner.loyalty.GetRelations(ship.loyalty).AtWar || Owner.loyalty.isFaction || ship.loyalty.isFaction)
                    BadGuysNear = true;
                return Target;
            }
            if (sortedList2.Any())
                Target = sortedList2.ElementAt(0).Ship;

            if (Owner.Weapons.Count > 0 || Owner.GetHangars().Count > 0)
                return Target;
            return null;
        }        

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
                    if (Vector2.Distance(Owner.Center, Owner.Mothership.Center) <=
                        Owner.Mothership.SensorRange - Owner.SensorRange)
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
            else if (Owner.Mothership != null)
                senseCenter = Owner.Mothership.Center;


            if (Owner.fleet != null)
            {
                if (!HasPriorityTarget)
                    Target = ScanForCombatTargets(senseCenter, radius);
                else
                    ScanForCombatTargets(senseCenter, radius);
            }
            else if (!HasPriorityTarget)
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
            if (
                (Owner.shipData.Role == ShipData.RoleName.freighter ||
                 Owner.shipData.ShipCategory == ShipData.Category.Civilian) && Owner.CargoSpaceMax > 0 ||
                Owner.shipData.Role == ShipData.RoleName.scout || Owner.isConstructor ||
                Owner.shipData.Role == ShipData.RoleName.troop || IgnoreCombat || State == AIState.Resupply ||
                State == AIState.ReturnToHangar || State == AIState.Colonize ||
                Owner.shipData.Role == ShipData.RoleName.supply)
                return;
            if (Owner.fleet != null && State == AIState.FormationWarp)
            {
                bool doreturn = !(Owner.fleet != null && State == AIState.FormationWarp &&
                                  Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset) < 15000f);
                if (doreturn)
                    return;
            }
            if (Owner.fleet != null)
                foreach (FleetDataNode datanode in Owner.fleet.DataNodes)
                {
                    if (datanode.Ship != Owner)
                        continue;
                    FleetNode = datanode;
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


        public class ShipWeight
        {
            public Ship Ship;

            public float Weight;
            public bool DefendEscort;

            public ShipWeight() {}
        }
    }
}