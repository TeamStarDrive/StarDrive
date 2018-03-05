using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
    public sealed partial class UniverseScreen : GameScreen
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
        public static readonly SpatialManager SpaceManager = new SpatialManager();
        public static Array<SolarSystem> SolarSystemList = new Array<SolarSystem>();
        public static BatchRemovalCollection<SpaceJunk> JunkList = new BatchRemovalCollection<SpaceJunk>();
        public static bool DisableClicks = false;
        //private static string fmt = "00000.##";
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
        private Array<ClickableSystem> ClickableSystems    = new Array<ClickableSystem>();
        public BatchRemovalCollection<Ship> SelectedShipList = new BatchRemovalCollection<Ship>();
        private Array<ClickableShip> ClickableShipsList    = new Array<ClickableShip>();
        private Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();
        public Background bg            = new Background();
        public float UniverseSize       = 5000000f; // universe width and height in world units
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
        public float CamHeight = 2550f;
        public Vector3 CamPos = Vector3.Zero;
        private float TooltipTimer = 0.5f;
        private float sTooltipTimer = 0.5f;
        private float TimerDelay = 0.25f;
        private GameTime zgameTime = new GameTime();
        public Array<ShipModule> ModulesNeedingReset = new Array<ShipModule>();
        private bool TurnFlip = true;
        private float TurnFlipCounter = 0;
        private int Auto = 1;
        private AutoResetEvent   ShipGateKeeper         = new AutoResetEvent(false);
        private ManualResetEvent SystemThreadGateKeeper = new ManualResetEvent(false);
        private AutoResetEvent   DeepSpaceGateKeeper    = new AutoResetEvent(false);
        private ManualResetEvent DeepSpaceDone          = new ManualResetEvent(false);
        private AutoResetEvent   EmpireGateKeeper       = new AutoResetEvent(false);
        private ManualResetEvent EmpireDone             = new ManualResetEvent(false);
        private Array<Ship> DeepSpaceShips  = new Array<Ship>();
        public bool ViewingShip             = false;
        public float transDuration          = 3f;
        private float SectorMiniMapHeight = 20000f;
        public Vector2 mouseWorldPos;
        public float SelectedSomethingTimer = 3f;
        private Array<FleetButton> FleetButtons = new Array<FleetButton>();
        //private Vector2 startDrag;
        //private Vector2 ProjectedPosition;
        private float desiredSectorZ = 20000f;
        public Array<FogOfWarNode> FogNodes = new Array<FogOfWarNode>();
        private bool drawBloom = GlobalStats.RenderBloom; //true
        private Array<ClickableFleet> ClickableFleetsList = new Array<ClickableFleet>();
        public bool ShowTacticalCloseup { get; private set; }
        public bool Debug;
        public bool GridOn;
        public Planet SelectedPlanet;
        public Ship SelectedShip;
        public ClickableItemUnderConstruction SelectedItem;
        private PieMenu pieMenu;
        private PieMenuNode planetMenu;
        private PieMenuNode shipMenu;
        private float PieMenuTimer;
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
        private Model SunModel;
        private Model NebModel;
        public Model xnaPlanetModel;
        public Texture2D RingTexture;
        public AudioListener Listener;
        public UnivScreenState viewState;
        public bool LookingAtPlanet;
        public bool snappingToShip;
        public bool returnToShip;
        public Vector3 CamDestination;
        public Texture2D cloudTex;
        public EmpireUIOverlay EmpireUI;
        public BloomComponent bloomComponent;
        public Texture2D FogMap;
        private RenderTarget2D FogMapTarget;
        public RenderTarget2D MainTarget;
        public RenderTarget2D MiniMapSector;
        public RenderTarget2D BorderRT;
        public RenderTarget2D StencilRT;
        private RenderTarget2D LightsTarget;
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
        //public MinimapButtons mmButtons;
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
        private float Zrotate;
        public BoundingFrustum Frustum;
        private ClickablePlanets tippedPlanet;
        private ClickableSystem tippedSystem;
        private bool ShowingSysTooltip;
        private bool ShowingPlanetToolTip;
        private float ClickTimer;
        private float ClickTimer2;
        private float zTime;
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
        //public InputState Input;
        private float Memory;
        public bool Paused;
        public bool SkipRightOnce;
        private bool UseRealLights = true;
        public bool showdebugwindow;
        private bool NeedARelease;
        public SolarSystem SelectedSystem;
        public Fleet SelectedFleet;
        //private Array<Fleet.Squad> SelectedFlank;
        private int FBTimer;
        private bool pickedSomethingThisFrame;
        //private Vector2 startDragWorld;
        //private Vector2 endDragWorld;
        private ShipGroup projectedGroup;
        private bool ProjectingPosition;
        private bool SelectingWithBox;
        private Effect AtmoEffect;
        private Model atmoModel;
        public PlanetScreen workersPanel;
        private ResolveTexture2D sceneMap;
        private CursorState cState;
        private float radlast;
        private int SelectorFrame;
        public int globalshipCount;
        public int empireShipCountReserve;
        //private float ztimeSnapShot;          //Not referenced in code, removing to save memory
        //public ConcurrentBag<Ship> ShipsToRemove = new  ConcurrentBag<Ship>();
        private Array<GameplayObject> GamePlayObjectToRemove = new Array<GameplayObject>();
        private Array<Ship> ShipsToAddToWorld = new Array<Ship>();
        public float Lag = 0;
        public Ship previousSelection;

        public UIButton ShipsInCombat;    
        public UIButton PlanetsInCombat;
        public int lastshipcombat   = 0;
        public int lastplanetcombat = 0;
        public int reducer          = 1;
        public float screenDelay    = 0f;

        // for really specific debuggingD
        public static int FrameId;
        
        private UniverseScreen() : base(null)
        {
        }

        public UniverseScreen(UniverseData data) : base(null) // new game
        {
            UniverseSize                = data.Size.X;
            FTLModifier                 = data.FTLSpeedModifier;
            EnemyFTLModifier            = data.EnemyFTLSpeedModifier;
            GravityWells                = data.GravityWells;
            SolarSystemList             = data.SolarSystemsList;
            MasterShipList              = data.MasterShipList;
            playerShip                  = data.playerShip;
            PlayerEmpire                = playerShip.loyalty;
            PlayerLoyalty               = playerShip.loyalty.data.Traits.Name;
            ShipToView                  = playerShip;
            PlayerEmpire.isPlayer       = true;            
            SpaceManager.Setup(UniverseSize);
        }

        public UniverseScreen(UniverseData data, string loyalty) : base(null) // savegame
        {
            UniverseSize          = data.Size.X;
            FTLModifier           = data.FTLSpeedModifier;
            EnemyFTLModifier      = data.EnemyFTLSpeedModifier;
            GravityWells          = data.GravityWells;
            SolarSystemList       = data.SolarSystemsList;
            MasterShipList        = data.MasterShipList;
            loadFogPath           = data.loadFogPath;
            playerShip            = data.playerShip;
            PlayerEmpire          = EmpireManager.GetEmpireByName(loyalty);
            PlayerLoyalty         = loyalty;
            ShipToView            = playerShip;
            PlayerEmpire.isPlayer = true;
            loading               = true;
            SpaceManager.Setup(UniverseSize);
        }

        public void ResetLighting() => SetLighting(UseRealLights);

        private void SetLighting(bool useRealLights)
        {
            if (!useRealLights)
            {
                AssignLightRig("example/NewGamelight_rig");
                return;
            }

            ScreenManager.RemoveAllLights();
            foreach (SolarSystem system in SolarSystemList)
            {
                float intensity = 2.5f;
                float radius = 150000f;
                switch (system.SunPath)
                {
                    case "star_red":
                        intensity -= 5f;
                        radius -= 50000f;
                        break;
                    case "star_yellow":                            
                    case "star_yellow2":break;
                    case "star_green":  break;
                    case "star_blue":
                    case "star_binary":                            
                        intensity += .5f;
                        radius += 50000f;
                        break; 
                }
                // standard 3 point lighting
                AddLight(system, intensity, radius, zpos: +2500f, fillLight: true);
                AddLight(system, 2.5f, 5000f,   zpos: -2500f, fillLight: false);
                AddLight(system, 1.0f, 100000f, zpos: -6500f, fillLight: false);
            }
        }

        private void AddLight(SolarSystem system, float intensity, float radius, float zpos, bool fillLight)
        {
            var light = new PointLight
            {
                DiffuseColor = new Vector3(1f, 1f, 0.85f),
                Intensity    = intensity,
                ObjectType   = ObjectType.Static, // RedFox: changed this to Static
                FillLight    = true,
                Radius       = radius,
                Position     = new Vector3(system.Position, zpos),
                Enabled      = true
            };
            light.World = Matrix.CreateTranslation(light.Position);
            AddLight(light);
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

        private void CreateProjectionMatrix()
        {
            float aspect = (float)Viewport.Width / Viewport.Height;
            projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspect, 100f, 3E+07f);
        }

        public override void LoadContent()
        {
            GlobalStats.ResearchRootUIDToDisplay = "Colonization";
            SystemInfoUIElement.SysFont  = Fonts.Arial12Bold;
            SystemInfoUIElement.DataFont = Fonts.Arial10;
            NotificationManager = new NotificationManager(ScreenManager, this);
            aw = new AutomationWindow(this);
            for (int i = 0; i < UniverseSize / 5000.0f; ++i)
            {
                var nebulousOverlay = new NebulousOverlay
                {
                    Path = "Textures/smoke",
                    Position = new Vector3(
                        RandomMath.RandomBetween(-0.5f * UniverseSize, UniverseSize + 0.5f * UniverseSize),
                        RandomMath.RandomBetween(-0.5f * UniverseSize, UniverseSize + 0.5f * UniverseSize),
                        RandomMath.RandomBetween(-200000f, -2E+07f)),
                    Scale = RandomMath.RandomBetween(10f, 100f)
                };
                nebulousOverlay.WorldMatrix = Matrix.CreateScale(50f)
                    * Matrix.CreateScale(nebulousOverlay.Scale)
                    * Matrix.CreateRotationZ(RandomMath.RandomBetween(0.0f, 6.283185f))
                    * Matrix.CreateTranslation(nebulousOverlay.Position);
                Stars.Add(nebulousOverlay);
            }

            LoadGraphics();
            DoParticleLoad();
            bg3d = new Background3D(this);
            starfield = new Starfield(Vector2.Zero, ScreenManager.GraphicsDevice, TransientContent);
            starfield.LoadContent();

            CreateProjectionMatrix();
            SetLighting(UseRealLights);
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                foreach (string fleetUid in solarSystem.DefensiveFleets)
                    CreateDefensiveRemnantFleet(fleetUid, solarSystem.PlanetList[0].Center, 120000f);

                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.customRemnantElements)
                    foreach (Planet p in solarSystem.PlanetList)
                        foreach (string fleetUid in p.PlanetFleets)
                            CreateDefensiveRemnantFleet(fleetUid, p.Center, 120000f);

                foreach (SolarSystem.FleetAndPos fleetAndPos in solarSystem.FleetsToSpawn)
                    CreateDefensiveRemnantFleet(fleetAndPos.fleetname, solarSystem.Position + fleetAndPos.Pos, 75000f);

                foreach (string key in solarSystem.ShipsToSpawn)
                    Ship.CreateShipAt(key, EmpireManager.Remnants, solarSystem.PlanetList[0], true);

                foreach (Planet p in solarSystem.PlanetList)
                {
                    if (p.Owner != null)
                    {
                        foreach (string key in p.Guardians)
                            Ship.CreateShipAt(key, p.Owner, p, true);
                        continue;
                    }
                    // Added by McShooterz: alternate hostile fleets populate universe
                    if (GlobalStats.HasMod && ResourceManager.HostileFleets.Fleets.Count > 0)
                    {
                        if (p.Guardians.Count > 0)
                        {
                            int randomFleet  = RandomMath.InRange(ResourceManager.HostileFleets.Fleets.Count);
                            var hostileFleet = ResourceManager.HostileFleets.Fleets[randomFleet];
                            var empire       = EmpireManager.GetEmpireByName(hostileFleet.Empire);
                            foreach (string ship in hostileFleet.Ships)
                                Ship.CreateShipAt(ship, empire, p, true);
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
                foreach (Anomaly anomaly in solarSystem.AnomaliesList)
                {
                    if (anomaly.type == "DP")
                        anomalyManager.AnomaliesList.Add(new DimensionalPrison(solarSystem.Position + anomaly.Position));
                }
            }
            float univSizeOnScreen = 10f;
            MaxCamHeight = 4E+07f;
            while (univSizeOnScreen < (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 50))
            {
                float univRadius = UniverseSize / 2f;
                Matrix camMaxToUnivCenter = Matrix.CreateLookAt(new Vector3(-univRadius, univRadius, MaxCamHeight), 
                                                                new Vector3(-univRadius, univRadius, 0.0f), Vector3.Up);

                Vector3 univTopLeft  = Viewport.Project(Vector3.Zero, projection, camMaxToUnivCenter, Matrix.Identity);
                Vector3 univBotRight = Viewport.Project(new Vector3(UniverseSize, UniverseSize, 0.0f), projection, camMaxToUnivCenter, Matrix.Identity);
                univSizeOnScreen = univBotRight.X - univTopLeft.X;
                if (univSizeOnScreen < (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 50))
                    MaxCamHeight -= 0.1f * MaxCamHeight;
            }
            if (MaxCamHeight > 23000000)
                MaxCamHeight = 23000000;

            if (!loading)
            {
                CamPos.X = playerShip.Center.X;
                CamPos.Y = playerShip.Center.Y;
                CamHeight = 2750f;
            }
            CamDestination = new Vector3(CamPos.X, CamPos.Y, CamHeight);
            foreach (NebulousOverlay nebulousOverlay in Stars)
                star_particles.AddParticleThreadA(nebulousOverlay.Position, Vector3.Zero);

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

        private void CreateDefensiveRemnantFleet(string fleetUid, Vector2 where, float defenseRadius)
        {
            Fleet defensiveFleetAt = HelperFunctions.CreateDefensiveFleetAt(fleetUid, EmpireManager.Remnants, where);
            var militaryTask = new MilitaryTask
            {
                AO = where,
                AORadius = defenseRadius,
                type = MilitaryTask.TaskType.DefendSystem
            };
            defensiveFleetAt.FleetTask = militaryTask;
            defensiveFleetAt.TaskStep = 3;
            militaryTask.WhichFleet = EmpireManager.Remnants.GetFleetsDict().Count + 10;
            EmpireManager.Remnants.GetFleetsDict().Add(militaryTask.WhichFleet, defensiveFleetAt);
            EmpireManager.Remnants.GetGSAI().TaskList.Add(militaryTask);
            militaryTask.Step = 2;
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
            //MinimapButtons.screen                 = this;
            Empire.Universe                       = this;
            ResourceManager.UniverseScreen        = this;
            Empire.Universe                   = this;
            ShipAI.UniverseScreen = this;
            FleetDesignScreen.Screen              = this;

            CreateProjectionMatrix();
            Frustum            = new BoundingFrustum(view * projection);
            mmHousing          = new Rectangle(width - (276 + minimapOffSet), height - 256, 276 + minimapOffSet, 256);
            MinimapDisplayRect = new Rectangle(mmHousing.X + 61 + minimapOffSet, mmHousing.Y + 43, 200, 200);
            minimap            = new MiniMap(mmHousing);
            //mmButtons          = new MinimapButtons(mmHousing, EmpireUI);
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
            aw = new AutomationWindow(this);
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
                FogMap = ResourceManager.Texture("UniverseFeather");
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
            Listener       = new AudioListener();
            MuzzleFlashManager.flashModel   = content.Load<Model>("Model/Projectiles/muzzleEnergy");
            MuzzleFlashManager.FlashTexture = content.Load<Texture2D>("Model/Projectiles/Textures/MuzzleFlash_01");
            xnaPlanetModel                  = content.Load<Model>("Model/SpaceObjects/planet");
            atmoModel                       = content.Load<Model>("Model/sphere");
            AtmoEffect                      = content.Load<Effect>("Effects/PlanetHalo");
            cloudTex                        = content.Load<Texture2D>("Model/SpaceObjects/earthcloudmap");
            RingTexture                     = content.Load<Texture2D>("Model/SpaceObjects/planet_rings");
            SunModel                        = content.Load<Model>("Model/SpaceObjects/star_plane");
            NebModel                        = content.Load<Model>("Model/SpaceObjects/star_plane");
            FTLManager.LoadContent(content);
            ScreenRectangle = new Rectangle(0, 0, width, height);
            starfield = new Starfield(Vector2.Zero, device, content);
            starfield.LoadContent();

            ShipsInCombat = ButtonMediumMenu(width - 275, height - 280, "ShipsInCombat",   "Ships: 0");
            PlanetsInCombat = ButtonMediumMenu(width - 135, height - 280, "PlanetsInCombat", "Planets: 0");
        }

        public override void UnloadContent()
        {
            starfield?.UnloadContent();
            ScreenManager.UnloadSceneObjects();
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
                MusicCheckTimer = 2f;
                if (ScreenManager.Music.IsStopped)
                    ScreenManager.Music = GameAudio.PlayMusic("AmbientMusic");
            }

            Listener.Position = new Vector3(CamPos.X, CamPos.Y, 0.0f);

            ScreenManager.UpdateSceneObjects(gameTime);
            EmpireUI.Update(deltaTime);

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
            zTime += deltaTime;
        }

        public void DoAutoSave()
        {
            SavedGame savedGame = new SavedGame(this, "Autosave " + Auto);
            if (++Auto > 3) Auto = 1;
        }

        private void ProjectPieMenu(Vector2 position, float z)
        {
            var proj = Viewport.Project(position.ToVec3(z), projection, view, Matrix.Identity);
            pieMenu.Position    = proj.ToVec2();
            pieMenu.Radius      = 75f;
            pieMenu.ScaleFactor = 1f;
        }

        public void PlayNegativeSound() => GameAudio.PlaySfxAsync("UI_Misc20");

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
            var ex = new Exception(reportType);
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

        //added by gremlin replace redundant code with method


        public override void ExitScreen()
        {
            IsExiting = true;
            var processTurnsThread = ProcessTurnsThread;
            ProcessTurnsThread = null;
            DrawCompletedEvt.Set(); // notify processTurnsThread that we're terminating
            processTurnsThread.Join(250);
            EmpireUI.empire = null;
            EmpireUI = null;
            //SpaceManager.Destroy();
            ScreenManager.Music.Stop();
            NebulousShit.Clear();
            bloomComponent = null;
            bg3d.BGItems.Clear();
            bg3d = null;
            playerShip = null;
            ShipToView = null;
            foreach (Ship ship in MasterShipList)
                ship.RemoveFromUniverseUnsafe();
            MasterShipList.ApplyPendingRemovals();
            MasterShipList.Clear();
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                solarSystem.FiveClosestSystems.Clear();
                foreach (Planet planet in solarSystem.PlanetList)
                {
                    planet.TilesList = new Array<PlanetGridSquare>();
                    if (planet.SO != null)
                    {
                        planet.SO.Clear();
                        ScreenManager.RemoveObject(planet.SO);
                        planet.SO = null;
                    }
                }
                foreach (Asteroid asteroid in solarSystem.AsteroidsList)
                {
                    if (asteroid.So!= null)
                    {
                        asteroid.So.Clear();
                        ScreenManager.RemoveObject(asteroid.So);
                    }
                }
                solarSystem.AsteroidsList.Clear();
                foreach (Moon moon in solarSystem.MoonList)
                {
                    if (moon.So != null)
                    {
                        moon.So.Clear();
                        ScreenManager.RemoveObject(moon.So);
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
            SpaceManager.Destroy();
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
            Empire.Universe                       = null;
            ResourceManager.UniverseScreen        = null;
            Empire.Universe                       = null;
            ShipAI.UniverseScreen                 = null;
            MuzzleFlashManager.universeScreen     = null;
            FleetDesignScreen.Screen              = null;
            ExplosionManager.Universe             = null;
            DroneAI.UniverseScreen                = null;
            StatTracker.SnapshotsDict.Clear();
            EmpireManager.Clear();            
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
        }

        private MultiShipData ComputeMultiShipCircle()
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

        private Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
        {
            return new Vector2(0.0f, 0.0f)
            {
                X = (float)-((double)OwnerPos.X - (double)TargetPos.X),
                Y = OwnerPos.Y - TargetPos.Y
            };
        }

        // Refactored by RedFox
        // this draws the colored empire borders
        // the borders are drawn into a separate framebuffer texture and later blended with final visual

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


        // @todo fleetIconScreenRadius could be replaced with clickableFleet.ClickRadius ????


        //This will likely only work with "this UI\planetNamePointer" texture 
        //Other textures might work but would need the x and y offset adjusted. 

        public void QueueGameplayObjectRemoval (GameplayObject gameplayObject)
        {
            if (gameplayObject == null) return;
            GamePlayObjectToRemove.Add(gameplayObject);
        }

        public void TotallyRemoveGameplayObjects()
        {
            while (!GamePlayObjectToRemove.IsEmpty)            
                GamePlayObjectToRemove.PopLast().RemoveFromUniverseUnsafe();             
        }

        public void QueueShipToWorldScene(Ship ship)
        {            
            ShipsToAddToWorld.Add(ship);
        }
        private void AddShipSceneObjectsFromQueue()
        {            
            while (!ShipsToAddToWorld.IsEmpty)
            {
                var ship = ShipsToAddToWorld.PopLast();
                if (!ship.Active) continue;
                try
                {
                    ship.InitiizeShipScene();
                }
                catch(Exception ex)
                {
                    Log.Error(ex,$"Crash attempting to create sceneobject. Destroying");
                    ship.RemoveFromUniverseUnsafe();

                }
            }           
        }

        protected override void Destroy()
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

            base.Destroy();
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

        private struct ClickableSystem
        {
            public Vector2 ScreenPos;
            public float Radius;
            public SolarSystem systemToClick;
            public bool Touched(Vector2 touchPoint)
            {
                if (!(Vector2.Distance(touchPoint, ScreenPos) <= Radius)) return false;

                GameAudio.SystemClick();
                return true;
            }
        }

        public class ClickableItemUnderConstruction
        {
            public Vector2 ScreenPos;
            public Vector2 BuildPos;
            public float Radius;
            public string UID;
            public Goal AssociatedGoal;
        }

        private struct ClickableFleet
        {
            public Fleet fleet;
            public Vector2 ScreenPos;
            public float ClickRadius;
        }
        public enum UnivScreenState
        {
            DetailView = 10000,
            ShipView   = 30000,
            SystemView = 250000,
            SectorView = 1775000,
            GalaxyView,
        }

        public float GetZfromScreenState(UnivScreenState screenState)
        {
            if (screenState == UnivScreenState.GalaxyView)
                return MaxCamHeight;
            return (float)screenState;
        }

        private struct FleetButton
        {
            public Rectangle ClickRect;
            public Fleet Fleet;
            public int Key;
        }

        private struct MultiShipData
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


        private enum CursorState
        {
            Normal,
            Move,
            Follow,
            Attack,
            Orbit,
        }
    }
}

