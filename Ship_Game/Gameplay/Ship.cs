// Type: Ship_Game.Gameplay.Ship
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using System.Collections.Concurrent;

namespace Ship_Game.Gameplay
{
    public class Ship : GameplayObject, IDisposable
    {
        public string VanityName = "";
        public List<Troop> TroopList = new List<Troop>();
        public List<Rectangle> AreaOfOperation = new List<Rectangle>();
        public bool RecallFightersBeforeFTL = true;
        private Dictionary<Vector2, ModuleSlot> ModulesDictionary = new Dictionary<Vector2, ModuleSlot>();
        //public float DefaultFTLSpeed = 1000f;    //Not referenced in code, removing to save memory
        public float RepairRate = 1f;
        public float SensorRange = 20000f;
        public float yBankAmount = 0.007f;
        public float maxBank = 0.5235988f;
        private Dictionary<string, float> CargoDict = new Dictionary<string, float>();
        private Dictionary<string, float> MaxGoodStorageDict = new Dictionary<string, float>();
        private Dictionary<string, float> ResourceDrawDict = new Dictionary<string, float>();
        public Vector2 projectedPosition = new Vector2();
        protected List<Thruster> ThrusterList = new List<Thruster>();
        public bool TradingFood = true;
        public bool TradingProd = true;
        public bool ShieldsUp = true;
        //public float AfterBurnerAmount = 20.5f;    //Not referenced in code, removing to save memory
        //protected Color CloakColor = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);    //Not referenced in code, removing to save memory
        //public float CloakTime = 5f;    //Not referenced in code, removing to save memory
        //public Vector2 Origin = new Vector2(256f, 256f);        //Not referenced in code, removing to save memory
        public List<ModuleSlot> ModuleSlotList = new List<ModuleSlot>();
        private BatchRemovalCollection<Projectile> projectiles = new BatchRemovalCollection<Projectile>();
        private BatchRemovalCollection<Beam> beams = new BatchRemovalCollection<Beam>();
        public List<Weapon> Weapons = new List<Weapon>();
        //public float fireThresholdSquared = 0.25f;    //Not referenced in code, removing to save memory
        public List<ModuleSlot> ExternalSlots = new List<ModuleSlot>();
        protected float JumpTimer = 3f;
        public BatchRemovalCollection<ProjectileTracker> ProjectilesFired = new BatchRemovalCollection<ProjectileTracker>();
        public AudioEmitter emitter = new AudioEmitter();
        public float ClickTimer = 10f;
        public Vector2 VelocityLast = new Vector2();
        public Vector2 ScreenPosition = new Vector2();
        public float ScuttleTimer = -1f;
        public Vector2 FleetOffset = new Vector2();
        public Vector2 RelativeFleetOffset = new Vector2();
        private List<ShipModule> Shields = new List<ShipModule>();
        private List<ShipModule> Hangars = new List<ShipModule>();
        public List<ShipModule> BombBays = new List<ShipModule>();
        public bool shipStatusChanged = false;
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
        public float ECMValue = 0f;
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
        protected SceneObject ShipSO;
        public bool ManualHangarOverride;
        public Fleet.FleetCombatStatus FleetCombatStatus;
        public Ship Mothership;
        public string ModelPath;
        public bool isThrusting;
        public float CargoSpace_Max;
        public float WarpDraw;
        public string Name;
        public float DamageModifier;
        public Empire loyalty;
        public int Size;
        //public int CrewRequired;    //Not referenced in code, removing to save memory
        //public int CrewSupplied;    //Not referenced in code, removing to save memory
        public float Ordinance;
        public float OrdinanceMax;
        //public float scale;    //Not referenced in code, removing to save memory
        protected ArtificialIntelligence AI;
        public float speed;
        public float Thrust;
        public float velocityMaximum;
        //public double armor_percent;    //Not referenced in code, removing to save memory
        public double shield_percent;
        public float armor_max;
        public float shield_max;
        public float shield_power;
        public float number_Internal_slots;
        public float number_Alive_Internal_slots;
        public float PowerCurrent;
        public float PowerFlowMax;
        public float PowerStoreMax;
        public float PowerDraw;
        public float ModulePowerDraw;
        public float ShieldPowerDraw;
        public float rotationRadiansPerSecond;
        public bool FromSave;
        public bool HasRepairModule;
        private Cue Afterburner;
        public bool isSpooling;
        //protected SolarSystem JumpTarget;   //Not referenced in code, removing to save memory
        //protected Cue hyperspace;           //Removed to save space, because this is set to null in ship initilizer, and never reassigned. -Gretman
        //protected Cue hyperspace_return;    //Not referenced in code, removing to save memory
        private Cue Jump;
        public float InhibitedTimer;
        public int Level;
        public bool PlayerShip;
        public float HealthMax;
        public float ShipMass;
        public int TroopCapacity;
        public float OrdAddedPerSecond;
        public bool HasTroopBay;
        public bool ModulesInitialized;
        //public bool WeaponCentered;    //Not referenced in code, removing to save memory
        protected Cue drone;
        public float ShieldRechargeTimer;
        public bool InCombat;
        private Vector3 pointat;
        private Vector3 scalefactors;
        public float xRotation;
        public Ship.MoveState engineState;
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
        public float percent;
        private float xdie;
        private float ydie;
        private float zdie;
        private float dietimer;
        public float BaseStrength;
        public bool BaseCanWarp;
        public bool dying;
        private bool reallyDie;
        private bool HasExploded;
        public static UniverseScreen universeScreen;
        public float FTLSpoolTime;
        public bool FTLSlowTurnBoost;

        //public Dictionary<Empire, diplomacticSpace> BorderState = new Dictionary<Empire, diplomacticSpace>();
        public List<ShipModule> Transporters = new List<ShipModule>();
        public List<ShipModule> RepairBeams = new List<ShipModule>();
        public bool hasTransporter;
        public bool hasOrdnanceTransporter;
        public bool hasAssaultTransporter;
        public bool hasRepairBeam;
        public bool hasCommand;
        private float FTLmodifier = 1f;

        public float RangeForOverlay;
        public ReaderWriterLockSlim supplyLock = new ReaderWriterLockSlim();
        //Random shiprandom = new Random();    //Not referenced in code, removing to save memory
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;
        List<ModuleSlot> AttackerTargetting = new List<ModuleSlot>();
        public sbyte TrackingPower = 0;
        public sbyte FixedTrackingPower = 0;

        //public ushort purgeCount =0;    //Not referenced in code, removing to save memory
        public Ship lastAttacker = null;
        private bool LowHealth = false; //fbedard: recalculate strength after repair
        public float TradeTimer;
        public bool shipInitialized = false;
        public float maxFTLSpeed;
        public float maxSTLSpeed;
        public float NormalWarpThrust;
        private BatchRemovalCollection<Empire> BorderCheck = new BatchRemovalCollection<Empire>();
        public BatchRemovalCollection<Empire> getBorderCheck
        {
            get {
                return BorderCheck; }
            set {
                BorderCheck = value; }
        }
        public bool IsInNeutralSpace
        {
            get
            {                
                foreach (Empire e in BorderCheck)
                {
                    
                    Relationship rel = loyalty.GetRelations(e);
                    if (rel.AtWar || rel.Treaty_Alliance || e == this.loyalty)
                    {
                        return false;
                    }
                    
                }

                return true; 
            }
        }
        public bool IsInFriendlySpace
        {
            get
            {
                foreach (Empire e in BorderCheck)
                {
                    if (e == loyalty)
                        return true;
                    Relationship rel = loyalty.GetRelations(e);
                    if (rel.Treaty_Alliance )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        
        public bool IsIndangerousSpace
        {
            get
            {
                foreach(Empire e in BorderCheck)
                {
                    Relationship rel = loyalty.GetRelations(e);
                    if (rel.AtWar )
                    {
                        return true;
                    }
                }

                return false;
            }            
        }


        public float CargoSpace_Used
        {
            get
            {
                float num = 0.0f;
                if (this.CargoDict.ContainsKey("Food"))
                    num += this.CargoDict["Food"];
                if (this.CargoDict.ContainsKey("Production"))
                    num += this.CargoDict["Production"];
                if (this.CargoDict.ContainsKey("Colonists_1000"))
                    num += this.CargoDict["Colonists_1000"];
                return num;
            }
            set
            {
            }
        }
        public void CargoClear()
        {
            List<string> keys = new List<string>(CargoDict.Keys);
            foreach (string cargo in keys)
            {
                CargoDict[cargo] = 0;
            }

        }
        public float GetFTLmodifier
        {
            get
            {
                return this.FTLmodifier;
            }
        }
        public BatchRemovalCollection<Projectile> Projectiles
        {
            get
            {
                return this.projectiles;
            }
        }

        public BatchRemovalCollection<Beam> Beams
        {
            get
            {
                return this.beams;
            }
        }
        public bool needResupplyOrdnance
        {
            get
            {
                if (this.OrdinanceMax > 0f && this.Ordinance / this.OrdinanceMax < 0.05f && !this.GetAI().hasPriorityTarget)//this.Owner.loyalty != ArtificialIntelligence.universeScreen.player)
                {
                    if (this.GetAI().FriendliesNearby.Where(supply => supply.HasSupplyBays && supply.Ordinance >= 100).Count() == 0)
                    {
                        return true;
                    }
                    else return false;
                }
                return false;
            }

        }
        public bool NeedResupplyTroops
        {
            get
            {
                try
                {
                    byte assaultSpots = 0;
                    if (this.Hangars.Count > 0)
                        foreach (ShipModule sm in this.Hangars)
                        {
                            if (sm.IsTroopBay)
                                assaultSpots++;
                        }
                    if (this.Transporters.Count > 0)
                        foreach (ShipModule at in this.Transporters)
                        {
                            assaultSpots += at.TransporterTroopLanding;
                        }
                    byte troops = 0;
                    if (this.TroopList.Count > 0)
                        foreach (Troop troop in this.TroopList)
                        {
                            troops++;
                            if (troops >= assaultSpots)
                                break;
                        }
                    return assaultSpots == 0 ? false : troops / (float)assaultSpots < .5f ? true : false;
                }
                catch { }
                return false;
            }
        }
        public byte ReadyPlanetAssaulttTroops
        {
            get
            {
                try
                {
                    byte assaultSpots = 0;
                    if (this.Hangars.Count > 0)
                        foreach (ShipModule sm in this.Hangars)
                        {
                            if (sm.hangarTimer < 0)
                                continue;
                            if (sm.IsTroopBay)
                                assaultSpots++;
                        }
                    if (this.Transporters.Count > 0)
                        foreach (ShipModule at in this.Transporters)
                        {
                            if (at.TransporterTimer > 0)
                                continue;
                            assaultSpots += at.TransporterTroopLanding;
                        }
                    byte troops = 0;
                    if (this.TroopList.Count > 0)
                        foreach (Troop troop in this.TroopList)
                        {
                            troops++;
                            if (troops >= assaultSpots)
                                break;
                        }

                    return troops;
                }
                catch
                { }
                return 0;


            }
        }
        public float PlanetAssaultStrength
        {
            get
            {
                try
                {
                    float assaultSpots = 0;
                    float assaultStrength = 0;
                    if (this.shipData.Role == ShipData.RoleName.troop)
                    {
                        assaultSpots += this.TroopList.Count;

                    }
                    if (this.Hangars.Count > 0)
                        foreach (ShipModule sm in this.Hangars)
                        {
                            //if (sm.hangarTimer > 0)
                            //    continue;
                            if (sm.IsTroopBay)
                                assaultSpots++;
                        }
                    if (this.Transporters.Count > 0)
                        foreach (ShipModule at in this.Transporters)
                        {
                            //if (at.TransporterTimer > 0)
                            //    continue;
                            assaultSpots += at.TransporterTroopLanding;
                        }
                    byte troops = 0;
                    if (this.TroopList.Count > 0)
                        foreach (Troop troop in this.TroopList)
                        {
                            troops++;
                            assaultStrength += troop.Strength;
                            if (troops >= assaultSpots)
                                break;
                        }

                    return assaultStrength;
                }
                catch
                { }
                return 0;


            }
        }
        public int PlanetAssaultCount
        {
            get
            {
                try
                {
                    int assaultSpots = 0;
                    if (this.shipData.Role == ShipData.RoleName.troop)
                    {
                        assaultSpots += this.TroopList.Count;

                    }
                    if (this.HasTroopBay)
                        foreach (ShipModule sm in this.Hangars)
                        {
                            if (sm.IsTroopBay)
                                assaultSpots++;
                        }
                    if (this.hasAssaultTransporter) //  this.Transporters.Count > 0)
                        foreach (ShipModule at in this.Transporters)
                        {
                            assaultSpots += at.TransporterTroopLanding;
                        }

                    if (assaultSpots > 0)
                    {
                        int temp = assaultSpots - this.TroopList.Count;
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
                if (this.Hangars.Count > 0)
                    try
                    {
                        foreach (ShipModule shipModule in this.Hangars)
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
                if (this.BombBays.Count > 0)
                {
                    ++Bombs;
                    if (this.Ordinance / this.OrdinanceMax > 0.2f)
                    {
                        Bombs += this.BombBays.Count;
                    }
                }
                return Bombs;
            }

        }
        public bool Resupplying
        {
            get
            {
                return this.AI.State == AIState.Resupply;
            }
            set
            {
                this.AI.OrderResupplyNearest(true);
            }
        }

        public bool FightersOut
        {
            get
            {
                bool flag = false;
                if (this.Hangars.Count <= 0)
                    return false;
                for (int index = 0; index < this.Hangars.Count; ++index)
                {
                    try
                    {
                        ShipModule shipModule = this.Hangars[index];
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
                this.fightersOut = value;
                if (this.fightersOut && this.engineState != Ship.MoveState.Warp)
                    this.ScrambleFighters();
                else
                    this.RecoverFighters();
            }
        }

        public bool DoingTransport
        {
            get
            {
                return this.AI.State == AIState.SystemTrader;
            }
            set
            {
                this.GetAI().start = null;
                this.GetAI().end = null;
                this.GetAI().OrderTrade(5f);
            }
        }

        public bool DoingPassTransport
        {
            get
            {
                return this.AI.State == AIState.PassengerTransport;
            }
            set
            {
                this.GetAI().start = null;
                this.GetAI().end = null;
                this.GetAI().OrderTransportPassengers(5f);
            }
        }

        public bool TransportingFood
        {
            get
            {
                return this.TradingFood;
            }
            set
            {
                this.TradingFood = value;
            }
        }

        public bool TransportingProduction
        {
            get
            {
                return this.TradingProd;
            }
            set
            {
                this.TradingProd = value;
            }
        }

        public bool DoingExplore
        {
            get
            {
                return this.AI.State == AIState.Explore;
            }
            set
            {
                this.GetAI().OrderExplore();
            }
        }

        public bool DoingResupply
        {
            get
            {
                return this.AI.State == AIState.Resupply;
            }
            set
            {
                this.GetAI().OrderResupplyNearest(true);
            }
        }

        public bool DoingSystemDefense
        {
            get
            {
                return this.AI.State == AIState.SystemDefender;
            }
            set
            {
                //added by gremlin Toggle Ship System Defense.


                if (EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this))
                {
                    EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetGSAI().DefensiveCoordinator.remove(this);
                    this.GetAI().OrderQueue.Clear();
                    this.GetAI().HasPriorityOrder = false;
                    this.GetAI().SystemToDefend = (SolarSystem)null;
                    this.GetAI().SystemToDefendGuid = Guid.Empty;
                    this.GetAI().State = AIState.AwaitingOrders;

                    return;
                }

                EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(this);
                this.GetAI().OrderQueue.Clear();
                this.GetAI().HasPriorityOrder = false;
                this.GetAI().SystemToDefend = (SolarSystem)null;
                this.GetAI().SystemToDefendGuid = Guid.Empty;
                this.GetAI().State = AIState.SystemDefender;
            }
        }

        public bool TroopsOut
        {
            get
            {
                return this.troopsOut;
            }
            set
            {
                this.troopsOut = value;
                if (this.troopsOut)
                    this.ScrambleAssaultShips(0);
                else
                    this.RecoverAssaultShips();
            }
        }

        public bool doingScrap
        {
            get
            {
                return this.AI.State == AIState.Scrap;
            }
            set
            {
                this.GetAI().OrderScrapShip();
            }
        }

        public bool doingRefit
        {
            get
            {
                return this.AI.State == AIState.Refit;
            }
            set
            {
                //this.GetAI().OrderScrapShip();
                Ship.universeScreen.ScreenManager.AddScreen((GameScreen)new RefitToWindow(this));
            }
        }

        public Ship()
        {
            foreach (KeyValuePair<string, Good> keyValuePair in Ship_Game.ResourceManager.GoodsDict)
            {
                this.AddGood(keyValuePair.Key, 0);
                if (!keyValuePair.Value.IsCargo)
                {
                    this.MaxGoodStorageDict.Add(keyValuePair.Key, 0.0f);
                    this.ResourceDrawDict.Add(keyValuePair.Key, 0.0f);
                }
            }
        }
        public void ShipRecreate()
        {
            this.Active = false;
            this.AI.Target = (GameplayObject)null;
            this.AI.ColonizeTarget = (Planet)null;
            this.AI.EscortTarget = (Ship)null;
            this.AI.start = (Planet)null;
            this.AI.end = (Planet)null;
            this.AI.PotentialTargets.Clear();
            this.AI.NearbyShips.Clear();
            this.AI.FriendliesNearby.Clear();


            if (this.System != null)
            {
                this.System.ShipList.QueuePendingRemoval(this);
                this.System.spatialManager.CollidableObjects.Remove((GameplayObject)this);
            }
            if (this.Mothership != null)
            {
                foreach (ShipModule shipModule in this.Mothership.Hangars)
                {
                    if (shipModule.GetHangarShip() == this)
                        shipModule.SetHangarShip((Ship)null);
                }
            }
            else if (this.isInDeepSpace)
                UniverseScreen.DeepSpaceManager.CollidableObjects.Remove((GameplayObject)this);
            for (int index = 0; index < this.projectiles.Count; ++index)
                this.projectiles[index].Die((GameplayObject)this, false);
            this.projectiles.Clear();

            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                moduleSlot.module.Clear();
            this.ModuleSlotList.Clear();
            this.TroopList.Clear();
            this.RemoveFromAllFleets();
            this.ShipSO.Clear();

            this.loyalty.RemoveShip(this);
            this.System = (SolarSystem)null;
            this.TetheredTo = (Planet)null;
        }

        public Ship(Vector2 pos, Vector2 dim, float rot)
        {
            this.Position = pos;
            this.Rotation = rot;
            this.Dimensions = dim;
        }

        public void SetAnimationController(AnimationController ac, SkinnedModel model)
        {
            this.animationController = ac;
            this.animationController.StartClip(model.AnimationClips["Take 001"]);
        }

        //added by gremlin The Generals GetFTL speed
        public void SetmaxFTLSpeed()
        {
            //Added by McShooterz: hull bonus speed 

            if (InhibitedTimer < -.25f || this.Inhibited ||  this.System != null && this.engineState == MoveState.Warp)
            {
                if (Ship.universeScreen.GravityWells && this.System != null && !this.IsInFriendlySpace)
                {
                    foreach (Planet planet in this.System.PlanetList)
                    {
                        if (Vector2.Distance(this.Position, planet.Position) < (GlobalStats.GravityWellRange * (1 + ((Math.Log(planet.scale)) / 1.5))))
                        {
                            this.InhibitedTimer = .3f;
                            break;
                        }
                    }
                }
                if (this.InhibitedTimer < 0)
                    this.InhibitedTimer = 0.0f;
            }
            //Apply in borders bonus through ftl modifier
            float ftlmodtemp = 1;

            //Change FTL modifier for ship based on solar system
            {
                if (this.System != null) // && ( || ))
                {
                    if (this.IsInFriendlySpace) // && Ship.universeScreen.FTLModifier < 1)
                        ftlmodtemp = Ship.universeScreen.FTLModifier;
                    else if (this.IsIndangerousSpace || !Ship.universeScreen.FTLInNuetralSystems) // && Ship.universeScreen.EnemyFTLModifier < 1)
                    {
                        ftlmodtemp = Ship.universeScreen.EnemyFTLModifier;
                    }

                }
            }
            this.FTLmodifier = 1;
            if (this.inborders && this.loyalty.data.Traits.InBordersSpeedBonus > 0)
                this.FTLmodifier += this.loyalty.data.Traits.InBordersSpeedBonus;
            this.FTLmodifier *= ftlmodtemp;
            this.maxFTLSpeed = (this.WarpThrust / base.Mass + this.WarpThrust / base.Mass * this.loyalty.data.FTLModifier) * this.FTLmodifier;


        }
        public float GetmaxFTLSpeed { get { return maxFTLSpeed; } }

    	

        public float GetSTLSpeed()
        {
            //Added by McShooterz: hull bonus speed
            float speed=  this.Thrust / this.Mass + this.Thrust / this.Mass * this.loyalty.data.SubLightModifier;
            return speed > 2500f ? 2500 : speed;
        }

        public Dictionary<Vector2, ModuleSlot> GetMD()
        {
            return this.ModulesDictionary;
        }

        public void TetherToPlanet(Planet p)
        {
            this.TetheredTo = p;
            this.TetherOffset = this.Center - p.Position;
        }

        public Planet GetTether()
        {
            return this.TetheredTo;
        }

        public Ship SoftCopy()
        {
            return new Ship()
            {
                shipData = this.shipData,
                FleetOffset = this.FleetOffset,
                RelativeFleetOffset = this.RelativeFleetOffset,
                guid = this.guid,
                projectedPosition = this.projectedPosition
            };
        }

        public Ship Clone()
        {
            return (Ship)MemberwiseClone();
        }

        public float GetCost(Empire e)
        {
            if (this.shipData.HasFixedCost)
                return (float)this.shipData.FixedCost;
            float num = 0.0f;
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                num += moduleSlot.module.Cost * UniverseScreen.GamePaceStatic;
            if (e != null)
            {
                //Added by McShooterz: hull bonus starting cost
				num += (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.shipData.Hull) ? ResourceManager.HullBonuses[this.shipData.Hull].StartingCost : 0);
                num += num * e.data.Traits.ShipCostMod;
				return (float)(int)(num * (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.shipData.Hull) ? 1f - ResourceManager.HullBonuses[this.shipData.Hull].CostBonus : 1));
            }
            else
                return (float)(int)num;
        }

        public ShipData GetShipData()
        {
            if (ResourceManager.ShipsDict.TryGetValue(this.Name, out Ship sd))
                return sd.shipData;            
            else
                return (ShipData)null;
        }

        public void SetShipData(ShipData data)
        {
            this.shipData = data;
        }

        public void Explore()
        {
            this.AI.State = AIState.Explore;
            this.AI.HasPriorityOrder = true;
        }

        public void AttackShip(Ship target)
        {
            this.AI.State = AIState.AttackTarget;
            this.AI.Target = (GameplayObject)target;
            this.AI.HasPriorityOrder = false;
            this.AI.hasPriorityTarget = true;
            this.InCombatTimer = 15f;
        }

        public Dictionary<string, float> GetCargo()
        {
            return this.CargoDict;
        }

        public Dictionary<string, float> GetResDrawDict()
        {
            return this.ResourceDrawDict;
        }

        public Dictionary<string, float> GetMaxGoods()
        {
            return this.MaxGoodStorageDict;
        }

        public void AddGood(string UID, int Amount)
        {
            //Log.Info("AddGood {0}: {1}", UID, Amount);
            if (this.CargoDict.ContainsKey(UID))
            {
                Dictionary<string, float> dictionary;
                string index;
                (dictionary = this.CargoDict)[index = UID] = dictionary[index] + (float)Amount;
            }
            else
                this.CargoDict.Add(UID, (float)Amount);
        }

        public void ProcessInput(float elapsedTime)
        {
            if (GlobalStats.TakingInput || this.disabled || !this.hasCommand)
                return;
            if (Ship.universeScreen.input != null)
                this.currentKeyBoardState = Ship.universeScreen.input.CurrentKeyboardState;
            if (this.currentKeyBoardState.IsKeyDown(Keys.D))
                this.AI.State = AIState.ManualControl;
            if (this.currentKeyBoardState.IsKeyDown(Keys.A))
                this.AI.State = AIState.ManualControl;
            if (this.currentKeyBoardState.IsKeyDown(Keys.W))
                this.AI.State = AIState.ManualControl;
            if (this.currentKeyBoardState.IsKeyDown(Keys.S))
                this.AI.State = AIState.ManualControl;
            if (this.AI.State == AIState.ManualControl)
            {
                if (this.Active && !this.currentKeyBoardState.IsKeyDown(Keys.LeftControl))
                {
                    this.isThrusting = false;
                    Vector2 vector2_1 = new Vector2((float)Math.Sin((double)this.Rotation), -(float)Math.Cos((double)this.Rotation));
                    Vector2 vector2_2 = new Vector2(-vector2_1.Y, vector2_1.X);
                    if (this.currentKeyBoardState.IsKeyDown(Keys.D))
                    {
                        this.isThrusting = true;
                        this.RotationalVelocity += this.rotationRadiansPerSecond * elapsedTime;
                        this.isTurning = true;
                        if (this.RotationalVelocity > this.rotationRadiansPerSecond)
                            this.RotationalVelocity = this.rotationRadiansPerSecond;
                        if (this.yRotation > -this.maxBank)
                            this.yRotation -= this.yBankAmount;
                    }
                    else if (this.currentKeyBoardState.IsKeyDown(Keys.A))
                    {
                        this.isThrusting = true;
                        this.RotationalVelocity -= this.rotationRadiansPerSecond * elapsedTime;
                        this.isTurning = true;
                        if (Math.Abs(this.RotationalVelocity) > this.rotationRadiansPerSecond)
                            this.RotationalVelocity = -this.rotationRadiansPerSecond;
                        if (this.yRotation < this.maxBank)
                            this.yRotation += this.yBankAmount;
                    }
                    else if (this.engineState == Ship.MoveState.Warp)
                    {
                        this.isSpooling = true;
                        this.isTurning = false;
                        this.isThrusting = true;
                        Vector2.Normalize(vector2_1);
                        Ship ship1 = this;
                        Vector2 vector2_3 = ship1.Velocity + vector2_1 * (elapsedTime * this.speed);
                        ship1.Velocity = vector2_3;
                        if (this.Velocity.Length() > this.velocityMaximum)
                            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                        if (this.Velocity.LengthSquared() <= 0.0)
                            this.Velocity = Vector2.Zero;
                        if (this.yRotation > 0.0)
                            this.yRotation -= this.yBankAmount;
                        else if (this.yRotation < 0.0)
                            this.yRotation += this.yBankAmount;
                        if (this.RotationalVelocity > 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity -= this.rotationRadiansPerSecond * elapsedTime;
                            if (this.RotationalVelocity < 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                        else if (this.RotationalVelocity < 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity += this.rotationRadiansPerSecond * elapsedTime;
                            if (this.RotationalVelocity > 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                    }
                    else
                    {
                        this.isTurning = false;
                        if (this.yRotation > 0.0)
                        {
                            this.yRotation -= this.yBankAmount;
                            if (this.yRotation < 0.0)
                                this.yRotation = 0.0f;
                        }
                        else if (this.yRotation < 0.0)
                        {
                            this.yRotation += this.yBankAmount;
                            if (this.yRotation > 0.0)
                                this.yRotation = 0.0f;
                        }
                        if (this.RotationalVelocity > 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity -= this.rotationRadiansPerSecond * elapsedTime;
                            if (this.RotationalVelocity < 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                        else if (this.RotationalVelocity < 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity += this.rotationRadiansPerSecond * elapsedTime;
                            if (this.RotationalVelocity > 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                        this.isThrusting = false;
                    }
                    if (this.Velocity.Length() > this.velocityMaximum)
                        this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                    if (this.currentKeyBoardState.IsKeyDown(Keys.F) && !this.lastKBState.IsKeyDown(Keys.F))
                    {
                        if (!this.isSpooling)
                            this.EngageStarDrive();
                        else
                            this.HyperspaceReturn();
                    }
                    if (this.currentKeyBoardState.IsKeyDown(Keys.W))
                    {
                        this.isThrusting = true;
                        Ship ship = this;
                        Vector2 vector2_3 = ship.Velocity + vector2_1 * (elapsedTime * this.speed);
                        ship.Velocity = vector2_3;
                        if (this.Velocity.Length() > this.velocityMaximum)
                            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                    }
                    else if (this.currentKeyBoardState.IsKeyDown(Keys.S))
                    {
                        this.isThrusting = true;
                        Ship ship = this;
                        Vector2 vector2_3 = ship.Velocity - vector2_1 * (elapsedTime * this.speed);
                        ship.Velocity = vector2_3;
                        if (this.Velocity.Length() > this.velocityMaximum)
                            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                    }
                    MouseState state = Mouse.GetState();
                    if (state.RightButton == ButtonState.Pressed)
                    {
                        Vector3 position = Ship.universeScreen.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)state.X, (float)state.Y, 0.0f), Ship.universeScreen.projection, Ship.universeScreen.view, Matrix.Identity);
                        Vector3 direction = Ship.universeScreen.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)state.X, (float)state.Y, 1f), Ship.universeScreen.projection, Ship.universeScreen.view, Matrix.Identity) - position;
                        direction.Normalize();
                        Ray ray = new Ray(position, direction);
                        float num = -ray.Position.Z / ray.Direction.Z;
                        Vector3 PickedPos = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                        foreach (Weapon w in this.Weapons)
                        {
                            if (w.timeToNextFire <= 0.0 && w.moduleAttachedTo.Powered)
                            {
                                if (this.CheckIfInsideFireArc(w, PickedPos))
                                {
                                    if (!w.isBeam)
                                        w.FireMouse(Vector2.Normalize(this.findVectorToTarget(new Vector2(w.Center.X, w.Center.Y), new Vector2(PickedPos.X, PickedPos.Y))));
                                    else if (w.isBeam)
                                        w.FireMouseBeam(new Vector2(PickedPos.X, PickedPos.Y));
                                }
                            }
                        }
                    }
                }
                else
                    GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
            }
            this.lastKBState = this.currentKeyBoardState;
        }
        public bool CheckRangeToTarget(Weapon w, GameplayObject target)
        {
            if (target == null || !target.Active || target.Health <= 0)
                return false;
            if (this.engineState == MoveState.Warp)
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
                    || targetship.ExternalSlots.Count <= 0
                    || !w.TargetValid(targetship.shipData.Role)

                    )
                    return false;
            }
            Vector2 PickedPos = target.Center;
            //radius = target.Radius;
            //added by gremlin attackrun compensator
            float modifyRangeAR = 50f;
            Vector2 pos = PickedPos;
            if (w.PrimaryTarget && !w.isBeam && this.GetAI().CombatState == CombatState.AttackRuns && this.maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                modifyRangeAR = this.speed;
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
            if (!this.CheckRangeToTarget(w, target))
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
            (target != null && targetShip != null && (this.loyalty == targetShip.loyalty ||
             !this.loyalty.isFaction &&
           this.loyalty.TryGetRelations(targetShip.loyalty, out enemy) && enemy.Treaty_NAPact))
                return false;
            
            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;            
            Vector2 PickedPos = target.Center;            
            Vector2 pos = PickedPos;
            
            Vector2 toTarget = pos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians); //HelperFunctions.AngleToTarget(w.Center, target.Center);//
            float facing = w.moduleAttachedTo.facing + MathHelper.ToDegrees(base.Rotation);

            
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
  
 
        public bool CheckIfInsideFireArc(Weapon w, Vector3 PickedPos )
        {

            //added by gremlin attackrun compensator
            float modifyRangeAR = 50f;
            Vector2 pos = new Vector2(PickedPos.X, PickedPos.Y);
            if (!w.isBeam && this.GetAI().CombatState == CombatState.AttackRuns && this.maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                modifyRangeAR = this.speed;
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
            float facing = w.moduleAttachedTo.facing + MathHelper.ToDegrees(base.Rotation);
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

        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Ship ship)
        {           
            Vector2 PickedPos = ship.Center;
            float radius = ship.Radius;
            GlobalStats.WeaponArcChecks = GlobalStats.WeaponArcChecks + 1;
            float modifyRangeAR = 50f;
            float distance =Vector2.Distance(w.moduleAttachedTo.Center, PickedPos) ;

            if (w.MassDamage > 0 || w.RepulsionDamage > 0)
            {
                Ship shiptarget = ship;
                if (shiptarget != null && (shiptarget.EnginesKnockedOut || shiptarget.IsTethered() ))
                {
                    return false;
                }
            }
            
            if (!w.isBeam && this.GetAI().CombatState == CombatState.AttackRuns && w.SalvoTimer > 0 && distance / w.SalvoTimer < w.GetOwner().speed) //&& this.maxWeaponsRange < 2000
            {
                
                
                modifyRangeAR = this.speed * w.SalvoTimer;

                if (modifyRangeAR < 50)
                    modifyRangeAR = 50;

            }
            if (distance > w.GetModifiedRange() + modifyRangeAR + radius)
            {
                return false;
            }
            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;
            Vector2 toTarget = PickedPos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians);
            float facing = w.moduleAttachedTo.facing + MathHelper.ToDegrees(base.Rotation);
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
            //float modifyRangeAR = 50f;
            //if (!w.isBeam && this.GetAI().CombatState == CombatState.AttackRuns && this.maxWeaponsRange < 2000 && w.SalvoTimer > 0)
            //{
            //    modifyRangeAR = this.speed * w.SalvoTimer;
            //}
            if (difference < halfArc )//&& Vector2.Distance(w.moduleAttachedTo.Center, PickedPos) < w.GetModifiedRange() + modifyRangeAR)
            {
                return true;
            }
            return false;
        }

        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Vector2 PickedPos, float Rotation)
        {
            if(Vector2.Distance(w.moduleAttachedTo.Center, PickedPos) > w.GetModifiedRange() + 50f)
            {
                return false;
            }
            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f + 1; //Gretman - Slight allowance for check (This version of CheckArc seems to only be called by the beam updater)
            Vector2 toTarget = PickedPos - w.Center;
            float radians = (float)Math.Atan2((double)toTarget.X, (double)toTarget.Y);
            float angleToMouse = 180f - MathHelper.ToDegrees(radians);
            float facing = w.moduleAttachedTo.facing + MathHelper.ToDegrees(Rotation);
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
            if (difference < halfArc )//&& Vector2.Distance(w.moduleAttachedTo.Center, PickedPos) < w.GetModifiedRange() + 50f)
            {
                return true;
            }
            return false;
        }

        public List<Thruster> GetTList()
        {
            return this.ThrusterList;
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

        public void SetTList(List<Thruster> list)
        {
            this.ThrusterList = list;
        }

        private Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
        {
            return new Vector2(0.0f, 0.0f)
            {
                X = (float)-(OwnerPos.X - TargetPos.X),
                Y = (float)-(OwnerPos.Y - TargetPos.Y)
            };
        }

        public void SetSO(SceneObject so)
        {
            ShipSO = so;
            ShipSO.Visibility = ObjectVisibility.Rendered;
            Radius = ShipSO.WorldBoundingSphere.Radius * 2f;
        }

        public SceneObject GetSO()
        {
            return ShipSO;
        }

        public void UpdateInitialWorldTransform()
        {
            ShipSO.World = Matrix.CreateTranslation(new Vector3(Position, 0.0f));
        }

        public ArtificialIntelligence GetAI()
        {
            return AI;
        }

        public void ReturnToHangar()
        {
            if (this.Mothership == null || !this.Mothership.Active)
                return;
            this.AI.State = AIState.ReturnToHangar;
            this.AI.OrderReturnToHangar();
        }

        // ModInfo activation option for Maintenance Costs:

        public float GetMaintCostRealism()
        {
            float maint = 0f;
            float maintModReduction = 1;
            ShipData.RoleName role = this.shipData.Role;
            
            //Free upkeep ships
            if (this.GetShipData().ShipStyle == "Remnant" || this.loyalty == null || this.loyalty.data == null || this.loyalty.data.PrototypeShip == this.Name
                || (this.Mothership != null && (this.shipData.Role >= ShipData.RoleName.fighter && this.shipData.Role <= ShipData.RoleName.frigate)))
            {
                return 0f;
            }
            
            // Calculate maintenance by proportion of ship cost, Duh.
            if (this.shipData.Role == ShipData.RoleName.fighter || this.shipData.Role == ShipData.RoleName.scout)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepFighter;
            else if (this.shipData.Role == ShipData.RoleName.corvette || this.shipData.Role == ShipData.RoleName.gunboat)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepCorvette;
            else if (this.shipData.Role == ShipData.RoleName.frigate || this.shipData.Role == ShipData.RoleName.destroyer)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepFrigate;
            else if (this.shipData.Role == ShipData.RoleName.cruiser)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepCruiser;
            else if (this.shipData.Role == ShipData.RoleName.carrier)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepCarrier;
            else if (this.shipData.Role == ShipData.RoleName.capital)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepCapital;
            else if (this.shipData.Role == ShipData.RoleName.freighter)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepFreighter;
            else if (this.shipData.Role == ShipData.RoleName.platform)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepPlatform;
            else if (this.shipData.Role == ShipData.RoleName.station)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepStation;
            else if (this.shipData.Role == ShipData.RoleName.drone && GlobalStats.ActiveModInfo.useDrones)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepDrone;
            else
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepBaseline;

            if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveModInfo.UpkeepBaseline;
            else if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline == 0)
                maint = this.GetCost(this.loyalty) * 0.004f;

            // Direct override in ShipDesign XML, e.g. for Shipyards/pre-defined designs with specific functions.

            if (this.shipData.HasFixedUpkeep && this.loyalty != null)
            {
                maint = shipData.FixedUpkeep;
            }      

            // Modifiers below here   


            //Doctor: Configurable civilian maintenance modifier.
            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && this.loyalty != null && !this.loyalty.isFaction && this.loyalty.data.CivMaintMod != 1)
            {
                maint *= this.loyalty.data.CivMaintMod;
            }

            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && this.loyalty != null && !this.loyalty.isFaction && this.loyalty.data.Privatization)
            {
                maint *= 0.5f;
            }

            if (GlobalStats.OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = GlobalStats.OptionIncreaseShipMaintenance;
                maint *= (float)maintModReduction;
            }
            return maint;

        }

        public float GetMaintCostRealism(Empire empire)
        {
            float maint = 0f;
            float maintModReduction = 1;
            //string role = this.shipData.Role;

            // Calculate maintenance by proportion of ship cost, Duh.
            if (this.shipData.Role == ShipData.RoleName.fighter || this.shipData.Role == ShipData.RoleName.scout)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepFighter;
            else if (this.shipData.Role == ShipData.RoleName.corvette || this.shipData.Role == ShipData.RoleName.gunboat)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepCorvette;
                else if (this.shipData.Role == ShipData.RoleName.frigate || this.shipData.Role == ShipData.RoleName.destroyer)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepFrigate;
                else if (this.shipData.Role == ShipData.RoleName.cruiser)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepCruiser;
                else if (this.shipData.Role == ShipData.RoleName.carrier)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepCarrier;
                else if (this.shipData.Role == ShipData.RoleName.capital)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepCapital;
                else if (this.shipData.Role == ShipData.RoleName.freighter)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepFreighter;
                else if (this.shipData.Role == ShipData.RoleName.platform)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepPlatform;
                else if (this.shipData.Role == ShipData.RoleName.station)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepStation;
                else if (this.shipData.Role == ShipData.RoleName.drone && GlobalStats.ActiveModInfo.useDrones)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepDrone;
                else
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepBaseline;

                if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0)
                    maint = this.GetCost(empire) * GlobalStats.ActiveModInfo.UpkeepBaseline;
                else if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline == 0)
                    maint = this.GetCost(empire) * 0.004f;


            // Direct override in ShipDesign XML, e.g. for Shipyards/pre-defined designs with specific functions.

            if (this.shipData.HasFixedUpkeep && empire != null)
            {
                maint = shipData.FixedUpkeep;
            }

            // Modifiers below here   

            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && empire != null && !empire.isFaction && empire.data.CivMaintMod != 1)
            {
                maint *= empire.data.CivMaintMod;
            }

            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && empire != null && !empire.isFaction && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            if (GlobalStats.OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = GlobalStats.OptionIncreaseShipMaintenance;
                maint *= (float)maintModReduction;
            }
            maint += maint * empire.data.Traits.MaintMod;
            return maint;

        }
            

        public float GetMaintCost()
        {
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                if(this.loyalty == null)
                return this.GetMaintCostRealism();
                else
                    return this.GetMaintCostRealism(this.loyalty);
            }
            float maint = 0f;
            //string role = this.shipData.Role;
            //string str = role;
            //bool nonCombat = false;
            //added by gremlin: Maintenance changes
            float maintModReduction = 1;

            //Ships without upkeep
            if (this.shipData.ShipStyle == "Remnant" || this.loyalty?.data == null || (this.Mothership != null && (this.shipData.Role >= ShipData.RoleName.fighter && this.shipData.Role <= ShipData.RoleName.frigate)))
            {
                return 0f;
            }

            //Get Maintanence of ship role
            bool foundMaint = false;
            if (ResourceManager.ShipRoles.ContainsKey(this.shipData.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[this.shipData.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[this.shipData.Role].RaceList[i].ShipType == this.loyalty.data.Traits.ShipType)
                    {
                        maint = ResourceManager.ShipRoles[this.shipData.Role].RaceList[i].Upkeep;
                        foundMaint = true;
                        break;
                    }
                }
                if (!foundMaint)
                    maint = ResourceManager.ShipRoles[this.shipData.Role].Upkeep;
            }
            else
                return 0f;

            //Modify Maintanence by freighter size
            if(this.shipData.Role == ShipData.RoleName.freighter)
            {
                switch (this.Size / 50)
                {
                    case 0:
                        {
                            break;
                        }

                    case 1:
                        {
                            maint *= 1.5f;
                            break;
                        }

                    case 2:
                    case 3:
                    case 4:
                        {
                            maint *= 2f;
                            break;
                        }
                    default:
                        {
                            maint *= this.Size / 50;
                            break;
                        }
                }
            }


            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && this.loyalty != null && !this.loyalty.isFaction && this.loyalty.data.CivMaintMod != 1.0)
            {
                maint *= this.loyalty.data.CivMaintMod;
            }

            //Apply Privatization
            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && this.loyalty != null && !this.loyalty.isFaction && this.loyalty.data.Privatization)
            {
                maint *= 0.5f;
            }

            //Subspace Projectors do not get any more modifiers
            if (this.Name == "Subspace Projector")
            {
                return maint;
            }

            //added by gremlin shipyard exploit fix
            if (this.IsTethered())
            {
                if (this.shipData.Role == ShipData.RoleName.platform)
                    return maint *= 0.5f;
                if (this.shipData.IsShipyard && this.GetTether().Shipyards.Count(shipyard => shipyard.Value.shipData.IsShipyard) > 3)
                    maint *= this.GetTether().Shipyards.Count(shipyard => shipyard.Value.shipData.IsShipyard) - 3;
            }

            //Maintenance fluctuator
            //string configvalue1 = ConfigurationManager.AppSettings["countoffiles"];
            float OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            if (OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = OptionIncreaseShipMaintenance;

                if (this.IsInFriendlySpace || this.inborders)// && Properties.Settings.Default.OptionIncreaseShipMaintenance >1)
                {
                    maintModReduction *= .25f;
                    if (this.inborders) maintModReduction *= .75f;
                    //if (this.GetAI().inOrbit)
                    //{
                    //    maintModReduction *= .25f;
                    //}
                }
                if (this.IsInNeutralSpace && !this.IsInFriendlySpace )
                {
                    maintModReduction *= .5f;
                }

                if (this.IsIndangerousSpace)
                {
                    maintModReduction *= 2f;
                }
                if (this.number_Alive_Internal_slots < this.number_Internal_slots)
                {
                    float damRepair = 2 - this.number_Internal_slots / this.number_Alive_Internal_slots;
                    if (damRepair > 1.5f) damRepair = 1.5f;
                    if (damRepair < 1) damRepair = 1;
                    maintModReduction *= damRepair;

                }
                if (maintModReduction < 1) maintModReduction = 1;
                maint *= maintModReduction;
            }
            return maint;
        }

        // The Doctor - This function is an overload which is used for the Ship Build menu.
        // It will calculate the maintenance cost in exactly the same way as the normal function, except as the ship build list elements have no loyalty data, this variable is called by the function
        //CG modified so that the original function will call the mod only function if a mod is present and such.
        public float GetMaintCost(Empire empire)
        {
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                return this.GetMaintCostRealism(empire);
            }
            float maint = 0f;
            //shipData.Role role = this.shipData.Role;
            //string str = role;
            //bool nonCombat = false;
            //added by gremlin: Maintenance changes
            float maintModReduction = 1;

            //Get Maintanence of ship role
            bool foundMaint = false;
            if (ResourceManager.ShipRoles.ContainsKey(this.shipData.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[this.shipData.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[this.shipData.Role].RaceList[i].ShipType == empire.data.Traits.ShipType)
                    {
                        maint = ResourceManager.ShipRoles[this.shipData.Role].RaceList[i].Upkeep;
                        foundMaint = true;
                        break;
                    }
                }
                if (!foundMaint)
                    maint = ResourceManager.ShipRoles[this.shipData.Role].Upkeep;
            }
            else
                return 0f;

            //Modify Maintanence by freighter size
            if (this.shipData.Role == ShipData.RoleName.freighter)
            {
                switch ((int)this.Size / 50)
                {
                    case 0:
                        {
                            break;
                        }

                    case 1:
                        {
                            maint *= 1.5f;
                            break;
                        }

                    case 2:
                    case 3:
                    case 4:
                        {
                            maint *= 2f;
                            break;
                        }
                    default:
                        {
                            maint *= (int)this.Size / 50;
                            break;
                        }
                }
            }

            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            //Apply Privatization
            if ((this.shipData.Role == ShipData.RoleName.freighter || this.shipData.Role == ShipData.RoleName.platform) && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            //Subspace Projectors do not get any more modifiers
            if (this.Name == "Subspace Projector")
            {
                return maint;
            }

            //Maintenance fluctuator
            //string configvalue1 = ConfigurationManager.AppSettings["countoffiles"];
            float OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            if (OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = OptionIncreaseShipMaintenance;
                maint *= maintModReduction;
            }
            return maint;
        }

        public int GetTechScore()
        {
            
            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                ShipModule shipModule = Ship_Game.ResourceManager.ShipModulesDict[moduleSlot.InstalledModuleUID];
                switch (shipModule.ModuleType)
                {
                    case ShipModuleType.Turret:
                        if ((int)shipModule.TechLevel > num3)
                        {
                            num3 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    case ShipModuleType.MainGun:
                        if ((int)shipModule.TechLevel > num3)
                        {
                            num3 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    case ShipModuleType.PowerPlant:
                        if ((int)shipModule.TechLevel > num4)
                        {
                            num4 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    case ShipModuleType.Engine:
                        if ((int)shipModule.TechLevel > num2)
                        {
                            num2 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    case ShipModuleType.Shield:
                        if ((int)shipModule.TechLevel > num1)
                        {
                            num1 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    case ShipModuleType.MissileLauncher:
                        if ((int)shipModule.TechLevel > num3)
                        {
                            num3 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    case ShipModuleType.Bomb:
                        if ((int)shipModule.TechLevel > num3)
                        {
                            num3 = (int)shipModule.TechLevel;
                            continue;
                        }
                        else
                            continue;
                    default:
                        continue;
                }
            }
            return num2 + num4 + num1 + num3;
        }

        public void DoEscort(Ship EscortTarget)
        {
            this.AI.OrderQueue.Clear();
            this.AI.State = AIState.Escort;
            this.AI.EscortTarget = EscortTarget;
        }

        public void DoDefense()
        {
            this.AI.State = AIState.SystemDefender;
        }

        public void DoDefense(SolarSystem toDefend)
        {
            this.AI.SystemToDefend = toDefend;
            this.AI.State = AIState.SystemDefender;
        }

        public void DefendSystem(SolarSystem toDefend)
        {
            this.AI.State = AIState.SystemDefender;
            this.AI.SystemToDefend = toDefend;
        }

        public void DoOrbit(Planet orbit)
        {
            this.AI.OrderToOrbit(orbit, true);
        }

        public void DoExplore()
        {
            this.AI.OrderExplore();
        }

        public void DoColonize(Planet p, Goal g)
        {
            this.AI.OrderColonization(p);
        }

        public void DoTrading()
        {
            this.AI.State = AIState.SystemTrader;
        }

        public void InitializeAI()
        {
            this.AI = new ArtificialIntelligence(this);
            this.AI.State = AIState.AwaitingOrders;
            if (this.shipData == null)
                return;
            this.AI.CombatState = this.shipData.CombatState;
            this.AI.CombatAI = new CombatAI(this);
        }

        public void LoadFromSave()
        {
            foreach (KeyValuePair<string, ShipData> keyValuePair in Ship_Game.ResourceManager.HullsDict)
            {
                if (keyValuePair.Value.ModelPath == this.ModelPath)
                {
                    if (keyValuePair.Value.Animated)
                    {
                        SkinnedModel skinnedModel = Ship_Game.ResourceManager.GetSkinnedModel(this.ModelPath);
                        this.ShipSO = new SceneObject(skinnedModel.Model);
                        this.animationController = new AnimationController(skinnedModel.SkeletonBones);
                        this.animationController.StartClip(skinnedModel.AnimationClips["Take 001"]);
                    }
                    else
                    {
                        this.ShipSO = new SceneObject(((ReadOnlyCollection<ModelMesh>)Ship_Game.ResourceManager.GetModel(this.ModelPath).Meshes)[0]);
                        this.ShipSO.ObjectType = ObjectType.Dynamic;
                    }
                }
            }
        }

        public void InitializeFromSave()
        {
            if (this.shipData.Role == ShipData.RoleName.platform)
                this.IsPlatform = true;
            this.Weapons.Clear();
            this.Center = new Vector2(this.Position.X + this.Dimensions.X / 2f, this.Position.Y + this.Dimensions.Y / 2f);
            this.InitFromSave();
            if (string.IsNullOrEmpty(this.VanityName))
            this.VanityName = this.Name;
            if (Ship_Game.ResourceManager.ShipsDict.ContainsKey(this.Name) && Ship_Game.ResourceManager.ShipsDict[this.Name].IsPlayerDesign)
                this.IsPlayerDesign = true;
            else if (!Ship_Game.ResourceManager.ShipsDict.ContainsKey(this.Name))
                this.FromSave = true;
            this.LoadInitializeStatus();
            if (Ship.universeScreen != null)
                Ship.universeScreen.ShipsToAdd.Add(this);
            else
                UniverseScreen.ShipSpatialManager.CollidableObjects.Add((GameplayObject)this);
            if (this.System != null && !this.System.spatialManager.CollidableObjects.Contains((GameplayObject)this))
            {
                this.isInDeepSpace = false;
                this.System.spatialManager.CollidableObjects.Add((GameplayObject)this);
                if (!this.System.ShipList.Contains(this))
                    this.System.ShipList.Add(this);
            }
            else if (this.isInDeepSpace)
            {
                lock (GlobalStats.DeepSpaceLock)
                    UniverseScreen.DeepSpaceManager.CollidableObjects.Add((GameplayObject)this);
            }
            this.FillExternalSlots();
            //this.hyperspace = (Cue)null;   //Removed to save space, because this is set to null in ship initilizers, and never reassigned. -Gretman
            base.Initialize();
            foreach (ModuleSlot ss in this.ModuleSlotList)
            {
                if (ss.module.ModuleType == ShipModuleType.PowerConduit)
                    ss.module.IconTexturePath = this.GetConduitGraphic(ss, this);
                if (ss.module.ModuleType == ShipModuleType.Hangar)
                {
                    ss.module.hangarShipUID = ss.SlotOptions;
                    this.Hangars.Add(ss.module);
                }
                if (ss.module.ModuleType == ShipModuleType.Transporter)
                {
                    this.Transporters.Add(ss.module);
                    this.hasTransporter = true;
                    if (ss.module.TransporterOrdnance > 0)
                        this.hasOrdnanceTransporter = true;
                    if (ss.module.TransporterTroopAssault > 0)
                        this.hasAssaultTransporter = true;
                }
                if (ss.module.IsRepairModule)
                    this.HasRepairModule = true;
                if (ss.module.InstalledWeapon != null && ss.module.InstalledWeapon.isRepairBeam)
                {
                    this.RepairBeams.Add(ss.module);
                    this.hasRepairBeam = true;
                }
            }
            this.ShipSO.Visibility = ObjectVisibility.Rendered;
            this.Radius = this.ShipSO.WorldBoundingSphere.Radius * 2f;
            this.ShipStatusChange();
            this.shipInitialized = true;
            this.RecalculateMaxHP();            //Fix for Ship Max health being greater than all modules combined (those damned haphazard engineers). -Gretman

            if (this.VanityName == "MerCraft") Log.Info("Health from InitializeFromSave is:  " + this.HealthMax);
        }

        public override void Initialize()
        {
            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;
            SetShipData(GetShipData());
            if (string.IsNullOrEmpty(VanityName))
            {
                VanityName = Name;
            }
            Weapons.Clear();
            Center = new Vector2(Position.X + Dimensions.X / 2f, Position.Y + Dimensions.Y / 2f);
            lock (GlobalStats.AddShipLocker)
            {
                if (universeScreen == null)
                    UniverseScreen.ShipSpatialManager.CollidableObjects.Add(this);
                else
                    universeScreen.ShipsToAdd.Add(this);
            }
            InitializeModules();

            Ship template = ResourceManager.ShipsDict[Name];
            IsPlayerDesign = template.IsPlayerDesign;

            InitializeStatus();
            if (AI == null)
                InitializeAI();
            AI.CombatState = template.shipData.CombatState;
            FillExternalSlots();
            //this.hyperspace = (Cue)null;   //Removed to save space, because this is set to null in ship initilizers, and never reassigned. -Gretman
            base.Initialize();
            foreach (ModuleSlot ss in ModuleSlotList)
            {
                if (ss.InstalledModuleUID == "Dummy") continue;
                if (ss.module.ModuleType == ShipModuleType.PowerConduit)
                    ss.module.IconTexturePath = GetConduitGraphic(ss, this);

                HasRepairModule |= ss.module.IsRepairModule;
                isColonyShip    |= ss.module.ModuleType == ShipModuleType.Colony;

                if (ss.module.ModuleType == ShipModuleType.Transporter)
                {
                    hasTransporter = true;
                    hasOrdnanceTransporter |= ss.module.TransporterOrdnance > 0;
                    hasAssaultTransporter  |= ss.module.TransporterTroopAssault > 0;
                }
                hasRepairBeam |= ss.module.InstalledWeapon != null && ss.module.InstalledWeapon.isRepairBeam;
            }
            RecalculatePower();        
            ShipStatusChange();
            shipInitialized = true;
        }

        private void FillExternalSlots()
        {
            this.ExternalSlots.Clear();
            this.ModulesDictionary.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                this.ModulesDictionary.Add(moduleSlot.Position, moduleSlot);
            foreach (KeyValuePair<Vector2, ModuleSlot> keyValuePair in this.ModulesDictionary)
            {

                if (keyValuePair.Value.module.Active)
                {
                    Vector2 key1 = new Vector2(keyValuePair.Key.X, keyValuePair.Key.Y - 16f);
                    if (this.ModulesDictionary.ContainsKey(key1))
                    {
                        if (!this.ModulesDictionary[key1].module.Active)
                        {
                            keyValuePair.Value.module.isExternal = true;
                            keyValuePair.Value.module.quadrant = 1;
                            this.ExternalSlots.Add(keyValuePair.Value);

                        }
                        else
                        {
                            Vector2 key2 = new Vector2(keyValuePair.Key.X, keyValuePair.Key.Y + 16f);
                            if (this.ModulesDictionary.ContainsKey(key2))
                            {
                                if (!this.ModulesDictionary[key2].module.Active)
                                {
                                    keyValuePair.Value.module.isExternal = true;
                                    keyValuePair.Value.module.quadrant = 2;
                                    this.ExternalSlots.Add(keyValuePair.Value);
                                }
                                else
                                {
                                    Vector2 key3 = new Vector2(keyValuePair.Key.X - 16f, keyValuePair.Key.Y);
                                    if (this.ModulesDictionary.ContainsKey(key3))
                                    {
                                        if (!this.ModulesDictionary[key3].module.Active)
                                        {
                                            keyValuePair.Value.module.isExternal = true;
                                            keyValuePair.Value.module.quadrant = 3;
                                            this.ExternalSlots.Add(keyValuePair.Value);
                                        }
                                        else
                                        {
                                            Vector2 key4 = new Vector2(keyValuePair.Key.X + 16f, keyValuePair.Key.Y);
                                            if (this.ModulesDictionary.ContainsKey(key4))
                                            {
                                                if (!this.ModulesDictionary[key4].module.Active)
                                                {
                                                    keyValuePair.Value.module.isExternal = true;
                                                    keyValuePair.Value.module.quadrant = 4;
                                                    this.ExternalSlots.Add(keyValuePair.Value);
                                                }
                                            }
                                            else
                                            {
                                                keyValuePair.Value.module.isExternal = true;
                                                keyValuePair.Value.module.quadrant = 4;
                                                this.ExternalSlots.Add(keyValuePair.Value);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        keyValuePair.Value.module.isExternal = true;
                                        keyValuePair.Value.module.quadrant = 3;

                                        this.ExternalSlots.Add(keyValuePair.Value);
                                    }
                                }
                            }
                            else
                            {
                                keyValuePair.Value.module.isExternal = true;
                                keyValuePair.Value.module.quadrant = 2;
                                this.ExternalSlots.Add(keyValuePair.Value);
                            }
                        }
                    }
                    else
                    {
                        keyValuePair.Value.module.isExternal = true;
                        keyValuePair.Value.module.quadrant = 1;
                        this.ExternalSlots.Add(keyValuePair.Value);
                    }
                }
                if (keyValuePair.Value.module.shield_power > 0.0 && !keyValuePair.Value.module.isExternal)
                {
                    keyValuePair.Value.module.isExternal = true;
                    this.ExternalSlots.Add(keyValuePair.Value);

                }
            }
        }

        public void ResetJumpTimer()
        {
                this.JumpTimer = this.FTLSpoolTime * this.loyalty.data.SpoolTimeModifier;
        } 

        //added by gremlin: Fighter recall and stuff
        public void EngageStarDrive()
        {
            if (this.isSpooling ||this.engineState == Ship.MoveState.Warp || this.GetmaxFTLSpeed <=2500 )
            {
                return;
            }

            #region carrier figter interaction recall
            //added by gremlin : fighter recal
            if (this.RecallFightersBeforeFTL && this.GetHangars().Count > 0)
            {
                bool RecallFigters = false;
                float JumpDistance = Vector2.Distance(this.Center, this.GetAI().MovePosition);
                float slowfighter = this.speed * 2;
                if (JumpDistance > 7500f)
                {

                    RecallFigters = true;


                    foreach (ShipModule Hanger in this.GetHangars().Select(t => t as ShipModule))
                    {
                        if (Hanger.IsSupplyBay || Hanger.GetHangarShip() == null) { RecallFigters = false; continue; }
                        Ship hangerShip = Hanger.GetHangarShip();
                        //min jump distance 7500f
                        //this.MovePosition
                        if (hangerShip.speed < slowfighter) slowfighter = hangerShip.speed;



                        float rangeTocarrier = Vector2.Distance(hangerShip.Center, this.Center);


                        if (rangeTocarrier > this.SensorRange)
                        { RecallFigters = false; continue; }
                        if (hangerShip.disabled || !hangerShip.hasCommand || hangerShip.dying || hangerShip.EnginesKnockedOut)
                        {
                            RecallFigters = false;
                            if (Hanger.GetHangarShip().ScuttleTimer == 0) Hanger.GetHangarShip().ScuttleTimer = 10f;
                            continue;
                        }


                        RecallFigters = true; break;
                    }
                }

                if (RecallFigters == true)
                {
                    this.RecoverAssaultShips();
                    this.RecoverFighters();
                    if (!this.DoneRecovering())
                    {


                        if (this.speed * 2 > slowfighter) { this.speed = slowfighter * .25f; }



                        return;

                    }
                }

            }
            #endregion
            if(EnginesKnockedOut)
            {
                this.HyperspaceReturn();
                return;
            }
            if (this.velocityMaximum > this.GetmaxFTLSpeed)
                return;
            if (this.engineState == Ship.MoveState.Sublight && !this.isSpooling && this.PowerCurrent / (this.PowerStoreMax + 0.01f) > 0.1f)
            {
                this.isSpooling = true;
                this.ResetJumpTimer();
            }
        }

        private string GetStartWarpCue()
        {
            if (this.loyalty.data.WarpStart != null)
                return this.loyalty.data.WarpStart;
            if (this.Size < 60)
                return "sd_warp_start_small";
            return this.Size > 350 ? "sd_warp_start_large" : "sd_warp_start_02";
        }

        private string GetEndWarpCue()
        {
            if (this.loyalty.data.WarpStart != null)
                return this.loyalty.data.WarpEnd;
            if (this.Size < 60)
                return "sd_warp_stop_small";
            return this.Size > 350 ? "sd_warp_stop_large" : "sd_warp_stop";
        }

        public void HyperspaceReturn()
        {
            if (Ship.universeScreen == null || this.engineState == Ship.MoveState.Sublight)
                return;
            if (this.Jump != null && this.Jump.IsPlaying)
            {
                this.Jump.Stop(AudioStopOptions.Immediate);
                this.Jump = (Cue)null;
            }
            if (this.engineState == Ship.MoveState.Warp && Vector2.Distance(this.Center, new Vector2(Ship.universeScreen.camPos.X, Ship.universeScreen.camPos.Y)) < 100000.0 && Ship.universeScreen.camHeight < 250000)
            {
                AudioManager.PlayCue(GetEndWarpCue(), universeScreen.listener, emitter);
                FTLManager.AddFTL(Center);
            }
            this.engineState = Ship.MoveState.Sublight;
            this.ResetJumpTimer();
            this.isSpooling = false;
            this.velocityMaximum = this.GetSTLSpeed();
            if (this.Velocity != Vector2.Zero)
                this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
            this.speed = this.velocityMaximum;
        }

        public bool isPlayerShip()
        {
            return this.PlayerShip;
        }

        ///added by gremlin Initialize status from deveks mod. 
        public void InitializeStatus()
        {
            #region Variables
            base.Mass = 0f;
            this.Mass += (float)this.Size;
            this.Thrust = 0f;
            this.WarpThrust = 0f;
            this.PowerStoreMax = 0f;
            this.PowerFlowMax = 0f;
            this.ModulePowerDraw = 0f;
            this.ShieldPowerDraw = 0f;
            this.shield_max = 0f;
            this.shield_power = 0f;
            this.armor_max = 0f;
            //this.CrewRequired = 0;    //Not referenced in code, removing to save memory
            //this.CrewSupplied = 0;    //Not referenced in code, removing to save memory
            this.Size = 0;
            this.velocityMaximum = 0f;
            this.speed = 0f;
            this.SensorRange = 0f;
            float sensorBonus = 0f;
            this.OrdinanceMax = 0f;
            this.OrdAddedPerSecond = 0f;
            this.rotationRadiansPerSecond = 0f;
            base.Health = 0f;
            this.TroopCapacity = 0;
            this.MechanicalBoardingDefense = 0f;
            this.TroopBoardingDefense = 0f;
            this.ECMValue = 0f;
            this.FTLSpoolTime = 0f;
            this.RangeForOverlay = 0f;

            string troopType = "Wyvern";
            string tankType = "Wyvern";
            string redshirtType = "Wyvern";

            foreach (Weapon w in this.Weapons)
            {
                if (w.GetModifiedRange() > this.RangeForOverlay)
                    this.RangeForOverlay = w.GetModifiedRange();
            }

            #endregion
            #region TroopListFix
            if (this.loyalty != null && this.loyalty.GetTrDict().Where(value => value.Value == true).Count() > 0)
            {

                troopType = ResourceManager.TroopsDict.Where(troop => this.loyalty.GetTrDict()[troop.Key] == true).OrderByDescending(strength => strength.Value.SoftAttack).First().Key;
                tankType = ResourceManager.TroopsDict.Where(troop => this.loyalty.GetTrDict()[troop.Key] == true).OrderByDescending(strength => strength.Value.HardAttack).First().Key;
                redshirtType = ResourceManager.TroopsDict.Where(troop => this.loyalty.GetTrDict()[troop.Key] == true).OrderBy(strength => strength.Value.SoftAttack).First().Key;

                troopType = troopType == redshirtType ? troopType = tankType : troopType;



            }
            #endregion
            #region ModuleCheck
            
            foreach (ModuleSlot moduleSlotList in this.ModuleSlotList)
            {
                if (moduleSlotList.Restrictions == Restrictions.I)
                    ++this.number_Internal_slots;
                if (moduleSlotList.module.ModuleType == ShipModuleType.Dummy)
                    continue;
                if (moduleSlotList.module.ModuleType == ShipModuleType.Colony)
                    this.isColonyShip = true;
                if (moduleSlotList.module.ModuleType == ShipModuleType.Construction)
                {
                    this.isConstructor = true;
                    this.shipData.Role = ShipData.RoleName.construction;
                }
                
                if (moduleSlotList.module.ResourceStorageAmount > 0f && ResourceManager.GoodsDict.ContainsKey(moduleSlotList.module.ResourceStored) && !ResourceManager.GoodsDict[moduleSlotList.module.ResourceStored].IsCargo)
                {
                    Dictionary<string, float> maxGoodStorageDict = this.MaxGoodStorageDict;
                    Dictionary<string, float> strs = maxGoodStorageDict;
                    string resourceStored = moduleSlotList.module.ResourceStored;
                    string str = resourceStored;
                    maxGoodStorageDict[resourceStored] = strs[str] + moduleSlotList.module.ResourceStorageAmount;
                }

                #region Troopload
                for (int i = 0; i < moduleSlotList.module.TroopsSupplied; i++)
                {
                    int hangars = this.ModuleSlotList.Where(hangarbay => hangarbay.module.IsTroopBay).Count();

                    if (hangars < this.TroopList.Count())
                    {
                        if (this.TroopList.Where(trooptype => trooptype.Name == tankType).Count() <= hangars / 2)
                        {
                            this.TroopList.Add(ResourceManager.CreateTroop(ResourceManager.TroopsDict[tankType], this.loyalty)); //"Space Marine"

                        }
                        else
                        {
                            this.TroopList.Add(ResourceManager.CreateTroop(ResourceManager.TroopsDict[troopType], this.loyalty)); //"Space Marine
                        }
                    }
                    else
                    {
                        this.TroopList.Add(ResourceManager.CreateTroop(ResourceManager.TroopsDict[redshirtType], this.loyalty)); //"Space Marine"
                    }
                #endregion
                }
                if (moduleSlotList.module.SensorRange > this.SensorRange)
                {
                    this.SensorRange = moduleSlotList.module.SensorRange;
                }
                if (moduleSlotList.module.SensorBonus > sensorBonus)
                {
                    sensorBonus = moduleSlotList.module.SensorBonus;
                }
                if (moduleSlotList.module.ECM > this.ECMValue)
                {
                    this.ECMValue = moduleSlotList.module.ECM;
                    if (this.ECMValue > 1.0f)
                        this.ECMValue = 1.0f;
                    if (this.ECMValue < 0f)
                        this.ECMValue = 0f;
                }
                this.TroopCapacity += moduleSlotList.module.TroopCapacity;
                this.MechanicalBoardingDefense += moduleSlotList.module.MechanicalBoardingDefense;
                if (this.MechanicalBoardingDefense < 1f)
                {
                    this.MechanicalBoardingDefense = 1f;
                }
                if (moduleSlotList.module.ModuleType == ShipModuleType.Hangar)
                {
                    moduleSlotList.module.hangarShipUID = moduleSlotList.SlotOptions;
                    if (moduleSlotList.module.IsTroopBay)
                    {
                        this.HasTroopBay = true;
                    }
                }
                if (moduleSlotList.module.ModuleType == ShipModuleType.Transporter)
                    this.Transporters.Add(moduleSlotList.module);
                if (moduleSlotList.module.InstalledWeapon != null && moduleSlotList.module.InstalledWeapon.isRepairBeam)
                    this.RepairBeams.Add(moduleSlotList.module);
                if (moduleSlotList.module.ModuleType == ShipModuleType.Armor && this.loyalty != null)
                {
                    float modifiedMass = moduleSlotList.module.Mass * this.loyalty.data.ArmourMassModifier;
                    this.Mass += modifiedMass;
                }
                else
                    this.Mass += moduleSlotList.module.Mass;
                this.Thrust += moduleSlotList.module.thrust;
                this.WarpThrust += moduleSlotList.module.WarpThrust;
                //Added by McShooterz: fuel cell modifier apply to all modules with power store
                this.PowerStoreMax += moduleSlotList.module.PowerStoreMax + moduleSlotList.module.PowerStoreMax * (this.loyalty != null ? this.loyalty.data.FuelCellModifier : 0);
                this.PowerCurrent += moduleSlotList.module.PowerStoreMax;
                this.PowerFlowMax += moduleSlotList.module.PowerFlowMax + (this.loyalty != null ? moduleSlotList.module.PowerFlowMax * this.loyalty.data.PowerFlowMod : 0);
                this.shield_max += moduleSlotList.module.shield_power_max + (this.loyalty != null ? moduleSlotList.module.shield_power_max * this.loyalty.data.ShieldPowerMod : 0);
                if (moduleSlotList.module.ModuleType == ShipModuleType.Armor)
                {
                    this.armor_max += moduleSlotList.module.HealthMax;
                }
                this.Size += 1;
                this.CargoSpace_Max += moduleSlotList.module.Cargo_Capacity;
                this.OrdinanceMax += (float)moduleSlotList.module.OrdinanceCapacity;
                this.Ordinance += (float)moduleSlotList.module.OrdinanceCapacity;
                if(moduleSlotList.module.ModuleType != ShipModuleType.Shield)
                    this.ModulePowerDraw += moduleSlotList.module.PowerDraw;
                else
                    this.ShieldPowerDraw += moduleSlotList.module.PowerDraw;
                this.Health += moduleSlotList.module.HealthMax;
                if (moduleSlotList.module.FTLSpoolTime > this.FTLSpoolTime)
                    this.FTLSpoolTime = moduleSlotList.module.FTLSpoolTime;
            }

            #endregion
            #region BoardingDefense
            foreach (Troop troopList in this.TroopList)
            {
                troopList.SetOwner(this.loyalty);
                troopList.SetShip(this);
                Ship troopBoardingDefense = this;
                troopBoardingDefense.TroopBoardingDefense = troopBoardingDefense.TroopBoardingDefense + (float)troopList.Strength;
            }
            {
                //mechanicalBoardingDefense1.MechanicalBoardingDefense = mechanicalBoardingDefense1.MechanicalBoardingDefense / (this.number_Internal_modules);
                this.MechanicalBoardingDefense *= (1 + this.TroopList.Count() / 10);
                if (this.MechanicalBoardingDefense < 1f)
                {
                    this.MechanicalBoardingDefense = 1f;
                }
            }
            #endregion
            this.HealthMax = base.Health;
            this.number_Alive_Internal_slots = this.number_Internal_slots;
            this.velocityMaximum = this.Thrust / this.Mass;
            this.speed = this.velocityMaximum;
            this.rotationRadiansPerSecond = this.speed / (float)this.Size;
            this.ShipMass = this.Mass;
            this.shield_power = this.shield_max;
            this.SensorRange += sensorBonus;
            if (this.FTLSpoolTime <= 0f)
                this.FTLSpoolTime = 3f;
        }
        public void RenderOverlay(SpriteBatch spriteBatch, Rectangle where, bool ShowModules)
        {
            if (Ship_Game.ResourceManager.HullsDict.ContainsKey(this.shipData.Hull) && !string.IsNullOrEmpty(Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic) && !ShowModules)
            {
                Rectangle destinationRectangle = where;
                destinationRectangle.X += 2;
                spriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["SelectionBox Ships/" + Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic], destinationRectangle, Color.White);
                if (this.shield_power > 0.0)
                {
                    float num = (float)byte.MaxValue * (float)this.shield_percent;
                    spriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["SelectionBox Ships/" + Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic + "_shields"], destinationRectangle, new Color(Color.White, (byte)num));
                }
            }
            if (!ShowModules && !string.IsNullOrEmpty(Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic) || this.ModuleSlotList.Count == 0)
                return;
            IOrderedEnumerable<ModuleSlot> orderedEnumerable1 = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)this.ModuleSlotList, (Func<ModuleSlot, float>)(slot => slot.Position.X));
            if (Enumerable.Count<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable1) == 0)
                return;
            float num1 = (float)(Enumerable.Last<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable1).Position.X - Enumerable.First<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable1).Position.X + 16.0);
            IOrderedEnumerable<ModuleSlot> orderedEnumerable2 = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)this.ModuleSlotList, (Func<ModuleSlot, float>)(slot => slot.Position.Y));
            float num2 = (float)(Enumerable.Last<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable2).Position.Y - Enumerable.First<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable2).Position.Y + 16.0);
            int num3;
            if (num1 > num2)
            {
                double num4 = num1 / where.Width;
                num3 = (int)num1 / 16 + 1;
            }
            else
            {
                double num4 = num2 / where.Width;
                num3 = (int)num2 / 16 + 1;
            }
            float num5 = (float)(where.Width / num3);
            if (num5 < 2.0)
                num5 = (float)where.Width / (float)num3;
            if (num5 > 10.0)
                num5 = 10f;
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                Vector2 vector2_1 = moduleSlot.module.XMLPosition - new Vector2(264f, 264f);
                Vector2 vector2_2 = new Vector2(vector2_1.X / 16f, vector2_1.Y / 16f) * num5;
                if (Math.Abs(vector2_2.X) > (where.Width / 2) || Math.Abs(vector2_2.Y) > (where.Height / 2))
                {
                    num5 = (float)(where.Width / (num3 + 10));
                    break;
                }
            }
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                Vector2 vector2 = moduleSlot.module.XMLPosition - new Vector2(264f, 264f);
                vector2 = new Vector2(vector2.X / 16f, vector2.Y / 16f) * num5;
                Rectangle rect = new Rectangle(where.X + where.Width / 2 + (int)vector2.X, where.Y + where.Height / 2 + (int)vector2.Y, (int)num5, (int)num5);
                Color green = Color.Green;
                Color color = moduleSlot.module.Health / moduleSlot.module.HealthMax < 0.899999976158142 ? (moduleSlot.module.Health / moduleSlot.module.HealthMax < 0.649999976158142 ? (moduleSlot.module.Health / moduleSlot.module.HealthMax < 0.449999988079071 ? (moduleSlot.module.Health / moduleSlot.module.HealthMax < 0.150000005960464 ? (moduleSlot.module.Health / moduleSlot.module.HealthMax > 0.150000005960464 || moduleSlot.module.Health <= 0.0 ? Color.Red : Color.Red) : Color.OrangeRed) : Color.Yellow) : Color.GreenYellow) : Color.Green;
                Primitives2D.FillRectangle(spriteBatch, rect, color);
            }
        }

        public void ScrambleAssaultShipsORIG()
        {
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                if (moduleSlot.module != null && moduleSlot.module.ModuleType == ShipModuleType.Hangar && (moduleSlot.module.IsTroopBay && this.TroopList.Count > 0) && (moduleSlot.module.GetHangarShip() == null && moduleSlot.module.hangarTimer <= 0.0))
                {
                    moduleSlot.module.LaunchBoardingParty(this.TroopList[0]);
                    this.TroopList.RemoveAt(0);
                }
            }
        }
        //added by gremlin deveksmod scramble assault ships
        public void ScrambleAssaultShips(float strengthNeeded)
        {
            bool flag = strengthNeeded > 0;
            foreach (ModuleSlot slot in this.ModuleSlotList.Where(slot => slot.module != null && slot.module.ModuleType == ShipModuleType.Hangar && slot.module.IsTroopBay && this.TroopList.Count > 0 && slot.module.GetHangarShip() == null && slot.module.hangarTimer <= 0f))
            {                
                if ( flag && strengthNeeded < 0)
                    break;
                strengthNeeded -= this.TroopList[0].Strength;
                slot.module.LaunchBoardingParty(this.TroopList[0]);
                
                this.TroopList.RemoveAt(0);
                
                
            }
        }

        public void RecoverAssaultShips()
        {
            for (int index = 0; index < this.Hangars.Count; ++index)
            {
                try
                {
                    ShipModule shipModule = this.Hangars[index];
                    if (shipModule.GetHangarShip() != null && shipModule.GetHangarShip().Active)
                    {
                        if (shipModule.IsTroopBay)
                        {
                            if (shipModule.GetHangarShip().TroopList.Count != 0)
                                shipModule.GetHangarShip().ReturnToHangar();
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public void ScrambleFighters()
        {
            for (int index = 0; index < this.Hangars.Count; ++index)
            {
                try
                {
                    this.Hangars[index].ScrambleFighters();
                }
                catch
                {
                }
            }
        }

        public void RecoverFighters()
        {
            for (int index = 0; index < this.Hangars.Count; ++index)
            {
                try
                {
                    ShipModule shipModule = this.Hangars[index];
                    if (shipModule.GetHangarShip() != null && shipModule.GetHangarShip().Active)
                        shipModule.GetHangarShip().ReturnToHangar();
                }
                catch
                {
                }
            }
        }

        public void LoadInitializeStatus()
        {
            this.Mass = 0.0f;
            this.Thrust = 0.0f;
            this.PowerStoreMax = 0.0f;
            this.PowerFlowMax = 0.0f;
            this.ModulePowerDraw = 0.0f;
            this.shield_max = 0.0f;
            this.shield_power = 0.0f;
            this.armor_max = 0.0f;
            //this.CrewRequired = 0;    //Not referenced in code, removing to save memory
            //this.CrewSupplied = 0;    //Not referenced in code, removing to save memory
            this.Size = 0;
            this.velocityMaximum = 0.0f;
            this.speed = 0.0f;
            this.OrdinanceMax = 0.0f;
            this.rotationRadiansPerSecond = 0.0f;
            this.Health = 0.0f;
            this.TroopCapacity = 0;
            this.MechanicalBoardingDefense = 0.0f;
            this.TroopBoardingDefense = 0.0f;
            this.ECMValue = 0.0f;
            this.FTLSpoolTime = 0f;
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                if (moduleSlot.Restrictions == Restrictions.I)
                    ++this.number_Internal_slots;
                if (moduleSlot.module.ModuleType == ShipModuleType.Dummy)
                    continue;
                if (moduleSlot.module.ECM > this.ECMValue)
                {
                    this.ECMValue = moduleSlot.module.ECM;
                    if (this.ECMValue > 1.0f)
                        this.ECMValue = 1.0f;
                    if (this.ECMValue < 0f)
                        this.ECMValue = 0f;
                }
                Ship ship1 = this;

                double num1 = ship1.Mass + moduleSlot.module.Mass;

                if (moduleSlot.module.ModuleType == ShipModuleType.Armor && this.loyalty != null)
                {
                    float modifiedMass = moduleSlot.module.Mass * this.loyalty.data.ArmourMassModifier;
                    num1 = (double)ship1.Mass + (double)modifiedMass;
                }
                ship1.Mass = (float)num1;
                this.Thrust += moduleSlot.module.thrust;
                this.WarpThrust += moduleSlot.module.WarpThrust;
                this.MechanicalBoardingDefense += moduleSlot.module.MechanicalBoardingDefense;
                //Added by McShooterz
                this.PowerStoreMax += this.loyalty.data.FuelCellModifier * moduleSlot.module.PowerStoreMax + moduleSlot.module.PowerStoreMax;
                this.PowerFlowMax += moduleSlot.module.PowerFlowMax + (this.loyalty != null ? moduleSlot.module.PowerFlowMax * this.loyalty.data.PowerFlowMod : 0);
                this.shield_max += moduleSlot.module.GetShieldsMax();
                this.shield_power += moduleSlot.module.shield_power;
                if (moduleSlot.module.ModuleType == ShipModuleType.Armor)
                    this.armor_max += moduleSlot.module.HealthMax;
                ++this.Size;
                this.CargoSpace_Max += moduleSlot.module.Cargo_Capacity;
                this.OrdinanceMax += (float)moduleSlot.module.OrdinanceCapacity;
                if (moduleSlot.module.ModuleType != ShipModuleType.Shield)
                    this.ModulePowerDraw += moduleSlot.module.PowerDraw;
                else
                    this.ShieldPowerDraw += moduleSlot.module.PowerDraw;
                Ship ship2 = this;
                double num2 = (double)ship2.Health + (double)moduleSlot.module.HealthMax;
                ship2.Health = (float)num2;
                this.TroopCapacity += (int)moduleSlot.module.TroopCapacity;
                if (moduleSlot.module.FTLSpoolTime > this.FTLSpoolTime)
                    this.FTLSpoolTime = moduleSlot.module.FTLSpoolTime;
            }
            this.MechanicalBoardingDefense += (float)(this.Size / 20);
            if ((double)this.MechanicalBoardingDefense < 1.0)
                this.MechanicalBoardingDefense = 1f;
            this.HealthMax = this.Health;
            this.velocityMaximum = this.Thrust / this.Mass;
            this.speed = this.velocityMaximum;
            this.rotationRadiansPerSecond = this.speed / 700f;
            this.number_Alive_Internal_slots = this.number_Internal_slots;
            this.ShipMass = this.Mass;
            if (this.FTLSpoolTime == 0)
                this.FTLSpoolTime = 3f;
        }

        public static Ship LoadSavedShip(ShipData data)
        {
            Ship parent = new Ship();
            //if (data.Name == "Left Right Test")
            //    parent.Position = new Vector2(200f, 200f);
            parent.Position = new Vector2(200f, 200f);
            parent.Name = data.Name;
            parent.Level = (int)data.Level;
            parent.shipData = data;
            parent.ModelPath = data.ModelPath;
            parent.ModuleSlotList = LoadSlotDataListToSlotList(data.ModuleSlotList, parent);
            foreach (var thrusterZone in data.ThrusterList)
                parent.ThrusterList.Add(new Thruster()
                {
                    tscale = thrusterZone.Scale,
                    XMLPos = thrusterZone.Position,
                    Parent = parent
                });
            return parent;
        }

        public static List<ModuleSlot> LoadSlotDataListToSlotList(List<ModuleSlotData> dataList, Ship parent)
        {
            var list = new List<ModuleSlot>(dataList.Count);
            foreach (ModuleSlotData slotData in dataList)
            {
                ModuleSlot moduleSlot = new ModuleSlot();
                moduleSlot.ModuleHealth = slotData.Health;
                moduleSlot.Shield_Power = slotData.Shield_Power;
                moduleSlot.Position     = slotData.Position;
                moduleSlot.facing       = slotData.facing;
                moduleSlot.state        = slotData.state;
                moduleSlot.Restrictions = slotData.Restrictions;
                moduleSlot.InstalledModuleUID = slotData.InstalledModuleUID;
                moduleSlot.HangarshipGuid     = slotData.HangarshipGuid;
                moduleSlot.SlotOptions        = slotData.SlotOptions;
                list.Add(moduleSlot);
            }
            return list;
        }

        public static Ship CreateShipFromShipData(ShipData data)
        {
            Ship parent = new Ship();
            parent.Position = new Vector2(200f, 200f);
            parent.Name = data.Name;
            parent.Level = data.Level;
            parent.experience = data.experience;
            parent.shipData   = data;
            parent.ModelPath  = data.ModelPath;
            parent.ModuleSlotList = SlotDataListToSlotList(data.ModuleSlotList, parent);
            
            foreach (var thrusterZone in data.ThrusterList)
                parent.ThrusterList.Add(new Thruster()
                {
                    tscale = thrusterZone.Scale,
                    XMLPos = thrusterZone.Position,
                    Parent = parent
                });
            return parent;
        }

        public static List<ModuleSlot> SlotDataListToSlotList(List<ModuleSlotData> dataList, Ship parent)
        {
            var list = new List<ModuleSlot>();
            foreach (ModuleSlotData moduleSlotData in dataList)
                list.Add(new ModuleSlot
                {
                    Position       = moduleSlotData.Position,
                    state          = moduleSlotData.state,
                    facing         = moduleSlotData.facing,
                    Restrictions   = moduleSlotData.Restrictions,
                    HangarshipGuid = moduleSlotData.HangarshipGuid,
                    InstalledModuleUID = moduleSlotData.InstalledModuleUID,
                    SlotOptions    = moduleSlotData.SlotOptions
                });
            return list;
        }

        public virtual void InitializeModules()
        {
            this.Weapons.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                moduleSlot.SetParent(this);
                moduleSlot.Initialize();
            }
            this.ModulesInitialized = true;
        }

        public bool InitFromSave()
        {
            this.SetShipData(this.GetShipData());
            this.ModulesInitialized = true;
            this.Weapons.Clear();
            List<ModuleSlot> list = new List<ModuleSlot>();
            if (this.Name == "Left Right Test")
                this.Weapons.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                moduleSlot.SetParent(this);
                if (!Ship_Game.ResourceManager.ShipModulesDict.ContainsKey(moduleSlot.InstalledModuleUID))
                    return false;
                moduleSlot.InitializeFromSave();
                if (moduleSlot.module == null)
                {
                    list.Add(moduleSlot);
                }
                else
                {
                    moduleSlot.module.Health = moduleSlot.ModuleHealth;
                    moduleSlot.module.shield_power = moduleSlot.Shield_Power;
                    if (moduleSlot.module.Health == 0.0)
                        moduleSlot.module.Active = false;
                }
            }
            foreach (ModuleSlot moduleSlot in list)
            {
                moduleSlot.Initialize();
                moduleSlot.module.Health = moduleSlot.ModuleHealth;
                moduleSlot.module.shield_power = moduleSlot.Shield_Power;
                if ((double)moduleSlot.module.Health == 0.0)
                    moduleSlot.module.Active = false;
            }
            this.RecalculatePower();
            return true;
        }

        public bool InitForLoad()
        {
            this.ModulesInitialized = true;
            this.Weapons.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                moduleSlot.SetParent(this);
                if (!ResourceManager.ShipModulesDict.ContainsKey(moduleSlot.InstalledModuleUID))
                {
                    Log.Warning("Ship {0} init failed, module {1} doesn't exist", Name, moduleSlot.InstalledModuleUID);
                    return false;
                }

                moduleSlot.InitializeForLoad();
                moduleSlot.module.Health = moduleSlot.ModuleHealth;
                moduleSlot.module.shield_power = moduleSlot.Shield_Power;
                if (!(moduleSlot.module.Health > 0f))
                    moduleSlot.module.Active = false;
            }
            RecalculatePower();
            return true;
        }

        //public virtual void LoadContent(ContentManager contentManager)
        //{
        //}

        public override void Update(float elapsedTime)
        {
            if (!Active)
                return;
            //if (!GlobalStats.WarpInSystem && this.system != null)
            //    this.InhibitedTimer = 1f;
            //else 
            //if (this.FTLmodifier < 1.0 && this.system != null && (this.engineState == Ship.MoveState.Warp && this.velocityMaximum < this.GetSTLSpeed() - 1 ))
            //{
            //    if (this.VanityName == "MerCraft") Log.Info("Break Hyperspace because of FTL Mod.  " + this.velocityMaximum + "  :  " + this.GetSTLSpeed());
            //    this.HyperspaceReturn();      //This section commented out because it was causing ships ot not be able ot warp at all if the FTL modifier was anything less than 1.0 -Gretman
            //}
            if (ScuttleTimer > -1.0 || ScuttleTimer <-1.0)
            {
                ScuttleTimer -= elapsedTime;
                if (ScuttleTimer <= 0.0)
                    Die(null, true);
            }
            if (System == null || System.isVisible)
            {
                BoundingSphere sphere = new BoundingSphere(new Vector3(this.Position, 0.0f), 2000f);
                if (universeScreen.Frustum.Contains(sphere) != ContainmentType.Disjoint && universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
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
            foreach (ProjectileTracker projectileTracker in ProjectilesFired)
            {
                projectileTracker.Timer -= elapsedTime;
                if (projectileTracker.Timer <= 0.0)
                    ProjectilesFired.QueuePendingRemoval(projectileTracker);
            }
            ProjectilesFired.ApplyPendingRemovals();
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
                if (dietimer <= 1.89999997615814 && dieCue == null && InFrustum)
                {
                    string cueName;
                    if      (Size < 80)  cueName = "sd_explosion_ship_warpdet_small";
                    else if (Size < 250) cueName = "sd_explosion_ship_warpdet_medium";
                    else                 cueName = "sd_explosion_ship_warpdet_large";
                    dieCue = AudioManager.PlayCue(cueName, universeScreen.listener, emitter);
                }
                if (dietimer <= 0.0)
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
                    universeScreen.flash.AddParticleThreadA(position, Vector3.Zero);
                }
                if (num1 >= 40)
                {
                    Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                    Ship.universeScreen.sparks.AddParticleThreadA(position, Vector3.Zero);
                }
                yRotation += xdie * elapsedTime;
                xRotation += ydie * elapsedTime;

                //Ship ship3 = this;
                //double num2 = (double)this.Rotation + (double)this.zdie * (double)elapsedTime;
                Rotation += zdie * elapsedTime;
                if (ShipSO == null)
                    return;
                if (universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && inSensorRange)
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
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile projectile = Projectiles[i];
                    if (projectile == null)
                        continue;
                    if (projectile.Active)
                        projectile.Update(elapsedTime);
                    else
                        Projectiles.QueuePendingRemoval(projectile);
                }
                projectiles.ApplyPendingRemovals();
                beams.ApplyPendingRemovals();
                emitter.Position = new Vector3(Center, 0);
                foreach (ModuleSlot moduleSlot in ModuleSlotList)
                {
                    moduleSlot.module.UpdateWhileDying(elapsedTime);
                }
            }
            else if (!dying)
            {
                if (System != null && elapsedTime > 0.0)
                {
                    foreach (Planet p in System.PlanetList)
                    {
                        if (p.Position.SqDist(Center) >= 3000f * 3000f)
                            continue;
                        if (p.ExploredDict[loyalty]) // already explored
                            continue;

                        if (loyalty == universeScreen.player)
                        {
                            foreach (Building building in p.BuildingList)
                                if (!string.IsNullOrEmpty(building.EventTriggerUID))
                                    universeScreen.NotificationManager.AddFoundSomethingInteresting(p);
                        }
                        p.ExploredDict[loyalty] = true;
                        foreach (Building building in p.BuildingList)
                        {
                            if (string.IsNullOrEmpty(building.EventTriggerUID) || 
                                loyalty == universeScreen.player || p.Owner != null) continue;

                            MilitaryTask militaryTask = new MilitaryTask
                            {
                                AO       = p.Position,
                                AORadius = 50000f,
                                type     = MilitaryTask.TaskType.Exploration
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
                    for (int i = 0; i < 5; ++i)
                    {
                        Vector3 randPos = UniverseRandom.Vector32D(third);
                        universeScreen.lightning.AddParticleThreadA(Center.ToVec3() + randPos, Vector3.Zero);
                    }
                }
                //Ship ship1 = this;
                //float num1 = this.Rotation + this.RotationalVelocity * elapsedTime;
                Rotation += RotationalVelocity * elapsedTime;
                if (Math.Abs(RotationalVelocity) > 0.0)
                    isTurning = true;

                if (!isSpooling && Afterburner != null && Afterburner.IsPlaying)
                    Afterburner.Stop(AudioStopOptions.Immediate);

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
                        if (AI.State == AIState.Combat && loyalty != universeScreen.player)
                        {
                            AI.State = AIState.AwaitingOrders;
                            AI.OrderQueue.Clear();
                        }
                    }
                    //this.Velocity.Length(); //Pretty sure this return value is a useless waste of 0.00012 CPU cycles... -Gretman
                    //Ship ship2 = this;
                    //Vector2 vector2_1 = this.Position + this.Velocity * elapsedTime;
                    Position += Velocity * elapsedTime;
                    //Ship ship3 = this;
                    //Vector2 vector2_2 = this.Center + this.Velocity * elapsedTime;
                    Center += Velocity * elapsedTime;
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
                        else if (this.shipData != null && this.animationController != null && this.shipData.Animated)
                        {
                            this.ShipSO.SkinBones = this.animationController.SkinnedBoneTransforms;
                            this.animationController.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                        }
                        foreach (Thruster thruster in this.ThrusterList)
                        {
                            thruster.SetPosition();
                            Vector2 vector2_3 = new Vector2((float)Math.Sin((double)this.Rotation), -(float)Math.Cos((double)this.Rotation));
                            vector2_3 = Vector2.Normalize(vector2_3);
                            float num2 = this.Velocity.Length() / this.velocityMaximum;
                            if (this.isThrusting)
                            {
                                if (this.engineState == Ship.MoveState.Warp)
                                {
                                    if (thruster.heat < num2)
                                        thruster.heat += 0.06f;
                                    this.pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                    this.scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                    thruster.update(thruster.WorldPos, this.pointat, this.scalefactors, thruster.heat, 0.004f, Color.OrangeRed, Color.LightBlue, Ship.universeScreen.camPos);
                                }
                                else
                                {
                                    if (thruster.heat < num2)
                                        thruster.heat += 0.06f;
                                    if (thruster.heat > 0.600000023841858)
                                        thruster.heat = 0.6f;
                                    this.pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                    this.scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                    thruster.update(thruster.WorldPos, this.pointat, this.scalefactors, thruster.heat, 1.0f / 500.0f, Color.OrangeRed, Color.LightBlue, Ship.universeScreen.camPos);
                                }
                            }
                            else
                            {
                                this.pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                this.scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                thruster.heat = 0.01f;
                                thruster.update(thruster.WorldPos, this.pointat, this.scalefactors, 0.1f, 1.0f / 500.0f, Color.OrangeRed, Color.LightBlue, Ship.universeScreen.camPos);
                            }
                        }
                    }
                    if (this.isSpooling)
                        this.fightersOut = false;
                    if (this.isSpooling && !this.Inhibited && this.GetmaxFTLSpeed >2500)
                    {
                        this.JumpTimer -= elapsedTime;
                        //task gremlin move fighter recall here.

                        if (this.JumpTimer <= 4.0) // let's see if we can sync audio to behaviour with new timers
                        {
                            if (Vector2.Distance(this.Center, new Vector2(Ship.universeScreen.camPos.X, Ship.universeScreen.camPos.Y)) < 100000.0 && (this.Jump == null || this.Jump != null && !this.Jump.IsPlaying) && Ship.universeScreen.camHeight < 250000)
                            {
                                this.Jump = AudioManager.GetCue(this.GetStartWarpCue());                                                               
                                this.Jump.Apply3D(GameplayObject.audioListener, this.emitter);
                                this.Jump.Play();
                                
                            }
                        }
                        if (this.JumpTimer <= 0.1)
                        {
                            if (this.engineState == Ship.MoveState.Sublight )//&& (!this.Inhibited && this.GetmaxFTLSpeed > this.velocityMaximum))
                            {
                                FTLManager.AddFTL(Center);
                                this.engineState = Ship.MoveState.Warp;
                            }
                            else
                                this.engineState = Ship.MoveState.Sublight;
                            this.isSpooling = false;
                            this.ResetJumpTimer();
                        }
                    }
                    if (this.isPlayerShip())
                    {
                        if ((!this.isSpooling || !this.Active) && this.Afterburner != null)
                        {
                            if (this.Afterburner.IsPlaying)
                                this.Afterburner.Stop(AudioStopOptions.Immediate);
                            this.Afterburner = (Cue)null;
                        }
                        if (this.isThrusting && this.drone == null && this.AI.State == AIState.ManualControl)
                        {
                            this.drone = AudioManager.GetCue("starcruiser_drone01");
                            this.drone.Play();
                        }
                        else if ((!this.isThrusting || !this.Active) && this.drone != null)
                        {
                            if (this.drone.IsPlaying)
                                this.drone.Stop(AudioStopOptions.Immediate);
                            this.drone = (Cue)null;
                        }
                    }
                    this.emitter.Position = new Vector3(this.Center, 0);
                    
                }
                if (elapsedTime > 0.0f)
                {
                    var source = Enumerable.Range(0, 0).ToArray();
                    var rangePartitioner = Partitioner.Create(0, 1);
                     

                    if (this.projectiles.Count >0)
                    {
                          source = Enumerable.Range(0, this.projectiles.Count).ToArray();
                          rangePartitioner = Partitioner.Create(0, source.Length);
                        //handle each weapon group in parallel
                        Parallel.ForEach(rangePartitioner, (range, loopState) =>
                        {
                            //standard for loop through each weapon group.
                            for (int T = range.Item1; T < range.Item2; T++)
                            {
                                Projectile projectile = this.projectiles[T];
                                //Parallel.ForEach<Projectile>(this.projectiles, projectile =>
                                //{
                                if (projectile != null && projectile.Active)
                                    projectile.Update(elapsedTime);
                                else
                                {
                                    // projectile.Die(null, true);
                                    this.Projectiles.QueuePendingRemoval(projectile);
                                }
                            }
                        }); 
                    }

                    if (this.beams.Count >0)
                    {
                        source = Enumerable.Range(0, this.beams.Count).ToArray();
                        rangePartitioner = Partitioner.Create(0, source.Length);
                        //handle each weapon group in parallel
                        Parallel.ForEach(rangePartitioner, (range, loopState) =>
                        {
                            //standard for loop through each weapon group.
                            for (int T = range.Item1; T < range.Item2; T++)
                            {
                                Beam beam = this.beams[T];
                                Vector2 origin = new Vector2();
                                if (beam.moduleAttachedTo != null)
                                {
                                    ShipModule shipModule = beam.moduleAttachedTo;
                                    origin = (int)shipModule.XSIZE != 1
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
                                    int thickness = (int)UniverseRandom.RandomBetween(beam.thickness*0.75f, beam.thickness*1.1f);

                                    beam.Update(beam.moduleAttachedTo != null ? origin : beam.owner.Center, 
                                        beam.followMouse ? universeScreen.mouseWorldPos : beam.Destination, 
                                        thickness, universeScreen.view, universeScreen.projection, elapsedTime);

                                    if (beam.duration < 0f && !beam.infinite)
                                    {
                                        beam.Die(null, false);
                                        this.beams.QueuePendingRemoval(beam);
                                    }
                                }
                                else
                                {
                                    beam.Die(null, false);
                                    this.beams.QueuePendingRemoval(beam);
                                }
                            }

                        }); 
                    }
                    //this.beams.thisLock.ExitReadLock();

                    this.beams.ApplyPendingRemovals() ; //this.GetAI().BadGuysNear && (this.InFrustum || GlobalStats.ForceFullSim));
                    //foreach (Projectile projectile in this.projectiles.pendingRemovals)
                    //    projectile.Die(null,false);
                    this.Projectiles.ApplyPendingRemovals(GetAI().BadGuysNear && (InFrustum || GlobalStats.ForceFullSim));//this.GetAI().BadGuysNear && (this.InFrustum || GlobalStats.ForceFullSim));
                }
            }
        }

        private void CheckAndPowerConduit(ModuleSlot slot)
        {
            if (!slot.module.Active)
                return;
            slot.module.Powered = true;
            slot.CheckedConduits = true;
            foreach (ModuleSlot slot1 in this.ModuleSlotList)
            {
                if (slot1 != slot && (int)Math.Abs(slot.Position.X - slot1.Position.X) / 16 + (int)Math.Abs(slot.Position.Y - slot1.Position.Y) / 16 == 1 && (slot1.module != null && slot1.module.ModuleType == ShipModuleType.PowerConduit) && (slot1.module.ModuleType == ShipModuleType.PowerConduit && !slot1.CheckedConduits))
                    this.CheckAndPowerConduit(slot1);
            }
        }

        public void RecalculatePower()
        {
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                moduleSlot.Powered = false;
                moduleSlot.module.Powered = false;
                moduleSlot.CheckedConduits = false;
                if (moduleSlot.module != null)
                    moduleSlot.module.Powered = false;
            }
            //added by Gremlin Parallel recalculate power.
            Parallel.ForEach<ModuleSlot>(this.ModuleSlotList, moduleSlot =>
            //foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                if (moduleSlot.module != null && moduleSlot.module.ModuleType == ShipModuleType.PowerPlant && moduleSlot.module.Active)
                {
                    foreach (ModuleSlot slot in this.ModuleSlotList)
                    {
                        if (slot.module != null && slot.module.ModuleType == ShipModuleType.PowerConduit && ((int)Math.Abs(slot.Position.X - moduleSlot.Position.X) / 16 + (int)Math.Abs(slot.Position.Y - moduleSlot.Position.Y) / 16 == 1 && slot.module != null))
                            this.CheckAndPowerConduit(slot);
                    }
                }
                else if (moduleSlot.module.ParentOfDummy != null && moduleSlot.module.ParentOfDummy.ModuleType == ShipModuleType.PowerPlant && moduleSlot.module.ParentOfDummy.Active)
                {
                    foreach (ModuleSlot slot in this.ModuleSlotList)
                    {
                        if (slot.module != null && slot.module.ModuleType == ShipModuleType.PowerConduit && ((int)Math.Abs(slot.Position.X - moduleSlot.Position.X) / 16 + (int)Math.Abs(slot.Position.Y - moduleSlot.Position.Y) / 16 == 1 && slot.module != null))
                            this.CheckAndPowerConduit(slot);
                    }
                }
            });
            
            //foreach (ModuleSlot moduleSlot1 in this.ModuleSlotList)
            Parallel.ForEach<ModuleSlot>(this.ModuleSlotList, moduleSlot1 =>
            {
                if (!moduleSlot1.isDummy && moduleSlot1.module != null && ((int)moduleSlot1.module.PowerRadius > 0 && moduleSlot1.module.Active) && (moduleSlot1.module.ModuleType != ShipModuleType.PowerConduit || moduleSlot1.module.Powered))
                {
                    foreach (ModuleSlot moduleSlot2 in this.ModuleSlotList)
                    {
                        if ((int)Math.Abs(moduleSlot1.Position.X - moduleSlot2.Position.X) / 16 + (int)Math.Abs(moduleSlot1.Position.Y - moduleSlot2.Position.Y) / 16 <= (int)moduleSlot1.module.PowerRadius)
                            moduleSlot2.Powered = true;
                    }
                    if ((int)moduleSlot1.module.XSIZE > 1 || (int)moduleSlot1.module.YSIZE > 1)
                    {
                        for (int index1 = 0; index1 < (int)moduleSlot1.module.YSIZE; ++index1)
                        {
                            for (int index2 = 0; index2 < (int)moduleSlot1.module.XSIZE; ++index2)
                            {
                                if (!(index2 == 0 & index1 == 0))
                                {
                                    foreach (ModuleSlot moduleSlot2 in this.ModuleSlotList)
                                    {
                                        if ((double)moduleSlot2.Position.Y == (double)moduleSlot1.Position.Y + (double)(16 * index1) && (double)moduleSlot2.Position.X == (double)moduleSlot1.Position.X + (double)(16 * index2))
                                        {
                                            foreach (ModuleSlot moduleSlot3 in this.ModuleSlotList)
                                            {
                                                if ((int)Math.Abs(moduleSlot2.Position.X - moduleSlot3.Position.X) / 16 + (int)Math.Abs(moduleSlot2.Position.Y - moduleSlot3.Position.Y) / 16 <= (int)moduleSlot1.module.PowerRadius)
                                                    moduleSlot3.Powered = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                if (moduleSlot.Powered)
                {
                    if (moduleSlot.module != null && moduleSlot.module.ModuleType != ShipModuleType.PowerConduit)
                        moduleSlot.module.Powered = true;
                    if (moduleSlot.module.isDummy && moduleSlot.module.ParentOfDummy != null)
                        moduleSlot.module.ParentOfDummy.Powered = true;                    
                }
                if (!moduleSlot.Powered && moduleSlot.module != null && moduleSlot.module.IndirectPower)
                    moduleSlot.module.Powered = true;
            }
        }

        public ShipData ToShipData()
        {
            ShipData shipData = new ShipData();
            shipData.BaseCanWarp = this.shipData.BaseCanWarp;
            shipData.BaseStrength = this.BaseStrength;
            shipData.techsNeeded = this.shipData.techsNeeded;
            shipData.TechScore = this.shipData.TechScore;
            shipData.ShipCategory = this.shipData.ShipCategory;
            shipData.Name = this.Name;
            shipData.Level = (byte)this.Level;
            shipData.experience = (byte)this.experience;
            shipData.Role = this.shipData.Role;
            shipData.IsShipyard = this.GetShipData().IsShipyard;
            shipData.IsOrbitalDefense = this.GetShipData().IsOrbitalDefense;
            shipData.Animated = this.GetShipData().Animated;
            shipData.CombatState = this.GetAI().CombatState;
            shipData.ModelPath = this.GetShipData().ModelPath;
            shipData.ModuleSlotList = this.ConvertToData(ModuleSlotList);
            shipData.ThrusterList = new List<ShipToolScreen.ThrusterZone>();
            shipData.MechanicalBoardingDefense = this.MechanicalBoardingDefense;
            foreach (Thruster thruster in this.ThrusterList)
                shipData.ThrusterList.Add(new ShipToolScreen.ThrusterZone()
                {
                    Scale = thruster.tscale,
                    Position = thruster.XMLPos
                });
            return shipData;
        }

        private List<ModuleSlotData> ConvertToData(List<ModuleSlot> slotList)
        {
            List<ModuleSlotData> list = new List<ModuleSlotData>();
            foreach (ModuleSlot moduleSlot in slotList)
            {
                ModuleSlotData moduleSlotData = new ModuleSlotData
                {
                    Position = moduleSlot.Position,
                    InstalledModuleUID = moduleSlot.InstalledModuleUID
                };
                if (moduleSlot.HangarshipGuid != Guid.Empty)
                    moduleSlotData.HangarshipGuid = moduleSlot.HangarshipGuid;
                moduleSlotData.Restrictions = moduleSlot.Restrictions;
                if (moduleSlot.module.ModuleType == ShipModuleType.Hangar)
                    moduleSlotData.SlotOptions = moduleSlot.module.hangarShipUID;
                moduleSlotData.facing = moduleSlot.module.facing;
                moduleSlotData.Health = moduleSlot.module.Health;
                moduleSlotData.Shield_Power = moduleSlot.module.shield_power;
                moduleSlotData.state = moduleSlot.state;
                list.Add(moduleSlotData);
            }
            return list;
        }

        public float CalculateRange()
        {
            return 200000f;
        }

        private string GetConduitGraphic(ModuleSlot ss, Ship ship)
        {
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            int num1 = 0;
            foreach (ModuleSlot moduleSlot in ship.ModuleSlotList)
            {
                if (moduleSlot.module != null && moduleSlot.module.ModuleType == ShipModuleType.PowerConduit && moduleSlot != ss)
                {
                    int num2 = (int)Math.Abs(moduleSlot.module.XMLPosition.X - ss.module.XMLPosition.X) / 16;
                    int num3 = (int)Math.Abs(moduleSlot.module.XMLPosition.Y - ss.module.XMLPosition.Y) / 16;
                    if (num2 == 1 && num3 == 0)
                    {
                        if ((double)moduleSlot.module.XMLPosition.X > (double)ss.module.XMLPosition.X)
                            flag1 = true;
                        else
                            flag2 = true;
                    }
                    if (num3 == 1 && num2 == 0)
                    {
                        if ((double)moduleSlot.module.XMLPosition.Y > (double)ss.module.XMLPosition.Y)
                            flag4 = true;
                        else
                            flag3 = true;
                    }
                }
            }
            if (flag2)
                ++num1;
            if (flag1)
                ++num1;
            if (flag3)
                ++num1;
            if (flag4)
                ++num1;
            if (num1 <= 1)
            {
                if (flag3)
                    return "Conduits/conduit_powerpoint_down";
                if (flag4)
                    return "Conduits/conduit_powerpoint_up";
                if (flag2)
                    return "Conduits/conduit_powerpoint_right";
                return flag1 ? "Conduits/conduit_powerpoint_left" : "Conduits/conduit_intersection";
            }
            else
            {
                if (num1 == 3)
                {
                    if (flag3 && flag4 && flag2)
                        return "Conduits/conduit_tsection_left";
                    if (flag3 && flag4 && flag1)
                        return "Conduits/conduit_tsection_right";
                    if (flag2 && flag1 && flag4)
                        return "Conduits/conduit_tsection_down";
                    if (flag2 && flag1 && flag3)
                        return "Conduits/conduit_tsection_up";
                }
                else
                {
                    if (num1 == 4)
                        return "Conduits/conduit_intersection";
                    if (num1 == 2)
                    {
                        if (flag2 && flag3)
                            return "Conduits/conduit_corner_BR";
                        if (flag2 && flag4)
                            return "Conduits/conduit_corner_TR";
                        if (flag1 && flag3)
                            return "Conduits/conduit_corner_BL";
                        if (flag1 && flag4)
                            return "Conduits/conduit_corner_TL";
                        if (flag3 && flag4)
                            return "Conduits/conduit_straight_vertical";
                        if (flag2 && flag1)
                            return "Conduits/conduit_straight_horizontal";
                    }
                }
                return "";
            }
        }

        public List<ShipModule> GetShields()
        {
            return this.Shields;
        }

        public List<ShipModule> GetHangars()
        {
            return this.Hangars;
        }

        public bool DoneRecovering()
        {
            for (int index = 0; index < this.Hangars.Count; ++index)
            {
                try
                {
                    ShipModule shipModule = this.Hangars[index];
                    if (shipModule.GetHangarShip() != null)
                    {
                        if (shipModule.GetHangarShip().Active)
                            return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        public virtual void UpdateShipStatus(float elapsedTime)
        {
            if (elapsedTime == 0.0f)
                return;
            
            
            if (this.velocityMaximum == 0f && this.shipData.Role <= ShipData.RoleName.station)
            {
                this.Rotation += 0.003f;
            }
            this.MoveModulesTimer -= elapsedTime;
            this.updateTimer -= elapsedTime;
            //Disable if enough EMP damage
            if (this.EMPDamage > 0 || this.disabled)
            {
                --this.EMPDamage;
                if (this.EMPDamage < 0.0)
                    this.EMPDamage = 0.0f;

                if (this.EMPDamage > this.Size + this.BonusEMP_Protection)
                    this.disabled = true;
                else
                    this.disabled = false;
            }
            //this.CargoMass = 0.0f;    //Not referenced in code, removing to save memory
            if (this.Rotation > 2.0 * Math.PI)
            {
                //Ship ship = this;
                //float num = ship.rotation - 6.28318548202515f;
                this.Rotation -= 6.28318548202515f;
            }
            if (this.Rotation < 0.0)
            {
                //Ship ship = this;
                //float num = ship.rotation + 6.28318548202515f;
                this.Rotation += 6.28318548202515f;
            }
            if (this.InCombat && !this.disabled && this.hasCommand || this.PlayerShip)
            {
                foreach (Weapon weapon in this.Weapons)
                    weapon.Update(elapsedTime);
            }
            this.TroopBoardingDefense = 0.0f;
            foreach (Troop troop in this.TroopList)
            {
                troop.SetShip(this);
                if (troop.GetOwner() == this.loyalty)
                    this.TroopBoardingDefense += troop.Strength;
            }
            if (this.updateTimer <= 0.0 ) //|| shipStatusChanged)
            {
                if ((this.InCombat && !this.disabled && this.hasCommand || this.PlayerShip) && this.Weapons.Count > 0)
                {
                    IOrderedEnumerable<Weapon> orderedEnumerable;
                    if(this.GetAI().CombatState == CombatState.ShortRange)
                        orderedEnumerable = Enumerable.OrderBy<Weapon, float>((IEnumerable<Weapon>)this.Weapons, (Func<Weapon, float>)(weapon => weapon.GetModifiedRange()));
                    else
                        orderedEnumerable = Enumerable.OrderByDescending<Weapon, float>((IEnumerable<Weapon>)this.Weapons, (Func<Weapon, float>)(weapon => weapon.GetModifiedRange()));
                    bool flag = false;
                    foreach (Weapon weapon in (IEnumerable<Weapon>)orderedEnumerable)
                    {
                        //Edited by Gretman
                        //This fixes ships with only 'other' damage types thinking it has 0 range, causing them to fly through targets even when set to attack at max/min range
                        if (!flag && (weapon.DamageAmount > 0.0 || weapon.EMPDamage > 0.0 || weapon.SiphonDamage > 0.0 || weapon.MassDamage > 0.0 || weapon.PowerDamage > 0.0 || weapon.RepulsionDamage > 0.0))
                        {
                            this.maxWeaponsRange = weapon.GetModifiedRange();
                            if (!weapon.Tag_PD) flag = true;
                        }

                        weapon.fireDelay = Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay;
                        //Added by McShooterz: weapon tag modifiers with check if mod uses them
						if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useWeaponModifiers)
                        {
                            if (weapon.Tag_Beam)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Beam"].Rate;
                            if (weapon.Tag_Energy)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Energy"].Rate;
                            if (weapon.Tag_Explosive)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Explosive"].Rate;
                            if (weapon.Tag_Guided)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Guided"].Rate;
                            if (weapon.Tag_Hybrid)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Hybrid"].Rate;
                            if (weapon.Tag_Intercept)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Intercept"].Rate;
                            if (weapon.Tag_Kinetic)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Kinetic"].Rate;
                            if (weapon.Tag_Missile)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Missile"].Rate;
                            if (weapon.Tag_Railgun)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Railgun"].Rate;
                            if (weapon.Tag_Torpedo)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Torpedo"].Rate;
                            if (weapon.Tag_Cannon)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Cannon"].Rate;
                            if (weapon.Tag_Subspace)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Subspace"].Rate;
                            if (weapon.Tag_PD)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["PD"].Rate;
                            if (weapon.Tag_Bomb)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Bomb"].Rate;
                            if (weapon.Tag_SpaceBomb)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Spacebomb"].Rate;
                            if (weapon.Tag_BioWeapon)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["BioWeapon"].Rate;
                            if (weapon.Tag_Drone)
                                weapon.fireDelay += -Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Drone"].Rate;
                            if (weapon.Tag_Warp)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Warp"].Rate;
                            if (weapon.Tag_Array)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Array"].Rate;
                            if (weapon.Tag_Flak)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Flak"].Rate;
                            if (weapon.Tag_Tractor)
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Tractor"].Rate;
                        }
                        //Added by McShooterz: Hull bonus Fire Rate
						if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                        {
                            HullBonus mod;
                            if (Ship_Game.ResourceManager.HullBonuses.TryGetValue(this.shipData.Hull, out mod))
                                weapon.fireDelay *= 1f - mod.FireRateBonus;
                        }
                    }
                }

                try
                {
                    if(this.InhibitedTimer <2f)
                    foreach (Empire index1 in EmpireManager.EmpireList)
                    {
                        if (index1 != this.loyalty && !this.loyalty.GetRelations(index1).Treaty_OpenBorders)
                        {
                            for (int index2 = 0; index2 < index1.Inhibitors.Count; ++index2)
                            {
                                Ship ship = index1.Inhibitors[index2];
                                if (ship != null && Vector2.Distance(this.Center, ship.Position) <= ship.InhibitionRadius)
                                {
                                    this.Inhibited = true;
                                    this.InhibitedTimer = 5f;
                                    break;
                                }
                            }
                            if (this.Inhibited)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "Inhibitor blew up");
                }
                this.inSensorRange = false;
                if (Ship.universeScreen.Debug || this.loyalty == Ship.universeScreen.player || this.loyalty != Ship.universeScreen.player && Ship.universeScreen.player.GetRelations(loyalty).Treaty_Alliance)
                    this.inSensorRange = true;
                else if (!this.inSensorRange)
                {
                    List<GameplayObject> nearby = UniverseScreen.ShipSpatialManager.GetNearby((GameplayObject)this);
                    for (int index = 0; index < nearby.Count; ++index)
                    //Parallel.For(0, nearby.Count, (index,status) =>
                    {
                        Ship ship = nearby[index] as Ship;
                        if (ship != null && ship.loyalty == Ship.universeScreen.player && ((double)Vector2.Distance(ship.Position, this.Center) <= (double)ship.SensorRange || Ship.universeScreen.Debug))
                        {
                            this.inSensorRange = true;
                            break;
                            //status.Stop();
                            //return;
                        }
                    }//);
                }
                if (this.shipStatusChanged || this.InCombat)
                    this.ShipStatusChange();
                //Power draw based on warp
                if (!this.inborders && this.engineState == Ship.MoveState.Warp)
                {
                    this.PowerDraw = (this.loyalty.data.FTLPowerDrainModifier * this.ModulePowerDraw) + (this.WarpDraw * this.loyalty.data.FTLPowerDrainModifier / 2);
                }
                else if (this.engineState != Ship.MoveState.Warp && this.ShieldsUp)
                    this.PowerDraw = this.ModulePowerDraw + this.ShieldPowerDraw;
                else
                    this.PowerDraw = this.ModulePowerDraw;

                //This is what updates all of the modules of a ship
                if (this.loyalty.RecalculateMaxHP) this.HealthMax = 0;
                foreach (ModuleSlot slot in this.ModuleSlotList)
                    slot.module.Update(1f);
                //Check Current Shields
                if (this.engineState == Ship.MoveState.Warp || !this.ShieldsUp)
                    this.shield_power = 0f;
                else
                {
                    if (this.InCombat || this.shield_power != this.shield_max)
                    {
                        this.shield_power = 0.0f;
                        foreach (ShipModule shield in this.Shields)
                            this.shield_power += shield.shield_power;
                        if (this.shield_power > this.shield_max)
                            this.shield_power = this.shield_max;
                    }
                }
                //Add ordnance
                if (this.Ordinance < this.OrdinanceMax)
                {
                    this.Ordinance += this.OrdAddedPerSecond;
                    if(this.Ordinance > this.OrdinanceMax)
                        this.Ordinance = this.OrdinanceMax;
                }
                else
                    this.Ordinance = this.OrdinanceMax;
                //Repair
                if (this.Health < this.HealthMax)
                {
                    this.shipStatusChanged = true;
					if (!this.InCombat || GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useCombatRepair)
                    {
                        //Added by McShooterz: Priority repair
                        float repairTracker = this.InCombat ? this.RepairRate * 0.1f : this.RepairRate;
                        IEnumerable<ModuleSlot> damagedModules = this.ModuleSlotList.Where(moduleSlot => moduleSlot.module.ModuleType != ShipModuleType.Dummy && moduleSlot.module.Health < moduleSlot.module.HealthMax).OrderBy(moduleSlot => HelperFunctions.ModulePriority(moduleSlot.module)).AsEnumerable();
                        foreach (ModuleSlot moduleSlot in damagedModules)
                        {
                            //if destroyed do not repair in combat
                            if (this.InCombat && moduleSlot.module.Health < 1)
                                continue;
                            if (moduleSlot.module.HealthMax - moduleSlot.module.Health > repairTracker)
                            {
                                moduleSlot.module.Repair(repairTracker);
                                break;
                            }
                            else
                            {
                                repairTracker -= moduleSlot.module.HealthMax - moduleSlot.module.Health;
                                moduleSlot.module.Repair(moduleSlot.module.HealthMax);
                            }
                        }
                    }
                }
                else
                {
                    this.shipStatusChanged = false;
                }
                List<Troop> OwnTroops = new List<Troop>();
                List<Troop> EnemyTroops = new List<Troop>();
                foreach (Troop troop in this.TroopList)
                {
                    if (troop.GetOwner() == this.loyalty)
                        OwnTroops.Add(troop);
                    else
                        EnemyTroops.Add(troop);
                }
                if (this.HealPerTurn > 0)
                {
                    foreach (Troop troop in OwnTroops)
                    {
                        if (troop.Strength < troop.GetStrengthMax())
                        {
                            troop.Strength += this.HealPerTurn;
                        }
                        else
                            troop.Strength = troop.GetStrengthMax();
                    }
                }
                if (EnemyTroops.Count > 0)
                {
                    float num1 = 0;
                    for (int index = 0; index < this.MechanicalBoardingDefense; ++index)
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
                                this.TroopList.Remove(troop);
                        }
                        else
                            break;
                    }
                    EnemyTroops.Clear();
                    foreach (Troop troop in this.TroopList)
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
                                    this.TroopList.Remove(troop);
                                if (num1 <= 0)
                                    break;
                            }
                            else
                                break;
                        }
                    }
                    EnemyTroops.Clear();
                    foreach (Troop troop in this.TroopList)
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
                                    this.TroopList.Remove(troop);
                            }
                            else
                                break;
                        }
                        if (num2 > 0)
                        {
                            this.MechanicalBoardingDefense -= (float)num2;
                            if (this.MechanicalBoardingDefense < 0.0)
                                this.MechanicalBoardingDefense = 0.0f;
                        }
                    }
                    OwnTroops.Clear();
                    foreach (Troop troop in this.TroopList)
                    {
                        if (troop.GetOwner() == this.loyalty)
                            OwnTroops.Add(troop);
                    }
                    if (OwnTroops.Count == 0 && this.MechanicalBoardingDefense <= 0.0)
                    {
                        this.loyalty.GetShips().QueuePendingRemoval(this);
                        this.loyalty = EnemyTroops[0].GetOwner();
                        this.loyalty.AddShipNextFrame(this);
                        if (this.fleet != null)
                        {
                            this.fleet.Ships.Remove(this);
                            this.RemoveFromAllFleets();                                                        
                            this.fleet = (Fleet)null;
                        }
                        this.AI.ClearOrdersNext = true;
                        this.AI.State = AIState.AwaitingOrders;
                    }
                }
                //this.UpdateSystem(elapsedTime);
                this.updateTimer = 1f;
                if (this.NeedRecalculate)
                {
                    this.RecalculatePower();
                    this.NeedRecalculate = false;
                }
               
            }
            else if (this.Active && this.GetAI().BadGuysNear || (this.InFrustum && Ship.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView )|| this.MoveModulesTimer > 0.0 ||  GlobalStats.ForceFullSim) // || (Ship.universeScreen !=null && Ship.universeScreen.Lag <= .03f)))
            {
                if (elapsedTime > 0.0)
                {
                    //if (this.Velocity != Vector2.Zero)
                    //this.UpdatedModulesOnce = false;
                    if (this.GetAI().BadGuysNear ||  this.Velocity != Vector2.Zero || this.isTurning || this.TetheredTo != null || this.shipData.Role <= ShipData.RoleName.station)
                    {
                        this.UpdatedModulesOnce = false;
                      
                        //int half = this.ModuleSlotList.Count / 2;

                        //List<ModuleSlot> firsthalf = this.ModuleSlotList.Skip(half).ToList();
                        //List<ModuleSlot> Secondhalf = this.ModuleSlotList.Reverse().Skip(this.ModuleSlotList.Count - half).ToList();

                        //foreach (ModuleSlot slots in this.ModuleSlotList)
                        //{
                        //    if (half > 0)
                        //        firsthalf.Add(slots.module);
                        //    else
                        //        Secondhalf.Add(slots.module);
                        //    half--;
                        //}

                        //Parallel.Invoke(() =>
                        //{
                        //    foreach (ModuleSlot moduleSlot in firsthalf)
                        //    {
                        //        ++GlobalStats.ModuleUpdates;
                        //        moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                        //    }

                        //},
                        //     () =>
                        //     {

                        //         foreach (ModuleSlot moduleSlot in Secondhalf)
                        //         {
                        //             ++GlobalStats.ModuleUpdates;
                        //             moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                        //         }
                        //     }

                        //     );

                        //if I am not mistaken, this is being run completely twice. The two Parallel foreach loops above are derived from 'this.ModuleSlotList' which
                        //is processed in its entirety again here. I think this is redundant, and likely a reasonable performance hit.    -Gretman
                        Task modules = new Task(() =>
                        {
                            float cos = (float)Math.Cos((double)this.Rotation);
                            float sin = (float)Math.Sin((double)this.Rotation);
                            float tan = (float)Math.Tan((double)this.yRotation);
                            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                            {
                                ++GlobalStats.ModuleUpdates;
                                moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                                if (!this.Active)
                                    break;
                            }
                        }
                        );
                        modules.Start();
                    }
                    else if( !this.UpdatedModulesOnce)
                    {
                        Task modules = new Task(() =>
                        {

                            float cos = (float)Math.Cos((double)this.Rotation);
                            float sin = (float)Math.Sin((double)this.Rotation);
                            float tan = (float)Math.Tan((double)this.yRotation);
                            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                            {                                
                                ++GlobalStats.ModuleUpdates;
                                moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                                if (!this.Active)
                                    break;
                            }
                        }); modules.Start();
                        this.UpdatedModulesOnce = true;
                    }
                }
                else if (elapsedTime < 0.0 && !this.UpdatedModulesOnce)
                {
                   
                    Task modules = new Task(() =>
                    {
                        float cos = (float)Math.Cos((double)this.Rotation);
                        float sin = (float)Math.Sin((double)this.Rotation);
                        float tan = (float)Math.Tan((double)this.yRotation);
                        foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                        {
                            ++GlobalStats.ModuleUpdates;
                            moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                            if (!this.Active)
                                break;
                        }
                    }); modules.Start();
                    this.UpdatedModulesOnce = true;
                }
            }
            this.SetmaxFTLSpeed();
            if (this.Ordinance > this.OrdinanceMax)
                this.Ordinance = this.OrdinanceMax;
            this.percent = this.number_Alive_Internal_slots / this.number_Internal_slots;
            if (this.percent < 0.35)
                this.Die(this.LastDamagedBy, false);
            if (this.Mass < (this.Size / 2))
                this.Mass = (this.Size / 2);
            this.PowerCurrent -= this.PowerDraw * elapsedTime;
            if (this.PowerCurrent < this.PowerStoreMax)
                this.PowerCurrent += (this.PowerFlowMax + (this.PowerFlowMax *this.loyalty?.data.PowerFlowMod ?? 0)) * elapsedTime;
            //if (this.ResourceDrawDict.Count > 0)
            //{

            //    //foreach (KeyValuePair<string, float> draw in this.ResourceDrawDict)
            //    //{
            //    //    string index1 = draw.Key;
            //    //    float drawvalue = draw.Value;
            //    //    if (drawvalue <= 0 || this.CargoDict[index1] <= 0.0f)
            //    //        continue;
            //    //    float store = this.CargoDict[index1];
            //    //    store -= drawvalue * elapsedTime;

            //    //    if (store < 0)
            //    //        store = 0;
            //    //    this.CargoDict[index1] = store;
            //    //}
            //    foreach (string index1 in Enumerable.ToList<string>((IEnumerable<string>)this.ResourceDrawDict.Keys))
            //    {
            //        Dictionary<string, float> dictionary;
            //        string index2;
            //        (dictionary = this.CargoDict)[index2 = index1] = dictionary[index2] - this.ResourceDrawDict[index1] * elapsedTime;
            //        if ((double)this.CargoDict[index1] <= 0.0)
            //            this.CargoDict[index1] = 0.0f;
            //    }
            //}
            if (this.PowerCurrent <= 0.0)
            {
                this.PowerCurrent = 0.0f;
                this.HyperspaceReturn();
            }
            if (this.PowerCurrent > this.PowerStoreMax)
                this.PowerCurrent = this.PowerStoreMax;
            if (this.shield_percent < 0.0f)
                this.shield_percent = 0.0f;
            this.shield_percent = 100.0 * this.shield_power / this.shield_max;
            if (this.shield_percent < 0.0f)
                this.shield_percent = 0.0f;
            if (this.Mass <= 0.0f)
                this.Mass = 1f;
            switch (this.engineState)
            {
                case Ship.MoveState.Sublight:
                    this.velocityMaximum = this.GetSTLSpeed();
                    break;
                case Ship.MoveState.Warp:
                    this.velocityMaximum = this.GetmaxFTLSpeed;
                    break;
            }
   
            this.speed = this.velocityMaximum;
            this.rotationRadiansPerSecond = this.TurnThrust / this.Mass / 700f;
            this.rotationRadiansPerSecond += (float)(this.rotationRadiansPerSecond * this.Level * 0.0500000007450581);
            this.yBankAmount = this.rotationRadiansPerSecond * elapsedTime;// 50f;
            if (this.engineState == Ship.MoveState.Warp)
            {
                //if (this.FTLmodifier != 1f)
                    //this.velocityMaximum *= this.FTLmodifier;
                this.Velocity = Vector2.Normalize(new Vector2((float)Math.Sin((double)this.Rotation), -(float)Math.Cos((double)this.Rotation))) * this.velocityMaximum;
            }
            if ((this.Thrust <= 0.0f || this.Mass <= 0.0f )&& !this.IsTethered())
            {
                this.EnginesKnockedOut = true;
                this.velocityMaximum = this.Velocity.Length();
                Ship ship = this;
                Vector2 vector2 = ship.Velocity - this.Velocity * (elapsedTime * 0.1f);
                ship.Velocity = vector2;
                if (this.engineState == MoveState.Warp)
                    this.HyperspaceReturn();
            }
            else
                this.EnginesKnockedOut = false;
            if (this.Velocity.Length() <= this.velocityMaximum)
                return;
            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
        }
        public void ShipStatusChange()
        {
            this.Health = 0f;
            float sensorBonus = 0f;
            //if (this.shipStatusChanged)
            {
                this.Hangars.Clear();
                this.Shields.Clear();
                this.Transporters.Clear();
                this.Thrust = 0f;
                this.Mass = this.Size / 2f;
                this.shield_max = 0f;
                this.number_Alive_Internal_slots = 0f;
                this.BonusEMP_Protection = 0f;
                this.PowerStoreMax = 0f;
                this.PowerFlowMax = 0f;
                this.OrdinanceMax = 0f;
                this.ModulePowerDraw = 0.0f;
                this.ShieldPowerDraw = 0f;
                this.RepairRate = 0f;
                this.CargoSpace_Max = 0f;
                this.SensorRange = 0f;
                this.HasTroopBay = false;
                this.WarpThrust = 0f;
                this.TurnThrust = 0f;
                this.NormalWarpThrust = 0f;
                this.FTLSlowTurnBoost = false;
                this.InhibitionRadius = 0f;
                this.OrdAddedPerSecond = 0f;
                this.WarpDraw = 0f;
                this.HealPerTurn = 0;
                this.ECMValue = 0f;
                this.FTLSpoolTime = 0f;
                this.hasCommand = this.IsPlatform;
                this.TrackingPower = 0;
                this.FixedTrackingPower = 0;
            }
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                //Get total internal slots
                if (moduleSlot.Restrictions == Restrictions.I && moduleSlot.module.Active)
                    ++this.number_Alive_Internal_slots;
                if (moduleSlot.module.ModuleType == ShipModuleType.Dummy)
                    continue;
                this.Health += moduleSlot.module.Health;
                //if (this.shipStatusChanged)
                {
                    this.RepairRate += moduleSlot.module.BonusRepairRate;
                    if (moduleSlot.module.Mass < 0.0 && moduleSlot.Powered)
                    {
                        //Ship ship3 = this;
                        //float num3 = ship3.Mass + moduleSlot.module.Mass;     //Some minor performance tweaks -Gretman
                        this.Mass += moduleSlot.module.Mass;
                    }
                    else if (moduleSlot.module.Mass > 0.0)
                    {
                        //Ship ship3 = this;

                        //float num3;
                        if (moduleSlot.module.ModuleType == ShipModuleType.Armor && this.loyalty != null)
                        {
                            float ArmourMassModifier = this.loyalty.data.ArmourMassModifier;
                            float ArmourMass = moduleSlot.module.Mass * ArmourMassModifier;
                            this.Mass += ArmourMass;
                        }
                        else
                        {
                            this.Mass += moduleSlot.module.Mass;
                        }
                        //ship3.Mass = num3;
                    }
                    //Checks to see if there is an active command module

                    if (moduleSlot.module.Active && (moduleSlot.module.Powered || moduleSlot.module.PowerDraw == 0))
                    {
                        if (!this.hasCommand && moduleSlot.module.IsCommandModule)
                            this.hasCommand = true;
                        //Doctor: For 'Fixed' tracking power modules - i.e. a system whereby a module provides a non-cumulative/non-stacking tracking power.
                        //The normal stacking/cumulative tracking is added on after the for loop for mods that want to mix methods. The original cumulative function is unaffected.
                        if (moduleSlot.module.FixedTracking > 0 && moduleSlot.module.FixedTracking > this.FixedTrackingPower)
                            this.FixedTrackingPower = moduleSlot.module.FixedTracking;
                        if (moduleSlot.module.TargetTracking > 0)
                            this.TrackingPower += moduleSlot.module.TargetTracking;
                        this.OrdinanceMax += (float)moduleSlot.module.OrdinanceCapacity;
                        this.CargoSpace_Max += moduleSlot.module.Cargo_Capacity;
                        this.InhibitionRadius += moduleSlot.module.InhibitionRadius;
                        this.BonusEMP_Protection += moduleSlot.module.EMP_Protection;
                        if (moduleSlot.module.SensorRange > this.SensorRange)
                            this.SensorRange = moduleSlot.module.SensorRange;
                        if (moduleSlot.module.SensorBonus > sensorBonus)
                            sensorBonus = moduleSlot.module.SensorBonus;
                        if (moduleSlot.module.shield_power_max > 0f)
                        {
                            this.shield_max += moduleSlot.module.GetShieldsMax();
                            this.ShieldPowerDraw += moduleSlot.module.PowerDraw;
                            this.Shields.Add(moduleSlot.module);
                        }
                        else
                            this.ModulePowerDraw += moduleSlot.module.PowerDraw;
                        this.Thrust += moduleSlot.module.thrust;
                        this.WarpThrust += moduleSlot.module.WarpThrust;
                        this.TurnThrust += moduleSlot.module.TurnThrust;
                        if (moduleSlot.module.ECM > this.ECMValue)
                        {
                            this.ECMValue = moduleSlot.module.ECM;
                            if (this.ECMValue > 1.0f)
                                this.ECMValue = 1.0f;
                            if (this.ECMValue < 0f)
                                this.ECMValue = 0f;
                        }
                        this.OrdAddedPerSecond += moduleSlot.module.OrdnanceAddedPerSecond;
                        this.HealPerTurn += moduleSlot.module.HealPerTurn;
                        if (moduleSlot.module.ModuleType == ShipModuleType.Hangar)
                        {
                            this.Hangars.Add(moduleSlot.module);
                            if (moduleSlot.module.IsTroopBay)
                                this.HasTroopBay = true;
                        }
                        if (moduleSlot.module.ModuleType == ShipModuleType.Transporter)
                            this.Transporters.Add(moduleSlot.module);
                        if (moduleSlot.module.InstalledWeapon != null && moduleSlot.module.InstalledWeapon.isRepairBeam)
                            this.RepairBeams.Add(moduleSlot.module);
                        if (moduleSlot.module.PowerStoreMax > 0)
                            this.PowerStoreMax += moduleSlot.module.PowerStoreMax;
                        if (moduleSlot.module.PowerFlowMax >  0)
                            this.PowerFlowMax += moduleSlot.module.PowerFlowMax;
                        this.WarpDraw += moduleSlot.module.PowerDrawAtWarp;
                        if (moduleSlot.module.FTLSpoolTime > this.FTLSpoolTime)
                            this.FTLSpoolTime = moduleSlot.module.FTLSpoolTime;
                    }
                }
            }
            this.NormalWarpThrust = this.WarpThrust;
            //Doctor: Add fixed tracking amount if using a mixed method in a mod or if only using the fixed method.
            this.TrackingPower += FixedTrackingPower;
            
            //Update max health due to bonuses that increase module health
            if (this.Health > this.HealthMax)
                this.HealthMax = this.Health;
            //if (this.shipStatusChanged)
            {
                this.SensorRange += sensorBonus;
                //Apply modifiers to stats
                if (this.loyalty != null)
                {
                    this.Mass *= this.loyalty.data.MassModifier;
                    this.RepairRate += (float)(this.RepairRate * this.Level * 0.05) + this.RepairRate * this.loyalty.data.Traits.RepairMod;
                    this.PowerFlowMax += this.PowerFlowMax * this.loyalty.data.PowerFlowMod;
                    this.PowerStoreMax += this.PowerStoreMax * this.loyalty.data.FuelCellModifier;
                    this.SensorRange *= this.loyalty.data.SensorModifier;
                }
                if (this.FTLSpoolTime <= 0)
                    this.FTLSpoolTime = 3f;
                //Hull bonuses
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    HullBonus mod;
                    if (ResourceManager.HullBonuses.TryGetValue(this.shipData.Hull, out mod))
                    {
                        this.RepairRate += this.RepairRate * mod.RepairBonus;
                        this.CargoSpace_Max += this.CargoSpace_Max * mod.CargoBonus;
                        this.SensorRange += this.SensorRange * mod.SensorBonus;
                        this.WarpThrust += this.WarpThrust * mod.SpeedBonus;
                        this.Thrust += this.Thrust * mod.SpeedBonus;
                    }
                }
            }
            
        }
        public bool IsTethered()
        {
            return this.TetheredTo != null;
        }

        public override bool Touch(GameplayObject target)
        {
            return false;
        }

        public override bool Damage(GameplayObject source, float damageAmount)
        {
            return true;
        }

        public float GetStrengthORIG()
        {
            float num1 = 0.0f;
            foreach (Weapon weapon in this.Weapons)
            {
                if (weapon.explodes)
                    num1 += (float)(weapon.DamageAmount * (1.0 / weapon.fireDelay) * 0.75);
                else if (weapon.isBeam)
                    num1 += weapon.DamageAmount * 180f;
                else
                    num1 += weapon.DamageAmount * (1f / weapon.fireDelay);
            }
            if (num1 <= 0.0f)
                return 0.0f;
            float num2 = (num1 + this.shield_power / 20f + this.Health) / (float)this.Size;
            if (this.shipData.Role == ShipData.RoleName.platform || this.shipData.Role == ShipData.RoleName.station)
                num2 /= 5f;
            return num2;
        }
        //added by Gremlin : active ship strength calculator
        public float GetStrength()
        {            
            if (this.Health >= this.HealthMax * .75 && !this.LowHealth && this.BaseStrength !=-1)
                return this.BaseStrength;
            float Str = 0f;
            float def = 0f;
            if (this.Health >= this.HealthMax * .75)
                this.LowHealth = false;
            else
                this.LowHealth = true;
            int slotCount = this.ModuleSlotList.Count;

            bool fighters = false;
            bool weapons = false;

            //Parallel.ForEach(this.ModuleSlotList, slot =>  //
            foreach (ModuleSlot slot in this.ModuleSlotList)
            {
#if DEBUG

                //if( this.BaseStrength ==0 && (this.Weapons.Count >0 ))
                    //Log.Info("No base strength: " + this.Name +" datastrength: " +this.shipData.BaseStrength);

#endif
                if (!slot.module.isDummy && (this.BaseStrength == -1 ||( slot.module.Powered && slot.module.Active )))
                {
                    ShipModule module = slot.module;//ResourceManager.ShipModulesDict[slot.InstalledModuleUID];

                    if (module.InstalledWeapon != null)
                    {
                        weapons = true;
                        float offRate = 0;
                        Weapon w = module.InstalledWeapon;
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
                        Str += offRate;
                    }


                    if (module.hangarShipUID != null && !module.IsSupplyBay )
                    {
                        if(module.IsTroopBay)
                        {
                            Str += 50;
                            continue;
                        }
                        fighters = true;
                        Ship hangarship = new Ship();
                        ResourceManager.ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

                        if (hangarship != null)
                        {
                            Str += hangarship.BaseStrength;
                        }
                        else Str += 300;
                    }
                    def += (module.shield_power) * ((module.shield_radius * .05f) / slotCount);
                    def += module.Health * ((module.ModuleType == ShipModuleType.Armor ? (module.XSIZE) : 1f) / (slotCount * 4));
                    /// (slotCount / (module.ModuleType == ShipModuleType.Armor ? module.XSIZE * module.YSIZE : 1));// (slotCount / (module.XSIZE * module.YSIZE));//module.ModuleType ==ShipModuleType.Armor?module.XSIZE*module.YSIZE:1
                    //ship.BaseStrength += module.HealthMax / (entry.Value.ModuleSlotList.Count / (module.XSIZE * module.YSIZE));
                    //ship.BaseStrength += (module.shield_powe) * ((module.shield_radius * .10f) / entry.Value.ModuleSlotList.Count);
                }
            }//);
            if (!fighters && !weapons) Str = 0;
            if (def > Str) def = Str;
            //the base strength should be the ships strength at full health. 
            //this.BaseStrength = Str + def;
            return Str + def;
        }


        public float GetDPS()
        {
            float num = 0.0f;
            foreach (Weapon weapon in this.Weapons)
                num += weapon.DamageAmount * (1f / weapon.fireDelay);
            return num;
        }

        //Added by McShooterz: add experience for cruisers and stations, modified for dynamic system
        public void AddKill(Ship killed)
        {
            ++this.kills;
            if (this.loyalty == null)
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
            Exp += Exp * this.loyalty.data.ExperienceMod;
            this.experience += Exp;
            ExpFound = false;
            //Added by McShooterz: a way to prevent remnant story in mods

            Empire remnant = EmpireManager.Remnants;  //Changed by Gretman, because this was preventing any "RemnantKills" from getting counted, thus no remnant event.
            //if (this.loyalty == Ship.universeScreen.player && killed.loyalty == remnant && this.shipData.ShipStyle == remnant.data.Traits.ShipType &&  (GlobalStats.ActiveModInfo == null || (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.removeRemnantStory)))
            if (this.loyalty == Ship.universeScreen.player && killed.loyalty == remnant &&  (GlobalStats.ActiveModInfo == null || (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.removeRemnantStory)))
                //GlobalStats.IncrementRemnantKills((int)Exp);
                GlobalStats.IncrementRemnantKills(1);   //I also changed this because the exp before was a lot, killing almost any remnant ship would unlock the remnant event immediately

            if (ResourceManager.ShipRoles.ContainsKey(this.shipData.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[this.shipData.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[this.shipData.Role].RaceList[i].ShipType == this.loyalty.data.Traits.ShipType)
                    {
                        ReqExp = ResourceManager.ShipRoles[this.shipData.Role].RaceList[i].ExpPerLevel;
                        ExpFound = true;
                        break;
                    }
                }
                if (!ExpFound)
                {
                    ReqExp = ResourceManager.ShipRoles[this.shipData.Role].ExpPerLevel;
                }
            }
            while (this.experience > ReqExp * (1 + this.Level))
            {
                this.experience -= ReqExp * (1 + this.Level);
                ++this.Level;
            }
            if (this.Level > 255)
                this.Level = 255;
            if (!loyalty.TryGetRelations(killed.loyalty, out Relationship rel) || !rel.AtWar)
                return;
            this.loyalty.GetRelations(killed.loyalty).ActiveWar.StrengthKilled += killed.BaseStrength;
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
            foreach (Beam beam in beams)
                beam.Die(this, true);
            beams.ClearAll();
            
            ++DebugInfoScreen.ShipsDied;
            Projectile psource = source as Projectile;
            if (!cleanupOnly)
                psource?.owner?.AddKill(this);

            // 35% the ship will not explode immediately, but will start tumbling out of control
            // we mark the ship as dying and the main update loop will set reallyDie
            if (UniverseRandom.IntBetween(0, 100) > 65.0 && !IsPlatform && InFrustum)
            {
                dying = true;
                xdie = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                ydie = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                zdie = UniverseRandom.RandomBetween(-1f, 1f) * 40f / Size;
                dietimer = UniverseRandom.RandomBetween(4f, 6f);
                if (psource != null && psource.explodes && psource.damageAmount > 100.0)
                    reallyDie = true;
            }
            else reallyDie = true;

            if (dying && !reallyDie)
                return;

            System?.ShipList.QueuePendingRemoval(this);
            if (psource?.owner != null)
            {
                float amount = 1f;
                if (ResourceManager.ShipRoles.ContainsKey(shipData.Role))
                    amount = ResourceManager.ShipRoles[shipData.Role].DamageRelations;
                loyalty.DamageRelationship(psource.owner.loyalty, "Destroyed Ship", amount, null);
            }
            if (!cleanupOnly && InFrustum)
            {
                string dieSoundEffect;
                if (Size < 80)       dieSoundEffect = "sd_explosion_ship_det_small";
                else if (Size < 250) dieSoundEffect = "sd_explosion_ship_det_medium";
                else                 dieSoundEffect = "sd_explosion_ship_det_large";
                AudioManager.PlayCue(dieSoundEffect, universeScreen.listener, emitter);
            }
            foreach (Empire empire in EmpireManager.EmpireList)
            {
                empire.GetGSAI().ThreatMatrix.Pins.TryRemove(guid, out ThreatMatrix.Pin pin);
            }
            BorderCheck.Clear();
            ModuleSlotList.Clear();
            ExternalSlots.Clear();
            ModulesDictionary.Clear();
            ThrusterList.Clear();
            GetAI().PotentialTargets.Clear();
            AttackerTargetting.Clear();
            Velocity = Vector2.Zero;
            velocityMaximum = 0.0f;
            //this.AfterBurnerAmount = 0.0f;    //Not referenced in code, removing to save memory


            if (Active)
            {
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
            var ship = ResourceManager.ShipsDict[Name];
            var hullData = ship.GetShipData();
            if (hullData.EventOnDeath != null)
            {
                var evt = ResourceManager.EventsDict[hullData.EventOnDeath];
                universeScreen.ScreenManager.AddScreen(new EventPopup(universeScreen, universeScreen.PlayerEmpire, evt, evt.PotentialOutcomes[0], true));
            }
            QueueTotalRemoval();
        }

        public void QueueTotalRemoval()
        {
            universeScreen.ShipsToRemove.Add(this);
        }

        public void TotallyRemove()
        {
            Active            = false;
            AI.Target         = null;
            AI.TargetShip     = null;
            AI.ColonizeTarget = null;
            AI.EscortTarget   = null;
            ExternalSlots.Clear();
     
            AI.start = null;
            AI.end   = null;
            AI.PotentialTargets.Clear();
            AI.TrackProjectiles.Clear();
            AI.NearbyShips.Clear();
            AI.FriendliesNearby.Clear();
            universeScreen.MasterShipList.QueuePendingRemoval(this);
            AttackerTargetting.Clear();
            if (universeScreen.SelectedShip == this)
                universeScreen.SelectedShip = null;
            universeScreen.SelectedShipList.Remove(this);
            if (System != null)
            {
                System.ShipList.QueuePendingRemoval(this);
                System.spatialManager.CollidableObjects.Remove(this);
            }
            else if (isInDeepSpace)
                UniverseScreen.DeepSpaceManager.CollidableObjects.Remove(this);
            if (this.Mothership != null)
            {
                foreach (ShipModule shipModule in this.Mothership.Hangars)
                {
                    if (shipModule.GetHangarShip() == this)
                        shipModule.SetHangarShip((Ship)null);
                }
            }
            foreach (ShipModule hanger in this.Hangars)
            {
                if (hanger.GetHangarShip() != null)
                    hanger.GetHangarShip().Mothership = null;
            }
            foreach(Empire empire in EmpireManager.EmpireList)
            {
                empire.GetGSAI().ThreatMatrix.UpdatePin(this);
            }

            foreach (Projectile projectile in projectiles)
                projectile.Die(this, false);
            projectiles.Clear();

            foreach (ModuleSlot moduleSlot in ModuleSlotList)
                moduleSlot.module.Clear();
            this.Shields.Clear();
            this.Hangars.Clear();
            this.BombBays.Clear();

            this.ModuleSlotList.Clear();
            this.TroopList.Clear();
            this.RemoveFromAllFleets();
            this.ShipSO.Clear();
            lock (GlobalStats.ObjectManagerLocker)
                universeScreen.ScreenManager.inter.ObjectManager.Remove(ShipSO);

            this.loyalty.RemoveShip(this);
            this.System = null;
            this.TetheredTo = null;
            this.Transporters.Clear();
            this.RepairBeams.Clear();
            this.ModulesDictionary.Clear();
            this.ProjectilesFired.Clear();


        }

        public void RemoveFromAllFleets()
        {
            if (this.fleet == null)
                return;
            this.fleet.Ships.Remove(this);
            foreach (FleetDataNode fleetDataNode in (List<FleetDataNode>)this.fleet.DataNodes)
            {
                if (fleetDataNode.Ship== this)
                    fleetDataNode.Ship = (Ship)null;
            }
            foreach (List<Fleet.Squad> list in this.fleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in list)
                {
                    if (squad.Ships.Contains(this))
                        squad.Ships.QueuePendingRemoval(this);
                    foreach (FleetDataNode fleetDataNode in (List<FleetDataNode>)squad.DataNodes)
                    {
                        if (fleetDataNode.Ship== this)
                            fleetDataNode.Ship = (Ship)null;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Ship() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.projectiles?.Dispose();
                    this.beams?.Dispose();
                    this.supplyLock?.Dispose();
                    this.AI?.Dispose();
                    this.ProjectilesFired?.Dispose();
                }
                this.projectiles = null;
                this.beams = null;
                this.supplyLock = null;
                this.AI = null;
                this.ProjectilesFired = null;
                this.disposed = true;
            }
        }
        
        public class target
        {
            public ShipModule module;
            public int weight;
            public target(ShipModule module, int weight)
            {
                this.module = module;
                this.weight = weight;
            }
        }

        public static ModuleSlot ClosestModuleSlot(List<ModuleSlot> slots, Vector2 center, float maxRange=999999f)
        {
            float nearest = maxRange*maxRange;
            ModuleSlot closestModule = null;
            foreach (ModuleSlot slot in slots)
            {
                if (slot.module.ModuleType == ShipModuleType.Dummy 
                    || !slot.module.Active || slot.module.quadrant == 0 || slot.module.Health <= 0f)
                    continue;

                float sqDist = center.SqDist(slot.module.Center);
                if (!(sqDist < nearest) && closestModule != null)
                    continue;
                nearest       = sqDist;
                closestModule = slot;
            }
            return closestModule;
        }

        public List<ModuleSlot> FilterSlotsInDamageRange(List<ModuleSlot> slots, ModuleSlot closestExtSlot)
        {
            Vector2 extSlotCenter = closestExtSlot.module.Center;
            sbyte quadrant        = closestExtSlot.module.quadrant;
            float sqDamageRadius  = Center.SqDist(extSlotCenter);

            var filtered = new List<ModuleSlot>();
            foreach (ModuleSlot slot in slots)
            {
                if (slot == null) continue;
                var module = slot.module;
                if (module.ModuleType == ShipModuleType.Dummy || !module.Active || module.Health <= 0f || 
                    (module.quadrant != quadrant && module.isExternal))
                    continue;
                if (module.Center.SqDist(extSlotCenter) < sqDamageRadius)
                    filtered.Add(slot);
            }
            return filtered;
        }

        // Refactor by RedFox: Picks a random internal module to target and updates targetting list if needed
        private ShipModule TargetRandomInternalModule(ref List<ModuleSlot> inAttackerTargetting, 
                                                      Vector2 center, int level, float weaponRange=999999f)
        {
            ModuleSlot closestExtSlot = ClosestModuleSlot(ExternalSlots, center, weaponRange);

            if (closestExtSlot == null) // ship might be destroyed, no point in targeting it
            {
                return ExternalSlots.Count == 0 ? null : ExternalSlots[0].module;
            }

            if (inAttackerTargetting == null || !inAttackerTargetting.Contains(closestExtSlot))
            {
                inAttackerTargetting = FilterSlotsInDamageRange(ModuleSlotList, closestExtSlot);
                if (level > 1)
                {
                    // Sort Descending, so first element is the module with greatest TargettingValue
                    inAttackerTargetting.Sort((sa, sb) => sb.module.ModuleTargettingValue 
                                                        - sa.module.ModuleTargettingValue);
                }
            }

            if (inAttackerTargetting.Count == 0)
                return ExternalSlots.Count == 0 ? null : ExternalSlots[0].module;

            if (inAttackerTargetting.Count == 0)
                return null;
            // higher levels lower the limit, which causes a better random pick
            int limit = inAttackerTargetting.Count / (level + 1);
            return inAttackerTargetting[RandomMath.InRange(limit)].module;
        }

        public ShipModule GetRandomInternalModule(Weapon source)
        {
            float searchRange = source.Range + 100;
            Vector2 center    = source.GetOwner()?.Center ?? source.Center;
            int level         = source.GetOwner()?.Level ?? 0;
            return TargetRandomInternalModule(ref source.AttackerTargetting, center, level, searchRange);
        }

        public ShipModule GetRandomInternalModule(Projectile source)
        {
            Vector2 center = source.Owner?.Center ?? source.Center;
            int level      = source.Owner?.Level ?? 0;
            return TargetRandomInternalModule(ref source.weapon.AttackerTargetting, center, level);
        }

        public void UpdateShields()
        {
            float shieldPower = 0.0f;
            foreach (ShipModule shield in Shields)
                shieldPower += shield.shield_power;
            if (shieldPower > shield_max)
                shieldPower = shield_max;

            this.shield_power = shieldPower;
        }

        public virtual void StopAllSounds()
        {
            if (drone == null)
                return;
            if (drone.IsPlaying)
                drone.Stop(AudioStopOptions.Immediate);
            drone.Dispose();
        }

        public static Ship Copy(Ship ship)
        {
            return new Ship
            {
                shipData       = ship.shipData,
                ThrusterList   = ship.ThrusterList,
                ModelPath      = ship.ModelPath,
                ModuleSlotList = ship.ModuleSlotList
            };
        }

        private static Vector2 MoveInCircle(GameTime gameTime, float speed)
        {
            double num = gameTime.TotalGameTime.TotalSeconds * speed;
            return new Vector2((float)Math.Cos(num), (float)Math.Sin(num));
        }

        public enum MoveState
        {
            Sublight,
            Warp,
        }

        public void RecalculateMaxHP()          //Added so ships would get the benefit of +HP mods from research and/or artifacts.   -Gretman
        {
            if (VanityName == "MerCraft") Log.Info("Health was " + Health + " / " + HealthMax + "   (" + loyalty.data.Traits.ModHpModifier + ")");
            this.HealthMax = 0;
            foreach (ModuleSlot slot in ModuleSlotList)
            {
                if (slot.module.isDummy) continue;
                bool isFullyHealed = slot.module.Health >= slot.module.HealthMax;
                slot.module.HealthMax = ResourceManager.ShipModulesDict[slot.module.UID].HealthMax;
                slot.module.HealthMax = slot.module.HealthMax + slot.module.HealthMax * loyalty.data.Traits.ModHpModifier;
                if (isFullyHealed)
                {                                                                   //Basically, set maxhealth to what it would be with no modifier, then
                    slot.module.Health = slot.module.HealthMax;                     //apply the total benefit to it. Next, if the module is fully healed,
                    slot.ModuleHealth  = slot.module.HealthMax;                     //adjust its HP so it is still fully healed. Also calculate and adjust                                            
                }                                                                   //the ships MaxHP so it will display properly.        -Gretman
                HealthMax += slot.module.HealthMax;
            }
            if (Health >= HealthMax) Health = HealthMax;
            if (VanityName == "MerCraft") Log.Info("Health is  " + Health + " / " + HealthMax);
        }

    }
}
