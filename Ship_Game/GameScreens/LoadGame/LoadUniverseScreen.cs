using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.GameScreens.NewGame;

namespace Ship_Game
{
    public partial class LoadUniverseScreen : GameScreen
    {
        UniverseScreen Universe;
        string PlayerLoyalty;
        string AdviceText;
        Texture2D LoadingImage;

        readonly TaskResult BackgroundTask;
        readonly ProgressCounter Progress = new ProgressCounter();
        bool LoadingFailed;

        public LoadUniverseScreen(FileInfo activeFile) : base(null/*no parent*/)
        {
            CanEscapeFromScreen = false;
            GlobalStats.Statreset();

            BackgroundTask = Parallel.Run(() =>
            {
                try
                {
                    Progress.Start(0.22f, 0.34f, 0.44f);

                    SavedGame.UniverseSaveData save = DecompressSaveGame(activeFile, Progress.NextStep()); // 641ms
                    Log.Info(ConsoleColor.Blue, $"  DecompressSaveGame     elapsed: {Progress[0].ElapsedMillis}ms");

                    UniverseData data = LoadEverything(save, Progress.NextStep()); // 992ms
                    Log.Info(ConsoleColor.Blue, $"  LoadEverything         elapsed: {Progress[1].ElapsedMillis}ms");

                    Universe = CreateUniverseScreen(data, save, Progress.NextStep()); // 1244ms
                    Log.Info(ConsoleColor.Blue, $"  CreateUniverseScreen   elapsed: {Progress[2].ElapsedMillis}ms");

                    Progress.Finish();

                    Log.Info(ConsoleColor.DarkRed, $"TOTAL LoadUniverseScreen elapsed: {Progress.ElapsedMillis}ms");
                }
                catch (Exception e)
                {
                    Log.ErrorDialog(e, $"LoadUniverseScreen failed: {activeFile.FullName}", 0);
                    LoadingFailed = true;
                }
            });
        }

        public override void LoadContent()
        {
            LoadingImage = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            AdviceText = Fonts.Arial12Bold.ParseText(ResourceManager.LoadRandomAdvice(), 500f);
            base.LoadContent();
        }

        public override bool HandleInput(InputState input)
        {
            if (Universe != null && input.InGameSelect)
            {
                ExitScreen();
                ScreenManager.AddScreenNoLoad(Universe);
                return true;
            }
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!BackgroundTask.IsComplete)
            {
                // heavily throttle main thread, so the worker thread can turbo
                Thread.Sleep(33);
                if (IsDisposed) // just in case we died
                    return;
            }

            if (LoadingFailed) // fatal error when loading save game
            {
                // go back to main menu
                ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects: true);
                return;
            }

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            batch.Begin();
            var artRect = new Rectangle(ScreenWidth / 2 - 960, ScreenHeight / 2 - 540, 1920, 1080);
            batch.Draw(LoadingImage, artRect, Color.White);
            var meterBar = new Rectangle(ScreenWidth / 2 - 150, ScreenHeight - 25, 300, 25);

            float percentLoaded = Progress.Percent;
            var pb = new ProgressBar(meterBar)
            {
                Max = 100f,
                Progress = percentLoaded * 100f
            };
            pb.Draw(ScreenManager.SpriteBatch);

            var cursor = new Vector2(ScreenCenter.X - 250f, meterBar.Y - Fonts.Arial12Bold.MeasureString(AdviceText).Y - 5f);
            batch.DrawString(Fonts.Arial12Bold, AdviceText, cursor, Color.White);

            if (Universe != null)
            {
                cursor.Y -= Fonts.Pirulen16.LineSpacing - 10f;
                const string begin = "Click to Continue!";
                cursor.X = ScreenCenter.X - Fonts.Pirulen16.MeasureString(begin).X / 2f;
                batch.DrawString(Fonts.Pirulen16, begin, cursor, CurrentFlashColor);
            }
            batch.End();
        }


        SavedGame.UniverseSaveData DecompressSaveGame(FileInfo file, ProgressCounter step)
        {
            // @note This one is annoying, since we can't monitor the progress directly
            // we just set an arbitrary time based on recorded perf
            step.StartTimeBased(maxSeconds:1f);
            SavedGame.UniverseSaveData usData = SavedGame.DeserializeFromCompressedSave(file);

            if (usData.SaveGameVersion != SavedGame.SaveGameVersion)
                Log.Error("Incompatible savegame version! Got v{0} but expected v{1}", usData.SaveGameVersion, SavedGame.SaveGameVersion);

            GlobalStats.GravityWellRange     = usData.GravityWellRange;
            GlobalStats.IconSize             = usData.IconSize;
            GlobalStats.MinimumWarpRange     = usData.MinimumWarpRange;
            GlobalStats.ShipMaintenanceMulti = usData.OptionIncreaseShipMaintenance;
            GlobalStats.PreventFederations   = usData.preventFederations;
            GlobalStats.EliminationMode      = usData.EliminationMode;
            GlobalStats.CustomMineralDecay   = usData.CustomMineralDecay;
            GlobalStats.TurnTimer            = usData.TurnTimer != 0 ? usData.TurnTimer : 5;
            PlayerLoyalty                    = usData.PlayerLoyalty;
            RandomEventManager.ActiveEvent   = null;

            GlobalStats.SuppressOnBuildNotifications  = usData.SuppressOnBuildNotifications;
            GlobalStats.PlanetScreenHideOwned         = usData.PlanetScreenHideOwned;
            GlobalStats.PlanetsScreenHideUnhabitable  = usData.PlanetsScreenHideUnhabitable;
            GlobalStats.ShipListFilterPlayerShipsOnly = usData.ShipListFilterPlayerShipsOnly;
            GlobalStats.ShipListFilterInFleetsOnly    = usData.ShipListFilterInFleetsOnly;
            GlobalStats.ShipListFilterNotInFleets     = usData.ShipListFilterNotInFleets;
            GlobalStats.DisableInhibitionWarning      = usData.DisableInhibitionWarning;

            StatTracker.SetSnapshots(usData.Snapshots);
            step.Finish();
            return usData;
        }

        // Universe does not exist yet !
        UniverseData LoadEverything(SavedGame.UniverseSaveData saveData, ProgressCounter step)
        {
            step.Start(9); // arbitrary count... check # of calls below:

            ScreenManager.RemoveAllObjects();
            var data = new UniverseData
            {
                loadFogPath           = saveData.FogMapName,
                difficulty            = saveData.gameDifficulty,
                GalaxySize            = saveData.GalaxySize,
                Size                  = saveData.Size,
                FTLSpeedModifier      = saveData.FTLModifier,
                EnemyFTLSpeedModifier = saveData.EnemyFTLModifier,
                GravityWells          = saveData.GravityWells
            };

            RandomEventManager.ActiveEvent = saveData.RandomEvent;
            int numEmpires                 = saveData.EmpireDataList.Filter(e => !e.IsFaction).Length; 
            CurrentGame.StartNew(data, saveData.GamePacing, saveData.StarsModifier, saveData.ExtraPlanets, numEmpires);
            
            EmpireManager.Clear();
            Empire.Universe?.Objects.Clear();
            
            CreateEmpires(saveData, data);                     step.Advance();
            GiftShipsFromServantEmpire(data);                  step.Advance();
            CreateRelations(saveData);                         step.Advance();
            CreateSolarSystems(saveData, data);                step.Advance();
            CreateAllObjects(saveData, data);                  step.Advance();
            CreateFleetsFromSave(saveData, data);              step.Advance();
            CreateTasksGoalsRoads(saveData, data);             step.Advance();
            CreatePlanetImportExportShipLists(saveData, data); step.Advance();
            UpdateDefenseShipBuildingOffense();                step.Advance();
            UpdatePopulation();                                step.Advance();
            RestoreSolarSystemCQs(saveData, data);             step.Finish();
            return data;
        }

        UniverseScreen CreateUniverseScreen(UniverseData data, SavedGame.UniverseSaveData save, ProgressCounter step)
        {
            var us = new UniverseScreen(data, PlayerLoyalty)
            {
                GamePace       = save.GamePacing,
                StarDate       = save.StarDate,
                ScreenManager  = ScreenManager,
                CamPos         = new Vector3(save.campos.X, save.campos.Y, save.camheight),
                CamHeight      = save.camheight,
            };

            step.Start(0.3f, 0.4f, 0.3f);

            EmpireShipBonuses.RefreshBonuses();
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep());
            CreateSceneObjects(data);
            AllSystemsLoaded(data, step.NextStep());

            step.NextStep().Start(1); // This last step is a mess, using arbitrary count
            
            StarDriveGame.Instance.ResetElapsedTime();
            us.LoadContent();
            CreateAOs(data);
            FinalizeShips(us);
            foreach(Empire empire in EmpireManager.Empires)
                empire.GetEmpireAI().ThreatMatrix.RestorePinGuidsFromSave();

            GameAudio.StopGenericMusic(immediate: false);

            step.Finish(); // finish everything
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {step[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## AllSystemsLoaded          elapsed: {step[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## LoadContent               elapsed: {step[2].ElapsedMillis}ms");
            return us;
        }

        static void FinalizeShips(UniverseScreen us)
        {
            foreach (Ship ship in us.GetMasterShipList())
            {
                if (!ship.Active)
                    continue;

                if (ship.loyalty != EmpireManager.Player && ship.fleet == null)
                {
                    if (!ship.AddedOnLoad) ship.loyalty.Pool.ForcePoolAdd(ship);
                }
                else if (ship.AI.State == AIState.SystemDefender)
                {
                    ship.loyalty.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }
        }

        void CreateSceneObjects(UniverseData data)
        {
            for (int i = 0; i < data.SolarSystemsList.Count; ++i)
            {
                SolarSystem system = data.SolarSystemsList[i];
                foreach (Planet p in system.PlanetList)
                {
                    p.ParentSystem = system;
                    p.InitializePlanetMesh(this);
                }
            }
        }

        void AllSystemsLoaded(UniverseData data, ProgressCounter step)
        {
            Stopwatch s = Stopwatch.StartNew();
            step.Start(data.MasterShipList.Count);
            foreach (Ship ship in data.MasterShipList)
            {
                InitializeShip(data, ship);
                step.Advance();
            }
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                sys.FiveClosestSystems = data.SolarSystemsList.FindMinItemsFiltered(5,
                                            filter => filter != sys,
                                            select => select.Position.SqDist(sys.Position));
            }
            Log.Info(ConsoleColor.Cyan, $"AllSystemsLoaded {s.Elapsed.TotalMilliseconds}ms");
        }

        static void InitializeShip(UniverseData data, Ship ship)
        {
            ship.InitializeShip(loadingFromSaveGame: true);

            if (ship.Carrier.HasHangars)
            {
                foreach (ShipModule hangar in ship.Carrier.AllActiveHangars)
                {
                    if (data.FindShip(ship.loyalty, hangar.HangarShipGuid, out Ship hangarShip))
                    {
                        hangar.ResetHangarShip(hangarShip);
                    }
                }
            }

            if (data.FindPlanet(ship.AI.OrbitTargetGuid, out Planet toOrbit))
            {
                if (ship.AI.State == AIState.Orbit)
                    ship.AI.OrderToOrbit(toOrbit);
            }

            ship.AI.SystemToDefend = data.FindSystemOrNull(ship.AI.SystemToDefendGuid);
            ship.AI.EscortTarget   = data.FindShipOrNull(ship.AI.EscortTargetGuid);
            ship.AI.Target         = data.FindShipOrNull(ship.AI.TargetGuid);
        }
    }
}