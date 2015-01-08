// Type: Ship_Game.UniverseScreen
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
using SynapseGaming.LightingSystem.Rendering;
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

        List<float> perfavg = new List<float>();
        List<float> perfavg2 = new List<float>();

        public static float GamePaceStatic = 1f;
        public static float GameScaleStatic = 1f;
        public static bool ShipWindowOpen = false;
        public static bool ColonizeWindowOpen = false;
        public static bool PlanetViewWindowOpen = false;
        public static SpatialManager DeepSpaceManager = new SpatialManager();
        public static SpatialManager ShipSpatialManager = new SpatialManager();
        public static List<SolarSystem> SolarSystemList = new List<SolarSystem>();
        public static BatchRemovalCollection<SpaceJunk> JunkList = new BatchRemovalCollection<SpaceJunk>();
        public static bool DisableClicks = false;
        //private static string fmt = "00000.##";
        private static string fmt2 = "0.#";
        public RandomThreadMath DeepSpaceRNG = new RandomThreadMath();
        public float GamePace = 1f;
        public float GameScale = 1f;
        public float GameSpeed = 1f;
        public float StarDate = 1000f;
        public string StarDateFmt = "0000.0";
        public float StarDateTimer = 5f;
        public float perStarDateTimer = 1000f;
        public float AutoSaveTimer = GlobalStats.Config.AutoSaveInterval;
        public bool MultiThread = true;
        public List<UniverseScreen.ClickablePlanets> ClickPlanetList = new List<UniverseScreen.ClickablePlanets>();
        public BatchRemovalCollection<UniverseScreen.ClickableItemUnderConstruction> ItemsToBuild = new BatchRemovalCollection<UniverseScreen.ClickableItemUnderConstruction>();
        protected List<UniverseScreen.ClickableSystem> ClickableSystems = new List<UniverseScreen.ClickableSystem>();
        public BatchRemovalCollection<Ship> SelectedShipList = new BatchRemovalCollection<Ship>();
        protected List<UniverseScreen.ClickableShip> ClickableShipsList = new List<UniverseScreen.ClickableShip>();
        protected float PieMenuDelay = 1f;
        protected Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();
        public Background bg = new Background();
        public Vector2 Size = new Vector2(5000000f, 5000000f);
        public float FTLModifier = 1f;
        public float EnemyFTLModifier = 1f;
        public UniverseData.GameDifficulty GameDifficulty = UniverseData.GameDifficulty.Normal;
        public Vector3 transitionStartPosition = new Vector3();
        public Vector3 camTransitionPosition = new Vector3();
        public List<NebulousOverlay> Stars = new List<NebulousOverlay>();
        public List<NebulousOverlay> NebulousShit = new List<NebulousOverlay>();
        private Rectangle ScreenRectangle = new Rectangle();
        public Dictionary<Guid, Planet> PlanetsDict = new Dictionary<Guid, Planet>();
        private List<Thread> SystemUpdateThreadList = new List<Thread>();
        private ManualResetEvent[] SystemResetEvents = new ManualResetEvent[4];
        private AutoResetEvent[] SystemGateKeeper = new AutoResetEvent[4];
        public Dictionary<Guid, SolarSystem> SolarSystemDict = new Dictionary<Guid, SolarSystem>();
        public BatchRemovalCollection<Bomb> BombList = new BatchRemovalCollection<Bomb>();
        public AutoResetEvent WorkerBeginEvent = new AutoResetEvent(false);
        public ManualResetEvent WorkerCompletedEvent = new ManualResetEvent(true);
        public float camHeight = 2550f;
        public Vector3 camPos = Vector3.Zero;
        public List<Ship> ShipsToAdd = new List<Ship>();
        protected float TooltipTimer = 0.5f;
        protected float sTooltipTimer = 0.5f;
        protected float TimerDelay = 0.25f;
        protected GameTime zgameTime = new GameTime();
        public List<ShipModule> ModulesNeedingReset = new List<ShipModule>();
        private bool flip = true;
        private int Auto = 1;
        private AutoResetEvent ShipGateKeeper = new AutoResetEvent(false);
        private ManualResetEvent SystemThreadGateKeeper = new ManualResetEvent(false);
        private AutoResetEvent DeepSpaceGateKeeper = new AutoResetEvent(false);
        private ManualResetEvent DeepSpaceDone = new ManualResetEvent(false);
        private AutoResetEvent EmpireGateKeeper = new AutoResetEvent(false);
        private ManualResetEvent EmpireDone = new ManualResetEvent(false);
        public List<Ship> ShipsToRemove = new List<Ship>();
        public List<Projectile> DSProjectilesToAdd = new List<Projectile>();
        private List<Ship> DeepSpaceShips = new List<Ship>();
        private object thislock = new object();
        public bool ViewingShip = true;
        public float transDuration = 3f;
        protected float SectorMiniMapHeight = 20000f;
        public Vector2 mouseWorldPos = new Vector2();
        public float SelectedSomethingTimer = 3f;
        private List<UniverseScreen.FleetButton> FleetButtons = new List<UniverseScreen.FleetButton>();
        protected Vector2 startDrag = new Vector2();
        private Vector2 ProjectedPosition = new Vector2();
        protected float desiredSectorZ = 20000f;
        public List<UniverseScreen.FogOfWarNode> FogNodes = new List<UniverseScreen.FogOfWarNode>();
        private bool FogOn = true;
        private bool drawBloom = true;
        private List<UniverseScreen.ClickableFleet> ClickableFleetsList = new List<UniverseScreen.ClickableFleet>();
        private bool ShowTacticalCloseup;
        public bool Debug;
        public bool GridOn;
        public Planet SelectedPlanet;
        public Ship SelectedShip;
        public UniverseScreen.ClickableItemUnderConstruction SelectedItem;
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
        public string PlayerLoyalty;
        public string loadFogPath;
        protected Model SunModel;
        protected Model NebModel;
        public Model xnaPlanetModel;
        public Texture2D RingTexture;
        public AudioListener listener;
        public Effect ThrusterEffect;
        public UniverseScreen.UnivScreenState viewState;
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
        //private float CamHeightAtScreenWidth;
        private float ArmageddonTimer;
        public Empire player;
        private MiniMap minimap;
        private bool loading;
        public Thread WorkerThread;
        public bool WorkerUpdateGameWorld;
        public Ship playerShip;
        public float transitionElapsedTime;
        protected float Zrotate;
        public BoundingFrustum Frustum;
        protected UniverseScreen.ClickablePlanets tippedPlanet;
        protected UniverseScreen.ClickableSystem tippedSystem;
        protected bool ShowingSysTooltip;
        protected bool ShowingPlanetToolTip;
        protected float ClickTimer;
        protected float ClickTimer2;
        protected float zTime;
        private float MusicCheckTimer;
        private int ArmageddonCounter;
        private float shiptimer;
        private Thread ShipUpdateThread;
        public Ship ShipToView;
        public float HeightOnSnap;
        public float AdjustCamTimer;
        public bool SnapBackToSystem;
        public AutomationWindow aw;
        public bool DefiningAO;
        public Rectangle AORect;
        public bool showingDSBW;
        public DeepSpaceBuildingWindow dsbw;
        private DebugInfoScreen debugwin;
        //private bool doubleTime;
        private bool ShowShipNames;
        public InputState input;
        private float Memory;
        public bool Paused;
        private bool SkipRightOnce;
        private bool UseRealLights;
        private bool showdebugwindow;
        private bool NeedARelease;
        //private int counter;
        public SolarSystem SelectedSystem;
        public Fleet SelectedFleet;
        private List<Fleet.Squad> SelectedFlank;
        private int FBTimer;
        private bool pickedSomethingThisFrame;
        private Vector2 startDragWorld;
        private Vector2 endDragWorld;
        private ShipGroup projectedGroup;
        private bool ProjectingPosition;
        //private bool draggingCam;
        //private Vector2 StartCamDragPos;
        private bool SelectingWithBox;
        //private bool computeCircle;
        private Effect AtmoEffect;
        private Model atmoModel;
        public PlanetScreen workersPanel;
        private ResolveTexture2D sceneMap;
        //private float scaleTimer;
        //private float FleetPosUpdateTimer;
        protected UniverseScreen.CursorState cState;
        //private int cursorFrame;
        private float radlast;
        private int SelectorFrame;
        private float garbageCollector;
        private float garbargeCollectorBase = 10;
        public static bool debug;
        public int globalshipCount;
        public int empireShipCountReserve;
        private float ztimeSnapShot;


        static UniverseScreen()
        {
        }

        public UniverseScreen()
        {
        }

        public UniverseScreen(UniverseData data)
        {
            this.Size = data.Size;
            this.FTLModifier = data.FTLSpeedModifier;
            this.EnemyFTLModifier = data.EnemyFTLSpeedModifier;
            this.GravityWells = data.GravityWells;
            UniverseScreen.SolarSystemList = data.SolarSystemsList;
            this.MasterShipList = data.MasterShipList;
            this.playerShip = data.playerShip;
            this.PlayerLoyalty = this.playerShip.loyalty.data.Traits.Name;
            this.playerShip.loyalty.isPlayer = true;
            this.ShipToView = this.playerShip;
        }

        public UniverseScreen(UniverseData data, string loyalty)
        {
            this.Size = data.Size;
            this.FTLModifier = data.FTLSpeedModifier;
            this.EnemyFTLModifier = data.EnemyFTLSpeedModifier;
            this.GravityWells = data.GravityWells;
            UniverseScreen.SolarSystemList = data.SolarSystemsList;
            this.MasterShipList = data.MasterShipList;
            this.loadFogPath = data.loadFogPath;
            this.PlayerLoyalty = loyalty;
            this.playerShip = data.playerShip;
            EmpireManager.GetEmpireByName(loyalty).isPlayer = true;
            this.ShipToView = this.playerShip;
            this.loading = true;
        }

        public UniverseScreen(int numsys, float size)
        {
            this.Size.X = size;
            this.Size.Y = size;
        }

        ~UniverseScreen()
        {
            this.Dispose(false);
        }

        public void SetLighting(bool Real)
        {
            lock (GlobalStats.ObjectManagerLocker)
                this.ScreenManager.inter.LightManager.Clear();
            if (!Real)
            {
                lock (GlobalStats.ObjectManagerLocker)
                {
                    LightRig local_0 = this.ScreenManager.Content.Load<LightRig>("example/NewGamelight_rig");
                    this.ScreenManager.inter.LightManager.Clear();
                    this.ScreenManager.inter.LightManager.Submit((ILightRig)local_0);
                }
            }
            else
            {
                lock (GlobalStats.ObjectManagerLocker)
                {
                    foreach (SolarSystem item_0 in UniverseScreen.SolarSystemList)
                    {
                        PointLight local_2 = new PointLight();
                        local_2.DiffuseColor = new Vector3(1f, 1f, 0.85f);
                        local_2.Intensity = 2.5f;
                        local_2.ObjectType = ObjectType.Dynamic;
                        local_2.FillLight = true;
                        local_2.Radius = 100000f;
                        local_2.Position = new Vector3(item_0.Position, 2500f);
                        local_2.World = Matrix.Identity * Matrix.CreateTranslation(local_2.Position);
                        local_2.Enabled = true;
                        this.ScreenManager.inter.LightManager.Submit((ILight)local_2);
                        PointLight local_3 = new PointLight();
                        local_3.DiffuseColor = new Vector3(1f, 1f, 0.85f);
                        local_3.Intensity = 2.5f;
                        local_3.ObjectType = ObjectType.Dynamic;
                        local_3.FillLight = false;
                        local_3.Radius = 5000f;
                        local_3.Position = new Vector3(item_0.Position, -2500f);
                        local_3.World = Matrix.Identity * Matrix.CreateTranslation(local_3.Position);
                        local_3.Enabled = true;
                        this.ScreenManager.inter.LightManager.Submit((ILight)local_3);
                        PointLight local_4 = new PointLight();
                        local_4.DiffuseColor = new Vector3(1f, 1f, 0.85f);
                        local_4.Intensity = 1f;
                        local_4.ObjectType = ObjectType.Dynamic;
                        local_4.FillLight = false;
                        local_4.Radius = 100000f;
                        local_4.Position = new Vector3(item_0.Position, -6500f);
                        local_4.World = Matrix.Identity * Matrix.CreateTranslation(local_4.Position);
                        local_4.Enabled = true;
                        this.ScreenManager.inter.LightManager.Submit((ILight)local_4);
                    }
                }
            }
        }

        protected virtual void LoadMenu()
        {
            this.pieMenu = new PieMenu();
            this.planetMenu = new PieMenuNode();
            this.planetMenu.Add(new PieMenuNode("View Planet", ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.ViewPlanet)));
            this.planetMenu.Add(new PieMenuNode("Mark for Colonization", ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.MarkForColonization)));
            this.shipMenu = new PieMenuNode();
            this.shipMenu.Add(new PieMenuNode("Commandeer Ship", ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.ViewShip)));
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
            Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f)) * Matrix.CreateRotationX(MathHelper.ToRadians(0.0f)) * Matrix.CreateLookAt(new Vector3(this.camPos.X, this.camPos.Y, DesiredCamHeight), new Vector3(this.camPos.X, this.camPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
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
            this.workersPanel = (PlanetScreen)new CombatScreen(this.ScreenManager, this.SelectedPlanet);
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
            this.SelectedShip.GetAI().State = AIState.Escort;
            this.SelectedShip.GetAI().EscortTarget = this.playerShip;
        }

        public void RefitTo(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.ScreenManager.AddScreen((GameScreen)new RefitToWindow(this.SelectedShip));
        }

        public void OrderScrap(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.GetAI().OrderScrapShip();
        }

        public void OrderScuttle(object sender)
        {
            if (this.SelectedShip == null)
                return;
            this.SelectedShip.ScuttleTimer = 10f;
        }

        protected void LoadShipMenuNodes(int which)
        {
            this.shipMenu.Children.Clear();
            if (which == 1)
            {
                if (this.SelectedShip != null && this.SelectedShip == this.playerShip)
                    this.shipMenu.Add(new PieMenuNode("Relinquish Control", ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.ViewShip)));
                else
                    this.shipMenu.Add(new PieMenuNode(Localizer.Token(1412), ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.ViewShip)));
                PieMenuNode newChild1 = new PieMenuNode(Localizer.Token(1413), ResourceManager.TextureDict["UI/OrdersIcon"], (SimpleDelegate)null);
                this.shipMenu.Add(newChild1);
                if (this.SelectedShip != null && (double)this.SelectedShip.CargoSpace_Max > 0.0)
                {
                    PieMenuNode newChild2 = new PieMenuNode(Localizer.Token(1414), ResourceManager.TextureDict["UI/PatrolIcon"], new SimpleDelegate(this.DoTransport));
                    newChild1.Add(newChild2);
                    PieMenuNode newChild3 = new PieMenuNode(Localizer.Token(1415), ResourceManager.TextureDict["UI/marketIcon"], new SimpleDelegate(this.DoTransportGoods));
                    newChild1.Add(newChild3);
                }
                PieMenuNode newChild4 = new PieMenuNode(Localizer.Token(1416), ResourceManager.TextureDict["UI/marketIcon"], new SimpleDelegate(this.DoExplore));
                newChild1.Add(newChild4);
                PieMenuNode newChild5 = new PieMenuNode("Empire Defense", ResourceManager.TextureDict["UI/PatrolIcon"], new SimpleDelegate(this.DoDefense));
                newChild1.Add(newChild5);
                PieMenuNode newChild6 = new PieMenuNode(Localizer.Token(1417), ResourceManager.TextureDict["UI/FollowIcon"], (SimpleDelegate)null);
                this.shipMenu.Add(newChild6);
                if (this.SelectedShip != null && this.SelectedShip.Role != "station" && this.SelectedShip.Role != "platform")
                {
                    PieMenuNode newChild2 = new PieMenuNode(Localizer.Token(1418), ResourceManager.TextureDict["UI/FollowIcon"], new SimpleDelegate(this.RefitTo));
                    newChild6.Add(newChild2);
                }
                if (this.SelectedShip != null && (this.SelectedShip.Role == "station" || this.SelectedShip.Role == "platform"))
                {
                    PieMenuNode newChild2 = new PieMenuNode("Scuttle", ResourceManager.TextureDict["UI/HoldPositionIcon"], new SimpleDelegate(this.OrderScuttle));
                    newChild6.Add(newChild2);
                }
                else
                {
                    if (this.SelectedShip == null || !(this.SelectedShip.Role != "station") || (!(this.SelectedShip.Role != "platform") || !(this.SelectedShip.Role != "construction")))
                        return;
                    PieMenuNode newChild2 = new PieMenuNode(Localizer.Token(1419), ResourceManager.TextureDict["UI/HoldPositionIcon"], new SimpleDelegate(this.OrderScrap));
                    newChild6.Add(newChild2);
                }
            }
            else
                this.shipMenu.Add(new PieMenuNode(Localizer.Token(1420), ResourceManager.TextureDict["UI/viewPlanetIcon"], new SimpleDelegate(this.ContactLeader)));
        }

        public void ContactLeader(object sender)
        {
            if (this.SelectedShip == null)
                return;
            if (this.SelectedShip.loyalty.isFaction)
            {
                foreach (Encounter e in ResourceManager.Encounters)
                {
                    if (this.SelectedShip.loyalty.data.Traits.Name == e.Faction && this.player.GetRelations()[this.SelectedShip.loyalty].EncounterStep == e.Step)
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
            this.SelectedShip.GetAI().OrderTransportPassengers();
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
            this.SelectedShip.GetAI().OrderTrade();
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
                if (this.SelectedShip.loyalty != this.player || this.SelectedShip.Role == "construction")
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
            this.snappingToShip = true;
            this.HeightOnSnap = this.camHeight;
            this.transitionDestination.Z = 3500f;
            this.AdjustCamTimer = 1.5f;
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
                foreach (Mole mole in (List<Mole>)this.player.data.MoleList)
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
                this.SelectedShip = (Ship)null;
                this.SelectedShipList.Clear();
                this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
            }
        }

        public void SnapViewSystem(SolarSystem system)
        {
            this.transitionDestination = new Vector3(system.Position.X, system.Position.Y + 400f, 80000f);
            this.transitionStartPosition = this.camPos;
            this.AdjustCamTimer = 2f;
            this.transitionElapsedTime = 0.0f;
            this.transDuration = 5f;
            this.ViewingShip = false;
            this.snappingToShip = false;
        }

        public void SnapViewPlanet(object sender)
        {
            this.ShowShipNames = false;
            if (this.SelectedPlanet == null)
                return;
            this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y + 400f, 2500f);
            if (!this.SelectedPlanet.system.ExploredDict[this.player])
            {
                this.PlayNegativeSound();
            }
            else
            {
                bool flag = false;
                foreach (Mole mole in (List<Mole>)this.player.data.MoleList)
                {
                    if (mole.PlanetGuid == this.SelectedPlanet.guid)
                        flag = true;
                }
                if (this.SelectedPlanet.Owner == this.player || flag || this.Debug && this.SelectedPlanet.Owner != null)
                    this.workersPanel = (PlanetScreen)new ColonyScreen(this.SelectedPlanet, this.ScreenManager, this.EmpireUI);
                else if (this.SelectedPlanet.Owner != null)
                {
                    this.workersPanel = (PlanetScreen)new UnownedPlanetScreen(this.SelectedPlanet, this.ScreenManager);
                    this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y + 400f, 95000f);
                }
                else
                {
                    this.workersPanel = (PlanetScreen)new UnexploredPlanetScreen(this.SelectedPlanet, this.ScreenManager);
                    this.transitionDestination = new Vector3(this.SelectedPlanet.Position.X, this.SelectedPlanet.Position.Y + 400f, 95000f);
                }
                this.SelectedPlanet.ExploredDict[this.player] = true;
                this.LookingAtPlanet = true;
                this.transitionStartPosition = this.camPos;
                this.AdjustCamTimer = 2f;
                this.transitionElapsedTime = 0.0f;
                this.transDuration = 5f;
                if (this.ViewingShip)
                    this.returnToShip = true;
                this.ViewingShip = false;
                this.snappingToShip = false;
                this.SelectedFleet = (Fleet)null;
                this.SelectedShip = (Ship)null;
                this.SelectedShipList.Clear();
                this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
            }
        }

        protected void MarkForColonization(object sender)
        {
            this.player.GetGSAI().Goals.Add(new Goal(this.SelectedPlanet, this.player));
        }

        protected void ViewSystem(SolarSystem system)
        {
            this.ViewingShip = false;
            this.transitionDestination = new Vector3(system.Position, 147000f);
            this.AdjustCamTimer = 1f;
            this.transDuration = 3f;
            this.transitionElapsedTime = 0.0f;
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            Random random = new Random();
            Vector2 vector2 = new Vector2(this.Size.X / 2f, this.Size.Y / 2f);
            float num1 = (float)((double)(6f / (float)numOfStars) * 2.0 * 3.14159274101257);
            for (int index = 120; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow((double)this.Size.X - 0.100000001490116 * (double)this.Size.X, (double)((float)index / (float)numOfStars));
                float num3 = (float)index * num1 + rotation;
                float x = vector2.X + (float)Math.Cos((double)num3) * num2;
                float y = vector2.Y + (float)Math.Sin((double)num3) * num2;
                NebulousOverlay nebulousOverlay = new NebulousOverlay();
                float z = RandomMath.RandomBetween(50000f, 450000f) - num2;
                if ((double)z < 0.0)
                    z = RandomMath.RandomBetween(25000f, 50000f);
                if ((double)RandomMath.RandomBetween(0.0f, 100f) > 50.0)
                    z *= -1f;
                double num4 = (double)RandomMath.RandomBetween(0.0f, 100f);
                nebulousOverlay.Path = "Textures/smoke";
                nebulousOverlay.Position = new Vector3(x, y, z);
                float radians = RandomMath.RandomBetween(0.0f, 6.283185f);
                nebulousOverlay.Scale = RandomMath.RandomBetween(10f, 100f);
                nebulousOverlay.WorldMatrix = Matrix.CreateScale(50f) * Matrix.CreateScale(nebulousOverlay.Scale) * Matrix.CreateRotationZ(radians) * Matrix.CreateTranslation(nebulousOverlay.Position);
                this.NebulousShit.Add(nebulousOverlay);
            }
            for (int index = 135; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow((double)this.Size.X - 0.100000001490116 * (double)this.Size.X, (double)((float)index / (float)numOfStars));
                float num3 = (float)index * num1 + rotation;
                float x = vector2.X + (float)Math.Cos((double)num3) * num2 + RandomMath.RandomBetween(-200000f, 200000f);
                float y = vector2.Y + (float)Math.Sin((double)num3) * num2 + RandomMath.RandomBetween(-100000f, 100000f);
                NebulousOverlay nebulousOverlay = new NebulousOverlay();
                float z = RandomMath.RandomBetween(250000f, 800000f) - num2;
                if ((double)z < 50000.0)
                    z = RandomMath.RandomBetween(100000f, 400000f);
                if ((double)RandomMath.RandomBetween(0.0f, 100f) > 50.0)
                    z *= -1f;
                double num4 = (double)RandomMath.RandomBetween(0.0f, 100f);
                nebulousOverlay.Path = "Textures/smoke";
                nebulousOverlay.Position = new Vector3(x, y, z);
                float radians = RandomMath.RandomBetween(0.0f, 6.283185f);
                nebulousOverlay.Scale = RandomMath.RandomBetween(100f, 200f);
                nebulousOverlay.WorldMatrix = Matrix.CreateScale(50f) * Matrix.CreateScale(nebulousOverlay.Scale) * Matrix.CreateRotationZ(radians) * Matrix.CreateTranslation(nebulousOverlay.Position);
                this.NebulousShit.Add(nebulousOverlay);
            }
            for (int index1 = 0; index1 < 5; ++index1)
            {
                for (int index2 = 130; index2 < numOfStars; ++index2)
                {
                    float num2 = (float)Math.Pow((double)this.Size.X - 0.100000001490116 * (double)this.Size.X, (double)((float)index2 / (float)numOfStars));
                    float num3 = (float)index2 * num1 + rotation;
                    float x = vector2.X + (float)Math.Cos((double)num3) * num2 + RandomMath.RandomBetween(-200000f, 200000f);
                    float y = vector2.Y + (float)Math.Sin((double)num3) * num2 + RandomMath.RandomBetween(-100000f, 100000f);
                    NebulousOverlay nebulousOverlay = new NebulousOverlay();
                    float z = RandomMath.RandomBetween(250000f, 800000f) - num2;
                    if ((double)z < 50000.0)
                        z = RandomMath.RandomBetween(50000f, 200000f);
                    if ((double)RandomMath.RandomBetween(0.0f, 100f) > 50.0)
                        z *= -1f;
                    nebulousOverlay.Path = "Textures/smoke";
                    nebulousOverlay.Position = new Vector3(x, y, z);
                    float radians = RandomMath.RandomBetween(0.0f, 6.283185f);
                    nebulousOverlay.Scale = RandomMath.RandomBetween(10f, 50f);
                    nebulousOverlay.WorldMatrix = Matrix.CreateScale(50f) * Matrix.CreateScale(nebulousOverlay.Scale) * Matrix.CreateRotationZ(radians) * Matrix.CreateTranslation(nebulousOverlay.Position);
                    this.NebulousShit.Add(nebulousOverlay);
                }
            }
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
            UniverseScreen.DeepSpaceManager.Setup((int)this.Size.X, (int)this.Size.Y, (int)(500000.0 * (double)this.GameScale), new Vector2(this.Size.X / 2f, this.Size.Y / 2f));
            UniverseScreen.ShipSpatialManager.Setup((int)this.Size.X, (int)this.Size.Y, (int)(500000.0 * (double)this.GameScale), new Vector2(this.Size.X / 2f, this.Size.Y / 2f));
            this.DoParticleLoad();
            this.bg3d = new Background3D(this);
            this.starfield = new Starfield(Vector2.Zero, this.ScreenManager.GraphicsDevice, this.ScreenManager.Content);
            this.starfield.LoadContent();
            GameplayObject.audioListener = this.listener;
            Weapon.audioListener = this.listener;
            GameplayObject.audioListener = this.listener;
            this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, (float)this.ScreenManager.GraphicsDevice.Viewport.Width / (float)this.ScreenManager.GraphicsDevice.Viewport.Height, 1000f, 3E+07f);
            this.SetLighting(false);
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                foreach (string FleetUID in solarSystem.DefensiveFleets)
                {
                    Fleet defensiveFleetAt = HelperFunctions.CreateDefensiveFleetAt(FleetUID, EmpireManager.GetEmpireByName("The Remnant"), solarSystem.PlanetList[0].Position);
                    MilitaryTask militaryTask = new MilitaryTask();
                    militaryTask.AO = solarSystem.PlanetList[0].Position;
                    militaryTask.AORadius = 120000f;
                    militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                    defensiveFleetAt.Task = militaryTask;
                    defensiveFleetAt.TaskStep = 3;
                    militaryTask.WhichFleet = EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Count + 10;
                    EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Add(EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Count + 10, defensiveFleetAt);
                    EmpireManager.GetEmpireByName("The Remnant").GetGSAI().TaskList.Add(militaryTask);
                    militaryTask.Step = 2;
                }
                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.customRemnantElements)
                {
                    foreach (Planet p in solarSystem.PlanetList)
                    {
                        foreach (string FleetUID in p.PlanetFleets)
                        {
                            Fleet planetFleetAt = HelperFunctions.CreateDefensiveFleetAt(FleetUID, EmpireManager.GetEmpireByName("The Remnant"), p.Position);
                            MilitaryTask militaryTask = new MilitaryTask();
                            militaryTask.AO = solarSystem.PlanetList[0].Position;
                            militaryTask.AORadius = 120000f;
                            militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                            planetFleetAt.Task = militaryTask;
                            planetFleetAt.TaskStep = 3;
                            militaryTask.WhichFleet = EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Count + 10;
                            EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Add(EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Count + 10, planetFleetAt);
                            EmpireManager.GetEmpireByName("The Remnant").GetGSAI().TaskList.Add(militaryTask);
                            militaryTask.Step = 2;
                        }
                    }
                }

                foreach (SolarSystem.FleetAndPos fleetAndPos in solarSystem.FleetsToSpawn)
                {
                    Fleet defensiveFleetAt = HelperFunctions.CreateDefensiveFleetAt(fleetAndPos.fleetname, EmpireManager.GetEmpireByName("The Remnant"), solarSystem.Position + fleetAndPos.Pos);
                    MilitaryTask militaryTask = new MilitaryTask();
                    militaryTask.AO = solarSystem.Position + fleetAndPos.Pos;
                    militaryTask.AORadius = 75000f;
                    militaryTask.type = MilitaryTask.TaskType.DefendSystem;
                    defensiveFleetAt.Task = militaryTask;
                    defensiveFleetAt.TaskStep = 3;
                    militaryTask.WhichFleet = EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Count + 10;
                    EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Add(EmpireManager.GetEmpireByName("The Remnant").GetFleetsDict().Count + 10, defensiveFleetAt);
                    EmpireManager.GetEmpireByName("The Remnant").GetGSAI().TaskList.Add(militaryTask);
                    militaryTask.Step = 2;
                }
                foreach (string key in solarSystem.ShipsToSpawn)
                    ResourceManager.CreateShipAt(key, EmpireManager.GetEmpireByName("The Remnant"), solarSystem.PlanetList[0], true);
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
                        if (GlobalStats.ActiveMod != null && ResourceManager.HostileFleets.Fleets.Count > 0)
                        {
                            if (p.Guardians.Count > 0)
                            {
                                int randomFleet = HelperFunctions.GetRandomIndex(ResourceManager.HostileFleets.Fleets.Count);
                                foreach (string ship in ResourceManager.HostileFleets.Fleets[randomFleet].Ships)
                                {
                                    ResourceManager.CreateShipAt(ship, EmpireManager.GetEmpireByName(ResourceManager.HostileFleets.Fleets[randomFleet].Empire), p, true);
                                }
                            }
                        }
                        else
                        {
                            foreach (string key in p.Guardians)
                                ResourceManager.CreateShipAt(key, EmpireManager.GetEmpireByName("The Remnant"), p, true);
                            if (p.CorsairPresence)
                            {
                                ResourceManager.CreateShipAt("Corsair Asteroid Base", EmpireManager.GetEmpireByName("Corsairs"), p, true).TetherToPlanet(p);
                                ResourceManager.CreateShipAt("Corsair", EmpireManager.GetEmpireByName("Corsairs"), p, true);
                                ResourceManager.CreateShipAt("Captured Gunship", EmpireManager.GetEmpireByName("Corsairs"), p, true);
                                ResourceManager.CreateShipAt("Captured Gunship", EmpireManager.GetEmpireByName("Corsairs"), p, true);
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
                Matrix view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f)) * Matrix.CreateRotationX(MathHelper.ToRadians(0.0f)) * Matrix.CreateLookAt(new Vector3(-vector2_1.X, vector2_1.Y, this.MaxCamHeight), new Vector3(-vector2_1.X, vector2_1.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
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
            this.transitionDestination = new Vector3(this.camPos.X, this.camPos.Y, this.camHeight);
            foreach (NebulousOverlay nebulousOverlay in this.Stars)
                this.star_particles.AddParticleThreadA(nebulousOverlay.Position, Vector3.Zero);
            if (this.MultiThread)
            {
                this.WorkerThread = new Thread(new ThreadStart(this.Worker));
                this.WorkerThread.IsBackground = true;
                this.WorkerThread.Start();
                this.ShipUpdateThread = new Thread(new ThreadStart(this.ShipUpdater));
                this.ShipUpdateThread.IsBackground = true;
                this.SystemResetEvents = new ManualResetEvent[4];
                this.SystemGateKeeper = new AutoResetEvent[4];
                List<SolarSystem> list1 = new List<SolarSystem>();
                List<SolarSystem> list2 = new List<SolarSystem>();
                List<SolarSystem> list3 = new List<SolarSystem>();
                List<SolarSystem> list4 = new List<SolarSystem>();
                Rectangle rect1 = new Rectangle(0, 0, (int)this.Size.X / 2, (int)this.Size.Y / 2);
                Rectangle rect2 = new Rectangle((int)this.Size.X / 2, 0, (int)this.Size.X / 2, (int)this.Size.Y / 2);
                Rectangle rect3 = new Rectangle(0, (int)this.Size.Y / 2, (int)this.Size.X / 2, (int)this.Size.Y / 2);
                Rectangle rect4 = new Rectangle((int)this.Size.X / 2, (int)this.Size.Y / 2, (int)this.Size.X / 2, (int)this.Size.Y / 2);
                this.SystemResetEvents[0] = new ManualResetEvent(false);
                this.SystemGateKeeper[0] = new AutoResetEvent(false);
                this.SystemResetEvents[1] = new ManualResetEvent(false);
                this.SystemGateKeeper[1] = new AutoResetEvent(false);
                this.SystemResetEvents[2] = new ManualResetEvent(false);
                this.SystemGateKeeper[2] = new AutoResetEvent(false);
                this.SystemResetEvents[3] = new ManualResetEvent(false);
                this.SystemGateKeeper[3] = new AutoResetEvent(false);
                for (int index = 0; index < UniverseScreen.SolarSystemList.Count; ++index)
                {
                    if (HelperFunctions.CheckIntersection(rect1, UniverseScreen.SolarSystemList[index].Position))
                    {
                        UniverseScreen.SolarSystemList[index].IndexOfResetEvent = 0;
                        list1.Add(UniverseScreen.SolarSystemList[index]);
                    }
                    if (HelperFunctions.CheckIntersection(rect2, UniverseScreen.SolarSystemList[index].Position))
                    {
                        UniverseScreen.SolarSystemList[index].IndexOfResetEvent = 1;
                        list2.Add(UniverseScreen.SolarSystemList[index]);
                    }
                    if (HelperFunctions.CheckIntersection(rect3, UniverseScreen.SolarSystemList[index].Position))
                    {
                        UniverseScreen.SolarSystemList[index].IndexOfResetEvent = 2;
                        list3.Add(UniverseScreen.SolarSystemList[index]);
                    }
                    if (HelperFunctions.CheckIntersection(rect4, UniverseScreen.SolarSystemList[index].Position))
                    {
                        UniverseScreen.SolarSystemList[index].IndexOfResetEvent = 3;
                        list4.Add(UniverseScreen.SolarSystemList[index]);
                    }
                }
                Thread thread1 = new Thread(new ParameterizedThreadStart(this.SystemUpdater));
                this.SystemUpdateThreadList.Add(thread1);
                thread1.Start((object)list1);
                thread1.IsBackground = true;
                Thread thread2 = new Thread(new ParameterizedThreadStart(this.SystemUpdater));
                this.SystemUpdateThreadList.Add(thread2);
                thread2.Start((object)list2);
                thread2.IsBackground = true;
                Thread thread3 = new Thread(new ParameterizedThreadStart(this.SystemUpdater));
                this.SystemUpdateThreadList.Add(thread3);
                thread3.Start((object)list3);
                thread3.IsBackground = true;
                Thread thread4 = new Thread(new ParameterizedThreadStart(this.SystemUpdater));
                this.SystemUpdateThreadList.Add(thread4);
                thread4.Start((object)list4);
                thread4.IsBackground = true;
                Thread thread5 = new Thread(new ThreadStart(this.DeepSpaceThread));
                this.SystemUpdateThreadList.Add(thread5);
                thread5.Start();
                thread5.IsBackground = true;
                Thread thread6 = new Thread(new ThreadStart(this.EmpireThread));
                this.SystemUpdateThreadList.Add(thread6);
                thread6.Start();
                thread6.IsBackground = true;
                
            }
            this.PlanetsDict.Clear();
            this.SolarSystemDict.Clear();
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                this.SolarSystemDict.Add(solarSystem.guid, solarSystem);
                foreach (Planet planet in solarSystem.PlanetList)
                    this.PlanetsDict.Add(planet.guid, planet);
            }
            foreach (Ship ship in (List<Ship>)this.MasterShipList)
            {
                if (ship.TetherGuid != Guid.Empty)
                    ship.TetherToPlanet(this.PlanetsDict[ship.TetherGuid]);
            }
        }
        private void EmpireThread()
        {

            
            while (true)
            {
                //float elapsedTime = this.ztimeSnapShot; 
                 

                this.EmpireGateKeeper.WaitOne();
                //float elapsedTime = this.ztimeSnapShot;
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;
                if (!this.Paused)
                {
                    //this.ztimeSnapShot = elapsedTime;
                    //elapsedTime += !this.Paused ? 0.01666667f : 0.0f;
                    //float elapsedTime = this.ztimeSnapShot;
                   // Parallel.ForEach(EmpireManager.EmpireList, empire =>
                    foreach (Empire empire in EmpireManager.EmpireList)
                    {
                        try
                        {
                            //var fleetdictClone = new Dictionary<int,Fleet>(empire.GetFleetsDict());
                            foreach (KeyValuePair<int, Fleet> keyValuePair in empire.GetFleetsDict())//leetdictClone)
                            {
                                if (keyValuePair.Value.Ships.Count > 0)
                                {
                                    keyValuePair.Value.Setavgtodestination();
                                    keyValuePair.Value.SetSpeed();
                                    try
                                    {
                                        keyValuePair.Value.StoredFleetPosistion = keyValuePair.Value.findAveragePositionset();
                                    }
                                    catch
                                    {
                                        System.Diagnostics.Debug.WriteLine("crash at find average posisiton");
                                    }



                                }
                            }
                        }
                        catch { };
                        //empire.updateContactsTimer -= elapsedTime;
                        //if ((double)empire.updateContactsTimer <= 0.0 && !empire.data.Defeated)
                        //{
                        //    empire.GetGSAI().ThreatMatrix.ScrubMatrix();
                        //    empire.ResetBorders();
                        //    lock (GlobalStats.KnownShipsLock)
                        //        empire.KnownShips.Clear();
                        //    empire.UpdateKnownShips();
                        //    empire.updateContactsTimer = RandomMath.RandomBetween(2f, 3.5f);
                        //}
                        //catch { }
                    }//);

                    //for (int index = 0; index < EmpireManager.EmpireList.Count; ++index)
                    //    EmpireManager.EmpireList[index].Update(elapsedTime);
                }
                    this.EmpireDone.Set();
                
            }

        }
        private void DoParticleLoad()
        {
            this.beamflashes = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/BeamFlash", this.ScreenManager.GraphicsDevice);
            this.beamflashes.LoadContent();
            this.explosionParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/ExplosionSettings", this.ScreenManager.GraphicsDevice);
            this.explosionParticles.LoadContent();
            this.photonExplosionParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/PhotonExplosionSettings", this.ScreenManager.GraphicsDevice);
            this.photonExplosionParticles.LoadContent();
            this.explosionSmokeParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/ExplosionSmokeSettings", this.ScreenManager.GraphicsDevice);
            this.explosionSmokeParticles.LoadContent();
            this.projectileTrailParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/ProjectileTrailSettings", this.ScreenManager.GraphicsDevice);
            this.projectileTrailParticles.LoadContent();
            this.fireTrailParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/FireTrailSettings", this.ScreenManager.GraphicsDevice);
            this.fireTrailParticles.LoadContent();
            this.smokePlumeParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/SmokePlumeSettings", this.ScreenManager.GraphicsDevice);
            this.smokePlumeParticles.LoadContent();
            this.fireParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/FireSettings", this.ScreenManager.GraphicsDevice);
            this.fireParticles.LoadContent();
            this.engineTrailParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/EngineTrailSettings", this.ScreenManager.GraphicsDevice);
            this.engineTrailParticles.LoadContent();
            this.flameParticles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/FlameSettings", this.ScreenManager.GraphicsDevice);
            this.flameParticles.LoadContent();
            this.sparks = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/sparks", this.ScreenManager.GraphicsDevice);
            this.sparks.LoadContent();
            this.lightning = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/lightning", this.ScreenManager.GraphicsDevice);
            this.lightning.LoadContent();
            this.flash = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/FlashSettings", this.ScreenManager.GraphicsDevice);
            this.flash.LoadContent();
            this.star_particles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/star_particles", this.ScreenManager.GraphicsDevice);
            this.star_particles.LoadContent();
            this.neb_particles = new ParticleSystem((Game)Game1.Instance, this.ScreenManager.Content, "3DParticles/GalaxyParticle", this.ScreenManager.GraphicsDevice);
            this.neb_particles.LoadContent();
        }

        public void LoadGraphics()
        {
            this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, (float)this.ScreenManager.GraphicsDevice.Viewport.Width / (float)this.ScreenManager.GraphicsDevice.Viewport.Height, 1000f, 3E+07f);
            this.Frustum = new BoundingFrustum(this.view * this.projection);
            this.mmHousing = new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 276, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 256, 276, 256);
            this.MinimapDisplayRect = new Rectangle(this.mmHousing.X + 61, this.mmHousing.Y + 43, 200, 200);
            this.minimap = new MiniMap(this.mmHousing);
            this.mmButtons = new MinimapButtons(this.mmHousing, this.EmpireUI);
            this.mmShowBorders = new Rectangle(this.MinimapDisplayRect.X, this.MinimapDisplayRect.Y - 25, 32, 32);
            this.mmDSBW = new Rectangle(this.mmShowBorders.X + 32, this.mmShowBorders.Y, 64, 32);
            this.mmAutomation = new Rectangle(this.mmDSBW.X + this.mmDSBW.Width, this.mmShowBorders.Y, 96, 32);
            this.mmShipView = new Rectangle(this.MinimapDisplayRect.X - 32, this.MinimapDisplayRect.Y, 32, 105);
            this.mmGalaxyView = new Rectangle(this.mmShipView.X, this.mmShipView.Y + 105, 32, 105);
            this.SectorMap = new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 300, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 150, 150, 150);
            this.GalaxyMap = new Rectangle(this.SectorMap.X + this.SectorMap.Width, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 150, 150, 150);
            this.SelectedStuffRect = new Rectangle(0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 247, 407, 242);
            this.ShipInfoUIElement = new ShipInfoUIElement(this.SelectedStuffRect, this.ScreenManager, this);
            this.sInfoUI = new SystemInfoUIElement(this.SelectedStuffRect, this.ScreenManager, this);
            this.pInfoUI = new PlanetInfoUIElement(this.SelectedStuffRect, this.ScreenManager, this);
            this.shipListInfoUI = new ShipListInfoUIElement(this.SelectedStuffRect, this.ScreenManager, this);
            this.vuiElement = new VariableUIElement(this.SelectedStuffRect, this.ScreenManager, this);
            this.SectorSourceRect = new Rectangle((this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 720) / 2, (this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 720) / 2, 720, 720);
            this.EmpireUI = new EmpireUIOverlay(this.player, this.ScreenManager.GraphicsDevice, this);
            this.bloomComponent = new BloomComponent(this.ScreenManager);
            this.bloomComponent.LoadContent();
            this.aw = new AutomationWindow(this.ScreenManager, this);
            PresentationParameters presentationParameters = this.ScreenManager.GraphicsDevice.PresentationParameters;
            int backBufferWidth = presentationParameters.BackBufferWidth;
            int backBufferHeight = presentationParameters.BackBufferHeight;
            SurfaceFormat backBufferFormat = presentationParameters.BackBufferFormat;
            this.sceneMap = new ResolveTexture2D(this.ScreenManager.GraphicsDevice, backBufferWidth, backBufferHeight, 1, backBufferFormat);
            this.MainTarget = BloomComponent.CreateRenderTarget(this.ScreenManager.GraphicsDevice, 1, backBufferFormat);
            this.LightsTarget = BloomComponent.CreateRenderTarget(this.ScreenManager.GraphicsDevice, 1, backBufferFormat);
            this.MiniMapSector = BloomComponent.CreateRenderTarget(this.ScreenManager.GraphicsDevice, 1, backBufferFormat);
            this.BorderRT = BloomComponent.CreateRenderTarget(this.ScreenManager.GraphicsDevice, 1, backBufferFormat);
            this.StencilRT = BloomComponent.CreateRenderTarget(this.ScreenManager.GraphicsDevice, 1, backBufferFormat);
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (this.loadFogPath == null)
            {
                this.FogMap = ResourceManager.TextureDict["Textures/UniverseFeather"];
            }
            else
            {
                using (FileStream fileStream = File.OpenRead(folderPath + "/StarDrive/Saved Games/Fog Maps/" + this.loadFogPath + ".png"))
                    this.FogMap = Texture2D.FromFile(this.ScreenManager.GraphicsDevice, (Stream)fileStream);
            }
            this.FogMapTarget = new RenderTarget2D(this.ScreenManager.GraphicsDevice, 512, 512, 1, backBufferFormat, this.ScreenManager.GraphicsDevice.PresentationParameters.MultiSampleType, presentationParameters.MultiSampleQuality);
            this.basicFogOfWarEffect = this.ScreenManager.Content.Load<Effect>("Effects/BasicFogOfWar");
            this.LoadMenu();
            MuzzleFlashManager.universeScreen = this;
            DroneAI.universeScreen = this;
            MuzzleFlashManager.flashModel = this.ScreenManager.Content.Load<Model>("Model/Projectiles/muzzleEnergy");
            MuzzleFlashManager.FlashTexture = this.ScreenManager.Content.Load<Texture2D>("Model/Projectiles/Textures/MuzzleFlash_01");
            ExplosionManager.universeScreen = this;
            FTLManager.universeScreen = this;
            FTLManager.FTLTexture = this.ScreenManager.Content.Load<Texture2D>("Textures/Ships/FTL");
            this.anomalyManager = new AnomalyManager();
            ShipDesignScreen.screen = this;
            Fleet.screen = this;
            Bomb.screen = this;
            Anomaly.screen = this;
            PlanetScreen.screen = this;
            MinimapButtons.screen = this;
            Projectile.contentManager = this.ScreenManager.Content;
            Projectile.universeScreen = this;
            ShipModule.universeScreen = this;
            Asteroid.universeScreen = this;
            Empire.universeScreen = this;
            SpaceJunk.universeScreen = this;
            ResourceManager.universeScreen = this;
            Planet.universeScreen = this;
            Weapon.universeScreen = this;
            Ship.universeScreen = this;
            ArtificialIntelligence.universeScreen = this;
            MissileAI.universeScreen = this;
            Moon.universeScreen = this;
            CombatScreen.universeScreen = this;
            FleetDesignScreen.screen = this;
            this.xnaPlanetModel = this.ScreenManager.Content.Load<Model>("Model/SpaceObjects/planet");
            this.atmoModel = this.ScreenManager.Content.Load<Model>("Model/sphere");
            this.AtmoEffect = this.ScreenManager.Content.Load<Effect>("Effects/PlanetHalo");
            this.cloudTex = this.ScreenManager.Content.Load<Texture2D>("Model/SpaceObjects/earthcloudmap");
            this.RingTexture = this.ScreenManager.Content.Load<Texture2D>("Model/SpaceObjects/planet_rings");
            this.ThrusterEffect = this.ScreenManager.Content.Load<Effect>("Effects/Thrust");
            this.listener = new AudioListener();
            this.SunModel = this.ScreenManager.Content.Load<Model>("Model/SpaceObjects/star_plane");
            this.NebModel = this.ScreenManager.Content.Load<Model>("Model/SpaceObjects/star_plane");
            this.ScreenRectangle = new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            this.starfield = new Starfield(Vector2.Zero, this.ScreenManager.GraphicsDevice, this.ScreenManager.Content);
            this.starfield.LoadContent();
        }

        protected void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        }

        public override void UnloadContent()
        {
            if (this.starfield != null)
                this.starfield.UnloadContent();
            this.ScreenManager.inter.Unload();
            this.ScreenManager.lightingSystemManager.Unload();
            base.UnloadContent();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (this.Debug)
                this.FogOn = false;
            if (this.viewState > UniverseScreen.UnivScreenState.ShipView)
            {
                foreach (NebulousOverlay nebulousOverlay in this.NebulousShit)
                    this.engineTrailParticles.AddParticleThreadA(nebulousOverlay.Position, Vector3.Zero);
            }
            this.zgameTime = gameTime;
            float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
            this.SelectedSomethingTimer -= num;
            if (this.SelectorFrame < 299)
                ++this.SelectorFrame;
            else
                this.SelectorFrame = 0;
            this.MusicCheckTimer -= num;
            if ((double)this.MusicCheckTimer <= 0.0)
            {
                if (this.ScreenManager.Music == null || this.ScreenManager.Music != null && this.ScreenManager.Music.IsStopped)
                {
                    this.ScreenManager.Music = AudioManager.GetCue("AmbientMusic");
                    this.ScreenManager.Music.Play();
                }
                this.MusicCheckTimer = 2f;
            }
            AudioManager.getAudioEngine().Update();
            this.listener.Position = new Vector3(this.camPos.X, this.camPos.Y, 0.0f);
            lock (GlobalStats.ObjectManagerLocker)
                this.ScreenManager.inter.Update(gameTime);
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            this.EmpireUI.Update(elapsedTime);
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
            this.zTime += elapsedTime;
        }

        protected virtual void Worker()
        {
            while (true)
            {
                this.WorkerBeginEvent.WaitOne();
                if (this.Paused)
                {
                    this.UpdateAllSystems(0.0f);
                    for (int index = 0; index < this.MasterShipList.Count; ++index)
                    {
                        try
                        {
                            Ship ship = this.MasterShipList[index];
                            if (this.Frustum.Contains(new BoundingSphere(new Vector3(ship.Position, 0.0f), 2000f)) != ContainmentType.Disjoint && this.viewState <= UniverseScreen.UnivScreenState.SystemView)
                            {
                                ship.InFrustum = true;
                                ship.GetSO().Visibility = ObjectVisibility.Rendered;
                                ship.GetSO().World = Matrix.Identity * Matrix.CreateRotationY(ship.yRotation) * Matrix.CreateRotationX(ship.xRotation) * Matrix.CreateRotationZ(ship.Rotation) * Matrix.CreateTranslation(new Vector3(ship.Center, 0.0f));
                            }
                            else
                            {
                                ship.InFrustum = false;
                                ship.GetSO().Visibility = ObjectVisibility.None;
                            }
                        }
                        catch
                        {
                        }
                    }
                    this.ClickTimer += (float)this.zgameTime.ElapsedGameTime.TotalSeconds;
                    this.ClickTimer2 += (float)this.zgameTime.ElapsedGameTime.TotalSeconds;
                    this.pieMenu.Update(this.zgameTime);
                    this.PieMenuTimer += (float)this.zgameTime.ElapsedGameTime.TotalSeconds;
                    this.WorkerCompletedEvent.Set();
                }
                else
                {
                    this.ClickTimer += (float)this.zgameTime.ElapsedGameTime.TotalSeconds;
                    this.ClickTimer2 += (float)this.zgameTime.ElapsedGameTime.TotalSeconds;
                    this.pieMenu.Update(this.zgameTime);
                    this.PieMenuTimer += (float)this.zgameTime.ElapsedGameTime.TotalSeconds;
                    this.NotificationManager.Update((float)this.zgameTime.ElapsedGameTime.TotalSeconds);
                    this.AutoSaveTimer -= 0.01666667f;
                    

                    if (this.AutoSaveTimer <= 0.0f)
                    {
                        this.AutoSaveTimer = GlobalStats.Config.AutoSaveInterval;
                        this.DoAutoSave();
                    }
                    if (this.IsActive)
                    {
                        if (this.Paused)
                            this.DoWork(0.0f);
                        else if ((double)this.GameSpeed < 1.0)
                        {
                            if (this.flip)
                            {
                                this.DoWork((float)this.zgameTime.ElapsedGameTime.TotalSeconds);
                                this.flip = false;
                            }
                            else
                                this.flip = true;
                        }
                        else
                        {
                            for (int index = 0; (double)index < (double)this.GameSpeed; ++index)
                            {
                                if (this.IsActive)
                                    this.DoWork((float)this.zgameTime.ElapsedGameTime.TotalSeconds);
                            }
                        }
                    }
                    this.WorkerCompletedEvent.Set();
                }
            }
        }

        public void DoAutoSave()
        {
            //GC.GetTotalMemory(true);
            GC.Collect();
            SavedGame savedGame = new SavedGame(this, "Autosave " + this.Auto.ToString());
            ++this.Auto;
            if (this.Auto <= 3)
                return;
            this.Auto = 1;
        }

        public void UpdateClickableItems()
        {
            lock (GlobalStats.ClickableItemLocker)
                this.ItemsToBuild.Clear();
            for (int index = 0; index < EmpireManager.GetEmpireByName(this.PlayerLoyalty).GetGSAI().Goals.Count; ++index)
            {
                Goal goal = this.player.GetGSAI().Goals[index];
                if (goal.GoalName == "BuildConstructionShip")
                {
                    float radius = 100f;
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(goal.BuildPosition, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, goal.BuildPosition, radius), 0.0f), this.projection, this.view, Matrix.Identity);
                    float num = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2) + 10f;
                    UniverseScreen.ClickableItemUnderConstruction underConstruction = new UniverseScreen.ClickableItemUnderConstruction();
                    underConstruction.Radius = num;
                    underConstruction.BuildPos = goal.BuildPosition;
                    underConstruction.ScreenPos = vector2;
                    underConstruction.UID = goal.ToBuildUID;
                    underConstruction.AssociatedGoal = goal;
                    lock (GlobalStats.ClickableItemLocker)
                        this.ItemsToBuild.Add(underConstruction);
                }
            }
        }

        private void BreakGame()
        {
            while (true)
            {
                double num = (double)RandomMath.RandomBetween(0.0f, 10f);
            }
        }

        private void BreakGame2()
        {
            while (true)
            {
                double num = (double)RandomMath2.RandomBetween(0.0f, 10f);
            }
        }

        protected virtual void DoWork(float elapsedTime)
        {

            float beginTime =this.zgameTime.ElapsedGameTime.Seconds;
            if (!this.IsActive)
            {
                this.ShowingSysTooltip = false;
                this.ShowingPlanetToolTip = false;
            }
            this.RecomputeFleetButtons(false);
            if (this.SelectedShip != null)
            {
                try
                {
                    Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                    this.pieMenu.Position = new Vector2(vector3.X, vector3.Y);
                    this.pieMenu.Radius = 75f;
                    this.pieMenu.ScaleFactor = 1f;
                }
                catch
                {
                }
            }
            else if (this.SelectedPlanet != null)
            {
                try
                {
                    Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedPlanet != null ? this.SelectedPlanet.Position : Vector2.Zero, 2500f), this.projection, this.view, Matrix.Identity);
                    this.pieMenu.Position = new Vector2(vector3.X, vector3.Y);
                    this.pieMenu.Radius = 75f;
                    this.pieMenu.ScaleFactor = 1f;
                }
                catch
                {
                }
            }
            if (GlobalStats.RemnantArmageddon)
            {
                if (!this.Paused)
                    this.ArmageddonTimer -= elapsedTime;
                if ((double)this.ArmageddonTimer < 0.0)
                {
                    this.ArmageddonTimer = 300f;
                    ++this.ArmageddonCounter;
                    if (this.ArmageddonCounter > 5)
                        this.ArmageddonCounter = 5;
                    for (int index = 0; index < this.ArmageddonCounter; ++index)
                        ResourceManager.CreateShipAtPoint("Remnant Exterminator", EmpireManager.GetEmpireByName("The Remnant"), this.player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f), RandomMath.RandomBetween(-500000f, 500000f))).GetAI().DefaultAIState = AIState.Exterminate;
                }
            }
            foreach (Ship ship in this.ShipsToRemove)
                ship.TotallyRemove();
            UniverseScreen.DeepSpaceManager.CollidableObjects.ApplyPendingRemovals();
            UniverseScreen.ShipSpatialManager.CollidableObjects.ApplyPendingRemovals();
            
            this.MasterShipList.ApplyPendingRemovals();
            if (!this.IsActive)
                return;
#if DEBUG
            List<Ship> inactive =  this.MasterShipList.Where(active => !active.Active).ToList();
            if(inactive.Count >0)
            System.Diagnostics.Debug.WriteLine(inactive.Count);
            List<GameplayObject> Coinactive = UniverseScreen.DeepSpaceManager.CollidableObjects.Where(active => !active.Active).ToList();
                        if(Coinactive.Count >0)
            System.Diagnostics.Debug.WriteLine(Coinactive.Count);
                
#endif
            //this.EmpireGateKeeper.Set();
            //this.EmpireDone.WaitOne();
            //this.EmpireDone.Reset();
            if (!this.Paused)
            {

                
                for (int index = 0; index < EmpireManager.EmpireList.Count; ++index)
                    EmpireManager.EmpireList[index].Update(elapsedTime);

                //Parallel.For(0, EmpireManager.EmpireList.Count, index =>
                //    {
                //        EmpireManager.EmpireList[index].Update(elapsedTime);
                //    });

                this.MasterShipList.ApplyPendingRemovals();
                
                lock (GlobalStats.AddShipLocker)
                {
                    foreach (Ship item_1 in this.ShipsToAdd)
                    {
                        this.MasterShipList.Add(item_1);
                        UniverseScreen.ShipSpatialManager.CollidableObjects.Add((GameplayObject)item_1);
                    }
                    this.ShipsToAdd.Clear();
                }
                this.ShipsToRemove.Clear();
                this.shiptimer -= elapsedTime; //0.01666667f;//
            }
            if ((double)elapsedTime > 0.0 && (double)this.shiptimer <= 0.0)
            {
                this.shiptimer = 1f;
                foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
                    solarSystem.ShipList.Clear();
                foreach (Ship ship in (List<Ship>)this.MasterShipList)
                {
                    ship.isInDeepSpace = false;
                    ship.SetSystem((SolarSystem)null);
                    foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                    {
                        if ((double)Vector2.Distance(ship.Position, s.Position) < 100000.0)
                        {
                            s.ExploredDict[ship.loyalty] = true;
                            ship.SetSystem(s);
                            s.ShipList.Add(ship);
                            if (!s.spatialManager.CollidableObjects.Contains((GameplayObject)ship))
                                s.spatialManager.CollidableObjects.Add((GameplayObject)ship);
                            if (!s.spatialManager.CollidableObjects.Contains((GameplayObject)ship))
                            {
                                Exception ex = new Exception();
                                ex.Source = "ship not in SM CO";
                                System.Diagnostics.Debug.WriteLine(ex.Source);
                            }
                        }
                    }
                    if (ship.GetSystem() == null)
                    {
                        ship.isInDeepSpace = true;
                        if (!UniverseScreen.DeepSpaceManager.CollidableObjects.Contains((GameplayObject)ship))
                            UniverseScreen.DeepSpaceManager.CollidableObjects.Add((GameplayObject)ship);
                        if (!UniverseScreen.DeepSpaceManager.CollidableObjects.Contains((GameplayObject)ship))
                        {
                            {
                                Exception ex = new Exception();
                                ex.Source = "ship not in DS CO";
                                System.Diagnostics.Debug.WriteLine(ex.Source);
                            }
                        }
                    }
                }

            }
            GlobalStats.BeamTests = 0;
            GlobalStats.Comparisons = 0;
            ++GlobalStats.ComparisonCounter;
            GlobalStats.ModuleUpdates = 0;
            GlobalStats.ModulesMoved = 0;


            foreach (Empire empire in EmpireManager.EmpireList)
            {
                try
                {
                    //var fleetdictClone = new Dictionary<int,Fleet>(empire.GetFleetsDict());
                    foreach (KeyValuePair<int, Fleet> keyValuePair in empire.GetFleetsDict())//leetdictClone)
                    {
                        if (keyValuePair.Value.Ships.Count > 0)
                        {
                            keyValuePair.Value.Setavgtodestination();
                            keyValuePair.Value.SetSpeed();
                            try
                            {
                                keyValuePair.Value.StoredFleetPosistion = keyValuePair.Value.findAveragePositionset();
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine("crash at find average posisiton");
                            }



                        }
                    }
                }
                catch { };
            }
            
            this.DeepSpaceGateKeeper.Set();
#if !ALTERTHREAD
            this.SystemGateKeeper[0].Set();
            this.SystemGateKeeper[1].Set();
            this.SystemGateKeeper[2].Set();
            this.SystemGateKeeper[3].Set();  
#endif


#if ALTERTHREAD
            List<SolarSystem> solarsystems = this.SolarSystemDict.Values.ToList();
            var source = Enumerable.Range(0, this.SolarSystemDict.Count).ToArray();
            var rangePartitioner = Partitioner.Create(0, source.Length);

            Parallel.ForEach(rangePartitioner, (range, loopState) =>
                {
                    List<SolarSystem> ss = new List<SolarSystem>();
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        ss.Add(solarsystems[i]);

                    }
                    SystemUpdater2(ss);

                }); 
#endif
            this.DeepSpaceDone.WaitOne();
#if !ALTERTHREAD
            this.SystemResetEvents[0].WaitOne();
            this.SystemResetEvents[1].WaitOne();
            this.SystemResetEvents[2].WaitOne();
            this.SystemResetEvents[3].WaitOne();



            this.SystemResetEvents[0].Reset();
            this.SystemResetEvents[1].Reset();
            this.SystemResetEvents[2].Reset();
            this.SystemResetEvents[3].Reset();  
#endif



             this.DeepSpaceDone.Reset();

             if (this.perfavg.Count > 10)
             {
                 this.perfavg2.Add(this.perfavg.Average());
                 System.Diagnostics.Debug.WriteLine(this.perfavg2.Average());

                 this.perfavg.Clear();
             }
             else
             {

                 this.perfavg.Add(elapsedTime - this.zgameTime.ElapsedGameTime.Seconds);

             }
             if (this.perfavg2.Count > 10)
             {
                 float temp = this.perfavg2.Average();
                 this.perfavg2.Clear();
                 this.perfavg2.Add(temp);
             }
            
                //System.Diagnostics.Debug.WriteLine(this.zgameTime.ElapsedGameTime.Seconds - elapsedTime);
            if ((double)elapsedTime > 0.0)
            {
                this.SpatManUpdate2(elapsedTime);
                UniverseScreen.ShipSpatialManager.UpdateBucketsOnly(elapsedTime);
            }

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
                        if ((double)Vector2.Distance(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y), local_12.ScreenPos) <= (double)local_12.Radius)
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
            if ((double)this.TooltipTimer <= 0.0 && !this.LookingAtPlanet)
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
                        if ((double)Vector2.Distance(new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y), local_16.ScreenPos) <= (double)local_16.Radius)
                        {
                            this.sTooltipTimer -= 0.01666667f;
                            this.tippedSystem = local_16;
                            flag2 = true;
                        }
                    }
                }
                if ((double)this.sTooltipTimer <= 0.0)
                    this.sTooltipTimer = 0.5f;
            }
            if (!flag2)
                this.ShowingSysTooltip = false;
            this.Zrotate += 0.03f * elapsedTime;

            




            UniverseScreen.JunkList.ApplyPendingRemovals();
            if ((double)elapsedTime > 0.0)
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
            foreach (Anomaly anomaly in (List<Anomaly>)this.anomalyManager.AnomaliesList)
                anomaly.Update(elapsedTime);
            if ((double)elapsedTime > 0.0)
            {
                lock (GlobalStats.BombLock)
                {
                    for (int local_19 = 0; local_19 < this.BombList.Count; ++local_19)
                    {
                        Bomb local_20 = this.BombList[local_19];
                        if (local_20 != null)
                            local_20.Update(elapsedTime);
                    }
                    this.BombList.ApplyPendingRemovals();
                }
            }
            this.anomalyManager.AnomaliesList.ApplyPendingRemovals();
            if ((double)elapsedTime > 0.0)
            {
                ShieldManager.Update();
                lock (GlobalStats.ShieldLocker)
                {
                    for (int local_21 = 0; local_21 < ShieldManager.shieldList.Count; ++local_21)
                    {
                        Shield local_22 = ShieldManager.shieldList[local_21];
                        if (local_22.Owner != null && !local_22.Owner.Active)
                        {
                            ShieldManager.shieldList.QueuePendingRemoval(local_22);
                            lock (GlobalStats.ObjectManagerLocker)
                                this.ScreenManager.inter.LightManager.Remove((ILight)local_22.pointLight);
                        }
                    }
                    ShieldManager.shieldList.ApplyPendingRemovals();
                }
                lock (FTLManager.FTLLock)
                {
                    FTLManager.Update(elapsedTime);
                    FTLManager.FTLList.ApplyPendingRemovals();
                }
                for (int index = 0; index < UniverseScreen.JunkList.Count; ++index)
                    UniverseScreen.JunkList[index].Update(elapsedTime);
            }
            this.SelectedShipList.ApplyPendingRemovals();
            this.MasterShipList.ApplyPendingRemovals();
            UniverseScreen.ShipSpatialManager.CollidableObjects.ApplyPendingRemovals();
            if(this.perStarDateTimer<=this.StarDate )
            {
                this.perStarDateTimer = this.StarDate +.1f;
                this.perStarDateTimer = (float)Math.Round((double)this.perStarDateTimer, 1);
                this.empireShipCountReserve = EmpireManager.EmpireList.Where(empire=> empire!=this.player &&!empire.data.Defeated &&!empire.isFaction).Sum(empire => empire.EmpireShipCountReserve);
                this.globalshipCount = this.MasterShipList.Where(ship => (ship.loyalty != null && ship.loyalty != this.player) && ship.Role != "troop" && ship.Mothership == null).Count() ;
            }
            //for (int index = 0; index < EmpireManager.EmpireList.Count; ++index)
            //{
            //    Empire empire;
            //    try
            //    {
            //        empire = EmpireManager.EmpireList[index];
            //    }
            //    catch
            //    {
            //        continue;
            //    }
            //    empire.updateContactsTimer -= elapsedTime;
            //    if ((double)empire.updateContactsTimer <= 0.0 && !empire.data.Defeated)
            //    {
            //        empire.ResetBorders();
            //        lock (GlobalStats.KnownShipsLock)
            //            empire.KnownShips.Clear();
            //        empire.UpdateKnownShips();
            //        empire.updateContactsTimer = RandomMath.RandomBetween(2f, 3.5f);
            //    }
                //catch { }
            //}

            //    for (int index = 0; index < EmpireManager.EmpireList.Count; ++index)
            //        EmpireManager.EmpireList[index].Update(elapsedTime);

  
        }

    



        public void ShipUpdater()
        {
            while (true)
            {
                this.ShipGateKeeper.WaitOne();
                GlobalStats.CombatScans = 0;
                GlobalStats.DSCombatScans = 0;
                GlobalStats.ModulesMoved = 0;
                GlobalStats.WeaponArcChecks = 0;
                


                foreach (Empire empire in EmpireManager.EmpireList)
                {
                    try
                    {
                        foreach (KeyValuePair<int, Fleet> keyValuePair in empire.GetFleetsDict())
                        {
                            if (keyValuePair.Value.Ships.Count > 0)
                            {
                                keyValuePair.Value.Setavgtodestination();


                            }
                        }
                    }
                    catch { }
                }

                for (int i = 0; i < this.MasterShipList.Count; i++)
                {
                    Ship item = this.MasterShipList[i];
                    if (item.Active)
                    {
                        if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                        {
                            item.Inhibited = true;
                            item.InhibitedTimer = 10f;
                        }
                        item.PauseUpdate = true;
                        item.Update(0.0166666675f);
                        if (item.PlayerShip)
                        {
                            item.ProcessInput(0.0166666675f);
                        }
                    }
                    else
                    {
                        this.MasterShipList.QueuePendingRemoval(item);
                    }
                }
                foreach (KeyValuePair<Guid, Planet> planetsDict in this.PlanetsDict)
                {
                    for (int j = 0; j < planetsDict.Value.Projectiles.Count; j++)
                    {
                        Projectile projectile = planetsDict.Value.Projectiles[j];
                        if (!projectile.Active)
                        {
                            planetsDict.Value.Projectiles.QueuePendingRemoval(projectile);
                        }
                        else
                        {
                            projectile.Update(0.0166666675f);
                        }
                    }
                }
            }
        }

        public void SystemUpdater(object data)
        {
            List<SolarSystem> list = (List<SolarSystem>)data;
            while (true)
            {
                this.SystemGateKeeper[list[0].IndexOfResetEvent].WaitOne();
                //float elapsedTime = this.ztimeSnapShot;
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;
                foreach (SolarSystem system in list)
                //Parallel.ForEach(list, system =>
                {
                    system.DangerTimer -= elapsedTime;
                    system.DangerUpdater -= elapsedTime;
                    if ((double)system.DangerUpdater < 0.0)
                    {
                        system.DangerUpdater = 10f;
                        system.DangerTimer = (double)this.player.GetGSAI().ThreatMatrix.PingRadarStr(system.Position, 100000f * UniverseScreen.GameScaleStatic, this.player) <= 0.0 ? 0.0f : 120f;
                    }
                    system.combatTimer -= elapsedTime;


                    if ((double)system.combatTimer <= 0.0)
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
                        system.isVisible = (double)this.camHeight < 250000.0;
                    }
                    if (system.isVisible && this.camHeight < 150000.0)
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.GetSO().Visibility = ObjectVisibility.Rendered;
                            asteroid.Update(elapsedTime);
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.GetSO().Visibility = ObjectVisibility.Rendered;
                            moon.UpdatePosition(elapsedTime);
                        }
                    }
                    else
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.GetSO().Visibility = ObjectVisibility.None;
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.GetSO().Visibility = ObjectVisibility.None;
                        }
                    }
                    foreach (Planet planet in system.PlanetList)
                    {
                        planet.Update(elapsedTime);
                        if (planet.HasShipyard && system.isVisible)
                            planet.Station.Update(elapsedTime);
                    }

                   foreach (Ship ship in (List<Ship>)system.ShipList)
                  // Parallel.ForEach(system.ShipList, ship =>
                    {
                        //try
                        {
                            if (ship.GetSystem() == null)
                                continue;
                                //return;
                            if (!ship.Active)
                            {
                                this.MasterShipList.QueuePendingRemoval(ship);
                            }
                            else
                            {
                                if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                                {
                                    ship.Inhibited = true;
                                    ship.InhibitedTimer = 10f;
                                }
                                //try
                                {
                                    ship.PauseUpdate = true;
                                    ship.Update(elapsedTime);
                                    if (ship.PlayerShip)
                                        ship.ProcessInput(elapsedTime);
                                }
                            //    catch (Exception ex)
                            //    {
                            //        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                            //    }
                            }
                        }
                        //catch
                        {
                        }
                    }//);
                    if (!this.Paused && this.IsActive)
                        system.spatialManager.Update(elapsedTime, system);
                    system.AsteroidsList.ApplyPendingRemovals();
                    system.ShipList.ApplyPendingRemovals();
                }//);
                this.SystemResetEvents[list[0].IndexOfResetEvent].Set();
            }
        }

        public void SystemUpdater2(object data)
        {
            List<SolarSystem> list = (List<SolarSystem>)data;
            // while (true)
            {
                //this.SystemGateKeeper[list[0].IndexOfResetEvent].WaitOne();
                //float elapsedTime = this.ztimeSnapShot;
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;
                float realTime = this.zgameTime.ElapsedGameTime.Seconds;
                foreach (SolarSystem system in list)
                //Parallel.ForEach(list, system =>
                {
                    system.DangerTimer -= realTime;
                    system.DangerUpdater -= realTime;
                    if ((double)system.DangerUpdater < 0.0)
                    {
                        system.DangerUpdater = 10f;
                        system.DangerTimer = (double)this.player.GetGSAI().ThreatMatrix.PingRadarStr(system.Position, 100000f * UniverseScreen.GameScaleStatic, this.player) <= 0.0 ? 0.0f : 120f;
                    }
                    system.combatTimer -= realTime;


                    if ((double)system.combatTimer <= 0.0)
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
                        system.isVisible = (double)this.camHeight < 250000.0;
                    }
                    if (system.isVisible && this.camHeight < 150000.0)
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.GetSO().Visibility = ObjectVisibility.Rendered;
                            asteroid.Update(elapsedTime);
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.GetSO().Visibility = ObjectVisibility.Rendered;
                            moon.UpdatePosition(elapsedTime);
                        }
                    }
                    else
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.GetSO().Visibility = ObjectVisibility.None;
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.GetSO().Visibility = ObjectVisibility.None;
                        }
                    }
                    foreach (Planet planet in system.PlanetList)
                    {
                        planet.Update(elapsedTime);
                        if (planet.HasShipyard && system.isVisible)
                            planet.Station.Update(elapsedTime);
                    }

                    foreach (Ship ship in (List<Ship>)system.ShipList)
                    // Parallel.ForEach(system.ShipList, ship =>
                    {
                        //try
                        {
                            if (ship.GetSystem() == null)
                                continue;
                            //return;
                            if (!ship.Active)
                            {
                                this.MasterShipList.QueuePendingRemoval(ship);
                            }
                            else
                            {
                                if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                                {
                                    ship.Inhibited = true;
                                    ship.InhibitedTimer = 10f;
                                }
                                //try
                                {
                                    ship.PauseUpdate = true;
                                    ship.Update(elapsedTime);
                                    if (ship.PlayerShip)
                                        ship.ProcessInput(elapsedTime);
                                }
                                //    catch (Exception ex)
                                //    {
                                //        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                                //    }
                            }
                        }
                        //catch
                        {
                        }
                    }//);
                    if (!this.Paused && this.IsActive)
                        system.spatialManager.Update(elapsedTime, system);
                    system.AsteroidsList.ApplyPendingRemovals();
                    system.ShipList.ApplyPendingRemovals();
                }//);
                // this.SystemResetEvents[list[0].IndexOfResetEvent].Set();
            }
        }
        protected void SpatManUpdate2(float elapsedTime)
        {
            lock (GlobalStats.DeepSpaceLock)
            {
                foreach (Projectile item_0 in this.DSProjectilesToAdd)
                    UniverseScreen.DeepSpaceManager.CollidableObjects.Add((GameplayObject)item_0);
            }
            this.DSProjectilesToAdd.Clear();
            UniverseScreen.DeepSpaceManager.Update(elapsedTime, (SolarSystem)null);
        }

        private void DeepSpaceThread()
        {
            while (true)
            {
                
                this.DeepSpaceGateKeeper.WaitOne();
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;

                
                
                this.DeepSpaceShips.Clear();

                lock (GlobalStats.DeepSpaceLock)
                {
                    for (int i = 0; i < UniverseScreen.DeepSpaceManager.CollidableObjects.Count; i++)
                    {
                        GameplayObject item = UniverseScreen.DeepSpaceManager.CollidableObjects[i];
                        if (item is Ship)
                        {
                            Ship ship = item as Ship;
                            if (ship.Active && ship.isInDeepSpace && ship.GetSystem() == null)
                            {
                                this.DeepSpaceShips.Add(ship);
                            }

                        }
                    }

                    foreach (Ship deepSpaceShip in this.DeepSpaceShips)
                    //Parallel.ForEach(this.DeepSpaceShips, deepSpaceShip =>
                    {
                        if (deepSpaceShip.Active)
                        {
                            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                            {
                                deepSpaceShip.Inhibited = true;
                                deepSpaceShip.InhibitedTimer = 10f;
                            }
                            //try
                            {
                                deepSpaceShip.PauseUpdate = true;
                                deepSpaceShip.Update(elapsedTime);
                                
                                if (!deepSpaceShip.PlayerShip)
                                {
                                    continue;
                                }
                                deepSpaceShip.ProcessInput(elapsedTime);
                            }
                            //catch (Exception ex)
                            //{
                            //    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                            //}
                        }
                        else
                        {
                            this.MasterShipList.QueuePendingRemoval(deepSpaceShip);
                        }
                    }//);
                }
                
                this.DeepSpaceDone.Set();
            }
        }

        public virtual void UpdateAllSystems(float elapsedTime)
        {
            if (this.IsExiting)
                return;
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                system.DangerTimer -= elapsedTime;
                system.DangerUpdater -= elapsedTime;
                foreach (KeyValuePair<Empire, SolarSystem.PredictionTimeout> predict in system.predictionTimeout)
                {
                    predict.Value.update(elapsedTime);

                }
                if ((double)system.DangerUpdater < 0.0)
                {
                    system.DangerUpdater = 10f;
                    system.DangerTimer = (double)this.player.GetGSAI().ThreatMatrix.PingRadarStr(system.Position, 100000f * UniverseScreen.GameScaleStatic, this.player) <= 0.0 ? 0.0f : 120f;
                }
                system.combatTimer -= elapsedTime;
                if ((double)system.combatTimer <= 0.0)
                    system.CombatInSystem = false;
                if ((double)elapsedTime > 0.0)
                    system.spatialManager.Update(elapsedTime, system);
                bool flag = false;
                this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(system.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                if (this.Frustum.Contains(new BoundingSphere(new Vector3(system.Position, 0.0f), 100000f)) != ContainmentType.Disjoint)
                    flag = true;
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
                        flag = true;
                }
                if (system.ExploredDict[this.player] && flag)
                {
                        system.isVisible = (double)this.camHeight < 250000.0; 
                }
                if (system.isVisible && this.camHeight < 150000.0)
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                    {
                        asteroid.GetSO().Visibility = ObjectVisibility.Rendered;
                        asteroid.Update(elapsedTime);
                    }
                    foreach (Moon moon in system.MoonList)
                    {
                        moon.GetSO().Visibility = ObjectVisibility.Rendered;
                        moon.UpdatePosition(elapsedTime);
                    }
                }
                else
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                    {
                        asteroid.GetSO().Visibility = ObjectVisibility.None;
                    }
                    foreach (Moon moon in system.MoonList)
                    {
                        moon.GetSO().Visibility = ObjectVisibility.None;
                    }
                }
                foreach (Planet planet in system.PlanetList)
                {
                    planet.Update(elapsedTime);
                    if (planet.HasShipyard && system.isVisible)
                        planet.Station.Update(elapsedTime);
                }
                if (system.isVisible && (double)this.camHeight < 150000.0)
                {
                    foreach (GameplayObject gameplayObject in (List<Asteroid>)system.AsteroidsList)
                        gameplayObject.Update(elapsedTime);
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
            if ((double)this.AdjustCamTimer > 0.0)
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
            if ((double)this.camPos.X > (double)this.Size.X)
                this.camPos.X = this.Size.X;
            if ((double)this.camPos.X < 0.0)
                this.camPos.X = 0.0f;
            if ((double)this.camPos.Y > (double)this.Size.Y)
                this.camPos.Y = this.Size.Y;
            if ((double)this.camPos.Y < 0.0)
                this.camPos.Y = 0.0f;
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
                    float num = (float)this.MinimapDisplayRect.Width / this.Size.X;
                    this.transitionDestination.X = vector2.X / num;
                    this.transitionDestination.Y = vector2.Y / num;
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
                    if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
                    {
                        this.DefiningAO = false;
                        return;
                    }
                }
                if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
                {
                    if ((double)this.AORect.X > (double)vector3.X)
                        this.AORect.X = (int)vector3.X;
                    if ((double)this.AORect.Y > (double)vector3.Y)
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
            if (this.ScreenManager.input.CurrentKeyboardState.IsKeyDown(Keys.Space) && this.ScreenManager.input.LastKeyboardState.IsKeyUp(Keys.Space) && !GlobalStats.TakingInput)
                this.Paused = !this.Paused;
            for (int index = 0; index < this.SelectedShipList.Count; ++index)
            {
                Ship ship = this.SelectedShipList[index];
                if (!ship.Active)
                    this.SelectedShipList.QueuePendingRemoval(ship);
            }
            this.input = input;
            this.ShowTacticalCloseup = input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt);
            if (input.CurrentKeyboardState.IsKeyDown(Keys.P) && input.LastKeyboardState.IsKeyUp(Keys.P) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                this.UseRealLights = !this.UseRealLights;
                this.SetLighting(this.UseRealLights);
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.OemTilde) && input.LastKeyboardState.IsKeyUp(Keys.OemTilde) && (input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift)))
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
                if ((double)this.GameSpeed < 1.0)
                    this.GameSpeed = 1f;
                else
                    ++this.GameSpeed;
                if ((double)this.GameSpeed > 4.0 && !GlobalStats.LimitSpeed)
                    this.GameSpeed = 4f;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.OemMinus) && input.LastKeyboardState.IsKeyUp(Keys.OemMinus))
            {
                if ((double)this.GameSpeed <= 1.0)
                    this.GameSpeed = 0.5f;
                else
                    --this.GameSpeed;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Add) && input.LastKeyboardState.IsKeyUp(Keys.Add))
            {
                if ((double)this.GameSpeed < 1.0)
                    this.GameSpeed = 1f;
                else
                    ++this.GameSpeed;
                if ((double)this.GameSpeed > 4.0 && GlobalStats.LimitSpeed)
                    this.GameSpeed = 4f;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Subtract) && input.LastKeyboardState.IsKeyUp(Keys.Subtract))
            {
                if ((double)this.GameSpeed <= 1.0)
                    this.GameSpeed = 0.5f;
                else
                    --this.GameSpeed;
            }
            if (!this.LookingAtPlanet)
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
                this.SelectedShip = (Ship)null;
                this.SelectedShipList.Clear();
                this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
                this.SelectedSystem = (SolarSystem)null;
            }
            if ((input.CurrentKeyboardState.IsKeyDown(Keys.Back) || input.CurrentKeyboardState.IsKeyDown(Keys.Delete)) && (this.SelectedItem != null && this.SelectedItem.AssociatedGoal.empire == this.player))
            {
                this.player.GetGSAI().Goals.QueuePendingRemoval(this.SelectedItem.AssociatedGoal);
                bool flag = false;
                foreach (Ship ship in (List<Ship>)this.player.GetShips())
                {
                    if (ship.Role == "construction" && ship.GetAI().OrderQueue.Count > 0)
                    {
                        for (int index = 0; index < ship.GetAI().OrderQueue.Count; ++index)
                        {
                            if (Enumerable.ElementAt<ArtificialIntelligence.ShipGoal>((IEnumerable<ArtificialIntelligence.ShipGoal>)ship.GetAI().OrderQueue, index).goal == this.SelectedItem.AssociatedGoal)
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
                        foreach (QueueItem queueItem in (List<QueueItem>)planet.ConstructionQueue)
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
                    if (input.C)
                        ResourceManager.CreateShipAtPoint("Kulrathi Assault Ship", this.player, this.mouseWorldPos);
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && !input.LastKeyboardState.IsKeyDown(Keys.Z))
                        HelperFunctions.CreateFleetAt("Fleet 2", this.player, this.mouseWorldPos);
                    if (this.SelectedShip != null && this.Debug)
                    {
                        if (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X))
                            this.SelectedShip.Die((GameplayObject)null, false);
                    }
                    else if (this.SelectedPlanet != null && this.Debug && (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X)))
                    {
                        foreach (KeyValuePair<string, Troop> keyValuePair in ResourceManager.TroopsDict)
                            this.SelectedPlanet.AssignTroopToTile(ResourceManager.CreateTroop(keyValuePair.Value, EmpireManager.GetEmpireByName("The Remnant")));
                    }
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.X) && !input.LastKeyboardState.IsKeyDown(Keys.X))
                        ResourceManager.CreateShipAtPoint("Target Dummy", EmpireManager.GetEmpireByName("The Remnant"), this.mouseWorldPos);
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.V) && !input.LastKeyboardState.IsKeyDown(Keys.V))
                        ResourceManager.CreateShipAtPoint("Remnant Mothership", EmpireManager.GetEmpireByName("The Remnant"), this.mouseWorldPos);
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
                                using (List<UniverseScreen.ClickableShip>.Enumerator enumerator = this.ClickableShipsList.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        UniverseScreen.ClickableShip current = enumerator.Current;
                                        if (clickableShip.shipToClick != current.shipToClick && current.shipToClick.loyalty == clickableShip.shipToClick.loyalty && current.shipToClick.Role == clickableShip.shipToClick.Role)
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
                    else
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
                            foreach (Ship item_7 in (List<Ship>)this.SelectedFleet.Ships)
                            {
                                if (item_7.inSensorRange)
                                    this.SelectedShipList.Add(item_7);
                            }
                            if (this.SelectedShipList.Count == 1)
                            {
                                this.SelectedShip = this.SelectedShipList[0];
                                this.ShipInfoUIElement.SetShip(this.SelectedShip);
                                this.SelectedShipList.Clear();
                            }
                            else if (this.SelectedShipList.Count > 1)
                                this.shipListInfoUI.SetShipList((List<Ship>)this.SelectedShipList, true);
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
                    foreach (Ship ship in (List<Ship>)this.player.GetFleetsDict()[index].Ships)
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
                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                    {

                        ship.RemoveFromAllFleets();

                    }
                    this.player.GetFleetsDict()[index] = new Fleet();
                    this.player.GetFleetsDict()[index].Name = str + " Fleet";
                    this.player.GetFleetsDict()[index].Owner = this.player;
                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player && ship.Role != "construction")
                            this.player.GetFleetsDict()[index].Ships.Add(ship);
                    }
                    this.player.GetFleetsDict()[index].AutoArrange();
                    this.SelectedShip = (Ship)null;
                    this.SelectedShipList.Clear();
                    this.SelectedFlank = (List<Fleet.Squad>)null;
                    if (this.player.GetFleetsDict()[index].Ships.Count > 0)
                    {
                        this.SelectedFleet = this.player.GetFleetsDict()[index];
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        this.SelectedFleet = (Fleet)null;
                    foreach (Ship ship in (List<Ship>)this.player.GetFleetsDict()[index].Ships)
                    {
                        this.SelectedShipList.Add(ship);
                        ship.fleet = this.player.GetFleetsDict()[index];
                    }
                    this.RecomputeFleetButtons(true);
                }
            }
            //added by gremlin add ships to exiting fleet
            else if (flag && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                if (this.SelectedShipList.Count > 0)
                {
                    //foreach (Ship ship in (List<Ship>)this.player.GetFleetsDict()[index].Ships)
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
                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
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
                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player && ship.Role != "construction" && (ship.fleet ==null ||ship.fleet.Name != str + " Fleet"))
                            this.player.GetFleetsDict()[index].Ships.Add(ship);
                    }
                    this.player.GetFleetsDict()[index].AutoArrange();
                    this.SelectedShip = (Ship)null;
                    this.SelectedShipList.Clear();
                    this.SelectedFlank = (List<Fleet.Squad>)null;
                    if (this.player.GetFleetsDict()[index].Ships.Count > 0)
                    {
                        this.SelectedFleet = this.player.GetFleetsDict()[index];
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        this.SelectedFleet = (Fleet)null;
                    foreach (Ship ship in (List<Ship>)this.player.GetFleetsDict()[index].Ships)
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
                    this.SelectedShip = (Ship)null;
                    this.SelectedFlank = (List<Fleet.Squad>)null;
                    if (this.player.GetFleetsDict()[index].Ships.Count > 0)
                    {
                        this.SelectedFleet = this.player.GetFleetsDict()[index];
                        AudioManager.PlayCue("techy_affirm1");
                    }
                    else
                        this.SelectedFleet = (Fleet)null;
                    this.SelectedShipList.Clear();
                    foreach (Ship ship in (List<Ship>)this.player.GetFleetsDict()[index].Ships)
                    {
                        this.SelectedShipList.Add(ship);
                        this.SelectedSomethingTimer = 3f;
                    }
                    if (this.SelectedFleet != null)
                    {
                        List<Ship> shipList = new List<Ship>();
                        foreach (Ship ship in (List<Ship>)this.SelectedFleet.Ships)
                            shipList.Add(ship);
                        this.shipListInfoUI.SetShipList(shipList, true);
                    }
                    if (this.SelectedFleet != null && (double)this.ClickTimer < (double)this.TimerDelay)
                    {
                        this.ViewingShip = false;
                        this.AdjustCamTimer = 0.5f;
                        this.transitionDestination.X = this.SelectedFleet.findAveragePosition().X;
                        this.transitionDestination.Y = this.SelectedFleet.findAveragePosition().Y;
                        if (this.camHeight < this.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView))
                            this.transitionDestination.Z = this.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                    }
                    else
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
                foreach (KeyValuePair<int, Fleet> item_0 in this.player.GetFleetsDict())
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
            foreach (UniverseScreen.ClickableShip clickableShip in this.ClickableShipsList)
            {
                if ((double)Vector2.Distance(this.input.CursorPosition, clickableShip.ScreenPos) <= (double)clickableShip.Radius)
                    return clickableShip.shipToClick;
            }
            return (Ship)null;
        }

        private Planet CheckPlanetClick(Vector2 ClickPos)
        {
            foreach (UniverseScreen.ClickablePlanets clickablePlanets in this.ClickPlanetList)
            {
                if ((double)Vector2.Distance(this.input.CursorPosition, clickablePlanets.ScreenPos) <= (double)clickablePlanets.Radius + 10.0)
                    return clickablePlanets.planetToClick;
            }
            return (Planet)null;
        }

        protected void HandleRightMouseNew(InputState input)
        {
            if (this.SkipRightOnce)
            {
                if (input.CurrentMouseState.RightButton != ButtonState.Released || input.LastMouseState.RightButton != ButtonState.Released)
                    return;
                this.SkipRightOnce = false;
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
                    float num2 = Math.Abs(MathHelper.ToRadians(this.findAngleToTarget(this.startDrag, target)));
                    Vector2 vector2_2 = Vector2.Normalize(target - this.startDrag);
                    if ((double)input.RightMouseTimer > 0.0)
                    {
                        if (this.SelectedFleet != null && this.SelectedFleet.Owner == this.player)
                        {
                            AudioManager.PlayCue("echo_affirm1");
                            this.SelectedSomethingTimer = 3f;
                            float num3 = Math.Abs(MathHelper.ToRadians(this.findAngleToTarget(this.SelectedFleet.Position, vector2_1)));
                            Vector2 vectorToTarget = HelperFunctions.FindVectorToTarget(Vector2.Zero, HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.SelectedFleet.Position, num3, 1f));
                            foreach (Ship ship in (List<Ship>)this.SelectedFleet.Ships)
                                this.player.GetGSAI().DefensiveCoordinator.remove(ship);
                            Ship ship1 = this.CheckShipClick(this.startDrag);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = this.CheckPlanetClick(this.startDrag);
                            if (ship1 != null && ship1.loyalty != this.player)
                            {
                                this.SelectedFleet.Position = ship1.Center;
                                this.SelectedFleet.AssignPositions(0.0f);
                                foreach (Ship ship2 in (List<Ship>)this.SelectedFleet.Ships)
                                {
                                    if (ship2.Role == "troop")
                                        ship2.GetAI().OrderTroopToBoardShip(ship1);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        ship2.GetAI().OrderQueueSpecificTarget(ship1);
                                    else
                                        ship2.GetAI().OrderAttackSpecificTarget(ship1);
                                }
                            }
                            else if (planet != null)
                            {
                                foreach (Ship ship2 in (List<Ship>)this.SelectedFleet.Ships)
                                {
                                    RightClickship(ship2, planet,false);
                                }
                            }
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                this.SelectedFleet.FormationWarpToQ(vector2_1, num3, vectorToTarget);
                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                this.SelectedFleet.MoveToDirectly(vector2_1, num3, vectorToTarget);
                            else
                                this.SelectedFleet.FormationWarpTo(vector2_1, num3, vectorToTarget);
                        }
                        else if (this.SelectedShip != null && this.SelectedShip.loyalty == this.player)
                        {
                            this.player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Remove(this.SelectedShip);
                            this.SelectedSomethingTimer = 3f;
                            Ship ship = this.CheckShipClick(this.startDrag);
                            Planet planet;
                            lock (GlobalStats.ClickableSystemsLock)
                                planet = this.CheckPlanetClick(this.startDrag);
                            if (ship != null && ship != this.SelectedShip)
                            {
                                if (this.SelectedShip.Role == "construction")
                                {
                                    AudioManager.PlayCue("UI_Misc20");
                                    return;
                                }
                                else
                                {
                                    AudioManager.PlayCue("echo_affirm1");
                                    if (ship.loyalty == this.player)
                                    {
                                        if (this.SelectedShip.Role == "troop")
                                        {
                                            if (ship.TroopList.Count < ship.TroopCapacity)
                                                this.SelectedShip.GetAI().OrderTroopToShip(ship);
                                            else
                                                this.SelectedShip.DoEscort(ship);
                                        }
                                        else
                                            this.SelectedShip.DoEscort(ship);
                                    }
                                    else if (this.SelectedShip.Role == "troop")
                                        this.SelectedShip.GetAI().OrderTroopToBoardShip(ship);
                                    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        this.SelectedShip.GetAI().OrderQueueSpecificTarget(ship);
                                    else
                                        this.SelectedShip.GetAI().OrderAttackSpecificTarget(ship);
                                }
                            }
                            else if (ship != null && ship == this.SelectedShip)
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
                                //if (this.SelectedShip.Role == "construction")
                                //{
                                //    AudioManager.PlayCue("UI_Misc20");
                                //    return;
                                //}
                                //else
                                //{
                                //    AudioManager.PlayCue("echo_affirm1");
                                //    if (this.SelectedShip.isColonyShip)
                                //    {
                                //        if (planet.Owner == null && planet.habitable)
                                //            this.SelectedShip.GetAI().OrderColonization(planet);
                                //        else
                                //            this.SelectedShip.GetAI().OrderToOrbit(planet, true);
                                //    }
                                //    else if (this.SelectedShip.Role == "troop")
                                //    {
                                //        if (planet.Owner != null && planet.Owner == this.player)
                                //        {
                                //            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                //                this.SelectedShip.GetAI().OrderRebase(planet, false);
                                //            else
                                //                this.SelectedShip.GetAI().OrderRebase(planet, true);
                                //        }
                                //            //add new right click troop and troop ship options on planets
                                //        if (planet.Owner == null)
                                //        {
                                //            this.SelectedShip.GetAI().State = AIState.AssaultPlanet;
                                //            this.SelectedShip.GetAI().OrderLandAllTroops(planet);
                                //        }
                                //        else if (this.SelectedShip.loyalty.GetRelations()[planet.Owner].AtWar)
                                //        {
                                //            this.SelectedShip.GetAI().State = AIState.AssaultPlanet;
                                //            this.SelectedShip.GetAI().OrderLandAllTroops(planet);
                                //        }
                                //            //end
                                //        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                //            this.SelectedShip.GetAI().OrderToOrbit(planet, false);
                                //        else
                                //            this.SelectedShip.GetAI().OrderToOrbit(planet, true);
                                //    }
                                //    else if (planet.Owner != null)
                                //    {
                                //        if (this.SelectedShip.BombBays.Count > 0 &&  (planet.Owner != this.player) && (this.player.GetRelations()[planet.Owner].AtWar || planet.Owner.isFaction &&(input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) || planet.TroopsHere.Where(ourtroops=> ourtroops.GetOwner()==this.player).Count()==0)))
                                //            this.SelectedShip.GetAI().OrderBombardPlanet(planet);
                                //        else
                                //            this.SelectedShip.GetAI().OrderToOrbit(planet, true);
                                //    }
                                //    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                //        this.SelectedShip.GetAI().OrderToOrbit(planet, false);
                                //    else
                                //        this.SelectedShip.GetAI().OrderToOrbit(planet, true);
                                //}
                            }
                            else if (this.SelectedShip.Role == "construction")
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
                                        this.SelectedShip.GetAI().OrderMoveTowardsPosition(vector2_1, num2, vector2_2, false);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    this.SelectedShip.GetAI().OrderMoveDirectlyTowardsPosition(vector2_1, num2, vector2_2, true);
                                else
                                    this.SelectedShip.GetAI().OrderMoveTowardsPosition(vector2_1, num2, vector2_2, true);
                            }
                        }
                        else if (this.SelectedShipList.Count > 0)
                        {
                            this.SelectedSomethingTimer = 3f;
                            foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                            {
                                if (ship.loyalty != this.player || ship.Role == "construction")
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
                            {
                                foreach (Ship ship2 in (List<Ship>)this.SelectedShipList)
                                {
                                    this.player.GetGSAI().DefensiveCoordinator.remove(ship2);
                                    if (ship1 != null && ship1 != ship2)
                                    {
                                        if (ship1.loyalty == this.player)
                                        {
                                            if (ship2.Role == "troop")
                                            {
                                                if (ship1.TroopList.Count < ship1.TroopCapacity)
                                                    ship2.GetAI().OrderTroopToShip(ship1);
                                                else
                                                    ship2.DoEscort(ship1);
                                            }
                                            else
                                                ship2.DoEscort(ship1);
                                        }
                                        else if (ship2.Role == "troop")
                                            ship2.GetAI().OrderTroopToBoardShip(ship1);
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            ship2.GetAI().OrderQueueSpecificTarget(ship1);
                                        else
                                            ship2.GetAI().OrderAttackSpecificTarget(ship1);
                                    }
                                    else if (planet != null)
                                    {
                                       RightClickship(ship2,planet,false);
                                        //if (ship2.isColonyShip)
                                        //{
                                        //    if (planet.Owner == null && planet.habitable)
                                        //        ship2.GetAI().OrderColonization(planet);
                                        //    else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        //        ship2.GetAI().OrderToOrbit(planet, false);
                                        //    else
                                        //        ship2.GetAI().OrderToOrbit(planet, true);
                                        //}
                                        //else if (ship2.Role == "troop" ||(ship2.TroopList.Count >0 && ship2.GetHangars().Where(troops=> troops.IsTroopBay).Count()>0))
                                        //{
                                        //    if (ship2.Role == "troop")
                                        //    {
                                        //        if (planet.Owner != null && planet.Owner == this.player)
                                        //        {
                                        //            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        //                ship2.GetAI().OrderRebase(planet, false);
                                        //            else
                                        //                ship2.GetAI().OrderRebase(planet, true);
                                        //        }
                                        //        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        //            ship2.GetAI().OrderToOrbit(planet, false);
                                        //        else
                                        //            ship2.GetAI().OrderToOrbit(planet, true);
                                        //    }

                                        //    else if (planet.Owner ==null ||  
                                        //        planet.Owner != this.player 
                                        //        && ((planet.Owner == null && planet.habitable) 
                                        //        || (ship2.loyalty.GetRelations()[planet.Owner].AtWar ||planet.Owner.isFaction)))
                                        //    {

                                        //        ship2.GetAI().OrderLandAllTroops(planet);
                                        //    }
                                            


                                        //}
                                        //else if (planet.Owner != null)
                                        //{
                                        //    if (ship2.BombBays.Count > 0 && planet.Owner != this.player && (this.player.GetRelations()[planet.Owner].AtWar || planet.Owner.isFaction) && (planet.TroopsHere.Where(ourtroops=> ourtroops.GetOwner() ==this.player).Count()==0 || input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift)))
                                        //        ship2.GetAI().OrderBombardPlanet(planet);
                                        //    else
                                        //        ship2.GetAI().OrderToOrbit(planet, true);
                                        //}
                                        //else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        //    ship2.GetAI().OrderToOrbit(planet, false);
                                        //else
                                        //    ship2.GetAI().OrderToOrbit(planet, true);
                                    }
                                }
                            }
                            else
                            {
                                this.SelectedSomethingTimer = 3f;
                                foreach (Ship ship2 in (List<Ship>)this.SelectedShipList)
                                {
                                    if (ship2.Role == "construction")
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
                                foreach (Ship ship2 in (List<Ship>)fleet.Ships)
                                {
                                    foreach (Ship ship3 in (List<Ship>)this.SelectedShipList)
                                    {
                                        if (ship2.guid == ship3.guid)
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                            {
                                                if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                    ship3.GetAI().OrderMoveDirectlyTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, false);
                                                else
                                                    ship3.GetAI().OrderMoveTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, false);
                                            }
                                            else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship3.GetAI().OrderMoveDirectlyTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, true);
                                            else
                                                ship3.GetAI().OrderMoveTowardsPosition(ship2.projectedPosition, num2 - 1.570796f, fVec, true);
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
                            if (ship != null)
                            {
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
                                this.SelectedFleet.FormationWarpToQ(this.ProjectedPosition, num2, vector2_2);
                            else
                                this.SelectedFleet.FormationWarpTo(this.ProjectedPosition, num2, vector2_2);
                            AudioManager.PlayCue("echo_affirm1");
                            foreach (Ship ship in (List<Ship>)this.SelectedFleet.Ships)
                                this.player.GetGSAI().DefensiveCoordinator.remove(ship);
                        }
                        else if (this.SelectedShip != null && this.SelectedShip.loyalty == this.player)
                        {
                            this.player.GetGSAI().DefensiveCoordinator.remove(this.SelectedShip);
                            this.SelectedSomethingTimer = 3f;
                            if (this.SelectedShip.Role == "construction")
                            {
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
                                        this.SelectedShip.GetAI().OrderMoveTowardsPosition(this.ProjectedPosition, num2, vector2_2, false);
                                }
                                else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                    this.SelectedShip.GetAI().OrderMoveDirectlyTowardsPosition(this.ProjectedPosition, num2, vector2_2, true);
                                else
                                    this.SelectedShip.GetAI().OrderMoveTowardsPosition(this.ProjectedPosition, num2, vector2_2, true);
                            }
                        }
                        else if (this.SelectedShipList.Count > 0)
                        {
                            this.SelectedSomethingTimer = 3f;
                            foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                            {
                                if (ship.loyalty != this.player)
                                    return;
                                if (ship.Role == "construction")
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
                            foreach (Ship ship1 in (List<Ship>)fleet.Ships)
                            {
                                foreach (Ship ship2 in (List<Ship>)this.SelectedShipList)
                                {
                                    if (ship1.guid == ship2.guid)
                                    {
                                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                                        {
                                            if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                                ship2.GetAI().OrderMoveDirectlyTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, false);
                                            else
                                                ship2.GetAI().OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, false);
                                        }
                                        else if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt))
                                            ship2.GetAI().OrderMoveDirectlyTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, true);
                                        else
                                            ship2.GetAI().OrderMoveTowardsPosition(ship1.projectedPosition, num2 - 1.570796f, fVec, true);
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
                    float facing = Math.Abs(MathHelper.ToRadians(this.findAngleToTarget(this.startDrag, target)));
                    Vector2 fVec1 = Vector2.Normalize(target - this.startDrag);
                    if ((double)input.RightMouseTimer > 0.0)
                        return;
                    this.ProjectingPosition = true;
                    if (this.SelectedFlank != null)
                    {
                        this.SelectedFleet.ProjectPos(this.ProjectedPosition, facing, this.SelectedFlank);
                        ShipGroup shipGroup = new ShipGroup();
                        foreach (Fleet.Squad squad in this.SelectedFlank)
                        {
                            foreach (Ship ship in (List<Ship>)squad.Ships)
                                shipGroup.Ships.Add(ship);
                        }
                        shipGroup.ProjectedFacing = facing;
                        this.projectedGroup = shipGroup;
                    }
                    else if (this.SelectedFleet != null && this.SelectedFleet.Owner == this.player)
                    {
                        this.ProjectingPosition = true;
                        this.SelectedFleet.ProjectPos(this.ProjectedPosition, facing, fVec1);
                        this.projectedGroup = (ShipGroup)this.SelectedFleet;
                    }
                    else if (this.SelectedShip != null && this.SelectedShip.loyalty == this.player)
                    {
                        if (this.SelectedShip.Role == "construction")
                        {
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
                        foreach (Ship ship in (List<Ship>)this.SelectedShipList)
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
            if (ship.Role == "construction")
            {
                if(audio)
                    AudioManager.PlayCue("UI_Misc20");
                return;
            }
            else
            {
                if(audio)
                    AudioManager.PlayCue("echo_affirm1");
                if (ship.isColonyShip)
                {
                    if (planet.Owner == null && planet.habitable)
                        ship.GetAI().OrderColonization(planet);
                    else
                        ship.GetAI().OrderToOrbit(planet, true);
                }
                else if (ship.Role == "troop" || (ship.TroopList.Count > 0 && (ship.HasTroopBay || ship.hasTransporter)))
                {
                    if (planet.Owner != null && planet.Owner == this.player && !ship.HasTroopBay && !ship.hasTransporter)
                    {
                        if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                            ship.GetAI().OrderToOrbit(planet, false);
                        else
                            ship.GetAI().OrderRebase(planet, true);
                    }
                    else
                    //add new right click troop and troop ship options on planets
                        if (planet.habitable && (planet.Owner == null || planet.Owner != this.player && (ship.loyalty.GetRelations()[planet.Owner].AtWar || planet.Owner.isFaction || planet.Owner.data.Defeated || planet.Owner == null)))
                        {
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

                                ship.GetAI().OrderRebase(planet, true);
                        }
                }
                else if (planet.Owner != null)
                {
                    if (ship.BombBays.Count > 0 && (planet.Owner != this.player) && (this.player.GetRelations()[planet.Owner].AtWar || planet.Owner.isFaction && (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) || planet.TroopsHere.Where(ourtroops => ourtroops.GetOwner() == this.player).Count() == 0)))
                        ship.GetAI().OrderBombardPlanet(planet);
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
            if (input.CurrentMouseState.MiddleButton == ButtonState.Pressed)
            {
                this.snappingToShip = false;
                this.ViewingShip = false;
            }
            if (input.CurrentMouseState.MiddleButton != ButtonState.Pressed || input.LastMouseState.MiddleButton != ButtonState.Released)
                return;
            Vector2 spaceFromScreenSpace2 = this.GetWorldSpaceFromScreenSpace(input.CursorPosition);
            this.transitionDestination.X = spaceFromScreenSpace2.X;
            this.transitionDestination.Y = spaceFromScreenSpace2.Y;
            this.transitionDestination.Z = this.camHeight;
            this.AdjustCamTimer = 1f;
            this.transitionElapsedTime = 0.0f;
        }

        protected void HandleScrolls(InputState input)
        {
            if ((double)this.AdjustCamTimer >= 0.0)
                return;
            if ((input.ScrollOut || input.BButtonHeld) && !this.LookingAtPlanet)
            {
                float num = (float)(1500.0 * (double)this.camHeight / 3000.0 + 100.0);
                if ((double)this.camHeight < 10000.0)
                    num -= 200f;
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
            if ((double)this.camHeight < 10000.0)
                num1 -= 200f;
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
            if (this.SelectedShip != null)
            {
                if (input.CurrentKeyboardState.IsKeyDown(Keys.R) && !input.LastKeyboardState.IsKeyDown(Keys.R))
                    this.SelectedShip.FightersOut = !this.SelectedShip.FightersOut;
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
                this.SelectedShip = (Ship)null;
                this.SelectedPlanet = (Planet)null;
                this.SelectedFleet = (Fleet)null;
                this.SelectedFlank = (List<Fleet.Squad>)null;
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
                            using (List<Ship>.Enumerator enumerator = this.SelectedFleet.Ships.GetEnumerator())
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
                                    this.SelectedShip = clickableShip.shipToClick;
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
                            this.shipListInfoUI.SetShipList((List<Ship>)this.SelectedShipList, false);
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
                this.SelectedShip = this.SelectedShipList[0];
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
                List<Ship> list = new List<Ship>();
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
                        if (ship.Role == "station" || ship.Role == "construction" || (ship.Role == "platform" || ship.Role == "supply"))
                            flag2 = true;
                        else
                            flag3 = true;
                    }
                    if (flag3 && flag2)
                    {
                        foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                        {
                            if (ship.Role == "station" || ship.Role == "construction" || (ship.Role == "platform" || ship.Role == "supply"))
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                    }
                    this.SelectedShipList.ApplyPendingRemovals();
                }
                if (this.SelectedShipList.Count > 1)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player)
                            flag2 = true;
                        if (ship.loyalty != this.player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                        this.SelectedShipList.ApplyPendingRemovals();
                    }
                    this.SelectedShip = (Ship)null;
                    this.shipListInfoUI.SetShipList((List<Ship>)this.SelectedShipList, true);
                }
                else if (this.SelectedShipList.Count == 1)
                {
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
                        foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                        {
                            if (ship.Role == "station" || ship.Role == "construction" || (ship.Role == "platform" || ship.Role == "supply") || (ship.Role == "freighter" || ship.Role == "colony"))
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
                                foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                                {
                                    if (ship.Role == "station" || ship.Role == "construction" || (ship.Role == "platform" || ship.Role == "supply") || (ship.Role == "freighter" || ship.Role == "colony"))
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
                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                    {
                        if (ship.loyalty == this.player)
                            flag2 = true;
                        if (ship.loyalty != this.player)
                            flag3 = true;
                    }
                    if (flag2 && flag3)
                    {
                        foreach (Ship ship in (List<Ship>)this.SelectedShipList)
                        {
                            if (ship.loyalty != this.player)
                                this.SelectedShipList.QueuePendingRemoval(ship);
                        }
                        this.SelectedShipList.ApplyPendingRemovals();
                    }
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
                                    foreach (Ship ship in (List<Ship>)this.SelectedShipList)
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
                            this.shipListInfoUI.SetShipList((List<Ship>)this.SelectedShipList, true);
                        else
                            this.shipListInfoUI.SetShipList((List<Ship>)this.SelectedShipList, false);
                    }
                    if (this.SelectedFleet == null)
                        this.ShipInfoUIElement.SetShip(this.SelectedShipList[0]);
                }
                else if (this.SelectedShipList.Count == 1)
                {
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
            if (this.MultiThread)
            {
                this.ShipUpdateThread.Abort();
                this.WorkerThread.Abort();
                foreach (Thread thread in this.SystemUpdateThreadList)
                    thread.Abort();
            }
            this.EmpireUI.empire = (Empire)null;
            this.EmpireUI = (EmpireUIOverlay)null;
            UniverseScreen.DeepSpaceManager.CollidableObjects.Clear();
            UniverseScreen.DeepSpaceManager.CollidableProjectiles.Clear();
            UniverseScreen.ShipSpatialManager.CollidableObjects.Clear();
            this.ScreenManager.Music.Stop(AudioStopOptions.Immediate);
            this.NebulousShit.Clear();
            this.bloomComponent = (BloomComponent)null;
            this.bg3d.BGItems.Clear();
            this.bg3d = (Background3D)null;
            this.playerShip = (Ship)null;
            this.ShipToView = (Ship)null;
            foreach (Ship ship in (List<Ship>)this.MasterShipList)
                ship.TotallyRemove();
            this.MasterShipList.ApplyPendingRemovals();
            this.MasterShipList.Clear();
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                solarSystem.spatialManager.CollidableProjectiles.Clear();
                solarSystem.spatialManager.CollidableObjects.Clear();
                solarSystem.spatialManager.ClearBuckets();
                solarSystem.spatialManager.Destroy();
                solarSystem.spatialManager = (SpatialManager)null;
                solarSystem.FiveClosestSystems.Clear();
                foreach (Planet planet in solarSystem.PlanetList)
                {
                    planet.TilesList = new List<PlanetGridSquare>();
                    if (planet.SO != null)
                    {
                        planet.SO.Clear();
                        this.ScreenManager.inter.ObjectManager.Remove((ISceneObject)planet.SO);
                        planet.SO = (SceneObject)null;
                    }
                }
                foreach (Asteroid asteroid in (List<Asteroid>)solarSystem.AsteroidsList)
                {
                    if (asteroid.GetSO() != null)
                    {
                        asteroid.GetSO().Clear();
                        this.ScreenManager.inter.ObjectManager.Remove((ISceneObject)asteroid.GetSO());
                    }
                }
                solarSystem.AsteroidsList.Clear();
                foreach (Moon moon in solarSystem.MoonList)
                {
                    if (moon.GetSO() != null)
                    {
                        moon.GetSO().Clear();
                        this.ScreenManager.inter.ObjectManager.Remove((ISceneObject)moon.GetSO());
                    }
                }
                solarSystem.MoonList.Clear();
            }
            foreach (Empire empire in EmpireManager.EmpireList)
                empire.CleanOut();
            foreach (SpaceJunk spaceJunk in (List<SpaceJunk>)UniverseScreen.JunkList)
            {
                spaceJunk.trailEmitter = (ParticleEmitter)null;
                spaceJunk.JunkSO.Clear();
                this.ScreenManager.inter.ObjectManager.Remove((ISceneObject)spaceJunk.JunkSO);
                spaceJunk.JunkSO = (SceneObject)null;
            }
            UniverseScreen.JunkList.Clear();
            this.SelectedShip = (Ship)null;
            this.SelectedFleet = (Fleet)null;
            this.SelectedPlanet = (Planet)null;
            this.SelectedSystem = (SolarSystem)null;
            ShieldManager.shieldList.Clear();
            ShieldManager.PlanetaryShieldList.Clear();
            this.PlanetsDict.Clear();
            this.ClickableFleetsList.Clear();
            this.ClickableShipsList.Clear();
            this.ClickPlanetList.Clear();
            this.ClickableSystems.Clear();
            UniverseScreen.DeepSpaceManager.ClearBuckets();
            UniverseScreen.DeepSpaceManager.CollidableObjects.Clear();
            UniverseScreen.DeepSpaceManager.CollidableObjects.Clear();
            UniverseScreen.DeepSpaceManager.CollidableProjectiles.Clear();
            UniverseScreen.DeepSpaceManager.ClearBuckets();
            UniverseScreen.DeepSpaceManager.Destroy();
            UniverseScreen.DeepSpaceManager = (SpatialManager)null;
            UniverseScreen.SolarSystemList.Clear();
            this.starfield.UnloadContent();
            this.starfield.Dispose();
            UniverseScreen.SolarSystemList.Clear();
            this.beamflashes.UnloadContent();
            this.explosionParticles.UnloadContent();
            this.photonExplosionParticles.UnloadContent();
            this.explosionSmokeParticles.UnloadContent();
            this.projectileTrailParticles.UnloadContent();
            this.fireTrailParticles.UnloadContent();
            this.smokePlumeParticles.UnloadContent();
            this.fireParticles.UnloadContent();
            this.engineTrailParticles.UnloadContent();
            this.flameParticles.UnloadContent();
            this.sparks.UnloadContent();
            this.lightning.UnloadContent();
            this.flash.UnloadContent();
            this.star_particles.UnloadContent();
            this.neb_particles.UnloadContent();
            this.SolarSystemDict.Clear();
            ShipDesignScreen.screen = (UniverseScreen)null;
            Fleet.screen = (UniverseScreen)null;
            Bomb.screen = (UniverseScreen)null;
            Anomaly.screen = (UniverseScreen)null;
            PlanetScreen.screen = (UniverseScreen)null;
            MinimapButtons.screen = (UniverseScreen)null;
            Projectile.contentManager = this.ScreenManager.Content;
            Projectile.universeScreen = (UniverseScreen)null;
            ShipModule.universeScreen = (UniverseScreen)null;
            Asteroid.universeScreen = (UniverseScreen)null;
            Empire.universeScreen = (UniverseScreen)null;
            SpaceJunk.universeScreen = (UniverseScreen)null;
            ResourceManager.universeScreen = (UniverseScreen)null;
            Planet.universeScreen = (UniverseScreen)null;
            Weapon.universeScreen = (UniverseScreen)null;
            Ship.universeScreen = (UniverseScreen)null;
            ArtificialIntelligence.universeScreen = (UniverseScreen)null;
            MissileAI.universeScreen = (UniverseScreen)null;
            Moon.universeScreen = (UniverseScreen)null;
            CombatScreen.universeScreen = (UniverseScreen)null;
            MuzzleFlashManager.universeScreen = (UniverseScreen)null;
            FleetDesignScreen.screen = (UniverseScreen)null;
            ExplosionManager.universeScreen = (UniverseScreen)null;
            FTLManager.universeScreen = (UniverseScreen)null;
            DroneAI.universeScreen = (UniverseScreen)null;
            StatTracker.SnapshotsDict.Clear();
            EmpireManager.EmpireList.Clear();
            this.ScreenManager.inter.Unload();
            GC.Collect();
            this.Dispose();
            base.ExitScreen();
        }

        private void ClearParticles()
        {
            this.beamflashes.UnloadContent();
            this.explosionParticles.UnloadContent();
            this.photonExplosionParticles.UnloadContent();
            this.explosionSmokeParticles.UnloadContent();
            this.projectileTrailParticles.UnloadContent();
            this.fireTrailParticles.UnloadContent();
            this.smokePlumeParticles.UnloadContent();
            this.fireParticles.UnloadContent();
            this.engineTrailParticles.UnloadContent();
            this.flameParticles.UnloadContent();
            this.sparks.UnloadContent();
            this.lightning.UnloadContent();
            this.flash.UnloadContent();
            this.star_particles.UnloadContent();
            this.neb_particles.UnloadContent();
            GC.Collect();
        }

        protected void DrawRings(Matrix world, Matrix view, Matrix projection, float scale)
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            foreach (BasicEffect basicEffect in ((ReadOnlyCollection<ModelMesh>)this.xnaPlanetModel.Meshes)[1].Effects)
            {
                basicEffect.World = Matrix.CreateScale(3f) * Matrix.CreateScale(scale) * world;
                basicEffect.View = view;
                basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                basicEffect.Texture = this.RingTexture;
                basicEffect.TextureEnabled = true;
                basicEffect.Projection = projection;
            }
            ((ReadOnlyCollection<ModelMesh>)this.xnaPlanetModel.Meshes)[1].Draw();
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        protected UniverseScreen.MultiShipData ComputeMultiShipCircle()
        {
            float num1 = 0.0f;
            float num2 = 0.0f;
            float num3 = 0.0f;
            float num4 = 0.0f;
            foreach (Ship ship in (List<Ship>)this.SelectedShipList)
            {
                num1 += ship.Position.X;
                num2 += ship.Position.Y;
                num3 += ship.Health;
                num4 += ship.HealthMax;
            }
            float x = num1 / (float)this.SelectedShipList.Count;
            float y = num2 / (float)this.SelectedShipList.Count;
            UniverseScreen.MultiShipData multiShipData = new UniverseScreen.MultiShipData();
            multiShipData.status = num3 / num4;
            multiShipData.weightedCenter = new Vector2(x, y);
            multiShipData.Radius = 0.0f;
            foreach (GameplayObject gameplayObject in (List<Ship>)this.SelectedShipList)
            {
                float num5 = Vector2.Distance(gameplayObject.Position, multiShipData.weightedCenter);
                if ((double)num5 > (double)multiShipData.Radius)
                    multiShipData.Radius = num5;
            }
            //this.computeCircle = false;
            return multiShipData;
        }

        protected void DrawAtmo(Model model, Matrix world, Matrix view, Matrix projection, Planet p)
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
            ModelMesh modelMesh = ((ReadOnlyCollection<ModelMesh>)model.Meshes)[0];
            foreach (BasicEffect basicEffect in modelMesh.Effects)
            {
                basicEffect.World = Matrix.CreateScale(4.1f) * world;
                basicEffect.View = view;
                basicEffect.Texture = ResourceManager.TextureDict["Textures/Atmos"];
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
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = false;
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
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
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
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
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
            Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, this.SelectedPlanet.Position, radius), 2500f), this.projection, this.view, Matrix.Identity);
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
                if (moduleSlot.module.ModuleType == ShipModuleType.Shield && moduleSlot.module.Active && (double)moduleSlot.module.shield_power > 0.0 && !moduleSlot.module.shieldsOff)
                {
                    Vector2 origin1 = (int)moduleSlot.module.XSIZE != 1 || (int)moduleSlot.module.YSIZE != 3 ? ((int)moduleSlot.module.XSIZE != 2 || (int)moduleSlot.module.YSIZE != 5 ? new Vector2(moduleSlot.module.Center.X - 8f + (float)(16 * (int)moduleSlot.module.XSIZE / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)moduleSlot.module.YSIZE / 2)) : new Vector2(moduleSlot.module.Center.X - 80f + (float)(16 * (int)moduleSlot.module.XSIZE / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)moduleSlot.module.YSIZE / 2))) : new Vector2(moduleSlot.module.Center.X - 50f + (float)(16 * (int)moduleSlot.module.XSIZE / 2), moduleSlot.module.Center.Y - 8f + (float)(16 * (int)moduleSlot.module.YSIZE / 2));
                    Vector2 target = new Vector2(moduleSlot.module.Center.X - 8f, moduleSlot.module.Center.Y - 8f);
                    float angleToTarget = this.findAngleToTarget(origin1, target);
                    Vector2 angleAndDistance = this.findPointFromAngleAndDistance(moduleSlot.module.Center, MathHelper.ToDegrees(ship.Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));
                    float num1 = (float)((int)moduleSlot.module.XSIZE * 16 / 2);
                    float num2 = (float)((int)moduleSlot.module.YSIZE * 16 / 2);
                    float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num1, 2.0) + (float)Math.Pow((double)num2, 2.0)));
                    float radians = 3.141593f - (float)Math.Asin((double)num1 / (double)distance) + ship.Rotation;
                    origin1 = this.findPointFromAngleAndDistance(angleAndDistance, MathHelper.ToDegrees(radians), distance);
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(origin1, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 vector2_2 = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, moduleSlot.module.Center, moduleSlot.module.shield_radius * 1.5f), 0.0f), this.projection, this.view, Matrix.Identity);
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
            foreach (UniverseScreen.FogOfWarNode fogOfWarNode in this.FogNodes)
            {
                if (fogOfWarNode.Discovered)
                {
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(fogOfWarNode.Position.X, fogOfWarNode.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, fogOfWarNode.Position, fogOfWarNode.Radius * 1.5f), 0.0f), this.projection, this.view, Matrix.Identity);
                    float num = Math.Abs(new Vector2(vector3_2.X, vector3_2.Y).X - vector2.X);
                    Rectangle destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, (int)num * 2, (int)num * 2);
                    Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], destinationRectangle, new Rectangle?(), new Color((byte)70, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0f, origin, SpriteEffects.None, 1f);
                }
            }
        }

        protected void DrawInfluenceNodes()
        {
            List<Empire.InfluenceNode> influenceNodes;
            this.player.SensorNodeLocker.EnterReadLock();
            {
                influenceNodes = new List<Empire.InfluenceNode>(this.player.SensorNodes);
            }
            this.player.SensorNodeLocker.ExitReadLock();
            {
                try
                {
                    
                    //foreach (Empire.InfluenceNode item_0 in (List<Empire.InfluenceNode>)this.player.SensorNodes)
                    foreach (Empire.InfluenceNode item_0 in influenceNodes)
                    {
                        if (item_0 == null)
                            continue;
                        Vector3 local_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item_0.Position.X, item_0.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_2 = new Vector2(local_1.X, local_1.Y);
                        Vector3 local_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, item_0.Position, item_0.Radius * 1.5f), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_6 = Math.Abs(new Vector2(local_4.X, local_4.Y).X - local_2.X);
                        Rectangle local_7 = new Rectangle((int)local_2.X, (int)local_2.Y, (int)((double)local_6 * 2.59999990463257), (int)((double)local_6 * 2.59999990463257));
                        Vector2 local_8 = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], local_7, new Rectangle?(), Color.White, 0.0f, local_8, SpriteEffects.None, 1f);
                    }
                }
                catch
                {
                }
            }
        }

        protected void DrawBorders()
        {
            try
            {
                foreach (Empire index in EmpireManager.EmpireList)
                {
                    if (this.Debug || index == this.player || this.player.GetRelations()[index].Known)
                    {
                        List<Circle> list = new List<Circle>();
                        index.BorderNodeLocker.EnterReadLock();
                        {
                            foreach (Empire.InfluenceNode item_1 in (List<Empire.InfluenceNode>)index.BorderNodes)
                            {
                                if (this.Frustum.Contains(new BoundingSphere(new Vector3(item_1.Position, 0.0f), item_1.Radius)) != ContainmentType.Disjoint)
                                {
                                    Vector3 local_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item_1.Position.X, item_1.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector2 local_3 = new Vector2(local_2.X, local_2.Y);
                                    Vector3 local_5 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, item_1.Position, item_1.Radius), 0.0f), this.projection, this.view, Matrix.Identity);
                                    float local_7 = Math.Abs(new Vector2(local_5.X, local_5.Y).X - local_3.X);
                                    Rectangle local_8 = new Rectangle((int)local_3.X, (int)local_3.Y, (int)local_7 * 5, (int)local_7 * 5);
                                    Vector2 local_9 = new Vector2((float)(ResourceManager.TextureDict["UI/nodecorrected"].Width / 2), (float)(ResourceManager.TextureDict["UI/nodecorrected"].Height / 2));
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/nodecorrected"], local_8, new Rectangle?(), index.EmpireColor, 0.0f, local_9, SpriteEffects.None, 1f);
                                    //Vector2 local_9 = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
                                    //this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], local_8, new Rectangle?(), index.EmpireColor, 0.0f, local_9, SpriteEffects.None, 1f);
                                    foreach (Empire.InfluenceNode item_0 in (List<Empire.InfluenceNode>)index.BorderNodes)
                                    {
                                        if (!(item_1.Position == item_0.Position) && (double)item_1.Radius <= (double)item_0.Radius && (double)Vector2.Distance(item_1.Position, item_0.Position) <= (double)item_1.Radius + (double)item_0.Radius + 150000.0)
                                        {
                                            Vector2 local_12 = item_0.Position - item_1.Position;
                                            Vector2 local_13 = item_1.Position + local_12 / 2f;
                                            local_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(local_13.X, local_13.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                                            local_3 = new Vector2(local_2.X, local_2.Y);
                                            Vector2 local_13_1 = local_3;
                                            local_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item_0.Position.X, item_0.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                                            local_3 = new Vector2(local_2.X, local_2.Y);
                                            float local_14 = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(local_13_1, local_3));
                                            local_8 = new Rectangle((int)local_13_1.X, (int)local_13_1.Y, (int)local_7, (int)Vector2.Distance(local_13_1, local_3));
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/nodeconnect"], local_8, new Rectangle?(), index.EmpireColor, local_14, new Vector2(2f, 2f), SpriteEffects.None, 1f);
                                            //this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], local_8, new Rectangle?(), index.EmpireColor, local_14, new Vector2(2f, 2f), SpriteEffects.None, 1f);
                                        }
                                    }
                                }
                            }
                        }
                        index.BorderNodeLocker.ExitReadLock();
                    }
                }
            }
            catch
            {
            }
        }

        protected void DrawMain(GameTime gameTime)
        {
            this.Render(gameTime);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            lock (GlobalStats.ExplosionLocker)
            {
                foreach (Explosion item_0 in (List<Explosion>)ExplosionManager.ExplosionList)
                {
                    if (!float.IsNaN(item_0.Radius))
                    {
                        Vector3 local_1 = item_0.module == null ? this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item_0.pos.X, item_0.pos.Y, 0.0f), this.projection, this.view, Matrix.Identity) : this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item_0.module.Position.X, item_0.module.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_2 = new Vector2(local_1.X, local_1.Y);
                        Vector3 local_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, new Vector2(item_0.pos.X, item_0.pos.Y), item_0.Radius), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_6 = Math.Abs(new Vector2(local_4.X, local_4.Y).X - local_2.X);
                        item_0.ExplosionRect = new Rectangle((int)local_2.X, (int)local_2.Y, (int)local_6, (int)local_6);
                        if (item_0.Animation == 1)
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[item_0.AnimationTexture], item_0.ExplosionRect, new Rectangle?(), item_0.color, item_0.Rotation, new Vector2((float)(ResourceManager.TextureDict[item_0.AnimationTexture].Width / 2), (float)(ResourceManager.TextureDict[item_0.AnimationTexture].Height / 2)), SpriteEffects.None, 1f);
                    }
                }
            }
            this.ScreenManager.SpriteBatch.End();
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
            float num = 512f / this.Size.X;
            try
            {
                foreach (Ship ship in (List<Ship>)this.player.GetShips())
                {
                    if (HelperFunctions.CheckIntersection(this.ScreenRectangle, ship.ScreenPosition))
                    {
                        Rectangle destinationRectangle = new Rectangle((int)((double)ship.Position.X * (double)num), (int)((double)ship.Position.Y * (double)num), (int)((double)ship.SensorRange * (double)num * 2.0), (int)((double)ship.SensorRange * (double)num * 2.0));
                        Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node"], destinationRectangle, new Rectangle?(), new Color(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue), 0.0f, origin, SpriteEffects.None, 1f);
                    }
                }
            }
            catch
            {
            }
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, (RenderTarget2D)null);
            this.FogMap = this.FogMapTarget.GetTexture();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.LightsTarget);
            this.ScreenManager.GraphicsDevice.Clear(Color.White);
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X, this.Size.Y, 0.0f), this.projection, this.view, Matrix.Identity);
            this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.Size.X / 2f, this.Size.Y / 2f, 0.0f), this.projection, this.view, Matrix.Identity);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            Rectangle destinationRectangle1 = new Rectangle((int)vector3_1.X - 40, (int)vector3_1.Y - 40, (int)vector3_2.X - (int)vector3_1.X + 80, (int)vector3_2.Y - (int)vector3_1.Y + 80);
            destinationRectangle1 = new Rectangle((int)vector3_1.X, (int)vector3_1.Y, (int)vector3_2.X - (int)vector3_1.X, (int)vector3_2.Y - (int)vector3_1.Y);
            if (this.FogOn)
            {
                Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), new Color((byte)0, (byte)0, (byte)0, (byte)170));
                this.ScreenManager.SpriteBatch.Draw(this.FogMap, destinationRectangle1, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)55));
            }
            this.DrawFogNodes();
            this.DrawInfluenceNodes();
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, (RenderTarget2D)null);
        }

        public override void Draw(GameTime gameTime)
        {
            if (this.MultiThread)
            {
                this.WorkerCompletedEvent.WaitOne();
                if (!this.Paused && this.IsActive)
                {
                    double totalSeconds = gameTime.ElapsedGameTime.TotalSeconds;
                }
                this.WorkerCompletedEvent.Reset();
            }
            lock (GlobalStats.BeamEffectLocker)
            {
                Beam.BeamEffect.Parameters["View"].SetValue(this.view);
                Beam.BeamEffect.Parameters["Projection"].SetValue(this.projection);
            }
            this.AdjustCamera((float)gameTime.ElapsedGameTime.TotalSeconds);
            this.camPos.Z = this.camHeight;
            this.view = Matrix.CreateTranslation(0.0f, 0.0f, 0.0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f)) * Matrix.CreateRotationX(MathHelper.ToRadians(0.0f)) * Matrix.CreateLookAt(new Vector3(-this.camPos.X, this.camPos.Y, this.camHeight), new Vector3(-this.camPos.X, this.camPos.Y, 0.0f), new Vector3(0.0f, -1f, 0.0f));
            Matrix matrix = this.view;
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.MainTarget);
            this.DrawMain(gameTime);
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, (RenderTarget2D)null);
            this.DrawLights(gameTime);
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, this.BorderRT);
            this.ScreenManager.GraphicsDevice.Clear(Color.TransparentBlack);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            this.ScreenManager.GraphicsDevice.RenderState.SeparateAlphaBlendEnabled = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaSourceBlend = Blend.One;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaDestinationBlend = Blend.One;
            this.ScreenManager.GraphicsDevice.RenderState.MultiSampleAntiAlias = true;
            if (this.viewState >= UniverseScreen.UnivScreenState.SectorView)
                this.DrawBorders();
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, (RenderTarget2D)null);
            Texture2D texture1 = this.MainTarget.GetTexture();
            Texture2D texture2 = this.LightsTarget.GetTexture();
            Texture2D texture3 = this.BorderRT.GetTexture();
            this.ScreenManager.GraphicsDevice.SetRenderTarget(0, (RenderTarget2D)null);
            this.ScreenManager.GraphicsDevice.Clear(Color.Black);
            this.basicFogOfWarEffect.Parameters["LightsTexture"].SetValue((Texture)texture2);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            this.basicFogOfWarEffect.Begin();
            this.basicFogOfWarEffect.CurrentTechnique.Passes[0].Begin();
            this.ScreenManager.SpriteBatch.Draw(texture1, new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), Color.White);
            this.basicFogOfWarEffect.CurrentTechnique.Passes[0].End();
            this.basicFogOfWarEffect.End();
            this.ScreenManager.SpriteBatch.End();
            this.view = matrix;
            if (this.drawBloom)
            this.bloomComponent.Draw(gameTime);
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            Color color = Color.White;
            float num = (float)(75.0 * (double)this.camHeight / 1800000.0);
            if ((double)num > 75.0)
                num = 75f;
            if ((double)num < 10.0)
                num = 0.0f;
            color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)MathHelper.Lerp((float)color.A, num, 0.99f));
            this.ScreenManager.SpriteBatch.Draw(texture3, new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), color);
            this.RenderOverFog(gameTime);
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.SpriteBatch.Begin();
            this.DrawPlanetInfo();
            if (this.LookingAtPlanet && this.SelectedPlanet != null && this.workersPanel != null)
                this.workersPanel.Draw(this.ScreenManager.SpriteBatch, gameTime);
            this.DrawShipsInRange();
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                try
                {
                    if (solarSystem.isVisible)
                    {
                        foreach (Planet planet in solarSystem.PlanetList)
                        {
                            foreach (Projectile projectile in (List<Projectile>)planet.Projectiles)
                            {
                                if (projectile.WeaponType != "Missile" && projectile.WeaponType != "Rocket" && projectile.WeaponType != "Drone")
                                    this.DrawTransparentModel(ResourceManager.ProjectileModelDict[projectile.modelPath], projectile.GetWorld(), this.view, this.projection, projectile.weapon.Animated != 0 ? ResourceManager.TextureDict[projectile.texturePath] : ResourceManager.ProjTextDict[projectile.texturePath], projectile.Scale);
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            this.DrawTacticalPlanetIcons();
            this.DrawFleetIcons(gameTime);
            if (!this.LookingAtPlanet)
                this.pieMenu.Draw(this.ScreenManager.SpriteBatch, Fonts.Arial12Bold);
            Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, this.SelectionBox, Color.Green, 1f);
            this.EmpireUI.Draw(this.ScreenManager.SpriteBatch);
            if (!this.LookingAtPlanet)
                this.DrawShipUI(gameTime);
            if (!this.LookingAtPlanet || this.LookingAtPlanet && this.workersPanel is UnexploredPlanetScreen || this.LookingAtPlanet && this.workersPanel is UnownedPlanetScreen)
            {
                Vector2 vector2_1 = new Vector2((float)(this.SectorMap.X + 75) - Fonts.Arial12Bold.MeasureString("Sector View").X / 2f, (float)(this.SectorMap.Y + 75 - Fonts.Arial12Bold.LineSpacing / 2));
                Vector2 vector2_2 = new Vector2((float)(this.GalaxyMap.X + 75) - Fonts.Arial12Bold.MeasureString("Galaxy View").X / 2f, (float)(this.GalaxyMap.Y + 75 - Fonts.Arial12Bold.LineSpacing / 2));
                this.DrawMinimap();
            }
            if (this.SelectedShipList.Count == 0)
                this.shipListInfoUI.ClearShipList();
            if (this.SelectedSystem != null && !this.LookingAtPlanet)
            {
                this.sInfoUI.SetSystem(this.SelectedSystem);
                this.sInfoUI.Update(gameTime);
                if (this.viewState == UniverseScreen.UnivScreenState.GalaxyView)
                    this.sInfoUI.Draw(gameTime);
            }
            if (this.SelectedPlanet != null && !this.LookingAtPlanet)
            {
                this.pInfoUI.SetPlanet(this.SelectedPlanet);
                this.pInfoUI.Update(gameTime);
                this.pInfoUI.Draw(gameTime);
            }
            else if (this.SelectedShip != null && !this.LookingAtPlanet)
            {
                this.ShipInfoUIElement.ship = this.SelectedShip;
                this.ShipInfoUIElement.ShipNameArea.Text = this.SelectedShip.VanityName;
                this.ShipInfoUIElement.Update(gameTime);
                this.ShipInfoUIElement.Draw(gameTime);
            }
            else if (this.SelectedShipList.Count > 1 && this.SelectedFleet == null)
            {
                this.shipListInfoUI.Update(gameTime);
                this.shipListInfoUI.Draw(gameTime);
            }
            else if (this.SelectedItem != null)
            {
                bool flag = false;
                for (int index = 0; index < this.SelectedItem.AssociatedGoal.empire.GetGSAI().Goals.Count; ++index)
                {
                    if (this.SelectedItem.AssociatedGoal.empire.GetGSAI().Goals[index].guid == this.SelectedItem.AssociatedGoal.guid)
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    string TitleText = "(" + ResourceManager.ShipsDict[this.SelectedItem.UID].Name + ")";
                    string BodyText = Localizer.Token(1410) + this.SelectedItem.AssociatedGoal.GetPlanetWhereBuilding().Name;
                    this.vuiElement.Draw(gameTime, TitleText, BodyText);
                    this.DrawItemInfoForUI();
                }
                else
                    this.SelectedItem = (UniverseScreen.ClickableItemUnderConstruction)null;
            }
            else if (this.SelectedFleet != null && !this.LookingAtPlanet)
            {
                this.shipListInfoUI.Update(gameTime);
                this.shipListInfoUI.Draw(gameTime);
            }
            if (this.SelectedShip == null || this.LookingAtPlanet)
                this.ShipInfoUIElement.ShipNameArea.HandlingInput = false;
            this.DrawToolTip();
            if (!this.LookingAtPlanet)
                this.NotificationManager.Draw();
            if (this.showingDSBW && !this.LookingAtPlanet)
            {
                lock (GlobalStats.ClickableSystemsLock)
                {
                    foreach (UniverseScreen.ClickablePlanets item_1 in this.ClickPlanetList)
                    {
                        float local_14 = 2500f * item_1.planetToClick.scale;
                        Vector3 local_15 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(item_1.planetToClick.Position.X, item_1.planetToClick.Position.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_16 = new Vector2(local_15.X, local_15.Y);
                        Vector3 local_18 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, item_1.planetToClick.Position, local_14), 0.0f), this.projection, this.view, Matrix.Identity);
                        float local_20 = Vector2.Distance(new Vector2(local_18.X, local_18.Y), local_16);
                        Rectangle local_21 = new Rectangle((int)local_16.X, (int)local_16.Y, (int)local_20 * 2, (int)local_20 * 2);
                        Vector2 local_22 = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node1"], local_21, new Rectangle?(), new Color((byte)0, (byte)0, byte.MaxValue, (byte)50), 0.0f, local_22, SpriteEffects.None, 1f);
                        Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, local_16, local_20, 50, new Color(byte.MaxValue, (byte)165, (byte)0, (byte)150), 3f);
                    }
                }
                this.dsbw.Draw(gameTime);
            }
            if (this.Debug && this.showdebugwindow)
                this.debugwin.Draw(gameTime);
            if (this.aw.isOpen && !this.LookingAtPlanet)
                this.aw.Draw(gameTime);
            if (this.Paused)
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(4005), new Vector2((float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, 45f), Color.White);
            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Hyperspace Flux", new Vector2((float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, (float)(45 + Fonts.Pirulen16.LineSpacing + 2)), Color.Yellow);
            if (SavedGame.thread != null && SavedGame.thread.IsAlive && this.IsActive)
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Saving...", new Vector2((float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Pirulen16.MeasureString(Localizer.Token(4005)).X / 2f, (float)(45 + Fonts.Pirulen16.LineSpacing * 2 + 4)), new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(Math.Abs((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds)) * (float)byte.MaxValue)));
            if (this.IsActive && (this.GameSpeed > 1.0f || this.GameSpeed < 1.0f))
            {
                Vector2 vector29 = new Vector2((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - Fonts.Pirulen16.MeasureString(string.Concat(this.GameSpeed, "x")).X - 13f, 44f);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, (this.GameSpeed < 1f ? string.Concat(this.GameSpeed.ToString("#.0"), "x") : string.Concat(this.GameSpeed, "x")), vector29, Color.White);
            }
            if (this.Debug)
            {
                Vector2 position = new Vector2((float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 250), 44f);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Comparisons: " + (object)GlobalStats.Comparisons, position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Dis Check Avg: " + (object)(GlobalStats.DistanceCheckTotal / GlobalStats.ComparisonCounter), position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Modules Moved: " + (object)GlobalStats.ModulesMoved, position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Modules Updated: " + (object)GlobalStats.ModuleUpdates, position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Arc Checks: " + (object)GlobalStats.WeaponArcChecks, position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Beam Tests: " + (object)GlobalStats.BeamTests, position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Memory: " + this.Memory.ToString(), position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Ship Count: " + this.MasterShipList.Count.ToString(), position, Color.White);
                position.Y += (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Perf Delay(s): " + this.perfavg2.Average().ToString("F"), position, Color.White);
            }
            if (this.IsActive)
                ToolTip.Draw(this.ScreenManager);
            this.ScreenManager.SpriteBatch.End();
            if (!this.MultiThread)
                return;
            this.WorkerBeginEvent.Set();
        }

        private void DrawCircle(Vector2 WorldPos, float radius, Color c, float thickness)
        {
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(WorldPos.X, WorldPos.Y, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector2 center = new Vector2(vector3_1.X, vector3_1.Y);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, WorldPos, radius), 0.0f), this.projection, this.view, Matrix.Identity);
            float radius1 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), center);
            Rectangle destinationRectangle = new Rectangle((int)center.X, (int)center.Y, (int)radius1 * 2, (int)radius1 * 2);
            Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node1"], destinationRectangle, new Rectangle?(), new Color(c, (byte)50), 0.0f, origin, SpriteEffects.None, 1f);
            Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, radius1, 50, c, thickness);
        }

        private void DrawFleetIcons(GameTime gameTime)
        {
            double totalSeconds = gameTime.ElapsedGameTime.TotalSeconds;
            //this.FleetPosUpdateTimer = 1f;
            this.ClickableFleetsList.Clear();
            if (this.viewState < UniverseScreen.UnivScreenState.SectorView)
                return;
            foreach (Empire empire in EmpireManager.EmpireList)
            {
                try
                {
                    foreach (KeyValuePair<int, Fleet> keyValuePair in empire.GetFleetsDict())
                    {
                        if (keyValuePair.Value.Ships.Count > 0)
                        {
                            bool flag = false;
                            Vector2 averagePosition = keyValuePair.Value.findAveragePositionset();


                            this.player.SensorNodeLocker.EnterReadLock();
                            {
                                foreach (Empire.InfluenceNode item_0 in (List<Empire.InfluenceNode>)this.player.SensorNodes)
                                {
                                    if ((double)Vector2.Distance(averagePosition, item_0.Position) <= (double)item_0.Radius)
                                        flag = true;
                                }
                            }
                            this.player.SensorNodeLocker.ExitReadLock();
                            if (flag || this.Debug || keyValuePair.Value.Owner == this.player)
                            {
                                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(averagePosition, 0.0f), this.projection, this.view, Matrix.Identity);
                                Vector2 vector2 = new Vector2(vector3_1.X, vector3_1.Y);
                                Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["FleetIcons/" + (object)keyValuePair.Value.FleetIconIndex].Width / 2), (float)(ResourceManager.TextureDict["FleetIcons/" + (object)keyValuePair.Value.FleetIconIndex].Width / 2));
                                foreach (Ship ship in (List<Ship>)keyValuePair.Value.Ships)
                                {
                                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Center.X, ship.Center.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(vector3_2.X, vector3_2.Y), vector2, new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)20));
                                }
                                this.ClickableFleetsList.Add(new UniverseScreen.ClickableFleet()
                                {
                                    fleet = keyValuePair.Value,
                                    ScreenPos = vector2,
                                    ClickRadius = 15f
                                });
                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["FleetIcons/" + (object)keyValuePair.Value.FleetIconIndex], vector2, new Rectangle?(), empire.EmpireColor, 0.0f, origin, 0.35f, SpriteEffects.None, 1f);
                                HelperFunctions.DrawDropShadowText(this.ScreenManager, keyValuePair.Value.Name, new Vector2(vector2.X + 10f, vector2.Y - 6f), Fonts.Arial8Bold);
                            }
                        }
                    }
                }
                catch
                {
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

        private void DrawOverlappingCirlcesLite(List<Circle> CircleList, Color color, float Thickness)
        {
            foreach (Circle circle in CircleList)
                ;
        }

        private void DrawSelectedShipGroup(List<Circle> CircleList, Color color, float Thickness)
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

        public void DrawOverlappingCirlces(List<Circle> CircleList, Color color, float Thickness)
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
            foreach (UniverseScreen.Intersection intersection in (List<UniverseScreen.Intersection>)removalCollection)
                ++num1;
            List<List<Vector2>> list1 = new List<List<Vector2>>();
            foreach (Circle circle in CircleList)
            {
                List<UniverseScreen.Intersection> list2 = new List<UniverseScreen.Intersection>();
                foreach (UniverseScreen.Intersection intersection in (List<UniverseScreen.Intersection>)removalCollection)
                {
                    if (intersection.C1 == circle || intersection.C2 == circle)
                        list2.Add(intersection);
                }
                foreach (UniverseScreen.Intersection intersection in list2)
                {
                    float num2 = Math.Abs(HelperFunctions.findAngleToTarget(circle.Center, intersection.inter)) - 90f;
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
                    List<Vector2> myArc = Primitives2D.CreateMyArc(this.ScreenManager.SpriteBatch, circle.Center, circle.Radius, 50, Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).Angle, Enumerable.ElementAt<UniverseScreen.Intersection>((IEnumerable<UniverseScreen.Intersection>)orderedEnumerable, index).AngularDistance, C0, C1, color, Thickness, CircleList);
                    list1.Add(myArc);
                }
            }
            List<Vector2> list3 = new List<Vector2>();
            foreach (List<Vector2> list2 in list1)
                list3.AddRange((IEnumerable<Vector2>)list2);
            foreach (Vector2 center in list3)
                Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, 2f, 10, color, 2f);
        }

        private Vector2[] Get2Intersections(Circle A, Circle B)
        {
            Vector2[] vector2Array = HelperFunctions.CircleIntersection(A, B);
            if (vector2Array == null)
                return (Vector2[])null;
            return new Vector2[2]
      {
        new Vector2(vector2Array[0].X, vector2Array[0].Y),
        new Vector2(vector2Array[1].X, vector2Array[1].Y)
      };
        }

        private void DrawCircleConnections(Circle A, List<Circle> Circles)
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
                        float startingAngle1 = Math.Abs(HelperFunctions.findAngleToTarget(A.Center, vector2_2)) - 90f;
                        Primitives2D.DrawMyArc(this.ScreenManager.SpriteBatch, A.Center, A.Radius, 50, startingAngle1, degrees1, vector2_1, vector2_2, Color.Red, 3f, Circles);
                        float num3 = MathHelper.ToDegrees((float)Math.Asin((double)num1 / 2.0 / (double)B.Radius) * 2f);
                        float startingAngle2 = Math.Abs(HelperFunctions.findAngleToTarget(B.Center, vector2_1)) - 90f;
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
                planet.UpdateIncomes();
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
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["FleetIcons/" + item_0.Fleet.FleetIconIndex.ToString()], local_2, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).EmpireColor);
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
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + local_9.Role], local_10, item_0.Fleet.Owner.EmpireColor);
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
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            lock (GlobalStats.KnownShipsLock)
            {
                
                for (int i = 0; i < this.player.KnownShips.Count; ++i)
                {
                    Ship ship = this.player.KnownShips[i];
                    if (ship != null)
                    {
                        if (!ship.Active)
                            this.MasterShipList.QueuePendingRemoval(ship);
                        else
                            this.DrawInRange(ship);
                    }
                }
            }
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            lock (GlobalStats.KnownShipsLock)
            {
                foreach (Ship ship in this.player.KnownShips)
                //Parallel.ForEach(this.player.KnownShips, ship =>
                {
                    if (ship.Active)
                    {
                        this.DrawTacticalIcons(ship);
                        this.DrawOverlay(ship);
                        if ((this.SelectedShipList.Contains(ship) || this.SelectedShip == ship) && HelperFunctions.CheckIntersection(this.ScreenRectangle, ship.ScreenPosition))
                        {
                            //Color local_3 = new Color();
                            if (!this.player.GetRelations().ContainsKey(ship.loyalty))
                            {
                                Color local_3_1 = Color.LightGreen;
                                Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, ship.ScreenPosition, ship.ScreenRadius, local_3_1);
                            }
                            else
                            {
                                Color local_3_2 = this.player.GetRelations()[ship.loyalty].AtWar || ship.loyalty.isFaction ? Color.Red : Color.Gray;
                                Primitives2D.BracketRectangle(this.ScreenManager.SpriteBatch, ship.ScreenPosition, ship.ScreenRadius, local_3_2);
                            }
                        }
                    }
                }//);
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
            foreach (Ship ship in (List<Ship>)this.projectedGroup.Ships)
            {
                Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.projectedPosition.X, ship.projectedPosition.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 position = new Vector2(vector3.X, vector3.Y);
                Rectangle rectangle = new Rectangle((int)vector3.X, (int)vector3.Y, ship.Size / 2, ship.Size / 2);
                float num = (float)ship.Size / (float)(30 + ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width);
                Vector2 origin = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
                if (ship.Active)
                {
                    float scale = num * 4000f / this.camHeight;
                    if ((double)scale > 1.0)
                        scale = 1f;
                    if ((double)scale < 0.100000001490116)
                        scale = !(ship.Role == "platform") || this.viewState < UniverseScreen.UnivScreenState.SectorView ? 0.15f : 0.08f;
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + ship.Role], position, new Rectangle?(), new Color((byte)0, byte.MaxValue, (byte)0, (byte)100), this.projectedGroup.ProjectedFacing, origin, scale, SpriteEffects.None, 1f);
                }
            }
        }

        private void DrawOverlay(Ship ship)
        {
            if (this.LookingAtPlanet || this.viewState > UniverseScreen.UnivScreenState.SystemView || (!this.ShowShipNames || ship.dying) || !ship.InFrustum)
                return;
            Vector2 vector2_1 = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
            foreach (ModuleSlot moduleSlot in ship.ModuleSlotList)
            {
                Vector2 vector2_2 = new Vector2(moduleSlot.module.Center.X, moduleSlot.module.Center.Y);
                float scale = 0.75f * (float)this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / this.camHeight;
                Vector3 vector3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(vector2_2, 0.0f), this.projection, this.view, Matrix.Identity);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], new Vector2(vector3.X, vector3.Y), new Rectangle?(), Color.White, ship.Rotation, new Vector2(8f, 8f), scale, SpriteEffects.None, 1f);
            }
            bool flag = false;
            if (flag)
            {
                foreach (Projectile projectile in (List<Projectile>)ship.Projectiles)
                {
                    Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(projectile.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 center = new Vector2(vector3_1.X, vector3_1.Y);
                    Vector2 vector2_2 = projectile.Center + new Vector2(8f, 0.0f);
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(vector2_2.X, vector2_2.Y, 0.0f), this.projection, this.view, Matrix.Identity);
                    float radius = Vector2.Distance(center, new Vector2(vector3_2.X, vector3_2.Y));
                    Primitives2D.DrawCircle(this.ScreenManager.SpriteBatch, center, radius, 50, Color.Red, 3f);
                }
            }
            Viewport viewport;
            foreach (ModuleSlot moduleSlot in ship.ModuleSlotList)
            {
                if ((double)this.camHeight > 6000.0)
                {
                    string index = "TacticalIcons/symbol_fighter";
                    viewport = this.ScreenManager.GraphicsDevice.Viewport;
                    Vector3 vector3_1 = viewport.Project(new Vector3(moduleSlot.module.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                    Vector2 origin1 = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Height / 2));
                    float num1 = moduleSlot.module.Health / moduleSlot.module.HealthMax;
                    float scale = 500f / this.camHeight;
                    if (this.Debug && moduleSlot.module.isExternal)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Blue, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    else if ((double)num1 >= 0.899999976158142)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Green, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    else if ((double)num1 >= 0.649999976158142)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.GreenYellow, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    else if ((double)num1 >= 0.449999988079071)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Yellow, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    else if ((double)num1 >= 0.150000005960464)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.OrangeRed, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    else if ((double)num1 <= 0.150000005960464 && (double)num1 > 0.0)
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Red, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    else
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[index], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Black, ship.Rotation, origin1, scale, SpriteEffects.None, 1f);
                    if (ship.isPlayerShip() && (double)moduleSlot.module.FieldOfFire != 0.0 && moduleSlot.module.InstalledWeapon != null)
                    {
                        float num2 = moduleSlot.module.FieldOfFire / 2f;
                        Vector2 angleAndDistance1 = this.findPointFromAngleAndDistance(moduleSlot.module.Center, (float)((double)MathHelper.ToDegrees(ship.Rotation) + (double)moduleSlot.module.facing + -(double)num2), moduleSlot.module.InstalledWeapon.Range);
                        Vector2 angleAndDistance2 = this.findPointFromAngleAndDistance(moduleSlot.module.Center, MathHelper.ToDegrees(ship.Rotation) + moduleSlot.module.facing + num2, moduleSlot.module.InstalledWeapon.Range);
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
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, MathHelper.ToRadians(moduleSlot.module.facing) + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "Laser" || moduleSlot.module.InstalledWeapon.WeaponType == "HeavyLaser")
                        {
                            Color color2 = new Color(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num3 * 2, (int)num3 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, MathHelper.ToRadians(moduleSlot.module.facing) + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "PhotonCannon")
                        {
                            Color color2 = new Color((byte)0, (byte)0, byte.MaxValue, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num3 * 2, (int)num3 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, MathHelper.ToRadians(moduleSlot.module.facing) + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
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
                    if (this.Debug && moduleSlot.module.isExternal && moduleSlot.module.Active)
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
                    float angleToTarget = this.findAngleToTarget(vector2_2, target);
                    Vector2 angleAndDistance1 = this.findPointFromAngleAndDistance(moduleSlot.module.Center, MathHelper.ToDegrees(ship.Rotation) - angleToTarget, 8f * (float)Math.Sqrt(2.0));
                    float num4 = (float)((int)num1 * 16 / 2);
                    float num5 = (float)((int)num2 * 16 / 2);
                    float distance = (float)Math.Sqrt((double)((float)Math.Pow((double)num4, 2.0) + (float)Math.Pow((double)num5, 2.0)));
                    float radians = 3.141593f - (float)Math.Asin((double)num4 / (double)distance) + ship.Rotation;
                    vector2_2 = this.findPointFromAngleAndDistance(angleAndDistance1, MathHelper.ToDegrees(radians), distance);
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
                        Vector2 angleAndDistance2 = this.findPointFromAngleAndDistance(vector2_2, (float)((double)MathHelper.ToDegrees(ship.Rotation) + (double)moduleSlot.module.facing + -(double)num7), moduleSlot.module.InstalledWeapon.Range);
                        Vector2 angleAndDistance3 = this.findPointFromAngleAndDistance(vector2_2, MathHelper.ToDegrees(ship.Rotation) + moduleSlot.module.facing + num7, moduleSlot.module.InstalledWeapon.Range);
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
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, MathHelper.ToRadians(moduleSlot.module.facing) + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "Laser" || moduleSlot.module.InstalledWeapon.WeaponType == "HeavyLaser")
                        {
                            Color color2 = new Color(byte.MaxValue, (byte)0, (byte)0, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num8 * 2, (int)num8 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, MathHelper.ToRadians(moduleSlot.module.facing) + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
                        }
                        else if (moduleSlot.module.InstalledWeapon.WeaponType == "PhotonCannon")
                        {
                            Color color2 = new Color((byte)0, (byte)0, byte.MaxValue, byte.MaxValue);
                            Rectangle destinationRectangle = new Rectangle((int)point1.X, (int)point1.Y, (int)num8 * 2, (int)num8 * 2);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc90"], destinationRectangle, new Rectangle?(), color2, MathHelper.ToRadians(moduleSlot.module.facing) + ship.Rotation, origin2, SpriteEffects.None, (float)(1.0 - (double)moduleSlot.module.InstalledWeapon.Range / 99999.0));
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
            if (this.LookingAtPlanet || ship.IsPlatform && this.viewState == UniverseScreen.UnivScreenState.GalaxyView)
                return;
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
                if (!ship.Active || !flag)
                    return;
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + ship.Role], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
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
                if(ResourceManager.TextureDict.ContainsKey("TacticalIcons/symbol_" + ship.Role))
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_" + ship.Role], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
                else
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"], position, new Rectangle?(), ship.loyalty.EmpireColor, ship.Rotation, origin, scale, SpriteEffects.None, 1f);
            }
            else
            {
                if (this.viewState != UniverseScreen.UnivScreenState.ShipView || this.LookingAtPlanet)
                    return;
                float num1 = ship.GetSO().WorldBoundingSphere.Radius;
                Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(ship.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                Vector2 vector2_1 = new Vector2(vector3_1.X, vector3_1.Y);
                Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(new Vector2(ship.Center.X + num1, ship.Center.Y), 0.0f), this.projection, this.view, Matrix.Identity);
                float num2 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2_1);
                if ((double)num2 < 5.0)
                    num2 = 5f;
                float scale = num2 / (float)(45 - GlobalStats.IconSize); //45
                Vector2 vector2_2 = new Vector2((float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
                if ((double)ship.OrdinanceMax <= 0.0)
                    return;
                if ((double)ship.Ordinance < 0.200000002980232 * (double)ship.OrdinanceMax)
                {
                    Vector2 position = new Vector2(vector2_1.X + 15f * scale, vector2_1.Y + 15f * scale);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_ammo"], position, new Rectangle?(), Color.Red, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
                else
                {
                    if ((double)ship.Ordinance >= 0.5 * (double)ship.OrdinanceMax)
                        return;
                    Vector2 position = new Vector2(vector2_1.X + 15f * scale, vector2_1.Y + 15f * scale);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_ammo"], position, new Rectangle?(), Color.Yellow, 0.0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }
            }
        }

        private void DrawBombs()
        {
            lock (GlobalStats.BombLock)
            {
                try
                {
                    foreach (Bomb item_0 in (List<Bomb>)this.BombList)
                        this.DrawTransparentModel(item_0.GetModel(), item_0.GetWorld(), this.view, this.projection, item_0.GetTexture(), 0.5f);
                }
                catch
                {
                }
            }
        }

        protected void DrawInRange(Ship ship)
        {
            if (this.viewState > UniverseScreen.UnivScreenState.SystemView)
                return;
            try
            {
                foreach (Projectile projectile in (List<Projectile>)ship.Projectiles)
                {
                    if (this.Frustum.Contains(new Vector3(projectile.Center, 0.0f)) != ContainmentType.Disjoint && projectile.WeaponType != "Missile" && (projectile.WeaponType != "Rocket" && projectile.WeaponType != "Drone"))
                        this.DrawTransparentModel(ResourceManager.ProjectileModelDict[projectile.modelPath], projectile.GetWorld(), this.view, this.projection, projectile.weapon.Animated != 0 ? ResourceManager.TextureDict[projectile.texturePath] : ResourceManager.ProjTextDict[projectile.texturePath], projectile.Scale);
                }
            }
            catch
            {
            }
        }

        private Circle DrawSelectionCircles(Vector2 WorldPos, float WorldRadius)
        {
            float radius = WorldRadius;
            Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(WorldPos, 0.0f), this.projection, this.view, Matrix.Identity);
            Vector2 Center = new Vector2(vector3_1.X, vector3_1.Y);
            Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, WorldPos, radius), 0.0f), this.projection, this.view, Matrix.Identity);
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
            Rectangle rect = new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            lock (GlobalStats.KnownShipsLock)
            {
                for (int local_1 = 0; local_1 < EmpireManager.GetEmpireByName(this.PlayerLoyalty).KnownShips.Count; ++local_1)
                {
                    Ship local_2 = EmpireManager.GetEmpireByName(this.PlayerLoyalty).KnownShips[local_1];
                    if (local_2 != null && local_2.Active && (this.viewState != UniverseScreen.UnivScreenState.GalaxyView || !local_2.IsPlatform))
                    {
                        float local_4 = local_2.GetSO().WorldBoundingSphere.Radius;
                        Vector3 local_6 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(local_2.Position, 0.0f), this.projection, this.view, Matrix.Identity);
                        Vector2 local_7 = new Vector2(local_6.X, local_6.Y);
                        if (HelperFunctions.CheckIntersection(rect, local_7))
                        {
                            Vector3 local_9 = new Vector3(this.GeneratePointOnCircle(90f, local_2.Position, local_4), 0.0f);
                            Vector3 local_10 = this.ScreenManager.GraphicsDevice.Viewport.Project(local_9, this.projection, this.view, Matrix.Identity);
                            Vector2 local_11 = new Vector2(local_10.X, local_10.Y);
                            float local_12 = Vector2.Distance(local_11, local_7);
                            if ((double)local_12 < 7.0)
                                local_12 = 7f;
                            local_2.ScreenRadius = local_12;
                            local_2.ScreenPosition = local_7;
                            this.ClickableShipsList.Add(new UniverseScreen.ClickableShip()
                            {
                                Radius = local_12,
                                ScreenPos = local_7,
                                shipToClick = local_2
                            });
                            if (local_2.loyalty == this.player || local_2.loyalty != this.player && this.player.GetRelations()[local_2.loyalty].Treaty_Alliance)
                            {
                                local_9 = new Vector3(this.GeneratePointOnCircle(90f, local_2.Position, local_2.SensorRange), 0.0f);
                                local_10 = this.ScreenManager.GraphicsDevice.Viewport.Project(local_9, this.projection, this.view, Matrix.Identity);
                                local_11 = new Vector2(local_10.X, local_10.Y);
                                local_2.ScreenSensorRadius = Vector2.Distance(local_11, local_7);
                            }
                        }
                        else
                            local_2.ScreenPosition = new Vector2(-1f, -1f);
                    }
                }
            }
            lock (GlobalStats.ClickableSystemsLock)
            {
                this.ClickPlanetList.Clear();
                this.ClickableSystems.Clear();
            }
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                Vector3 vector3_1 = new Vector3(solarSystem.Position, 0.0f);
                if (this.Frustum.Contains(new BoundingSphere(vector3_1, 100000f)) != ContainmentType.Disjoint)
                {
                    Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(vector3_1, this.projection, this.view, Matrix.Identity);
                    Vector2 vector2 = new Vector2(vector3_2.X, vector3_2.Y);
                    Vector3 vector3_3 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, solarSystem.Position, 4500f), 0.0f), this.projection, this.view, Matrix.Identity);
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
                            if (solarSystem.ExploredDict[EmpireManager.GetEmpireByName(this.PlayerLoyalty)])
                            {
                                float radius = planet.SO.WorldBoundingSphere.Radius;
                                Vector3 vector3_4 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position, 2500f), this.projection, this.view, Matrix.Identity);
                                Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
                                Vector3 vector3_5 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, planet.Position, radius), 2500f), this.projection, this.view, Matrix.Identity);
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
                        if (solarSystem.ExploredDict[EmpireManager.GetEmpireByName(this.PlayerLoyalty)])
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
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
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
                        Vector3 vector3_5 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, solarSystem.Position, 25000f), 0.0f), this.projection, this.view, Matrix.Identity);
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
                                            if (planet.BuildingList[index].EventTriggerUID != "")
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
                                        HelperFunctions.DrawDropShadowText(this.ScreenManager, solarSystem.Name, vector2, SystemInfoUIElement.SysFont, solarSystem.OwnerList[0].EmpireColor);
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
                                        HelperFunctions.DrawDropShadowText(this.ScreenManager, solarSystem.Name[index2].ToString(), Pos, SystemInfoUIElement.SysFont, solarSystem.OwnerList.Count > index1 ? solarSystem.OwnerList[index1].EmpireColor : Enumerable.Last<Empire>((IEnumerable<Empire>)solarSystem.OwnerList).EmpireColor);
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
                                            if (planet.BuildingList[index].EventTriggerUID != "")
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
            lock (GlobalStats.KnownShipsLock)
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
            float num = (float)(150.0 * (double)this.SelectedSomethingTimer / 3.0);
            if ((double)num < 0.0)
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
                    if (this.SelectedShip.GetAI().State == AIState.Rebase || this.SelectedShip.GetAI().State == AIState.AssaultPlanet)
                    {
                        lock (GlobalStats.WayPointLock)
                        {
                            for (int local_23 = 0; local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count; ++local_23)
                            {
                                if (local_23 == 0)
                                {
                                    Vector3 local_24 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.Center, 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_25 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.Peek(), 0.0f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_24.X, local_24.Y), new Vector2(local_25.X, local_25.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                }
                                if (local_23 < this.SelectedShip.GetAI().ActiveWayPoints.Count - 1)
                                {
                                    Vector3 local_26 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23], 0.0f), this.projection, this.view, Matrix.Identity);
                                    Vector3 local_27 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.SelectedShip.GetAI().ActiveWayPoints.ToArray()[local_23 + 1], 2500f), this.projection, this.view, Matrix.Identity);
                                    Primitives2D.DrawLine(this.ScreenManager.SpriteBatch, new Vector2(local_26.X, local_26.Y), new Vector2(local_27.X, local_27.Y), new Color((byte)0, byte.MaxValue, byte.MaxValue, (byte)num));
                                }
                            }
                        }
                    }
                    if (this.SelectedShip.GetAI().ActiveWayPoints.Count > 0 && (this.SelectedShip.GetAI().State == AIState.MoveTo || this.SelectedShip.GetAI().State == AIState.PassengerTransport || this.SelectedShip.GetAI().State == AIState.SystemTrader))
                    {
                        lock (GlobalStats.WayPointLock)
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
                        if (!flag)
                        {
                            if (ship.GetAI().ActiveWayPoints.Count > 0)
                            {
                                lock (GlobalStats.WayPointLock)
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
            for (int index1 = 0; index1 < this.player.KnownShips.Count; ++index1)
            {
                try
                {
                    Ship ship = this.player.KnownShips[index1];
                    foreach (Projectile projectile in (List<Projectile>)ship.Projectiles)
                    {
                        if (projectile.weapon.IsRepairDrone && projectile.GetDroneAI() != null)
                        {
                            for (int index2 = 0; index2 < projectile.GetDroneAI().Beams.Count; ++index2)
                                projectile.GetDroneAI().Beams[index2].Draw(this.ScreenManager);
                        }
                    }
                    if (this.viewState < UniverseScreen.UnivScreenState.SectorView)
                    {
                        foreach (Beam beam in (List<Beam>)ship.Beams)
                        {
                            if ((double)Vector2.Distance(beam.Source, beam.ActualHitDestination) < (double)beam.range + 10.0)
                                beam.Draw(this.ScreenManager);
                            //else
                            //    beam.Die((GameplayObject)null, true);
                        }
                    }
                }
                catch
                {
                }
            }
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            foreach (Anomaly anomaly in (List<Anomaly>)this.anomalyManager.AnomaliesList)
                anomaly.Draw();
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (solarSystem.ExploredDict[this.player] && solarSystem.isVisible && (double)this.camHeight < 150000.0)
                {
                    foreach (Planet p in solarSystem.PlanetList)
                    {
                        if (this.Frustum.Contains(p.SO.WorldBoundingSphere) != ContainmentType.Disjoint)
                        {
                            if (p.hasEarthLikeClouds)
                            {
                                this.DrawClouds(this.xnaPlanetModel, p.cloudMatrix, this.view, this.projection, p);
                                this.DrawAtmo(this.xnaPlanetModel, p.cloudMatrix, this.view, this.projection, p);
                            }
                            if (p.hasRings)
                                this.DrawRings(p.RingWorld, this.view, this.projection, p.scale);
                        }
                    }
                }
            }
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            this.RenderThrusters();
            this.RenderParticles();
            lock (FTLManager.FTLLock)
            {
                for (int local_69 = 0; local_69 < FTLManager.FTLList.Count; ++local_69)
                {
                    FTL local_70 = FTLManager.FTLList[local_69];
                    if (local_70 != null)
                        this.DrawTransparentModel(this.SunModel, local_70.WorldMatrix, this.view, this.projection, FTLManager.FTLTexture, (float)((double)local_70.scale * 1.0 / 50.0));
                }
                FTLManager.FTLList.ApplyPendingRemovals();
            }
            lock (GlobalStats.ExplosionLocker)
            {
                foreach (MuzzleFlash item_4 in (List<MuzzleFlash>)MuzzleFlashManager.FlashList)
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
            if (!this.Paused)
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
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        protected void DrawShields()
        {
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            ShieldManager.Draw(this.view, this.projection);
        }

        protected virtual void DrawPlanetInfo()
        {
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (this.viewState <= UniverseScreen.UnivScreenState.SectorView && solarSystem.isVisible)
                {
                    foreach (Planet planet in solarSystem.PlanetList)
                    {
                        float radius = planet.SO.WorldBoundingSphere.Radius;
                        Vector3 vector3_1 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(planet.Position, 2500f), this.projection, this.view, Matrix.Identity);
                        Vector2 vector2_1 = new Vector2(vector3_1.X, vector3_1.Y);
                        Vector3 vector3_2 = this.ScreenManager.GraphicsDevice.Viewport.Project(new Vector3(this.GeneratePointOnCircle(90f, planet.Position, radius), 2500f), this.projection, this.view, Matrix.Identity);
                        float num1 = Vector2.Distance(new Vector2(vector3_2.X, vector3_2.Y), vector2_1) + 10f;
                        Vector2 vector2_2 = new Vector2(vector3_1.X, vector3_1.Y - num1);
                        if (planet.ExploredDict[this.player])
                        {
                            if (!this.LookingAtPlanet && this.viewState < UniverseScreen.UnivScreenState.SectorView && this.viewState > UniverseScreen.UnivScreenState.ShipView)
                            {
                                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/planetNamePointer"], new Vector2(vector3_1.X, vector3_1.Y), new Rectangle?(), Color.Green, 0.0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
                                Vector2 pos1 = new Vector2(vector3_1.X + 20f, vector3_1.Y + 37f);
                                HelperFunctions.ClampVectorToInt(ref pos1);
                                if (planet.Owner == null)
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma10, planet.Name, pos1, Color.White);
                                else
                                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Tahoma10, planet.Name, pos1, planet.Owner.EmpireColor);
                                Vector2 pos2 = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
                                int num2 = 0;
                                Vector2 vector2_3 = new Vector2(vector3_1.X + 10f, vector3_1.Y + 60f);
                                if (planet.RecentCombat)
                                {
                                    Rectangle rectangle = new Rectangle((int)vector2_3.X, (int)vector2_3.Y, 14, 14);
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_fighting_small"], rectangle, Color.White);
                                    if (HelperFunctions.CheckIntersection(rectangle, pos2))
                                        ToolTip.CreateTooltip(119, this.ScreenManager);
                                    ++num2;
                                }
                                if (this.player.data.MoleList.Count > 0)
                                {
                                    foreach (Mole mole in (List<Mole>)this.player.data.MoleList)
                                    {
                                        if (mole.PlanetGuid == planet.guid)
                                        {
                                            vector2_3.X = vector2_3.X + (float)(18 * num2);
                                            Rectangle rectangle = new Rectangle((int)vector2_3.X, (int)vector2_3.Y, 14, 14);
                                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy_small"], rectangle, Color.White);
                                            ++num2;
                                            if (HelperFunctions.CheckIntersection(rectangle, pos2))
                                            {
                                                ToolTip.CreateTooltip(120, this.ScreenManager);
                                                break;
                                            }
                                            else
                                                break;
                                        }
                                    }
                                }
                                foreach (Building building in planet.BuildingList)
                                {
                                    if (building.EventTriggerUID != "")
                                    {
                                        vector2_3.X = vector2_3.X + (float)(18 * num2);
                                        Rectangle rectangle = new Rectangle((int)vector2_3.X, (int)vector2_3.Y, 14, 14);
                                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_anomaly_small"], rectangle, Color.White);
                                        if (HelperFunctions.CheckIntersection(rectangle, pos2))
                                        {
                                            ToolTip.CreateTooltip(121, this.ScreenManager);
                                            break;
                                        }
                                        else
                                            break;
                                    }
                                }
                            }
                        }
                        else if ((double)this.camHeight < 50000.0)
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

        protected Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
        {
            return this.findPointFromAngleAndDistance(center, angle, radius);
        }

        protected Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
        {
            Vector2 vector2 = new Vector2(0.0f, 0.0f);
            float num1 = angle;
            float num2 = distance;
            int num3 = 0;
            float num4 = 0.0f;
            float num5 = 0.0f;
            if ((double)num1 > 360.0)
                num1 -= 360f;
            if ((double)num1 < 90.0)
            {
                float num6 = (float)((double)(90f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 1;
            }
            else if ((double)num1 > 90.0 && (double)num1 < 180.0)
            {
                float num6 = (float)((double)(num1 - 90f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 2;
            }
            else if ((double)num1 > 180.0 && (double)num1 < 270.0)
            {
                float num6 = (float)((double)(270f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 3;
            }
            else if ((double)num1 > 270.0 && (double)num1 < 360.0)
            {
                float num6 = (float)((double)(num1 - 270f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 4;
            }
            if ((double)num1 == 0.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y - num2;
            }
            if ((double)num1 == 90.0)
            {
                vector2.X = position.X + num2;
                vector2.Y = position.Y;
            }
            if ((double)num1 == 180.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y + num2;
            }
            if ((double)num1 == 270.0)
            {
                vector2.X = position.X - num2;
                vector2.Y = position.Y;
            }
            if (num3 == 1)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y - num4;
            }
            else if (num3 == 2)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 3)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 4)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y - num4;
            }
            return vector2;
        }

        protected void DrawTransparentModel(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex)
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World = Matrix.CreateScale(50f) * world;
                    basicEffect.View = view;
                    basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                    basicEffect.Texture = projTex;
                    basicEffect.TextureEnabled = true;
                    basicEffect.Projection = projection;
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        protected void DrawTransparentModelAdditiveNoAlphaFade(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex, float scale)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (BasicEffect basicEffect in modelMesh.Effects)
                {
                    basicEffect.World = world;
                    basicEffect.View = view;
                    basicEffect.Texture = projTex;
                    basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
                    basicEffect.TextureEnabled = true;
                    basicEffect.Projection = projection;
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
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        protected void DrawTransparentModel(Model model, Matrix world, Matrix view, Matrix projection, Texture2D projTex, float scale, Vector3 Color)
        {
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            this.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            this.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            this.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
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
            this.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
        }

        public static FileInfo[] GetFilesFromDirectory(string DirPath)
        {
            return new DirectoryInfo(DirPath).GetFiles("*.*", SearchOption.AllDirectories);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        public float findAngleToTarget(Vector2 origin, Vector2 target)
        {
            float num1 = target.X;
            float num2 = target.Y;
            float num3 = origin.X;
            float num4 = origin.Y;
            float num5 = 0.0f;
            if ((double)num1 > (double)num3 && (double)num2 < (double)num4)
                num5 = 90f - Math.Abs((float)(Math.Atan(((double)num2 - (double)num4) / ((double)num1 - (double)num3)) * 180.0 / 3.14159274101257));
            else if ((double)num1 > (double)num3 && (double)num2 > (double)num4)
                num5 = 90f + (float)(Math.Atan(((double)num2 - (double)num4) / ((double)num1 - (double)num3)) * 180.0 / 3.14159274101257);
            else if ((double)num1 < (double)num3 && (double)num2 > (double)num4)
                num5 = -(270f - Math.Abs((float)(Math.Atan(((double)num2 - (double)num4) / ((double)num1 - (double)num3)) * 180.0 / 3.14159274101257)));
            else if ((double)num1 < (double)num3 && (double)num2 < (double)num4)
                num5 = -(270f + (float)(Math.Atan(((double)num2 - (double)num4) / ((double)num1 - (double)num3)) * 180.0 / 3.14159274101257));
            if ((double)num1 == (double)num3 && (double)num2 < (double)num4)
                num5 = 0.0f;
            else if ((double)num1 > (double)num3 && (double)num2 == (double)num4)
                num5 = 90f;
            else if ((double)num1 == (double)num3 && (double)num2 > (double)num4)
                num5 = 180f;
            else if ((double)num1 < (double)num3 && (double)num2 == (double)num4)
                num5 = 270f;
            return num5;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            lock (this)
            {
                if (this.starfield == null)
                    return;
                this.starfield.Dispose();
                this.starfield = (Starfield)null;
            }
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
                    returnZ = 4500f; 
                    break;
                case UnivScreenState.SystemView:
                    returnZ = 500000;
                    break;
                case UnivScreenState.SectorView:
                    returnZ = 1000000;
                    break;
                case UnivScreenState.GalaxyView:
                    returnZ = this.MaxCamHeight;
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
