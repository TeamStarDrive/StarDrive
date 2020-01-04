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
using Ship_Game.Universe;

namespace Ship_Game
{
    public partial class UniverseScreen : GameScreen
    {
        readonly PerfTimer EmpireUpdatePerf  = new PerfTimer();
        readonly PerfTimer Perfavg2          = new PerfTimer();
        readonly PerfTimer PreEmpirePerf     = new PerfTimer();
        readonly PerfTimer perfavg4          = new PerfTimer();
        readonly PerfTimer perfavg5          = new PerfTimer();

        public static readonly SpatialManager SpaceManager = new SpatialManager();
        public static Array<SolarSystem> SolarSystemList = new Array<SolarSystem>();
        public static BatchRemovalCollection<SpaceJunk> JunkList = new BatchRemovalCollection<SpaceJunk>();
        public float GamePace = 1f;
        public float GameScale = 1f;
        public float GameSpeed = 1f;
        public float StarDate = 1000f;
        public string StarDateString => StarDate.StarDateString();
        public float perStarDateTimer = 1000f;
        public float AutoSaveTimer = GlobalStats.AutoSaveFreq;
        public Array<ClickablePlanets> ClickPlanetList = new Array<ClickablePlanets>();
        public BatchRemovalCollection<ClickableItemUnderConstruction> ItemsToBuild = new BatchRemovalCollection<ClickableItemUnderConstruction>();
        Array<ClickableSystem> ClickableSystems    = new Array<ClickableSystem>();
        public BatchRemovalCollection<Ship> SelectedShipList = new BatchRemovalCollection<Ship>();
        Array<ClickableShip> ClickableShipsList    = new Array<ClickableShip>();
        Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        public BatchRemovalCollection<Ship> MasterShipList;
        public Background bg;
        public float UniverseSize       = 5000000f; // universe width and height in world units
        public float FTLModifier        = 1f;
        public float EnemyFTLModifier   = 1f;
        public bool FTLInNuetralSystems = true;
        public Vector3 transitionStartPosition;
        public Vector3 camTransitionPosition;
        public Array<NebulousOverlay> Stars        = new Array<NebulousOverlay>();
        public Array<NebulousOverlay> NebulousShit = new Array<NebulousOverlay>();
        Rectangle ScreenRectangle;
        public Map<Guid, Planet> PlanetsDict          = new Map<Guid, Planet>();
        public Map<Guid, SolarSystem> SolarSystemDict = new Map<Guid, SolarSystem>();
        public BatchRemovalCollection<Bomb> BombList  = new BatchRemovalCollection<Bomb>();
        readonly AutoResetEvent DrawCompletedEvt         = new AutoResetEvent(false);
        readonly AutoResetEvent ProcessTurnsCompletedEvt = new AutoResetEvent(true);
        public float CamHeight = 2550f;
        public Vector3 CamPos = Vector3.Zero;
        float TooltipTimer = 0.5f;
        float sTooltipTimer = 0.5f;
        GameTime SimulationTime = new GameTime();
        float TurnFlipCounter;
        int Auto = 1;
        AutoResetEvent   ShipGateKeeper         = new AutoResetEvent(false);
        ManualResetEvent SystemThreadGateKeeper = new ManualResetEvent(false);
        AutoResetEvent   DeepSpaceGateKeeper    = new AutoResetEvent(false);
        ManualResetEvent DeepSpaceDone          = new ManualResetEvent(false);
        AutoResetEvent   EmpireGateKeeper       = new AutoResetEvent(false);
        ManualResetEvent EmpireDone             = new ManualResetEvent(false);
        Array<Ship> DeepSpaceShips  = new Array<Ship>();
        public bool ViewingShip             = false;
        public float transDuration          = 3f;
        public Vector2 mouseWorldPos;
        public float SelectedSomethingTimer = 3f;
        Array<FleetButton> FleetButtons = new Array<FleetButton>();
        public Array<FogOfWarNode> FogNodes = new Array<FogOfWarNode>();
        bool drawBloom = GlobalStats.RenderBloom; //true
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
        public RenderTarget2D MiniMapSector;
        public RenderTarget2D BorderRT;
        public RenderTarget2D StencilRT;
        RenderTarget2D LightsTarget;
        public Effect basicFogOfWarEffect;
        public Rectangle SectorMap;
        public Rectangle SectorSourceRect;
        public Rectangle GalaxyMap;
        public Rectangle SelectedStuffRect;
        public NotificationManager NotificationManager;
        public Rectangle MinimapDisplayRect;
        public Rectangle mmShowBorders;
        public Rectangle mmDSBW;
        public Rectangle mmShipView;
        public Rectangle mmAutomation;
        public Rectangle mmGalaxyView;
        public Rectangle mmHousing;
        protected float MaxCamHeight;
        public AnomalyManager anomalyManager;
        public ShipInfoUIElement ShipInfoUIElement;
        public PlanetInfoUIElement pInfoUI;
        public SystemInfoUIElement sInfoUI;
        public ShipListInfoUIElement shipListInfoUI;
        public VariableUIElement vuiElement;
        float ArmageddonTimer;
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
        int ArmageddonCounter;
        float shiptimer;
        public Ship ShipToView;
        public float HeightOnSnap;
        public float AdjustCamTimer;
        public bool SnapBackToSystem;
        public AutomationWindow aw;
        public bool DefiningAO; // are we defining a new AO?
        public bool DefiningTradeRoutes; // are we defining  trade routes for a freighter?
        public Rectangle AORect; // used for showing current AO Rect definition
        public bool showingDSBW;

        public bool showingFTLOverlay;
        public bool showingRangeOverlay;

        public DeepSpaceBuildingWindow dsbw;
        public DebugInfoScreen DebugWin;
        public bool ShowShipNames;
        float Memory;
        public bool Paused = true; // always start paused
        public bool NoEliminationVictory;
        bool UseRealLights = true;
        public SolarSystem SelectedSystem;
        public Fleet SelectedFleet;
        int FBTimer;
        bool pickedSomethingThisFrame;
        bool SelectingWithBox;
        Effect AtmoEffect;
        Model atmoModel;
        public PlanetScreen workersPanel;
        ResolveTexture2D sceneMap;
        CursorState cState;
        float radlast;
        int SelectorFrame;
        public int globalshipCount;
        public int empireShipCountReserve;
        readonly Array<GameplayObject> GamePlayObjectToRemove = new Array<GameplayObject>();
        public float Lag = 0;
        public Ship previousSelection;

        public UIButton ShipsInCombat;
        public UIButton PlanetsInCombat;
        public int lastshipcombat   = 0;
        public int lastplanetcombat = 0;
        public int PathMapReducer   = 1;
        public float screenDelay    = 0f;
        public SubSpaceProjectors SubSpaceProjectors;

        ShipMoveCommands ShipCommands;

        // for really specific debugging
        public static int FrameId;

        public bool IsViewingCombatScreen(Planet p) => LookingAtPlanet && workersPanel is CombatScreen cs && cs.p == p;

        public UniverseScreen(UniverseData data, Empire loyalty) : base(null) // new game
        {
            Name = "UniverseScreen";
            UniverseSize          = data.Size.X;
            FTLModifier           = data.FTLSpeedModifier;
            EnemyFTLModifier      = data.EnemyFTLSpeedModifier;
            GravityWells          = data.GravityWells;
            SolarSystemList       = data.SolarSystemsList;
            MasterShipList        = data.MasterShipList;
            PlayerEmpire          = loyalty;
            player                = loyalty;
            PlayerLoyalty         = loyalty.data.Traits.Name;
            PlayerEmpire.isPlayer = true;
            SubSpaceProjectors    = new SubSpaceProjectors(UniverseSize);
            SpaceManager.Setup(UniverseSize);
            DoPathingMapRebuild();
            ShipCommands = new ShipMoveCommands(this);
        }

        public UniverseScreen(UniverseData data, string loyalty) : base(null) // savegame
        {
            Name = "UniverseScreen";
            UniverseSize          = data.Size.X;
            FTLModifier           = data.FTLSpeedModifier;
            EnemyFTLModifier      = data.EnemyFTLSpeedModifier;
            GravityWells          = data.GravityWells;
            SolarSystemList       = data.SolarSystemsList;
            MasterShipList        = data.MasterShipList;
            loadFogPath           = data.loadFogPath;
            PlayerEmpire          = EmpireManager.GetEmpireByName(loyalty);
            player                = PlayerEmpire;
            PlayerLoyalty         = loyalty;
            PlayerEmpire.isPlayer = true;
            loading               = true;
            SubSpaceProjectors    = new SubSpaceProjectors(UniverseSize);
            SpaceManager.Setup(UniverseSize);
            DoPathingMapRebuild();
            ShipCommands = new ShipMoveCommands(this);
        }

        public void ResetLighting() => SetLighting(UseRealLights);

        public Planet GetPlanet(Guid guid)
        {
            if (PlanetsDict.TryGetValue(guid, out Planet planet))
                return planet;
            Log.Error($"Guid for planet not found guid: {guid}");
            return null;
        }

        void SetLighting(bool useRealLights)
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
            if (SelectedShip.loyalty.isFaction)
            {
                foreach (Encounter e in ResourceManager.Encounters)
                {
                    if (SelectedShip.loyalty.data.Traits.Name == e.Faction && player.GetRelations(SelectedShip.loyalty).EncounterStep == e.Step)
                    {
                        EncounterPopup.Show(this, player, SelectedShip.loyalty, e);
                        break;
                    }
                }
            }
            else
            {
                DiplomacyScreen.Show(SelectedShip.loyalty, player, "Greeting");
            }
        }

        void CreateProjectionMatrix()
        {
            float aspect = (float)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspect, 100f, 3E+07f);
        }

        public override void LoadContent()
        {
            RemoveAll();
            GlobalStats.ResearchRootUIDToDisplay = "Colonization";
            SystemInfoUIElement.SysFont  = Fonts.Arial12Bold;
            SystemInfoUIElement.DataFont = Fonts.Arial10;
            NotificationManager = new NotificationManager(ScreenManager, this);
            aw = Add(new AutomationWindow(this));
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
            CreateStartingShips();
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                SpawnRemnantsInSolarSystem(solarSystem);
                foreach (Planet p in solarSystem.PlanetList)
                {
                    if (p.Owner != null)
                    {
                        foreach (string key in p.Guardians)
                        {
                            Ship.CreateShipAt(key, p.Owner, p, true);
                        }

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
                            {
                                Ship guardian = Ship.CreateShipAt(key, EmpireManager.Remnants, p, RandomMath.Vector2D(p.ObjectRadius * 2), true);
                                guardian.IsGuardian = true;
                            }
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

                Vector3 univTopLeft  = Viewport.Project(Vector3.Zero, Projection, camMaxToUnivCenter, Matrix.Identity);
                Vector3 univBotRight = Viewport.Project(new Vector3(UniverseSize, UniverseSize, 0.0f), Projection, camMaxToUnivCenter, Matrix.Identity);
                univSizeOnScreen = univBotRight.X - univTopLeft.X;
                if (univSizeOnScreen < (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 50))
                    MaxCamHeight -= 0.1f * MaxCamHeight;
            }
            if (MaxCamHeight > 23000000)
                MaxCamHeight = 23000000;

            if (!loading)
            {
                CamPos.X = PlayerEmpire.GetPlanets()[0].Center.X;
                CamPos.Y = PlayerEmpire.GetPlanets()[0].Center.Y;
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
                    ship.TetherToPlanet(GetPlanet(ship.TetherGuid));
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

        private void CreateStartingShips()
        {
            if (StarDate > 1000f) // not a new game
                return;

            foreach (Empire empire in EmpireManager.Empires)
            {
                if (empire.isFaction)
                    continue;

                Planet homePlanet    = empire.GetPlanets()[0];
                string colonyShip    = empire.data.DefaultColonyShip;
                string startingScout = empire.data.StartingScout;
                string starterShip   = empire.data.Traits.Prototype == 0
                                       ? empire.data.StartingShip
                                       : empire.data.PrototypeShip;

                if (GlobalStats.HardcoreRuleset)
                {
                    colonyShip    += " STL";
                    startingScout += " STL";
                    starterShip   += " STL";
                }

                Ship.CreateShipAt(starterShip, empire, homePlanet, new Vector2(350f, 0.0f), true);
                Ship.CreateShipAt(colonyShip, empire, homePlanet, new Vector2(-2000, -2000), true);
                Ship.CreateShipAt(startingScout, empire, homePlanet, new Vector2(-2500, -2000), true);
            }
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
                CreateDefensiveRemnantFleet(fleetAndPos.FleetName, solarSystem.Position + fleetAndPos.Pos, 75000f);

            foreach (string key in solarSystem.ShipsToSpawn)
                Ship.CreateShipAt(key, EmpireManager.Remnants, solarSystem.PlanetList[0], true);
        }

        public static void CreateDefensiveRemnantFleet(string fleetUid, Vector2 where, float defenseRadius)
        {
            if (EmpireManager.Remnants == null)
                return;
            Fleet defensiveFleetAt = HelperFunctions.CreateFleetAt(fleetUid, EmpireManager.Remnants, where, CombatState.Artillery);
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

        void DoParticleLoad()
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
            int width   = GameBase.ScreenWidth;
            int height  = GameBase.ScreenHeight;

            Empire.Universe = this;

            CreateProjectionMatrix();
            bg = new Background();
            Frustum            = new BoundingFrustum(View * Projection);
            mmHousing          = new Rectangle(width - (276 + minimapOffSet), height - 256, 276 + minimapOffSet, 256);
            MinimapDisplayRect = new Rectangle(mmHousing.X + 61 + minimapOffSet, mmHousing.Y + 43, 200, 200);
            minimap            = Add(new MiniMap(mmHousing));
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
            SurfaceFormat backBufferFormat = device.PresentationParameters.BackBufferFormat;
            sceneMap      = new ResolveTexture2D(device, width, height, 1, backBufferFormat);
            MainTarget    = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            LightsTarget  = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            MiniMapSector = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            BorderRT      = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);
            StencilRT     = BloomComponent.CreateRenderTarget(device, 1, backBufferFormat);

            NotificationManager.ReSize();

            if (loadFogPath != null)
            {
                try
                {
                    using (FileStream fs = File.OpenRead($"{Dir.StarDriveAppData}/Saved Games/Fog Maps/{loadFogPath}.png"))
                        FogMap = Texture2D.FromFile(device, fs);
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

            FogMapTarget = new RenderTarget2D(device, 512, 512, 1, backBufferFormat, device.PresentationParameters.MultiSampleType, device.PresentationParameters.MultiSampleQuality);
            basicFogOfWarEffect = content.Load<Effect>("Effects/BasicFogOfWar");
            LoadMenu();

            anomalyManager = new AnomalyManager();
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
            PlanetsInCombat = ButtonMediumMenu(width - 135, height - 280, "Planets: 0");
        }

        public override void UnloadContent()
        {
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

            float gameTimeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            SelectedSomethingTimer -= gameTimeDelta;

            if (++SelectorFrame > 299) SelectorFrame = 0;

            MusicCheckTimer -= gameTimeDelta;
            if (MusicCheckTimer <= 0.0f)
            {
                MusicCheckTimer = 2f;
                if (ScreenManager.Music.IsStopped)
                    ScreenManager.Music = GameAudio.PlayMusic("AmbientMusic");
            }

            GameAudio.Update3DSound(new Vector3(CamPos.X, CamPos.Y, 0.0f));

            ScreenManager.UpdateSceneObjects();
            EmpireUI.Update(deltaTime);

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public void DoAutoSave()
        {
            SavedGame savedGame = new SavedGame(this, "Autosave " + Auto);
            if (++Auto > 3) Auto = 1;
        }

        void ProjectPieMenu(Vector2 position, float z)
        {
            var proj = Viewport.Project(position.ToVec3(z), Projection, View, Matrix.Identity);
            pieMenu.Position    = proj.ToVec2();
            pieMenu.Radius      = 75f;
            pieMenu.ScaleFactor = 1f;
        }

        public void PlayNegativeSound() => GameAudio.NegativeClick();

        //added by gremlin replace redundant code with method
        public override void ExitScreen()
        {
            IsExiting = true;
            Thread processTurnsThread = ProcessTurnsThread;
            ProcessTurnsThread = null;
            DrawCompletedEvt.Set(); // notify processTurnsThread that we're terminating
            processTurnsThread?.Join(250);
            EmpireUI = null;

            //SpaceManager.Destroy();
            ScreenManager.Music.Stop();
            NebulousShit.Clear();
            bloomComponent = null;
            bg3d?.Dispose(ref bg3d);
            StarField?.Dispose();

            ShipToView = null;
            for (int i = 0; i < MasterShipList.Count; ++i)
                MasterShipList[i]?.RemoveFromUniverseUnsafe();
            MasterShipList.ClearPendingRemovals();
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
            SolarSystemList.Clear();
            SolarSystemDict.Clear();

            beamflashes?.UnloadContent();
            explosionParticles?.UnloadContent();
            photonExplosionParticles?.UnloadContent();
            explosionSmokeParticles?.UnloadContent();
            projectileTrailParticles?.UnloadContent();
            fireTrailParticles?.UnloadContent();
            smokePlumeParticles?.UnloadContent();
            fireParticles?.UnloadContent();
            engineTrailParticles?.UnloadContent();
            flameParticles?.UnloadContent();
            SmallflameParticles?.UnloadContent();
            sparks?.UnloadContent();
            lightning?.UnloadContent();
            flash?.UnloadContent();
            star_particles?.UnloadContent();
            neb_particles?.UnloadContent();

            Empire.Universe = null;
            StatTracker.SnapshotsDict.Clear();
            EmpireManager.Clear();

            HelperFunctions.CollectMemory();
            base.ExitScreen();
            Dispose();
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

        public void QueueGameplayObjectRemoval(GameplayObject gameplayObject)
        {
            if (gameplayObject == null) return;
            GamePlayObjectToRemove.Add(gameplayObject);
        }

        public void TotallyRemoveGameplayObjects()
        {
            while (GamePlayObjectToRemove.TryPopLast(out GameplayObject toRemove))
                toRemove.RemoveFromUniverseUnsafe();
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

        struct ClickableSystem
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

        struct ClickableFleet
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

