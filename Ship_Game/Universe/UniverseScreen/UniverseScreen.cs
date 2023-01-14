using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;
using System;
using System.Threading;
using SDGraphics;
using SDGraphics.Sprites;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Universe;
using Ship_Game.Fleets;
using Ship_Game.Graphics;
using Ship_Game.Graphics.Particles;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Rectangle = SDGraphics.Rectangle;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public partial class UniverseScreen : GameScreen
    {
        // The non-visible state of the Universe
        public readonly UniverseState UState;

        SpriteRenderer SR;

        public string StarDateString => UState.StarDate.StarDateString();
        public float LastAutosaveTime = 0;

        public Array<Ship> SelectedShipList = new Array<Ship>();

        public ClickablePlanet[] ClickablePlanets = Empty<ClickablePlanet>.Array;
        ClickableSystem[] ClickableSystems = Empty<ClickableSystem>.Array;
        ClickableShip[] ClickableShips = Empty<ClickableShip>.Array;
        public ClickableSpaceBuildGoal[] ClickableBuildGoals = Empty<ClickableSpaceBuildGoal>.Array;

        readonly Array<ClickableFleet> ClickableFleetsList = new Array<ClickableFleet>();
        public Planet SelectedPlanet;
        public Ship SelectedShip;
        public ClickableSpaceBuildGoal SelectedItem;
        ClickablePlanet TippedPlanet;
        ClickableSystem tippedSystem;

        Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);
        
        public Background bg;

        public BatchRemovalCollection<Bomb> BombList  = new();
        readonly AutoResetEvent DrawCompletedEvt = new(false);

        public const double MinCamHeight = 450.0;
        protected double MaxCamHeight;
        public Vector3d CamDestination;
        public Vector3d CamPos { get => UState.CamPos; set => UState.CamPos = value; }
        public Vector3d transitionStartPosition;

        float TooltipTimer = 0.5f;
        float sTooltipTimer = 0.5f;
        public bool ViewingShip = false;
        public float transDuration = 3f;
        public float SelectedSomethingTimer = 3f;
        public Vector2 mouseWorldPos;

        FleetButton[] FleetButtons = Empty<FleetButton>.Array;
        public bool ShowTacticalCloseup { get; private set; }
        public bool Debug => UState.Debug;
        public DebugModes DebugMode => UState.DebugMode;

        PieMenu pieMenu;
        PieMenuNode planetMenu;
        PieMenuNode shipMenu;

        public ParticleManager Particles;

        public Background3D bg3d;
        public Empire Player => UState.Player;
        public string PlayerLoyalty => Player.data.Traits.Name;

        public UnivScreenState viewState { get => UState.ViewState; set => UState.ViewState = value; }
        public bool LookingAtPlanet;
        public bool snappingToShip;
        public bool returnToShip;
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
        public ShieldManager Shields;
        public Rectangle MinimapDisplayRect;
        public Rectangle mmShowBorders;
        public Rectangle mmHousing;
        public AnomalyManager anomalyManager;
        public ShipInfoUIElement ShipInfoUIElement;
        public PlanetInfoUIElement pInfoUI;
        public SolarsystemOverlay SystemInfoOverlay;
        public ShipListInfoUIElement shipListInfoUI;
        public VariableUIElement vuiElement;
        MiniMap minimap;
        bool loading;
        public float transitionElapsedTime;

        // @note Initialize with a default frustum for UnitTests
        public BoundingFrustum Frustum = new(Matrix.CreateTranslation(1000000, 1000000, 0));

        bool ShowingSysTooltip;
        bool ShowingPlanetToolTip;
        float MusicCheckTimer;
        public Ship ShipToView;
        public float AdjustCamTimer;
        public AutomationWindow aw;
        public bool DefiningAO; // are we defining a new AO?
        public bool DefiningTradeRoutes; // are we defining  trade routes for a freighter?
        public Rectangle AORect; // used for showing current AO Rect definition

        public bool ShowingFTLOverlay;
        public bool ShowingRangeOverlay;

        /// <summary>
        /// Toggles Cinematic Mode (no UI) on or off
        /// </summary>
        bool IsCinematicModeEnabled = false;
        float CinematicModeTextTimer = 3f;

        /// <summary>
        /// Conditions to suppress diplomacy screen popups
        /// </summary>
        public bool CanShowDiplomacyScreen => UState.CanShowDiplomacyScreen && !IsCinematicModeEnabled;

        public DeepSpaceBuildingWindow DeepSpaceBuildWindow;
        public DebugInfoScreen DebugWin;
        public bool ShowShipNames;
        bool UseRealLights = true;
        public SolarSystem SelectedSystem;
        public Fleet SelectedFleet;
        int FBTimer = 60;
        bool pickedSomethingThisFrame;
        bool SelectingWithBox;

        public PlanetScreen workersPanel;
        CursorState cState;
        int SelectorFrame;
        public Ship previousSelection;

        public UIButton ShipsInCombat;
        public UIButton PlanetsInCombat;
        public int lastshipcombat   = 0;
        public int nextPlanetCombat = 0;

        ShipMoveCommands ShipCommands;

        // for really specific debugging
        public int SimTurnId;

        // To avoid double-loading universe thread when
        // graphics setting changes cause 
        bool IsUniverseInitialized;

        public bool IsViewingCombatScreen(Planet p) => LookingAtPlanet && workersPanel is CombatScreen cs && cs.P == p;
        public bool IsViewingColonyScreen(Planet p) => LookingAtPlanet && workersPanel is ColonyScreen cs && cs.P == p;
        
        /// <summary>
        /// RADIUS of the universe, Stars are generated within XY range [-universeRadius, +universeRadius]
        /// </summary>
        public UniverseScreen(UniverseParams settings, float universeRadius) : base(null, toPause: null)
        {
            UState = new UniverseState(this, settings, universeRadius);
            Initialize();
        }

        public UniverseScreen(UniverseState state) : base(null, toPause: null) // load game
        {
            UState = state;
            UState.OnUniverseScreenLoaded(this);
            loading = true;
            Initialize();
        }

        void Initialize()
        {
            UState.EvtOnShipRemoved += Objects_OnShipRemoved;
            Name = "UniverseScreen";
            CanEscapeFromScreen = false;

            ShipCommands = new ShipMoveCommands(this);
            DeepSpaceBuildWindow = new DeepSpaceBuildingWindow(this);
        }

        void Objects_OnShipRemoved(Ship ship)
        {
            void RemoveShip()
            {
                if (SelectedShip == ship)
                    SelectedShip = null;
                SelectedShipList.RemoveRef(ship);
            }
            RunOnNextFrame(RemoveShip);
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

            float globalLightRad = (float)(UState.Size * 2 + MaxCamHeight * 10);
            float globalLightZPos = (float)(MaxCamHeight * 10);
            AddLight("Global Fill Light", new Vector2(0, 0), 0.7f, globalLightRad, Color.White, -globalLightZPos, fillLight: false, shadowQuality: 0f);
            AddLight("Global Back Light", new Vector2(0, 0), 0.6f, globalLightRad, Color.White, +globalLightZPos, fillLight: false, shadowQuality: 0f);

            foreach (SolarSystem system in UState.Systems)
            {
                system.Lights.Clear();

                Color color     = system.Sun.LightColor;
                float intensity = system.Sun.LightIntensity;
                float radius    = system.Sun.Radius;
                var light1 = AddLight("Key",               system, intensity,         radius,         color, -5500);
                var light2 = AddLight("OverSaturationKey", system, intensity * 5.00f, radius * 0.05f, color, -1500);
                var light3 = AddLight("LocalFill",         system, intensity * 0.55f, radius,         Color.White, 0);
                //AddLight("Back", system, intensity * 0.5f , radius, color, 2500, fallOff: 0, fillLight: true);
                system.Lights.Add(light1);
                system.Lights.Add(light2);
                system.Lights.Add(light3);
            }
        }

        void RemoveLighting()
        {
            ScreenManager.RemoveAllLights();
        }

        PointLight AddLight(string name, SolarSystem system, float intensity, float radius, Color color, float zpos, float fallOff = 1f, bool fillLight = false)
        {
            return AddLight($"{system.Name} - {system.Sun.Id} - {name}", system.Position, intensity, radius, color,
                            zpos, fillLight: fillLight, fallOff:fallOff, shadowQuality:0f);
        }

        PointLight AddLight(string name, Vector2 source, float intensity, float radius, Color color,
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

            light.World = Matrix.CreateTranslation((Vector3)light.Position);
            AddLight(light, dynamic:false);
            return light;
        }

        public void ContactLeader()
        {
            if (SelectedShip == null)
                return;

            Empire leaderLoyalty = SelectedShip.Loyalty;
            if (leaderLoyalty.IsFaction)
                Encounter.ShowEncounterPopUpPlayerInitiated(SelectedShip.Loyalty, this);
            else
                DiplomacyScreen.Show(SelectedShip.Loyalty, Player, "Greeting");
        }

        public override void LoadContent()
        {
            Log.Write(ConsoleColor.Cyan, "UniverseScreen.LoadContent");
            RemoveAll();
            UnloadGraphics();

            UState.ResearchRootUIDToDisplay = GlobalStats.Defaults.ResearchRootUIDToDisplay;

            NotificationManager = new(ScreenManager, this);
            aw = Add(new AutomationWindow(this));

            Shields = new(this);

            InitializeCamera(); // ResetLighting requires MaxCamHeight
            ResetLighting(forceReset: true);
            LoadGraphics();

            InitializeUniverse();
        }

        // So this should be the absolute max height for the camera
        // And this also defines the limit to Perspective Matrix's MaxDistance
        // The bigger Perspective project MaxDistance is, the less accurate our screen coordinates
        public const double CAM_MAX = 15_000_000;

        void InitializeCamera()
        {
            float univSizeOnScreen = 10f;

            MaxCamHeight = CAM_MAX;
            SetPerspectiveProjection(maxDistance: CAM_MAX);

            while (univSizeOnScreen < (ScreenWidth + 50))
            {
                float univRadius = UState.Size / 2f;
                var camMaxToUnivCenter = Matrices.CreateLookAtDown(-univRadius, univRadius, MaxCamHeight);

                Vector3 univTopLeft  = new Vector3(
                    Viewport.Project(Vector3.Zero, Projection, camMaxToUnivCenter, Matrix.Identity)
                );
                Vector3 univBotRight = new Vector3(
                    Viewport.Project(new Vector3(UState.Size * 1.25f, UState.Size * 1.25f, 0.0f), Projection, camMaxToUnivCenter, Matrix.Identity)
                );
                univSizeOnScreen = Math.Abs(univBotRight.X - univTopLeft.X);
                if (univSizeOnScreen < (ScreenWidth + 50))
                    MaxCamHeight -= 0.1 * MaxCamHeight;
            }

            if (MaxCamHeight > CAM_MAX)
                MaxCamHeight = CAM_MAX;

            if (!loading)
                CamPos = new Vector3d(Player.GetPlanets()[0].Position, 2750);

            CamDestination = CamPos;
        }

        void InitializeUniverse()
        {
            if (IsUniverseInitialized)
                return;

            IsUniverseInitialized = true;
            CreateStartingShips();
            InitializeSolarSystems();

            foreach (Empire empire in UState.Empires)
            {
                empire.InitEmpireFromSave(UState);
            }

            WarmUpShipsForLoad();
            RecomputeFleetButtons(true);

            if (UState.StarDate.AlmostEqual(1000)) // Run once to get all empire goals going
            {
                Array<Empire> updated = UpdateEmpires(FixedSimTime.Zero);
                EndOfTurnUpdate(updated, FixedSimTime.Zero);
            }
            CreateUniverseSimThread();
        }

        void CreateUniverseSimThread()
        {
            if (!CreateSimThread)
                return;
            SimThread = new Thread(UniverseSimMonitored);
            SimThread.Name = "Universe.SimThread";
            SimThread.IsBackground = false; // RedFox - make sure ProcessTurns runs with top priority
            SimThread.Start();
        }

        void InitializeSolarSystems()
        {
            anomalyManager = new AnomalyManager();

            foreach (SolarSystem system in UState.Systems)
            {
                foreach (Anomaly anomaly in system.AnomaliesList)
                {
                    if (anomaly.type == "DP")
                    {
                        anomalyManager.AnomaliesList.Add(new DimensionalPrison(UState, system.Position + anomaly.Position));
                    }
                }

                foreach (Empire empire in UState.ActiveEmpires)
                {
                    system.UpdateFullyExploredBy(empire);
                }

                foreach (Planet planet in system.PlanetList)
                {
                    planet.InitializePlanetMesh();
                }
            }
        }

        void CreateStartingShips()
        {
            // not a new game or load game at stardate 1000 
            if (UState.StarDate > 1000f || UState.Ships.Count > 0)
                return;

            foreach (Empire empire in UState.MajorEmpires)
            {
                Planet homePlanet    = empire.GetPlanets()[0];
                string colonyShip    = empire.data.DefaultColonyShip;
                string startingScout = empire.data.StartingScout;
                string freighter     = empire.data.DefaultSmallTransport;
                string starterShip   = empire.data.Traits.Prototype == 0
                                       ? empire.data.StartingShip
                                       : empire.data.PrototypeShip;

                Ship.CreateShipAt(UState, starterShip, empire, homePlanet, RandomMath.Vector2D(homePlanet.Radius * 3), true);
                Ship.CreateShipAt(UState, colonyShip, empire, homePlanet, RandomMath.Vector2D(homePlanet.Radius * 2), true);
                Ship.CreateShipAt(UState, freighter, empire, homePlanet, RandomMath.Vector2D(homePlanet.Radius * 2), true);
                Ship.CreateShipAt(UState, startingScout, empire, homePlanet, RandomMath.Vector2D(homePlanet.Radius * 3), true);
            }
        }

        void LoadGraphics()
        {
            const int minimapOffSet = 14;

            var device  = ScreenManager.GraphicsDevice;
            int width   = GameBase.ScreenWidth;
            int height  = GameBase.ScreenHeight;

            Particles = new ParticleManager(TransientContent);
            SR = new SpriteRenderer(device);

            if (GlobalStats.DrawStarfield)
            {
                bg = new Background(this, device);
            }

            if (GlobalStats.DrawNebulas)
            {
                bg3d = new Background3D(this);
            }

            Frustum = new BoundingFrustum(ViewProjection);
            mmHousing = new Rectangle(width - (276 + minimapOffSet), height - 256, 276 + minimapOffSet, 256);
            minimap = Add(new MiniMap(this, mmHousing));

            MinimapDisplayRect = new Rectangle(mmHousing.X + 61 + minimapOffSet, mmHousing.Y + 43, 200, 200);
            mmShowBorders = new Rectangle(MinimapDisplayRect.X, MinimapDisplayRect.Y - 25, 32, 32);

            SelectedStuffRect = new Rectangle(0, height - 247, 407, 242);
            ShipInfoUIElement = new ShipInfoUIElement(SelectedStuffRect, ScreenManager, this);
            SystemInfoOverlay = new SolarsystemOverlay(SelectedStuffRect, ScreenManager, this);
            pInfoUI           = new PlanetInfoUIElement(SelectedStuffRect, ScreenManager, this);
            shipListInfoUI    = new ShipListInfoUIElement(SelectedStuffRect, ScreenManager, this);
            vuiElement        = new VariableUIElement(SelectedStuffRect, ScreenManager, this);
            EmpireUI          = new EmpireUIOverlay(Player, device, this);

            if (GlobalStats.RenderBloom)
            {
                bloomComponent = new BloomComponent(ScreenManager);
                bloomComponent.LoadContent();
            }

            MainTarget   = RenderTargets.Create(device);
            LightsTarget = RenderTargets.Create(device);
            BorderRT     = RenderTargets.Create(device);

            NotificationManager.ReSize();

            CreateFogMap(TransientContent, device);
            LoadMenu();

            FTLManager.LoadContent(this);

            ShipsInCombat = ButtonMediumMenu(width - 275, height - 280, "Ships: 0");
            ShipsInCombat.DynamicText = () =>
            {
                ShipsInCombat.Style = Player.empireShipCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
                return $"Ships: {Player.empireShipCombat}";
            };
            ShipsInCombat.Tooltip = "Cycle through ships not in fleet that are in combat";
            ShipsInCombat.OnClick = ShipsInCombatClick;
            Add(ShipsInCombat);

            PlanetsInCombat = ButtonMediumMenu(width - 135, height - 280, "Planets: 0");
            PlanetsInCombat.DynamicText = () =>
            {
                PlanetsInCombat.Style = Player.empirePlanetCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
                return $"Planets: {Player.empirePlanetCombat}";
            };
            PlanetsInCombat.OnClick = CyclePlanetsInCombat;
            PlanetsInCombat.Tooltip = "Cycle through planets that are in combat";
        }

        void ShipsInCombatClick(UIButton b)
        {
            int nbrship = 0;
            if (lastshipcombat >= Player.empireShipCombat)
                lastshipcombat = 0;
            var ships = Player.OwnedShips;
            foreach (Ship ship in ships)
            {
                if (ship.Fleet != null || ship.OnLowAlert || ship.IsHangarShip || ship.IsHomeDefense || !ship.Active)
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

        void CreateFogMap(Data.GameContentManager content, GraphicsDevice device)
        {
            if (UState.FogMapBytes != null)
            {
                FogMap = content.RawContent.TexImport.FromAlphaOnly(UState.FogMapBytes);
                UState.FogMapBytes = null; // free the mem of course, even if load failed
            }

            if (FogMap == null)
            {
                FogMap = ResourceManager.Texture2D("UniverseFeather.dds");
            }

            UpdateFogMap(ScreenManager.SpriteBatch, device); // this will change FogMap surface format
            basicFogOfWarEffect = content.Load<Effect>("Effects/BasicFogOfWar");
        }

        public override void UnloadContent()
        {
            UState.Paused = true;

            if (StarDriveGame.Instance != null) // don't show in tests
                Log.Write(ConsoleColor.Cyan, "UniverseScreen.UnloadContent");

            ScreenManager.UnloadSceneObjects();
            // destroy SceneObjects for everything
            UState.RemoveSceneObjects();
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

            GameAudio.Update3DSound(new Vector3((float)CamPos.X, (float)CamPos.Y, 0.0f));

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

        void ProjectPieMenu(Vector3 position)
        {
            Vector3 proj = new Vector3(Viewport.Project(position, Projection, View, Matrix.Identity));
            pieMenu.Position = proj.ToVec2();
            pieMenu.Radius = 75f;
            pieMenu.ScaleFactor = 1f;
        }

        public void OnPlayerDefeated()
        {
            StarDriveGame.Instance?.EndingGame(true);
            UState.GameOver = true;
            UState.Paused = true;
            UState.Objects.Clear();
            HelperFunctions.CollectMemory();
            StarDriveGame.Instance?.EndingGame(false);
            ScreenManager.AddScreen(new YouLoseScreen(this));
            UState.Paused = false;
        }

        public void OnPlayerWon(LocalizedText title = default)
        {
            UState.GameOver = true;
            ScreenManager.AddScreen(new YouWinScreen(this, title));
        }

        void UnloadGraphics()
        {
            if (SR == null)
                return;
            if (!GlobalStats.IsUnitTest)
                Log.Write(ConsoleColor.Cyan, "Universe.UnloadGraphics");
            Mem.Dispose(ref bloomComponent);
            Mem.Dispose(ref bg);
            Mem.Dispose(ref FogMap);
            Mem.Dispose(ref FogMapTarget);
            Mem.Dispose(ref BorderRT);
            Mem.Dispose(ref MainTarget);
            Mem.Dispose(ref LightsTarget);
            Mem.Dispose(ref Particles);
            Mem.Dispose(ref bg);
            Mem.Dispose(ref SR);
            Mem.Dispose(ref Shields);
        }

        protected override void Destroy()
        {
            UnloadGraphics();

            Mem.Dispose(ref anomalyManager);
            Mem.Dispose(ref BombList);
            NotificationManager.Clear();
            SelectedShipList = new();
            base.Destroy();
        }

        public override void ExitScreen()
        {
            IsExiting = true;
            UState.Paused = true;

            Thread processTurnsThread = SimThread;
            SimThread = null;
            DrawCompletedEvt.Set(); // notify processTurnsThread that we're terminating
            processTurnsThread?.Join(250);

            RemoveLighting();
            ScreenManager.Music.Stop();

            UState.Dispose();

            ShipToView = null;
            SelectedShip   = null;
            SelectedFleet  = null;
            SelectedPlanet = null;
            SelectedSystem = null;

            EmpireHullBonuses.Clear();
            ClickableFleetsList.Clear();
            ClickableShips = Empty<ClickableShip>.Array;
            ClickablePlanets = Empty<ClickablePlanet>.Array;
            ClickableSystems = Empty<ClickableSystem>.Array;

            base.ExitScreen();
            Dispose(); // will call Destroy() and UnloadGraphics()

            HelperFunctions.CollectMemory();
            // make sure we reset the latest savegame attachment
            Log.ConfigureStatsReporter(null);
        }

        public struct ClickablePlanet
        {
            public Vector2 ScreenPos;
            public float Radius;
            public Planet Planet;
            public bool HitTest(Vector2 touch) => touch.InRadius(ScreenPos, Radius);
        }

        public struct ClickableShip
        {
            public Vector2 ScreenPos;
            public float Radius;
            public Ship Ship;
            public bool HitTest(Vector2 touch) => touch.InRadius(ScreenPos, Radius);
        }

        struct ClickableSystem
        {
            public Vector2 ScreenPos;
            public float Radius;
            public SolarSystem System;
            public bool Touched(Vector2 touchPoint)
            {
                if (!touchPoint.InRadius(ScreenPos, Radius)) return false;

                GameAudio.MouseOver();
                return true;
            }
        }

        // When user or automation AI orders a deep space build goal
        // Then these are used to visualize it to players
        public class ClickableSpaceBuildGoal
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

        public double GetZfromScreenState(UnivScreenState screenState)
        {
            if (screenState == UnivScreenState.GalaxyView)
                return MaxCamHeight;
            return (double)screenState;
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

