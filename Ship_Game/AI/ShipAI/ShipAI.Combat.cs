using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using Ship_Game.Empires;
using Ship_Game.Spatial;


namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public CombatState CombatState = CombatState.AttackRuns;
        public CombatAI CombatAI;

        public Ship[] PotentialTargets = Empty<Ship>.Array;
        public Ship[] FriendliesNearby = Empty<Ship>.Array;
        public Projectile[] TrackProjectiles = Empty<Projectile>.Array;

        float TargetSelectTimer;
        float ProjectileScanTimer;
        float EnemyScanTimer;
        float FriendScanTimer;

        public Ship EscortTarget;
        public Planet ExterminationTarget;

        public Ship Target;
        public Array<Ship> TargetQueue = new Array<Ship>();
        float TriggerDelay;
        Array<Ship> ScannedTargets = new Array<Ship>();
        Array<Ship> ScannedFriendlies = new Array<Ship>();
        Array<Projectile> ScannedProjectiles = new Array<Projectile>();

        void InitializeTargeting()
        {
            TargetProjectiles = DoesShipHavePointDefense(Owner);
            IsNonCombatant = Owner.IsSubspaceProjector
                          || Owner.IsFreighter
                          || Owner.IsConstructor
                          || Owner.DesignRole == RoleName.supply 
                          || Owner.DesignRole == RoleName.scout
                          || Owner.DesignRole == RoleName.troop;
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

            bool didFireAtMainTarget = false;
            bool didFireAtAny = false;
            for (int i = 0; i < count; ++i)
            {
                var weapon = weapons[i];
                bool didFire = weapon.UpdateAndFireAtTarget(Target, TrackProjectiles, PotentialTargets);
                didFireAtAny |= didFire;
                if (didFire)
                {
                    GameplayObject target = weapon.FireTarget;
                    Ship parent = (target as Ship) ?? (target as ShipModule)?.GetParent();
                    if (parent != null && parent == Target)
                        didFireAtMainTarget = true;
                }
            }

            if (didFireAtAny)
                IsFiringAtMainTarget = didFireAtMainTarget;
            return didFireAtAny;
        }

        // TODO: This can be optimized for friendly nearby ships to share scan data
        public void ScanForFriendlies(Ship sensorShip, float sensorRadius)
        {
            if (sensorRadius <= 0f) // sensors can be disabled, we use radius 0 to signal this
            {
                FriendliesNearby = Empty<Ship>.Array;
                return;
            }

            ++Empire.Universe.Objects.Scans;

            var findFriends = new SearchOptions(sensorShip.Position, sensorRadius, GameObjectType.Ship)
            {
                MaxResults = 32,
                Exclude = sensorShip,
                OnlyLoyalty = sensorShip.loyalty,
            };

            GameplayObject[] friends = UniverseScreen.Spatial.FindNearby(ref findFriends);
            
            for (int i = 0; i < friends.Length; ++i)
            {
                var friend = (Ship)friends[i];
                if (friend.Active && !friend.dying && !friend.IsHangarShip)
                    ScannedFriendlies.Add(friend);
            }

            FriendliesNearby = ScannedFriendlies.ToArray();
            ScannedFriendlies.Clear();
        }

        public void ScanForEnemies(Ship sensorShip, float sensorRadius)
        {
            if (sensorRadius <= 0f) // sensors can be disabled, we use radius 0 to signal this
            {
                PotentialTargets = Empty<Ship>.Array;
                return;
            }

            ++Empire.Universe.Objects.Scans;
            BadGuysNear = false;

            Empire us = sensorShip.loyalty;
            var findEnemies = new SearchOptions(sensorShip.Position, sensorRadius, GameObjectType.Ship)
            {
                MaxResults = 64,
                Exclude = sensorShip,
                ExcludeLoyalty = us,
            };

            GameplayObject[] enemies = UniverseScreen.Spatial.FindNearby(ref findEnemies);

            for (int i = 0; i < enemies.Length; ++i)
            {
                var enemy = (Ship)enemies[i];
                if (!enemy.Active || enemy.dying)
                    continue;

                // update two-way visibility,
                // enemy is known by our Empire - this information is used by our Empire later
                // the enemy itself does not care about it
                enemy.KnownByEmpires.SetSeen(us);
                // and our ship has seen nearbyShip
                sensorShip.HasSeenEmpires.SetSeen(enemy.loyalty);

                if (us.IsEmpireScannedAsEnemy(enemy.loyalty, enemy))
                {
                    BadGuysNear = true;
                    ScannedTargets.Add(enemy);
                }
            }

            PotentialTargets = ScannedTargets.ToArray();
            ScannedTargets.Clear();
        }

        void ScanForProjectiles(Ship sensorShip, float sensorRadius)
        {
            // sensors can be disabled, we use radius 0 to signal this
            if (sensorRadius <= 0f || sensorShip.WeaponsMaxRange <= 0f)
            {
                TrackProjectiles = Empty<Projectile>.Array;
                return;
            }

            ++Empire.Universe.Objects.Scans;

            // as optimization we use WeaponsMaxRange instead
            var opt = new SearchOptions(sensorShip.Position, sensorShip.WeaponsMaxRange, GameObjectType.Proj)
            {
                MaxResults = 32,
                SortByDistance = true, // only care about closest results
                ExcludeLoyalty = Owner.loyalty,
                FilterFunction = (go) =>
                {
                    var missile = (Projectile)go;
                    // Note: this ensures we don't accidentally target Allied projectiles
                    // TODO: But this check is also done again in Weapon.cs target selection
                    // TODO: use the intercept tag and loyalty tag in Qtree
                    bool canIntercept = missile.Weapon.Tag_Intercept && Owner.loyalty.IsEmpireAttackable(missile.Loyalty);
                    return canIntercept;
                }
            };

            GameplayObject[] missiles = UniverseScreen.Spatial.FindNearby(ref opt);
            for (int i = 0; i < missiles.Length; ++i)
                ScannedProjectiles.Add((Projectile)missiles[i]);

            // Always make a full copy, this is for thread safety
            // Once the new array is assigned, its elements must not be modified
            TrackProjectiles = ScannedProjectiles.ToArray();
            ScannedProjectiles.Clear();
        }

        void FetchTargetsFromMothership(Ship mothership, float sensorRadius)
        {
            if (sensorRadius > 0f)
            {
                TrackProjectiles = mothership.AI.TrackProjectiles;
            }
            else
            {
                TrackProjectiles = Empty<Projectile>.Array;
            }
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
                Center        += ship.Position;
                DPSCenter     += ship.Position * ship.TotalDps;
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

        Ship SelectCombatTarget(float radius)
        {
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

            // TODO: Ships without PD should Evade the planet
            // TODO: Ships with Bombs or PD should Orbit the planet
            SolarSystem thisSystem = Owner.System;
            if (thisSystem?.OwnerList.Count > 0)
            {
                for (int i = 0; i < thisSystem.PlanetList.Count; i++)
                {
                    Planet p = thisSystem.PlanetList[i];
                    if (!BadGuysNear)
                        BadGuysNear = Owner.loyalty.IsEmpireAttackable(p.Owner)
                                   && Owner.Position.InRadius(p.Center, radius);
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
                    if (!HasPriorityOrder && !Owner.loyalty.isPlayer)
                        State = AIState.AwaitingOrders;
                    return null;
                }
            }

            // non combat ships dont process combat weight concepts. 
            if (Owner.IsSubspaceProjector || IgnoreCombat || Owner.WeaponsMaxRange == 0f)
                return Target;

            // check target validity
            bool isTargetActive = IsTargetActive(Target);
            if (!isTargetActive)
            {
                Target = null;
                HasPriorityTarget = false;
            }
            else if (HasPriorityTarget && Target != null)
            {
                BadGuysNear |= Owner.loyalty.IsEmpireAttackable(Target.loyalty, Target);
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

            float minimumDistance = Owner.Radius + ship.Radius;
            float value = ship.TotalDps + 100;
            value *= 1 + ship.Carrier.AllFighterHangars.Length/2;
            value *= 1 + ship.TroopCapacity/2;
            value *= 1 + (ship.HasBombs && ship.AI.OrbitTarget?.Owner == Owner.loyalty
                        ? ship.BombBays.Count
                        : ship.BombBays.Count/2);

            float angleMod = Owner.AngleDifferenceToPosition(ship.Position).Clamped(0.5f, 3);
            if (Owner.AI.EscortTarget != null && ship.AI.Target == Owner.AI.EscortTarget)
            {
                value   *= 10;
                angleMod = 1;
            }


            float distance = Owner.Position.Distance(ship.Position).LowerBound(minimumDistance);
            if (ship.AI.Target == Owner && distance < ship.DesiredCombatRange)
                value *= 2;

            if (ship.Resupplying)
                value /= 10;

            float sizeMod  = ((float)ship.SurfaceArea / Owner.SurfaceArea).UpperBound(1);
            value /= angleMod;
            return value / (distance*distance) * sizeMod;
        }
        
        // Checks whether it's time to run a SensorScan
        public void ScanForTargets(FixedSimTime timeStep)
        {
            bool isSSP = Owner.IsSubspaceProjector;
            float sensorRadius = GetSensorRadius(out Ship sensorShip);

            // scanning for friendlies is used by a lot of ships for specific
            // resupply, repair and retreat tasks
            // however, SSP-s and Platforms do not need to scan for friendlies
            bool shouldScanForFriends = !(Owner.IsPlatform || isSSP);
            if (shouldScanForFriends)
            {
                FriendScanTimer -= timeStep.FixedTime;
                if (FriendScanTimer <= 0f)
                {
                    FriendScanTimer += EmpireConstants.FriendScanInterval;
                    ScanForFriendlies(sensorShip, sensorRadius);
                }
            }

            // projectiles can only be tracked by ships with PD-s and tracking power
            // anything else should avoid tracking projectiles since this is really expensive
            bool canTrackProjectiles = TargetProjectiles && Owner.TrackingPower > 0;
            // if we're a HangarShip, we can fetch targets from carrier
            bool canFetchCarrierTargets = Owner.IsHangarShip;
            if (canTrackProjectiles || canFetchCarrierTargets)
            {
                ProjectileScanTimer -= timeStep.FixedTime;
                if (ProjectileScanTimer <= 0f)
                {
                    ProjectileScanTimer += EmpireConstants.ProjectileScanInterval;
                    if (canTrackProjectiles)
                        ScanForProjectiles(sensorShip, sensorRadius);
                    else
                        FetchTargetsFromMothership(Owner.Mothership, sensorRadius);
                }
            }

            // scanning for enemies is important for First Contact
            // as well as for selecting combat targets
            // For non-combat ships, it is important to be able to Flee
            // Only subspace projectors don't need it
            bool shouldScanForEnemies = !isSSP;
            if (shouldScanForEnemies)
            {
                EnemyScanTimer -= timeStep.FixedTime;
                if (EnemyScanTimer <= 0f)
                {
                    EnemyScanTimer += EmpireConstants.EnemyScanInterval;
                    ScanForEnemies(sensorShip, sensorRadius);
                }
            }

            bool canSelectTargets = !IsNonCombatant;
            if (canSelectTargets)
            {
                TargetSelectTimer -= timeStep.FixedTime;
                if (TargetSelectTimer <= 0f)
                {
                    TargetSelectTimer += EmpireConstants.TargetSelectionInterval;

                    Ship selectedTarget = SelectCombatTarget(sensorRadius);
                    if (Owner.fleet == null && selectedTarget == null && Owner.IsHangarShip)
                    {
                        selectedTarget = Owner.Mothership.AI.Target;
                    }

                    // automatically choose `Target` if ship does not have Priority
                    // or if current target already died
                    if (!HasPriorityOrder && !HasPriorityTarget)
                    {
                        Target = selectedTarget;
                    }
                }
            }
        }

        bool ShouldEnterAutoCombat()
        {
            if (Target == null || State == AIState.Combat ||
                HasPriorityOrder || IgnoreCombat || IsNonCombatant)
                return false;

            bool canAttack = Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters;
            if (!canAttack)
                return false;

            if (CombatState == CombatState.GuardMode && 
                !Target.Position.InRadius(Owner.Position, Ship.GuardModeRange))
                return false;

            if (CombatState == CombatState.HoldPosition &&
                !Target.Position.InRadius(Owner.Position, Ship.HoldPositionRange))
                return false;

            return true;
        }

        void EnterCombatState(AIState combatState)
        {
            if (!Owner.InCombat)
            {
                Owner.InCombat = true;
                //Log.Write(ConsoleColor.Green, $"ENTER combat: {Owner}");
            }

            // always override the combat state
            State = combatState;
            Owner.SetHighAlertStatus();

            switch (OrderQueue.PeekFirst?.Plan)
            {
                // if not in combative state, enter auto-combat AFTER current order queue is finished
                default:
                    if (!FindGoal(Plan.DoCombat, out ShipGoal _))
                        AddShipGoal(Plan.DoCombat, combatState, pushToFront: true);
                    break;
                case Plan.DoCombat:
                case Plan.Bombard:
                case Plan.BoardShip:
                    break;
            }
        }

        void ExitCombatState()
        {
            if (OrderQueue.TryPeekFirst(out ShipGoal goal) &&
                goal?.WantedState == AIState.Combat)
            {
                DequeueCurrentOrder();
            }

            //Log.Write(ConsoleColor.Red, $"EXIT combat: {Owner}");
            if (Owner.InCombat)
                Owner.SetHighAlertStatus();
            Owner.InCombat = false;

            if (OrderQueue.IsEmpty) // need to change the state to prevent re-enter combat bug
                State = AIState.AwaitingOrders;

            int count = Owner.Weapons.Count;
            Weapon[] items = Owner.Weapons.GetInternalArrayItems();
            for (int x = 0; x < count; x++)
                items[x].ClearFireTarget();
        }

        void UpdateCombatStateAI(FixedSimTime timeStep)
        {
            bool badGuysNear = BadGuysNear;
            bool inCombat = Owner.InCombat;

            if (badGuysNear && !inCombat && ShouldEnterAutoCombat())
            {
                EnterCombatState(AIState.Combat);
            }
            // no nearby bad guys, no priority target, exit auto-combat
            else if (!badGuysNear && inCombat && Target == null)
            {
                ExitCombatState();
            }

            if (PotentialTargets.Length == 0 && Target == null)
                Owner.Carrier.RecallAfterCombat();

            // fbedard: civilian ships will evade combat (nice target practice)
            if (badGuysNear && Owner.shipData.ShipCategory == ShipCategory.Civilian)
            {
                if (Owner.WeaponsMaxRange <= 0)
                {
                    CombatState = CombatState.Evade;
                }
            }

            // try fire weapons if Alert is set
            if (badGuysNear || TrackProjectiles.Length != 0)
            {
                FireWeapons(timeStep);
            }

            // Honor fighter launch buttons
            Owner.Carrier.HandleHangarShipsScramble();
        }

        public float GetSensorRadius() => GetSensorRadius(out Ship _);

        public float GetSensorRadius(out Ship sensorShip)
        {
            if (Owner.IsHangarShip)
            {
                // get the motherships sensor status.
                float motherRange = Owner.Mothership.AI.GetSensorRadius(out sensorShip);

                // in radius of the motherships sensors then use that.
                if (Owner.Position.InRadius(sensorShip.Position, motherRange - Owner.SensorRange))
                    return motherRange;
            }
            sensorShip = Owner;
            float sensorRange = Owner.SensorRange + (Owner.IsInFriendlyProjectorRange ? 10000 : 0);
            return sensorRange;
        }

        public void DropBombsAtGoal(ShipGoal goal, bool inOrbit)
        {
            if (!inOrbit) 
                return;

            foreach (ShipModule bombBay in Owner.BombBays)
            {
                if (bombBay.InstalledWeapon.CooldownTimer > 0f)
                    continue;
                var bomb = new Bomb(new Vector3(Owner.Position, 0f), Owner.loyalty, bombBay.BombType, Owner.Level);
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