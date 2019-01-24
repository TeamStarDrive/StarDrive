using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Empires;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;
using System;
using System.IO;
using System.Threading;
using Ship_Game.Audio;

namespace Ship_Game
{
    public partial class UniverseScreen : GameScreen
    {
        private readonly PerfTimer EmpireUpdatePerf  = new PerfTimer();
        private readonly PerfTimer Perfavg2          = new PerfTimer();
        private readonly PerfTimer PreEmpirePerf     = new PerfTimer();
        private readonly PerfTimer perfavg4          = new PerfTimer();
        private readonly PerfTimer perfavg5          = new PerfTimer();

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
        public string StarDateString => StarDate.StarDateString();
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
        private GameTime SimulationTime = new GameTime();
        public Array<ShipModule> ModulesNeedingReset = new Array<ShipModule>();
        private bool TurnFlip = true;
        private float TurnFlipCounter;
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
        public ParticleSystem SmallflameParticles;
        public ParticleSystem sparks;
        public ParticleSystem lightning;
        public ParticleSystem flash;
        public ParticleSystem star_particles;
        public ParticleSystem neb_particles;
        public StarField StarField;
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
        protected float MaxCamHeight;
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
        public BoundingFrustum Frustum;
        private ClickablePlanets tippedPlanet;
        private ClickableSystem tippedSystem;
        private bool ShowingSysTooltip;
        private bool ShowingPlanetToolTip;
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
        public bool Paused = true; // always start paused
        public bool SkipRightOnce;
        public bool NoEliminationVictory;
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
        public int PathMapReducer          = 1;
        public float screenDelay    = 0f;
        public SubSpaceProjectors SubSpaceProjectors;

        // for really specific debuggingD
        public static int FrameId;

        public bool IsViewingCombatScreen(Planet p) => LookingAtPlanet && workersPanel is CombatScreen cs && cs.p == p;

        public UniverseScreen(UniverseData data) : base(null) // new game
        {
            UniverseSize          = data.Size.X;
            FTLModifier           = data.FTLSpeedModifier;
            EnemyFTLModifier      = data.EnemyFTLSpeedModifier;
            GravityWells          = data.GravityWells;
            SolarSystemList       = data.SolarSystemsList;
            MasterShipList        = data.MasterShipList;
            playerShip            = data.playerShip;
            PlayerEmpire          = playerShip.loyalty;
            PlayerLoyalty         = playerShip.loyalty.data.Traits.Name;
            ShipToView            = playerShip;
            PlayerEmpire.isPlayer = true;
            SubSpaceProjectors    = new SubSpaceProjectors(UniverseSize);
            SpaceManager.Setup(UniverseSize);
            DoPathingMapRebuild();
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
            SubSpaceProjectors    = new SubSpaceProjectors(UniverseSize);
            SpaceManager.Setup(UniverseSize);
            DoPathingMapRebuild();
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

            AddLight("Global Fill Light", new Vector2(0, 0), .7f, UniverseSize * 2 + MaxCamHeight * 10, Color.White, -MaxCamHeight * 10, fillLight: false, shadowQuality: 0f);
            AddLight("Global Back Light", new Vector2(0, 0), .6f, UniverseSize * 2 + MaxCamHeight * 10, Color.White, +MaxCamHeight * 10, fillLight: false, shadowQuality: 0f);

            foreach (SolarSystem system in SolarSystemList)
            {
                Color color     = system.Sun.LightColor;
                float intensity = system.Sun.LightIntensity;
                float radius    = system.Sun.Radius;
                AddLight("Key",               system, intensity,         radius,         color, -5500);
                AddLight("OverSaturationKey", system, intensity * 5.00f, radius * 0.05f, color, -1500);
                AddLight("LocalFill",         system, intensity * 0.55f, radius,         Color.White, 0);
                //AddLight("Back", system, intensity * 0.5f , radius, color, 2500, fallOff: 0, fillLight: true);
            }
        }

        private void AddLight(string name, SolarSystem system, float intensity, float radius, Color color, float zpos, float fallOff = 1f, bool fillLight = false)
        {
            AddLight($"{system.Name} - {system.Sun.Id} - {name}", system.Position, intensity, radius, color,
                zpos, fillLight: fillLight, fallOff:fallOff, shadowQuality:0f);
        }

        private void AddLight(string name, Vector2 source, float intensity, float radius, Color color,
                              float zpos, bool fillLight, float fallOff = 0, float shadowQuality = 1)
        {
            var light = new PointLight
            {
                Name                = name,
                DiffuseColor        = color.ToVector3(),
                Intensity           = intensity,
                ObjectType          = ObjectType.Static, // RedFox: changed this to Static
                FillLight           = fillLight,
                Radius              = radius,
                Position            = new Vector3(source, zpos),
                Enabled             = true,
                FalloffStrength     = fallOff,
                ShadowPerSurfaceLOD = true,
                ShadowQuality = shadowQuality
            };

            if (shadowQuality > 0f)
                light.ShadowType = ShadowType.AllObjects;

            light.World = Matrix.CreateTranslation(light.Position);
            AddLight(light);
        }

        public void ContactLeader()
        {
            if (SelectedShip == null)
                return;
            if (SelectedShip.loyalty.isFaction)
            {
                foreach (Encounter e in ResourceManager.Encounters)
                {
                    if (SelectedShip.loyalty.data.Traits.Name == e.Faction && player.GetRelations(SelectedShip.loyalty).EncounterStep == e.Step)
                    {
                        ScreenManager.AddScreen(new EncounterPopup(this, player, SelectedShip.loyalty, null, e));
                        break;
                    }
                }
            }
            else
                ScreenManager.AddScreen(new DiplomacyScreen(this, SelectedShip.loyalty, player, "Greeting"));
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
            StarField = new StarField(this);

            CreateProjectionMatrix();
            SetLighting(UseRealLights);
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                SpawnRemnantsInSolarSystem(solarSystem);

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

            foreach (Empire empire in EmpireManager.Empires)
            {
                if (!ResourceManager.PreLoadModels(empire))
                {
                    ExitScreen();
                    StarDriveGame.Instance.Exit();
                    return;
                }
            }

            //HelperFunctions.CollectMemory();

            ProcessTurnsThread = new Thread(ProcessTurns);
            ProcessTurnsThread.Name = "Universe.ProcessTurns()";
            ProcessTurnsThread.IsBackground = false; // RedFox - make sure ProcessTurns runs with top priority
            ProcessTurnsThread.Start();
        }

        public static void SpawnRemnantsInSolarSystem(SolarSystem solarSystem)
        {
            if (EmpireManager.Remnants == null)
                return;

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
        }

        public static void CreateDefensiveRemnantFleet(string fleetUid, Vector2 where, float defenseRadius)
        {
            if (EmpireManager.Remnants == null)
                return;
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
            EmpireManager.Remnants.GetEmpireAI().TaskList.Add(militaryTask);
            militaryTask.Step = 2;
        }

        private void DoParticleLoad()
        {
            var content              = TransientContent;
            var device               = ScreenManager.GraphicsDevice;
            beamflashes              = new ParticleSystem(content, "3DParticles/BeamFlash", device);
            explosionParticles       = new ParticleSystem(content, "3DParticles/ExplosionSettings", device);
            photonExplosionParticles = new ParticleSystem(content, "3DParticles/PhotonExplosionSettings", device);
            explosionSmokeParticles  = new ParticleSystem(content, "3DParticles/ExplosionSmokeSettings", device);
            projectileTrailParticles = new ParticleSystem(content, "3DParticles/ProjectileTrailSettings", device, 1f);
            fireTrailParticles       = new ParticleSystem(content, "3DParticles/FireTrailSettings", device,1);
            smokePlumeParticles      = new ParticleSystem(content, "3DParticles/SmokePlumeSettings", device , 1);
            fireParticles            = new ParticleSystem(content, "3DParticles/FireSettings", device, 1);
            engineTrailParticles     = new ParticleSystem(content, "3DParticles/EngineTrailSettings", device);
            flameParticles           = new ParticleSystem(content, "3DParticles/FlameSettings", device);
            SmallflameParticles      = new ParticleSystem(content, "3DParticles/FlameSettings", device, .25f, (int)(4000 * GlobalStats.DamageIntensity));
            sparks                   = new ParticleSystem(content, "3DParticles/sparks", device, 1);
            lightning                = new ParticleSystem(content, "3DParticles/lightning", device, 1);
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
            if (loadFogPath == null)
            {
                FogMap = ResourceManager.Texture2D("UniverseFeather");
            }
            else
            {
                using (FileStream fileStream = File.OpenRead($"{Dir.StarDriveAppData}/Saved Games/Fog Maps/{loadFogPath}.png"))
                    FogMap = Texture2D.FromFile(device, fileStream);
            }
            FogMapTarget = new RenderTarget2D(device, 512, 512, 1, backBufferFormat, device.PresentationParameters.MultiSampleType, device.PresentationParameters.MultiSampleQuality);
            basicFogOfWarEffect = content.Load<Effect>("Effects/BasicFogOfWar");
            LoadMenu();

            anomalyManager = new AnomalyManager();
            Listener       = new AudioListener();
            xnaPlanetModel = content.Load<Model>("Model/SpaceObjects/planet");
            atmoModel      = content.Load<Model>("Model/sphere");
            AtmoEffect     = content.Load<Effect>("Effects/PlanetHalo");
            cloudTex       = content.Load<Texture2D>("Model/SpaceObjects/earthcloudmap");
            RingTexture    = content.Load<Texture2D>("Model/SpaceObjects/planet_rings");
            SunModel       = content.Load<Model>("Model/SpaceObjects/star_plane");
            NebModel       = content.Load<Model>("Model/SpaceObjects/star_plane");
            FTLManager.LoadContent(content);
            MuzzleFlashManager.LoadContent(content);
            ScreenRectangle = new Rectangle(0, 0, width, height);
            StarField = new StarField(this);

            ShipsInCombat = ButtonMediumMenu(width - 275, height - 280, "Ships: 0");
            PlanetsInCombat = ButtonMediumMenu(width - 135, height - 280, "Planets: 0");
        }

        public override void UnloadContent()
        {
            StarField?.UnloadContent();
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
            SimulationTime = gameTime;
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

        public void PlayNegativeSound() => GameAudio.NegativeClick();

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
            bg3d.Dispose(ref bg3d);
            playerShip = null;
            ShipToView = null;
            foreach (Ship ship in MasterShipList)
                ship?.RemoveFromUniverseUnsafe();
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
            StarField.UnloadContent();
            StarField.Dispose();
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
            SmallflameParticles.UnloadContent();
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
            FleetDesignScreen.Screen              = null;
            ExplosionManager.Universe             = null;
            DroneAI.UniverseScreen                = null;
            StatTracker.SnapshotsDict.Clear();
            EmpireManager.Clear();
            HelperFunctions.CollectMemory();
            base.ExitScreen();
            Dispose();

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
            var multiShipData = new MultiShipData();
            multiShipData.status = num3 / num4;
            multiShipData.weightedCenter = new Vector2(x, y);
            multiShipData.Radius = 0.0f;
            foreach (Ship gameplayObject in SelectedShipList)
            {
                float num5 = Vector2.Distance(gameplayObject.Position, multiShipData.weightedCenter);
                if (num5 > multiShipData.Radius)
                    multiShipData.Radius = num5;
            }
            //this.computeCircle = false;
            return multiShipData;
        }

        // Refactored by RedFox
        // this draws the colored empire borders
        // the borders are drawn into a separate framebuffer texture and later blended with final visual

        private SubTexture Arc15  = ResourceManager.Texture("Arcs/Arc15");
        private SubTexture Arc20  = ResourceManager.Texture("Arcs/Arc20");
        private SubTexture Arc45  = ResourceManager.Texture("Arcs/Arc45");
        private SubTexture Arc60  = ResourceManager.Texture("Arcs/Arc60");
        private SubTexture Arc90  = ResourceManager.Texture("Arcs/Arc90");
        private SubTexture Arc120 = ResourceManager.Texture("Arcs/Arc120");
        private SubTexture Arc180 = ResourceManager.Texture("Arcs/Arc180");
        private SubTexture Arc360 = ResourceManager.Texture("Arcs/Arc360");

        public SubTexture GetArcTexture(float weaponArc)
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
            if (weapon.WeaponType == "Laser" || weapon.WeaponType == "HeavyLaser")
                return Color.Red;
            if (weapon.WeaponType == "PhotonCannon")
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
            StarField               ?.Dispose(ref StarField);
            DeepSpaceDone           ?.Dispose(ref DeepSpaceDone);
            EmpireDone              ?.Dispose(ref EmpireDone);
            DeepSpaceGateKeeper     ?.Dispose(ref DeepSpaceGateKeeper);
            ItemsToBuild            ?.Dispose(ref ItemsToBuild);
            anomalyManager          ?.Dispose(ref anomalyManager);
            bloomComponent          ?.Dispose(ref bloomComponent);
            ShipGateKeeper          ?.Dispose(ref ShipGateKeeper);
            SystemThreadGateKeeper  ?.Dispose(ref SystemThreadGateKeeper);
            //FogMap                  ?.Dispose(ref FogMap);
            MasterShipList          ?.Dispose(ref MasterShipList);
            EmpireGateKeeper        ?.Dispose(ref EmpireGateKeeper);
            BombList                ?.Dispose(ref BombList);
            flash                   ?.Dispose(ref flash);
            lightning               ?.Dispose(ref lightning);
            neb_particles           ?.Dispose(ref neb_particles);
            photonExplosionParticles?.Dispose(ref photonExplosionParticles);
            projectileTrailParticles?.Dispose(ref projectileTrailParticles);
            sceneMap                ?.Dispose(ref sceneMap);
            smokePlumeParticles     ?.Dispose(ref smokePlumeParticles);
            sparks                  ?.Dispose(ref sparks);
            star_particles          ?.Dispose(ref star_particles);
            engineTrailParticles    ?.Dispose(ref engineTrailParticles);
            explosionParticles      ?.Dispose(ref explosionParticles);
            explosionSmokeParticles ?.Dispose(ref explosionSmokeParticles);
            fireTrailParticles      ?.Dispose(ref fireTrailParticles);
            fireParticles           ?.Dispose(ref fireParticles);
            flameParticles          ?.Dispose(ref flameParticles);
            SmallflameParticles     ?.Dispose(ref SmallflameParticles);
            beamflashes             ?.Dispose(ref beamflashes);
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

                GameAudio.MouseOver();
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
            GalaxyView
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
            Orbit
        }
    }
}

