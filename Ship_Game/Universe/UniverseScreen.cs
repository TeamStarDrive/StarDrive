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
using System.Collections.Concurrent;
using Ship_Game.AI;
using Ship_Game.Debug;

namespace Ship_Game
{
    public class UniverseScreen : GameScreen
    {
        private readonly PerfTimer EmpireUpdatePerf  = new PerfTimer();
        private readonly PerfTimer Perfavg2          = new PerfTimer();
        private readonly PerfTimer PreEmpirePerf     = new PerfTimer();
        private readonly PerfTimer perfavg4          = new PerfTimer();
        private readonly PerfTimer perfavg5          = new PerfTimer();

        public static float GamePaceStatic      = 1f;
        public static float GameScaleStatic     = 1f;
        public static bool ShipWindowOpen       = false;
        public static bool ColonizeWindowOpen   = false;
        public static bool PlanetViewWindowOpen = false;
        public static readonly SpatialManager DeepSpaceManager = new SpatialManager();
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
        public BatchRemovalCollection<ClickableItemUnderConstruction> ItemsToBuild = new BatchRemovalCollection<ClickableItemUnderConstruction>();
        protected Array<ClickableSystem> ClickableSystems    = new Array<ClickableSystem>();
        public BatchRemovalCollection<Ship> SelectedShipList = new BatchRemovalCollection<Ship>();
        protected Array<ClickableShip> ClickableShipsList    = new Array<ClickableShip>();
        protected float PieMenuDelay = 1f;
        protected Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();
        public Background bg            = new Background();
        public Vector2 Size             = new Vector2(5000000f, 5000000f);
        public float FTLModifier        = 1f;
        public float EnemyFTLModifier   = 1f;
        public bool FTLInNuetralSystems = true;
        public UniverseData.GameDifficulty GameDifficulty = UniverseData.GameDifficulty.Normal;
        public Vector3 transitionStartPosition;
        public Vector3 camTransitionPosition;
        public Array<NebulousOverlay> Stars        = new Array<NebulousOverlay>();
        public Array<NebulousOverlay> NebulousShit = new Array<NebulousOverlay>();
        private Rectangle ScreenRectangle;
        public Map<Guid, Planet> PlanetsDict            = new Map<Guid, Planet>();
        public Map<Guid, SolarSystem> SolarSystemDict   = new Map<Guid, SolarSystem>();
        public BatchRemovalCollection<Bomb> BombList    = new BatchRemovalCollection<Bomb>();
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
        private Array<Ship> DeepSpaceShips  = new Array<Ship>();
        private object thislock             = new object();
        public bool ViewingShip             = false;
        public float transDuration          = 3f;
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
        public DebugInfoScreen DebugWin;
        public bool ShowShipNames;
        public InputState Input;
        private float Memory;
        public bool Paused;
        public bool SkipRightOnce;
        private bool UseRealLights = true;
        public bool showdebugwindow;
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
        public int lastshipcombat   = 0;
        public int lastplanetcombat = 0;
        public int reducer          = 1;
        public float screenDelay    = 0f;

        public UniverseScreen() : base(null)
        {
        }

        public UniverseScreen(UniverseData data) : base(null)
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

        public UniverseScreen(UniverseData data, string loyalty) : base(null)
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

        public UniverseScreen(int numsys, float size) : base(null)
        {
            Size.X = size;
            Size.Y = size;
        }

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

            LightRig rig = TransientContent.Load<LightRig>("example/NewGamelight_rig");
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
            SelectedShip.AI.State = AIState.Escort;
            SelectedShip.AI.EscortTarget = this.playerShip;
        }

        public void RefitTo(object sender)
        {
            if (SelectedShip != null)
                ScreenManager.AddScreen(new RefitToWindow(this, SelectedShip));
        }

        public void OrderScrap(object sender)
        {
            SelectedShip.AI.OrderScrapShip();
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
                if (SelectedShip != null && SelectedShip.CargoSpaceMax > 0.0)
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
                        this.ScreenManager.AddScreen(new EncounterPopup(this, player, this.SelectedShip.loyalty, (SolarSystem)null, e));
                        break;
                    }
                }
            }
            else
                this.ScreenManager.AddScreen(new DiplomacyScreen(this, SelectedShip.loyalty, this.player, "Greeting"));
        }

        public void DoHoldPosition(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.HoldPosition();
        }

        public void DoExplore(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.OrderExplore();
        }

        public void DoTransport(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.OrderTransportPassengers(5f);
        }

        public void DoDefense(object sender)
        {
            if (this.SelectedShip == null || this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(this.SelectedShip))
                return;
            this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(this.SelectedShip);
            this.SelectedShip.AI.OrderQueue.Clear();
            this.SelectedShip.AI.HasPriorityOrder = false;
            this.SelectedShip.AI.SystemToDefend = (SolarSystem)null;
            this.SelectedShip.AI.SystemToDefendGuid = Guid.Empty;
            this.SelectedShip.AI.State = AIState.SystemDefender;
        }

        public void DoTransportGoods(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.AI.State = AIState.SystemTrader;
            this.SelectedShip.AI.start = null;
            this.SelectedShip.AI.end = null;
            this.SelectedShip.AI.OrderTrade(5f);
        }

        public void ViewShip(object sender)
        {
            if (this.SelectedShip == null)
                return;
            if (this.playerShip != null && this.SelectedShip == this.playerShip)
            {
                this.playerShip.PlayerShip = false;
                this.playerShip.AI.State = AIState.AwaitingOrders;
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
                    this.playerShip.AI.State = AIState.AwaitingOrders;
                    this.playerShip = this.SelectedShip;
                    this.playerShip.PlayerShip = true;
                    this.playerShip.AI.State = AIState.ManualControl;
                }
                else
                {
                    this.playerShip = this.SelectedShip;
                    this.playerShip.PlayerShip = true;
                    this.playerShip.AI.State = AIState.ManualControl;
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
            ShowShipNames = false;
            if (SelectedPlanet == null)
                return;
            if (!SelectedPlanet.system.ExploredDict[player])
            {
                PlayNegativeSound();
            }
            else
            {
                bool flag = false;
                foreach (Mole mole in player.data.MoleList)
                {
                    if (mole.PlanetGuid == SelectedPlanet.guid)
                    {
                        flag = true;
                        break;
                    }
                }
                workersPanel = SelectedPlanet.Owner == player || flag || Debug && SelectedPlanet.Owner != null 
                    ? new ColonyScreen(SelectedPlanet, ScreenManager, EmpireUI) : (SelectedPlanet.Owner == null 
                    ? new UnexploredPlanetScreen(SelectedPlanet, ScreenManager) : (PlanetScreen)new UnownedPlanetScreen(SelectedPlanet, ScreenManager));
                LookingAtPlanet         = true;
                transitionStartPosition = camPos;
                transitionDestination   = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y + 400f, 2500f);
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
                SelectedShipList.Clear();
                SelectedItem = null;
            }
        }

        public void SnapViewSystem(SolarSystem system, UnivScreenState camHeight)
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
                if (ViewingShip) returnToShip = true;
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
            transitionDestination = new Vector3(system.Position, 147000f);
            ViewingShip           = false;
            AdjustCamTimer        = 1f;
            transDuration         = 3f;
            transitionElapsedTime = 0.0f;
        }

        private void CreateProjectionMatrix()
        {
            float aspect = (float)ScreenManager.GraphicsDevice.Viewport.Width / ScreenManager.GraphicsDevice.Viewport.Height;
            projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspect, 100f, 3E+07f);
        }

        public override void LoadContent()
        {
            GlobalStats.ResearchRootUIDToDisplay = "Colonization";
            SystemInfoUIElement.SysFont = Fonts.Arial12Bold;
            SystemInfoUIElement.DataFont = Fonts.Arial10;
            NotificationManager = new NotificationManager(ScreenManager, this);
            aw = new AutomationWindow(ScreenManager, this);
            for (int i = 0; i < Size.X / 5000.0f; ++i)
            {
                NebulousOverlay nebulousOverlay = new NebulousOverlay();
                float z = RandomMath.RandomBetween(-200000f, -2E+07f);
                nebulousOverlay.Path = "Textures/smoke";
                nebulousOverlay.Position = new Vector3(RandomMath.RandomBetween(-0.5f * Size.X, Size.X + 0.5f * Size.X), RandomMath.RandomBetween(-0.5f * Size.X, Size.X + 0.5f * Size.X), z);
                float radians = RandomMath.RandomBetween(0.0f, 6.283185f);
                nebulousOverlay.Scale = RandomMath.RandomBetween(10f, 100f);
                nebulousOverlay.WorldMatrix = Matrix.CreateScale(50f) * Matrix.CreateScale(nebulousOverlay.Scale) * Matrix.CreateRotationZ(radians) * Matrix.CreateTranslation(nebulousOverlay.Position);
                Stars.Add(nebulousOverlay);
            }
            LoadGraphics();

            DeepSpaceManager.SetupForDeepSpace(Size.X, Size.Y);

            DoParticleLoad();
            bg3d = new Background3D(this);
            starfield = new Starfield(Vector2.Zero, ScreenManager.GraphicsDevice, TransientContent);
            starfield.LoadContent();
            GameplayObject.audioListener = listener;
            Weapon.audioListener         = listener;
            GameplayObject.audioListener = listener;

            CreateProjectionMatrix();
            SetLighting(UseRealLights);
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                foreach (string FleetUID in solarSystem.DefensiveFleets)
                {
                    Fleet defensiveFleetAt = HelperFunctions.CreateDefensiveFleetAt(FleetUID, EmpireManager.Remnants, solarSystem.PlanetList[0].Position);
                    MilitaryTask militaryTask = new MilitaryTask();
                    militaryTask.AO = solarSystem.PlanetList[0].Position;
                    militaryTask.AORadius = 120000f;
                    militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                    defensiveFleetAt.FleetTask = militaryTask;
                    defensiveFleetAt.TaskStep = 3;
                    militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
                    EmpireManager.Remnants.GetFleetsDict().Add(EmpireManager.Remnants.GetFleetsDict().Count + 10, defensiveFleetAt);
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
                            planetFleetAt.FleetTask = militaryTask;
                            planetFleetAt.TaskStep = 3;
                            militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
                            EmpireManager.Remnants.GetFleetsDict().Add(EmpireManager.Remnants.GetFleetsDict().Count + 10, planetFleetAt);
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
                    defensiveFleetAt.FleetTask = militaryTask;
                    defensiveFleetAt.TaskStep = 3;
                    militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
                    EmpireManager.Remnants.GetFleetsDict().Add(EmpireManager.Remnants.GetFleetsDict().Count + 10, defensiveFleetAt);
                    EmpireManager.Remnants.GetGSAI().TaskList.Add(militaryTask);
                    militaryTask.Step = 2;
                }
                foreach (string key in solarSystem.ShipsToSpawn)
                    Ship.CreateShipAt(key, EmpireManager.Remnants, solarSystem.PlanetList[0], true);
                foreach (Planet p in solarSystem.PlanetList)
                {
                    if (p.Owner != null)
                    {
                        foreach (string key in p.Guardians)
                            Ship.CreateShipAt(key, p.Owner, p, true);
                    }
                    else
                    {
                        // Added by McShooterz: alternate hostile fleets populate universe
                        if (GlobalStats.HasMod && ResourceManager.HostileFleets.Fleets.Count > 0)
                        {
                            if (p.Guardians.Count > 0)
                            {
                                int randomFleet  = RandomMath.InRange(ResourceManager.HostileFleets.Fleets.Count);
                                var hostileFleet = ResourceManager.HostileFleets.Fleets[randomFleet];
                                var empire       = EmpireManager.GetEmpireByName(hostileFleet.Empire);
                                foreach (string ship in hostileFleet.Ships)
                                {
                                    Ship.CreateShipAt(ship, empire, p, true);
                                }
                            }
                        }
                        else
                        {
                            // Remnants or Corsairs may be null if Mods disable default Races
                            if (EmpireManager.Remnants != null)
                            {
                                foreach (string key in p.Guardians)
                                    Ship.CreateShipAt(key, EmpireManager.Remnants, p, true);
                            }
                            if (p.CorsairPresence && EmpireManager.Corsairs != null)
                            {
                                Ship.CreateShipAt("Corsair Asteroid Base", EmpireManager.Corsairs, p, true).TetherToPlanet(p);
                                Ship.CreateShipAt("Corsair", EmpireManager.Corsairs, p, true);
                                Ship.CreateShipAt("Captured Gunship", EmpireManager.Corsairs, p, true);
                                Ship.CreateShipAt("Captured Gunship", EmpireManager.Corsairs, p, true);
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
            var content = TransientContent;
            var device = ScreenManager.GraphicsDevice;
            beamflashes              = new ParticleSystem(content, "3DParticles/BeamFlash", device);
            explosionParticles       = new ParticleSystem(content, "3DParticles/ExplosionSettings", device);
            photonExplosionParticles = new ParticleSystem(content, "3DParticles/PhotonExplosionSettings", device);
            explosionSmokeParticles  = new ParticleSystem(content, "3DParticles/ExplosionSmokeSettings", device);
            projectileTrailParticles = new ParticleSystem(content, "3DParticles/ProjectileTrailSettings", device);
            fireTrailParticles       = new ParticleSystem(content, "3DParticles/FireTrailSettings", device);
            smokePlumeParticles      = new ParticleSystem(content, "3DParticles/SmokePlumeSettings", device);
            fireParticles            = new ParticleSystem(content, "3DParticles/FireSettings", device);
            engineTrailParticles     = new ParticleSystem(content, "3DParticles/EngineTrailSettings", device);
            flameParticles           = new ParticleSystem(content, "3DParticles/FlameSettings", device);
            sparks                   = new ParticleSystem(content, "3DParticles/sparks", device);
            lightning                = new ParticleSystem(content, "3DParticles/lightning", device);
            flash                    = new ParticleSystem(content, "3DParticles/FlashSettings", device);
            star_particles           = new ParticleSystem(content, "3DParticles/star_particles", device);
            neb_particles            = new ParticleSystem(content, "3DParticles/GalaxyParticle", device);
        }

        public void LoadGraphics()
        {
            const int minimapOffSet = 14;

            var content = TransientContent;
            var device  = ScreenManager.GraphicsDevice;
            int width   = device.PresentationParameters.BackBufferWidth;
            int height  = device.PresentationParameters.BackBufferHeight;

            MuzzleFlashManager.universeScreen     = this;
            DroneAI.UniverseScreen                = this;
            ExplosionManager.Universe             = this;
            Fleet.Screen                          = this;
            Bomb.Screen                           = this;
            Anomaly.screen                        = this;
            MinimapButtons.screen                 = this;
            Empire.Universe                       = this;
            ResourceManager.UniverseScreen        = this;
            Empire.Universe                   = this;
            ArtificialIntelligence.universeScreen = this;
            FleetDesignScreen.screen              = this;

            CreateProjectionMatrix();
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
            EmpireUI           = new EmpireUIOverlay(player, device);
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
                    if (ProcessTurnsThread == null)
                        return; // this thread is aborting

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
                            ship.Update(0);
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
                        throw; // the loop is having a cyclic crash, no way to recover
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
                SolarSystem ss = node.SourceObject as SolarSystem;
                Planet p = node.SourceObject as Planet;
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
                cy = cy < 0 ? 0 : cy;
                cy = cy > granularity*2 ? granularity*2 : cy;
                cx = cx < 0 ? 0 : cx;
                cx = cx > granularity*2 ? granularity*2 : cx;
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
                        Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants, player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f), RandomMath.RandomBetween(-500000f, 500000f))).AI.DefaultAIState = AIState.Exterminate;
                }
            }


            while (!ShipsToRemove.IsEmpty)
            {
                ShipsToRemove.TryTake(out Ship remove);
                remove.TotallyRemove();
            }
            MasterShipList.ApplyPendingRemovals();
            
            if (!Paused)
            {
                bool rebuild = false;
                //System.Threading.Tasks.Parallel.ForEach(EmpireManager.Empires, empire =>
                //Parallel.For(EmpireManager.Empires.Count, (start, end) =>
                {
                    //for (int i = start; i < end; i++)
                    for (int i = 0; i < EmpireManager.Empires.Count; i++)
                    {
                        var empire = EmpireManager.Empires[i];
                        foreach (Ship s in empire.ShipsToAdd)
                        {
                            empire.AddShip(s);
                            if (!empire.isPlayer) empire.ForcePoolAdd(s);
                        }

                        empire.ShipsToAdd.Clear();
                        empire.updateContactsTimer -= 0.01666667f;//elapsedTime;
                        if (empire.updateContactsTimer <= 0f && !empire.data.Defeated)
                        {
                            int check = empire.BorderNodes.Count;
                            empire.ResetBorders();

                            if (empire.BorderNodes.Count != check)
                            {
                                rebuild = true;
                                empire.PathCache.Clear();
                            }
                            foreach (Ship ship in MasterShipList)
                            {
                                //added by gremlin reset border stats.
                                ship.BorderCheck.Remove(empire);
                            }

                            empire.UpdateKnownShips();
                            empire.updateContactsTimer = elapsedTime + RandomMath.RandomBetween(2f, 3.5f);
                        }
                    }
                    
                }//);
                if (rebuild)
                {
                    reducer = (int) (Empire.ProjectorRadius*.75f);
                    int granularity = (int)(Size.X / reducer);
                    int elegran = granularity*2;
                    int elements = elegran < 128 ? 128 : elegran < 256 ? 256 : elegran < 512 ? 512 : 1024;
                    byte[,] grid = new byte[elements, elements];
                    for (int x = 0; x < elements; x++)
                        for (int y = 0; y < elements; y++)
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
                    //System.Threading.Tasks.Parallel.ForEach(EmpireManager.Empires, empire =>
                    //Parallel.For(EmpireManager.Empires.Count, (start, end) =>
                    {
                        //for (int i = start; i < end; i++)
                        for (int i = 0; i < EmpireManager.Empires.Count; i++)
                        {
                            var empire = EmpireManager.Empires[i];

                            byte[,] grid1 = (byte[,])grid.Clone();
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
                        }
                    }//);
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
                    }
                    ShipsToAdd.Clear();
                }
                shiptimer -= elapsedTime; // 0.01666667f;//
                EmpireUpdatePerf.Stop();
            }

            #endregion

       
            perfavg4.Start();
            #region Mid
            if (elapsedTime > 0.0f && shiptimer <= 0.0f)
            {
                shiptimer = 1f;
                //foreach (Ship ship in (Array<Ship>)this.MasterShipList)
                //var source = Enumerable.Range(0, this.MasterShipList.Count).ToArray();
                //var rangePartitioner = Partitioner.Create(0, source.Length);

                //System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (range, loopState) =>
                //Parallel.For(this.MasterShipList.Count, (start, end) =>
                {
                    //for (int i = start; i < end; i++)
                    for (int i = 0; i < MasterShipList.Count; i++)
                    {                                               
                        Ship ship = MasterShipList[i];
                        foreach (SolarSystem system in SolarSystemList)
                        {
                            if (ship.Position.InRadius(system.Position, 100000.0f))
                            {
                                system.ExploredDict[ship.loyalty] = true;
                                ship.SetSystem(system);
                                break; // No need to keep looping through all other systems if one is found -Gretman
                            }
                        }
                        if(ship.System == null)
                            ship.SetSystem(null); //Add ships to deepspacemanageer if system is null. Ships are not getting added to the deepspace manager from here. 
                    }
                }//);
            }

            //System.Threading.Tasks.Parallel.ForEach(EmpireManager.Empires, empire =>
            //Parallel.For(EmpireManager.Empires.Count, (start, end) =>
            {
                //for (int i = start; i < end; i++)
                for (int i = 0; i < EmpireManager.Empires.Count; i++)
                {
                    foreach (var kv in EmpireManager.Empires[i].GetFleetsDict())
                    {
                        var fleet = kv.Value;
                        if (fleet.Ships.Count <= 0)
                            continue;
                        using (fleet.Ships.AcquireReadLock())
                        {
                            fleet.Setavgtodestination();
                            fleet.SetSpeed();
                            fleet.StoredFleetPosition = fleet.FindAveragePositionset();
                        }
                    }
                }
            }//);

            GlobalStats.BeamTests = 0;
            GlobalStats.Comparisons = 0;
            ++GlobalStats.ComparisonCounter;
            GlobalStats.ModuleUpdates = 0;
            GlobalStats.ModulesMoved = 0;
            #endregion
            perfavg4.Stop();


            Perfavg2.Start();
            #region Ships

#if !PLAYERONLY

            DeepSpaceThread();

            //Parallel.For(SolarSystemList.Count, (first, last) =>
            {
                //for (int i = first; i < last; i++)
                for (int i = 0; i < SolarSystemList.Count; i++)
                {
                    SystemUpdaterTaskBased(SolarSystemList[i]);
                }

            }//);

            /*
            Array<SolarSystem> peacefulSystems = new Array<SolarSystem>(SolarSystemList.Count / 2);
            Array<SolarSystem> combatSystems   = new Array<SolarSystem>(SolarSystemList.Count / 2);

            foreach (SolarSystem system in SolarSystemList)
            {
                int shipsInCombat = 0;
                foreach (Ship ship in system.ShipList)
                    if (ship.InCombatTimer == 15 && ++shipsInCombat >= 5)
                        break;
                (shipsInCombat < 5 ? peacefulSystems : combatSystems).Add(system);
            }

            //FleetTask DeepSpaceTask = FleetTask.Factory.StartNew(() =>
            {
                DeepSpaceThread();
                foreach (SolarSystem system in combatSystems)
                {
                    SystemUpdaterTaskBased(system);
                }
            }//);

            #if true // use multithreaded update loop
                //var source1 = Enumerable.Range(0, peacefulSystems.Count).ToArray();
                //var normalsystems = Partitioner.Create(0, source1.Length);
                //System.Threading.Tasks.Parallel.ForEach(normalsystems, (range, loopState) =>
                {
                    //standard for loop through each weapon group.
                    //for (int T = range.Item1; T < range.Item2; T++)
                    for (int T = 0; T < peacefulSystems.Count; T++)
                    {
                        SystemUpdaterTaskBased(peacefulSystems[T]);
                    }
                }//);
#else
                foreach(SolarSystem s in peacefulSystems)
                {
                    SystemUpdaterTaskBased(s);
                }
                */
#endif

            //The two above were the originals

            //if (DeepSpaceTask != null)
            //    DeepSpaceTask.Wait();
//#endif
#if PLAYERONLY
            FleetTask DeepSpaceTask = FleetTask.Factory.StartNew(this.DeepSpaceThread);
            foreach (SolarSystem solarsystem in this.SolarSystemDict.Values)
            {
                SystemUpdaterTaskBased(solarsystem);
            }
            if (DeepSpaceTask != null)
                DeepSpaceTask.Wait();
#endif

#endregion

            Perfavg2.Stop();

            #region end

            //Log.Info(this.zgameTime.TotalGameTime.Seconds - elapsedTime);
            //System.Threading.Tasks.Parallel.Invoke(() =>
            {
                if (elapsedTime > 0f)
                {
                    DeepSpaceManager.Update(elapsedTime, null);
                }
            }//,
            //() =>
            {
                //lock (GlobalStats.ClickableItemLocker)
                this.UpdateClickableItems();
                if (this.LookingAtPlanet)
                    this.workersPanel.Update(elapsedTime);
                bool flag1 = false;
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int i = 0; i < this.ClickPlanetList.Count; ++i)
                    {
                        try
                        {
                            UniverseScreen.ClickablePlanets local_12 = this.ClickPlanetList[i];
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
            //);
            if (elapsedTime > 0)
            {
                ShieldManager.Update();
                FTLManager.Update(elapsedTime);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(elapsedTime);
            }
            this.SelectedShipList.ApplyPendingRemovals();
            this.MasterShipList.ApplyPendingRemovals();
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
                    else if (this.viewState <= UniverseScreen.UnivScreenState.ShipView)
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
                    if (system.Explored(player) && viewing)
                    {
                        system.isVisible = viewState <= UnivScreenState.SectorView;
                    }
                    if (system.isVisible && viewState <= UnivScreenState.SystemView)
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

                    for (int i = 0; i < system.ShipList.Count; ++i)
                    {
                        Ship ship = system.ShipList[i];
                        if (ship.System == null)
                            continue;
                        if (!ship.Active || ship.ModuleSlotList.Length == 0) // added by gremlin ghost ship killer
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
                    if (!Paused && IsActive)
                        system.spatialManager.Update(elapsedTime, system);
                    system.AsteroidsList.ApplyPendingRemovals();
                }//);
                // this.SystemResetEvents[list[0].IndexOfResetEvent].Set();
            }
        }

        private void DeepSpaceThread()
        {
            float elapsedTime = !Paused ? 0.01666667f : 0.0f;

            DeepSpaceManager.GetDeepSpaceShips(DeepSpaceShips);

            //Parallel.For(0, DeepSpaceShips.Count, (start, end) =>
            {
                //for (int i = start; i < end; i++)
                for (int i = 0; i < DeepSpaceShips.Count; i++)
                {
                    if (!DeepSpaceShips[i].shipInitialized)
                        continue;

                    if (DeepSpaceShips[i].Active && DeepSpaceShips[i].ModuleSlotList.Length != 0)
                    {
                        if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                        {
                            DeepSpaceShips[i].Inhibited = true;
                            DeepSpaceShips[i].InhibitedTimer = 10f;
                        }

                        if (DeepSpaceShips[i].PlayerShip)
                            DeepSpaceShips[i].ProcessInput(elapsedTime);
                    }
                    else
                    {
                        DeepSpaceShips[i].Die(null, true);
                    }
                    DeepSpaceShips[i].Update(elapsedTime);
                }
            }//);
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
                else if (viewState <= UnivScreenState.ShipView)
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
                if (system.Explored(this.player) && inFrustrum)
                {
                        system.isVisible =camHeight < GetZfromScreenState(UnivScreenState.GalaxyView); 
                }
                if (system.isVisible && camHeight < GetZfromScreenState(UnivScreenState.SystemView) )
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
                if (system.isVisible && camHeight < GetZfromScreenState(UnivScreenState.SystemView))
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                        asteroid.Update(elapsedTime);
                }
                system.AsteroidsList.ApplyPendingRemovals();
            }
        }

        protected virtual void AdjustCamera(float elapsedTime)
        {
            if (this.ShipToView == null)
                this.ViewingShip = false;

            
            #if DEBUG
                float minCamHeight = 400.0f;
            #else
                float minCamHeight = Debug ? 1337.0f : 400.0f;
            #endif

            this.AdjustCamTimer -= elapsedTime;
            if (this.ViewingShip && !this.snappingToShip)
            {
                this.camPos.X = this.ShipToView.Center.X;
                this.camPos.Y = this.ShipToView.Center.Y;
                this.camHeight = (float)(int)MathHelper.SmoothStep(this.camHeight, this.transitionDestination.Z, 0.2f);
                if (camHeight < minCamHeight)
                    camHeight = minCamHeight;
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
                if (camHeight < minCamHeight)
                    camHeight = minCamHeight;
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
                if (camHeight < minCamHeight)
                    camHeight = minCamHeight;
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
            else if (camHeight < minCamHeight)
                camHeight = minCamHeight;
            foreach(UnivScreenState screenHeight in Enum.GetValues(typeof(UnivScreenState)))
            {
                if (camHeight <= GetZfromScreenState(screenHeight))
                {
                    viewState = screenHeight;
                    break;
                }
                
            }

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

        private void ToggleUIComponent(string audioCue, ref bool toggle)
        {
            AudioManager.PlayCue(audioCue);
            toggle = !toggle;
        }

        private void HandleInputNotLookingAtPlanet(InputState input)
        {
            mouseWorldPos = UnprojectToWorldPosition(input.MouseScreenPos);
            if (input.DeepSpaceBuildWindow)
            {
                if (!showingDSBW)
                {
                    dsbw = new DeepSpaceBuildingWindow(ScreenManager, this);
                    AudioManager.PlayCue("echo_affirm");
                    showingDSBW = true;
                }
                else
                    showingDSBW = false;
            }
            if (input.PlanetListScreen)
            {
                AudioManager.PlayCue("sd_ui_accept_alt3");
                ScreenManager.AddScreen(new PlanetListScreen(this, EmpireUI));
            }

            if (input.FTLOverlay) ToggleUIComponent("sd_ui_accept_alt3", ref showingFTLOverlay);
            if (input.RangeOverlay) ToggleUIComponent("sd_ui_accept_alt3", ref showingRangeOverlay);
            //if (input.FTLOverlay)
            //{
            //    AudioManager.PlayCue("sd_ui_accept_alt3");
            //    showingFTLOverlay = !showingFTLOverlay;                    
            //}
            //if (input.RangeOverlay)
            //{
            //    AudioManager.PlayCue("sd_ui_accept_alt3");
            //    showingRangeOverlay = !showingRangeOverlay;                    
            //}
            if (input.ShipListScreen)
            {
                AudioManager.PlayCue("sd_ui_accept_alt3");
                ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI));
            }
            if (input.FleetDesignScreen)
            {
                AudioManager.PlayCue("sd_ui_accept_alt3");
                ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI));
                FleetDesignScreen.Open = true;
            }
            if (input.AutomationWindow)
            {
                AudioManager.PlayCue("sd_ui_accept_alt3");
                aw.isOpen = !aw.isOpen;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.PageUp) && !input.LastKeyboardState.IsKeyDown(Keys.PageUp))
            {
                AudioManager.PlayCue("sd_ui_accept_alt3");
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                transitionDestination.Z = 4500f;
                snappingToShip = true;
                ViewingShip = true;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.PageDown) && !input.LastKeyboardState.IsKeyDown(Keys.PageDown))
            {
                AudioManager.PlayCue("sd_ui_accept_alt3");
                AdjustCamTimer = 1f;
                transitionElapsedTime = 0.0f;
                transitionDestination.X = camPos.X;
                transitionDestination.Y = camPos.Y;
                transitionDestination.Z = 4200000f * UniverseScreen.GameScaleStatic;
            }
            if (Debug)
            {
                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.C) && !input.LastKeyboardState.IsKeyDown(Keys.C))
                    Ship.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", EmpireManager.Remnants, mouseWorldPos);
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.C) && !input.LastKeyboardState.IsKeyDown(Keys.C))
                    Ship.CreateShipAtPoint("Bondage-Class Mk IIIa Cruiser", player, mouseWorldPos);

                try
                {

                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.Z) && !input.LastKeyboardState.IsKeyDown(Keys.Z))
                        HelperFunctions.CreateFleetAt("Fleet 2", EmpireManager.Remnants, mouseWorldPos);
                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && !input.LastKeyboardState.IsKeyDown(Keys.Z))
                        HelperFunctions.CreateFleetAt("Fleet 1", player, mouseWorldPos);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Fleet creation failed");
                }
                if (SelectedShip != null && Debug)
                {
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X))
                    {
                        foreach (ShipModule mod in SelectedShip.ModuleSlotList)
                        { mod.Health = 1; } //Added by Gretman so I can hurt ships when the disobey me... I mean for testing... Yea, thats it...
                        SelectedShip.Health = SelectedShip.ModuleSlotList.Length;
                    }
                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X))
                        SelectedShip.Die(null, false);
                }
                else if (SelectedPlanet != null && Debug && (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X)))
                {
                    foreach (string troopType in ResourceManager.TroopTypes)
                        SelectedPlanet.AssignTroopToTile(ResourceManager.CreateTroop(troopType, EmpireManager.Remnants));
                }

                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.V) && !input.LastKeyboardState.IsKeyDown(Keys.V))
                    Ship.CreateShipAtPoint("Remnant Mothership", EmpireManager.Remnants, mouseWorldPos);
                else if (input.CurrentKeyboardState.IsKeyDown(Keys.V) && !input.LastKeyboardState.IsKeyDown(Keys.V))
                    Ship.CreateShipAtPoint("Target Dummy", EmpireManager.Remnants, mouseWorldPos);

                //This little sections added to stress-test the resource manager, and load lots of models into memory.      -Gretman
                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentKeyboardState.IsKeyDown(Keys.B) && !input.LastKeyboardState.IsKeyDown(Keys.B))
                {
                    if (DebugInfoScreen.Loadmodels == 5)    //Repeat
                        DebugInfoScreen.Loadmodels = 0;

                    if (DebugInfoScreen.Loadmodels == 4)    //Capital and Carrier
                    {
                        Ship.CreateShipAtPoint("Mordaving L", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Revenant-Class Dreadnought", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Draylok Warbird", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Archangel-Class Dreadnought", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Zanbato-Class Mk IV Battleship", player, mouseWorldPos);    //Kulrathi
                        Ship.CreateShipAtPoint("Tarantula-Class Mk V Battleship", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Black Widow-Class Dreadnought", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Corpse Flower III", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Wolfsbane-Class Mk III Battleship", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Sceptre Torp", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Devourer-Class Mk V Battleship", player, mouseWorldPos);    //Vulfen
                        Ship.CreateShipAtPoint("SS-Fighter Base Alpha", player, mouseWorldPos);    //Station
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 3)    //Cruiser
                    {
                        Ship.CreateShipAtPoint("Storving Laser", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Draylok Bird of Prey", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Terran Torpedo Cruiser", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Terran Inhibitor", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Mauler Carrier", player, mouseWorldPos);    //Kulrathi
                        Ship.CreateShipAtPoint("Chitin Cruiser Zero L", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Doom Flower", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Missile Acolyte II", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Ancient Torpedo Cruiser", player, mouseWorldPos);    //Remnant
                        Ship.CreateShipAtPoint("Type X Artillery", player, mouseWorldPos);    //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 2)    //Frigate
                    {
                        Ship.CreateShipAtPoint("Owlwok Beamer", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Scythe Torpedo", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Laser Frigate", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Missile Corvette", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Kulrathi Railer", player, mouseWorldPos);    //Kulrathi
                        Ship.CreateShipAtPoint("Stormsoldier", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Fern Artillery", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Adv Zion Railer", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Corsair", player, mouseWorldPos);    //Remnant
                        Ship.CreateShipAtPoint("Type VII Laser", player, mouseWorldPos);    //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 1)    //Corvette
                    {
                        Ship.CreateShipAtPoint("Laserlitving I", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Crescent Rocket", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Missile Hunter", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Razor RS", player, mouseWorldPos);    //Kulrathi
                        Ship.CreateShipAtPoint("Armored Worker", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Thicket Attack Fighter", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Ralyeh Railship", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Heavy Drone", player, mouseWorldPos);    //Remnant
                        Ship.CreateShipAtPoint("Grinder", player, mouseWorldPos);    //Vulfen
                        Ship.CreateShipAtPoint("Stalker III Hvy Laser", player, mouseWorldPos);    //Vulfen
                        Ship.CreateShipAtPoint("Listening Post", player, mouseWorldPos);    //Platform
                        ++DebugInfoScreen.Loadmodels;
                    }

                    if (DebugInfoScreen.Loadmodels == 0)    //Fighters and freighters
                    {
                        Ship.CreateShipAtPoint("Laserving", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter S", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter M", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Owlwok Freighter L", player, mouseWorldPos);    //Cordrazine
                        Ship.CreateShipAtPoint("Laserwisp", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Draylok Transporter", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Draylok Medium Trans", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Draylok Mobilizer", player, mouseWorldPos);    //Draylock
                        Ship.CreateShipAtPoint("Rocket Scout", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Small Transport", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Medium Transport", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Large Transport", player, mouseWorldPos);    //Human
                        Ship.CreateShipAtPoint("Flak Fang", player, mouseWorldPos);    //Kulrathi
                        Ship.CreateShipAtPoint("Drone Railer", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Creeper Transport", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Crawler Transport", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Trawler Transport", player, mouseWorldPos);    //Opteris
                        Ship.CreateShipAtPoint("Rocket Thorn", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Seeder Transport", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Sower Transport", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Grower Transport", player, mouseWorldPos);    //Pollops
                        Ship.CreateShipAtPoint("Ralyeh Interceptor", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Vessel S", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Vessel M", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Vessel L", player, mouseWorldPos);    //Rayleh
                        Ship.CreateShipAtPoint("Xeno Fighter", player, mouseWorldPos);    //Remnant
                        Ship.CreateShipAtPoint("Type I Vulcan", player, mouseWorldPos);    //Vulfen
                        ++DebugInfoScreen.Loadmodels;
                    }
                }
            }
            HandleFleetSelections(input);
            if (input.Escaped)
            {
                snappingToShip = false;
                ViewingShip = false;
                if (camHeight < GetZfromScreenState(UnivScreenState.GalaxyView) && camHeight > GetZfromScreenState(UnivScreenState.SectorView))
                {
                    AdjustCamTimer = 1f;
                    transitionElapsedTime = 0.0f;
                    transitionDestination = new Vector3(camPos.X, camPos.Y, 1175000f);
                }
                else if (camHeight > GetZfromScreenState(UnivScreenState.ShipView))
                {
                    AdjustCamTimer = 1f;
                    transitionElapsedTime = 0.0f;
                    transitionDestination = new Vector3(camPos.X, camPos.Y, 147000f);
                }
                else if (viewState < UniverseScreen.UnivScreenState.SystemView)
                    transitionDestination = new Vector3(camPos.X, camPos.Y, GetZfromScreenState(UnivScreenState.SystemView));
            }
            if (input.Tab)
                ShowShipNames = !ShowShipNames;
            HandleRightMouseNew(input);
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
            {
                if ((double)ClickTimer < (double)TimerDelay)
                {
                    SelectedShipList.Clear();
                    if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                        previousSelection = SelectedShip;
                    SelectedShip = (Ship)null;
                    if (viewState <= UniverseScreen.UnivScreenState.SystemView)
                    {
                        foreach (UniverseScreen.ClickablePlanets clickablePlanets in ClickPlanetList)
                        {
                            if ((double)Vector2.Distance(input.CursorPosition, clickablePlanets.ScreenPos) <= (double)clickablePlanets.Radius)
                            {
                                AudioManager.PlayCue("sub_bass_whoosh");
                                SelectedPlanet = clickablePlanets.planetToClick;
                                if (!SnapBackToSystem)
                                    HeightOnSnap = camHeight;
                                ViewPlanet((object)SelectedPlanet);
                            }
                        }
                    }
                    foreach (ClickableShip clickableShip in ClickableShipsList)
                    {
                        if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                        {
                            pickedSomethingThisFrame = true;
                            SelectedShipList.Add(clickableShip.shipToClick);

                            foreach (ClickableShip ship in ClickableShipsList)
                            {
                                if (clickableShip.shipToClick != ship.shipToClick &&
                                    ship.shipToClick.loyalty == clickableShip.shipToClick.loyalty &&
                                    ship.shipToClick.shipData.Role == clickableShip.shipToClick.shipData.Role)
                                {
                                    SelectedShipList.Add(ship.shipToClick);
                                }
                            }
                            break;
                        }
                    }
                    if (viewState > UniverseScreen.UnivScreenState.SystemView)
                    {
                        lock (GlobalStats.ClickableSystemsLock)
                        {
                            for (int i = 0; i < ClickableSystems.Count; ++i)
                            {
                                UniverseScreen.ClickableSystem local_28 = ClickableSystems[i];
                                if ((double)Vector2.Distance(input.CursorPosition, local_28.ScreenPos) <= (double)local_28.Radius)
                                {
                                    if (local_28.systemToClick.ExploredDict[player])
                                    {
                                        AudioManager.GetCue("sub_bass_whoosh").Play();
                                        HeightOnSnap = camHeight;
                                        ViewSystem(local_28.systemToClick);
                                    }
                                    else
                                        PlayNegativeSound();
                                }
                            }
                        }
                    }
                }
                else if (SelectedShip != null)
                    ClickTimer = 0.0f;
                //ClickTimer = 0.5f;
            }
            HandleSelectionBox(input);
            HandleScrolls(input);
        }

        private void HandleInputLookingAtPlanet(InputState input)
        {
            if (input.Tab)
                ShowShipNames = !ShowShipNames;
            if ((input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released || workersPanel is ColonyScreen && (workersPanel as ColonyScreen).close.HandleInput(input)) && (!(workersPanel is ColonyScreen) || !(workersPanel as ColonyScreen).ClickedTroop))
            {
                if (workersPanel is ColonyScreen && (workersPanel as ColonyScreen).p.Owner == null)
                {
                    AdjustCamTimer = 1f;
                    if (returnToShip)
                    {
                        ViewingShip = true;
                        returnToShip = false;
                        snappingToShip = true;
                        transitionDestination.Z = transitionStartPosition.Z;
                    }
                    else
                        transitionDestination = transitionStartPosition;
                    transitionElapsedTime = 0.0f;
                    LookingAtPlanet = false;
                }
                else
                {
                    AdjustCamTimer = 1f;
                    if (returnToShip)
                    {
                        ViewingShip = true;
                        returnToShip = false;
                        snappingToShip = true;
                        transitionDestination.Z = transitionStartPosition.Z;
                    }
                    else
                        transitionDestination = transitionStartPosition;
                    transitionElapsedTime = 0.0f;
                    LookingAtPlanet = false;
                }
            }
        }
        private void PreviousTargetSelection(InputState input)
        {
            if (previousSelection.Active)
            {
                Ship tempship = previousSelection;
                if (SelectedShip != null && SelectedShip != previousSelection)
                    previousSelection = SelectedShip;
                SelectedShip = tempship;
                ShipInfoUIElement.SetShip(SelectedShip);
                SelectedFleet = (Fleet)null;
                SelectedShipList.Clear();
                SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                SelectedSystem = (SolarSystem)null;
                SelectedPlanet = (Planet)null;
                SelectedShipList.Add(SelectedShip);
                //snappingToShip = false;
                ViewingShip = false;
                return;
            }
            else
                previousSelection = null;  //fbedard: remove inactive ship
        }
        private void ChaseCame()
        {
            if (!ViewingShip)
            {
                ViewToShip(null);
            }
            ViewingShip = !ViewingShip;
        }
        private void ReportManual(string reportType, bool kudos) //@TODO this should be mostly moved to a exception tracker constructor i think. 
        {
            bool switchedmode = false;
#if RELEASE //only switch screens in release
                
                if (Game1.Instance.graphics.IsFullScreen)
                {
                    switchedmode = true;
                    Game1.Instance.graphics.ToggleFullScreen();
                }
#endif
            Exception ex = new Exception(reportType);
            ExceptionTracker.TrackException(ex);
            ExceptionTracker.Kudos = kudos;
            bool wasPaused = Paused;
            Paused = true;
            ExceptionTracker.DisplayException(ex);
            Paused = wasPaused;

            if (switchedmode)
            {
                switchedmode = false;
                Game1.Instance.Graphics.ToggleFullScreen();
            }
        }
        
        private void CycleShipsInCombat(InputState input)
        {
            ShipsInCombat.State = UIButton.PressState.Hover;
            ToolTip.CreateTooltip("Cycle through ships not in fleet that are in combat", ScreenManager);
            if (input.InGameSelect)
            {
                if (player.empireShipCombat > 0)
                {
                    AudioManager.PlayCue("echo_affirm");
                    int nbrship = 0;
                    if (lastshipcombat >= player.empireShipCombat)
                        lastshipcombat = 0;
                    foreach (Ship ship in EmpireManager.Player.GetShips())
                    {
                        if (ship.fleet != null || !ship.InCombat || ship.Mothership != null || !ship.Active)
                            continue;
                        else
                        {
                            if (nbrship == lastshipcombat)
                            {
                                if (SelectedShip != null && SelectedShip != previousSelection && SelectedShip != ship)
                                    previousSelection = SelectedShip;
                                SelectedShip = ship;
                                ViewToShip(null);
                                SelectedShipList.Add(SelectedShip);
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
        private void CyclePlanetsInCombat(InputState input)
        {
            PlanetsInCombat.State = UIButton.PressState.Hover;
            ToolTip.CreateTooltip("Cycle through planets that are in combat", ScreenManager);
            if (input.InGameSelect)
            {
                if (player.empirePlanetCombat > 0)
                {
                    AudioManager.PlayCue("echo_affirm");
                    Planet PlanetToView = (Planet)null;
                    int nbrplanet = 0;
                    if (lastplanetcombat >= player.empirePlanetCombat)
                        lastplanetcombat = 0;
                    bool flagPlanet;

                    foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                    {
                        foreach (Planet p in system.PlanetList)
                        {
                            if (p.IsExploredBy(EmpireManager.Player) && p.RecentCombat)
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
                        if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                            previousSelection = SelectedShip;
                        SelectedShip = (Ship)null;
                        SelectedFleet = (Fleet)null;
                        SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                        SelectedSystem = (SolarSystem)null;
                        SelectedPlanet = PlanetToView;
                        SelectedShipList.Clear();
                        pInfoUI.SetPlanet(PlanetToView);
                        lastplanetcombat++;

                        transitionDestination = new Vector3(SelectedPlanet.Position.X, SelectedPlanet.Position.Y, 9000f);
                        LookingAtPlanet = false;
                        transitionStartPosition = camPos;
                        AdjustCamTimer = 2f;
                        transitionElapsedTime = 0.0f;
                        transDuration = 5f;
                        returnToShip = false;
                        ViewingShip = false;
                        snappingToShip = false;
                        SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                    }
                }
                else
                {
                    AudioManager.PlayCue("blip_click");
                }
            }
        }
        private void ResetToolTipTimer(ref bool toolTipToReset, float timer = 0.5f)
        {
            toolTipToReset = false;
            TooltipTimer = 0.5f;
        }
        private void HandleIntputFleetButtons(InputState input)
        {
            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                previousSelection = SelectedShip;
            SelectedShip = (Ship)null;
            SelectedShipList.Clear();
            SelectedFleet = (Fleet)null;
            lock (GlobalStats.FleetButtonLocker)
            {
                for (int i = 0; i < FleetButtons.Count; ++i)
                {
                    UniverseScreen.FleetButton fleetButton = FleetButtons[i];
                    if (HelperFunctions.CheckIntersection(fleetButton.ClickRect, input.CursorPosition))
                    {
                        SelectedFleet = fleetButton.Fleet;
                        SelectedShipList.Clear();
                        foreach (Ship ship in SelectedFleet.Ships)
                        {
                            if (ship.inSensorRange)
                                SelectedShipList.Add(ship);
                        }
                        if (SelectedShipList.Count == 1)
                        {
                            if (SelectedShip != null && previousSelection != SelectedShip && SelectedShip != SelectedShipList[0]) //fbedard
                                previousSelection = SelectedShip;
                            SelectedShip = SelectedShipList[0];
                            ShipInfoUIElement.SetShip(SelectedShip);
                            SelectedShipList.Clear();
                        }
                        else if (SelectedShipList.Count > 1)
                            shipListInfoUI.SetShipList((Array<Ship>)SelectedShipList, true);
                        SelectedSomethingTimer = 3f;

                        if ((double)ClickTimer < (double)TimerDelay)
                        {
                            ViewingShip = false;
                            AdjustCamTimer = 0.5f;
                            transitionDestination.X = SelectedFleet.FindAveragePosition().X;
                            transitionDestination.Y = SelectedFleet.FindAveragePosition().Y;
                            if (viewState < UniverseScreen.UnivScreenState.SystemView)
                                transitionDestination.Z = GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                        }
                        else
                            ClickTimer = 0.0f;
                    }
                }
            }
        }
        public void GameSpeedIncrease(bool increase)
        {
            if (!increase) return;

            if (GameSpeed < 1.0)
                GameSpeed = 1f;
            else
                ++GameSpeed;
            if (GameSpeed > 4.0 && GlobalStats.LimitSpeed)
                GameSpeed = 4f;
        }
        private void GameSpeedDecrease(bool decrease)
        {
            if (decrease)
            {
                if (GameSpeed <= 1.0)
                    GameSpeed = 0.5f;
                else
                    --GameSpeed;
            }
        }
        private void HandleInputScrap(InputState input)
        {
            player.GetGSAI().Goals.QueuePendingRemoval(SelectedItem.AssociatedGoal);
            bool flag = false;
            foreach (Ship ship in player.GetShips())
            {
                if (ship.isConstructor && ship.AI.OrderQueue.NotEmpty)
                {
                    for (int index = 0; index < ship.AI.OrderQueue.Count; ++index)
                    {
                        if (ship.AI.OrderQueue[index].goal == SelectedItem.AssociatedGoal)
                        {
                            flag = true;
                            ship.AI.OrderScrapShip();
                            break;
                        }
                    }
                }
            }
            if (!flag)
            {
                foreach (Planet planet in player.GetPlanets())
                {
                    foreach (QueueItem queueItem in planet.ConstructionQueue)
                    {
                        if (queueItem.Goal == SelectedItem.AssociatedGoal)
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
                for (int local_10 = 0; local_10 < ItemsToBuild.Count; ++local_10)
                {
                    ClickableItemUnderConstruction local_11 = ItemsToBuild[local_10];
                    if (local_11.BuildPos == SelectedItem.BuildPos)
                    {
                        ItemsToBuild.QueuePendingRemoval(local_11);
                        AudioManager.PlayCue("blip_click");
                    }
                }
                ItemsToBuild.ApplyPendingRemovals();
            }
            player.GetGSAI().Goals.ApplyPendingRemovals();
            SelectedItem = null;
        }
        public override void HandleInput(InputState input)
        {
            this.Input = input;

            if (input.PauseGame && !GlobalStats.TakingInput) Paused = !Paused;
            if (ScreenManager.UpdateExitTimeer(!LookingAtPlanet)) return; //if planet screen is still exiting prevent further input

            for (int index = SelectedShipList.Count - 1; index >= 0; --index)
            {
                Ship ship = SelectedShipList[index];
                if (!ship.Active)
                    SelectedShipList.RemoveSwapLast(ship);
            }
            //CG: previous target code. 
            if (previousSelection != null && input.PreviousTarget) PreviousTargetSelection(input);

            //fbedard: Set camera chase on ship
            if (input.ChaseCam) ChaseCame();

            ShowTacticalCloseup = input.TacticalIcons;

            if (input.UseRealLights)
            {
                UseRealLights = !UseRealLights; // toggle real lights
                SetLighting(UseRealLights);
            }
            if (input.ShowExceptionTracker && !ExceptionTracker.Visible) ReportManual("Manual Report", false);

            if (input.SendKudos && !ExceptionTracker.Visible)            ReportManual("Kudos", true);
            
            if (input.DebugMode)
            {
                Debug = !Debug;
                UniverseScreen.debug = !Debug;
                foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                    solarSystem.ExploredDict[player] = true;
                GlobalStats.LimitSpeed = GlobalStats.LimitSpeed || Debug;
            }

            HandleEdgeDetection(input);
            GameSpeedIncrease(input.SpeedUp);
            GameSpeedDecrease(input.SpeedDown);
            

            //fbedard: Click button to Cycle through ships in Combat
            if (!ShipsInCombat.Rect.HitTest(input.CursorPosition))
            {
                ShipsInCombat.State = UIButton.PressState.Default;
            }
            else CycleShipsInCombat(input);


            //fbedard: Click button to Cycle through Planets in Combat
            if (!HelperFunctions.CheckIntersection(PlanetsInCombat.Rect, input.CursorPosition))
            {
                PlanetsInCombat.State = UIButton.PressState.Default;
            }
            else CyclePlanetsInCombat(input);

            if (!LookingAtPlanet )
            {
                if (HandleGUIClicks(input))
                {
                    SkipRightOnce = true;
                    NeedARelease = true;
                    return;
                }
            }
            else
            {
                SelectedFleet = (Fleet)null;
                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                    previousSelection = SelectedShip;
                SelectedShip = (Ship)null;
                SelectedShipList.Clear();
                SelectedItem = null;
                SelectedSystem = null;
            }
            if (input.ScrapShip && (SelectedItem != null && SelectedItem.AssociatedGoal.empire == player))
                HandleInputScrap(input);
            
            if(debug)
            {
                if (input.ShowDebugWindow)
                {
                    if (!showdebugwindow)
                        DebugWin = new DebugInfoScreen(ScreenManager, this);
                    else
                        DebugWin = null;
                    showdebugwindow = !showdebugwindow;
                }
                if (Debug && showdebugwindow)
                {
                    DebugWin.HandleInput(input);
                }
                if (input.GetMemory)
                {
                    Memory = (float)GC.GetTotalMemory(true);
                    Memory /= 1000f;
                }
            }

            if (DefiningAO)
            {
                if (NeedARelease)
                {
                    if (input.LeftMouseRelease)
                        NeedARelease = false;
                }
                else
                {
                    DefineAO(input);
                    return;
                }
            }
            pickedSomethingThisFrame = false;        
            if (LookingAtPlanet)
                workersPanel.HandleInput(input);
            if (IsActive)
                EmpireUI.HandleInput(input);
            if (ShowingPlanetToolTip && input.MouseScreenPos.OutsideRadius(tippedPlanet.ScreenPos, tippedPlanet.Radius))
                ResetToolTipTimer(ref ShowingPlanetToolTip);
            
            if (ShowingSysTooltip && input.MouseScreenPos.OutsideRadius(tippedPlanet.ScreenPos, tippedSystem.Radius))
                ResetToolTipTimer(ref ShowingSysTooltip);

            if (!LookingAtPlanet)
                HandleInputNotLookingAtPlanet(input);
            else
                HandleInputLookingAtPlanet(input);

            if (input.InGameSelect && !pickedSomethingThisFrame && (!input.RepeatingKeyCheck(Keys.LeftShift) && !pieMenu.Visible))
                HandleIntputFleetButtons(input);            

            cState = SelectedShip != null || SelectedShipList.Count > 0 ? UniverseScreen.CursorState.Move : UniverseScreen.CursorState.Normal;
            if (SelectedShip == null && SelectedShipList.Count <= 0)
                return;
            foreach (UniverseScreen.ClickableShip clickableShip in ClickableShipsList)
            {
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    cState = UniverseScreen.CursorState.Follow;
            }
            if (cState == UniverseScreen.CursorState.Follow)
                return;
            lock (GlobalStats.ClickableSystemsLock)
            {
                foreach (UniverseScreen.ClickablePlanets planets in ClickPlanetList)
                {
                    if (input.CursorPosition.InRadius(planets.ScreenPos, planets.Radius) && planets.planetToClick.habitable)
                        cState = UniverseScreen.CursorState.Orbit;
                }
            }
        }

        private void HandleFleetSelections(InputState input)
        {
            bool flag = false;
            int index = 10;
            if (input.RepeatingKeyCheck(Keys.LeftControl))
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
            if (flag && !input.RepeatingKeyCheck(Keys.LeftShift))
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
                        SelectedFleet = null;
                    SelectedShipList.Clear();
                    foreach (Ship ship in player.GetFleetsDict()[index].Ships)
                    {
                        SelectedShipList.Add(ship);
                        SelectedSomethingTimer = 3f;
                    }
                    if (SelectedShipList.Count == 1)  //fbedard:display new fleet in UI
                    {
                        if (SelectedShip != null && previousSelection != SelectedShip && SelectedShip != SelectedShipList[0]) //fbedard
                            previousSelection = SelectedShip;
                        SelectedShip = SelectedShipList[0];
                        ShipInfoUIElement.SetShip(SelectedShip);
                    }
                    else if (SelectedShipList.Count > 1)
                        shipListInfoUI.SetShipList(SelectedShipList, true);  

                    if (SelectedFleet != null && ClickTimer < TimerDelay)
                    {
                        ViewingShip = false;
                        AdjustCamTimer = 0.5f;
                        transitionDestination.X = SelectedFleet.FindAveragePosition().X;
                        transitionDestination.Y = SelectedFleet.FindAveragePosition().Y;
                        if (camHeight < GetZfromScreenState(UnivScreenState.SystemView))
                            transitionDestination.Z = GetZfromScreenState(UnivScreenState.SystemView);
                    }
                    else if (SelectedFleet != null)
                        ClickTimer = 0.0f;
                }
            }
        }

        public void RecomputeFleetButtons(bool now)
        {
            ++FBTimer;
            if (FBTimer <= 60 && !now)
                return;
            lock (GlobalStats.FleetButtonLocker)
            {
                int local_0 = 0;
                int local_1 = 60;
                int local_2 = 20;
                FleetButtons.Clear();
                foreach (KeyValuePair<int, Fleet> item_0 in player.GetFleetsDict())
                {
                    if (item_0.Value.Ships.Count > 0)
                    {
                        FleetButtons.Add(new FleetButton()
                        {
                            ClickRect = new Rectangle(local_2, local_1 + local_0 * local_1, 52, 48),
                            Fleet = item_0.Value,
                            Key = item_0.Key
                        });
                        ++local_0;
                    }
                }
                FBTimer = 0;
            }
        }

        private Ship CheckShipClick(Vector2 ClickPos, InputState input)
        {
            foreach (ClickableShip clickableShip in ClickableShipsList)
            {
                if (input.CursorPosition.InRadius(clickableShip.ScreenPos, clickableShip.Radius))
                    return clickableShip.shipToClick;
            }
            return null;
        }

        private Planet CheckPlanetClick(Vector2 ClickPos)
        {
            foreach (ClickablePlanets clickablePlanets in ClickPlanetList)
            {
                if (Input.CursorPosition.InRadius(clickablePlanets.ScreenPos, clickablePlanets.Radius + 10.0f))
                    return clickablePlanets.planetToClick;
            }
            return null;
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
                    SelectedSomethingTimer = 3f;
                    startDrag = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
                    startDragWorld = UnprojectToWorldPosition(startDrag);
                    ProjectedPosition = UnprojectToWorldPosition(startDrag);
                    Vector3 position = ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    viewport = ScreenManager.GraphicsDevice.Viewport;
                    Vector3 direction = viewport.Unproject(new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 1f), projection, view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
                }
                if (SelectedShip != null && SelectedShip.AI.State == AIState.ManualControl && Vector2.Distance(startDragWorld, SelectedShip.Center) < 5000.0)
                    return;
                if (input.CurrentMouseState.RightButton == ButtonState.Released && input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    viewport = ScreenManager.GraphicsDevice.Viewport;
                    Vector3 position = viewport.Unproject(new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0.0f), projection, view, Matrix.Identity);
                    viewport = ScreenManager.GraphicsDevice.Viewport;
                    Vector3 direction = viewport.Unproject(new Vector3(input.CurrentMouseState.X, input.CurrentMouseState.Y, 1f), projection, view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num1 = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num1 * ray.Direction.X, ray.Position.Y + num1 * ray.Direction.Y, 0.0f);
                    Vector2 vector2_1 = new Vector2(vector3.X, vector3.Y);
                    Vector2 target = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
                    float num2 = startDrag.RadiansToTarget(target);
                    Vector2 vector2_2 = Vector2.Normalize(target - startDrag);
                    if (input.RightMouseTimer > 0.0f)
                    {
                        if (SelectedFleet != null && SelectedFleet.Owner.isPlayer)
                        {
                            AudioManager.PlayCue("echo_affirm1");
                            SelectedSomethingTimer = 3f;
                            float num3 = SelectedFleet.Position.RadiansToTarget(vector2_1);
                            Vector2 vectorToTarget = Vector2.Zero.FindVectorToTarget(SelectedFleet.Position.PointFromRadians(num3, 1f));
                            foreach (Ship ship in SelectedFleet.Ships)
                                player.GetGSAI().DefensiveCoordinator.Remove(ship);
                            Ship ship1 = CheckShipClick(startDrag, input);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = CheckPlanetClick(startDrag);
                            if (ship1 != null && ship1.loyalty != player)
                            {
                                SelectedFleet.Position = ship1.Center;
                                SelectedFleet.AssignPositions(0.0f);
                                foreach (Ship ship2 in SelectedFleet.Ships)
                                {
                                    if (ship2.shipData.Role == ShipData.RoleName.troop)
                                        ship2.AI.OrderTroopToBoardShip(ship1);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        ship2.AI.OrderQueueSpecificTarget(ship1);
                                    else
                                        ship2.AI.OrderAttackSpecificTarget(ship1);
                                }
                            }
                            else if (planet != null)
                            {
                                SelectedFleet.Position = planet.Position;  //fbedard: center fleet on planet
                                foreach (Ship ship2 in SelectedFleet.Ships)
                                {
                                    RightClickship(ship2, planet, false);
                                }
                            }
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget, true);
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                SelectedFleet.MoveToDirectly(vector2_1, num3, vectorToTarget);
                            else
                                SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget);
                        }
                        else if (SelectedShip != null && SelectedShip.loyalty.isPlayer)
                        {
                            player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Remove(SelectedShip);
                            SelectedSomethingTimer = 3f;
                            Ship ship = CheckShipClick(startDrag, input);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = CheckPlanetClick(startDrag);
                            if (ship != null && ship != SelectedShip)
                            {
                                if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                                else
                                {
                                    AudioManager.PlayCue("echo_affirm1");
                                    if (ship.loyalty == player)
                                    {
                                        if (SelectedShip.shipData.Role == ShipData.RoleName.troop)
                                        {
                                            if (ship.TroopList.Count < ship.TroopCapacity)
                                                SelectedShip.AI.OrderTroopToShip(ship);
                                            else
                                                SelectedShip.DoEscort(ship);
                                        }
                                        else
                                            SelectedShip.DoEscort(ship);
                                    }
                                    else if (SelectedShip.shipData.Role == ShipData.RoleName.troop)
                                        SelectedShip.AI.OrderTroopToBoardShip(ship);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        SelectedShip.AI.OrderQueueSpecificTarget(ship);
                                    else
                                        SelectedShip.AI.OrderAttackSpecificTarget(ship);
                                }
                            } 
                            // else if (ship != null && ship == this.SelectedShip)
                            else if (ship != null && ship == SelectedShip && SelectedShip.Mothership == null && !SelectedShip.isConstructor)  //fbedard: prevent hangar ship and constructor
                            {
                                if (ship.loyalty == player)
                                    LoadShipMenuNodes(1);
                                else
                                    LoadShipMenuNodes(0);
                                if (!pieMenu.Visible)
                                {
                                    pieMenu.RootNode = shipMenu;
                                    pieMenu.Show(pieMenu.Position);
                                }
                                else
                                    pieMenu.ChangeTo(null);
                            }
                            else if (planet != null)
                            {

                                RightClickship(SelectedShip, planet,true);
                               
                            }
                            else if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
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
                                        SelectedShip.AI.OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2, false);
                                    else
                                        SelectedShip.AI.OrderMoveTowardsPosition(vector2_1, num2, vector2_2, false,null);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    SelectedShip.AI.OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2, true);
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
                                {
                                    SelectedShip.AI.OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true,null);
                                    SelectedShip.AI.OrderQueue.Enqueue(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.HoldPosition, vector2_1, num2));
                                    SelectedShip.AI.HasPriorityOrder = true;
                                    SelectedShip.AI.IgnoreCombat = true;
                                }
                                else
                                    SelectedShip.AI.OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true,null);
                            }
                        }
                        else if (SelectedShipList.Count > 0)
                        {
                            SelectedSomethingTimer = 3f;
                            foreach (Ship ship in SelectedShipList)
                            {
                                if (ship.loyalty != player || ship.isConstructor || ship.shipData.Role == ShipData.RoleName.supply)
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                            }
                            AudioManager.PlayCue("echo_affirm1");
                            Ship ship1 = CheckShipClick(startDrag, input);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = CheckPlanetClick(startDrag);
                            if (ship1 != null || planet != null)
                            #region Target Planet
                            {
                                foreach (Ship ship2 in SelectedShipList)
                                {
                                    player.GetGSAI().DefensiveCoordinator.Remove(ship2);
                                    if (ship1 != null && ship1 != ship2)
                                    {
                                        if (ship1.loyalty == player)
                                        {
                                            if (ship2.shipData.Role == ShipData.RoleName.troop)
                                            {
                                                if (ship1.TroopList.Count < ship1.TroopCapacity)
                                                    ship2.AI.OrderTroopToShip(ship1);
                                                else
                                                    ship2.DoEscort(ship1);
                                            }
                                            else
                                                ship2.DoEscort(ship1);
                                        }
                                        else if (ship2.shipData.Role == ShipData.RoleName.troop)
                                            ship2.AI.OrderTroopToBoardShip(ship1);
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            ship2.AI.OrderQueueSpecificTarget(ship1);
                                        else
                                            ship2.AI.OrderAttackSpecificTarget(ship1);
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
                                SelectedSomethingTimer = 3f;
                                foreach (Ship ship2 in SelectedShipList)
                                {
                                    if (ship2.isConstructor || ship2.shipData.Role == ShipData.RoleName.supply)
                                    {
                                        SelectedShipList.Clear();
                                        AudioManager.PlayCue("UI_Misc20");
                                        return;
                                    }
                                }
                                AudioManager.PlayCue("echo_affirm1");
                                endDragWorld = UnprojectToWorldPosition(input.CursorPosition);
                                SelectedShipList.OrderBy(ship => ship.Center.X);
                                Vector2 fVec = new Vector2(-vector2_2.Y, vector2_2.X);
                                float num3 = Vector2.Distance(endDragWorld, startDragWorld);
                                int num4 = 0;
                                int num5 = 0;
                                float num6 = 0.0f;
                                for (int i = 0; i < SelectedShipList.Count; ++i)
                                {
                                    player.GetGSAI().DefensiveCoordinator.Remove(SelectedShipList[i]);
                                    if (SelectedShipList[i].GetSO().WorldBoundingSphere.Radius > num6)
                                        num6 = SelectedShipList[i].GetSO().WorldBoundingSphere.Radius;
                                }
                                Fleet fleet = new Fleet();
                                if (SelectedShipList.Count * num6 > num3)
                                {
                                    for (int i = 0; i < SelectedShipList.Count; ++i)
                                    {
                                        fleet.AddShip(SelectedShipList[i]);
                                        fleet.Ships[i].RelativeFleetOffset = new Vector2((num6 + 200f) * num5, num4 * (num6 + 200f));
                                        ++num5;
                                        if (fleet.Ships[i].RelativeFleetOffset.X + num6 > num3)
                                        {
                                            num5 = 0;
                                            ++num4;
                                        }
                                    }
                                }
                                else
                                {
                                    float num7 = num3 / SelectedShipList.Count;
                                    for (int i = 0; i < SelectedShipList.Count; ++i)
                                    {
                                        fleet.AddShip(SelectedShipList[i]);
                                        fleet.Ships[i].RelativeFleetOffset = new Vector2(num7 * i, 0.0f);
                                    }
                                }
                                fleet.ProjectPos(endDragWorld, num2 - 1.570796f, fVec);
                                foreach (Ship ship2 in fleet.Ships)
                                {
                                    foreach (Ship ship3 in SelectedShipList)
                                    {
                                        if (ship2.guid == ship3.guid)
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            {
                                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                    ship3.AI.OrderMoveDirectlyTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, false);
                                                else
                                                    ship3.AI.OrderMoveTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, false,null);
                                            }
                                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship3.AI.OrderMoveDirectlyTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, true);
                                            else
                                                ship3.AI.OrderMoveTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, true,null);
                                        }
                                    }
                                }
                                projectedGroup = fleet;
                                fleet.Reset();
                            }
                        }
                        if (SelectedFlank == null && SelectedFleet  == null && SelectedItem == null &&
                            SelectedShip  == null && SelectedPlanet == null && SelectedShipList.Count == 0)
                        {
                            Ship ship = CheckShipClick(input.CursorPosition, input);
                            if (ship != null && ship.Mothership == null && !ship.isConstructor)  //fbedard: prevent hangar ship and constructor
                            {
                                if (SelectedShip != null && previousSelection != SelectedShip && SelectedShip != ship) //fbedard
                                    previousSelection = SelectedShip;
                                SelectedShip = ship;
                                if (ship.loyalty == player)
                                    LoadShipMenuNodes(1);
                                else
                                    LoadShipMenuNodes(0);
                                if (!pieMenu.Visible)
                                {
                                    pieMenu.RootNode = shipMenu;
                                    pieMenu.Show(pieMenu.Position);
                                }
                                else
                                    pieMenu.ChangeTo(null);
                            }
                        }
                    }
                    else
                    {
                        ProjectingPosition = true;
                        if (SelectedFleet != null && SelectedFleet.Owner == player)
                        {
                            SelectedSomethingTimer = 3f;
                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                SelectedFleet.FormationWarpTo(ProjectedPosition, num2, vector2_2, true);
                            else
                                SelectedFleet.FormationWarpTo(ProjectedPosition, num2, vector2_2);
                            AudioManager.PlayCue("echo_affirm1");
                            foreach (Ship ship in SelectedFleet.Ships)
                                player.GetGSAI().DefensiveCoordinator.Remove(ship);
                        }
                        else if (SelectedShip != null && SelectedShip.loyalty == player)
                        {
                            player.GetGSAI().DefensiveCoordinator.Remove(SelectedShip);
                            SelectedSomethingTimer = 3f;
                            if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                            {
                                if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                                    previousSelection = SelectedShip;
                                SelectedShip = null;
                                AudioManager.PlayCue("UI_Misc20");
                                return;
                            }
                            else
                            {
                                AudioManager.PlayCue("echo_affirm1");
                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                {
                                    if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                        SelectedShip.AI.OrderMoveDirectlyTowardsPosition(ProjectedPosition, num2, vector2_2, false);
                                    else
                                        SelectedShip.AI.OrderMoveTowardsPosition(ProjectedPosition, num2, vector2_2, false, null);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    SelectedShip.AI.OrderMoveDirectlyTowardsPosition(ProjectedPosition, num2, vector2_2, true);
                                else
                                    SelectedShip.AI.OrderMoveTowardsPosition(ProjectedPosition, num2, vector2_2, true, null);
                            }
                        }
                        else if (SelectedShipList.Count > 0)
                        {
                            SelectedSomethingTimer = 3f;
                            foreach (Ship ship in SelectedShipList)
                            {
                                if (ship.loyalty != player)
                                    return;
                                if (ship.isConstructor || ship.shipData.Role == ShipData.RoleName.supply)
                                {
                                    SelectedShipList.Clear();
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                            }
                            AudioManager.PlayCue("echo_affirm1");
                            endDragWorld = UnprojectToWorldPosition(input.CursorPosition);
                            Vector2 fVec = new Vector2(-vector2_2.Y, vector2_2.X);
                            float num3 = Vector2.Distance(endDragWorld, startDragWorld);
                            int num4 = 0;
                            int num5 = 0;
                            float num6 = 0.0f;
                            for (int i = 0; i < SelectedShipList.Count; ++i)
                            {
                                player.GetGSAI().DefensiveCoordinator.Remove(SelectedShipList[i]);
                                if (SelectedShipList[i].GetSO().WorldBoundingSphere.Radius > num6)
                                    num6 = SelectedShipList[i].GetSO().WorldBoundingSphere.Radius;
                            }
                            Fleet fleet = new Fleet();
                            if (SelectedShipList.Count * num6 > num3)
                            {
                                for (int i = 0; i < SelectedShipList.Count; ++i)
                                {
                                    fleet.AddShip(SelectedShipList[i]);
                                    fleet.Ships[i].RelativeFleetOffset = new Vector2((num6 + 200f) * num5, num4 * (num6 + 200f));
                                    ++num5;
                                    if (SelectedShipList[i].RelativeFleetOffset.X + num6 > num3)
                                    {
                                        num5 = 0;
                                        ++num4;
                                    }
                                }
                            }
                            else
                            {
                                float num7 = num3 / SelectedShipList.Count;
                                for (int i = 0; i < SelectedShipList.Count; ++i)
                                {
                                    fleet.AddShip(SelectedShipList[i]);
                                    fleet.Ships[i].RelativeFleetOffset = new Vector2(num7 * i, 0.0f);
                                }
                            }
                            fleet.ProjectPos(ProjectedPosition, num2 - 1.570796f, fVec);
                            foreach (Ship ship1 in fleet.Ships)
                            {
                                foreach (Ship ship2 in SelectedShipList)
                                {
                                    if (ship1.guid == ship2.guid)
                                    {
                                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship2.AI.OrderMoveDirectlyTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, false);
                                            else
                                                ship2.AI.OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, false, null);
                                        }
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                            ship2.AI.OrderMoveDirectlyTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, true);
                                        else
                                            ship2.AI.OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, true, null);
                                    }
                                }
                            }
                            projectedGroup = fleet;
                        }
                    }
                }
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    var target = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
                    float facing = startDrag.RadiansToTarget(target);
                    Vector2 fVec1 = Vector2.Normalize(target - startDrag);
                    if (input.RightMouseTimer > 0.0)
                        return;
                    ProjectingPosition = true;
                    if (SelectedFleet != null && SelectedFleet.Owner == player)
                    {
                        ProjectingPosition = true;
                        SelectedFleet.ProjectPos(ProjectedPosition, facing, fVec1);
                        projectedGroup = SelectedFleet;
                    }
                    else if (SelectedShip != null && SelectedShip.loyalty == player)
                    {
                        if (SelectedShip.isConstructor || SelectedShip.shipData.Role == ShipData.RoleName.supply)
                        {
                            if (SelectedShip != null && previousSelection != SelectedShip) //fbedard
                                previousSelection = SelectedShip;
                            SelectedShip = null;
                            AudioManager.PlayCue("UI_Misc20");
                        }
                        else
                        {
                            ShipGroup shipGroup = new ShipGroup();
                            shipGroup.Ships.Add(SelectedShip);
                            shipGroup.ProjectPos(ProjectedPosition, facing, fVec1);
                            projectedGroup = shipGroup;
                        }
                    }
                    else
                    {
                        if (SelectedShipList.Count <= 0)
                            return;
                        foreach (Ship ship in SelectedShipList)
                        {
                            if (ship.loyalty != player)
                                return;
                        }
                        endDragWorld = UnprojectToWorldPosition(input.CursorPosition);
                        Vector2 fVec2 = new Vector2(-fVec1.Y, fVec1.X);
                        float num1 = Vector2.Distance(endDragWorld, startDragWorld);
                        int num2 = 0;
                        int num3 = 0;
                        float num4 = 0.0f;
                        for (int i = 0; i < SelectedShipList.Count; ++i)
                        {
                            if (SelectedShipList[i].GetSO().WorldBoundingSphere.Radius > num4)
                                num4 = SelectedShipList[i].GetSO().WorldBoundingSphere.Radius;
                        }
                        Fleet fleet = new Fleet();
                        if (SelectedShipList.Count * num4 > num1)
                        {
                            for (int i = 0; i < SelectedShipList.Count; ++i)
                            {
                                fleet.AddShip(SelectedShipList[i]);
                                fleet.Ships[i].RelativeFleetOffset = new Vector2((num4 + 200f) * num3, num2 * (num4 + 200f));
                                ++num3;
                                if (SelectedShipList[i].RelativeFleetOffset.X + num4 > num1)
                                {
                                    num3 = 0;
                                    ++num2;
                                }
                            }
                        }
                        else
                        {
                            float num5 = num1 / SelectedShipList.Count;
                            for (int i = 0; i < SelectedShipList.Count; ++i)
                            {
                                fleet.AddShip(SelectedShipList[i]);
                                fleet.Ships[i].RelativeFleetOffset = new Vector2(num5 * i, 0.0f);
                            }
                        }
                        fleet.ProjectPos(ProjectedPosition, facing - 1.570796f, fVec2);
                        projectedGroup = fleet;
                    }
                }
                else
                    ProjectingPosition = false;
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
                        ship.AI.OrderColonization(planet);
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (ship.shipData.Role == ShipData.RoleName.troop || (ship.TroopList.Count > 0 && (ship.HasTroopBay || ship.hasTransporter)))
                {
                    if (planet.Owner != null && planet.Owner == this.player && (!ship.HasTroopBay && !ship.hasTransporter))
                    {
                        if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.AI.OrderToOrbit(planet, false);
                        else
                            ship.AI.OrderRebase(planet, true);
                    }
                    else if (planet.habitable && (planet.Owner == null || planet.Owner != player && (ship.loyalty.GetRelations(planet.Owner).AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated)))
                    {
                        //add new right click troop and troop ship options on planets
                        if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.AI.OrderToOrbit(planet, false);
                        else
                        {
                            ship.AI.State = AIState.AssaultPlanet;
                            ship.AI.OrderLandAllTroops(planet);
                        }
                    }
                    else
                    {
                        ship.AI.OrderOrbitPlanet(planet);// OrderRebase(planet, true);
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
                            if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                ship.AI.OrderBombardPlanet(planet);
                            else if (enemies > friendlies || planet.Population > 0f)
                                ship.AI.OrderBombardPlanet(planet);
                            else
                            {
                                ship.AI.OrderToOrbit(planet, false);
                            }
                        }
                        else
                        {
                            ship.AI.OrderToOrbit(planet, false);
                        }


                    }
                    else if (enemies > friendlies && Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        ship.AI.OrderBombardPlanet(planet);
                    }
                    else
                        ship.AI.OrderToOrbit(planet, true);
                }
                else if (Input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    ship.AI.OrderToOrbit(planet, false);
                else
                    ship.AI.OrderToOrbit(planet, true);
            }
                            



        }


        private void HandleEdgeDetection(InputState input)
        {
            if (this.LookingAtPlanet || ViewingShip )
                return;
            PresentationParameters presentationParameters = this.ScreenManager.GraphicsDevice.PresentationParameters;
            Vector2 spaceFromScreenSpace1 = this.UnprojectToWorldPosition(new Vector2(0.0f, 0.0f));
            float num = this.UnprojectToWorldPosition(new Vector2((float)presentationParameters.BackBufferWidth, (float)presentationParameters.BackBufferHeight)).X - spaceFromScreenSpace1.X;
            input.Repeat = true;
            if (input.CursorPosition.X <= 1f || input.Left )
            {
                this.transitionDestination.X -= 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if (input.CursorPosition.X >= (presentationParameters.BackBufferWidth - 1) || input.Right )
            {
                this.transitionDestination.X += 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if (input.CursorPosition.Y <= 0.0f || input.Up )
            {
                this.snappingToShip = false;
                this.ViewingShip = false;
                this.transitionDestination.Y -= 0.008f * num;
            }
            if (input.CursorPosition.Y >= (presentationParameters.BackBufferHeight - 1) || input.Down )
            {
                this.transitionDestination.Y += 0.008f * num;
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            input.Repeat = false;
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

            float scrollAmount = 1500.0f * camHeight / 3000.0f + 100.0f;

            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                this.transitionDestination.X = this.camPos.X;
                this.transitionDestination.Y = this.camPos.Y;
                this.transitionDestination.Z = this.camHeight + scrollAmount;
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

            this.transitionDestination.Z = this.camHeight - scrollAmount;
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
            if ((double)this.camHeight <= 450.0f)
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
                            this.transitionDestination = new Vector3(this.SelectedFleet.FindAveragePosition().X, this.SelectedFleet.FindAveragePosition().Y, num2);
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
                            SelectedShipList.AddRange(SelectedFleet.Ships);
                            break;
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
                            if (ship.shipData.Role <= ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.AI.State ==  AIState.Colonize)
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
                                    if (ship.shipData.Role <= ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.AI.State == AIState.Colonize)
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
                                    foreach (Ship ship in SelectedShipList)
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
                            this.shipListInfoUI.SetShipList(SelectedShipList, true);
                        else
                            this.shipListInfoUI.SetShipList(SelectedShipList, false);
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
            var processTurnsThread = ProcessTurnsThread;
            ProcessTurnsThread = null;
            DrawCompletedEvt.Set(); // notify processTurnsThread that we're terminating
            processTurnsThread.Join(250);
            EmpireUI.empire = null;
            EmpireUI = null;
            DeepSpaceManager.Destroy();
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
            DeepSpaceManager.Destroy();
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
            Fleet.Screen                          = null;
            Bomb.Screen                           = null;
            Anomaly.screen                        = null;
            MinimapButtons.screen                 = null;
            Empire.Universe                       = null;
            ResourceManager.UniverseScreen        = null;
            Empire.Universe                   = null;
            ArtificialIntelligence.universeScreen = null;
            MuzzleFlashManager.universeScreen     = null;
            FleetDesignScreen.screen              = null;
            ExplosionManager.Universe             = null;
            DroneAI.UniverseScreen                = null;
            StatTracker.SnapshotsDict.Clear();
            EmpireManager.Clear();            
            ScreenManager.inter.Unload();
            HelperFunctions.CollectMemory();
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
            var uiNode = ResourceManager.Texture("UI/node");

            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            foreach (ShipModule slot in ship.ModuleSlotList)
            {
                if (slot.Active && slot.ModuleType == ShipModuleType.Shield && slot.ShieldPower > 0)
                {
                    ProjectToScreenCoords(slot.Center, slot.shield_radius * 2.75f, out Vector2 posOnScreen, out float radiusOnScreen);

                    float shieldRate = slot.ShieldPower / (slot.shield_power_max + (ship.loyalty?.data.ShieldPowerMod*slot.shield_power_max ?? 0));

                    DrawTextureSized(uiNode, posOnScreen, 0f, radiusOnScreen, radiusOnScreen, new Color(0f, 1f, 0f, shieldRate*0.8f));
                }
            }
            ScreenManager.SpriteBatch.End();
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
            Render(gameTime);
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            ExplosionManager.DrawExplosions(ScreenManager, view, projection);
            ScreenManager.SpriteBatch.End();
            if (!ShowShipNames || LookingAtPlanet)
                return;
            foreach (ClickableShip clickableShip in ClickableShipsList)
                if (clickableShip.shipToClick.InFrustum)
                    DrawShieldBubble(clickableShip.shipToClick);
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
            if (!Debug) // don't draw fog of war in debug
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
                if (!ProcessTurnsCompletedEvt.WaitOne(100))
                    Log.Warning("Universe ProcessTurns Wait timed out: ProcessTurns was taking too long!");

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
                                DrawTransparentModel(ResourceManager.ProjectileModelDict[projectile.ModelPath], projectile.GetWorld(), 
                                    view, projection, projectile.Weapon.Animated != 0 
                                        ? ResourceManager.TextureDict[projectile.TexturePath] 
                                        : ResourceManager.ProjTextDict[projectile.TexturePath], projectile.Scale);
                        }
                    }
                }
                catch
                {
                }
            }       
            if (Debug) //input.CurrentKeyboardState.IsKeyDown(Keys.T) && !input.LastKeyboardState.IsKeyDown(Keys.T) && 
            {
                //foreach (Empire e in EmpireManager.Empires)
                {
                    //if (e.isPlayer || e.isFaction)
                    //    continue;
                    //foreach (ThreatMatrix.Pin pin in e.GetGSAI().ThreatMatrix.Pins.Values)
                    //{
                    //    if (pin.Position != Vector2.Zero) // && pin.InBorders)
                    //    {
                    //        Circle circle = this.DrawSelectionCircles(pin.Position, 50f);
                    //        DrawCircle(circle.Center, circle.Radius, 6, e.EmpireColor);
                    //        if(pin.InBorders)
                    //        {
                    //            circle = this.DrawSelectionCircles(pin.Position, 50f);
                    //            DrawCircle(circle.Center, circle.Radius, 3, e.EmpireColor);
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
                    //        DrawCircle(circle.Center, circle.Radius, 4, e.EmpireColor);
                    //    }
                }
            }

            this.DrawTacticalPlanetIcons();
            if (showingFTLOverlay && GlobalStats.PlanetaryGravityWells && !LookingAtPlanet)
            {
                var inhibit = ResourceManager.TextureDict["UI/node_inhibit"];
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets cplanet in ClickPlanetList)
                    {
                        float radius = GlobalStats.GravityWellRange * (1 + (((float)Math.Log(cplanet.planetToClick.scale)) / 1.5f));
                        DrawCircleProjected(cplanet.planetToClick.Position, radius, new Color(255, 50, 0, 150), 50, 1f, inhibit, new Color(200, 0, 0, 50));
                    }
                }
                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.InhibitionRadius > 0f)
                    {
                        float radius = ship.shipToClick.InhibitionRadius;
                        DrawCircleProjected(ship.shipToClick.Position, radius, new Color(255, 50, 0, 150), 50, 1f, inhibit, new Color(200, 0, 0, 40));
                    }
                }
                if (viewState >= UnivScreenState.SectorView)
                {
                    foreach (Empire.InfluenceNode influ in player.BorderNodes.AtomicCopy())
                    {
                        DrawCircleProjected(influ.Position, influ.Radius, new Color(30, 30, 150, 150), 50, 1f, inhibit, new Color(0, 200, 0, 20));
                    }
                }
            }

            if (showingRangeOverlay && !LookingAtPlanet)
            {
                var shipRangeTex = ResourceManager.Texture("UI/node_shiprange");
                foreach (ClickableShip ship in ClickableShipsList)
                {
                    if (ship.shipToClick != null && ship.shipToClick.RangeForOverlay > 0f)
                    {
                        Color color = (ship.shipToClick.loyalty == EmpireManager.Player) ? new Color(0, 200, 0, 30) : new Color(200, 0, 0, 30);
                        float radius = ship.shipToClick.RangeForOverlay;
                        //DrawCircleProjected(ship.shipToClick.Position, radius, new Color(255, 50, 0, 150), 50, 2f, nodeShipRange, color);
                        DrawTextureProjected(shipRangeTex, ship.shipToClick.Position, radius, color);
                    }
                }
            }
            
            if (showingDSBW && !LookingAtPlanet)
            {
                var nodeTex = ResourceManager.TextureDict["UI/node1"];
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (ClickablePlanets cplanet in ClickPlanetList)
                    {
                        float radius = 2500f * cplanet.planetToClick.scale;
                        DrawCircleProjected(cplanet.planetToClick.Position, radius, new Color(255, 165, 0, 150), 50, 1f, nodeTex, new Color(0, 0, 255, 50));
                    }
                }
                dsbw.Draw(gameTime);
            }
            DrawFleetIcons(gameTime);

            //fbedard: display values in new buttons
            ShipsInCombat.Text = "Ships: " + this.player.empireShipCombat;
            if (player.empireShipCombat > 0)
            {
                ShipsInCombat.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px");
                ShipsInCombat.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_hover");
                ShipsInCombat.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_pressed");
            }
            else
            {
                ShipsInCombat.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu");
                ShipsInCombat.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu_hover");
                ShipsInCombat.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu_pressed");
            }
            ShipsInCombat.Draw(ScreenManager.SpriteBatch);

            PlanetsInCombat.Text = "Planets: " + player.empirePlanetCombat;
            if (player.empirePlanetCombat > 0)
            {
                PlanetsInCombat.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px");
                PlanetsInCombat.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_hover");
                PlanetsInCombat.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_pressed");
            }
            else
            {
                PlanetsInCombat.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu");
                PlanetsInCombat.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu_hover");
                PlanetsInCombat.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu_pressed");
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
                DebugWin.Draw(gameTime);

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
                DrawLinesToScreen(pos, lines);
            }
            if (IsActive)
                ToolTip.Draw(ScreenManager);
            ScreenManager.SpriteBatch.End();

            // Notify ProcessTurns that Drawing has finished and while SwapBuffers is blocking,
            // the game logic can be updated
            DrawCompletedEvt.Set();
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

                    Vector2 averagePosition = kv.Value.FindAveragePositionset();
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

        private void DrawSelectedShipGroup(Array<Circle> CircleList, Color color, float Thickness)
        {
            for (int i = 0; i < CircleList.Count; ++i)
            {
                Circle c = CircleList[i];
                var rect = new Rectangle((int)c.Center.X - (int)c.Radius, (int)c.Center.Y - (int)c.Radius, (int)c.Radius * 2, (int)c.Radius * 2);
                if (i < CircleList.Count - 1)
                    Primitives2D.BracketRectangle(ScreenManager.SpriteBatch, rect, new Color(color.R, color.G, color.B, 100), 3);
                else
                    Primitives2D.BracketRectangle(ScreenManager.SpriteBatch, rect, color, 3);
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
                if (queueItem.isTroop)
                {
                    Troop template = ResourceManager.GetTroopTemplate(queueItem.troopType);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Troops/" + template.TexturePath], new Rectangle((int)vector2.X, (int)vector2.Y, 29, 30), Color.White);
                    Vector2 position5 = new Vector2(vector2.X + 40f, vector2.Y);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, queueItem.troopType, position5, Color.White);
                    position5.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    new ProgressBar(new Rectangle((int)position5.X, (int)position5.Y, 150, 18))
                    {
                        Max = queueItem.Cost,
                        Progress = queueItem.productionTowards
                    }.Draw(this.ScreenManager.SpriteBatch);
                }
            }
            else
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(1408), new Vector2((float)(this.stuffSelector.Menu.X + 40), (float)(this.stuffSelector.Menu.Y + 20)), new Color(byte.MaxValue, (byte)239, (byte)208));
        }

        private void DrawItemInfoForUI()
        {
            var goal = SelectedItem?.AssociatedGoal;
            if (goal != null)
                DrawCircleProjected(goal.BuildPosition, 50f, 50, goal.empire.EmpireColor);
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
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Blend.SourceAlpha;
            renderState.DestinationBlend       = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode               = CullMode.None;
            
            using (player.KnownShips.AcquireReadLock())
            {                
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active) continue;
                    DrawInRange(ship);
                }
            }
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Blend.SourceAlpha;
            renderState.DestinationBlend       = Blend.InverseSourceAlpha;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode               = CullMode.None;

            using (player.KnownShips.AcquireReadLock())
            {
                foreach (Ship ship in player.KnownShips)
                {
                    if (!ship.Active || !ScreenRectangle.HitTest(ship.ScreenPosition))
                        continue;

                    DrawTacticalIcons(ship);
                    DrawOverlay(ship);

                    if (SelectedShip == ship || SelectedShipList.Contains(ship))
                    {
                        Color color = Color.LightGreen;
                        color = player.IsEmpireAttackable(ship.loyalty) ? Color.Red : Color.Gray;     
                        Primitives2D.BracketRectangle(ScreenManager.SpriteBatch, ship.ScreenPosition, ship.ScreenRadius, color);
                    }
                }
            }
            if (ProjectingPosition)
                DrawProjectedGroup();

            var platform = ResourceManager.Texture("TacticalIcons/symbol_platform");

            lock (GlobalStats.ClickableItemLocker)
            {
                for (int i = 0; i < ItemsToBuild.Count; ++i)
                {
                    ClickableItemUnderConstruction item = ItemsToBuild[i];
                    
                    if (ResourceManager.GetShipTemplate(item.UID, out Ship buildTemplate))
                    {
                        //float scale2 = 0.07f;
                        float scale = ((float)buildTemplate.Size / platform.Width) * 4000f / camHeight;
                        DrawTextureProjected(platform, item.BuildPos, scale, 0.0f, new Color(0, 255, 0, 100));
                        if (showingDSBW)
                        {
                            if (item.UID == "Subspace Projector")
                            {
                                DrawCircleProjected(item.BuildPos, Empire.ProjectorRadius, 50, Color.Orange, 2f);
                            }
                            else if (buildTemplate.SensorRange > 0f)
                            {
                                DrawCircleProjected(item.BuildPos, buildTemplate.SensorRange, 50, Color.Blue, 2f);
                            }
                        }
                    }
                }
            }

            // show the object placement/build circle
            if (showingDSBW && dsbw.itemToBuild != null && dsbw.itemToBuild.Name == "Subspace Projector" && AdjustCamTimer <= 0f)
            {
                Vector2 center = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
                float screenRadius = ProjectToScreenSize(Empire.ProjectorRadius);
                DrawCircle(center, MathExt.SmoothStep(ref radlast, screenRadius, 0.01f), 50, Color.Orange, 2f);
            }
        }

        protected void DrawProjectedGroup()
        {
            if (projectedGroup == null)
                return;

            foreach (Ship ship in projectedGroup.Ships)
            {
                if (!ship.Active)
                    continue;

                var symbol = ResourceManager.Texture("TacticalIcons/symbol_" + (ship.isConstructor ? "construction" : ship.shipData.GetRole()));

                float num = ship.Size / (30f + symbol.Width);
                float scale = num * 4000f / camHeight;
                if (scale > 1.0f) scale = 1f;
                else if (scale <= 0.1f)
                    scale = ship.shipData.Role != ShipData.RoleName.platform || viewState < UnivScreenState.SectorView ? 0.15f : 0.08f;

                DrawTextureProjected(symbol, ship.projectedPosition, scale, projectedGroup.ProjectedFacing, new Color(0, 255, 0, 100));
            }
        }

        private Texture2D Arc15  = ResourceManager.Texture("Arcs/Arc15");
        private Texture2D Arc20  = ResourceManager.Texture("Arcs/Arc20");
        private Texture2D Arc45  = ResourceManager.Texture("Arcs/Arc45");
        private Texture2D Arc60  = ResourceManager.Texture("Arcs/Arc60");
        private Texture2D Arc90  = ResourceManager.Texture("Arcs/Arc90");
        private Texture2D Arc120 = ResourceManager.Texture("Arcs/Arc120");
        private Texture2D Arc180 = ResourceManager.Texture("Arcs/Arc180");
        private Texture2D Arc360 = ResourceManager.Texture("Arcs/Arc360");

        public Texture2D GetArcTexture(float weaponArc)
        {
            if (weaponArc >= 240f) return Arc360; // @note We're doing loose ARC matching to catch freak angles
            if (weaponArc >= 150f) return Arc180;
            if (weaponArc >= 105f) return Arc120;
            if (weaponArc >= 75f)  return Arc90;
            if (weaponArc >= 52.5f) return Arc60;
            if (weaponArc >= 32.5f) return Arc45;
            if (weaponArc >= 17.5f) return Arc20;
            return Arc15;
        }

        public Color GetWeaponArcColor(Weapon weapon)
        {
            if (weapon.WeaponType == "Flak" || weapon.WeaponType == "Vulcan")
                return Color.Yellow;
            else if (weapon.WeaponType == "Laser" || weapon.WeaponType == "HeavyLaser")
                return Color.Red;
            else if (weapon.WeaponType == "PhotonCannon")
                return Color.Blue;
            return new Color(255, 165, 0, 100);
            //color = new Color(255, 0, 0, 75); // full red is kinda too strong :|
        }

        public void DrawWeaponArc(ShipModule module, Vector2 posOnScreen, float rotation)
        {
            Color color     = GetWeaponArcColor(module.InstalledWeapon);
            Texture2D arc   = GetArcTexture(module.FieldOfFire);
            float arcLength = ProjectToScreenSize(module.InstalledWeapon.Range);
            DrawTextureSized(arc, posOnScreen, rotation, arcLength, arcLength, color);
        }

        public void DrawWeaponArc(Ship parent, ShipModule module)
        {
            float rotation = parent.Rotation + module.Facing.ToRadians();
            Vector2 posOnScreen = ProjectToScreenPosition(module.Center);
            DrawWeaponArc(module, posOnScreen, rotation);
        }


        private void DrawOverlay(Ship ship)
        {
            if (LookingAtPlanet || viewState > UnivScreenState.SystemView || (!ShowShipNames || ship.dying) || !ship.InFrustum)
                return;

            var symbolFighter = ResourceManager.Texture("TacticalIcons/symbol_fighter");
            var concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1"); // 1x1 gray ship module background tile, 16x16px in size
            var lightningBolt = ResourceManager.Texture("UI/lightningBolt");

            bool enableModuleDebug = Debug && true;
            if (enableModuleDebug)
            {
                foreach (Projectile projectile in ship.Projectiles)
                    DrawCircleProjected(projectile.Center, projectile.Radius, 50, Color.Red, 3f);
            }

            float shipDegrees  = (float)Math.Round(ship.Rotation.ToDegrees());
            float shipRotation = shipDegrees.ToRadians();

            for (int i = 0; i < ship.ModuleSlotList.Length; ++i)
            {
                ShipModule slot = ship.ModuleSlotList[i];

                float moduleWidth  = slot.XSIZE * 16.5f; // using 16.5f instead of 16 to reduce pixel error flickering
                float moduleHeight = slot.YSIZE * 16.5f;
                ProjectToScreenCoords(slot.Center, moduleWidth, moduleHeight,
                                      out Vector2 posOnScreen, out float widthOnScreen, out float heightOnScreen);

                // round all the values to TRY prevent module flickering on screen (well, it only helps a tiny bit)
                posOnScreen.X = (float)Math.Round(posOnScreen.X);
                posOnScreen.Y = (float)Math.Round(posOnScreen.Y);

                float slotFacing   = ((int)((slot.Facing + 45) / 90)) * 90f; // align the facing to 0, 90, 180, 270...
                float slotRotation = (shipDegrees + slotFacing).ToRadians();

                DrawTextureSized(concreteGlass, posOnScreen, shipRotation, widthOnScreen, heightOnScreen, Color.White);

                if (camHeight > 6000.0f) // long distance view, draw the modules as colored icons
                {
                    DrawTextureSized(symbolFighter, posOnScreen, shipRotation, widthOnScreen, heightOnScreen, slot.GetHealthStatusColor());
                }
                else
                {
                    var moduleTex = ResourceManager.Texture(slot.IconTexturePath);
                    float moduleSize = moduleTex.Width / (slot.XSIZE * 16f);
                    float scale = 0.75f * ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / camHeight / moduleSize;

                    DrawTextureSized(moduleTex, posOnScreen, slotRotation, widthOnScreen, heightOnScreen, slot.GetHealthStatusColorWhite());

                    if (enableModuleDebug)
                    {
                        DrawCircleProjected(slot.Center, slot.Radius, 20, Color.Red, 2f);
                    }
                    if (slot.ModuleType == ShipModuleType.PowerConduit)
                    {
                        if (slot.Powered)
                        {
                            var poweredTex = ResourceManager.Texture(slot.IconTexturePath + "_power");
                            DrawTextureSized(poweredTex, posOnScreen, slotRotation, widthOnScreen, heightOnScreen, Color.White);
                        }
                    }
                    else if (!slot.Powered && slot.PowerDraw > 0.0f)
                    {
                        DrawTexture(lightningBolt, posOnScreen, scale * 2f, slotRotation, Color.White);
                    }
                    if (Debug && slot.isExternal && slot.Active)
                    {
                        DrawTexture(symbolFighter, posOnScreen, scale * 0.6f, shipRotation, new Color(0, 0, 255, 120));
                    }
                    if (enableModuleDebug)
                    {
                        DrawString(posOnScreen, shipRotation, 350f / camHeight, Color.Red, $"{slot.LocalCenter}");
                    }
                }

                // finally, draw firing arcs for the player ship
                if (ship.isPlayerShip() && slot.FieldOfFire > 0.0f && slot.InstalledWeapon != null)
                    DrawWeaponArc(slot, posOnScreen, slotRotation);
            }

            if (enableModuleDebug)
                ship.DrawSparseModuleGrid(this);
        }

        private void DrawTacticalIcons(Ship ship)
        {
            if (LookingAtPlanet || (!showingFTLOverlay && ship.IsPlatform && viewState == UnivScreenState.GalaxyView))
                return;
            if (showingFTLOverlay && ship.IsPlatform && ship.Name != "Subspace Projector")
                return;

            if (ship.StrategicIconPath.IsEmpty())
            {
                ship.StrategicIconPath = "TacticalIcons/symbol_" + (ship.isConstructor ? "construction" : ship.shipData.GetRole());
            }
            if (viewState == UnivScreenState.GalaxyView)
            {
                float worldRadius = ship.GetSO().WorldBoundingSphere.Radius;
                ProjectToScreenCoords(ship.Position, worldRadius, out Vector2 screenPos, out float screenRadius);
                if (screenRadius < 8.0f)
                    screenRadius = 8f;
                float scale = screenRadius / (32 - GlobalStats.IconSize);

                bool flag = true;
                foreach (ClickableFleet clickableFleet in ClickableFleetsList)
                {
                    if (clickableFleet.fleet == ship.fleet && screenPos.Distance(clickableFleet.ScreenPos) < 20f)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                    return;
                Texture2D tex = ResourceManager.Texture(ship.StrategicIconPath);
                if (tex != null) DrawTexture(tex, screenPos, scale, ship.Rotation, ship.loyalty?.EmpireColor ?? Color.White);
            }
            else if ((this.ShowTacticalCloseup || this.viewState > UnivScreenState.ShipView) && !this.LookingAtPlanet)
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
                foreach (ClickableFleet clickableFleet in this.ClickableFleetsList)
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
                if (this.viewState > UnivScreenState.ShipView || this.LookingAtPlanet)
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
                float check = this.GetZfromScreenState(UnivScreenState.ShipView);
                if (ship.shipData.Role != ShipData.RoleName.fighter && ship.shipData.Role != ShipData.RoleName.scout)
                {
                    
                    scale2 *= (this.camHeight / (check + (check*2 -camHeight*2)));
                }
                else
                {
                   // scale2 *= this.camHeight * 2 > this.GetZfromScreenState(UnivScreenState.ShipView) ? 1 : this.camHeight * 2 / this.GetZfromScreenState(UniverseScreen.UnivScreenState.ShipView);
                     scale2 *= (this.camHeight * 3 / this.GetZfromScreenState(UnivScreenState.ShipView));
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
                
                if (ship.OrdinanceMax <= 0.0f)
                    return;
                if (ship.Ordinance <= 0.2f * ship.OrdinanceMax)
                {
                    position = new Vector2(vector2_1.X + 15f * scale, vector2_1.Y + 15f * scale);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_ammo"], position, new Rectangle?(), Color.Red, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
                else
                {
                    if (ship.Ordinance >= 0.5f * ship.OrdinanceMax)
                        return;
                    position = new Vector2(vector2_1.X + 15f * scale, vector2_1.Y + 15f * scale);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_ammo"], position, new Rectangle?(), Color.Yellow, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
            }
        }

        private void DrawBombs()
        {
            using (BombList.AcquireReadLock())
            {
                for (int i = 0; i < this.BombList.Count; i++)
                {
                    Bomb bomb = this.BombList[i];
                    this.DrawTransparentModel(bomb.Model, bomb.World, this.view, this.projection, bomb.Texture, 0.5f);
                }
            }
        }

        protected void DrawInRange(Ship ship)
        {
            if (viewState > UnivScreenState.SystemView)
                return;

            for (int i = 0; i < ship.Projectiles.Count; i++)
            {
                Projectile projectile = ship.Projectiles[i];
                if (projectile.WeaponType != "Missile" &&
                    projectile.WeaponType != "Rocket" &&
                    projectile.WeaponType != "Drone" &&
                    (ship.InFrustum || Frustum.Contains(projectile.Center, projectile.Radius)))
                {
                    DrawTransparentModel(ResourceManager.ProjectileModelDict[projectile.ModelPath],
                        projectile.GetWorld(), this.view, this.projection,
                        projectile.Weapon.Animated != 0
                            ? ResourceManager.Texture(projectile.TexturePath)
                            : ResourceManager.ProjTexture(projectile.TexturePath), projectile.Scale);
                }
            }
        }

        private Circle GetSelectionCircles(Vector2 WorldPos, float WorldRadius, float radiusMin = 0, float radiusIncrease = 0 )
        {
            ProjectToScreenCoords(WorldPos, WorldRadius, out Vector2 screenPos, out float screenRadius);
            if (radiusMin > 0)
                screenRadius = screenRadius < radiusMin ? radiusMin : screenRadius;            
            return new Circle(screenPos, screenRadius + radiusIncrease);

        }

        private Circle GetSelectionCirclesAroundShip(Ship ship)
            => GetSelectionCircles(ship.Center, ship.GetSO().WorldBoundingSphere.Radius, 5, 0);            
        

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
            bg.Draw(this, starfield);                           
            bg3d.Draw();
            
            ClickableShipsList.Clear();
            ScreenManager.SpriteBatch.Begin();
            Rectangle rect = new Rectangle(0, 0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);

            using (player.KnownShips.AcquireReadLock())
            {
                for (int i = 0; i < player.KnownShips.Count; i++)
                {
                    Ship ship = player.KnownShips[i];
                    if (ship != null && ship.Active &&
                        (viewState != UnivScreenState.GalaxyView || !ship.IsPlatform))
                    {
                        ProjectToScreenCoords(ship.Position, ship.GetSO().WorldBoundingSphere.Radius, out Vector2 screenPos, out float ScreenRadius);

                        if (HelperFunctions.CheckIntersection(rect, screenPos))
                        {
                            if (ScreenRadius < 7.0f) ScreenRadius = 7f;
                            ship.ScreenRadius = ScreenRadius;
                            ship.ScreenPosition = screenPos;
                            ClickableShipsList.Add(new ClickableShip
                            {
                                Radius = ScreenRadius,
                                ScreenPos = screenPos,
                                shipToClick = ship
                            });                            
                        }
                        else
                            ship.ScreenPosition = new Vector2(-1f, -1f);
                    }
                }
            }

            lock (GlobalStats.ClickableSystemsLock)
            {
                ClickPlanetList.Clear();
                ClickableSystems.Clear();
            }
            Texture2D Glow_Terran = ResourceManager.TextureDict["PlanetGlows/Glow_Terran"];
            Texture2D Glow_Red    = ResourceManager.TextureDict["PlanetGlows/Glow_Red"];
            Texture2D Glow_White  = ResourceManager.TextureDict["PlanetGlows/Glow_White"];
            Texture2D Glow_Aqua   = ResourceManager.TextureDict["PlanetGlows/Glow_Aqua"];
            Texture2D Glow_Orange = ResourceManager.TextureDict["PlanetGlows/Glow_Orange"];
            Texture2D sunTexture = null;
            string sunPath = string.Empty;
            for (int index = 0; index < SolarSystemList.Count; index++)
            {
                SolarSystem solarSystem = SolarSystemList[index];
                Vector3 systemV3 = solarSystem.Position.ToVec3();
                if (Frustum.Contains(new BoundingSphere(systemV3, 100000f)) != ContainmentType.Disjoint)
                {
                    ProjectToScreenCoords(solarSystem.Position, 4500f, out Vector2 sysScreenPos, out float sysScreenPosDisToRight);

                    lock (GlobalStats.ClickableSystemsLock)
                        ClickableSystems.Add(new UniverseScreen.ClickableSystem()
                        {
                            Radius = sysScreenPosDisToRight < 8f ? 8f : sysScreenPosDisToRight,
                            ScreenPos = sysScreenPos,
                            systemToClick = solarSystem
                        });
                    if (viewState <= UniverseScreen.UnivScreenState.SectorView)
                    {
                        for (int i = 0; i < solarSystem.PlanetList.Count; i++)
                        {
                            Planet planet = solarSystem.PlanetList[i];
                            if (solarSystem.Explored(EmpireManager.Player))
                            {
                                this.ProjectToScreenCoords(planet.Position, 2500f, planet.SO.WorldBoundingSphere.Radius,
                                    out Vector2 planetScreenPos, out float planetScreenRadius);
                                float scale = planetScreenRadius / 115f;

                                if (planet.planetType == 1 || planet.planetType == 11 ||
                                    (planet.planetType == 13 || planet.planetType == 21) ||
                                    (planet.planetType == 22 || planet.planetType == 25 ||
                                     (planet.planetType == 27 || planet.planetType == 29)))
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Terran, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                else if (planet.planetType == 5 || planet.planetType == 7 ||
                                         (planet.planetType == 8 || planet.planetType == 9) || planet.planetType == 23)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Red, planetScreenPos, new Rectangle?(),
                                        Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                else if (planet.planetType == 17)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_White, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                else if (planet.planetType == 19)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Aqua, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                else if (planet.planetType == 14 || planet.planetType == 18)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Orange, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                lock (GlobalStats.ClickableSystemsLock)
                                    this.ClickPlanetList.Add(new UniverseScreen.ClickablePlanets()
                                    {
                                        ScreenPos = planetScreenPos,
                                        Radius = planetScreenRadius < 8f ? 8f : planetScreenRadius,
                                        planetToClick = planet
                                    });
                            }
                        }
                    }
                    if (viewState < UniverseScreen.UnivScreenState.GalaxyView)
                    {
                        if(solarSystem.SunPath != sunPath)
                            sunTexture = ResourceManager.Texture("Suns/" + solarSystem.SunPath);
                        DrawTransparentModel(SunModel,
                            Matrix.CreateRotationZ(Zrotate) *
                            Matrix.CreateTranslation(solarSystem.Position.ToVec3()), view,
                            projection, sunTexture, 10.0f);
                        DrawTransparentModel(SunModel,
                            Matrix.CreateRotationZ((float) (- Zrotate / 2.0)) *
                            Matrix.CreateTranslation(solarSystem.Position.ToVec3()), view,
                            projection, sunTexture, 10.0f);
                        if (solarSystem.Explored(EmpireManager.Player))
                        {
                            for (int i = 0; i < solarSystem.PlanetList.Count; i++)
                            {
                                Planet planet = solarSystem.PlanetList[i];
                                Vector2 planetScreenPos = ProjectToScreenPosition(planet.Position, 2500f);
                                float planetOrbitRadius = sysScreenPos.Distance(planetScreenPos);
                                if (this.viewState > UniverseScreen.UnivScreenState.ShipView)
                                {
                                    DrawCircle(sysScreenPos, planetOrbitRadius, 100,
                                        new Color((byte)50, (byte)50, (byte)50, (byte)90), 3f);
                                    if (planet.Owner == null)
                                        this.DrawCircle(sysScreenPos, planetOrbitRadius, 100,
                                            new Color((byte) 50, (byte) 50, (byte) 50, (byte) 90), 3f);
                                    else
                                        this.DrawCircle(sysScreenPos, planetOrbitRadius, 100,
                                            new Color(planet.Owner.EmpireColor.R, planet.Owner.EmpireColor.G,
                                                planet.Owner.EmpireColor.B, (byte) 100), 3f);
                                }
                            }
                        }
                    }
                }
            }
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Blend.SourceAlpha;
            renderState.DestinationBlend       = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode               = CullMode.None;
            renderState.DepthBufferWriteEnable = true;
            ScreenManager.SpriteBatch.End();
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
                                DrawCircle(new Vector2(vector3_4.X, vector3_4.Y), radius, 100, new Color((byte)50, (byte)50, (byte)50, (byte)90), 1f);
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
            if (this.viewState > UniverseScreen.UnivScreenState.ShipView)
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
                                item_0.load_and_assign_effects(TransientContent, "Effects/ThrustCylinderB", "Effects/NoiseVolume", this.ThrusterEffect);
                        }
                    }
                }
            }
        }

        public virtual void Render(GameTime gameTime)
        {
            if (Frustum == (BoundingFrustum)null)
                Frustum = new BoundingFrustum(view * projection);
            else
                Frustum.Matrix = view * projection;
            ScreenManager.sceneState.BeginFrameRendering(view, projection, gameTime, ScreenManager.environment, true);
            ScreenManager.editor.BeginFrameRendering(ScreenManager.sceneState);
            lock (GlobalStats.ObjectManagerLocker)
                ScreenManager.inter.BeginFrameRendering(ScreenManager.sceneState);
            RenderBackdrop();
            ScreenManager.SpriteBatch.Begin();
            if (DefiningAO && Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                DrawRectangleProjected(AORect, Color.Red);                
            }
            if (DefiningAO && SelectedShip != null)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(1411), new Vector2((float)SelectedStuffRect.X, (float)(SelectedStuffRect.Y - Fonts.Pirulen16.LineSpacing - 2)), Color.White);
                for (int index = 0; index < this.SelectedShip.AreaOfOperation.Count; index++)
                {
                    Rectangle rectangle = this.SelectedShip.AreaOfOperation[index];
                    this.DrawRectangleProjected(rectangle, Color.Red, new Color(Color.Red, 10));
                }
            }
            else
                DefiningAO = false;
            float num = (float)(150.0 * SelectedSomethingTimer / 3f);
            if (num < 0f)
                num = 0.0f;
            byte alpha = (byte)num;
            if (alpha > 0)
            {
                if (SelectedShip != null)
                {
                    DrawShipLines(SelectedShip, alpha);
                }
                else if (SelectedShipList.Count > 0)
                {
                    for (int index1 = 0; index1 < this.SelectedShipList.Count; ++index1)
                    {
                        try
                        {
                            Ship ship = this.SelectedShipList[index1];
                            DrawShipLines(ship, alpha);
                        }
                        catch
                        {
                            Log.Warning("DrawShipLines Blew Up");
                        }
                    }
                }
            }
            this.ScreenManager.SpriteBatch.End();
            this.DrawBombs();
            lock (GlobalStats.ObjectManagerLocker)
                this.ScreenManager.inter.RenderManager.Render();
            if (viewState < UnivScreenState.SectorView)
            {
                using (player.KnownShips.AcquireReadLock())
                    for (int index = player.KnownShips.Count - 1; index >= 0; index--)
                    {
                        Ship ship = player.KnownShips[index];
                        if (!ship.InFrustum) continue;

                        for (int i = ship.Projectiles.Count - 1; i >= 0; i--)
                        {
                            Projectile projectile = ship.Projectiles[i];
                            if (projectile.Weapon.IsRepairDrone && projectile.GetDroneAI() != null)
                            {
                                for (int j = 0; j < projectile.GetDroneAI().Beams.Count; ++j)
                                    projectile.GetDroneAI().Beams[j].Draw(ScreenManager);
                            }
                        }

                        {
                            for (int i = ship.Beams.Count - 1; i >= 0; --i) // regular FOR to mitigate multi-threading issues
                            {
                                Beam beam = ship.Beams[i];
                                if (beam.Source.InRadius(beam.ActualHitDestination, beam.Range + 10.0f))
                                    beam.Draw(ScreenManager);
                                else
                                    beam.Die(null, true);
                            }
                        }
                    }
            }

            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.DepthBufferWriteEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            foreach (Anomaly anomaly in anomalyManager.AnomaliesList)
                anomaly.Draw();
            if (viewState < UnivScreenState.SectorView)
                for (int i = 0; i < SolarSystemList.Count; i++)
                {
                    SolarSystem solarSystem = SolarSystemList[i];
                    if (!solarSystem.Explored(player)) continue;

                    for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                    {
                        Planet p = solarSystem.PlanetList[j];
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
            renderState.AlphaBlendEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            if (viewState < UnivScreenState.SectorView)
            {
                RenderThrusters();
                RenderParticles();

                FTLManager.DrawFTLModels(this);
                lock (GlobalStats.ExplosionLocker)
                {
                    for (int i = 0; i < MuzzleFlashManager.FlashList.Count; i++)
                    {
                        MuzzleFlash flash = MuzzleFlashManager.FlashList[i];
                        DrawTransparentModel(MuzzleFlashManager.flashModel, flash.WorldMatrix, view,
                            projection, MuzzleFlashManager.FlashTexture, flash.scale);
                    }
                    MuzzleFlashManager.FlashList.ApplyPendingRemovals();
                }
                beamflashes.Draw(gameTime);
                explosionParticles.Draw(gameTime);
                photonExplosionParticles.Draw(gameTime);
                explosionSmokeParticles.Draw(gameTime);
                projectileTrailParticles.Draw(gameTime);
                fireTrailParticles.Draw(gameTime);
                smokePlumeParticles.Draw(gameTime);
                fireParticles.Draw(gameTime);
                engineTrailParticles.Draw(gameTime);
                star_particles.Draw(gameTime);
                neb_particles.Draw(gameTime);
                flameParticles.Draw(gameTime);
                sparks.Draw(gameTime);
                lightning.Draw(gameTime);
                flash.Draw(gameTime);
            }
            if (!Paused) // Particle pools need to be updated
            {
                beamflashes.Update(gameTime);
                explosionParticles.Update(gameTime);
                photonExplosionParticles.Update(gameTime);
                explosionSmokeParticles.Update(gameTime);
                projectileTrailParticles.Update(gameTime);
                fireTrailParticles.Update(gameTime);
                smokePlumeParticles.Update(gameTime);
                fireParticles.Update(gameTime);
                engineTrailParticles.Update(gameTime);
                star_particles.Update(gameTime);
                neb_particles.Update(gameTime);
                flameParticles.Update(gameTime);
                sparks.Update(gameTime);
                lightning.Update(gameTime);
                flash.Update(gameTime);
            }
            lock (GlobalStats.ObjectManagerLocker)
            {
                ScreenManager.inter.EndFrameRendering();
                ScreenManager.editor.EndFrameRendering();
                ScreenManager.sceneState.EndFrameRendering();
            }
            if (viewState < UnivScreenState.SectorView)
                DrawShields();
            renderState.DepthBufferWriteEnable = true;
        }
 

        private void DrawShipLines(Ship ship, byte alpha)
        {
            if (ship == null)
                return;
            Color color;
                Vector2 start = ship.Center;
                
                ArtificialIntelligence.ShipGoal goal;
                if (!ship.InCombat || ship.AI.HasPriorityOrder)
                {
                    color = Colors.Orders(alpha); 
                    if (ship.AI.State == AIState.Ferrying)
                    {
                        DrawLineProjected(start, ship.AI.EscortTarget.Center, color);
                        return;
                    }
                    if (ship.AI.State == AIState.ReturnToHangar)
                    {
                        if (ship.Mothership != null)
                            DrawLineProjected(start, ship.Mothership.Center, color);
                        else
                            ship.AI.State = AIState.AwaitingOrders; //@todo this looks like bug fix hack. investigate and fix. 
                        return;
                    }
                    if (ship.AI.State == AIState.Escort && ship.AI.EscortTarget != null)
                    {
                        DrawLineProjected(start, ship.AI.EscortTarget.Center, color);
                        return;
                    }

                    if (ship.AI.State == AIState.Explore && ship.AI.ExplorationTarget != null)
                    {
                        DrawLineProjected(start, ship.AI.ExplorationTarget.Position, color);
                        return;
                    }

                    if (ship.AI.State == AIState.Colonize && ship.AI.ColonizeTarget != null)
                    {
                        Vector2 screenPos = ProjectToScreenPosition(ship.Center);
                        Vector2 screenPosTarget = ProjectToScreenPosition(ship.AI.ColonizeTarget.Position,2500f);
                        DrawLine(screenPos, screenPosTarget, color);
                        string text = String.Format("Colinize\nSystem : {0}\nPlanet : {1}", ship.AI.ColonizeTarget.ParentSystem.Name, ship.AI.ColonizeTarget.Name);
                        DrawPointerWithText(screenPos, ResourceManager.Texture("UI/planetNamePointer"), color, text, new Color(ship.loyalty.EmpireColor, alpha));
                        return;
                    }
                    if (ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget != null)
                    {
                        DrawLineProjected(start, ship.AI.OrbitTarget.Position, color, 2500f);
                        return;
                    }
                    if (ship.AI.State == AIState.Rebase)
                    {
                        DrawWayPointLines(ship, color);
                        return;
                    }
                    goal = ship.AI.OrderQueue.PeekFirst;
                    if (ship.AI.State == AIState.Bombard && goal?.TargetPlanet != null)
                    {
                        DrawLineProjected(ship.Center, goal.TargetPlanet.Position, Colors.CombatOrders(alpha), 2500f);
                        DrawWayPointLines(ship, Colors.CombatOrders(alpha));
                    }
                }
                if (!ship.AI.HasPriorityOrder && (ship.AI.State == AIState.AttackTarget || ship.AI.State == AIState.Combat) && ship.AI.Target is Ship)
                {                    
                    DrawLineProjected(ship.Center, ship.AI.Target.Center, Colors.Attack(alpha));
                    if (ship.AI.TargetQueue.Count > 1)
                    {                        
                        for (int i = 0; i < ship.AI.TargetQueue.Count - 1; ++i)
                        {
                            var target = ship.AI.TargetQueue[i];
                            if (!target?.Active ?? true) continue;
                            DrawLineProjected(target.Center, ship.AI.TargetQueue[i].Center, Colors.Attack((byte)(alpha * .5f)));
                        }
                    }
                    return;
                }
                if (ship.AI.State == AIState.Boarding && ship.AI.EscortTarget != null)
                {
                    DrawLineProjected(start, ship.AI.EscortTarget.Center, Colors.CombatOrders(alpha));
                    return;
                }
                if (ship.AI.State == AIState.AssaultPlanet && ship.AI.OrbitTarget != null)
                {
                    int spots = ship.AI.OrbitTarget.GetGroundLandingSpots();
                    if (spots > 4)
                        DrawLineProjected(start, ship.AI.OrbitTarget.Position, Colors.CombatOrders(alpha), 2500f);
                    else if (spots > 0)
                        DrawLineProjected(start, ship.AI.OrbitTarget.Position, Colors.Warning(alpha), 2500f);
                    else
                        DrawLineProjected(start, ship.AI.OrbitTarget.Position, Colors.Error(alpha), 2500f);
                    DrawWayPointLines(ship, new Color(Color.Lime, alpha));
                    return;
                }

                DrawWayPointLines(ship, Colors.WayPoints(alpha));
        }

        public void DrawWayPointLines(Ship ship, Color color)
        {
            if (ship.AI.ActiveWayPoints.Count < 1)
                return;

            Vector2[] waypoints;
            lock (ship.AI.WayPointLocker)
                waypoints = ship.AI.ActiveWayPoints.ToArray();

            DrawLineProjected(ship.Center, waypoints[0], color);

            for (int i = 1; i < waypoints.Length; ++i)
            {
                DrawLineProjected(waypoints[i-1], waypoints[i], color);
            }
        }
                  
        protected void DrawShields()
        {            
            var renderState                    = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable       = true;
            renderState.AlphaBlendOperation    = BlendFunction.Add;
            renderState.SourceBlend            = Blend.SourceAlpha;
            renderState.DestinationBlend       = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            ShieldManager.Draw(view, projection);
        }

        protected virtual void DrawPlanetInfo()
        {
            if (LookingAtPlanet || viewState > UnivScreenState.SectorView || viewState < UnivScreenState.ShipView)
                return;
            Vector2 mousePos              = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
            Texture2D planetNamePointer   = ResourceManager.Texture("UI/planetNamePointer");
            Texture2D icon_fighting_small = ResourceManager.Texture("UI/icon_fighting_small");
            Texture2D icon_spy_small      = ResourceManager.Texture("UI/icon_spy_small");
            Texture2D icon_anomaly_small  = ResourceManager.Texture("UI/icon_anomaly_small");
            Texture2D icon_troop          = ResourceManager.Texture("UI/icon_troop");
            for (int k = 0; k < SolarSystemList.Count; k++)
            {
                SolarSystem solarSystem = SolarSystemList[k];
                if (!solarSystem.isVisible)
                    continue;

                for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                {
                    Planet planet = solarSystem.PlanetList[j];
                    if (!planet.IsExploredBy(player))
                        continue;

                    Vector2 screenPosPlanet = this.ProjectToScreenPosition(planet.Position, 2500f);
                    Vector2 posOffSet = screenPosPlanet;
                    posOffSet.X += 20f;
                    posOffSet.Y += 37f;
                    int drawLocationOffset = 0;

                    DrawPointerWithText(screenPosPlanet, planetNamePointer, Color.Green, planet.Name, planet.Owner?.EmpireColor ?? Color.White);

                    posOffSet = new Vector2(screenPosPlanet.X + 10f, screenPosPlanet.Y + 60f);

                    if (planet.RecentCombat)
                    {
                        this.DrawTextureWithToolTip(icon_fighting_small, Color.White, 121, mousePos, (int) posOffSet.X,
                            (int) posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }
                    if (this.player.data.MoleList.Count > 0)
                    {
                        for (int i = 0; i < ((Array<Mole>) this.player.data.MoleList).Count; i++)
                        {
                            Mole mole = ((Array<Mole>) this.player.data.MoleList)[i];
                            if (mole.PlanetGuid == planet.guid)
                            {
                                posOffSet.X += (float) (18 * drawLocationOffset);
                                DrawTextureWithToolTip(icon_spy_small, Color.White, 121, mousePos,
                                    (int) posOffSet.X, (int) posOffSet.Y, 14, 14);
                                ++drawLocationOffset;
                                break;
                            }
                        }
                    }
                    for (int i = 0; i < planet.BuildingList.Count; i++)
                    {
                        Building building = planet.BuildingList[i];
                        if (string.IsNullOrEmpty(building.EventTriggerUID)) continue;
                        posOffSet.X += (float) (18 * drawLocationOffset);
                        string text = Localizer.Token(building.DescriptionIndex);
                        DrawTextureWithToolTip(icon_anomaly_small, Color.White, text, mousePos, (int) posOffSet.X,
                            (int) posOffSet.Y, 14, 14);
                        break;
                    }
                    int troopCount = planet.CountEmpireTroops(player);
                    if (troopCount > 0)
                    {
                        posOffSet.X += (float)(18 * drawLocationOffset);
                        DrawTextureWithToolTip(icon_troop, Color.TransparentWhite, string.Format("Troops {0}",troopCount), mousePos,
                            (int)posOffSet.X, (int)posOffSet.Y, 14, 14);
                        ++drawLocationOffset;
                    }
                    
                }
            }
        }
        //This will likely only work with "this UI\planetNamePointer" texture 
        //Other textures might work but would need the x and y offset adjusted. 
        public void DrawPointerWithText(Vector2 screenPos, Texture2D planetNamePointer, Color pointerColor, string text, Color textColor, SpriteFont font = null, float xOffSet =20f, float yOffSet = 37f)
        {
            font = font ?? Fonts.Tahoma10;
            DrawTextureRect(planetNamePointer, screenPos, pointerColor);
            Vector2 posOffSet = screenPos;
            posOffSet.X      += xOffSet;
            posOffSet.Y      += yOffSet;
            HelperFunctions.ClampVectorToInt(ref posOffSet);            
            ScreenManager.SpriteBatch.DrawString(font, text, posOffSet, textColor);
        }


        // this does some magic to convert a game position/coordinate to a drawable screen position
        private Vector2 ProjectToScreenPosition(Vector2 posInWorld, float zAxis = 0f)
        {
            //return ScreenManager.GraphicsDevice.Viewport.Project(position.ToVec3(zAxis), projection, view, Matrix.Identity).ToVec2();
            return ScreenManager.GraphicsDevice.Viewport.ProjectTo2D(posInWorld.ToVec3(zAxis), ref projection, ref view);
        }

        private void ProjectToScreenCoords(Vector2 posInWorld, float zAxis, float sizeInWorld, out Vector2 posOnScreen, out float sizeOnScreen)
        {
            posOnScreen = ProjectToScreenPosition(posInWorld, zAxis);
            sizeOnScreen = ProjectToScreenPosition(new Vector2(posInWorld.X + sizeInWorld, posInWorld.Y),zAxis).Distance(ref posOnScreen);
        }

        private void ProjectToScreenCoords(Vector2 posInWorld, float sizeInWorld, out Vector2 posOnScreen, out float sizeOnScreen)
        {
            ProjectToScreenCoords(posInWorld, 0f, sizeInWorld, out posOnScreen, out sizeOnScreen);
        }

        private void ProjectToScreenCoords(Vector2 posInWorld, float widthInWorld, float heightInWorld, 
                                       out Vector2 posOnScreen, out float widthOnScreen, out float heightOnScreen)
        {
            posOnScreen    = ProjectToScreenPosition(posInWorld);
            widthOnScreen  = ProjectToScreenPosition(new Vector2(posInWorld.X + widthInWorld,  posInWorld.Y)).Distance(ref posOnScreen);
            heightOnScreen = ProjectToScreenPosition(new Vector2(posInWorld.X + heightInWorld, posInWorld.Y)).Distance(ref posOnScreen);
        }

        private Vector2 ProjectToScreenSize(float widthInWorld, float heightInWorld)
        {
            return ProjectToScreenPosition(new Vector2(widthInWorld, heightInWorld));
        }

        private float ProjectToScreenSize(float sizeInWorld)
        {
            Vector2 zero = ProjectToScreenPosition(Vector2.Zero);
            return zero.Distance(ProjectToScreenPosition(new Vector2(sizeInWorld, 0f)));
        }
        public Vector2 UnprojectToWorldPosition(Vector2 screenSpace)
        {
            Vector3 position = ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(screenSpace, 0.0f), projection, view, Matrix.Identity);
            Vector3 direction = ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(screenSpace, 1f), projection, view, Matrix.Identity) - position;
            direction.Normalize();
            var ray = new Ray(position, direction);
            float num = -ray.Position.Z / ray.Direction.Z;
            var vector3 = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, 0.0f);
            return new Vector2(vector3.X, vector3.Y);
        }
        // projects the line from World positions into Screen positions, then draws the line
        public void DrawLineProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, float zAxis = 0f)
        {
            DrawLine(ProjectToScreenPosition(startInWorld, zAxis), ProjectToScreenPosition(endInWorld, zAxis), color);
        }

        // non-projected draw to screen
        public void DrawLinesToScreen(Vector2 posOnScreen, Array<string> lines)
        {
            foreach (string line in lines)
            {
                if (line.Length != 0)
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, line, posOnScreen, Color.White);
                posOnScreen.Y += Fonts.Arial12Bold.LineSpacing + 2;
            }
        }
        
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, int sides, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            DrawCircle(screenPos, screenRadius, sides, color, thickness);
        }

        public void DrawCircleProjectedZ(Vector2 posInWorld, float radiusInWorld, Color color, int sides = 16, float zAxis = 0f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            DrawCircle(screenPos, screenRadius, sides, color);
        }

        // draws a projected circle, with an additional overlay texture
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, int sides, float thickness, Texture2D overlay, Color overlayColor)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            float scale = screenRadius / overlay.Width;
            DrawTexture(overlay, screenPos, scale, 0f, overlayColor);
            DrawCircle(screenPos, screenRadius, sides, color, thickness);
        }

        public void DrawRectangleProjected(Rectangle rectangle, Color edge)
        {
            Vector2 rectTopLeft = ProjectToScreenPosition(new Vector2((float)rectangle.X, (float)rectangle.Y), 0f);
            Vector2 rectBotRight = ProjectToScreenPosition(new Vector2((float)rectangle.X, (float)rectangle.Y), 0f);
            Rectangle rect = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, (int)Math.Abs(rectTopLeft.X - rectBotRight.X), (int)Math.Abs(rectTopLeft.Y - rectBotRight.Y));
            DrawRectangle(rect, edge);
        }
        public void DrawRectangleProjected(Rectangle rectangle, Color edge, Color fill)
        {
            Vector2 rectTopLeft  = ProjectToScreenPosition(new Vector2((float)rectangle.X, (float)rectangle.Y), 0f);
            Vector2 rectBotRight = ProjectToScreenPosition(new Vector2((float)rectangle.X, (float)rectangle.Y), 0f);
            Rectangle rect       = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, 
                                    (int)Math.Abs(rectTopLeft.X - rectBotRight.X), (int)Math.Abs(rectTopLeft.Y - rectBotRight.Y));
            DrawRectangle(rect, edge, fill);            
        }

        public void DrawTextureProjected(Texture2D texture, Vector2 posInWorld, float textureScale, Color color)
            => DrawTexture(texture, ProjectToScreenPosition(posInWorld), textureScale, 0.0f, color);

        public void DrawTextureProjected(Texture2D texture, Vector2 posInWorld, float textureScale, float rotation, Color color)
            => DrawTexture(texture, ProjectToScreenPosition(posInWorld), textureScale, rotation, color);

        public void DrawTextureWithToolTip(Texture2D texture, Color color, int tooltipID, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            Rectangle rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);
            
            if (HelperFunctions.CheckIntersection(rectangle, mousePos))
            {
                ToolTip.CreateTooltip(tooltipID, ScreenManager);                
            }
        }
        public void DrawTextureWithToolTip(Texture2D texture, Color color, string text, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            Rectangle rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);

            if (HelperFunctions.CheckIntersection(rectangle, mousePos))
            {
                ToolTip.CreateTooltip(text, ScreenManager);
            }
        }
        public void DrawStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 screenPos = ProjectToScreenPosition(posInWorld);
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, screenPos, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
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
            DrawModelMesh(model, Matrix.CreateScale(50f) * world, viewMat, new Vector3(1f, 1f, 1f), projMat, projTex);
            renderState.DepthBufferWriteEnable = true;
        }

        protected void DrawTransparentModelAdditiveNoAlphaFade(Model model, Matrix world, Matrix viewMat, Matrix projMat, Texture2D projTex, float scale)
            => DrawModelMesh(model, world, viewMat, new Vector3(1f, 1f, 1f), projMat, projTex);

        protected void DrawTransparentModelAdditive(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex, float scale)
            =>  DrawModelMesh(model, world, view, new Vector3(1f, 1f, 1f), projection, projTex, camHeight / 3500000);            
        
        public void DrawSunModel(Matrix world, Texture2D texture, float scale)        
            => DrawTransparentModel(SunModel, world, view, projection, texture, scale);
        
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
            DrawModelMesh(model, Matrix.CreateScale(50f) * Matrix.CreateScale(scale) * world, view, Color, projection, projTex);

            renderState.DepthBufferWriteEnable = true;
        }

        public static FileInfo[] GetFilesFromDirectory(string DirPath)
        {
            return new DirectoryInfo(DirPath).GetFiles("*.*", SearchOption.AllDirectories);
        }

        protected override void Dispose(bool disposing)
        {
            starfield               ?.Dispose(ref starfield);
            DeepSpaceDone           ?.Dispose(ref DeepSpaceDone);
            EmpireDone              ?.Dispose(ref EmpireDone);
            DeepSpaceGateKeeper     ?.Dispose(ref DeepSpaceGateKeeper);
            ItemsToBuild            ?.Dispose(ref ItemsToBuild);
            anomalyManager          ?.Dispose(ref anomalyManager);
            bloomComponent          ?.Dispose(ref bloomComponent);
            ShipGateKeeper          ?.Dispose(ref ShipGateKeeper);
            SystemThreadGateKeeper  ?.Dispose(ref SystemThreadGateKeeper);
            FogMap                  ?.Dispose(ref FogMap);
            MasterShipList          ?.Dispose(ref MasterShipList);
            EmpireGateKeeper        ?.Dispose(ref EmpireGateKeeper);
            BombList                ?.Dispose(ref BombList);
            flash                   ?.Dispose(ref flash);
            lightning               ?.Dispose(ref lightning);
            neb_particles           ?.Dispose(ref neb_particles);
            photonExplosionParticles?.Dispose(ref photonExplosionParticles);
            projectileTrailParticles?.Dispose(ref projectileTrailParticles);
            sceneMap                ?.Dispose(ref sceneMap);
            shipListInfoUI          ?.Dispose(ref shipListInfoUI);
            smokePlumeParticles     ?.Dispose(ref smokePlumeParticles);
            sparks                  ?.Dispose(ref sparks);
            star_particles          ?.Dispose(ref star_particles);
            engineTrailParticles    ?.Dispose(ref engineTrailParticles);
            explosionParticles      ?.Dispose(ref explosionParticles);
            explosionSmokeParticles ?.Dispose(ref explosionSmokeParticles);
            fireTrailParticles      ?.Dispose(ref fireTrailParticles);
            fireParticles           ?.Dispose(ref fireParticles);
            flameParticles          ?.Dispose(ref flameParticles);
            beamflashes             ?.Dispose(ref beamflashes);
            dsbw                    ?.Dispose(ref dsbw);
            SelectedShipList        ?.Dispose(ref SelectedShipList);
            NotificationManager     ?.Dispose(ref NotificationManager);
            FogMapTarget            ?.Dispose(ref FogMapTarget);

            base.Dispose(true);
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
            DetailView = 10000,
            ShipView   = 30000,
            SystemView = 250000,
            SectorView = 1775000,
            GalaxyView  ,
        }


        public float GetZfromScreenState(UnivScreenState screenState)
        {
            float returnZ = 0;
            switch (screenState)
            {
                case UnivScreenState.DetailView:
                    returnZ = (float)UnivScreenState.DetailView;
                    break;
                case UnivScreenState.ShipView:
                    returnZ = (float)UnivScreenState.ShipView; 
                    break;
                case UnivScreenState.SystemView:
                    returnZ = (float)UnivScreenState.SystemView;
                    break;
                case UnivScreenState.SectorView:
                    returnZ = (float)UnivScreenState.SectorView; // 1775000.0f;
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

