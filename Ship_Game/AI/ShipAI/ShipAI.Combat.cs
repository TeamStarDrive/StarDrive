using Microsoft.Xna.Framework;
using Newtonsoft.Json;
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
        
        //readonly Array<ShipWeight> ScannedNearby      = new Array<ShipWeight>(); Checking Alternative Logic
        readonly Array<Ship> ScannedTargets           = new Array<Ship>();
        readonly Array<Ship> ScannedFriendlies        = new Array<Ship>();
        readonly Array<Projectile> ScannedProjectiles = new Array<Projectile>();
        // public Vector2 FriendliesSwarmCenter { get; private set; } Checking Alternative Logic
        // public Vector2 ProjectileSwarmCenter { get; private set; } Checking Alternative Logic
        Ship ScannedTarget = null;
        
        bool ScanComplete = true;
        bool ScanDataProcessed = true;
        bool ScanTargetUpdated = true;

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
                    if (target == null || !target.Active || target.Health <= 0.0f || target.dying || target.TroopsAreBoardingShip)
                        PotentialTargets.RemoveAtSwapLast(x);
                }

                for (int x = TrackProjectiles.Count - 1; x >= 0; x--)
                {
                    var target = TrackProjectiles[x];
                    if (target == null || !target.Active || target.Health <= 0.0f)
                        TrackProjectiles.RemoveAtSwapLast(x);
                }
            }

            if (Target?.TroopsAreBoardingShip == true)
                return;

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
                if (Owner.IsHangarShip)
                {
                    Owner.Mothership.AI.TrackProjectiles.ForEach(p=> TrackProjectiles.AddUnique(p));
                }
                return;
            }

            // find hostile projectiles
            GameplayObject[] projectiles = UniverseScreen.Spatial.FindNearby(GameObjectType.Proj,
                                    Owner, Owner.WeaponsMaxRange, maxResults:64, excludeLoyalty:Owner.loyalty);
            {
                ScannedProjectiles.Clear();
                if (Owner.IsHangarShip)
                    ScannedProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
                for (int i = 0; i < projectiles.Length; i++)
                {
                    GameplayObject go = projectiles[i];
                    if (go == null)
                        continue;
                    var missile = (Projectile) go;
                    if (missile.Weapon.Tag_Intercept && Owner.loyalty.IsEmpireAttackable(missile.Loyalty))
                    {
                        ScannedProjectiles.AddUniqueRef(missile);
                        //ProjectileSwarmCenter += missile.Center; Checking Alternative Logic
                    }
                }
            }
            /* Checking Alternative Logic
            if (ScannedProjectiles.Count > 0)
                ProjectileSwarmCenter /= ScannedProjectiles.Count; */
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

        public struct  TargetParameterTotals
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
            ScannedFriendlies.Clear();
            ScannedTargets.Clear();
            // ScannedNearby.Clear(); Checking Alternative Logic
            //FriendliesSwarmCenter = Owner.Center; Checking Alternative Logic
            // var targetPrefs = new TargetParameterTotals(); Checking Alternative Logic
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

            SolarSystem thisSystem = Owner.System;

            if (thisSystem?.OwnerList.Count > 0)
            {
                for (int i = 0; i < thisSystem.PlanetList.Count; i++)
                {
                    Planet p = thisSystem.PlanetList[i];
                    BadGuysNear = BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) && Owner.Center.InRadius(p.Center, radius);
                }
            }

            // get enemies and friends in close proximity
            // radius hack needs investigation.
            // i believe this is an orbital no control systems compensator. But it should not be handled here. 
            float scanRadius = radius + (radius < 0.01f ? 10000 : 0);
            int maxResults   = Owner.System?.ShipList.Count.LowerBound(128) ?? 128;
            GameplayObject[] scannedShips = UniverseScreen.Spatial.FindNearby(GameObjectType.Ship,
                                                                    Owner, scanRadius, maxResults);

            for (int x = 0; x < scannedShips.Length; x++)
            {
                var go = scannedShips[x];
                var nearbyShip = (Ship)go;

                nearbyShip.KnownByEmpires.SetSeen(Owner.loyalty);

                // do not process dead and dying any further. Let them be visibilbe but not show up in any target lists. 
                if (!nearbyShip.Active || nearbyShip.dying)                 
                    continue;
                
                // this should be expanded to include allied ships. 
                Empire empire = nearbyShip.loyalty;
                if (empire == Owner.loyalty && !nearbyShip.IsHangarShip)
                {
                    ScannedFriendlies.Add(nearbyShip);
                    //FriendliesSwarmCenter += nearbyShip.Center; Checking Alternative Logic
                    continue;
                }

                bool canAttack = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                if (canAttack)
                {
                    BadGuysNear = true;
                    ScannedTargets.Add(nearbyShip);
                    /* Checking Alternative Logic
                    if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange.AlmostZero())
                    {
                        ScannedTargets.Add(nearbyShip);
                    }
                    
                    else
                    {
                        // this radius <1 hack needs investigation. 
                        // technically this should never be true however the above radius hack may be causing ships 
                        // with no control to have a scan radius and so its possible this would 0 and still scanning.
                        if (radius < 1)
                            continue;
                        
                        targetPrefs.AddTargetValue(nearbyShip);
                        ScannedNearby.Add(new ShipWeight(nearbyShip, 0));  
                    }*/
                }
            }

            /*
            if (ScannedFriendlies.Count > 0)
            {
                FriendliesSwarmCenter = Vector2.Divide(FriendliesSwarmCenter, ScannedFriendlies.Count + 1);
            }*/

            if (Target is Ship target)
            {
                if (target.loyalty == Owner.loyalty)
                {
                    ScannedTarget = null;
                    HasPriorityTarget = false;
                }
                else if (!Intercepting && target.IsInWarp)
                {
                    Target = null;
                    ScannedTarget = null;
                    if (!HasPriorityOrder && Owner.loyalty != Empire.Universe.player)
                        State = AIState.AwaitingOrders;
                    return null;
                }
            }

            // non combat ships dont process combat weight concepts. 
            if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange.AlmostZero() /* ||ScannedNearby.Count == 0 Checking Alternative Logic */ )
                return Target;

            /*
            if (EscortTarget?.Active == true && IsTargetValid(EscortTarget) && !scannedShips.Contains(EscortTarget))
            {
                var sw = new ShipWeight(EscortTarget.AI.Target, 0);
                ScannedNearby.Add(sw); Checking Alternative Logic
                targetPrefs.AddTargetValue(EscortTarget);
            }*/ // Checking Alternative Logic
            // SetTargetWeights(targetPrefs); Checking Alternative Logic

            // ShipWeight[] sortedTargets = ScannedNearby.SortedDescending(weight => weight.Weight); Checking Alternative Logic

            // ScannedTargets.AddRange(sortedTargets.Select(ship => ship.Ship)); Checking Alternative Logic

            // check target validity
            if (Target != null && !Target.Active)
            {
                Target = null;
                ScannedTarget = null;
                HasPriorityTarget = false;
            }
            else if (Target?.Active == true && HasPriorityTarget && Target is Ship ship)
            {
                if (Owner.loyalty.IsEmpireAttackable(ship.loyalty, ship))
                    BadGuysNear = true;

                return Target;
            }

            /* Checking Alternative Logic
            if (sortedTargets.Length > 0 && (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars))
            {
                if (sortedTargets.FindFirstValid(sortedTargets.Length, w=> w.Weight > -10000f, out _, out var shipWeight))
                    return shipWeight.Ship;
            }
            return null; */
            return TryGetTarget(out Ship t) ? t : null; // This is the alternative logic
        }

        bool TryGetTarget(out Ship target)
        {
            target = null;
            if (Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                return false;

            if (ScannedTargets.Count > 0)
            {
                target = ScannedTargets.FindMax(GetTargetPriority);
                return true;
            }

            return false;

            /*  FB - Commented my own logic as well to try simple targeting not dependent of command ship targeting
            bool isCommandShip        = Owner.fleet?.CommandShip == Owner;
            bool commandShipHasTarget = Owner.fleet?.CommandShip?.AI.Target != null;
            Ship commandShipTarget    = commandShipHasTarget ? Owner.fleet.CommandShip.AI.Target : null;

            // Get the Target of the Command ship if the owner is not a hangar ship or a small craft
            if (commandShipHasTarget && Owner.Mothership == null && Owner.shipData.HullRole >= ShipData.RoleName.frigate)
            {
                target = commandShipTarget;
                return true;
            }

            if (ScannedTargets.Count > 0 && (isCommandShip || !commandShipHasTarget || Owner.Mothership != null))
            {

                // target = ScannedTargets.FindMax(GetTargetPriority);
                return true;
            }

            return false;*/
        }

        float GetTargetPriority(Ship ship)
        {
            if (ship.IsInWarp)
                return 0;

            float minimumDistance = Owner.Radius * 2;
            float value           = ship.TotalDps;
            value += ship.Carrier.EstimateFightersDps;
            value += ship.TroopCapacity * 100;
            value += ship.HasBombs && ship.AI.OrbitTarget?.Owner == Owner.loyalty 
                       ? ship.BombBays.Count * 50 
                       : ship.BombBays.Count * 10;

            float angleMod = Owner.AngleDifferenceToPosition(ship.Position).LowerBound(0.25f)
                             / Owner.RotationRadiansPerSecond.LowerBound(0.25f);

            float distance = Owner.Center.Distance(ship.Center).LowerBound(minimumDistance);
            value /= angleMod;
            return value / distance;
        }

        /* Checking Alternative Logic
        void SetTargetWeights(TargetParameterTotals targetPrefs)
        {
            targetPrefs     = targetPrefs.GetAveragedValues();

            for (int i = ScannedNearby.Count - 1; i >= 0; i--)
            {
                ShipWeight copyWeight = ScannedNearby[i]; //Remember we have a copy.

                if (!Owner.loyalty.IsEmpireAttackable(copyWeight.Ship?.loyalty, copyWeight.Ship))
                {
                    copyWeight.Weight = float.MinValue;
                    ScannedNearby[i] = copyWeight;
                    continue;
                }


                // Initially we are setting up the ships own targeting pref. After that we setup fleet rules for targeting. 
                // the way i am planing to do this is very ratios. 
                // adding up these ratios and using that as a weight. 
                // one step further would be to normalize the weights by averaging the ratios. 


                // standard ship targeting:
                // this should cover individual targeting needs. 

                copyWeight = CombatAI.ShipCommandTargeting(copyWeight, targetPrefs);

                if (Owner.fleet != null && FleetNode != null)
                {
                    Vector2 fleetPos = Owner.fleet.AveragePosition() + FleetNode.FleetOffset;

                    // ship is within fleet position and orders radius.
                    bool orderRatio = fleetPos.InRadius(copyWeight.Ship.Center, FleetNode.OrdersRadius);
                    if (orderRatio)
                    {
                        if (copyWeight.Ship.InRadius(fleetPos, FleetNode.OrdersRadius))
                            copyWeight += FleetNode.ApplyFleetWeight(Owner.fleet.Ships, copyWeight.Ship, targetPrefs) / 2;
                        else
                            copyWeight.SetWeight(int.MinValue);
                    }
                        
                }

                ////ShipWeight is a struct so we are working with a copy. Need to overwrite existing value.
                ScannedNearby[i] = copyWeight;
            }
        }
        */

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
                if (Owner.IsHangarShip)
                    ScannedTarget = ScanForCombatTargets(sensorShip, radius) ?? Owner.Mothership.AI.Target;
                else
                    ScannedTarget = ScanForCombatTargets(sensorShip, radius);
            }
            else
            {
                if (Owner.IsHangarShip)
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

        public void DropBombsAtGoal(ShipGoal goal, float radius)
        {
            if (!Owner.Center.InRadius(goal.TargetPlanet.Center, radius)) 
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