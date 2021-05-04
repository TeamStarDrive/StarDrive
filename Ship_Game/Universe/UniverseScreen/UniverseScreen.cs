using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.AI;
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
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Universe;
using Ship_Game.Fleets;
using Ship_Game.Graphics;

namespace Ship_Game
{
    public partial class UniverseScreen : GameScreen
    {
        public static readonly SpatialManager Spatial = new SpatialManager();

        /// <summary>
        /// Manages universe objects in a thread-safe manner
        /// </summary>
        public UniverseObjectManager Objects;

        public bool GameOver = false;

        // TODO: Encapsulate
        public static Array<SolarSystem> SolarSystemList = new Array<SolarSystem>();

        // TODO: Encapsulate
        public static BatchRemovalCollection<SpaceJunk> JunkList = new BatchRemovalCollection<SpaceJunk>();
        
        public float GamePace = 1f;
        public float GameSpeed = 1f;
        public float StarDate = 1000f;
        public string StarDateString => StarDate.StarDateString();
        public float LastAutosaveTime = 0;
        public Array<ClickablePlanets> ClickPlanetList = new Array<ClickablePlanets>();
        public BatchRemovalCollection<ClickableItemUnderConstruction> ItemsToBuild = new BatchRemovalCollection<ClickableItemUnderConstruction>();
        Array<ClickableSystem> ClickableSystems    = new Array<ClickableSystem>();
        public BatchRemovalCollection<Ship> SelectedShipList = new BatchRemovalCollection<Ship>();
        Array<ClickableShip> ClickableShipsList    = new Array<ClickableShip>();
        Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        public Background bg;
        public float UniverseSize       = 5000000f; // universe width and height in world units
        public float FTLModifier        = 1f;
        public float EnemyFTLModifier   = 1f;
        public bool FTLInNuetralSystems = true;
        public Vector3 transitionStartPosition;
        public Vector3 camTransitionPosition;
        Rectangle ScreenRectangle;
        public Map<Guid, Planet> PlanetsDict          = new Map<Guid, Planet>();
        public Map<Guid, SolarSystem> SolarSystemDict = new Map<Guid, SolarSystem>();
        public BatchRemovalCollection<Bomb> BombList  = new BatchRemovalCollection<Bomb>();
        readonly AutoResetEvent DrawCompletedEvt = new AutoResetEvent(false);
        public float CamHeight = 2550f;
        public Vector3 CamPos = Vector3.Zero;
        float TooltipTimer = 0.5f;
        float sTooltipTimer = 0.5f;
        int Auto = 1;
        public bool ViewingShip             = false;
        public float transDuration          = 3f;
        public float SelectedSomethingTimer = 3f;
        public Vector2 mouseWorldPos;
        Array<FleetButton> FleetButtons = new Array<FleetButton>();
        public Array<FogOfWarNode> FogNodes = new Array<FogOfWarNode>();
        Array<ClickableFleet> ClickableFleetsList = new Array<ClickableFleet>();
        public bool ShowTacticalCloseup { get; private set; }
        public bool Debug;
        public Planet SelectedPlanet;
        public Ship SelectedShip;
        public ClickableItemUnderConstruction SelectedItem;
        PieMenu pieMenu;
        PieMenuNode planetMenu;
        PieMenuNode shipMenu;
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
        public Model xnaPlanetModel;
        public Texture2D RingTexture;
        public UnivScreenState viewState;
        public bool LookingAtPlanet;
        public bool snappingToShip;
        public bool returnToShip;
        public Vector3 CamDestination;
        public Texture2D cloudTex;
        public EmpireUIOverlay EmpireUI;
        public BloomComponent bloomComponent;
        public Texture2D FogMap;
        RenderTarget2D FogMapTarget;
        public RenderTarget2D MainTarget;
        public RenderTarget2D BorderRT;
        RenderTarget2D LightsTarget;
        public Effect basicFogOfWarEffect;
        public Rectangle SelectedStuffRect;
        public NotificationManager NotificationManager;
        public Rectangle MinimapDisplayRect;
        public Rectangle mmShowBorders;
        public Rectangle mmHousing;
        protected float MaxCamHeight;
        public AnomalyManager anomalyManager;
        public ShipInfoUIElement ShipInfoUIElement;
        public PlanetInfoUIElement pInfoUI;
        public SolarsystemOverlay SystemInfoOverlay;
        public ShipListInfoUIElement shipListInfoUI;
        public VariableUIElement vuiElement;
        public Empire player;
        MiniMap minimap;
        bool loading;
        public Thread ProcessTurnsThread;
        public float transitionElapsedTime;

        // @note Initialize with a default frustum for UnitTests
        public BoundingFrustum Frustum = new BoundingFrustum(Matrix.CreateTranslation(1000000, 1000000, 0));
        ClickablePlanets tippedPlanet;
        ClickableSystem tippedSystem;
        bool ShowingSysTooltip;
        bool ShowingPlanetToolTip;
        float MusicCheckTimer;
        public Ship ShipToView;
        public float AdjustCamTimer;
        public AutomationWindow aw;
        public bool DefiningAO; // are we defining a new AO?
        public bool DefiningTradeRoutes; // are we defining  trade routes for a freighter?
        public Rectangle AORect; // used for showing current AO Rect definition

        public bool showingFTLOverlay;
        public bool showingRangeOverlay;

        /// <summary>
        /// Toggles Cinematic Mode (no UI) on or off
        /// </summary>
        bool IsCinematicModeEnabled = false;
        float CinematicModeTextTimer = 3f;

        /// <summary>
        /// Conditions to suppress diplomacy screen popups
        /// </summary>
        public bool CanShowDiplomacyScreen => !IsCinematicModeEnabled;

        public DeepSpaceBuildingWindow DeepSpaceBuildWindow;
        public DebugInfoScreen DebugWin;
        public bool ShowShipNames;
        public bool Paused = true; // always start paused
        public bool NoEliminationVictory;
        bool UseRealLights = true;
        public SolarSystem SelectedSystem;
        public Fleet SelectedFleet;
        int FBTimer = 60;
        bool pickedSomethingThisFrame;
        bool SelectingWithBox;
        Effect AtmoEffect;
        Model atmoModel;
        public PlanetScreen workersPanel;
        CursorState cState;
        int SelectorFrame;
        public Ship previousSelection;
        public DeferredRenderer RenderQueue;

        public UIButton ShipsInCombat;
        public UIButton PlanetsInCombat;
        public int lastshipcombat   = 0;
        public int lastplanetcombat = 0;
        public SubSpaceProjectors SubSpaceProjectors;

        ShipMoveCommands ShipCommands;

        // for really specific debugging
        public int SimTurnId;

        // To avoid double-loading universe thread when
        // graphics setting changes cause 
        bool IsUniverseInitialized;

        public bool IsViewingCombatScreen(Planet p) => LookingAtPlanet && workersPanel is CombatScreen cs && cs.P == p;
        public bool IsViewingColonyScreen(Planet p) => LookingAtPlanet && workersPanel is ColonyScreen cs && cs.P == p;

        public Array<Ship> GetMasterShipList() => Objects.Ships;

        public bool IsSectorViewOrCloser => viewState <= UnivScreenState.SectorView;
        public bool IsSystemViewOrCloser => viewState <= UnivScreenState.SystemView;
        public bool IsPlanetViewOrCloser => viewState <= UnivScreenState.PlanetView;
        public bool IsShipViewOrCloser   => viewState <= UnivScreenState.ShipView;

        public UniverseScreen(UniverseData data, Empire loyalty) : base(null) // new game
        {
            SetupUniverseScreen(data, loyalty);
        }

        public UniverseScreen(UniverseData data, string loyalty) : base(null) // savegame
        {
            loading = true;
            loadFogPath = data.loadFogPath;
            Empire thePlayer = EmpireManager.GetEmpireByName(loyalty);
            SetupUniverseScreen(data, thePlayer);
        }

        void SetupUniverseScreen(UniverseData data, Empire thePlayer)
        {
            Name = "UniverseScreen";
            CanEscapeFromScreen = false;
         
            PlayerLoyalty         = thePlayer.data.Traits.Name;
            PlayerEmpire          = thePlayer;
            player                = thePlayer;
            if (!player.isPlayer)
                throw new ArgumentException($"Invalid Player Empire, isPlayer==false: {player}");

            UniverseSize          = data.Size.X;
            FTLModifier           = data.FTLSpeedModifier;
            EnemyFTLModifier      = data.EnemyFTLSpeedModifier;
            GravityWells          = data.GravityWells;
            SolarSystemList       = data.SolarSystemsList;
            
            Spatial.Setup(UniverseSize);
            Objects = new UniverseObjectManager(this, Spatial, data);
            Objects.OnShipRemoved += Objects_OnShipRemoved;
            SubSpaceProjectors = new SubSpaceProjectors(UniverseSize);
            ShipCommands = new ShipMoveCommands(this);
            DeepSpaceBuildWindow = new DeepSpaceBuildingWindow(this);
        }

        void Objects_OnShipRemoved(Ship ship)
        {
            RunOnUIThread(() =>
            {
                if (SelectedShip == ship)
                    SelectedShip = null;
                SelectedShipList.RemoveRef(ship);
            });
        }

        public Planet GetPlanet(Guid guid)
        {
            if (guid == Guid.Empty) return null;
            if (PlanetsDict.TryGetValue(guid, out Planet planet))
                return planet;
            Log.Error($"Guid for planet not found: {guid}");
            return null;
        }

        // NOTE: this relies on MaxCamHeight and UniverseSize
        void ResetLighting(bool forceReset)
        {
            if (!forceReset && ScreenManager.LightRigIdentity == LightRigIdentity.UniverseScreen)
                return;

            if (!UseRealLights)
            {
                AssignLightRig(LightRigIdentity.UniverseScreen, "example/NewGamelight_rig");
                return;
            }

            RemoveLighting();
            ScreenManager.LightRigIdentity = LightRigIdentity.UniverseScreen;

            AddLight("Global Fill Light", new Vector2(0, 0), 0.7f, UniverseSize * 2 + MaxCamHeight * 10, Color.White, -MaxCamHeight * 10, fillLight: false, shadowQuality: 0f);
            AddLight("Global Back Light", new Vector2(0, 0), 0.6f, UniverseSize * 2 + MaxCamHeight * 10, Color.White, +MaxCamHeight * 10, fillLight: false, shadowQuality: 0f);

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

        void RemoveLighting()
        {
            ScreenManager.RemoveAllLights();
        }

        void AddLight(string name, SolarSystem system, float intensity, float radius, Color color, float zpos, float fallOff = 1f, bool fillLight = false)
        {
            AddLight($"{system.Name} - {system.Sun.Id} - {name}", system.Position, intensity, radius, color,
                zpos, fillLight: fillLight, fallOff:fallOff, shadowQuality:0f);
        }

        void AddLight(string name, Vector2 source, float intensity, float radius, Color color,
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

            Empire leaderLoyalty = SelectedShip.loyalty;
            if (leaderLoyalty.isFaction)
                Encounter.ShowEncounterPopUpPlayerInitiated(SelectedShip.loyalty, this);
            else
                DiplomacyScreen.Show(SelectedShip.loyalty, player, "Greeting");
        }

        void CreateProjectionMatrix()
        {
            float aspect = (float)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspect, 100f, 3E+07f);
        }

        public override void LoadContent()
        {
            Log.Write(ConsoleColor.Cyan, "UniverseScreen.LoadContent");
            RemoveAll();
            UnloadGraphics();
            
            Empire.Universe = this;

            GlobalStats.ResearchRootUIDToDisplay = "Colonization";
            SolarsystemOverlay.SysFont  = Fonts.Arial12Bold;
            SolarsystemOverlay.DataFont = Fonts.Arial10;

            NotificationManager = new NotificationManager(ScreenManager, this);
            aw = Add(new AutomationWindow(this));

            InitializeCamera(); // ResetLighting requires MaxCamHeight
            ResetLighting(forceReset: true);
            LoadGraphics();

            InitializeUniverse();
        }

        void InitializeCamera()
        {
             float univSizeOnScreen = 10f;
            MaxCamHeight = 15000000;
            CreateProjectionMatrix();

            while (univSizeOnScreen < (ScreenWidth + 50))
            {
                float univRadius = UniverseSize / 2f;
                var camMaxToUnivCenter = Matrix.CreateLookAt(new Vector3(-univRadius, univRadius, MaxCamHeight),
                                                             new Vector3(-univRadius, univRadius, 0.0f), Vector3.Up);

                Vector3 univTopLeft  = Viewport.Project(Vector3.Zero, Projection, camMaxToUnivCenter, Matrix.Identity);
                Vector3 univBotRight = Viewport.Project(new Vector3(UniverseSize * 1.25f, UniverseSize * 1.25f, 0.0f), Projection, camMaxToUnivCenter, Matrix.Identity);
                univSizeOnScreen = univBotRight.X - univTopLeft.X;
                if (univSizeOnScreen < (ScreenWidth + 50))
                    MaxCamHeight -= 0.1f * MaxCamHeight;
            }

            if (MaxCamHeight > 15000000)
                MaxCamHeight = 15000000;

            if (!loading)
            {
                CamPos.X = PlayerEmpire.GetPlanets()[0].Center.X;
                CamPos.Y = PlayerEmpire.GetPlanets()[0].Center.Y;
                CamHeight = 2750f;
            }

            CamDestination = new Vector3(CamPos.X, CamPos.Y, CamHeight);
        }

        void InitializeUniverse()
        {
            if (IsUniverseInitialized)
                return;

            IsUniverseInitialized = true;
            CreateStartingShips();
            InitializeSolarSystems();
            CreatePlanetsLookupTable();
            CreateStationTethers();
            EmpireManager.RestoreUnserializableDataFromSave();
            RecomputeFleetButtons(true);
            CreateProcessTurnsThread();

            if (StarDate.AlmostEqual(1000)) // Run once to get all empire goals going
                UpdateEmpires(FixedSimTime.Zero);
            RenderQueue = new DeferredRenderer(this);
        }

        void CreateProcessTurnsThread()
        {
            ProcessTurnsThread = new Thread(ProcessTurnsMonitored);
            ProcessTurnsThread.Name = "Universe.ProcessTurns";
            ProcessTurnsThread.IsBackground = false; // RedFox - make sure ProcessTurns runs with top priority
            ProcessTurnsThread.Start();
        }

        void CreatePlanetsLookupTable()
        {
            PlanetsDict.Clear();
            SolarSystemDict.Clear();
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                SolarSystemDict.Add(solarSystem.guid, solarSystem);
                foreach (Planet planet in solarSystem.PlanetList)
                    PlanetsDict.Add(planet.guid, planet);
            }
        }

        void CreateStationTethers()
        {
            foreach (Ship ship in GetMasterShipList())
            {
                if (ship.TetherGuid != Guid.Empty)
                {
                    Planet p = GetPlanet(ship.TetherGuid);
                    if (p != null)
                    {
                        ship.TetherToPlanet(p);
                        p.OrbitalStations.Add(ship);
                    }
                }
            }
        }

        void InitializeSolarSystems()
        {
            anomalyManager = new AnomalyManager();

            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                foreach (Anomaly anomaly in solarSystem.AnomaliesList)
                {
                    if (anomaly.type == "DP")
                        anomalyManager.AnomaliesList.Add(
                            new DimensionalPrison(solarSystem.Position + anomaly.Position));
                }

                foreach (Empire empire in EmpireManager.ActiveEmpires)
                {
                        solarSystem.UpdateFullyExploredBy(empire);
                }
            }
        }

        void CreateStartingShips()
        {
            // not a new game or load game at stardate 1000 
            if (StarDate > 1000f || GetMasterShipList().Count > 0)
                return;

            foreach (Empire empire in EmpireManager.MajorEmpires)
            {
                Planet homePlanet    = empire.GetPlanets()[0];
                string colonyShip    = empire.data.DefaultColonyShip;
                string startingScout = empire.data.StartingScout;
                string starterShip   = empire.data.Traits.Prototype == 0
                                       ? empire.data.StartingShip
                                       : empire.data.PrototypeShip;

                Ship.CreateShipAt(starterShip, empire, homePlanet, RandomMath.Vector2D(homePlanet.ObjectRadius * 3), true);
                Ship.CreateShipAt(colonyShip, empire, homePlanet, RandomMath.Vector2D(homePlanet.ObjectRadius * 2), true);
                Ship.CreateShipAt(startingScout, empire, homePlanet, RandomMath.Vector2D(homePlanet.ObjectRadius * 3), true);
            }
        }

        void LoadParticles(Data.GameContentManager content, GraphicsDevice device)
        {
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

        void CreateStarParticles()
        {
            int numStars = (int)(UniverseSize / 5000.0f);
            for (int i = 0; i < numStars; ++i)
            {
                var position = new Vector3(
                    RandomMath.RandomBetween(-0.5f * UniverseSize, UniverseSize + 0.5f * UniverseSize),
                    RandomMath.RandomBetween(-0.5f * UniverseSize, UniverseSize + 0.5f * UniverseSize),
                    RandomMath.RandomBetween(-200000f, -2E+07f));
                star_particles.AddParticleThreadA(position, Vector3.Zero);
            }
        }

        void LoadGraphics()
        {
            const int minimapOffSet = 14;

            var content = TransientContent;
            var device  = ScreenManager.GraphicsDevice;
            int width   = GameBase.ScreenWidth;
            int height  = GameBase.ScreenHeight;

            LoadParticles(content, device);

            bg = new Background();
            bg3d = new Background3D(this);
            StarField = new StarField(this);
            
            CreateStarParticles();

            Frustum            = new BoundingFrustum(View * Projection);
            mmHousing          = new Rectangle(width - (276 + minimapOffSet), height - 256, 276 + minimapOffSet, 256);
            MinimapDisplayRect = new Rectangle(mmHousing.X + 61 + minimapOffSet, mmHousing.Y + 43, 200, 200);
            minimap            = Add(new MiniMap(mmHousing));
            mmShowBorders      = new Rectangle(MinimapDisplayRect.X, MinimapDisplayRect.Y - 25, 32, 32);
            SelectedStuffRect  = new Rectangle(0, height - 247, 407, 242);
            ShipInfoUIElement  = new ShipInfoUIElement(SelectedStuffRect, ScreenManager, this);
            SystemInfoOverlay            = new SolarsystemOverlay(SelectedStuffRect, ScreenManager, this);
            pInfoUI            = new PlanetInfoUIElement(SelectedStuffRect, ScreenManager, this);
            shipListInfoUI     = new ShipListInfoUIElement(SelectedStuffRect, ScreenManager, this);
            vuiElement         = new VariableUIElement(SelectedStuffRect, ScreenManager, this);
            EmpireUI           = new EmpireUIOverlay(player, device, this);

            if (GlobalStats.RenderBloom)
            {
                bloomComponent = new BloomComponent(ScreenManager);
                bloomComponent.LoadContent();
            }

            SurfaceFormat backBufferFormat = device.PresentationParameters.BackBufferFormat;
            MainTarget    = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            LightsTarget  = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            BorderRT      = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);

            NotificationManager.ReSize();

            CreateFogMap(content, device, backBufferFormat);
            LoadMenu();

            xnaPlanetModel = content.Load<Model>("Model/SpaceObjects/planet");
            atmoModel      = content.Load<Model>("Model/sphere");
            AtmoEffect     = content.Load<Effect>("Effects/PlanetHalo");
            cloudTex       = content.Load<Texture2D>("Model/SpaceObjects/earthcloudmap");
            RingTexture    = content.Load<Texture2D>("Model/SpaceObjects/planet_rings");

            Glows = new Map<PlanetGlow, SubTexture>(new []
            {
                (PlanetGlow.Terran, ResourceManager.Texture("PlanetGlows/Glow_Terran")),
                (PlanetGlow.Red,    ResourceManager.Texture("PlanetGlows/Glow_Red")),
                (PlanetGlow.White,  ResourceManager.Texture("PlanetGlows/Glow_White")),
                (PlanetGlow.Aqua,   ResourceManager.Texture("PlanetGlows/Glow_Aqua")),
                (PlanetGlow.Orange, ResourceManager.Texture("PlanetGlows/Glow_Orange"))
            });

            Arc15  = ResourceManager.Texture("Arcs/Arc15");
            Arc20  = ResourceManager.Texture("Arcs/Arc20");
            Arc45  = ResourceManager.Texture("Arcs/Arc45");
            Arc60  = ResourceManager.Texture("Arcs/Arc60");
            Arc90  = ResourceManager.Texture("Arcs/Arc90");
            Arc120 = ResourceManager.Texture("Arcs/Arc120");
            Arc180 = ResourceManager.Texture("Arcs/Arc180");
            Arc360 = ResourceManager.Texture("Arcs/Arc360");

            FTLManager.LoadContent(this);
            MuzzleFlashManager.LoadContent(content);
            ScreenRectangle = new Rectangle(0, 0, width, height);
            StarField = new StarField(this);

            ShipsInCombat = ButtonMediumMenu(width - 275, height - 280, "Ships: 0");
            ShipsInCombat.DynamicText = () =>
            {
                ShipsInCombat.Style = player.empireShipCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
                return $"Ships: {player.empireShipCombat}";
            };
            ShipsInCombat.Tooltip = "Cycle through ships not in fleet that are in combat";
            ShipsInCombat.OnClick = ShipsInCombatClick;
            Add(ShipsInCombat);

            PlanetsInCombat = ButtonMediumMenu(width - 135, height - 280, "Planets: 0");
            PlanetsInCombat.DynamicText = () =>
            {
                PlanetsInCombat.Style = player.empirePlanetCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
                return $"Planets: {player.empirePlanetCombat}";
            };
            PlanetsInCombat.OnClick = CyclePlanetsInCombat;
            PlanetsInCombat.Tooltip = "Cycle through planets that are in combat";
        }

        void ShipsInCombatClick(UIButton b)
        {
            int nbrship = 0;
            if (lastshipcombat >= player.empireShipCombat)
                lastshipcombat = 0;
            foreach (Ship ship in EmpireManager.Player.GetShips())
            {
                if (ship.fleet != null || !ship.InCombat || ship.IsHangarShip || ship.IsHomeDefense || !ship.Active)
                    continue;
                if (nbrship == lastshipcombat)
                {
                    if (SelectedShip != null && SelectedShip != previousSelection && SelectedShip != ship)
                        previousSelection = SelectedShip;
                    SelectedShip = ship;
                    ViewToShip();
                    SelectedShipList.Add(SelectedShip);
                    lastshipcombat++;
                    break;
                }

                nbrship++;
            }
        }

        void CreateFogMap(Data.GameContentManager content, GraphicsDevice device, SurfaceFormat backBufferFormat)
        {
            if (loadFogPath != null)
            {
                try
                {
                    string fogCache = $"{Dir.StarDriveAppData}/Saved Games/Fog Maps/{loadFogPath}.png";
                    using (FileStream fs = File.OpenRead(fogCache))
                    {
                        FogMap = Texture2D.FromFile(device, fs);
                    }
                }
                catch (Exception e) // whatever issue with fog map
                {
                    Log.Warning(e.Message);
                }
            }

            if (FogMap == null)
            {
                FogMap = ResourceManager.Texture2D("UniverseFeather");
            }

            FogMapTarget = new RenderTarget2D(device, 512, 512, 1, backBufferFormat,
                device.PresentationParameters.MultiSampleType,
                device.PresentationParameters.MultiSampleQuality);
            basicFogOfWarEffect = content.Load<Effect>("Effects/BasicFogOfWar");
        }

        public override void UnloadContent()
        {
            Log.Write(ConsoleColor.Cyan, "UniverseScreen.UnloadContent");
            ScreenManager.UnloadSceneObjects();
            base.UnloadContent();
        }

        public override void Update(float fixedDeltaTime)
        {
            if (LookingAtPlanet)
                workersPanel.Update(fixedDeltaTime);
            
            DeepSpaceBuildWindow.Update(fixedDeltaTime);
            pieMenu.Update(fixedDeltaTime);
            SelectedSomethingTimer -= fixedDeltaTime;

            if (++SelectorFrame > 299)
                SelectorFrame = 0;

            MusicCheckTimer -= fixedDeltaTime;
            if (MusicCheckTimer <= 0f)
            {
                MusicCheckTimer = 2f;
                if (ScreenManager.Music.IsStopped)
                    ScreenManager.Music = GameAudio.PlayMusic("AmbientMusic");
            }

            NotificationManager.Update(fixedDeltaTime);

            // if the debug window hits a cyclic crash it can be turned off in game.
            // i don't see a point in crashing the game because of a debug window error.
            try
            {
                if (Debug)
                    DebugWin?.Update(fixedDeltaTime);
            }
            catch
            {
                Debug = false;
                Log.Warning("DebugWindowCrashed");
            }

            GameAudio.Update3DSound(new Vector3(CamPos.X, CamPos.Y, 0.0f));

            ScreenManager.UpdateSceneObjects(fixedDeltaTime);
            EmpireUI.Update(fixedDeltaTime);
            UpdateSelectedItems(GameBase.Base.Elapsed);

            base.Update(fixedDeltaTime);
        }

        void UpdateSelectedItems(UpdateTimes elapsed)
        {
            if (ShowSystemInfoOverlay)
            {
                SystemInfoOverlay.SetSystem(SelectedSystem);
                SystemInfoOverlay.Update(elapsed);
            }

            if (ShowPlanetInfo)
            {
                pInfoUI.SetPlanet(SelectedPlanet);
                pInfoUI.Update(elapsed);
            }
            else if (ShowShipInfo)
            {
                ShipInfoUIElement.SetShip(SelectedShip);
                ShipInfoUIElement.Update(elapsed);
            }
            else if (ShowShipList)
            {
                shipListInfoUI.Update(elapsed);
            }
            else if (ShowFleetInfo)
            {
                shipListInfoUI.Update(elapsed);
            }
        }

        void AutoSaveCurrentGame()
        {
            var savedGame = new SavedGame(this, "Autosave" + Auto);
            if (++Auto > 3) Auto = 1;
        }

        void ProjectPieMenu(Vector2 position, float z)
        {
            Vector3 proj = Viewport.Project(position.ToVec3(z), Projection, View, Matrix.Identity);
            pieMenu.Position    = proj.ToVec2();
            pieMenu.Radius      = 75f;
            pieMenu.ScaleFactor = 1f;
        }

        void UnloadGraphics()
        {
            Log.Write(ConsoleColor.Cyan, "Universe.UnloadGraphics");
            bloomComponent?.Dispose(ref bloomComponent);
            bg3d          ?.Dispose(ref bg3d);
            StarField     ?.Dispose(ref StarField);
            FogMap      ?.Dispose(ref FogMap);
            FogMapTarget?.Dispose(ref FogMapTarget);
            BorderRT    ?.Dispose(ref BorderRT);
            MainTarget  ?.Dispose(ref MainTarget);
            LightsTarget?.Dispose(ref LightsTarget);

            beamflashes             ?.Dispose(ref beamflashes);
            explosionParticles      ?.Dispose(ref explosionParticles);
            photonExplosionParticles?.Dispose(ref photonExplosionParticles);
            explosionSmokeParticles ?.Dispose(ref explosionSmokeParticles);
            projectileTrailParticles?.Dispose(ref projectileTrailParticles);
            fireTrailParticles      ?.Dispose(ref fireTrailParticles);
            smokePlumeParticles     ?.Dispose(ref smokePlumeParticles);
            fireParticles           ?.Dispose(ref fireParticles);
            engineTrailParticles    ?.Dispose(ref engineTrailParticles);
            flameParticles          ?.Dispose(ref flameParticles);
            SmallflameParticles     ?.Dispose(ref SmallflameParticles);
            sparks                  ?.Dispose(ref sparks);
            lightning               ?.Dispose(ref lightning);
            flash                   ?.Dispose(ref flash);
            star_particles          ?.Dispose(ref star_particles);
            neb_particles           ?.Dispose(ref neb_particles);
        }

        protected override void Destroy()
        {
            UnloadGraphics();

            ItemsToBuild       ?.Dispose(ref ItemsToBuild);
            anomalyManager     ?.Dispose(ref anomalyManager);
            BombList           ?.Dispose(ref BombList);
            SelectedShipList   ?.Dispose(ref SelectedShipList);
            NotificationManager?.Dispose(ref NotificationManager);

            base.Destroy();
        }

        public override void ExitScreen()
        {
            IsExiting = true;

            Thread processTurnsThread = ProcessTurnsThread;
            ProcessTurnsThread = null;
            DrawCompletedEvt.Set(); // notify processTurnsThread that we're terminating
            processTurnsThread?.Join(250);

            RemoveLighting();
            ScreenManager.Music.Stop();

            Objects.Clear();
            ClearSolarSystems();
            ClearSpaceJunk();

            foreach (Empire empire in EmpireManager.Empires)
                empire.CleanOut();
            EmpireManager.Clear();

            ShipToView = null;
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
            SolarSystemDict.Clear();

            Spatial.Destroy();

            Empire.Universe = null;
            StatTracker.Reset();

            base.ExitScreen();
            Dispose(); // will call Destroy() and UnloadGraphics()

            HelperFunctions.CollectMemory();
        }

        void ClearSolarSystems()
        {
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                solarSystem.FiveClosestSystems.Clear();
                foreach (Planet planet in solarSystem.PlanetList)
                {
                    planet.TilesList = new Array<PlanetGridSquare>();
                    if (planet.SO != null)
                    {
                        ScreenManager.RemoveObject(planet.SO);
                        planet.SO = null;
                    }
                }

                foreach (Asteroid asteroid in solarSystem.AsteroidsList)
                {
                    asteroid.DestroySceneObject();
                }
                solarSystem.AsteroidsList.Clear();

                foreach (Moon moon in solarSystem.MoonList)
                {
                    moon.DestroySceneObject();
                }
                solarSystem.MoonList.Clear();
            }
            SolarSystemList.Clear();
        }

        void ClearSpaceJunk()
        {
            JunkList.ApplyPendingRemovals();
            foreach (SpaceJunk spaceJunk in JunkList)
                spaceJunk.DestroySceneObject();
            JunkList.Clear();
        }


        // Refactored by RedFox
        // this draws the colored empire borders
        // the borders are drawn into a separate framebuffer texture and later blended with final visual

        SubTexture Arc15;
        SubTexture Arc20;
        SubTexture Arc45;
        SubTexture Arc60;
        SubTexture Arc90;
        SubTexture Arc120;
        SubTexture Arc180;
        SubTexture Arc360;

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

        public struct ClickablePlanets
        {
            public Vector2 ScreenPos;
            public float Radius;
            public Planet planetToClick;
            public bool HitTest(Vector2 touch) => touch.InRadius(ScreenPos, Radius);
        }

        public struct ClickableShip
        {
            public Vector2 ScreenPos;
            public float Radius;
            public Ship shipToClick;
            public bool HitTest(Vector2 touch) => touch.InRadius(ScreenPos, Radius);
        }

        struct ClickableSystem
        {
            public Vector2 ScreenPos;
            public float Radius;
            public SolarSystem systemToClick;
            public bool Touched(Vector2 touchPoint)
            {
                if (!touchPoint.InRadius(ScreenPos, Radius)) return false;

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
            public bool HitTest(Vector2 touch) => touch.InRadius(ScreenPos, Radius);
        }

        struct ClickableFleet
        {
            public Fleet fleet;
            public Vector2 ScreenPos;
            public float ClickRadius;
            public bool HitTest(Vector2 touch) => touch.InRadius(ScreenPos, ClickRadius);
        }
        public enum UnivScreenState
        {
            DetailView = 7000,
            ShipView   = 15000,
            PlanetView = 35000,
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

        struct FleetButton
        {
            public Rectangle ClickRect;
            public Fleet Fleet;
            public int Key;
        }

        public class FogOfWarNode
        {
            public float Radius = 50000f;
            public Vector2 Position;
            public bool Discovered;
        }


        enum CursorState
        {
            Normal,
            Move,
            Follow,
            Attack,
            Orbit
        }
    }
}

