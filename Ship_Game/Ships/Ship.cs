using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Empires.Components;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships.Components;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Ships
{
    [StarDataType]
    public partial class Ship : PhysicsObject, IDisposable
    {
        [StarData] public string VanityName = ""; // user modifiable ship name. Usually same as Ship.Name

        public float SensorRange = 20000f;
        public float MaxBank = 0.5236f;

        public Vector2 ProjectedPosition;
        public Thruster[] ThrusterList = Empty<Thruster>.Array;

        public Array<Weapon> Weapons = new();
        [StarData] float JumpTimer = 3f;
        public AudioEmitter SoundEmitter = new();
        [StarData] public float ScuttleTimer = -1f;
        
        [StarData] public Fleet Fleet;
        // Ship's rotated offset from fleet center
        [StarData] public Vector2 FleetOffset;

        // Unrotated fleet offset from [0,0]
        [StarData] public Vector2 RelativeFleetOffset;

        // This is the current cluster that our ship
        // belongs to in ThreatMatrix's clusters
        [StarData] public ThreatCluster CurrentCluster;

        public Array<ShipModule> BombBays = new();
        [StarData] public CarrierBays Carrier;
        [StarData] public ShipResupply Supply;
        public bool ShipStatusChanged;
        public bool IsMeteor { get; private set; }

        [StarData] Planet TetheredTo;
        [StarData] public Vector2 TetherOffset;
        public float EMPDamage { get; private set; }
        [StarData] public float YRotation;
        public float MechanicalBoardingDefense;
        public float TroopBoardingDefense;
        public float ECMValue;
        [StarData] public IShipDesign ShipData;
        [StarData] public int Kills;
        [StarData] public float Experience;
        public bool EnginesKnockedOut;
        public float InhibitionRadius;
        public bool IsPlatform;
        public bool IsGuardian; // Remnant Guardian created at game start
        SceneObject ShipSO;
        public bool ManualHangarOverride;
        [StarData] public Ship Mothership;
        [StarData] public string Name;   // name of the original design of the ship, eg "Subspace Projector". Look at VanityName
        public float PackDamageModifier { get; private set; }

        // Current owner of this ship.
        // This is accessed a lot, so we keep it as a public field
        [StarData] public Empire Loyalty;
        public LoyaltyChanges LoyaltyTracker { get; private set; }
        public void LoyaltyChangeFromBoarding(Empire empire, bool addNotification = true) => LoyaltyTracker.SetBoardingLoyalty(empire, addNotification);
        public void LoyaltyChangeByGift(Empire empire, bool addNotification = true) => LoyaltyTracker.SetLoyaltyForAbsorbedShip(empire, addNotification);
        public void LoyaltyChangeAtSpawn(Empire empire) => LoyaltyTracker.SetLoyaltyForNewShip(empire);

        [StarData] public float Ordinance { get; private set; } // FB: use ChanceOrdnance function to control Ordnance
        public float OrdnanceMin { get; private set; } // FB: minimum ordnance required to fire any of the ship's weapons
        public float OrdinanceMax;
        [StarData] public ShipAI AI { get; private set; }

        public float ShieldPercent;
        public float ArmorMax;
        public float ShieldMax;

        // Total sum of all active shield module's current power
        public float ShieldPower { get; private set; }

        // total number of installed module SLOTS with Internal Restrictions
        // example: a 2x2 internal module would give +4
        public int NumInternalSlots => Grid.NumInternalSlots;

        // active internal modules SLOTS that have health > 0
        public int ActiveInternalModuleSlots;

        [StarData] public float PowerCurrent;
        public float PowerFlowMax;
        public float PowerStoreMax;
        public float PowerDraw;
        public Power NetPower;
        readonly AudioHandle JumpSfx = new();

        [StarData] public int Level;
        int MaxHealthRevision;
        public float HealthMax { get; private set; }
        public int TroopCapacity;
        public float OrdAddedPerSecond;
        public float ShieldRechargeTimer;
        [StarData] public bool InCombat;
        public float XRotation;
        public float BonusEMPProtection;
        public bool InPlayerSensorRange => KnownByEmpires.KnownByPlayer(Universe);
        public KnownByEmpire KnownByEmpires;
        public KnownByEmpire PlayerProjectorHasSeenEmpires;
        public bool EMPDisabled;
        public float TractorDamage { get; private set; }
        private bool BeingTractored;
        float UpdateTimer;

        float HighAlertTimer;
        public bool OnHighAlert => HighAlertTimer > 0f;
        public bool OnLowAlert => HighAlertTimer <= 0f;
        public const float HighAlertSeconds = 10;
        public void SetHighAlertStatus() => HighAlertTimer = HighAlertSeconds;
        public float GetHighAlertTimer() => HighAlertTimer;

        public float ExplorePlanetDistance => (SensorRange * 0.1f).LowerBound(500) * Loyalty.data.Traits.ExploreDistanceMultiplier;
        public float ExploreSystemDistance => SensorRange * Loyalty.data.Traits.ExploreDistanceMultiplier;

        public float InternalSlotsHealthPercent; // number_Alive_Internal_slots / number_Internal_slots
        Vector3 DieRotation;
        [StarData] private float DieTimer;
        [StarData] public float BaseStrength;
        [StarData] public bool Dying;

        /// TRUE if this ship has been completely destroyed, or is displaying its Dying animation
        public bool IsDeadOrDying => !Active || Dying;

        /// TRUE if this ship is Active and not displaying its Dying animation
        public bool IsAlive => Active && !Dying;

        [StarData] public PlanetCrash PlanetCrash;
        [StarData] public LaunchShip LaunchShip;
        private bool ReallyDie;
        private bool HasExploded;
        public float TotalDps { get; private set; }

        public bool HasCommand;
        public int SecondsAlive { get; private set; } // FB - for scrap loop warnings

        public ReaderWriterLockSlim SupplyLock = new ReaderWriterLockSlim();
        public int TrackingPower;
        public int TargetingAccuracy;
        public float ResearchPerTurn;

        public float BoardingDefenseTotal => MechanicalBoardingDefense + TroopBoardingDefense;

        public float FTLModifier { get; private set; } = 1f;
        [StarData] public Planet HomePlanet { get; private set; }

        public Weapon FastestWeapon => Weapons.FindMax(w => w.ProjectileSpeed);
        public float MaxWeaponError = 0;

        public bool IsLaunching => LaunchShip != null;

        public bool IsDefaultAssaultShuttle => Loyalty.data.DefaultAssaultShuttle == Name || Empire.DefaultBoardingShuttleName == Name;
        public bool IsDefaultTroopShip      => !IsDefaultAssaultShuttle && (Loyalty.data.DefaultTroopShip == Name || DesignRole == RoleName.troop);
        public bool IsDefaultTroopTransport => IsDefaultTroopShip || IsDefaultAssaultShuttle;
        public bool IsSingleTroopShip       => ShipData.IsSingleTroopShip;
        public bool IsTroopShip             => ShipData.IsTroopShip;
        public bool IsBomber                => ShipData.IsBomber;
        public bool IsSubspaceProjector     => ShipData.IsSubspaceProjector;
        public bool IsResearchStation       => ShipData.IsResearchStation;
        public bool HasBombs                => BombBays.Count > 0;
        public bool IsEmpireSupport         => DesignRoleType == RoleType.EmpireSupport;
        public bool Resupplying             => AI.State == AIState.Resupply || AI.State == AIState.ResupplyEscort;
                
        /// <summary>
        /// Ship is expected to exchange fire with enemy ships directly not through hangar ships and other such things.
        /// </summary>
        public bool IsAWarShip => DesignRoleType == RoleType.Warship;
        public bool IsOrbital  => DesignRoleType == RoleType.Orbital;
        public bool IsInAFleet => Fleet != null;

        /// <summary>
        /// This ship is a carrier which launches fighters/corvettes/frigates
        /// </summary>
        public bool IsPrimaryCarrier   => DesignRole == RoleName.carrier;
        public bool IsSecondaryCarrier => !IsPrimaryCarrier && Carrier.HasFighterBays;

        [StarData] public Array<Rectangle> AreaOfOperation = new();
        
        /// <summary>
        /// Removes ship from any pools and fleets and doesn't put them back into Empire Force Pools
        /// </summary>
        /// <param name="clearOrders">Clear Ship AI orders?</param>
        public void RemoveFromPoolAndFleet(bool clearOrders)
        {
            if (clearOrders)
                AI?.ClearOrders();
            ClearFleet(returnToManagedPools: false, clearOrders: clearOrders);
        }

        /// <summary>
        /// Removes this ship from its assigned Fleet (if any)
        /// </summary>
        /// <param name="returnToManagedPools">if true, return the ship to empire's AI pool for reassignment</param>
        /// <param name="clearOrders">if true, AI orders are cleared upon successful removal</param>
        public void ClearFleet(bool returnToManagedPools, bool clearOrders)
        {
            Fleet?.RemoveShip(this, returnToEmpireAI: returnToManagedPools, clearOrders: clearOrders);
        }

        public bool IsConstructor => ShipData.IsConstructor;

        /// <summary>
        /// Where this is true the force pool add will reject these ships.
        /// </summary>
        public bool ShouldNotBeAddedToForcePools()
        {
            return !Active || IsInAFleet || IsHangarShip || IsHomeDefense
                || ShipData.IsCarrierOnly || IsEmpireSupport || IsOrbital
                || DoingRefit || DoingScrap || DoingScuttle || ShipData.IsColonyShip
                || IsFreighter || IsSupplyShuttle || Resupplying;
        }

        /// <summary>
        /// Ship is not directly a combat ship. It is used to support a fleet or fleet goals
        /// </summary>
        public bool IsFleetSupportShip()
        {
            return DesignRoleType == RoleType.WarSupport ||
                   DesignRoleType == RoleType.Troop ||
                       DesignRole == RoleName.carrier;
        }

        public bool IsGoodScout() => ShipData.Role != RoleName.supply 
                                     && Fleet == null
                                     && DesignRole == RoleName.scout;

        public bool IsIdleScout()
        {
            if (ShipData.Role == RoleName.supply)
                return false; // FB - this is a workaround, since supply shuttle register as scouts design role.

            return Fleet == null
                   && AI.State != AIState.Flee
                   && AI.State != AIState.Scrap
                   && AI.State != AIState.Explore
                   && !AI.HasPriorityOrder
                   && DesignRole == RoleName.scout;
        }

        public void SetCombatStance(CombatState stance)
        {
            AI.CombatState = stance;
            if (stance == CombatState.HoldPosition)
            {
                AI.OrderAllStop();
            }
            ShipStatusChanged = true;
        }

        public bool CanTakeFleetMoveOrders()
        {
            return Active
                && ShipEngines.EngineStatus == EngineStatus.Active
                && CanTakeFleetOrders
                && MaxFTLSpeed >= 10_000f; // can it even warp properly?
        }

        public bool CanTakeFleetOrders
        {
            get
            {
                switch (AI.State)
                {
                    case AIState.Resupply:
                    case AIState.Refit:
                    case AIState.Scrap:
                    case AIState.Scuttle:
                        return false;
                    default:
                        if (EMPDisabled)
                            return false;
                        return true;
                }
            }
        }

        public bool InsideAreaOfOperation(Vector2 pos)
        {
            if (AreaOfOperation.IsEmpty)
                return true;

            foreach (Rectangle ao in AreaOfOperation)
                if (ao.HitTest(pos))
                    return true;

            return false;
        }

        public bool IsBeingTargeted(out Ship targetingShip)
        {
            targetingShip = null;
            for (int i = 0; i < AI.PotentialTargets.Length; i++)
            {
                Ship potentialShip = AI.PotentialTargets[i];
                if (potentialShip.AI.Target == this)
                {
                    targetingShip = potentialShip;
                    return true;
                }
            }

            return false;
        }

        public void PiratePostChangeLoyalty()
        {
            if (Loyalty.WeArePirates)
            {
                if (IsSubspaceProjector || IsResearchStation)
                    ScuttleTimer = 20;
                else
                    AI.OrderPirateFleeHome();
            }
        }

        public void UpdateHomePlanet(Planet planet)
        {
            HomePlanet = planet;
        }

        public bool PlayerShipCanTakeFleetOrders()
        {
            if (Loyalty.isPlayer && !CanTakeFleetOrders)
            {
                ResupplyReason resupplyReason = Supply.Resupply();
                // Resupply reason is not severe enough for the player ship to ignore fleet commands.
                return resupplyReason == ResupplyReason.LowOrdnanceNonCombat
                    || resupplyReason == ResupplyReason.NotNeeded;
            }

            return true; // AI ship or a player ship which can take fleet orders
        }
        
        public float EmpTolerance  => SurfaceArea + BonusEMPProtection;
        public float HealthPercent => HealthMax > 0f ? Health / HealthMax : 0f;

        public float EmpRecovery
        {
            get
            {
                if (Loyalty.WeAreRemnants)
                    return 20 + BonusEMPProtection / 20;

                return OnHighAlert ? 1 + BonusEMPProtection / 1000 : 20 + BonusEMPProtection / 20;
            }
        }

        public void DebugDamage(float percent)
        {
            percent = percent.Clamped(0f, 1f);
            foreach (ShipModule module in ModuleSlotList)
                module.DebugDamage(percent);
        }

        public void DebugBlowBiggestExplodingModule()  => DebugBlowExplodingModule(true);
        public void DebugBlowSmallestExplodingModule() => DebugBlowExplodingModule(false);

        void DebugBlowExplodingModule(bool biggest)
        {
            var exploders = ModuleSlotList.Filter(m => m.Explodes && m.Active);
            if (exploders.Length > 0)
            {
                if (biggest)
                    exploders.FindMax(m => m.ExplosionDamage).DebugDamage(1);
                else
                    exploders.FindMin(m => m.ExplosionDamage).DebugDamage(1);
            }
        }

        public void DamageByRecoveredFromCrash(float modifier)
        {
            foreach (ShipModule module in ModuleSlotList)
                module.DamageByRecoveredFromCrash(modifier);

            Carrier.ResetAllHangarTimers();
            KillAllTroops();
        }

        public RoleName DesignRole => ShipData.Role;
        public RoleType DesignRoleType => ShipDesign.ShipRoleToRoleType(DesignRole);

        public Color GetStatusColor()
        {
            Color color = Color.Black;
            if (HealthPercent < 0.75f) color = Color.Yellow;
            if (InternalSlotsHealthPercent < 0.75f) color = Color.Red;
            return color;
        }

        public TacticalIcon TacticalIcon() => ShipData.GetTacticalIcon();

        float GetYBankAmount(FixedSimTime timeStep)
        {
            float yBank = RotationRadsPerSecond * timeStep.FixedTime;
            switch (ShipData.Role)
            {
                default:
                    return yBank;
                case RoleName.drone:
                case RoleName.scout:
                case RoleName.fighter:
                    return yBank * 2f;
            }
        }

        public bool IsPlatformOrStation => ShipData.IsPlatformOrStation;
        public bool IsShipyard => ShipData.IsShipyard;
        public bool IsStation => ShipData.IsStation;

        public void CauseEmpDamage(float empDamage) // FB - also used for recover EMP
        {
            EMPDamage = (EMPDamage + empDamage).Clamped(0, 10000f.LowerBound(EmpTolerance*10));
            EMPDisabled = EMPDamage > EmpTolerance;
        }

        public void CausePowerDamage(float powerDamage) => PowerCurrent = (PowerCurrent - powerDamage).Clamped(0, PowerStoreMax);
        public void AddPower(float powerAcquired)       => PowerCurrent = (PowerCurrent + powerAcquired).Clamped(0, PowerStoreMax);

        public void CauseTroopDamage(float troopDamageChance)
        {
            if (HasOurTroops)
            {
                if (Loyalty.Random.RollDice(troopDamageChance) && GetOurFirstTroop(out Troop first))
                {
                    float damage = 1;
                    first.DamageTroop(this, ref damage);
                }
            }
            else if (MechanicalBoardingDefense > 0f)
            {
                if (Loyalty.Random.RollDice(troopDamageChance))
                    MechanicalBoardingDefense -= 1f;
            }
        }

        public void CauseRepulsionDamage(Beam beam, float beamModifier)
        {
            if (IsTethered || EnginesKnockedOut)
                return;
            if (beam.Owner == null || beam.Weapon == null)
                return;
            Vector2 repulsion = (Position - beam.Owner.Position) * beam.Weapon.RepulsionDamage * beamModifier;
            ApplyForce(repulsion);
        }

        public void CauseTractorDamage(float tractorDamage, bool hittingShields)
        {
            if (IsTethered)
                return;

            BeingTractored = true;
            ShipStatusChanged = true;
            TractorDamage += hittingShields ? tractorDamage/5 : tractorDamage;
            if (TractorDamage > Mass)
            {
                AllStop();
                HyperspaceReturn();
                EnginesKnockedOut = true;
            }
        }

        public void CauseRadiationDamage(float damage, GameObject source)
        {
            if (IsInWarp)
                damage *= 0.5f; // some protection while in warp

            var damagedShields = new HashSet<ShipModule>();

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.IsExternal) // only apply radiation to outer modules
                    continue;

                if (IsCoveredByShield(module, out ShipModule shield))
                {
                    // only damage shields once, depending on their radius and their energy resistance
                    if (!damagedShields.Contains(shield))
                    {
                        float damageAbsorb = 1 - shield.ShieldEnergyResist;
                        shield.Damage(source, damage * damageAbsorb * shield.ShieldHitRadius);
                        damagedShields.Add(shield);
                    }
                }
                else
                {
                    // again, damage also depends on module radius and their energy resistance
                    float damageAbsorb = 1 - module.EnergyResist;
                    module.Damage(source, damage * damageAbsorb * module.Radius);
                    if (InFrustum && Universe.IsShipViewOrCloser)
                    {
                        // visualize radiation hits on external modules
                        Vector3 center = module.Center3D;
                        for (int j = 0; j < 50; j++)
                            Universe.Screen.Particles.Sparks.AddParticle(center);
                    }
                }
            }
        }

        public bool IsOrbiting(Planet p) => AI.IsOrbiting(p);

        public override bool IsAttackable(Empire attacker, Relationship attackerToUs)
        {
            if (IsResearchStation && !attacker.WeAreRemnants && !attackerToUs.AtWar || IsLaunching)
                return false; 

            if (attackerToUs.CanAttack == false && !attackerToUs.Treaty_Alliance)
            {
                if (System != null && System.HasPlanetsOwnedBy(Loyalty))
                    return false;

                if (attackerToUs.AttackForBorderViolation(attacker.data.DiplomaticPersonality, Loyalty, attacker, IsFreighter)
                 && IsInBordersOf(attacker))
                {
                    return true;
                }

                SolarSystem system = System;
                if (system != null)
                {
                    if (attackerToUs.WarnedSystemsList.Contains(system) && !IsFreighter)
                        return true;

                    if (DesignRole == RoleName.troop &&
                        attacker.GetOwnedSystems().ContainsRef(system))
                        return true;
                }

                if (attackerToUs.AttackForTransgressions(attacker.data.DiplomaticPersonality))
                    return true;

                if (AI.Target?.GetLoyalty() == attacker)
                    return true;
            }

            if (attackerToUs.Treaty_Trade && IsFreighter && AI.State == AIState.SystemTrader)
                return false;

            return attackerToUs.CanAttack;
        }

        // Level 5 crews can use advanced targeting which even predicts acceleration
        public bool CanUseAdvancedTargeting => Level >= 5;

        public override Vector2 JammingError()
        {
            if (CombatDisabled)
                return Vector2.Zero;

            Vector2 error = default;
            if (ECMValue > 0)
                error += Loyalty.Random.Vector2D(ECMValue * 80f);

            return error;
        }

        public override float DodgeMultiplier()
        {
            return 1 + Loyalty.data.Traits.DodgeMod;
        }

        public bool IsHangarShip   => Mothership != null;
        public bool IsHomeDefense  => HomePlanet != null;
        public bool CanBeRefitted  => CanBeScrapped;
        public bool CanBeScrapped  => !IsHangarShip && !IsHomeDefense;
        public bool CombatDisabled => EMPDisabled || Dying || !Active || !HasCommand;

        public bool SupplyShipCanSupply => Carrier.HasSupplyBays 
            && OrdnanceStatusWithIncoming(OrdAddedPerSecond * 60) > Status.Critical
            && OrdnanceStatus != Status.NotApplicable;

        public Status OrdnanceStatus => OrdnanceStatusWithIncoming(0);

        public Status OrdnanceStatusWithIncoming(float incomingAmount)
        {
            if (IsInWarp
                || AI.State == AIState.Scrap
                || AI.State == AIState.Resupply
                || AI.State == AIState.Refit 
                || !CanBeRefitted
                || IsSupplyShuttle
                || ShipData.HullRole < RoleName.fighter && ShipData.HullRole != RoleName.station
                || OrdinanceMax < 1
                || IsTethered && ShipData.HullRole == RoleName.platform)
            {
                return Status.NotApplicable;
            }

            float amount = Ordinance;
            if (incomingAmount > 0)
                amount = (amount + incomingAmount).Clamped(0, OrdinanceMax);
            return ToShipStatus(amount, OrdinanceMax);
        }
        public int BombsGoodFor60Secs
        {
            get
            {
                int bombBays = BombBays.Count;
                switch (Bomb60SecStatus())
                {
                    case Status.Critical:      return bombBays / 10;
                    case Status.Poor:          return bombBays / 5;
                    case Status.Average:
                    case Status.Good:          return bombBays / 2;
                    case Status.Excellent:
                    case Status.Maximum:       return bombBays;
                    case Status.NotApplicable: return 0;
                }
                return 0;
            }

        }

        public Status Bomb60SecStatus()
        {
            if (BombBays.Count <= 0) return Status.NotApplicable;
            if (OrdnanceStatus < Status.Poor) return Status.Critical;
            // we need a standard formula for calculating the below.
            // one is the alpha strike. the other is the continued firing. The below only gets the sustained.
            // so the effect is that it might not have enough ordnance to fire the alpha strike. But it will do.
            float bombSeconds = Ordinance / BombBays.Sum(b =>
            {
                var bomb = b.InstalledWeapon;
                return bomb.OrdinanceRequiredToFire / bomb.FireDelay;
            });
            bombSeconds = bombSeconds.Clamped(0, 60); //can we bomb for a full minute?
            return ToShipStatus(bombSeconds, 60);
        }

        public bool DoingExplore
        {
            get => AI.State == AIState.Explore;
            set => AI.OrderExplore();
        }

        public bool DoingResupply
        {
            get => AI.State == AIState.Resupply;
            set => Supply.ResupplyFromButton();
        }

        public bool DoingScrap
        {
            get => AI.State == AIState.Scrap;
            set => AI.OrderScrapShip();
        }

        public bool DoingRefit
        {
            get => AI.State == AIState.Refit;
            set => Universe.Screen.ScreenManager.AddScreen(new RefitToWindow(Universe.Screen, this));
        }

        public bool DoingScuttle => AI.State == AIState.Scuttle;

        public bool IsInhibitedByUnfriendlyGravityWell
        {
            get
            {
                // friendly projectors disable gravity wells
                if (Loyalty.WeAreRemnants)
                    return false;

                Planet planet = System?.IdentifyGravityWell(this);
                return planet != null;
            }
        }

        // Note - ship with launch plan cannot enter combat until plan is finished.
        // For testing we have Universe.P.DebugDisableShipLaunch
        public void InitLaunch(LaunchPlan launchPlan, float startingRotationDegrees = -1f)
        {
            if (!Universe.P.DebugDisableShipLaunch) 
                LaunchShip = new(this, launchPlan, startingRotationDegrees);
        }

        // Calculates estimated trip time by turns
        public float GetAstrogateTimeTo(Planet destination)
        {
            float distance    = Position.Distance(destination.Position);
            float distanceSTL = destination.GravityWellForEmpire(Loyalty);
            Planet planet     = System?.IdentifyGravityWell(this); // Get the gravity well owner if the ship is in one

            if (planet != null && !IsInFriendlyProjectorRange)
                distanceSTL += planet.GravityWellRadius;

            return GetAstrogateTime(distance, distanceSTL, destination.Position);
        }

        public float GetAstrogateTimeBetween(Planet origin, Ship targetStation)
        {
            float distance = origin.Position.Distance(targetStation.Position);
            float distanceSTL = origin.GravityWellForEmpire(Loyalty);
            if (targetStation.IsTethered)
                distanceSTL += targetStation.GetTether().GravityWellForEmpire(Loyalty);

            return GetAstrogateTime(distance, distanceSTL, targetStation.Position);
        }

        public float GetAstrogateTimeBetween(Planet origin, Planet destination)
        {
            float distance    = origin.Position.Distance(destination.Position);
            float distanceSTL = destination.GravityWellForEmpire(Loyalty) + origin.GravityWellForEmpire(Loyalty);

            return GetAstrogateTime(distance, distanceSTL, destination.Position);
        }

        private float GetAstrogateTime(float distance, float distanceSTL, Vector2 targetPos)
        {
            float angleDiff = Position.AngleToTarget(targetPos) - RotationDegrees;
            if (angleDiff > 180)
                angleDiff = 360 - Position.AngleToTarget(targetPos) + RotationDegrees;

            float rotationTime = angleDiff / RotationRadsPerSecond.ToDegrees().LowerBound(1);
            float distanceFTL  = Math.Max(distance - distanceSTL, 0);
            float travelSTL    = distanceSTL / MaxSTLSpeed.LowerBound(1);
            float travelFTL    = distanceFTL / MaxFTLSpeed.LowerBound(1);

            return (travelFTL + travelSTL + rotationTime + Stats.FTLSpoolTime) / Universe.P.TurnTimer;
        }

        public void TetherToPlanet(Planet p)
        {
            TetheredTo = p;
            TetherOffset = Position - p.Position;
        }

        public Planet GetTether()
        {
            return TetheredTo;
        }

        public Ship Clone()
        {
            return (Ship)MemberwiseClone();
        }

        public float GetCost(Empire empire = null)
        {
            return Stats.GetCost(empire ?? Loyalty);
        }

        public float GetScrapCost()
        {
            return GetCost(Loyalty) / 2f;
        }

        public ShipHull BaseHull => ShipData.BaseHull;

        public void Explore()
        {
            AI.ChangeAIState(AIState.Explore);
            AI.SetPriorityOrder(true);
        }

        public bool InRadius(Vector2 worldPos, float radius)
            => Position.InRadius(worldPos, Radius + radius);

        public bool CheckRangeToTarget(Weapon w, GameObject target)
        {
            if (target == null || !target.Active || target.Health <= 0)
                return false;

            if (engineState == MoveState.Warp)
                return false;

            var targetModule = target as ShipModule;
            Ship targetShip = target as Ship ?? targetModule?.GetParent();
            if (targetShip != null)
            {
                if (targetShip.Dying || !targetShip.Active ||
                    targetShip.Externals.NumModules <= 0)
                    return false;
            }

            float attackRunRange = 50f;
            if (!w.IsBeam && DesiredCombatRange < 2000)
            {
                attackRunRange = STLSpeedLimit;
                if (attackRunRange < 50f)
                    attackRunRange = 50f;
            }

            float range = attackRunRange + w.BaseRange;
            return target.Position.InRadius(w.Module.Position, range);
        }

        // Added by McShooterz
        public bool IsTargetInFireArcRange(Weapon w, GameObject target)
        {
            if (!CheckRangeToTarget(w, target))
                return false;

            if (target is Ship targetShip)
            {
                if (w.TractorDamage > 0 && targetShip.IsTethered)
                    return false;

                if (w.RepulsionDamage > 0 && (targetShip.IsTethered  || targetShip.EnginesKnockedOut))
                    return false;

                if (!AI.IsTargetValid(targetShip))
                    return false;
            }

            ShipModule m = w.Module;
            return RadMath.IsTargetInsideArc(m.Position, target.Position,
                                             Rotation + m.TurretAngleRads, m.FieldOfFire);
        }

        // This is used by Beam weapons and by Testing
        public bool IsInsideFiringArc(Weapon w, Vector2 pickedPos)
        {
            ShipModule m = w.Module;
            return RadMath.IsTargetInsideArc(m.Position, pickedPos,
                                             Rotation + m.TurretAngleRads, m.FieldOfFire);
        }

        public SceneObject GetSO()
        {
            return ShipSO;
        }

        public void ReturnHome()
        {
            AI.OrderReturnHome();
        }

        public float GetMaintCost() => GetMaintCost(Loyalty);

        public float GetMaintCost(Empire empire)
        {
            return ShipMaintenance.GetMaintenanceCost(this, empire, TroopCount);
        }

        public void DoEscort(Ship escortTarget)
        {
            AI.ClearOrders(AIState.Escort);
            AI.EscortTarget = escortTarget;
        }

        public void DoDefense()
        {
            AI.ChangeAIState(AIState.SystemDefender);
        }

        public void OrderToOrbit(Planet orbit, bool clearOrders, MoveOrder order = MoveOrder.Regular)
        {
            AI.OrderToOrbit(orbit, clearOrders, order);
        }

        public void DoExplore()
        {
            AI.OrderExplore();
        }

        public void DoColonize(Planet p, Goal g)
        {
            AI.OrderColonization(p, g);
        }

        // This is used during Saving for ShipSaveData
        public ModuleSaveData[] GetModuleSaveData()
        {
            // probably inactive ship, but we have to serialize these
            // in order to support refit goals
            if (ModuleSlotList.Length == 0)
                return null;

            var slots = new ModuleSaveData[ModuleSlotList.Length];
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                slots[i] = new ModuleSaveData(ModuleSlotList[i]);
            }
            return slots;
        }

        // if enemy ships get within guard mode range, ships will enter combat
        public const float GuardModeRange = 5000;
        public const float HoldPositionRange = 1000; // enter combat at this range
        public const float UnarmedRange = 10000; // also used as evade range

        public float WeaponsMaxRange { get; private set; }
        public float WeaponsMinRange { get; private set; }
        public float WeaponsAvgRange { get; private set; }
        public float DesiredCombatRange { get; private set; }
        public float InterceptSpeed { get; private set; }

        // @return Filtered list of purely offensive weapons
        public Weapon[] OffensiveWeapons => Weapons.Filter(w => w.DamageAmount > 0.1f && !w.TruePD);

        /// <summary>
        /// Get Active weapons, preferring Offensive weapons at first.
        /// If there's no Offensive weapons, it returns all Active weapons.
        /// </summary>
        public Array<Weapon> ActiveWeapons
        {
            get
            {
                var weapons = new Array<Weapon>();
                // prefer offensive weapons:
                for (int i = 0; i < Weapons.Count; ++i) // using raw loops for perf
                {
                    Weapon w = Weapons[i];
                    if (w.Module?.Active == true && w.DamageAmount > 0.1f && !w.TruePD && Ordinance >= w.OrdinanceRequiredToFire)
                    {
                        weapons.Add(w);
                    }
                }

                // maybe we are equipped with Phalanx PD's only?
                // just us any active weapon then
                if (weapons.Count == 0)
                {
                    for (int i = 0; i < Weapons.Count; ++i) // using raw loops for perf
                    {
                        Weapon w = Weapons[i];
                        if (w.Module?.Active == true)
                            weapons.Add(w);
                    }
                }
                return weapons;
            }
        }

        /// <summary>
        /// Gets the weapons ranges.
        /// </summary>
        /// <param name="weapons">The weapons.</param>
        /// <returns></returns>
        float[] GetWeaponsRanges(Array<Weapon> weapons)
        {
            var ranges = new float[weapons.Count];

            for (int i = 0; i < weapons.Count; ++i) // using raw loops for perf
                ranges[i] = weapons[i].GetActualRange(Loyalty);

            return ranges;
        }


        /**
         * Updates the [min, max, avg, desired] weapon ranges based on "real" damage dealing
         * weapons installed, Not utility/repair/truePD
         */

        /// <summary>
        ///   <para>
        /// Updates the weapon ranges.
        /// Updates the [min, max, avg, desired] weapon ranges based on "real" damage dealing
        /// weapons installed, Not utility/repair/truePD.</para>
        ///   <para>Carriers will the use the carrier hangar range for max range.
        /// for min range carriers will use the max range of normal weapons.
        /// </para>
        /// </summary>
        void UpdateWeaponRanges()
        {
            Array<Weapon> weapons = ActiveWeapons;
            float[] ranges = GetWeaponsRanges(weapons);

            // Carriers will use the carrier range for max range.
            // for min range carriers will use the max range of normal weapons.
            if (Carrier.IsPrimaryCarrierRoleForLaunchRange)
            {
                WeaponsMinRange = Math.Max(ranges.Min(), Carrier.HangarRange);
                WeaponsMaxRange = Math.Max(Carrier.HangarRange, ranges.Max());
                float sumRanges = ranges.Sum() + Carrier.HangarRange;
                WeaponsAvgRange = (int)Math.Min(sumRanges / (ranges.Length + 1), ranges.Max());
            }
            else
            {
                WeaponsMinRange = ranges.Min();
                WeaponsMaxRange = ranges.Max();
                WeaponsAvgRange = (int)ranges.Avg();
            }

            DesiredCombatRange = CalcDesiredDesiredCombatRange(ranges, AI?.CombatState ?? CombatState.AttackRuns);
            InterceptSpeed     = CalcInterceptSpeed(weapons);
            MaxWeaponError     = Weapons.FindMax(w => w.BaseTargetError(Level, TargetErrorFocalPoint))?.BaseTargetError(Level, TargetErrorFocalPoint) ?? 0;
        }

        // This is used for previewing range during CombatState change
        // Not performance critical.
        float GetDesiredCombatRangeForState(CombatState state)
        {
            float[] ranges = GetWeaponsRanges(ActiveWeapons);
            return CalcDesiredDesiredCombatRange(ranges, state);
        }

        // NOTE: Make sure to validate TestShipRanges.ShipRanges and TestShipRanges.ShipRangesWithModifiers
        float CalcDesiredDesiredCombatRange(float[] ranges, CombatState state)
        {
            if (ranges.Length == 0)
                return UnarmedRange;

            // for game balancing, so ships won't kite way too far
            // and still have chance to hit while moving
            switch (state)
            {
                case CombatState.GuardMode:    return WeaponsMaxRange * 0.9f;
                case CombatState.Evade:        return UnarmedRange;
                case CombatState.HoldPosition: return WeaponsMaxRange;
                case CombatState.ShortRange:   return WeaponsMinRange * 0.9f;
                case CombatState.Artillery:    return WeaponsMaxRange * 0.9f;
                case CombatState.AssaultShip:
                case CombatState.AttackRuns:
                default:                       return WeaponsAvgRange * 0.9f;
            }
        }

        // This calculates our Ship's interception speed
        //   If we have weapons, then let the weapons do the talking
        //   If no weapons, give max ship speed instead
        float CalcInterceptSpeed(Array<Weapon> weapons)
        {
            // if no offensive weapons, default to ship speed
            if (weapons.Count == 0)
                return MaxSTLSpeed;

            // @note beam weapon speeds need special treatment, since they are currently instantaneous
            float[] speeds = weapons.Select(w => w.IsBeam ? w.GetActualRange(Loyalty) * 1.5f : w.ProjectileSpeed);
            return speeds.Avg();
        }

        public void SetActiveInternalSlotCount(int activeInternalSlots)
        {
            ActiveInternalModuleSlots = activeInternalSlots;
            InternalSlotsHealthPercent = (float)activeInternalSlots / NumInternalSlots;
        }

        // TODO: This needs a performance refactor
        public void UpdateShipStatus(FixedSimTime timeStep)
        {
            if (timeStep.FixedTime > 0f && VelocityMax <= 0f
                && !ShipData.IsShipyard && ShipData.Role <= RoleName.station)
            {
                // rotate Platform and SSP:
                // the randomized rotation part is based on Id, because
                // calling RandomMath.Float is super expensive here
                float addedRotation = 0.003f + ((Id % 10) * 0.0001f);
                Rotation += addedRotation;
            }

            if (timeStep.FixedTime > 0 && (EMPDamage > 0 || EMPDisabled))
                CauseEmpDamage(-EmpRecovery);

            Rotation = Rotation.AsNormalizedRadians();

            if (!EMPDisabled && HasCommand)
            {
                for (int i = 0; i < Weapons.Count; i++)
                    Weapons[i].Update(timeStep);

                for (int i = 0; i < BombBays.Count; i++)
                    BombBays[i].InstalledWeapon.Update(timeStep);
            }

            AI.CombatAI.SetCombatTactics(AI.CombatState);

            // NOTE: need to constantly update HighAlertTimer, using the 1 second update block doesn't work well
            if (HighAlertTimer > 0f)
                HighAlertTimer -= timeStep.FixedTime;

            UpdateStatusOncePerSecond(timeStep);
            UpdatePower(timeStep);
            ShieldPercent = ShieldMax > 0f ? (100f * ShieldPower) / ShieldMax : 0;
            ShipEngines.Update(this);
        }

        void UpdatePower(FixedSimTime timeStep)
        {
            PowerCurrent -= PowerDraw * timeStep.FixedTime;
            if (PowerCurrent < PowerStoreMax)
                PowerCurrent += (PowerFlowMax + PowerFlowMax * (Loyalty?.data.PowerFlowMod ?? 0)) * timeStep.FixedTime;

            if (PowerCurrent <= 0.0f)
            {
                PowerCurrent = 0.0f;
                HyperspaceReturn();
            }

            PowerCurrent = Math.Min(PowerCurrent, PowerStoreMax);
        }

        void UpdateStatusOncePerSecond(FixedSimTime timeStep)
        {
            UpdateTimer -= timeStep.FixedTime;
            if (UpdateTimer <= 0f)
            {
                UpdateTimer += 1f;
                UpdateModulesAndStatus(FixedSimTime.One);
                ExploreCurrentSystem(timeStep);
                ScrambleFightersIfInCombat();
                RecallHangarShipIfTooFarFromCarrier();
                UpdateTractor();
                UpdateSystem();
                UpdateRebaseTarget();
                SecondsAlive += 1;

                if (Carrier.HasHangars)
                    Carrier.HandleHangarShipsByPlayerLaunchButton();
            }
        }

        void UpdateRebaseTarget()
        {
            AI.UpdateRebase();
        }

        void UpdateSystem()
        {
            if (System?.Position.OutsideRadius(Position, System.Radius) == true)
                SetSystem(null);
        }   

        void RecallHangarShipIfTooFarFromCarrier()
        {
            if (IsHangarShip && !InCombat && Position.OutsideRadius(Mothership.Position, Mothership.SensorRange))
                AI.BackToCarrier();
        }

        void UpdateTractor()
        {
            if (TractorDamage > 0 && !BeingTractored)
            {
                TractorDamage = 0;
                ShipStatusChanged = true;
            }

            BeingTractored = false;
        }
        void ScrambleFightersIfInCombat()
        {
            if (Ordinance > 0 && Carrier.HasFighterBays && AI.Target != null && InCombat && !IsSpoolingOrInWarp)
            {
                float distanceToTarget = AI.Target.Position.Distance(Position);
                if (Carrier.IsInHangarLaunchRange(distanceToTarget))
                    Carrier.ScrambleFighters();
            }
        }

        public void UpdateSensors(FixedSimTime timeStep)
        {
            UpdateInfluenceStatus();

            // update our knowledge of the surrounding universe
            KnownByEmpires.Update(timeStep, Loyalty);
            if (Loyalty.isPlayer && IsSubspaceProjector)
                PlayerProjectorHasSeenEmpires.Update(timeStep, Loyalty);

            // scan universe and make decisions for combat
            AI.ScanForTargets(timeStep);
        }

        public void UpdateModulePositions(FixedSimTime timeStep, bool isSystemView, bool forceUpdate = false)
        {
            bool visible = IsVisibleToPlayer;
            if (Active && (AI.BadGuysNear || visible || forceUpdate))
            {
                var a = new ShipModule.UpdateEveryFrameArgs
                {
                    TimeStep = timeStep,
                    ParentX = Position.X,
                    ParentY = Position.Y,
                    // TODO: Figure out a more accurate, yet FAST way to approximate hull height
                    ParentZ = BaseHull.ModelZ.Clamped(0, 200) * -1f,
                    ParentRotation = Rotation,
                    ParentScale = PlanetCrash?.Scale ?? 1f,
                    Cos = RadMath.Cos(Rotation),
                    Sin = RadMath.Sin(Rotation),
                    Tan = (float)Math.Tan(YRotation)
                };

                if (Active)
                {
                    bool enableVisualizeDamage = PlanetCrash == null;
                    for (int i = 0; i < ModuleSlotList.Length; ++i)
                    {
                        ShipModule m = ModuleSlotList[i];
                        m.UpdateEveryFrame(a);
                        if (enableVisualizeDamage && m.CanVisualizeDamage)
                            m.UpdateDamageVisualization(timeStep, a.ParentScale, visible);
                    }
                }
                else
                {
                    for (int i = 0; i < ModuleSlotList.Length; ++i)
                    {
                        ModuleSlotList[i].UpdateEveryFrame(a);
                    }
                }
            }
        }

        void UpdateModulesAndStatus(FixedSimTime timeSinceLastUpdate)
        {
            for (int i = 0; i < ModuleSlotList.Length; ++i)
                ModuleSlotList[i].Update(timeSinceLastUpdate);

            if (ShouldRecalculatePower) // must be before ShipStatusChange
                RecalculatePower();
            
            if (OrdAddedPerSecond > 0f)
                ChangeOrdnance(OrdAddedPerSecond); // Add ordnance

            if (OrdnanceChanged)
            {
                OrdnanceChanged = false;
                ShipStatusChanged = true;
            }

            if (ShipStatusChanged)
                ShipStatusChange();

            //Power draw based on warp
            if (!IsInFriendlyProjectorRange && engineState == MoveState.Warp)
                PowerDraw = NetPower.NetWarpPowerDraw;
            else if (engineState != MoveState.Warp)
                PowerDraw = NetPower.NetSubLightPowerDraw;
            else
                PowerDraw = NetPower.NetWarpPowerDraw;

            if (InCombat
                || ShieldPower < ShieldMax
                || engineState == MoveState.Warp)
            {
                ShieldPower = 0.0f;
                foreach (ShipModule shield in GetShields())
                {
                    ShieldPower = (ShieldPower + shield.ShieldPower).Clamped(0, ShieldMax);
                }
            }

            // Update max health if needed
            int latestRevision = EmpireHullBonuses.GetBonusRevisionId(Loyalty);
            if (MaxHealthRevision != latestRevision)
            {
                MaxHealthRevision = latestRevision;
                HealthMax = RecalculateMaxHealth();
            }

            // return home if it is a defense ship
            if (!InCombat && IsHomeDefense && !HomePlanet.SpaceCombatNearPlanet && AI.State != AIState.ReturnHome)
                ReturnHome();

            // Ship Repair
            if (HealthPercent < 0.9999999f)
                Repair(timeSinceLastUpdate);

            UpdateResupply();
            UpdateTroops(timeSinceLastUpdate);

            if (TimeSinceLastDamage > 10f)
                LastDamagedBy = null; // we need to clear this to avoid memory leaks

            if (!AI.BadGuysNear)
                Universe.Shields?.RemoveShieldLights(GetActiveShields());
        }

        public void UpdateResupply()
        {
            if (Health < 1f)
                return;
            
            Carrier.SupplyShuttles.ProcessSupplyShuttles(AI.GetSensorRadius());

            ResupplyReason resupplyReason = Supply.Resupply();
            if (resupplyReason != ResupplyReason.NotNeeded)
            {
                if (Mothership?.Active == true)
                {
                    AI.OrderReturnToHangar(); // dealing with hangar ships needing resupply
                    return;
                }

                if (DesignRole == RoleName.drone)
                { 
                    AI.OrderScuttleShip(); // drones just scuttle if they have no mothership to resupply
                    return;
                }
            }

            AI.ProcessResupply(resupplyReason);
        }

        public bool IsSuitableForPlanetaryRearm()
        {
            if (InCombat
                || !Active
                || OrdnancePercent >= 1
                || IsHangarShip
                || IsPlatformOrStation && TetheredTo?.Owner == Loyalty
                || AI.OrbitTarget?.Owner == Loyalty
                || AI.OrbitTarget?.Owner?.IsAlliedWith(Loyalty) == true
                || AI.State is AIState.Resupply or AIState.Scrap or AIState.Refit 
                || IsHomeDefense 
                || IsSupplyShuttle)
            {
                return false;
            }

            return true;
        }

        public bool IsTroopShipAndRebasingOrAssaulting(Planet p)
        {
            return (DesignRole == RoleName.troop || DesignRole == RoleName.troopShip)
                   && AI.OrderQueue.Any(g => (g.Plan == ShipAI.Plan.Rebase || g.Plan == ShipAI.Plan.LandTroop) && g.TargetPlanet == p);
        }

        public bool IsSupplyShuttle => ShipData.IsSupplyShuttle;

        public int RefitCost(Ship newShip)
        {
            return RefitCost(newShip.ShipData);
        }

        public int RefitCost(IShipDesign newShip)
        {
            if (Loyalty.IsFaction)
                return 0;

            float oldShipCost = GetCost(Loyalty);

            // FB: Refit works normally only for ship of the same hull. But freighters can be replaced to other hulls by the auto trade.
            // So replacement cost is higher, the old ship cost is halved, just like scrapping it.
            if (ShipData.Hull != newShip.Hull)
                oldShipCost /= 2;

            float newShipCost = newShip.GetCost(Loyalty);
            int cost          = Math.Max((int)(newShipCost - oldShipCost), 0);
            return cost + (int)(10 * Universe.ProductionPace); // extra refit cost: accord for GamePace;
        }

        public bool IsTethered => TetheredTo != null;

        private float CurrentStrength = -1.0f;

        /// <summary>
        /// Gets the current strength of the ship, which is dynamic (active modules)
        /// </summary>
        /// <returns></returns>
        public float GetStrength()
        {
            return CurrentStrength;
        }

        //Added by McShooterz: Refactored by CG
        public void AddKill(Ship killed)
        {
            ++Kills;
            if (Loyalty == null)
                return;

            float exp   = killed.ExperienceShipIsWorth();
            exp        += exp * Loyalty.data.ExperienceMod;
            Experience += exp;
            ConvertExperienceToLevel();

            if (killed.Loyalty?.WeArePirates ?? false)
                killed.Loyalty.Pirates.KillBaseReward(Loyalty, killed);
        }

        public float ExperienceShipIsWorth()
        {
            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(this);
            float exp = killedExpSettings.ExpPerLevel * (1 + Level);
            return exp;
        }

        private void ConvertExperienceToLevel()
        {
            ShipRole.Race ownerExpSettings = ShipRole.GetExpSettings(this);
            while (true)
            {
                if (Experience <= 0) return;
                float experienceThreshold = ownerExpSettings.ExpPerLevel * (1 + Level);
                if (experienceThreshold <= 0) return;
                if (Experience < experienceThreshold) return;
                AddToShipLevel(1);
                Experience -= experienceThreshold;
            }
        }

        public void AddToShipLevel(int amountToAdd) => Level = (Level + amountToAdd).Clamped(0,10);

        public void UpdateEmpiresOnKill(Ship killedShip)
        {
            Loyalty.WeKilledTheirShip(killedShip.Loyalty, killedShip);
            killedShip.Loyalty.TheyKilledOurShip(Loyalty, killedShip);
        }

        void NotifyPlayerIfDiedExploring()
        {
            if (Loyalty.isPlayer && AI.IsExploring)
            {
                Universe.Screen.NotificationManager.AddExplorerDestroyedNotification(this);
            }
        }

        // Base chance to evade and exploding ship
        public int ExplosionEvadeBaseChance()
        {
            switch (ShipData.HullRole)
            {
                default:
                case RoleName.drone:      return 80;
                case RoleName.scout:      return 80;
                case RoleName.fighter:    return 70;
                case RoleName.corvette:   return 60;
                case RoleName.frigate:    return 40;
                case RoleName.cruiser:    return 20;
                case RoleName.battleship: return 10;
                case RoleName.capital: 
                case RoleName.station:    return 0;
            }
        }

        void AddExplosionEffect(bool addWarpExplode)
        {
            var position = new Vector3(Position.X, Position.Y, -100f);

            float boost = GlobalStats.Defaults.ShipExplosionVisualIncreaser;
            float diameter = 2f * Radius * (ShipData.EventOnDeath?.NotEmpty() == true ? 3 : 1);
            float explosionSize = PlanetCrash != null ? diameter * 0.05f : diameter * boost;

            // the ShipExplode effect itself has a bit of empty space
            // so the effect needs to be doubled in size, or in some case more
            switch (ShipData.HullRole)
            {
                case RoleName.scout:      explosionSize *= 2; break;
                case RoleName.fighter:    explosionSize *= 2; break;
                case RoleName.corvette:   explosionSize *= 2; break;
                case RoleName.frigate:    explosionSize *= 2; break;
                case RoleName.battleship: explosionSize *= 3; break;
                case RoleName.capital:    explosionSize *= 3; break;
                case RoleName.cruiser:    explosionSize *= 3; break;
                case RoleName.station:    explosionSize *= 4; break;
                default:                  explosionSize *= 3; break;
            }

            ExplosionManager.AddExplosion(Universe.Screen, position, Velocity, explosionSize, 12f, ExplosionType.Ship);

            if (PlanetCrash == null && addWarpExplode)
            {
                ExplosionManager.AddExplosion(Universe.Screen, position, Velocity, explosionSize * 1.75f, 12f, ExplosionType.Warp);
            }
        }

        float GetExplosionDamage()
        {
            float damage = 0;
            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule m = ModuleSlotList[i];
                damage += m.GetExplosionDamageOnShipExplode();
            }

            damage += PowerCurrent + Ordinance + Health*0.05f;
            return damage.LowerBound(Radius * 10);
        }

        public void InstantKill()
        {
            Die(this, false);
            Die(this, true);
        }

        // cleanupOnly: for tumbling ships that are already dead
        public override void Die(GameObject source, bool cleanupOnly)
        {
            if (!Active)
                return; // already dead

            var pSource = source as Projectile;
            // Mostly for supply ships to remove incoming supply
            AI.ChangeAIState(AIState.AwaitingOrders);
            ReallyDie = cleanupOnly || WillShipDieNow(pSource);
            if (Dying && !ReallyDie)
                return; // planet crash or tumble

            OnShipDie(pSource);
            Mothership?.OnLaunchedShipDie(this);
            QueueTotalRemoval(); // sets Active=false
            
            bool visible = IsVisibleToPlayer;
            Loyalty.AI.ExpansionAI.RemoveExplorationTargetFromList(AI.ExplorationTarget);
            Carrier.ScuttleHangarShips();
            NotifyPlayerIfDiedExploring();
            Loyalty.TryAutoRequisitionShip(Fleet, this);
            CreateExplosionEffects(visible: visible, cleanupOnly: cleanupOnly);
        }

        bool WillShipDieNow(Projectile proj)
        {
            if (Dying) // already dying, no need to calc explosion chances again
                return false;

            if (proj != null && proj.Explodes && proj.DamageAmount > (SurfaceArea/2f).LowerBound(200))
                return true;

            if (Loyalty.Random.RollDice(35))
            {
                // 35% the ship will not explode immediately, but will start tumbling out of control
                // we mark the ship as dying and the main update loop will set reallyDie
                if (PlanetCrash.GetPlanetToCrashOn(this, out Planet planet))
                {
                    Dying = true;
                    PlanetCrash = new PlanetCrash(planet, this);
                }

                if (InFrustum)
                {
                    Dying = true;
                    DieTimer = Loyalty.Random.Int(4, 8);
                }

                if (Dying)
                {
                    DieRotation.X = Loyalty.Random.Float(-1f, 1f) * 50f / SurfaceArea;
                    DieRotation.Y = Loyalty.Random.Float(-1f, 1f) * 50f / SurfaceArea;
                    DieRotation.Z = Loyalty.Random.Float(-1f, 1f) * 50f / SurfaceArea;
                    return false; // ship will really die later
                }
            }

            return true;
        }

        void CreateExplosionEffects(bool visible, bool cleanupOnly)
        {
            if (!cleanupOnly && visible)
            {
                string dieSoundEffect;
                if      (SurfaceArea < 80)  dieSoundEffect ="sd_explosion_ship_det_small";
                else if (SurfaceArea < 250) dieSoundEffect = "sd_explosion_ship_det_medium";
                else                        dieSoundEffect = "sd_explosion_ship_det_large";

                GameAudio.PlaySfxAsync(dieSoundEffect, SoundEmitter);
            }

            if (!HasExploded)
            {
                HasExploded = true;
                if (visible)
                    AddExplosionEffect(addWarpExplode: cleanupOnly);

                if (PlanetCrash == null)
                {
                    float explosionDamage = GetExplosionDamage();
                    Universe.Spatial.ShipExplode(this, explosionDamage, Position, Radius + explosionDamage / 500);
                    if (visible)
                    {
                        // Added by RedFox - spawn flaming spacejunk when a ship dies
                        int howMuchJunk = (int)(Radius * 0.05f);
                        Vector2 pos = Position.GenerateRandomPointOnCircle(Radius / 2, Loyalty.Random);
                        SpaceJunk.SpawnJunk(Universe, howMuchJunk, pos, Velocity, this,
                                            maxSize: Radius * 0.1f, ignite: false);
                    }
                }
            }
        }

        void CreateEventOnDeath()
        {
            if (ShipData.EventOnDeath != null)
            {
                var evt = ResourceManager.EventsDict[ShipData.EventOnDeath];
                Universe.Screen.ScreenManager.AddScreen(
                    new EventPopup(Universe.Screen, Universe.Player, evt, evt.PotentialOutcomes[0], true));
            }
        }

        void DamageRelationsOnDeath(Projectile pSource)
        {
            if (pSource?.Owner != null && !pSource.Owner.Loyalty.IsAlliedWith(Loyalty))
            {
                float amount = 1f;
                if (ResourceManager.ShipRoles.ContainsKey(ShipData.Role))
                    amount = ResourceManager.ShipRoles[ShipData.Role].DamageRelations;

                Loyalty.DamageRelationship(pSource.Owner.Loyalty, "Destroyed Ship", amount, null);
            }
        }

        public void SetReallyDie()
        {
            ReallyDie = true;
        }

        public void SetDieTimer(float value)
        {
            DieTimer = value;
        }

        public void RemoveTether()
        {
            TetheredTo = null;
        }

        /// <summary>
        /// Sets ship as Inactive and marks it for removal from UniverseObjectManager
        /// during next Objects.Update()
        /// </summary>
        public void QueueTotalRemoval()
        {
            Active = false;
            TetheredTo?.RemoveFromOrbitalStations(this);
            AI.ClearOrdersAndWayPoints(); // This calls immediate Dispose() on Orders that require cleanup
        }

        public void RemoveFromUniverseUnsafe()
        {
            AI?.Reset();

            var carrier = Mothership?.Carrier;
            if (IsHangarShip && carrier?.AllActiveHangars != null)
            {
                foreach (ShipModule shipModule in carrier.AllActiveHangars)
                    if (shipModule.TryGetHangarShip(out Ship ship) && ship == this)
                        shipModule.SetHangarShip(null);
            }
            
            Carrier?.Dispose();

            var slots = ModuleSlotList;
            ModuleSlotList = Empty<ShipModule>.Array;
            for (int i = 0; i < slots.Length; ++i)
                slots[i]?.Dispose();
            
            DestroyThrusters();

            BombBays.Clear();
            OurTroops.Clear();
            HostileTroops.Clear();
            RepairBeams = null;
            PlanetCrash = null;

            ((IEmpireShipLists)Loyalty).RemoveShipAtEndOfTurn(this);
            RemoveFromPoolAndFleet(clearOrders: false/*already cleared*/);
            RemoveTether();
            RemoveSceneObject();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Ship() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            if (ModuleSlotList != null && ModuleSlotList.Length != 0)
            {
                RemoveFromUniverseUnsafe();
            }

            // It's extremely important we manually clear these
            // The .NET GC is not able to handler all the cyclic references
            Mem.Dispose(ref SupplyLock);
            AI?.Dispose();
            AI = null;

            Weapons = null;
            SoundEmitter = null;
            BombBays = null;
            TetheredTo = null;
            Fleet = null;
            ShipData = null;
            Mothership = null;
            JumpSfx?.Destroy();
            KnownByEmpires = null;
            PlayerProjectorHasSeenEmpires = null;
            PlanetCrash = null;
            HomePlanet = null;
            RemoveSceneObject();

            Stats?.Dispose();
            Cargo = null;
            ModuleSlotList = Empty<ShipModule>.Array;
            ShipEngines = null;
            TradeRoutes = null;
            OurTroops = null;
            HostileTroops = null;
            LoyaltyTracker = default;
            LastDamagedBy = null;
        }

        public void UpdateShields()
        {
            // NOTE: this can happen with serialized dead ships which we need to keep around in serialized Goals
            if (Modules.Length == 0)
                return;

            float shieldPower = 0.0f;
            foreach (ShipModule shield in GetShields())
                shieldPower += shield.ShieldPower;

            ShieldPower = shieldPower.UpperBound(ShieldMax);
        }

        public enum MoveState
        {
            Sublight,
            Warp
        }

        float RecalculateMaxHealth()
        {
            #if DEBUG
                bool maxHealthDebug = VanityName == "MerCraft";
                if (maxHealthDebug) Log.Info($"Health was {Health} / {HealthMax}   ({Loyalty.data.Traits.ModHpModifier})");
            #endif

            float healthMax = 0;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
                healthMax += ModuleSlotList[i].ActualMaxHealth;

            #if DEBUG
                if (maxHealthDebug) Log.Info($"Health is  {Health} / {HealthMax}");
            #endif
            return healthMax;
        }

        // For Unit Tests
        public ShipModule TestGetModule(string uid)
        {
            foreach (ShipModule module in ModuleSlotList)
            {
                if (module.UID == uid)
                    return module;
            }
            return null;
        }

        public float CalculateShipStrength()
        {
            if (IsMeteor)
                return 0;

            float offense   = 0;
            float defense   = 0;
            int weaponArea  = 0;
            int hangarArea  = 0;
            bool hasWeapons = false;
            TotalDps = 0;

            for (int i = 0; i < ModuleSlotList.Length; i++ )
            {
                ShipModule m = ModuleSlotList[i];
                if (m.Active)
                {
                    if (m.InstalledWeapon != null)
                    {
                        weaponArea += m.Area;
                        TotalDps   += m.InstalledWeapon.DamagePerSecond;
                        hasWeapons = true;
                    }

                    if (m.IsTroopBay || m.IsSupplyBay || m.MaximumHangarShipSize > 0)
                        hangarArea += m.Area;

                    offense += m.CalculateModuleOffense();
                    defense += m.CalculateModuleOffenseDefense(SurfaceArea);
                }
            }

            int offensiveArea = weaponArea + hangarArea;
            if (offensiveArea == 0
                && (IsDefaultTroopShip || IsSupplyShuttle || DesignRole == RoleName.scout
                    || IsSubspaceProjector || IsFreighter || IsConstructor))
            {
                return 0;
            }

            if (IsPlatformOrStation) 
                offense /= 2;

            if (!Carrier.HasFighterBays && !hasWeapons) 
                offense = 0f;

            return ShipBuilder.GetModifiedStrength(SurfaceArea, offensiveArea, offense, defense);
        }

        // UI statistics, show average repair per second

        public void ApplyModuleHealthTechBonus(float bonus)
        {
            HealthMax = RecalculateMaxHealth();
            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule module = ModuleSlotList[i];
                module.Repair(module.Health + module.Health * bonus);
            }
        }

        public void UpdatePackDamageModifier()
        {
            float modifier = -0.15f + 0.01f * AI.FriendliesNearby.Length;
            PackDamageModifier = modifier.Clamped(-0.15f, 0.3f);
        }

        // prefers VanityName, otherwise uses Name
        public string ShipName => VanityName.NotEmpty() ? VanityName : Name;

        public override string ToString() =>
            $"Ship:{Id} {ShipData?.Role.ToString() ?? "disposed"} '{ShipName}' {Loyalty.data.ArchetypeName} {(Active?"Active":"DEAD")} Pos:{{{Position.X.String(2)},{Position.Y.String(2)}}} {System} State:{AI?.State} Health:{(HealthPercent*100f).String()}%";

        // TODO: this is duplicated
        public bool ShipIsGoodForGoals()
        {
            if (!Active)
                return false;
            float minRange = Universe?.P.MinAcceptableShipWarpRange ?? GlobalStats.Defaults.MinAcceptableShipWarpRange;
            bool warpTimeGood = IsWarpRangeGood(minRange);
            return warpTimeGood;
        }

        public Status ToShipStatus(float valueToCheck, float maxValue)
        {
            if (maxValue <= 0) return Status.NotApplicable;
            if (valueToCheck > maxValue)
            {
                //Log.Info("MaxValue of check as greater than value to check");
                return Status.NotApplicable;
            }

            float ratio = 0.5f + ShipStatusCount * valueToCheck / maxValue;
            ratio = ratio.Clamped(1, ShipStatusCount);
            return (Status)(int)ratio;
        }

        // if the shipstatus enum is added to then "6" will need to be changed.
        // it should count all but "NotApplicable"
        const int ShipStatusCount = 6;
    }

    public enum Status
    {
        Critical = 1,
        Poor,
        Average,
        Good,
        Excellent,
        Maximum,
        NotApplicable
    }
}

