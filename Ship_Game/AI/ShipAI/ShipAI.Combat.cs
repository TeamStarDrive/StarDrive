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
        
        readonly Array<ShipWeight> ScannedNearby      = new Array<ShipWeight>();
        readonly Array<Ship> ScannedTargets           = new Array<Ship>();
        readonly Array<Ship> ScannedFriendlies        = new Array<Ship>();
        readonly Array<Projectile> ScannedProjectiles = new Array<Projectile>();
        Ship ScannedTarget = null;
        
        bool ScanComplete = true;
        bool ScanDataProcessed = true;
        bool ScanTargetUpdated = true;
        public static FleetDataNode DefaultFleetNode = new FleetDataNode();

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
                if (Owner.Mothership != null)
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
                if (Owner.Mothership != null)
                    ScannedProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
                for (int i = 0; i < projectiles.Length; i++)
                {
                    GameplayObject go = projectiles[i];
                    if (go == null)
                        continue;
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

        public struct  TargetParameterTotals
        {
            // target characteristics 
            public float Armor, Shield, DPS, Size, Speed, Health;

            int count;

            public void AddTargetValue(Ship ship)
            {
                Armor  += ship.armor_max;
                Shield += ship.shield_max;
                DPS    += ship.GetDPS();
                Size   += ship.SurfaceArea;
                Speed  += ship.MaxSTLSpeed;
                Health += ship.HealthPercent; 
                count++;

            }

            public TargetParameterTotals GetAveragedValues()
            {
                var returnValue = new TargetParameterTotals
                {
                    Armor  = this.Armor / count,
                    Shield = this.Shield / count,
                    DPS    = this.DPS / count,
                    Size   = this.Size / count,
                    Speed  = this.Speed / count,
                    Health = this.Health / count
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
            ScannedNearby.Clear();
            var targetPrefs = new TargetParameterTotals();
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

            if (thisSystem != null)
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
            GameplayObject[] scannedShips = UniverseScreen.Spatial.FindNearby(GameObjectType.Ship,
                                                                    Owner, scanRadius, maxResults:128);
 
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
                if (empire == Owner.loyalty)
                {
                    ScannedFriendlies.Add(nearbyShip);
                    continue;
                }

                bool canAttack = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                if (canAttack)
                {
                    BadGuysNear = true;
                    
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

                        var sw = new ShipWeight(nearbyShip, 0);
                        ScannedNearby.Add(sw);

                        targetPrefs.AddTargetValue(nearbyShip);
                        
                        ScannedNearby[ScannedNearby.Count - 1] = sw;
                    }
                }
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

            // non combat ships dont process combat weight concepts. 
            if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange.AlmostZero() || ScannedNearby.Count == 0)
                return Target;

            SetTargetWeights(targetPrefs);

            if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
            {
                var sw = new ShipWeight(EscortTarget.AI.Target, 1f);
                ScannedNearby.Add(sw);
            }

            // limiting combat targets to the arbitrary -100 weight. Poor explained here. 
            ShipWeight[] SortedTargets = ScannedNearby.Filter(weight => weight.Weight > -100)
                .OrderByDescending(weight => weight.Weight).ToArray();

            ScannedTargets.AddRange(SortedTargets.Select(ship => ship.Ship));

            // check target validity
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
            if (SortedTargets.Length > 0)
                targetShip = SortedTargets[0].Ship;

            if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars)
                return targetShip;
            return null;
        }

        void SetTargetWeights(TargetParameterTotals targetPrefs)
        {
            targetPrefs     = targetPrefs.GetAveragedValues();

            for (int i = ScannedNearby.Count - 1; i >= 0; i--)
            {
                ShipWeight copyWeight = ScannedNearby[i]; //Remember we have a copy.

                if (!Owner.loyalty.IsEmpireAttackable(copyWeight.Ship.loyalty))
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

                    // if outside ordersRatio drop a heavy weight.
                    bool orderRatio = fleetPos.InRadius(copyWeight.Ship.Center, FleetNode.OrdersRadius);
                    if (orderRatio)
                    {
                        copyWeight += FleetNode.ApplyFleetWeight(Owner.fleet.Ships, copyWeight.Ship, targetPrefs) / 2;
                    }
                    else
                        copyWeight.SetWeight(float.MinValue);
                }
                ////ShipWeight is a struct so we are working with a copy. Need to overwrite existing value. 

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