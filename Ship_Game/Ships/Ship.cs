using System;
using System.Collections.Generic;
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

        public Vector2 Acceleration { get; private set; }

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
        //public float ClickTimer = 10f;    //Never used
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
        public bool ShipInitialized;
        public float maxFTLSpeed;
        public float maxSTLSpeed;
        public float NormalWarpThrust;
        public float BoardingDefenseTotal => (MechanicalBoardingDefense + TroopBoardingDefense);

        public Array<Empire> BorderCheck = new Array<Empire>();

        public float FTLModifier { get; private set; } = 1f;

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
        public ShipData.RoleName DesignRole { get; private set; }
        public string GetDesignRoleName() => ShipData.GetRole(DesignRole);
        public Texture2D GetTacticalIcon()
        {
            if (DesignRole == ShipData.RoleName.support)
                return ResourceManager.Texture("TacticalIcons/symbol_supply");

            string roleName = DesignRole.ToString();
            string iconName = $"TacticalIcons/symbol_";
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

        public bool needResupplyOrdnance
        {
            get
            {
                bool lowAmmo =  !AI.HasPriorityOrder && OrdinanceMax > 0f && Ordinance / OrdinanceMax < 0.05f;
                if (!lowAmmo) return false;
                foreach (var weapon in Weapons)
                    if (weapon.OrdinanceRequiredToFire < .01 && AI.State != AIState.AwaitingOrders)
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
                assaultSpots += shipData.Role == ShipData.RoleName.troop ? 1 : 0;
                return Math.Min(TroopList.Count, assaultSpots);
            }
        }
        public float PlanetAssaultStrength
        {
            get
            {
                if (TroopList.IsEmpty)
                    return 0.0f;

                int assaultSpots =( DesignRole == ShipData.RoleName.troop || DesignRole == ShipData.RoleName.troopShip) ? TroopList.Count : 0;
                assaultSpots += Hangars.FilterBy(sm => sm.IsTroopBay).Length;
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
                    flag |= Hangars[index]?.FighterOut ?? false;
                return flag;
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
        private bool TFood = false;
        public bool TransportingFood
        {
            get
            {
                return AI.State != AIState.SystemTrader || TFood; ;
            }
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
            get
            {
                return AI.State != AIState.SystemTrader || TProd;
            }
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
                return loyalty.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this);
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
                        if (Position.InRadius(planet.Center,
                             planet.GravityWellRadius))
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
            maxFTLSpeed = (WarpThrust / base.Mass + WarpThrust / base.Mass * (loyalty?.data?.FTLModifier ?? 35)) * FTLModifier;


        }
        public float GetmaxFTLSpeed => maxFTLSpeed;


        public float GetSTLSpeed()
        {
            //Added by McShooterz: hull bonus speed
            float speed = Thrust / Mass + Thrust / Mass * loyalty.data.SubLightModifier;
            return  Math.Min(speed, 2500);
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
            if (shipData.HullData == null) shipData.SetHullData();
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

        public void AddThruster(Thruster t)
        {
            ThrusterList.Add(new Thruster
            {
                Parent = this,
                tscale = t.tscale,
                XMLPos = t.XMLPos
            });
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

            ShipData.RoleName role = shipData.HullRole;

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
                if (ActiveInternalSlotCount >0 && ActiveInternalSlotCount < InternalSlotCount)
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
                    case ShipModuleType.Armor:
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

        public void ResetJumpTimer()
        {
            JumpTimer = FTLSpoolTime * loyalty.data.SpoolTimeModifier;
        } 

        
        public void EngageStarDrive() // added by gremlin: Fighter recall and stuff
        {
            if (isSpooling || engineState == MoveState.Warp || GetmaxFTLSpeed <= 2500 )
                return;
            if (RecallingFighters())
                return;
            if (EnginesKnockedOut)
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

        private bool RecallingFighters()
        {
            if (!RecallFightersBeforeFTL || Hangars.Count <= 0)
                return false;
            bool recallFighters = false;
            float jumpDistance = Center.Distance(AI.MovePosition);
            float slowestFighter = Speed * 2;
            if (jumpDistance > 7500f)
            {
                recallFighters = true;

                foreach (ShipModule hangar in Hangars)
                {
                    Ship hangarShip = hangar.GetHangarShip();
                    if (hangar.IsSupplyBay || hangarShip == null)
                    {
                        recallFighters = false;
                        continue;
                    }
                    if (hangarShip.Speed < slowestFighter) slowestFighter = hangarShip.Speed;

                    float rangeTocarrier = hangarShip.Center.Distance(Center);
                    if (rangeTocarrier > SensorRange)
                    {
                        recallFighters = false;
                        continue;
                    }
                    if (hangarShip.EMPdisabled || !hangarShip.hasCommand || hangarShip.dying || hangarShip.EnginesKnockedOut)
                    {
                        recallFighters = false;
                        if (hangarShip.ScuttleTimer <= 0f) hangarShip.ScuttleTimer = 10f;
                        continue;
                    }
                    recallFighters = true;
                    break;
                }
            }
            if (!recallFighters)
                return false;
            RecoverAssaultShips();
            RecoverFighters();
            if (DoneRecovering())
                return false;
            if (Speed * 2 > slowestFighter)
                Speed = slowestFighter * .25f;
            return true;
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


        //added by gremlin deveksmod scramble assault ships
        public void ScrambleAssaultShips(float strengthNeeded)
        {
            bool flag = strengthNeeded > 0;
            
            foreach (ShipModule hangar in Hangars)
            {
                if (!hangar.IsTroopBay || hangar.hangarTimer > 0 || TroopList.Count < 1 
                    || hangar.GetHangarShip() != null)
                {
                    if (flag && strengthNeeded < 0)
                        break;
                    strengthNeeded -= TroopList[0].Strength;
                    hangar.LaunchBoardingParty(TroopList[0]);
                    TroopList.RemoveAt(0);

                }
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
                if (shipModule.GetHangarShip() == null || !shipModule.GetHangarShip().Active) continue;
                if (shipModule.IsTroopBay) continue;

                shipModule.GetHangarShip().ReturnToHangar();
            }
        }

        public ShipData ToShipData()
        {
            var data                       = new ShipData();
            data.BaseCanWarp               = shipData.BaseCanWarp;
            data.BaseStrength              = -1;
            data.techsNeeded               = shipData.techsNeeded;
            data.TechScore                 = shipData.TechScore;
            data.ShipCategory              = shipData.ShipCategory;
            data.Name                      = Name;
            data.Level                     = (byte)Level;
            data.experience                = (byte)experience;
            data.Role                      = shipData.Role;
            data.IsShipyard                = GetShipData().IsShipyard;
            data.IsOrbitalDefense          = GetShipData().IsOrbitalDefense;
            data.Animated                  = GetShipData().Animated;
            data.CombatState               = AI.CombatState;
            data.ModelPath                 = GetShipData().ModelPath;
            data.ModuleSlots               = GetModuleSlotDataArray();
            data.ThrusterList              = new Array<ShipToolScreen.ThrusterZone>();
            data.MechanicalBoardingDefense = MechanicalBoardingDefense;
            data.HullData                  = shipData.HullData;
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
                if (shield.ShieldPower >= 1f)
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
            if (!Empire.Universe.Paused && velocityMaximum <= 0f && !shipData.IsShipyard && shipData.Role <= ShipData.RoleName.station)                                           
                Rotation += 0.003f + RandomMath.AvgRandomBetween(.0001f,.0005f);
            

            MoveModulesTimer -= deltaTime;
            updateTimer -= deltaTime;
            if (deltaTime > 0 && (EMPDamage > 0 || EMPdisabled))
            {
                --EMPDamage;
                EMPDamage = Math.Max(0, EMPDamage);

                EMPdisabled = EMPDamage > Size + BonusEMP_Protection;
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

            if (updateTimer <= 0.0) //|| shipStatusChanged)
            {
                TroopBoardingDefense = 0.0f;
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
                    Weapon[] sortedByRange = Weapons.SortedBy(weapon => direction*weapon.GetModifiedRange());

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

                inSensorRange = false;
                if (Empire.Universe.Debug || loyalty == EmpireManager.Player || loyalty != EmpireManager.Player && EmpireManager.Player.GetRelations(loyalty).Treaty_Alliance)
                    inSensorRange = true;
                else if (!inSensorRange)
                {
                    GameplayObject[] nearby = GetObjectsInSensors(GameObjectType.Ship);
                    foreach (GameplayObject go in nearby)
                    {
                        var ship = (Ship) go;
                        if (ship.loyalty == EmpireManager.Player && Center.InRadius(ship.Position, ship.SensorRange))
                        {
                            inSensorRange = true;
                            break;
                        }
                    }
                }
                foreach (ShipModule slot in ModuleSlotList)
                      slot.Update(1);
                if (shipStatusChanged) //|| InCombat
                {
                    ShipStatusChange();
                    
                }
                //Power draw based on warp
                if (!inborders && engineState == MoveState.Warp)
                {
                    PowerDraw = (loyalty.data.FTLPowerDrainModifier * ModulePowerDraw) + (WarpDraw * loyalty.data.FTLPowerDrainModifier / 2);
                }
                else if (engineState != MoveState.Warp && ShieldsUp)
                    PowerDraw = ModulePowerDraw + ShieldPowerDraw;
                else
                    PowerDraw = ModulePowerDraw;

                //This is what updates all of the modules of a ship
                if (loyalty.RecalculateMaxHP) HealthMax = 0;
                

                //Check Current Shields
                if (engineState == MoveState.Warp || !ShieldsUp)
                    shield_power = 0f;
                else
                {
                    if (InCombat || shield_power != shield_max)
                    {
                        shield_power = 0.0f;
                        for (int x = 0; x < Shields.Length; x++)
                        {
                            ShipModule shield = Shields[x];
                            shield_power += shield.ShieldPower;
                        }
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
                    //shipStatusChanged = true;
                    if (!InCombat || GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useCombatRepair)
                    {
                        //Added by McShooterz: Priority repair
                        float repairTracker = InCombat ? RepairRate * 0.1f : RepairRate;
                        var damagedModules = ModuleSlotList
                            .FilterBy(slot => slot.Health < slot.HealthMax)
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
                    //shipStatusChanged = false;
                }
                UpdateTroops();
                //this.UpdateSystem(elapsedTime);
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

            SetmaxFTLSpeed();
            Ordinance = Math.Min(Ordinance, OrdinanceMax);

            if ((InternalSlotsHealthPercent = (float)ActiveInternalSlotCount / InternalSlotCount) <.35f)            
                  Die(LastDamagedBy, false);

            Mass = Math.Max((Size / 2), Mass);
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
            
            shield_percent = Math.Max(100.0 * shield_power / shield_max, 0);
            
            
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
            yBankAmount = rotationRadiansPerSecond * deltaTime;// 50f;

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

        private void UpdateTroops()
        {
            Array<Troop> ownTroops = new Array<Troop>();
            Array<Troop> EnemyTroops = new Array<Troop>();
            foreach (Troop troop in TroopList)
            {
                if (troop.GetOwner() == loyalty)
                    ownTroops.Add(troop);
                else
                    EnemyTroops.Add(troop);
            }
            if (HealPerTurn > 0)
            {
                foreach (Troop troop in ownTroops)
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
                if (ownTroops.Count > 0 && EnemyTroops.Count > 0)
                {
                    foreach (Troop troop in ownTroops)
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
                    foreach (Troop troop in ownTroops)
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
                        MechanicalBoardingDefense -= (float) num2;
                        if (MechanicalBoardingDefense < 0.0)
                            MechanicalBoardingDefense = 0.0f;
                    }
                }
                ownTroops.Clear();
                foreach (Troop troop in TroopList)
                {
                    if (troop.GetOwner() == loyalty)
                        ownTroops.Add(troop);
                }
                if (ownTroops.Count != 0 || !(MechanicalBoardingDefense <= 0.0)) return;
                ChangeLoyalty(changeTo: EnemyTroops[0].GetOwner());

                if (!AI.BadGuysNear)
                ShieldManager.RemoveShieldLights(Shields);
            }
        }

   

        public void ShipStatusChange()
        {
            shipStatusChanged = false;
            Health = 0f;
            float sensorBonus = 0f;
            Hangars.Clear();
            Transporters.Clear();
            Thrust                      = 0f;
            Mass                        = Size;
            shield_max                  = 0f;
            ActiveInternalSlotCount     = 0;
            BonusEMP_Protection         = 0f;
            PowerStoreMax               = 0f;
            PowerFlowMax                = 0f;
            OrdinanceMax                = 0f;
            ModulePowerDraw             = 0.0f;
            ShieldPowerDraw             = 0f;
            RepairRate                  = 0f;
            CargoSpaceMax               = 0f;
            SensorRange                 = 1000f;
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
                    ActiveInternalSlotCount += slot.XSIZE * slot.YSIZE;
                Health += slot.Health;
                RepairRate += slot.BonusRepairRate;
                if (slot.Mass < 0.0 && slot.Powered)
                    Mass += slot.Mass;
                else if (slot.Mass > 0.0)
                {
                    if (slot.ModuleType == ShipModuleType.Armor && loyalty != null)
                    {
                        float armourMassModifier = loyalty.data.ArmourMassModifier;
                        float armourMass = slot.Mass * armourMassModifier;
                        Mass += armourMass;
                    }
                    else                    
                        Mass += slot.Mass;
                }
                //Checks to see if there is an active command module

                if (slot.Active && (slot.Powered || slot.PowerDraw <= 0f))
                {                    
                    hasCommand |= slot.IsCommandModule;
                    //Doctor: For 'Fixed' tracking power modules - i.e. a system whereby a module provides a non-cumulative/non-stacking tracking power.
                    //The normal stacking/cumulative tracking is added on after the for loop for mods that want to mix methods. The original cumulative function is unaffected.
                    if (slot.FixedTracking > 0 && slot.FixedTracking > FixedTrackingPower)
                        FixedTrackingPower = slot.FixedTracking;
                    
                    TrackingPower         += Math.Max(0, slot.TargetTracking);
                    OrdinanceMax          += slot.OrdinanceCapacity;
                    CargoSpaceMax         += slot.Cargo_Capacity;
                    InhibitionRadius      += slot.InhibitionRadius;
                    BonusEMP_Protection   += slot.EMP_Protection;
                    SensorRange            = Math.Max(SensorRange, slot.SensorRange);
                    sensorBonus            = Math.Max(sensorBonus, slot.SensorBonus);                    
                    if (slot.shield_power_max > 0f)
                    {
                        shield_max += slot.GetShieldsMax();
                        ShieldPowerDraw += slot.PowerDraw;
                    }
                    else
                        ModulePowerDraw += slot.PowerDraw;
                    Thrust              += slot.thrust;
                    WarpThrust          += slot.WarpThrust;
                    TurnThrust          += slot.TurnThrust;
                    WarpDraw            += slot.PowerDrawAtWarp;
                    OrdAddedPerSecond   += slot.OrdnanceAddedPerSecond;
                    HealPerTurn         += slot.HealPerTurn;
                    ECMValue             = 1f.Clamp(0f, Math.Max(ECMValue, slot.ECM)); // 0-1 using greatest value.                    
                    PowerStoreMax       += Math.Max(0, slot.PowerStoreMax);
                    PowerFlowMax        += Math.Max(0, slot.PowerFlowMax);                                        
                    FTLSpoolTime   = Math.Max(FTLSpoolTime, slot.FTLSpoolTime);
                    if (slot.AddModuleTypeToList(ShipModuleType.Hangar, addToList: Hangars))
                        HasTroopBay |= slot.IsTroopBay;
                    slot.AddModuleTypeToList(ShipModuleType.Transporter, addToList: Transporters);
                    slot.AddModuleTypeToList(slot.ModuleType, isTrue: slot.InstalledWeapon?.isRepairBeam == true, addToList: RepairBeams);
                }
            }
            NormalWarpThrust = WarpThrust;
            //Doctor: Add fixed tracking amount if using a mixed method in a mod or if only using the fixed method.
            TrackingPower += FixedTrackingPower;
            shield_percent = Math.Max(100.0 * shield_power / shield_max, 0);
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
            CalculateShipStrength(setBaseStrength: false);
            maxWeaponsRange = CalculatMaxWeaponsRange();
        }
        public bool IsTethered()
        {
            return TetheredTo != null;
        }

        //added by Gremlin : active ship strength calculator
        private float CurrentStrength = -1;
        public float GetStrength(bool recalculate = false)
        {            
            if (Health >= HealthMax * 0.75f && !LowHealth && CurrentStrength > -1)
                return CurrentStrength;
            if (recalculate || CurrentStrength < 0)
                CurrentStrength = CalculateShipStrength(false);
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
                for (int i = 0; i < role.RaceList.Count(); i++)
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
            var hullData = shipData.HullData;
            if (hullData?.EventOnDeath != null)
            {
                var evt = ResourceManager.EventsDict[hullData.EventOnDeath];
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
                foreach (ShipModule shipModule in Mothership.Hangars)
                    if (shipModule.GetHangarShip() == this)
                        shipModule.SetHangarShip(null);
            }
            foreach (ShipModule hanger in Hangars)
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

            Hangars.Clear();
            BombBays.Clear();
            TroopList.Clear();
            ClearFleet();
            ShipSO?.Clear();
            Empire.Universe.RemoveObject(ShipSO);
            loyalty.RemoveShip(this);
            SetSystem(null);
            TetheredTo = null;
            Transporters.Clear();
            RepairBeams.Clear();
            Empire.Universe.MasterShipList.QueuePendingRemoval(this);
        }

        public bool ClearFleet() => fleet?.RemoveShip(this) ?? false;
        
        public bool ShipReadyForWarp()
        {
            if (AI.State != AIState.FormationWarp ) return true;
            if (!isSpooling && PowerCurrent / (PowerStoreMax + 0.01f) < 0.2f) return false;
            if (engineState == MoveState.Warp) return true;
            return !RecallingFighters();
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

        public void RecalculateMaxHP()          //Added so ships would get the benefit of +HP mods from research and/or artifacts.   -Gretman
        {
            if (VanityName == "MerCraft")
                Log.Info($"Health was {Health} / {HealthMax}   ({loyalty.data.Traits.ModHpModifier})");

            HealthMax = 0;
            foreach (ShipModule slot in ModuleSlotList)
            {
                bool isFullyHealed = slot.Health >= slot.HealthMax;
                slot.HealthMax     = ResourceManager.GetModuleTemplate(slot.UID).HealthMax;
                slot.HealthMax     = slot.HealthMax + slot.HealthMax * loyalty.data.Traits.ModHpModifier;
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
            if (VanityName == "MerCraft")
                Log.Info($"Health is  {Health} / {HealthMax}");
        }

        private float PercentageOfShipByModules(ShipModule[] modules)
        {
            int area = 0;
            foreach (ShipModule module in modules)
                area += module.XSIZE * module.YSIZE;
            return area > 0 ? area / (float)Size : 0.0f;
        }
        public float PercentageOfShipByModules(ShipModuleType moduleType)
        {
            return PercentageOfShipByModules(ModuleSlotList.FilterBy(module => module.ModuleType == moduleType), Size);
        }
        private static float PercentageOfShipByModules(ShipModule[] modules ,int size)
        {
            int area = 0;
            int count = modules.Length;
            for (int i = 0; i < count; ++i)
            {
                ShipModule module = modules[i];
                area += module.XSIZE * module.YSIZE;
            }
            return area > 0 ? area / (float)size : 0.0f;
        }
        private ShipData.RoleName GetDesignRole()
        {
            ShipModule[] modules = ModuleSlotList;
            ShipData.RoleName hullRole = shipData?.HullRole ?? shipData.Role;
            return GetDesignRole(modules, hullRole, shipData.Role, Size, this);            
        }
        public static ShipData.RoleName GetDesignRole(ShipModule[] modules, ShipData.RoleName hullRole, ShipData.RoleName dataRole, int size, Ship ship)
        {
            if (ship != null)
            {
                if (ship.isConstructor)
                    return ShipData.RoleName.construction;
                if (ship.isColonyShip || modules.Any(colony => colony.ModuleType == ShipModuleType.Colony))
                    return ShipData.RoleName.colony;
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    return ShipData.RoleName.troop;
                if (ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform)
                    return ship.shipData.Role;
                if (ship.IsSupplyShip && ship.Weapons.Count == 0)
                    return ShipData.RoleName.supply;
            }
            //troops ship
            if (hullRole >= ShipData.RoleName.freighter)
            {
                float pTroops = PercentageOfShipByModules(modules.FilterBy(troopbay => troopbay.IsTroopBay), size);
                float pTrans =
                    PercentageOfShipByModules(modules.FilterBy(troopbay => troopbay.TransporterTroopLanding > 0), size);
                float troops = PercentageOfShipByModules(modules.FilterBy(module => module.TroopCapacity > 0), size);
                if (pTrans + pTroops + troops > .1f)
                    return ShipData.RoleName.troopShip;
                if (PercentageOfShipByModules(
                        modules.FilterBy(bombBay => bombBay.ModuleType == ShipModuleType.Bomb),size) > .05f)
                    return ShipData.RoleName.bomber;
                //carrier
                
                ShipModule[] carrier = modules.FilterBy(hangar => hangar.ModuleType == ShipModuleType.Hangar && !hangar.IsSupplyBay && !hangar.IsTroopBay);
                ShipModule[] support = modules.FilterBy(hangar => hangar.ModuleType == ShipModuleType.Hangar && (hangar.IsSupplyBay || hangar.IsTroopBay));

                if (PercentageOfShipByModules(carrier, size) > .1)
                    return ShipData.RoleName.carrier;
                if (PercentageOfShipByModules(support, size) > .1)
                    return ShipData.RoleName.support;
            }
            float pSpecial = PercentageOfShipByModules(modules.FilterBy(module =>
                module.TransporterOrdnance > 0
                || module.IsSupplyBay
                || module.InhibitionRadius > 0
                || module.InstalledWeapon != null && module.InstalledWeapon.DamageAmount < 1 &&
                (module.InstalledWeapon.MassDamage > 0
                 || module.InstalledWeapon.EMPDamage > 0
                 || module.InstalledWeapon.RepulsionDamage > 0
                 || module.InstalledWeapon.SiphonDamage > 0
                 || module.InstalledWeapon.TroopDamageChance > 0
                 || module.InstalledWeapon.isRepairBeam || module.InstalledWeapon.IsRepairDrone)
            ), size);
            pSpecial += PercentageOfShipByModules(
                modules.FilterBy(repair => repair.InstalledWeapon?.IsRepairDrone == true), size);

            if (pSpecial > .10f)
                return ShipData.RoleName.support;


            ShipData.RoleName fixRole = dataRole == ShipData.RoleName.prototype ? dataRole : hullRole;

            switch (fixRole)
            {
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.gunboat:
                    return ShipData.RoleName.corvette;
                case ShipData.RoleName.carrier:
                case ShipData.RoleName.capital:
                    return ShipData.RoleName.capital;
                case ShipData.RoleName.destroyer:
                case ShipData.RoleName.frigate:
                    return ShipData.RoleName.frigate;
                case ShipData.RoleName.scout:
                case ShipData.RoleName.fighter:
                    return modules.Any(weapons => weapons.InstalledWeapon != null)
                        ? ShipData.RoleName.fighter
                        : ShipData.RoleName.scout;
            }

            return hullRole;
        }

        public void TestShipModuleDamage()
        {
            foreach (ShipModule mod in ModuleSlotList)
            {
                mod.Health = 1;
            } //Added by Gretman so I can hurt ships when the disobey me... I mean for testing... Yea, thats it...
            Health = ModuleSlotList.Length;
        }
        
        

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
        private int DPS =0;
        private int Defense = 0;
        public float CalculateShipStrength(bool setBaseStrength = true)
        {
            float offense = 0f;
            float defense = 0f;
            bool fighters = false;
            bool weapons = false;

            foreach (ShipModule slot in ModuleSlotList)
            {
                //ShipModule template = GetModuleTemplate(slot.UID);
                weapons  |= slot.InstalledWeapon != null;
                fighters |= slot.hangarShipUID   != null && !slot.IsSupplyBay && !slot.IsTroopBay;

                offense += slot.CalculateModuleOffense();
                defense += slot.CalculateModuleDefense(Size);

                ShipModule template = ResourceManager.GetModuleTemplate(slot.UID);
                if (template.WarpThrust > 0)
                    BaseCanWarp = true;
            }
            DPS = (int)offense;
            Defense = (int)defense;

            if (!fighters && !weapons) offense = 0f;
            if (defense > offense) defense = offense;
            if (setBaseStrength)
                shipData.BaseStrength = offense + defense;
            return offense + defense;
        }


        public void RepairShipModules(ref float repairPool)
        {
            shipStatusChanged = true;

            // .Where(slot => slot.ModuleType != ShipModuleType.Dummy && slot.Health != slot.HealthMax))
            foreach (ShipModule slot in ModuleSlotList) 
            {
                //repairing = true;
                if (loyalty.data.Traits.ModHpModifier > 0)
                {
                    float maxHealth = ResourceManager.GetModuleTemplate(slot.UID).HealthMax;
                    slot.HealthMax  = maxHealth + maxHealth * loyalty.data.Traits.ModHpModifier; 
                }
                if (slot.Health < slot.HealthMax)
                {
                    if (slot.HealthMax - slot.Health > repairPool)
                    {
                        slot.Repair(repairPool);
                        repairPool = 0;
                        break;
                    }
                    else
                    {
                        repairPool -= slot.HealthMax - slot.Health;
                        slot.Repair(slot.HealthMax);
                    }
                }
            }

            if (repairPool > 0)
            {
                float shieldrepair = 0.2f * repairPool;
                if (shield_max - shield_power > shieldrepair)
                    shield_power += shieldrepair;
                else
                {
                    shieldrepair = shield_max - shield_power;
                    shield_power = shield_max;
                }
                repairPool = -shieldrepair;
            }
        }

        public void RepairShipModulesByDrone(float repairAmount)
        {
            var damagedModules = ModuleSlotList
                .FilterBy(slot => slot.Health < slot.HealthMax)
                .OrderBy(slot => slot.ModulePriority);
            foreach (ShipModule module in damagedModules)
            {
                if (module.Health >= module.HealthMax) continue;
                module.Health += repairAmount;

                if (module.Health >= module.HealthMax)
                    module.Health = module.HealthMax;
                break; //FB: concentrate on one module and dont repair the all damaged modules at once like before
            }
        }

        public override string ToString() => $"Ship Id={Id} '{VanityName}' Pos {Position}  Loyalty {loyalty} Role {DesignRole}" ;

        public bool ShipIsGoodForGoalsUI(float baseStrengthNeeded = 0) => ShipIsGoodForGoals(baseStrengthNeeded, EmpireManager.Player);
        public bool ShipIsGoodForGoals(float baseStrengthNeeded = 0, Empire empire = null)
        {
            if (!Active) return false;
            empire = empire ?? loyalty;
            if (!shipData.BaseCanWarp) return false;
            float powerDraw = ModulePowerDraw * (empire?.data.FTLPowerDrainModifier ?? 1);
            float goodPowerSupply = PowerFlowMax - powerDraw;
            float powerTime = GlobalStats.MinimumWarpRange;
            if (goodPowerSupply <0)
            {
                powerTime = PowerStoreMax / -goodPowerSupply * maxFTLSpeed;
            }
            bool warpTimeGood = goodPowerSupply >= 0 || powerTime >= GlobalStats.MinimumWarpRange;

            bool goodPower = shipData.BaseCanWarp && warpTimeGood ;
            if (!goodPower || empire == null)
                Log.Info($"WARNING ship design {Name} with hull {shipData.Hull} :Bad WarpTime. {powerDraw}/{PowerFlowMax}");
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
    }
}

