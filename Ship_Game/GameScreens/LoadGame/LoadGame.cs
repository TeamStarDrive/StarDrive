using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;
using Ship_Game.Universe;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Utils;

namespace Ship_Game.GameScreens.LoadGame
{
    public class LoadGame
    {
        readonly FileInfo SaveFile;
        TaskResult<UniverseScreen> BackgroundTask;
        readonly ProgressCounter Progress = new();

        public float ProgressPercent => Progress.Percent;
        public bool LoadingFailed { get; private set; }
        bool StartSimThread;

        public bool Verbose;

        public LoadGame(FileInfo saveFile)
        {
            SaveFile = saveFile;
        }

        /// <param name="file">SaveGame file</param>
        /// <param name="noErrorDialogs">Do not show error dialogs</param>
        /// <param name="startSimThread">Start Universe sim thread (set false for testing)</param>
        public static UniverseScreen Load(FileInfo file, bool noErrorDialogs = false, bool startSimThread = true)
        {
            return new LoadGame(file).Load(noErrorDialogs, startSimThread);
        }

        public UniverseScreen Load(bool noErrorDialogs = false, bool startSimThread = true)
        {
            StartSimThread = startSimThread;
            GlobalStats.Statreset();
            try
            {
                Progress.Start(0.22f, 0.34f, 0.44f);

                using SavedGame.UniverseSaveData save = DecompressSaveGame(SaveFile, Progress.NextStep()); // 641ms
                Log.Info(ConsoleColor.Blue, $"  DecompressSaveGame     elapsed: {Progress[0].ElapsedMillis}ms");

                UniverseScreen us = LoadEverything(save, Progress.NextStep()); // 992ms
                Log.Info(ConsoleColor.Blue, $"  LoadEverything         elapsed: {Progress[1].ElapsedMillis}ms");

                SetupUniverseScreen(us.UState, Progress.NextStep()); // 1244ms
                Log.Info(ConsoleColor.Blue, $"  CreateUniverseScreen   elapsed: {Progress[2].ElapsedMillis}ms");

                Progress.Finish();

                Log.Info(ConsoleColor.DarkRed, $"TOTAL LoadUniverseScreen elapsed: {Progress.ElapsedMillis}ms");
                return us;
            }
            catch (Exception e)
            {
                LoadingFailed = true;
                if (noErrorDialogs)
                    Log.Error(e, $"LoadUniverseScreen failed: {SaveFile.FullName}");
                else
                    Log.ErrorDialog(e, $"LoadUniverseScreen failed: {SaveFile.FullName}", 0);
                return null;
            }
        }

        public TaskResult<UniverseScreen> LoadAsync()
        {
            if (BackgroundTask != null)
                return BackgroundTask;

            BackgroundTask = Parallel.Run(() => Load());
            return BackgroundTask;
        }

        /// <summary>
        /// Peeks at the header portion of the new binary save file
        /// </summary>
        public static HeaderData PeekHeader(FileInfo file, bool verbose = false)
        {
            using var stream = file.OpenRead();
            var reader = new Reader(stream);

            object[] objects = BinarySerializer.DeserializeMultiType(reader, new[]{ typeof(HeaderData) }, verbose);
            return (HeaderData)objects[0];
        }

        SavedGame.UniverseSaveData DecompressSaveGame(FileInfo file, ProgressCounter step)
        {
            // @note This one is annoying, since we can't monitor the progress directly
            // we just set an arbitrary time based on recorded perf
            step.StartTimeBased(maxSeconds:1f);

            if (!file.Exists)
                throw new FileNotFoundException($"SaveGame file does not exist: {file.FullName}");

            SavedGame.UniverseSaveData usData = SavedGame.Deserialize(file, Verbose);

            if (usData.Version != SavedGame.SaveGameVersion)
                Log.Error($"Incompatible savegame version! Got v{usData.Version} but expected v{SavedGame.SaveGameVersion}");

            GlobalStats.GravityWellRange     = usData.GravityWellRange;
            GlobalStats.IconSize             = usData.IconSize;
            GlobalStats.MinAcceptableShipWarpRange = usData.MinAcceptableShipWarpRange;
            GlobalStats.ShipMaintenanceMulti = usData.ShipMaintenanceMultiplier;
            GlobalStats.PreventFederations   = usData.PreventFederations;
            GlobalStats.EliminationMode      = usData.EliminationMode;
            GlobalStats.CustomMineralDecay   = usData.CustomMineralDecay;
            GlobalStats.TurnTimer            = usData.TurnTimer != 0 ? usData.TurnTimer : 5;
            RandomEventManager.ActiveEvent   = null;

            GlobalStats.SuppressOnBuildNotifications  = usData.SuppressOnBuildNotifications;
            GlobalStats.PlanetScreenHideOwned         = usData.PlanetScreenHideOwned;
            GlobalStats.PlanetsScreenHideInhospitable  = usData.PlanetsScreenHideInhospitable;
            GlobalStats.ShipListFilterPlayerShipsOnly = usData.ShipListFilterPlayerShipsOnly;
            GlobalStats.ShipListFilterInFleetsOnly    = usData.ShipListFilterInFleetsOnly;
            GlobalStats.ShipListFilterNotInFleets     = usData.ShipListFilterNotInFleets;
            GlobalStats.DisableInhibitionWarning      = usData.DisableInhibitionWarning;
            GlobalStats.DisableVolcanoWarning         = usData.DisableVolcanoWarning;
            GlobalStats.CordrazinePlanetCaptured      = usData.CordrazinePlanetCaptured;
            GlobalStats.UsePlayerDesigns              = usData.UsePlayerDesigns;
            GlobalStats.UseUpkeepByHullSize           = usData.UseUpkeepByHullSize;

            if (usData.VolcanicActivity > 0) // save support - can remove the if and use the usdata in June 2022
                GlobalStats.VolcanicActivity = usData.VolcanicActivity;

            StatTracker.SetSnapshots(usData.Snapshots);
            step.Finish();
            return usData;
        }

        // Universe SETUP is done after loading individual objects like Systems / Ships 
        UniverseScreen LoadEverything(SavedGame.UniverseSaveData saveData, ProgressCounter step)
        {
            if (EmpireManager.NumEmpires != 0)
                throw new Exception("LoadGame.LoadEverything: EmpireManager.NumEmpires must be 0!");

            step.Start(7); // arbitrary count... check # of calls below:

            ScreenManager.Instance.ClearScene();

            var universe = new UniverseScreen(saveData)
            {
                CreateSimThread = StartSimThread,
            };

            UniverseState us = universe.UState;
            RandomEventManager.ActiveEvent = saveData.RandomEvent;
            int numEmpires = saveData.Empires.Filter(e => !e.IsFaction).Length; 
            CurrentGame.StartNew(us, saveData.GamePacing, saveData.StarsModifier, saveData.ExtraPlanets, numEmpires);

            CreateEmpires(us);                               step.Advance();
            GiftShipsFromServantEmpire(us);                  step.Advance();
            CreateRelations(saveData);                       step.Advance();
            CreateSolarSystems(us, saveData);                step.Advance();
            CreateAllObjects(us, saveData);                  step.Advance();
            UpdateDefenseShipBuildingOffense();              step.Advance();
            UpdatePopulation();                              step.Advance();
            step.Finish();
            return universe;
        }

        void SetupUniverseScreen(UniverseState us, ProgressCounter step)
        {
            step.StartAbsolute(0.05f, 0.5f, 2f);

            EmpireHullBonuses.RefreshBonuses();
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep());
            us.Objects.UpdateLists(removeInactiveObjects: false);
            AllSystemsLoaded(us, step.NextStep());

            step.NextStep().Start(1); // This last step is a mess, using arbitrary count

            GameBase.Base.ResetElapsedTime();
            CreateAOs(us);
            FinalizeShips(us);

            us.Screen.LoadContent();
            foreach(Empire empire in EmpireManager.Empires)
            {
                empire.GetEmpireAI().ThreatMatrix.RestorePinGuidsFromSave(us);
            }
            us.Objects.UpdateLists(removeInactiveObjects: false);

            GameAudio.StopGenericMusic(immediate: false);

            step.Finish(); // finish everything
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {step[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## AllSystemsLoaded          elapsed: {step[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## LoadContent               elapsed: {step[2].ElapsedMillis}ms");
        }

        static void FinalizeShips(UniverseState us)
        {
            foreach (Ship ship in us.Ships)
            {
                if (!ship.Active)
                    continue;

                if (ship.Loyalty != EmpireManager.Player && !ship.Loyalty.IsFaction && ship.Fleet == null)
                {
                    if (ship.Pool != null)
                        ship.Loyalty.AddShipToManagedPools(ship);
                }
                else if (ship.AI.State == AIState.SystemDefender)
                {
                    ship.Loyalty.GetEmpireAI().DefensiveCoordinator.Add(ship);
                }

                if (ship.Carrier.HasHangars)
                {
                    foreach (ShipModule hangar in ship.Carrier.AllActiveHangars)
                    {
                        if (us.GetShip(hangar.HangarShipId, out Ship hangarShip))
                            hangar.ResetHangarShip(hangarShip);
                    }
                }
            }
        }

        void AllSystemsLoaded(UniverseState us, ProgressCounter step)
        {
            Stopwatch s = Stopwatch.StartNew();
            step.Start(us.Ships.Count);

            // TODO: Maybe run this in parallel?
            foreach (Ship ship in us.Ships)
            {
                ship.InitializeShip(loadingFromSaveGame: true);
                step.Advance();
            }
            Log.Info(ConsoleColor.Cyan, $"AllSystemsLoaded {s.Elapsed.TotalMilliseconds}ms");
        }

        static void CreateAOs(UniverseState us)
        {
            foreach (Empire e in us.Empires)
                e.GetEmpireAI().InitializeAOsFromSave(us);
        }

        void CreateEmpires(UniverseState us)
        {
            for (int i = 0; i < d.GSAIData.MilitaryTaskList.Count; i++)
            {
                d.GSAIData.MilitaryTaskList[i].RestoreFromSaveNoUniverse(us, e);
            }

            e.GetEmpireAI().ReadFromSave(d.GSAIData);
        }

        static bool IsShipGoalInvalid(SavedGame.GoalSave g)
        {
            if (g.Type != GoalType.BuildOffensiveShips &&
                g.Type != GoalType.IncreaseFreighters)
            {
                return false;
            }

            return g.ToBuildUID != null && !ResourceManager.ShipTemplateExists(g.ToBuildUID);
        }
        
        static void CreateGoals(UniverseState us, SavedGame.EmpireSaveData esd, Empire e)
        {
            foreach (SavedGame.GoalSave gsave in esd.GSAIData.Goals)
            {
                if (IsShipGoalInvalid(gsave))
                    continue;

                Goal g = Goal.Deserialize(gsave.GoalName, us, e, gsave);
                if (gsave.FleetId != 0)
                {
                    foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                    {
                        if (fleet.Value.Id == gsave.FleetId) g.Fleet = fleet.Value;
                    }
                }

                g.TargetSystem = us.GetSystem(gsave.TargetSystemId);
                g.PlanetBuildingAt   = us.GetPlanet(gsave.PlanetBuildingAtId);
                g.ColonizationTarget = us.GetPlanet(gsave.MarkedPlanetId);
                g.TargetPlanet       = us.GetPlanet(gsave.TargetPlanetId);
                g.FinishedShip = us.GetShip(gsave.ColonyShipId);
                g.OldShip      = us.GetShip(gsave.OldShipId);
                g.TargetShip   = us.GetShip(gsave.TargetShipId);

                if (g.type == GoalType.Refit && gsave.ToBuildUID != null)
                {
                    IShipDesign shipToBuild = ResourceManager.Ships.GetDesign(gsave.ToBuildUID, false);
                    if (shipToBuild != null)
                        g.ShipToBuild = shipToBuild;
                    else
                        Log.Error($"Could not find ship name {gsave.ToBuildUID} in dictionary when trying to load Refit goal!");
                }

                if (gsave.TargetEmpireId > 0)
                    g.TargetEmpire = EmpireManager.GetEmpireById(gsave.TargetEmpireId);

                g.PostInit();
                e.GetEmpireAI().Goals.Add(g);
            }
        }

        static void CreateShipGoals(UniverseState us, SavedGame.EmpireSaveData esd)
        {
            foreach (SavedGame.ShipSaveData shipData in esd.OwnedShips)
            {
                if (!us.Objects.FindShip(shipData.Id, out Ship ship))
                    continue;

                if (shipData.AISave.WayPoints != null)
                    ship.AI.SetWayPoints(shipData.AISave.WayPoints);

                ship.AI.Target         = us.GetShip(shipData.AISave.AttackTargetId);
                ship.AI.EscortTarget   = us.GetShip(shipData.AISave.EscortTargetId);
                ship.AI.SystemToDefend = us.GetSystem(shipData.AISave.SystemToDefendId);
                ship.AI.SetOrbitTarget(us.GetPlanet(shipData.AISave.OrbitTargetId));

                foreach (SavedGame.ShipGoalSave sg in shipData.AISave.ShipGoalsList)
                {
                    ship.AI.AddGoalFromSave(sg, us);
                }
            }
        }

        void CreateEmpires(UniverseState us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                bool isPlayer = d.Traits.Name == PlayerLoyalty;
                Empire e = CreateEmpireFromEmpireSaveData(d, us, isPlayer);
                if (isPlayer)
                {
                    e.AutoColonize          = saveData.AutoColonize;
                    e.AutoExplore           = saveData.AutoExplore;
                    e.AutoFreighters        = saveData.AutoFreighters;
                    e.AutoPickBestFreighter = saveData.AutoPickBestFreighter;
                    e.AutoPickBestColonizer = saveData.AutoPickBestColonizer;
                    e.AutoBuild             = saveData.AutoProjectors;
                }
            }

            // FB: moved from empire int to after all empires created otherwise it wont work
            foreach (Empire e in us.Empires)
                e.ResetTechsUsableByShips(e.GetOurFactionShips(), unlockBonuses: false);
        }

        static void GiftShipsFromServantEmpire(UniverseState us)
        {
            foreach (Empire e in us.Empires)
            {
                if (e.data.AbsorbedBy == null)
                    continue;
                Empire servantEmpire = e;
                Empire masterEmpire = EmpireManager.GetEmpireByName(servantEmpire.data.AbsorbedBy);
                
                masterEmpire.AssimilateTech(servantEmpire);
            }
        }

        static void CreateAllObjects(UniverseState us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.ShipSaveData shipSave in saveData.AllShips)
                Ship.CreateShipFromSave(us, shipSave.Owner, saveData, shipSave);

            if (saveData.Projectiles != null) // NULL check: backwards compatibility
            {
                foreach (SavedGame.ProjectileSaveData projData in saveData.Projectiles)
                    Projectile.CreateFromSave(projData, us);
            }
            if (saveData.Beams != null) // NULL check: backwards compatibility
            {
                foreach (SavedGame.BeamSaveData beamData in saveData.Beams)
                    Beam.CreateFromSave(beamData, us);
            }
        }

        void CreateSolarSystems(UniverseState us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SolarSystem system in saveData.Systems)
            {
                us.AddSolarSystem(system);
            }
            foreach (SolarSystem system in us.Systems)
            {
                system.FiveClosestSystems = us.GetFiveClosestSystems(system);
            }
        }

        void UpdateDefenseShipBuildingOffense()
        {
            foreach (Empire empire in EmpireManager.MajorEmpires)
                empire.UpdateDefenseShipBuildingOffense();
        }

        void UpdatePopulation()
        {
            foreach (Empire empire in EmpireManager.ActiveEmpires)
                empire.UpdatePopulation();
        }

        static void CreateRelations(SavedGame.UniverseSaveData saveData)
        {
            Empire.InitializeRelationships(saveData.Empires);
        }
    }
}
