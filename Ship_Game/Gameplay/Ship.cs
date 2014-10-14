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


namespace Ship_Game.Gameplay
{
    public class Ship : GameplayObject
    {
        public string VanityName = "";
        public List<Troop> TroopList = new List<Troop>();
        public List<Rectangle> AreaOfOperation = new List<Rectangle>();
        public bool RecallFightersBeforeFTL = true;
        private Dictionary<Vector2, ModuleSlot> ModulesDictionary = new Dictionary<Vector2, ModuleSlot>();
        public float DefaultFTLSpeed = 1000f;
        public float RepairRate = 1f;
        public float SensorRange = 20000f;
        public float yBankAmount = 0.007f;
        public float maxBank = 0.5235988f;
        private Dictionary<string, float> CargoDict = new Dictionary<string, float>();
        private Dictionary<string, float> MaxGoodStorageDict = new Dictionary<string, float>();
        private Dictionary<string, float> ResourceDrawDict = new Dictionary<string, float>();
        public Vector2 projectedPosition = new Vector2();
        protected List<Thruster> ThrusterList = new List<Thruster>();
        public string Role = "fighter";
        public bool TradingFood = true;
        public bool TradingProd = true;
        public bool ShieldsUp = true;
        public float AfterBurnerAmount = 20.5f;
        protected Color CloakColor = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        public float CloakTime = 5f;
        public Vector2 Origin = new Vector2(256f, 256f);
        public LinkedList<ModuleSlot> ModuleSlotList = new LinkedList<ModuleSlot>();
        private BatchRemovalCollection<Projectile> projectiles = new BatchRemovalCollection<Projectile>();
        private BatchRemovalCollection<Beam> beams = new BatchRemovalCollection<Beam>();
        public List<Weapon> Weapons = new List<Weapon>();
        public float fireThresholdSquared = 0.25f;
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
        public bool shipStatusChanged = true;
        public Guid guid = Guid.NewGuid();
        public bool AddedOnLoad;
        private AnimationController animationController;
        public bool IsPlayerDesign;
        public bool IsSupplyShip;
        public bool reserved;
        public bool isColonyShip;
        public string StrategicIconPath;
        private Planet TetheredTo;
        public Vector2 TetherOffset;
        public Guid TetherGuid;
        public float EMPDamage;
        public Fleet fleet;
        public string DesignUID;
        public float yRotation;
        public float RotationalVelocity;
        public float MechanicalBoardingDefense;
        public float TroopBoardingDefense;
        public float ECMValue = 0f;
        public ShipData shipData;
        public int kills;
        public float experience;
        public bool EnginesKnockedOut;
        protected float ThrustLast;
        public float InCombatTimer;
        public bool isTurning;
        public bool PauseUpdate;
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
        public int CrewRequired;
        public int CrewSupplied;
        public float Ordinance;
        public float OrdinanceMax;
        public float scale;
        public ShipModuleNode FirstNode;
        protected ArtificialIntelligence AI;
        public float speed;
        public float Thrust;
        public float velocityMaximum;
        public double armor_percent;
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
        protected SolarSystem JumpTarget;
        protected Cue hyperspace;
        protected Cue hyperspace_return;
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
        public bool WeaponCentered;
        protected Cue drone;
        public float ShieldRechargeTimer;
        public bool InCombat;
        private Vector3 pointat;
        private Vector3 scalefactors;
        public float xRotation;
        public Ship.MoveState engineState;
        public float ScreenRadius;
        public float ScreenSensorRadius;
        public bool InFrustum;
        public bool NeedRecalculate;
        public bool Deleted;
        public float CargoMass;
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
        public static UniverseScreen universeScreen;
        public float FTLSpoolTime;
        public bool IsIndangerousSpace;
        public bool IsInNeutralSpace;
        public bool IsInFriendlySpace;
        public List<ShipModule> Transporters = new List<ShipModule>();
        public List<ShipModule> RepairBeams = new List<ShipModule>();
        public bool hasTransporter;
        public bool hasOrdnanceTransporter;
        public bool hasAssaultTransporter;
        public bool hasRepairBeam;
        public bool hasCommand;
        private float FTLmodifier = 1f;

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

        public bool HasSupplyBays
        {
            get
            {
                foreach (ShipModule shipModule in this.Hangars)
                {
                    if (shipModule.IsSupplyBay)
                        return true;
                }
                return false;
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
                this.AI.OrderResupplyNearest();
            }
        }

        public bool FightersOut
        {
            get
            {
                bool flag = false;
                for (int index = 0; index < this.Hangars.Count; ++index)
                {
                    try
                    {
                        ShipModule shipModule = this.Hangars[index];
                        if (shipModule.GetHangarShip() != null)
                        {
                            if (shipModule.GetHangarShip() != null)
                            {
                                if (!shipModule.GetHangarShip().Active)
                                {
                                    if ((double)shipModule.hangarTimer >= 0.0)
                                        continue;
                                }
                                else
                                    continue;
                            }
                            else
                                continue;
                        }
                        return false;
                    }
                    catch
                    {
                    }
                }
                return !flag;
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
                this.GetAI().OrderTrade();
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
                this.GetAI().OrderTransportPassengers();
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
                this.GetAI().OrderResupplyNearest();
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
                    EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetGSAI().DefensiveCoordinator.DefensiveForcePool.Remove(this);
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
                    this.ScrambleAssaultShips();
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

        public Ship(Vector2 pos, Vector2 dim, float rot)
        {
            this.Position = pos;
            this.rotation = rot;
            this.Dimensions = dim;
        }

        public void SetAnimationController(AnimationController ac, SkinnedModel model)
        {
            this.animationController = ac;
            this.animationController.StartClip(model.AnimationClips["Take 001"]);
        }

        //added by gremlin The Generals GetFTL speed
        public float GetFTLSpeed()
        {
            //Added by McShooterz: hull bonus speed 
            return this.WarpThrust / base.Mass + this.WarpThrust / base.Mass * this.loyalty.data.FTLModifier;
        }

        public float GetSTLSpeed()
        {
            //Added by McShooterz: hull bonus speed
            return (float)((double)this.Thrust / (double)this.Mass + (double)this.Thrust / (double)this.Mass * (double)this.loyalty.data.SubLightModifier);
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
                Role = this.Role,
                FleetOffset = this.FleetOffset,
                RelativeFleetOffset = this.RelativeFleetOffset,
                guid = this.guid,
                projectedPosition = this.projectedPosition
            };
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
                num += (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.shipData.Hull) ? ResourceManager.HullBonuses[this.shipData.Hull].StartingCost : 0);
                num += num * e.data.Traits.ShipCostMod;
                return (float)(int)(num * (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.shipData.Hull) ? 1f - ResourceManager.HullBonuses[this.shipData.Hull].CostBonus : 1));
            }
            else
                return (float)(int)num;
        }

        public ShipData GetShipData()
        {
            if (Ship_Game.ResourceManager.ShipsDict.ContainsKey(this.Name))
                return Ship_Game.ResourceManager.ShipsDict[this.Name].shipData;
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
                        if ((double)this.RotationalVelocity > (double)this.rotationRadiansPerSecond)
                            this.RotationalVelocity = this.rotationRadiansPerSecond;
                        if ((double)this.yRotation > -(double)this.maxBank)
                            this.yRotation -= this.yBankAmount;
                    }
                    else if (this.currentKeyBoardState.IsKeyDown(Keys.A))
                    {
                        this.isThrusting = true;
                        this.RotationalVelocity -= this.rotationRadiansPerSecond * elapsedTime;
                        this.isTurning = true;
                        if ((double)Math.Abs(this.RotationalVelocity) > (double)this.rotationRadiansPerSecond)
                            this.RotationalVelocity = -this.rotationRadiansPerSecond;
                        if ((double)this.yRotation < (double)this.maxBank)
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
                        if ((double)this.Velocity.Length() > (double)this.velocityMaximum)
                            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                        if ((double)this.Velocity.LengthSquared() <= 0.0)
                            this.Velocity = Vector2.Zero;
                        if ((double)this.yRotation > 0.0)
                            this.yRotation -= this.yBankAmount;
                        else if ((double)this.yRotation < 0.0)
                            this.yRotation += this.yBankAmount;
                        if ((double)this.RotationalVelocity > 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity -= this.rotationRadiansPerSecond * elapsedTime;
                            if ((double)this.RotationalVelocity < 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                        else if ((double)this.RotationalVelocity < 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity += this.rotationRadiansPerSecond * elapsedTime;
                            if ((double)this.RotationalVelocity > 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                    }
                    else
                    {
                        this.isTurning = false;
                        if ((double)this.yRotation > 0.0)
                        {
                            this.yRotation -= this.yBankAmount;
                            if ((double)this.yRotation < 0.0)
                                this.yRotation = 0.0f;
                        }
                        else if ((double)this.yRotation < 0.0)
                        {
                            this.yRotation += this.yBankAmount;
                            if ((double)this.yRotation > 0.0)
                                this.yRotation = 0.0f;
                        }
                        if ((double)this.RotationalVelocity > 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity -= this.rotationRadiansPerSecond * elapsedTime;
                            if ((double)this.RotationalVelocity < 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                        else if ((double)this.RotationalVelocity < 0.0)
                        {
                            this.isTurning = true;
                            this.RotationalVelocity += this.rotationRadiansPerSecond * elapsedTime;
                            if ((double)this.RotationalVelocity > 0.0)
                                this.RotationalVelocity = 0.0f;
                        }
                        this.isThrusting = false;
                    }
                    if ((double)this.Velocity.Length() > (double)this.velocityMaximum)
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
                        if ((double)this.Velocity.Length() > (double)this.velocityMaximum)
                            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                    }
                    else if (this.currentKeyBoardState.IsKeyDown(Keys.S))
                    {
                        this.isThrusting = true;
                        Ship ship = this;
                        Vector2 vector2_3 = ship.Velocity - vector2_1 * (elapsedTime * this.speed);
                        ship.Velocity = vector2_3;
                        if ((double)this.Velocity.Length() > (double)this.velocityMaximum)
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
                            if ((double)w.timeToNextFire <= 0.0 && w.moduleAttachedTo.Powered)
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

        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Vector3 PickedPos)
        {
            Vector2 pos = new Vector2(PickedPos.X, PickedPos.Y);
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
            //added by gremlin attackrun compensator
            float modifyRangeAR = 50f;
            if (!w.isBeam && this.GetAI().CombatState == CombatState.AttackRuns && this.maxWeaponsRange < 2000 && w.SalvoCount > 0)
            {
                modifyRangeAR = this.speed;
            }
            if (difference < halfArc && Vector2.Distance(base.Position, pos) < w.GetModifiedRange() + modifyRangeAR)
            {
                return true;
            }
            return false;
        }

        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Vector2 PickedPos)
        {
            GlobalStats.WeaponArcChecks = GlobalStats.WeaponArcChecks + 1;
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
            float modifyRangeAR = 50f;
            if (!w.isBeam && this.GetAI().CombatState == CombatState.AttackRuns && this.maxWeaponsRange < 2000 && w.SalvoTimer > 0)
            {
                modifyRangeAR = this.speed * w.SalvoTimer;
            }
            if (difference < halfArc && Vector2.Distance(w.moduleAttachedTo.Center, PickedPos) < w.GetModifiedRange() + modifyRangeAR)
            {
                return true;
            }
            return false;
        }

        //Added by McShooterz
        public bool CheckIfInsideFireArc(Weapon w, Vector2 PickedPos, float Rotation)
        {
            float halfArc = w.moduleAttachedTo.FieldOfFire / 2f;
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
            if (difference < halfArc && Vector2.Distance(w.moduleAttachedTo.Center, PickedPos) < w.GetModifiedRange() + 50f)
            {
                return true;
            }
            return false;
        }

        public List<Thruster> GetTList()
        {
            return this.ThrusterList;
        }

        public void SetTList(List<Thruster> list)
        {
            this.ThrusterList = list;
        }

        private Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
        {
            return new Vector2(0.0f, 0.0f)
            {
                X = (float)-((double)OwnerPos.X - (double)TargetPos.X),
                Y = (float)-((double)OwnerPos.Y - (double)TargetPos.Y)
            };
        }

        public void SetSO(SceneObject so)
        {
            this.ShipSO = so;
            this.ShipSO.Visibility = ObjectVisibility.Rendered;
            this.radius = this.ShipSO.WorldBoundingSphere.Radius * 2f;
        }

        public SceneObject GetSO()
        {
            return this.ShipSO;
        }

        public ArtificialIntelligence GetAI()
        {
            return this.AI;
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
            string role = this.Role;
            
            //Free upkeep ships
            if (role == null || this.GetShipData().ShipStyle == "Remnant" || this.loyalty == null || this.loyalty.data == null || this.loyalty.data.PrototypeShip == this.Name
                || (this.Mothership != null && (this.Role == "fighter" || this.Role == "corvette" || this.Role == "scout" || this.Role == "frigate")))
            {
                return 0f;
            }
            
            // Calculate maintenance by proportion of ship cost, Duh.
            if (this.Role == "fighter" || this.Role == "scout")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepFighter;
            else if (this.Role == "corvette")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepCorvette;
            else if (this.Role == "frigate" || this.Role == "destroyer")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepFrigate;
            else if (this.Role == "cruiser")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepCruiser;
            else if (this.Role == "carrier")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepCarrier;
            else if (this.Role == "capital")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepCapital;
            else if (this.Role == "freighter")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepFreighter;
            else if (this.Role == "platform")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepPlatform;
            else if (this.Role == "station")
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepStation;
            else if (this.Role == "drone" && GlobalStats.ActiveMod.mi.useDrones)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepDrone;
            else
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepBaseline;

            if (maint == 0f && GlobalStats.ActiveMod.mi.UpkeepBaseline > 0)
                maint = this.GetCost(this.loyalty) * GlobalStats.ActiveMod.mi.UpkeepBaseline;
            else if (maint == 0f && GlobalStats.ActiveMod.mi.UpkeepBaseline == 0)
                maint = this.GetCost(this.loyalty) * 0.004f;

            // Direct override in ShipDesign XML, e.g. for Shipyards/pre-defined designs with specific functions.

            if (this.shipData.HasFixedUpkeep && this.loyalty != null)
            {
                maint = shipData.FixedUpkeep;
            }      

            // Modifiers below here   
            if ((this.Role == "freighter" || this.Role == "platform") && this.loyalty != null && !this.loyalty.isFaction && this.loyalty.data.Privatization)
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
            string role = this.Role;

            // Calculate maintenance by proportion of ship cost, Duh.
                if (this.Role == "fighter" || this.Role == "scout")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepFighter;
                else if (this.Role == "corvette")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepCorvette;
                else if (this.Role == "frigate" || this.Role == "destroyer")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepFrigate;
                else if (this.Role == "cruiser")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepCruiser;
                else if (this.Role == "carrier")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepCarrier;
                else if (this.Role == "capital")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepCapital;
                else if (this.Role == "freighter")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepFreighter;
                else if (this.Role == "platform")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepPlatform;
                else if (this.Role == "station")
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepStation;
                else if (this.Role == "drone" && GlobalStats.ActiveMod.mi.useDrones)
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepDrone;
                else
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepBaseline;

                if (maint == 0f && GlobalStats.ActiveMod.mi.UpkeepBaseline > 0)
                    maint = this.GetCost(empire) * GlobalStats.ActiveMod.mi.UpkeepBaseline;
                else if (maint == 0f && GlobalStats.ActiveMod.mi.UpkeepBaseline == 0)
                    maint = this.GetCost(empire) * 0.004f;


            // Direct override in ShipDesign XML, e.g. for Shipyards/pre-defined designs with specific functions.

            if (this.shipData.HasFixedUpkeep && empire != null)
            {
                maint = shipData.FixedUpkeep;
            }

            // Modifiers below here   

            if ((this.Role == "freighter" || this.Role == "platform") && empire != null && !empire.isFaction && empire.data.Privatization)
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
            

        public float GetMaintCost()
        {
            float maint = 0f;
            string role = this.Role;
            string str = role;
            //bool nonCombat = false;
            //added by gremlin: Maintenance changes
            float maintModReduction = 1;

            //Ships without upkeep
            if (role == null || this.shipData.ShipStyle == "Remnant" || this.loyalty == null || this.loyalty.data == null
                || (this.Mothership != null && (this.Role == "fighter" || this.Role == "corvette" || this.Role == "scout" || this.Role == "frigate")))
            {
                return 0f;
            }

            //Get Maintanence of ship role
            bool foundMaint = false;
            if (ResourceManager.ShipRoles.ContainsKey(this.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[this.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[this.Role].RaceList[i].ShipType == this.loyalty.data.Traits.ShipType)
                    {
                        maint = ResourceManager.ShipRoles[this.Role].RaceList[i].Upkeep;
                        foundMaint = true;
                        break;
                    }
                }
                if (!foundMaint)
                    maint = ResourceManager.ShipRoles[this.Role].Upkeep;
            }
            else
                return 0f;

            //Modify Maintanence by freighter size
            if(this.Role == "freighter")
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

            //Apply Privatization
            if ((this.Role == "freighter" || this.Role == "platform") && this.loyalty != null && !this.loyalty.isFaction && this.loyalty.data.Privatization)
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
                if (this.Role == "platform")
                    return maint *= 0.5f;
                if (this.shipData.IsShipyard && this.GetTether().Shipyards.Where(shipyard => shipyard.Value.shipData.IsShipyard).Count() > 3)
                    maint *= this.GetTether().Shipyards.Where(shipyard => shipyard.Value.shipData.IsShipyard).Count() - 3;
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
                    if (this.GetAI().inOrbit)
                    {
                        maintModReduction *= .25f;
                    }
                }
                if (this.IsInNeutralSpace && !this.IsInFriendlySpace && !this.inborders)
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

        public float GetMaintCost(Empire empire)
        {
            float maint = 0f;
            string role = this.Role;
            string str = role;
            //bool nonCombat = false;
            //added by gremlin: Maintenance changes
            float maintModReduction = 1;

            //Get Maintanence of ship role
            bool foundMaint = false;
            if (ResourceManager.ShipRoles.ContainsKey(this.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[this.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[this.Role].RaceList[i].ShipType == empire.data.Traits.ShipType)
                    {
                        maint = ResourceManager.ShipRoles[this.Role].RaceList[i].Upkeep;
                        foundMaint = true;
                        break;
                    }
                }
                if (!foundMaint)
                    maint = ResourceManager.ShipRoles[this.Role].Upkeep;
            }
            else
                return 0f;

            //Modify Maintanence by freighter size
            if (this.Role == "freighter")
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

            //Apply Privatization
            if ((this.Role == "freighter" || this.Role == "platform") && empire.data.Privatization)
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
            if (this.Role == "platform")
                this.IsPlatform = true;
            this.Weapons.Clear();
            this.Center = new Vector2(this.Position.X + this.Dimensions.X / 2f, this.Position.Y + this.Dimensions.Y / 2f);
            this.InitFromSave();
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
            if (this.system != null && !this.system.spatialManager.CollidableObjects.Contains((GameplayObject)this))
            {
                this.isInDeepSpace = false;
                this.system.spatialManager.CollidableObjects.Add((GameplayObject)this);
                if (!this.system.ShipList.Contains(this))
                    this.system.ShipList.Add(this);
            }
            else if (this.isInDeepSpace)
            {
                lock (GlobalStats.DeepSpaceLock)
                    UniverseScreen.DeepSpaceManager.CollidableObjects.Add((GameplayObject)this);
            }
            this.FillExternalSlots();
            this.hyperspace = (Cue)null;
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
            this.radius = this.ShipSO.WorldBoundingSphere.Radius * 2f;
        }

        public override void Initialize()
        {
            if (this.Role == "platform")
                this.IsPlatform = true;
            this.SetShipData(this.GetShipData());
            this.VanityName = this.Name;
            this.Weapons.Clear();
            this.Center = new Vector2(this.Position.X + this.Dimensions.X / 2f, this.Position.Y + this.Dimensions.Y / 2f);
            lock (GlobalStats.AddShipLocker)
            {
                if (Ship.universeScreen == null)
                    UniverseScreen.ShipSpatialManager.CollidableObjects.Add((GameplayObject)this);
                else
                    Ship.universeScreen.ShipsToAdd.Add(this);
            }
            this.InitializeModules();
            if (Ship_Game.ResourceManager.ShipsDict.ContainsKey(this.Name) && Ship_Game.ResourceManager.ShipsDict[this.Name].IsPlayerDesign)
                this.IsPlayerDesign = true;
            if (this.AI == null)
                this.InitializeAI();
            this.InitializeStatus();
            this.AI.CombatState = Ship_Game.ResourceManager.ShipsDict[this.Name].shipData.CombatState;
            this.FillExternalSlots();
            this.hyperspace = (Cue)null;
            base.Initialize();
            foreach (ModuleSlot ss in this.ModuleSlotList)
            {
                if (ss.module.ModuleType == ShipModuleType.PowerConduit)
                    ss.module.IconTexturePath = this.GetConduitGraphic(ss, this);
                if (ss.module.IsRepairModule)
                    this.HasRepairModule = true;
                if (ss.module.ModuleType == ShipModuleType.Colony)
                    this.isColonyShip = true;
                if (ss.module.ModuleType == ShipModuleType.Transporter)
                {
                    this.hasTransporter = true;
                    if (ss.module.TransporterOrdnance > 0)
                        this.hasOrdnanceTransporter = true;
                    if (ss.module.TransporterTroopAssault > 0)
                        this.hasAssaultTransporter = true;
                }
                if (ss.module.InstalledWeapon != null && ss.module.InstalledWeapon.isRepairBeam)
                    this.hasRepairBeam = true;
            }
            this.RecalculatePower();
        }

        private void FillExternalSlots()
        {
            this.ExternalSlots.Clear();
            this.ModulesDictionary.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                this.ModulesDictionary.Add(moduleSlot.Position, moduleSlot);
            foreach (KeyValuePair<Vector2, ModuleSlot> keyValuePair in this.ModulesDictionary)
            {
                if ((double)keyValuePair.Value.module.shield_power > 0.0)
                {
                    keyValuePair.Value.module.isExternal = true;
                    this.ExternalSlots.Add(keyValuePair.Value);
                }
                else if (keyValuePair.Value.module.Active)
                {
                    Vector2 key1 = new Vector2(keyValuePair.Key.X, keyValuePair.Key.Y - 16f);
                    if (this.ModulesDictionary.ContainsKey(key1))
                    {
                        if (!this.ModulesDictionary[key1].module.Active)
                        {
                            keyValuePair.Value.module.isExternal = true;
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
                                                    this.ExternalSlots.Add(keyValuePair.Value);
                                                }
                                            }
                                            else
                                            {
                                                keyValuePair.Value.module.isExternal = true;
                                                this.ExternalSlots.Add(keyValuePair.Value);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        keyValuePair.Value.module.isExternal = true;
                                        this.ExternalSlots.Add(keyValuePair.Value);
                                    }
                                }
                            }
                            else
                            {
                                keyValuePair.Value.module.isExternal = true;
                                this.ExternalSlots.Add(keyValuePair.Value);
                            }
                        }
                    }
                    else
                    {
                        keyValuePair.Value.module.isExternal = true;
                        this.ExternalSlots.Add(keyValuePair.Value);
                    }
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
            if (this.isSpooling)
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
            if (this.engineState == Ship.MoveState.Warp)
            {
                return;
            }
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
            if (this.engineState == Ship.MoveState.Warp && (double)Vector2.Distance(this.Center, new Vector2(Ship.universeScreen.camPos.X, Ship.universeScreen.camPos.Y)) < 100000.0 && Ship.universeScreen.camHeight < 250000)
            {
                Cue cue = AudioManager.GetCue(this.GetEndWarpCue());
                cue.Apply3D(Ship.universeScreen.listener, this.emitter);
                cue.Play();
                FTL ftl = new FTL();
                ftl.Center = new Vector2(this.Center.X, this.Center.Y);
                lock (FTLManager.FTLLock)
                    FTLManager.FTLList.Add(ftl);
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

        //added by gremlin Initialize status from deveks mod. 
        public void InitializeStatus()
        {
            #region Variables
            base.Mass = 0f;
            this.Mass += (float)this.Size;
            this.Thrust = 0f;
            this.PowerStoreMax = 0f;
            this.PowerFlowMax = 0f;
            this.ModulePowerDraw = 0f;
            this.ShieldPowerDraw = 0f;
            this.shield_max = 0f;
            this.shield_power = 0f;
            this.armor_max = 0f;
            this.CrewRequired = 0;
            this.CrewSupplied = 0;
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

            string troopType = "Wyvern";
            string tankType = "Wyvern";
            string redshirtType = "Wyvern";

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
                this.mass += moduleSlotList.module.Mass;
                this.Thrust += moduleSlotList.module.thrust;
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
            this.velocityMaximum = this.Thrust / this.mass;
            this.speed = this.velocityMaximum;
            this.rotationRadiansPerSecond = this.speed / (float)this.Size;
            this.ShipMass = this.mass;
            this.shield_power = this.shield_max;
            this.SensorRange += sensorBonus;
            if (this.FTLSpoolTime == 0)
                this.FTLSpoolTime = 3f;
        }

        public void RenderOverlay(SpriteBatch spriteBatch, Rectangle where, bool ShowModules)
        {
            if (Ship_Game.ResourceManager.HullsDict.ContainsKey(this.shipData.Hull) && Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic != "" && !ShowModules)
            {
                Rectangle destinationRectangle = where;
                destinationRectangle.X += 2;
                spriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["SelectionBox Ships/" + Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic], destinationRectangle, Color.White);
                if ((double)this.shield_power > 0.0)
                {
                    float num = (float)byte.MaxValue * (float)this.shield_percent;
                    spriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["SelectionBox Ships/" + Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic + "_shields"], destinationRectangle, new Color(Color.White, (byte)num));
                }
            }
            if (!ShowModules && Ship_Game.ResourceManager.HullsDict[this.shipData.Hull].SelectionGraphic != "" || this.ModuleSlotList.Count == 0)
                return;
            IOrderedEnumerable<ModuleSlot> orderedEnumerable1 = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)this.ModuleSlotList, (Func<ModuleSlot, float>)(slot => slot.Position.X));
            if (Enumerable.Count<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable1) == 0)
                return;
            float num1 = (float)((double)Enumerable.Last<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable1).Position.X - (double)Enumerable.First<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable1).Position.X + 16.0);
            IOrderedEnumerable<ModuleSlot> orderedEnumerable2 = Enumerable.OrderBy<ModuleSlot, float>((IEnumerable<ModuleSlot>)this.ModuleSlotList, (Func<ModuleSlot, float>)(slot => slot.Position.Y));
            float num2 = (float)((double)Enumerable.Last<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable2).Position.Y - (double)Enumerable.First<ModuleSlot>((IEnumerable<ModuleSlot>)orderedEnumerable2).Position.Y + 16.0);
            int num3;
            if ((double)num1 > (double)num2)
            {
                double num4 = (double)num1 / (double)where.Width;
                num3 = (int)num1 / 16 + 1;
            }
            else
            {
                double num4 = (double)num2 / (double)where.Width;
                num3 = (int)num2 / 16 + 1;
            }
            float num5 = (float)(where.Width / num3);
            if ((double)num5 < 2.0)
                num5 = (float)where.Width / (float)num3;
            if ((double)num5 > 10.0)
                num5 = 10f;
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                Vector2 vector2_1 = moduleSlot.module.XMLPosition - new Vector2(264f, 264f);
                Vector2 vector2_2 = new Vector2(vector2_1.X / 16f, vector2_1.Y / 16f) * num5;
                if ((double)Math.Abs(vector2_2.X) > (double)(where.Width / 2) || (double)Math.Abs(vector2_2.Y) > (double)(where.Height / 2))
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
                Color color = (double)moduleSlot.module.Health / (double)moduleSlot.module.HealthMax < 0.899999976158142 ? ((double)moduleSlot.module.Health / (double)moduleSlot.module.HealthMax < 0.649999976158142 ? ((double)moduleSlot.module.Health / (double)moduleSlot.module.HealthMax < 0.449999988079071 ? ((double)moduleSlot.module.Health / (double)moduleSlot.module.HealthMax < 0.150000005960464 ? ((double)moduleSlot.module.Health / (double)moduleSlot.module.HealthMax > 0.150000005960464 || (double)moduleSlot.module.Health <= 0.0 ? Color.Red : Color.Red) : Color.OrangeRed) : Color.Yellow) : Color.GreenYellow) : Color.Green;
                Primitives2D.FillRectangle(spriteBatch, rect, color);
            }
        }

        public void ScrambleAssaultShipsORIG()
        {
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                if (moduleSlot.module != null && moduleSlot.module.ModuleType == ShipModuleType.Hangar && (moduleSlot.module.IsTroopBay && this.TroopList.Count > 0) && (moduleSlot.module.GetHangarShip() == null && (double)moduleSlot.module.hangarTimer <= 0.0))
                {
                    moduleSlot.module.LaunchBoardingParty(this.TroopList[0]);
                    this.TroopList.RemoveAt(0);
                }
            }
        }
        //added by gremlin deveksmod scramble assault ships
        public void ScrambleAssaultShips()
        {
            foreach (ModuleSlot slot in this.ModuleSlotList.Where(slot => slot.module != null && slot.module.ModuleType == ShipModuleType.Hangar && slot.module.IsTroopBay && this.TroopList.Count > 0 && slot.module.GetHangarShip() == null && slot.module.hangarTimer <= 0f))
            {
                //if (slot.module == null || slot.module.ModuleType != ShipModuleType.Hangar || !slot.module.IsTroopBay || this.TroopList.Count <= 0 || slot.module.GetHangarShip() != null || slot.module.hangarTimer > 0f)
                //{
                //    continue;
                //}

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
                    if (shipModule.GetHangarShip() != null)
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
                    if (shipModule.GetHangarShip() != null)
                        shipModule.GetHangarShip().ReturnToHangar();
                }
                catch
                {
                }
            }
        }

        public void LoadInitializeStatus()
        {
            this.mass = 0.0f;
            this.Thrust = 0.0f;
            this.PowerStoreMax = 0.0f;
            this.PowerFlowMax = 0.0f;
            this.ModulePowerDraw = 0.0f;
            this.shield_max = 0.0f;
            this.shield_power = 0.0f;
            this.armor_max = 0.0f;
            this.CrewRequired = 0;
            this.CrewSupplied = 0;
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
                double num1 = (double)ship1.mass + (double)moduleSlot.module.Mass;
                ship1.mass = (float)num1;
                this.Thrust += moduleSlot.module.thrust;
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
            this.velocityMaximum = this.Thrust / this.mass;
            this.speed = this.velocityMaximum;
            this.rotationRadiansPerSecond = this.speed / 700f;
            this.number_Alive_Internal_slots = this.number_Internal_slots;
            this.ShipMass = this.mass;
            if (this.FTLSpoolTime == 0)
                this.FTLSpoolTime = 3f;
        }

        public static Ship LoadSavedShip(ShipData data)
        {
            Ship parent = new Ship();
            if (data.Name == "Left Right Test")
                parent.Position = new Vector2(200f, 200f);
            parent.Position = new Vector2(200f, 200f);
            parent.Name = data.Name;
            parent.Level = (int)data.Level;
            parent.Role = data.Role;
            parent.ModelPath = data.ModelPath;
            parent.ModuleSlotList = Ship.LoadSlotDataListToSlotList(data.ModuleSlotList, parent);
            foreach (ShipToolScreen.ThrusterZone thrusterZone in data.ThrusterList)
                parent.ThrusterList.Add(new Thruster()
                {
                    tscale = thrusterZone.scale,
                    XMLPos = thrusterZone.Position,
                    Parent = parent
                });
            return parent;
        }

        public static LinkedList<ModuleSlot> LoadSlotDataListToSlotList(List<ModuleSlotData> dataList, Ship parent)
        {
            LinkedList<ModuleSlot> linkedList = new LinkedList<ModuleSlot>();
            foreach (ModuleSlotData moduleSlotData in dataList)
            {
                ModuleSlot moduleSlot = new ModuleSlot();
                moduleSlot.ModuleHealth = moduleSlotData.Health;
                moduleSlot.Shield_Power = moduleSlotData.Shield_Power;
                moduleSlot.Position = moduleSlotData.Position;
                moduleSlot.facing = moduleSlotData.facing;
                moduleSlot.state = moduleSlotData.state;
                moduleSlot.Restrictions = moduleSlotData.Restrictions;
                moduleSlot.InstalledModuleUID = moduleSlotData.InstalledModuleUID;
                moduleSlot.HangarshipGuid = moduleSlotData.HangarshipGuid;
                if (moduleSlotData.SlotOptions != null)
                    moduleSlot.SlotOptions = moduleSlotData.SlotOptions;
                linkedList.AddLast(moduleSlot);
            }
            return linkedList;
        }

        public static Ship CreateShipFromShipData(ShipData data)
        {
            Ship parent = new Ship();
            parent.Position = new Vector2(200f, 200f);
            parent.Name = data.Name;
            parent.Level = (int)data.Level;
            parent.experience = (int)data.experience;
            parent.Role = data.Role;
            parent.ModelPath = data.ModelPath;
            parent.ModuleSlotList = Ship.SlotDataListToSlotList(data.ModuleSlotList, parent);
            
            foreach (ShipToolScreen.ThrusterZone thrusterZone in data.ThrusterList)
                parent.ThrusterList.Add(new Thruster()
                {
                    tscale = thrusterZone.scale,
                    XMLPos = thrusterZone.Position,
                    Parent = parent
                });
            return parent;
        }

        public static LinkedList<ModuleSlot> SlotDataListToSlotList(List<ModuleSlotData> dataList, Ship parent)
        {
            LinkedList<ModuleSlot> linkedList = new LinkedList<ModuleSlot>();
            foreach (ModuleSlotData moduleSlotData in dataList)
                linkedList.AddLast(new ModuleSlot()
                {
                    Position = moduleSlotData.Position,
                    state = moduleSlotData.state,
                    facing = moduleSlotData.facing,
                    Restrictions = moduleSlotData.Restrictions,
                    HangarshipGuid = moduleSlotData.HangarshipGuid,
                    InstalledModuleUID = moduleSlotData.InstalledModuleUID,
                    SlotOptions = moduleSlotData.SlotOptions
                });
            return linkedList;
        }

        public virtual void InitializeModules()
        {
            this.ModulesInitialized = true;
            this.Weapons.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                moduleSlot.SetParent(this);
                moduleSlot.Initialize();
            }
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
                moduleSlot.Initialize();
                if (moduleSlot.module == null)
                {
                    list.Add(moduleSlot);
                }
                else
                {
                    moduleSlot.module.Health = moduleSlot.ModuleHealth;
                    moduleSlot.module.shield_power = moduleSlot.Shield_Power;
                    if ((double)moduleSlot.module.Health == 0.0)
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
            List<ModuleSlot> list = new List<ModuleSlot>();
            if (this.Name == "Left Right Test")
                this.Weapons.Clear();
            foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
            {
                moduleSlot.SetParent(this);
                if (!Ship_Game.ResourceManager.ShipModulesDict.ContainsKey(moduleSlot.InstalledModuleUID))
                    return false;
                moduleSlot.InitializeForLoad();
                moduleSlot.module.Health = moduleSlot.ModuleHealth;
                moduleSlot.module.shield_power = moduleSlot.Shield_Power;
                if ((double)moduleSlot.module.Health == 0.0)
                    moduleSlot.module.Active = false;
            }
            this.RecalculatePower();
            return true;
        }

        public virtual void LoadContent(ContentManager contentManager)
        {
        }

        public override void Update(float elapsedTime)
        {
            if (!this.Active)
                return;
            if (!GlobalStats.WarpInSystem && this.system != null)
                this.InhibitedTimer = 1f;
            else if ((double)Ship.universeScreen.FTLModifier < 1.0 && this.system != null && (this.engineState == Ship.MoveState.Warp && (double)this.GetFTLSpeed() < (double)this.GetSTLSpeed()))
                this.HyperspaceReturn();
            if (this.system != null && this.system.isVisible || this.system == null)
            {
                BoundingSphere sphere = new BoundingSphere(new Vector3(this.Position, 0.0f), 2000f);
                if (Ship.universeScreen.Frustum.Contains(sphere) != ContainmentType.Disjoint && Ship.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    this.InFrustum = true;
                    this.ShipSO.Visibility = ObjectVisibility.Rendered;
                }
                else
                {
                    this.InFrustum = false;
                    this.ShipSO.Visibility = ObjectVisibility.None;
                }
                if ((double)this.ScuttleTimer > -1.0)
                {
                    this.ScuttleTimer -= elapsedTime;
                    if ((double)this.ScuttleTimer <= 0.0)
                        this.Die((GameplayObject)null, true);
                }
            }
            else
            {
                this.InFrustum = false;
                this.ShipSO.Visibility = ObjectVisibility.None;
            }
            foreach (ProjectileTracker projectileTracker in (List<ProjectileTracker>)this.ProjectilesFired)
            {
                projectileTracker.Timer -= elapsedTime;
                if ((double)projectileTracker.Timer <= 0.0)
                    this.ProjectilesFired.QueuePendingRemoval(projectileTracker);
            }
            this.ProjectilesFired.ApplyPendingRemovals();
            this.ShieldRechargeTimer += elapsedTime;
            this.InhibitedTimer -= elapsedTime;
            this.Inhibited = (double)this.InhibitedTimer > 0.0;
            if (this.Inhibited && this.engineState == Ship.MoveState.Warp)
            {
                this.HyperspaceReturn();
            }
            if (this.TetheredTo != null)
            {
                this.Position = this.TetheredTo.Position + this.TetherOffset;
                this.Center = this.TetheredTo.Position + this.TetherOffset;
            }
            if (this.Mothership != null && !this.Mothership.Active)
                this.Mothership = (Ship)null;
            if (this.dying)
            {
                this.ThrusterList.Clear();
                this.dietimer -= elapsedTime;
                if ((double)this.dietimer <= 1.89999997615814 && this.dieCue == null && this.InFrustum)
                {
                    if (this.Size < 80)
                    {
                        this.dieCue = AudioManager.GetCue("sd_explosion_ship_warpdet_small");
                        this.dieCue.Apply3D(Ship.universeScreen.listener, this.emitter);
                        this.dieCue.Play();
                    }
                    else if (this.Size >= 80 && this.Size < 250)
                    {
                        this.dieCue = AudioManager.GetCue("sd_explosion_ship_warpdet_medium");
                        this.dieCue.Apply3D(Ship.universeScreen.listener, this.emitter);
                        this.dieCue.Play();
                    }
                    else
                    {
                        this.dieCue = AudioManager.GetCue("sd_explosion_ship_warpdet_large");
                        this.dieCue.Apply3D(Ship.universeScreen.listener, this.emitter);
                        this.dieCue.Play();
                    }
                }
                if ((double)this.dietimer <= 0.0)
                {
                    this.reallyDie = true;
                    this.Die(this.LastDamagedBy, true);
                    return;
                }
                else
                {
                    if ((double)this.Velocity.Length() > (double)this.velocityMaximum)
                        this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
                    Ship ship1 = this;
                    Vector2 vector2_1 = ship1.Position + this.Velocity * elapsedTime;
                    ship1.Position = vector2_1;
                    Ship ship2 = this;
                    Vector2 vector2_2 = ship2.Center + this.Velocity * elapsedTime;
                    ship2.Center = vector2_2;
                    int num1 = (int)(this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, 60f);
                    if (num1 >= 57 && this.InFrustum)
                    {
                        float radius = this.ShipSO.WorldBoundingSphere.Radius;
                        Vector3 vector3 = new Vector3((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, this.Radius), (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, this.Radius), (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, this.Radius));
                        ExplosionManager.AddExplosion(vector3, radius, 2.5f, 0.2f);
                        Ship.universeScreen.flash.AddParticleThreadA(vector3, Vector3.Zero);
                    }
                    if (num1 >= 40)
                    {
                        Vector3 position = new Vector3((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, this.Radius), (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, this.Radius), (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(0.0f, this.Radius));
                        Ship.universeScreen.sparks.AddParticleThreadA(position, Vector3.Zero);
                    }
                    this.yRotation += this.xdie * elapsedTime;
                    this.xRotation += this.ydie * elapsedTime;
                    Ship ship3 = this;
                    double num2 = (double)ship3.Rotation + (double)this.zdie * (double)elapsedTime;
                    ship3.Rotation = (float)num2;
                    if (this.ShipSO == null)
                        return;
                    if (Ship.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.inSensorRange)
                    {
                        this.ShipSO.World = Matrix.Identity * Matrix.CreateRotationY(this.yRotation) * Matrix.CreateRotationX(this.xRotation) * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(new Vector3(this.Center, 0.0f));
                        if (this.shipData.Animated)
                        {
                            this.ShipSO.SkinBones = this.animationController.SkinnedBoneTransforms;
                            this.animationController.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                        }
                    }
                    for (int index = 0; index < this.Projectiles.Count; ++index)
                    {
                        Projectile projectile = this.Projectiles[index];
                        if (projectile != null)
                        {
                            if (projectile.Active)
                                projectile.Update(elapsedTime);
                            else
                                this.Projectiles.QueuePendingRemoval(projectile);
                        }
                    }
                    this.emitter.Position = new Vector3(this.Center, 0.0f);
                    foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                    //Parallel.ForEach<ModuleSlot>(this.ModuleSlotList, moduleSlot =>
                    {
                        moduleSlot.module.UpdateWhileDying(elapsedTime);
                    }//);
                }
            }
            else if (!this.dying)
            {
                if (this.system != null && (double)elapsedTime > 0.0)
                {
                    foreach (Planet p in this.system.PlanetList)
                    {
                        if ((double)Vector2.Distance(p.Position, this.Center) < 3000.0)
                        {
                            if (this.loyalty == Ship.universeScreen.player&&!p.ExploredDict[this.loyalty])
                            {
                                foreach (Building building in p.BuildingList)
                                {
                                    if (building.EventTriggerUID != "")
                                        Ship.universeScreen.NotificationManager.AddFoundSomethingInteresting(p);
                                }
                            }
                            if (!p.ExploredDict[this.loyalty])
                            {
                                p.ExploredDict[this.loyalty] = true;
                                foreach (Building building in p.BuildingList)
                                {
                                    if (building.EventTriggerUID != "" && this.loyalty != Ship.universeScreen.player && p.Owner == null)
                                    {
                                        MilitaryTask militaryTask = new MilitaryTask();
                                        militaryTask.AO = p.Position;
                                        militaryTask.AORadius = 50000f;
                                        militaryTask.SetTargetPlanet(p);
                                        militaryTask.type = MilitaryTask.TaskType.Exploration;
                                        militaryTask.SetEmpire(this.loyalty);
                                        lock (GlobalStats.TaskLocker)
                                            this.loyalty.GetGSAI().TaskList.Add(militaryTask);
                                    }
                                }
                                //added by gremlin put shahmatts exploration notifications here
                            }
                        }
                    }
                    if (this.InCombat && this.GetAI().Target != null && this.GetAI().Target.GetSystem() != null && this.GetAI().Target.GetSystem() == this.GetSystem())
                    {
                        this.system.CombatInSystem = true;
                        this.system.combatTimer = 15f;
                    }
                }
                if (this.disabled)
                {
                    for (int index = 0; index < 5; ++index)
                    {
                        Vector3 vector3 = new Vector3((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween((float)(-(double)this.radius / 3.0), this.radius / 3f), (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween((float)(-(double)this.radius / 3.0), this.radius / 3f), 0.0f);
                        Ship.universeScreen.lightning.AddParticleThreadA(new Vector3(this.Center, 0.0f) + vector3, Vector3.Zero);
                    }
                }
                Ship ship1 = this;
                double num1 = (double)ship1.Rotation + (double)this.RotationalVelocity * (double)elapsedTime;
                ship1.Rotation = (float)num1;
                if ((double)Math.Abs(this.RotationalVelocity) > 0.0)
                    this.isTurning = true;
                if (!this.isSpooling && this.Afterburner != null && this.Afterburner.IsPlaying)
                    this.Afterburner.Stop(AudioStopOptions.Immediate);
                this.ClickTimer -= elapsedTime;
                if ((double)this.ClickTimer < 0.0)
                    this.ClickTimer = 10f;
                if (this.Active)
                {
                    this.InCombatTimer -= elapsedTime;
                    if ((double)this.InCombatTimer > 0.0)
                    {
                        this.InCombat = true;
                    }
                    else
                    {
                        if (this.InCombat)
                            this.InCombat = false;
                        try
                        {
                            if (this.AI.State == AIState.Combat)
                            {
                                if (this.loyalty != Ship.universeScreen.player)
                                    this.AI.State = AIState.AwaitingOrders;
                            }
                        }
                        catch
                        {
                        }
                    }
                    this.Velocity.Length();
                    Ship ship2 = this;
                    Vector2 vector2_1 = ship2.Position + this.Velocity * elapsedTime;
                    ship2.Position = vector2_1;
                    Ship ship3 = this;
                    Vector2 vector2_2 = ship3.Center + this.Velocity * elapsedTime;
                    ship3.Center = vector2_2;
                    this.UpdateShipStatus(elapsedTime);
                    if (!this.Active)
                        return;
                    if (!this.disabled && this.hasCommand && !Ship.universeScreen.Paused)
                        this.AI.Update(elapsedTime);
                    if (this.InFrustum)
                    {
                        if (this.ShipSO == null)
                            return;
                        this.ShipSO.World = Matrix.Identity * Matrix.CreateRotationY(this.yRotation) * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(new Vector3(this.Center, 0.0f));
                        if (this.shipData.Animated && this.animationController != null)
                        {
                            this.ShipSO.SkinBones = this.animationController.SkinnedBoneTransforms;
                            this.animationController.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
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
                                    if ((double)thruster.heat < (double)num2)
                                        thruster.heat += 0.06f;
                                    this.pointat = new Vector3(vector2_3.X, vector2_3.Y, 0.0f);
                                    this.scalefactors = new Vector3(thruster.tscale, thruster.tscale, thruster.tscale);
                                    thruster.update(thruster.WorldPos, this.pointat, this.scalefactors, thruster.heat, 0.004f, Color.OrangeRed, Color.LightBlue, Ship.universeScreen.camPos);
                                }
                                else
                                {
                                    if ((double)thruster.heat < (double)num2)
                                        thruster.heat += 0.06f;
                                    if ((double)thruster.heat > 0.600000023841858)
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
                    if (this.isSpooling && !this.Inhibited)
                    {
                        this.JumpTimer -= elapsedTime;
                        //task gremlin move fighter recall here.
                        if ((double)this.JumpTimer <= 4.0) // let's see if we can sync audio to behaviour with new timers
                        {
                            if ((double)Vector2.Distance(this.Center, new Vector2(Ship.universeScreen.camPos.X, Ship.universeScreen.camPos.Y)) < 100000.0 && (this.Jump == null || this.Jump != null && !this.Jump.IsPlaying) && Ship.universeScreen.camHeight < 250000)
                            {
                                this.Jump = AudioManager.GetCue(this.GetStartWarpCue());
                                this.Jump.Apply3D(GameplayObject.audioListener, this.emitter);
                                this.Jump.Play();
                            }
                        }
                        if ((double)this.JumpTimer <= 0.1)
                        {
                            if (this.engineState == Ship.MoveState.Sublight && (!this.Inhibited && (double)this.GetFTLSpeed() > (double)this.GetSTLSpeed()))
                            {
                                FTL ftl = new FTL();
                                ftl.Center = new Vector2(this.Center.X, this.Center.Y);
                                lock (FTLManager.FTLLock)
                                    FTLManager.FTLList.Add(ftl);
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
                    this.emitter.Position = new Vector3(this.Center, 0.0f);
                }
                if (elapsedTime > 0.0f)
                {
                    //task gremlin look at parallel here for weapons
                    foreach (Projectile projectile in (List<Projectile>)this.Projectiles)
                    //Parallel.ForEach<Projectile>(this.projectiles, projectile =>
                    {
                        if (projectile !=null && projectile.Active)
                            projectile.Update(elapsedTime);
                        else
                            this.Projectiles.QueuePendingRemoval(projectile);
                     }//);
                    foreach (Beam beam in (List<Beam>)this.beams)
                    //Parallel.ForEach<Beam>(this.beams, beam =>
                    {
                        Vector2 origin = new Vector2();
                        if (beam.moduleAttachedTo != null)
                        {
                            ShipModule shipModule = beam.moduleAttachedTo;
                            origin = (int)shipModule.XSIZE != 1 || (int)shipModule.YSIZE != 3 ? ((int)shipModule.XSIZE != 2 || (int)shipModule.YSIZE != 5 ? new Vector2(shipModule.Center.X - 8f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2)) : new Vector2(shipModule.Center.X - 80f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2))) : new Vector2(shipModule.Center.X - 50f + (float)(16 * (int)shipModule.XSIZE / 2), shipModule.Center.Y - 8f + (float)(16 * (int)shipModule.YSIZE / 2));
                            Vector2 target = new Vector2(shipModule.Center.X - 8f, shipModule.Center.Y - 8f);
                            float angleToTarget = HelperFunctions.findAngleToTarget(origin, target);
                            Vector2 angleAndDistance = HelperFunctions.findPointFromAngleAndDistance(shipModule.Center, MathHelper.ToDegrees(shipModule.Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));
                            float num2 = (float)((int)shipModule.XSIZE * 16 / 2);
                            float num3 = (float)((int)shipModule.YSIZE * 16 / 2);
                            float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num2, 2.0) + (float)Math.Pow((double)num3, 2.0)));
                            float radians = 3.141593f - (float)Math.Asin((double)num2 / (double)distance) + shipModule.GetParent().Rotation;
                            origin = HelperFunctions.findPointFromAngleAndDistance(angleAndDistance, MathHelper.ToDegrees(radians), distance);
                        }
                        int Thickness = this.system != null ? (int)this.system.RNG.RandomBetween((float)beam.thickness - 0.25f * (float)beam.thickness, (float)beam.thickness + 0.1f * (float)beam.thickness) : (int)Ship.universeScreen.DeepSpaceRNG.RandomBetween((float)beam.thickness - 0.25f * (float)beam.thickness, (float)beam.thickness + 0.1f * (float)beam.thickness);
                        beam.Update(beam.moduleAttachedTo != null ? origin : beam.owner.Center, beam.followMouse ? Ship.universeScreen.mouseWorldPos : beam.Destination, Thickness, Ship.universeScreen.view, Ship.universeScreen.projection, elapsedTime);
                        if ((double)beam.duration < 0.0 && !beam.infinite)
                            this.beams.QueuePendingRemoval(beam);
                    }//);
                    this.beams.ApplyPendingRemovals();
                }
            }
            this.Projectiles.ApplyPendingRemovals();
            this.VelocityLast = this.Velocity;
            base.Update(elapsedTime);
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
            }
        }

        public ShipData ToShipData()
        {
            ShipData shipData = new ShipData();
            shipData.Name = this.Name;
            shipData.Level = (byte)this.Level;
            shipData.experience = (byte)this.experience;
            shipData.Role = this.Role;
            shipData.IsShipyard = this.GetShipData().IsShipyard;
            shipData.IsOrbitalDefense = this.GetShipData().IsOrbitalDefense;
            shipData.Animated = this.GetShipData().Animated;
            shipData.CombatState = this.GetAI().CombatState;
            shipData.ModelPath = this.GetShipData().ModelPath;
            shipData.ModuleSlotList = this.ConvertToData(this.ModuleSlotList);
            shipData.ThrusterList = new List<ShipToolScreen.ThrusterZone>();
            shipData.MechanicalBoardingDefense = this.MechanicalBoardingDefense;
            foreach (Thruster thruster in this.ThrusterList)
                shipData.ThrusterList.Add(new ShipToolScreen.ThrusterZone()
                {
                    scale = thruster.tscale,
                    Position = thruster.XMLPos
                });
            return shipData;
        }

        private List<ModuleSlotData> ConvertToData(LinkedList<ModuleSlot> slotList)
        {
            List<ModuleSlotData> list = new List<ModuleSlotData>();
            foreach (ModuleSlot moduleSlot in slotList)
            {
                ModuleSlotData moduleSlotData = new ModuleSlotData();
                moduleSlotData.Position = moduleSlot.Position;
                moduleSlotData.InstalledModuleUID = moduleSlot.InstalledModuleUID;
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
            if ((double)elapsedTime == 0.0)
                return;
            if (Ship.universeScreen.GravityWells && this.system != null && !this.inborders)
            {
                bool flag = false;
                foreach (Planet planet in this.system.PlanetList)
                {
                    if ((double)Vector2.Distance(this.Position, planet.Position) < (double)(GlobalStats.GravityWellRange * (1 + ((Math.Log(planet.scale))/1.5) )))
                    {
                        flag = true;
                        this.InhibitedTimer = 1.5f;
                        break;
                    }
                }
                if (!flag)
                    this.InhibitedTimer = 0.0f;
            }
            this.MoveModulesTimer -= elapsedTime;
            this.updateTimer -= elapsedTime;
            //Disable if enough EMP damage
            --this.EMPDamage;
            if ((double)this.EMPDamage < 0.0)
                this.EMPDamage = 0.0f;
            else if ((double)this.EMPDamage > (double)this.Size + (double)this.BonusEMP_Protection)
                this.disabled = true;
            this.CargoMass = 0.0f;
            if ((double)this.rotation > 2.0 * Math.PI)
            {
                Ship ship = this;
                double num = (double)ship.rotation - 6.28318548202515;
                ship.rotation = (float)num;
            }
            if ((double)this.rotation < 0.0)
            {
                Ship ship = this;
                double num = (double)ship.rotation + 6.28318548202515;
                ship.rotation = (float)num;
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
                    this.TroopBoardingDefense += (float)troop.Strength;
            }
            if ((double)this.updateTimer < 0.0)
            {
                if ((this.InCombat && !this.disabled && this.hasCommand || this.PlayerShip) && this.Weapons.Count > 0)
                {
                    IOrderedEnumerable<Weapon> orderedEnumerable = Enumerable.OrderByDescending<Weapon, float>((IEnumerable<Weapon>)this.Weapons, (Func<Weapon, float>)(weapon => weapon.Range));
                    bool flag = false;
                    foreach (Weapon weapon in (IEnumerable<Weapon>)orderedEnumerable)
                    {
                        if ((double)weapon.DamageAmount > 0.0 && !flag)
                        {
                            this.maxWeaponsRange = weapon.Range;
                            flag = true;
                        }
                        weapon.fireDelay = Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay;
                        //Added by McShooterz: weapon tag modifiers with check if mod uses them
                        if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useWeaponModifiers)
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
                                weapon.fireDelay += - Ship_Game.ResourceManager.WeaponsDict[weapon.UID].fireDelay * this.loyalty.data.WeaponTags["Drone"].Rate;
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
                        if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses)
                        {
                            HullBonus mod;
                            if (Ship_Game.ResourceManager.HullBonuses.TryGetValue(this.shipData.Hull, out mod))
                                weapon.fireDelay *= 1f - mod.FireRateBonus;
                        }
                    }
                }
                foreach (Empire index1 in EmpireManager.EmpireList)
                {
                    if (index1 != this.loyalty && !this.loyalty.GetRelations()[index1].Treaty_OpenBorders)
                    {
                        for (int index2 = 0; index2 < index1.Inhibitors.Count; ++index2)
                        {
                            Ship ship = index1.Inhibitors[index2];
                            if (ship != null && (double)Vector2.Distance(this.Center, ship.Position) <= (double)ship.InhibitionRadius)
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
                this.inSensorRange = false;
                if (Ship.universeScreen.Debug || this.loyalty == Ship.universeScreen.player || this.loyalty != Ship.universeScreen.player && Ship.universeScreen.player.GetRelations()[this.loyalty].Treaty_Alliance)
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
                {
                    this.Health = 0f;
                    float sensorBonus = 0f;
                    if (this.shipStatusChanged)
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
                        this.InhibitionRadius = 0f;
                        this.OrdAddedPerSecond = 0f;
                        this.WarpDraw = 0f;
                        this.HealPerTurn = 0;
                        this.ECMValue = 0f;
                        this.FTLSpoolTime = 0f;
                        this.hasCommand = this.IsPlatform;
                    }
                    foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                    {
                        //Get total internal slots
                        if (moduleSlot.Restrictions == Restrictions.I && moduleSlot.module.Active)
                            ++this.number_Alive_Internal_slots;
                        if (moduleSlot.module.ModuleType == ShipModuleType.Dummy)
                            continue;
                        this.Health = (float)((double)this.Health + (double)moduleSlot.module.Health);
                        if (this.shipStatusChanged)
                        {
                            this.RepairRate += moduleSlot.module.BonusRepairRate;
                            if ((double)moduleSlot.module.Mass < 0.0 && moduleSlot.Powered)
                            {
                                Ship ship3 = this;
                                double num3 = (double)ship3.Mass + (double)moduleSlot.module.Mass;
                                ship3.Mass = (float)num3;
                            }
                            else if ((double)moduleSlot.module.Mass > 0.0)
                            {
                                Ship ship3 = this;
                                double num3 = (double)ship3.Mass + (double)moduleSlot.module.Mass;
                                ship3.Mass = (float)num3;
                            }
                            if (moduleSlot.module.Active && (moduleSlot.module.Powered || moduleSlot.module.PowerDraw == 0))
                            {
                                //Checks to see if there is an active command module
                                if (!this.hasCommand && moduleSlot.module.IsCommandModule)
                                    this.hasCommand = true;
                                this.OrdinanceMax += (float)moduleSlot.module.OrdinanceCapacity;
                                this.CargoSpace_Max += moduleSlot.module.Cargo_Capacity;
                                this.InhibitionRadius += moduleSlot.module.InhibitionRadius;
                                this.BonusEMP_Protection += moduleSlot.module.EMP_Protection;
                                if ((double)moduleSlot.module.SensorRange > (double)this.SensorRange)
                                    this.SensorRange = moduleSlot.module.SensorRange;
                                if ((double)moduleSlot.module.SensorBonus > (double)sensorBonus)
                                    sensorBonus = moduleSlot.module.SensorBonus;
                                if ((double)moduleSlot.module.shield_power_max > 0f)
                                {
                                    this.shield_max += moduleSlot.module.GetShieldsMax();
                                    this.ShieldPowerDraw += moduleSlot.module.PowerDraw;
                                    this.Shields.Add(moduleSlot.module);
                                }
                                else
                                    this.ModulePowerDraw += moduleSlot.module.PowerDraw;
                                this.Thrust += moduleSlot.module.thrust;
                                this.WarpThrust += (float)moduleSlot.module.WarpThrust;
                                this.TurnThrust += (float)moduleSlot.module.TurnThrust;
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
                                if (moduleSlot.module.PowerStoreMax != 0)
                                    this.PowerStoreMax += moduleSlot.module.PowerStoreMax;
                                if (moduleSlot.module.PowerFlowMax != 0)
                                    this.PowerFlowMax += moduleSlot.module.PowerFlowMax;
                                this.WarpDraw += moduleSlot.module.PowerDrawAtWarp;
                                if (moduleSlot.module.FTLSpoolTime > this.FTLSpoolTime)
                                    this.FTLSpoolTime = moduleSlot.module.FTLSpoolTime;
                            }
                        }
                    }
                    //Update max health due to bonuses that increase module health
                    if (this.Health > this.HealthMax)
                        this.HealthMax = this.Health;
                    if (this.shipStatusChanged)
                    {
                        this.SensorRange += sensorBonus;
                        //Apply modifiers to stats
                        if (this.loyalty != null)
                        {
                            this.Mass *= this.loyalty.data.MassModifier;
                            this.RepairRate += (float)((double)this.RepairRate * (double)this.Level * 0.05) + this.RepairRate * this.loyalty.data.Traits.RepairMod;
                            this.PowerFlowMax += this.PowerFlowMax * this.loyalty.data.PowerFlowMod;
                            this.PowerStoreMax += this.PowerStoreMax * this.loyalty.data.FuelCellModifier;
                            this.SensorRange *= this.loyalty.data.SensorModifier;
                        }
                        if (this.FTLSpoolTime == 0)
                            this.FTLSpoolTime = 3f;
                        //Hull bonuses
                        if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses)
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
                foreach (ModuleSlot slot in this.ModuleSlotList)
                    slot.module.Update(1f);
                //Check Current Shields
                if (this.engineState == Ship.MoveState.Warp)
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
                    if (!this.InCombat || GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useCombatRepair)
                    {
                        //Added by McShooterz: Priority repair
                        float repairTracker = this.InCombat ? this.RepairRate * 0.1f : this.RepairRate;
                        IEnumerable<ModuleSlot> damagedModules = this.ModuleSlotList.AsParallel().Where(moduleSlot => moduleSlot.module.ModuleType != ShipModuleType.Dummy && moduleSlot.module.Health < moduleSlot.module.HealthMax).OrderBy(moduleSlot => HelperFunctions.ModulePriority(moduleSlot.module)).AsEnumerable();
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
                    for (int index = 0; (double)index < (double)this.MechanicalBoardingDefense; ++index)
                    {
                        if ((this.system != null ? (double)this.system.RNG.RandomBetween(0.0f, 100f) : (double)Ship.universeScreen.DeepSpaceRNG.RandomBetween(0.0f, 100f)) <= 60.0)
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
                                if ((this.system != null ? (double)this.system.RNG.RandomBetween(0.0f, 100f) : (double)Ship.universeScreen.DeepSpaceRNG.RandomBetween(0.0f, 100f)) >= (double)troop.BoardingStrength)
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
                                if ((this.system != null ? (double)this.system.RNG.RandomBetween(0.0f, 100f) : (double)Ship.universeScreen.DeepSpaceRNG.RandomBetween(0.0f, 100f)) >= (double)troop.BoardingStrength)
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
                            if ((double)this.MechanicalBoardingDefense < 0.0)
                                this.MechanicalBoardingDefense = 0.0f;
                        }
                    }
                    OwnTroops.Clear();
                    foreach (Troop troop in this.TroopList)
                    {
                        if (troop.GetOwner() == this.loyalty)
                            OwnTroops.Add(troop);
                    }
                    if (OwnTroops.Count == 0 && (double)this.MechanicalBoardingDefense <= 0.0)
                    {
                        this.loyalty.GetShips().QueuePendingRemoval(this);
                        this.loyalty = EnemyTroops[0].GetOwner();
                        this.loyalty.AddShipNextFrame(this);
                        if (this.fleet != null)
                        {
                            this.fleet.Ships.Remove(this);
                            this.fleet = (Fleet)null;
                        }
                        this.AI.ClearOrdersNext = true;
                        this.AI.State = AIState.AwaitingOrders;
                    }
                }
                this.UpdateSystem(elapsedTime);
                this.updateTimer = 1f;
                if (this.NeedRecalculate)
                {
                    this.RecalculatePower();
                    this.NeedRecalculate = false;
                }
                //Change FTL modifier for ship based on solar system
                if (this.system != null)
                {
                    bool modified = false;
                    if (Ship.universeScreen.FTLModifier < 1f)
                    {
                        this.FTLmodifier = Ship.universeScreen.FTLModifier;
                        modified = true;
                    }
                    if (Ship.universeScreen.EnemyFTLModifier < Ship.universeScreen.FTLModifier && this.system.OwnerList.Count() > 0 && !this.system.OwnerList.Contains(this.loyalty))
                    {
                        bool friendlySystem = false;
                        foreach (Empire empire in this.system.OwnerList)
                        {
                            if (this.loyalty.GetRelations()[empire].Treaty_OpenBorders)
                            {
                                friendlySystem = true;
                                break;
                            }
                        }
                        if (!friendlySystem)
                        {
                            this.FTLmodifier = Ship.universeScreen.EnemyFTLModifier;
                            modified = true;
                        }
                    }
                    if (!modified)
                        this.FTLmodifier = 1f;
                }
                else
                    this.FTLmodifier = 1f;
                //Apply inboarders bonus through ftl modifier
                if (this.inborders && this.loyalty.data.Traits.InBordersSpeedBonus > 0)
                    this.FTLmodifier += this.FTLmodifier * this.loyalty.data.Traits.InBordersSpeedBonus;
            }
            else if (this.InFrustum && Ship.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView || (double)this.MoveModulesTimer > 0.0 || this.InCombat && GlobalStats.ForceFullSim)
            {
                if ((double)elapsedTime > 0.0)
                {
                    this.UpdatedModulesOnce = false;
                    if (this.Velocity != Vector2.Zero || this.isTurning || this.TetheredTo != null)
                    {
                        float cos = (float)Math.Cos((double)this.Rotation);
                        float sin = (float)Math.Sin((double)this.Rotation);
                        float tan = (float)Math.Tan((double)this.yRotation);
                        foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                        //Parallel.ForEach(this.ModuleSlotList, moduleSlot =>
                        {
                            ++GlobalStats.ModuleUpdates;
                            moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                        }
                    }
                }
                else if ((double)elapsedTime < 0.0 && !this.UpdatedModulesOnce)
                {
                    float cos = (float)Math.Cos((double)this.Rotation);
                    float sin = (float)Math.Sin((double)this.Rotation);
                    float tan = (float)Math.Tan((double)this.yRotation);
                    foreach (ModuleSlot moduleSlot in this.ModuleSlotList)
                    {
                        ++GlobalStats.ModuleUpdates;
                        moduleSlot.module.UpdateEveryFrame(elapsedTime, cos, sin, tan);
                    }
                    this.UpdatedModulesOnce = true;
                }
            }
            if ((double)this.Ordinance > (double)this.OrdinanceMax)
                this.Ordinance = this.OrdinanceMax;
            this.percent = this.number_Alive_Internal_slots / this.number_Internal_slots;
            if ((double)this.percent < 0.35)
                this.Die(this.LastDamagedBy, false);
            if ((double)this.Mass < (double)(this.Size / 2))
                this.Mass = (float)(this.Size / 2);
            this.PowerCurrent -= this.PowerDraw * elapsedTime;
            if ((double)this.PowerCurrent < (double)this.PowerStoreMax)
                this.PowerCurrent += (this.PowerFlowMax + (this.loyalty != null ? this.PowerFlowMax * this.loyalty.data.PowerFlowMod : 0)) * elapsedTime;
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
            if ((double)this.PowerCurrent <= 0.0)
            {
                this.PowerCurrent = 0.0f;
                this.HyperspaceReturn();
            }
            if ((double)this.PowerCurrent > (double)this.PowerStoreMax)
                this.PowerCurrent = this.PowerStoreMax;
            if (this.shield_percent < 0.0)
                this.shield_percent = 0.0;
            this.shield_percent = 100.0 * (double)this.shield_power / (double)this.shield_max;
            if (this.shield_percent < 0.0)
                this.shield_percent = 0.0;
            if ((double)this.Mass <= 0.0)
                this.Mass = 1f;
            switch (this.engineState)
            {
                case Ship.MoveState.Sublight:
                    this.velocityMaximum = this.GetSTLSpeed();
                    break;
                case Ship.MoveState.Warp:
                    this.velocityMaximum = this.GetFTLSpeed();
                    break;
            }
            this.speed = this.velocityMaximum;
            this.rotationRadiansPerSecond = (float)((double)this.TurnThrust / (double)this.Mass / 700.0);
            this.rotationRadiansPerSecond += (float)((double)this.rotationRadiansPerSecond * (double)this.Level * 0.0500000007450581);
            this.yBankAmount = this.rotationRadiansPerSecond / 50f;
            if (this.engineState == Ship.MoveState.Warp)
            {
                if (this.FTLmodifier != 1f)
                    this.velocityMaximum *= this.FTLmodifier;
                this.Velocity = Vector2.Normalize(new Vector2((float)Math.Sin((double)this.Rotation), -(float)Math.Cos((double)this.Rotation))) * this.velocityMaximum;
            }
            if ((double)this.Thrust == 0.0 || (double)this.mass == 0.0)
            {
                this.EnginesKnockedOut = true;
                this.velocityMaximum = this.Velocity.Length();
                Ship ship = this;
                Vector2 vector2 = ship.velocity - this.velocity * (elapsedTime * 0.1f);
                ship.velocity = vector2;
            }
            else
                this.EnginesKnockedOut = false;
            if ((double)this.Velocity.Length() <= (double)this.velocityMaximum)
                return;
            this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
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
                    num1 += (float)((double)weapon.DamageAmount * (1.0 / (double)weapon.fireDelay) * 0.75);
                else if (weapon.isBeam)
                    num1 += weapon.DamageAmount * 180f;
                else
                    num1 += weapon.DamageAmount * (1f / weapon.fireDelay);
            }
            if ((double)num1 == 0.0)
                return 0.0f;
            float num2 = (num1 + this.shield_power / 20f + this.Health) / (float)this.Size;
            if (this.Role == "platform" || this.Role == "station")
                num2 /= 5f;
            return num2;
        }
        //added by Gremlin : active ship strength calculator
        public float GetStrength()
        {
            if (this.Health >= this.HealthMax * .90)
                return this.BaseStrength;
            float Str = 0f;
            float def = 0f;

            int slotCount = this.ModuleSlotList.Count;

            bool fighters = false;
            bool weapons = false;

            //Parallel.ForEach(this.ModuleSlotList, slot =>  //
            foreach (ModuleSlot slot in this.ModuleSlotList)
            {
                if (!slot.module.isDummy && slot.module.Powered && slot.module.Active)
                {
                    ShipModule module = slot.module;//ResourceManager.ShipModulesDict[slot.InstalledModuleUID];

                    if (module.InstalledWeapon != null)
                    {
                        weapons = true;
                        float offRate = 0;
                        Weapon w = module.InstalledWeapon;
                        if (!w.explodes)
                        {
                            offRate += (!w.isBeam ? (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);
                        }
                        else
                        {
                            
                            offRate += (w.DamageAmount*w.SalvoCount) * (1f / w.fireDelay) * 0.75f;

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
                        if (w.EMPDamage > 0) offRate += w.EMPDamage * (1f / w.fireDelay) * .2f;
                        Str += offRate;
                    }


                    if (module.hangarShipUID != null && !module.IsSupplyBay && !module.IsTroopBay)
                    {

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
            //Added by McShooterz: a way to prevent remnant story in mods
            if (this.loyalty == Ship.universeScreen.player && killed.loyalty == EmpireManager.GetEmpireByName("The Remnant") && (GlobalStats.ActiveMod == null || (GlobalStats.ActiveMod != null && !GlobalStats.ActiveMod.mi.removeRemnantStory)))
                GlobalStats.IncrementRemnantKills();
            //Added by McShooterz: change level cap, dynamic experience required per level
            float Exp = 1;
            float ExpLevel = 1;
            bool ExpFound = false;
            float ReqExp = 1;
            if (ResourceManager.ShipRoles.ContainsKey(killed.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[killed.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[killed.Role].RaceList[i].ShipType == killed.loyalty.data.Traits.ShipType)
                    {
                        Exp = ResourceManager.ShipRoles[killed.Role].RaceList[i].KillExp;
                        ExpLevel = ResourceManager.ShipRoles[killed.Role].RaceList[i].KillExpPerLevel;
                        ExpFound = true;
                        break;
                    }
                }
                if(!ExpFound)
                {
                    Exp = ResourceManager.ShipRoles[killed.Role].KillExp;
                    ExpLevel = ResourceManager.ShipRoles[killed.Role].KillExpPerLevel;
                }
            }
            Exp = (Exp + (ExpLevel * killed.Level));
            Exp += Exp * this.loyalty.data.ExperienceMod;
            this.experience += Exp;
            ExpFound = false;
            if (ResourceManager.ShipRoles.ContainsKey(this.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[this.Role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[this.Role].RaceList[i].ShipType == this.loyalty.data.Traits.ShipType)
                    {
                        ReqExp = ResourceManager.ShipRoles[this.Role].RaceList[i].ExpPerLevel;
                        ExpFound = true;
                        break;
                    }
                }
                if (!ExpFound)
                {
                    ReqExp = ResourceManager.ShipRoles[this.Role].ExpPerLevel;
                }
            }
            while (this.experience > ReqExp * (1 + this.Level))
            {
                this.experience -= ReqExp * (1 + this.Level);
                ++this.Level;
            }
            if (this.Level > 255)
                this.Level = 255;
            if (!this.loyalty.GetRelations().ContainsKey(killed.loyalty) || !this.loyalty.GetRelations()[killed.loyalty].AtWar)
                return;
            this.loyalty.GetRelations()[killed.loyalty].ActiveWar.StrengthKilled += killed.BaseStrength;
            killed.loyalty.GetRelations()[this.loyalty].ActiveWar.StrengthLost += killed.BaseStrength;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            for (int index = 0; index < this.beams.Count; ++index)
                this.beams[index].Die((GameplayObject)this, true);
            this.beams.Clear();
            ++DebugInfoScreen.ShipsDied;
            if (!cleanupOnly && source is Projectile && (source as Projectile).owner != null)
                (source as Projectile).owner.AddKill(this);
            if ((this.system != null ? (double)this.system.RNG.RandomBetween(0.0f, 100f) : (double)Ship.universeScreen.DeepSpaceRNG.RandomBetween(0.0f, 100f)) > 65.0 && !this.IsPlatform && this.InFrustum)
            {
                this.dying = true;
                this.xdie = (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(-1f, 1f) * 40f / (float)this.Size;
                this.ydie = (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(-1f, 1f) * 40f / (float)this.Size;
                this.zdie = (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(-1f, 1f) * 40f / (float)this.Size;
                this.dietimer = (this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG).RandomBetween(4f, 6f);
                if (source is Projectile && (source as Projectile).explodes && (double)(source as Projectile).damageAmount > 100.0)
                    this.reallyDie = true;
            }
            else
                this.reallyDie = true;
            if (this.dying && !this.reallyDie)
                return;
            if (this.system != null)
                this.system.ShipList.QueuePendingRemoval(this);
            if (source is Projectile && (source as Projectile).owner != null)
            {
                float Amount = 1f;
                if(ResourceManager.ShipRoles.ContainsKey(this.Role))
                    Amount = ResourceManager.ShipRoles[this.Role].DamageRelations;
                this.loyalty.DamageRelationship((source as Projectile).owner.loyalty, "Destroyed Ship", Amount, (Planet)null);
            }
            if (!cleanupOnly && this.InFrustum)
            {
                if (this.Size < 80)
                {
                    this.dieCue = AudioManager.GetCue("sd_explosion_ship_det_small");
                    this.dieCue.Apply3D(Ship.universeScreen.listener, this.emitter);
                    this.dieCue.Play();
                }
                else if (this.Size >= 80 && this.Size < 250)
                {
                    this.dieCue = AudioManager.GetCue("sd_explosion_ship_det_medium");
                    this.dieCue.Apply3D(Ship.universeScreen.listener, this.emitter);
                    this.dieCue.Play();
                }
                else
                {
                    this.dieCue = AudioManager.GetCue("sd_explosion_ship_det_large");
                    this.dieCue.Apply3D(Ship.universeScreen.listener, this.emitter);
                    this.dieCue.Play();
                }
            }
            this.ModuleSlotList.Clear();
            this.ExternalSlots.Clear();
            this.ModulesDictionary.Clear();
            this.Velocity = Vector2.Zero;
            this.velocityMaximum = 0.0f;
            this.AfterBurnerAmount = 0.0f;
            Vector3 Position = new Vector3(this.Center.X, this.Center.Y, -100f);
            if (this.Active)
            {
                if (!cleanupOnly)
                {
                    switch (this.Role)
                    {
                        case "freighter":
                            ExplosionManager.AddExplosion(Position, 500f, 12f, 0.2f);
                            break;
                        case "platform":
                            ExplosionManager.AddExplosion(Position, 500f, 12f, 0.2f);
                            break;
                        case "fighter":
                            ExplosionManager.AddExplosion(Position, 500f, 12f, 0.2f);
                            break;
                        case "frigate":
                            ExplosionManager.AddExplosion(Position, 850f, 12f, 0.2f);
                            break;
                        case "capital":
                            ExplosionManager.AddExplosion(Position, 850f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 850f, 12f, 0.2f);
                            break;
                        case "carrier":
                            ExplosionManager.AddExplosion(Position, 850f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 850f, 12f, 0.2f);
                            break;
                        case "cruiser":
                            ExplosionManager.AddExplosion(Position, 850f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 850f, 12f, 0.2f);
                            break;
                        default:
                            ExplosionManager.AddExplosion(Position, 500f, 12f, 0.2f);
                            break;
                    }
                    if (this.system != null)
                        this.system.spatialManager.ShipExplode((GameplayObject)this, (float)(this.Size * 50), this.Center, this.radius);
                }
                else
                {
                    switch (this.Role)
                    {
                        case "freighter":
                            ExplosionManager.AddExplosion(Position, 500f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 1200f, 12f, 0.2f);
                            break;
                        case "platform":
                            ExplosionManager.AddExplosion(Position, 500f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 1200f, 12f, 0.2f);
                            break;
                        case "fighter":
                            ExplosionManager.AddExplosion(Position, 600f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 1200f, 12f, 0.2f);
                            break;
                        case "frigate":
                            ExplosionManager.AddExplosion(Position, 1000f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 2000f, 12f, 0.2f);
                            break;
                        case "capital":
                            ExplosionManager.AddExplosion(Position, 1200f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 2400f, 12f, 0.2f);
                            break;
                        case "cruiser":
                            ExplosionManager.AddExplosion(Position, 850f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 850f, 12f, 0.2f);
                            break;
                        default:
                            ExplosionManager.AddExplosion(Position, 600f, 12f, 0.2f);
                            ExplosionManager.AddWarpExplosion(Position, 1200f, 12f, 0.2f);
                            break;
                    }
                    if (this.system != null)
                        this.system.spatialManager.ShipExplode((GameplayObject)this, (float)(this.Size * 50), this.Center, this.radius);
                }
            }
            if (Ship_Game.ResourceManager.ShipsDict[this.Name].GetShipData().EventOnDeath != null)
                Ship.universeScreen.ScreenManager.AddScreen((GameScreen)new EventPopup(Ship.universeScreen, EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty), Ship_Game.ResourceManager.EventsDict[Ship_Game.ResourceManager.ShipsDict[this.Name].GetShipData().EventOnDeath], Ship_Game.ResourceManager.EventsDict[Ship_Game.ResourceManager.ShipsDict[this.Name].GetShipData().EventOnDeath].PotentialOutcomes[0], true));
            this.QueueTotalRemoval();
        }

        public void QueueTotalRemoval()
        {
            lock (GlobalStats.AddShipLocker)
                Ship.universeScreen.ShipsToRemove.Add(this);

        }

        public void TotallyRemove()
        {
            this.Active = false;
            this.AI.PotentialTargets.Clear();
            this.AI.NearbyShips.Clear();
            this.AI.FriendliesNearby.Clear();
            this.AI.Target = (GameplayObject)null;
            this.AI.ColonizeTarget = (Planet)null;
            this.AI.EscortTarget = (Ship)null;
            this.AI.start = (Planet)null;
            this.AI.end = (Planet)null;
            Ship.universeScreen.MasterShipList.QueuePendingRemoval(this);
            UniverseScreen.ShipSpatialManager.CollidableObjects.Remove((GameplayObject)this);
            if (Ship.universeScreen.SelectedShip == this)
                Ship.universeScreen.SelectedShip = (Ship)null;
            if (Ship.universeScreen.SelectedShipList.Contains(this))
                Ship.universeScreen.SelectedShipList.Remove(this);
            if (this.system != null)
            {
                this.system.ShipList.QueuePendingRemoval(this);
                this.system.spatialManager.CollidableObjects.Remove((GameplayObject)this);
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
            lock (GlobalStats.ObjectManagerLocker)
                Ship.universeScreen.ScreenManager.inter.ObjectManager.Remove((ISceneObject)this.ShipSO);
            this.loyalty.RemoveShip(this);
            this.system = (SolarSystem)null;
            this.TetheredTo = (Planet)null;
        }

        public void RemoveFromAllFleets()
        {
            if (this.fleet == null)
                return;
            this.fleet.Ships.Remove(this);
            foreach (FleetDataNode fleetDataNode in (List<FleetDataNode>)this.fleet.DataNodes)
            {
                if (fleetDataNode.GetShip() == this)
                    fleetDataNode.SetShip((Ship)null);
            }
            foreach (List<Fleet.Squad> list in this.fleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in list)
                {
                    if (squad.Ships.Contains(this))
                        squad.Ships.QueuePendingRemoval(this);
                    foreach (FleetDataNode fleetDataNode in (List<FleetDataNode>)squad.DataNodes)
                    {
                        if (fleetDataNode.GetShip() == this)
                            fleetDataNode.SetShip((Ship)null);
                    }
                }
            }
        }

        public ShipModule GetRandomInternalModule()
        {
            List<ShipModule> InternalModules = new List<ShipModule>();
            foreach (ModuleSlot slot in this.ModuleSlotList)
            {
                if (slot.Restrictions == Restrictions.I || slot.module.Active)
                    InternalModules.Add(slot.module);
            }
            if (InternalModules.Count > 0)
                return InternalModules[HelperFunctions.GetRandomIndex(InternalModules.Count)];
            else
                return null;
        }

        public virtual void StopAllSounds()
        {
            if (this.drone == null)
                return;
            if (this.drone.IsPlaying)
                this.drone.Stop(AudioStopOptions.Immediate);
            this.drone.Dispose();
        }

        public static Ship Copy(Ship ship)
        {
            return new Ship()
            {
                Role = ship.Role,
                ThrusterList = ship.ThrusterList,
                ModelPath = ship.ModelPath,
                ModuleSlotList = ship.ModuleSlotList
            };
        }

        private static Vector2 MoveInCircle(GameTime gameTime, float speed)
        {
            double num = gameTime.TotalGameTime.TotalSeconds * (double)speed;
            return new Vector2((float)Math.Cos(num), (float)Math.Sin(num));
        }

        public enum MoveState
        {
            Sublight,
            Warp,
        }
    }
}
