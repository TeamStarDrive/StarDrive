using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Debug;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships.DataPackets;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ship_Game.Ships
{
    using static ShipMaintenance;

    public partial class Ship : GameplayObject, IDisposable
    {
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! Ship must not be serialized! Add [XmlIgnore][JsonIgnore] to `public Ship XXX;` PROPERTIES/FIELDS. {this}");

        public static Array<Ship> GetShipsFromGuids(Array<Guid> guids)
        {
            var ships = new Array<Ship>();
            for (int i = 0; i < guids.Count; i++)
            {
                Ship ship = Empire.Universe.Objects.FindShip(guids[i]);
                if (ship != null)
                    ships.AddUnique(ship);
            }
            return ships;
        }

        public string VanityName                = ""; // user modifiable ship name. Usually same as Ship.Name
        public Array<Rectangle> AreaOfOperation = new Array<Rectangle>();

        public float RepairRate  = 1f;
        public float SensorRange = 20000f;
        public float MaxBank     = 0.5236f;

        public Vector2 projectedPosition;
        readonly Array<Thruster> ThrusterList = new Array<Thruster>();

        public Array<Weapon> Weapons = new Array<Weapon>();
        float JumpTimer = 3f;
        public AudioEmitter SoundEmitter = new AudioEmitter();
        public Vector2 ScreenPosition;
        public float ScuttleTimer = -1f;
        public Vector2 FleetOffset;
        public Vector2 RelativeFleetOffset;
        ShipModule[] Shields;
        ShipModule[] Amplifiers;
        public Array<ShipModule> BombBays = new Array<ShipModule>();
        public CarrierBays Carrier;
        public ShipResupply Supply;
        public bool shipStatusChanged;
        public Guid guid = Guid.NewGuid();
        public bool AddedOnLoad;
        public bool IsPlayerDesign;
        public bool IsSupplyShip;
        public bool IsReadonlyDesign;
        public bool isColonyShip;
        public bool HasRegeneratingModules;
        Planet TetheredTo;
        public Vector2 TetherOffset;
        public Guid TetherGuid;
        public float EMPDamage { get; private set; }
        public Fleet fleet;
        public float yRotation;
        public float MechanicalBoardingDefense;
        public float TroopBoardingDefense;
        public float ECMValue;
        public ShipData shipData;
        public int kills;
        public float experience;
        public bool EnginesKnockedOut;
        public float InCombatTimer;
        public bool IsTurning { get; private set; }
        public float InhibitionRadius;
        public bool IsPlatform;
        public bool IsGuardian; // Remnant Guardian created at game start
        SceneObject ShipSO;
        public bool ManualHangarOverride;
        public Ship Mothership;
        public string Name;   // name of the original design of the ship, eg "Subspace Projector". Look at VanityName
        public float PackDamageModifier { get; private set; }
        public Empire loyalty;
        public int SurfaceArea;
        public float Ordinance { get; private set; } // FB: use ChanceOrdnance function to control Ordnance
        public float OrdinanceMax;
        public ShipAI AI { get; private set; }

        public double shield_percent;
        public float armor_max;
        public float shield_max;
        public float shield_power;
        public int InternalSlotCount;       // total number of internal slots (@todo this should be in ShipTemplate !!)
        public int ActiveInternalSlotCount; // active slots have Health > 0
        public float PowerCurrent;
        public float PowerFlowMax;
        public float PowerStoreMax;
        public float PowerDraw;
        public Power NetPower { get; private set; }
        public bool FromSave;
        public bool HasRepairModule;
        readonly AudioHandle JumpSfx = new AudioHandle();

        public int Level;
        int MaxHealthRevision;
        public float HealthMax { get; private set; }
        public int TroopCapacity;
        public float OrdAddedPerSecond;
        public float ShieldRechargeTimer;
        public bool InCombat;
        public float xRotation;
        public float ScreenRadius;
        public bool ShouldRecalculatePower;
        public bool Deleted;
        private float BonusEMP_Protection;
        public bool inSensorRange => KnownByEmpires.KnownByPlayer;
        public KnownByEmpire KnownByEmpires;
        public KnownByEmpire HasSeenEmpires;
        public bool EMPdisabled;
        private float updateTimer;
        public float HealPerTurn;
        public float InternalSlotsHealthPercent; // number_Alive_Internal_slots / number_Internal_slots
        Vector3 DieRotation;
        private float dietimer;
        public float BaseStrength;
        public bool dying;
        private bool reallyDie;
        private bool HasExploded;
        public float FTLSpoolTime;

        public Array<ShipModule> RepairBeams = new Array<ShipModule>();
        public bool hasRepairBeam;
        public bool hasCommand;
        public int SecondsAlive { get; private set; } // FB - for scrap loop warnings

        public ReaderWriterLockSlim supplyLock = new ReaderWriterLockSlim();
        public int TrackingPower;
        public int TargetingAccuracy;
        public override bool ParentIsThis(Ship ship) => this == ship;
        public float BoardingDefenseTotal => MechanicalBoardingDefense + TroopBoardingDefense;

        public float FTLModifier { get; private set; } = 1f;
        public float BaseCost    { get; private set; }
        public Planet HomePlanet { get; private set; }

        public Weapon FastestWeapon => Weapons.FindMax(w => w.ProjectileSpeed);

        public bool IsDefaultAssaultShuttle => loyalty.data.DefaultAssaultShuttle == Name || loyalty.BoardingShuttle.Name == Name;
        public bool IsDefaultTroopShip      => loyalty.data.DefaultTroopShip == Name;
        public bool IsDefaultTroopTransport => IsDefaultTroopShip || IsDefaultAssaultShuttle;
        public bool IsSubspaceProjector => Name == "Subspace Projector";
        public bool HasBombs => BombBays.Count > 0;

        public bool IsConstructor
        {
            get => DesignRole == ShipData.RoleName.construction;
            set => DesignRole = value ? ShipData.RoleName.construction : GetDesignRole();
        }

        Status FleetCapableStatus;
        public bool CanTakeFleetMoveOrders() => 
            FleetCapableStatus == Status.Good && ShipEngines.EngineStatus >= Status.Poor;

        void SetFleetCapableStatus()
        {
            if (!EMPdisabled && !InhibitedByEnemy)
            {
                switch (AI.State)
                {
                    case AIState.Resupply:
                    case AIState.Refit:
                    case AIState.Scrap:
                    case AIState.Scuttle:
                        FleetCapableStatus = Status.Poor;
                        break;
                    default:
                        FleetCapableStatus = Status.Good;
                        break;
                }
            }
            else
                FleetCapableStatus = Status.Poor;
        }

        public bool CanTakeFleetOrders => FleetCapableStatus == Status.Good;

        // This bit field marks all the empires which are influencing
        // us within their borders via Subspace projectors

        // Somewhat ugly, but optimized book-keeping of projector influences
        // for every ship.
        // Each bit in the bitfield marks whether an empire is influencing us or not

        public bool InsideAreaOfOperation(Planet planet)
        {
            if (AreaOfOperation.IsEmpty)
                return true;

            foreach (Rectangle ao in AreaOfOperation)
                if (ao.HitTest(planet.Center))
                    return true;

            return false;
        }

        public void PiratePostChangeLoyalty()
        {
            if (loyalty.WeArePirates)
            {
                if (IsSubspaceProjector)
                    ScuttleTimer = 10;
                else
                    AI.OrderPirateFleeHome();
            }
        }

        public void UpdateHomePlanet(Planet planet)
        {
            HomePlanet = planet;
        }

        public float EmpTolerance  => SurfaceArea + BonusEMP_Protection;
        public float EmpRecovery   => InCombat ? 1 + BonusEMP_Protection / 1000 : 20 + BonusEMP_Protection / 50;
        public float HealthPercent => Health / HealthMax;

        public void DebugDamage(float percent)
        {
            percent = percent.Clamped(0f, 1f);
            foreach (ShipModule module in ModuleSlotList)
                module.DebugDamage(percent);
        }

        public ShipData.RoleName DesignRole { get; private set; }
        public ShipData.RoleType DesignRoleType => ShipData.ShipRoleToRoleType(DesignRole);
        public string DesignRoleName            => ShipData.GetRole(DesignRole);

        public SubTexture GetTacticalIcon()
        {
            if (IsConstructor)
                return ResourceManager.Texture("TacticalIcons/symbol_construction");

            string roleName = DesignRole.ToString();
            string iconName = "TacticalIcons/symbol_";
            return ResourceManager.TextureOrNull(iconName + roleName) ??
                ResourceManager.TextureOrDefault(iconName + shipData.HullRole, "TacticalIcons/symbol_construction");
        }

        float GetYBankAmount(FixedSimTime timeStep)
        {
            float yBank = RotationRadiansPerSecond * timeStep.FixedTime;
            switch (shipData.Role)
            {
                default:
                    return yBank;
                case ShipData.RoleName.drone:
                case ShipData.RoleName.scout:
                case ShipData.RoleName.fighter:
                    return yBank * 2f;
            }
        }

        public bool IsPlatformOrStation => shipData.Role == ShipData.RoleName.platform || shipData.Role == ShipData.RoleName.station;
        public bool IsStation           => shipData.Role == ShipData.RoleName.station && !shipData.IsShipyard;

        public void CauseEmpDamage(float empDamage) // FB - also used for recover EMP
        {
            EMPDamage   = (EMPDamage + empDamage).Clamped(0, 10000f.LowerBound(EmpTolerance*10));
            EMPdisabled = EMPDamage > EmpTolerance;
        }

        public void CausePowerDamage(float powerDamage) => PowerCurrent = (PowerCurrent - powerDamage).Clamped(0, PowerStoreMax);
        public void AddPower(float powerAcquired)       => PowerCurrent = (PowerCurrent + powerAcquired).Clamped(0, PowerStoreMax);

        public void CauseTroopDamage(float troopDamageChance)
        {
            if (HasOurTroops)
            {
                if (UniverseRandom.RollDice(troopDamageChance) && GetOurFirstTroop(out Troop first))
                {
                    float damage = 1;
                    first.DamageTroop(this, ref damage);
                }
            }
            else if (MechanicalBoardingDefense > 0f)
            {
                if (RandomMath.RollDice(troopDamageChance))
                    MechanicalBoardingDefense -= 1f;
            }
        }

        public void CauseRepulsionDamage(Beam beam)
        {
            if (IsTethered || EnginesKnockedOut)
                return;
            if (beam.Owner == null || beam.Weapon == null)
                return;
            Vector2 repulsion = (Center - beam.Owner.Center) * beam.Weapon.RepulsionDamage;
            ApplyForce(repulsion);
        }

        public void CauseMassDamage(float massDamage, bool hittingShields)
        {
            if (IsTethered || EnginesKnockedOut)
                return;

            Mass += hittingShields ? massDamage/2 : massDamage;
            UpdateMaxVelocity();
            shipStatusChanged = true;
        }

        public void CauseRadiationDamage(float damage)
        {
            if (IsInWarp)
                damage *= 0.5f; // some protection while in warp

            GameplayObject damageCauser = this; // @todo We need a way to do environmental damage
            var damagedShields = new HashSet<ShipModule>();

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.isExternal) // only apply radiation to outer modules
                    continue;

                if (IsCoveredByShield(module, out ShipModule shield))
                {
                    // only damage shields once, depending on their radius and their energy resistance
                    if (!damagedShields.Contains(shield))
                    {
                        float damageAbsorb = 1 - shield.shield_energy_resist;
                        shield.Damage(damageCauser, damage * damageAbsorb * shield.ShieldHitRadius);
                        damagedShields.Add(shield);
                    }
                }
                else
                {
                    // again, damage also depends on module radius and their energy resistance
                    float damageAbsorb = 1 - module.EnergyResist;
                    module.Damage(damageCauser, damage * damageAbsorb * module.Radius);
                    if (InFrustum && Empire.Universe?.IsShipViewOrCloser == true)
                    {
                        // visualize radiation hits on external modules
                        for (int j = 0; j < 50; j++)
                            Empire.Universe.sparks.AddParticleThreadB(module.GetCenter3D, Vector3.Zero);
                    }
                }
            }
        }

        public override bool IsAttackable(Empire attacker, Relationship attackerToUs)
        {
            if (attackerToUs.AttackForBorderViolation(attacker.data.DiplomaticPersonality, loyalty, attacker, IsFreighter)
                && IsInBordersOf(attacker))
            {
                return true;
            }

            if (attackerToUs.AttackForTransgressions(attacker.data.DiplomaticPersonality))
            {
                return true;
            }

            SolarSystem system = System;
            if (system != null)
            {
                if (attackerToUs.WarnedSystemsList.Contains(system.guid))
                    return true;

                if (DesignRole == ShipData.RoleName.troop &&
                    attacker.GetOwnedSystems().ContainsRef(system))
                    return true;
            }

            return false;
        }

        // Level 5 crews can use advanced targeting which even predicts acceleration
        public bool CanUseAdvancedTargeting => Level >= 5;

        public override Vector2 JammingError()
        {
            if (CombatDisabled)
                return Vector2.Zero;

            Vector2 error = default;
            if (ECMValue > 0)
                error += RandomMath2.Vector2D(ECMValue * 80f);

            if (loyalty.data.Traits.DodgeMod > 0)
                error += RandomMath2.Vector2D(loyalty.data.Traits.DodgeMod * 80f);

            return error;
        }

        public bool CanBeScrapped  => Mothership == null && HomePlanet == null;
        public bool CombatDisabled => EMPdisabled || dying || !Active || !hasCommand;

        public bool SupplyShipCanSupply => Carrier.HasSupplyBays && OrdnanceStatus > Status.Critical
                                                                 && OrdnanceStatus != Status.NotApplicable;

        public Status OrdnanceStatus => OrdnanceStatusWithincoming(0);

        public Status OrdnanceStatusWithincoming(float incomingAmount)
        {
            if (IsInWarp
                || AI.State == AIState.Scrap
                || AI.State == AIState.Resupply
                || AI.State == AIState.Refit || Mothership != null
                || shipData.Role == ShipData.RoleName.supply
                || (shipData.HullRole < ShipData.RoleName.fighter && shipData.HullRole != ShipData.RoleName.station)
                || OrdinanceMax < 1
                || (IsTethered && shipData.HullRole == ShipData.RoleName.platform))
                return Status.NotApplicable;

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
                return bomb.OrdinanceRequiredToFire / bomb.fireDelay;
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

        public bool DoingSystemDefense
        {
            get => loyalty.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(this);
            set
            {
                //added by gremlin Toggle Ship System Defense.
                if (EmpireManager.Player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(this))
                {
                    EmpireManager.Player.GetEmpireAI().DefensiveCoordinator.Remove(this);
                    AI.ClearOrders();
                    return;
                }
                EmpireManager.Player.GetEmpireAI().DefensiveCoordinator.AddShip(this);
                AI.State = AIState.SystemDefender;
            }
        }

        public bool DoingScrap
        {
            get => AI.State == AIState.Scrap;
            set => AI.OrderScrapShip();
        }

        public bool DoingRefit
        {
            get => AI.State == AIState.Refit;
            set => Empire.Universe.ScreenManager.AddScreenDeferred(new RefitToWindow(Empire.Universe, this));
        }

        public bool IsInhibitedByUnfriendlyGravityWell
        {
            get
            {
                // friendly projectors disable gravity wells
                if (IsInFriendlyProjectorRange)
                    return false;

                Planet planet = System?.IdentifyGravityWell(this);
                return planet != null;
            }
        }

        // Calculates estimated trip time by turns
        public float GetAstrograteTimeTo(Planet destination)
        {
            float distance    = Center.Distance(destination.Center);
            float distanceSTL = destination.GravityWellForEmpire(loyalty);
            Planet planet     = System?.IdentifyGravityWell(this); // Get the gravity well owner if the ship is in one

            if (planet != null && !IsInFriendlyProjectorRange)
                distanceSTL += planet.GravityWellRadius;

            return GetAstrogateTime(distance, distanceSTL, destination.Center);
        }

        public float GetAstrogateTimeBetween(Planet origin, Planet destination)
        {
            float distance    = origin.Center.Distance(destination.Center);
            float distanceSTL = destination.GravityWellForEmpire(loyalty) + origin.GravityWellForEmpire(loyalty);

            return GetAstrogateTime(distance, distanceSTL, destination.Center);
        }

        private float GetAstrogateTime(float distance, float distanceSTL, Vector2 targetPos)
        {
            float rotationTime = Direction.AngleToTarget(targetPos) / (RotationDegrees / GlobalStats.TurnTimer);
            float distanceFTL  = Math.Max(distance - distanceSTL, 0);
            float travelSTL    = distanceSTL / MaxSTLSpeed;
            float travelFTL    = distanceFTL / MaxFTLSpeed;

            return (travelFTL + travelSTL + rotationTime + FTLSpoolTime) / GlobalStats.TurnTimer;
        }


        public void TetherToPlanet(Planet p)
        {
            TetheredTo = p;
            TetherOffset = Center - p.Center;
        }

        public Planet GetTether()
        {
            return TetheredTo;
        }

        public Ship Clone()
        {
            return (Ship)MemberwiseClone();
        }

        public float GetCost(Empire empire)
        {
            return ShipStats.GetCost(BaseCost, shipData, empire);
        }

        public float GetScrapCost()
        {
            return GetCost(loyalty) / 2f;
        }

        public ShipData BaseHull => shipData.BaseHull;

        public void SetShipData(ShipData data)
        {
            shipData = data;
            shipData.UpdateBaseHull();
        }

        public void Explore()
        {
            AI.State = AIState.Explore;
            AI.SetPriorityOrder(true);
        }

        /// <summary>Forces the ship to be in combat without a target.</summary>
        public void ForceCombatTimer(float timer = 15f) => InCombatTimer = timer;

        public bool InRadiusOfSystem(SolarSystem system) =>
            system != null && InRadius(system.Position, system.Radius);

        public bool InRadiusOfCurrentSystem => InRadiusOfSystem(System);

        public bool InRadius(Vector2 worldPos, float radius)
            => Center.InRadius(worldPos, Radius + radius);

        public bool CheckRangeToTarget(Weapon w, GameplayObject target)
        {
            if (target == null || !target.Active || target.Health <= 0)
                return false;

            if (engineState == MoveState.Warp)
                return false;

            var targetModule = target as ShipModule;
            Ship targetShip = target as Ship ?? targetModule?.GetParent();
            if (targetShip == null && targetModule == null && w.isBeam)
                return false;

            if (targetShip != null)
            {
                if (targetShip.dying || !targetShip.Active ||
                    targetShip.NumExternalSlots <= 0)
                    return false;
            }

            float attackRunRange = 50f;
            if (!w.isBeam && DesiredCombatRange < 2000)
            {
                attackRunRange = SpeedLimit;
                if (attackRunRange < 50f)
                    attackRunRange = 50f;
            }

            float range = attackRunRange + w.BaseRange;
            return target.Center.InRadius(w.Module.Center, range);
        }

        // Added by McShooterz
        public bool IsTargetInFireArcRange(Weapon w, GameplayObject target)
        {
            ++GlobalStats.WeaponArcChecks;

            if (!CheckRangeToTarget(w, target))
                return false;

            if (target is Ship targetShip)
            {
                if (w.MassDamage > 0 || w.RepulsionDamage > 0)
                {
                    if (targetShip.EnginesKnockedOut || targetShip.IsTethered)
                        return false;
                }
                if (!loyalty.IsEmpireAttackable(target.GetLoyalty()))
                    return false;
            }

            ShipModule m = w.Module;
            return RadMath.IsTargetInsideArc(m.Center, target.Center,
                                             Rotation + m.FacingRadians, m.FieldOfFire);
        }

        // This is used by Beam weapons and by Testing
        public bool IsInsideFiringArc(Weapon w, Vector2 pickedPos)
        {
            ++GlobalStats.WeaponArcChecks;

            ShipModule m = w.Module;
            return RadMath.IsTargetInsideArc(m.Center, pickedPos,
                                             Rotation + m.FacingRadians, m.FieldOfFire);
        }

        public SceneObject GetSO()
        {
            return ShipSO;
        }

        public void ReturnHome()
        {
            AI.OrderReturnHome();
        }

        // ModInfo activation option for Maintenance Costs:
        bool IsFreeUpkeepShip(ShipData.RoleName role, Empire empire)
        {
            return shipData.ShipStyle == "Remnant"
                || empire?.data == null
                || loyalty.data.PrototypeShip == Name
                || (Mothership != null && role >= ShipData.RoleName.fighter && role <= ShipData.RoleName.frigate);
        }

        // Calculate maintenance by proportion of ship cost
        private static float GetModMaintenanceModifier(ShipData.RoleName role)
        {
            ModInformation mod = GlobalStats.ActiveModInfo;
            switch (role)
            {
                case ShipData.RoleName.fighter:
                case ShipData.RoleName.scout:     return mod.UpkeepFighter;
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.gunboat:   return mod.UpkeepCorvette;
                case ShipData.RoleName.frigate:
                case ShipData.RoleName.destroyer: return mod.UpkeepFrigate;
                case ShipData.RoleName.cruiser:   return mod.UpkeepCruiser;
                case ShipData.RoleName.carrier:   return mod.UpkeepCarrier;
                case ShipData.RoleName.capital:   return mod.UpkeepCapital;
                case ShipData.RoleName.freighter: return mod.UpkeepFreighter;
                case ShipData.RoleName.platform:  return mod.UpkeepPlatform;
                case ShipData.RoleName.station:   return mod.UpkeepStation;
            }
            if (role == ShipData.RoleName.drone && mod.useDrones) return mod.UpkeepDrone;
            return mod.UpkeepBaseline;
        }

        public float GetMaintCost() => GetMaintCost(loyalty);

        public float GetMaintCost(Empire empire)
        {
            return GetMaintenanceCost(this, empire);
        }

        public void DoEscort(Ship escortTarget)
        {
            AI.ClearOrders(AIState.Escort);
            AI.EscortTarget = escortTarget;
        }

        public void DoDefense()
        {
            AI.State = AIState.SystemDefender;
        }

        public void OrderToOrbit(Planet orbit, bool offensiveMove = false)
        {
            AI.OrderToOrbit(orbit, offensiveMove);
        }

        public void DoExplore()
        {
            AI.OrderExplore();
        }

        public void DoColonize(Planet p, Goal g)
        {
            AI.OrderColonization(p, g);
        }

        public ShipData ToShipData()
        {
            var data = new ShipData();
            data.BaseCanWarp               = shipData.BaseCanWarp;
            data.BaseStrength              = -1;
            data.TechsNeeded               = shipData.TechsNeeded;
            data.TechScore                 = shipData.TechScore;
            data.ShipCategory              = shipData.ShipCategory;
            data.Name                      = Name;
            data.Level                     = (byte)Level;
            data.experience                = (byte)experience;
            data.Role                      = shipData.Role;
            data.IsShipyard                = shipData.IsShipyard;
            data.IsOrbitalDefense          = shipData.IsOrbitalDefense;
            data.Animated                  = shipData.Animated;
            data.CombatState               = AI.CombatState;
            data.ModelPath                 = shipData.ModelPath;
            data.ModuleSlots               = GetModuleSlotDataArray();
            data.ThrusterList              = new Array<ShipToolScreen.ThrusterZone>();
            data.MechanicalBoardingDefense = MechanicalBoardingDefense;
            data.BaseHull                  = shipData.BaseHull;
            foreach (Thruster thruster in ThrusterList)
            {
                data.ThrusterList.Add(new ShipToolScreen.ThrusterZone
                {
                    Scale    = thruster.tscale,
                    Position = thruster.XMLPos
                });
            }
            return data;
        }

        // @todo This exists solely because ModuleSlot updating is buggy
        // if you get a chance, just fix ModuleSlot updates so this isn't needed
        ModuleSlotData[] GetModuleSlotDataArray()
        {
            var slots = new ModuleSlotData[ModuleSlotList.Length];
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                var data = new ModuleSlotData
                {
                    Position           = module.XMLPosition,
                    InstalledModuleUID = module.UID,
                    Health             = module.Health,
                    ShieldPower        = module.ShieldPower,
                    Facing             = module.FacingDegrees,
                    Restrictions       = module.Restrictions
                };

                if (module.TryGetHangarShip(out Ship hangarShip))
                    data.HangarshipGuid = hangarShip.guid;

                if (module.ModuleType == ShipModuleType.Hangar)
                    data.SlotOptions = module.DynamicHangar == DynamicHangarOptions.Static
                                                               ? module.hangarShipUID
                                                               : module.DynamicHangar.ToString();

                slots[i] = data;
            }
            return slots;
        }

        private string GetConduitGraphic(ShipModule forModule)
        {
            var conduit = new ConduitGraphic();
            foreach (ShipModule module in ModuleSlotList)
                if (module.ModuleType == ShipModuleType.PowerConduit)
                    conduit.Add((int)(module.XMLPosition.X - forModule.XMLPosition.X),
                                (int)(module.XMLPosition.Y - forModule.XMLPosition.Y));
            return conduit.GetGraphic();
        }

        public struct ConduitGraphic
        {
            public bool Right;
            public bool Left;
            public bool Down;
            public bool Up;
            public void Add(int dx, int dy)
            {
                AddGridPos(dx / 16, dy / 16);
            }
            public void AddGridPos(int dx, int dy)
            {
                Left  |= dx == -1 && dy == 0;
                Right |= dx == +1 && dy == 0;
                Down  |= dx ==  0 && dy == -1;
                Up    |= dx ==  0 && dy == +1;
            }
            public int Sides => (Left?1:0) + (Right?1:0) + (Down?1:0) + (Up?1:0);
            public string GetGraphic()
            {
                switch (Sides)
                {
                    case 1:
                        if (Down)  return "Conduits/conduit_powerpoint_down";
                        if (Up)    return "Conduits/conduit_powerpoint_up";
                        if (Left)  return "Conduits/conduit_powerpoint_right";
                        if (Right) return "Conduits/conduit_powerpoint_left";
                        break;
                    case 2:
                        if (Left && Down)  return "Conduits/conduit_corner_BR";
                        if (Left && Up)    return "Conduits/conduit_corner_TR";
                        if (Right && Down) return "Conduits/conduit_corner_BL";
                        if (Right && Up)   return "Conduits/conduit_corner_TL";
                        if (Down && Up)    return "Conduits/conduit_straight_vertical";
                        if (Left && Right) return "Conduits/conduit_straight_horizontal";
                        break;
                    case 3:
                        if (!Right)  return "Conduits/conduit_tsection_left";
                        if (!Left)   return "Conduits/conduit_tsection_right";
                        if (!Down)   return "Conduits/conduit_tsection_down";
                        if (!Up)     return "Conduits/conduit_tsection_up";
                        break;
                }
                return "Conduits/conduit_intersection";
            }
        }

        public const float UnarmedRange = 10000; // also used as evade range
        public float WeaponsMaxRange { get; private set; }
        public float WeaponsMinRange { get; private set; }
        public float WeaponsAvgRange { get; private set; }
        public float DesiredCombatRange { get; private set; }
        public float InterceptSpeed { get; private set; }

        // @return Filtered list of purely offensive weapons
        public Weapon[] OffensiveWeapons => Weapons.Filter(w => w.DamageAmount > 0.1f && !w.TruePD);

        /// <summary>
        /// get weapons filtered  by the below.
        /// If that gives no weapons use any weapon.<list type="bullet"><item> active</item><item>Have a damage greater than 0</item><item>Are not <font color="#b7dde8"><font style="background-color: #D8D8D8;"><strong>TruePD</strong></font></font>weapons</item></list><para><font color="#333333"></font></para></summary>
        Array<Weapon> GetActiveWeapons()
        {
            var weapons = new Array<Weapon>();
            // prefer offensive weapons:
            for (int i = 0; i < Weapons.Count; ++i) // using raw loops for perf
            {
                Weapon w = Weapons[i];
                if (w.Module.Active && w.DamageAmount > 0.1f && !w.TruePD)
                    weapons.Add(w);
            }

            // maybe we are equipped with Phalanx PD's only?
            // just us any active weapon then
            if (weapons.Count == 0)
            {
                for (int i = 0; i < Weapons.Count; ++i) // using raw loops for perf
                {
                    Weapon w = Weapons[i];
                    if (w.Module.Active)
                        weapons.Add(w);
                }
            }
            return weapons;
        }

        /// <summary>
        /// Gets the distinct weapons ranges.
        /// </summary>
        /// <param name="weapons">The weapons.</param>
        /// <returns></returns>
        float[] GetDistinctWeaponsRanges(Array<Weapon> weapons)
        {
            Array<float> ranges = new Array<float>();

            for (int i = 0; i < weapons.Count; ++i) // using raw loops for perf
                ranges.AddUnique(weapons[i].GetActualRange());
            return ranges.ToArray();
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
            Array<Weapon> weapons = GetActiveWeapons();
            float[] ranges = GetDistinctWeaponsRanges(weapons);

            // Carriers will use the carrier range for max range.
            // for min range carriers will use the max range of normal weapons.
            if (Carrier.IsPrimaryCarrierRole)
            {
                WeaponsMinRange = Math.Min(ranges.Max(), Carrier.HangarRange);
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
        }

        // This is used for previewing range during CombatState change
        // Not performance critical.
        float GetDesiredCombatRangeForState(CombatState state)
        {
            float[] ranges = GetDistinctWeaponsRanges(GetActiveWeapons());
            return CalcDesiredDesiredCombatRange(ranges, state);
        }

        // NOTE: Make sure to validate TestShipRanges.ShipRanges and TestShipRanges.ShipRangesWithModifiers
        float CalcDesiredDesiredCombatRange(float[] ranges, CombatState state)
        {
            if (ranges.Length == 0)
                return UnarmedRange;

            // for game balancing, so ships won't kite way too far
            // and still have chance to hit while moving
            const float rangeBalance = 0.9f; 

            switch (state)
            {
                case CombatState.Evade:        return UnarmedRange;
                case CombatState.HoldPosition: return WeaponsMaxRange;
                case CombatState.ShortRange:   return WeaponsMinRange * rangeBalance;
                case CombatState.Artillery:    return WeaponsMaxRange * rangeBalance;
                case CombatState.AssaultShip:  return WeaponsMaxRange * rangeBalance;
                default:
                    return WeaponsAvgRange * 0.9f;
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
            float[] speeds = weapons.Select(w => w.isBeam ? w.GetActualRange() * 1.5f : w.ProjectileSpeed);
            return speeds.Avg();
        }


        public void UpdateShipStatus(FixedSimTime timeStep)
        {
            if (!Empire.Universe.Paused && VelocityMaximum <= 0f
                && !shipData.IsShipyard && shipData.Role <= ShipData.RoleName.station)
            {
                Rotation += 0.003f + RandomMath.AvgRandomBetween(0.0001f, 0.0005f);
            }

            ShipEngines.Update();

            if (timeStep.FixedTime > 0 && (EMPDamage > 0 || EMPdisabled))
                CauseEmpDamage(-EmpRecovery);

            Rotation = Rotation.AsNormalizedRadians();

            //UpdateModulePositions(deltaTime);

            if (!EMPdisabled && hasCommand)
            {
                for (int i = 0; i < Weapons.Count; i++)
                {
                    Weapons[i].Update(timeStep);
                }
                for (int i = 0; i < BombBays.Count; i++)
                {
                    BombBays[i].InstalledWeapon.Update(timeStep);
                }
            }

            AI.CombatAI.SetCombatTactics(AI.CombatState);

            updateTimer -= timeStep.FixedTime;
            if (updateTimer <= 0f)
            {
                updateTimer += 1f; // update the ship modules and status only once per second
                UpdateModulesAndStatus(FixedSimTime.One);
                SecondsAlive += 1;
            }

            Carrier.HandleHangarShipsScramble();

            InternalSlotsHealthPercent = (float)ActiveInternalSlotCount / InternalSlotCount;

            if (InternalSlotsHealthPercent < ShipResupply.ShipDestroyThreshold)
                Die(LastDamagedBy, false);

            PowerCurrent -= PowerDraw * timeStep.FixedTime;
            if (PowerCurrent < PowerStoreMax)
                PowerCurrent += (PowerFlowMax + PowerFlowMax * (loyalty?.data.PowerFlowMod ?? 0)) * timeStep.FixedTime;

            if (PowerCurrent <= 0.0f)
            {
                PowerCurrent = 0.0f;
                HyperspaceReturn();
            }
            PowerCurrent = Math.Min(PowerCurrent, PowerStoreMax);

            shield_percent = shield_max >0 ? 100.0 * shield_power / shield_max : 0;
        }

        public void UpdateSensorsAndInfluence(FixedSimTime timeStep)
        {
            // update our knowledge of the surrounding universe
            UpdateInfluence(timeStep);
            KnownByEmpires.Update(timeStep);
            SetFleetCapableStatus();
            
            // scan universe and make decisions for combat
            AI.StartSensorScan(timeStep);
        }

        public void UpdateModulePositions(FixedSimTime timeStep, bool isSystemView, bool forceUpdate = false)
        {
            if (Active && AI.BadGuysNear || (InFrustum && isSystemView) || forceUpdate)
            {  
                float cos = RadMath.Cos(Rotation);
                float sin = RadMath.Sin(Rotation);
                float tan = (float)Math.Tan(yRotation);
                float parentX = Center.X;
                float parentY = Center.Y;
                float rotation = Rotation;
                for (int i = 0; i < ModuleSlotList.Length; ++i)
                {
                    ModuleSlotList[i].UpdateEveryFrame(timeStep, parentX, parentY, rotation, cos, sin, tan);
                }
                GlobalStats.ModuleUpdates += ModuleSlotList.Length;
            }
        }

        void UpdateModulesAndStatus(FixedSimTime timeSinceLastUpdate)
        {
            if (InCombat && !EMPdisabled && hasCommand && Weapons.Count > 0)
            {
                AI.CombatAI.UpdateTargetPriorities(this);

                foreach (Weapon weapon in Weapons)
                {
                    if (GlobalStats.HasMod)
                    {
                        Weapon weaponTemplate = ResourceManager.GetWeaponTemplate(weapon.UID);
                        weapon.fireDelay = weaponTemplate.fireDelay;

                        //Added by McShooterz: Hull bonus Fire Rate
                        if (GlobalStats.ActiveModInfo.useHullBonuses)
                        {
                            weapon.fireDelay *= 1f - shipData.Bonuses.FireRateBonus;
                        }
                    }
                }
            }

            if (InhibitedTimer < 1f)
            {
                UpdateInhibitedFromEnemyShips();
            }

            for (int i = 0; i < ModuleSlotList.Length; ++i)
                ModuleSlotList[i].Update(timeSinceLastUpdate);

            if (ShouldRecalculatePower) // must be before ShipStatusChange
                RecalculatePower();

            if (shipStatusChanged)
                ShipStatusChange();

            //Power draw based on warp
            if (!IsInFriendlyProjectorRange && engineState == MoveState.Warp)
                PowerDraw = NetPower.NetWarpPowerDraw;
            else if (engineState != MoveState.Warp)
                PowerDraw = NetPower.NetSubLightPowerDraw;
            else
                PowerDraw = NetPower.NetWarpPowerDraw;

            if (InCombat
                || shield_power < shield_max
                || engineState == MoveState.Warp)
            {
                shield_power = 0.0f;
                for (int x = 0; x < Shields.Length; x++)
                {
                    ShipModule shield = Shields[x];
                    shield_power = (shield_power + shield.ShieldPower).Clamped(0, shield_max);
                }
            }

            // Add ordnance
            ChangeOrdnance(OrdAddedPerSecond);
            UpdateMovementFromOrdnanceChange();

            // Update max health if needed
            int latestRevision = EmpireShipBonuses.GetBonusRevisionId(loyalty);
            if (MaxHealthRevision != latestRevision)
            {
                MaxHealthRevision = latestRevision;
                HealthMax = RecalculateMaxHealth();
            }

            // return home if it is a defense ship
            if (!InCombat && HomePlanet != null && !HomePlanet.SpaceCombatNearPlanet)
                ReturnHome();

            // Repair
            if (Health.Less(HealthMax))
            {
                if (!InCombat || (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useCombatRepair))
                {
                    // Added by McShooterz: Priority repair
                    float repair = InCombat ? RepairRate * 0.1f : RepairRate;
                    ApplyAllRepair(repair, Level);
                }

                if (!EMPdisabled)
                    PerformRegeneration();
            }

            UpdateResupply();
            UpdateTroops(timeSinceLastUpdate);


            if (!AI.BadGuysNear)
                ShieldManager.RemoveShieldLights(Shields);
        }

        void PerformRegeneration()
        {
            if (!HasRegeneratingModules)
                return;

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule module = ModuleSlotList[i];
                module.RegenerateSelf();
            }
        }

        public void UpdateResupply()
        {
            if (Health < 1f)
                return;
            
            Carrier.SupplyShuttle.ProcessSupplyShuttles(AI.GetSensorRadius());

            ResupplyReason resupplyReason = Supply.Resupply();
            if (resupplyReason != ResupplyReason.NotNeeded && Mothership?.Active == true)
            {
                AI.OrderReturnToHangar(); // dealing with hangar ships needing resupply
                return;
            }

            AI.ProcessResupply(resupplyReason);

        }

        public bool IsSuitableForPlanetaryRearm()
        {
            if (InCombat 
                || !Active
                || OrdnancePercent.AlmostEqual(1)
                || IsPlatformOrStation && TetheredTo?.Owner == loyalty
                || AI.OrbitTarget?.Owner == loyalty
                || AI.State == AIState.Resupply
                || AI.State == AIState.Scrap
                || AI.State == AIState.Refit
                || IsSupplyShuttle)

            {
                return false;
            }

            return true;
        }

        bool IsSupplyShuttle => Name == loyalty.GetSupplyShuttleName();

        public int RefitCost(Ship newShip)
        {
            if (loyalty.isFaction)
                return 0;

            float oldShipCost = GetCost(loyalty);

            // FB: Refit works normally only for ship of the same hull. But freighters can be replaced to other hulls by the auto trade.
            // So replacement cost is higher, the old ship cost is halved, just like scrapping it.
            if (shipData.Hull != newShip.shipData.Hull)
                oldShipCost /= 2;

            float newShipCost = newShip.GetCost(loyalty);
            int cost          = Math.Max((int)(newShipCost - oldShipCost), 0);
            return cost + (int)(10 * CurrentGame.Pace); // extra refit cost: accord for GamePace;
        }

        public Status HealthStatus
        {
            get
            {
                if (engineState == MoveState.Warp
                    || AI.State == AIState.Refit
                    || AI.State == AIState.Resupply)
                {
                    return Status.NotApplicable;
                }

                Health = Health.Clamped(0, HealthMax);
                return ToShipStatus(Health, HealthMax);
            }
        }

        public void AddShipHealth(float addHealth) => Health = (Health + addHealth).Clamped(0, HealthMax);

        public void ShipStatusChange()
        {
            shipStatusChanged = false;

            float sensorBonus           = 0f;
            shield_max                  = 0f;
            ActiveInternalSlotCount     = 0;
            BonusEMP_Protection         = 0f;
            PowerStoreMax               = 0f;
            PowerFlowMax                = 0f;
            OrdinanceMax                = 0f;
            RepairRate                  = 0f;
            CargoSpaceMax               = 0f;
            SensorRange                 = 1000f;
            InhibitionRadius            = 0f;
            OrdAddedPerSecond           = 0f;
            HealPerTurn                 = 0;
            ECMValue                    = 0f;
            hasCommand                  = IsPlatform;
            TrackingPower               = 0;
            TargetingAccuracy           = 0;

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule module = ModuleSlotList[i];
                // active internal slots
                if (module.HasInternalRestrictions && module.Active)
                    ActiveInternalSlotCount += module.XSIZE * module.YSIZE;

                RepairRate += module.Active ? module.ActualBonusRepairRate : module.ActualBonusRepairRate / 10; // FB - so destroyed modules with repair wont have full repair rate

                if (module.Active && (module.Powered || module.PowerDraw <= 0f))
                {
                    hasCommand |= module.IsCommandModule;
                    
                    OrdinanceMax        += module.OrdinanceCapacity;
                    CargoSpaceMax       += module.Cargo_Capacity;
                    BonusEMP_Protection += module.EMP_Protection;
                    OrdAddedPerSecond   += module.OrdnanceAddedPerSecond;
                    HealPerTurn         += module.HealPerTurn;
                    InhibitionRadius     = module.InhibitionRadius.LowerBound(InhibitionRadius);
                    SensorRange          = module.SensorRange.LowerBound(SensorRange);
                    sensorBonus          = module.SensorBonus.LowerBound(sensorBonus);
                    TrackingPower       += module.TargetTracking;
                    TargetingAccuracy    = module.TargetingAccuracy.LowerBound(TargetingAccuracy);
                    ECMValue             = 1f.Clamped(0f, Math.Max(ECMValue, module.ECM)); // 0-1 using greatest value.
                    module.AddModuleTypeToList(module.ModuleType, isTrue: module.InstalledWeapon?.isRepairBeam == true, addToList: RepairBeams);
                }
            }

            if (IsTethered)
            {
                var planet = TetheredTo;
                if (planet?.Owner != null && (planet.Owner == loyalty || loyalty.IsAlliedWith(planet.Owner)))
                {
                    TrackingPower     = TrackingPower.LowerBound(planet.Level);
                    TargetingAccuracy = TrackingPower.LowerBound(planet.Level);
                }
            }

            shield_max     = ShipUtils.UpdateShieldAmplification(Amplifiers, Shields);
            NetPower       = Power.Calculate(ModuleSlotList, loyalty);
            PowerStoreMax  = NetPower.PowerStoreMax;
            PowerFlowMax   = NetPower.PowerFlowMax;
            shield_percent = (100.0 * shield_power / shield_max.LowerBound(0.1f)).LowerBound(0);
            SensorRange   += sensorBonus;

            // Apply modifiers to stats
            if (IsPlatform) SensorRange = SensorRange.LowerBound(10000);
            RepairRate     += (float)(RepairRate * Level * 0.05);
            SensorRange    *= loyalty.data.SensorModifier;
            CargoSpaceMax  *= shipData.Bonuses.CargoModifier;
            SensorRange    *= shipData.Bonuses.SensorModifier;

            (Thrust, WarpThrust, TurnThrust) = ShipStats.GetThrust(ModuleSlotList, shipData);
            Mass         = ShipStats.GetMass(ModuleSlotList, loyalty, OrdnancePercent);
            FTLSpoolTime = ShipStats.GetFTLSpoolTime(ModuleSlotList, loyalty);

            CurrentStrength = CalculateShipStrength();
            UpdateWeaponRanges();
            UpdateMaxVelocity();
            SetMaxFTLSpeed();
            SetMaxSTLSpeed();
        }

        public bool IsTethered => TetheredTo != null;

        private float CurrentStrength = -1.0f;

        public float GetStrength()
        {
            return CurrentStrength;
        }

        public float NormalizedStrength => BaseStrength / shipData.ModuleSlots.Length;

        public float GetDPS() => DPS;

        //Added by McShooterz: Refactored by CG
        public void AddKill(Ship killed)
        {
            ++kills;
            if (loyalty == null)
                return;

            float exp   = killed.ExperienceShipIsWorth();
            exp        += exp * loyalty.data.ExperienceMod;
            experience += exp;
            ConvertExperienceToLevel();

            if (killed.loyalty?.WeArePirates ?? false)
                killed.loyalty.Pirates.KillBaseReward(loyalty, killed);
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
                if (experience <= 0) return;
                float experienceThreshold = ownerExpSettings.ExpPerLevel * (1 + Level);
                if (experienceThreshold <= 0) return;
                if (experience < experienceThreshold) return;
                AddToShipLevel(1);
                experience -= experienceThreshold;
            }
        }

        public void AddToShipLevel(int amountToAdd) => Level = (Level + amountToAdd).Clamped(0,10);

        public bool NotThreatToPlayer()
        {
            if (loyalty == EmpireManager.Player || IsInWarp)
                return true;

            if (loyalty == EmpireManager.Remnants)
                return false;

            return BaseStrength.LessOrEqual(0)
                   || IsFreighter
                   || !EmpireManager.Player.IsAtWarWith(loyalty);
        }

        public void UpdateEmpiresOnKill(Ship killedShip)
        {
            loyalty.WeKilledTheirShip(killedShip.loyalty, killedShip);
            killedShip.loyalty.TheyKilledOurShip(loyalty, killedShip);
        }

        void NotifyPlayerIfDiedExploring()
        {
            if (AI.State == AIState.Explore && loyalty.isPlayer)
                Empire.Universe.NotificationManager.AddExplorerDestroyedNotification(this);
        }

        void ExplodeShip(float size, bool addWarpExplode)
        {
            if (!InFrustum) return;
            var position = new Vector3(Center.X, Center.Y, -100f);

            float boost = 1f;
            if (GlobalStats.HasMod)
                boost = GlobalStats.ActiveModInfo.GlobalShipExplosionVisualIncreaser;

            ExplosionManager.AddExplosion(position, Velocity,
                size * boost, 12f, ExplosionType.Ship);
            if (addWarpExplode)
            {
                ExplosionManager.AddExplosion(position, Velocity,
                    size*1.75f, 12f, ExplosionType.Warp);
            }
            UniverseScreen.Spatial.ShipExplode(this, size * 50, Center, Radius);
        }

        // cleanupOnly: for tumbling ships that are already dead
        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ShipsDied;
            Projectile psource = source as Projectile;
            if (!cleanupOnly)
            {
                psource?.Module?.GetParent().UpdateEmpiresOnKill(this);
                psource?.Module?.GetParent().AddKill(this);
            }

            // 35% the ship will not explode immediately, but will start tumbling out of control
            // we mark the ship as dying and the main update loop will set reallyDie
            if (UniverseRandom.IntBetween(0, 100) > 65.0 && !IsPlatform && InFrustum)
            {
                dying         = true;
                DieRotation.X = UniverseRandom.RandomBetween(-1f, 1f) * 40f / SurfaceArea;
                DieRotation.Y = UniverseRandom.RandomBetween(-1f, 1f) * 40f / SurfaceArea;
                DieRotation.Z = UniverseRandom.RandomBetween(-1f, 1f) * 40f / SurfaceArea;
                dietimer      = UniverseRandom.RandomBetween(4f, 6f);
                if (psource != null && psource.Explodes && psource.DamageAmount > 100.0)
                    reallyDie = true;
            }
            else
            {
                reallyDie = true;
            }

            if (dying && !reallyDie)
                return;

            if (psource?.Owner != null)
            {
                float amount = 1f;
                if (ResourceManager.ShipRoles.ContainsKey(shipData.Role))
                    amount = ResourceManager.ShipRoles[shipData.Role].DamageRelations;
                loyalty.DamageRelationship(psource.Owner.loyalty, "Destroyed Ship", amount, null);
            }
            if (!cleanupOnly && InFrustum)
            {
                string dieSoundEffect;
                if (SurfaceArea < 80)       dieSoundEffect = "sd_explosion_ship_det_small";
                else if (SurfaceArea < 250) dieSoundEffect = "sd_explosion_ship_det_medium";
                else                        dieSoundEffect = "sd_explosion_ship_det_large";
                GameAudio.PlaySfxAsync(dieSoundEffect, SoundEmitter);
            }

            NotifyPlayerIfDiedExploring();
            Carrier.ScuttleNonWarpHangarShips();
            ResetProjectorInfluence();

            float size = Radius * (shipData.EventOnDeath?.NotEmpty() == true ? 3 : 1);
            if (Active)
            {
                Active = false;
                switch (shipData.HullRole)
                {
                    case ShipData.RoleName.corvette:
                    case ShipData.RoleName.scout:
                    case ShipData.RoleName.fighter:
                    case ShipData.RoleName.frigate:   ExplodeShip(size * 10, cleanupOnly); break;
                    case ShipData.RoleName.carrier:
                    case ShipData.RoleName.capital:
                    case ShipData.RoleName.cruiser:
                    case ShipData.RoleName.station:   ExplodeShip(size * 8, true);         break;
                    default:                          ExplodeShip(size * 8, cleanupOnly);  break;
                }

                if (!HasExploded)
                {
                    HasExploded = true;

                    // Added by RedFox - spawn flaming spacejunk when a ship dies
                    float radSqrt   = (float)Math.Sqrt(Radius);
                    float junkScale = radSqrt * 0.05f; // trial and error, depends on junk model sizes
                    if (junkScale > 1.4f) junkScale = 1.4f; // bigger doesn't look good

                    //Log.Info("Ship.Explode r={1} rsq={2} junk={3} scale={4}   {0}", Name, Radius, radSqrt, explosionJunk, junkScale);
                    for (int x = 0; x < 3; ++x)
                    {
                        int howMuchJunk = (int)RandomMath.RandomBetween(Radius * 0.05f, Radius * 0.15f);
                        SpaceJunk.SpawnJunk(howMuchJunk, Center.GenerateRandomPointOnCircle(Radius/2),
                            Velocity, this, Radius, junkScale, true);
                    }
                }
            }
            if (BaseHull.EventOnDeath != null)
            {
                var evt = ResourceManager.EventsDict[BaseHull.EventOnDeath];
                Empire.Universe.ScreenManager.AddScreenDeferred(
                    new EventPopup(Empire.Universe, EmpireManager.Player, evt, evt.PotentialOutcomes[0], true));
            }

            QueueTotalRemoval();

            base.Die(source, cleanupOnly);
        }

        public void QueueTotalRemoval()
        {
            Active = false;
            TetheredTo?.RemoveFromOrbitalStations(this);
            AI.ClearOrdersAndWayPoints(); // This calls immediate Dispose() on Orders that require cleanup
        }

        public override void RemoveFromUniverseUnsafe()
        {
            AI.Reset();

            if (Mothership != null)
            {
                foreach (ShipModule shipModule in Mothership.Carrier.AllActiveHangars)
                    if (shipModule.TryGetHangarShip(out Ship ship) && ship == this)
                        shipModule.SetHangarShip(null);
            }

            foreach (ShipModule hangar in Carrier.AllHangars) // FB: use here all hangars and not just active hangars
            {
                if (hangar.TryGetHangarShip(out Ship hangarShip))
                    hangarShip.Mothership = null; // Todo - Setting this to null might be risky
            }

            foreach (Empire empire in EmpireManager.Empires)
            {
                empire.GetEmpireAI().ThreatMatrix.RemovePin(this);
            }

            ModuleSlotList     = Empty<ShipModule>.Array;
            SparseModuleGrid   = Empty<ShipModule>.Array;
            ExternalModuleGrid = Empty<ShipModule>.Array;
            NumExternalSlots   = 0;
            Shields            = Empty<ShipModule>.Array;
            ThrusterList.Clear();
            BombBays.Clear();
            OurTroops.Clear();
            HostileTroops.Clear();
            RepairBeams.Clear();

            loyalty.RemoveShip(this);
            TetheredTo = null;

            RemoveSceneObject();
            base.RemoveFromUniverseUnsafe();
        }

        public void ClearFleet() => fleet?.RemoveShip(this);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Ship() { Dispose(false); }

        void Dispose(bool disposing)
        {
            supplyLock?.Dispose(ref supplyLock);
            AI?.Dispose();
            AI = null;
        }

        public void UpdateShields()
        {
            float shieldPower = 0.0f;
            for (int i = 0; i < Shields.Length; ++i)
                shieldPower += Shields[i].ShieldPower;
            if (shieldPower > shield_max)
                shieldPower = shield_max;
            shield_power = shieldPower;
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
                if (maxHealthDebug) Log.Info($"Health was {Health} / {HealthMax}   ({loyalty.data.Traits.ModHpModifier})");
            #endif

            float healthMax = 0;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
                healthMax += ModuleSlotList[i].ActualMaxHealth;

            #if DEBUG
                if (maxHealthDebug) Log.Info($"Health is  {Health} / {HealthMax}");
            #endif
            return healthMax;
        }

        public bool AnyModulesOf(ShipModuleType moduleType)
        {
            return ModuleSlotList.Any(moduleType);
        }

        public float StartingColonyGoods()
        {
            return ModuleSlotList.Sum(m => m.numberOfEquipment + m.numberOfFood);
        }

        public int NumBuildingsDeployedOnColonize()
        {
            return ModuleSlotList.Count(m => m.DeployBuildingOnColonize.NotEmpty());
        }

        ShipData.RoleName GetDesignRole() => new RoleData(this, ModuleSlotList).DesignRole;

        public void MarkShipRolesUsableForEmpire(Empire empire)
        {
            switch (DesignRole)
            {
                case ShipData.RoleName.bomber:    empire.canBuildBombers      = true; break;
                case ShipData.RoleName.carrier:   empire.canBuildCarriers     = true; break;
                case ShipData.RoleName.support:   empire.canBuildSupportShips = true; break;
                case ShipData.RoleName.troopShip: empire.canBuildTroopShips   = true; break;
                case ShipData.RoleName.corvette:  empire.canBuildCorvettes    = true; break;
                case ShipData.RoleName.frigate:   empire.canBuildFrigates     = true; break;
                case ShipData.RoleName.cruiser:   empire.canBuildCruisers     = true; break;
                case ShipData.RoleName.capital:   empire.canBuildCapitals     = true; break;
                case ShipData.RoleName.platform:  empire.CanBuildPlatforms    = true; break;
                case ShipData.RoleName.station:   empire.CanBuildStations     = true; break;
            }
            if (shipData.IsShipyard)
                empire.CanBuildShipyards = true;
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

        // @todo autocalculate during ship instance init
        private int DPS;
        public float CalculateShipStrength()
        {
            float offense      = 0f;
            float defense      = 0f;
            bool fighters      = false;
            bool weapons       = false;
            int numWeaponSlots = 0;
            float turnThrust   = 0;

            foreach (ShipModule slot in ModuleSlotList)
            {
                //ShipModule template = GetModuleTemplate(slot.UID);
                if (slot.InstalledWeapon != null)
                {
                    weapons         = true;
                    numWeaponSlots += slot.Area;
                }
                fighters |= slot.hangarShipUID   != null && !slot.IsSupplyBay && !slot.IsTroopBay;

                offense    += slot.CalculateModuleOffense();
                defense    += slot.CalculateModuleDefense(SurfaceArea);
                turnThrust += slot.TurnThrust;

                BaseCanWarp |= slot.WarpThrust > 0;
            }
            DPS = (int)offense;
            if (IsPlatformOrStation) offense  /= 2;
            if (!fighters && !weapons) offense = 0f;

            return ShipBuilder.GetModifiedStrength(SurfaceArea, numWeaponSlots, offense, defense);
        }

        private void ApplyRepairToShields(float repairPool)
        {
            float shieldrepair = 0.2f * repairPool;
            if (shield_max - shield_power > shieldrepair)
                shield_power += shieldrepair;
            else
                shield_power = shield_max;
        }

        public void ApplyAllRepair(float repairAmount, int repairLevel, bool repairShields = false)
        {
            if (repairAmount.AlmostEqual(0)) return;
            int damagedModules = ModuleSlotList.Count(module => !module.Health.AlmostEqual(module.ActualMaxHealth));
            for (int x =0; x < damagedModules; x++)
            {
                if (repairAmount.AlmostEqual(0)) break;
                repairAmount = ApplyRepairOnce(repairAmount, repairLevel);
            }

            ApplyRepairToShields(repairAmount);
            if (Health.AlmostEqual(HealthMax))
                RefreshMechanicalBoardingDefense();
        }

        /**
         * @param repairLevel Level of the crew or repair level of orbital shipyard
         */
        public float ApplyRepairOnce(float repairAmount, int repairLevel)
        {
            if (!Active)
                return repairAmount;

            // RepairSkill Reduces the priority of mostly healed modules.
            // It allows a ship to become fully functional faster.
            float repairSkill = 1.0f - (repairLevel * 0.1f).Clamped(0.0f, 0.95f);

            ShipModule moduleToRepair = ModuleSlotList.FindMax(module =>
            {
                if (module.HealthPercent.AlmostEqual(1)) return 0;
                // fully damaged modules get priority 1.0
                float damagePriority =  module.Health.Less(module.ActualMaxHealth * repairSkill)
                                    ? 1.0f
                                    : 1.0f - module.HealthPercent;

                // best modules get priority 1.0
                float moduleImportance = 1.1f - (float)module.ModulePriority / ShipModule.MaxPriority;
                return damagePriority * moduleImportance;
            });

            return moduleToRepair.Repair(repairAmount);
        }

        public void UpdatePackDamageModifier()
        {
            float modifier = -0.25f + 0.05f * AI.FriendliesNearby.Count;
            PackDamageModifier = modifier.Clamped(-0.25f, 0.5f);
        }

        // prefers VanityName, otherwise uses Name
        public string ShipName => VanityName.NotEmpty() ? VanityName : Name;

        public override string ToString() =>
            $"Ship Id={Id} '{ShipName}' Pos {Position} {System} Loyalty {loyalty} Role {DesignRole} State {AI?.State}" ;

        public bool ShipIsGoodForGoals(float baseStrengthNeeded = 0, Empire empire = null)
        {
            if (!Active) return false;
            empire = empire ?? loyalty;
            if (!shipData.BaseCanWarp) return false;

            float goodPowerSupply = PowerFlowMax - NetPower.NetWarpPowerDraw;
            float powerTime = GlobalStats.MinimumWarpRange;
            if (goodPowerSupply <0)
            {
                powerTime = PowerStoreMax / -goodPowerSupply * MaxFTLSpeed;
            }

            bool warpTimeGood = goodPowerSupply >= 0 || powerTime >= GlobalStats.MinimumWarpRange;

            bool goodPower = shipData.BaseCanWarp && warpTimeGood ;
            if (!goodPower || empire == null)
            {
                Empire.Universe?.DebugWin?.DebugLogText($"WARNING ship design {Name} with hull {shipData.Hull} :Bad WarpTime. {NetPower.NetWarpPowerDraw}/{PowerFlowMax}", DebugModes.Normal);
            }
            if (DesignRole < ShipData.RoleName.fighter || GetStrength() >  baseStrengthNeeded )
                return goodPower;
            return false;
        }

        public bool IsBuildableByPlayer
        {
            get
            {
                ShipRole role = shipData.ShipRole;
                return  !shipData.CarrierShip && !Deleted
                    && !role.Protected && !role.NoBuild
                    && (GlobalStats.ShowAllDesigns || IsPlayerDesign);
            }
        }

        public bool ShipGoodToBuild(Empire empire)
        {
            if (IsPlatformOrStation || shipData.CarrierShip)
                return true;

            NetPower = Power.Calculate(ModuleSlotList, empire);
            return ShipIsGoodForGoals(float.MinValue, empire);
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

        // if the shipstatus enum is added to then "5" will need to be changed.
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

