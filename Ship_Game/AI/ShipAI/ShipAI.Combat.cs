using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Empires;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;


namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        [StarData] public CombatState CombatState = CombatState.AttackRuns;
        public CombatAI CombatAI;

        public Ship[] PotentialTargets = Empty<Ship>.Array;
        public Ship[] FriendliesNearby = Empty<Ship>.Array;
        public Projectile[] TrackProjectiles = Empty<Projectile>.Array;

        float TargetSelectTimer;
        float ProjectileScanTimer;
        float EnemyScanTimer;
        float FriendScanTimer;

        #pragma warning disable CA2213
        [StarData] public Ship EscortTarget;
        public Planet ExterminationTarget;
        [StarData] public Ship Target;
        #pragma warning restore CA2213

        public Array<Ship> TargetQueue = new();
        float TriggerDelay;
        Array<Ship> ScannedTargets = new();
        Array<Ship> ScannedFriendlies = new();
        Array<Projectile> ScannedProjectiles = new();

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
            if (!Owner.HasCommand || Owner.engineState == Ship.MoveState.Warp
                || Owner.EMPDisabled || Owner.Weapons.Count == 0 || !BadGuysNear)
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
                    GameObject target = weapon.FireTarget;
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

            ++sensorShip.Universe.Objects.Scans;

            var findFriends = new SearchOptions(sensorShip.Position, sensorRadius, GameObjectType.Ship)
            {
                MaxResults = 32,
                Exclude = sensorShip,
                OnlyLoyalty = sensorShip.Loyalty,
            };

            SpatialObjectBase[] friends = sensorShip.Universe.Spatial.FindNearby(ref findFriends);
            
            for (int i = 0; i < friends.Length; ++i)
            {
                var friend = (Ship)friends[i];
                if (friend.Active && !friend.Dying && !friend.IsHangarShip)
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

            ++sensorShip.Universe.Objects.Scans;
            BadGuysNear = false;

            Empire us = sensorShip.Loyalty;
            var findEnemies = new SearchOptions(sensorShip.Position, sensorRadius, GameObjectType.Ship)
            {
                MaxResults = 64,
                Exclude = sensorShip,
                ExcludeLoyalty = us,
            };

            SpatialObjectBase[] enemies = sensorShip.Universe.Spatial.FindNearby(ref findEnemies);

            for (int i = 0; i < enemies.Length; ++i)
            {
                var enemy = (Ship)enemies[i];
                if (!enemy.Active || enemy.Dying)
                    continue;

                Empire other = enemy.Loyalty;

                // update two-way visibility,
                // enemy is known by our Empire - this information is used by our Empire later
                // the enemy itself does not care about it
                us.AI.ThreatMatrix.SetSeen(enemy, fromBackgroundThread:true);
                if (us.AlliedWithPlayer)
                    enemy.KnownByEmpires.SetSeen(us.Universe.Player);

                if (!us.IsKnown(other))
                {
                    us.FirstContact.SetReadyForContact(other);
                }
                if (us.IsEmpireAttackable(other, enemy, scanOnly:true))
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

            ++sensorShip.Universe.Objects.Scans;

            // as optimization we use WeaponsMaxRange instead
            var opt = new SearchOptions(sensorShip.Position, sensorShip.WeaponsMaxRange, GameObjectType.Proj)
            {
                MaxResults = 32,
                SortByDistance = true, // only care about closest results
                ExcludeLoyalty = Owner.Loyalty,
                FilterFunction = (go) =>
                {
                    var missile = (Projectile)go;
                    // Note: this ensures we don't accidentally target Allied projectiles
                    // TODO: But this check is also done again in Weapon.cs target selection
                    // TODO: use the intercept tag and loyalty tag in Qtree
                    bool canIntercept = missile.Weapon.Tag_Intercept && Owner.Loyalty.IsEmpireAttackable(missile.Loyalty);
                    return canIntercept;
                }
            };

            SpatialObjectBase[] missiles = sensorShip.Universe.Spatial.FindNearby(ref opt);
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
            public bool ScreenShip(Ship ship) => ship.ArmorMax + ship.ShieldMax >= Defense;
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
                
                Armor       += ship.ArmorMax;
                Shield      += ship.ShieldMax;
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
                        BadGuysNear = Owner.Loyalty.IsEmpireAttackable(p.Owner)
                                   && Owner.Position.InRadius(p.Position, radius);
                }
            }

            if (Target != null)
            {
                if (Target.Loyalty == Owner.Loyalty)
                {
                    HasPriorityTarget = false;
                }
                else if (!Intercepting && Target.IsInWarp)
                {
                    Target = null;
                    if (!HasPriorityOrder && !Owner.Loyalty.isPlayer)
                        OrderAwaitOrders(false);
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
                BadGuysNear |= Owner.Loyalty.IsEmpireAttackable(Target.Loyalty, Target);
                return Target;
            }

            return GetHighestPriorityTarget(); // This is the alternative logic
        }

        static bool IsTargetActive(Ship target)
        {
            return target != null && target.Active && !target.Dying;
        }

        public Ship GetHighestPriorityTarget()
        {
            if (Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                return null;

            if (PotentialTargets.Length > 0)
            {
                return PotentialTargets.FindMax(GetTargetPriority);
            }
            return null;
        }

        // Set this to get debug output of GetTargetPriority() weights
        public static bool EnableTargetPriorityDebug;

        float GetTargetPriority(Ship tgt)
        {
            if (tgt.IsInWarp)
                return 0;

            // when selecting enemy targets, we should see how easy they are to kill
            // if we chose strongest enemies, we could make 0 effect and get whittled down by small fries
            // so instead choose base value from how killable the ship is
            float dps = Owner.TotalDps;
            if (dps == 0f) // not all ships have DPS, so equate them with a weak ship
                dps = 100f;

            float value = (10000f * dps) / (tgt.Health + tgt.ShieldPower).LowerBound(1);
            value *= 1f + tgt.Carrier.AllFighterHangars.Length*0.75f;
            value *= 1 + tgt.TroopCapacity*0.5f;
            value *= 1 + (tgt.HasBombs ? tgt.BombBays.Count*2 : tgt.BombBays.Count*0.5f);

            bool debug = EnableTargetPriorityDebug && (Owner.Loyalty.isPlayer);
            void Debug(string s) => Log.Write(
                $"{tgt.Loyalty.data.ArchetypeName} {tgt.DesignRole,9}={value.String(),-6} | "+
                $"{s,-16} | tgt: {tgt.Name+tgt.Id,-20} | us: {Owner.Name+Owner.Id,-20} |");

            if (debug) Debug($"str={tgt.GetStrength().String()}");
            if (Owner.AI.EscortTarget != null && tgt.AI.Target == Owner.AI.EscortTarget)
            {
                value *= 10; // higher priority to anyone attacking the ship we are escorting
            }
            else
            {
                float angleDiff = Owner.AngleDifferenceToPosition(tgt.Position);
                // treat everything inside -45 and +45 degs as 1.0
                // targets at 90 degs will be 2.0, anything above 135 degs will be 3.0
                float angleMod = angleDiff.Clamped(RadMath.Deg45AsRads, RadMath.Deg135AsRads) / RadMath.Deg45AsRads;
                value /= angleMod;
                if (debug) Debug($"angle={angleDiff.String()}");
            }
            
            float minimumDistance = Owner.Radius + tgt.Radius;
            float distance = Owner.Distance(tgt).LowerBound(minimumDistance);
            if (tgt.AI.Target == Owner && distance < tgt.DesiredCombatRange) // prefer enemies targeting us (within range)
                value *= 2.0f;
            if (tgt.Resupplying) // lower priority to enemies that are retreating
                value *= 0.75f;

            float relDist = (distance / Owner.DesiredCombatRange) * 10;
            value /= relDist; // prefer targets that are closer, but in 50m increments
            if (debug) Debug($"d={distance.String(),-4} dv={relDist.String(2),-4}");

            // make ships prefer targets which are closer to their own size
            // this ensures fighters prefer fighters and frigates prefer frigates
            float relSize = (float)tgt.SurfaceArea / Owner.SurfaceArea;
            if (relSize < 1f)
            {
                // if we are interceptors - we want to get these smaller ships.
                // value = 100 * (1/0.25) = 400
                if (Owner.ShipData.HangarDesignation == HangarOptions.Interceptor)
                {
                    value *= (1 / relSize);
                }
                // if target is smaller then 0.5^2 = 0.25, smaller ships are
                // almost always weaker than us so they're not a huge priority.
                // other ships of same size are more important
                // value = 100 * 0.25 = 25
                else
                {
                    value *= (relSize * relSize);
                }
            }
            else if (relSize > 1f)
            {
                // if we are anti-ship, always prefer to target big ships:
                // value = 100 * (1.25*1.25) = 156.25 (stronger? prefer it)
                if (Owner.ShipData.HangarDesignation == HangarOptions.AntiShip)
                {
                    value *= (relSize * relSize);
                }
                // if target is bigger than us, use division to make us less afraid of it
                // value = 100 / (2.0 * 0.25) = 200 (2x stronger? good target)
                // value = 100 / (4.0 * 0.25) = 100 (4x stronger? not afraid at all)
                // value = 100 / (4.47 * 0.25) = 89.4 (a bit afraid)
                else
                {
                    value /= (relSize * 0.25f);
                }
            }

            if (debug) Debug($"relSize={relSize.String(2),-4}");
            if (debug) Debug($"{value.String(2)}");
            return value;
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

                    // TODO: this should be split into two parts 1) update target 2) choose new target
                    //       because choosing a new target is expensive, and it's not even used sometimes
                    Ship selectedTarget = SelectCombatTarget(sensorRadius);
                    if (Owner.Fleet == null && selectedTarget == null && Owner.IsHangarShip)
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

        // can the ship automatically enter combat from any existing goal?
        bool ShouldEnterAutoCombat()
        {
            // not if we're already in combat, or we have a priority order 
            if (State == AIState.Combat || HasPriorityOrder)
                return false;
            // Get the active Movement stance, or Regular
            // if ship is warping, it will always have this MoveOrder available
            MoveOrder order = OrderQueue.PeekFirst?.MoveOrder ?? MoveOrder.Regular;
            return CanEnterCombatFromCurrentStance(order);
        }

        // does current CombatState and Move Stance allows us to enter combat with `Target`
        bool CanEnterCombatFromCurrentStance(MoveOrder order)
        {
            if (Target == null || IgnoreCombat || IsNonCombatant)
                return false;

            bool canAttack = Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters;
            if (!canAttack)
                return false;

            // always enter combat if we're in Aggressive Stance, even if ship is in warp
            if (order.IsSet(MoveOrder.Aggressive))
                return true; // waaaagh!

            // in regular move stance, respect CombatState settings
            if (order == 0 || order.IsSet(MoveOrder.Regular))
            {
                // regular move stance never exits warp
                if (Owner.IsInWarp)
                    return false;
                // are we close enough to automatically engage this enemy?
                float enterCombatRange = GetEnterCombatRangeFromCombatState(CombatState);
                if (Target.Position.InRadius(Owner.Position, enterCombatRange))
                    return true;
                return false; // no combat
            }

            // in StandGround, never enter combat
            if (order.IsSet(MoveOrder.StandGround)) // this is here for correctness sake, or if we decide to add to it
                return false; // no combat

            return false; // should not reach here
        }

        float GetEnterCombatRangeFromCombatState(CombatState cs)
        {
            // GuardMode and HoldPosition Stances are for avoiding combat, so significantly smaller
            if (cs == CombatState.GuardMode) return Ship.GuardModeRange;
            if (cs == CombatState.HoldPosition) return Ship.HoldPositionRange;
            // for all other Combat stances, enter combat from Sensor Range
            return Owner.SensorRange;
        }

        void EnterCombatState(AIState combatState)
        {
            if (!Owner.InCombat)
            {
                Owner.InCombat = true;
                //Log.Write(ConsoleColor.Green, $"ENTER combat: {Owner}");
            }

            // always override the combat state
            ChangeAIState(combatState);

            // check if DoCombat is already the first ShipGoal
            switch (OrderQueue.PeekFirst?.Plan)
            {
                // if we haven't already queued a DoCombat order (eg MoveTo + Attack)
                // then push a new DoCombat ShipGoal into the front of the queue
                // once combat is over, ShipAI will automatically resume any previous goals
                default:
                    if (!FindGoal(Plan.DoCombat, out ShipGoal _))
                    {
                        AddShipGoal(Plan.DoCombat, combatState, pushToFront: true);
                    }
                    break;

                // the first Goal is already one of these combat Plans, we don't need to do anything:
                case Plan.DoCombat: case Plan.Bombard: case Plan.BoardShip:
                    break;
            }
        }

        void ExitCombatState()
        {
            if (OrderQueue.TryPeekFirst(out ShipGoal goal) &&
                goal?.Plan == Plan.DoCombat)
            {
                //Log.Info($"ExitCombat {Owner.Name}: DequeueOrder {goal.Plan} {goal.MoveOrder}");
                DequeueCurrentOrder();
            }

            //Log.Write(ConsoleColor.Red, $"EXIT combat: {Owner}");
            Owner.InCombat = false;

            if (OrderQueue.IsEmpty) // need to change the state to prevent re-enter combat bug
                OrderAwaitOrders(false);

            int count = Owner.Weapons.Count;
            Weapon[] items = Owner.Weapons.GetInternalArrayItems();
            for (int x = 0; x < count; x++)
                items[x].ClearFireTarget();
        }

        public void BackToCarrier()
        {
            if (Owner.Mothership.Carrier.FightersLaunched)
                Owner.DoEscort(Owner.Mothership);
            else
                OrderReturnToHangarDeferred();
        }

        void UpdateCombatStateAI(FixedSimTime timeStep)
        {
            bool badGuysNear = BadGuysNear;
            bool inCombat = Owner.InCombat;
 
            // if there are Enemies nearby, or ship is in combat, always set HighAlert
            if (badGuysNear || inCombat)
            {
                Owner.SetHighAlertStatus();
                
                // HACK: This is a workaround for invalid AI states
                if (inCombat && !Owner.Loyalty.isPlayer)
                {
                    // AI in combat should never be in HoldPosition, because they'll be stuck as dummy targets
                    // changing to GuardMode will make them properly engage combat
                    if (CombatState == CombatState.HoldPosition)
                        CombatState = CombatState.GuardMode;
                }
            }

            if (badGuysNear && !inCombat && Target != null && ShouldEnterAutoCombat())
            {
                EnterCombatState(AIState.Combat);
            }
            // no nearby bad guys, no priority target, exit auto-combat
            else if (!badGuysNear && inCombat && Target == null)
            {
                ExitCombatState();
                if (Owner.IsHangarShip)
                    BackToCarrier();
            }

            // fbedard: civilian ships will evade combat (nice target practice)
            if (badGuysNear && Owner.ShipData.ShipCategory == ShipCategory.Civilian)
            {
                if (Owner.WeaponsMaxRange <= 0 && !HasPriorityOrder)
                {
                    CombatState = CombatState.Evade;
                }
            }

            // try fire weapons if Alert is set
            if (badGuysNear || TrackProjectiles.Length != 0)
            {
                FireWeapons(timeStep);
            }
        }

        public float GetSensorRadius() => GetSensorRadius(out Ship _);

        public float GetSensorRadius(out Ship sensorShip)
        {
            if (Owner.IsLaunching)
            {
                sensorShip = Owner;
                return 0;
            }

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
                var bomb = new Bomb(new Vector3(Owner.Position, 0f), Owner.Loyalty,
                    bombBay.BombType, Owner.Level, Owner.HealthPercent);

                if (Owner.Ordinance > bombBay.InstalledWeapon.OrdinanceRequiredToFire)
                {
                    Owner.ChangeOrdnance(-bombBay.InstalledWeapon.OrdinanceRequiredToFire);
                    bomb.SetTarget(goal.TargetPlanet);
                    Owner.Universe.Screen.BombList.Add(bomb);
                    bombBay.InstalledWeapon.CooldownTimer = bombBay.InstalledWeapon.FireDelay;
                }
            }
        }
    }
}
