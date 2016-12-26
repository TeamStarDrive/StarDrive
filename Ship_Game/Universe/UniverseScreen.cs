// Type:+836Ship_Game.UniverseScreen
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Particle3DSample;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Ship_Game
{
    public class UniverseScreen : GameScreen, IDisposable
    {
        private readonly PerfTimer EmpireUpdatePerf  = new PerfTimer();
        private readonly PerfTimer Perfavg2 = new PerfTimer();
        private readonly PerfTimer PreEmpirePerf = new PerfTimer();
        private readonly PerfTimer perfavg4 = new PerfTimer();
        private readonly PerfTimer perfavg5 = new PerfTimer();

        public static float GamePaceStatic      = 1f;
        public static float GameScaleStatic     = 1f;
        public static bool ShipWindowOpen       = false;
        public static bool ColonizeWindowOpen   = false;
        public static bool PlanetViewWindowOpen = false;
        public static SpatialManager DeepSpaceManager = new SpatialManager();
        public static SpatialManager ShipSpatialManager = new SpatialManager();
        public static Array<SolarSystem> SolarSystemList = new Array<SolarSystem>();
        public static BatchRemovalCollection<SpaceJunk> JunkList = new BatchRemovalCollection<SpaceJunk>();
        public static bool DisableClicks = false;
        //private static string fmt = "00000.##";
        private static string fmt2 = "0.#";
        public float GamePace = 1f;
        public float GameScale = 1f;
        public float GameSpeed = 1f;
        public float StarDate = 1000f;
        public string StarDateFmt = "0000.0";
        public float StarDateTimer = 5f;
        public float perStarDateTimer = 1000f;
        public float AutoSaveTimer = GlobalStats.AutoSaveFreq;
        public Array<ClickablePlanets> ClickPlanetList = new Array<ClickablePlanets>();
        public BatchRemovalCollection<ClickableItemUnderConstruction> ItemsToBuild = new BatchRemovalCollection<UniverseScreen.ClickableItemUnderConstruction>();
        protected Array<ClickableSystem> ClickableSystems = new Array<ClickableSystem>();
        public BatchRemovalCollection<Ship> SelectedShipList = new BatchRemovalCollection<Ship>();
        protected Array<ClickableShip> ClickableShipsList = new Array<ClickableShip>();
        protected float PieMenuDelay = 1f;
        protected Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();
        public Background bg = new Background();
        public Vector2 Size = new Vector2(5000000f, 5000000f);
        public float FTLModifier = 1f;
        public float EnemyFTLModifier = 1f;
        public bool FTLInNuetralSystems = true;
        public UniverseData.GameDifficulty GameDifficulty = UniverseData.GameDifficulty.Normal;
        public Vector3 transitionStartPosition;
        public Vector3 camTransitionPosition;
        public Array<NebulousOverlay> Stars = new Array<NebulousOverlay>();
        public Array<NebulousOverlay> NebulousShit = new Array<NebulousOverlay>();
        private Rectangle ScreenRectangle;
        public Map<Guid, Planet> PlanetsDict = new Map<Guid, Planet>();
        public Map<Guid, SolarSystem> SolarSystemDict = new Map<Guid, SolarSystem>();
        public BatchRemovalCollection<Bomb> BombList = new BatchRemovalCollection<Bomb>();
        private AutoResetEvent DrawCompletedEvt         = new AutoResetEvent(false);
        private AutoResetEvent ProcessTurnsCompletedEvt = new AutoResetEvent(true);
        public float camHeight = 2550f;
        public Vector3 camPos = Vector3.Zero;
        public Array<Ship> ShipsToAdd = new Array<Ship>();
        protected float TooltipTimer = 0.5f;
        protected float sTooltipTimer = 0.5f;
        protected float TimerDelay = 0.25f;
        protected GameTime zgameTime = new GameTime();
        public Array<ShipModule> ModulesNeedingReset = new Array<ShipModule>();
        private bool TurnFlip = true;
        private int Auto = 1;
        private AutoResetEvent   ShipGateKeeper         = new AutoResetEvent(false);
        private ManualResetEvent SystemThreadGateKeeper = new ManualResetEvent(false);
        private AutoResetEvent   DeepSpaceGateKeeper    = new AutoResetEvent(false);
        private ManualResetEvent DeepSpaceDone          = new ManualResetEvent(false);
        private AutoResetEvent   EmpireGateKeeper       = new AutoResetEvent(false);
        private ManualResetEvent EmpireDone             = new ManualResetEvent(false);
        //public Array<Ship> ShipsToRemove = new Array<Ship>();
        public Array<Projectile> DSProjectilesToAdd = new Array<Projectile>();
        private Array<Ship> DeepSpaceShips = new Array<Ship>();
        private object thislock = new object();
        public bool ViewingShip = true;
        public float transDuration = 3f;
        protected float SectorMiniMapHeight = 20000f;
        public Vector2 mouseWorldPos;
        public float SelectedSomethingTimer = 3f;
        private Array<FleetButton> FleetButtons = new Array<FleetButton>();
        protected Vector2 startDrag;
        private Vector2 ProjectedPosition;
        protected float desiredSectorZ = 20000f;
        public Array<FogOfWarNode> FogNodes = new Array<FogOfWarNode>();
        private bool drawBloom = true;
        private Array<ClickableFleet> ClickableFleetsList = new Array<ClickableFleet>();
        private bool ShowTacticalCloseup;
        public bool Debug;
        public bool GridOn;
        public Planet SelectedPlanet;
        public Ship SelectedShip;
        public ClickableItemUnderConstruction SelectedItem;
        protected PieMenu pieMenu;
        protected PieMenuNode planetMenu;
        protected PieMenuNode shipMenu;
        protected float PieMenuTimer;
        public Matrix view;
        public Matrix projection;
        public ParticleSystem beamflashes;
        public ParticleSystem explosionParticles;
        public ParticleSystem photonExplosionParticles;
        public ParticleSystem explosionSmokeParticles;
        public ParticleSystem projectileTrailParticles;
        public ParticleSystem fireTrailParticles;
        public ParticleSystem smokePlumeParticles;
        public ParticleSystem fireParticles;
        public ParticleSystem engineTrailParticles;
        public ParticleSystem flameParticles;
        public ParticleSystem sparks;
        public ParticleSystem lightning;
        public ParticleSystem flash;
        public ParticleSystem star_particles;
        public ParticleSystem neb_particles;
        public Starfield starfield;
        public Background3D bg3d;
        public bool GravityWells;
        public Empire PlayerEmpire;
        public string PlayerLoyalty;
        public string loadFogPath;
        protected Model SunModel;
        protected Model NebModel;
        public Model xnaPlanetModel;
        public Texture2D RingTexture;
        public AudioListener listener;
        public Effect ThrusterEffect;
        public UnivScreenState viewState;
        public bool LookingAtPlanet;
        public bool snappingToShip;
        public bool returnToShip;
        public Vector3 transitionDestination;
        public Texture2D cloudTex;
        public EmpireUIOverlay EmpireUI;
        public BloomComponent bloomComponent;
        public Texture2D FogMap;
        protected RenderTarget2D FogMapTarget;
        public RenderTarget2D MainTarget;
        public RenderTarget2D MiniMapSector;
        public RenderTarget2D BorderRT;
        public RenderTarget2D StencilRT;
        protected RenderTarget2D LightsTarget;
        public Effect basicFogOfWarEffect;
        public Rectangle SectorMap;
        public Rectangle SectorSourceRect;
        public Rectangle GalaxyMap;
        public Rectangle SelectedStuffRect;
        public NotificationManager NotificationManager;
        public Selector minimapSelector;
        public Selector stuffSelector;
        public Rectangle MinimapDisplayRect;
        public Rectangle mmShowBorders;
        public Rectangle mmDSBW;
        public Rectangle mmShipView;
        public Rectangle mmAutomation;
        public MinimapButtons mmButtons;
        public Rectangle mmGalaxyView;
        public Rectangle mmHousing;
        private float MaxCamHeight;
        public AnomalyManager anomalyManager;
        public ShipInfoUIElement ShipInfoUIElement;
        public PlanetInfoUIElement pInfoUI;
        public SystemInfoUIElement sInfoUI;
        public ShipListInfoUIElement shipListInfoUI;
        public VariableUIElement vuiElement;
        private float ArmageddonTimer;
        public Empire player;
        private MiniMap minimap;
        private bool loading;
        public Thread ProcessTurnsThread;
        public bool WorkerUpdateGameWorld;
        public Ship playerShip;
        public float transitionElapsedTime;
        protected float Zrotate;
        public BoundingFrustum Frustum;
        protected ClickablePlanets tippedPlanet;
        protected ClickableSystem tippedSystem;
        protected bool ShowingSysTooltip;
        protected bool ShowingPlanetToolTip;
        protected float ClickTimer;
        protected float ClickTimer2;
        protected float zTime;
        private float MusicCheckTimer;
        private int ArmageddonCounter;
        private float shiptimer;
        public Ship ShipToView;
        public float HeightOnSnap;
        public float AdjustCamTimer;
        public bool SnapBackToSystem;
        public AutomationWindow aw;
        public bool DefiningAO;
        public Rectangle AORect;
        public bool showingDSBW;

        public bool showingFTLOverlay;
        public bool showingRangeOverlay;

        public DeepSpaceBuildingWindow dsbw;
        private DebugInfoScreen debugwin;
        private bool ShowShipNames;
        public InputState input;
        private float Memory;
        public bool Paused;
        public bool SkipRightOnce;
        private bool UseRealLights = true;
        private bool showdebugwindow;
        private bool NeedARelease;
        public SolarSystem SelectedSystem;
        public Fleet SelectedFleet;
        private Array<Fleet.Squad> SelectedFlank;
        private int FBTimer;
        private bool pickedSomethingThisFrame;
        private Vector2 startDragWorld;
        private Vector2 endDragWorld;
        private ShipGroup projectedGroup;
        private bool ProjectingPosition;
        private bool SelectingWithBox;
        private Effect AtmoEffect;
        private Model atmoModel;
        public PlanetScreen workersPanel;
        private ResolveTexture2D sceneMap;
        protected CursorState cState;
        private float radlast;
        private int SelectorFrame;
        public static bool debug;
        public int globalshipCount;
        public int empireShipCountReserve;
        //private float ztimeSnapShot;          //Not referenced in code, removing to save memory
        public ConcurrentBag<Ship> ShipsToRemove = new  ConcurrentBag<Ship>();
        public float Lag = 0;
        public Ship previousSelection;

        public UIButton ShipsInCombat;    
        public UIButton PlanetsInCombat;
        public int lastshipcombat = 0;
        public int lastplanetcombat = 0;
        public int reducer = 1;
        public float screenDelay = 0f;

        public UniverseScreen()
        {
        }

        public UniverseScreen(UniverseData data)
        {
            Size                        = data.Size;
            FTLModifier                 = data.FTLSpeedModifier;
            EnemyFTLModifier            = data.EnemyFTLSpeedModifier;
            GravityWells                = data.GravityWells;
            SolarSystemList             = data.SolarSystemsList;
            MasterShipList              = data.MasterShipList;
            playerShip                  = data.playerShip;
            PlayerEmpire                = playerShip.loyalty;
            PlayerLoyalty               = playerShip.loyalty.data.Traits.Name;
            playerShip.loyalty.isPlayer = true;
            ShipToView                  = playerShip;

        }
        
        public UniverseScreen(UniverseData data, string loyalty)
        {
            Size                  = data.Size;
            FTLModifier           = data.FTLSpeedModifier;
            EnemyFTLModifier      = data.EnemyFTLSpeedModifier;
            GravityWells          = data.GravityWells;
            SolarSystemList       = data.SolarSystemsList;
            MasterShipList        = data.MasterShipList;
            loadFogPath           = data.loadFogPath;
            playerShip            = data.playerShip;
            PlayerLoyalty         = loyalty;
            PlayerEmpire          = EmpireManager.GetEmpireByName(loyalty);
            PlayerEmpire.isPlayer = true;
            ShipToView            = playerShip;
            loading               = true;
        }

        public UniverseScreen(int numsys, float size)
        {
            Size.X = size;
            Size.Y = size;
        }

        ~UniverseScreen() { Destroy(); }

        public void SetLighting(bool real)
        {
            lock (GlobalStats.ObjectManagerLocker)
                ScreenManager.inter.LightManager.Clear();
            if (real)
            {
                foreach (SolarSystem system in SolarSystemList)
                {
                    AddLight(system, 2.5f, 150000f, zpos: +2500f, fillLight: true);
                    AddLight(system, 2.5f, 5000f,   zpos: -2500f, fillLight: false);
                    AddLight(system, 1.0f, 100000f, zpos: -6500f, fillLight: false);
                }
                return;
            }

            LightRig rig = ScreenManager.Content.Load<LightRig>("example/NewGamelight_rig");
            lock (GlobalStats.ObjectManagerLocker)
                ScreenManager.inter.LightManager.Submit(rig);
        }

        protected void AddLight(SolarSystem system, float intensity, float radius, float zpos, bool fillLight)
        {
            PointLight light = new PointLight
            {
                DiffuseColor = new Vector3(1f, 1f, 0.85f),
                Intensity    = intensity,
                ObjectType   = ObjectType.Static, // RedFox: changed this to Static
                FillLight    = true,
                Radius       = radius,
                Position     = new Vector3(system.Position, zpos),
                Enabled      = true
            };
            light.World = Matrix.Identity * Matrix.CreateTranslation(light.Position);
            light.AddTo(this);
        }

        protected virtual void LoadMenu()
        {
            var viewPlanetIcon = ResourceManager.TextureDict["UI/viewPlanetIcon"];
            pieMenu    = new PieMenu();
            planetMenu = new PieMenuNode();
            shipMenu   = new PieMenuNode();
            planetMenu.Add(new PieMenuNode("View Planet", viewPlanetIcon, ViewPlanet));
            planetMenu.Add(new PieMenuNode("Mark for Colonization", viewPlanetIcon, MarkForColonization));
            shipMenu.Add(new PieMenuNode("Commandeer Ship", viewPlanetIcon, ViewShip));
        }

        protected Vector2 CalculateCameraPositionOnMouseZoom(Vector2 MousePosition, float DesiredCamHeight)
        {
            Vector2 vector2_1 = new Vector2(MousePosition.X - (float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), MousePosition.Y - (float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
            Vector3 position1 = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector3 direction1 = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 1f), this.projection, this.view, Matrix.Identity) - position1;
            direction1.Normalize();
            Ray ray = new Ray(position1, direction1);
            float num1 = -ray.Position.Z / ray.Direction.Z;
            Vector3 source = new Vector3(ray.Position.X + num1 * ray.Direction.X, ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
            Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(180f.ToRadians()) * Matrix.CreateRotationX(0.0f.ToRadians()) * Matrix.CreateLookAt(new Vector3(this.camPos.X, this.camPos.Y, DesiredCamHeight), new Vector3(this.camPos.X, this.camPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(source, this.projection, view, Matrix.Identity);
            Vector2 vector2_2 = new Vector2((float)(int)vector3.X - vector2_1.X, (float)(int)vector3.Y - vector2_1.Y);
            Vector3 position2 = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(vector2_2.X, vector2_2.Y, 0.0f), this.projection, view, Matrix.Identity);
            Vector3 direction2 = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(vector2_2.X, vector2_2.Y, 1f), this.projection, view, Matrix.Identity) - position2;
            direction2.Normalize();
            ray = new Ray(position2, direction2);
            float num2 = -ray.Position.Z / ray.Direction.Z;
            return new Vector2(ray.Position.X + num2 * ray.Direction.X, ray.Position.Y + num2 * ray.Direction.Y);
        }

        protected void LoadMenuNodes(bool Owned, bool Habitable)
        {
            this.planetMenu.Children.Clear();
            this.planetMenu.Add(new PieMenuNode(Localizer.Token(1421), ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.ViewPlanet)));
            if (!Owned && Habitable)
                this.planetMenu.Add(new PieMenuNode(Localizer.Token(1422), ResourceManager.TextureDict["UI/ColonizeIcon"], new SimpleDelegate(this.MarkForColonization)));
            if (!Habitable)
                return;
            this.planetMenu.Add(new PieMenuNode(Localizer.Token(1423), ResourceManager.TextureDict["UI/ColonizeIcon"], new SimpleDelegate(this.OpenCombatMenu)));
        }

        public void OpenCombatMenu(object sender)
        {
            this.workersPanel = new CombatScreen(this.ScreenManager, this.SelectedPlanet);
            this.LookingAtPlanet = true;
            this.transitionStartPosition = this.camPos;
            this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y + 400f, 2500f);
            this.AdjustCamTimer = 2f;
            this.transitionElapsedTime = 0.0f;
            this.transDuration = 5f;
            if (this.ViewingShip)
                this.returnToShip = true;
            this.ViewingShip = false;
            this.snappingToShip = false;
        }

        public void FollowPlayer(object sender)
        {
            SelectedShip.GetAI().State = AIState.Escort;
            SelectedShip.GetAI().EscortTarget = this.playerShip;
        }

        public void RefitTo(object sender)
        {
            if (SelectedShip != null)
                ScreenManager.AddScreen(new RefitToWindow(SelectedShip));
        }

        public void OrderScrap(object sender)
        {
            SelectedShip?.GetAI().OrderScrapShip();
        }

        public void OrderScuttle(object sender)
        {
            if (SelectedShip != null)
                SelectedShip.ScuttleTimer = 10f;
        }

        protected void LoadShipMenuNodes(int which)
        {
            shipMenu.Children.Clear();
            if (which == 1)
            {
                if (SelectedShip != null && SelectedShip == playerShip)
                    shipMenu.Add(new PieMenuNode("Relinquish Control", ResourceManager.TextureDict["UI/viewPlanetIcon"], ViewShip));
                else
                    shipMenu.Add(new PieMenuNode(Localizer.Token(1412), ResourceManager.TextureDict["UI/viewPlanetIcon"], ViewShip));
                PieMenuNode newChild1 = new PieMenuNode(Localizer.Token(1413), ResourceManager.TextureDict["UI/OrdersIcon"], null);
                shipMenu.Add(newChild1);
                if (SelectedShip != null && SelectedShip.CargoSpace_Max > 0.0)
                {
                    newChild1.Add(new PieMenuNode(Localizer.Token(1414), ResourceManager.TextureDict["UI/PatrolIcon"], DoTransport));
                    newChild1.Add(new PieMenuNode(Localizer.Token(1415), ResourceManager.TextureDict["UI/marketIcon"], DoTransportGoods));
                }
                newChild1.Add(new PieMenuNode(Localizer.Token(1416), ResourceManager.TextureDict["UI/marketIcon"], DoExplore));
                newChild1.Add(new PieMenuNode("Empire Defense", ResourceManager.TextureDict["UI/PatrolIcon"], DoDefense));
                PieMenuNode newChild6 = new PieMenuNode(Localizer.Token(1417), ResourceManager.TextureDict["UI/FollowIcon"], null);
                shipMenu.Add(newChild6);
                if (SelectedShip != null && SelectedShip.shipData.Role != ShipData.RoleName.station && SelectedShip.shipData.Role != ShipData.RoleName.platform)
                {
                    newChild6.Add(new PieMenuNode(Localizer.Token(1418), ResourceManager.TextureDict["UI/FollowIcon"], RefitTo));
                }
                if (SelectedShip != null && (SelectedShip.shipData.Role == ShipData.RoleName.station || SelectedShip.shipData.Role == ShipData.RoleName.platform))
                {
                    newChild6.Add(new PieMenuNode("Scuttle", ResourceManager.TextureDict["UI/HoldPositionIcon"], OrderScuttle));
                }
                else
                {
                    if (SelectedShip == null || SelectedShip.shipData.Role > ShipData.RoleName.construction)
                        return;
                    newChild6.Add(new PieMenuNode(Localizer.Token(1419), ResourceManager.TextureDict["UI/HoldPositionIcon"], OrderScrap));
                }
            }
            else
                shipMenu.Add(new PieMenuNode(Localizer.Token(1420), ResourceManager.TextureDict["UI/viewPlanetIcon"], ContactLeader));
        }

        public void ContactLeader(object sender)
        {
            if (this.SelectedShip == null)
                return;
            if (this.SelectedShip.loyalty.isFaction)
            {
                foreach (Encounter e in ResourceManager.Encounters)
                {
                    if (this.SelectedShip.loyalty.data.Traits.Name == e.Faction && this.player.GetRelations(SelectedShip.loyalty).EncounterStep == e.Step)
                    {
                        this.ScreenManager.AddScreen((GameScreen)new EncounterPopup(this, this.player, this.SelectedShip.loyalty, (SolarSystem)null, e));
                        break;
                    }
                }
            }
            else
                this.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(this.SelectedShip.loyalty, this.player, "Greeting"));
        }

        public void DoHoldPosition(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.GetAI().HoldPosition();
        }

        public void DoExplore(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.GetAI().OrderExplore();
        }

        public void DoTransport(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.GetAI().OrderTransportPassengers(5f);
        }

        public void DoDefense(object sender)
        {
            if (this.SelectedShip == null || this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this.SelectedShip))
                return;
            this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(this.SelectedShip);
            this.SelectedShip.GetAI().OrderQueue.Clear();
            this.SelectedShip.GetAI().HasPriorityOrder = false;
            this.SelectedShip.GetAI().SystemToDefend = (SolarSystem)null;
            this.SelectedShip.GetAI().SystemToDefendGuid = Guid.Empty;
            this.SelectedShip.GetAI().State = AIState.SystemDefender;
        }

        public void DoTransportGoods(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.GetAI().State = AIState.SystemTrader;
            this.SelectedShip.GetAI().start = null;
            this.SelectedShip.GetAI().end = null;
            this.SelectedShip.GetAI().OrderTrade(5f);
        }

        public void ViewShip(object sender)
        {
            if (this.SelectedShip == null)
                return;
            if (this.playerShip != null && this.SelectedShip == this.playerShip)
            {
                this.playerShip.PlayerShip = false;
                this.playerShip.GetAI().State = AIState.AwaitingOrders;
                this.playerShip = (Ship)null;
            }
            else
            {
                if (this.SelectedShip.loyalty != this.player || this.SelectedShip.isConstructor)
                    return;
                this.ShipToView = this.SelectedShip;
                this.snappingToShip = true;
                this.HeightOnSnap = this.camHeight;
                this.transitionDestination.Z = 3500f;
                if (this.playerShip != null)
                {
                    this.playerShip.PlayerShip = false;
                    this.playerShip.GetAI().State = AIState.AwaitingOrders;
                    this.playerShip = this.SelectedShip;
                    this.playerShip.PlayerShip = true;
                    this.playerShip.GetAI().State = AIState.ManualControl;
                }
                else
                {
                    this.playerShip = this.SelectedShip;
                    this.playerShip.PlayerShip = true;
                    this.playerShip.GetAI().State = AIState.ManualControl;
                }
                this.AdjustCamTimer = 1.5f;
                this.transitionElapsedTime = 0.0f;
                this.transitionDestination.Z = 4500f;
                this.snappingToShip = true;
                this.ViewingShip = true;
                if (!this.playerShip.isSpooling)
                    return;
                this.playerShip.HyperspaceReturn();
            }
        }

        public void ViewToShip(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.ShipToView = this.SelectedShip;
            this.ShipInfoUIElement.SetShip(this.SelectedShip);  //fbedard: was not updating correctly from shiplist
            this.SelectedFleet = (Fleet)null;
            this.SelectedShipList.Clear();
            this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
            this.SelectedSystem = (SolarSystem)null;
            this.SelectedPlanet = (Planet)null;
            this.snappingToShip = true;
            this.HeightOnSnap = this.camHeight;
            this.transitionDestination.Z = 3500f;
            this.AdjustCamTimer = 1.0f;
            this.transitionElapsedTime = 0.0f;
            this.transitionDestination.Z = 4500f;
            this.snappingToShip = true;
            this.ViewingShip = true;
        }

        public void ViewPlanet(object sender)
        {
            this.ShowShipNames = false;
            if (this.SelectedPlanet == null)
                return;
            if (!this.SelectedPlanet.system.ExploredDict[this.player])
            {
                this.PlayNegativeSound();
            }
            else
            {
                bool flag = false;
                foreach (Mole mole in this.player.data.MoleList)
                {
                    if (mole.PlanetGuid == this.SelectedPlanet.guid)
                        flag = true;
                }
                this.workersPanel = this.SelectedPlanet.Owner == this.player || flag || this.Debug && this.SelectedPlanet.Owner != null ? (PlanetScreen)new ColonyScreen(this.SelectedPlanet, this.ScreenManager, this.EmpireUI) : (this.SelectedPlanet.Owner == null ? (PlanetScreen)new UnexploredPlanetScreen(this.SelectedPlanet, this.ScreenManager) : (PlanetScreen)new UnownedPlanetScreen(this.SelectedPlanet, this.ScreenManager));
                this.LookingAtPlanet = true;
                this.transitionStartPosition = this.camPos;
                this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y + 400f, 2500f);
                this.AdjustCamTimer = 2f;
                this.transitionElapsedTime = 0.0f;
                this.transDuration = 5f;
                if (this.ViewingShip)
                    this.returnToShip = true;
                this.ViewingShip = false;
                this.snappingToShip = false;
                this.SelectedFleet = (Fleet)null;
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = (Ship)null;
                this.SelectedShipList.Clear();
                this.SelectedItem = null;
            }
        }

        public void SnapViewSystem(SolarSystem system, UniverseScreen.UnivScreenState camHeight)
        {
            float x = this.GetZfromScreenState(camHeight);
            this.transitionDestination = new Vector3(system.Position.X, system.Position.Y + 400f, x);
            this.transitionStartPosition = this.camPos;
            this.AdjustCamTimer = 2f;
            this.transitionElapsedTime = 0.0f;
            this.transDuration = 5f;
            this.ViewingShip = false;
            this.snappingToShip = false;
            if (this.ViewingShip)
                this.returnToShip = true;
            this.ViewingShip = false;
            this.snappingToShip = false;
            this.SelectedFleet = null;
            if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                this.previousSelection = this.SelectedShip;
            this.SelectedShip = null;
            this.SelectedShipList.Clear();
            this.SelectedItem = null;
        }

        public void SnapViewPlanet(object sender)
        {
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;
            transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 2500f);
            if (!SelectedPlanet.system.ExploredDict[player])
            {
                PlayNegativeSound();
            }
            else
            {
                bool flag = player.data.MoleList.Any(mole => mole.PlanetGuid == SelectedPlanet.guid);

                if (SelectedPlanet.Owner == player || flag || Debug && SelectedPlanet.Owner != null)
                    workersPanel = new ColonyScreen(SelectedPlanet, ScreenManager, EmpireUI);
                else if (SelectedPlanet.Owner != null)
                {
                    workersPanel = new UnownedPlanetScreen(SelectedPlanet, ScreenManager);
                    transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 95000f);
                }
                else
                {
                    workersPanel = new UnexploredPlanetScreen(SelectedPlanet, ScreenManager);
                    transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 95000f);
                }
                SelectedPlanet.ExploredDict[player] = true;
                LookingAtPlanet         = true;
                transitionStartPosition = camPos;
                AdjustCamTimer          = 2f;
                transitionElapsedTime   = 0.0f;
                transDuration           = 5f;
                if (ViewingShip)
                    returnToShip = true;
                ViewingShip    = false;
                snappingToShip = false;
                SelectedFleet  = null;
                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = null;
                SelectedItem = null;
                SelectedShipList.Clear();
            }
        }

        protected void MarkForColonization(object sender)
        {
            player.GetGSAI().Goals.Add(new Goal(SelectedPlanet, player));
        }

        protected void ViewSystem(SolarSystem system)
        {
            this.ViewingShip = false;
            this.transitionDestination = new Vector3(system.Position, 147000f);
            this.AdjustCamTimer = 1f;
            this.transDuration = 3f;
            this.transitionElapsedTime = 0.0f;
        }

        public override void LoadContent()
        {
            GlobalStats.ResearchRootUIDToDisplay = "Colonization";
            SystemInfoUIElement.SysFont = Fonts.Arial12Bold;
            SystemInfoUIElement.DataFont = Fonts.Arial10;
            this.NotificationManager = new NotificationManager(this.ScreenManager, this);
            this.aw = new AutomationWindow(this.ScreenManager, this);
            for (int index = 0; (double)index < (double)this.Size.X / 5000.0; ++index)
            {
                NebulousOverlay nebulousOverlay = new NebulousOverlay();
                float z = RandomMath.RandomBetween(-200000f, -2E+07f);
                nebulousOverlay.Path = "Textures/smoke";
                nebulousOverlay.Position = new Vector3(RandomMath.RandomBetween(-0.5f * this.Size.X, this.Size.X + 0.5f * this.Size.X), RandomMath.RandomBetween(-0.5f * this.Size.X, this.Size.X + 0.5f * this.Size.X), z);
                float radians = RandomMath.RandomBetween(0.0f, 6.283185f);
                nebulousOverlay.Scale = RandomMath.RandomBetween(10f, 100f);
                nebulousOverlay.WorldMatrix = Matrix.CreateScale(50f) * Matrix.CreateScale(nebulousOverlay.Scale) * Matrix.CreateRotationZ(radians) * Matrix.CreateTranslation(nebulousOverlay.Position);
                this.Stars.Add(nebulousOverlay);
            }
            this.LoadGraphics();
            DeepSpaceManager.Setup((int)this.Size.X, (int)this.Size.Y, (int)(500000.0 * (double)this.GameScale), new Vector2(this.Size.X / 2f, this.Size.Y / 2f));       //Mer Investigate me
            UniverseScreen.ShipSpatialManager.Setup((int)this.Size.X, (int)this.Size.Y, (int)(500000.0 * (double)this.GameScale), new Vector2(this.Size.X / 2f, this.Size.Y / 2f));
            this.DoParticleLoad();
            this.bg3d = new Background3D(this);
            this.starfield = new Starfield(Vector2.Zero, this.ScreenManager.GraphicsDevice, this.ScreenManager.Content);
            this.starfield.LoadContent();
            GameplayObject.audioListener = this.listener;
            Weapon.audioListener = this.listener;
            GameplayObject.audioListener = this.listener;
            this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, (float)this.ScreenManager.GraphicsDevice.Viewport.Width / (float)this.ScreenManager.GraphicsDevice.Viewport.Height, 1000f, 3E+07f);
            this.SetLighting(this.UseRealLights);
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                foreach (string FleetUID in solarSystem.DefensiveFleets)
                {
                    Fleet defensiveFleetAt = HelperFunctions.CreateDefensiveFleetAt(FleetUID, EmpireManager.Remnants, solarSystem.PlanetList[0].Position);
                    MilitaryTask militaryTask = new MilitaryTask();
                    militaryTask.AO = solarSystem.PlanetList[0].Position;
                    militaryTask.AORadius = 120000f;
                    militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                    defensiveFleetAt.Task = militaryTask;
                    defensiveFleetAt.TaskStep = 3;
                    militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
                    EmpireManager.Remnants.GetFleetsDict().TryAdd(EmpireManager.Remnants.GetFleetsDict().Count + 10, defensiveFleetAt);
                    EmpireManager.Remnants.GetGSAI().TaskList.Add(militaryTask);
                    militaryTask.Step = 2;
                }
				if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.customRemnantElements)
                {
                    foreach (Planet p in solarSystem.PlanetList)
                    {
                        foreach (string FleetUID in p.PlanetFleets)
                        {
                            Fleet planetFleetAt = HelperFunctions.CreateDefensiveFleetAt(FleetUID, EmpireManager.Remnants, p.Position);
                            MilitaryTask militaryTask = new MilitaryTask();
                            militaryTask.AO = solarSystem.PlanetList[0].Position;
                            militaryTask.AORadius = 120000f;
                            militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                            planetFleetAt.Task = militaryTask;
                            planetFleetAt.TaskStep = 3;
                            militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
                            EmpireManager.Remnants.GetFleetsDict().TryAdd(EmpireManager.Remnants.GetFleetsDict().Count + 10, planetFleetAt);
                            EmpireManager.Remnants.GetGSAI().TaskList.Add(militaryTask);
                            militaryTask.Step = 2;
                        }
                    }
                }

                foreach (SolarSystem.FleetAndPos fleetAndPos in solarSystem.FleetsToSpawn)
                {
                    Fleet defensiveFleetAt = HelperFunctions.CreateDefensiveFleetAt(fleetAndPos.fleetname, EmpireManager.Remnants, solarSystem.Position + fleetAndPos.Pos);
                    MilitaryTask militaryTask = new MilitaryTask();
                    militaryTask.AO = solarSystem.Position + fleetAndPos.Pos;
                    militaryTask.AORadius = 75000f;
                    militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                    defensiveFleetAt.Task = militaryTask;
                    defensiveFleetAt.TaskStep = 3;
                    militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
                    EmpireManager.Remnants.GetFleetsDict().TryAdd(EmpireManager.Remnants.GetFleetsDict().Count + 10, defensiveFleetAt);
                    EmpireManager.Remnants.GetGSAI().TaskList.Add(militaryTask);
                    militaryTask.Step = 2;
                }
                foreach (string key in solarSystem.ShipsToSpawn)
                    ResourceManager.CreateShipAt(key, EmpireManager.Remnants, solarSystem.PlanetList[0], true);
                foreach (Planet p in solarSystem.PlanetList)
                {
                    if (p.Owner != null)
                    {
                        foreach (string key in p.Guardians)
                            ResourceManager.CreateShipAt(key, p.Owner, p, true);
                    }
                    else
                    {
                        //Added by McShooterz: alternate hostile fleets populate universe
						if (GlobalStats.ActiveModInfo != null && ResourceManager.HostileFleets.Fleets.Count > 0)
                        {
                            if (p.Guardians.Count > 0)
                            {
                                int randomFleet = RandomMath.InRange(ResourceManager.HostileFleets.Fleets.Count);
                                foreach (string ship in ResourceManager.HostileFleets.Fleets[randomFleet].Ships)
                                {
                                    ResourceManager.CreateShipAt(ship, EmpireManager.GetEmpireByName(ResourceManager.HostileFleets.Fleets[randomFleet].Empire), p, true);
                                }
                            }
                        }
                        else
                        {
                            foreach (string key in p.Guardians)
                                ResourceManager.CreateShipAt(key, EmpireManager.Remnants, p, true);
                            if (p.CorsairPresence)
                            {
                                ResourceManager.CreateShipAt("Corsair Asteroid Base", EmpireManager.Corsairs, p, true).TetherToPlanet(p);
                                ResourceManager.CreateShipAt("Corsair", EmpireManager.Corsairs, p, true);
                                ResourceManager.CreateShipAt("Captured Gunship", EmpireManager.Corsairs, p, true);
                                ResourceManager.CreateShipAt("Captured Gunship", EmpireManager.Corsairs, p, true);
                            }
                        }
                    }
                }
                foreach (Anomaly anomaly in solarSystem.AnomaliesList)
                {
                    if (anomaly.type == "DP")
                        this.anomalyManager.AnomaliesList.Add((Anomaly)new DimensionalPrison(solarSystem.Position + anomaly.Position));
                }
            }
            float num = 10f;
            Matrix matrix = this.view;
            this.MaxCamHeight = 4E+07f;
            while ((double)num < (double)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 50))
            {
                Vector2 vector2_1 = new Vector2(this.Size.X / 2f, this.Size.Y / 2f);
                Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(180f.ToRadians()) * Matrix.CreateRotationX(0.0f.ToRadians()) * Matrix.CreateLookAt(new Vector3(-vector2_1.X, vector2_1.Y, this.MaxCamHeight), new Vector3(-vector2_1.X, vector2_1.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(Vector3.Zero, this.projection, view, Matrix.Identity);
                Vector2 vector2_2 = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size, 0.0f), this.projection, view, Matrix.Identity);
                num = new Vector2(vector3_2.X, vector3_2.Y).X - vector2_2.X;
                if ((double)num < (double)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 50))
                    this.MaxCamHeight -= 0.1f * this.MaxCamHeight;
            }
            if (MaxCamHeight > 23000000) MaxCamHeight = 23000000; 
            if (!this.loading)
            {
                this.camPos.X = this.playerShip.Center.X;
                this.camPos.Y = this.playerShip.Center.Y;
                this.camHeight = 2750f;
            }
            transitionDestination = new Vector3(camPos.X, camPos.Y, camHeight);
            foreach (NebulousOverlay nebulousOverlay in Stars)
                this.star_particles.AddParticleThreadA(nebulousOverlay.Position, Vector3.Zero);

            PlanetsDict.Clear();
            SolarSystemDict.Clear();
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                SolarSystemDict.Add(solarSystem.guid, solarSystem);
                foreach (Planet planet in solarSystem.PlanetList)
                    PlanetsDict.Add(planet.guid, planet);
            }
            foreach (Ship ship in MasterShipList)
            {
                if (ship.TetherGuid != Guid.Empty)
                    ship.TetherToPlanet(PlanetsDict[ship.TetherGuid]);
            }

            ProcessTurnsThread = new Thread(ProcessTurns);
            ProcessTurnsThread.Name = "Universe.ProcessTurns()";
            ProcessTurnsThread.IsBackground = false; // RedFox - make sure ProcessTurns runs with top priority
            ProcessTurnsThread.Start();
        }

        private void DoParticleLoad()
        {
            var content = ScreenManager.Content;
            var device = ScreenManager.GraphicsDevice;
            beamflashes              = new ParticleSystem(Game1.Instance, content, "3DParticles/BeamFlash", device);
            explosionParticles       = new ParticleSystem(Game1.Instance, content, "3DParticles/ExplosionSettings", device);
            photonExplosionParticles = new ParticleSystem(Game1.Instance, content, "3DParticles/PhotonExplosionSettings", device);
            explosionSmokeParticles  = new ParticleSystem(Game1.Instance, content, "3DParticles/ExplosionSmokeSettings", device);
            projectileTrailParticles = new ParticleSystem(Game1.Instance, content, "3DParticles/ProjectileTrailSettings", device);
            fireTrailParticles       = new ParticleSystem(Game1.Instance, content, "3DParticles/FireTrailSettings", device);
            smokePlumeParticles      = new ParticleSystem(Game1.Instance, content, "3DParticles/SmokePlumeSettings", device);
            fireParticles            = new ParticleSystem(Game1.Instance, content, "3DParticles/FireSettings", device);
            engineTrailParticles     = new ParticleSystem(Game1.Instance, content, "3DParticles/EngineTrailSettings", device);
            flameParticles           = new ParticleSystem(Game1.Instance, content, "3DParticles/FlameSettings", device);
            sparks                   = new ParticleSystem(Game1.Instance, content, "3DParticles/sparks", device);
            lightning                = new ParticleSystem(Game1.Instance, content, "3DParticles/lightning", device);
            flash                    = new ParticleSystem(Game1.Instance, content, "3DParticles/FlashSettings", device);
            star_particles           = new ParticleSystem(Game1.Instance, content, "3DParticles/star_particles", device);
            neb_particles            = new ParticleSystem(Game1.Instance, content, "3DParticles/GalaxyParticle", device);
        }

        public void LoadGraphics()
        {
            const int minimapOffSet = 14;

            var content = ScreenManager.Content;
            var device  = ScreenManager.GraphicsDevice;
            int width   = device.PresentationParameters.BackBufferWidth;
            int height  = device.PresentationParameters.BackBufferHeight;

            projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, device.Viewport.Width / (float)device.Viewport.Height, 1000f, 3E+07f);
            Frustum            = new BoundingFrustum(view * projection);
            mmHousing          = new Rectangle(width - (276 + minimapOffSet), height - 256, 276 + minimapOffSet, 256);
            MinimapDisplayRect = new Rectangle(mmHousing.X + 61 + minimapOffSet, mmHousing.Y + 43, 200, 200);
            minimap            = new MiniMap(mmHousing);
            mmButtons          = new MinimapButtons(mmHousing, EmpireUI);
            mmShowBorders      = new Rectangle(MinimapDisplayRect.X, MinimapDisplayRect.Y - 25, 32, 32);
            mmDSBW             = new Rectangle(mmShowBorders.X + 32, mmShowBorders.Y, 64, 32);
            mmAutomation       = new Rectangle(mmDSBW.X + mmDSBW.Width, mmShowBorders.Y, 96, 32);
            mmShipView         = new Rectangle(MinimapDisplayRect.X - 32, MinimapDisplayRect.Y, 32, 105);
            mmGalaxyView       = new Rectangle(mmShipView.X, mmShipView.Y + 105, 32, 105);
            SectorMap          = new Rectangle(width - 300, height - 150, 150, 150);
            GalaxyMap          = new Rectangle(SectorMap.X + SectorMap.Width, height - 150, 150, 150);
            SelectedStuffRect  = new Rectangle(0, height - 247, 407, 242);
            ShipInfoUIElement  = new ShipInfoUIElement(SelectedStuffRect, ScreenManager, this);
            sInfoUI            = new SystemInfoUIElement(SelectedStuffRect, ScreenManager, this);
            pInfoUI            = new PlanetInfoUIElement(SelectedStuffRect, ScreenManager, this);
            shipListInfoUI     = new ShipListInfoUIElement(SelectedStuffRect, ScreenManager, this);
            vuiElement         = new VariableUIElement(SelectedStuffRect, ScreenManager, this);
            SectorSourceRect   = new Rectangle((width - 720) / 2, (height - 720) / 2, 720, 720);
            EmpireUI           = new EmpireUIOverlay(player, device, this);
            bloomComponent     = new BloomComponent(ScreenManager);
            bloomComponent.LoadContent();
            aw = new AutomationWindow(ScreenManager, this);
            SurfaceFormat backBufferFormat = device.PresentationParameters.BackBufferFormat;
            sceneMap      = new ResolveTexture2D(device, width, height, 1, backBufferFormat);
            MainTarget    = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            LightsTarget  = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            MiniMapSector = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            BorderRT      = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            StencilRT     = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            string folderPath = Dir.ApplicationData;
            if (loadFogPath == null)
            {
                FogMap = ResourceManager.TextureDict["UniverseFeather"];
            }
            else
            {
                using (FileStream fileStream = File.OpenRead(folderPath + "/StarDrive/Saved Games/Fog Maps/" + loadFogPath + ".png"))
                    FogMap = Texture2D.FromFile(device, fileStream);
            }
            FogMapTarget = new RenderTarget2D(device, 512, 512, 1, backBufferFormat, device.PresentationParameters.MultiSampleType, device.PresentationParameters.MultiSampleQuality);
            basicFogOfWarEffect = content.Load<Effect>("Effects/BasicFogOfWar");
            LoadMenu();

            anomalyManager = new AnomalyManager();
            listener       = new AudioListener();
            MuzzleFlashManager.flashModel   = content.Load<Model>("Model/Projectiles/muzzleEnergy");
            MuzzleFlashManager.FlashTexture = content.Load<Texture2D>("Model/Projectiles/Textures/MuzzleFlash_01");
            xnaPlanetModel                  = content.Load<Model>("Model/SpaceObjects/planet");
            atmoModel                       = content.Load<Model>("Model/sphere");
            AtmoEffect                      = content.Load<Effect>("Effects/PlanetHalo");
            cloudTex                        = content.Load<Texture2D>("Model/SpaceObjects/earthcloudmap");
            RingTexture                     = content.Load<Texture2D>("Model/SpaceObjects/planet_rings");
            ThrusterEffect                  = content.Load<Effect>("Effects/Thrust");
            SunModel                        = content.Load<Model>("Model/SpaceObjects/star_plane");
            NebModel                        = content.Load<Model>("Model/SpaceObjects/star_plane");
            FTLManager.LoadContent(content);
            Projectile.contentManager             = content;
            MuzzleFlashManager.universeScreen     = this;
            DroneAI.universeScreen                = this;
            ExplosionManager.Universe       = this;
            ShipDesignScreen.screen               = this;
            Fleet.screen                          = this;
            Bomb.Screen                           = this;
            Anomaly.screen                        = this;
            PlanetScreen.screen                   = this;
            MinimapButtons.screen                 = this;
            Projectile.universeScreen             = this;
            ShipModule.universeScreen             = this;
            Empire.Universe                       = this;
            ResourceManager.UniverseScreen        = this;
            Planet.universeScreen                 = this;
            Weapon.universeScreen                 = this;
            Ship.universeScreen                   = this;
            ArtificialIntelligence.universeScreen = this;
            MissileAI.universeScreen              = this;
            CombatScreen.universeScreen           = this;
            FleetDesignScreen.screen              = this;
            ScreenRectangle = new Rectangle(0, 0, width, height);
            starfield = new Starfield(Vector2.Zero, device, content);
            starfield.LoadContent();

            // fbedard: new button for ShipsInCombat
            var empireTopBtnTex = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"];
            ShipsInCombat = new UIButton
            {
                Rect           = new Rectangle(width - 275, height - 280, empireTopBtnTex.Width, empireTopBtnTex.Height),
                NormalTexture  = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu"],
                HoverTexture   = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_pressed"],
                Text           = "Ships: 0",
                Launches       = "ShipsInCombat"
            };
            // fbedard: new button for PlanetsInCombat
            PlanetsInCombat = new UIButton
            {
                Rect           = new Rectangle(width - 135, height - 280, empireTopBtnTex.Width, empireTopBtnTex.Height),
                NormalTexture  = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu"],
                HoverTexture   = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_pressed"],
                Text           = "Planets: 0",
                Launches       = "PlanetsInCombat"
            };
        }

        protected void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        }

        public override void UnloadContent()
        {
            starfield?.UnloadContent();
            ScreenManager.inter.Unload();
            ScreenManager.lightingSystemManager.Unload();
            base.UnloadContent();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (viewState > UnivScreenState.ShipView)
            {
                foreach (NebulousOverlay nebulousOverlay in NebulousShit)
                    engineTrailParticles.AddParticleThreadA(nebulousOverlay.Position, Vector3.Zero);
            }
            zgameTime = gameTime;
            float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
            SelectedSomethingTimer -= num;
            if (SelectorFrame < 299)
                ++SelectorFrame;
            else
                SelectorFrame = 0;
            MusicCheckTimer -= num;
            if (MusicCheckTimer <= 0.0f)
            {
                if (ScreenManager.Music == null || ScreenManager.Music != null && ScreenManager.Music.IsStopped)
                {
                    ScreenManager.Music = AudioManager.GetCue("AmbientMusic");
                    ScreenManager.Music.Play();
                }
                MusicCheckTimer = 2f;
            }
            AudioManager.AudioEngine.Update();
            listener.Position = new Vector3(camPos.X, camPos.Y, 0.0f);
            lock (GlobalStats.ObjectManagerLocker)
                ScreenManager.inter.Update(gameTime);

            EmpireUI.Update(deltaTime);

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
            zTime += deltaTime;
        }

        private void ProcessTurns()
        {
            int failedLoops = 0; // for detecting cyclic crash loops
            while (true)
            {
                try
                {
                    // Wait for Draw() to finish. While SwapBuffers is blocking, we process the turns inbetween
                    DrawCompletedEvt.WaitOne();

                    float deltaTime = (float)zgameTime.ElapsedGameTime.TotalSeconds;
                    if (Paused)
                    {
                        UpdateAllSystems(0.0f);
                        foreach (Ship ship in MasterShipList)
                        {
                            if (viewState <= UnivScreenState.SystemView && Frustum.Contains(ship.Position, 2000f))
                            {
                                ship.InFrustum = true;
                                ship.GetSO().Visibility = ObjectVisibility.Rendered;
                                ship.GetSO().World = Matrix.Identity 
                                    * Matrix.CreateRotationY(ship.yRotation) 
                                    * Matrix.CreateRotationX(ship.xRotation) 
                                    * Matrix.CreateRotationZ(ship.Rotation) 
                                    * Matrix.CreateTranslation(new Vector3(ship.Center, 0.0f));
                            }
                            else
                            {
                                ship.InFrustum = false;
                                ship.GetSO().Visibility = ObjectVisibility.None;
                            }
                        }
                        ClickTimer += deltaTime;
                        ClickTimer2 += deltaTime;
                        pieMenu.Update(zgameTime);
                        PieMenuTimer += deltaTime;
                    }
                    else
                    {
                        ClickTimer  += deltaTime;
                        ClickTimer2 += deltaTime;
                        pieMenu.Update(zgameTime);
                        PieMenuTimer += deltaTime;
                        NotificationManager.Update(deltaTime);
                        AutoSaveTimer -= deltaTime;
                    
                        if (AutoSaveTimer <= 0.0f)
                        {
                            AutoSaveTimer = GlobalStats.AutoSaveFreq;
                            DoAutoSave();
                        }
                        if (IsActive)
                        {
                            if (GameSpeed < 1.0f) // default to 0.5x,
                            {
                                if (TurnFlip) ProcessTurnDelta(deltaTime);
                                TurnFlip = !TurnFlip;
                            }
                            else
                            {
                                // With higher GameSpeed, we take more than 1 turn
                                for (int numTurns = 0; numTurns < GameSpeed && IsActive; ++numTurns)
                                {
                                    ProcessTurnDelta(deltaTime);
                                    deltaTime = (float)zgameTime.ElapsedGameTime.TotalSeconds;
                                }
                            }
                            #if AUTOTIME
                                if (perfavg5.NumSamples > 0 && perfavg5.AvgTime * GameSpeed < 0.05f)
                                    ++GameSpeed;
                                else if (--GameSpeed < 1.0f) GameSpeed = 1.0f;
                            #endif
                        }
                    }
                    failedLoops = 0; // no exceptions this turn
                }
                catch (ThreadAbortException)
                {
                    return; // Game over, Make sure to Quit the loop!
                }
                catch (Exception ex)
                {
                    if (++failedLoops > 1)
                        throw ex; // the loop is having a cyclic crash, no way to recover
                    Log.Error(ex, "ProcessTurns crashed");
                }
                finally
                {
                    // Notify Draw() that taketurns has finished and another frame can be drawn now
                    ProcessTurnsCompletedEvt.Set();
                }
            }
        }

        public void DoAutoSave()
        {
            SavedGame savedGame = new SavedGame(this, "Autosave " + Auto);
            if (++Auto > 3) Auto = 1;
        }

        public void UpdateClickableItems()
        {
            lock (GlobalStats.ClickableItemLocker)
                this.ItemsToBuild.Clear();
            for (int index = 0; index < EmpireManager.Player.GetGSAI().Goals.Count; ++index)
            {
                Goal goal = player.GetGSAI().Goals[index];
                if (goal.GoalName == "BuildConstructionShip")
                {
                    float radius = 100f;
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(goal.BuildPosition, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(goal.BuildPosition.PointOnCircle(90f, radius), 0.0f), this.projection, this.view, Matrix.Identity);
                    float num = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2) + 10f;
                    ClickableItemUnderConstruction underConstruction = new ClickableItemUnderConstruction
                    {
                        Radius         = num,
                        BuildPos       = goal.BuildPosition,
                        ScreenPos      = vector2,
                        UID            = goal.ToBuildUID,
                        AssociatedGoal = goal
                    };
                    lock (GlobalStats.ClickableItemLocker)
                        ItemsToBuild.Add(underConstruction);
                }
            }
        }

        private void PathGridtranslateBordernode(Empire empire, byte weight, byte[,] grid)
        {
            //this.reducer = (int)(Empire.ProjectorRadius *.5f  );
            int granularity = (int) (this.Size.X / this.reducer);
            foreach (var node in empire.BorderNodes)
            {
                SolarSystem ss = node.KeyedObject as SolarSystem;
                Planet p = node.KeyedObject as Planet;
                if (this.FTLModifier < 1 && ss != null)
                    weight += 20;
                if ((this.EnemyFTLModifier < 1 || !this.FTLInNuetralSystems) && ss != null && weight > 1)
                    weight += 20;
                if (p != null && weight > 1)
                    weight += 20;
                float xround = node.Position.X > 0 ? .5f : -.5f;
                float yround = node.Position.Y > 0 ? .5f : -.5f;
                int ocx = (int)((node.Position.X / this.reducer)+ xround);
                int ocy = (int)((node.Position.Y / this.reducer)+ yround);                
                int cx = ocx + granularity;
                int cy = ocy + granularity;
                Vector2 upscale = new Vector2((float)(ocx * this.reducer),
                                    (float)(ocy * this.reducer));
                if (Vector2.Distance(upscale, node.Position) < node.Radius )
                    grid[cx, cy] = weight;
                if (weight > 1 || weight ==0 || node.Radius > Empire.ProjectorRadius)
                {
                    float test = node.Radius > Empire.ProjectorRadius ? 1 : 2;
                    int rad = (int) (Math.Ceiling((double) (node.Radius/((float) reducer)*test)));
                    //rad--;

                    int negx = cx - rad;
                    if (negx < 0)
                        negx = 0;
                    int posx = cx + rad;
                    if (posx > granularity*2)
                        posx = granularity*2;
                    int negy = cy - rad;
                    if (negy < 0)
                        negy = 0;
                    int posy = cy + rad;
                    if (posy > granularity*2)
                        posy = granularity*2;
                    for (int x = negx; x < posx; x++)
                        for (int y = negy; y < posy; y++)
                        {
                            //if (grid[x, y] >= 80 || grid[x, y] <= weight)
                            {
                                upscale = new Vector2((float) ((x - granularity)*reducer),
                                    (float) ((y - granularity)*reducer));
                                if (Vector2.Distance(upscale, node.Position) <= node.Radius *test)
                                    grid[x, y] = weight;
                            }

                        }
                }
            }
        }

        private void ProjectPieMenu(Vector2 position, float z)
        {
            var proj = ScreenManager.GraphicsDevice.Viewport.Project(position.ToVec3(z), projection, view, Matrix.Identity);
            pieMenu.Position    = proj.ToVec2();
            pieMenu.Radius      = 75f;
            pieMenu.ScaleFactor = 1f;
        }

        protected void ProcessTurnDelta(float elapsedTime)
        {
            perfavg5.Start(); // total dowork lag

            PreEmpirePerf.Start();
            zTime = elapsedTime;
            #region PreEmpire

            if (!IsActive)
            {
                ShowingSysTooltip = false;
                ShowingPlanetToolTip = false;
            }
            RecomputeFleetButtons(false);
            if (SelectedShip != null)
            {
                ProjectPieMenu(SelectedShip.Position, 0.0f);
            }
            else if (SelectedPlanet != null)
            {
                ProjectPieMenu(SelectedPlanet.Position, 2500f);
            }
            if (GlobalStats.RemnantArmageddon)
            {
                if (!Paused) ArmageddonTimer -= elapsedTime;
                if (ArmageddonTimer < 0.0)
                {
                    ArmageddonTimer = 300f;
                    ++ArmageddonCounter;
                    if (ArmageddonCounter > 5)
                        ArmageddonCounter = 5;
                    for (int i = 0; i < ArmageddonCounter; ++i)
                        ResourceManager.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants, player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f), RandomMath.RandomBetween(-500000f, 500000f))).GetAI().DefaultAIState = AIState.Exterminate;
                }
            }


            while (!ShipsToRemove.IsEmpty)
            {
                ShipsToRemove.TryTake(out Ship remove);
                remove.TotallyRemove();
            }
            ShipSpatialManager.CollidableObjects.ApplyPendingRemovals();
            DeepSpaceManager.CollidableObjects.ApplyPendingRemovals();
            MasterShipList.ApplyPendingRemovals();
            
            if (!Paused)
            {
                bool rebuild = false;
                Parallel.ForEach(EmpireManager.Empires, empire =>
                {
                    foreach (Ship s in empire.ShipsToAdd)
                    {
                        empire.AddShip(s);
                        if (!empire.isPlayer)
                            empire.ForcePoolAdd(s);
                    }

                    empire.ShipsToAdd.Clear();
                    empire.updateContactsTimer = empire.updateContactsTimer - 0.01666667f;//elapsedTime;
                    if (empire.updateContactsTimer <= 0f && !empire.data.Defeated)
                    {
                        int check = empire.BorderNodes.Count;                            
                        empire.ResetBorders();
                          
                        if (empire.BorderNodes.Count != check )
                        {
                            rebuild = true;
                            empire.PathCache.Clear();
                            // empire.lockPatchCache.ExitWriteLock();
                           
                        }
                        // empire.KnownShips.thisLock.EnterWriteLock();
                        {
                            empire.KnownShips.Clear();
                        }
                        //empire.KnownShips.thisLock.ExitWriteLock();
                        //this.UnownedShipsInOurBorders.Clear();
                        foreach (Ship ship in MasterShipList)
                        {
                            //added by gremlin reset border stats.
                            ship.getBorderCheck.Remove(empire);                                
                        }
                        empire.UpdateKnownShips();
                        empire.updateContactsTimer = elapsedTime + RandomMath.RandomBetween(2f, 3.5f);
                    }
                    
                });
                if (rebuild)
                {
                    this.reducer = (int) (Empire.ProjectorRadius*.75f);
                    int granularity = (int)(Size.X / reducer);
                    int elegran = granularity*2;
                    int elements = elegran < 128 ? 128 : elegran < 256 ? 256 : elegran < 512 ? 512 : 1024;
                   // this.reducer =(int)this.Size.X/elements;
                    byte[,] grid = new byte[elements, elements];
                        //  [granularity*2, granularity*2];     //[1024, 1024];// 
                    for (int x = 0; x < grid.GetLength(0); x++)
                        for (int y = 0; y < grid.GetLength(1); y++)
                        {
                            if (x > elegran || y > elegran)
                                grid[x, y] = 0;
                            else
                                grid[x, y] = 80;
                        }
                    foreach (Planet p in PlanetsDict.Values)
                    {
                        int x = granularity;
                        int y = granularity;
                        float xround = p.Position.X > 0 ? .5f : -.5f;
                        float yround = p.Position.Y > 0 ? .5f : -.5f;
                        x += (int) (p.Position.X / reducer + xround);
                        y += (int) (p.Position.Y / reducer + yround);
                        if (y < 0) y = 0;
                        if (x < 0) x = 0;
                        grid[x, y] = 200;
                    }
                    Parallel.ForEach(EmpireManager.Empires, empire =>
                    {
                        byte[,] grid1 = (byte[,]) grid.Clone();
                        PathGridtranslateBordernode(empire, 1, grid1);

                        foreach (KeyValuePair<Empire, Relationship> rels in empire.AllRelations)
                        {
                            if (!rels.Value.Known)
                                continue;
                            if (rels.Value.Treaty_Alliance)
                            {
                                PathGridtranslateBordernode(rels.Key, 1, grid1);
                            }
                            if (rels.Value.AtWar)
                                PathGridtranslateBordernode(rels.Key, 80, grid1);
                            else if (!rels.Value.Treaty_OpenBorders)
                                PathGridtranslateBordernode(rels.Key, 0, grid1);
                        }

                        empire.grid = grid1;
                        empire.granularity = granularity;
                    #if false
                        if (this.Debug && empire.isPlayer && this.debugwin != null && false)
                        {
                            using (var fs = new FileStream("map.astar", FileMode.Create, FileAccess.Write))
                            {
                                fs.WriteByte((byte)(0 >> 8));
                                fs.WriteByte((byte) (0 & 0x000000FF));
                                fs.WriteByte((byte) (0 >> 8));
                                fs.WriteByte((byte) (0 & 0x000000FF));
                                fs.WriteByte((byte) (256 >> 8));
                                fs.WriteByte((byte) (256 & 0x000000FF));
                                fs.WriteByte((byte) (256 >> 8));
                                fs.WriteByte((byte) (256 & 0x000000FF));
                                fs.WriteByte((byte) (true ? 1 : 0));
                                fs.WriteByte((byte) (true ? 1 : 0));
                                fs.WriteByte((byte) (false ? 1 : 0));
                                fs.WriteByte((byte) (true ? 1 : 0));
                                fs.WriteByte((byte) 2);
                                fs.WriteByte((byte) 2);
                                fs.WriteByte((byte) (false ? 1 : 0));
                                fs.WriteByte((byte) (16) >> 24);
                                fs.WriteByte((byte) (16 >> 16));
                                fs.WriteByte((byte) (16 >> 8));
                                fs.WriteByte((byte) (16 & 0x000000FF));
                                fs.WriteByte((byte) 10);
                                fs.WriteByte((byte) 10);

                                for (int y = 0; y < 1000; y++)
                                for (int x = 0; x < 1000; x++)
                                {
                                    if (y < elegran && x < elegran)
                                        fs.WriteByte(grid1[x, y]);
                                    else
                                        fs.WriteByte(0);
                                }
                            }
                        }
                    #endif
                    });
                }

                #endregion

                PreEmpirePerf.Stop();
                if (!IsActive)
                    return;
                #region Empire

                EmpireUpdatePerf.Start();
                for (var index = 0; index < EmpireManager.Empires.Count; index++)
                {
                    Empire empire = EmpireManager.Empires[index];
                    empire.Update(elapsedTime);
                }
                MasterShipList.ApplyPendingRemovals();

                lock (GlobalStats.AddShipLocker) //needed to fix Issue #629
                {
                    foreach (Ship ship in ShipsToAdd)
                    {
                        MasterShipList.Add(ship);
                        ShipSpatialManager.CollidableObjects.Add(ship);
                    }
                    ShipsToAdd.Clear();
                }
                shiptimer -= elapsedTime; // 0.01666667f;//
                EmpireUpdatePerf.Stop();
            }

            #endregion

       
            perfavg4.Start();
            #region Mid


            Parallel.Invoke(() =>
                {
                    if (elapsedTime > 0.0 && shiptimer <= 0.0)
                    {
                        foreach (SolarSystem solarSystem in SolarSystemList)
                            solarSystem.ShipList.Clear();
                        this.shiptimer = 1f;
                        //foreach (Ship ship in (Array<Ship>)this.MasterShipList)
                        var source = Enumerable.Range(0, this.MasterShipList.Count).ToArray();
                        var rangePartitioner = Partitioner.Create(0, source.Length);

                        Parallel.ForEach(rangePartitioner, (range, loopState) =>
                                       { //Parallel.ForEach(this.MasterShipList, ship =>
                                           for (int i = range.Item1; i < range.Item2; i++)
                                           {                                               
                                               Ship ship = this.MasterShipList[i];
                                               ship.isInDeepSpace = false;
                                               ship.SetSystem((SolarSystem)null);
                                               foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                                               {
                                                   if (Vector2.Distance(ship.Position, s.Position) < 100000.0)
                                                   {
                                                                                                       
                                                       s.ExploredDict[ship.loyalty] = true;
                                                       ship.SetSystem(s);
                                                       s.ShipList.Add(ship);
                                                       if (!s.spatialManager.CollidableObjects.Contains((GameplayObject)ship))
                                                           s.spatialManager.CollidableObjects.Add((GameplayObject)ship);
                                                       break;       //No need to keep looping through all other systems if one is found -Gretman
                                                   }
                                                   
                                               }
                                               if (ship.System== null)
                                               {
                                                 
                                                   ship.isInDeepSpace = true;
                                                   if (!DeepSpaceManager.CollidableObjects.Contains((GameplayObject)ship))
                                                       DeepSpaceManager.CollidableObjects.Add((GameplayObject)ship);
                                               }
                                     



                                           }//);
                                       });
                    }
                },
            () =>
            {
                Parallel.ForEach(EmpireManager.Empires, empire =>
                {
                    foreach (var kv in empire.GetFleetsDict())
                    {
                        var fleet = kv.Value;
                        if (fleet.Ships.Count <= 0)
                            continue;
                        using (fleet.Ships.AcquireReadLock())
                        {
                            fleet.Setavgtodestination();
                            fleet.SetSpeed();
                            fleet.StoredFleetPosition = fleet.findAveragePositionset();
                        }
                    }
                });
            });



            GlobalStats.BeamTests = 0;
            GlobalStats.Comparisons = 0;
            ++GlobalStats.ComparisonCounter;
            GlobalStats.ModuleUpdates = 0;
            GlobalStats.ModulesMoved = 0;
            #endregion
            perfavg4.Stop();


            Perfavg2.Start();
            #region Ships
            //this.DeepSpaceGateKeeper.Set();

//#if !ALTERTHREAD
//            this.SystemGateKeeper[0].Set();
//            this.SystemGateKeeper[1].Set();
//            this.SystemGateKeeper[2].Set();
//            this.SystemGateKeeper[3].Set();  
//#endif




#if !PLAYERONLY
  
            //Task.Run(() => 
            //if(Task.CurrentId == null)
            //Task.Run(() =>
            //{
            //
            //    Parallel.ForEach(this.SolarSystemDict, TheSystem =>
            //    {                                                               //Lets try simplifing this a lot, and go with just one clean Parallel.ForEach  -Gretman
            //        SystemUpdaterTaskBased(TheSystem.Value);
            //    });
            //});
            //Task.WaitAll();    //This commented out area was the original stuff here, which I replaced with the simgle ForEach above -Gretman
            Array<SolarSystem> solarsystems = new Array<SolarSystem>( this.SolarSystemDict.Values.Where(nocombat =>  nocombat.ShipList.Where(ship=> ship.InCombatTimer ==15).Count() <5) ); //.ToList();
            Array<SolarSystem> Combatsystems = new Array<SolarSystem>( this.SolarSystemDict.Values.Where(nocombat => nocombat.ShipList.Where(ship => ship.InCombatTimer == 15).Count() >= 5)); //.ToList();
            Task DeepSpaceTask = Task.Factory.StartNew(() =>
            {
                this.DeepSpaceThread();
                foreach (SolarSystem combatsystem in Combatsystems)
                { SystemUpdaterTaskBased(combatsystem); }
            });

            #if true // use multithreaded update loop
                var source1 = Enumerable.Range(0, solarsystems.Count).ToArray();
                var normalsystems = Partitioner.Create(0, source1.Length);
                //ParallelOptions parOpts = new ParallelOptions();
                //parOpts.MaxDegreeOfParallelism = 2;               
                Parallel.ForEach(normalsystems, (range, loopState) =>
                {
                    //standard for loop through each weapon group.
                    for (int T = range.Item1; T < range.Item2; T++)
                    {
                        SystemUpdaterTaskBased(solarsystems[T]);
                    }
                });
            #else
                foreach(SolarSystem s in solarsystems)
                {
                    SystemUpdaterTaskBased(s);
                }
            #endif

            //The two above were the originals

            if (DeepSpaceTask != null)
                DeepSpaceTask.Wait();


            //if (Combatsystems.Count > 0)                                      //This was my first attempt at helping this out a little, with a second ForEach for the systems with 
            //{
            //    var source2 = Enumerable.Range(0, Combatsystems.Count).ToArray();
            //    var SystemsWithCombat = Partitioner.Create(0, source2.Length);

            //    Parallel.ForEach(SystemsWithCombat, (AnotherRange, loopState) =>       //More threading! Yay!  -Gretman
            //    {
            //        for (int NotT = AnotherRange.Item1; NotT < AnotherRange.Item2; NotT++)
            //        {
            //            SystemUpdaterTaskBased(Combatsystems[NotT]);
            //        }
            //    });
            //}



#endif
#if PLAYERONLY
            Task DeepSpaceTask = Task.Factory.StartNew(this.DeepSpaceThread);
            foreach (SolarSystem solarsystem in this.SolarSystemDict.Values)
            {
                SystemUpdaterTaskBased(solarsystem);
            }
            if (DeepSpaceTask != null)
                DeepSpaceTask.Wait();
#endif



//#if !ALTERTHREAD
//            this.SystemResetEvents[0].WaitOne();
//            this.SystemResetEvents[1].WaitOne();
//            this.SystemResetEvents[2].WaitOne();
//            this.SystemResetEvents[3].WaitOne();



//            this.SystemResetEvents[0].Reset();
//            this.SystemResetEvents[1].Reset();
//            this.SystemResetEvents[2].Reset();
//            this.SystemResetEvents[3].Reset();  
//#endif



            //this.DeepSpaceDone.Reset();
            //foreach(Ship ship in this.MasterShipList)
            //{
            //    if (ship.GetAI().fireTask != null && !ship.GetAI().fireTask.IsCompleted)
            //    {
            //        ship.GetAI().fireTask.Start();
            //    }
            //}
#endregion

            Perfavg2.Stop();

            #region end

            //Log.Info(this.zgameTime.TotalGameTime.Seconds - elapsedTime);
            Parallel.Invoke(() =>
            {
                if (elapsedTime > 0)
                {
                    this.SpatManUpdate2(elapsedTime);
                    UniverseScreen.ShipSpatialManager.UpdateBucketsOnly(elapsedTime);
                }
            },
            () =>
            {
                //lock (GlobalStats.ClickableItemLocker)
                this.UpdateClickableItems();
                if (this.LookingAtPlanet)
                    this.workersPanel.Update(elapsedTime);
                bool flag1 = false;
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int local_11 = 0; local_11 < this.ClickPlanetList.Count; ++local_11)
                    {
                        try
                        {
                            UniverseScreen.ClickablePlanets local_12 = this.ClickPlanetList[local_11];
                            if (Vector2.Distance(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y), local_12.ScreenPos) <= local_12.Radius)
                            {
                                flag1 = true;
                                this.TooltipTimer -= 0.01666667f;
                                this.tippedPlanet = local_12;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                if (this.TooltipTimer <= 0f && !this.LookingAtPlanet)
                    this.TooltipTimer = 0.5f;
                if (!flag1)
                {
                    this.ShowingPlanetToolTip = false;
                    this.TooltipTimer = 0.5f;
                }
           
                bool flag2 = false;
                if (this.viewState > UniverseScreen.UnivScreenState.SectorView)
                {
                    lock (GlobalStats.ClickableSystemsLock)
                    {
                        for (int local_15 = 0; local_15 < this.ClickableSystems.Count; ++local_15)
                        {
                            UniverseScreen.ClickableSystem local_16 = this.ClickableSystems[local_15];
                            if (Vector2.Distance(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y), local_16.ScreenPos) <= local_16.Radius)
                            {
                                this.sTooltipTimer -= 0.01666667f;
                                this.tippedSystem = local_16;
                                flag2 = true;
                            }
                        }
                    }
                    if (this.sTooltipTimer <= 0f)
                        this.sTooltipTimer = 0.5f;
                }
                if (!flag2)
                    this.ShowingSysTooltip = false;
                this.Zrotate += 0.03f * elapsedTime;


                JunkList.ApplyPendingRemovals();
            //},
            //() =>
            //{
                if (elapsedTime > 0)
                {
                    lock (GlobalStats.ExplosionLocker)
                    {
                        ExplosionManager.Update(elapsedTime);
                        ExplosionManager.ExplosionList.ApplyPendingRemovals();
                    }
                    MuzzleFlashManager.Update(elapsedTime);
                }
                lock (GlobalStats.ExplosionLocker)
                    MuzzleFlashManager.FlashList.ApplyPendingRemovals();
                foreach (Anomaly anomaly in (Array<Anomaly>)this.anomalyManager.AnomaliesList)
                    anomaly.Update(elapsedTime);
                if (elapsedTime > 0)
                {
                    using (BombList.AcquireReadLock())
                    {
                        for (int local_19 = 0; local_19 < this.BombList.Count; ++local_19)
                        {
                            Bomb local_20 = this.BombList[local_19];
                            if (local_20 != null)
                                local_20.Update(elapsedTime);
                        }
                    }
                    BombList.ApplyPendingRemovals();
                }
                this.anomalyManager.AnomaliesList.ApplyPendingRemovals();
            }
            );
            if (elapsedTime > 0)
            {
                ShieldManager.Update();
                FTLManager.Update(elapsedTime);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(elapsedTime);
            }
            this.SelectedShipList.ApplyPendingRemovals();
            this.MasterShipList.ApplyPendingRemovals();
            UniverseScreen.ShipSpatialManager.CollidableObjects.ApplyPendingRemovals();
            if (this.perStarDateTimer <= this.StarDate)
            {
                this.perStarDateTimer = this.StarDate + .1f;
                this.perStarDateTimer = (float)Math.Round((double)this.perStarDateTimer, 1);
                this.empireShipCountReserve = EmpireManager.Empires.Where(empire => empire != this.player && !empire.data.Defeated && !empire.isFaction).Sum(empire => empire.EmpireShipCountReserve);
                this.globalshipCount = this.MasterShipList.Where(ship => (ship.loyalty != null && ship.loyalty != this.player) && ship.shipData.Role != ShipData.RoleName.troop && ship.Mothership == null).Count();
            }

            #endregion
            perfavg5.Stop();
            Lag = perfavg5.AvgTime;

            //if (this.perfavg5.Average() > .1f)
            //    GlobalStats.ForceFullSim = false;
            //else GlobalStats.ForceFullSim = true;

        }

        public void SystemUpdaterTaskBased(SolarSystem system)
        {
            //Array<SolarSystem> list = (Array<SolarSystem>)data;
            // while (true)
            {
                //this.SystemGateKeeper[list[0].IndexOfResetEvent].WaitOne();
                //float elapsedTime = this.ztimeSnapShot;
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;
                float realTime = this.zTime;// this.zgameTime.ElapsedGameTime.Seconds;
                {
                    system.DangerTimer -= realTime;
                    system.DangerUpdater -= realTime;
                    if (system.DangerUpdater < 0.0)
                    {
                        system.DangerUpdater = 10f;
                        system.DangerTimer = (double)this.player.GetGSAI().ThreatMatrix.PingRadarStr(system.Position, 100000f * UniverseScreen.GameScaleStatic, this.player) <= 0.0 ? 0.0f : 120f;
                    }
                    system.combatTimer -= realTime;


                    if (system.combatTimer <= 0.0)
                        system.CombatInSystem = false;
                    bool viewing = false;
                    this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(system.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                    if (this.Frustum.Contains(new BoundingSphere(new Vector3(system.Position, 0.0f), 100000f)) != ContainmentType.Disjoint)
                        viewing = true;
                    else if (this.viewState == UniverseScreen.UnivScreenState.ShipView)
                    {
                        Rectangle rect = new Rectangle((int)system.Position.X - 100000, (int)system.Position.Y - 100000, 200000, 200000);
                        Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(500f, 500f, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector3 direction = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(500f, 500f, 1f), this.projection, this.view, Matrix.Identity) - position;
                        direction.Normalize();
                        Ray ray = new Ray(position, direction);
                        float num = -ray.Position.Z / ray.Direction.Z;
                        Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                        Vector2 pos = new Vector2(vector3.X, vector3.Y);
                        if (HelperFunctions.CheckIntersection(rect, pos))
                            viewing = true;
                    }
                    if (system.ExploredDict[this.player] && viewing)
                    {
                        system.isVisible = this.camHeight < 250000.0;
                    }
                    if (system.isVisible && this.camHeight < 150000.0)
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.So.Visibility = ObjectVisibility.Rendered;
                            asteroid.Update(elapsedTime);
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.So.Visibility = ObjectVisibility.Rendered;
                            moon.UpdatePosition(elapsedTime);
                        }
                    }
                    else
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.So.Visibility = ObjectVisibility.None;
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.So.Visibility = ObjectVisibility.None;
                        }
                    }
                    foreach (Planet planet in system.PlanetList)
                    {
                        planet.Update(elapsedTime);
                        if (planet.HasShipyard && system.isVisible)
                            planet.Station.Update(elapsedTime);
                    }                    

                    using (system.ShipList.AcquireReadLock())
                    {
                        foreach (Ship ship in (Array<Ship>)system.ShipList)
                        {
                            if (ship.System == null)
                                continue;
                            if (!ship.Active || ship.ModuleSlotList.Count == 0) // added by gremlin ghost ship killer
                            {
                                ship.Die(null, true);
                            }
                            else
                            {
                                if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                                {
                                    ship.Inhibited = true;
                                    ship.InhibitedTimer = 10f;
                                }
                                //ship.PauseUpdate = true;
                                ship.Update(elapsedTime);
                                if (ship.PlayerShip)
                                    ship.ProcessInput(elapsedTime);
                            }
                        }
                    }
                    if (!Paused && IsActive)
                        system.spatialManager.Update(elapsedTime, system);
                    system.AsteroidsList.ApplyPendingRemovals();
                    system.ShipList.ApplyPendingRemovals();
                }//);
                // this.SystemResetEvents[list[0].IndexOfResetEvent].Set();
            }
        }
        protected void SpatManUpdate2(float elapsedTime)
        {
           // lock (GlobalStats.DeepSpaceLock)
            {
                foreach (Projectile item_0 in DSProjectilesToAdd)
                    DeepSpaceManager.CollidableObjects.Add(item_0);
            }
            DSProjectilesToAdd.Clear();
            DeepSpaceManager.Update(elapsedTime, null);
        }

        private void DeepSpaceThread()
        {
            //while (true)
            {
                
                //this.DeepSpaceGateKeeper.WaitOne();
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;

                
                
                this.DeepSpaceShips.Clear();

                lock (GlobalStats.DeepSpaceLock)
                {
                    for (int i = 0; i < DeepSpaceManager.CollidableObjects.Count; i++)
                    {
                        GameplayObject item = DeepSpaceManager.CollidableObjects[i];
                        if (item is Ship)
                        {
                            Ship ship = item as Ship;
                            if (ship.Active && ship.isInDeepSpace && ship.System== null)
                            {
                                this.DeepSpaceShips.Add(ship);
                            }

                        }
                    }
                    Array<Ship> faster = new Array<Ship>();
                    Array<Ship> death = new Array<Ship>();
                    foreach (Ship deepSpaceShip in this.DeepSpaceShips)
                    //Parallel.ForEach(this.DeepSpaceShips, deepSpaceShip =>
                    {
                        if (!deepSpaceShip.shipInitialized)
                            continue;
                        if (deepSpaceShip.Active)
                        {
                            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                            {
                                deepSpaceShip.Inhibited = true;
                                deepSpaceShip.InhibitedTimer = 10f;
                            }
                            //try
                            {
                                //deepSpaceShip.PauseUpdate = true;
                                if (deepSpaceShip.InCombat || deepSpaceShip.PlayerShip)
                                    deepSpaceShip.Update(elapsedTime);
                                else
                                    faster.Add(deepSpaceShip);
                                if (!deepSpaceShip.PlayerShip)
                                {
                                    continue;
                                }
                                deepSpaceShip.ProcessInput(elapsedTime);
                            }
                            if (deepSpaceShip.ModuleSlotList.Count == 0)
                            {
                                death.Add(deepSpaceShip);
                                //deepSpaceShip.Die(null, true);
                            }
                        }
                        else
                        {
                            death.Add(deepSpaceShip);
                            deepSpaceShip.Die(null,true);
                            //this.MasterShipList.QueuePendingRemoval(deepSpaceShip);
                        }
                    }//);
                    var source1 = Enumerable.Range(0, faster.Count).ToArray();
                    Partitioner<int> rangePartitioner1 = Partitioner.Create(source1, true);
                    Parallel.ForEach(rangePartitioner1, (range, loopState) =>
                    {
                        faster[range].Update(elapsedTime);

                    });
                     source1 = Enumerable.Range(0, death.Count).ToArray();
                    rangePartitioner1 = Partitioner.Create(source1, true);
                    Parallel.ForEach(rangePartitioner1, (range, loopState) =>
                    {
                        death[range].Update(elapsedTime);

                    });

                }
                
                //this.DeepSpaceDone.Set();
            }
        }

        public virtual void UpdateAllSystems(float elapsedTime)
        {
            if (IsExiting)
                return;

            foreach (SolarSystem system in SolarSystemList)
            {
                system.DangerTimer   -= elapsedTime;
                system.DangerUpdater -= elapsedTime;
                foreach (KeyValuePair<Empire, SolarSystem.PredictionTimeout> predict in system.predictionTimeout)
                    predict.Value.update(elapsedTime);

                if (system.DangerUpdater < 0.0f)
                {
                    system.DangerUpdater = 10f;
                    system.DangerTimer   = player.GetGSAI().ThreatMatrix.PingRadarStr(system.Position, 100000f * GameScaleStatic, player) <= 0.0 ? 0.0f : 120f;
                }
                system.combatTimer -= elapsedTime;
                if (system.combatTimer <= 0.0f)
                    system.CombatInSystem = false;

                if (elapsedTime > 0.0f)
                    system.spatialManager.Update(elapsedTime, system);

                bool inFrustrum = false;
                if (Frustum.Contains(system.Position, 100000f))
                    inFrustrum = true;
                else if (viewState == UnivScreenState.ShipView)
                {
                    Rectangle rect = new Rectangle((int)system.Position.X - 100000, (int)system.Position.Y - 100000, 200000, 200000);
                    Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(500f, 500f, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector3 direction = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(500f, 500f, 1f), this.projection, this.view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                    Vector2 pos = new Vector2(vector3.X, vector3.Y);
                    if (HelperFunctions.CheckIntersection(rect, pos))
                        inFrustrum = true;
                }
                if (system.ExploredDict[this.player] && inFrustrum)
                {
                        system.isVisible = (double)this.camHeight < 250000.0; 
                }
                if (system.isVisible && this.camHeight < 150000.0)
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                    {
                        asteroid.So.Visibility = ObjectVisibility.Rendered;
                        asteroid.Update(elapsedTime);
                    }
                    foreach (Moon moon in system.MoonList)
                    {
                        moon.So.Visibility = ObjectVisibility.Rendered;
                        moon.UpdatePosition(elapsedTime);
                    }
                }
                else
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                    {
                        asteroid.So.Visibility = ObjectVisibility.None;
                    }
                    foreach (Moon moon in system.MoonList)
                    {
                        moon.So.Visibility = ObjectVisibility.None;
                    }
                }
                foreach (Planet planet in system.PlanetList)
                {
                    planet.Update(elapsedTime);
                    if (planet.HasShipyard && system.isVisible)
                        planet.Station.Update(elapsedTime);
                }
                if (system.isVisible && camHeight < 150000.0f)
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                        asteroid.Update(elapsedTime);
                }
                system.AsteroidsList.ApplyPendingRemovals();
                system.ShipList.ApplyPendingRemovals();
            }
        }

        protected virtual void AdjustCamera(float elapsedTime)
        {
            if (this.ShipToView == null)
                this.ViewingShip = false;
            this.AdjustCamTimer -= elapsedTime;
            if (this.ViewingShip && !this.snappingToShip)
            {
                this.camPos.X = this.ShipToView.Center.X;
                this.camPos.Y = this.ShipToView.Center.Y;
                this.camHeight = (float)(int)MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, 0.2f);
                if ((double)this.camHeight < 550.0)
                    this.camHeight = 550f;
            }
            if (this.AdjustCamTimer > 0.0)
            {
                if (this.ShipToView == null)
                    this.snappingToShip = false;
                if (this.snappingToShip)
                {
                    this.transitionDestination.X = this.ShipToView.Center.X;
                    this.transitionDestination.Y = this.ShipToView.Center.Y;
                    this.transitionElapsedTime += elapsedTime;
                    float amount = (float)Math.Pow((double)this.transitionElapsedTime / (double)this.transDuration, 0.699999988079071);
                    this.camTransitionPosition.X = MathHelper.SmoothStep(this.camPos.X, this.transitionDestination.X, amount);
                    float num1 = MathHelper.SmoothStep(this.camPos.Y, this.transitionDestination.Y, amount);
                    float num2 = MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, amount);
                    this.camTransitionPosition.Y = num1;
                    this.camHeight = (float)(int)num2;
                    this.camPos = this.camTransitionPosition;
                    if ((double)this.AdjustCamTimer - (double)elapsedTime <= 0.0)
                    {
                        this.ViewingShip = true;
                        this.transitionElapsedTime = 0.0f;
                        this.AdjustCamTimer = -1f;
                        this.snappingToShip = false;
                    }
                }
                else
                {
                    this.transitionElapsedTime += elapsedTime;
                    float amount = (float)Math.Pow((double)this.transitionElapsedTime / (double)this.transDuration, 0.699999988079071);
                    this.camTransitionPosition.X = MathHelper.SmoothStep(this.camPos.X, this.transitionDestination.X, amount);
                    float num1 = MathHelper.SmoothStep(this.camPos.Y, this.transitionDestination.Y, amount);
                    float num2 = MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, amount);
                    this.camTransitionPosition.Y = num1;
                    this.camHeight = num2;
                    this.camPos = this.camTransitionPosition;
                    if ((double)this.transitionElapsedTime > (double)this.transDuration || (double)Vector2.Distance(new Vector2(this.camPos.X, this.camPos.Y), new Vector2(this.transitionDestination.X, this.transitionDestination.Y)) < 50.0 && (double)Math.Abs(this.camHeight - this.transitionDestination.Z) < 50.0)
                    {
                        this.transitionElapsedTime = 0.0f;
                        this.AdjustCamTimer = -1f;
                    }
                }
                if ((double)this.camHeight < 550.0)
                    this.camHeight = 550f;
            }
            else if (this.LookingAtPlanet && this.SelectedPlanet != null)
            {
                this.camTransitionPosition.X = MathHelper.SmoothStep(this.camPos.X, this.SelectedPlanet.Position.X, 0.2f);
                this.camTransitionPosition.Y = MathHelper.SmoothStep(this.camPos.Y, this.SelectedPlanet.Position.Y + 400f, 0.2f);
                this.camPos = this.camTransitionPosition;
            }
            else if (!this.ViewingShip)
            {
                this.camTransitionPosition.X = MathHelper.SmoothStep(this.camPos.X, this.transitionDestination.X, 0.2f);
                float num1 = MathHelper.SmoothStep(this.camPos.Y, this.transitionDestination.Y, 0.2f);
                float num2 = MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, 0.2f);
                this.camTransitionPosition.Y = num1;
                this.camHeight = num2;
                if ((double)this.camHeight < 550.0)
                    this.camHeight = 550f;
                this.camPos = this.camTransitionPosition;
            }
            if (this.camPos.X > this.Size.X)
                this.camPos.X = this.Size.X;
            if (this.camPos.X < -this.Size.X)   //So the camera can pan out into the new negative map coordinates -Gretman
                this.camPos.X = -this.Size.X;
            if (this.camPos.Y > (double)this.Size.Y)
                this.camPos.Y = this.Size.Y;
            if ((double)this.camPos.Y < -this.Size.Y)
                this.camPos.Y = -this.Size.Y;
            if ((double)this.camHeight > (double)this.MaxCamHeight * (double)this.GameScale)
                this.camHeight = this.MaxCamHeight * this.GameScale;
            else if ((double)this.camHeight < 1337.0)
                this.camHeight = 1337f;
            if ((double)this.camHeight > 30000.0)
            {
                this.viewState = UniverseScreen.UnivScreenState.SystemView;
                if ((double)this.camHeight <= 250000.0)
                    return;
                this.viewState = UniverseScreen.UnivScreenState.SectorView;
                if ((double)this.camHeight <= 1775000.0)
                    return;
                this.viewState = UniverseScreen.UnivScreenState.GalaxyView;
            }
            else
                this.viewState = UniverseScreen.UnivScreenState.ShipView;
        }

        public void PlayNegativeSound()
        {
            AudioManager.GetCue("UI_Misc20").Play();
        }

        protected bool HandleGUIClicks(InputState input)
        {
            bool flag = false;
            if (this.dsbw != null && this.showingDSBW && this.dsbw.HandleInput(input))
                flag = true;
            if (this.aw.isOpen && this.aw.HandleInput(input))
                return true;
            if (HelperFunctions.CheckIntersection(this.MinimapDisplayRect, input.CursorPosition) && !this.SelectingWithBox)
            {
                this.HandleScrolls(input);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    Vector2 vector2 = input.CursorPosition - new Vector2((float)this.MinimapDisplayRect.X, (float)this.MinimapDisplayRect.Y);
                    float num = (float)this.MinimapDisplayRect.Width / (this.Size.X * 2);
                    this.transitionDestination.X = -this.Size.X + (vector2.X / num);        //Fixed clicking on the mini-map on location with negative coordinates -Gretman
                    this.transitionDestination.Y = -this.Size.X + (vector2.Y / num);
                    this.snappingToShip = false;
                    this.ViewingShip = false;
                }
                flag = true;
            }
            if (this.SelectedShip != null && this.ShipInfoUIElement.HandleInput(input) && !this.LookingAtPlanet)
                flag = true;
            if (this.SelectedPlanet != null && this.pInfoUI.HandleInput(input) && !this.LookingAtPlanet)
                flag = true;
            if (this.SelectedShipList != null && this.shipListInfoUI.HandleInput(input) && !this.LookingAtPlanet)
                flag = true;
            if (this.SelectedSystem != null)
            {
                if (this.sInfoUI.HandleInput(input) && !this.LookingAtPlanet)
                    flag = true;
            }
            else
                this.sInfoUI.SelectionTimer = 0.0f;
            if (this.minimap.HandleInput(input, this))
                flag = true;
            if (this.NotificationManager.HandleInput(input))
                flag = true;
            if (HelperFunctions.CheckIntersection(this.ShipsInCombat.Rect, input.CursorPosition))  //fbedard
                flag = true;
            if (HelperFunctions.CheckIntersection(this.PlanetsInCombat.Rect, input.CursorPosition))  //fbedard
                flag = true;

            return flag;
        }

        private void DefineAO(InputState input)
        {
            this.HandleScrolls(input);
            if (this.SelectedShip == null)
            {
                this.DefiningAO = false;
            }
            else
            {
                if (input.Escaped)      //Easier out from defining an AO. Used to have to left and Right click at the same time.    -Gretman
                {
                    this.DefiningAO = false;
                    return;
                }
                Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector3 direction = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.projection, this.view, Matrix.Identity) - position;
                direction.Normalize();
                Ray ray = new Ray(position, direction);
                float num = -ray.Position.Z / ray.Direction.Z;
                Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                    this.AORect = new Rectangle((int)vector3.X, (int)vector3.Y, 0, 0);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                {
                    this.AORect = new Rectangle(this.AORect.X, this.AORect.Y, (int)vector3.X - this.AORect.X, (int)vector3.Y - this.AORect.Y);
                }
                if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
                {
                    if (this.AORect.X > vector3.X)
                        this.AORect.X = (int)vector3.X;
                    if (this.AORect.Y > vector3.Y)
                        this.AORect.Y = (int)vector3.Y;
                    this.AORect.Width = Math.Abs(this.AORect.Width);
                    this.AORect.Height = Math.Abs(this.AORect.Height);
                    if (this.AORect.Width > 100 && this.AORect.Height > 100)
                    {
                        AudioManager.PlayCue("echo_affirm");
                        this.SelectedShip.AreaOfOperation.Add(this.AORect);
                    }
                }
                for (int index = 0; index < this.SelectedShip.AreaOfOperation.Count; ++index)
                {
                    if (HelperFunctions.CheckIntersection(this.SelectedShip.AreaOfOperation[index], new Vector2(vector3.X, vector3.Y)) && input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
                        this.SelectedShip.AreaOfOperation.Remove(this.SelectedShip.AreaOfOperation[index]);
                }
            }
        }

        public override void HandleInput(InputState input)
        {
            if (ScreenManager.input.CurrentKeyboardState.IsKeyDown(Keys.Space) && ScreenManager.input.LastKeyboardState.IsKeyUp(Keys.Space) && !GlobalStats.TakingInput)
                Paused = !Paused;

            if (!LookingAtPlanet)
            {
                ScreenManager.exitScreenTimer -= .0016f;
                if (ScreenManager.exitScreenTimer > 0f)
                    return;
            }
            else ScreenManager.exitScreenTimer = .025f;

            for (int index = 0; index < SelectedShipList.Count; ++index)
            {
                Ship ship = SelectedShipList[index];
                if (!ship.Active)
                    SelectedShipList.QueuePendingRemoval(ship);
            }
            //CG: previous target code. 
            if (previousSelection != null 
                && input.CurrentMouseState.XButton1 == ButtonState.Pressed 
                && input.LastMouseState.XButton1 == ButtonState.Released)
            {
                if (previousSelection.Active)
                {
                    Ship tempship = this.previousSelection;
                    if (this.SelectedShip != null && this.SelectedShip != this.previousSelection)
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = tempship;
                    this.ShipInfoUIElement.SetShip(this.SelectedShip);
                    this.SelectedFleet = (Fleet)null;
                    this.SelectedShipList.Clear();
                    this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                    this.SelectedSystem = (SolarSystem)null;
                    this.SelectedPlanet = (Planet)null;
                    this.SelectedShipList.Add(this.SelectedShip);
                    //this.snappingToShip = false;
                    this.ViewingShip = false;
                    return;
                }
                else
                    this.previousSelection = null;  //fbedard: remove inactive ship
            }
            //fbedard: Set camera chase on ship
            if (input.CurrentMouseState.MiddleButton == ButtonState.Pressed)
            {
                this.ViewToShip(null);
            }
            this.input = input;
            this.ShowTacticalCloseup = input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt);
            // something nicer...
            //if (input.CurrentKeyboardState.IsKeyDown(Keys.P) && input.LastKeyboardState.IsKeyUp(Keys.P) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            if (input.CurrentKeyboardState.IsKeyDown(Keys.F5) && input.LastKeyboardState.IsKeyUp(Keys.F5))
            {
                UseRealLights = !UseRealLights; // toggle real lights
                SetLighting(UseRealLights);
            } 
            if (input.CurrentKeyboardState.IsKeyDown(Keys.F6) && input.LastKeyboardState.IsKeyUp(Keys.F6) && !ExceptionTracker.Visible)
            {
                bool switchedmode = false;
            #if RELEASE //only switch screens in release
                
                if (Game1.Instance.graphics.IsFullScreen)
                {
                    switchedmode = true;
                    Game1.Instance.graphics.ToggleFullScreen();
                }
            #endif
                Exception ex = new Exception("Manual Report");
                ExceptionTracker.TrackException(ex);

                // if(ExceptionViewer.ActiveForm == null)
                {
                    bool wasPaused = Paused;
                    Paused = true;
                    ExceptionTracker.DisplayException(ex);
                    Paused = wasPaused;
                }
                if (switchedmode)
                {
                    switchedmode = false;
                    Game1.Instance.Graphics.ToggleFullScreen();
                }
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.F7) && input.LastKeyboardState.IsKeyUp(Keys.F7) && !ExceptionTracker.Visible)
            {
                bool switchedmode = false;
#if RELEASE //only switch screens in release
                
                if (Game1.Instance.graphics.IsFullScreen)
                {
                    switchedmode = true;
                    Game1.Instance.graphics.ToggleFullScreen();
                }
#endif
                Exception ex = new Exception("Kudos");
                
                ExceptionTracker.TrackException(ex);
                ExceptionTracker.Kudos = true;
                // if(ExceptionViewer.ActiveForm == null)
                {
                    bool wasPaused = Paused;
                    Paused = true;
                    ExceptionTracker.DisplayException(ex);
                    Paused = wasPaused;
                }
                if (switchedmode)
                {
                    switchedmode = false;
                    Game1.Instance.Graphics.ToggleFullScreen();
                }
            }
            if ((input.CurrentKeyboardState.IsKeyDown(Keys.OemTilde) && input.LastKeyboardState.IsKeyUp(Keys.OemTilde) && (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift)))                
                || (input.CurrentKeyboardState.IsKeyDown(Keys.Tab) && input.LastKeyboardState.IsKeyUp(Keys.Tab) && (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift)))
                
            )
            {
                this.Debug = !this.Debug;
                UniverseScreen.debug = !this.Debug;
                foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                    solarSystem.ExploredDict[this.player] = true;
                GlobalStats.LimitSpeed = this.Debug;
            }
            if (this.Debug && input.CurrentKeyboardState.IsKeyDown(Keys.G) && input.LastKeyboardState.IsKeyUp(Keys.G))
            {
                this.Memory = (float)GC.GetTotalMemory(true);
                this.Memory /= 1000f;
            }
            this.HandleEdgeDetection(input);
            if (input.CurrentKeyboardState.IsKeyDown(Keys.OemPlus) && input.LastKeyboardState.IsKeyUp(Keys.OemPlus))
            {
                if (this.GameSpeed < 1.0)
                    this.GameSpeed = 1f;
                else
                    ++this.GameSpeed;
                if (this.GameSpeed > 4.0 && GlobalStats.LimitSpeed)
                    this.GameSpeed = 4f;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.OemMinus) && input.LastKeyboardState.IsKeyUp(Keys.OemMinus))
            {
                if (this.GameSpeed <= 1.0)
                    this.GameSpeed = 0.5f;
                else
                    --this.GameSpeed;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Add) && input.LastKeyboardState.IsKeyUp(Keys.Add))
            {
                if (this.GameSpeed < 1.0)
                    this.GameSpeed = 1f;
                else
                    ++this.GameSpeed;
                if (this.GameSpeed > 4.0 && GlobalStats.LimitSpeed)
                    this.GameSpeed = 4f;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Subtract) && input.LastKeyboardState.IsKeyUp(Keys.Subtract))
            {
                if (this.GameSpeed <= 1.0)
                    this.GameSpeed = 0.5f;
                else
                    --this.GameSpeed;
            }

            //fbedard: Click button to Cycle through ships in Combat
            if (!HelperFunctions.CheckIntersection(this.ShipsInCombat.Rect, input.CursorPosition))
            {
                this.ShipsInCombat.State = UIButton.PressState.Default;
            }
            else
            {
                this.ShipsInCombat.State = UIButton.PressState.Hover;
                ToolTip.CreateTooltip("Cycle through ships not in fleet that are in combat", this.ScreenManager);
                if (input.InGameSelect)
                {
                    if (this.player.empireShipCombat > 0)
                    {
                        AudioManager.PlayCue("echo_affirm");
                        int nbrship = 0;
                        if (lastshipcombat >= this.player.empireShipCombat)
                            lastshipcombat = 0;
                        foreach (Ship ship in EmpireManager.Player.GetShips())
                        {
                            if (ship.fleet != null || !ship.InCombat || ship.Mothership != null || !ship.Active)
                                continue;
                            else
                            {
                                if (nbrship == lastshipcombat)
                                {
                                    if (this.SelectedShip != null && this.SelectedShip != this.previousSelection && this.SelectedShip != ship)
                                        this.previousSelection = this.SelectedShip;
                                    this.SelectedShip = ship;
                                    this.ViewToShip(null);
                                    this.SelectedShipList.Add(this.SelectedShip);
                                    lastshipcombat++;
                                    break;
                                }
                                else nbrship++;
                            }
                        }
                    }
                    else
                    {
                        AudioManager.PlayCue("blip_click");
                    }
                }
            }

            //fbedard: Click button to Cycle through Planets in Combat
            if (!HelperFunctions.CheckIntersection(this.PlanetsInCombat.Rect, input.CursorPosition))
            {
                this.PlanetsInCombat.State = UIButton.PressState.Default;
            }
            else
            {
                this.PlanetsInCombat.State = UIButton.PressState.Hover;
                ToolTip.CreateTooltip("Cycle through planets that are in combat", this.ScreenManager);
                if (input.InGameSelect)
                {
                    if (this.player.empirePlanetCombat > 0)
                    {
                        AudioManager.PlayCue("echo_affirm");
                        Planet PlanetToView = (Planet)null;
                        int nbrplanet = 0;
                        if (lastplanetcombat >= this.player.empirePlanetCombat)
                            lastplanetcombat = 0;
                        bool flagPlanet;

                        foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                        {
                            foreach (Planet p in system.PlanetList)
                            {
                                if (p.ExploredDict[Empire.Universe.PlayerEmpire] && p.RecentCombat)
                                {
                                    if (p.Owner == Empire.Universe.PlayerEmpire)
                                    {
                                        if (nbrplanet == lastplanetcombat)
                                            PlanetToView = p;
                                        nbrplanet++;
                                    }
                                    else
                                    {
                                        flagPlanet = false;
                                        foreach (Troop troop in p.TroopsHere)
                                        {
                                            if (troop.GetOwner() != null && troop.GetOwner() == Empire.Universe.PlayerEmpire)
                                            {
                                                flagPlanet = true;
                                                break;
                                            }
                                        }
                                        if (flagPlanet) 
                                        {
                                            if (nbrplanet == lastplanetcombat)
                                                PlanetToView = p;
                                            nbrplanet++;
                                        }
                                    }
                                }
                            }                       
                        }
                        if (PlanetToView != null)
                        {
                            if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                                this.previousSelection = this.SelectedShip;
                            this.SelectedShip = (Ship)null;
                            //this.ShipInfoUIElement.SetShip(this.SelectedShip);
                            this.SelectedFleet = (Fleet)null;
                            this.SelectedShipList.Clear();
                            this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                            this.SelectedSystem = (SolarSystem)null;
                            this.SelectedPlanet = PlanetToView;
                            this.pInfoUI.SetPlanet(PlanetToView);
                            lastplanetcombat++;

                            this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y, 9000f);
                            this.LookingAtPlanet = false;
                            this.transitionStartPosition = this.camPos;
                            this.AdjustCamTimer = 2f;
                            this.transitionElapsedTime = 0.0f;
                            this.transDuration = 5f;
                            this.returnToShip = false;
                            this.ViewingShip = false;
                            this.snappingToShip = false;
                            this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                            //PlanetToView.OpenCombatMenu(null);
                        }
                    }
                    else
                    {
                        AudioManager.PlayCue("blip_click");
                    }
                }
            }

            if (!this.LookingAtPlanet )
            {
                if (this.HandleGUIClicks(input))
                {
                    this.SkipRightOnce = true;
                    this.NeedARelease = true;
                    return;
                }
            }
            else
            {
                this.SelectedFleet = (Fleet)null;
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = (Ship)null;
                this.SelectedShipList.Clear();
                this.SelectedItem = null;
                this.SelectedSystem = null;
            }
            if ((input.CurrentKeyboardState.IsKeyDown(Keys.Back) || input.CurrentKeyboardState.IsKeyDown(Keys.Delete)) && (this.SelectedItem != null && this.SelectedItem.AssociatedGoal.empire == this.player))
            {
                this.player.GetGSAI().Goals.QueuePendingRemoval(SelectedItem.AssociatedGoal);
                bool flag = false;
                foreach (Ship ship in player.GetShips())
                {
                    if (ship.isConstructor && ship.GetAI().OrderQueue.Count > 0)
                    {
                        for (int index = 0; index < ship.GetAI().OrderQueue.Count; ++index)
                        {
                            if (Enumerable.ElementAt(ship.GetAI().OrderQueue, index).goal == SelectedItem.AssociatedGoal)
                            {
                                flag = true;
                                ship.GetAI().OrderScrapShip();
                                break;
                            }
                        }
                    }
                }
                if (!flag)
                {
                    foreach (Planet planet in this.player.GetPlanets())
                    {
                        foreach (QueueItem queueItem in planet.ConstructionQueue)
                        {
                            if (queueItem.Goal == this.SelectedItem.AssociatedGoal)
                            {
                                planet.ProductionHere += queueItem.productionTowards;
                                if ((double)planet.ProductionHere > (double)planet.MAX_STORAGE)
                                    planet.ProductionHere = planet.MAX_STORAGE;
                                planet.ConstructionQueue.QueuePendingRemoval(queueItem);
                            }
                        }
                        planet.ConstructionQueue.ApplyPendingRemovals();
                    }
                }
                lock (GlobalStats.ClickableItemLocker)
                {
                    for (int local_10 = 0; local_10 < this.ItemsToBuild.Count; ++local_10)
                    {
                        UniverseScreen.ClickableItemUnderConstruction local_11 = this.ItemsToBuild[local_10];
                        if (local_11.BuildPos == this.SelectedItem.BuildPos)
                        {
                            this.ItemsToBuild.QueuePendingRemoval(local_11);
                            AudioManager.PlayCue("blip_click");
                        }
                    }
                    this.ItemsToBuild.ApplyPendingRemovals();
                }
                this.player.GetGSAI().Goals.ApplyPendingRemovals();
                this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.H) && !input.LastKeyboardState.IsKeyDown(Keys.H) && this.Debug)
            {
                this.debugwin = new DebugInfoScreen(this.ScreenManager, this);
                this.showdebugwindow = !this.showdebugwindow;
            }
            
            if (this.DefiningAO)
            {
                if (this.NeedARelease)
                {
                    if (input.CurrentMouseState.LeftButton == ButtonState.Released)
                        this.NeedARelease = false;
                }
                else
                {
                    this.DefineAO(input);
                    return;
                }
            }
            this.pickedSomethingThisFrame = false;
            this.input = input;
            if (this.LookingAtPlanet)
                this.workersPanel.HandleInput(input);
            if (this.IsActive)
                this.EmpireUI.HandleInput(input);
            if (this.ShowingPlanetToolTip && (double)Vector2.Distance(new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y), this.tippedPlanet.ScreenPos) > (double)this.tippedPlanet.Radius)
            {
                this.ShowingPlanetToolTip = false;
                this.TooltipTimer = 0.5f;
            }
            if (this.ShowingSysTooltip && (double)Vector2.Distance(new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y), this.tippedSystem.ScreenPos) > (double)this.tippedSystem.Radius)
            {
                this.ShowingSysTooltip = false;
                this.sTooltipTimer = 0.5f;
            }
            if (!this.LookingAtPlanet)
            {
                Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector3 direction = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.projection, this.view, Matrix.Identity) - position;
                direction.Normalize();
                Ray ray = new Ray(position, direction);
                float num = -ray.Position.Z / ray.Direction.Z;
                Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                this.mouseWorldPos = new Vector2(vector3.X, vector3.Y);
                if (input.CurrentKeyboardState.IsKeyDown(Keys.B) && !input.LastKeyboardState.IsKeyDown(Keys.B))
                {
                    if (!this.showingDSBW)
                    {
                        this.dsbw = new DeepSpaceBuildingWindow(this.ScreenManager, this);
                        AudioManager.PlayCue("echo_affirm");
                        this.showingDSBW = true;
                    }
                    else
                        this.showingDSBW = false;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.L) && !input.LastKeyboardState.IsKeyDown(Keys.L))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.ScreenManager.AddScreen((GameScreen)new PlanetListScreen(this.ScreenManager, this.EmpireUI));
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.F1) && !input.LastKeyboardState.IsKeyDown(Keys.F1))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    if (!this.showingFTLOverlay)
                    {
                        this.showingFTLOverlay = true;
                    }
                    else
                        this.showingFTLOverlay = false;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.F2) && !input.LastKeyboardState.IsKeyDown(Keys.F2))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    if (!this.showingRangeOverlay)
                    {
                        this.showingRangeOverlay = true;
                    }
                    else
                        this.showingRangeOverlay = false;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.K) && !input.LastKeyboardState.IsKeyDown(Keys.K))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.ScreenManager.AddScreen((GameScreen)new ShipListScreen(this.ScreenManager, this.EmpireUI));
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.J) && !input.LastKeyboardState.IsKeyDown(Keys.J))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.ScreenManager.AddScreen((GameScreen)new FleetDesignScreen(this.EmpireUI));
                    FleetDesignScreen.Open = true;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.H) && !input.LastKeyboardState.IsKeyDown(Keys.H))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.aw.isOpen = !this.aw.isOpen;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.PageUp) && !input.LastKeyboardState.IsKeyDown(Keys.PageUp))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.AdjustCamTimer = 1f;
                    this.transitionElapsedTime = 0.0f;
                    this.transitionDestination.Z = 4500f;
                    this.snappingToShip = true;
                    this.ViewingShip = true;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.PageDown) && !input.LastKeyboardState.IsKeyDown(Keys.PageDown))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.AdjustCamTimer = 1f;
                    this.transitionElapsedTime = 0.0f;
                    this.transitionDestination.X = this.camPos.X;
                    this.transitionDestination.Y = this.camPos.Y;
                    this.transitionDestination.Z = 4200000f * UniverseScreen.GameScaleStatic;
                }
                if (this.Debug)
                {
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.C) && !input.LastKeyboardState.IsKeyDown(Keys.C))
                        ResourceManager.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", EmpireManager.Remnants, this.mouseWorldPos);
                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.C) && !input.LastKeyboardState.IsKeyDown(Keys.C))
                        ResourceManager.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", this.player, this.mouseWorldPos);

                    try
                    {

                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.Z) && !input.LastKeyboardState.IsKeyDown(Keys.Z))
                            HelperFunctions.CreateFleetAt("Fleet 2", EmpireManager.Remnants, this.mouseWorldPos);
                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && !input.LastKeyboardState.IsKeyDown(Keys.Z))
                            HelperFunctions.CreateFleetAt("Fleet 1", this.player, this.mouseWorldPos);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Fleet creation failed");
                    }
                    if (this.SelectedShip != null && this.Debug)
                    {
                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X))
                        {
                            foreach (ModuleSlot mod in SelectedShip.ModuleSlotList)
                            { mod.module.Health = 1; }    //Added by Gretman so I can hurt ships when the disobey me... I mean for testing... Yea, thats it...
                            SelectedShip.Health = SelectedShip.ModuleSlotList.Count;
                        }
                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X))
                            SelectedShip.Die(null, false);
                    }
                    else if (SelectedPlanet != null && Debug && (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X)))
                    {
                        foreach (KeyValuePair<string, Troop> keyValuePair in ResourceManager.TroopsDict)
                            SelectedPlanet.AssignTroopToTile(ResourceManager.CreateTroop(keyValuePair.Value, EmpireManager.Remnants));
                    }

                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.V) && !input.LastKeyboardState.IsKeyDown(Keys.V))
                        ResourceManager.CreateShipAtPoint("Remnant Mothership", EmpireManager.Remnants, this.mouseWorldPos);
                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.V) && !input.LastKeyboardState.IsKeyDown(Keys.V))
                        ResourceManager.CreateShipAtPoint("Target Dummy", EmpireManager.Remnants, this.mouseWorldPos);

                    //This little sections added to stress-test the resource manager, and load lots of models into memory.      -Gretman
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.B) && !input.LastKeyboardState.IsKeyDown(Keys.B))
                    {
                        if (DebugInfoScreen.loadmodels == 4)    //Capital and Carrier
                        {
                            ResourceManager.CreateShipAtPoint("Mordaving L", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Revenant-Class Dreadnought", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Draylok Warbird", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Archangel-Class Dreadnought", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Zanbato-Class Mk IV Battleship", this.player, this.mouseWorldPos);    //Kulrathi
                            ResourceManager.CreateShipAtPoint("Tarantula-Class Mk V Battleship", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Black Widow-Class Dreadnought", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Corpse Flower III", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Wolfsbane-Class Mk III Battleship", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Sceptre Torp", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Devourer-Class Mk V Battleship", this.player, this.mouseWorldPos);    //Vulfen
                            ResourceManager.CreateShipAtPoint("SS-Fighter Base Alpha", this.player, this.mouseWorldPos);    //Station
                            ++DebugInfoScreen.loadmodels;
                        }

                        if (DebugInfoScreen.loadmodels == 3)    //Cruiser
                        {
                            ResourceManager.CreateShipAtPoint("Storving Laser", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Draylok Bird of Prey", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Terran Torpedo Cruiser", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Terran Inhibitor", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Mauler Carrier", this.player, this.mouseWorldPos);    //Kulrathi
                            ResourceManager.CreateShipAtPoint("Chitin Cruiser Zero L", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Doom Flower", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Missile Acolyte II", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Ancient Torpedo Cruiser", this.player, this.mouseWorldPos);    //Remnant
                            ResourceManager.CreateShipAtPoint("Type X Artillery", this.player, this.mouseWorldPos);    //Vulfen
                            ++DebugInfoScreen.loadmodels;
                        }

                        if (DebugInfoScreen.loadmodels == 2)    //Frigate
                        {
                            ResourceManager.CreateShipAtPoint("Owlwok Beamer", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Scythe Torpedo", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Laser Frigate", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Missile Corvette", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Razorclaw", this.player, this.mouseWorldPos);    //Kulrathi
                            ResourceManager.CreateShipAtPoint("Kulrathi Railer", this.player, this.mouseWorldPos);    //Kulrathi
                            ResourceManager.CreateShipAtPoint("Stormsoldier", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Fern Artillery", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Adv Zion Railer", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Corsair", this.player, this.mouseWorldPos);    //Remnant
                            ResourceManager.CreateShipAtPoint("Type VII Laser", this.player, this.mouseWorldPos);    //Vulfen
                            ++DebugInfoScreen.loadmodels;
                        }

                        if (DebugInfoScreen.loadmodels == 1)    //Corvette
                        {
                            ResourceManager.CreateShipAtPoint("Laserlitving I", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Crescent Rocket", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Missile Hunter", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Razor RS", this.player, this.mouseWorldPos);    //Kulrathi
                            ResourceManager.CreateShipAtPoint("Armored Worker", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Thicket Attack Fighter", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Ralyeh Railship", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Heavy Drone", this.player, this.mouseWorldPos);    //Remnant
                            ResourceManager.CreateShipAtPoint("Grinder", this.player, this.mouseWorldPos);    //Vulfen
                            ResourceManager.CreateShipAtPoint("Stalker III Hvy Laser", this.player, this.mouseWorldPos);    //Vulfen
                            ResourceManager.CreateShipAtPoint("Listening Post", this.player, this.mouseWorldPos);    //Platform
                            ++DebugInfoScreen.loadmodels;
                        }

                        if (DebugInfoScreen.loadmodels == 0)    //Fighters and freighters
                        {
                            ResourceManager.CreateShipAtPoint("Laserving", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Owlwok Freighter S", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Owlwok Freighter M", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Owlwok Freighter L", this.player, this.mouseWorldPos);    //Cordrazine
                            ResourceManager.CreateShipAtPoint("Laserwisp", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Draylok Transporter", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Draylok Medium Trans", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Draylok Mobilizer", this.player, this.mouseWorldPos);    //Draylock
                            ResourceManager.CreateShipAtPoint("Rocket Scout", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Small Transport", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Medium Transport", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Large Transport", this.player, this.mouseWorldPos);    //Human
                            ResourceManager.CreateShipAtPoint("Flak Fang", this.player, this.mouseWorldPos);    //Kulrathi
                            ResourceManager.CreateShipAtPoint("Drone Railer", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Creeper Transport", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Crawler Transport", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Trawler Transport", this.player, this.mouseWorldPos);    //Opteris
                            ResourceManager.CreateShipAtPoint("Rocket Thorn", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Seeder Transport", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Sower Transport", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Grower Transport", this.player, this.mouseWorldPos);    //Pollops
                            ResourceManager.CreateShipAtPoint("Ralyeh Inquisitor", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Vessel S", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Vessel M", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Vessel L", this.player, this.mouseWorldPos);    //Rayleh
                            ResourceManager.CreateShipAtPoint("Xeno Fighter", this.player, this.mouseWorldPos);    //Remnant
                            ResourceManager.CreateShipAtPoint("Type I Vulcan", this.player, this.mouseWorldPos);    //Vulfen
                            ++DebugInfoScreen.loadmodels;
                        }
                    }
                }
                this.HandleFleetSelections(input);
                if (input.Escaped)
                {
                    this.snappingToShip = false;
                    this.ViewingShip = false;
                    if ((double)this.camHeight < 1175000.0 && (double)this.camHeight > 146900.0)
                    {
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                        this.transitionDestination = new Vector3(this.camPos.X, this.camPos.Y, 1175000f);
                    }
                    else if ((double)this.camHeight < 146900.0)
                    {
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                        this.transitionDestination = new Vector3(this.camPos.X, this.camPos.Y, 147000f);
                    }
                    else if (this.viewState < UniverseScreen.UnivScreenState.SystemView)
                        this.transitionDestination =new Vector3(this.camPos.X, this.camPos.Y, this.GetZfromScreenState(UnivScreenState.SystemView));
                }
                if (input.Tab)
                    this.ShowShipNames = !this.ShowShipNames;
                this.HandleRightMouseNew(input);
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                {
                    if ((double)this.ClickTimer < (double)this.TimerDelay)
                    {
                        this.SelectedShipList.Clear();
                        if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                            this.previousSelection = this.SelectedShip;
                        this.SelectedShip = (Ship)null;
                        if (this.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            foreach (UniverseScreen.ClickablePlanets clickablePlanets in this.ClickPlanetList)
                            {
                                if ((double)Vector2.Distance(input.CursorPosition, clickablePlanets.ScreenPos) <= (double)clickablePlanets.Radius)
                                {
                                    AudioManager.PlayCue("sub_bass_whoosh");
                                    this.SelectedPlanet = clickablePlanets.planetToClick;
                                    if (!this.SnapBackToSystem)
                                        this.HeightOnSnap = this.camHeight;
                                    this.ViewPlanet((object)this.SelectedPlanet);
                                }
                            }
                        }
                        foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                        {
                            if ((double)Vector2.Distance(input.CursorPosition, clickableShip.ScreenPos) <= (double)clickableShip.Radius)
                            {
                                this.pickedSomethingThisFrame = true;
                                this.SelectedShipList.Add(clickableShip.shipToClick);
                                using (Array<UniverseScreen.ClickableShip>.Enumerator enumerator = this.ClickableShipsList.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        UniverseScreen.ClickableShip current = enumerator.Current;
                                        if (clickableShip.shipToClick != current.shipToClick && current.shipToClick.loyalty == clickableShip.shipToClick.loyalty && current.shipToClick.shipData.Role == clickableShip.shipToClick.shipData.Role)
                                            this.SelectedShipList.Add(current.shipToClick);
                                    }
                                    break;
                                }
                            }
                        }
                        if (this.viewState > UniverseScreen.UnivScreenState.SystemView)
                        {
                            lock (GlobalStats.ClickableSystemsLock)
                            {
                                for (int local_27 = 0; local_27 < this.ClickableSystems.Count; ++local_27)
                                {
                                    UniverseScreen.ClickableSystem local_28 = this.ClickableSystems[local_27];
                                    if ((double)Vector2.Distance(input.CursorPosition, local_28.ScreenPos) <= (double)local_28.Radius)
                                    {
                                        if (local_28.systemToClick.ExploredDict[this.player])
                                        {
                                            AudioManager.GetCue("sub_bass_whoosh").Play();
                                            this.HeightOnSnap = this.camHeight;
                                            this.ViewSystem(local_28.systemToClick);
                                        }
                                        else
                                            this.PlayNegativeSound();
                                    }
                                }
                            }
                        }
                    }
                    else if (this.SelectedShip !=null)
                        this.ClickTimer = 0.0f;
                        //this.ClickTimer = 0.5f;
                }
                this.HandleSelectionBox(input);
                this.HandleScrolls(input);
            }
            if (this.LookingAtPlanet)
            {
                if (input.Tab)
                    this.ShowShipNames = !this.ShowShipNames;
                if ((input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released || this.workersPanel is ColonyScreen && (this.workersPanel as ColonyScreen).close.HandleInput(input)) && (!(this.workersPanel is ColonyScreen) || !(this.workersPanel as ColonyScreen).ClickedTroop))
                {
                    if (this.workersPanel is ColonyScreen && (this.workersPanel as ColonyScreen).p.Owner == null)
                    {
                        this.AdjustCamTimer = 1f;
                        if (this.returnToShip)
                        {
                            this.ViewingShip = true;
                            this.returnToShip = false;
                            this.snappingToShip = true;
                            this.transitionDestination.Z = this.transitionStartPosition.Z;
                        }
                        else
                            this.transitionDestination = this.transitionStartPosition;
                        this.transitionElapsedTime = 0.0f;
                        this.LookingAtPlanet = false;
                    }
                    else
                    {
                        this.AdjustCamTimer = 1f;
                        if (this.returnToShip)
                        {
                            this.ViewingShip = true;
                            this.returnToShip = false;
                            this.snappingToShip = true;
                            this.transitionDestination.Z = this.transitionStartPosition.Z;
                        }
                        else
                            this.transitionDestination = this.transitionStartPosition;
                        this.transitionElapsedTime = 0.0f;
                        this.LookingAtPlanet = false;
                    }
                }
            }
            if (input.InGameSelect && !this.pickedSomethingThisFrame && (!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && !this.pieMenu.Visible))
            {
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = (Ship)null;
                this.SelectedShipList.Clear();
                this.SelectedFleet = (Fleet)null;
                lock (GlobalStats.FleetButtonLocker)
                {
                    for (int local_31 = 0; local_31 < this.FleetButtons.Count; ++local_31)
                    {
                        UniverseScreen.FleetButton local_32 = this.FleetButtons[local_31];
                        if (HelperFunctions.CheckIntersection(local_32.ClickRect, input.CursorPosition))
                        {
                            this.SelectedFleet = local_32.Fleet;
                            this.SelectedShipList.Clear();
                            foreach (Ship item_7 in (Array<Ship>)this.SelectedFleet.Ships)
                            {
                                if (item_7.inSensorRange)
                                    this.SelectedShipList.Add(item_7);
                            }
                            if (this.SelectedShipList.Count == 1)
                            {
                                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip && this.SelectedShip != this.SelectedShipList[0]) //fbedard
                                    this.previousSelection = this.SelectedShip;
                                this.SelectedShip = this.SelectedShipList[0];
                                this.ShipInfoUIElement.SetShip(this.SelectedShip);
                                this.SelectedShipList.Clear();
                            }
                            else if (this.SelectedShipList.Count > 1)
                                this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, true);
                            this.SelectedSomethingTimer = 3f;

                            if ((double)this.ClickTimer < (double)this.TimerDelay)
                                {
                                    this.ViewingShip = false;
                                    this.AdjustCamTimer = 0.5f;
                                    this.transitionDestination.X = this.SelectedFleet.findAveragePosition().X;
                                    this.transitionDestination.Y = this.SelectedFleet.findAveragePosition().Y;
                                    if (this.viewState < UniverseScreen.UnivScreenState.SystemView)
                                        this.transitionDestination.Z = this.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                                }
                            else 
                                this.ClickTimer = 0.0f;
                        }
                    }
                }
            }

            this.cState = this.SelectedShip != null || this.SelectedShipList.Count > 0 ? UniverseScreen.CursorState.Move : UniverseScreen.CursorState.Normal;
            if (this.SelectedShip == null && this.SelectedShipList.Count <= 0)
                return;
            foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
            {
                if ((double)Vector2.Distance(input.CursorPosition, clickableShip.ScreenPos) <= (double)clickableShip.Radius)
                    this.cState = UniverseScreen.CursorState.Follow;
            }
            if (this.cState == UniverseScreen.CursorState.Follow)
                return;
            lock (GlobalStats.ClickableSystemsLock)
            {
                foreach (UniverseScreen.ClickablePlanets item_9 in this.ClickPlanetList)
                {
                    if ((double)Vector2.Distance(input.CursorPosition, item_9.ScreenPos) <= (double)item_9.Radius && item_9.planetToClick.habitable)
                        this.cState = UniverseScreen.CursorState.Orbit;
                }
            }
        }

        private void HandleFleetSelections(InputState input)
        {
            bool flag = false;
            int index = 10;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (input.CurrentKeyboardState.IsKeyDown(Keys.D1) && input.LastKeyboardState.IsKeyUp(Keys.D1))
                {
                    index = 1;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D2) && input.LastKeyboardState.IsKeyUp(Keys.D2))
                {
                    index = 2;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D3) && input.LastKeyboardState.IsKeyUp(Keys.D3))
                {
                    index = 3;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D4) && input.LastKeyboardState.IsKeyUp(Keys.D4))
                {
                    index = 4;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D5) && input.LastKeyboardState.IsKeyUp(Keys.D5))
                {
                    index = 5;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D6) && input.LastKeyboardState.IsKeyUp(Keys.D6))
                {
                    index = 6;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D7) && input.LastKeyboardState.IsKeyUp(Keys.D7))
                {
                    index = 7;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D8) && input.LastKeyboardState.IsKeyUp(Keys.D8))
                {
                    index = 8;
                    flag = true;
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D9) && input.LastKeyboardState.IsKeyUp(Keys.D9))
                {
                    index = 9;
                    flag = true;
                }
            }
            if (flag && !input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                if (this.SelectedShipList.Count > 0)
                {
                    foreach (Ship ship in (Array<Ship>)this.player.GetFleetsDict()[index].Ships)
                        ship.fleet = (Fleet)null;
                    this.player.GetFleetsDict()[index] = (Fleet)null;
                    string str = "";
                    switch (index)
                    {
                        case 1:
                            str = "First";
                            break;
                        case 2:
                            str = "Second";
                            break;
                        case 3:
                            str = "Third";
                            break;
                        case 4:
                            str = "Fourth";
                            break;
                        case 5:
                            str = "Fifth";
                            break;
                        case 6:
                            str = "Sixth";
                            break;
                        case 7:
                            str = "Seventh";
                            break;
                        case 8:
                            str = "Eigth";
                            break;
                        case 9:
                            str = "Ninth";
                            break;
                    }
                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                    {

                        ship.RemoveFromAllFleets();

                    }
                    this.player.GetFleetsDict()[index] = new Fleet();
                    this.player.GetFleetsDict()[index].Name = str + " Fleet";
                    this.player.GetFleetsDict()[index].Owner = this.player;
                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player && !ship.isConstructor && ship.Mothership == null)  //fbedard: cannot add ships from hangar in fleet
                            this.player.GetFleetsDict()[index].Ships.Add(ship);
                    }
                    this.player.GetFleetsDict()[index].AutoArrange();
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship)null;
                    this.SelectedShipList.Clear();
                    this.SelectedFlank = (Array<Fleet.Squad>)null;
                    if (this.player.GetFleetsDict()[index].Ships.Count > 0)
                    {
                        this.SelectedFleet = this.player.GetFleetsDict()[index];
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        this.SelectedFleet = (Fleet)null;
                    foreach (Ship ship in (Array<Ship>)this.player.GetFleetsDict()[index].Ships)
                    {
                        this.SelectedShipList.Add(ship);
                        ship.fleet = this.player.GetFleetsDict()[index];
                    }
                    this.RecomputeFleetButtons(true);
                    this.shipListInfoUI.SetShipList(this.SelectedShipList, true);  //fbedard:display new fleet in UI
                }
            }
            //added by gremlin add ships to exiting fleet
            else if (flag && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                if (this.SelectedShipList.Count > 0)
                {
                    //foreach (Ship ship in (Array<Ship>)this.player.GetFleetsDict()[index].Ships)
                    //    ship.fleet = (Fleet)null;
                    //this.player.GetFleetsDict()[index] = (Fleet)null;
                    string str = "";
                    switch (index)
                    {
                        case 1:
                            str = "First";
                            break;
                        case 2:
                            str = "Second";
                            break;
                        case 3:
                            str = "Third";
                            break;
                        case 4:
                            str = "Fourth";
                            break;
                        case 5:
                            str = "Fifth";
                            break;
                        case 6:
                            str = "Sixth";
                            break;
                        case 7:
                            str = "Seventh";
                            break;
                        case 8:
                            str = "Eigth";
                            break;
                        case 9:
                            str = "Ninth";
                            break;
                    }
                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                    {
                        if (ship.fleet != null && ship.fleet.Name == str + " Fleet")
                            continue;
                        if (ship.fleet != null && ship.fleet.Name != str + " Fleet")
                            ship.RemoveFromAllFleets();
                    }
                    if (this.player.GetFleetsDict()[index] !=null && this.player.GetFleetsDict()[index].Ships.Count == 0)
                    {
                        this.player.GetFleetsDict()[index] = new Fleet();
                        this.player.GetFleetsDict()[index].Name = str + " Fleet";
                        this.player.GetFleetsDict()[index].Owner = this.player;
                    }
                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player && !ship.isConstructor && (ship.fleet == null || ship.fleet.Name != str + " Fleet") && ship.Mothership == null)  //fbedard: cannot add ships from hangar in fleeet
                            this.player.GetFleetsDict()[index].Ships.Add(ship);
                    }
                    this.player.GetFleetsDict()[index].AutoArrange();
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship)null;
                    this.SelectedShipList.Clear();
                    this.SelectedFlank = (Array<Fleet.Squad>)null;
                    if (this.player.GetFleetsDict()[index].Ships.Count > 0)
                    {
                        this.SelectedFleet = this.player.GetFleetsDict()[index];
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        this.SelectedFleet = (Fleet)null;
                    foreach (Ship ship in (Array<Ship>)this.player.GetFleetsDict()[index].Ships)
                    {
                        this.SelectedShipList.Add(ship);
                        ship.fleet = this.player.GetFleetsDict()[index];
                    }
                    this.RecomputeFleetButtons(true);
                }
            }
               //end of added by
            else
            {
                if (input.CurrentKeyboardState.IsKeyDown(Keys.D1) && input.LastKeyboardState.IsKeyUp(Keys.D1))
                    index = 1;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D2) && input.LastKeyboardState.IsKeyUp(Keys.D2))
                    index = 2;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D3) && input.LastKeyboardState.IsKeyUp(Keys.D3))
                    index = 3;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D4) && input.LastKeyboardState.IsKeyUp(Keys.D4))
                    index = 4;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D5) && input.LastKeyboardState.IsKeyUp(Keys.D5))
                    index = 5;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D6) && input.LastKeyboardState.IsKeyUp(Keys.D6))
                    index = 6;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D7) && input.LastKeyboardState.IsKeyUp(Keys.D7))
                    index = 7;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D8) && input.LastKeyboardState.IsKeyUp(Keys.D8))
                    index = 8;
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.D9) && input.LastKeyboardState.IsKeyUp(Keys.D9))
                    index = 9;
                if (index != 10)
                {
                    this.SelectedPlanet = (Planet)null;
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship)null;
                    this.SelectedFlank = (Array<Fleet.Squad>)null;
                    if (this.player.GetFleetsDict()[index].Ships.Count > 0)
                    {
                        this.SelectedFleet = this.player.GetFleetsDict()[index];
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        this.SelectedFleet = (Fleet)null;
                    this.SelectedShipList.Clear();
                    foreach (Ship ship in (Array<Ship>)this.player.GetFleetsDict()[index].Ships)
                    {
                        this.SelectedShipList.Add(ship);
                        this.SelectedSomethingTimer = 3f;
                    }
                    if (this.SelectedShipList.Count == 1)  //fbedard:display new fleet in UI
                    {
                        if (this.SelectedShip != null && this.previousSelection != this.SelectedShip && this.SelectedShip != this.SelectedShipList[0]) //fbedard
                            this.previousSelection = this.SelectedShip;
                        this.SelectedShip = this.SelectedShipList[0];
                        this.ShipInfoUIElement.SetShip(this.SelectedShip);
                    }
                    else if (this.SelectedShipList.Count > 1)
                        this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, true);  
                    //if (this.SelectedFleet != null)
                    //{
                    //    Array<Ship> shipList = new Array<Ship>();
                    //    foreach (Ship ship in (Array<Ship>)this.SelectedFleet.Ships)
                    //        shipList.Add(ship);
                    //    this.shipListInfoUI.SetShipList(shipList, true);
                    //}
                    if (this.SelectedFleet != null && (double)this.ClickTimer < (double)this.TimerDelay)
                    {
                        this.ViewingShip = false;
                        this.AdjustCamTimer = 0.5f;
                        this.transitionDestination.X = this.SelectedFleet.findAveragePosition().X;
                        this.transitionDestination.Y = this.SelectedFleet.findAveragePosition().Y;
                        if (this.camHeight < this.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView))
                            this.transitionDestination.Z = this.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                    }
                    else if (this.SelectedFleet != null)
                        this.ClickTimer = 0.0f;
                }
            }
            Fleet fleet = this.SelectedFleet;
        }

        public void RecomputeFleetButtons(bool now)
        {
            ++this.FBTimer;
            if (this.FBTimer <= 60 && !now)
                return;
            lock (GlobalStats.FleetButtonLocker)
            {
                int local_0 = 0;
                int local_1 = 60;
                int local_2 = 20;
                this.FleetButtons.Clear();
                foreach (KeyValuePair<int, Fleet> item_0 in player.GetFleetsDict())
                {
                    if (item_0.Value.Ships.Count > 0)
                    {
                        this.FleetButtons.Add(new UniverseScreen.FleetButton()
                        {
                            ClickRect = new Rectangle(local_2, local_1 + local_0 * local_1, 52, 48),
                            Fleet = item_0.Value,
                            Key = item_0.Key
                        });
                        ++local_0;
                    }
                }
                this.FBTimer = 0;
            }
        }

        private Ship CheckShipClick(Vector2 ClickPos)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (Vector2.Distance(this.input.CursorPosition, clickableShip.ScreenPos) <= clickableShip.Radius)
                    return clickableShip.shipToClick;
            }
            return (Ship)null;
        }

        private Planet CheckPlanetClick(Vector2 ClickPos)
        {
            foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
            {
                if (Vector2.Distance(input.CursorPosition, clickablePlanets.ScreenPos) <= clickablePlanets.Radius + 10.0)
                    return clickablePlanets.planetToClick;
            }
            return (Planet)null;
        }

        protected void HandleRightMouseNew(InputState input)
        {
            if (SkipRightOnce)
            {
                if (input.CurrentMouseState.RightButton != ButtonState.Released || input.LastMouseState.RightButton != ButtonState.Released)
                    return;
                SkipRightOnce = false;
            }
            else
            {
                Viewport viewport;
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
                {
                    this.SelectedSomethingTimer = 3f;
                    this.startDrag = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
                    this.startDragWorld = this.GetWorldSpaceFromScreenSpace(this.startDrag);
                    this.ProjectedPosition = this.GetWorldSpaceFromScreenSpace(this.startDrag);
                    Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    viewport = this.ScreenManager.GraphicsDevice.Viewport;
                    Vector3 direction = viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.projection, this.view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                }
                if (this.SelectedShip != null && this.SelectedShip.GetAI().State == AIState.ManualControl && (double)Vector2.Distance(this.startDragWorld, this.SelectedShip.Center) < 5000.0)
                    return;
                if (input.CurrentMouseState.RightButton == ButtonState.Released && input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    viewport = this.ScreenManager.GraphicsDevice.Viewport;
                    Vector3 position = viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    viewport = this.ScreenManager.GraphicsDevice.Viewport;
                    Vector3 direction = viewport.Unproject(new Vector3((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y, 1f), this.projection, this.view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num1 = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num1 * ray.Direction.X, ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
                    Vector2 vector2_1 = new Vector2(vector3.X, vector3.Y);
                    Vector2 target = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
                    float num2 = startDrag.RadiansToTarget(target);
                    Vector2 vector2_2 = Vector2.Normalize(target - this.startDrag);
                    if (input.RightMouseTimer > 0.0f)
                    {
                        if (this.SelectedFleet != null && this.SelectedFleet.Owner.isPlayer)
                        {
                            AudioManager.PlayCue("echo_affirm1");
                            this.SelectedSomethingTimer = 3f;
                            float num3 = SelectedFleet.Position.RadiansToTarget(vector2_1);
                            Vector2 vectorToTarget = Vector2.Zero.FindVectorToTarget(SelectedFleet.Position.PointFromRadians(num3, 1f));
                            foreach (Ship ship in (Array<Ship>)this.SelectedFleet.Ships)
                                this.player.GetGSAI().DefensiveCoordinator.remove(ship);
                            Ship ship1 = this.CheckShipClick(this.startDrag);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = this.CheckPlanetClick(this.startDrag);
                            if (ship1 != null && ship1.loyalty != this.player)
                            {
                                this.SelectedFleet.Position = ship1.Center;
                                this.SelectedFleet.AssignPositions(0.0f);
                                foreach (Ship ship2 in (Array<Ship>)this.SelectedFleet.Ships)
                                {
                                    if (ship2.shipData.Role == ShipData.RoleName.troop)
                                        ship2.GetAI().OrderTroopToBoardShip(ship1);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        ship2.GetAI().OrderQueueSpecificTarget(ship1);
                                    else
                                        ship2.GetAI().OrderAttackSpecificTarget(ship1);
                                }
                            }
                            else if (planet != null)
                            {
                                this.SelectedFleet.Position = planet.Position;  //fbedard: center fleet on planet
                                foreach (Ship ship2 in (Array<Ship>)this.SelectedFleet.Ships)
                                {
                                    RightClickship(ship2, planet,false);
                                }
                            }
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                this.SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget, true);
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                this.SelectedFleet.MoveToDirectly(vector2_1, num3, vectorToTarget);
                            else
                                this.SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget);
                        }
                        else if (this.SelectedShip != null && this.SelectedShip.loyalty.isPlayer)
                        {
                            this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Remove(this.SelectedShip);
                            this.SelectedSomethingTimer = 3f;
                            Ship ship = this.CheckShipClick(this.startDrag);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = this.CheckPlanetClick(this.startDrag);
                            if (ship != null && ship != this.SelectedShip)
                            #region Target Ship
                            {
                                if (this.SelectedShip.isConstructor || this.SelectedShip.shipData.Role == ShipData.RoleName.supply)
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                                else
                                {
                                    AudioManager.PlayCue("echo_affirm1");
                                    if (ship.loyalty == this.player)
                                    {
                                        if (this.SelectedShip.shipData.Role == ShipData.RoleName.troop)
                                        {
                                            if (ship.TroopList.Count < ship.TroopCapacity)
                                                this.SelectedShip.GetAI().OrderTroopToShip(ship);
                                            else
                                                this.SelectedShip.DoEscort(ship);
                                        }
                                        else
                                            this.SelectedShip.DoEscort(ship);
                                    }
                                    else if (this.SelectedShip.shipData.Role == ShipData.RoleName.troop)
                                        this.SelectedShip.GetAI().OrderTroopToBoardShip(ship);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        this.SelectedShip.GetAI().OrderQueueSpecificTarget(ship);
                                    else
                                        this.SelectedShip.GetAI().OrderAttackSpecificTarget(ship);
                                }
                            } 
                            #endregion
                            // else if (ship != null && ship == this.SelectedShip)
                            else if (ship != null && ship == this.SelectedShip && this.SelectedShip.Mothership == null && !this.SelectedShip.isConstructor)  //fbedard: prevent hangar ship and constructor
                            {
                                if (ship.loyalty == this.player)
                                    this.LoadShipMenuNodes(1);
                                else
                                    this.LoadShipMenuNodes(0);
                                if (!this.pieMenu.Visible)
                                {
                                    this.pieMenu.RootNode = this.shipMenu;
                                    this.pieMenu.Show(this.pieMenu.Position);
                                }
                                else
                                    this.pieMenu.ChangeTo((PieMenuNode)null);
                            }
                            else if (planet != null)
                            {

                                RightClickship(this.SelectedShip, planet,true);
                               
                            }
                            else if (this.SelectedShip.isConstructor || this.SelectedShip.shipData.Role == ShipData.RoleName.supply)
                            {
                                AudioManager.PlayCue("UI_Misc20");
                                return;
                            }
                            else
                            {
                                AudioManager.PlayCue("echo_affirm1");
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                {
                                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                        this.SelectedShip.GetAI().OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2, false);
                                    else
                                        this.SelectedShip.GetAI().OrderMoveTowardsPosition(vector2_1, num2, vector2_2, false,null);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    this.SelectedShip.GetAI().OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2, true);
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
                                {
                                    this.SelectedShip.GetAI().OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true,null);
                                    this.SelectedShip.GetAI().OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.HoldPosition, vector2_1, num2));
                                    this.SelectedShip.GetAI().HasPriorityOrder = true;
                                    this.SelectedShip.GetAI().IgnoreCombat = true;
                                }
                                else
                                    this.SelectedShip.GetAI().OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true,null);
                            }
                        }
                        else if (this.SelectedShipList.Count > 0)
                        {
                            this.SelectedSomethingTimer = 3f;
                            foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                            {
                                if (ship.loyalty != this.player || ship.isConstructor || ship.shipData.Role == ShipData.RoleName.supply)
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                            }
                            AudioManager.PlayCue("echo_affirm1");
                            Ship ship1 = this.CheckShipClick(this.startDrag);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = this.CheckPlanetClick(this.startDrag);
                            if (ship1 != null || planet != null)
                            #region Target Planet
                            {
                                foreach (Ship ship2 in (Array<Ship>)this.SelectedShipList)
                                {
                                    this.player.GetGSAI().DefensiveCoordinator.remove(ship2);
                                    if (ship1 != null && ship1 != ship2)
                                    {
                                        if (ship1.loyalty == this.player)
                                        {
                                            if (ship2.shipData.Role == ShipData.RoleName.troop)
                                            {
                                                if (ship1.TroopList.Count < ship1.TroopCapacity)
                                                    ship2.GetAI().OrderTroopToShip(ship1);
                                                else
                                                    ship2.DoEscort(ship1);
                                            }
                                            else
                                                ship2.DoEscort(ship1);
                                        }
                                        else if (ship2.shipData.Role == ShipData.RoleName.troop)
                                            ship2.GetAI().OrderTroopToBoardShip(ship1);
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            ship2.GetAI().OrderQueueSpecificTarget(ship1);
                                        else
                                            ship2.GetAI().OrderAttackSpecificTarget(ship1);
                                    }
                                    else if (planet != null)
                                    {
                                        RightClickship(ship2, planet, false);

                                    }
                                }
                            } 
                            #endregion
                            else
                            {
                                this.SelectedSomethingTimer = 3f;
                                foreach (Ship ship2 in (Array<Ship>)this.SelectedShipList)
                                {
                                    if (ship2.isConstructor || ship2.shipData.Role == ShipData.RoleName.supply)
                                    {
                                        this.SelectedShipList.Clear();
                                        AudioManager.PlayCue("UI_Misc20");
                                        return;
                                    }
                                }
                                AudioManager.PlayCue("echo_affirm1");
                                this.endDragWorld = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
                                Enumerable.OrderBy<Ship, float>((IEnumerable<Ship>)this.SelectedShipList, (Func<Ship, float>)(ship => ship.Center.X));
                                Vector2 fVec = new Vector2(-vector2_2.Y, vector2_2.X);
                                float num3 = Vector2.Distance(this.endDragWorld, this.startDragWorld);
                                int num4 = 0;
                                int num5 = 0;
                                float num6 = 0.0f;
                                for (int index = 0; index < this.SelectedShipList.Count; ++index)
                                {
                                    this.player.GetGSAI().DefensiveCoordinator.remove(this.SelectedShipList[index]);
                                    if ((double)this.SelectedShipList[index].GetSO().WorldBoundingSphere.Radius > (double)num6)
                                        num6 = this.SelectedShipList[index].GetSO().WorldBoundingSphere.Radius;
                                }
                                Fleet fleet = new Fleet();
                                if ((double)this.SelectedShipList.Count * (double)num6 > (double)num3)
                                {
                                    for (int index = 0; index < this.SelectedShipList.Count; ++index)
                                    {
                                        fleet.AddShip(this.SelectedShipList[index].SoftCopy());
                                        fleet.Ships[index].RelativeFleetOffset = new Vector2((num6 + 200f) * (float)num5, (float)num4 * (num6 + 200f));
                                        ++num5;
                                        if ((double)fleet.Ships[index].RelativeFleetOffset.X + (double)num6 > (double)num3)
                                        {
                                            num5 = 0;
                                            ++num4;
                                        }
                                    }
                                }
                                else
                                {
                                    float num7 = num3 / (float)this.SelectedShipList.Count;
                                    for (int index = 0; index < this.SelectedShipList.Count; ++index)
                                    {
                                        fleet.AddShip(this.SelectedShipList[index].SoftCopy());
                                        fleet.Ships[index].RelativeFleetOffset = new Vector2(num7 * (float)index, 0.0f);
                                    }
                                }
                                fleet.ProjectPos(this.endDragWorld, num2 - 1.570796f, fVec);
                                foreach (Ship ship2 in (Array<Ship>)fleet.Ships)
                                {
                                    foreach (Ship ship3 in (Array<Ship>)this.SelectedShipList)
                                    {
                                        if (ship2.guid == ship3.guid)
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            {
                                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                    ship3.GetAI().OrderMoveDirectlyTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, false);
                                                else
                                                    ship3.GetAI().OrderMoveTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, false,null);
                                            }
                                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship3.GetAI().OrderMoveDirectlyTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, true);
                                            else
                                                ship3.GetAI().OrderMoveTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, true,null);
                                        }
                                    }
                                }
                                this.projectedGroup = (ShipGroup)fleet;
                                fleet.Reset();
                            }
                        }
                        if (this.SelectedFlank == null && this.SelectedFleet == null && (this.SelectedItem == null && this.SelectedShip == null) && (this.SelectedPlanet == null && this.SelectedShipList.Count == 0))
                        {
                            Ship ship = this.CheckShipClick(input.CursorPosition);
                            //if (ship != null)
                            if (ship != null && ship.Mothership == null && !ship.isConstructor)  //fbedard: prevent hangar ship and constructor
                            {
                                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip && this.SelectedShip != ship) //fbedard
                                    this.previousSelection = this.SelectedShip;
                                this.SelectedShip = ship;
                                if (ship.loyalty == this.player)
                                    this.LoadShipMenuNodes(1);
                                else
                                    this.LoadShipMenuNodes(0);
                                if (!this.pieMenu.Visible)
                                {
                                    this.pieMenu.RootNode = this.shipMenu;
                                    this.pieMenu.Show(this.pieMenu.Position);
                                }
                                else
                                    this.pieMenu.ChangeTo((PieMenuNode)null);
                            }
                        }
                    }
                    else
                    {
                        this.ProjectingPosition = true;
                        if (this.SelectedFleet != null && this.SelectedFleet.Owner == this.player)
                        {
                            this.SelectedSomethingTimer = 3f;
                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                this.SelectedFleet.FormationWarpTo(this.ProjectedPosition, num2, vector2_2, true);
                            else
                                this.SelectedFleet.FormationWarpTo(this.ProjectedPosition, num2, vector2_2);
                            AudioManager.PlayCue("echo_affirm1");
                            foreach (Ship ship in (Array<Ship>)this.SelectedFleet.Ships)
                                this.player.GetGSAI().DefensiveCoordinator.remove(ship);
                        }
                        else if (this.SelectedShip != null && this.SelectedShip.loyalty == this.player)
                        {
                            this.player.GetGSAI().DefensiveCoordinator.remove(this.SelectedShip);
                            this.SelectedSomethingTimer = 3f;
                            if (this.SelectedShip.isConstructor || this.SelectedShip.shipData.Role == ShipData.RoleName.supply)
                            {
                                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                                    this.previousSelection = this.SelectedShip;
                                this.SelectedShip = (Ship)null;
                                AudioManager.PlayCue("UI_Misc20");
                                return;
                            }
                            else
                            {
                                AudioManager.PlayCue("echo_affirm1");
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                {
                                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                        this.SelectedShip.GetAI().OrderMoveDirectlyTowardsPosition(this.ProjectedPosition, num2, vector2_2, false);
                                    else
                                        this.SelectedShip.GetAI().OrderMoveTowardsPosition(this.ProjectedPosition, num2, vector2_2, false,null);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    this.SelectedShip.GetAI().OrderMoveDirectlyTowardsPosition(this.ProjectedPosition, num2, vector2_2, true);
                                else
                                    this.SelectedShip.GetAI().OrderMoveTowardsPosition(this.ProjectedPosition, num2, vector2_2, true,null);
                            }
                        }
                        else if (this.SelectedShipList.Count > 0)
                        {
                            this.SelectedSomethingTimer = 3f;
                            foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                            {
                                if (ship.loyalty != this.player)
                                    return;
                                if (ship.isConstructor || ship.shipData.Role == ShipData.RoleName.supply)
                                {
                                    this.SelectedShipList.Clear();
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                            }
                            AudioManager.PlayCue("echo_affirm1");
                            this.endDragWorld = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
                            Enumerable.OrderBy<Ship, float>((IEnumerable<Ship>)this.SelectedShipList, (Func<Ship, float>)(ship => ship.Center.X));
                            Vector2 fVec = new Vector2(-vector2_2.Y, vector2_2.X);
                            float num3 = Vector2.Distance(this.endDragWorld, this.startDragWorld);
                            int num4 = 0;
                            int num5 = 0;
                            float num6 = 0.0f;
                            for (int index = 0; index < this.SelectedShipList.Count; ++index)
                            {
                                this.player.GetGSAI().DefensiveCoordinator.remove(this.SelectedShipList[index]);
                                if ((double)this.SelectedShipList[index].GetSO().WorldBoundingSphere.Radius > (double)num6)
                                    num6 = this.SelectedShipList[index].GetSO().WorldBoundingSphere.Radius;
                            }
                            Fleet fleet = new Fleet();
                            if ((double)this.SelectedShipList.Count * (double)num6 > (double)num3)
                            {
                                for (int index = 0; index < this.SelectedShipList.Count; ++index)
                                {
                                    fleet.AddShip(this.SelectedShipList[index].SoftCopy());
                                    fleet.Ships[index].RelativeFleetOffset = new Vector2((num6 + 200f) * (float)num5, (float)num4 * (num6 + 200f));
                                    ++num5;
                                    if ((double)this.SelectedShipList[index].RelativeFleetOffset.X + (double)num6 > (double)num3)
                                    {
                                        num5 = 0;
                                        ++num4;
                                    }
                                }
                            }
                            else
                            {
                                float num7 = num3 / (float)this.SelectedShipList.Count;
                                for (int index = 0; index < this.SelectedShipList.Count; ++index)
                                {
                                    fleet.AddShip(this.SelectedShipList[index].SoftCopy());
                                    fleet.Ships[index].RelativeFleetOffset = new Vector2(num7 * (float)index, 0.0f);
                                }
                            }
                            fleet.ProjectPos(this.ProjectedPosition, num2 - 1.570796f, fVec);
                            foreach (Ship ship1 in (Array<Ship>)fleet.Ships)
                            {
                                foreach (Ship ship2 in (Array<Ship>)this.SelectedShipList)
                                {
                                    if (ship1.guid == ship2.guid)
                                    {
                                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship2.GetAI().OrderMoveDirectlyTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, false);
                                            else
                                                ship2.GetAI().OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, false,null);
                                        }
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                            ship2.GetAI().OrderMoveDirectlyTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, true);
                                        else
                                            ship2.GetAI().OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, true,null);
                                    }
                                }
                            }
                            this.projectedGroup = (ShipGroup)fleet;
                        }
                    }
                }
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    Vector2 target = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
                    float facing = startDrag.RadiansToTarget(target);
                    Vector2 fVec1 = Vector2.Normalize(target - this.startDrag);
                    if ((double)input.RightMouseTimer > 0.0)
                        return;
                    this.ProjectingPosition = true;
                    if (this.SelectedFleet != null && this.SelectedFleet.Owner == this.player)
                    {
                        this.ProjectingPosition = true;
                        this.SelectedFleet.ProjectPos(this.ProjectedPosition, facing, fVec1);
                        this.projectedGroup = (ShipGroup)this.SelectedFleet;
                    }
                    else if (this.SelectedShip != null && this.SelectedShip.loyalty == this.player)
                    {
                        if (this.SelectedShip.isConstructor || this.SelectedShip.shipData.Role == ShipData.RoleName.supply)
                        {
                            if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                                this.previousSelection = this.SelectedShip;
                            this.SelectedShip = (Ship)null;
                            AudioManager.PlayCue("UI_Misc20");
                        }
                        else
                        {
                            ShipGroup shipGroup = new ShipGroup();
                            shipGroup.Ships.Add(this.SelectedShip);
                            shipGroup.ProjectPos(this.ProjectedPosition, facing, fVec1);
                            this.projectedGroup = shipGroup;
                        }
                    }
                    else
                    {
                        if (this.SelectedShipList.Count <= 0)
                            return;
                        foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                return;
                        }
                        this.endDragWorld = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
                        Enumerable.OrderBy<Ship, float>((IEnumerable<Ship>)this.SelectedShipList, (Func<Ship, float>)(ship => ship.Center.X));
                        Vector2 fVec2 = new Vector2(-fVec1.Y, fVec1.X);
                        float num1 = Vector2.Distance(this.endDragWorld, this.startDragWorld);
                        int num2 = 0;
                        int num3 = 0;
                        float num4 = 0.0f;
                        for (int index = 0; index < this.SelectedShipList.Count; ++index)
                        {
                            if ((double)this.SelectedShipList[index].GetSO().WorldBoundingSphere.Radius > (double)num4)
                                num4 = this.SelectedShipList[index].GetSO().WorldBoundingSphere.Radius;
                        }
                        Fleet fleet = new Fleet();
                        if ((double)this.SelectedShipList.Count * (double)num4 > (double)num1)
                        {
                            for (int index = 0; index < this.SelectedShipList.Count; ++index)
                            {
                                fleet.AddShip(this.SelectedShipList[index].SoftCopy());
                                fleet.Ships[index].RelativeFleetOffset = new Vector2((num4 + 200f) * (float)num3, (float)num2 * (num4 + 200f));
                                ++num3;
                                if ((double)this.SelectedShipList[index].RelativeFleetOffset.X + (double)num4 > (double)num1)
                                {
                                    num3 = 0;
                                    ++num2;
                                }
                            }
                        }
                        else
                        {
                            float num5 = num1 / (float)this.SelectedShipList.Count;
                            for (int index = 0; index < this.SelectedShipList.Count; ++index)
                            {
                                fleet.AddShip(this.SelectedShipList[index].SoftCopy());
                                fleet.Ships[index].RelativeFleetOffset = new Vector2(num5 * (float)index, 0.0f);
                            }
                        }
                        fleet.ProjectPos(this.ProjectedPosition, facing - 1.570796f, fVec2);
                        this.projectedGroup = (ShipGroup)fleet;
                    }
                }
                else
                    this.ProjectingPosition = false;
            }
        }

        //added by gremlin replace redundant code with method
        private void RightClickship(Ship ship, Planet planet, bool audio)
        {
            if (ship.isConstructor)
            {
                if (!audio)
                    return;
                AudioManager.PlayCue("UI_Misc20");
            }
            else
            {
                if (audio)
                    AudioManager.PlayCue("echo_affirm1");
                if (ship.isColonyShip)
                {
                    if (planet.Owner == null && planet.habitable)
                        ship.GetAI().OrderColonization(planet);
                    else
                        ship.GetAI().OrderToOrbit(planet, true);
                }
                else if (ship.shipData.Role == ShipData.RoleName.troop || (ship.TroopList.Count > 0 && (ship.HasTroopBay || ship.hasTransporter)))
                {
                    if (planet.Owner != null && planet.Owner == this.player && (!ship.HasTroopBay && !ship.hasTransporter))
                    {
                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.GetAI().OrderToOrbit(planet, false);
                        else
                            ship.GetAI().OrderRebase(planet, true);
                    }
                    else if (planet.habitable && (planet.Owner == null || planet.Owner != player && (ship.loyalty.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)))
                    {
                        //add new right click troop and troop ship options on planets
                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.GetAI().OrderToOrbit(planet, false);
                        else
                        {
                            ship.GetAI().State = AIState.AssaultPlanet;
                            ship.GetAI().OrderLandAllTroops(planet);
                        }
                    }
                    else
                    {
                        ship.GetAI().OrderOrbitPlanet(planet);// OrderRebase(planet, true);
                    }
                }
                else if (ship.BombBays.Count > 0)
                {
                    float enemies = planet.GetGroundStrengthOther(this.player) * 1.5f;
                    float friendlies = planet.GetGroundStrength(this.player);
                    if (planet.Owner != this.player)
                    {
                        if (planet.Owner == null || this.player.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)
                        {
                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                ship.GetAI().OrderBombardPlanet(planet);
                            else if (enemies > friendlies || planet.Population > 0f)
                                ship.GetAI().OrderBombardPlanet(planet);
                            else
                            {
                                ship.GetAI().OrderToOrbit(planet, false);
                            }
                        }
                        else
                        {
                            ship.GetAI().OrderToOrbit(planet, false);
                        }


                    }
                    else if (enemies > friendlies && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        ship.GetAI().OrderBombardPlanet(planet);
                    }
                    else
                        ship.GetAI().OrderToOrbit(planet, true);
                }
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    ship.GetAI().OrderToOrbit(planet, false);
                else
                    ship.GetAI().OrderToOrbit(planet, true);
            }
                            



        }

        public Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenSpace)
        {
            Vector3 position = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(screenSpace, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector3 direction = this.ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(screenSpace, 1f), this.projection, this.view, Matrix.Identity) - position;
            direction.Normalize();
            Ray ray = new Ray(position, direction);
            float num = -ray.Position.Z / ray.Direction.Z;
            Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
            return new Vector2(vector3.X, vector3.Y);
        }

        private void HandleEdgeDetection(InputState input)
        {
            if (this.LookingAtPlanet)
                return;
            PresentationParameters presentationParameters = this.ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 spaceFromScreenSpace1 = this.GetWorldSpaceFromScreenSpace(new Vector2(0.0f, 0.0f));
            float num = this.GetWorldSpaceFromScreenSpace(new Vector2((float)presentationParameters.BackBufferWidth, (float)presentationParameters.BackBufferHeight)).X - spaceFromScreenSpace1.X;
            if ((double)input.CursorPosition.X <= 1.0 || input.CurrentKeyboardState.IsKeyDown(Keys.Left) || (input.CurrentKeyboardState.IsKeyDown(Keys.A) && !this.ViewingShip))
            {
                this.transitionDestination.X -= 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if ((double)input.CursorPosition.X >= (double)(presentationParameters.BackBufferWidth - 1) || input.CurrentKeyboardState.IsKeyDown(Keys.Right) || (input.CurrentKeyboardState.IsKeyDown(Keys.D) && !this.ViewingShip))
            {
                this.transitionDestination.X += 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if ((double)input.CursorPosition.Y <= 0.0 || input.CurrentKeyboardState.IsKeyDown(Keys.Up) || (input.CurrentKeyboardState.IsKeyDown(Keys.W) && !this.ViewingShip))
            {
                this.snappingToShip = false;
                this.ViewingShip = false;
                this.transitionDestination.Y -= 0.008f * num;
            }
            if ((double)input.CursorPosition.Y >= (double)(presentationParameters.BackBufferHeight - 1) || input.CurrentKeyboardState.IsKeyDown(Keys.Down) || (input.CurrentKeyboardState.IsKeyDown(Keys.S) && !this.ViewingShip))
            {
                this.transitionDestination.Y += 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            //fbedard: remove middle button scrolling
            //if (input.CurrentMouseState.MiddleButton == ButtonState.Pressed)
            //{
            //    this.snappingToShip = false;
            //    this.ViewingShip = false;
            //}
            //if (input.CurrentMouseState.MiddleButton != ButtonState.Pressed || input.LastMouseState.MiddleButton != ButtonState.Released)
            //    return;
            //Vector2 spaceFromScreenSpace2 = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
            //this.transitionDestination.X = spaceFromScreenSpace2.X;
            //this.transitionDestination.Y = spaceFromScreenSpace2.Y;
            //this.transitionDestination.Z = this.camHeight;
            //this.AdjustCamTimer = 1f;
            //this.transitionElapsedTime = 0.0f;
        }

        protected void HandleScrolls(InputState input)
        {
            if ((double)this.AdjustCamTimer >= 0.0)
                return;
            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.camHeight / 3000.0 + 100.0);
                //fbedard: faster scroll
                //if ((double)this.camHeight < 10000.0)
                //    num -= 200f;
                this.transitionDestination.X = this.camPos.X;
                this.transitionDestination.Y = this.camPos.Y;
                this.transitionDestination.Z = this.camHeight + num;
                if ((double)this.camHeight > 12000.0)
                {
                    this.transitionDestination.Z += 3000f;
                    this.viewState = UniverseScreen.UnivScreenState.SectorView;
                    if ((double)this.camHeight > 32000.0)
                        this.transitionDestination.Z += 15000f;
                    if ((double)this.camHeight > 100000.0)
                        this.transitionDestination.Z += 40000f;
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
                {
                    if ((double)this.camHeight < 55000.0)
                    {
                        this.transitionDestination.Z = 60000f;
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                    }
                    else
                    {
                        this.transitionDestination.Z = 4200000f * this.GameScale;
                        this.AdjustCamTimer = 1f;
                        this.transitionElapsedTime = 0.0f;
                    }
                }
            }
            if (!input.YButtonHeld && !input.ScrollIn || this.LookingAtPlanet)
                return;
            float num1 = (float)(1500.0 * (double)this.camHeight / 3000.0 + 100.0);
            //fbedard: faster scroll
            //if ((double)this.camHeight < 10000.0)
            //    num1 -= 200f;
            this.transitionDestination.Z = this.camHeight - num1;
            if ((double)this.camHeight >= 16000.0)
            {
                this.transitionDestination.Z -= 2000f;
                if ((double)this.camHeight > 32000.0)
                    this.transitionDestination.Z -= 7500f;
                if ((double)this.camHeight > 150000.0)
                    this.transitionDestination.Z -= 40000f;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && (double)this.camHeight > 10000.0)
                this.transitionDestination.Z = (double)this.camHeight <= 65000.0 ? 10000f : 60000f;
            if (this.ViewingShip)
                return;
            if ((double)this.camHeight <= 450.0)
               this.camHeight = 450f;
            float num2 = this.transitionDestination.Z;
            
            //fbedard: add a scroll on selected object
            if ((!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && GlobalStats.ZoomTracking) || (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && !GlobalStats.ZoomTracking))
            {
                if (this.SelectedShip != null && this.SelectedShip.Active)
                {
                    this.transitionDestination = new Vector3(this.SelectedShip.Position.X, this.SelectedShip.Position.Y, num2);
                }
                else
                    if (this.SelectedPlanet != null)
                    {
                        this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y, num2);
                    }  
                    else
                        if (this.SelectedFleet != null && this.SelectedFleet.Ships.Count > 0)
                        {
                            this.transitionDestination = new Vector3(this.SelectedFleet.findAveragePosition().X, this.SelectedFleet.findAveragePosition().Y, num2);
                        }
                        else
                            if (this.SelectedShipList.Count > 0 && this.SelectedShipList[0] != null && this.SelectedShipList[0].Active)
                            {
                                this.transitionDestination = new Vector3(this.SelectedShipList[0].Position.X, this.SelectedShipList[0].Position.Y, num2);
                            }
                            else
                                this.transitionDestination = new Vector3(this.CalculateCameraPositionOnMouseZoom(new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y), num2), num2);
            }
            else
                this.transitionDestination = new Vector3(this.CalculateCameraPositionOnMouseZoom(new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y), num2), num2);
        }

        protected void HandleScrollsSectorMiniMap(InputState input)
        {
            this.SectorMiniMapHeight = MathHelper.SmoothStep(this.SectorMiniMapHeight, this.desiredSectorZ, 0.2f);
            if ((double)this.SectorMiniMapHeight < 6000.0)
                this.SectorMiniMapHeight = 6000f;
            if (input.InGameSelect)
            {
                this.transitionDestination.Z = this.SectorMiniMapHeight;
                this.transitionDestination.X = this.playerShip.Center.X;
                this.transitionDestination.Y = this.playerShip.Center.Y;
            }
            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.SectorMiniMapHeight / 3000.0 + 550.0);
                if ((double)this.SectorMiniMapHeight < 10000.0)
                    num -= 200f;
                this.desiredSectorZ = this.SectorMiniMapHeight + num;
                if ((double)this.SectorMiniMapHeight > 12000.0)
                {
                    this.desiredSectorZ += 3000f;
                    this.viewState = UniverseScreen.UnivScreenState.SectorView;
                    if ((double)this.SectorMiniMapHeight > 32000.0)
                        this.desiredSectorZ += 15000f;
                    if ((double)this.SectorMiniMapHeight > 100000.0)
                        this.desiredSectorZ += 40000f;
                }
            }
            if ((input.YButtonHeld || input.ScrollIn) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.SectorMiniMapHeight / 3000.0 + 550.0);
                if ((double)this.SectorMiniMapHeight < 10000.0)
                    num -= 200f;
                this.desiredSectorZ = this.SectorMiniMapHeight - num;
                if ((double)this.SectorMiniMapHeight >= 16000.0)
                {
                    this.desiredSectorZ -= 3000f;
                    if ((double)this.SectorMiniMapHeight > 32000.0)
                        this.desiredSectorZ -= 7500f;
                    if ((double)this.SectorMiniMapHeight > 150000.0)
                        this.desiredSectorZ -= 40000f;
                }
            }
            if ((double)this.camHeight <= 168471840.0 * (double)this.GameScale)
                return;
            this.camHeight = 1.684718E+08f * this.GameScale;
        }

        protected void HandleSelectionBox(InputState input)
        {
            if (this.LookingAtPlanet)
                return;
            if (this.SelectedShip != null && this.SelectedShip.Mothership == null && !this.SelectedShip.isConstructor)  //fbedard: prevent hangar ship and constructor
            {
                //if (input.CurrentKeyboardState.IsKeyDown(Keys.R) && !input.LastKeyboardState.IsKeyDown(Keys.R))  //fbedard: what is that !!!!
                //    this.SelectedShip.FightersOut = !this.SelectedShip.FightersOut;
                if (input.CurrentKeyboardState.IsKeyDown(Keys.Q) && !input.LastKeyboardState.IsKeyDown(Keys.Q))
                {
                    if (!this.pieMenu.Visible)
                    {
                        if (this.SelectedShip != null)
                            this.LoadShipMenuNodes(this.SelectedShip.loyalty == this.player ? 1 : 0);
                        this.pieMenu.RootNode = this.shipMenu;
                        this.pieMenu.Show(this.pieMenu.Position);
                    }
                    else
                        this.pieMenu.ChangeTo((PieMenuNode)null);
                }
            }
            Vector2 vector2 = input.CursorPosition - this.pieMenu.Position;
            vector2.Y *= -1f;
            Vector2 selectionVector = vector2 / this.pieMenu.Radius;
            this.pieMenu.HandleInput(input, selectionVector);
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released && !this.pieMenu.Visible)
            {
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = (Ship)null;
                this.SelectedPlanet = (Planet)null;
                this.SelectedFleet = (Fleet)null;
                this.SelectedFlank = (Array<Fleet.Squad>)null;
                this.SelectedSystem = (SolarSystem)null;
                this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                this.ProjectingPosition = false;
                this.projectedGroup = (ShipGroup)null;
                bool flag1 = false;
                if (this.viewState >= UniverseScreen.UnivScreenState.SectorView)
                {
                    lock (GlobalStats.ClickableSystemsLock)
                    {
                        for (int local_2 = 0; local_2 < this.ClickableSystems.Count; ++local_2)
                        {
                            UniverseScreen.ClickableSystem local_3 = this.ClickableSystems[local_2];
                            if ((double)Vector2.Distance(input.CursorPosition, local_3.ScreenPos) <= (double)local_3.Radius)
                            {
                                AudioManager.PlayCue("mouse_over4");
                                this.SelectedSystem = local_3.systemToClick;
                                this.sInfoUI.SetSystem(this.SelectedSystem);
                                flag1 = true;
                            }
                        }
                    }
                }
                bool flag2 = false;
                if (!flag1)
                {
                    foreach (UniverseScreen.ClickableFleet clickableFleet in this.ClickableFleetsList)
                    {
                        if ((double)Vector2.Distance(input.CursorPosition, clickableFleet.ScreenPos) <= (double)clickableFleet.ClickRadius)
                        {
                            this.SelectedShipList.Clear();
                            this.SelectedFleet = clickableFleet.fleet;
                            flag2 = true;
                            this.pickedSomethingThisFrame = true;
                            AudioManager.PlayCue("techy_affirm1");
                            using (Array<Ship>.Enumerator enumerator = this.SelectedFleet.Ships.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                    this.SelectedShipList.Add(enumerator.Current);
                                break;
                            }
                        }
                    }
                    if (!flag2)
                    {
                        foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                        {
                            if ((double)Vector2.Distance(input.CursorPosition, clickableShip.ScreenPos) <= (double)clickableShip.Radius)
                            {
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && this.SelectedShipList.Count > 1 && this.SelectedShipList.Contains(clickableShip.shipToClick))
                                {
                                    this.SelectedShipList.Remove(clickableShip.shipToClick);
                                    this.pickedSomethingThisFrame = true;
                                    AudioManager.GetCue("techy_affirm1").Play();
                                    break;
                                }
                                else
                                {
                                    if (this.SelectedShipList.Count > 0 && !input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && !this.pickedSomethingThisFrame)
                                        this.SelectedShipList.Clear();
                                    this.pickedSomethingThisFrame = true;
                                    AudioManager.GetCue("techy_affirm1").Play();
                                    //this.SelectedShip = clickableShip.shipToClick;  removed by fbedard
                                    this.SelectedSomethingTimer = 3f;
                                    if (!this.SelectedShipList.Contains(clickableShip.shipToClick))
                                    {
                                        if (clickableShip.shipToClick != null)
                                        {
                                            if (clickableShip.shipToClick.inSensorRange)
                                            {
                                                this.SelectedShipList.Add(clickableShip.shipToClick);
                                                break;
                                            }
                                            else
                                                break;
                                        }
                                        else
                                            break;
                                    }
                                    else
                                        break;
                                }
                            }
                        }
                        if (this.SelectedShip != null && this.SelectedShipList.Count == 1)
                            this.ShipInfoUIElement.SetShip(this.SelectedShip);
                        else if (this.SelectedShipList.Count > 1)
                            this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, false);
                        bool flag3 = false;
                        if (this.SelectedShipList.Count == 1)
                        {
                            if (this.SelectedShipList[0] == this.playerShip)
                                this.LoadShipMenuNodes(1);
                            else if (this.SelectedShipList[0].loyalty == this.player)
                                this.LoadShipMenuNodes(1);
                            else
                                this.LoadShipMenuNodes(0);
                        }
                        else
                        {
                            lock (GlobalStats.ClickableSystemsLock)
                            {
                                foreach (UniverseScreen.ClickablePlanets item_2 in this.ClickPlanetList)
                                {
                                    if ((double)Vector2.Distance(input.CursorPosition, item_2.ScreenPos) <= (double)item_2.Radius)
                                    {
                                        if ((double)this.ClickTimer2 < (double)this.TimerDelay)
                                        {
                                            this.SelectedPlanet = item_2.planetToClick;
                                            this.pInfoUI.SetPlanet(this.SelectedPlanet);
                                            this.SelectedSomethingTimer = 3f;
                                            flag3 = true;
                                            this.ViewPlanet((object)null);
                                            this.SelectionBox = new Rectangle();
                                        }
                                        else
                                        {
                                            AudioManager.GetCue("techy_affirm1").Play();
                                            this.SelectedPlanet = item_2.planetToClick;
                                            this.pInfoUI.SetPlanet(this.SelectedPlanet);
                                            this.SelectedSomethingTimer = 3f;
                                            flag3 = true;
                                            this.ClickTimer2 = 0.0f;
                                        }
                                    }
                                }
                            }
                        }
                        if (!flag3)
                        {
                            lock (GlobalStats.ClickableItemLocker)
                            {
                                for (int local_17 = 0; local_17 < this.ItemsToBuild.Count; ++local_17)
                                {
                                    UniverseScreen.ClickableItemUnderConstruction local_18 = this.ItemsToBuild[local_17];
                                    if (local_18 != null && (double)Vector2.Distance(input.CursorPosition, local_18.ScreenPos) <= (double)local_18.Radius)
                                    {
                                        AudioManager.GetCue("techy_affirm1").Play();
                                        this.SelectedItem = local_18;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (this.SelectedShip == null && this.SelectedShipList.Count == 0 && (this.SelectedPlanet != null && input.CurrentKeyboardState.IsKeyDown(Keys.Q)) && !input.LastKeyboardState.IsKeyDown(Keys.Q))
            {
                if (!this.pieMenu.Visible)
                {
                    this.pieMenu.RootNode = this.planetMenu;
                    if (this.SelectedPlanet.Owner == null && this.SelectedPlanet.habitable)
                        this.LoadMenuNodes(false, true);
                    else
                        this.LoadMenuNodes(false, false);
                    this.pieMenu.Show(this.pieMenu.Position);
                }
                else
                    this.pieMenu.ChangeTo((PieMenuNode)null);
            }
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                this.SelectionBox = new Rectangle(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0, 0);
            if (this.SelectedShipList.Count == 1)
            {
                if (this.SelectedShip != null && this.previousSelection != this.SelectedShip && this.SelectedShip != this.SelectedShipList[0]) //fbedard
                    this.previousSelection = this.SelectedShip;
                this.SelectedShip = this.SelectedShipList[0];
            }
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
            {
                this.SelectingWithBox = true;
                if (this.SelectionBox.X == 0 || this.SelectionBox.Y == 0)
                    return;
                this.SelectionBox = new Rectangle(this.SelectionBox.X, this.SelectionBox.Y, input.CurrentMouseState.X - this.SelectionBox.X, input.CurrentMouseState.Y - this.SelectionBox.Y);
            }
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
            {
                if (input.CurrentMouseState.X < this.SelectionBox.X)
                    this.SelectionBox.X = input.CurrentMouseState.X;
                if (input.CurrentMouseState.Y < this.SelectionBox.Y)
                    this.SelectionBox.Y = input.CurrentMouseState.Y;
                this.SelectionBox.Width = Math.Abs(this.SelectionBox.Width);
                this.SelectionBox.Height = Math.Abs(this.SelectionBox.Height);
                bool flag1 = true;
                Array<Ship> list = new Array<Ship>();
                foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                {
                    if (this.SelectionBox.Contains(new Point((int)clickableShip.ScreenPos.X, (int)clickableShip.ScreenPos.Y)) && !this.SelectedShipList.Contains(clickableShip.shipToClick))
                    {
                        this.SelectedPlanet = (Planet)null;
                        this.SelectedShipList.Add(clickableShip.shipToClick);
                        this.SelectedSomethingTimer = 3f;
                        list.Add(clickableShip.shipToClick);
                    }
                }
                if (this.SelectedShipList.Count > 0 && flag1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in list)
                    {
                        if (ship.shipData.Role <= ShipData.RoleName.supply)
                            flag2 = true;
                        else
                            flag3 = true;
                    }
                    if (flag3 && flag2)
                    {
                        foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                        {
                            if (ship.shipData.Role <= ShipData.RoleName.supply)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                    }
                    this.SelectedShipList.ApplyPendingRemovals();
                }
                if (this.SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player)
                            flag2 = true;
                        if (ship.loyalty != this.player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                        this.SelectedShipList.ApplyPendingRemovals();
                    }
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship)null;
                    //this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, true);
                    this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, false);  //fbedard: this is not a fleet!
                }
                else if (this.SelectedShipList.Count == 1)
                {
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip && this.SelectedShip != this.SelectedShipList[0]) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = this.SelectedShipList[0];
                    this.ShipInfoUIElement.SetShip(this.SelectedShip);
                }
                this.SelectionBox = new Rectangle(0, 0, -1, -1);
            }
            else
            {
                if (input.CurrentMouseState.LeftButton != ButtonState.Released || input.LastMouseState.LeftButton != ButtonState.Pressed)
                    return;
                this.SelectingWithBox = false;
                if (input.CurrentMouseState.X < this.SelectionBox.X)
                    this.SelectionBox.X = input.CurrentMouseState.X;
                if (input.CurrentMouseState.Y < this.SelectionBox.Y)
                    this.SelectionBox.Y = input.CurrentMouseState.Y;
                this.SelectionBox.Width = Math.Abs(this.SelectionBox.Width);
                this.SelectionBox.Height = Math.Abs(this.SelectionBox.Height);
                bool flag1 = false;
                if (this.SelectedShipList.Count == 0)
                    flag1 = true;
                foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                {
                    if (this.SelectionBox.Contains(new Point((int)clickableShip.ScreenPos.X, (int)clickableShip.ScreenPos.Y)))
                    {
                        this.SelectedPlanet = (Planet)null;
                        this.SelectedShipList.Add(clickableShip.shipToClick);
                        this.SelectedSomethingTimer = 3f;
                    }
                }
                if (this.SelectedShipList.Count > 0 && flag1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    try
                    {
                        foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                        {
                            if (ship.shipData.Role <= ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.GetAI().State ==  AIState.Colonize)
                                flag2 = true;
                            else
                                flag3 = true;
                        }
                    }
                    catch
                    {
                    }
                    if (flag3)
                    {
                        if (flag2)
                        {
                            try
                            {
                                foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                                {
                                    if (ship.shipData.Role <= ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.GetAI().State == AIState.Colonize)
                                        this.SelectedShipList.QueuePendingRemoval(ship);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    this.SelectedShipList.ApplyPendingRemovals();
                }
                if (this.SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player)
                            flag2 = true;
                        if (ship.loyalty != this.player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                        this.SelectedShipList.ApplyPendingRemovals();
                    }
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = (Ship)null;
                    bool flag4 = true;
                    if (this.SelectedShipList.Count > 0)
                    {
                        if (this.SelectedShipList[0].fleet != null)
                        {
                            if (this.SelectedShipList.Count == this.SelectedShipList[0].fleet.Ships.Count)
                            {
                                try
                                {
                                    foreach (Ship ship in (Array<Ship>)this.SelectedShipList)
                                    {
                                        if (ship.fleet == null || ship.fleet != this.SelectedShipList[0].fleet)
                                            flag4 = false;
                                    }
                                    if (flag4)
                                        this.SelectedFleet = this.SelectedShipList[0].fleet;
                                }
                                catch
                                {
                                }
                            }
                        }
                        if (this.SelectedFleet != null)
                            this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, true);
                        else
                            this.shipListInfoUI.SetShipList((Array<Ship>)this.SelectedShipList, false);
                    }
                    if (this.SelectedFleet == null)
                        this.ShipInfoUIElement.SetShip(this.SelectedShipList[0]);
                }
                else if (this.SelectedShipList.Count == 1)
                {
                    if (this.SelectedShip != null && this.previousSelection != this.SelectedShip && this.SelectedShip != this.SelectedShipList[0]) //fbedard
                        this.previousSelection = this.SelectedShip;
                    this.SelectedShip = this.SelectedShipList[0];
                    this.ShipInfoUIElement.SetShip(this.SelectedShip);
                    if (this.SelectedShipList[0] == this.playerShip)
                        this.LoadShipMenuNodes(1);
                    else if (this.SelectedShipList[0].loyalty == this.player)
                        this.LoadShipMenuNodes(1);
                    else
                        this.LoadShipMenuNodes(0);
                }
                this.SelectionBox = new Rectangle(0, 0, -1, -1);
            }
        }

        public override void ExitScreen()
        {
            ProcessTurnsThread.Abort();
            ProcessTurnsThread = null;
            EmpireUI.empire = null;
            EmpireUI = null;
            DeepSpaceManager.CollidableObjects.Clear();
            DeepSpaceManager.CollidableProjectiles.Clear();
            ShipSpatialManager.CollidableObjects.Clear();
            ScreenManager.Music.Stop(AudioStopOptions.Immediate);
            NebulousShit.Clear();
            bloomComponent = null;
            bg3d.BGItems.Clear();
            bg3d = null;
            playerShip = null;
            ShipToView = null;
            foreach (Ship ship in MasterShipList)
                ship.TotallyRemove();
            MasterShipList.ApplyPendingRemovals();
            MasterShipList.Clear();
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                solarSystem.spatialManager.CollidableProjectiles.Clear();
                solarSystem.spatialManager.CollidableObjects.Clear();
                solarSystem.spatialManager.ClearBuckets();
                solarSystem.spatialManager.Destroy();
                solarSystem.spatialManager = null;
                solarSystem.FiveClosestSystems.Clear();
                foreach (Planet planet in solarSystem.PlanetList)
                {
                    planet.TilesList = new Array<PlanetGridSquare>();
                    if (planet.SO != null)
                    {
                        planet.SO.Clear();
                        ScreenManager.inter.ObjectManager.Remove(planet.SO);
                        planet.SO = null;
                    }
                }
                foreach (Asteroid asteroid in solarSystem.AsteroidsList)
                {
                    if (asteroid.So!= null)
                    {
                        asteroid.So.Clear();
                        ScreenManager.inter.ObjectManager.Remove(asteroid.So);
                    }
                }
                solarSystem.AsteroidsList.Clear();
                foreach (Moon moon in solarSystem.MoonList)
                {
                    if (moon.So != null)
                    {
                        moon.So.Clear();
                        ScreenManager.inter.ObjectManager.Remove(moon.So);
                        moon.So = null;
                    }
                }
                solarSystem.MoonList.Clear();
            }
            foreach (Empire empire in EmpireManager.Empires)
                empire.CleanOut();

            JunkList.ApplyPendingRemovals();
            foreach (SpaceJunk spaceJunk in JunkList)
                spaceJunk.DestroySceneObject();
            JunkList.Clear();

            ResourceManager.ModelDict.Clear();            
            SelectedShip   = null;
            SelectedFleet  = null;
            SelectedPlanet = null;
            SelectedSystem = null;
            ShieldManager.Clear();
            PlanetsDict.Clear();
            ClickableFleetsList.Clear();
            ClickableShipsList.Clear();
            ClickPlanetList.Clear();
            ClickableSystems.Clear();
            DeepSpaceManager.ClearBuckets();
            DeepSpaceManager.CollidableObjects.Clear();
            DeepSpaceManager.CollidableObjects.Clear();
            DeepSpaceManager.CollidableProjectiles.Clear();
            DeepSpaceManager.ClearBuckets();
            DeepSpaceManager.Destroy();
            DeepSpaceManager = null;
            SolarSystemList.Clear();
            starfield.UnloadContent();
            starfield.Dispose();
            SolarSystemList.Clear();
            beamflashes.UnloadContent();
            explosionParticles.UnloadContent();
            photonExplosionParticles.UnloadContent();
            explosionSmokeParticles.UnloadContent();
            projectileTrailParticles.UnloadContent();
            fireTrailParticles.UnloadContent();
            smokePlumeParticles.UnloadContent();
            fireParticles.UnloadContent();
            engineTrailParticles.UnloadContent();
            flameParticles.UnloadContent();
            sparks.UnloadContent();
            lightning.UnloadContent();
            flash.UnloadContent();
            star_particles.UnloadContent();
            neb_particles.UnloadContent();
            SolarSystemDict.Clear();
            ShipDesignScreen.screen               = null;
            Fleet.screen                          = null;
            Bomb.Screen                           = null;
            Anomaly.screen                        = null;
            PlanetScreen.screen                   = null;
            MinimapButtons.screen                 = null;
            Projectile.contentManager             = ScreenManager.Content;
            Projectile.universeScreen             = null;
            ShipModule.universeScreen             = null;
            Empire.Universe                       = null;
            ResourceManager.UniverseScreen        = null;
            Planet.universeScreen                 = null;
            Weapon.universeScreen                 = null;
            Ship.universeScreen                   = null;
            ArtificialIntelligence.universeScreen = null;
            MissileAI.universeScreen              = null;
            CombatScreen.universeScreen           = null;
            MuzzleFlashManager.universeScreen     = null;
            FleetDesignScreen.screen              = null;
            ExplosionManager.Universe       = null;
            DroneAI.universeScreen                = null;
            StatTracker.SnapshotsDict.Clear();
            EmpireManager.Clear();            
            ScreenManager.inter.Unload();
            GC.Collect();            
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Dispose();
            base.ExitScreen();
        }

        private void ClearParticles()
        {
            beamflashes.UnloadContent();
            explosionParticles.UnloadContent();
            photonExplosionParticles.UnloadContent();
            explosionSmokeParticles.UnloadContent();
            projectileTrailParticles.UnloadContent();
            fireTrailParticles.UnloadContent();
            smokePlumeParticles.UnloadContent();
            fireParticles.UnloadContent();
            engineTrailParticles.UnloadContent();
            flameParticles.UnloadContent();
            sparks.UnloadContent();
            lightning.UnloadContent();
            flash.UnloadContent();
            star_particles.UnloadContent();
            neb_particles.UnloadContent();
           // GC.Collect(1, GCCollectionMode.Optimized);
        }

        protected void DrawRings(Matrix world, Matrix view, Matrix projection, float scale)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            foreach (BasicEffect basicEffect in xnaPlanetModel.Meshes[1].Effects)
            {
                basicEffect.World = Matrix.CreateScale(3f) * Matrix.CreateScale(scale) * world;
                basicEffect.View = view;
                basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.Texture = this.RingTexture;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
            }
            xnaPlanetModel.Meshes[1].Draw();
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        protected MultiShipData ComputeMultiShipCircle()
        {
            float num1 = 0.0f;
            float num2 = 0.0f;
            float num3 = 0.0f;
            float num4 = 0.0f;
            foreach (Ship ship in SelectedShipList)
            {
                num1 += ship.Position.X;
                num2 += ship.Position.Y;
                num3 += ship.Health;
                num4 += ship.HealthMax;
            }
            float x = num1 / SelectedShipList.Count;
            float y = num2 / SelectedShipList.Count;
            MultiShipData multiShipData = new MultiShipData();
            multiShipData.status = num3 / num4;
            multiShipData.weightedCenter = new Vector2(x, y);
            multiShipData.Radius = 0.0f;
            foreach (GameplayObject gameplayObject in SelectedShipList)
            {
                float num5 = Vector2.Distance(gameplayObject.Position, multiShipData.weightedCenter);
                if (num5 > multiShipData.Radius)
                    multiShipData.Radius = num5;
            }
            //this.computeCircle = false;
            return multiShipData;
        }

        protected void DrawAtmo(Model model, Matrix world, Matrix view, Matrix projection, Planet p)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.CullClockwiseFace;
            ModelMesh modelMesh = ((ReadOnlyCollection<ModelMesh>)model.Meshes)[0];
            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World = Matrix.CreateScale(4.1f) * world;
                basicEffect.View = view;
                basicEffect.Texture = ResourceManager.TextureDict["Atmos"];
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
                basicEffect.LightingEnabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.Direction = new Vector3(0.98f, -0.025f, 0.2f);
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight1.Direction = new Vector3(0.98f, -0.025f, 0.2f);
            }
            modelMesh.Draw();
            this.DrawAtmo1(world, view, projection);
            renderState.DepthBufferWriteEnable = true;
            renderState.CullMode = CullMode.CullCounterClockwiseFace;
            renderState.AlphaBlendEnable = false;
        }

        protected Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
        {
            return new Vector2(0.0f, 0.0f)
            {
                X = (float)-((double)OwnerPos.X - (double)TargetPos.X),
                Y = OwnerPos.Y - TargetPos.Y
            };
        }

        protected void DrawAtmo1(Matrix world, Matrix view, Matrix projection)
        {
            world = Matrix.CreateScale(3.83f) * world;
            Matrix matrix = world * view * projection;
            this.AtmoEffect.Parameters["World"].SetValue(world);
            this.AtmoEffect.Parameters["Projection"].SetValue(projection);
            this.AtmoEffect.Parameters["View"].SetValue(view);
            this.AtmoEffect.Parameters["CameraPosition"].SetValue(new Vector3(0.0f, 0.0f, 1500f));
            this.AtmoEffect.Parameters["DiffuseLightDirection"].SetValue(new Vector3(-0.98f, 0.425f, -0.4f));
            for (int index1 = 0; index1 < this.AtmoEffect.CurrentTechnique.Passes.Count; ++index1)
            {
                for (int index2 = 0; index2 < this.atmoModel.Meshes.Count; ++index2)
                {
                    ModelMesh modelMesh = ((ReadOnlyCollection<ModelMesh>)this.atmoModel.Meshes)[index2];
                    for (int index3 = 0; index3 < modelMesh.MeshParts.Count; ++index3)
                        modelMesh.MeshParts[index3].Effect = this.AtmoEffect;
                    modelMesh.Draw();
                }
            }
        }

        protected void DrawClouds(Model model, Matrix world, Matrix view, Matrix projection, Planet p)
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            ModelMesh modelMesh = ((ReadOnlyCollection<ModelMesh>)model.Meshes)[0];
            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World = Matrix.CreateScale(4.05f) * world;
                basicEffect.View = view;
                basicEffect.Texture = this.cloudTex;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
                basicEffect.LightingEnabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.SpecularColor = new Vector3(1f, 1f, 1f);
                if (this.UseRealLights)
                {
                    Vector3 vector3 = new Vector3(p.Position - p.system.Position, 0.0f);
                    vector3 = Vector3.Normalize(vector3);
                    basicEffect.DirectionalLight0.Direction = vector3;
                }
                else
                    basicEffect.DirectionalLight0.Direction = new Vector3(0.98f, -0.025f, 0.2f);
            }
            modelMesh.Draw();
            renderState.DepthBufferWriteEnable = true;
        }

        private void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect)
        {
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
            effect.Begin();
            effect.CurrentTechnique.Passes[0].Begin();
            this.ScreenManager.SpriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            this.ScreenManager.SpriteBatch.End();
            effect.CurrentTechnique.Passes[0].End();
            effect.End();
        }

        protected void DrawToolTip()
        {
            if (this.SelectedSystem != null && !this.LookingAtPlanet)
            {
                float num = 4500f;
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedSystem.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 Position = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(this.SelectedSystem.Position.X + num, this.SelectedSystem.Position.Y), 0.0f), this.projection, this.view, Matrix.Identity);
                float Radius = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), Position);
                if ((double)Radius < 5.0)
                    Radius = 5f;
                Rectangle rectangle = new Rectangle((int)Position.X - (int)Radius, (int)Position.Y - (int)Radius, (int)Radius * 2, (int)Radius * 2);
                Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, Position, Radius, Color.White);
            }
            if (this.SelectedPlanet == null || this.LookingAtPlanet || this.viewState >= UniverseScreen.UnivScreenState.GalaxyView)
                return;
            float radius = this.SelectedPlanet.SO.WorldBoundingSphere.Radius;
            Vector3 vector3_3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedPlanet.Position, 2500f), this.projection, this.view, Matrix.Identity);
            Vector2 Position1 = new Vector2(vector3_3.X, vector3_3.Y);
            Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(SelectedPlanet.Position.PointOnCircle(90f, radius), 2500f), this.projection, this.view, Matrix.Identity);
            float Radius1 = Vector2.Distance(new Vector2(vector3_4.X, vector3_4.Y), Position1);
            if ((double)Radius1 < 8.0)
                Radius1 = 8f;
            Vector2 vector2 = new Vector2(vector3_3.X, vector3_3.Y - Radius1);
            Rectangle rectangle1 = new Rectangle((int)Position1.X - (int)Radius1, (int)Position1.Y - (int)Radius1, (int)Radius1 * 2, (int)Radius1 * 2);
            Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, Position1, Radius1, this.SelectedPlanet.Owner != null ? this.SelectedPlanet.Owner.EmpireColor : Color.Gray);
        }

        protected void DrawShieldBubble(Ship ship)
        {
            if (this.LookingAtPlanet)
                return;
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            Vector2 vector2_1 = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
            foreach (ModuleSlot moduleSlot in ship.ModuleSlotList)
            {
                //Added by McShooterz: Changed it so when shields are turned off manually, do not draw bubble
                if (moduleSlot.module.ModuleType == ShipModuleType.Shield && moduleSlot.module.Active && moduleSlot.module.shield_power > 0 && !moduleSlot.module.shieldsOff)
                {
                    Vector2 origin1 = (int)moduleSlot.module.XSIZE != 1 || (int)moduleSlot.module.YSIZE != 3 ? ((int)moduleSlot.module.XSIZE != 2 || (int)moduleSlot.module.YSIZE != 5 ? new Vector2(moduleSlot.module.Center.X - 8f + (float)(16 * (int)moduleSlot.module.XSIZE / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)moduleSlot.module.YSIZE / 2)) : new Vector2(moduleSlot.module.Center.X - 80f + (float)(16 * (int)moduleSlot.module.XSIZE / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)moduleSlot.module.YSIZE / 2))) : new Vector2(moduleSlot.module.Center.X - 50f + (float)(16 * (int)moduleSlot.module.XSIZE / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)moduleSlot.module.YSIZE / 2));
                    Vector2 target = new Vector2(moduleSlot.module.Center.X - 8f, moduleSlot.module.Center.Y - 8f);
                    float angleToTarget = origin1.AngleToTarget(target);
                    Vector2 angleAndDistance = moduleSlot.module.Center.PointFromAngle(
                        MathHelper.ToDegrees(ship.Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));
                    float num1 = (float)((int)moduleSlot.module.XSIZE * 16 / 2);
                    float num2 = (float)((int)moduleSlot.module.YSIZE * 16 / 2);
                    float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num1, 2.0) + (float)Math.Pow((double)num2, 2.0)));
                    float radians = 3.141593f - (float)Math.Asin((double)num1 / (double)distance) + ship.Rotation;
                    origin1 = angleAndDistance.PointFromAngle(MathHelper.ToDegrees(radians), distance);
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(origin1, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 vector2_2 = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(moduleSlot.module.Center.PointOnCircle(90f, moduleSlot.module.shield_radius * 1.5f), 0.0f), this.projection, this.view, Matrix.Identity);
                    float num3 = Math.Abs(new Vector2(vector3_2.X, vector3_2.Y).X - vector2_2.X);
                    Rectangle destinationRectangle = new Rectangle((int)vector2_2.X, (int)vector2_2.Y, (int)num3 * 2, (int)num3 * 2);
                    Vector2 origin2 = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
                    float num4 = moduleSlot.module.shield_power / (moduleSlot.module.shield_power_max + (ship.loyalty != null ? ship.loyalty.data.ShieldPowerMod * moduleSlot.module.shield_power_max : 0));
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], destinationRectangle, new Rectangle?(), new Color(Color.Green.R, Color.Green.G, Color.Green.B, (byte)((double)byte.MaxValue * (double)num4)), 0.0f, origin2, SpriteEffects.None, 1f);
                }
            }
            this.ScreenManager.SpriteBatch.End();
        }
        
        protected void DrawFogNodes()
        {
            var uiNode = ResourceManager.TextureDict["UI/node"];
            var viewport = ScreenManager.GraphicsDevice.Viewport;

            foreach (FogOfWarNode fogOfWarNode in FogNodes)
            {
                if (!fogOfWarNode.Discovered)
                    continue;

                Vector3 vector3_1 = viewport.Project(fogOfWarNode.Position.ToVec3(), this.projection, this.view, Matrix.Identity);
                Vector2 vector2 = vector3_1.ToVec2();
                Vector3 vector3_2 = viewport.Project(new Vector3(fogOfWarNode.Position.PointOnCircle(90f, fogOfWarNode.Radius * 1.5f), 0.0f), this.projection, this.view, Matrix.Identity);
                float num = Math.Abs(new Vector2(vector3_2.X, vector3_2.Y).X - vector2.X);
                Rectangle destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, (int)num * 2, (int)num * 2);
                ScreenManager.SpriteBatch.Draw(uiNode, destinationRectangle, null, new Color(70, 255, 255, 255), 0.0f, uiNode.Center(), SpriteEffects.None, 1f);
            }
        }

        protected void DrawInfluenceNodes()
        {
            var uiNode = ResourceManager.TextureDict["UI/node"];
            var viewport = ScreenManager.GraphicsDevice.Viewport;

            foreach (Empire.InfluenceNode influ in player.SensorNodes.AtomicCopy())
            {
                Vector3 local_1 = viewport.Project(influ.Position.ToVec3(), this.projection, this.view, Matrix.Identity);
                Vector2 local_2 = local_1.ToVec2();
                Vector3 local_4 = viewport.Project(new Vector3(influ.Position.PointOnCircle(90f, influ.Radius * 1.5f), 0.0f), this.projection, this.view, Matrix.Identity);

                float local_6 = Math.Abs(new Vector2(local_4.X, local_4.Y).X - local_2.X) * 2.59999990463257f;
                Rectangle local_7 = new Rectangle((int)local_2.X, (int)local_2.Y, (int)local_6, (int)local_6);

                ScreenManager.SpriteBatch.Draw(uiNode, local_7, null, Color.White, 0.0f, uiNode.Center(), SpriteEffects.None, 1f);
            }
        }

        // this does some magic to convert a game position/coordinate to a drawable screen position
        private Vector2 ProjectToScreenPosition(Vector2 position)
        {
            return ScreenManager.GraphicsDevice.Viewport.Project(
                position.ToVec3(), projection, view, Matrix.Identity).ToVec2();
        }

        // Refactored by RedFox
        // this draws the colored empire borders
        // the borders are drawn into a separate framebuffer texture and later blended with final visual
        protected void DrawColoredEmpireBorders()
        {
            var spriteBatch = ScreenManager.SpriteBatch;
            var graphics    = ScreenManager.GraphicsDevice;
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            graphics.RenderState.SeparateAlphaBlendEnabled = true;
            graphics.RenderState.AlphaBlendOperation       = BlendFunction.Add;
            graphics.RenderState.AlphaSourceBlend          = Blend.One;
            graphics.RenderState.AlphaDestinationBlend     = Blend.One;
            graphics.RenderState.MultiSampleAntiAlias      = true;

            var nodeCorrected = ResourceManager.TextureDict["UI/nodecorrected"];
            var nodeConnect   = ResourceManager.TextureDict["UI/nodeconnect"];

            foreach (Empire empire in EmpireManager.Empires)
            {
                if (!Debug && empire != player && !player.GetRelations(empire).Known)
                    continue;

                var empireColor = empire.EmpireColor;
                using (empire.BorderNodes.AcquireReadLock())
                {
                    foreach (Empire.InfluenceNode influ in empire.BorderNodes)
                    {
                        if (!Frustum.Contains(influ.Position, influ.Radius))
                            continue;

                        Vector2 nodePos = ProjectToScreenPosition(influ.Position);
                        int size = (int)Math.Abs(ProjectToScreenPosition(influ.Position.PointOnCircle(90f, influ.Radius)).X - nodePos.X);

                        Rectangle rect = new Rectangle((int)nodePos.X, (int)nodePos.Y, size * 5, size * 5);
                        spriteBatch.Draw(nodeCorrected, rect, null, empireColor, 0.0f, nodeCorrected.Center(), SpriteEffects.None, 1f);

                        foreach (Empire.InfluenceNode influ2 in empire.BorderNodes)
                        {
                            if (influ.Position == influ2.Position || influ.Radius > influ2.Radius ||
                                influ.Position.OutsideRadius(influ2.Position, influ.Radius + influ2.Radius + 150000.0f))
                                continue;

                            Vector2 endPos = ProjectToScreenPosition(influ2.Position);

                            //Vector2 halfwayPosToNode2 = influ.Position + (influ2.Position - influ.Position) * 0.5f;
                            //Vector2 halfwayCenter     = ProjectToScreenPosition(halfwayPosToNode2);

                            // Debugging
                            //Primitives2D.DrawLine(spriteBatch, nodePos, endPos, Color.Brown);
                            //Primitives2D.DrawCircle(spriteBatch, halfwayCenter, 10.0f, 16, Color.Yellow);

                            float rotation = nodePos.RadiansToTarget(endPos);
                            rect = new Rectangle((int)endPos.X, (int)endPos.Y, size*3/2, (int)Vector2.Distance(nodePos, endPos));
                            spriteBatch.Draw(nodeConnect, rect, null, empireColor, rotation, new Vector2(2f,2f), SpriteEffects.None, 1f);
                        }
                    }
                }
            }
            spriteBatch.End();
        }

        protected void DrawMain(GameTime gameTime)
        {
            this.Render(gameTime);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            ExplosionManager.DrawExplosions(ScreenManager, view, projection);
            ScreenManager.SpriteBatch.End();
            if (!this.ShowShipNames)
                return;
            foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
                this.DrawShieldBubble(clickableShip.shipToClick);
        }

        protected virtual void DrawLights(GameTime gameTime)
        {
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.FogMapTarget);
            this.ScreenManager.GraphicsDevice.Clear(Color.TransparentWhite);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            this.ScreenManager.SpriteBatch.Draw(this.FogMap, new Rectangle(0, 0, 512, 512), Color.White);
            float num = 512f / Size.X;
            var uiNode = ResourceManager.TextureDict["UI/node"];
            foreach (Ship ship in player.GetShips())
            {
                if (HelperFunctions.CheckIntersection(ScreenRectangle, ship.ScreenPosition))
                {
                    Rectangle destinationRectangle = new Rectangle(
                        (int)(ship.Position.X * num), 
                        (int)(ship.Position.Y * num), 
                        (int)(ship.SensorRange * num * 2.0), 
                        (int)(ship.SensorRange * num * 2.0));
                    ScreenManager.SpriteBatch.Draw(uiNode, destinationRectangle, null, new Color(255, 0, 0, 255), 0.0f, uiNode.Center(), SpriteEffects.None, 1f);
                }
            }
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, null);
            this.FogMap = this.FogMapTarget.GetTexture();

            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.LightsTarget);
            this.ScreenManager.GraphicsDevice.Clear(Color.White);

            this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X / 2f, this.Size.Y / 2f, 0.0f), this.projection, this.view, Matrix.Identity);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            if (!Debug) // don't draw dark fog in debug
            {
                Vector3 vector3_1 = ScreenManager.GraphicsDevice.Viewport.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
                Vector3 vector3_2 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X, this.Size.Y, 0.0f), this.projection, this.view, Matrix.Identity);


                Rectangle fogRect = new Rectangle((int)vector3_1.X, (int)vector3_1.Y, (int)vector3_2.X - (int)vector3_1.X, (int)vector3_2.Y - (int)vector3_1.Y);
                Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), new Color((byte)0, (byte)0, (byte)0, (byte)170));
                this.ScreenManager.SpriteBatch.Draw(this.FogMap, fogRect, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)55));
            }
            this.DrawFogNodes();
            this.DrawInfluenceNodes();
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, null);
        }

        public override void Draw(GameTime gameTime)
        {
            // Wait for ProcessTurns to finish before we start drawing
            if (ProcessTurnsThread != null && ProcessTurnsThread.IsAlive) // check if thread is alive to avoid deadlock
                ProcessTurnsCompletedEvt.WaitOne();

            lock (GlobalStats.BeamEffectLocker)
            {
                Beam.BeamEffect.Parameters["View"].SetValue(view);
                Beam.BeamEffect.Parameters["Projection"].SetValue(projection);
            }
            AdjustCamera((float)gameTime.ElapsedGameTime.TotalSeconds);
            camPos.Z = camHeight;
            view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) 
                * Matrix.CreateRotationY(180f.ToRadians()) 
                * Matrix.CreateRotationX(0.0f.ToRadians()) 
                * Matrix.CreateLookAt(new Vector3(-camPos.X, camPos.Y, camHeight), new Vector3(-camPos.X, camPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Matrix matrix = view;

            var graphics = ScreenManager.GraphicsDevice;
            graphics.SetRenderTarget(0, MainTarget);
            DrawMain(gameTime);
            graphics.SetRenderTarget(0, null);
            DrawLights(gameTime);

            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                graphics.SetRenderTarget(0, BorderRT);
                graphics.Clear(Color.TransparentBlack);
                DrawColoredEmpireBorders();
            }

            graphics.SetRenderTarget(0, null);
            Texture2D texture1 = MainTarget.GetTexture();
            Texture2D texture2 = LightsTarget.GetTexture();
            graphics.SetRenderTarget(0, null);
            graphics.Clear(Color.Black);
            basicFogOfWarEffect.Parameters["LightsTexture"].SetValue((Texture)texture2);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            basicFogOfWarEffect.Begin();
            basicFogOfWarEffect.CurrentTechnique.Passes[0].Begin();
            ScreenManager.SpriteBatch.Draw(texture1, new Rectangle(0, 0, graphics.PresentationParameters.BackBufferWidth, graphics.PresentationParameters.BackBufferHeight), Color.White);
            basicFogOfWarEffect.CurrentTechnique.Passes[0].End();
            basicFogOfWarEffect.End();
            ScreenManager.SpriteBatch.End();
            view = matrix;
            if (drawBloom) bloomComponent.Draw(gameTime);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);

            if (viewState >= UnivScreenState.SectorView) // draw colored empire borders only if zoomed out
            {
                // set the alpha value depending on camera height
                int alpha = (int)(90.0f*camHeight / 1800000.0f);
                if (alpha > 90)      alpha = 90;
                else if (alpha < 10) alpha = 0;
                var color = new Color(255, 255, 255, (byte)alpha);

                ScreenManager.SpriteBatch.Draw(BorderRT.GetTexture(), 
                    new Rectangle(0, 0, 
                        graphics.PresentationParameters.BackBufferWidth, 
                        graphics.PresentationParameters.BackBufferHeight), color);
            }

            RenderOverFog(gameTime);
            ScreenManager.SpriteBatch.End();
            ScreenManager.SpriteBatch.Begin();
            DrawPlanetInfo();

            if (LookingAtPlanet && SelectedPlanet != null)
                workersPanel?.Draw(ScreenManager.SpriteBatch, gameTime);
            DrawShipsInRange();

            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                if (!solarSystem.isVisible)
                    continue;
                try
                {
                    foreach (Planet planet in solarSystem.PlanetList)
                    {
                        foreach (Projectile projectile in planet.Projectiles)
                        {
                            if (projectile.WeaponType != "Missile" && projectile.WeaponType != "Rocket" && projectile.WeaponType != "Drone")
                                DrawTransparentModel(ResourceManager.ProjectileModelDict[projectile.modelPath], projectile.GetWorld(), 
                                    view, projection, projectile.weapon.Animated != 0 
                                        ? ResourceManager.TextureDict[projectile.texturePath] 
                                        : ResourceManager.ProjTextDict[projectile.texturePath], projectile.Scale);
                        }
                    }
                }
                catch
                {
                }
            }       
            if (Debug) //input.CurrentKeyboardState.IsKeyDown(Keys.T) && !input.LastKeyboardState.IsKeyDown(Keys.T) && 
            {
                foreach (Empire e in EmpireManager.Empires)
                {
                    //if (e.isPlayer || e.isFaction)
                    //    continue;
                    //foreach (ThreatMatrix.Pin pin in e.GetGSAI().ThreatMatrix.Pins.Values)
                    //{
                    //    if (pin.Position != Vector2.Zero) // && pin.InBorders)
                    //    {
                    //        Circle circle = this.DrawSelectionCircles(pin.Position, 50f);
                    //        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 6, e.EmpireColor);
                    //        if(pin.InBorders)
                    //        {
                    //            circle = this.DrawSelectionCircles(pin.Position, 50f);
                    //            Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 3, e.EmpireColor);
                    //        }
                    //    }
                    //}
                    //for(int x=0;x < e.grid.GetLength(0);x++)
                    //    for (int y = 0; y < e.grid.GetLength(1); y++)
                    //    {
                    //        if (e.grid[x, y] != 1)
                    //            continue;
                    //        Vector2 translated = new Vector2((x - e.granularity) * reducer, (y - e.granularity) * reducer);
                    //        Circle circle = this.DrawSelectionCircles(translated, reducer *.5f);
                    //        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 4, e.EmpireColor);
                    //    }
                }
            }

            this.DrawTacticalPlanetIcons();
            if (showingFTLOverlay && GlobalStats.PlanetaryGravityWells && !LookingAtPlanet)
            {
                var inhibit = ResourceManager.TextureDict["UI/node_inhibit"];
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets item_1 in ClickPlanetList)
                    {
                        float local_14 = (float)(GlobalStats.GravityWellRange * (1 + ((Math.Log(item_1.planetToClick.scale)) / 1.5)));
                        Vector3 local_15 = graphics.Viewport.Project(new Vector3(item_1.planetToClick.Position.X, item_1.planetToClick.Position.Y, 0.0f), projection, view, Matrix.Identity);
                        Vector2 local_16 = local_15.ToVec2();


                        Vector3 local_18 = graphics.Viewport.Project(new Vector3(item_1.planetToClick.Position.PointOnCircle(90f, local_14), 0.0f), projection, view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);
                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);

                        ScreenManager.SpriteBatch.Draw(inhibit, local_21, null, 
                            new Color(200, 0, 0, 50), 0.0f, inhibit.Center(), SpriteEffects.None, 1f);

                        Primitives2D.DrawCircle(ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(255, 50, 0, 150), 1f);
                    }
                }
                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.InhibitionRadius > 0)
                    {
                        float local_14 = ship.shipToClick.InhibitionRadius;
                        Vector3 local_15 = graphics.Viewport.Project(new Vector3(ship.shipToClick.Position.X, ship.shipToClick.Position.Y, 0.0f), projection, view, Matrix.Identity);
                        Vector2 local_16 = local_15.ToVec2();
                        Vector3 local_18 = graphics.Viewport.Project(new Vector3(ship.shipToClick.Position.PointOnCircle(90f, local_14), 0.0f), projection, view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);

                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);
                        this.ScreenManager.SpriteBatch.Draw(inhibit, local_21, null, 
                            new Color(200, 0, 0, 40), 0.0f, inhibit.Center(), SpriteEffects.None, 1f);

                        Primitives2D.DrawCircle(ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(255, 50, 0, 150), 1f);
                    }
                }
                if (viewState >= UnivScreenState.SectorView)
                {
                    foreach (Empire.InfluenceNode influ in player.BorderNodes.AtomicCopy())
                    {
                        Vector3 local_15 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(influ.Position.X, influ.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_16 = local_15.ToVec2();
                        Vector3 local_18 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(influ.Position.PointOnCircle(90f, influ.Radius), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);
                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);

                        ScreenManager.SpriteBatch.Draw(inhibit, local_21, null, new Color(0, 200, 0, 20), 0.0f, inhibit.Center(), SpriteEffects.None, 1f);

                        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(30, 30, 150, 150), 1f);
                    }
                }
            }

            if (this.showingRangeOverlay && !this.LookingAtPlanet)
            {
                foreach (ClickableShip ship in this.ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.RangeForOverlay > 0 && ship.shipToClick.loyalty == EmpireManager.Player)
                    {
                        float local_14 = (float)(ship.shipToClick.RangeForOverlay);
                        Vector3 local_15 = graphics.Viewport.Project(new Vector3(ship.shipToClick.Position.X, ship.shipToClick.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_16 = new Vector2(local_15.X, local_15.Y);
                        Vector3 local_18 = graphics.Viewport.Project(new Vector3(ship.shipToClick.Position.PointOnCircle(90f, local_14), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);
                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);
                        Vector2 local_22 = new Vector2((float)(ResourceManager.TextureDict["UI/node_shiprange"].Width / 2), (float)(ResourceManager.TextureDict["UI/node_shiprange"].Height / 2));
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node_shiprange"], local_21, new Rectangle?(), new Color((byte)0, (byte)200, (byte)0, (byte)30), 0.0f, local_22, SpriteEffects.None, 1f);
                        //Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(byte.MaxValue, (byte)50, (byte)0, (byte)150), 2f);
                    }
                    else if (ship.shipToClick != null && ship.shipToClick.RangeForOverlay > 0)
                    {
                        float local_14 = (float)(ship.shipToClick.RangeForOverlay);
                        Vector3 local_15 = graphics.Viewport.Project(new Vector3(ship.shipToClick.Position.X, ship.shipToClick.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_16 = new Vector2(local_15.X, local_15.Y);
                        Vector3 local_18 = graphics.Viewport.Project(new Vector3(ship.shipToClick.Position.PointOnCircle(90f, local_14), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);
                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);
                        Vector2 local_22 = new Vector2((float)(ResourceManager.TextureDict["UI/node_shiprange"].Width / 2), (float)(ResourceManager.TextureDict["UI/node_shiprange"].Height / 2));
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node_shiprange"], local_21, new Rectangle?(), new Color((byte)200, (byte)0, (byte)0, (byte)30), 0.0f, local_22, SpriteEffects.None, 1f);
                        //Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(byte.MaxValue, (byte)50, (byte)0, (byte)150), 2f);
                    }
                }
            }
            
            if (showingDSBW && !LookingAtPlanet)
            {
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets item_1 in ClickPlanetList)
                    {
                        float local_14 = 2500f * item_1.planetToClick.scale;
                        Vector3 local_15 = graphics.Viewport.Project(new Vector3(item_1.planetToClick.Position.X, item_1.planetToClick.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_16 = new Vector2(local_15.X, local_15.Y);
                        Vector3 local_18 = graphics.Viewport.Project(new Vector3(item_1.planetToClick.Position.PointOnCircle(90f, local_14), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);
                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);
                        Vector2 local_22 = new Vector2(ResourceManager.TextureDict["UI/node"].Width / 2f, ResourceManager.TextureDict["UI/node"].Height / 2f);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node1"], local_21, new Rectangle?(), new Color(0, 0, 255, 50), 0.0f, local_22, SpriteEffects.None, 1f);
                        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(255, 165, 0, 150), 1f);
                    }
                }
                dsbw.Draw(gameTime);
            }
            DrawFleetIcons(gameTime);

            //fbedard: display values in new buttons
            ShipsInCombat.Text = "Ships: " + this.player.empireShipCombat;  
            if (player.empireShipCombat > 0)
            {
                ShipsInCombat.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"];
                ShipsInCombat.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"];
                ShipsInCombat.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"];
            }
            else
            {
                ShipsInCombat.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu"];
                ShipsInCombat.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_hover"];
                ShipsInCombat.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_pressed"];
            }
            ShipsInCombat.Draw(ScreenManager.SpriteBatch);

            PlanetsInCombat.Text = "Planets: " + player.empirePlanetCombat;
            if (player.empirePlanetCombat > 0)
            {
                PlanetsInCombat.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"];
                PlanetsInCombat.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"];
                PlanetsInCombat.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"];
            }
            else
            {
                PlanetsInCombat.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu"];
                PlanetsInCombat.HoverTexture  = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_hover"];
                PlanetsInCombat.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_menu_pressed"];
            }
            PlanetsInCombat.Draw(ScreenManager.SpriteBatch);

            if (!LookingAtPlanet)
                pieMenu.Draw(this.ScreenManager.SpriteBatch, Fonts.Arial12Bold);

            Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, SelectionBox, Color.Green, 1f);
            EmpireUI.Draw(ScreenManager.SpriteBatch);
            if (!LookingAtPlanet)
                DrawShipUI(gameTime);

            if (!LookingAtPlanet || LookingAtPlanet && workersPanel is UnexploredPlanetScreen || LookingAtPlanet && workersPanel is UnownedPlanetScreen)
            {
                DrawMinimap();
            }
            if (SelectedShipList.Count == 0)
                shipListInfoUI.ClearShipList();
            if (SelectedSystem != null && !LookingAtPlanet)
            {
                sInfoUI.SetSystem(SelectedSystem);
                sInfoUI.Update(gameTime);
                if (viewState == UnivScreenState.GalaxyView)
                    sInfoUI.Draw(gameTime);
            }
            if (SelectedPlanet != null && !LookingAtPlanet)
            {
                pInfoUI.SetPlanet(SelectedPlanet);
                pInfoUI.Update(gameTime);
                pInfoUI.Draw(gameTime);
            }
            else if (SelectedShip != null && !LookingAtPlanet)
            {
                ShipInfoUIElement.ship = SelectedShip;
                ShipInfoUIElement.ShipNameArea.Text = SelectedShip.VanityName;
                ShipInfoUIElement.Update(gameTime);
                ShipInfoUIElement.Draw(gameTime);
            }
            else if (SelectedShipList.Count > 1 && SelectedFleet == null)
            {
                shipListInfoUI.Update(gameTime);
                shipListInfoUI.Draw(gameTime);
            }
            else if (SelectedItem != null)
            {
                bool flag = false;
                for (int index = 0; index < SelectedItem.AssociatedGoal.empire.GetGSAI().Goals.Count; ++index)
                {
                    if (SelectedItem.AssociatedGoal.empire.GetGSAI().Goals[index].guid !=
                        SelectedItem.AssociatedGoal.guid) continue;
                    flag = true;
                    break;
                }
                if (flag)
                {
                    string titleText = "(" + ResourceManager.ShipsDict[SelectedItem.UID].Name + ")";
                    string bodyText  = Localizer.Token(1410) + SelectedItem.AssociatedGoal.GetPlanetWhereBuilding().Name;
                    vuiElement.Draw(gameTime, titleText, bodyText);
                    DrawItemInfoForUI();
                }
                else
                    SelectedItem = null;
            }
            else if (SelectedFleet != null && !LookingAtPlanet)
            {
                shipListInfoUI.Update(gameTime);
                shipListInfoUI.Draw(gameTime);
            }
            if (SelectedShip == null || LookingAtPlanet)
                ShipInfoUIElement.ShipNameArea.HandlingInput = false;
            DrawToolTip();
            if (!LookingAtPlanet)
                NotificationManager.Draw();
            
            if (Debug && showdebugwindow)
                debugwin.Draw(gameTime);

            if (aw.isOpen && !LookingAtPlanet)
                aw.Draw(gameTime);

            if (Paused)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(4005), 
                    new Vector2(graphics.PresentationParameters.BackBufferWidth/2f - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, 45f), Color.White);
            }
            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Hyperspace Flux", 
                    new Vector2(graphics.PresentationParameters.BackBufferWidth/2f - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, 45 + Fonts.Pirulen16.LineSpacing + 2), Color.Yellow);
            }
            if (IsActive && SavedGame.IsSaving)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Saving...", 
                    new Vector2(graphics.PresentationParameters.BackBufferWidth/2f - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, 45 + Fonts.Pirulen16.LineSpacing * 2 + 4), 
                    new Color(255, 255, 255, (float)Math.Abs(Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * 255f));
            }

            if (IsActive && (GameSpeed > 1.0f || GameSpeed < 1.0f))
            {
                string speed = GameSpeed.ToString("#.0") + "x";
                Vector2 speedTextPos = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - Fonts.Pirulen16.MeasureString(speed).X - 13f, 64f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, speed, speedTextPos, Color.White);
            }
            if (Debug)
            {
                var lines = new Array<string>();
                lines.Add("Comparisons:      " + GlobalStats.Comparisons);
                lines.Add("Dis Check Avg:    " + GlobalStats.DistanceCheckTotal / GlobalStats.ComparisonCounter);
                lines.Add("Modules Moved:    " + GlobalStats.ModulesMoved);
                lines.Add("Modules Updated:  " + GlobalStats.ModuleUpdates);
                lines.Add("Arc Checks:       " + GlobalStats.WeaponArcChecks);
                lines.Add("Beam Tests:       " + GlobalStats.BeamTests);
                lines.Add("Memory:           " + Memory);
                lines.Add("");
                lines.Add("Ship Count:       " + MasterShipList.Count);
                lines.Add("Ship Time:        " + Perfavg2);
                lines.Add("Empire Time:      " + EmpireUpdatePerf);
                lines.Add("PreEmpire Time:   " + PreEmpirePerf);
                lines.Add("Post Empire Time: " + perfavg4);
                lines.Add("");
                lines.Add("Total Time:       " + perfavg5);

                Vector2 pos = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 250f, 44f);
                DrawLines(pos, lines);
            }
            if (IsActive)
                ToolTip.Draw(ScreenManager);
            ScreenManager.SpriteBatch.End();

            // Notify ProcessTurns that Drawing has finished and while SwapBuffers is blocking,
            // the game logic can be updated
            DrawCompletedEvt.Set();
        }

        private void DrawLines(Vector2 position, Array<string> lines)
        {
            foreach (string line in lines)
            {
                if (line.Length != 0)
                    ScreenManager.SpriteBatch.DrawString(
                        Fonts.Arial12Bold, line, position, Color.White);
                position.Y += Fonts.Arial12Bold.LineSpacing + 2;
            }
        }

        private void DrawCircle(Vector2 worldPos, float radius, Color c, float thickness)
        {
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(worldPos.X, worldPos.Y, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector2 center = new Vector2(vector3_1.X, vector3_1.Y);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(worldPos.PointOnCircle(90f, radius), 0.0f), this.projection, this.view, Matrix.Identity);
            float radius1 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), center);
            Rectangle destinationRectangle = new Rectangle((int)center.X, (int)center.Y, (int)radius1 * 2, (int)radius1 * 2);
            Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node1"], destinationRectangle, new Rectangle?(), new Color(c, (byte)50), 0.0f, origin, SpriteEffects.None, 1f);
            Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, radius1, 50, c, thickness);
        }

        private void DrawFleetIcons(GameTime gameTime)
        {
            ClickableFleetsList.Clear();
            if (viewState < UnivScreenState.SectorView)
                return;

            foreach (Empire empire in EmpireManager.Empires)
            {
                foreach (var kv in empire.GetFleetsDict())
                {
                    if (kv.Value.Ships.Count <= 0)
                        continue;

                    Vector2 averagePosition = kv.Value.findAveragePositionset();
                    bool flag = player.IsPointInSensors(averagePosition);

                    if (flag || Debug || kv.Value.Owner == player)
                    {
                        var icon = ResourceManager.TextureDict["FleetIcons/" + kv.Value.FleetIconIndex];
                        Vector3 vector3_1 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(averagePosition, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                        foreach (Ship ship in kv.Value.Ships)
                        {
                            Vector3 vector3_2 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center.X, ship.Center.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(ScreenManager.SpriteBatch, new Vector2(vector3_2.X, vector3_2.Y), vector2, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)20));
                        }
                        ClickableFleetsList.Add(new ClickableFleet
                        {
                            fleet = kv.Value,
                            ScreenPos = vector2,
                            ClickRadius = 15f
                        });
                        ScreenManager.SpriteBatch.Draw(icon, vector2, new Rectangle?(), empire.EmpireColor, 0.0f, icon.Center(), 0.35f, SpriteEffects.None, 1f);
                        HelperFunctions.DrawDropShadowText(ScreenManager, kv.Value.Name, new Vector2(vector2.X + 10f, vector2.Y - 6f), Fonts.Arial8Bold);
                    }
                }
            }
        }

        private void DrawMinimap()
        {
            this.minimap.Draw(this.ScreenManager, this);
        }

        private void DrawTacticalPlanetIcons()
        {
            if (this.LookingAtPlanet || this.viewState <= UniverseScreen.UnivScreenState.SystemView || this.viewState >= UniverseScreen.UnivScreenState.GalaxyView)
                return;
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (solarSystem.ExploredDict[this.player])
                {
                    foreach (Planet planet in solarSystem.PlanetList)
                    {
                        float fIconScale = 0.1875f * (0.7f + ((float)(Math.Log(planet.scale))/2.75f));
                        if (planet.Owner != null)
                        {
                            Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position, 2500f), this.projection, this.view, Matrix.Identity);
                            Vector2 position = new Vector2(vector3.X, vector3.Y);
                            Rectangle rectangle = new Rectangle((int)position.X - 8, (int)position.Y - 8, 16, 16);
                            Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["Planets/" + (object)planet.planetType].Width / 2f), (float)(ResourceManager.TextureDict["Planets/" + (object)planet.planetType].Height / 2f));
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Planets/" + (object)planet.planetType], position, new Rectangle?(), Color.White, 0.0f, origin, fIconScale, SpriteEffects.None, 1f);
                            origin = new Vector2((float)(ResourceManager.FlagTextures[planet.Owner.data.Traits.FlagIndex].Value.Width / 2), (float)(ResourceManager.FlagTextures[planet.Owner.data.Traits.FlagIndex].Value.Height / 2));
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.FlagTextures[planet.Owner.data.Traits.FlagIndex].Value, position, new Rectangle?(), planet.Owner.EmpireColor, 0.0f, origin, 0.045f, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position, 2500f), this.projection, this.view, Matrix.Identity);
                            Vector2 position = new Vector2(vector3.X, vector3.Y);
                            Rectangle rectangle = new Rectangle((int)position.X - 8, (int)position.Y - 8, 16, 16);
                            Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["Planets/" + (object)planet.planetType].Width / 2), (float)(ResourceManager.TextureDict["Planets/" + (object)planet.planetType].Height / 2f));
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Planets/" + (object)planet.planetType], position, new Rectangle?(), Color.White, 0.0f, origin, fIconScale, SpriteEffects.None, 1f);
                        }
                    }
                }
            }
        }

        private void DrawOverlappingCirlcesLite(Array<Circle> CircleList, Color color, float Thickness)
        {
            foreach (Circle circle in CircleList)
                ;
        }

        private void DrawSelectedShipGroup(Array<Circle> CircleList, Color color, float Thickness)
        {
            for (int index = 0; index < CircleList.Count; ++index)
            {
                Rectangle rect = new Rectangle((int)CircleList[index].Center.X - (int)CircleList[index].Radius, (int)CircleList[index].Center.Y - (int)CircleList[index].Radius, (int)CircleList[index].Radius * 2, (int)CircleList[index].Radius * 2);
                if (index < CircleList.Count - 1)
                    Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, rect, new Color(color.R, color.G, color.B, (byte)100), 3);
                else
                    Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, rect, color, 3);
            }
        }

        public void DrawOverlappingCirlces(Array<Circle> CircleList, Color color, float Thickness)
        {
            BatchRemovalCollection<UniverseScreen.Intersection> removalCollection = new BatchRemovalCollection<UniverseScreen.Intersection>();
            foreach (Circle A in CircleList)
            {
                A.isChecked = true;
                bool flag = false;
                foreach (Circle B in CircleList)
                {
                    Vector2[] vector2Array = this.Get2Intersections(A, B);
                    if (vector2Array != null)
                    {
                        UniverseScreen.Intersection intersection1 = new UniverseScreen.Intersection();
                        intersection1.C1 = A;
                        intersection1.C2 = B;
                        intersection1.inter = vector2Array[0];
                        UniverseScreen.Intersection intersection2 = new UniverseScreen.Intersection();
                        intersection2.C1 = A;
                        intersection2.C2 = B;
                        intersection2.inter = vector2Array[1];
                        flag = true;
                        if (!B.isChecked)
                        {
                            removalCollection.Add(intersection1);
                            removalCollection.Add(intersection2);
                        }
                    }
                }
                if (!flag)
                    Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, A.Center, A.Radius, 100, color, Thickness);
            }
            int num1 = 0;
            foreach (UniverseScreen.Intersection intersection in (Array<UniverseScreen.Intersection>)removalCollection)
                ++num1;
            Array<Array<Vector2>> list1 = new Array<Array<Vector2>>();
            foreach (Circle circle in CircleList)
            {
                Array<UniverseScreen.Intersection> list2 = new Array<UniverseScreen.Intersection>();
                foreach (UniverseScreen.Intersection intersection in (Array<UniverseScreen.Intersection>)removalCollection)
                {
                    if (intersection.C1 == circle || intersection.C2 == circle)
                        list2.Add(intersection);
                }
                foreach (UniverseScreen.Intersection intersection in list2)
                {
                    float num2 = Math.Abs(circle.Center.AngleToTarget(intersection.inter)) - 90f;
                    if ((double)num2 < 0.0)
                        num2 += 360f;
                    intersection.Angle = num2;
                }
                IOrderedEnumerable<UniverseScreen.Intersection> orderedEnumerable = Enumerable.OrderBy<UniverseScreen.Intersection, float>((IEnumerable<UniverseScreen.Intersection>)list2, (Func<UniverseScreen.Intersection, float>)(inter => inter.Angle));
                for (int index = 0; index < Enumerable.Count<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable); ++index)
                {
                    Vector2[] vector2Array = HelperFunctions.CircleIntersection(Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).C1, Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).C2);
                    //Vector2 vector2_1 = new Vector2();
                    //Vector2 vector2_2 = new Vector2();
                    Vector2 C0;
                    Vector2 C1;
                    if (index < Enumerable.Count<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable) - 1)
                    {
                        C0 = Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).inter;
                        C1 = Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index + 1).inter;
                    }
                    else
                    {
                        C0 = Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).inter;
                        C1 = Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, 0).inter;
                    }
                    float num2 = MathHelper.ToDegrees((float)Math.Asin((double)Vector2.Distance(C0, C1) / 2.0 / (double)circle.Radius) * 2f);
                    //float num3 = 0.0f;
                    if (float.IsNaN((double)Vector2.Distance(Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).C1.Center, Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).C2.Center) >= (double)vector2Array[2].Y ? 360f - num2 : num2))
                        //num3 = 180f;
                    Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).AngularDistance = 360f;
                    Array<Vector2> myArc = Primitives2D.CreateMyArc(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 50, Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).Angle, Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).AngularDistance, C0, C1, color, Thickness, CircleList);
                    list1.Add(myArc);
                }
            }
            Array<Vector2> list3 = new Array<Vector2>();
            foreach (Array<Vector2> list2 in list1)
                list3.AddRange((IEnumerable<Vector2>)list2);
            foreach (Vector2 center in list3)
                Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, 2f, 10, color, 2f);
        }
        private Vector2[] Get2Intersections(Circle a, Circle b)
        {
            Vector2[] vector2Array = HelperFunctions.CircleIntersection(a, b);
            if (vector2Array == null)
                return null;
            return new []
            {
                new Vector2(vector2Array[0].X, vector2Array[0].Y),
                new Vector2(vector2Array[1].X, vector2Array[1].Y)
            };
        }

        private void DrawCircleConnections(Circle A, Array<Circle> Circles)
        {
            A.isChecked = true;
            foreach (Circle B in Circles)
            {
                if (!B.isChecked)
                {
                    Vector2[] vector2Array = HelperFunctions.CircleIntersection(A, B);
                    if (vector2Array != null)
                    {
                        Vector2 vector2_1 = new Vector2(vector2Array[0].X, vector2Array[0].Y);
                        Vector2 vector2_2 = new Vector2(vector2Array[1].X, vector2Array[1].Y);
                        float num1 = Vector2.Distance(vector2_1, vector2_2);
                        float num2 = MathHelper.ToDegrees((float)Math.Asin((double)num1 / 2.0 / (double)A.Radius) * 2f);
                        float degrees1 = (double)Vector2.Distance(B.Center, A.Center) >= (double)vector2Array[2].Y ? 360f - num2 : num2;
                        float startingAngle1 = Math.Abs(A.Center.AngleToTarget(vector2_2)) - 90f;
                        Primitives2D.DrawMyArc(this.ScreenManager.SpriteBatch, A.Center, A.Radius, 50, startingAngle1, degrees1, vector2_1, vector2_2, Color.Red, 3f, Circles);
                        float num3 = MathHelper.ToDegrees((float)Math.Asin((double)num1 / 2.0 / (double)B.Radius) * 2f);
                        float startingAngle2 = Math.Abs(B.Center.AngleToTarget(vector2_1)) - 90f;
                        float degrees2 = (double)Vector2.Distance(B.Center, A.Center) >= (double)vector2Array[2].X ? 360f - num3 : num3;
                        Primitives2D.DrawMyArc(this.ScreenManager.SpriteBatch, B.Center, B.Radius, 50, startingAngle2, degrees2, vector2_2, vector2_1, Color.Red, 3f, Circles);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, "C0", vector2_1, Color.White);
                        this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, "C1", vector2_2, Color.White);
                    }
                }
            }
        }

        private void DrawPlanetInfoForUI()
        {
            this.stuffSelector = new Selector(this.ScreenManager, this.SelectedStuffRect, new Color((byte)0, (byte)0, (byte)0, (byte)80));
            Planet planet = this.SelectedPlanet;
            if (planet.Owner != null)
                planet.UpdateIncomes(false);
            this.stuffSelector.Draw();
            if (this.SelectedPlanet.ExploredDict[this.player])
            {
                Rectangle destinationRectangle1 = new Rectangle(this.SelectedStuffRect.X + 30, this.SelectedStuffRect.Y + 60, this.SelectedStuffRect.Height - 80, this.SelectedStuffRect.Height - 80);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Planets/" + (object)planet.planetType], destinationRectangle1, Color.White);
                Vector2 position1 = new Vector2((float)(this.stuffSelector.Menu.X + 40), (float)(this.stuffSelector.Menu.Y + 20));
                if (planet.Owner != null)
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.SelectedPlanet.Name + " - " + planet.Owner.data.Traits.Name, position1, new Color(byte.MaxValue, (byte)239, (byte)208));
                else
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.SelectedPlanet.Name, position1, new Color(byte.MaxValue, (byte)239, (byte)208));
                position1.X += 135f;
                position1.Y += (float)(Fonts.Arial20Bold.LineSpacing + 5);
                string format = "0.#";
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(384) + ":", position1, Color.Orange);
                Vector2 position2 = new Vector2(position1.X + 80f, position1.Y);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, planet.GetTypeTranslation(), position2, new Color(byte.MaxValue, (byte)239, (byte)208));
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                position2 = new Vector2(position1.X + 80f, position1.Y);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(385) + ":", position1, Color.Orange);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (planet.Population / 1000f).ToString(format) + " / " + ((float)(((double)planet.MaxPopulation + (double)planet.MaxPopBonus) / 1000.0)).ToString(format), position2, new Color(byte.MaxValue, (byte)239, (byte)208));
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                position2 = new Vector2(position1.X + 80f, position1.Y);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(386) + ":", position1, Color.Orange);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, planet.Fertility.ToString(format), position2, new Color(byte.MaxValue, (byte)239, (byte)208));
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                position2 = new Vector2(position1.X + 80f, position1.Y);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(387) + ":", position1, Color.Orange);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, planet.MineralRichness.ToString(format), position2, new Color(byte.MaxValue, (byte)239, (byte)208));
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                Vector2 position3 = position1;
                if (planet.Owner == null)
                    return;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, (planet.NetFoodPerTurn - planet.consumption).ToString(UniverseScreen.fmt2), position3, (double)planet.NetFoodPerTurn - (double)planet.consumption > 0.0 ? new Color(byte.MaxValue, (byte)239, (byte)208) : Color.LightPink);
                position3.X += Fonts.Arial20Bold.MeasureString((planet.NetFoodPerTurn - planet.consumption).ToString(UniverseScreen.fmt2)).X + 4f;
                Rectangle destinationRectangle2 = new Rectangle((int)position3.X, (int)position3.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], destinationRectangle2, Color.White);
                position3.X += (float)ResourceManager.TextureDict["NewUI/icon_food"].Width;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, " / ", position3, Color.Gray);
                position3.X += Fonts.Arial20Bold.MeasureString(" / ").X;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, planet.Owner.data.Traits.Cybernetic > 0 ? (planet.NetProductionPerTurn - planet.consumption).ToString(UniverseScreen.fmt2) : planet.NetProductionPerTurn.ToString(UniverseScreen.fmt2), position3, planet.Owner.data.Traits.Cybernetic > 0 ? ((double)planet.NetProductionPerTurn - (double)planet.consumption > 0.0 ? new Color(byte.MaxValue, (byte)239, (byte)208) : Color.LightPink) : new Color(byte.MaxValue, (byte)239, (byte)208));
                position3.X += Fonts.Arial20Bold.MeasureString(planet.Owner.data.Traits.Cybernetic > 0 ? (planet.NetProductionPerTurn - planet.consumption).ToString(UniverseScreen.fmt2) : planet.NetProductionPerTurn.ToString(UniverseScreen.fmt2)).X + 4f;
                Rectangle destinationRectangle3 = new Rectangle((int)position3.X, (int)position3.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], destinationRectangle3, Color.White);
                position3.X += (float)ResourceManager.TextureDict["NewUI/icon_food"].Width;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, " / ", position3, Color.Gray);
                position3.X += Fonts.Arial20Bold.MeasureString(" / ").X;
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, planet.NetResearchPerTurn.ToString(UniverseScreen.fmt2), position3, new Color(byte.MaxValue, (byte)239, (byte)208));
                position3.X += Fonts.Arial20Bold.MeasureString(planet.NetResearchPerTurn.ToString(UniverseScreen.fmt2)).X + 4f;
                Rectangle destinationRectangle4 = new Rectangle((int)position3.X, (int)position3.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], destinationRectangle4, Color.White);
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                position1.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                Vector2 vector2 = position1;
                if (planet.ConstructionQueue.Count <= 0)
                    return;
                QueueItem queueItem = planet.ConstructionQueue[0];
                if (queueItem.isBuilding)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + queueItem.Building.Icon + "_48x48"], new Rectangle((int)vector2.X, (int)vector2.Y, 29, 30), Color.White);
                    Vector2 position4 = new Vector2(vector2.X + 40f, vector2.Y);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, queueItem.Building.Name, position4, Color.White);
                    position4.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    new ProgressBar(new Rectangle((int)position4.X, (int)position4.Y, 150, 18))
                    {
                        Max = queueItem.Cost,
                        Progress = queueItem.productionTowards
                    }.Draw(this.ScreenManager.SpriteBatch);
                }
                if (queueItem.isShip)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Icons/icon_structure_placeholder"], new Rectangle((int)vector2.X, (int)vector2.Y, 29, 30), Color.White);
                    Vector2 position4 = new Vector2(vector2.X + 40f, vector2.Y);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, queueItem.sData.Name, position4, Color.White);
                    position4.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    new ProgressBar(new Rectangle((int)position4.X, (int)position4.Y, 150, 18))
                    {
                        Max = queueItem.Cost,
                        Progress = queueItem.productionTowards
                    }.Draw(this.ScreenManager.SpriteBatch);
                }
                if (!queueItem.isTroop)
                    return;
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Troops/" + queueItem.troop.TexturePath], new Rectangle((int)vector2.X, (int)vector2.Y, 29, 30), Color.White);
                Vector2 position5 = new Vector2(vector2.X + 40f, vector2.Y);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, queueItem.troop.Name, position5, Color.White);
                position5.Y += (float)Fonts.Arial12Bold.LineSpacing;
                new ProgressBar(new Rectangle((int)position5.X, (int)position5.Y, 150, 18))
                {
                    Max = queueItem.Cost,
                    Progress = queueItem.productionTowards
                }.Draw(this.ScreenManager.SpriteBatch);
            }
            else
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(1408), new Vector2((float)(this.stuffSelector.Menu.X + 40), (float)(this.stuffSelector.Menu.Y + 20)), new Color(byte.MaxValue, (byte)239, (byte)208));
        }

        private void DrawItemInfoForUI()
        {
            if (this.SelectedItem == null || this.SelectedItem == null)
                return;
            Circle circle = this.DrawSelectionCircles(this.SelectedItem.AssociatedGoal.BuildPosition, 50f);
            Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 50, this.SelectedItem.AssociatedGoal.empire.EmpireColor);
        }

        protected void DrawShipUI(GameTime gameTime)
        {
            Vector2 vector2 = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
            lock (GlobalStats.FleetButtonLocker)
            {
                foreach (UniverseScreen.FleetButton item_0 in this.FleetButtons)
                {
                    Selector local_1 = new Selector(this.ScreenManager, item_0.ClickRect, Color.TransparentBlack);
                    Rectangle local_2 = new Rectangle(item_0.ClickRect.X + 6, item_0.ClickRect.Y + 6, item_0.ClickRect.Width - 12, item_0.ClickRect.Width - 12);
                    bool local_3 = false;
                    for (int local_4 = 0; local_4 < item_0.Fleet.Ships.Count; ++local_4)
                    {
                        try
                        {
                            if (item_0.Fleet.Ships[local_4].InCombat)
                            {
                                local_3 = true;
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    float local_6_1 = Math.Abs((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * 200f;
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/rounded_square"], item_0.ClickRect, local_3 ? new Color(byte.MaxValue, (byte)0, (byte)0, (byte)local_6_1) : new Color((byte)0, (byte)0, (byte)0, (byte)80));
                    local_1.Draw();
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["FleetIcons/" + item_0.Fleet.FleetIconIndex.ToString()], local_2, EmpireManager.Player.EmpireColor);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, item_0.Key.ToString(), new Vector2((float)(item_0.ClickRect.X + 4), (float)(item_0.ClickRect.Y + 4)), Color.Orange);
                    Vector2 local_7 = new Vector2((float)(item_0.ClickRect.X + 50), (float)item_0.ClickRect.Y);
                    for (int local_8 = 0; local_8 < item_0.Fleet.Ships.Count; ++local_8)
                    {
                        try
                        {
                            Ship local_9 = item_0.Fleet.Ships[local_8];
                            
                            Rectangle local_10 = new Rectangle((int)local_7.X, (int)local_7.Y, 15, 15);
                            local_7.X += (float)(15 );
                            if ((double)local_7.X > 200.0)
                            {
                                local_7.X = (float)(item_0.ClickRect.X + 50);
                                local_7.Y += 15f;
                            }
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + (local_9.isConstructor ? "construction" : local_9.shipData.GetRole())], local_10, item_0.Fleet.Owner.EmpireColor);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        protected void DrawShipsInRange()
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            //lock (GlobalStats.KnownShipsLock)
            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active)
                        MasterShipList.QueuePendingRemoval(ship);
                    else
                        DrawInRange(ship);
                }
            }
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;

            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active)
                        continue;
                    this.DrawTacticalIcons(ship);
                    this.DrawOverlay(ship);
                    if ((this.SelectedShipList.Contains(ship) || this.SelectedShip == ship) && HelperFunctions.CheckIntersection(this.ScreenRectangle, ship.ScreenPosition))
                    {
                        //Color local_3 = new Color();
                        Relationship rel;
                        if (player.TryGetRelations(ship.loyalty, out rel))
                        {
                            Color local_3_2 = rel.AtWar || ship.loyalty.isFaction ? Color.Red : Color.Gray;
                            Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, ship.ScreenPosition, ship.ScreenRadius, local_3_2);
                        }
                        else
                        {
                            Color local_3_1 = Color.LightGreen;
                            Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, ship.ScreenPosition, ship.ScreenRadius, local_3_1);
                        }
                    }
                }
            }
            if (this.ProjectingPosition)
                this.DrawProjectedGroup();
            lock (GlobalStats.ClickableItemLocker)
            {
                for (int i = 0; i < this.ItemsToBuild.Count; ++i)
                {
                    UniverseScreen.ClickableItemUnderConstruction item = this.ItemsToBuild[i];
                    if (ResourceManager.ShipsDict.ContainsKey(item.UID))
                    {
                        float local_6 = (float)(ResourceManager.ShipsDict[item.UID].Size / ResourceManager.TextureDict["TacticalIcons/symbol_platform"].Width);
                        Vector2 local_7 = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_platform"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_platform"].Width / 2));
                        float local_6_1 = local_6 * 4000f / this.camHeight;
                        float local_6_2 = 0.07f;
                        Vector3 local_8 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item.BuildPos, 0.0f), this.projection, this.view, Matrix.Identity);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_platform"], new Vector2(local_8.X, local_8.Y), new Rectangle?(), new Color((byte)0, byte.MaxValue, (byte)0, (byte)100), 0.0f, local_7, local_6_2, SpriteEffects.None, 1f);
                        if (this.showingDSBW)
                        {
                            if (item.UID == "Subspace Projector")
                            {
                                Vector3 local_10 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item.AssociatedGoal.BuildPosition + new Vector2(Empire.ProjectorRadius, 0.0f), 0.0f), this.projection, this.view, Matrix.Identity);
                                float local_11 = Vector2.Distance(new Vector2(local_8.X, local_8.Y), new Vector2(local_10.X, local_10.Y));
                                Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, new Vector2(local_8.X, local_8.Y), local_11, 50, Color.Orange, 2f);
                            }
                            else if ((double)ResourceManager.ShipsDict[item.UID].SensorRange > 0.0)
                            {
                                Vector3 local_13 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item.AssociatedGoal.BuildPosition + new Vector2(ResourceManager.ShipsDict[item.UID].SensorRange, 0.0f), 0.0f), this.projection, this.view, Matrix.Identity);
                                float local_14 = Vector2.Distance(new Vector2(local_8.X, local_8.Y), new Vector2(local_13.X, local_13.Y));
                                Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, new Vector2(local_8.X, local_8.Y), local_14, 50, Color.Blue, 1f);
                            }
                        }
                    }
                }
            }
            if (!this.showingDSBW || this.dsbw.itemToBuild == null || (!(this.dsbw.itemToBuild.Name == "Subspace Projector") || (double)this.AdjustCamTimer > 0.0))
                return;
            Vector2 center = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
            Vector2 vector2 = Vector2.Zero + new Vector2(Empire.ProjectorRadius, 0.0f);
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(Vector2.Zero, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(vector2, 0.0f), this.projection, this.view, Matrix.Identity);
            float num = Vector2.Distance(new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y));
            Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, MathHelper.SmoothStep(this.radlast, num, 0.01f), 50, Color.Orange, 2f);
            this.radlast = num;
        }

        protected void DrawProjectedGroup()
        {
            if (this.projectedGroup == null)
                return;
            foreach (Ship ship in (Array<Ship>)this.projectedGroup.Ships)
            {
                Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.projectedPosition.X, ship.projectedPosition.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 position = new Vector2(vector3.X, vector3.Y);
                Rectangle rectangle = new Rectangle((int)vector3.X, (int)vector3.Y, ship.Size / 2, ship.Size / 2);

                var symbolFighter = ResourceManager.TextureDict["TacticalIcons/symbol_fighter"];
                Vector2 origin = new Vector2(symbolFighter.Width / 2f);
                if (ship.Active)
                {
                    float num = ship.Size / (30f + symbolFighter.Width);
                    float scale = num * 4000f / camHeight;
                    if (scale > 1.0) scale = 1f;
                    else if (scale < 0.100000001490116)
                        scale = ship.shipData.Role != ShipData.RoleName.platform || viewState < UnivScreenState.SectorView ? 0.15f : 0.08f;
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + (ship.isConstructor ? "construction" : ship.shipData.GetRole())], position, new Rectangle?(), new Color((byte)0, byte.MaxValue, (byte)0, (byte)100), this.projectedGroup.ProjectedFacing, origin, scale, SpriteEffects.None, 1f);
                }
            }
        }

        private void DrawOverlay(Ship ship)
        {
            if (LookingAtPlanet || viewState > UnivScreenState.SystemView || (!ShowShipNames || ship.dying) || !ship.InFrustum)
                return;
            var symbolFighter = ResourceManager.TextureDict["TacticalIcons/symbol_fighter"];
            foreach (ModuleSlot moduleSlot in ship.ModuleSlotList)
            {
                Vector2 vector2_2 = new Vector2(moduleSlot.module.Center.X, moduleSlot.module.Center.Y);
                float scale = 0.75f * (float)this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / this.camHeight;
                Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(vector2_2, 0.0f), this.projection, this.view, Matrix.Identity);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], new Vector2(vector3.X, vector3.Y), new Rectangle?(), Color.White, ship.Rotation, new Vector2(8f, 8f), scale, SpriteEffects.None, 1f);
            }
            bool flag = false; // @todo What debug flag is this?
            if (flag)
            {
                foreach (Projectile projectile in ship.Projectiles)
                {
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(projectile.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 center = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector2 vector2_2 = projectile.Center + new Vector2(8f, 0.0f);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(vector2_2.X, vector2_2.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    float radius = Vector2.Distance(center, new Vector2(vector3_2.X, vector3_2.Y));
                    Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, radius, 50, Color.Red, 3f);
                }
            }

            foreach (ModuleSlot moduleSlot in ship.ModuleSlotList)
            {
                Viewport viewport;
                if (camHeight > 6000.0)
                {
                    string index = "TacticalIcons/symbol_fighter";
                    viewport = this.ScreenManager.GraphicsDevice.Viewport;
                    Vector3 vector3_1 = viewport.Project(new Vector3(moduleSlot.module.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 origin1 = new Vector2((float)(symbolFighter.Width / 2), (float)(symbolFighter.Height / 2));
                    float num1 = moduleSlot.module.Health / moduleSlot.module.HealthMax;
                    float scale = 500f / this.camHeight;
                    var tex = ResourceManager.TextureDict[index];

                    var color = Color.Black;
                    if (Debug && moduleSlot.module.isExternal) color = Color.Blue;
                    else if (num1 >= 0.899999976158142) color = Color.Green;
                    else if (num1 >= 0.649999976158142) color = Color.GreenYellow;
                    else if (num1 >= 0.449999988079071) color = Color.Yellow;
                    else if (num1 >= 0.150000005960464) color = Color.OrangeRed;
                    else if (num1 < 0.0 && num1 <= 0.150000005960464) color = Color.Red;
                    else color = Color.Black;

                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), 
                                                   null, color, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);

                    if (ship.isPlayerShip() && (double)moduleSlot.module.FieldOfFire != 0.0 && moduleSlot.module.InstalledWeapon != null)
                    {
                        float num2 = moduleSlot.module.FieldOfFire / 2f;
                        Vector2 angleAndDistance1 = moduleSlot.module.Center.PointFromAngle((float)((double)MathHelper.ToDegrees(ship.Rotation) + (double)moduleSlot.module.facing + -(double)num2), moduleSlot.module.InstalledWeapon.Range);
                        Vector2 angleAndDistance2 = moduleSlot.module.Center.PointFromAngle(MathHelper.ToDegrees(ship.Rotation) + moduleSlot.module.facing + num2, moduleSlot.module.InstalledWeapon.Range);
                        viewport = this.ScreenManager.GraphicsDevice.Viewport;
                        Vector3 vector3_2 = viewport.Project(new Vector3(angleAndDistance1, 0.0f), this.projection, this.view, Matrix.Identity);
                        viewport = this.ScreenManager.GraphicsDevice.Viewport;
                        Vector3 vector3_3 = viewport.Project(new Vector3(angleAndDistance2, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 point1 = new Vector2(vector3_1.X, vector3_1.Y);
                        Vector2 point2_1 = new Vector2(vector3_2.X, vector3_2.Y);
                        Vector2 point2_2 = new Vector2(vector3_3.X, vector3_3.Y);
                        float num3 = Vector2.Distance(point1, point2_1);
                        Color color1 = new Color(byte.MaxValue, (byte)165, (byte)0, (byte)100);
                        Vector2 origin2 = new Vector2(250f, 250f);
                        if (moduleSlot.module.InstalledWeapon.WeaponType == "Flak" || moduleSlot.module.InstalledWeapon.WeaponType == "Vulcan")
                        {
                            Color color2 = new Color(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num3 * 2, (int)num3 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, moduleSlot.module.facing.ToRadians() + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "Laser" || moduleSlot.module.InstalledWeapon.WeaponType == "HeavyLaser")
                        {
                            Color color2 = new Color(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num3 * 2, (int)num3 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, moduleSlot.module.facing.ToRadians() + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "PhotonCannon")
                        {
                            Color color2 = new Color((byte)0, (byte)0, byte.MaxValue, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num3 * 2, (int)num3 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, moduleSlot.module.facing.ToRadians() + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else
                        {
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, point1, point2_1, new Color(byte.MaxValue, (byte)0, (byte)0, (byte)75), 1f);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, point1, point2_2, new Color(byte.MaxValue, (byte)0, (byte)0, (byte)75), 1f);
                        }
                    }
                }
                else if (this.Debug)
                {
                    if (moduleSlot.module.isExternal && moduleSlot.module.Active)
                    {
                        string index = "TacticalIcons/symbol_fighter";
                        viewport = this.ScreenManager.GraphicsDevice.Viewport;
                        Vector3 vector3 = viewport.Project(new Vector3(moduleSlot.module.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Height / 2));
                        float scale = 500f / this.camHeight;
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3.X, vector3.Y), new Rectangle?(), Color.Blue, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
                    }
                }
                else if (!moduleSlot.module.isDummy)
                {
                    byte num1 = moduleSlot.module.XSIZE;
                    byte num2 = moduleSlot.module.YSIZE;
                    float num3 = 0.0f;
                    if (moduleSlot.state == ShipDesignScreen.ActiveModuleState.Left)
                        num3 = 4.712389f;
                    else if (moduleSlot.state == ShipDesignScreen.ActiveModuleState.Right)
                        num3 = 1.570796f;
                    else if (moduleSlot.state == ShipDesignScreen.ActiveModuleState.Rear)
                        num3 = 3.141593f;
                    Vector2 vector2_2 = (int)moduleSlot.module.XSIZE != 1 || (int)moduleSlot.module.YSIZE != 3 ? ((int)moduleSlot.module.XSIZE != 2 || (int)moduleSlot.module.YSIZE != 5 ? new Vector2(moduleSlot.module.Center.X - 8f + (float)(16 * (int)num1 / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)num2 / 2)) : new Vector2(moduleSlot.module.Center.X - 80f + (float)(16 * (int)num1 / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)num2 / 2))) : new Vector2(moduleSlot.module.Center.X - 50f + (float)(16 * (int)num1 / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)num2 / 2));
                    Vector2 target = new Vector2(moduleSlot.module.Center.X - 8f, moduleSlot.module.Center.Y - 8f);

                    float angleToTarget = vector2_2.AngleToTargetSigned(target);
                    Vector2 angleAndDistance1 = moduleSlot.module.Center.PointFromAngle(
                        MathHelper.ToDegrees(ship.Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));

                    float num4 = (float)((int)num1 * 16 / 2);
                    float num5 = (float)((int)num2 * 16 / 2);
                    float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num4, 2.0) + (float)Math.Pow((double)num5, 2.0)));
                    float radians = 3.141593f - (float)Math.Asin((double)num4 / (double)distance) + ship.Rotation;
                    vector2_2 = MathExt.PointFromAngle(angleAndDistance1, MathHelper.ToDegrees(radians), distance);
                    viewport = this.ScreenManager.GraphicsDevice.Viewport;
                    Vector3 vector3_1 = viewport.Project(new Vector3(vector2_2, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 origin1 = new Vector2((float)(ResourceManager.TextureDict[ResourceManager.ShipModulesDict[moduleSlot.module.UID].IconTexturePath].Width / 2), (float)(ResourceManager.TextureDict[ResourceManager.ShipModulesDict[moduleSlot.module.UID].IconTexturePath].Height / 2));
                    float num6 = moduleSlot.module.Health / moduleSlot.module.HealthMax;
                    string index1 = ResourceManager.ShipModulesDict[moduleSlot.module.UID].IconTexturePath;
                    float scale1 = 0.75f * (float)this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / this.camHeight / (float)(ResourceManager.TextureDict[ResourceManager.ShipModulesDict[moduleSlot.module.UID].IconTexturePath].Width / ((int)ResourceManager.ShipModulesDict[moduleSlot.module.UID].XSIZE * 16));
                    if (moduleSlot.module.ModuleType == ShipModuleType.PowerConduit)
                    {
                        origin1 = new Vector2((float)(ResourceManager.TextureDict[moduleSlot.module.IconTexturePath].Width / 2), (float)(ResourceManager.TextureDict[moduleSlot.module.IconTexturePath].Width / 2));
                        float num7 = (float)(ResourceManager.TextureDict[moduleSlot.module.IconTexturePath].Width / 16);
                        float scale2 = 0.75f * (float)this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / this.camHeight / num7;
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[moduleSlot.module.IconTexturePath], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.White, ship.Rotation, origin1, scale2, SpriteEffects.None, 1f);
                        if (moduleSlot.module.Powered)
                        {
                            string index2 = moduleSlot.module.IconTexturePath + "_power";
                            float scale3 = 0.75f * (float)this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / this.camHeight / num7;
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index2], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.White, ship.Rotation, origin1, scale3, SpriteEffects.None, 1f);
                        }
                    }
                    else
                    {
                        if ((double)num6 >= 0.899999976158142)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index1], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.White, ship.Rotation + num3, origin1, scale1, SpriteEffects.None, 1f);
                        else if ((double)num6 >= 0.649999976158142)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index1], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.GreenYellow, ship.Rotation + num3, origin1, scale1, SpriteEffects.None, 1f);
                        else if ((double)num6 >= 0.449999988079071)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index1], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Yellow, ship.Rotation + num3, origin1, scale1, SpriteEffects.None, 1f);
                        else if ((double)num6 >= 0.150000005960464)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index1], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.OrangeRed, ship.Rotation + num3, origin1, scale1, SpriteEffects.None, 1f);
                        else if ((double)num6 <= 0.150000005960464 && (double)num6 > 0.0)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index1], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Red, ship.Rotation + num3, origin1, scale1, SpriteEffects.None, 1f);
                        else
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index1], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Black, ship.Rotation + num3, origin1, scale1, SpriteEffects.None, 1f);
                        if (flag)
                        {
                            Vector2 center = new Vector2(vector3_1.X, vector3_1.Y);
                            Vector2 vector2_3 = vector2_2 + new Vector2(8f, 0.0f);
                            viewport = this.ScreenManager.GraphicsDevice.Viewport;
                            Vector3 vector3_2 = viewport.Project(new Vector3(vector2_3.X, vector2_3.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                            float radius = Vector2.Distance(center, new Vector2(vector3_2.X, vector3_2.Y));
                            Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, radius, 50, Color.Red, 3f);
                        }
                    }
                    if (ship.isPlayerShip() && (double)moduleSlot.module.FieldOfFire != 0.0 && moduleSlot.module.InstalledWeapon != null)
                    {
                        float num7 = moduleSlot.module.FieldOfFire / 2f;
                        Vector2 angleAndDistance2 = vector2_2.PointFromAngle((float)((double)MathHelper.ToDegrees(ship.Rotation) + (double)moduleSlot.module.facing + -(double)num7), moduleSlot.module.InstalledWeapon.Range);
                        Vector2 angleAndDistance3 = vector2_2.PointFromAngle(MathHelper.ToDegrees(ship.Rotation) + moduleSlot.module.facing + num7, moduleSlot.module.InstalledWeapon.Range);
                        viewport = this.ScreenManager.GraphicsDevice.Viewport;
                        Vector3 vector3_2 = viewport.Project(new Vector3(angleAndDistance2, 0.0f), this.projection, this.view, Matrix.Identity);
                        viewport = this.ScreenManager.GraphicsDevice.Viewport;
                        Vector3 vector3_3 = viewport.Project(new Vector3(angleAndDistance3, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 point1 = new Vector2(vector3_1.X, vector3_1.Y);
                        Vector2 point2_1 = new Vector2(vector3_2.X, vector3_2.Y);
                        Vector2 point2_2 = new Vector2(vector3_3.X, vector3_3.Y);
                        float num8 = Vector2.Distance(point1, point2_1);
                        Color color1 = new Color(byte.MaxValue, (byte)165, (byte)0, (byte)100);
                        Vector2 origin2 = new Vector2(250f, 250f);
                        if (moduleSlot.module.InstalledWeapon.WeaponType == "Flak" || moduleSlot.module.InstalledWeapon.WeaponType == "Vulcan")
                        {
                            Color color2 = new Color(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num8 * 2, (int)num8 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, moduleSlot.module.facing.ToRadians() + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "Laser" || moduleSlot.module.InstalledWeapon.WeaponType == "HeavyLaser")
                        {
                            Color color2 = new Color(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num8 * 2, (int)num8 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, moduleSlot.module.facing.ToRadians() + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "PhotonCannon")
                        {
                            Color color2 = new Color((byte)0, (byte)0, byte.MaxValue, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num8 * 2, (int)num8 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, moduleSlot.module.facing.ToRadians() + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else
                        {
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, point1, point2_1, new Color(byte.MaxValue, (byte)0, (byte)0, (byte)75), 1f);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, point1, point2_2, new Color(byte.MaxValue, (byte)0, (byte)0, (byte)75), 1f);
                        }
                    }
                    if (!moduleSlot.module.Powered && (double)moduleSlot.module.PowerDraw > 0.0 && moduleSlot.module.ModuleType != ShipModuleType.PowerConduit)
                    {
                        Vector2 origin2 = new Vector2(8f, 8f);
                        float scale2 = 1250f / this.camHeight;
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/lightningBolt"], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.White, 0.0f, origin2, scale2, SpriteEffects.None, 1f);
                    }
                }
            }
        }

        private void DrawTacticalIcons(Ship ship)
        {
            if (this.LookingAtPlanet || (!this.showingFTLOverlay && ship.IsPlatform && this.viewState == UniverseScreen.UnivScreenState.GalaxyView))
                return;
            if (this.showingFTLOverlay && ship.IsPlatform && ship.Name != "Subspace Projector")
            {
                return;
            }
            if(string.IsNullOrEmpty(ship.StrategicIconPath))
            {
                ship.StrategicIconPath = string.Intern("TacticalIcons/symbol_" + (ship.isConstructor ? "construction" : ship.shipData.GetRole()));
            }
            if (this.viewState == UniverseScreen.UnivScreenState.GalaxyView)
            {
                float num1 = ship.GetSO().WorldBoundingSphere.Radius;
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 position = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(ship.Center.X + num1, ship.Center.Y), 0.0f), this.projection, this.view, Matrix.Identity);
                float num2 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), position);
                if ((double)num2 < 5.0)
                    num2 = 5f;
                float scale = num2 / (float)(45 - GlobalStats.IconSize);
                Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
                bool flag = true;
                foreach (UniverseScreen.ClickableFleet clickableFleet in this.ClickableFleetsList)
                {
                    if (clickableFleet.fleet == ship.fleet && (double)Vector2.Distance(position, clickableFleet.ScreenPos) < 20.0)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!ship.Active || !flag || !ResourceManager.TextureDict.ContainsKey(ship.StrategicIconPath))
                    return;
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ship.StrategicIconPath], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
            }
            else if ((this.ShowTacticalCloseup || this.viewState > UniverseScreen.UnivScreenState.ShipView) && !this.LookingAtPlanet)
            {
                float num1 = ship.GetSO().WorldBoundingSphere.Radius;
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 position = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(ship.Center.X + num1, ship.Center.Y), 0.0f), this.projection, this.view, Matrix.Identity);
                float num2 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), position);
                if ((double)num2 < 5.0)
                    num2 = 5f;
                float scale = num2 / (float)(45- GlobalStats.IconSize); //45
                Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
                bool flag = true;
                foreach (UniverseScreen.ClickableFleet clickableFleet in this.ClickableFleetsList)
                {
                    if (clickableFleet.fleet == ship.fleet && (double)Vector2.Distance(position, clickableFleet.ScreenPos) < (double)num2 + 3.0)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!ship.Active || !flag)
                    return;
                if (ship.isColonyShip)
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/flagicon"], position + new Vector2(-7f, -17f), ship.loyalty.EmpireColor);
                //Added by McShooterz: Make Fighter tactical symbol default if not found
                if (ResourceManager.TextureDict.ContainsKey(ship.StrategicIconPath))
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ship.StrategicIconPath], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
                else
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
            }
            //else if (this.viewState == UniverseScreen.UnivScreenState.ShipView)
            //{
            //    float num1 = ship.GetSO().WorldBoundingSphere.Radius;
            //    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), this.projection, this.view, Matrix.Identity);
            //    Vector2 position = new Vector2(vector3_1.X, vector3_1.Y);
            //    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(ship.Center.X + num1, ship.Center.Y), 0.0f), this.projection, this.view, Matrix.Identity);
            //    float num2 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), position);
            //    if ((double)num2 < 5.0)
            //        num2 = 5f;
            //    float scale = num2 / (float)(45 - GlobalStats.IconSize); //45
            //    scale *= .2f;
            //    Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
            //    bool flag = true;
            //    foreach (UniverseScreen.ClickableFleet clickableFleet in this.ClickableFleetsList)
            //    {
            //        if (clickableFleet.fleet == ship.fleet && (double)Vector2.Distance(position, clickableFleet.ScreenPos) < (double)num2 + 3.0)
            //        {
            //            flag = false;
            //            break;
            //        }
            //    }
            //    if (!ship.Active || !flag)
            //        return;
            //    if (ship.isColonyShip)
            //        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/flagicon"], position + new Vector2(-7f, -17f), ship.loyalty.EmpireColor);
            //    //Added by McShooterz: Make Fighter tactical symbol default if not found
            //    if (ResourceManager.TextureDict.ContainsKey("TacticalIcons/symbol_" + ship.Role))
            //        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + ship.Role], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
            //    else
            //        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
            //}
            else
            {
                if (this.viewState != UniverseScreen.UnivScreenState.ShipView || this.LookingAtPlanet)
                    return;
                float num1 = ship.GetSO().WorldBoundingSphere.Radius;
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 vector2_1 = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(ship.Center.X + num1, ship.Center.Y), 0.0f), this.projection, this.view, Matrix.Identity);
                float num2 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2_1);
                float scale2 = num2 / (float)(45 - GlobalStats.IconSize); 
                if ((double)num2 < 5.0)
                    num2 = 5f;
                float scale = num2 / (float)(45 - GlobalStats.IconSize); //45
                float check = this.GetZfromScreenState(UniverseScreen.UnivScreenState.ShipView);
                if (ship.shipData.Role != ShipData.RoleName.fighter && ship.shipData.Role != ShipData.RoleName.scout)
                {
                    
                    scale2 *= (this.camHeight / (check + (check*2 -camHeight*2)));
                }
                else
                {
                   // scale2 *= this.camHeight * 2 > this.GetZfromScreenState(UniverseScreen.UnivScreenState.ShipView) ? 1 : this.camHeight * 2 / this.GetZfromScreenState(UniverseScreen.UnivScreenState.ShipView);
                     scale2 *= (this.camHeight * 3 / this.GetZfromScreenState(UniverseScreen.UnivScreenState.ShipView));
                }

                if (!ship.Active )
                    return;
               
                Vector2 position = new Vector2(vector2_1.X + 15f * scale2, vector2_1.Y + 15f * scale);
                if (ship.isColonyShip)
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/flagicon"], position + new Vector2(-7f, -17f), ship.loyalty.EmpireColor);
                position = new Vector2(vector3_1.X, vector3_1.Y);
                //Added by McShooterz: Make Fighter tactical symbol default if not found
                position = new Vector2(vector3_1.X, vector3_1.Y);
                if (ResourceManager.TextureDict.ContainsKey(ship.StrategicIconPath))
                {

                    float width = (float)(ResourceManager.TextureDict[ship.StrategicIconPath].Width / 2);
                    //width = width * scale2 < 20? width / scale2 : width;
                    Vector2 origin = new Vector2(width, width);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ship.StrategicIconPath], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale2, SpriteEffects.None, 1f);
                }
                else
                {
                    float width = (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2);
                    //width = width * scale2 < 20 ? width / scale2 : width;
                    Vector2 origin = new Vector2(width, width);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale2, SpriteEffects.None, 1f);
                }
                
                if ((double)ship.OrdinanceMax <= 0.0)
                    return;
                if ((double)ship.Ordinance < 0.200000002980232 * (double)ship.OrdinanceMax)
                {
                     position = new Vector2(vector2_1.X + 15f * scale, vector2_1.Y + 15f * scale);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_ammo"], position, new Rectangle?(), Color.Red, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
                else
                {
                    if ((double)ship.Ordinance >= 0.5 * (double)ship.OrdinanceMax)
                        return;
                     position = new Vector2(vector2_1.X + 15f * scale, vector2_1.Y + 15f * scale);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_ammo"], position, new Rectangle?(), Color.Yellow, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }

            }
        }

        private void DrawBombs()
        {
            using (BombList.AcquireReadLock())
            {
                foreach (Bomb bomb in BombList)
                    DrawTransparentModel(bomb.Model, bomb.World, view, projection, bomb.Texture, 0.5f);
            }
        }

        protected void DrawInRange(Ship ship)
        {
            if (viewState > UnivScreenState.SystemView)
                return;
            using (ship.Projectiles.AcquireReadLock())
            {
                foreach (Projectile projectile in ship.Projectiles)
                {
                    if (Frustum.Contains(projectile.Center.ToVec3()) != ContainmentType.Disjoint 
                        && projectile.WeaponType != "Missile" 
                        && projectile.WeaponType != "Rocket" 
                        && projectile.WeaponType != "Drone")

                    {
                        DrawTransparentModel(ResourceManager.ProjectileModelDict[projectile.modelPath], 
                            projectile.GetWorld(), this.view, this.projection, 
                            projectile.weapon.Animated != 0 
                            ? ResourceManager.TextureDict[projectile.texturePath] 
                            : ResourceManager.ProjTextDict[projectile.texturePath], projectile.Scale);
                    }
                }
            }
        }

        private Circle DrawSelectionCircles(Vector2 WorldPos, float WorldRadius)
        {
            float radius = WorldRadius;
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(WorldPos, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector2 Center = new Vector2(vector3_1.X, vector3_1.Y);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(WorldPos.PointOnCircle(90f, radius), 0.0f), this.projection, this.view, Matrix.Identity);
            float Radius = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), Center) + 10f;
            return new Circle(Center, Radius);
        }

        private Circle DrawSelectionCirclesAroundShip(Ship ship)
        {
            if (this.SelectedShip != null && this.SelectedShip == ship || this.SelectedShipList.Contains(ship))
            {
                float num = ship.GetSO().WorldBoundingSphere.Radius;
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 Center = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(ship.Center.X + num, ship.Center.Y), 0.0f), this.projection, this.view, Matrix.Identity);
                float Radius = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), Center);
                if ((double)Radius < 5.0)
                    Radius = 5f;
                if (this.viewState < UniverseScreen.UnivScreenState.SectorView)
                    return new Circle(Center, Radius);
            }
            return (Circle)null;
        }

        protected void RenderParticles()
        {
            this.beamflashes.SetCamera(this.view, this.projection);
            this.explosionParticles.SetCamera(this.view, this.projection);
            this.photonExplosionParticles.SetCamera(this.view, this.projection);
            this.explosionSmokeParticles.SetCamera(this.view, this.projection);
            this.projectileTrailParticles.SetCamera(this.view, this.projection);
            this.fireTrailParticles.SetCamera(this.view, this.projection);
            this.smokePlumeParticles.SetCamera(this.view, this.projection);
            this.fireParticles.SetCamera(this.view, this.projection);
            this.engineTrailParticles.SetCamera(this.view, this.projection);
            this.flameParticles.SetCamera(this.view, this.projection);
            this.sparks.SetCamera(this.view, this.projection);
            this.lightning.SetCamera(this.view, this.projection);
            this.flash.SetCamera(this.view, this.projection);
            this.star_particles.SetCamera(this.view, this.projection);
            this.neb_particles.SetCamera(this.view, this.projection);
        }

        protected virtual void RenderBackdrop()
        {
            this.bg.Draw(this, this.starfield);
            this.bg3d.Draw();
            this.ClickableShipsList.Clear();
            this.ScreenManager.SpriteBatch.Begin();
            Rectangle rect = new Rectangle(0, 0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);

            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (ship != null && ship.Active && (viewState != UnivScreenState.GalaxyView || !ship.IsPlatform))
                    {
                        float local_4 = ship.GetSO().WorldBoundingSphere.Radius;
                        Vector3 local_6 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), projection, view, Matrix.Identity);
                        Vector2 local_7 = new Vector2(local_6.X, local_6.Y);
                        if (HelperFunctions.CheckIntersection(rect, local_7))
                        {
                            Vector3 local_9 = new Vector3(ship.Position.PointOnCircle(90f, local_4), 0.0f);
                            Vector3 local_10 = ScreenManager.GraphicsDevice.Viewport.Project(local_9, projection, view, Matrix.Identity);
                            Vector2 local_11 = new Vector2(local_10.X, local_10.Y);
                            float local_12 = Vector2.Distance(local_11, local_7);
                            if (local_12 < 7.0f) local_12 = 7f;
                            ship.ScreenRadius = local_12;
                            ship.ScreenPosition = local_7;
                            ClickableShipsList.Add(new ClickableShip
                            {
                                Radius = local_12,
                                ScreenPos = local_7,
                                shipToClick = ship
                            });
                            if (ship.loyalty == player || ship.loyalty != player && player.GetRelations(ship.loyalty).Treaty_Alliance)
                            {
                                local_9 = new Vector3(ship.Position.PointOnCircle(90f, ship.SensorRange), 0.0f);
                                local_10 = ScreenManager.GraphicsDevice.Viewport.Project(local_9, projection, view, Matrix.Identity);
                                local_11 = new Vector2(local_10.X, local_10.Y);
                                //local_2.ScreenSensorRadius = Vector2.Distance(local_11, local_7);    //This is assigned here, but is not referenced anywhere, removing to save memory -Gretman
                            }
                        }
                        else
                            ship.ScreenPosition = new Vector2(-1f, -1f);
                    }
                }
            }

            lock (GlobalStats.ClickableSystemsLock)
            {
                this.ClickPlanetList.Clear();
                this.ClickableSystems.Clear();
            }
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                Vector3 vector3_1 = new Vector3(solarSystem.Position, 0.0f);
                if (this.Frustum.Contains(new BoundingSphere(vector3_1, 100000f)) != ContainmentType.Disjoint)
                {
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(vector3_1, this.projection, this.view, Matrix.Identity);
                    Vector2 vector2 = new Vector2(vector3_2.X, vector3_2.Y);
                    Vector3 vector3_3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(solarSystem.Position.PointOnCircle(90f, 4500f), 0.0f), this.projection, this.view, Matrix.Identity);
                    float num1 = Vector2.Distance(new Vector2(vector3_3.X, vector3_3.Y), vector2);
                    lock (GlobalStats.ClickableSystemsLock)
                        this.ClickableSystems.Add(new UniverseScreen.ClickableSystem()
                        {
                            Radius = (double)num1 < 8.0 ? 8f : num1,
                            ScreenPos = vector2,
                            systemToClick = solarSystem
                        });
                    if (this.viewState <= UniverseScreen.UnivScreenState.SectorView)
                    {
                        foreach (Planet planet in solarSystem.PlanetList)
                        {
                            if (solarSystem.ExploredDict[EmpireManager.Player])
                            {
                                float radius = planet.SO.WorldBoundingSphere.Radius;
                                Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position, 2500f), this.projection, this.view, Matrix.Identity);
                                Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
                                Vector3 vector3_5 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position.PointOnCircle(90f, radius), 2500f), this.projection, this.view, Matrix.Identity);
                                float num2 = Vector2.Distance(new Vector2(vector3_5.X, vector3_5.Y), position);
                                float scale = num2 / 115f;
                                if (planet.planetType == 1 || planet.planetType == 11 || (planet.planetType == 13 || planet.planetType == 21) || (planet.planetType == 22 || planet.planetType == 25 || (planet.planetType == 27 || planet.planetType == 29)))
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetGlows/Glow_Terran"], position, new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                else if (planet.planetType == 5 || planet.planetType == 7 || (planet.planetType == 8 || planet.planetType == 9) || planet.planetType == 23)
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetGlows/Glow_Red"], position, new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                else if (planet.planetType == 17)
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetGlows/Glow_White"], position, new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                else if (planet.planetType == 19)
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetGlows/Glow_Aqua"], position, new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                else if (planet.planetType == 14 || planet.planetType == 18)
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["PlanetGlows/Glow_Orange"], position, new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                lock (GlobalStats.ClickableSystemsLock)
                                    this.ClickPlanetList.Add(new UniverseScreen.ClickablePlanets()
                                    {
                                        ScreenPos = position,
                                        Radius = (double)num2 < 8.0 ? 8f : num2,
                                        planetToClick = planet
                                    });
                            }
                        }
                    }
                    if (this.viewState < UniverseScreen.UnivScreenState.GalaxyView)
                    {
                        this.DrawTransparentModel(this.SunModel, Matrix.CreateRotationZ(this.Zrotate) * Matrix.CreateTranslation(new Vector3(solarSystem.Position, 0.0f)), this.view, this.projection, ResourceManager.TextureDict["Suns/" + solarSystem.SunPath], 10.0f);
                        this.DrawTransparentModel(this.SunModel, Matrix.CreateRotationZ((float)(-(double)this.Zrotate / 2.0)) * Matrix.CreateTranslation(new Vector3(solarSystem.Position, 0.0f)), this.view, this.projection, ResourceManager.TextureDict["Suns/" + solarSystem.SunPath], 10.0f);
                        if (solarSystem.ExploredDict[EmpireManager.Player])
                        {
                            foreach (Planet planet in solarSystem.PlanetList)
                            {
                                Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position.X, planet.Position.Y, 2500f), this.projection, this.view, Matrix.Identity);
                                float radius = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), new Vector2(vector3_4.X, vector3_4.Y));
                                if (this.viewState != UniverseScreen.UnivScreenState.ShipView)
                                {
                                    if (planet.Owner == null)
                                        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, new Vector2(vector3_2.X, vector3_2.Y), radius, 100, new Color((byte)50, (byte)50, (byte)50, (byte)90), 3f);
                                    else
                                        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, new Vector2(vector3_2.X, vector3_2.Y), radius, 100, new Color(planet.Owner.EmpireColor.R, planet.Owner.EmpireColor.G, planet.Owner.EmpireColor.B, (byte)100), 3f);
                                }
                            }
                        }
                    }
                }
            }
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Blend.SourceAlpha;
            renderState.DestinationBlend       = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode               = CullMode.None;
            renderState.DepthBufferWriteEnable = true;
            this.ScreenManager.SpriteBatch.End();
        }

        protected virtual void RenderGalaxyBackdrop()
        {
            this.bg.DrawGalaxyBackdrop(this, this.starfield);
            this.ScreenManager.SpriteBatch.Begin();
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3((float)((double)index * (double)this.Size.X / 40.0), 0.0f, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3((float)((double)index * (double)this.Size.X / 40.0), this.Size.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)211, (byte)211, (byte)211, (byte)70));
            }
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(0.0f, (float)((double)index * (double)this.Size.Y / 40.0), 40f), this.projection, this.view, Matrix.Identity);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X, (float)((double)index * (double)this.Size.Y / 40.0), 0.0f), this.projection, this.view, Matrix.Identity);
                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)211, (byte)211, (byte)211, (byte)70));
            }
            this.ScreenManager.SpriteBatch.End();
        }

        protected virtual void RenderOverFog(GameTime gameTime)
        {
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X, this.Size.Y, 0.0f), this.projection, this.view, Matrix.Identity);
            this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X / 2f, this.Size.Y / 2f, 0.0f), this.projection, this.view, Matrix.Identity);
            Rectangle rectangle1 = new Rectangle((int)vector3_1.X, (int)vector3_1.Y, (int)vector3_2.X - (int)vector3_1.X, (int)vector3_2.Y - (int)vector3_1.Y);
            if (this.viewState >= UniverseScreen.UnivScreenState.SectorView)
            {
                float num = (float)((double)byte.MaxValue * (double)this.camHeight / 9000000.0);
                if ((double)num > (double)byte.MaxValue)
                    num = (float)byte.MaxValue;
                Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)num);
                this.ScreenManager.SpriteBatch.End();
                this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
                Rectangle rectangle2 = new Rectangle(rectangle1.X, rectangle1.Y, rectangle1.Width / 2, rectangle1.Height / 2);
                this.ScreenManager.SpriteBatch.End();
                this.ScreenManager.SpriteBatch.Begin();
            }
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (this.viewState >= UniverseScreen.UnivScreenState.SectorView)
                {
                    Vector3 vector3_3 = new Vector3(solarSystem.Position, 0.0f);
                    if (this.Frustum.Contains(vector3_3) != ContainmentType.Disjoint)
                    {
                        Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(vector3_3, this.projection, this.view, Matrix.Identity);
                        Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
                        Vector3 vector3_5 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(solarSystem.Position.PointOnCircle(90f, 25000f), 0.0f), this.projection, this.view, Matrix.Identity);
                        float num2 = Vector2.Distance(new Vector2(vector3_5.X, vector3_5.Y), position);
                        Vector2 vector2 = new Vector2(position.X, position.Y);
                        if ((solarSystem.ExploredDict[this.player] || this.Debug) && this.SelectedSystem != solarSystem)
                        {
                            if (this.Debug)
                            {
                                solarSystem.ExploredDict[this.player] = true;
                                foreach (Planet planet in solarSystem.PlanetList)
                                    planet.ExploredDict[this.player] = true;
                            }
                            Vector3 vector3_6 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(100000f * UniverseScreen.GameScaleStatic, 0.0f) + solarSystem.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                            float radius = Vector2.Distance(new Vector2(vector3_6.X, vector3_6.Y), position);
                            if (this.viewState == UniverseScreen.UnivScreenState.SectorView)
                            {
                                vector2.Y += radius;
                                Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, new Vector2(vector3_4.X, vector3_4.Y), radius, 100, new Color((byte)50, (byte)50, (byte)50, (byte)90), 1f);
                            }
                            else
                                vector2.Y += num2;
                            vector2.X -= SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X / 2f;
                            Vector2 pos = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
                            if (solarSystem.OwnerList.Count == 0)
                            {
                                if (this.SelectedSystem != solarSystem || this.viewState < UniverseScreen.UnivScreenState.GalaxyView)
                                    this.ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.SysFont, solarSystem.Name, vector2, Color.Gray);
                                int num3 = 0;
                                --vector2.Y;
                                vector2.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X + 6f;
                                bool flag = false;
                                foreach (Planet planet in solarSystem.PlanetList)
                                {
                                    if (planet.ExploredDict[this.player])
                                    {
                                        for (int index = 0; index < planet.BuildingList.Count; ++index)
                                        {
                                            if (!string.IsNullOrEmpty(planet.BuildingList[index].EventTriggerUID))
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (flag)
                                            break;
                                    }
                                }
                                if (flag)
                                {
                                    vector2.Y -= 2f;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y, 15, 15);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_anomaly_small"], rectangle2, color);
                                    if (HelperFunctions.CheckIntersection(rectangle2, pos))
                                        ToolTip.CreateTooltip(138, this.ScreenManager);
                                    ++num3;
                                }
                                TimeSpan totalGameTime;
                                if (solarSystem.CombatInSystem)
                                {
                                    vector2.X += (float)(num3 * 20);
                                    vector2.Y -= 2f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(totalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], rectangle2, color);
                                    if (HelperFunctions.CheckIntersection(rectangle2, pos))
                                        ToolTip.CreateTooltip(122, this.ScreenManager);
                                    ++num3;
                                }
                                if ((double)solarSystem.DangerTimer > 0.0)
                                {
                                    if (num3 == 1 || num3 == 2)
                                        vector2.X += 20f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(totalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/EnemyHere"], rectangle2, color);
                                    if (HelperFunctions.CheckIntersection(rectangle2, pos))
                                        ToolTip.CreateTooltip(123, this.ScreenManager);
                                }
                            }
                            else
                            {
                                int num3 = 0;
                                if (solarSystem.OwnerList.Count == 1)
                                {
                                    if (this.SelectedSystem != solarSystem || this.viewState < UniverseScreen.UnivScreenState.GalaxyView)
                                        HelperFunctions.DrawDropShadowText(this.ScreenManager, solarSystem.Name, vector2, SystemInfoUIElement.SysFont, solarSystem.OwnerList.ToList()[0].EmpireColor);
                                }
                                else if (this.SelectedSystem != solarSystem || this.viewState < UniverseScreen.UnivScreenState.GalaxyView)
                                {
                                    Vector2 Pos = vector2;
                                    int length = solarSystem.Name.Length;
                                    int num4 = length / solarSystem.OwnerList.Count;
                                    int index1 = 0;
                                    for (int index2 = 0; index2 < length; ++index2)
                                    {
                                        if (index2 + 1 > num4 + num4 * index1)
                                            ++index1;
                                        HelperFunctions.DrawDropShadowText(this.ScreenManager, solarSystem.Name[index2].ToString(), Pos, SystemInfoUIElement.SysFont, solarSystem.OwnerList.Count > index1 ? solarSystem.OwnerList.ToList()[index1].EmpireColor : Enumerable.Last<Empire>((IEnumerable<Empire>)solarSystem.OwnerList).EmpireColor);
                                        Pos.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name[index2].ToString()).X;
                                    }
                                }
                                --vector2.Y;
                                vector2.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X + 6f;
                                bool flag = false;
                                foreach (Planet planet in solarSystem.PlanetList)
                                {
                                    if (planet.ExploredDict[this.player])
                                    {
                                        for (int index = 0; index < planet.BuildingList.Count; ++index)
                                        {
                                            if (!string.IsNullOrEmpty(planet.BuildingList[index].EventTriggerUID))
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (flag)
                                            break;
                                    }
                                }
                                if (flag)
                                {
                                    vector2.Y -= 2f;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y, 15, 15);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_anomaly_small"], rectangle2, color);
                                    if (HelperFunctions.CheckIntersection(rectangle2, pos))
                                        ToolTip.CreateTooltip(138, this.ScreenManager);
                                    ++num3;
                                }
                                TimeSpan totalGameTime;
                                if (solarSystem.CombatInSystem)
                                {
                                    vector2.X += (float)(num3 * 20);
                                    vector2.Y -= 2f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(totalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], rectangle2, color);
                                    if (HelperFunctions.CheckIntersection(rectangle2, pos))
                                        ToolTip.CreateTooltip(122, this.ScreenManager);
                                    ++num3;
                                }
                                if ((double)solarSystem.DangerTimer > 0.0)
                                {
                                    if (num3 == 1 || num3 == 2)
                                        vector2.X += 20f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(totalGameTime.TotalSeconds)) * (float)byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int)vector2.X, (int)vector2.Y, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/EnemyHere"], rectangle2, color);
                                    if (HelperFunctions.CheckIntersection(rectangle2, pos))
                                        ToolTip.CreateTooltip(123, this.ScreenManager);
                                }
                            }
                        }
                        else
                            vector2.X -= SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X / 2f;
                    }
                }
                if (this.viewState >= UniverseScreen.UnivScreenState.GalaxyView)
                {
                    float scale = 0.05f;
                    Vector3 vector3_3 = new Vector3(solarSystem.Position, 0.0f);
                    Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(vector3_3, this.projection, this.view, Matrix.Identity);
                    Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath], position, new Rectangle?(), Color.White, this.Zrotate, new Vector2((float)(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Width / 2), (float)(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Height / 2)), scale, SpriteEffects.None, 0.9f);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath], position, new Rectangle?(), Color.White, (float)(-(double)this.Zrotate / 2.0), new Vector2((float)(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Width / 2), (float)(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Height / 2)), scale, SpriteEffects.None, 0.9f);
                }
            }
        }

        protected void RenderThrusters()
        {
            if (this.viewState != UniverseScreen.UnivScreenState.ShipView)
                return;
            using (player.KnownShips.AcquireReadLock())
            {
                for (int local_0 = 0; local_0 < this.player.KnownShips.Count; ++local_0)
                {
                    Ship local_1 = this.player.KnownShips[local_0];
                    if (local_1 != null && this.Frustum.Contains(new Vector3(local_1.Center, 0.0f)) != ContainmentType.Disjoint && local_1.inSensorRange)
                    {
                        foreach (Thruster item_0 in local_1.GetTList())
                        {
                            if (item_0.technique != null)
                            {
                                item_0.draw(ref this.view, ref this.projection, this.ThrusterEffect);
                                item_0.draw(ref this.view, ref this.projection, this.ThrusterEffect);
                            }
                            else
                                item_0.load_and_assign_effects(this.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                        }
                    }
                }
            }
        }

        public virtual void Render(GameTime gameTime)
        {
            if (this.Frustum == (BoundingFrustum)null)
                this.Frustum = new BoundingFrustum(this.view * this.projection);
            else
                this.Frustum.Matrix = this.view * this.projection;
            this.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, (ISceneEnvironment)this.ScreenManager.environment, true);
            this.ScreenManager.editor.BeginFrameRendering((ISceneState)this.ScreenManager.sceneState);
            lock (GlobalStats.ObjectManagerLocker)
                this.ScreenManager.inter.BeginFrameRendering((ISceneState)this.ScreenManager.sceneState);
            this.RenderBackdrop();
            this.ScreenManager.SpriteBatch.Begin();
            if (this.DefiningAO && Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3((float)this.AORect.X, (float)this.AORect.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3((float)(this.AORect.X + this.AORect.Width), (float)(this.AORect.Y + this.AORect.Height), 0.0f), this.projection, this.view, Matrix.Identity);
                Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, new Rectangle((int)vector3_1.X, (int)vector3_1.Y, (int)((double)vector3_2.X - (double)vector3_1.X), (int)((double)vector3_2.Y - (double)vector3_1.Y)), Color.Red);
            }
            if (this.DefiningAO && this.SelectedShip != null)
            {
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(1411), new Vector2((float)this.SelectedStuffRect.X, (float)(this.SelectedStuffRect.Y - Fonts.Pirulen16.LineSpacing - 2)), Color.White);
                foreach (Rectangle rectangle in this.SelectedShip.AreaOfOperation)
                {
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3((float)rectangle.X, (float)rectangle.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3((float)(rectangle.X + rectangle.Width), (float)(rectangle.Y + rectangle.Height), 0.0f), this.projection, this.view, Matrix.Identity);
                    Rectangle rect = new Rectangle((int)vector3_1.X, (int)vector3_1.Y, (int)Math.Abs(vector3_1.X - vector3_2.X), (int)Math.Abs(vector3_1.Y - vector3_2.Y));
                    Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, rect, new Color(byte.MaxValue, (byte)0, (byte)0, (byte)10));
                    Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, rect, Color.Red);
                }
            }
            else
                this.DefiningAO = false;
            float num = (float)(150.0 * this.SelectedSomethingTimer / 3f);
            if (num < 0f)
                num = 0.0f;
            if (this.SelectedShip != null)
            {
                if (!this.SelectedShip.InCombat || this.SelectedShip.GetAI().HasPriorityOrder)
                {
                    if (this.SelectedShip.GetAI().State == AIState.Ferrying)
                    {
                        Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().EscortTarget.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                    }
                    else if (this.SelectedShip.GetAI().State == AIState.Escort)
                    {
                        if (this.SelectedShip.GetAI().EscortTarget != null)
                        {
                            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().EscortTarget.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                        }
                    }
                    else if (this.SelectedShip.GetAI().State == AIState.ReturnToHangar)
                    {
                        if (this.SelectedShip.Mothership != null)
                        {
                            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Mothership.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                        }
                        else
                            this.SelectedShip.GetAI().State = AIState.AwaitingOrders;
                    }
                    if (this.SelectedShip.GetAI().State == AIState.Explore && this.SelectedShip.GetAI().ExplorationTarget != null)
                    {
                        Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ExplorationTarget.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                        Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                    }
                    if (this.SelectedShip.GetAI().State == AIState.Orbit && this.SelectedShip.GetAI().OrbitTarget != null)
                    {
                        Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().OrbitTarget.Position, 2500f), this.projection, this.view, Matrix.Identity);
                        Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                    }
                    if (this.SelectedShip.GetAI().State == AIState.Colonize && this.SelectedShip.GetAI().ColonizeTarget != null)
                    {
                        Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ColonizeTarget.Position, 2500f), this.projection, this.view, Matrix.Identity);
                        Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                    }
                    if (this.SelectedShip.GetAI().State == AIState.Bombard && this.SelectedShip.GetAI().OrderQueue.Count > 0 && Enumerable.First<ArtificialIntelligence.ShipGoal>((IEnumerable<ArtificialIntelligence.ShipGoal>)this.SelectedShip.GetAI().OrderQueue).TargetPlanet != null)
                    {
                        Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(Enumerable.First<ArtificialIntelligence.ShipGoal>((IEnumerable<ArtificialIntelligence.ShipGoal>)this.SelectedShip.GetAI().OrderQueue).TargetPlanet.Position, 2500f), this.projection, this.view, Matrix.Identity);
                        Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color(Color.Red, (byte)num));
                    }
                    if (this.SelectedShip.GetAI().State == AIState.Rebase )
                    {
                        lock (this.SelectedShip.GetAI().WayPointLocker)
                        {
                            bool waydpoint =false;
                            for (int local_23 = 0; local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count; ++local_23)
                            {
                                waydpoint =true;
                                if (local_23 == 0)
                                {
                                    Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.Peek(), 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                             }
                                else if (local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count - 1)
                                {
                                    Vector3 local_26 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23], 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_27 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23 + 1], 2500f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_26.X, local_26.Y), new Vector2(local_27.X, local_27.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                }
                            }
                            if(!waydpoint )
                            {
                                ArtificialIntelligence.ShipGoal goal = this.SelectedShip.GetAI().OrderQueue.FirstOrDefault();
                                if (goal != null && goal.TargetPlanet != null)
                                {
                                    Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(goal.TargetPlanet.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                }
                            }
                        }

                    }
                    if ( this.SelectedShip.GetAI().State == AIState.AssaultPlanet)
                    {
                        //ArtificialIntelligence.ShipGoal goal =null;
                        Vector2 target = this.SelectedShip.GetAI().OrbitTarget.Position;
                        //foreach(ArtificialIntelligence.ShipGoal Goal in this.SelectedShip.GetAI().OrderQueue)
                        //{
                        //    if (Goal.Plan == ArtificialIntelligence.Plan.LandTroop)
                        //        target = Goal.TargetPlanet.Position;
                        //    else continue;
                        //    break;
                        //}
                        Color mode;
                        int spots = 0;// this.SelectedShip.GetAI().OrbitTarget.GetGroundLandingSpots();
                        if (Vector2.Distance(this.SelectedShip.GetAI().OrbitTarget.Position, this.SelectedShip.Center) <= this.SelectedShip.SensorRange)
                            spots = this.SelectedShip.GetAI().OrbitTarget.GetGroundLandingSpots();
                        else spots = 11;
                        if (spots >10)
                        {
                            
                            mode = new Color(Color.Red, (byte)num);
                        }
                        else if(spots >0)
                            mode = new Color(Color.OrangeRed, (byte)num);
                        else
                            mode = new Color(Color.Orange, (byte)num);
                        //New Color
                        lock (this.SelectedShip.GetAI().WayPointLocker)
                        {
                            bool waydpoint = false;
                            for (int local_23 = 0; local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count; ++local_23)
                            {
                                waydpoint = true;
                                if (local_23 == 0)
                                {
                                    Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.Peek(), 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), mode);
                                }
                                else if (local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count - 1)
                                {
                                    Vector3 local_26 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23], 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_27 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23 + 1], 2500f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_26.X, local_26.Y), new Vector2(local_27.X, local_27.Y), mode);
                                }
                            }
                            if (!waydpoint && target != Vector2.Zero) //this.SelectedShip.GetAI().OrderQueue.First.Value.TargetPlanet.Position
                            {
                                Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(target, 0.0f), this.projection, this.view, Matrix.Identity);
                                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), mode);
                            }
                        }

                    }
                    
                    if (this.SelectedShip.GetAI().ActiveWayPoints.Count > 0 && (this.SelectedShip.GetAI().State == AIState.MoveTo || this.SelectedShip.GetAI().State == AIState.PassengerTransport || this.SelectedShip.GetAI().State == AIState.SystemTrader))
                    {
                        lock (this.SelectedShip.GetAI().WayPointLocker)
                        {
                            for (int local_28 = 0; local_28 < this.SelectedShip.GetAI().ActiveWayPoints.Count; ++local_28)
                            {
                                if (local_28 == 0)
                                {
                                    Vector3 local_29 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_30 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.Peek(), 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_29.X, local_29.Y), new Vector2(local_30.X, local_30.Y), new Color((byte)0, byte.MaxValue, (byte)0, (byte)num));
                                }
                                if (local_28 < this.SelectedShip.GetAI().ActiveWayPoints.Count - 1)
                                {
                                    Vector3 local_31 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_28], 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_32 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_28 + 1], 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_31.X, local_31.Y), new Vector2(local_32.X, local_32.Y), new Color((byte)0, byte.MaxValue, (byte)0, (byte)num));
                                }
                            }
                        }
                    }
                }
                if (!this.SelectedShip.GetAI().HasPriorityOrder && (this.SelectedShip.GetAI().State == AIState.AttackTarget || this.SelectedShip.GetAI().State == AIState.Combat) && (this.SelectedShip.GetAI().Target != null && this.SelectedShip.GetAI().Target is Ship))
                {
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().Target.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color(byte.MaxValue, (byte)0, (byte)0, (byte)num));
                    if (this.SelectedShip.GetAI().TargetQueue.Count > 1)
                    {
                        for (int index = 0; index < this.SelectedShip.GetAI().TargetQueue.Count - 1; ++index)
                        {
                            vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)this.SelectedShip.GetAI().TargetQueue, index).Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Vector3 vector3_3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)this.SelectedShip.GetAI().TargetQueue, index + 1).Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_3.X, vector3_3.Y), new Color(byte.MaxValue, (byte)0, (byte)0, (byte)num));
                        }
                    }
                }
                if (this.SelectedShip.GetAI().State == AIState.Boarding && this.SelectedShip.GetAI().EscortTarget != null)
                {
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().EscortTarget.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color(byte.MaxValue, (byte)0, (byte)0, (byte)num));
                }
            }
            else if (this.SelectedShipList.Count > 0)
            {
                int ships = this.SelectedShipList.Count;
                bool planetFullCheck = false;
                Color modeSelected = new Color(Color.Orange, (byte)num);
                for (int index1 = 0; index1 < this.SelectedShipList.Count; ++index1)
                {
                    try
                    {
                        Ship ship = this.SelectedShipList[index1];
                        bool flag = false;
 
                        if (!ship.InCombat || ship.GetAI().HasPriorityOrder)
                        {
                            if (ship.GetAI().State == AIState.Ferrying)
                            {
                                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().EscortTarget.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                flag = true;
                            }
                            else if (ship.GetAI().State == AIState.ReturnToHangar)
                            {
                                if (ship.Mothership != null)
                                {
                                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Mothership.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                    flag = true;
                                }
                                else
                                    ship.GetAI().State = AIState.AwaitingOrders;
                            }
                            else if (ship.GetAI().State == AIState.Escort && ship.GetAI().EscortTarget != null)
                            {
                                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().EscortTarget.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                flag = true;
                            }
                            if (ship.GetAI().State == AIState.Explore && ship.GetAI().ExplorationTarget != null)
                            {
                                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().ExplorationTarget.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                flag = true;
                            }
                            if (ship.GetAI().State == AIState.Orbit && ship.GetAI().OrbitTarget != null)
                            {
                                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().OrbitTarget.Position, 2500f), this.projection, this.view, Matrix.Identity);
                                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                flag = true;
                            }
                          
                        }
                        if (!ship.GetAI().HasPriorityOrder && (ship.GetAI().State == AIState.AttackTarget || ship.GetAI().State == AIState.Combat) && (ship.GetAI().Target != null && ship.GetAI().Target is Ship))
                        {
                            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().Target.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color(byte.MaxValue, (byte)0, (byte)0, (byte)num));
                            if (ship.GetAI().TargetQueue.Count > 1)
                            {
                                for (int index2 = 0; index2 < ship.GetAI().TargetQueue.Count - 1; ++index2)
                                {
                                    vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)ship.GetAI().TargetQueue, index2).Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 vector3_3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(Enumerable.ElementAt<Ship>((IEnumerable<Ship>)ship.GetAI().TargetQueue, index2 + 1).Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_3.X, vector3_3.Y), new Color(byte.MaxValue, (byte)0, (byte)0, (byte)num));
                                }
                            }
                            flag = true;
                        }
                        if (ship.GetAI().State == AIState.Boarding && ship.GetAI().EscortTarget != null)
                        {
                            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().EscortTarget.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), new Color(byte.MaxValue, (byte)0, (byte)0, (byte)num));
                            flag = true;
                        }
                        if (ship.GetAI().State == AIState.AssaultPlanet && ship.GetAI().OrbitTarget != null)
                        {

                            if (!planetFullCheck)
                            {
                                planetFullCheck = true;
                                int spots = 0;// ship.GetAI().OrbitTarget.GetGroundLandingSpots();
                                if (Vector2.Distance(ship.GetAI().OrbitTarget.Position, ship.Center) <= ship.SensorRange)
                                    spots = ship.GetAI().OrbitTarget.GetGroundLandingSpots();
                                else spots = -1;

                                if (spots < 0 || (spots > 10 && spots < ships))
                                {

                                    modeSelected = new Color(Color.Red, (byte)num);
                                }
                                else if (spots > 0)
                                    modeSelected = new Color(Color.OrangeRed, (byte)num);
                               

                            }


                            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().OrbitTarget.Position, 2500f), this.projection, this.view, Matrix.Identity);
                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_1.X, vector3_1.Y), new Vector2(vector3_2.X, vector3_2.Y), modeSelected);
                            flag = true;


                        }

                        //try to fix troop assault projected position. Too slow right now. just use single target version. its much faster. 
                        //if (ship.GetAI().State == AIState.AssaultPlanet && ship.GetAI().OrbitTarget != null)
                        //{
                        //    //ArtificialIntelligence.ShipGoal goal =null;
                        //    Vector2 target = ship.GetAI().OrbitTarget.Position;
                        //    //foreach (ArtificialIntelligence.ShipGoal Goal in this.SelectedShip.GetAI().OrderQueue)
                        //    //{
                        //    //    if (Goal.Plan == ArtificialIntelligence.Plan.LandTroop)
                        //    //        target = Goal.TargetPlanet.Position;
                        //    //    else continue;
                        //    //    break;
                        //    //}

                        //    // goal = this.SelectedShip.GetAI().OrderQueue.LastOrDefault(); //.Value;
                        //    lock (this.SelectedShip.GetAI().WayPointLocker)
                        //    {
                        //        bool waydpoint = false;
                        //        for (int local_23 = 0; local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count; ++local_23)
                        //        {
                        //            waydpoint = true;
                        //            if (local_23 == 0)
                        //            {
                        //                Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        //                Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.Peek(), 0.0f), this.projection, this.view, Matrix.Identity);
                        //                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), new Color(Color.Red, (byte)num));
                        //            }
                        //            else if (local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count - 1)
                        //            {
                        //                Vector3 local_26 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23], 0.0f), this.projection, this.view, Matrix.Identity);
                        //                Vector3 local_27 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23 + 1], 2500f), this.projection, this.view, Matrix.Identity);
                        //                Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_26.X, local_26.Y), new Vector2(local_27.X, local_27.Y), new Color(Color.Red, (byte)num));
                        //            }
                        //        }
                        //        if (!waydpoint && target != Vector2.Zero) //this.SelectedShip.GetAI().OrderQueue.First.Value.TargetPlanet.Position
                        //        {
                        //            Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                        //            Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(target, 0.0f), this.projection, this.view, Matrix.Identity);
                        //            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), new Color(Color.Red, (byte)num));
                        //        }
                        //    }

                        //}


                        if (!flag)
                        {
                            if (ship.GetAI().ActiveWayPoints.Count > 0)
                            {
                                lock (ship.GetAI().WayPointLocker)
                                {
                                    for (int local_56 = 0; local_56 < ship.GetAI().ActiveWayPoints.Count; ++local_56)
                                    {
                                        if (local_56 == 0)
                                        {
                                            Vector3 local_57 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                            Vector3 local_58 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().ActiveWayPoints.Peek(), 0.0f), this.projection, this.view, Matrix.Identity);
                                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_57.X, local_57.Y), new Vector2(local_58.X, local_58.Y), new Color((byte)0, byte.MaxValue, (byte)0, (byte)num));
                                        }
                                        if (local_56 < ship.GetAI().ActiveWayPoints.Count - 1)
                                        {
                                            Vector3 local_59 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().ActiveWayPoints.ToArray()[local_56], 0.0f), this.projection, this.view, Matrix.Identity);
                                            Vector3 local_60 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.GetAI().ActiveWayPoints.ToArray()[local_56 + 1], 0.0f), this.projection, this.view, Matrix.Identity);
                                            Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_59.X, local_59.Y), new Vector2(local_60.X, local_60.Y), new Color((byte)0, byte.MaxValue, (byte)0, (byte)num));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            this.ScreenManager.SpriteBatch.End();
            this.DrawBombs();
            lock (GlobalStats.ObjectManagerLocker)
                this.ScreenManager.inter.RenderManager.Render();

            using (player.KnownShips.AcquireReadLock())
            foreach (Ship ship in player.KnownShips)
            {
                using (ship.Projectiles.AcquireReadLock())
                foreach (Projectile projectile in ship.Projectiles)
                {
                    if (projectile.weapon.IsRepairDrone && projectile.GetDroneAI() != null)
                    {
                        for (int j = 0; j < projectile.GetDroneAI().Beams.Count; ++j)
                            projectile.GetDroneAI().Beams[j].Draw(this.ScreenManager);
                    }
                }
                if (viewState < UnivScreenState.SectorView)
                {
                    for (int i = 0; i < ship.Beams.Count; ++i) // regular FOR to mitigate multi-threading issues
                    {
                        Beam beam = ship.Beams[i];
                        if (beam.Source.InRadius(beam.ActualHitDestination, beam.range + 10.0f))
                            beam.Draw(ScreenManager);
                        else
                            beam.Die(null, true);
                    }
                }
            }

            var renderState = ScreenManager.GraphicsDevice.RenderState;

            renderState.DepthBufferWriteEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            foreach (Anomaly anomaly in anomalyManager.AnomaliesList)
                anomaly.Draw();
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                if (solarSystem.ExploredDict[player] && solarSystem.isVisible && camHeight < 150000.0f)
                {
                    foreach (Planet p in solarSystem.PlanetList)
                    {
                        if (Frustum.Contains(p.SO.WorldBoundingSphere) != ContainmentType.Disjoint)
                        {
                            if (p.hasEarthLikeClouds)
                            {
                                DrawClouds(xnaPlanetModel, p.cloudMatrix, view, projection, p);
                                DrawAtmo(xnaPlanetModel, p.cloudMatrix, view, projection, p);
                            }
                            if (p.hasRings)
                                DrawRings(p.RingWorld, view, projection, p.scale);
                        }
                    }
                }
            }
            renderState.AlphaBlendEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            this.RenderThrusters();
            this.RenderParticles();

            FTLManager.DrawFTLModels(this);
            lock (GlobalStats.ExplosionLocker)
            {
                foreach (MuzzleFlash item_4 in MuzzleFlashManager.FlashList)
                    this.DrawTransparentModel(MuzzleFlashManager.flashModel, item_4.WorldMatrix, this.view, this.projection, MuzzleFlashManager.FlashTexture, item_4.scale);
                MuzzleFlashManager.FlashList.ApplyPendingRemovals();
            }
            this.beamflashes.Draw(gameTime);
            this.explosionParticles.Draw(gameTime);
            this.photonExplosionParticles.Draw(gameTime);
            this.explosionSmokeParticles.Draw(gameTime);
            this.projectileTrailParticles.Draw(gameTime);
            this.fireTrailParticles.Draw(gameTime);
            this.smokePlumeParticles.Draw(gameTime);
            this.fireParticles.Draw(gameTime);
            this.engineTrailParticles.Draw(gameTime);
            this.star_particles.Draw(gameTime);
            this.neb_particles.Draw(gameTime);
            this.flameParticles.Draw(gameTime);
            this.sparks.Draw(gameTime);
            this.lightning.Draw(gameTime);
            this.flash.Draw(gameTime);
            if (!Paused)
            {
                this.beamflashes.Update(gameTime);
                this.explosionParticles.Update(gameTime);
                this.photonExplosionParticles.Update(gameTime);
                this.explosionSmokeParticles.Update(gameTime);
                this.projectileTrailParticles.Update(gameTime);
                this.fireTrailParticles.Update(gameTime);
                this.smokePlumeParticles.Update(gameTime);
                this.fireParticles.Update(gameTime);
                this.engineTrailParticles.Update(gameTime);
                this.star_particles.Update(gameTime);
                this.neb_particles.Update(gameTime);
                this.flameParticles.Update(gameTime);
                this.sparks.Update(gameTime);
                this.lightning.Update(gameTime);
                this.flash.Update(gameTime);
            }
            lock (GlobalStats.ObjectManagerLocker)
            {
                this.ScreenManager.inter.EndFrameRendering();
                this.ScreenManager.editor.EndFrameRendering();
                this.ScreenManager.sceneState.EndFrameRendering();
            }
            this.DrawShields();
            renderState.DepthBufferWriteEnable = true;
        }

        protected void DrawShields()
        {
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            ShieldManager.Draw(view, projection);
        }

        protected virtual void DrawPlanetInfo()
        {
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                if (viewState <= UnivScreenState.SectorView && solarSystem.isVisible)
                {
                    foreach (Planet planet in solarSystem.PlanetList)
                    {
                        float radius = planet.SO.WorldBoundingSphere.Radius;
                        Vector3 vector3_1 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position, 2500f), projection, view, Matrix.Identity);
                        Vector2 vector2_1 = new Vector2(vector3_1.X, vector3_1.Y);
                        Vector3 vector3_2 = ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position.PointOnCircle(90f, radius), 2500f), projection, view, Matrix.Identity);
                        float num1 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2_1) + 10f;
                        Vector2 vector2_2 = new Vector2(vector3_1.X, vector3_1.Y - num1);
                        if (planet.ExploredDict[player])
                        {
                            if (!LookingAtPlanet && viewState < UniverseScreen.UnivScreenState.SectorView && viewState > UniverseScreen.UnivScreenState.ShipView)
                            {
                                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/planetNamePointer"], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Green, 0.0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
                                Vector2 pos1 = new Vector2(vector3_1.X + 20f, vector3_1.Y + 37f);
                                HelperFunctions.ClampVectorToInt(ref pos1);
                                if (planet.Owner == null)
                                    ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma10, planet.Name, pos1, Color.White);
                                else
                                    ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma10, planet.Name, pos1, planet.Owner.EmpireColor);
                                Vector2 pos2 = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
                                int num2 = 0;
                                Vector2 vector2_3 = new Vector2(vector3_1.X + 10f, vector3_1.Y + 60f);
                                if (planet.RecentCombat)
                                {
                                    Rectangle rectangle = new Rectangle((int)vector2_3.X, (int)vector2_3.Y, 14, 14);
                                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_fighting_small"], rectangle, Color.White);
                                    if (HelperFunctions.CheckIntersection(rectangle, pos2))
                                        ToolTip.CreateTooltip(119, ScreenManager);
                                    ++num2;
                                }
                                if (player.data.MoleList.Count > 0)
                                {
                                    foreach (Mole mole in (Array<Mole>)player.data.MoleList)
                                    {
                                        if (mole.PlanetGuid == planet.guid)
                                        {
                                            vector2_3.X = vector2_3.X + (float)(18 * num2);
                                            Rectangle rectangle = new Rectangle((int)vector2_3.X, (int)vector2_3.Y, 14, 14);
                                            ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy_small"], rectangle, Color.White);
                                            ++num2;
                                            if (HelperFunctions.CheckIntersection(rectangle, pos2))
                                            {
                                                ToolTip.CreateTooltip(120, ScreenManager);
                                                break;
                                            }
                                            else
                                                break;
                                        }
                                    }
                                }
                                foreach (Building building in planet.BuildingList)
                                {
                                    if (!string.IsNullOrEmpty(building.EventTriggerUID))
                                    {
                                        vector2_3.X = vector2_3.X + (float)(18 * num2);
                                        Rectangle rectangle = new Rectangle((int)vector2_3.X, (int)vector2_3.Y, 14, 14);
                                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_anomaly_small"], rectangle, Color.White);
                                        if (HelperFunctions.CheckIntersection(rectangle, pos2))
                                        {
                                            ToolTip.CreateTooltip(121, ScreenManager);
                                            break;
                                        }
                                        else
                                            break;
                                    }
                                }
                            }
                        }
                        else if (camHeight < 50000f)
                        {
                            if (planet.Owner != null)
                                continue;
                        }
                        else
                        {
                            Empire empire = planet.Owner;
                        }
                    }
                }
            }
        }

        protected void DrawTransparentModel(Model model, Matrix world, Matrix viewMat, Matrix projMat, Texture2D projTex)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Blend.SourceAlpha;
            renderState.DestinationBlend       = Blend.InverseSourceAlpha;
            renderState.CullMode               = CullMode.None;
            renderState.DepthBufferWriteEnable = false;
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World           = Matrix.CreateScale(50f) * world;
                    basicEffect.View            = viewMat;
                    basicEffect.DiffuseColor    = new Vector3(1f, 1f, 1f);
                    basicEffect.Texture         = projTex;
                    basicEffect.TextureEnabled  = true;
                    basicEffect.Projection      = projMat;
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
            renderState.DepthBufferWriteEnable = true;
        }

        protected void DrawTransparentModelAdditiveNoAlphaFade(Model model, Matrix world, Matrix viewMat, Matrix projMat, Texture2D projTex, float scale)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World           = world;
                    basicEffect.View            = viewMat;
                    basicEffect.Texture         = projTex;
                    basicEffect.DiffuseColor    = new Vector3(1f, 1f, 1f);
                    basicEffect.TextureEnabled  = true;
                    basicEffect.Projection      = projMat;
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
        }

        protected void DrawTransparentModelAdditive(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex, float scale)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World = world;
                    basicEffect.View = view;
                    basicEffect.Texture = projTex;
                    basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                    basicEffect.Alpha = this.camHeight / 3500000f;
                    basicEffect.TextureEnabled = true;
                    basicEffect.Projection = projection;
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
        }

        protected void DrawTransparentModel(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex, float scale)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World = Matrix.CreateScale(50f) * Matrix.CreateScale(scale) * world;
                    basicEffect.View = view;
                    basicEffect.Texture = projTex;
                    basicEffect.Alpha = 1f;
                    basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                    basicEffect.TextureEnabled = true;
                    basicEffect.Projection = projection;
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
            ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        public void DrawSunModel(Matrix world, Texture2D texture, float scale)
        {
            DrawTransparentModel(SunModel, world, view, projection, texture, scale);
        }

        protected void DrawTransparentModel(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex, float scale, Vector3 Color)
        {
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World = Matrix.CreateScale(50f) * Matrix.CreateScale(scale) * world;
                    basicEffect.View = view;
                    basicEffect.DiffuseColor = Color;
                    basicEffect.Texture = projTex;
                    basicEffect.TextureEnabled = true;
                    basicEffect.Projection = projection;
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
            renderState.DepthBufferWriteEnable = true;
        }

        public static FileInfo[] GetFilesFromDirectory(string DirPath)
        {
            return new DirectoryInfo(DirPath).GetFiles("*.*", SearchOption.AllDirectories);
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        protected virtual void Destroy()
        {
            starfield?.Dispose();
            DeepSpaceDone?.Dispose();
            EmpireDone?.Dispose();
            DeepSpaceGateKeeper?.Dispose();
            ItemsToBuild?.Dispose();
            anomalyManager?.Dispose();
            bloomComponent?.Dispose();
            ShipGateKeeper?.Dispose();
            SystemThreadGateKeeper?.Dispose();
            FogMap?.Dispose();
            MasterShipList?.Dispose();
            EmpireGateKeeper?.Dispose();
            BombList?.Dispose();
            flash?.Dispose();
            lightning?.Dispose();
            neb_particles?.Dispose();
            photonExplosionParticles?.Dispose();
            projectileTrailParticles?.Dispose();
            sceneMap?.Dispose();
            shipListInfoUI?.Dispose();
            smokePlumeParticles?.Dispose();
            sparks?.Dispose();
            star_particles?.Dispose();
            engineTrailParticles?.Dispose();
            explosionParticles?.Dispose();
            explosionSmokeParticles?.Dispose();
            fireTrailParticles?.Dispose();
            fireParticles?.Dispose();
            flameParticles?.Dispose();
            beamflashes?.Dispose();
            dsbw?.Dispose();
            SelectedShipList?.Dispose();
            NotificationManager?.Dispose();
            FogMapTarget?.Dispose();
            starfield = null;
            DeepSpaceDone = null;
            EmpireDone = null;
            DeepSpaceGateKeeper = null;
            ItemsToBuild = null;
            anomalyManager = null;
            bloomComponent = null;
            ShipGateKeeper = null;
            SystemThreadGateKeeper = null;
            FogMap = null;
            MasterShipList = null;
            EmpireGateKeeper = null;
            BombList = null;
            flash = null;
            lightning = null;
            neb_particles = null;
            photonExplosionParticles = null;
            projectileTrailParticles = null;
            sceneMap = null;
            shipListInfoUI = null;
            smokePlumeParticles = null;
            sparks = null;
            star_particles = null;
            engineTrailParticles = null;
            explosionParticles = null;
            explosionSmokeParticles = null;
            fireTrailParticles = null;
            fireParticles = null;
            flameParticles = null;
            beamflashes = null;
            dsbw = null;
            SelectedShipList = null;
            NotificationManager = null;
            FogMapTarget = null;
        }

        public struct ClickablePlanets
        {
            public Vector2 ScreenPos;
            public float Radius;
            public Planet planetToClick;
        }

        public struct ClickableShip
        {
            public Vector2 ScreenPos;
            public float Radius;
            public Ship shipToClick;
        }

        protected struct ClickableSystem
        {
            public Vector2 ScreenPos;
            public float Radius;
            public SolarSystem systemToClick;
        }

        public class ClickableItemUnderConstruction
        {
            public Vector2 ScreenPos;
            public Vector2 BuildPos;
            public float Radius;
            public string UID;
            public Goal AssociatedGoal;
        }

        public enum UnivScreenState
        {
            ShipView,
            SystemView,
            SectorView,
            GalaxyView,
        }

        //80000f

        public float GetZfromScreenState(UnivScreenState screenState)
        {
            float returnZ = 0;
            switch (screenState)
            {
                case UnivScreenState.ShipView:
                    returnZ = 30000f; 
                    break;
                case UnivScreenState.SystemView:
                    returnZ = 250000.0f;
                    break;
                case UnivScreenState.SectorView:
                    returnZ = 1775000.0f;
                    break;
                case UnivScreenState.GalaxyView:
                    returnZ = MaxCamHeight;
                    break;
                default:
                    returnZ = 550f;
                    break;
            }
            return returnZ;

        }
        private struct FleetButton
        {
            public Rectangle ClickRect;
            public Fleet Fleet;
            public int Key;
        }

        protected struct MultiShipData
        {
            public float status;
            public Vector2 weightedCenter;
            public float Radius;
        }

        public class FogOfWarNode
        {
            public float Radius = 50000f;
            public Vector2 Position;
            public bool Discovered;
        }

        private struct ClickableFleet
        {
            public Fleet fleet;
            public Vector2 ScreenPos;
            public float ClickRadius;
        }

        private class Intersection
        {
            public Vector2 inter;
            public Circle C1;
            public Circle C2;
            public float Angle;
            public float AngularDistance;
        }

        protected enum CursorState
        {
            Normal,
            Move,
            Follow,
            Attack,
            Orbit,
        }
    }
}

