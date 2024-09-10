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
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.Universe;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.FleetDesign;
using Ship_Game.Graphics;
using Ship_Game.Graphics.Particles;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Rectangle = SDGraphics.Rectangle;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
using Ship_Game.ExtensionMethods;

namespace Ship_Game
{
    public partial class UniverseScreen : GameScreen
    {
        // The non-visible state of the Universe
        public readonly UniverseState UState;

        public string StarDateString => UState.StarDate.StarDateString();
        public float LastAutosaveTime = 0;

        public Background bg;

        public Array<Bomb> BombList  = new();
        readonly AutoResetEvent DrawCompletedEvt = new(false);

        public const double MinCamHeight = 450.0;
        protected double MaxCamHeight;
        public Vector3d CamDestination;
        public Vector3d CamPos { get => UState.CamPos; set => UState.CamPos = value; }
        public Vector3d transitionStartPosition;

        public bool ViewingShip = false;
        public float transDuration = 3f;
        public float SelectedSomethingTimer = 3f;

        public bool ShowTacticalCloseup { get; private set; }
        public bool Debug => UState.Debug;
        public DebugModes DebugMode => UState.DebugMode;

        public PieMenu pieMenu;
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

        #pragma warning disable CA2213 // managed by Content Manager
        public Effect basicFogOfWarEffect;
        #pragma warning restore CA2213

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
        public MiniMap Minimap { get; private set; }
        bool loading;
        public float transitionElapsedTime;

        // @note Initialize with a default frustum for UnitTests
        public BoundingFrustum Frustum = new(Matrix.CreateTranslation(1000000, 1000000, 0));

        float MusicCheckTimer;
        public Ship ShipToView;
        public float AdjustCamTimer;
        public AutomationWindow aw;
        public ExoticBonusesWindow ExoticBonusesWindow;
        public FreighterUtilizationWindow FreighterUtilizationWindow;
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
        bool SelectingWithBox;

        public PlanetScreen workersPanel;
        int SelectorFrame;

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
                ResetSolarSystemLights(system);
        }

        public void ResetSolarSystemLights(SolarSystem system)
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
            anomalyManager = new();

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
                    planet.UpdatePlanetStatsByRecalculation();
                }
            }
        }

        void CreateStartingShips()
        {
            // not a new game or load game at stardate 1000 
            if (UState.StarDate > 1000f || UState.Ships.Length > 0)
                return;

            foreach (Empire empire in UState.MajorEmpires)
            {
                Planet homePlanet = empire.GetPlanets()[0];
                string colonyShip = empire.data.DefaultColonyShip;
                string startingScout = empire.data.StartingScout;
                string freighter = empire.data.DefaultSmallTransport;
                string starterShip = empire.data.Traits.Prototype == 0
                                   ? empire.data.StartingShip
                                   : empire.data.PrototypeShip;

                //if starting ship is a station - make it orbit the planet
                Ship createdStartingShip = Ship.CreateShipNearPlanet(UState, starterShip, empire, homePlanet, true);
                if (createdStartingShip != null && (createdStartingShip.MaxFTLSpeed == 0 || createdStartingShip.MaxSTLSpeed == 0))
                {
                    createdStartingShip.Position = homePlanet.Position.GenerateRandomPointOnCircle(500 + homePlanet.Radius, UState.Random);
                    createdStartingShip.TetherToPlanet(homePlanet);
                }
                Ship.CreateShipNearPlanet(UState, colonyShip, empire, homePlanet, true);
                Ship startingFrieghter = Ship.CreateShipNearPlanet(UState, freighter, empire, homePlanet, true);
                if (startingFrieghter != null) // FB - wa for new frieghter since this is done onShipComplete in sbproduction
                {
                    startingFrieghter.TransportingProduction = true;
                    startingFrieghter.TransportingFood       = true;
                    startingFrieghter.TransportingColonists  = true;
                    startingFrieghter.AllowInterEmpireTrade  = true;
                }

                for (int i = 0; i < 1 + empire.data.Traits.ExtraStartingScouts; i++)
                    Ship.CreateShipNearPlanet(UState, startingScout, empire, homePlanet, true);
            }
        }

        void LoadGraphics()
        {
            const int minimapOffSet = 14;

            var device  = ScreenManager.GraphicsDevice;
            int width   = GameBase.ScreenWidth;
            int height  = GameBase.ScreenHeight;

            Particles = new ParticleManager(TransientContent);

            if (GlobalStats.DrawStarfield)
            {
                bg = new Background(this, device);
            }

            if (GlobalStats.DrawNebulas)
            {
                bg3d = new Background3D(this, device);
            }

            Frustum = new BoundingFrustum(ViewProjection);
            mmHousing = new Rectangle(width - (276 + minimapOffSet), height - 256, 276 + minimapOffSet, 256);
            Minimap = Add(new MiniMap(this, mmHousing));
            ExoticBonusesWindow = Add(new ExoticBonusesWindow(this));
            FreighterUtilizationWindow = Add(new FreighterUtilizationWindow(this));

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
            CreatePieMenu();

            FTLManager.LoadContent(this);

            ShipsInCombat = ButtonMediumMenu(width - 275, height - 280, "Ships: 0");
            ShipsInCombat.DynamicText = () =>
            {
                ShipsInCombat.Style = Player.EmpireShipCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
                return $"Ships: {Player.EmpireShipCombat}";
            };
            ShipsInCombat.Tooltip = "Cycle through ships not in fleet that are in combat";
            ShipsInCombat.OnClick = ShipsInCombatClick;
            Add(ShipsInCombat);

            PlanetsInCombat = ButtonMediumMenu(width - 135, height - 280, "Planets: 0");
            PlanetsInCombat.DynamicText = () =>
            {
                PlanetsInCombat.Style = Player.EmpirePlanetCombat > 0 ? ButtonStyle.Medium : ButtonStyle.MediumMenu;
                return $"Planets: {Player.EmpirePlanetCombat}";
            };
            PlanetsInCombat.OnClick = CyclePlanetsInCombat;
            PlanetsInCombat.Tooltip = "Cycle through planets that are in combat";

            RectF leftRect = new(20, 60, 200, 500);
            Add(new FleetButtonsList(leftRect, this, this,
                onClick: OnFleetButtonClicked,
                onHotKey: OnFleetHotKeyPressed,
                isSelected: (b) => SelectedFleet?.Key == b.FleetKey
            ));
        }

        void ShipsInCombatClick(UIButton b)
        {
            int nbrship = 0;
            if (lastshipcombat >= Player.EmpireShipCombat)
                lastshipcombat = 0;
            var ships = Player.OwnedShips;
            foreach (Ship ship in ships)
            {
                if (ship.Fleet != null || ship.OnLowAlert || ship.IsHangarShip || ship.IsHomeDefense || !ship.Active)
                    continue;
                if (nbrship == lastshipcombat)
                {
                    ViewToShip(ship);
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
                var fogMapTarget = GetCachedFogMapRenderTarget(device, ref FogMapTarget);
                device.SetRenderTarget(0, fogMapTarget);
                device.Clear(Color.TransparentWhite);
                Color defaultFogColor = new(0, 0, 0, 150);
                ScreenManager.SpriteRenderer.Begin(OrthographicProjection);
                ScreenManager.SpriteRenderer.FillRect(new(0, 0, ScreenArea), defaultFogColor);
                ScreenManager.SpriteRenderer.End();
                device.SetRenderTarget(0, null);
                FogMap = fogMapTarget.GetTexture();
            }

            //FogMap ??= ResourceManager.Texture2D("UniverseFeather.dds");
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
                workersPanel?.Update(fixedDeltaTime);
            
            DeepSpaceBuildWindow.Update(fixedDeltaTime);
            pieMenu.Update(fixedDeltaTime);
            SelectedSomethingTimer -= fixedDeltaTime;

            if (++SelectorFrame > 299)
                SelectorFrame = 0;

            ScreenManager.StartMusic("AmbientMusic");
            NotificationManager.Update(fixedDeltaTime);

            GameAudio.Update3DSound(new Vector3((float)CamPos.X, (float)CamPos.Y, (float)CamPos.Z));

            ScreenManager.UpdateSceneObjects(fixedDeltaTime);
            EmpireUI.Update(fixedDeltaTime);
            UpdateSelectedItems(GameBase.Base.Elapsed);

            base.Update(fixedDeltaTime);
        }

        void UpdateSelectedItems(UpdateTimes elapsed)
        {
            if (ShowSystemInfoOverlay)
            {
                SystemInfoOverlay.Update(elapsed);
            }
            if (ShowPlanetInfo)
            {
                pInfoUI.Update(elapsed);
            }
            else if (ShowShipInfo)
            {
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
            if (MainTarget == null)
                return;
            if (!GlobalStats.IsUnitTest)
                Log.Write(ConsoleColor.Cyan, "Universe.UnloadGraphics");
            Mem.Dispose(ref bloomComponent);
            Mem.Dispose(ref bg);
            Mem.Dispose(ref FogMap);
            Mem.Dispose(ref FogMapTarget);
            Mem.Dispose(ref MainTarget);
            Mem.Dispose(ref BorderRT);
            Mem.Dispose(ref LightsTarget);
            Mem.Dispose(ref Particles);
            Mem.Dispose(ref Shields);
            Mem.Dispose(ref aw);
            Mem.Dispose(ref ExoticBonusesWindow);
            Mem.Dispose(ref FreighterUtilizationWindow);
            Mem.Dispose(ref DebugWin);
            Mem.Dispose(ref workersPanel);
        }

        protected override void Dispose(bool disposing)
        {
            UnloadGraphics();

            anomalyManager = null;
            BombList.Clear();
            PendingSimThreadActions.Dispose();
            NotificationManager?.Clear();
            SelectedShipList = new();

            DrawCompletedEvt.Dispose();
            UState.Dispose();

            base.Dispose(disposing);
        }

        public override void ExitScreen()
        {
            if (IsDisposed)
                return; // already exited and disposed

            IsExiting = true;
            UState.Paused = true;

            Thread processTurnsThread = SimThread;
            SimThread = null;
            DrawCompletedEvt.Set(); // notify processTurnsThread that we're terminating
            processTurnsThread?.Join(250);

            RemoveLighting();
            ScreenManager.StopMusic();

            ClearSelectedItems();
            ShipToView = null;

            EmpireHullBonuses.Clear();
            ClickableFleetsList.Clear();

            base.ExitScreen();
            Dispose(); // will call virtual Dispose(bool disposing) and UnloadGraphics()

            HelperFunctions.CollectMemory();
            // make sure we reset the latest savegame attachment
            Log.ConfigureStatsReporter(null);
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
        }
        public enum UnivScreenState
        {
            DetailView = 7000,
            ShipView   = 15000,
            PlanetView = 35000,
            SystemView = 250000,
            SectorView = 1775000, // from 250_001 to 1_775_000
            GalaxyView
        }

        public double GetZfromScreenState(UnivScreenState screenState)
        {
            if (screenState == UnivScreenState.GalaxyView)
                return MaxCamHeight;
            return (double)screenState;
        }
    }
}

