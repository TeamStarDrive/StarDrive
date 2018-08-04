using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion.Controllers;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    using static ShipMaintenance;

    public sealed partial class Ship : GameplayObject, IDisposable
    {
        public string VanityName                = ""; // user modifiable ship name. Usually same as Ship.Name
        public Array<Troop> TroopList           = new Array<Troop>();
        public Array<Rectangle> AreaOfOperation = new Array<Rectangle>();
        public bool RecallFightersBeforeFTL     = true;

        //public float DefaultFTLSpeed = 1000f;    //Not referenced in code, removing to save memory
        public float RepairRate  = 1f;
        public float SensorRange = 20000f;
        public float yBankAmount = 0.007f;
        public float MaxBank     = 0.5235988f; 
        public Vector2 Acceleration { get; private set; }

        public Vector2 projectedPosition;
        private readonly Array<Thruster> ThrusterList = new Array<Thruster>();
        public bool TradingFood = true;
        public bool TradingProd = true;
        public bool ShieldsUp   = true;
        //public float AfterBurnerAmount = 20.5f;    //Not referenced in code, removing to save memory
        //protected Color CloakColor = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);    //Not referenced in code, removing to save memory
        //public float CloakTime = 5f;    //Not referenced in code, removing to save memory
        //public Vector2 Origin = new Vector2(256f, 256f);        //Not referenced in code, removing to save memory
        private Array<Projectile> projectiles = new Array<Projectile>();
        private Array<Beam> beams             = new Array<Beam>();
        public Array<Weapon> Weapons          = new Array<Weapon>();
        private float JumpTimer               = 3f;
        public AudioEmitter SoundEmitter      = new AudioEmitter();
        public Vector2 ScreenPosition         = new Vector2();
        public float ScuttleTimer             = -1f;
        public Vector2 FleetOffset;
        public Vector2 RelativeFleetOffset;
        //public float ClickTimer = 10f;    //Never used

        private ShipModule[] Shields;
        public Array<ShipModule> BombBays = new Array<ShipModule>();
        public CarrierBays Carrier;
        public ShipResupply Supply;
        public bool shipStatusChanged;
        public Guid guid = Guid.NewGuid();
        public bool AddedOnLoad;
        private AnimationController ShipMeshAnim;
        public bool IsPlayerDesign;
        public bool IsSupplyShip;
        public bool IsReadonlyDesign;
        public bool isColonyShip;
        public bool isConstructor;
        public string StrategicIconPath;
        private Planet TetheredTo;
        public Vector2 TetherOffset;
        public Guid TetherGuid;
        public float EMPDamage;
        public Fleet fleet;
        //public string DesignUID;
        public float yRotation;
        public float RotationalVelocity;
        public float MechanicalBoardingDefense;
        public float TroopBoardingDefense;
        public float ECMValue;
        public ShipData shipData;
        public int kills;
        public float experience;
        public bool EnginesKnockedOut;
        //protected float ThrustLast;    //Not referenced in code, removing to save memory
        public float InCombatTimer;
        public bool isTurning;
        //public bool PauseUpdate;      //Not used in code, removing to save memory
        public float InhibitionRadius;
        private KeyboardState lastKBState;
        private KeyboardState currentKeyBoardState;
        public bool IsPlatform;
        private SceneObject ShipSO;
        public bool ManualHangarOverride;
        public Fleet.FleetCombatStatus FleetCombatStatus;
        public Ship Mothership;
        public bool isThrusting;
        public string Name;   // name of the original design of the ship, eg "Subspace Projector". Look at VanityName
        public float PackDamageModifier { get; private set; }
        public Empire loyalty;
        public int Size;
        //public int CrewRequired;    //Not referenced in code, removing to save memory
        //public int CrewSupplied;    //Not referenced in code, removing to save memory
        public float Ordinance;
        public float OrdinanceMax;
        //public float scale;    //Not referenced in code, removing to save memory
        public ShipAI AI { get; private set; }
        public float Speed;
        public float Thrust;
        public float velocityMaximum;
        //public double armor_percent;    //Not referenced in code, removing to save memory
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
        public float rotationRadiansPerSecond;
        public bool FromSave;
        public bool HasRepairModule;
        private AudioHandle Afterburner;
        public bool isSpooling;
        //protected SolarSystem JumpTarget;   //Not referenced in code, removing to save memory
        //protected Cue hyperspace;           //Removed to save space, because this is set to null in ship initilizer, and never reassigned. -Gretman
        //protected Cue hyperspace_return;    //Not referenced in code, removing to save memory
        private AudioHandle JumpSfx;
        public float InhibitedTimer;
        public int Level;
        public bool PlayerShip;
        private int MaxHealthRevision;
        public float HealthMax { get; private set; }
        public float ShipMass;
        public int TroopCapacity;
        public float OrdAddedPerSecond;
        //public bool WeaponCentered;    //Not referenced in code, removing to save memory
        private AudioHandle DroneSfx;
        public float ShieldRechargeTimer;
        public bool InCombat;
        public float xRotation;
        public MoveState engineState;
        public float ScreenRadius;
        //public float ScreenSensorRadius;    //Not referenced in code, removing to save memory
        public bool InFrustum;
        public bool NeedRecalculate;
        public bool Deleted;
        //public float CargoMass;    //Not referenced in code, removing to save memory
        public bool inborders;
        public bool FightersLaunched { get; private set; }
        public bool TroopsLaunched { get; private set; }
        public bool Inhibited;
        private float BonusEMP_Protection;
        public bool inSensorRange;
        public bool EMPdisabled;
        private float updateTimer;
        public float WarpThrust;
        public float TurnThrust;
        public float maxWeaponsRange;
        public float MoveModulesTimer;
        public float HealPerTurn;
        private bool UpdatedModulesOnce;
        public float InternalSlotsHealthPercent; // number_Alive_Internal_slots / number_Internal_slots
        private float xdie;
        private float ydie;
        private float zdie;
        private float dietimer;
        public float BaseStrength;
        public bool BaseCanWarp;
        public bool dying;
        private bool reallyDie;
        private bool HasExploded;
        public float FTLSpoolTime;
        public bool FTLSlowTurnBoost;

        public Array<ShipModule> RepairBeams = new Array<ShipModule>();
        public bool hasRepairBeam;
        public bool hasCommand;

        public float RangeForOverlay;
        public ReaderWriterLockSlim supplyLock = new ReaderWriterLockSlim();
        Array<ShipModule> AttackerTargetting   = new Array<ShipModule>();
        public int TrackingPower;
        public int FixedTrackingPower;
        public Ship lastAttacker = null;
        //private bool LowHealth; //fbedard: recalculate strength after repair - FB: commented since its not used.
        public float TradeTimer;
        public bool ShipInitialized;
        public float maxFTLSpeed;
        public float maxSTLSpeed;
        public float NormalWarpThrust;
        public float BoardingDefenseTotal => (MechanicalBoardingDefense + TroopBoardingDefense);
        public Array<Empire> BorderCheck  = new Array<Empire>();

        public float FTLModifier { get; private set; } = 1f;
        public float BaseCost { get; private set; }

        public GameplayObject[] GetObjectsInSensors(GameObjectType filter = GameObjectType.None, float radius = float.MaxValue)
        {
            radius = Math.Min(radius, SensorRange);
            return UniverseScreen.SpaceManager.FindNearby(this, radius, filter);
        }
        public bool IsInNeutralSpace
        {
            get
            {
                for (int i = 0; i < BorderCheck.Count; ++i)
                {
                    Empire e = BorderCheck[i];
                    if (e == loyalty)
                        return false;
                    Relationship rel = loyalty.GetRelations(e);
                    if (rel.AtWar || rel.Treaty_Alliance)
                        return false;
                }
                return true;
            }
        }
        public bool IsInFriendlySpace
        {
            get
            {
                for (int i = 0; i < BorderCheck.Count; ++i)
                {
                    Empire e = BorderCheck[i];
                    if (e == loyalty || loyalty.GetRelations(e).Treaty_Alliance)
                        return true;
                }
                return false;
            }
        }

        public bool IsIndangerousSpace
        {
            get
            {
                for (int i = 0; i < BorderCheck.Count; ++i)
                    if (loyalty?.GetRelations(BorderCheck[i])?.AtWar == true)
                        return true;
                return false;
            }
        }

        public float EmpTolerance => Size + BonusEMP_Protection;
        public float EmpRecovery => 1 + BonusEMP_Protection / 1000;
        public float HealthPercent => Health / HealthMax;

        public void DebugDamage(float percent)
        {
            percent = percent.Clamped(0f, 1f);
            foreach (ShipModule module in ModuleSlotList)            
                module.DebugDamage(percent);            
        }

        public string WarpState => engineState == MoveState.Warp ? "FTL" : "Sublight";

        public ShipData.RoleName DesignRole { get; private set; }
        public string DesignRoleName => ShipData.GetRole(DesignRole);
        public Texture2D GetTacticalIcon()
        {
            if (DesignRole == ShipData.RoleName.support)
                return ResourceManager.Texture("TacticalIcons/symbol_supply");

            string roleName = DesignRole.ToString();
            string iconName = "TacticalIcons/symbol_";
            return ResourceManager.Texture(iconName + roleName, "") ??
                ResourceManager.Texture(iconName + shipData.HullRole, "TacticalIcons/symbol_construction");
        }

        private int Calculatesize()
        {
            int size = 0;
            
            for (int x = 0; x < SparseModuleGrid.Length; x++)
            {
                var gridPoint = SparseModuleGrid[x];
                if (gridPoint == null) continue;
                size++;
            }

            return size;
        }

        private float GetyBankAmount(float yBank)
        {
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

        public void CauseEmpDamage(float empDamage) => EMPDamage += empDamage;

        public void CausePowerDamage(float powerDamage) => PowerCurrent = (PowerCurrent - powerDamage).Clamped(0, PowerStoreMax);

        public void AddPower(float powerAcquired) => PowerCurrent = (PowerCurrent + powerAcquired).Clamped(0, PowerStoreMax);

        public void CauseTroopDamage(float troopDamageChance)
        {
            if (TroopList.Count > 0)
            {
                if (UniverseRandom.RandomBetween(0f, 100f) < troopDamageChance)
                {
                    TroopList[0].Strength = TroopList[0].Strength - 1;
                    if (TroopList[0].Strength <= 0)
                        TroopList.RemoveAt(0);
                }
            }
            else if (MechanicalBoardingDefense > 0f && RandomMath.RandomBetween(0f, 100f) < troopDamageChance)
                MechanicalBoardingDefense -= 1f;
        }

        public void CauseRepulsionDamage(Beam beam)
        {
            if (IsTethered() || EnginesKnockedOut)
                return;
            if (beam.Owner == null || beam.Weapon == null)
                return;
            Velocity += ((Center - beam.Owner.Center) * beam.Weapon.RepulsionDamage) / Mass;
        }

        public void CauseMassDamage(float massDamage)
        {
            if (IsTethered() || EnginesKnockedOut)
                return;
            Mass += massDamage;
            velocityMaximum = Thrust / Mass;
            Speed = velocityMaximum;
            rotationRadiansPerSecond = Speed / 700f;
            shipStatusChanged = true; 
        }

        public override bool IsAttackable(Empire attacker, Relationship attackerRelationThis)
        {
            if (attackerRelationThis.Treaty_NAPact) return false;
            if (AI.Target?.GetLoyalty() == attacker)
            {
                //if (!InCombat) Log.Info($"{attacker.Name} : Is being attacked by : {loyalty.Name}");
                return true;
            }

            if (attacker.isPlayer) return true;

            if (attackerRelationThis.AttackForTransgressions(attacker.data.DiplomaticPersonality))
            {
                //if (!InCombat) Log.Info($"{attacker.Name} : Has filed transgressions against : {loyalty.Name} ");
                return true;
            }
            if (isColonyShip && System != null && attackerRelationThis.WarnedSystemsList.Contains(System.guid)) return true;
            if ((DesignRole == ShipData.RoleName.troop || DesignRole == ShipData.RoleName.troop)
                && System != null && attacker.GetOwnedSystems().Contains(System)) return true;
            //the below does a search for being inborders so its expensive. 
            if (attackerRelationThis.AttackForBorderViolation(attacker.data.DiplomaticPersonality)
                && attacker.GetGSAI().ThreatMatrix.ShipInOurBorders(this))
            {
                //if (!InCombat) Log.Info($"{attacker.Name} : Has filed border violations against : {loyalty.Name}  ");
                return true;
            }

            return false;
        }

        public override Vector2 JitterPosition()
        {
            Vector2 jitter = Vector2.Zero;
            if (CombatDisabled)
                return jitter;
            
            if (ECMValue >0)            
                jitter += RandomMath2.Vector2D(ECMValue *80f);
            
            if (loyalty.data.Traits.DodgeMod >0)            
                jitter += RandomMath2.Vector2D(loyalty.data.Traits.DodgeMod * 80f);
            
            return jitter;
        }

        public bool CombatDisabled => EMPdisabled || dying || !Active || !hasCommand;        
      
        public IReadOnlyList<Projectile> Projectiles => projectiles;
        public IReadOnlyList<Beam> Beams => beams;

        public void AddBeam(Beam beam)
        {
            if (beam == null || beams.ContainsRef(beam)) {
                Log.Error($"Invalid beam: {beam}");
                return;
            }
            beams.Add(beam);
        }
        public void RemoveBeam(Beam beam)
        {
            if (beam == null || !beams.ContainsRef(beam)) {
                Log.Error($"Invalid beam: {beam}");
                return;
            }
            beams.RemoveRef(beam);
        }
        public void AddProjectile(Projectile projectile)
        {
            if (projectile == null || beams.ContainsRef(projectile)) {
                Log.Error($"Invalid projectile: {projectile}");
                return;
            }
            projectiles.Add(projectile);
        }

        public bool SupplyShipCanSupply => Carrier.HasSupplyBays && OrdnanceStatus > ShipStatus.Critical
                                                                 && OrdnanceStatus != ShipStatus.NotApplicable;

        public ShipStatus OrdnanceStatus
        {
            get
            {
                if ( engineState        == MoveState.Warp
                    || AI.State         == AIState.Scrap
                    || AI.State         == AIState.Resupply
                    || AI.State         == AIState.Refit || Mothership != null
                    || shipData.Role    == ShipData.RoleName.supply || shipData.HullRole < ShipData.RoleName.fighter
                    || OrdinanceMax < 1
                    || IsTethered())                
                    return ShipStatus.NotApplicable;
                
                return ToShipStatus(Ordinance, OrdinanceMax);
            }

        }

        public int BombCount
        {
            get
            {
                int Bombs = 0;
                if (BombBays.Count > 0)
                {
                    ++Bombs;
                    if (Ordinance / OrdinanceMax > 0.2f)
                    {
                        Bombs += BombBays.Count;
                    }
                }
                return Bombs;
            }

        }

        public bool FightersOut
        {
            get => FightersLaunched;
            set
            {
                if (engineState == MoveState.Warp || isSpooling)
                {
                    GameAudio.PlaySfxAsync("UI_Misc20"); // dont allow changing button state if the ship is spooling or at warp
                    return;
                }
                FightersLaunched = value;
                if (FightersLaunched)
                    Carrier.ScrambleFighters();
                else
                    Carrier.RecoverFighters();
            }
        }

        public bool DoingTransport
        {
            get => AI.State == AIState.SystemTrader;
            set
            {
                TransportingProduction = value;
                TransportingFood = value;
                if (!value) return;
                if (AI.State != AIState.SystemTrader)
                {
                    AI.start = null;
                    AI.end = null;
                    AI.State = AIState.SystemTrader;                    
                }
                
                AI.OrderTrade(0f);
            }
        }

        public bool DoingFoodTransport => AI.State == AIState.SystemTrader && TransportingFood;
        public bool DoingProdTransport => AI.State == AIState.SystemTrader && TransportingProduction;

        public bool DoingPassTransport
        {
            get => AI.State == AIState.PassengerTransport;
            set
            {
                AI.start = null;
                AI.end = null;
                AI.OrderTransportPassengers(5f);
            }
        }
        private bool TFood = false;
        public bool TransportingFood
        {
            get => AI.State != AIState.SystemTrader || TFood;
            set
            {
                TFood = value;
                if (!value)
                {
                    if (!TransportingProduction)
                    {
                        AI.State = AIState.AwaitingOrders;
                    }
                    return;
                }
                if (AI.State == AIState.SystemTrader)
                {
                    AI.OrderTrade(0);
                    return;
                }
                AI.start = null;
                AI.end = null;
                AI.State = AIState.SystemTrader;
                //AI.OrderTrade(0);
            }
        }
        private bool TProd = false;
        public bool TransportingProduction
        {
            get => AI.State != AIState.SystemTrader || TProd;
            set
            {
                TProd = value;
                if (!value)
                {
                    if (!TransportingFood)
                    {
                        AI.State = AIState.AwaitingOrders;
                    }
                    return;
                }
                if (AI.State == AIState.SystemTrader)
                {
                    AI.OrderTrade(0);
                    return;
                }
                AI.start = null;
                AI.end = null;
                AI.State = AIState.SystemTrader;
                //AI.OrderTrade(0);
            }
        }

        public bool DoingExplore
        {
            get => AI.State == AIState.Explore;
            set => AI.OrderExplore();
        }

        public bool DoingResupply
        {
            get => AI.State == AIState.Resupply;
            set => AI.GoOrbitNearestPlanetAndResupply(true);
        }

        public bool DoingSystemDefense
        {
            get => loyalty.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this);
            set
            {
                //added by gremlin Toggle Ship System Defense.
                if (EmpireManager.Player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this))
                {
                    EmpireManager.Player.GetGSAI().DefensiveCoordinator.Remove(this);
                    AI.OrderQueue.Clear();
                    AI.HasPriorityOrder = false;
                    AI.State = AIState.AwaitingOrders;

                    return;
                }
                EmpireManager.Player.GetGSAI().DefensiveCoordinator.AddShip(this);
                AI.State = AIState.SystemDefender;
            }
        }

        public bool TroopsOut
        {
            get => TroopsLaunched;
            set
            {
                if (engineState == MoveState.Warp || isSpooling)
                {
                    GameAudio.PlaySfxAsync("UI_Misc20"); // dont allow changing button state if the ship is spooling or at warp
                    return;
                }
                TroopsLaunched = value;
                if (TroopsLaunched)
                    Carrier.ScrambleAllAssaultShips();
                else
                    Carrier.RecoverAssaultShips();
            }
        }

        public bool doingScrap
        {
            get => AI.State == AIState.Scrap;
            set => AI.OrderScrapShip();
        }

        public bool doingRefit
        {
            get => AI.State == AIState.Refit;
            set => Empire.Universe.ScreenManager.AddScreen(new RefitToWindow(Empire.Universe, this));
        }

        public void ShipRecreate()
        {
            Active = false;
            AI.Target = null;
            AI.ColonizeTarget = null;
            AI.EscortTarget = null;
            AI.start = null;
            AI.end = null;
            AI.PotentialTargets.Clear();
            AI.NearByShips.Clear();
            AI.FriendliesNearby.Clear();

            if (Mothership != null)
            {
                foreach (ShipModule shipModule in Mothership.Carrier.AllActiveHangars)
                {
                    if (shipModule.GetHangarShip() == this)
                        shipModule.SetHangarShip(null);
                }
            }

            for (int i = 0; i < projectiles.Count; ++i)
                projectiles[i].Die(this, false);
            projectiles.Clear();

            ModuleSlotList = Empty<ShipModule>.Array;
            TroopList.Clear();
            ClearFleet();
            ShipSO.Clear();

            loyalty.RemoveShip(this);
            SetSystem(null);
            TetheredTo = null;
        }

        public bool IsWithinPlanetaryGravityWell
        {
            get
            {
                if (!Empire.Universe.GravityWells || System == null || IsInFriendlySpace)
                    return false;

                for (int i = 0; i < System.PlanetList.Count; i++)
                {
                    Planet planet = System.PlanetList[i];
                    if (Position.InRadius(planet.Center, planet.GravityWellRadius))
                        return true;
                }
                return false;
            }
        }

        public float AvgProjectileSpeed
        {
            get
            {
                if (Weapons.IsEmpty)
                    return 800f;
                int count = 0;
                float speed = 0f;
                foreach (Weapon weapon in Weapons)
                {
                    if (weapon.isBeam) continue;
                    speed += weapon.ProjectileSpeed;
                    ++count;
                }
                return speed / count;
            }
        }

        //added by gremlin The Generals GetFTL speed
        public void SetmaxFTLSpeed()
        {
            //Added by McShooterz: hull bonus speed 
            if (InhibitedTimer < -0.25f || Inhibited || System != null && engineState == MoveState.Warp)
            {
                if (IsWithinPlanetaryGravityWell) InhibitedTimer = 0.3f;
                else if (InhibitedTimer < 0.0f)   InhibitedTimer = 0.0f;
            }
            //Apply in borders bonus through ftl modifier
            float ftlmodtemp = 1;

            //Change FTL modifier for ship based on solar system
            {
                if (System != null) // && ( || ))
                {
                    if (IsInFriendlySpace) // && Empire.Universe.FTLModifier < 1)
                        ftlmodtemp = Empire.Universe.FTLModifier;
                    else if (IsIndangerousSpace || !Empire.Universe.FTLInNuetralSystems) // && Empire.Universe.EnemyFTLModifier < 1)
                    {
                        ftlmodtemp = Empire.Universe.EnemyFTLModifier;
                    }

                }
            }
            FTLModifier = 1;
            if (inborders && loyalty.data.Traits.InBordersSpeedBonus > 0)
                FTLModifier += loyalty.data.Traits.InBordersSpeedBonus;
            FTLModifier *= ftlmodtemp;
            maxFTLSpeed = (WarpThrust / base.Mass + WarpThrust / base.Mass * (loyalty?.data?.FTLModifier ?? 35)) * FTLModifier;


        }
        public float GetmaxFTLSpeed => maxFTLSpeed;


        public float GetSTLSpeed()
        {
            //Added by McShooterz: hull bonus speed
            float thrustWeightRatio = Thrust / Mass;
            float speed = thrustWeightRatio + thrustWeightRatio * loyalty.data.SubLightModifier;
            return Math.Min(speed, 2500);
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
            if (shipData.HasFixedCost)
                return shipData.FixedCost;

            float cost = 0.0f;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
                cost += ModuleSlotList[i].Cost * UniverseScreen.GamePaceStatic;

            if (empire != null)
            {
                cost += shipData.Bonuses.StartingCost;
                cost += cost * empire.data.Traits.ShipCostMod;
                cost *= 1f - shipData.Bonuses.CostBonus; // @todo Sort out (1f - CostBonus) weirdness
            }
            return (int)cost;
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
            AI.HasPriorityOrder = true;
        }

        public void AttackShip(Ship target)
        {
            AI.State = AIState.AttackTarget;
            AI.Target = target;
            AI.HasPriorityOrder = false;
            AI.HasPriorityTarget = true;
            InCombatTimer = 15f;
        }
        /// <summary>
        /// forces the ship to be in combat without a target.
        /// </summary>
        /// <param name="timer"></param>
        public void ForceCombatTimer(float timer = 15f) => InCombatTimer = timer;

        public void ProcessInput(float elapsedTime)
        {
            if (GlobalStats.TakingInput || EMPdisabled || !hasCommand)
                return;
            if (Empire.Universe.Input != null)
                currentKeyBoardState = Empire.Universe.Input.KeysCurr;
            if (currentKeyBoardState.IsKeyDown(Keys.D))
                AI.State = AIState.ManualControl;
            if (currentKeyBoardState.IsKeyDown(Keys.A))
                AI.State = AIState.ManualControl;
            if (currentKeyBoardState.IsKeyDown(Keys.W))
                AI.State = AIState.ManualControl;
            if (currentKeyBoardState.IsKeyDown(Keys.S))
                AI.State = AIState.ManualControl;
            if (AI.State == AIState.ManualControl)
            {
                if (Active && !currentKeyBoardState.IsKeyDown(Keys.LeftControl))
                {
                    isThrusting = false;
                    Vector2 vector2_1 = new Vector2((float)Math.Sin(Rotation), -(float)Math.Cos(Rotation));
                    Vector2 vector2_2 = new Vector2(-vector2_1.Y, vector2_1.X);
                    if (currentKeyBoardState.IsKeyDown(Keys.D))
                    {
                        isThrusting = true;
                        RotationalVelocity += rotationRadiansPerSecond * elapsedTime;
                        isTurning = true;
                        if (RotationalVelocity > rotationRadiansPerSecond)
                            RotationalVelocity = rotationRadiansPerSecond;
                        if (yRotation > -MaxBank)
                            yRotation -= yBankAmount;
                    }
                    else if (currentKeyBoardState.IsKeyDown(Keys.A))
                    {
                        isThrusting = true;
                        RotationalVelocity -= rotationRadiansPerSecond * elapsedTime;
                        isTurning = true;
                        if (Math.Abs(RotationalVelocity) > rotationRadiansPerSecond)
                            RotationalVelocity = -rotationRadiansPerSecond;
                        if (yRotation < MaxBank)
                            yRotation += yBankAmount;
                    }
                    else if (engineState == Ship.MoveState.Warp)
                    {
                        isSpooling = true;
                        isTurning = false;
                        isThrusting = true;
                        Vector2.Normalize(vector2_1);
                        Ship ship1 = this;
                        Vector2 vector2_3 = ship1.Velocity + vector2_1 * (elapsedTime * Speed);
                        ship1.Velocity = vector2_3;
                        if (Velocity.Length() > velocityMaximum)
                            Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
                        if (Velocity.LengthSquared() <= 0.0)
                            Velocity = Vector2.Zero;
                        if (yRotation > 0.0)
                            yRotation -= yBankAmount;
                        else if (yRotation < 0.0)
                            yRotation += yBankAmount;
                        if (RotationalVelocity > 0.0)
                        {
                            isTurning = true;
                            RotationalVelocity -= rotationRadiansPerSecond * elapsedTime;
                            if (RotationalVelocity < 0.0)
                                RotationalVelocity = 0.0f;
                        }
                        else if (RotationalVelocity < 0.0)
                        {
                            isTurning = true;
                            RotationalVelocity += rotationRadiansPerSecond * elapsedTime;
                            if (RotationalVelocity > 0.0)
                                RotationalVelocity = 0.0f;
                        }
                    }
                    else
                    {
                        isTurning = false;
                        if (yRotation > 0.0)
                        {
                            yRotation -= yBankAmount;
                            if (yRotation < 0.0)
                                yRotation = 0.0f;
                        }
                        else if (yRotation < 0.0)
                        {
                            yRotation += yBankAmount;
                            if (yRotation > 0.0)
                                yRotation = 0.0f;
                        }
                        if (RotationalVelocity > 0.0)
                        {
                            isTurning = true;
                            RotationalVelocity -= rotationRadiansPerSecond * elapsedTime;
                            if (RotationalVelocity < 0.0)
                                RotationalVelocity = 0.0f;
                        }
                        else if (RotationalVelocity < 0.0)
                        {
                            isTurning = true;
                            RotationalVelocity += rotationRadiansPerSecond * elapsedTime;
                            if (RotationalVelocity > 0.0)
                                RotationalVelocity = 0.0f;
                        }
                        isThrusting = false;
                    }
                    if (Velocity.Length() > velocityMaximum)
                        Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
                    if (currentKeyBoardState.IsKeyDown(Keys.F) && !lastKBState.IsKeyDown(Keys.F))
                    {
                        if (!isSpooling)
                            EngageStarDrive();
                        else
                            HyperspaceReturn();
                    }
                    if (currentKeyBoardState.IsKeyDown(Keys.W))
                    {
                        isThrusting = true;
                        Ship ship = this;
                        Vector2 vector2_3 = ship.Velocity + vector2_1 * (elapsedTime * Speed);
                        ship.Velocity = vector2_3;
                        if (Velocity.Length() > velocityMaximum)
                            Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
                    }
                    else if (currentKeyBoardState.IsKeyDown(Keys.S))
                    {
                        isThrusting = true;
                        Ship ship = this;
                        Vector2 vector2_3 = ship.Velocity - vector2_1 * (elapsedTime * Speed);
                        ship.Velocity = vector2_3;
                        if (Velocity.Length() > velocityMaximum)
                            Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
                    }
                    MouseState state = Mouse.GetState();
                    if (state.RightButton == ButtonState.Pressed)
                    {
                        Vector2 pickedPos = Empire.Universe.UnprojectToWorldPosition(new Vector2(state.X, state.Y));
                        foreach (Weapon w in Weapons)
                            w.MouseFireAtTarget(pickedPos);
                    }
                }
                else GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
            }
            lastKBState = currentKeyBoardState;
        }
        public bool CheckRangeToTarget(Weapon w, GameplayObject target)
        {
            if (target == null || !target.Active || target.Health <= 0)
                return false;
            if (engineState == MoveState.Warp)
                return false;

            var targetship = target as Ship;
            var targetModule = target as ShipModule;
            if (targetship == null && targetModule != null)
                targetship = targetModule.GetParent();
            if (targetship == null && targetModule == null && w.isBeam)
                return false;
            if (targetship != null)
            {
                if (targetship.engineState == MoveState.Warp
                    || targetship.dying
                    || !targetship.Active
                    || targetship.NumExternalSlots <= 0
                    //|| !w.TargetValid(targetship.shipData.HullRole)

                    )
                    return false;
            }
            float attackRunRange = 50f;
            if (w.FireTarget != null && !w.isBeam && AI.CombatState == CombatState.AttackRuns && maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                attackRunRange = Speed;
                if (attackRunRange < 50f)
                    attackRunRange = 50f;
            }

            return target.Center.InRadius(w.Module.Center, w.GetModifiedRange() + attackRunRange);
        }
        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, GameplayObject target)
        {
            if (!CheckRangeToTarget(w, target))
                return false;

            if (target is Ship targetShip)
            {
                if (w.MassDamage > 0 || w.RepulsionDamage > 0)
                {
                    if (targetShip.EnginesKnockedOut || targetShip.IsTethered())
                        return false;
                }
                if ((loyalty == targetShip.loyalty || !loyalty.isFaction &&
                                           loyalty.TryGetRelations(targetShip.loyalty, out Relationship enemy) && enemy.Treaty_NAPact))
                    return false;
            }

            float halfArc = w.Module.FieldOfFire / 2f;

            Vector2 toTarget = target.Center - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians); //HelperFunctions.AngleToTarget(w.Center, target.Center);//
            float facing = w.Module.Facing + MathHelper.ToDegrees(base.Rotation);


            if (facing > 360f)
            {
                facing = facing - 360f;
            }
            float difference = 0f;
            difference = Math.Abs(angleToMouse - facing);
            if (difference > halfArc)
            {
                if (angleToMouse > 180f)
                {
                    angleToMouse = -1f * (360f - angleToMouse);
                }
                if (facing > 180f)
                {
                    facing = -1f * (360f - facing);
                }
                difference = Math.Abs(angleToMouse - facing);
            }

            if (difference < halfArc)// && Vector2.Distance(base.Position, pos) < w.GetModifiedRange() + modifyRangeAR)
            {
                return true;
            }
            return false;
        }


        public bool CheckIfInsideFireArc(Weapon w, Vector2 pos)
        {
            //added by gremlin attackrun compensator

            if (w.Module.Center.OutsideRadius(pos, w.GetModifiedRange()))
            {
                return false;
            }

            float halfArc = w.Module.FieldOfFire / 2f;
            Vector2 toTarget = pos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians);
            float facing = w.Module.Facing + MathHelper.ToDegrees(base.Rotation);
            if (facing > 360f)
            {
                facing = facing - 360f;
            }
            float difference = Math.Abs(angleToMouse - facing);
            if (difference > halfArc)
            {
                if (angleToMouse > 180f)
                {
                    angleToMouse = -1f * (360f - angleToMouse);
                }
                if (facing > 180f)
                {
                    facing = -1f * (360f - facing);
                }
                difference = Math.Abs(angleToMouse - facing);
            }

            if (difference < halfArc)// && Vector2.Distance(base.Position, pos) < w.GetModifiedRange() + modifyRangeAR)
            {
                return true;
            }
            return false;
        }

        public bool CheckIfInsideFireArc(Weapon w, Vector3 PickedPos)
        {

            //added by gremlin attackrun compensator
            float modifyRangeAR = 50f;
            Vector2 pos = new Vector2(PickedPos.X, PickedPos.Y);
            if (!w.isBeam && AI.CombatState == CombatState.AttackRuns && maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                modifyRangeAR = Speed;
                if (modifyRangeAR < 50)
                    modifyRangeAR = 50;
            }
            if (Vector2.Distance(pos, w.Module.Center) > w.GetModifiedRange() + modifyRangeAR)
            {
                return false;
            }

            float halfArc = w.Module.FieldOfFire / 2f;
            Vector2 toTarget = pos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians);
            float facing = w.Module.Facing + MathHelper.ToDegrees(base.Rotation);
            if (facing > 360f)
            {
                facing = facing - 360f;
            }
            float difference = 0f;
            difference = Math.Abs(angleToMouse - facing);
            if (difference > halfArc)
            {
                if (angleToMouse > 180f)
                {
                    angleToMouse = -1f * (360f - angleToMouse);
                }
                if (facing > 180f)
                {
                    facing = -1f * (360f - facing);
                }
                difference = Math.Abs(angleToMouse - facing);
            }

            if (difference < halfArc)// && Vector2.Distance(base.Position, pos) < w.GetModifiedRange() + modifyRangeAR)
            {
                return true;
            }
            return false;
        }

        // Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Ship ship)
        {
            Vector2 pickedPos = ship.Center;
            float radius = ship.Radius;
            ++GlobalStats.WeaponArcChecks;
            float modifyRangeAr = 50f;
            float distance = w.Module.Center.Distance(pickedPos);

            if (w.MassDamage > 0 || w.RepulsionDamage > 0)
            {
                if (ship.EnginesKnockedOut || ship.IsTethered())
                    return false;
            }

            if (!w.isBeam && AI.CombatState == CombatState.AttackRuns && w.SalvoTimer > 0 && distance / w.SalvoTimer < w.Owner.Speed) //&& this.maxWeaponsRange < 2000
            {
                modifyRangeAr = Math.Max(Speed * w.SalvoTimer, 50f);
            }
            if (distance > w.GetModifiedRange() + modifyRangeAr + radius)
                return false;

            float halfArc = w.Module.FieldOfFire / 2f;
            Vector2 toTarget = pickedPos - w.Center;
            float radians = (float)Math.Atan2(toTarget.X, toTarget.Y);
            float angleToMouse = 180f - radians.ToDegrees();
            float facing = w.Module.Facing + Rotation.ToDegrees();
            if (facing > 360f)
            {
                facing = facing - 360f;
            }
            float difference = Math.Abs(angleToMouse - facing);
            if (!(difference > halfArc)) return difference < halfArc;
            if (angleToMouse > 180f)
            {
                angleToMouse = -1f * (360f - angleToMouse);
            }
            if (facing > 180f)
            {
                facing = -1f * (360f - facing);
            }
            difference = Math.Abs(angleToMouse - facing);
            return difference < halfArc;
        }

        // Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Vector2 pickedPos, float rotation, bool skipRangeCheck = false)
        {
            if (!skipRangeCheck && w.Module.Center.OutsideRadius(pickedPos, w.GetModifiedRange() + 50f))
                return false;

            float halfArc = w.Module.FieldOfFire / 2f + 1; //Gretman - Slight allowance for check (This version of CheckArc seems to only be called by the beam updater)
            Vector2 toTarget = pickedPos - w.Center;
            float radians = (float)Math.Atan2(toTarget.X, toTarget.Y);
            float angleToMouse = 180f - radians.ToDegrees();
            float facing = w.Module.Facing + rotation.ToDegrees();
            if (facing > 360f)
            {
                facing = facing - 360f;
            }
            float difference = Math.Abs(angleToMouse - facing);
            if (difference > halfArc)
            {
                if (angleToMouse > 180f)
                {
                    angleToMouse = -1f * (360f - angleToMouse);
                }
                if (facing > 180f)
                {
                    facing = -1f * (360f - facing);
                }
                difference = Math.Abs(angleToMouse - facing);
            }
            return difference < halfArc;
        }

        public SceneObject GetSO()
        {
            return ShipSO;
        }

        public void ReturnToHangar()
        {
            if (Mothership == null || !Mothership.Active)
                return;
            AI.State = AIState.ReturnToHangar;
            AI.OrderReturnToHangar();
        }

        // ModInfo activation option for Maintenance Costs:

        private bool IsFreeUpkeepShip(ShipData.RoleName role, Empire empire)
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

        public static float GetMaintenanceModifier(ShipData shipData, Empire empire)
        {
            ShipData.RoleName role = shipData.Role;
            float maint = GetModMaintenanceModifier(role);

            if (maint <= 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0f)
            {
                maint = GlobalStats.ActiveModInfo.UpkeepBaseline;
            }

            // Direct override in ShipDesign XML, e.g. for Shipyards/pre-defined designs with specific functions.
            if (empire != null && shipData.HasFixedUpkeep)
            {
                maint = shipData.FixedUpkeep;
            }

            // Doctor: Configurable civilian maintenance modifier.
            if ((role == ShipData.RoleName.freighter || role == ShipData.RoleName.platform) && empire?.isFaction == false)
            {
                maint *= empire.data.CivMaintMod;
                maint *= empire.data.Privatization ? 0.5f : 1.0f;
            }

            if (GlobalStats.ShipMaintenanceMulti > 1)
                maint *= GlobalStats.ShipMaintenanceMulti;

            if (empire != null)
            {
                maint += maint * empire.data.Traits.MaintMod;
            }
            return maint;
        }

        public float GetMaintCostRealism() => GetMaintCostRealism(loyalty);

        public float GetMaintCostRealism(Empire empire)
        {
            if (IsFreeUpkeepShip(shipData.Role, loyalty))
                return 0f;

            float shipCost = GetCost(empire);
            return shipCost * GetMaintenanceModifier(shipData, empire);
        }

        public float GetMaintCost() => GetMaintCost(loyalty);

        public float GetMaintCost(Empire empire)
        {
            int numShipYards = IsTethered() ? GetTether().Shipyards.Count(shipyard => shipyard.Value.shipData.IsShipyard) : 0;
            return GetMaintenanceCost(this, empire, numShipYards: numShipYards);
        }

        public int GetTechScore(out int[] techScores) // FB: move this to ship init if possible - its a constant thing which can be calculated one time.
        {
            int [] scores = new int[4];
            scores[0] = 0;
            scores[1] = 0;
            scores[2] = 0;
            scores[3] = 0;
            foreach (ShipModule module in ModuleSlotList)
            {
                switch (module.ModuleType) // FB: using main module type since we want the main module funcion here
                {                    
                    case ShipModuleType.Turret:
                    case ShipModuleType.MainGun:
                    case ShipModuleType.MissileLauncher:
                    case ShipModuleType.Bomb:       scores[2] = Math.Max(scores[2], module.TechLevel); continue;
                    case ShipModuleType.PowerPlant: scores[3] = Math.Max(scores[3], module.TechLevel); continue;
                    case ShipModuleType.Engine:     scores[1] = Math.Max(scores[1], module.TechLevel); continue;
                    case ShipModuleType.Armor:
                    case ShipModuleType.Shield:     scores[0] = Math.Max(scores[0], module.TechLevel); continue;
                }
            }
            techScores = scores;
            return scores[0] + scores[1] + scores[3] + scores[2];
        }        

        public void DoEscort(Ship escortTarget)
        {
            AI.OrderQueue.Clear();
            AI.State        = AIState.Escort;
            AI.EscortTarget = escortTarget;
        }

        public void DoDefense()
        {
            AI.State = AIState.SystemDefender;
        }

        public void DoDefense(SolarSystem toDefend)
        {
            AI.SystemToDefend = toDefend;
            AI.State = AIState.SystemDefender;
        }

        public void DefendSystem(SolarSystem toDefend)
        {
            AI.State = AIState.SystemDefender;
            AI.SystemToDefend = toDefend;
        }

        public void DoOrbit(Planet orbit)
        {
            AI.OrderToOrbit(orbit, true);
        }

        public void DoExplore()
        {
            AI.OrderExplore();
        }

        public void DoColonize(Planet p, Goal g)
        {
            AI.OrderColonization(p);
        }

        public void DoTrading()
        {
            AI.State = AIState.SystemTrader;
        }

        public void ResetJumpTimer()
        {
            JumpTimer = FTLSpoolTime * loyalty.data.SpoolTimeModifier;
        } 

        
        public void EngageStarDrive() // added by gremlin: Fighter recall and stuff
        {
            if (isSpooling || engineState == MoveState.Warp || GetmaxFTLSpeed <= 2500 )
            {
                if (engineState == MoveState.Warp)
                {
                    isSpooling = false;
                    ResetJumpTimer();
                }
                return;
            }
            if (Carrier.RecallingFighters())
                return;
            if (EnginesKnockedOut || Inhibited)
            {
                HyperspaceReturn();
                return;
            }
            if (velocityMaximum > GetmaxFTLSpeed)
                return;
            if (engineState == MoveState.Sublight && !isSpooling && PowerCurrent / (PowerStoreMax + 0.01f) > 0.1f)
            {
                isSpooling = true;
                ResetJumpTimer();
            }
        }

        private string GetStartWarpCue()
        {
            if (loyalty.data.WarpStart != null)
                return loyalty.data.WarpStart;
            if (Size < 60)
                return "sd_warp_start_small";
            return Size > 350 ? "sd_warp_start_large" : "sd_warp_start_02";
        }

        private string GetEndWarpCue()
        {
            if (loyalty.data.WarpStart != null)
                return loyalty.data.WarpEnd;
            if (Size < 60)
                return "sd_warp_stop_small";
            return Size > 350 ? "sd_warp_stop_large" : "sd_warp_stop";
        }

        public void HyperspaceReturn()
        {
            if (Empire.Universe == null || engineState == MoveState.Sublight)
                return;
            if (JumpSfx.IsPlaying)
                JumpSfx.Stop();

            if (engineState == MoveState.Warp && 
                Center.InRadius(Empire.Universe.CamPos.ToVec2(), 100000f) && Empire.Universe.CamHeight < 250000)
            {
                GameAudio.PlaySfxAsync(GetEndWarpCue(), SoundEmitter);
                FTLManager.AddFTL(Center);
            }
            engineState = MoveState.Sublight;
            ResetJumpTimer();
            isSpooling = false;
            velocityMaximum = GetSTLSpeed();
            if (Velocity != Vector2.Zero)
                Velocity = Velocity.Normalized() * velocityMaximum;
            Speed = velocityMaximum;
        }

        public ShipData ToShipData()
        {
            var data                       = new ShipData();
            data.BaseCanWarp               = shipData.BaseCanWarp;
            data.BaseStrength              = -1;
            data.TechsNeeded               = shipData.TechsNeeded;
            data.TechScore                 = shipData.TechScore;
            data.ShipCategory              = shipData.ShipCategory;
            data.ShieldsBehavior           = shipData.ShieldsBehavior;
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
                data.ThrusterList.Add(new ShipToolScreen.ThrusterZone
                {
                    Scale    = thruster.tscale,
                    Position = thruster.XMLPos
                });
            return data;
        }

        // @todo This exists solely because ModuleSlot updating is buggy
        // if you get a chance, just fix ModuleSlot updates so this isn't needed
        private ModuleSlotData[] GetModuleSlotDataArray()
        {
            var slots = new ModuleSlotData[ModuleSlotList.Length];
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                var data = new ModuleSlotData
                {
                    Position              = module.XMLPosition,
                    InstalledModuleUID    = module.UID,
                    Health                = module.Health,
                    ShieldPower           = module.ShieldPower,
                    ShieldPowerBeforeWarp = module.ShieldPowerBeforeWarp,
                    Facing                = module.Facing,
                    Restrictions          = module.Restrictions
                };

                if (module.Is(ShipModuleType.Shield))
                    data.ShieldUpChance = module.ShieldUpChance;

                if (module.GetHangarShip() != null)
                    data.HangarshipGuid = module.GetHangarShip().guid;

                if (module.ModuleType == ShipModuleType.Hangar)
                    data.SlotOptions = module.DynamicHangar ? DynamicHangarLaunch.DynamicLaunch.ToString()
                                                            : module.hangarShipUID;

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

        public void DamageShieldInvisible(Ship damageSource, float damageAmount)
        {
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldPower >= 1f)
                    shield.Damage(damageSource, damageAmount);
            }
        }

        struct Ranger
        {
            public int Count;
            public float RangeBase;
            public float DamageBase;

            public void AddRange(Weapon w)
            {
                Count++;
                if (w.DamageAmount < 1 || w.TruePD)
                    return;
                if (w.isBeam)
                    DamageBase += w.DamageAmount * w.BeamDuration / Math.Max(w.fireDelay - w.BeamDuration, 1);
                else
                    DamageBase += w.DamageAmount * w.SalvoCount / w.fireDelay;

                RangeBase += w.Range;
            }
            public float GetAverageDam()
            {
                return DamageBase / Count;
            }
            public float GetAverageRange()
            {
                return RangeBase / Count;
            }
        }
        private float CalculatMaxWeaponsRange()
        {
            

            if (Weapons.Count == 0) return 7500f;
            float maxRange =0;
            float minRange = float.MaxValue;
            float avgRange = 0;
            int noDamage = 0;
            foreach (Weapon w in Weapons)
            {
                maxRange = Math.Max(w.Range, maxRange);
                minRange = Math.Min(w.Range, minRange);
                noDamage += w.DamageAmount <1 || w.TruePD || w.Tag_PD  ? 1 :0 ;
                avgRange += w.Range;
            }
            avgRange /= Weapons.Count;
            if (avgRange > maxRange *.75f) return avgRange;
            bool ignoreDamage = noDamage / (Weapons.Count + 1f) > .75f;                       
            Ranger shortRange = new Ranger();
            Ranger longRange = new Ranger();
            Ranger utility = new Ranger();

            foreach (var w in Weapons)
            {
                if (w.DamageAmount <1 || w.TruePD || w.Tag_PD)
                {
                    utility.AddRange(w);
                    if (ignoreDamage)
                        continue;
                }
                if (w.Range < avgRange)
                    shortRange.AddRange(w);
                else  longRange.AddRange(w);
            }
            if (ignoreDamage)
            {
                return utility.GetAverageRange();
            }

            float longR = longRange.GetAverageDam();
            float shotR = shortRange.GetAverageDam();

            if (AI.CombatState == CombatState.Artillery || AI.CombatState != CombatState.ShortRange && longR > shotR)
            {
                return longRange.GetAverageRange();
            }
            return shortRange.GetAverageRange();

        }

        public void UpdateShipStatus(float deltaTime)
        {
            if (!Empire.Universe.Paused && velocityMaximum <= 0f 
                && !shipData.IsShipyard && shipData.Role <= ShipData.RoleName.station)                                           
                Rotation += 0.003f + RandomMath.AvgRandomBetween(.0001f,.0005f);
            

            MoveModulesTimer -= deltaTime;
            updateTimer -= deltaTime;
            if (deltaTime > 0 && (EMPDamage > 0 || EMPdisabled))
            {
                EMPDamage -= EmpRecovery;
                EMPDamage = Math.Max(0, EMPDamage);

                EMPdisabled = EMPDamage > EmpTolerance;
            }
            if (Rotation > 6.28318548202515f) Rotation -= 6.28318548202515f;
            if (Rotation < 0f) Rotation += 6.28318548202515f;

            if (InCombat && !EMPdisabled && hasCommand || PlayerShip)
            {
                for (int i = 0; i < Weapons.Count; i++)
                {
                    Weapons[i].Update(deltaTime);
                }
            }

            if (updateTimer <= 0) //|| shipStatusChanged)
            {
                TroopBoardingDefense = 0f;
                for (int i = 0; i < TroopList.Count; i++)   //Do we need to update this every frame? I mived it here so it would be every second, instead.   -Gretman
                {
                    TroopList[i].SetShip(this);
                    if (TroopList[i].GetOwner() == loyalty)
                        TroopBoardingDefense += TroopList[i].Strength;
                }

                if ((InCombat && !EMPdisabled && hasCommand || PlayerShip) && Weapons.Count > 0)
                {
                    
                    AI.CombatAI.UpdateCombatAI(this);

                    float direction = AI.CombatState == CombatState.ShortRange ? 1f : -1f; // ascending : descending
                    //Weapon[] sortedByRange = Weapons.Sorted(weapon => direction*weapon.GetModifiedRange());

                    foreach (Weapon weapon in Weapons)
                    {
                        //Edited by Gretman
                        //This fixes ships with only 'other' damage types thinking it has 0 range, causing them to fly through targets even when set to attack at max/min range
                        //if (!flag && (weapon.DamageAmount > 0.0 || weapon.EMPDamage > 0.0 || weapon.SiphonDamage > 0.0 || weapon.MassDamage > 0.0 || weapon.PowerDamage > 0.0 || weapon.RepulsionDamage > 0.0))
                        //{
                        //    maxWeaponsRange = weapon.GetModifiedRange();
                        //    if (!weapon.Tag_PD) flag = true;
                        //}

                        if (GlobalStats.HasMod)
                        {
                            Weapon weaponTemplate = ResourceManager.GetWeaponTemplate(weapon.UID);
                            weapon.fireDelay = weaponTemplate.fireDelay;

                            //Added by McShooterz: weapon tag modifiers with check if mod uses them
                            if (GlobalStats.ActiveModInfo.useWeaponModifiers)
                            {
                                var tags = loyalty.data.WeaponTags;
                                if (weapon.Tag_Beam)      weapon.fireDelay -= weaponTemplate.fireDelay * tags["Beam"].Rate;
                                if (weapon.Tag_Energy)    weapon.fireDelay -= weaponTemplate.fireDelay * tags["Energy"].Rate;
                                if (weapon.Tag_Explosive) weapon.fireDelay -= weaponTemplate.fireDelay * tags["Explosive"].Rate;
                                if (weapon.Tag_Guided)    weapon.fireDelay -= weaponTemplate.fireDelay * tags["Guided"].Rate;
                                if (weapon.Tag_Hybrid)    weapon.fireDelay -= weaponTemplate.fireDelay * tags["Hybrid"].Rate;
                                if (weapon.Tag_Intercept) weapon.fireDelay -= weaponTemplate.fireDelay * tags["Intercept"].Rate;
                                if (weapon.Tag_Kinetic)   weapon.fireDelay -= weaponTemplate.fireDelay * tags["Kinetic"].Rate;
                                if (weapon.Tag_Missile)   weapon.fireDelay -= weaponTemplate.fireDelay * tags["Missile"].Rate;
                                if (weapon.Tag_Railgun)   weapon.fireDelay -= weaponTemplate.fireDelay * tags["Railgun"].Rate;
                                if (weapon.Tag_Torpedo)   weapon.fireDelay -= weaponTemplate.fireDelay * tags["Torpedo"].Rate;
                                if (weapon.Tag_Cannon)    weapon.fireDelay -= weaponTemplate.fireDelay * tags["Cannon"].Rate;
                                if (weapon.Tag_Subspace)  weapon.fireDelay -= weaponTemplate.fireDelay * tags["Subspace"].Rate;
                                if (weapon.Tag_PD)        weapon.fireDelay -= weaponTemplate.fireDelay * tags["PD"].Rate;
                                if (weapon.Tag_Bomb)      weapon.fireDelay -= weaponTemplate.fireDelay * tags["Bomb"].Rate;
                                if (weapon.Tag_SpaceBomb) weapon.fireDelay -= weaponTemplate.fireDelay * tags["Spacebomb"].Rate;
                                if (weapon.Tag_BioWeapon) weapon.fireDelay -= weaponTemplate.fireDelay * tags["BioWeapon"].Rate;
                                if (weapon.Tag_Drone)     weapon.fireDelay -= weaponTemplate.fireDelay * tags["Drone"].Rate;
                                if (weapon.Tag_Warp)      weapon.fireDelay -= weaponTemplate.fireDelay * tags["Warp"].Rate;
                                if (weapon.Tag_Array)     weapon.fireDelay -= weaponTemplate.fireDelay * tags["Array"].Rate;
                                if (weapon.Tag_Flak)      weapon.fireDelay -= weaponTemplate.fireDelay * tags["Flak"].Rate;
                                if (weapon.Tag_Tractor)   weapon.fireDelay -= weaponTemplate.fireDelay * tags["Tractor"].Rate;
                            }                            
                            //Added by McShooterz: Hull bonus Fire Rate
                            if (GlobalStats.ActiveModInfo.useHullBonuses)
                            {
                                if (ResourceManager.HullBonuses.TryGetValue(shipData.Hull, out HullBonus mod))
                                    weapon.fireDelay *= 1f - mod.FireRateBonus;
                            }
                        }
                    }
                }

                if (InhibitedTimer < 2f)
                {
                    foreach (Empire e in EmpireManager.Empires)
                    {
                        if (e != loyalty && !loyalty.GetRelations(e).Treaty_OpenBorders)
                        {
                            for (int i = 0; i < e.Inhibitors.Count; ++i)
                            {
                                Ship ship = e.Inhibitors[i];
                                if (ship != null && Center.InRadius(ship.Position, ship.InhibitionRadius))
                                {
                                    Inhibited = true;
                                    InhibitedTimer = 5f;
                                    break;
                                }
                            }
                            if (Inhibited)
                                break;
                        }
                    }
                }

                SetShipsVisibleByPlayer();
                foreach (ShipModule slot in ModuleSlotList)
                      slot.Update(1);

                if (shipStatusChanged) //|| InCombat
                {
                    ShipStatusChange();
                }

                //Power draw based on warp
                if (!inborders && engineState == MoveState.Warp)
                    PowerDraw = NetPower.NetWarpPowerDraw;
                else if (engineState != MoveState.Warp && ShieldsUp)
                    PowerDraw = NetPower.NetSubLightPowerDraw;
                else
                    PowerDraw = NetPower.NetWarpPowerDraw;

                if (InCombat 
                    || shield_power < shield_max 
                    || engineState == MoveState.Warp 
                    || shipData.ShieldsBehavior != ShieldsWarpBehavior.FullPower)
                {
                    shield_power = 0.0f;
                    for (int x = 0; x < Shields.Length; x++)
                    {
                        ShipModule shield = Shields[x];
                        shield_power = (shield_power + shield.ShieldPower).Clamped(0, shield_max);
                    }
                }

                // Add ordnance
                if (Ordinance < OrdinanceMax)
                {
                    Ordinance += OrdAddedPerSecond;
                    if (Ordinance > OrdinanceMax)
                        Ordinance = OrdinanceMax;
                }
                else Ordinance = OrdinanceMax;

                // Update max health if needed
                int latestRevision = EmpireShipBonuses.GetBonusRevisionId(loyalty);
                if (MaxHealthRevision != latestRevision)
                {
                    MaxHealthRevision = latestRevision;
                    HealthMax = RecalculateMaxHealth();
                }

                // Repair
                if (Health < HealthMax)
                {
                    if (!InCombat || (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useCombatRepair))
                    {
                        // Added by McShooterz: Priority repair
                        float repair = InCombat ? RepairRate * 0.1f : RepairRate;
                        ApplyAllRepair(repair, Level);
                    }                  
                }
           
                UpdateTroops();
                updateTimer = 1f;
                if (NeedRecalculate)
                {
                    RecalculatePower();
                    NeedRecalculate = false;
                }
            }
            //This used to be an 'else if' but it was causing modules to skip an update every second. -Gretman
            if (MoveModulesTimer > 0.0f || GlobalStats.ForceFullSim || AI.BadGuysNear 
                || (InFrustum && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView) )
            {
                if (deltaTime > 0.0f || UpdatedModulesOnce)
                {
                    UpdatedModulesOnce = deltaTime > 0;

                    float cos = (float)Math.Cos(Rotation);
                    float sin = (float)Math.Sin(Rotation);
                    float tan = (float)Math.Tan(yRotation);
                    for (int i = 0; i < ModuleSlotList.Length; ++i)
                    {
                        ModuleSlotList[i].UpdateEveryFrame(deltaTime, cos, sin, tan); 
                        ++GlobalStats.ModuleUpdates;
                    }
                }
            }

            if (FightersLaunched) // for ships with hangars and with fighters out button on.
                Carrier.ScrambleFighters(); // FB: If new fighters are ready in hangars, scramble them
            if (TroopsLaunched)
                Carrier.ScrambleAllAssaultShips(); // FB: if the troops out button is on, launch every availble assualt shuttle

            SetmaxFTLSpeed();
            Ordinance = Math.Min(Ordinance, OrdinanceMax);

            InternalSlotsHealthPercent = (float)ActiveInternalSlotCount / InternalSlotCount;
            if (InternalSlotsHealthPercent < ShipResupply.ShipDestroyThreshold)
                Die(LastDamagedBy, false);

            Mass = Math.Max(Size * 0.5f, Mass);
            Mass = Math.Max(Mass, 1);
            PowerCurrent -= PowerDraw * deltaTime;
            if (PowerCurrent < PowerStoreMax)
                PowerCurrent += (PowerFlowMax + PowerFlowMax * (loyalty?.data.PowerFlowMod ?? 0)) * deltaTime;

            if (PowerCurrent <= 0.0f)
            {
                PowerCurrent = 0.0f;
                HyperspaceReturn();
            }
            PowerCurrent = Math.Min(PowerCurrent, PowerStoreMax);
            
            shield_percent = shield_max >0 ? 100.0 * shield_power / shield_max : 0;
            
            
            switch (engineState)
            {
                case MoveState.Sublight:
                    velocityMaximum = GetSTLSpeed();
                    break;
                case MoveState.Warp:
                    velocityMaximum = GetmaxFTLSpeed;
                    break;
            }

            Speed = velocityMaximum;
            rotationRadiansPerSecond = TurnThrust / Mass / 700f;
            rotationRadiansPerSecond += (float)(rotationRadiansPerSecond * Level * 0.0500000007450581);
            yBankAmount =  GetyBankAmount(rotationRadiansPerSecond * deltaTime);// 50f;

            Vector2 oldVelocity = Velocity;
            if (engineState == MoveState.Warp)
            {
                Velocity = Rotation.RadiansToDirection() * velocityMaximum;
            }
            if ((Thrust <= 0.0f || Mass <= 0.0f) && !IsTethered())
            {
                EnginesKnockedOut = true;
                velocityMaximum = Velocity.Length();
                Velocity -= Velocity * (deltaTime * 0.1f);
                if (engineState == MoveState.Warp)
                    HyperspaceReturn();
            }
            else
                EnginesKnockedOut = false;

            if (Velocity.Length() > velocityMaximum)
                Velocity = Velocity.Normalized() * velocityMaximum;

            Acceleration = oldVelocity.Acceleration(Velocity, deltaTime);
        }

        private void SetShipsVisibleByPlayer()
        {
            /* Changed this so that the other ships will only check if they are not in sensors if they have been marked
             insensors. Player ships will check for to see that ships near them are in sensor range. 
             this seems redundent. there are several places where ships are checked for being in sensors. 
             scanforshipsinsensors, 
                 */

            if (Empire.Universe.Debug)
            {
                inSensorRange = true;
                return;
            }

            if (loyalty.isPlayer || EmpireManager.Player.GetRelations(loyalty).Treaty_Alliance)            
                inSensorRange = true;
            
            if (inSensorRange)
            {
                SetOtherShipsInsensorRange();
            }
        }
        
        private void SetOtherShipsInsensorRange()
        {
            GameplayObject[] nearby = GetObjectsInSensors(GameObjectType.Ship);
            bool checkFromThis = loyalty.isPlayer || EmpireManager.Player.GetRelations(loyalty).Treaty_Alliance;
            foreach (GameplayObject go in nearby)
            {
                Ship ship;
                if (checkFromThis)
                {
                    ship = (Ship)go;
                    if (go.GetLoyalty().isPlayer || ship.inSensorRange || !Center.InRadius(go.Position, SensorRange))
                        continue;                    
                    ship.inSensorRange = true;
                    break;
                }
                
                if (go.GetLoyalty().isPlayer)
                {
                    ship = (Ship)go;
                    if (Center.OutsideRadius(ship.Position, ship.SensorRange))
                        continue;
                    inSensorRange = true;
                    break;
                }
                inSensorRange = false;
                
            }
        }

        public bool TroopsAreBoardingShip => TroopList.Count(troop => troop.GetOwner() == loyalty) != TroopList.Count;

        public int NumPlayerTroopsOnShip => TroopList.Count(troop => troop.GetOwner() == EmpireManager.Player);

        public int NumAiTroopsOnShip => TroopList.Count(troop => troop.GetOwner() != EmpireManager.Player);

        private void UpdateTroops() //FB: this is the weirdest implemetations i've ever seen. Anyway i refactored it a bit
        {
            Array<Troop> ownTroops = new Array<Troop>(TroopList.FilterBy(troop => troop.GetOwner() == loyalty));
            Array<Troop> enemyTroops = new Array<Troop>(TroopList.FilterBy(troop => troop.GetOwner() != loyalty));

            HealTroops();
            int troopThreshold = TroopCapacity + (TroopCapacity > 0 ? 0 : 1); // leave a garrion of 1 if ship without barracks was boarded
            if (!InCombat && enemyTroops.Count <= 0 && ownTroops.Count > troopThreshold) 
                DisengageExcessTroops(ownTroops.Count - troopThreshold);

            if (enemyTroops.Count <= 0)
                return; // Boarding is over or never started

            float mechanicalDefenseRoll = GetMechanicalDefenseRoll();
            ResolveMachanicalDefenseVersusEnemy();

            float defendingTroopsRoll = mechanicalDefenseRoll; // since mechanicalDefenseRoll might beaten by enemy troops to be negative.

            enemyTroops.Clear();
            enemyTroops = new Array<Troop>(TroopList.FilterBy(troop => troop.GetOwner() != loyalty));

            ResolveOwnVersusEnemy();

            enemyTroops.Clear();
            enemyTroops = new Array<Troop>(TroopList.FilterBy(troop => troop.GetOwner() != loyalty));

            ResolveEnemyVersusOwn();

            ownTroops.Clear();
            ownTroops = new Array<Troop>(TroopList.FilterBy(troop => troop.GetOwner() == loyalty));

            if (ownTroops.Count > 0 || MechanicalBoardingDefense > 0.0)
                return;

            ChangeLoyalty(changeTo: enemyTroops[0].GetOwner());
            RefreshMechanicalBoardingDefense();

            if (!AI.BadGuysNear)
            ShieldManager.RemoveShieldLights(Shields);

            // local methods
            void HealTroops()
            {
                if (!(HealPerTurn > 0))
                    return;

                foreach (Troop troop in ownTroops)
                    troop.Strength = (troop.Strength += HealPerTurn).Clamped(0, troop.GetStrengthMax());
            }

            float GetMechanicalDefenseRoll()
            {
                float mechanicalDefResult = 0f;
                for (int index = 0; index < MechanicalBoardingDefense; ++index)
                    if (UniverseRandom.RandomBetween(0.0f, 100f) <= 50.0f)
                        ++mechanicalDefResult;

                return mechanicalDefResult;
            }

            void ResolveMachanicalDefenseVersusEnemy()
            {
                foreach (Troop troop in enemyTroops)
                {
                    if (mechanicalDefenseRoll > 0)
                    {
                        mechanicalDefenseRoll -= troop.Strength;
                        troop.Strength -= mechanicalDefenseRoll + troop.Strength;
                        if (troop.Strength <= 0)
                            TroopList.Remove(troop);
                    }
                    else
                        break;
                }
            }

            void ResolveOwnVersusEnemy()
            {
                if (ownTroops.Count <= 0 || enemyTroops.Count <= 0)
                    return;

                foreach (Troop troop in ownTroops)
                {
                    for (int index = 0; index < troop.Strength; ++index)
                        if (UniverseRandom.IntBetween(0, 100) <= troop.BoardingStrength)
                            ++defendingTroopsRoll;
                }
                foreach (Troop troop in enemyTroops)
                {
                    if (defendingTroopsRoll > 0)
                    {
                        defendingTroopsRoll -= troop.Strength;
                        troop.Strength -= defendingTroopsRoll + troop.Strength;
                        if (troop.Strength <= 0)
                            TroopList.Remove(troop);

                        if (defendingTroopsRoll <= 0)
                            break;
                    }
                    else
                        break;
                }
            }

            void ResolveEnemyVersusOwn()
            {
                if (enemyTroops.Count <= 0)
                    return;

                float attackingTroopsRoll = 0;
                foreach (Troop troop in enemyTroops)
                {
                    for (int index = 0; index < troop.Strength; ++index)
                    {
                        if (UniverseRandom.IntBetween(0, 100) < troop.BoardingStrength)
                            ++attackingTroopsRoll;
                    }
                }
                foreach (Troop troop in ownTroops)
                {
                    if (attackingTroopsRoll > 0)
                    {
                        attackingTroopsRoll -= troop.Strength;
                        troop.Strength -= attackingTroopsRoll + troop.Strength;
                        if (troop.Strength <= 0)
                            TroopList.Remove(troop);
                    }
                    else
                        break;
                }
                if (attackingTroopsRoll > 0)
                    MechanicalBoardingDefense = Math.Max(MechanicalBoardingDefense - attackingTroopsRoll, 0);
            }

        }

        private void RefreshMechanicalBoardingDefense()
        {
            MechanicalBoardingDefense =  ModuleSlotList.Sum(module => module.MechanicalBoardingDefense);
        }

        private void DisengageExcessTroops(int troopsToRemove) // excess troops will leave the ship, usually after successful boarding
        {
            for (int i = 0; i < troopsToRemove; i++)
            {
                Ship assaultShip     = CreateTroopShipAtPoint(GetAssaultShuttleName(loyalty), loyalty, Center, TroopList[0]);
                assaultShip.Velocity = UniverseRandom.RandomDirection() * assaultShip.Speed + Velocity;
                TroopList.Remove(TroopList[0]);
                if (assaultShip.Velocity.Length() > assaultShip.velocityMaximum)
                    assaultShip.Velocity = Vector2.Normalize(assaultShip.Velocity) * assaultShip.Speed;

                Ship friendlyTroopShiptoRebase = FindClosestAllyToRebase(assaultShip);

                bool rebaseSucceeded = false;
                if (friendlyTroopShiptoRebase != null)
                    rebaseSucceeded = friendlyTroopShiptoRebase.Carrier.RebaseAssaultShip(assaultShip);

                if (!rebaseSucceeded) // did not found a friendly troopship to rebase to
                    assaultShip.AI.OrderRebaseToNearest();

                if (assaultShip.AI.State == AIState.AwaitingOrders) // nowhere to rebase
                    assaultShip.DoEscort(this);
            }
        }

        private Ship FindClosestAllyToRebase(Ship ship)
        {
            ship.AI.ScanForCombatTargets(ship, ship.SensorRange); // to find friendlies nearby
            return ship.AI.FriendliesNearby.FindMinFiltered(
                troopShip => troopShip.Carrier.NumTroopsInShipAndInSpace < troopShip.TroopCapacity
                && troopShip.Carrier.HasTroopBays,
                troopShip => ship.Center.SqDist(troopShip.Center));
        }

        public static string GetAssaultShuttleName(Empire empire) // this will get the name of an Assault Shuttle if defined in race.xml or use default one
        {
            return  empire.data.DefaultAssaultShuttle.IsEmpty() ? empire.BoardingShuttle.Name 
                                                                : empire.data.DefaultAssaultShuttle;
        }

        public static string GetSupplyShuttleName(Empire empire) // this will get the name of a Supply Shuttle if defined in race.xml or use default one
        {
            return empire.data.DefaultSupplyShuttle.IsEmpty() ? empire.SupplyShuttle.Name
                                                              : empire.data.DefaultSupplyShuttle;
        }

        public ShipStatus HealthStatus
        {
            get
            {
                if (engineState == MoveState.Warp
                    || AI.State == AIState.Refit
                    || AI.State == AIState.Resupply)
                        return ShipStatus.NotApplicable;
                Health = Health.Clamped(0, HealthMax);
                return ToShipStatus(Health, HealthMax);
            }
        }

        public void AddShipHealth(float addHealth) => Health = (Health + addHealth).Clamped(0, HealthMax);

        public void ShipStatusChange()
        {
            shipStatusChanged = false;            
            float sensorBonus = 0f;
            Thrust                      = 0f;
            Mass                        = Size;
            shield_max                  = 0f;
            ActiveInternalSlotCount     = 0;
            BonusEMP_Protection         = 0f;
            PowerStoreMax               = 0f;
            PowerFlowMax                = 0f;
            OrdinanceMax                = 0f;
            RepairRate                  = 0f;
            CargoSpaceMax               = 0f;
            SensorRange                 = 1000f;
            WarpThrust                  = 0f;
            TurnThrust                  = 0f;
            NormalWarpThrust            = 0f;
            FTLSlowTurnBoost            = false;
            InhibitionRadius            = 0f;
            OrdAddedPerSecond           = 0f;
            HealPerTurn                 = 0;
            ECMValue                    = 0f;
            FTLSpoolTime                = 0f;
            hasCommand                  = IsPlatform;
            TrackingPower               = 0;
            FixedTrackingPower          = 0;

            foreach (ShipModule module in ModuleSlotList)
            {
                //Get total internal slots
                if (module.HasInternalRestrictions && module.Active)
                    ActiveInternalSlotCount += module.XSIZE * module.YSIZE;
                
                RepairRate += module.Active ? module.ActualBonusRepairRate : module.ActualBonusRepairRate / 10; // FB - so destroyed modules with repair wont have full repair rate
                if (module.Mass < 0.0 && module.Powered)
                    Mass += module.Mass;
                else if (module.Mass > 0.0)
                {
                    if (module.Is(ShipModuleType.Armor) && loyalty != null)
                    {
                        float armourMassModifier = loyalty.data.ArmourMassModifier;
                        float armourMass = module.Mass * armourMassModifier;
                        Mass += armourMass;
                    }
                    else                    
                        Mass += module.Mass;
                }
                //Checks to see if there is an active command module

                if (module.Active && (module.Powered || module.PowerDraw <= 0f))
                {                    
                    hasCommand |= module.IsCommandModule;
                    //Doctor: For 'Fixed' tracking power modules - i.e. a system whereby a module provides a non-cumulative/non-stacking tracking power.
                    //The normal stacking/cumulative tracking is added on after the for loop for mods that want to mix methods. The original cumulative function is unaffected.
                    if (module.FixedTracking > 0 && module.FixedTracking > FixedTrackingPower)
                        FixedTrackingPower = module.FixedTracking;
                    
                    TrackingPower         += Math.Max(0, module.TargetTracking);
                    OrdinanceMax          += module.OrdinanceCapacity;
                    CargoSpaceMax         += module.Cargo_Capacity;
                    InhibitionRadius      += module.InhibitionRadius;
                    BonusEMP_Protection   += module.EMP_Protection;
                    SensorRange            = Math.Max(SensorRange, module.SensorRange);
                    sensorBonus            = Math.Max(sensorBonus, module.SensorBonus);
                    if (module.Is(ShipModuleType.Shield))
                        shield_max += module.ActualShieldPowerMax;

                    Thrust              += module.thrust;
                    WarpThrust          += module.WarpThrust;
                    TurnThrust          += module.TurnThrust;
                    OrdAddedPerSecond   += module.OrdnanceAddedPerSecond;
                    HealPerTurn         += module.HealPerTurn;
                    ECMValue             = 1f.Clamped(0f, Math.Max(ECMValue, module.ECM)); // 0-1 using greatest value.                    
                    PowerStoreMax       += module.ActualPowerStoreMax;
                    PowerFlowMax        += module.ActualPowerFlowMax;      
                    FTLSpoolTime   = Math.Max(FTLSpoolTime, module.FTLSpoolTime);
                    module.AddModuleTypeToList(module.ModuleType, isTrue: module.InstalledWeapon?.isRepairBeam == true, addToList: RepairBeams);
                }
            }

            NetPower = Power.Calculate(ModuleSlotList, loyalty, shipData.ShieldsBehavior);

            NormalWarpThrust = WarpThrust;
            //Doctor: Add fixed tracking amount if using a mixed method in a mod or if only using the fixed method.
            TrackingPower += FixedTrackingPower;
            shield_percent = Math.Max(100.0 * shield_power / shield_max, 0);
        
            //if (this.shipStatusChanged)
            {
                SensorRange += sensorBonus;
                //Apply modifiers to stats
                if (loyalty != null)
                {
                    Mass          *= loyalty.data.MassModifier;
                    RepairRate    += (float)(RepairRate * Level * 0.05);
                    //PowerFlowMax  += PowerFlowMax * loyalty.data.PowerFlowMod;
                    //PowerStoreMax += PowerStoreMax * loyalty.data.FuelCellModifier;
                    SensorRange   *= loyalty.data.SensorModifier;
                }
                if (FTLSpoolTime <= 0)
                    FTLSpoolTime = 3f;
                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses && 
                    ResourceManager.HullBonuses.TryGetValue(shipData.Hull, out HullBonus mod))
                {
                    CargoSpaceMax  += CargoSpaceMax * mod.CargoBonus;
                    SensorRange    += SensorRange * mod.SensorBonus;
                    WarpThrust     += WarpThrust * mod.SpeedBonus;
                    Thrust         += Thrust * mod.SpeedBonus;
                }
            }
            CurrentStrength = CalculateShipStrength();
            maxWeaponsRange = CalculatMaxWeaponsRange();
            
        }

        public bool IsTethered()
        {
            return TetheredTo != null;
        }

        //added by Gremlin : active ship strength calculator
        private float CurrentStrength = -1;
        public float GetStrength()
        {
            if (CurrentStrength == -1)
                Debugger.Break();
            return CurrentStrength;
        }


        public float GetDPS() => DPS;

        //Added by McShooterz: add experience for cruisers and stations, modified for dynamic system
        public void AddKill(Ship killed)
        {
            ++kills;
            if (loyalty == null)
                return;
        
            //Added by McShooterz: change level cap, dynamic experience required per level
            float exp = 1;
            float expLevel = 1;
            bool expFound = false;
            float reqExp = 1;
            if (ResourceManager.ShipRoles.TryGetValue(killed.shipData.Role, out ShipRole role))
            {
                for (int i = 0; i < role.RaceList.Count(); i++)
                {
                    if (role.RaceList[i].ShipType != killed.loyalty.data.Traits.ShipType) continue;
                    exp = role.RaceList[i].KillExp;
                    expLevel = role.RaceList[i].KillExpPerLevel;
                    expFound = true;
                    break;
                }
                if(!expFound)
                {
                    exp = role.KillExp;
                    expLevel = role.KillExpPerLevel;
                }
            }
            exp = (exp + (expLevel * killed.Level));
            exp += exp * loyalty.data.ExperienceMod;
            experience += exp;
            expFound = false;
            //Added by McShooterz: a way to prevent remnant story in mods

            Empire remnant = EmpireManager.Remnants;  //Changed by Gretman, because this was preventing any "RemnantKills" from getting counted, thus no remnant event.
            //if (this.loyalty == EmpireManager.Player && killed.loyalty == remnant && this.shipData.ShipStyle == remnant.data.Traits.ShipType &&  (GlobalStats.ActiveModInfo == null || (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.removeRemnantStory)))
            if (loyalty == EmpireManager.Player && killed.loyalty == remnant &&  (GlobalStats.ActiveModInfo == null || (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.removeRemnantStory)))
                //GlobalStats.IncrementRemnantKills((int)Exp);
                GlobalStats.IncrementRemnantKills(1);   //I also changed this because the exp before was a lot, killing almost any remnant ship would unlock the remnant event immediately

            if (role != null)
            {
                for (int i = 0; i < role.RaceList.Count; i++)
                {
                    if (role.RaceList[i].ShipType == loyalty.data.Traits.ShipType)
                    {
                        reqExp = role.RaceList[i].ExpPerLevel;
                        expFound = true;
                        break;
                    }
                }
                if (!expFound)
                {
                    reqExp = role.ExpPerLevel;
                }
            }
            while (experience > reqExp * (1 + Level))
            {
                experience -= reqExp * (1 + Level);
                AddToShipLevel(1);
            }
            
            if (!loyalty.TryGetRelations(killed.loyalty, out Relationship rel) || !rel.AtWar)
                return;
            rel.ActiveWar.StrengthKilled += killed.BaseStrength;
            rel.ActiveWar.StrengthLost += killed.BaseStrength;
        }

        public void AddToShipLevel(int amountToAdd) => Level = Math.Min(255, Level + amountToAdd);        
        private void ExplodeShip(Tuple<float , bool> splode)
        {
            ExplodeShip(splode.Item1, splode.Item2);
        }
        private void ExplodeShip(float explodeRadius, bool useWarpExplodeEffect)
        {
            if (!InFrustum) return;
            Vector3 position = new Vector3(Center.X, Center.Y, -100f);

            float explosionboost = 1f;
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi != null)
                explosionboost = GlobalStats.ActiveMod.mi.GlobalShipExplosionVisualIncreaser;

            ExplosionManager.AddExplosion(position, explodeRadius * explosionboost, 12f, 0.2f);
            if (useWarpExplodeEffect)
            {
                ExplosionManager.AddWarpExplosion(position, explodeRadius*1.75f, 12f, 0.2f);
            }
        }

        private static Tuple<float, bool> SetSplodeData(float size, bool warp) => new Tuple<float, bool>(size, warp);

        // cleanupOnly: for tumbling ships that are already dead
        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            for (int i = 0; i < beams.Count; i++)
            {
                Beam beam = beams[i];
                beam.Die(this, true);
                beams.RemoveRef(beam);
            }

            ++DebugInfoScreen.ShipsDied;
            Projectile psource = source as Projectile;
            if (!cleanupOnly)
                psource?.Owner?.AddKill(this);

            // 35% the ship will not explode immediately, but will start tumbling out of control
            // we mark the ship as dying and the main update loop will set reallyDie
            if (UniverseRandom.IntBetween(0, 100) > 65.0 && !IsPlatform && InFrustum)
            {
                dying         = true;
                xdie          = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                ydie          = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                zdie          = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                dietimer      = UniverseRandom.RandomBetween(4f, 6f);
                if (psource != null && psource.Explodes && psource.DamageAmount > 100.0)
                    reallyDie = true;
            }
            else reallyDie = true;

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
                if (Size < 80)       dieSoundEffect = "sd_explosion_ship_det_small";
                else if (Size < 250) dieSoundEffect = "sd_explosion_ship_det_medium";
                else                 dieSoundEffect = "sd_explosion_ship_det_large";
                GameAudio.PlaySfxAsync(dieSoundEffect, SoundEmitter);
            }
            for (int index = 0; index < EmpireManager.Empires.Count; index++)
            {
                EmpireManager.Empires[index].GetGSAI().ThreatMatrix.RemovePin(this);                 
            }
            Carrier.ScuttleNonWarpHangarShips();
            ModuleSlotList     = Empty<ShipModule>.Array;
            SparseModuleGrid   = Empty<ShipModule>.Array;
            ExternalModuleGrid = Empty<ShipModule>.Array;
            NumExternalSlots   = 0;
            BorderCheck.Clear();
            ThrusterList.Clear();
            AI.PotentialTargets.Clear();
            AttackerTargetting.Clear();
            Velocity = Vector2.Zero;
            velocityMaximum = 0.0f;
            float size = Radius  * (shipData.EventOnDeath?.NotEmpty() == true? 3 :1);// Math.Max(GridHeight, GridWidth);
            if (Active)
            {
                Active = false;
                Tuple<float, bool> splodeType; 
                switch (shipData.HullRole)
                {
                    case ShipData.RoleName.freighter: splodeType = SetSplodeData(size * 8, cleanupOnly);  break;
                    case ShipData.RoleName.platform:  splodeType = SetSplodeData(size * 8, cleanupOnly);  break;
                    case ShipData.RoleName.corvette: 
                    case ShipData.RoleName.scout:                        
                    case ShipData.RoleName.fighter:   splodeType = SetSplodeData(size * 10, cleanupOnly); break;
                    case ShipData.RoleName.frigate:   splodeType = SetSplodeData(size * 10, cleanupOnly); break;
                    case ShipData.RoleName.carrier:
                    case ShipData.RoleName.capital:   splodeType = SetSplodeData(size * 8, true);         break; 
                    case ShipData.RoleName.cruiser:   splodeType = SetSplodeData(size * 8, true);         break;
                    case ShipData.RoleName.station:   splodeType = SetSplodeData(size * 8, true);         break; 
                    default:                          splodeType = SetSplodeData(size * 8, cleanupOnly);  break;
                }
                ExplodeShip(splodeType); 
                UniverseScreen.SpaceManager.ShipExplode(this, splodeType.Item1 * 50, Center, Radius);

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
                        SpaceJunk.SpawnJunk(howMuchJunk, Center.GenerateRandomPointOnCircle(Radius/2), System, this, Radius, junkScale, true);
                    }
                }
            }
            if (BaseHull.EventOnDeath != null)
            {
                var evt = ResourceManager.EventsDict[BaseHull.EventOnDeath];
                Empire.Universe.ScreenManager.AddScreen(new EventPopup(Empire.Universe, EmpireManager.Player, evt, evt.PotentialOutcomes[0], true));
            }
            QueueTotalRemoval();

            base.Die(source, cleanupOnly);
        }

        public void QueueTotalRemoval()
        {
            SetSystem(null);

            Empire.Universe.QueueGameplayObjectRemoval(this);
        }

        public override void RemoveFromUniverseUnsafe()
        {
            Active                           = false;
            AI.Target                        = null;
            AI.ColonizeTarget                = null;
            AI.EscortTarget                  = null;
            AI.start                         = null;
            AI.end                           = null;
            AI.PotentialTargets.Clear();
            AI.TrackProjectiles.Clear();
            AI.NearByShips.Clear();
            AI.FriendliesNearby.Clear();
            Empire.Universe.MasterShipList.QueuePendingRemoval(this);
            AttackerTargetting.Clear();
            if (Empire.Universe.SelectedShip == this)
                Empire.Universe.SelectedShip = null;
            Empire.Universe.SelectedShipList.Remove(this);

            if (Mothership != null)
            {
                foreach (ShipModule shipModule in Mothership.Carrier.AllActiveHangars)
                    if (shipModule.GetHangarShip() == this)
                        shipModule.SetHangarShip(null);
            }
            foreach (ShipModule hanger in Carrier.AllHangars) // FB: use here all hangars and not just active hangars
            {
                if (hanger.GetHangarShip() != null)
                    hanger.GetHangarShip().Mothership = null;
            }
            foreach (Empire empire in EmpireManager.Empires)
            {
                empire.GetGSAI().ThreatMatrix.RemovePin(this);
            }

            foreach (Projectile projectile in projectiles)
                projectile.Die(this, false);
            if (beams != null)
                for (int i = 0; i < beams.Count; i++)
                {
                    Beam beam = beams[i];
                    beam.Die(this, true);
                    beams.RemoveRef(beam);
                }
            projectiles.Clear();

            ModuleSlotList     = Empty<ShipModule>.Array;
            SparseModuleGrid   = Empty<ShipModule>.Array;
            ExternalModuleGrid = Empty<ShipModule>.Array;
            NumExternalSlots   = 0;
            Shields            = Empty<ShipModule>.Array;

            BombBays.Clear();
            TroopList.Clear();
            ClearFleet();
            ShipSO?.Clear();
            Empire.Universe.RemoveObject(ShipSO);
            loyalty.RemoveShip(this);
            SetSystem(null);
            TetheredTo = null;
            RepairBeams.Clear();
            Empire.Universe.MasterShipList.QueuePendingRemoval(this);
        }

        public bool ClearFleet() => fleet?.RemoveShip(this) ?? false;
        
        public bool ShipReadyForWarp()
        {
            if (AI.State != AIState.FormationWarp ) return true;
            if (!isSpooling && PowerCurrent / (PowerStoreMax + 0.01f) < 0.2f) return false;
            if (engineState == MoveState.Warp) return true;
            return !Carrier.RecallingFighters();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Ship() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            supplyLock?.Dispose(ref supplyLock);
            AI?.Dispose();
            AI               = null;
            projectiles      = null;
            beams            = null;
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
            Warp,
        }

        private float RecalculateMaxHealth()
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

        public float PercentageOfShipByModules(ShipModuleType moduleType)
        {
            return RoleData.PercentageOfShipByModules(ModuleSlotList.FilterBy(module => module.ModuleType == moduleType), Size);
        }
        
        private ShipData.RoleName GetDesignRole() => new RoleData(this, ModuleSlotList).DesignRole;

        public void CreateColonizationBuildingFor(Planet colonizeTarget)
        {
            // @TODO create building placement methods in planet.cs that take into account the below logic.

            foreach (ShipModule slot in ModuleSlotList) 
            {
                if (slot?.HasColonyBuilding != true)
                    continue;

                Building template = ResourceManager.GetBuildingTemplate(slot.DeployBuildingOnColonize);
                if (template.Unique && colonizeTarget.BuildingExists(slot.DeployBuildingOnColonize))
                    continue;

                Building building = ResourceManager.CreateBuilding(template);
                colonizeTarget.BuildingList.Add(building);
                building.AssignBuildingToTileOnColonize(colonizeTarget);
               
            }
        }

        public void UnloadColonizationResourcesAt(Planet colonizeTarget)
        {
            foreach (ShipModule slot in ModuleSlotList)
            {
                if (slot?.HasColonyBuilding != true)
                    continue;
                colonizeTarget.FoodHere       += slot.numberOfFood;				
                colonizeTarget.ProductionHere += slot.numberOfEquipment;				
                colonizeTarget.Population     += slot.numberOfColonists;
            }
        }

        public void MarkShipRolesUsableForEmpire(Empire empire)
        {
            empire.canBuildBombers      = empire.canBuildBombers      || DesignRole == ShipData.RoleName.bomber;
            empire.canBuildCarriers     = empire.canBuildCarriers     || DesignRole == ShipData.RoleName.carrier;
            empire.canBuildSupportShips = empire.canBuildSupportShips || DesignRole == ShipData.RoleName.support;
            empire.canBuildTroopShips   = empire.canBuildTroopShips   || DesignRole == ShipData.RoleName.troopShip;
            empire.canBuildCorvettes    = empire.canBuildCorvettes    || DesignRole == ShipData.RoleName.corvette;
            empire.canBuildFrigates     = empire.canBuildFrigates     || DesignRole == ShipData.RoleName.frigate;
            empire.canBuildCruisers     = empire.canBuildCruisers     || DesignRole == ShipData.RoleName.cruiser;
            empire.canBuildCapitals     = empire.canBuildCapitals     || DesignRole == ShipData.RoleName.capital;
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

            foreach (ShipModule slot in ModuleSlotList)
            {
                //ShipModule template = GetModuleTemplate(slot.UID);
                if (slot.InstalledWeapon != null)
                {
                    weapons         = true;
                    numWeaponSlots += slot.Area;
                }
                fighters |= slot.hangarShipUID   != null && !slot.IsSupplyBay && !slot.IsTroopBay;

                offense += slot.CalculateModuleOffense();
                defense += slot.CalculateModuleDefense(Size);

                BaseCanWarp |= slot.WarpThrust > 0;
            }
            DPS = (int)offense;

            if (!fighters && !weapons) offense = 0f;

            return ShipBuilder.GetModifiedStrength(Size, numWeaponSlots, offense, defense, shipData.Role, velocityMaximum) ;
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
            float currentRepair;
            do
            {
                currentRepair = repairAmount;
                repairAmount = ApplyRepairOnce(repairAmount, repairLevel);
            }
            while (0.0f < repairAmount && repairAmount < currentRepair - 0.05f);
            ApplyRepairToShields(repairAmount);
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
                // fully damaged modules get priority 1.0
                float damagePriority =  module.Health < (module.ActualMaxHealth * repairSkill)
                                    ? 1.0f
                                    : 1.0f - module.HealthPercent;

                // best modules get priority 1.0
                float moduleImportance = 1.1f - (float)module.ModulePriority / ShipModule.MaxPriority;
                return damagePriority * moduleImportance;
            });

            return moduleToRepair.Repair(repairAmount);
        }

        public void ApplyPackDamageModifier()
        {
            float modifier     = -0.25f;
            modifier          += 0.05f * AI.FriendliesNearby.Count;
            PackDamageModifier = modifier.Clamped(0, 0.5f);
        }

        public override string ToString() => $"Ship Id={Id} '{VanityName}' Pos {Position}  Loyalty {loyalty} Role {DesignRole}" ;

        public bool ShipIsGoodForGoals(float baseStrengthNeeded = 0, Empire empire = null)
        {
            if (!Active) return false;
            empire = empire ?? loyalty;
            if (!shipData.BaseCanWarp) return false;

            float goodPowerSupply = PowerFlowMax - NetPower.NetWarpPowerDraw;
            float powerTime = GlobalStats.MinimumWarpRange;
            if (goodPowerSupply <0)
            {
                powerTime = PowerStoreMax / -goodPowerSupply * maxFTLSpeed;
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

        public bool ShipGoodToBuild(Empire empire)
        {
            if (shipData.HullRole == ShipData.RoleName.station ||
                shipData.HullRole == ShipData.RoleName.platform ||
                shipData.CarrierShip)
                return true;
            return ShipIsGoodForGoals(float.MinValue, empire);
        }

        public ShipStatus ToShipStatus(float valueToCheck, float maxValue)
        {
            if (maxValue <= 0)  return ShipStatus.NotApplicable;
            if (valueToCheck > maxValue)
            {
                Log.Info($"MaxValue of check as greater than value to check");
                return ShipStatus.NotApplicable;
            }

            var ratio = .5f + ShipStatusCount * valueToCheck / maxValue;
            ratio = ratio.Clamped(1, ShipStatusCount); 
            return (ShipStatus)(int)ratio;
        }
        //if the shipstatus enum is added to then "5" will need to be changed.
        //it should count all but "NotApplicable"
        private const int ShipStatusCount = 6;
    }
    
    public enum ShipStatus
    {        
        Critical =1,
        Poor,
        Average,        
        Good,
        Excellent,
        Maximum,
        NotApplicable
    }
}

