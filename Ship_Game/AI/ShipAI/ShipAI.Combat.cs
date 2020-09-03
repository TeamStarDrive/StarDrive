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
        public Array<Projectile> TrackProjectiles = new Array<Projectile>();
        public Guid EscortTargetGuid;
        public Ship Target;
        public Array<Ship> TargetQueue = new Array<Ship>();
        public bool HasPriorityTarget;
        private float TriggerDelay;
        
        readonly Array<ShipWeight> ScannedNearby      = new Array<ShipWeight>();
        readonly Array<Ship> ScannedTargets           = new Array<Ship>();
        readonly Array<Ship> ScannedFriendlies        = new Array<Ship>();
        readonly Array<Projectile> ScannedProjectiles = new Array<Projectile>();
        Ship ScannedTarget = null;
        
        bool ScanComplete = true;
        bool ScanDataProcessed = true;
        bool ScanTargetUpdated = true;

        public GameplayObject[] GetObjectsInSensors(GameObjectType gameObjectType, float radius)
        {
            return UniverseScreen.SpaceManager.FindNearby(Owner, radius, gameObjectType);
        }

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

            // ScanDataProcessed check prevents the potential targets and projectile lists from being modified 
            // while they could be being updated on the ship thread. when the ScanDataProcessed is true the lists have been been updated.
            // while false it means there are results waiting to be processed. 
            if (ScanDataProcessed)
            {
                for (int x = PotentialTargets.Count - 1; x >= 0; x--)
                {
                    var target = PotentialTargets[x];
                    if (target == null || !target.Active || target.Health <= 0.0f || target.dying)
                        PotentialTargets.RemoveAtSwapLast(x);
                }

                for (int x = TrackProjectiles.Count - 1; x >= 0; x--)
                {
                    var target = TrackProjectiles[x];
                    if (target == null || !target.Active || target.Health <= 0.0f)
                        TrackProjectiles.RemoveAtSwapLast(x);
                }
            }

            if (Target?.Active == false || Target?.Health <= 0.0f || Target is Ship ship && ship.dying)
            {
                ScannedTarget = null;
                ScanTargetUpdated = true;
            }

            for (int i = 0; i < count; ++i)
            {
                var weapon = weapons[i];
                if (weapon.UpdateAndFireAtTarget(ScannedTarget, TrackProjectiles, PotentialTargets) &&
                    weapon.FireTarget.ParentIsThis(ScannedTarget))
                {
                    float weaponFireTime;
                    if (weapon.isBeam)
                    {
                        weaponFireTime = weapon.BeamDuration.LowerBound(FireOnMainTargetTime);
                    }
                    else if (weapon.SalvoDuration > 0)
                    {
                        weaponFireTime = weapon.SalvoDuration.LowerBound(FireOnMainTargetTime);
                    }
                    else
                    {
                        weaponFireTime = (1f - weapon.CooldownTimer).LowerBound(FireOnMainTargetTime);
                    }
                    FireOnMainTargetTime = weaponFireTime.LowerBound(FireOnMainTargetTime);
                }
            }
        }

        void UpdateTrackedProjectiles()
        {
            bool hasPointDefense = OwnerHasPointDefense();

            if (Owner.TrackingPower <= 0 || !hasPointDefense)
            {
                if (Owner.Mothership != null)
                {
                    Owner.Mothership.AI.TrackProjectiles.ForEach(p=> TrackProjectiles.AddUnique(p));
                }
                return;
            }

            GameplayObject[] projectiles = GetObjectsInSensors(GameObjectType.Proj, Owner.WeaponsMaxRange);
            {
                ScannedProjectiles.Clear();
                if (Owner.Mothership != null)
                    ScannedProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
                for (int i = 0; i < projectiles.Length; i++)
                {
                    GameplayObject go = projectiles[i];
                    var missile = (Projectile) go;
                    if (missile.Weapon.Tag_Intercept && Owner.loyalty.IsEmpireAttackable(missile.Loyalty))
                        ScannedProjectiles.AddUniqueRef(missile);
                }
            }
            ScannedProjectiles.Sort(missile => Owner.Center.SqDist(missile.Center));
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
            Owner.KnownByEmpires.SetSeen(Owner.loyalty);
            if (Empire.Universe?.Debug == true)
            {
                if (Empire.Universe.SelectedShip == null || Empire.Universe.SelectedShip == Owner)
                {
                    Owner.KnownByEmpires.SetSeen(EmpireManager.Player);
                }
            }
            BadGuysNear = false;
            ScannedFriendlies.Clear();
            ScannedTargets.Clear();
            ScannedNearby.Clear();

            if (HasPriorityTarget)
            {
                if (Target == null)
                {
                    HasPriorityTarget = false;
                    if (TargetQueue.Count > 0)
                    {
                        HasPriorityTarget = true;
                        ScannedTarget = TargetQueue.First();
                    }
                }
            }

            UpdateTrackedProjectiles();

            

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
                    BadGuysNear = BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) && Owner.Center.InRadius(p.Center, radius);
                }
            }

            GameplayObject[] scannedShips = sensorShip.AI.GetObjectsInSensors(GameObjectType.Ship, radius + (radius < 0.01f ? 10000 : 0));
 
            for (int x = 0; x < scannedShips.Length; x++)
            {
                var go = scannedShips[x];
                var nearbyShip = (Ship) go;

                var sw = new ShipWeight(nearbyShip, 1);
                ScannedNearby.Add(sw);

                nearbyShip.KnownByEmpires.SetSeen(Owner.loyalty);
                if (Empire.Universe?.Debug == true)
                {
                    if (Empire.Universe.SelectedShip == null || Empire.Universe.SelectedShip == Owner)
                    {
                        nearbyShip.KnownByEmpires.SetSeen(EmpireManager.Player);
                    }
                }
                if (!nearbyShip.Active || nearbyShip.dying)
                { 
                    continue;
                }

                Empire empire = nearbyShip.loyalty;
                if (empire == Owner.loyalty)
                {
                    ScannedFriendlies.Add(nearbyShip);
                    continue;
                }

                bool canAttack = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                if (!canAttack)
                    continue;
                BadGuysNear = true;
                if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange.AlmostZero())
                {
                    ScannedTargets.Add(nearbyShip);
                    continue;
                }

                armorAvg  += nearbyShip.armor_max;
                shieldAvg += nearbyShip.shield_max;
                dpsAvg    += nearbyShip.GetDPS();
                sizeAvg   += nearbyShip.SurfaceArea;
                
                if (radius < 1)
                    continue;

                if (BadGuysNear && nearbyShip.AI.Target is Ship ScannedNearbyTarget &&
                    ScannedNearbyTarget == EscortTarget && nearbyShip.engineState != Ship.MoveState.Warp)
                {
                    sw += 3f;
                }

                ScannedNearby[ScannedNearby.Count -1] = sw;
            }

            if (Target is Ship target)
            {
                if (target.loyalty == Owner.loyalty)
                {
                    ScannedTarget = null;
                    HasPriorityTarget = false;
                }
                else if (!Intercepting && target.engineState == Ship.MoveState.Warp)
                {
                    ScannedTarget = null;
                    if (!HasPriorityOrder && Owner.loyalty != Empire.Universe.player)
                        State = AIState.AwaitingOrders;
                    return null;
                }
            }

            if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange.AlmostZero())
                return Target;

            if (Target is Ship shipTarget)
            {
                if (Owner.fleet != null && !HasPriorityOrder && !HasPriorityTarget)
                {
                    var sw = new ShipWeight(shipTarget, 1);
                    ScannedNearby.AddUnique(sw);
                }
            }
            if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
            {
                var sw = new ShipWeight(EscortTarget.AI.Target, 2f);
                ScannedNearby.AddUnique(sw);
            }

            SetTargetWeights(armorAvg, shieldAvg, dpsAvg, sizeAvg);

            ShipWeight[] sortedList2 = ScannedNearby.Filter(weight => weight.Weight > -100)
                .OrderByDescending(weight => weight.Weight).ToArray();

            ScannedTargets.AddRange(sortedList2.Select(ship => ship.Ship));

            if (Target?.Active != true)
            {
                ScannedTarget = null;
                HasPriorityTarget = false;
            }
            else if (Target?.Active == true && HasPriorityTarget && Target is Ship ship)
            {
                if (Owner.loyalty.IsEmpireAttackable(ship.loyalty, ship))
                    BadGuysNear = true;
                return Target;
            }

            Ship targetShip = null;
            if (sortedList2.Length > 0)
                targetShip = sortedList2[0].Ship;

            if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars)
                return targetShip;
            return null;
        }

        private void SetTargetWeights(float armorAvg, float shieldAvg, float dpsAvg, float sizeAvg)
        {
            armorAvg  /= ScannedNearby.Count + .01f;
            shieldAvg /= ScannedNearby.Count + .01f;
            dpsAvg    /= ScannedNearby.Count + .01f;

            for (int i = ScannedNearby.Count - 1; i >= 0; i--)
            {
                ShipWeight copyWeight = ScannedNearby[i]; //Remember we have a copy.

                if (!Owner.loyalty.IsEmpireAttackable(copyWeight.Ship.loyalty))
                {
                    copyWeight.Weight = float.MinValue;
                    ScannedNearby[i] = copyWeight;
                    continue;
                }

                copyWeight += CombatAI.ApplyWeight(copyWeight.Ship);

                if (Owner.fleet == null || FleetNode == null)
                {
                    ScannedNearby[i] = copyWeight;//update stored weight from copy
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
                //ShipWeight is a struct so we are working with a copy. Need to overwrite existing value. 
                ScannedNearby[i] = copyWeight;
            }
        }
        
        public void ScanForTargets()
        {
            float radius = GetSensorRadius(out Ship sensorShip);
            if (Owner.IsSubspaceProjector)
            {
                ScanForCombatTargets(sensorShip, radius); 
                ScanComplete = true;
                return;
            }

            if (Owner.fleet != null)
            {
                if (!HasPriorityTarget)
                    ScannedTarget = ScanForCombatTargets(sensorShip, radius);
                else
                    ScanForCombatTargets(sensorShip, radius);
            }
            else if (!HasPriorityTarget)
            {
                if (Owner.Mothership != null)
                    ScannedTarget = ScanForCombatTargets(sensorShip, radius) ?? Owner.Mothership.AI.Target;
                else
                    ScannedTarget = ScanForCombatTargets(sensorShip, radius);
            }
            else
            {
                if (Owner.Mothership != null)
                    ScannedTarget = ScanForCombatTargets(sensorShip, radius) ?? Owner.Mothership.AI.Target;
                else
                    ScanForCombatTargets(sensorShip, radius);
            }

            ScanComplete = true;

            if (State == AIState.Resupply || DoNotEnterCombat)
                return;

            if (Owner.fleet != null && State == AIState.FormationWarp && HasPriorityOrder && !HasPriorityTarget)
            {
                bool doreturn = !(Owner.fleet != null && State == AIState.FormationWarp &&
                                  Owner.Center.InRadius(Owner.fleet.FinalPosition + Owner.FleetOffset, 15000f));
                if (doreturn)
                    return;
            }

            // we have a combat target, but we're not in combat state?
            if (Target != null && State != AIState.Combat)
            {
                Owner.InCombatTimer = 15f;
                if (!HasPriorityOrder)
                    EnterCombat();
                else if ((Owner.IsPlatform || Owner.shipData.Role == ShipData.RoleName.station)
                         && OrderQueue.PeekFirst?.Plan != Plan.DoCombat)
                    EnterCombat();
                else if (CombatState != CombatState.HoldPosition && !OrderQueue.NotEmpty)
                    EnterCombat();
            }
        }

        void EnterCombat()
        {
            AddShipGoal(Plan.DoCombat, AIState.Combat, pushToFront: true);
        }

        public float GetSensorRadius() => GetSensorRadius(out Ship _);

        public float GetSensorRadius(out Ship sensorShip)
        {
            if (!UseSensorsForTargets) // this is obsolete and not needed anymore. 
            {
                sensorShip = Owner.Mothership ?? Owner;
                return 30000f;
            }

            if (Owner.Mothership != null)
            {
                // get the motherships sensor status. 
                float motherRange = Owner.Mothership.AI.GetSensorRadius(out sensorShip);

                // in radius of the motherships sensors then use that.
                if (Owner.Center.InRadius(sensorShip.Center, motherRange - Owner.SensorRange))
                    return motherRange;
            }
            sensorShip = Owner;
            float sensorRange = Owner.SensorRange + (Owner.IsInFriendlyProjectorRange ? 10000 : 0);
            return sensorRange;
            
            
        }

        bool DoNotEnterCombat
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
    }
}