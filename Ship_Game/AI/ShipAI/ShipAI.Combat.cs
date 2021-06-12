using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;
using Ship_Game.Spatial;


namespace Ship_Game.AI
{

    public sealed partial class ShipAI
    {
        public CombatState CombatState = CombatState.AttackRuns;
        public CombatAI CombatAI = new CombatAI();

        public Ship[] PotentialTargets = Empty<Ship>.Array;
        public Projectile[] TrackProjectiles = Empty<Projectile>.Array;
        public Ship[] FriendliesNearby = Empty<Ship>.Array;

        public Ship EscortTarget;
        float ScanForThreatTimer;
        public Planet ExterminationTarget;
        public bool Intercepting { get; private set; }
        public Guid TargetGuid;
        public bool IgnoreCombat;
        public bool BadGuysNear;
        public bool CanTrackProjectiles { get; set; }
        public Guid EscortTargetGuid;
        public Ship Target;
        public Array<Ship> TargetQueue = new Array<Ship>();
        public bool HasPriorityTarget;
        float TriggerDelay;
        
        Array<Ship> ScannedTargets = new Array<Ship>();
        Array<Ship> ScannedFriendlies = new Array<Ship>();
        Array<Projectile> ScannedProjectiles = new Array<Projectile>();

        void InitializeTargeting()
        {
            CanTrackProjectiles = DoesShipHavePointDefense(Owner);
        }

        // Allow controlling the Trigger delay for ships
        // This can be used to stop ships from Firing for X seconds
        public void SetCombatTriggerDelay(float triggerDelay)
        {
            TriggerDelay = triggerDelay;
        }
        
        static bool DoesShipHavePointDefense(Ship ship)
        {
            for (int i = 0; i < ship.Weapons.Count; ++i)
            {
                Weapon purge = ship.Weapons[i];
                if (purge.Tag_PD || purge.TruePD)
                    return true;
            }
            return false;
        }

        public void CancelIntercept()
        {
            HasPriorityTarget = false;
            Intercepting = false;
        }

        bool FireOnTarget()
        {
            // base reasons not to fire. @TODO actions decided by loyalty like should be the same in all areas. 
            if (!Owner.hasCommand || Owner.engineState == Ship.MoveState.Warp
                || Owner.EMPdisabled || Owner.Weapons.Count == 0 || !BadGuysNear)
                return false;

            if (Target?.TroopsAreBoardingShip == true)
                return false;

            // main Target has died, reset it
            if (Target != null && !IsTargetActive(Target))
            {
                Target = null;
            }

            int count = Owner.Weapons.Count;
            Weapon[] weapons = Owner.Weapons.GetInternalArrayItems();

            bool didFireAtAny = false;
            for (int i = 0; i < count; ++i)
            {
                var weapon = weapons[i];
                bool didFire = weapon.UpdateAndFireAtTarget(Target, TrackProjectiles, PotentialTargets);
                didFireAtAny |= didFire;
                if (didFire && weapon.FireTarget.ParentIsThis(Target))
                {
                    float weaponFireTime;
                    if      (weapon.isBeam)            weaponFireTime = weapon.BeamDuration;
                    else if (weapon.SalvoDuration > 0) weaponFireTime = weapon.SalvoDuration;
                    else                               weaponFireTime = (1f - weapon.CooldownTimer);
                    FireOnMainTargetTime = weaponFireTime.LowerBound(FireOnMainTargetTime);
                }
            }
            return didFireAtAny;
        }

        void UpdateTrackedShips(Ship sensorShip, float radius)
        {
            // get enemies and friends in close proximity
            // radius hack needs investigation.
            // i believe this is an orbital no control systems compensator. But it should not be handled here. 
            float scanRadius = radius + (radius < 0.01f ? 10000 : 0);
            int maxResults = sensorShip.System?.ShipList.Count.LowerBound(128) ?? 128;
            GameplayObject[] scannedShips = UniverseScreen.Spatial.FindNearby(
                        GameObjectType.Ship, sensorShip, scanRadius, maxResults);

            for (int i = 0; i < scannedShips.Length; ++i)
            {
                // Do not process dead and dying ships
                var nearbyShip = (Ship)scannedShips[i];
                if (!nearbyShip.Active || nearbyShip.dying)
                    continue;

                // update two-way visibility,
                // nearbyShip is known by our Empire - this information is used by our Empire later
                // the nearbyShip itself does not care about it
                nearbyShip.KnownByEmpires.SetSeen(sensorShip.loyalty);
                // and our ship has seen nearbyShip
                sensorShip.HasSeenEmpires.SetSeen(nearbyShip.loyalty);
                
                // this should be expanded to include allied ships. 
                Empire empire = nearbyShip.loyalty;
                if (empire == Owner.loyalty && !nearbyShip.IsHangarShip)
                {
                    ScannedFriendlies.Add(nearbyShip);
                }
                else  if (Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip))
                {
                    BadGuysNear = true;
                    ScannedTargets.Add(nearbyShip);
                }
            }

            PotentialTargets = ScannedTargets.ToArray();
            FriendliesNearby = ScannedFriendlies.ToArray();
            ScannedTargets.Clear();
            ScannedFriendlies.Clear();
        }

        void UpdateTrackedProjectiles(Ship sensorShip)
        {
            if (Owner.IsHangarShip)
                ScannedProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);

            if (CanTrackProjectiles && Owner.TrackingPower > 0)
            {
                var opt = new SearchOptions(sensorShip.Center, sensorShip.WeaponsMaxRange, GameObjectType.Proj)
                {
                    MaxResults = 32,
                    SortByDistance = true, // only care about closest results
                    ExcludeLoyalty = Owner.loyalty,
                    FilterFunction = (go) =>
                    {
                        var missile = (Projectile)go;
                        bool canIntercept = missile.Weapon.Tag_Intercept && Owner.loyalty.IsEmpireAttackable(missile.Loyalty);
                        if (canIntercept)
                        {
                            // ignore duplicate projectiles from Mothership, since they were already added
                            return !Owner.IsHangarShip || !Owner.Mothership.AI.TrackProjectiles.ContainsRef(missile);
                        }
                        return false;
                    }
                };

                GameplayObject[] missiles = UniverseScreen.Spatial.FindNearby(ref opt);
                for (int i = 0; i < missiles.Length; ++i)
                    ScannedProjectiles.Add((Projectile)missiles[i]);
            }

            // Always make a full copy, this is for thread safety
            // Once the new array is assigned, its elements must not be modified
            var newProjectiles = ScannedProjectiles.ToArray();
            ScannedProjectiles.Clear();

            // we always need to sort by distance, because targets inherited from Mothership
            // are different distance from hangar fighter
            newProjectiles.SortByDistance(Owner.Center);

            // atomic assign, the Tracked projectiles array is now FINAL until next update
            TrackProjectiles = newProjectiles;
        }

        public struct TargetParameterTotals
        {
            // target characteristics 
            public float Armor, Shield, DPS, Size, Speed, Health, Largest, MostFirePower, MaxRange, MediumRange, ShortRange, SensorRange, X, Y;
            public Vector2 Center;
            public Vector2 DPSCenter;
            public float MaxSensorRange;
            public float Defense => Armor + Shield;
            public bool ScreenShip(Ship ship) => ship.armor_max + ship.shield_max >= Defense;
            public bool LongRange(Ship ship) => ship.WeaponsMaxRange >= MaxRange;
            public bool Interceptor(Ship ship) => ship.MaxSTLSpeed >= Speed;
            public bool FirePower(Ship ship) => ship.TotalDps >= DPS;
            public bool Big(Ship ship) => ship.SurfaceArea >= Size;

            int Count;

            public void AddTargetValue(Ship ship)
            {
                if (ship.engineState == Ship.MoveState.Warp) return;
                Count++;
                Largest = Math.Max(Largest, ship.SurfaceArea);
                MostFirePower = Math.Max(MostFirePower, ship.TotalDps);
                
                Armor       += ship.armor_max;
                Shield      += ship.shield_max;
                DPS         += ship.TotalDps;
                Size        += ship.SurfaceArea;
                Speed       += ship.MaxSTLSpeed;
                Health      += ship.HealthPercent;
                MaxRange    += ship.WeaponsMaxRange;
                MediumRange += ship.WeaponsAvgRange;
                ShortRange  += ship.WeaponsMinRange;
                SensorRange += ship.SensorRange;

                MaxSensorRange = Math.Max(MaxSensorRange, ship.SensorRange);
                Center        += ship.Center;
                DPSCenter     += ship.Center * ship.TotalDps;
            }

            public TargetParameterTotals GetAveragedValues()
            {
                if (Count == 0) return new TargetParameterTotals();
                var returnValue = new TargetParameterTotals()
                {
                    DPSCenter   = DPS > 0 ? DPSCenter / DPS : Center / Count,
                    Center      = Center / Count,
                    Armor       = this.Armor / Count,
                    Shield      = this.Shield / Count,
                    DPS         = this.DPS / Count,
                    Size        = this.Size / Count,
                    Speed       = this.Speed / Count,
                    Health      = this.Health / Count,
                    MaxRange    = this.MaxRange / Count,
                    MediumRange = this.MediumRange / Count,
                    ShortRange  = this.ShortRange / Count,
                    SensorRange = this.SensorRange / Count,
                    Largest     = Largest,

                    MaxSensorRange = MaxSensorRange
                };
                return returnValue;
            }
        }

        public Ship ScanForCombatTargets(Ship sensorShip, float radius)
        {
            Owner.KnownByEmpires.SetSeen(Owner.loyalty);
            BadGuysNear = false;
            UpdateTrackedProjectiles(sensorShip);
            UpdateTrackedShips(sensorShip, radius);

            if (HasPriorityTarget && Target == null)
            {
                HasPriorityTarget = false;

                // if we still have items in the priority target queue, just pop them
                if (TargetQueue.TryPopFirst(out Ship firstPriority))
                {
                    HasPriorityTarget = true;
                    return firstPriority;
                }
            }

            SolarSystem thisSystem = Owner.System;
            if (thisSystem?.OwnerList.Count > 0)
            {
                for (int i = 0; i < thisSystem.PlanetList.Count; i++)
                {
                    Planet p = thisSystem.PlanetList[i];
                    if (!BadGuysNear)
                        BadGuysNear = Owner.loyalty.IsEmpireAttackable(p.Owner)
                                   && Owner.Center.InRadius(p.Center, radius);
                }
            }

            if (Target != null)
            {
                if (Target.loyalty == Owner.loyalty)
                {
                    HasPriorityTarget = false;
                }
                else if (!Intercepting && Target.IsInWarp)
                {
                    Target = null;
                    if (!HasPriorityOrder && Owner.loyalty != Empire.Universe.player)
                        State = AIState.AwaitingOrders;
                    return null;
                }
            }

            // non combat ships dont process combat weight concepts. 
            if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange.AlmostZero())
                return Target;

            // check target validity
            bool isTargetActive = IsTargetActive(Target);
            if (!isTargetActive)
            {
                Target = null;
                HasPriorityTarget = false;
            }
            else if (HasPriorityTarget)
            {
                if (Owner.loyalty.IsEmpireAttackable(Target.loyalty, Target))
                    BadGuysNear = true;
                return Target;
            }

            return PickHighestPriorityTarget(); // This is the alternative logic
        }

        static bool IsTargetActive(Ship target)
        {
            return target != null && target.Active && !target.dying;
        }

        Ship PickHighestPriorityTarget()
        {
            if (Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                return null;

            if (PotentialTargets.Length > 0)
            {
                return PotentialTargets.FindMax(GetTargetPriority);
            }
            return null;
        }

        float GetTargetPriority(Ship ship)
        {
            if (ship.IsInWarp)
                return 0;

            float minimumDistance = Owner.Radius;
            float value           = ship.TotalDps;
            value += ship.Carrier.EstimateFightersDps;
            value += ship.TroopCapacity * 100;
            value += ship.HasBombs && ship.AI.OrbitTarget?.Owner == Owner.loyalty 
                       ? ship.BombBays.Count * 50 
                       : ship.BombBays.Count * 10;

            float angleMod = Owner.AngleDifferenceToPosition(ship.Position).LowerBound(0.5f)
                             / Owner.RotationRadiansPerSecond.LowerBound(0.5f);

            float distance = Owner.Center.Distance(ship.Center).LowerBound(minimumDistance);
            value /= angleMod;
            return value / distance;
        }

        
        // Runs an immediate sensor scan
        public void SensorScan()
        {
            ScanForThreatTimer = SensorScanIntervalSeconds;

            float radius = GetSensorRadius(out Ship sensorShip);
            if (Owner.IsSubspaceProjector)
            {
                // TODO: Why does SSP scan for nearby ships?
                ScanForCombatTargets(sensorShip, radius);
                return;
            }

            Ship scannedTarget = ScanForCombatTargets(sensorShip, radius);

            if (Owner.fleet == null && scannedTarget == null && Owner.IsHangarShip)
            {
                scannedTarget = Owner.Mothership.AI.Target;
            }

            // automatically choose `Target` if ship does not have Priority
            // or if current target already died
            if (!HasPriorityOrder && (!HasPriorityTarget || !IsTargetActive(Target)))
                Target = scannedTarget;

            if (State != AIState.Resupply && !DoNotEnterCombat)
            {
                if (Owner.fleet != null && State == AIState.FormationWarp &&
                    HasPriorityOrder && !HasPriorityTarget)
                {
                    bool finalApproach = Owner.Center.InRadius(Owner.fleet.FinalPosition + Owner.FleetOffset, 15000f);
                    if (!finalApproach)
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
        }

        void EnterCombat()
        {
            AddShipGoal(Plan.DoCombat, AIState.Combat, pushToFront: true);
        }

        public float GetSensorRadius() => GetSensorRadius(out Ship _);

        public float GetSensorRadius(out Ship sensorShip)
        {
            if (Owner.IsHangarShip)
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

        public void DropBombsAtGoal(ShipGoal goal, bool inOrbit)
        {
            if (!inOrbit) 
                return;

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