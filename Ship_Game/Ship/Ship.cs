using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using SgMotion.Controllers;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using Ship_Game.AI;
using Ship_Game.Debug;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship : GameplayObject, IDisposable
    {
        public string VanityName = ""; // user modifiable ship name. Usually same as Ship.Name
        public Array<Troop> TroopList = new Array<Troop>();
        public Array<Rectangle> AreaOfOperation = new Array<Rectangle>();
        public bool RecallFightersBeforeFTL = true;

        //public float DefaultFTLSpeed = 1000f;    //Not referenced in code, removing to save memory
        public float RepairRate = 1f;
        public float SensorRange = 20000f;
        public float yBankAmount = 0.007f;
        public float maxBank = 0.5235988f;

        public Vector2 projectedPosition;
        private Array<Thruster> ThrusterList = new Array<Thruster>();
        public bool TradingFood = true;
        public bool TradingProd = true;
        public bool ShieldsUp = true;
        //public float AfterBurnerAmount = 20.5f;    //Not referenced in code, removing to save memory
        //protected Color CloakColor = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);    //Not referenced in code, removing to save memory
        //public float CloakTime = 5f;    //Not referenced in code, removing to save memory
        //public Vector2 Origin = new Vector2(256f, 256f);        //Not referenced in code, removing to save memory
        private Array<Projectile> projectiles = new Array<Projectile>();
        private Array<Beam> beams = new Array<Beam>();
        public Array<Weapon> Weapons = new Array<Weapon>();
        private float JumpTimer = 3f;
        public AudioEmitter SoundEmitter = new AudioEmitter();
        public float ClickTimer = 10f;
        public Vector2 VelocityLast = new Vector2();
        public Vector2 ScreenPosition = new Vector2();
        public float ScuttleTimer = -1f;
        public Vector2 FleetOffset;
        public Vector2 RelativeFleetOffset;

        private ShipModule[] Shields;
        private Array<ShipModule> Hangars = new Array<ShipModule>();
        public Array<ShipModule> BombBays = new Array<ShipModule>();
        public bool shipStatusChanged;
        public Guid guid = Guid.NewGuid();
        public bool AddedOnLoad;
        private AnimationController animationController;
        public bool IsPlayerDesign;
        public bool IsSupplyShip;
        public bool reserved;
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
        public string ModelPath;
        public bool isThrusting;
        public float WarpDraw;
        public string Name;   // name of the original design of the ship, eg "Subspace Projector". Look at VanityName
        public float DamageModifier;
        public Empire loyalty;
        public int Size;
        //public int CrewRequired;    //Not referenced in code, removing to save memory
        //public int CrewSupplied;    //Not referenced in code, removing to save memory
        public float Ordinance;
        public float OrdinanceMax;
        //public float scale;    //Not referenced in code, removing to save memory
        public ShipAI AI { get; private set; }
        public float speed;
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
        public float ModulePowerDraw;
        public float ShieldPowerDraw;
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
        public float HealthMax;
        public float ShipMass;
        public int TroopCapacity;
        public float OrdAddedPerSecond;
        public bool HasTroopBay;
        //public bool WeaponCentered;    //Not referenced in code, removing to save memory
        private AudioHandle DroneSfx;
        public float ShieldRechargeTimer;
        public bool InCombat;
        private Vector3 pointat;
        private Vector3 scalefactors;
        public float xRotation;
        public MoveState engineState;
        public float ScreenRadius;
        //public float ScreenSensorRadius;    //Not referenced in code, removing to save memory
        public bool InFrustum;
        public bool NeedRecalculate;
        public bool Deleted;
        //public float CargoMass;    //Not referenced in code, removing to save memory
        public bool inborders;
        private bool fightersOut;
        private bool troopsOut;
        public bool Inhibited;
        private float BonusEMP_Protection;
        public bool inSensorRange;
        public bool disabled;
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

        public Array<ShipModule> Transporters = new Array<ShipModule>();
        public Array<ShipModule> RepairBeams = new Array<ShipModule>();
        public bool hasTransporter;
        public bool hasOrdnanceTransporter;
        public bool hasAssaultTransporter;
        public bool hasRepairBeam;
        public bool hasCommand;

        public float RangeForOverlay;
        public ReaderWriterLockSlim supplyLock = new ReaderWriterLockSlim();
        Array<ShipModule> AttackerTargetting = new Array<ShipModule>();
        public int TrackingPower;
        public int FixedTrackingPower;
        public Ship lastAttacker = null;
        private bool LowHealth; //fbedard: recalculate strength after repair
        public float TradeTimer;
        public bool shipInitialized;
        public float maxFTLSpeed;
        public float maxSTLSpeed;
        public float NormalWarpThrust;
        public float BoardingDefenseTotal => (MechanicalBoardingDefense  +TroopBoardingDefense);

        public Array<Empire> BorderCheck = new Array<Empire>();

        public float FTLModifier { get; private set; } = 1f;

        public T[] GetObjectsInSensors<T>() where T : GameplayObject => SpatialManagerForSystem(System).GetNearby<T>(Position, SensorRange);

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
                    if (loyalty.GetRelations(BorderCheck[i]).AtWar)
                        return true;
                return false;
            }            
        }

        private int Calculatesize()
        {
            int size = 0;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                size += module.XSIZE * module.YSIZE;
            }
            return size;
        }


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

        public bool needResupplyOrdnance
        {
            get
            {
                if (!(OrdinanceMax > 0f) || !(Ordinance / OrdinanceMax < 0.05f) || AI.HasPriorityTarget)
                    return false;
                return !AI.FriendliesNearby.Any(supply => supply.HasSupplyBays && supply.Ordinance >= 100);
            }

        }
        public bool NeedResupplyTroops
        {
            get
            {
                int assaultSpots = Hangars.Count(sm => sm.IsTroopBay);
                assaultSpots += Transporters.Sum(sm => sm.TransporterTroopLanding);

                int troops = Math.Min(TroopList.Count, assaultSpots);
                return assaultSpots != 0 && (troops / (float)assaultSpots) < 0.5f;
            }
        }
        public int ReadyPlanetAssaulttTroops
        {
            get
            {
                if (TroopList.IsEmpty)
                    return 0;
                int assaultSpots = Hangars.Count(sm => sm.hangarTimer > 0 && sm.IsTroopBay);
                assaultSpots += Transporters.Sum(sm => sm.TransporterTimer > 0 ? 0 : sm.TransporterTroopLanding);
                return Math.Min(TroopList.Count, assaultSpots);
            }
        }
        public float PlanetAssaultStrength
        {
            get
            {
                if (TroopList.IsEmpty)
                    return 0.0f;

                int assaultSpots = shipData.Role == ShipData.RoleName.troop ? TroopList.Count : 0;
                assaultSpots += Hangars.Count(sm => sm.IsTroopBay);
                assaultSpots += Transporters.Sum(sm => sm.TransporterTroopLanding);

                int troops = Math.Min(TroopList.Count, assaultSpots);
                return TroopList.SubRange(0, troops).Sum(troop => troop.Strength);
            }
        }
        public int PlanetAssaultCount
        {
            get
            {
                try
                {
                    int assaultSpots = 0;
                    if (shipData.Role == ShipData.RoleName.troop)
                    {
                        assaultSpots += TroopList.Count;

                    }
                    if (HasTroopBay)
                        for (int index = 0; index < Hangars.Count; index++)
                        {
                            ShipModule sm = Hangars[index];
                            if (sm.IsTroopBay)
                                assaultSpots++;
                        }
                    if (hasAssaultTransporter)
                        for (int index = 0; index < Transporters.Count; index++)
                        {
                            ShipModule at = Transporters[index];
                            assaultSpots += at.TransporterTroopLanding;
                        }

                    if (assaultSpots > 0)
                    {
                        int temp = assaultSpots - TroopList.Count;
                        assaultSpots -= temp < 0 ? 0 : temp;
                    }
                    return assaultSpots;
                }
                catch
                { }
                return 0;


            }
        }
        public bool HasSupplyBays
        {
            get
            {
                if (Hangars.Count > 0)
                    try
                    {
                        foreach (ShipModule shipModule in Hangars)
                        {
                            if (shipModule.IsSupplyBay)
                                return true;
                        }
                    }
                    catch
                    { }
                return false;
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
        public bool Resupplying
        {
            get
            {
                return AI.State == AIState.Resupply;
            }
            set
            {
                AI.OrderResupplyNearest(true);
            }
        }

        public bool FightersOut
        {
            get
            {
                bool flag = false;
                if (Hangars.Count <= 0)
                    return false;
                for (int index = 0; index < Hangars.Count; ++index)
                {
                    try
                    {
                        ShipModule shipModule = Hangars[index];
                        if (shipModule.IsTroopBay || shipModule.IsSupplyBay)
                            continue;
                        if (shipModule.GetHangarShip() != null

                            )
                        {
                            //if ()
                            //{
                            if (!shipModule.GetHangarShip().Active && shipModule.hangarTimer > 0.0)
                            {
                                //if (shipModule.hangarTimer >= 0.0)
                                continue;
                            }
                            flag = true;
                            //return false;
                            //}
                            //else
                            //    continue;
                        }
                        //else if (shipModule.hangarTimer <= 0.0 )
                        //    flag =true;
                        //else
                        //return flag;

                    }
                    catch
                    {
                    }
                }
                return flag;// !flag;
            }
            set
            {
                fightersOut = value;
                if (fightersOut && engineState != Ship.MoveState.Warp)
                    ScrambleFighters();
                else
                    RecoverFighters();
            }
        }

        public bool DoingTransport
        {
            get
            {
                return AI.State == AIState.SystemTrader;
            }
            set
            {
                AI.start = null;
                AI.end = null;
                AI.OrderTrade(5f);
            }
        }

        public bool DoingPassTransport
        {
            get
            {
                return AI.State == AIState.PassengerTransport;
            }
            set
            {
                AI.start = null;
                AI.end = null;
                AI.OrderTransportPassengers(5f);
            }
        }

        public bool TransportingFood
        {
            get
            {
                return TradingFood;
            }
            set
            {
                TradingFood = value;
            }
        }

        public bool TransportingProduction
        {
            get
            {
                return TradingProd;
            }
            set
            {
                TradingProd = value;
            }
        }

        public bool DoingExplore
        {
            get
            {
                return AI.State == AIState.Explore;
            }
            set
            {
                AI.OrderExplore();
            }
        }

        public bool DoingResupply
        {
            get
            {
                return AI.State == AIState.Resupply;
            }
            set
            {
                AI.OrderResupplyNearest(true);
            }
        }

        public bool DoingSystemDefense
        {
            get
            {
                return  loyalty.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this);
            }
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
        //added by gremlin : troops out property        
        public bool TroopsOut
        {
            get
            {
                //this.troopsout = false;
                if (troopsOut)
                {
                    troopsOut = true;
                    return true;
                }

                if (TroopList.Count == 0)
                {
                    troopsOut = true;
                    return true;
                }
                if (!Hangars.Any(troopbay => troopbay.IsTroopBay))
                {
                    troopsOut = true;
                    return true;
                }
                if (TroopList.Any(loyal => loyal.GetOwner() != loyalty))
                {
                    troopsOut = true;
                    return true;
                }

                if (troopsOut)
                    foreach (ShipModule hangar in Hangars)
                        if (hangar.IsTroopBay && (hangar.GetHangarShip() == null || hangar.GetHangarShip() != null && !hangar.GetHangarShip().Active) && hangar.hangarTimer <= 0)
                        {
                            troopsOut = false;
                            break;

                        }
                return troopsOut;
            }
            set
            {
                troopsOut = value;
                if (troopsOut)
                {
                    ScrambleAssaultShips(0);
                    return;
                }
                RecoverAssaultShips();
            }
        }
        public bool TroopsOutold
        {
            get
            {
                return troopsOut;
            }
            set
            {
                troopsOut = value;
                if (troopsOut)
                    ScrambleAssaultShips(0);
                else
                    RecoverAssaultShips();
            }
        }

        public bool doingScrap
        {
            get
            {
                return AI.State == AIState.Scrap;
            }
            set
            {
                AI.OrderScrapShip();
            }
        }

        public bool doingRefit
        {
            get => AI.State == AIState.Refit;
            set => Empire.Universe.ScreenManager.AddScreen(new RefitToWindow(Empire.Universe, this));
        }

        public void ShipRecreate()
        {
            Active            = false;
            AI.Target         = null;
            AI.ColonizeTarget = null;
            AI.EscortTarget   = null;
            AI.start          = null;
            AI.end            = null;
            AI.PotentialTargets.Clear();
            AI.NearbyShips.Clear();
            AI.FriendliesNearby.Clear();

            if (Mothership != null)
            {
                foreach (ShipModule shipModule in Mothership.Hangars)
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

        public Ship(Vector2 pos, Vector2 dim, float rot)
        {
            Position = pos;
            Rotation = rot;
            Dimensions = dim;
        }

        public void SetAnimationController(AnimationController ac, SkinnedModel model)
        {
            animationController = ac;
            animationController.StartClip(model.AnimationClips["Take 001"]);
        }

        //added by gremlin The Generals GetFTL speed
        public void SetmaxFTLSpeed()
        {
            //Added by McShooterz: hull bonus speed 

            if (InhibitedTimer < -.25f || Inhibited || System != null && engineState == MoveState.Warp)
            {
                if (Empire.Universe.GravityWells && System != null && !IsInFriendlySpace)
                {
                    for (int i = 0; i < System.PlanetList.Count; i++)
                    {
                        Planet planet = System.PlanetList[i];
                        if (Position.InRadius(planet.Position,
                            GlobalStats.GravityWellRange * planet.GravityWellRadius))
                        {
                            InhibitedTimer = .3f;
                            break;
                        }
                    }
                }
                if (InhibitedTimer < 0)
                    InhibitedTimer = 0.0f;
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
            maxFTLSpeed = (WarpThrust / base.Mass + WarpThrust / base.Mass * loyalty.data.FTLModifier) * FTLModifier;


        }
        public float GetmaxFTLSpeed => maxFTLSpeed;


        public float GetSTLSpeed()
        {
            //Added by McShooterz: hull bonus speed
            float speed = Thrust / Mass + Thrust / Mass * loyalty.data.SubLightModifier;
            return speed > 2500f ? 2500 : speed;
        }

        public void TetherToPlanet(Planet p)
        {
            TetheredTo = p;
            TetherOffset = Center - p.Position;
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
                HullBonus bonus = null;
                bool hasBonus = GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses
                    && ResourceManager.HullBonuses.TryGetValue(shipData.Hull, out bonus);

                if (hasBonus) cost += bonus.StartingCost;
                cost += cost * empire.data.Traits.ShipCostMod;
                if (hasBonus) cost *= 1f - bonus.CostBonus;
            }
            return (int)cost;
        }

        public ShipData GetShipData()
        {
            return ResourceManager.ShipsDict.TryGetValue(Name, out Ship sd) ? sd.shipData : null;
        }

        public void SetShipData(ShipData data)
        {
            shipData = data;
        }

        public void Explore()
        {
            AI.State = AIState.Explore;
            AI.HasPriorityOrder = true;
        }

        public void AttackShip(Ship target)
        {
            AI.State             = AIState.AttackTarget;
            AI.Target            = target;
            AI.HasPriorityOrder  = false;
            AI.HasPriorityTarget = true;
            InCombatTimer        = 15f;
        }


        public void ProcessInput(float elapsedTime)
        {
            if (GlobalStats.TakingInput || disabled || !hasCommand)
                return;
            if (Empire.Universe.Input != null)
                currentKeyBoardState = Empire.Universe.Input.CurrentKeyboardState;
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
                        if (yRotation > -maxBank)
                            yRotation -= yBankAmount;
                    }
                    else if (currentKeyBoardState.IsKeyDown(Keys.A))
                    {
                        isThrusting = true;
                        RotationalVelocity -= rotationRadiansPerSecond * elapsedTime;
                        isTurning = true;
                        if (Math.Abs(RotationalVelocity) > rotationRadiansPerSecond)
                            RotationalVelocity = -rotationRadiansPerSecond;
                        if (yRotation < maxBank)
                            yRotation += yBankAmount;
                    }
                    else if (engineState == Ship.MoveState.Warp)
                    {
                        isSpooling = true;
                        isTurning = false;
                        isThrusting = true;
                        Vector2.Normalize(vector2_1);
                        Ship ship1 = this;
                        Vector2 vector2_3 = ship1.Velocity + vector2_1 * (elapsedTime * speed);
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
                        Vector2 vector2_3 = ship.Velocity + vector2_1 * (elapsedTime * speed);
                        ship.Velocity = vector2_3;
                        if (Velocity.Length() > velocityMaximum)
                            Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
                    }
                    else if (currentKeyBoardState.IsKeyDown(Keys.S))
                    {
                        isThrusting = true;
                        Ship ship = this;
                        Vector2 vector2_3 = ship.Velocity - vector2_1 * (elapsedTime * speed);
                        ship.Velocity = vector2_3;
                        if (Velocity.Length() > velocityMaximum)
                            Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
                    }
                    MouseState state = Mouse.GetState();
                    if (state.RightButton == ButtonState.Pressed)
                    {
                        Vector3 position = Empire.Universe.Viewport.Unproject(new Vector3(state.X, state.Y, 0.0f), Empire.Universe.projection, Empire.Universe.view, Matrix.Identity);
                        Vector3 direction = Empire.Universe.Viewport.Unproject(new Vector3(state.X, state.Y, 1f), Empire.Universe.projection, Empire.Universe.view, Matrix.Identity) - position;
                        direction.Normalize();
                        var ray = new Ray(position, direction);
                        float num = -ray.Position.Z / ray.Direction.Z;
                        var pickedPos3D = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                        Vector2 pickedPos = pickedPos3D.ToVec2();
                        foreach (Weapon w in Weapons)
                        {
                            if (w.timeToNextFire <= 0.0 && w.moduleAttachedTo.Powered)
                            {
                                if (CheckIfInsideFireArc(w, pickedPos3D))
                                {
                                    if (w.isBeam)
                                        w.FireMouseBeam(pickedPos.Normalized());
                                    else 
                                        w.FireMouse((pickedPos - w.Center).Normalized());
                                }
                            }
                        }
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
            Ship targetship = target as Ship;
            ShipModule targetModule = target as ShipModule;
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
                    || !w.TargetValid(targetship.shipData.Role)

                    )
                    return false;
            }
            Vector2 PickedPos = target.Center;
            //radius = target.Radius;
            //added by gremlin attackrun compensator
            float modifyRangeAR = 50f;
            Vector2 pos = PickedPos;
            if (w.PrimaryTarget && !w.isBeam && AI.CombatState == CombatState.AttackRuns && maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                modifyRangeAR = speed;
                if (modifyRangeAR < 50)
                    modifyRangeAR = 50;
            }
            if (Vector2.Distance(pos, w.moduleAttachedTo.Center) > w.GetModifiedRange() + modifyRangeAR)//+radius)
            {
                return false;
            }
            return true;
        }
        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, GameplayObject target)
        {
            if (!CheckRangeToTarget(w, target))
                return false;
            Ship targetShip = target as Ship;
            if (w.MassDamage >0 || w.RepulsionDamage >0)
            {                
                if (targetShip != null && (targetShip.EnginesKnockedOut || targetShip.IsTethered() )) 
                {
                    return false;
                }
            }
            Relationship enemy;
            if
            (target != null && targetShip != null && (loyalty == targetShip.loyalty ||
             !loyalty.isFaction &&
           loyalty.TryGetRelations(targetShip.loyalty, out enemy) && enemy.Treaty_NAPact))
                return false;
            
            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;            
            Vector2 PickedPos = target.Center;            
            Vector2 pos = PickedPos;
            
            Vector2 toTarget = pos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians); //HelperFunctions.AngleToTarget(w.Center, target.Center);//
            float facing = w.moduleAttachedTo.Facing + MathHelper.ToDegrees(base.Rotation);

            
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
            
            if (w.moduleAttachedTo.Center.OutsideRadius(pos, w.GetModifiedRange()))
            {
                return false;
            }

            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;
            Vector2 toTarget = pos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians);
            float facing = w.moduleAttachedTo.Facing + MathHelper.ToDegrees(base.Rotation);
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

        public bool CheckIfInsideFireArc(Weapon w, Vector3 PickedPos )
        {

            //added by gremlin attackrun compensator
            float modifyRangeAR = 50f;
            Vector2 pos = new Vector2(PickedPos.X, PickedPos.Y);
            if (!w.isBeam && AI.CombatState == CombatState.AttackRuns && maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                modifyRangeAR = speed;
                if (modifyRangeAR < 50)
                    modifyRangeAR = 50;
            }
            if (Vector2.Distance(pos, w.moduleAttachedTo.Center) > w.GetModifiedRange() + modifyRangeAR )
            {
                return false;
            }

            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;
            Vector2 toTarget = pos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians);
            float facing = w.moduleAttachedTo.Facing + MathHelper.ToDegrees(base.Rotation);
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
            float distance = w.moduleAttachedTo.Center.Distance(pickedPos);

            if (w.MassDamage > 0 || w.RepulsionDamage > 0)
            {
                if (ship.EnginesKnockedOut || ship.IsTethered())
                    return false;
            }
            
            if (!w.isBeam && AI.CombatState == CombatState.AttackRuns && w.SalvoTimer > 0 && distance / w.SalvoTimer < w.Owner.speed) //&& this.maxWeaponsRange < 2000
            {
                modifyRangeAr = Math.Max(speed * w.SalvoTimer, 50f);
            }
            if (distance > w.GetModifiedRange() + modifyRangeAr + radius)
                return false;

            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;
            Vector2 toTarget = pickedPos - w.Center;
            float radians = (float)Math.Atan2(toTarget.X, toTarget.Y);
            float angleToMouse = 180f - radians.ToDegrees();
            float facing = w.moduleAttachedTo.Facing + Rotation.ToDegrees();
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

        // Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Vector2 pickedPos, float rotation)
        {
            if (w.moduleAttachedTo.Center.OutsideRadius(pickedPos, w.GetModifiedRange() + 50f))
                return false;
            
            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f + 1; //Gretman - Slight allowance for check (This version of CheckArc seems to only be called by the beam updater)
            Vector2 toTarget = pickedPos - w.Center;
            float radians = (float)Math.Atan2(toTarget.X, toTarget.Y);
            float angleToMouse = 180f - radians.ToDegrees();
            float facing = w.moduleAttachedTo.Facing + rotation.ToDegrees();
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

        public Array<Thruster> GetTList()
        {
            return ThrusterList;
        }

        public void AddThruster(Thruster t)
        {
            ThrusterList.Add(new Thruster
            {
                Parent = this,
                tscale = t.tscale,
                XMLPos = t.XMLPos
            });
        }

        public void SetTList(Array<Thruster> list)
        {
            ThrusterList = list;
        }

        public void SetSO(SceneObject so)
        {
            ShipSO = so;
            ShipSO.Visibility = ObjectVisibility.Rendered;
            Radius = ShipSO.WorldBoundingSphere.Radius;
        }

        public SceneObject GetSO()
        {
            return ShipSO;
        }

        public void UpdateInitialWorldTransform()
        {
            ShipSO.World = Matrix.CreateTranslation(new Vector3(Position, 0.0f));
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
            if (role == ShipData.RoleName.fighter  || role == ShipData.RoleName.scout)     return mod.UpkeepFighter;
            if (role == ShipData.RoleName.corvette || role == ShipData.RoleName.gunboat)   return mod.UpkeepCorvette;
            if (role == ShipData.RoleName.frigate  || role == ShipData.RoleName.destroyer) return mod.UpkeepFrigate;
            if (role == ShipData.RoleName.cruiser)   return mod.UpkeepCruiser;
            if (role == ShipData.RoleName.carrier)   return mod.UpkeepCarrier;
            if (role == ShipData.RoleName.capital)   return mod.UpkeepCapital;
            if (role == ShipData.RoleName.freighter) return mod.UpkeepFreighter;
            if (role == ShipData.RoleName.platform)  return mod.UpkeepPlatform;
            if (role == ShipData.RoleName.station)   return mod.UpkeepStation;
            if (role == ShipData.RoleName.drone && mod.useDrones) return mod.UpkeepDrone;
            return mod.UpkeepBaseline;
        }

        public float GetMaintCostRealism() => GetMaintCostRealism(loyalty);

        public float GetMaintCostRealism(Empire empire)
        {
            ShipData.RoleName role = shipData.Role;
            if (IsFreeUpkeepShip(role, loyalty))
                return 0f;

            float maint = GetCost(empire) * GetModMaintenanceModifier(role);

            if (maint <= 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0f)
            {
                maint = GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepBaseline;
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
                if (!empire.data.Privatization)
                    maint *= 0.5f;
            }

            if (GlobalStats.ShipMaintenanceMulti > 1)
            {
                maint *= GlobalStats.ShipMaintenanceMulti;
            }

            if (empire != null)
            {
                maint += maint * empire.data.Traits.MaintMod;
            }
            return maint;

        }

        private float GetFreighterSizeCostMultiplier()
        {
            switch (Size / 50)
            {
                default: return (int)(Size / 50);
                case 0: return 1.0f;
                case 1: return 1.5f;
                case 2: case 3: case 4: return 2f;
            }
        }

        private static float GetShipRoleMaintenance(ShipRole role, Empire empire)
        {
            for (int i = 0; i < role.RaceList.Count; ++i)
                if (role.RaceList[i].ShipType == empire.data.Traits.ShipType)
                    return role.RaceList[i].Upkeep;
            return role.Upkeep;
        }

        public float GetMaintCost() => GetMaintCost(loyalty);

        public float GetMaintCost(Empire empire)
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                return GetMaintCostRealism(empire);

            ShipData.RoleName role = shipData.Role;

            if (!ResourceManager.ShipRoles.TryGetValue(role, out ShipRole shipRole))
            {
                Log.Error("ShipRole {0} not found!", role);
                return 0f;
            }

            // Free upkeep ships
            if (shipData.ShipStyle == "Remnant" || empire?.data == null || 
                (Mothership != null && role >= ShipData.RoleName.fighter && role <= ShipData.RoleName.frigate))
                return 0f;

            float maint = GetShipRoleMaintenance(shipRole, empire);
            if (role == ShipData.RoleName.freighter)
                maint *= GetFreighterSizeCostMultiplier();

            if (role == ShipData.RoleName.freighter || role == ShipData.RoleName.platform)
            {
                maint *= empire.data.CivMaintMod;
                if (empire.data.Privatization)
                    maint *= 0.5f;
            }

            // Subspace Projectors do not get any more modifiers
            if (Name == "Subspace Projector")
                return maint;

            //added by gremlin shipyard exploit fix
            if (IsTethered())
            {
                if (role == ShipData.RoleName.platform)
                    return maint * 0.5f;
                if (shipData.IsShipyard)
                {
                    int numShipYards = GetTether().Shipyards.Count(shipyard => shipyard.Value.shipData.IsShipyard);
                    if (numShipYards > 3)
                        maint *= numShipYards - 3;
                }
            }

            // Maintenance fluctuator
            float maintModReduction = GlobalStats.ShipMaintenanceMulti;
            if (maintModReduction > 1)
            {
                if (IsInFriendlySpace || inborders)
                {
                    maintModReduction *= .25f;
                    if (inborders) maintModReduction *= .75f;
                }
                if (IsInNeutralSpace && !IsInFriendlySpace)
                {
                    maintModReduction *= .5f;
                }

                if (IsIndangerousSpace)
                {
                    maintModReduction *= 2f;
                }
                if (ActiveInternalSlotCount < InternalSlotCount)
                {
                    float damRepair = 2 - InternalSlotCount / ActiveInternalSlotCount;
                    if (damRepair > 1.5f) damRepair = 1.5f;
                    if (damRepair < 1) damRepair = 1;
                    maintModReduction *= damRepair;

                }
                if (maintModReduction < 1) maintModReduction = 1;
                maint *= maintModReduction;
            }
            return maint;
        }

        public int GetTechScore(out int[] techScores)
        {
            int [] scores = new int[4];
            scores[0] = 0;
            scores[1] = 0;
            scores[2] = 0;
            scores[3] = 0;
            foreach (ShipModule module in ModuleSlotList)
            {
                switch (module.ModuleType)
                {                    
                    case ShipModuleType.Turret:
                    case ShipModuleType.MainGun:
                    case ShipModuleType.MissileLauncher:
                    case ShipModuleType.Bomb:       scores[2] = Math.Max(scores[2], module.TechLevel); continue;
                    case ShipModuleType.PowerPlant: scores[3] = Math.Max(scores[3], module.TechLevel); continue;
                    case ShipModuleType.Engine:     scores[1] = Math.Max(scores[1], module.TechLevel); continue;
                    case ShipModuleType.Shield:     scores[0] = Math.Max(scores[0], module.TechLevel); continue;
                }
            }
            techScores = scores;
            return scores[1] + scores[3] + scores[0] + scores[2];
        }        

        public void DoEscort(Ship EscortTarget)
        {
            AI.OrderQueue.Clear();
            AI.State = AIState.Escort;
            AI.EscortTarget = EscortTarget;
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

        public void InitializeAI()
        {
            AI = new ShipAI(this);
            AI.State = AIState.AwaitingOrders;
            if (shipData == null)
                return;
            AI.CombatState = shipData.CombatState;
            AI.CombatAI = new CombatAI(this);
        }

        public void LoadFromSave()
        {
            foreach (KeyValuePair<string, ShipData> keyValuePair in ResourceManager.HullsDict)
            {
                if (keyValuePair.Value.ModelPath == ModelPath)
                {
                    if (keyValuePair.Value.Animated)
                    {
                        SkinnedModel skinnedModel = ResourceManager.GetSkinnedModel(ModelPath);
                        ShipSO = new SceneObject(skinnedModel.Model);
                        animationController = new AnimationController(skinnedModel.SkeletonBones);
                        animationController.StartClip(skinnedModel.AnimationClips["Take 001"]);
                    }
                    else
                    {
                        ShipSO = new SceneObject((ResourceManager.GetModel(ModelPath).Meshes)[0]);
                        ShipSO.ObjectType = ObjectType.Dynamic;
                    }
                }
            }
        }

        public void InitializeFromSave()
        {
            if (string.IsNullOrEmpty(VanityName))
                VanityName = Name;

            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;
            //Weapons.Clear();
            Center = new Vector2(Position.X + Dimensions.X / 2f, Position.Y + Dimensions.Y / 2f);
            Init(fromSave: true);
            if (ResourceManager.ShipsDict.ContainsKey(Name) && ResourceManager.ShipsDict[Name].IsPlayerDesign)
                IsPlayerDesign = true;
            else if (!ResourceManager.ShipsDict.ContainsKey(Name))
                FromSave = true;
            LoadInitializeStatus();
            if (Empire.Universe != null)
                Empire.Universe.ShipsToAdd.Add(this);

            SetSystem(System);
            InitExternalSlots();

            base.Initialize();
            foreach (ShipModule m in ModuleSlotList)
            {
                if (m.ModuleType == ShipModuleType.PowerConduit)
                    m.IconTexturePath = GetConduitGraphic(m);
                if (m.ModuleType == ShipModuleType.Hangar)
                {
                    Hangars.Add(m);
                }
                if (m.ModuleType == ShipModuleType.Transporter)
                {
                    Transporters.Add(m);
                    hasTransporter = true;
                    if (m.TransporterOrdnance > 0)
                        hasOrdnanceTransporter = true;
                    if (m.TransporterTroopAssault > 0)
                        hasAssaultTransporter = true;
                }
                if (m.IsRepairModule)
                    HasRepairModule = true;
                if (m.InstalledWeapon != null && m.InstalledWeapon.isRepairBeam)
                {
                    RepairBeams.Add(m);
                    hasRepairBeam = true;
                }
            }
            ShipSO.Visibility = ObjectVisibility.Rendered;
            Radius = ShipSO.WorldBoundingSphere.Radius;
            ShipStatusChange();
            shipInitialized = true;
            RecalculateMaxHP();            //Fix for Ship Max health being greater than all modules combined (those damned haphazard engineers). -Gretman

            if (VanityName == "MerCraft") Log.Info("Health from InitializeFromSave is:  " + HealthMax);
        }

        public override void Initialize()
        {
            if (string.IsNullOrEmpty(VanityName))
                VanityName = Name;

            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;
            SetShipData(GetShipData());
            //Weapons.Clear();
            Center = new Vector2(Position.X + Dimensions.X / 2f, Position.Y + Dimensions.Y / 2f);
            lock (GlobalStats.AddShipLocker)
            {
                Empire.Universe?.ShipsToAdd.Add(this);
            }

            Ship template = ResourceManager.ShipsDict[Name];
            IsPlayerDesign = template.IsPlayerDesign;

            InitializeStatus(fromSave: false);
            if (AI == null)
                InitializeAI();
            AI.CombatState = template.shipData.CombatState;
            InitExternalSlots();
            //this.hyperspace = (Cue)null;   //Removed to save space, because this is set to null in ship initilizers, and never reassigned. -Gretman
            base.Initialize();
            foreach (ShipModule module in ModuleSlotList)
            {
                if (module.UID == "Dummy")
                    continue;
                if (module.ModuleType == ShipModuleType.PowerConduit)
                    module.IconTexturePath = GetConduitGraphic(module);

                HasRepairModule |= module.IsRepairModule;
                isColonyShip    |= module.ModuleType == ShipModuleType.Colony;

                if (module.ModuleType == ShipModuleType.Transporter)
                {
                    hasTransporter = true;
                    hasOrdnanceTransporter |= module.TransporterOrdnance > 0;
                    hasAssaultTransporter  |= module.TransporterTroopAssault > 0;
                }
                hasRepairBeam |= module.InstalledWeapon != null && module.InstalledWeapon.isRepairBeam;
            }
            RecalculatePower();        
            ShipStatusChange();
            shipInitialized = true;
        }

        public void ResetJumpTimer()
        {
            JumpTimer = FTLSpoolTime * loyalty.data.SpoolTimeModifier;
        } 

        //added by gremlin: Fighter recall and stuff
        public void EngageStarDrive()
        {
            if (isSpooling || engineState == MoveState.Warp || GetmaxFTLSpeed <= 2500 )
            {
                return;
            }

            #region carrier figter interaction recall
            //added by gremlin : fighter recal
            if (RecallFightersBeforeFTL && GetHangars().Count > 0)
            {
                bool RecallFigters = false;
                float JumpDistance = Vector2.Distance(Center, AI.MovePosition);
                float slowfighter = speed * 2;
                if (JumpDistance > 7500f)
                {

                    RecallFigters = true;


                    foreach (ShipModule hangar in GetHangars())
                    {
                        Ship hangarShip = hangar.GetHangarShip();
                        if (hangar.IsSupplyBay || hangarShip == null) { RecallFigters = false; continue; }
                        //min jump distance 7500f
                        //this.MovePosition
                        if (hangarShip.speed < slowfighter) slowfighter = hangarShip.speed;

                        float rangeTocarrier = Vector2.Distance(hangarShip.Center, Center);

                        if (rangeTocarrier > SensorRange) { RecallFigters = false; continue; }

                        if (hangarShip.disabled || !hangarShip.hasCommand || hangarShip.dying || hangarShip.EnginesKnockedOut)
                        {
                            RecallFigters = false;
                            if (hangarShip.ScuttleTimer <= 0f) hangarShip.ScuttleTimer = 10f;
                            continue;
                        }


                        RecallFigters = true; break;
                    }
                }

                if (RecallFigters == true)
                {
                    RecoverAssaultShips();
                    RecoverFighters();
                    if (!DoneRecovering())
                    {


                        if (speed * 2 > slowfighter) { speed = slowfighter * .25f; }



                        return;

                    }
                }

            }
            #endregion
            if(EnginesKnockedOut)
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
                Center.InRadius(Empire.Universe.camPos.ToVec2(), 100000f) && Empire.Universe.camHeight < 250000)
            {
                GameAudio.PlaySfxAsync(GetEndWarpCue(), SoundEmitter);
                FTLManager.AddFTL(Center);
            }
            engineState = MoveState.Sublight;
            ResetJumpTimer();
            isSpooling = false;
            velocityMaximum = GetSTLSpeed();
            if (Velocity != Vector2.Zero)
                Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
            speed = velocityMaximum;
        }

        public bool isPlayerShip()
        {
            return PlayerShip;
        }

        ///added by gremlin Initialize status from deveks mod. 
        public bool InitializeStatus(bool fromSave)
        {
            Init(fromSave);
            base.Mass = 0f;
            Mass += (float)Size;
            Thrust                    = 0f;
            WarpThrust                = 0f;
            PowerStoreMax             = 0f;
            PowerFlowMax              = 0f;
            ModulePowerDraw           = 0f;
            ShieldPowerDraw           = 0f;
            shield_max                = 0f;
            shield_power              = 0f;
            armor_max                 = 0f;
            Size                      = Calculatesize();
            velocityMaximum           = 0f;
            speed                     = 0f;
            SensorRange               = 0f;
            float sensorBonus         = 0f;
            OrdinanceMax              = 0f;
            OrdAddedPerSecond         = 0f;
            rotationRadiansPerSecond  = 0f;
            base.Health               = 0f;
            TroopCapacity             = 0;
            MechanicalBoardingDefense = 0f;
            TroopBoardingDefense      = 0f;
            ECMValue                  = 0f;
            FTLSpoolTime              = 0f;
            RangeForOverlay           = 0f;

            string troopType = "Wyvern";
            string tankType = "Wyvern";
            string redshirtType = "Wyvern";

            foreach (Weapon w in Weapons)
            {
                if (w.GetModifiedRange() > RangeForOverlay)
                    RangeForOverlay = w.GetModifiedRange();
            }


            IReadOnlyList<Troop> unlockedTroops = loyalty?.GetUnlockedTroops();
            if (unlockedTroops?.Count > 0)
            {
                troopType    = unlockedTroops.FindMax(troop => troop.SoftAttack).Name;
                tankType     = unlockedTroops.FindMax(troop => troop.HardAttack).Name;
                redshirtType = unlockedTroops.FindMin(troop => troop.SoftAttack).Name; // redshirts are weakest

                troopType = (troopType == redshirtType) ? tankType : troopType;
            }

            #region ModuleCheck
            
            foreach (ShipModule module in ModuleSlotList)
            {
                if (module.Restrictions == Restrictions.I)
                    ++InternalSlotCount;
                if (module.ModuleType == ShipModuleType.Colony)
                    isColonyShip = true;
                if (module.ModuleType == ShipModuleType.Construction)
                {
                    isConstructor = true;
                    shipData.Role = ShipData.RoleName.construction;
                }
                
                //if (module.ResourceStorageAmount > 0f && ResourceManager.GoodsDict.TryGetValue(module.ResourceStored, out Good good) && !good.IsCargo)
                //{
                //    MaxGoodStorageDict[module.ResourceStored] += module.ResourceStorageAmount;
                //}

                for (int i = 0; i < module.TroopsSupplied; i++) // TroopLoad (?)
                {
                    int numTroopHangars = ModuleSlotList.Count(hangarbay => hangarbay.IsTroopBay);
                    if (numTroopHangars < TroopList.Count)
                    {
                        string type = troopType; // ex: "Space Marine"
                        if (TroopList.Count(trooptype => trooptype.Name == tankType) <= numTroopHangars / 2)
                            type = tankType;

                        TroopList.Add(ResourceManager.CreateTroop(type, loyalty));
                    }
                    else
                    {
                        TroopList.Add(ResourceManager.CreateTroop(redshirtType, loyalty));
                    }
                }
                if (module.SensorRange > SensorRange)
                {
                    SensorRange = module.SensorRange;
                }
                if (module.SensorBonus > sensorBonus)
                {
                    sensorBonus = module.SensorBonus;
                }
                if (module.ECM > ECMValue)
                {
                    ECMValue = module.ECM;
                    if (ECMValue > 1.0f)
                        ECMValue = 1.0f;
                    if (ECMValue < 0f)
                        ECMValue = 0f;
                }
                TroopCapacity += module.TroopCapacity;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;
                if (MechanicalBoardingDefense < 1f)
                {
                    MechanicalBoardingDefense = 1f;
                }
                if (module.ModuleType == ShipModuleType.Hangar)
                {
                    if (module.IsTroopBay)
                    {
                        HasTroopBay = true;
                    }
                }
                if (module.ModuleType == ShipModuleType.Transporter)
                    Transporters.Add(module);
                if (module.InstalledWeapon != null && module.InstalledWeapon.isRepairBeam)
                    RepairBeams.Add(module);
                if (module.ModuleType == ShipModuleType.Armor && loyalty != null)
                {
                    float modifiedMass = module.Mass * loyalty.data.ArmourMassModifier;
                    Mass += modifiedMass;
                }
                else
                    Mass += module.Mass;
                Thrust += module.thrust;
                WarpThrust += module.WarpThrust;
                //Added by McShooterz: fuel cell modifier apply to all modules with power store
                PowerStoreMax += module.PowerStoreMax + module.PowerStoreMax * (loyalty?.data.FuelCellModifier ?? 0);
                PowerCurrent += module.PowerStoreMax;
                PowerFlowMax += module.PowerFlowMax + (module.PowerFlowMax * loyalty?.data.PowerFlowMod ?? 0);
                shield_max += module.shield_power_max + (module.shield_power_max * loyalty?.data.ShieldPowerMod ?? 0);
                if (module.ModuleType == ShipModuleType.Armor)
                {
                    armor_max += module.HealthMax;
                }
                
                CargoSpaceMax += module.Cargo_Capacity;
                OrdinanceMax += module.OrdinanceCapacity;
                Ordinance += module.OrdinanceCapacity;
                if(module.ModuleType != ShipModuleType.Shield)
                    ModulePowerDraw += module.PowerDraw;
                else
                    ShieldPowerDraw += module.PowerDraw;
                Health += module.HealthMax;
                if (module.FTLSpoolTime > FTLSpoolTime)
                    FTLSpoolTime = module.FTLSpoolTime;
            }

            #endregion
            #region BoardingDefense
            foreach (Troop troopList in TroopList)
            {
                troopList.SetOwner(loyalty);
                troopList.SetShip(this);
                Ship troopBoardingDefense = this;
                troopBoardingDefense.TroopBoardingDefense = troopBoardingDefense.TroopBoardingDefense + (float)troopList.Strength;
            }
            {
                //mechanicalBoardingDefense1.MechanicalBoardingDefense = mechanicalBoardingDefense1.MechanicalBoardingDefense / (number_Internal_modules);
                MechanicalBoardingDefense *= (1 + TroopList.Count() / 10);
                if (MechanicalBoardingDefense < 1f)
                {
                    MechanicalBoardingDefense = 1f;
                }
            }
            #endregion
            HealthMax = base.Health;
            ActiveInternalSlotCount = InternalSlotCount;
            velocityMaximum = Thrust / Mass;
            speed = velocityMaximum;
            rotationRadiansPerSecond = speed / (float)Size;
            ShipMass = Mass;
            shield_power = shield_max;
            SensorRange += sensorBonus;
            if (FTLSpoolTime <= 0f)
                FTLSpoolTime = 3f;

            return true;
        }


        public void LoadInitializeStatus()
        {
            Mass                      = 0.0f;
            Thrust                    = 0.0f;
            PowerStoreMax             = 0.0f;
            PowerFlowMax              = 0.0f;
            ModulePowerDraw           = 0.0f;
            shield_max                = 0.0f;
            shield_power              = 0.0f;
            armor_max                 = 0.0f;
            velocityMaximum           = 0.0f;
            speed                     = 0.0f;
            OrdinanceMax              = 0.0f;
            rotationRadiansPerSecond  = 0.0f;
            Health                    = 0.0f;
            TroopCapacity             = 0;
            MechanicalBoardingDefense = 0.0f;
            TroopBoardingDefense      = 0.0f;
            ECMValue                  = 0.0f;
            FTLSpoolTime              = 0f;
            Size                      = Calculatesize();

            foreach (ShipModule module in ModuleSlotList)
            {
                if (module.Restrictions == Restrictions.I)
                    ++InternalSlotCount;
                
                if (module.ECM > ECMValue)
                {
                    ECMValue = module.ECM;
                    if (ECMValue > 1.0f)
                        ECMValue = 1.0f;
                    if (ECMValue < 0f)
                        ECMValue = 0f;
                }

                float massModifier = 1.0f;
                if (module.ModuleType == ShipModuleType.Armor && loyalty != null)
                    massModifier = loyalty.data.ArmourMassModifier;
                Mass += module.Mass * massModifier;                
                Thrust += module.thrust;
                WarpThrust += module.WarpThrust;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;
                //Added by McShooterz
                PowerStoreMax += module.PowerStoreMax + (module.PowerStoreMax * loyalty?.data.FuelCellModifier ?? 0);
                PowerFlowMax  += module.PowerFlowMax  + (module.PowerFlowMax  * loyalty?.data.PowerFlowMod ?? 0);
                shield_max += module.GetShieldsMax();
                shield_power += module.ShieldPower;
                if (module.ModuleType == ShipModuleType.Armor)
                    armor_max += module.HealthMax;                
                CargoSpaceMax += module.Cargo_Capacity;
                OrdinanceMax += (float)module.OrdinanceCapacity;
                if (module.ModuleType != ShipModuleType.Shield)
                    ModulePowerDraw += module.PowerDraw;
                else
                    ShieldPowerDraw += module.PowerDraw;
                Health += module.HealthMax;
                TroopCapacity += module.TroopCapacity;
                if (module.FTLSpoolTime > FTLSpoolTime)
                    FTLSpoolTime = module.FTLSpoolTime;
            }
            MechanicalBoardingDefense += (Size / 20);
            if (MechanicalBoardingDefense < 1f)
                MechanicalBoardingDefense = 1f;


            HealthMax                   = Health;
            velocityMaximum             = Thrust / Mass;
            speed                       = velocityMaximum;
            rotationRadiansPerSecond    = speed / 700f;
            ActiveInternalSlotCount = InternalSlotCount;
            ShipMass                    = Mass;
            if (FTLSpoolTime == 0)
                FTLSpoolTime = 3f;
        }

        public void RenderOverlay(SpriteBatch spriteBatch, Rectangle drawRect, bool showModules)
        {
            if (!ResourceManager.TryGetHull(shipData.Hull, out ShipData hullData))
                return;
            if (hullData.SelectionGraphic.NotEmpty() && !showModules)
            {
                Rectangle destinationRectangle = drawRect;
                destinationRectangle.X += 2;

                spriteBatch.Draw(ResourceManager.Texture("SelectionBox Ships/" + hullData.SelectionGraphic), destinationRectangle, Color.White);
                if (shield_power > 0.0)
                {
                    byte alpha = (byte)(shield_percent * 255.0f);
                    spriteBatch.Draw(ResourceManager.Texture("SelectionBox Ships/" + hullData.SelectionGraphic + "_shields"), destinationRectangle, new Color(Color.White, alpha));
                }
            }
            if (!showModules || hullData.SelectionGraphic.IsEmpty() || ModuleSlotList.Length == 0)
                return;

            ShipModule[] sortedByX = ModuleSlotList.CloneArray();
            sortedByX.Sort(slot => slot.XMLPosition.X);

            ShipModule[] sortedByY = ModuleSlotList.CloneArray();
            sortedByY.Sort(slot => slot.XMLPosition.Y);

            float spanX = sortedByX[sortedByX.Length - 1].XMLPosition.X - sortedByX[0].XMLPosition.X + 16.0f;
            float spanY = sortedByY[sortedByY.Length - 1].XMLPosition.Y - sortedByY[0].XMLPosition.Y + 16.0f;

            int maxSpan = (int)Math.Max(spanX, spanY) / 16 + 1;

            float moduleSize = (drawRect.Width / maxSpan);
            if (moduleSize < 2.0)
                moduleSize = drawRect.Width / (float)maxSpan;
            if (moduleSize > 10.0)
                moduleSize = 10f;
            foreach (ShipModule module in ModuleSlotList)
            {
                Vector2 moduleOffset = module.XMLPosition - new Vector2(264f, 264f);
                Vector2 vector2_2 = new Vector2(moduleOffset.X / 16f, moduleOffset.Y / 16f) * moduleSize;
                if (Math.Abs(vector2_2.X) > (drawRect.Width / 2) || Math.Abs(vector2_2.Y) > (drawRect.Height / 2))
                {
                    moduleSize = (float)(drawRect.Width / (maxSpan + 10));
                    break;
                }
            }
            foreach (ShipModule module in ModuleSlotList)
            {
                Vector2 moduleOffset = module.XMLPosition - new Vector2(264f, 264f);
                moduleOffset = new Vector2(moduleOffset.X / 16f, moduleOffset.Y / 16f) * moduleSize;
                var rect = new Rectangle(drawRect.X + drawRect.Width / 2 + (int)moduleOffset.X, drawRect.Y + drawRect.Height / 2 + (int)moduleOffset.Y, (int)moduleSize, (int)moduleSize);

                Primitives2D.FillRectangle(spriteBatch, rect, module.GetHealthStatusColor());
            }
        }

        //added by gremlin deveksmod scramble assault ships
        public void ScrambleAssaultShips(float strengthNeeded)
        {
            bool flag = strengthNeeded > 0;
            foreach (ShipModule slot in ModuleSlotList.Where(slot => slot.ModuleType == ShipModuleType.Hangar && slot.IsTroopBay && TroopList.Count > 0 && slot.GetHangarShip() == null && slot.hangarTimer <= 0f))
            {                
                if (flag && strengthNeeded < 0)
                    break;
                strengthNeeded -= TroopList[0].Strength;
                slot.LaunchBoardingParty(TroopList[0]);
                TroopList.RemoveAt(0);
            }
        }

        public void RecoverAssaultShips()
        {
            for (int i = 0; i < Hangars.Count; ++i)
            {
                ShipModule shipModule = Hangars[i];
                if (shipModule.GetHangarShip() != null && shipModule.GetHangarShip().Active)
                {
                    if (shipModule.IsTroopBay)
                    {
                        if (shipModule.GetHangarShip().TroopList.Count != 0)
                            shipModule.GetHangarShip().ReturnToHangar();
                    }
                }
            }
        }

        public void ScrambleFighters()
        {
            for (int i = 0; i < Hangars.Count; ++i)
                Hangars[i].ScrambleFighters();
        }

        public void RecoverFighters()
        {
            for (int i = 0; i < Hangars.Count; ++i)
            {
                ShipModule shipModule = Hangars[i];
                if (shipModule.GetHangarShip() != null && shipModule.GetHangarShip().Active)
                    shipModule.GetHangarShip().ReturnToHangar();
            }
        }



        public bool Init(bool fromSave = false)
        {
            if (fromSave) SetShipData(GetShipData());
            //Weapons.Clear();
            RecalculatePower();
            return true;
        }

        public override void Update(float elapsedTime)
        {
            if (!Active)
                return;

            if (ScuttleTimer > -1.0 || ScuttleTimer <-1.0)
            {
                ScuttleTimer -= elapsedTime;
                if (ScuttleTimer <= 0.0)
                    Die(null, true);
            }
            if (System == null || System.isVisible)
            {
                var sphere = new BoundingSphere(new Vector3(Position, 0.0f), 2000f);

                if (Empire.Universe.Frustum.Contains(sphere) != ContainmentType.Disjoint && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    InFrustum = true;
                    ShipSO.Visibility = ObjectVisibility.Rendered;
                }
                else
                {
                    InFrustum = false;
                    ShipSO.Visibility = ObjectVisibility.None;
                }
            }
            else
            {
                InFrustum = false;
                ShipSO.Visibility = ObjectVisibility.None;
            }
            
            ShieldRechargeTimer += elapsedTime;
            InhibitedTimer -= elapsedTime;
            Inhibited = InhibitedTimer > 0.0f;//|| this.maxFTLSpeed < 2500f;
            if ((Inhibited || maxFTLSpeed < 2500f) && engineState == MoveState.Warp)
            {
                HyperspaceReturn();
            }
            if (TetheredTo != null)
            {
                Position = TetheredTo.Position + TetherOffset;
                Center   = TetheredTo.Position + TetherOffset;
                velocityMaximum = 0;
            }
            if (Mothership != null && !Mothership.Active)
                Mothership = null;

            if (dying)
            {
                ThrusterList.Clear();
                dietimer -= elapsedTime;
                if (dietimer <= 1.9f && InFrustum && DeathSfx.IsStopped)
                {
                    string cueName;
                    if      (Size < 80)  cueName = "sd_explosion_ship_warpdet_small";
                    else if (Size < 250) cueName = "sd_explosion_ship_warpdet_medium";
                    else                 cueName = "sd_explosion_ship_warpdet_large";
                    DeathSfx.PlaySfxAsync(cueName, SoundEmitter);
                }
                if (dietimer <= 0.0f)
                {
                    reallyDie = true;
                    Die(LastDamagedBy, true);
                    return;
                }

                if (Velocity.LengthSquared() > velocityMaximum*velocityMaximum) // RedFox: use SqLen instead of Len
                    Velocity = Vector2.Normalize(Velocity) * velocityMaximum;

                Vector2 deltaMove = Velocity * elapsedTime;
                Position += deltaMove;
                Center   += deltaMove;

                int num1 = UniverseRandom.IntBetween(0, 60);
                if (num1 >= 57 && InFrustum)
                {
                    Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                    ExplosionManager.AddExplosion(position, ShipSO.WorldBoundingSphere.Radius, 2.5f, 0.2f);
                    Empire.Universe.flash.AddParticleThreadA(position, Vector3.Zero);
                }
                if (num1 >= 40)
                {
                    Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                    Empire.Universe.sparks.AddParticleThreadA(position, Vector3.Zero);
                }
                yRotation += xdie * elapsedTime;
                xRotation += ydie * elapsedTime;

                //Ship ship3 = this;
                //double num2 = (double)this.Rotation + (double)this.zdie * (double)elapsedTime;
                Rotation += zdie * elapsedTime;
                if (ShipSO == null)
                    return;
                if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView && inSensorRange)
                {
                    ShipSO.World = Matrix.Identity * Matrix.CreateRotationY(yRotation) 
                                                   * Matrix.CreateRotationX(xRotation) 
                                                   * Matrix.CreateRotationZ(Rotation) 
                                                   * Matrix.CreateTranslation(new Vector3(Center, 0.0f));
                    if (shipData.Animated)
                    {
                        ShipSO.SkinBones = animationController.SkinnedBoneTransforms;
                        animationController.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                    }
                }
                for (int i = 0; i < projectiles.Count; ++i)
                {
                    Projectile projectile = projectiles[i];
                    if (projectile == null)
                        continue;
                    if (projectile.Active)
                        projectile.Update(elapsedTime);
                    else
                        projectiles.RemoveRef(projectile);
                }
                SoundEmitter.Position = new Vector3(Center, 0);
                for (int i = 0; i < ModuleSlotList.Length; i++)
                {
                    ModuleSlotList[i].UpdateWhileDying(elapsedTime);
                }
            }
            else if (!dying)
            {
                if (System != null && elapsedTime > 0.0)
                {
                    foreach (Planet p in System.PlanetList)
                    {
                        if (p.Position.OutsideRadius(Center, 3000f * 3000f))
                            continue;
                        if (p.ExploredDict[loyalty]) // already explored
                            continue;

                        if (loyalty == EmpireManager.Player)
                        {
                            for (int index = 0; index < p.BuildingList.Count; index++)
                            {
                                Building building = p.BuildingList[index];
                                if (!string.IsNullOrEmpty(building.EventTriggerUID))
                                    Empire.Universe.NotificationManager.AddFoundSomethingInteresting(p);
                            }
                        }
                        p.ExploredDict[loyalty] = true;
                        for (int index = 0; index < p.BuildingList.Count; index++)
                        {
                            Building building = p.BuildingList[index];
                            if (string.IsNullOrEmpty(building.EventTriggerUID) ||
                                loyalty == EmpireManager.Player || p.Owner != null) continue;

                            MilitaryTask militaryTask = new MilitaryTask
                            {
                                AO = p.Position,
                                AORadius = 50000f,
                                type = MilitaryTask.TaskType.Exploration
                            };
                            militaryTask.SetTargetPlanet(p);
                            militaryTask.SetEmpire(loyalty);
                            loyalty.GetGSAI().TaskList.Add(militaryTask);
                        }
                    }
                    if (AI.BadGuysNear && InCombat && System != null)
                    {
                        System.CombatInSystem = true;
                        System.combatTimer = 15f;
                    }
                }
                if (disabled)
                {
                    float third = Radius / 3f;
                    for (int i = 5 - 1; i >= 0; --i)
                    {
                        Vector3 randPos = UniverseRandom.Vector32D(third);
                        Empire.Universe.lightning.AddParticleThreadA(Center.ToVec3() + randPos, Vector3.Zero);
                    }
                }
                //Ship ship1 = this;
                //float num1 = this.Rotation + this.RotationalVelocity * elapsedTime;
                Rotation += RotationalVelocity * elapsedTime;
                if (Math.Abs(RotationalVelocity) > 0.0)
                    isTurning = true;

                if (!isSpooling && Afterburner.IsPlaying)
                    Afterburner.Stop();

                ClickTimer -= elapsedTime;
                if (ClickTimer < 0.0)
                    ClickTimer = 10f;
                if (Active)
                {
                    InCombatTimer -= elapsedTime;
                    if (InCombatTimer > 0.0)
                    {
                        InCombat = true;
                    }
                    else
                    {
                        if (InCombat)
                            InCombat = false;
                        if (AI.State == AIState.Combat && loyalty != EmpireManager.Player)
                        {
                            AI.State = AIState.AwaitingOrders;
                            AI.OrderQueue.Clear();
                        }
                    }
                    Position += Velocity * elapsedTime;
                    Center   += Velocity * elapsedTime;
                    UpdateShipStatus(elapsedTime);
                    if (!Active)
                        return;
                    if (!disabled && !Empire.Universe.Paused) //this.hasCommand &&
                        AI.Update(elapsedTime);
                    if (InFrustum)
                    {
                        if (ShipSO == null)
                            return;
                        ShipSO.World = Matrix.Identity 
                            * Matrix.CreateRotationY(yRotation) 
                            * Matrix.CreateRotationZ(Rotation) 
                            * Matrix.CreateTranslation(new Vector3(Center, 0.0f));

                        if (shipData.Animated && animationController != null)
                        {
                            ShipSO.SkinBones = animationController.SkinnedBoneTransforms;
                            animationController.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                        }
                        else if (shipData != null && animationController != null && shipData.Animated)
                        {
                            ShipSO.SkinBones = animationController.SkinnedBoneTransforms;
                            animationController.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                        }
                        foreach (Thruster thruster in ThrusterList)
                        {
                            thruster.SetPosition();
                            Vector2 vector2_3 = new Vector2((float)Math.Sin((double)Rotation), -(float)Math.Cos((double)Rotation));
                            vector2_3 = Vector2.Normalize(vector2_3);
                            float num2 = Velocity.Length() / velocityMaximum;
                            if (isThrusting)
                            {
                                if (engineState == Ship.MoveState.Warp)
                                {
                                    if (thruster.heat < num2)
                                        thruster.heat += 0.06f;
                                    pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                    scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                    thruster.update(thruster.WorldPos, pointat, scalefactors, thruster.heat, 0.004f, Color.OrangeRed, Color.LightBlue, Empire.Universe.camPos);
                                }
                                else
                                {
                                    if (thruster.heat < num2)
                                        thruster.heat += 0.06f;
                                    if (thruster.heat > 0.600000023841858)
                                        thruster.heat = 0.6f;
                                    pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                    scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                    thruster.update(thruster.WorldPos, pointat, scalefactors, thruster.heat, 1.0f / 500.0f, Color.OrangeRed, Color.LightBlue, Empire.Universe.camPos);
                                }
                            }
                            else
                            {
                                pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                thruster.heat = 0.01f;
                                thruster.update(thruster.WorldPos, pointat, scalefactors, 0.1f, 1.0f / 500.0f, Color.OrangeRed, Color.LightBlue, Empire.Universe.camPos);
                            }
                        }
                    }
                    if (isSpooling)
                        fightersOut = false;
                    if (isSpooling && !Inhibited && GetmaxFTLSpeed > 2500)
                    {
                        JumpTimer -= elapsedTime;
                        //task gremlin move fighter recall here.

                        if (JumpTimer <= 4.0) // let's see if we can sync audio to behaviour with new timers
                        {
                            if (Empire.Universe.camHeight < 250000 && Empire.Universe.camPos.InRadius(Center, 100000f)
                                && JumpSfx.IsStopped)
                            {
                                JumpSfx.PlaySfxAsync(GetStartWarpCue(), SoundEmitter);
                            }
                        }
                        if (JumpTimer <= 0.1)
                        {
                            if (engineState == MoveState.Sublight)
                            {
                                FTLManager.AddFTL(Center);
                                engineState = MoveState.Warp;
                            }
                            else engineState = MoveState.Sublight;
                            isSpooling = false;
                            ResetJumpTimer();
                        }
                    }
                    if (isPlayerShip())
                    {
                        if ((!isSpooling || !Active) && Afterburner.IsPlaying)
                        {
                            Afterburner.Stop();
                        }
                        if (isThrusting && AI.State == AIState.ManualControl && DroneSfx.IsStopped)
                        {
                            DroneSfx.PlaySfxAsync("starcruiser_drone01", SoundEmitter);
                        }
                        else if ((!isThrusting || !Active) && DroneSfx.IsPlaying)
                        {
                            DroneSfx.Stop();
                        }
                    }
                    SoundEmitter.Position = new Vector3(Center, 0);
                    
                }
                if (elapsedTime > 0.0f)
                {
                    if (projectiles.Count > 0)
                    {
                        //standard for loop through each weapon group.
                        for (int i = projectiles.Count - 1; i >= 0; --i)
                        {
                            Projectile projectile = projectiles[i];
                            if (projectile?.Active == true)
                                projectiles[i].Update(elapsedTime);
                            else
                                projectiles.RemoveAtSwapLast(i);
                        }
                    }

                    if (beams.Count > 0)
                    {
                        //source = Enumerable.Range(0, this.beams.Count).ToArray();
                        //rangePartitioner = Partitioner.Create(0, source.Length);
                        //handle each weapon group in parallel
                        //global::System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (range, loopState) =>
                        //Parallel.For(this.beams.Count, (start, end) =>
                        {
                            //standard for loop through each weapon group.
                            //for (int T = start; T < end; T++)
                            for (int i = 0; i < beams.Count; i++)
                            {
                                Beam beam = beams[i];
                                if (beam.ModuleAttachedTo != null)
                                {
                                    ShipModule shipModule = beam.ModuleAttachedTo;
                                    Vector2 origin = (int)shipModule.XSIZE != 1
                                        || (int)shipModule.YSIZE != 3
                                        ? ((int)shipModule.XSIZE != 2 || (int)shipModule.YSIZE != 5 ? new Vector2(shipModule.Center.X - 8f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2))
                                        : new Vector2(shipModule.Center.X - 80f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2))) : new Vector2(shipModule.Center.X - 50f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2));
                                    Vector2 target = new Vector2(shipModule.Center.X - 8f, shipModule.Center.Y - 8f);
                                    float angleToTarget = origin.AngleToTarget(shipModule.Center);
                                    Vector2 angleAndDistance = shipModule.Center.PointFromAngle(MathHelper.ToDegrees(shipModule.Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));
                                    float num2 = (float)((int)shipModule.XSIZE * 16 / 2);
                                    float num3 = (float)((int)shipModule.YSIZE * 16 / 2);
                                    float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num2, 2.0) + (float)Math.Pow((double)num3, 2.0)));
                                    float radians = 3.141593f - (float)Math.Asin((double)num2 / (double)distance) + shipModule.GetParent().Rotation;
                                    origin = angleAndDistance.PointFromAngle(MathHelper.ToDegrees(radians), distance);
                                    int thickness = (int)UniverseRandom.RandomBetween(beam.Thickness*0.75f, beam.Thickness*1.1f);

                                    beam.Update(beam.ModuleAttachedTo != null ? origin : beam.Owner.Center, 
                                        beam.FollowMouse ? Empire.Universe.mouseWorldPos : beam.Destination, 
                                        thickness, Empire.Universe.view, Empire.Universe.projection, elapsedTime);

                                    if (beam.Duration < 0f && !beam.Infinite)
                                    {
                                        beam.Die(null, false);
                                        beams.RemoveRef(beam);
                                    }
                                }
                                else
                                {
                                    beam.Die(null, false);
                                }
                            }

                        }//); 
                    }
                   
                }
            }
        }

        private void CheckAndPowerConduit(ShipModule module)
        {
            if (!module.Active)
                return;
            module.Powered = true;
            module.CheckedConduits = true;
            foreach (ShipModule slot in ModuleSlotList)
            {
                if (slot != module && slot.ModuleType == ShipModuleType.PowerConduit && !slot.CheckedConduits && 
                    (int)Math.Abs(module.Position.X - slot.Position.X) / 16 + 
                    (int)Math.Abs(module.Position.Y - slot.Position.Y) / 16 == 1)
                    CheckAndPowerConduit(slot);
            }
        }

        public void RecalculatePower()
        {
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule slot = ModuleSlotList[i];
                slot.Powered = false;
                slot.CheckedConduits = false;
            }

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];

                if (module.ModuleType == ShipModuleType.PowerPlant && module.Active)
                {
                    foreach (ShipModule slot2 in ModuleSlotList)
                    {
                        if (slot2.ModuleType == ShipModuleType.PowerConduit
                            && ((int)Math.Abs(slot2.Position.X - module.Position.X) / 16 + (int)Math.Abs(slot2.Position.Y - module.Position.Y) / 16 == 1))
                            CheckAndPowerConduit(slot2);
                    }
                }
            }

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || (module.PowerRadius < 1 && module.ModuleType != ShipModuleType.PowerConduit) || module.Powered)
                    continue;

                float cx = module.XSIZE * 8;
                      cx = cx <= 8 ? module.Position.X : module.Position.X + cx;
                float cy = module.YSIZE * 8;
                      cy = cy <= 8 ? module.Position.Y : module.Position.Y + cy;

                int powerRadius = module.PowerRadius * 16 + 8;

                foreach (ShipModule slot2 in ModuleSlotList)

                {
                    if (!slot2.Active || slot2.PowerDraw < 1)
                        continue;
                    if ((int)Math.Abs(cx - slot2.Position.X) / 16 + (int)Math.Abs(cy - slot2.Position.Y) / 16 <= powerRadius)
                    {
                        slot2.Powered = true;
                        continue;
                    }
                    for (int y = 0; y < slot2.YSIZE; ++y)
                    {
                        if (slot2.Powered) break;
                        float sy = slot2.Position.Y + (y * 16);
                        for (int x = 0; x < slot2.XSIZE; ++x)
                        {
                            if (x == 0 && y == 0)
                                continue;

                            float sx = slot2.Position.X + (x * 16);
                            if ((int)Math.Abs(cx - sx) / 16 + (int)Math.Abs(cy - sy) /16 <= powerRadius)
                            {
                                slot2.Powered = true;
                                break;
                            }

                        }
                    }
                }

            }

            foreach (ShipModule module in ModuleSlotList)
            {
                if (!module.Powered && module.IndirectPower)
                    module.Powered = true;
            }
        }
        public ShipData ToShipData()
        {
            var data = new ShipData();
            data.BaseCanWarp      = shipData.BaseCanWarp;
            data.BaseStrength     = BaseStrength;
            data.techsNeeded      = shipData.techsNeeded;
            data.TechScore        = shipData.TechScore;
            data.ShipCategory     = shipData.ShipCategory;
            data.Name             = Name;
            data.Level            = (byte)Level;
            data.experience       = (byte)experience;
            data.Role             = shipData.Role;
            data.IsShipyard       = GetShipData().IsShipyard;
            data.IsOrbitalDefense = GetShipData().IsOrbitalDefense;
            data.Animated         = GetShipData().Animated;
            data.CombatState      = AI.CombatState;
            data.ModelPath        = GetShipData().ModelPath;
            data.ModuleSlots      = GetModuleSlotDataArray();
            data.ThrusterList     = new Array<ShipToolScreen.ThrusterZone>();
            data.MechanicalBoardingDefense = MechanicalBoardingDefense;
            foreach (Thruster thruster in ThrusterList)
                data.ThrusterList.Add(new ShipToolScreen.ThrusterZone()
                {
                    Scale = thruster.tscale,
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
                    Position           = module.XMLPosition,
                    InstalledModuleUID = module.UID,
                    Health             = module.Health,
                    ShieldPower        = module.ShieldPower,
                    Facing             = module.Facing,
                    Restrictions       = module.Restrictions
                };

                if (module.GetHangarShip() != null)
                    data.HangarshipGuid = module.GetHangarShip().guid;

                if (module.ModuleType == ShipModuleType.Hangar)
                    data.SlotOptions = module.hangarShipUID;

                slots[i] = data;
            }
            return slots;
        }

        public float CalculateRange()
        {
            return 200000f;
        }

        private string GetConduitGraphic(ShipModule forModule)
        {
            bool right = false;
            bool left  = false;
            bool down  = false;
            bool up    = false;
            int sides  = 0;
            foreach (ShipModule module in ModuleSlotList)
            {
                if (module != forModule && module.ModuleType == ShipModuleType.PowerConduit)
                {
                    int dx = (int)Math.Abs(module.XMLPosition.X - forModule.XMLPosition.X) / 16;
                    int dy = (int)Math.Abs(module.XMLPosition.Y - forModule.XMLPosition.Y) / 16;
                    if (dx == 1 && dy == 0)
                    {
                        if (module.XMLPosition.X > forModule.XMLPosition.X)
                            right = true;
                        else
                            left = true;
                    }
                    if (dy == 1 && dx == 0)
                    {
                        if (module.XMLPosition.Y > forModule.XMLPosition.Y)
                            up = true;
                        else
                            down = true;
                    }
                }
            }
            if (left)   ++sides;
            if (right)  ++sides;
            if (down)   ++sides;
            if (up) ++sides;
            if (sides <= 1)
            {
                if (down) return "Conduits/conduit_powerpoint_down";
                if (up) return "Conduits/conduit_powerpoint_up";
                if (left) return "Conduits/conduit_powerpoint_right";
                return right ? "Conduits/conduit_powerpoint_left" : "Conduits/conduit_intersection";
            }
            else
            {
                if (sides == 3)
                {
                    if (down && up && left) return "Conduits/conduit_tsection_left";
                    if (down && up && right) return "Conduits/conduit_tsection_right";
                    if (left && right && up) return "Conduits/conduit_tsection_down";
                    if (left && right && down) return "Conduits/conduit_tsection_up";
                }
                else
                {
                    if (sides == 4)
                        return "Conduits/conduit_intersection";
                    if (sides == 2)
                    {
                        if (left && down)
                            return "Conduits/conduit_corner_BR";
                        if (left && up)
                            return "Conduits/conduit_corner_TR";
                        if (right && down)
                            return "Conduits/conduit_corner_BL";
                        if (right && up)
                            return "Conduits/conduit_corner_TL";
                        if (down && up)
                            return "Conduits/conduit_straight_vertical";
                        if (left && right)
                            return "Conduits/conduit_straight_horizontal";
                    }
                }
                return "";
            }
        }

        public Array<ShipModule> GetHangars()
        {
            return Hangars;
        }

        public void DamageShieldInvisible(Ship damageSource, float damageAmount)
        {
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldPower > 0f)
                    shield.Damage(damageSource, damageAmount);
            }
        }

        public Array<ShipModule> GetTroopHangars()
        {
            var returnList = new Array<ShipModule>();
            foreach (ShipModule s in Hangars)
                if (s.IsTroopBay) returnList.Add(s);
            return returnList;
        }
        public bool DoneRecovering()
        {
            for (int i = 0; i < Hangars.Count; ++i)
            {
                bool? hangarShipActive = Hangars[i]?.GetHangarShip()?.Active;
                if (hangarShipActive.HasValue && hangarShipActive.Value)
                    return false;
            }
            return true;
        }

        public void UpdateShipStatus(float elapsedTime)
        {
            //if (elapsedTime == 0.0f)
            //    return;


            if (velocityMaximum == 0f && shipData.Role <= ShipData.RoleName.station)
            {
                Rotation += 0.003f;
            }
            MoveModulesTimer -= elapsedTime;
            updateTimer -= elapsedTime;
            //Disable if enough EMP damage
            if (elapsedTime > 0 && (EMPDamage > 0 || disabled))
            {
                --EMPDamage;
                if (EMPDamage < 0.0)
                    EMPDamage = 0.0f;

                disabled = EMPDamage > Size + BonusEMP_Protection;
            }
            //this.CargoMass = 0.0f;    //Not referenced in code, removing to save memory
            if (Rotation > 2.0 * Math.PI)
            {
                //Ship ship = this;
                //float num = ship.rotation - 6.28318548202515f;
                Rotation -= 6.28318548202515f;
            }
            if (Rotation < 0.0)
            {
                //Ship ship = this;
                //float num = ship.rotation + 6.28318548202515f;
                Rotation += 6.28318548202515f;
            }
            if (InCombat && !disabled && hasCommand || PlayerShip)
            {
                foreach (Weapon weapon in Weapons)
                    weapon.Update(elapsedTime);
            }
            TroopBoardingDefense = 0.0f;
            for (int i = 0; i < TroopList.Count; i++)
            {
                Troop troop = TroopList[i];
                troop.SetShip(this);
                if (troop.GetOwner() == loyalty)
                    TroopBoardingDefense += troop.Strength;
            }
            if (updateTimer <= 0.0) //|| shipStatusChanged)
            {
                if ((InCombat && !disabled && hasCommand || PlayerShip) && Weapons.Count > 0)
                {
                    
                    AI.CombatAI.UpdateCombatAI(this);
                    IOrderedEnumerable<Weapon> orderedEnumerable;
                    if (AI.CombatState == CombatState.ShortRange)
                        orderedEnumerable = Enumerable.OrderBy<Weapon, float>((IEnumerable<Weapon>)Weapons, (Func<Weapon, float>)(weapon => weapon.GetModifiedRange()));
                    else
                        orderedEnumerable = Enumerable.OrderByDescending<Weapon, float>((IEnumerable<Weapon>)Weapons, (Func<Weapon, float>)(weapon => weapon.GetModifiedRange()));
                    bool flag = false;
                    foreach (Weapon weapon in (IEnumerable<Weapon>)orderedEnumerable)
                    {
                        //Edited by Gretman
                        //This fixes ships with only 'other' damage types thinking it has 0 range, causing them to fly through targets even when set to attack at max/min range
                        if (!flag && (weapon.DamageAmount > 0.0 || weapon.EMPDamage > 0.0 || weapon.SiphonDamage > 0.0 || weapon.MassDamage > 0.0 || weapon.PowerDamage > 0.0 || weapon.RepulsionDamage > 0.0))
                        {
                            maxWeaponsRange = weapon.GetModifiedRange();
                            if (!weapon.Tag_PD) flag = true;
                        }
                        Weapon weaponTemplate = ResourceManager.GetWeaponTemplate(weapon.UID);
                        weapon.fireDelay = weaponTemplate.fireDelay;
                        //Added by McShooterz: weapon tag modifiers with check if mod uses them
                        if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useWeaponModifiers)
                        {                            

                            if (weapon.Tag_Beam)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Beam"].Rate;
                            if (weapon.Tag_Energy)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Energy"].Rate;
                            if (weapon.Tag_Explosive)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Explosive"].Rate;
                            if (weapon.Tag_Guided)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Guided"].Rate;
                            if (weapon.Tag_Hybrid)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Hybrid"].Rate;
                            if (weapon.Tag_Intercept)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Intercept"].Rate;
                            if (weapon.Tag_Kinetic)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Kinetic"].Rate;
                            if (weapon.Tag_Missile)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Missile"].Rate;
                            if (weapon.Tag_Railgun)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Railgun"].Rate;
                            if (weapon.Tag_Torpedo)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Torpedo"].Rate;
                            if (weapon.Tag_Cannon)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Cannon"].Rate;
                            if (weapon.Tag_Subspace)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Subspace"].Rate;
                            if (weapon.Tag_PD)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["PD"].Rate;
                            if (weapon.Tag_Bomb)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Bomb"].Rate;
                            if (weapon.Tag_SpaceBomb)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Spacebomb"].Rate;
                            if (weapon.Tag_BioWeapon)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["BioWeapon"].Rate;
                            if (weapon.Tag_Drone)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Drone"].Rate;
                            if (weapon.Tag_Warp)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Warp"].Rate;
                            if (weapon.Tag_Array)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Array"].Rate;
                            if (weapon.Tag_Flak)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Flak"].Rate;
                            if (weapon.Tag_Tractor)
                                weapon.fireDelay += -weaponTemplate.fireDelay * loyalty.data.WeaponTags["Tractor"].Rate;
                        }
                        //Added by McShooterz: Hull bonus Fire Rate
                        if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                        {
                            HullBonus mod;
                            if (Ship_Game.ResourceManager.HullBonuses.TryGetValue(shipData.Hull, out mod))
                                weapon.fireDelay *= 1f - mod.FireRateBonus;
                        }
                    }
                }

                try
                {
                    if (InhibitedTimer < 2f)
                        foreach (Empire index1 in EmpireManager.Empires)
                        {
                            if (index1 != loyalty && !loyalty.GetRelations(index1).Treaty_OpenBorders)
                            {
                                for (int index2 = 0; index2 < index1.Inhibitors.Count; ++index2)
                                {
                                    Ship ship = index1.Inhibitors[index2];
                                    if (ship != null && Vector2.Distance(Center, ship.Position) <= ship.InhibitionRadius)
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
                catch (Exception ex)
                {
                    Log.Error(ex, "Inhibitor blew up");
                }
                inSensorRange = false;
                if (Empire.Universe.Debug || loyalty == EmpireManager.Player || loyalty != EmpireManager.Player && EmpireManager.Player.GetRelations(loyalty).Treaty_Alliance)
                    inSensorRange = true;
                else if (!inSensorRange)
                {
                    Ship[] nearby = GetObjectsInSensors<Ship>();
                    foreach (Ship ship in nearby)
                    {
                        if (ship.loyalty == EmpireManager.Player && (Center.InRadius(ship.Position, ship.SensorRange) || Empire.Universe.Debug))
                        {
                            inSensorRange = true;
                            break;
                        }
                    }
                }
                if (shipStatusChanged || InCombat)
                    ShipStatusChange();
                //Power draw based on warp
                if (!inborders && engineState == Ship.MoveState.Warp)
                {
                    PowerDraw = (loyalty.data.FTLPowerDrainModifier * ModulePowerDraw) + (WarpDraw * loyalty.data.FTLPowerDrainModifier / 2);
                }
                else if (engineState != Ship.MoveState.Warp && ShieldsUp)
                    PowerDraw = ModulePowerDraw + ShieldPowerDraw;
                else
                    PowerDraw = ModulePowerDraw;

                //This is what updates all of the modules of a ship
                if (loyalty.RecalculateMaxHP) HealthMax = 0;
                foreach (ShipModule slot in ModuleSlotList)
                    slot.Update(1f);
                //Check Current Shields
                if (engineState == Ship.MoveState.Warp || !ShieldsUp)
                    shield_power = 0f;
                else
                {
                    if (InCombat || shield_power != shield_max)
                    {
                        shield_power = 0.0f;
                        foreach (ShipModule shield in Shields)
                            shield_power += shield.ShieldPower;
                        if (shield_power > shield_max)
                            shield_power = shield_max;
                    }
                }
                //Add ordnance
                if (Ordinance < OrdinanceMax)
                {
                    Ordinance += OrdAddedPerSecond;
                    if (Ordinance > OrdinanceMax)
                        Ordinance = OrdinanceMax;
                }
                else
                    Ordinance = OrdinanceMax;
                //Repair
                if (Health < HealthMax)
                {
                    shipStatusChanged = true;
                    if (!InCombat || GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useCombatRepair)
                    {
                        //Added by McShooterz: Priority repair
                        float repairTracker = InCombat ? RepairRate * 0.1f : RepairRate;
                        var damagedModules = ModuleSlotList
                            .Where(slot => slot.Health < slot.HealthMax)
                            .OrderBy(slot => slot.ModulePriority);
                        foreach (ShipModule moduleSlot in damagedModules)
                        {
                            //if destroyed do not repair in combat
                            if (InCombat && moduleSlot.Health < 1)
                                continue;
                            if (moduleSlot.HealthMax - moduleSlot.Health > repairTracker)
                            {
                                moduleSlot.Repair(repairTracker);
                                break;
                            }
                            else
                            {
                                repairTracker -= moduleSlot.HealthMax - moduleSlot.Health;
                                moduleSlot.Repair(moduleSlot.HealthMax);
                            }
                        }
                    }
                }
                else
                {
                    shipStatusChanged = false;
                }
                Array<Troop> OwnTroops = new Array<Troop>();
                Array<Troop> EnemyTroops = new Array<Troop>();
                foreach (Troop troop in TroopList)
                {
                    if (troop.GetOwner() == loyalty)
                        OwnTroops.Add(troop);
                    else
                        EnemyTroops.Add(troop);
                }
                if (HealPerTurn > 0)
                {
                    foreach (Troop troop in OwnTroops)
                    {
                        if (troop.Strength < troop.GetStrengthMax())
                        {
                            troop.Strength += HealPerTurn;
                        }
                        else
                            troop.Strength = troop.GetStrengthMax();
                    }
                }
                if (EnemyTroops.Count > 0)
                {
                    float num1 = 0;
                    for (int index = 0; index < MechanicalBoardingDefense; ++index)
                    {
                        if (UniverseRandom.RandomBetween(0.0f, 100f) <= 60.0f)
                            ++num1;
                    }
                    foreach (Troop troop in EnemyTroops)
                    {
                        float num2 = num1;
                        if (num1 > 0)
                        {
                            if (num1 > troop.Strength)
                            {
                                float num3 = troop.Strength;
                                troop.Strength = 0;
                                num1 -= num3;
                            }
                            else
                            {
                                troop.Strength -= num1;
                                num1 -= num2;
                            }
                            if (troop.Strength <= 0)
                                TroopList.Remove(troop);
                        }
                        else
                            break;
                    }
                    EnemyTroops.Clear();
                    foreach (Troop troop in TroopList)
                        EnemyTroops.Add(troop);
                    if (OwnTroops.Count > 0 && EnemyTroops.Count > 0)
                    {
                        foreach (Troop troop in OwnTroops)
                        {
                            for (int index = 0; index < troop.Strength; ++index)
                            {
                                if (UniverseRandom.IntBetween(0, 100) >= troop.BoardingStrength)
                                    ++num1;
                            }
                        }
                        foreach (Troop troop in EnemyTroops)
                        {
                            float num2 = num1;
                            if (num1 > 0)
                            {
                                if (num1 > troop.Strength)
                                {
                                    float num3 = troop.Strength;
                                    troop.Strength = 0;
                                    num1 -= num3;
                                }
                                else
                                {
                                    troop.Strength -= num1;
                                    num1 -= num2;
                                }
                                if (troop.Strength <= 0)
                                    TroopList.Remove(troop);
                                if (num1 <= 0)
                                    break;
                            }
                            else
                                break;
                        }
                    }
                    EnemyTroops.Clear();
                    foreach (Troop troop in TroopList)
                        EnemyTroops.Add(troop);
                    if (EnemyTroops.Count > 0)
                    {
                        float num2 = 0;
                        foreach (Troop troop in EnemyTroops)
                        {
                            for (int index = 0; index < troop.Strength; ++index)
                            {
                                if (UniverseRandom.IntBetween(0, 100) >= troop.BoardingStrength)
                                    ++num2;
                            }
                        }
                        foreach (Troop troop in OwnTroops)
                        {
                            float num3 = num2;
                            if (num2 > 0)
                            {
                                if (num2 > troop.Strength)
                                {
                                    float num4 = troop.Strength;
                                    troop.Strength = 0;
                                    num2 -= num4;
                                }
                                else
                                {
                                    troop.Strength -= num2;
                                    num2 -= num3;
                                }
                                if (troop.Strength <= 0)
                                    TroopList.Remove(troop);
                            }
                            else
                                break;
                        }
                        if (num2 > 0)
                        {
                            MechanicalBoardingDefense -= (float)num2;
                            if (MechanicalBoardingDefense < 0.0)
                                MechanicalBoardingDefense = 0.0f;
                        }
                    }
                    OwnTroops.Clear();
                    foreach (Troop troop in TroopList)
                    {
                        if (troop.GetOwner() == loyalty)
                            OwnTroops.Add(troop);
                    }
                    if (OwnTroops.Count == 0 && MechanicalBoardingDefense <= 0.0)
                    {
                        loyalty.GetShips().QueuePendingRemoval(this);
                        loyalty = EnemyTroops[0].GetOwner();
                        loyalty.AddShipNextFrame(this);
                        if (fleet != null)                                                   
                            ClearFleet();                            
                        
                        AI.ClearOrdersNext = true;
                        AI.State = AIState.AwaitingOrders;
                    }
                }
                //this.UpdateSystem(elapsedTime);
                updateTimer = 1f;
                if (NeedRecalculate)
                {
                    RecalculatePower();
                    NeedRecalculate = false;
                }

            }
            else if (Active && AI.BadGuysNear || (InFrustum && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView) || MoveModulesTimer > 0.0f || GlobalStats.ForceFullSim) 
            {
                if (elapsedTime > 0.0f || UpdatedModulesOnce)
                {
                    UpdatedModulesOnce = elapsedTime > 0;

                    float cos = (float)Math.Cos(Rotation);
                    float sin = (float)Math.Sin(Rotation);
                    float tan = (float)Math.Tan(yRotation);
                    var slots = ModuleSlotList;
                    for (int i = 0; i < slots.Length; ++i)
                    {
                        if (!Active) break;
                        slots[i].UpdateEveryFrame(elapsedTime, cos, sin, tan);
                        ++GlobalStats.ModuleUpdates;
                    }
                }
            }
            SetmaxFTLSpeed();
            if (Ordinance > OrdinanceMax)
                Ordinance = OrdinanceMax;
            InternalSlotsHealthPercent = (float)ActiveInternalSlotCount / InternalSlotCount;
            if (InternalSlotsHealthPercent < 0.35f)
                Die(LastDamagedBy, false);
            if (Mass < (Size / 2))
                Mass = (Size / 2);
            PowerCurrent -= PowerDraw * elapsedTime;
            if (PowerCurrent < PowerStoreMax)
                PowerCurrent += (PowerFlowMax + (PowerFlowMax * loyalty?.data.PowerFlowMod ?? 0)) * elapsedTime;

            if (PowerCurrent <= 0.0f)
            {
                PowerCurrent = 0.0f;
                HyperspaceReturn();
            }
            if (PowerCurrent > PowerStoreMax)
                PowerCurrent = PowerStoreMax;
            if (shield_percent < 0.0f)
                shield_percent = 0.0f;
            shield_percent = 100.0 * shield_power / shield_max;
            if (shield_percent < 0.0f)
                shield_percent = 0.0f;
            if (Mass <= 0.0f)
                Mass = 1f;
            switch (engineState)
            {
                case MoveState.Sublight:
                    velocityMaximum = GetSTLSpeed();
                    break;
                case MoveState.Warp:
                    velocityMaximum = GetmaxFTLSpeed;
                    break;
            }

            speed = velocityMaximum;
            rotationRadiansPerSecond = TurnThrust / Mass / 700f;
            rotationRadiansPerSecond += (float)(rotationRadiansPerSecond * Level * 0.0500000007450581);
            yBankAmount = rotationRadiansPerSecond * elapsedTime;// 50f;
            if (engineState == Ship.MoveState.Warp)
            {
                //if (this.FTLmodifier != 1f)
                //this.velocityMaximum *= this.FTLmodifier;
                Velocity = Vector2.Normalize(new Vector2((float)Math.Sin((double)Rotation), -(float)Math.Cos((double)Rotation))) * velocityMaximum;
            }
            if ((Thrust <= 0.0f || Mass <= 0.0f) && !IsTethered())
            {
                EnginesKnockedOut = true;
                velocityMaximum = Velocity.Length();
                Velocity -= Velocity * (elapsedTime * 0.1f);
                if (engineState == MoveState.Warp)
                    HyperspaceReturn();
            }
            else
                EnginesKnockedOut = false;
            if (Velocity.Length() <= velocityMaximum)
                return;
            Velocity = Vector2.Normalize(Velocity) * velocityMaximum;
        }

        public void ShipStatusChange()
        {
            Health = 0f;
            float sensorBonus = 0f;
            Hangars.Clear();
            Transporters.Clear();
            Thrust                      = 0f;
            Mass                        = Size / 2f;
            shield_max                  = 0f;
            ActiveInternalSlotCount     = 0;
            BonusEMP_Protection         = 0f;
            PowerStoreMax               = 0f;
            PowerFlowMax                = 0f;
            OrdinanceMax                = 0f;
            ModulePowerDraw             = 0.0f;
            ShieldPowerDraw             = 0f;
            RepairRate                  = 0f;
            CargoSpaceMax              = 0f;
            SensorRange                 = 0f;
            HasTroopBay                 = false;
            WarpThrust                  = 0f;
            TurnThrust                  = 0f;
            NormalWarpThrust            = 0f;
            FTLSlowTurnBoost            = false;
            InhibitionRadius            = 0f;
            OrdAddedPerSecond           = 0f;
            WarpDraw                    = 0f;
            HealPerTurn                 = 0;
            ECMValue                    = 0f;
            FTLSpoolTime                = 0f;
            hasCommand                  = IsPlatform;
            TrackingPower               = 0;
            FixedTrackingPower          = 0;

            foreach (ShipModule slot in ModuleSlotList)
            {
                //Get total internal slots
                if (slot.Restrictions == Restrictions.I && slot.Active)
                    ++ActiveInternalSlotCount;
                Health += slot.Health;
                //if (this.shipStatusChanged)
                {
                    RepairRate += slot.BonusRepairRate;
                    if (slot.Mass < 0.0 && slot.Powered)
                    {
                        //Ship ship3 = this;
                        //float num3 = ship3.Mass + moduleSlot.module.Mass;     //Some minor performance tweaks -Gretman
                        Mass += slot.Mass;
                    }
                    else if (slot.Mass > 0.0)
                    {
                        //Ship ship3 = this;

                        //float num3;
                        if (slot.ModuleType == ShipModuleType.Armor && loyalty != null)
                        {
                            float ArmourMassModifier = loyalty.data.ArmourMassModifier;
                            float ArmourMass = slot.Mass * ArmourMassModifier;
                            Mass += ArmourMass;
                        }
                        else
                        {
                            Mass += slot.Mass;
                        }
                        //ship3.Mass = num3;
                    }
                    //Checks to see if there is an active command module

                    if (slot.Active && (slot.Powered || slot.PowerDraw <= 0f))
                    {
                        if (!hasCommand && slot.IsCommandModule)
                            hasCommand = true;
                        //Doctor: For 'Fixed' tracking power modules - i.e. a system whereby a module provides a non-cumulative/non-stacking tracking power.
                        //The normal stacking/cumulative tracking is added on after the for loop for mods that want to mix methods. The original cumulative function is unaffected.
                        if (slot.FixedTracking > 0 && slot.FixedTracking > FixedTrackingPower)
                            FixedTrackingPower = slot.FixedTracking;
                        if (slot.TargetTracking > 0)
                            TrackingPower += slot.TargetTracking;
                        OrdinanceMax += slot.OrdinanceCapacity;
                        CargoSpaceMax += slot.Cargo_Capacity;
                        InhibitionRadius += slot.InhibitionRadius;
                        BonusEMP_Protection += slot.EMP_Protection;
                        if (slot.SensorRange > SensorRange)
                            SensorRange = slot.SensorRange;
                        if (slot.SensorBonus > sensorBonus)
                            sensorBonus = slot.SensorBonus;
                        if (slot.shield_power_max > 0f)
                        {
                            shield_max += slot.GetShieldsMax();
                            ShieldPowerDraw += slot.PowerDraw;
                        }
                        else
                            ModulePowerDraw += slot.PowerDraw;
                        Thrust += slot.thrust;
                        WarpThrust += slot.WarpThrust;
                        TurnThrust += slot.TurnThrust;
                        if (slot.ECM > ECMValue)
                        {
                            ECMValue = slot.ECM;
                            if (ECMValue > 1.0f)
                                ECMValue = 1.0f;
                            if (ECMValue < 0f)
                                ECMValue = 0f;
                        }
                        OrdAddedPerSecond += slot.OrdnanceAddedPerSecond;
                        HealPerTurn += slot.HealPerTurn;
                        if (slot.ModuleType == ShipModuleType.Hangar)
                        {
                            Hangars.Add(slot);
                            if (slot.IsTroopBay)
                                HasTroopBay = true;
                        }
                        if (slot.ModuleType == ShipModuleType.Transporter)
                            Transporters.Add(slot);
                        if (slot.InstalledWeapon != null && slot.InstalledWeapon.isRepairBeam)
                            RepairBeams.Add(slot);
                        if (slot.PowerStoreMax > 0)
                            PowerStoreMax += slot.PowerStoreMax;
                        if (slot.PowerFlowMax >  0)
                            PowerFlowMax += slot.PowerFlowMax;
                        WarpDraw += slot.PowerDrawAtWarp;
                        if (slot.FTLSpoolTime > FTLSpoolTime)
                            FTLSpoolTime = slot.FTLSpoolTime;
                    }
                }
            }
            NormalWarpThrust = WarpThrust;
            //Doctor: Add fixed tracking amount if using a mixed method in a mod or if only using the fixed method.
            TrackingPower += FixedTrackingPower;
            
            //Update max health due to bonuses that increase module health
            if (Health > HealthMax)
                HealthMax = Health;
            //if (this.shipStatusChanged)
            {
                SensorRange += sensorBonus;
                //Apply modifiers to stats
                if (loyalty != null)
                {
                    Mass          *= loyalty.data.MassModifier;
                    RepairRate    += (float)(RepairRate * Level * 0.05) + RepairRate * loyalty.data.Traits.RepairMod;
                    PowerFlowMax  += PowerFlowMax * loyalty.data.PowerFlowMod;
                    PowerStoreMax += PowerStoreMax * loyalty.data.FuelCellModifier;
                    SensorRange   *= loyalty.data.SensorModifier;
                }
                if (FTLSpoolTime <= 0)
                    FTLSpoolTime = 3f;
                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses && 
                    ResourceManager.HullBonuses.TryGetValue(shipData.Hull, out HullBonus mod))
                {
                    RepairRate     += RepairRate * mod.RepairBonus;
                    CargoSpaceMax += CargoSpaceMax * mod.CargoBonus;
                    SensorRange    += SensorRange * mod.SensorBonus;
                    WarpThrust     += WarpThrust * mod.SpeedBonus;
                    Thrust         += Thrust * mod.SpeedBonus;
                }
            }
            
        }
        public bool IsTethered()
        {
            return TetheredTo != null;
        }

        //added by Gremlin : active ship strength calculator
        public float GetStrength()
        {            
            if (Health >= HealthMax * 0.75f && !LowHealth && BaseStrength != -1)
                return BaseStrength;
            float strength = 0f;
            float defense = 0f;
            LowHealth = !(Health >= HealthMax * 0.75f);

            int slotCount = ModuleSlotList.Length;

            bool fighters = false;
            bool weapons = false;

            //Parallel.ForEach(this.ModuleSlotList, slot =>  //
            foreach (ShipModule slot in ModuleSlotList)
            {
#if DEBUG

                //if( this.BaseStrength ==0 && (this.Weapons.Count >0 ))
                    //Log.Info("No base strength: " + this.Name +" datastrength: " +this.shipData.BaseStrength);

#endif
                if ((BaseStrength == -1 ||( slot.Powered && slot.Active )))
                {
                    if (slot.InstalledWeapon != null)
                    {
                        weapons = true;
                        float offRate = 0;
                        Weapon w = slot.InstalledWeapon;
                        float damageAmount = w.DamageAmount + w.EMPDamage + w.PowerDamage + w.MassDamage;
                        if (!w.explodes)
                        {
                            offRate += (!w.isBeam ? (damageAmount * w.SalvoCount) * (1f / w.fireDelay) : damageAmount * 18f);
                        }
                        else
                        {

                            offRate += (damageAmount * w.SalvoCount) * (1f / w.fireDelay) * 0.75f;

                        }
                        if (offRate > 0 && w.TruePD || w.Range < 1000)
                        {
                            float range = 0f;
                            if (w.Range < 1000)
                            {
                                range = (1000f - w.Range) * .01f;
                            }
                            offRate /= (2 + range);
                        }
                        //if (w.EMPDamage > 0) offRate += w.EMPDamage * (1f / w.fireDelay) * .2f;
                        strength += offRate;
                    }

                    if (slot.hangarShipUID != null && !slot.IsSupplyBay)
                    {
                        if(slot.IsTroopBay)
                        {
                            strength += 50;
                            continue;
                        }
                        fighters = true;
                        if (ResourceManager.ShipsDict.TryGetValue(slot.hangarShipUID, out Ship hangarship))
                        {
                            strength += hangarship.BaseStrength;
                        }
                        else strength += 300;
                    }
                    defense += slot.ShieldPower * ((slot.shield_radius * 0.05f) / slotCount);
                    defense += slot.Health * ((slot.ModuleType == ShipModuleType.Armor ? (slot.XSIZE) : 1f) / (slotCount * 4));
                }
            }//);
            if (!fighters && !weapons) strength = 0;
            if (defense > strength) defense = strength;
            //the base strength should be the ships strength at full health. 
            //this.BaseStrength = Str + def;
            return strength + defense;
        }


        public float GetDPS()
        {
            float num = 0.0f;
            foreach (Weapon weapon in Weapons)
                num += weapon.DamageAmount * (1f / weapon.fireDelay);
            return num;
        }

        //Added by McShooterz: add experience for cruisers and stations, modified for dynamic system
        public void AddKill(Ship killed)
        {
            ++kills;
            if (loyalty == null)
                return;
        
            //Added by McShooterz: change level cap, dynamic experience required per level
            float Exp = 1;
            float ExpLevel = 1;
            bool ExpFound = false;
            float ReqExp = 1;
            if (ResourceManager.ShipRoles.ContainsKey(killed.shipData.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[killed.shipData.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[killed.shipData.Role].RaceList[i].ShipType == killed.loyalty.data.Traits.ShipType)
                    {
                        Exp = ResourceManager.ShipRoles[killed.shipData.Role].RaceList[i].KillExp;
                        ExpLevel = ResourceManager.ShipRoles[killed.shipData.Role].RaceList[i].KillExpPerLevel;
                        ExpFound = true;
                        break;
                    }
                }
                if(!ExpFound)
                {
                    Exp = ResourceManager.ShipRoles[killed.shipData.Role].KillExp;
                    ExpLevel = ResourceManager.ShipRoles[killed.shipData.Role].KillExpPerLevel;
                }
            }
            Exp = (Exp + (ExpLevel * killed.Level));
            Exp += Exp * loyalty.data.ExperienceMod;
            experience += Exp;
            ExpFound = false;
            //Added by McShooterz: a way to prevent remnant story in mods

            Empire remnant = EmpireManager.Remnants;  //Changed by Gretman, because this was preventing any "RemnantKills" from getting counted, thus no remnant event.
            //if (this.loyalty == EmpireManager.Player && killed.loyalty == remnant && this.shipData.ShipStyle == remnant.data.Traits.ShipType &&  (GlobalStats.ActiveModInfo == null || (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.removeRemnantStory)))
            if (loyalty == EmpireManager.Player && killed.loyalty == remnant &&  (GlobalStats.ActiveModInfo == null || (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.removeRemnantStory)))
                //GlobalStats.IncrementRemnantKills((int)Exp);
                GlobalStats.IncrementRemnantKills(1);   //I also changed this because the exp before was a lot, killing almost any remnant ship would unlock the remnant event immediately

            if (ResourceManager.ShipRoles.ContainsKey(shipData.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[shipData.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[shipData.Role].RaceList[i].ShipType == loyalty.data.Traits.ShipType)
                    {
                        ReqExp = ResourceManager.ShipRoles[shipData.Role].RaceList[i].ExpPerLevel;
                        ExpFound = true;
                        break;
                    }
                }
                if (!ExpFound)
                {
                    ReqExp = ResourceManager.ShipRoles[shipData.Role].ExpPerLevel;
                }
            }
            while (experience > ReqExp * (1 + Level))
            {
                experience -= ReqExp * (1 + Level);
                ++Level;
            }
            if (Level > 255)
                Level = 255;
            if (!loyalty.TryGetRelations(killed.loyalty, out Relationship rel) || !rel.AtWar)
                return;
            loyalty.GetRelations(killed.loyalty).ActiveWar.StrengthKilled += killed.BaseStrength;
            killed.loyalty.GetRelations(loyalty).ActiveWar.StrengthLost += killed.BaseStrength;
        }

        private void ExplodeShip(float explodeRadius, bool useWarpExplodeEffect)
        {
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
                dying = true;
                xdie = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                ydie = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                zdie = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                dietimer = UniverseRandom.RandomBetween(4f, 6f);
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

            if (Active)
            {
                Active = false;
                switch (shipData.Role)
                {
                    case ShipData.RoleName.freighter:   ExplodeShip(500f, cleanupOnly); break;
                    case ShipData.RoleName.platform:    ExplodeShip(500f, cleanupOnly); break;
                    case ShipData.RoleName.fighter:     ExplodeShip(600f, cleanupOnly); break;
                    case ShipData.RoleName.frigate:     ExplodeShip(1000f,cleanupOnly); break;
                    case ShipData.RoleName.capital:     ExplodeShip(1200f, true);       break;
                    case ShipData.RoleName.carrier:     ExplodeShip(900f, true);        break;
                    case ShipData.RoleName.cruiser:     ExplodeShip(850f, true);        break;
                    case ShipData.RoleName.station:     ExplodeShip(1200f, true);       break;
                    default:                            ExplodeShip(600f, cleanupOnly); break;
                }
                System?.spatialManager.ShipExplode(this, Size * 50, Center, Radius);

                if (!HasExploded)
                {
                    HasExploded = true;

                    // Added by RedFox - spawn flaming spacejunk when a ship dies
                    int explosionJunk = (int)RandomMath.RandomBetween(Radius * 0.08f, Radius * 0.12f);
                    float radSqrt     = (float)Math.Sqrt(Radius);
                    float junkScale   = radSqrt * 0.05f; // trial and error, depends on junk model sizes
                    if (junkScale > 1.4f) junkScale = 1.4f; // bigger doesn't look good

                    //Log.Info("Ship.Explode r={1} rsq={2} junk={3} scale={4}   {0}", Name, Radius, radSqrt, explosionJunk, junkScale);
                    SpaceJunk.SpawnJunk(explosionJunk, Center, System, this, Radius/4, junkScale);
                }
            }
            var hullData = shipData.HullData;
            if (hullData?.EventOnDeath != null)
            {
                var evt = ResourceManager.EventsDict[hullData.EventOnDeath];
                Empire.Universe.ScreenManager.AddScreen(new EventPopup(Empire.Universe, EmpireManager.Player, evt, evt.PotentialOutcomes[0], true));
            }
            QueueTotalRemoval();
        }

        public void QueueTotalRemoval()
        {
            SetSystem(null);
            Empire.Universe.ShipsToRemove.Add(this);
        }

        public void TotallyRemove()
        {
            Active            = false;
            AI.Target         = null;
            AI.TargetShip     = null;
            AI.ColonizeTarget = null;
            AI.EscortTarget   = null;
            AI.start = null;
            AI.end   = null;
            AI.PotentialTargets.Clear();
            AI.TrackProjectiles.Clear();
            AI.NearbyShips.Clear();
            AI.FriendliesNearby.Clear();
            Empire.Universe.MasterShipList.QueuePendingRemoval(this);
            AttackerTargetting.Clear();
            if (Empire.Universe.SelectedShip == this)
                Empire.Universe.SelectedShip = null;
            Empire.Universe.SelectedShipList.Remove(this);

            if (Mothership != null)
            {
                foreach (ShipModule shipModule in Mothership.Hangars)
                    if (shipModule.GetHangarShip() == this)
                        shipModule.SetHangarShip(null);
            }
            foreach (ShipModule hanger in Hangars)
            {
                if (hanger.GetHangarShip() != null)
                    hanger.GetHangarShip().Mothership = null;
            }
            foreach(Empire empire in EmpireManager.Empires)
            {
                empire.GetGSAI().ThreatMatrix.RemovePin(this);
            }

            foreach (Projectile projectile in projectiles)
                projectile.Die(this, false);
            projectiles.Clear();

            ModuleSlotList     = Empty<ShipModule>.Array;
            SparseModuleGrid   = Empty<ShipModule>.Array;
            ExternalModuleGrid = Empty<ShipModule>.Array;
            NumExternalSlots = 0;
            Shields = Empty<ShipModule>.Array;

            Hangars.Clear();
            BombBays.Clear();
            TroopList.Clear();
            ClearFleet();
            ShipSO.Clear();
            lock (GlobalStats.ObjectManagerLocker)
                Empire.Universe.ScreenManager.inter.ObjectManager.Remove(ShipSO);

            loyalty.RemoveShip(this);
            SetSystem(null);
            TetheredTo = null;
            Transporters.Clear();
            RepairBeams.Clear();
        }

        public void ClearFleet() => fleet?.RemoveShip(this);
        

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

        public void RecalculateMaxHP()          //Added so ships would get the benefit of +HP mods from research and/or artifacts.   -Gretman
        {
            if (VanityName == "MerCraft") Log.Info("Health was " + Health + " / " + HealthMax + "   (" + loyalty.data.Traits.ModHpModifier + ")");
            HealthMax = 0;
            foreach (ShipModule slot in ModuleSlotList)
            {
                bool isFullyHealed = slot.Health >= slot.HealthMax;
                slot.HealthMax = ResourceManager.GetModuleTemplate(slot.UID).HealthMax;
                slot.HealthMax = slot.HealthMax + slot.HealthMax * loyalty.data.Traits.ModHpModifier;
                if (isFullyHealed)
                {
                    // Basically, set maxhealth to what it would be with no modifier, then
                    // apply the total benefit to it. Next, if the module is fully healed,
                    // adjust its HP so it is still fully healed. Also calculate and adjust
                    slot.Health = slot.HealthMax;
                }
                //the ships MaxHP so it will display properly.        -Gretman
                HealthMax += slot.HealthMax;
            }
            if (Health >= HealthMax) Health = HealthMax;
            if (VanityName == "MerCraft") Log.Info("Health is  " + Health + " / " + HealthMax);
        }

        public override string ToString() => $"Ship Id={Id} '{VanityName}' Pos {Position}";
    }
}
