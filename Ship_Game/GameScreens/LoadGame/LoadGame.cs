using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game.GameScreens.LoadGame
{
    public class LoadGame
    {
        readonly FileInfo SaveFile;
        string PlayerLoyalty;
        TaskResult<UniverseScreen> BackgroundTask;
        readonly ProgressCounter Progress = new ProgressCounter();

        public float ProgressPercent => Progress.Percent;
        public bool LoadingFailed { get; private set; }
        bool StartSimThread;

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

                SavedGame.UniverseSaveData save = DecompressSaveGame(SaveFile, Progress.NextStep()); // 641ms
                Log.Info(ConsoleColor.Blue, $"  DecompressSaveGame     elapsed: {Progress[0].ElapsedMillis}ms");

                UniverseScreen us = LoadEverything(save, Progress.NextStep()); // 992ms
                Log.Info(ConsoleColor.Blue, $"  LoadEverything         elapsed: {Progress[1].ElapsedMillis}ms");

                SetupUniverseScreen(us, save, Progress.NextStep()); // 1244ms
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

        SavedGame.UniverseSaveData DecompressSaveGame(FileInfo file, ProgressCounter step)
        {
            // @note This one is annoying, since we can't monitor the progress directly
            // we just set an arbitrary time based on recorded perf
            step.StartTimeBased(maxSeconds:1f);

            if (!file.Exists)
                throw new FileNotFoundException($"SaveGame file does not exist: {file.FullName}");

            SavedGame.UniverseSaveData usData = SavedGame.DeserializeFromCompressedSave(file);

            if (usData.SaveGameVersion != SavedGame.SaveGameVersion)
                Log.Error("Incompatible savegame version! Got v{0} but expected v{1}", usData.SaveGameVersion, SavedGame.SaveGameVersion);

            GlobalStats.GravityWellRange     = usData.GravityWellRange;
            GlobalStats.IconSize             = usData.IconSize;
            GlobalStats.MinAcceptableShipWarpRange = usData.MinAcceptableShipWarpRange;
            GlobalStats.ShipMaintenanceMulti = usData.OptionIncreaseShipMaintenance;
            GlobalStats.PreventFederations   = usData.PreventFederations;
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

            step.Start(11); // arbitrary count... check # of calls below:

            ScreenManager.Instance.RemoveAllObjects();

            var us = new UniverseScreen(saveData)
            {
                CreateSimThread = StartSimThread,
                FogMapBase64     = saveData.FogMapBase64,
                UniverseSize     = saveData.UniverseSize,

                FTLModifier      = saveData.FTLModifier,
                EnemyFTLModifier = saveData.EnemyFTLModifier,
                GravityWells        = saveData.GravityWells,
                FTLInNeutralSystems = saveData.FTLInNeutralSystems,

                Difficulty       = saveData.GameDifficulty,
                GalaxySize       = saveData.GalaxySize,
            };

            RandomEventManager.ActiveEvent = saveData.RandomEvent;
            int numEmpires = saveData.EmpireDataList.Filter(e => !e.IsFaction).Length; 
            CurrentGame.StartNew(us, saveData.GamePacing, saveData.StarsModifier, saveData.ExtraPlanets, numEmpires);

            CreateEmpires(us, saveData);                     step.Advance();
            GiftShipsFromServantEmpire(us);                  step.Advance();
            CreateRelations(saveData);                       step.Advance();
            CreateSolarSystems(us, saveData);                step.Advance();
            CreateAllObjects(us, saveData);                  step.Advance();
            CreateFleetsFromSave(us, saveData);              step.Advance();
            CreateTasksGoalsRoads(us, saveData);             step.Advance();
            CreatePlanetImportExportShipLists(us, saveData); step.Advance();
            UpdateDefenseShipBuildingOffense();              step.Advance();
            UpdatePopulation();                              step.Advance();
            RestoreCapitals(saveData, us);                   step.Finish();
            return us;
        }

        void SetupUniverseScreen(UniverseScreen us, SavedGame.UniverseSaveData save, ProgressCounter step)
        {
            RestoreSolarSystemCQs(save, us);

            step.StartAbsolute(0.05f, 0.5f, 2f);

            EmpireHullBonuses.RefreshBonuses();
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep());
            AllSystemsLoaded(us, step.NextStep());

            step.NextStep().Start(1); // This last step is a mess, using arbitrary count

            GameBase.Base.ResetElapsedTime();
            CreateAOs(us);
            FinalizeShips(us);

            us.LoadContent();
            us.Objects.UpdateLists(removeInactiveObjects: false);

            foreach(Empire empire in EmpireManager.Empires)
            {
                empire.GetEmpireAI().ThreatMatrix.RestorePinGuidsFromSave(us);
            }

            GameAudio.StopGenericMusic(immediate: false);

            step.Finish(); // finish everything
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {step[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## AllSystemsLoaded          elapsed: {step[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## LoadContent               elapsed: {step[2].ElapsedMillis}ms");
        }

        static void FinalizeShips(UniverseScreen us)
        {
            foreach (Ship ship in us.GetMasterShipList())
            {
                if (!ship.Active)
                    continue;

                if (ship.Loyalty != EmpireManager.Player && !ship.Loyalty.isFaction && ship.Fleet == null)
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
                        if (us.GetShip(hangar.HangarShipGuid, out Ship hangarShip))
                            hangar.ResetHangarShip(hangarShip);
                    }
                }
            }
        }

        void AllSystemsLoaded(UniverseScreen us, ProgressCounter step)
        {
            Stopwatch s = Stopwatch.StartNew();
            step.Start(us.GetMasterShipList().Count);

            // TODO: Maybe run this in parallel?
            foreach (Ship ship in us.GetMasterShipList())
            {
                ship.InitializeShip(loadingFromSaveGame: true);
                step.Advance();
            }
            Log.Info(ConsoleColor.Cyan, $"AllSystemsLoaded {s.Elapsed.TotalMilliseconds}ms");
        }

        static void RestoreCommodities(Planet p, SavedGame.PlanetSaveData psdata)
        {
            p.FoodHere = psdata.FoodHere;
            p.ProdHere = psdata.ProdHere;
            p.Population = psdata.Population;
        }

        static Empire CreateEmpireFromEmpireSaveData(SavedGame.EmpireSaveData sdata, bool isPlayer)
        {
            var e = new Empire();
            e.isPlayer = isPlayer;
            //TempEmpireData  Tdata = new TempEmpireData();

            e.isFaction = sdata.IsFaction;
            if (sdata.EmpireData == null)
            {
                e.data.Traits = sdata.Traits;
                e.EmpireColor = sdata.Traits.Color;
            }
            else
            {
                e.data = sdata.EmpireData;
                e.data.ResearchQueue = sdata.EmpireData.ResearchQueue;
                e.Research.SetTopic(sdata.ResearchTopic);
                e.PortraitName = e.data.PortraitName;
                e.dd           = ResourceManager.GetDiplomacyDialog(e.data.DiplomacyDialogPath);
                e.EmpireColor  = e.data.Traits.Color;
                e.UpdateNormalizedMoney(sdata.NormalizedMoneyVal, fromSave:true);
                e.data.CurrentAutoScout       = sdata.CurrentAutoScout     ?? e.data.ScoutShip;
                e.data.CurrentAutoColony      = sdata.CurrentAutoColony    ?? e.data.ColonyShip;
                e.data.CurrentAutoFreighter   = sdata.CurrentAutoFreighter ?? e.data.FreighterShip;
                e.data.CurrentConstructor     = sdata.CurrentConstructor   ?? e.data.ConstructorShip;
                e.IncreaseFastVsBigFreighterRatio(sdata.FastVsBigFreighterRatio - e.FastVsBigFreighterRatio);
                if (e.data.DefaultTroopShip.IsEmpty())
                    e.data.DefaultTroopShip = e.data.PortraitName + " " + "Troop";

                e.SetAverageFreighterCargoCap(sdata.AverageFreighterCargoCap);
                e.SetAverageFreighterFTLSpeed(sdata.AverageFreighterFTLSpeed);

                e.RestoreFleetStrEmpireMultiplier(sdata.FleetStrEmpireModifier);
                e.RestoreDiplomacyConcatQueue(sdata.DiplomacyContactQueue);

                e.RushAllConstruction = sdata.RushAllConstruction;
                e.WeightedCenter      = sdata.WeightedCenter;

                if (sdata.ObsoletePlayerShipModules != null)
                    e.ObsoletePlayerShipModules = sdata.ObsoletePlayerShipModules;
            }

            foreach (TechEntry tech in sdata.TechTree)
            {
                if (ResourceManager.TryGetTech(tech.UID, out _))
                {
                    tech.ResolveTech();
                    e.TechnologyDict.Add(tech.UID, tech);
                }
                else Log.Warning($"LoadTech ignoring invalid tech: {tech.UID}");
            }
            e.InitializeFromSave();
            e.Money = sdata.Money;
            e.GetEmpireAI().AreasOfOperations = sdata.AOs;
            e.GetEmpireAI().ExpansionAI.SetExpandSearchTimer(sdata.ExpandSearchTimer);
            e.GetEmpireAI().ExpansionAI.SetMaxSystemsToCheckedDiv(sdata.MaxSystemsToCheckedDiv.LowerBound(1));

            if (e.WeArePirates)
                e.Pirates.RestoreFromSave(sdata);

            if (e.WeAreRemnants)
                e.Remnants.RestoreFromSave(sdata);

            return e;
        }

        SolarSystem CreateSystemFromData(SavedGame.SolarSystemSaveData ssd, UniverseScreen us)
        {
            var system = new SolarSystem
            {
                Guid          = ssd.Guid,
                Name          = ssd.Name,
                Position      = ssd.Position,
                Sun           = SunType.FindSun(ssd.SunPath), // old SunPath is actually the ID @todo RENAME
            };

            system.SetPiratePresence(ssd.PiratePresence);
            system.AsteroidsList.AddRange(ssd.AsteroidsList);
            system.MoonList.AddRange(ssd.Moons);
            foreach (Moon moon in system.MoonList)
                moon.SetSystem(system); // restore system

            system.SetExploredBy(ssd.ExploredBy);
            system.RingList = new Array<SolarSystem.Ring>();

            foreach (SavedGame.RingSave ring in ssd.RingList)
            {
                if (ring.Asteroids)
                {
                    system.RingList.Add(new SolarSystem.Ring
                    {
                        Asteroids = true,
                        OrbitalDistance = ring.OrbitalDistance
                    });
                }
                else
                {
                    Planet p = Planet.FromSaveData(system, ring.Planet);
                    p.Center = system.Position.PointFromAngle(p.OrbitalAngle, p.OrbitalRadius);
                    
                    foreach (Building b in p.BuildingList)
                    {
                        if (!b.IsSpacePort)
                            continue;

                        p.Station = new SpaceStation(p);
                        p.Station.LoadContent(ScreenManager.Instance, p.Owner);
                        p.HasSpacePort = true;
                    }
                    
                    if (p.Owner != null && !system.OwnerList.Contains(p.Owner))
                        system.OwnerList.Add(p.Owner);

                    system.PlanetList.Add(p);
                    p.SetExploredBy(ssd.ExploredBy);

                    system.RingList.Add(new SolarSystem.Ring
                    {
                        planet    = p,
                        Asteroids = false,
                        OrbitalDistance = ring.OrbitalDistance
                    });
                    p.UpdateIncomes(true);  // must be before restoring commodities since max storage is set here           
                    RestoreCommodities(p, ring.Planet);
                }
            }
            return system;
        }

        static void CreateSpaceRoads(UniverseScreen us, SavedGame.EmpireSaveData d, Empire e)
        {
            e.SpaceRoadsList = new Array<SpaceRoad>();
            foreach (SavedGame.SpaceRoadSave roadSave in d.SpaceRoadData)
            {
                var road = new SpaceRoad();
                road.Origin      = us.GetSystem(roadSave.OriginGUID);
                road.Destination = us.GetSystem(roadSave.DestGUID);

                foreach (SavedGame.RoadNodeSave roadNode in roadSave.RoadNodes)
                {
                    var node = new RoadNode();
                    us.GetShip(roadNode.Guid_Platform, out node.Platform);
                    node.Position = roadNode.Position;
                    road.RoadNodesList.Add(node);
                }

                e.SpaceRoadsList.Add(road);
            }
        }

        static void RestorePlanetConstructionQueue(SavedGame.RingSave rsave, Planet p, UniverseScreen us)
        {
            foreach (SavedGame.QueueItemSave qisave in rsave.Planet.QISaveList)
            {
                var qi  = new QueueItem(p);
                qi.Rush = qisave.Rush;
                if (qisave.IsBuilding)
                {
                    qi.isBuilding    = true;
                    qi.IsMilitary    = qisave.IsMilitary;
                    qi.Building      = ResourceManager.CreateBuilding(us, qisave.UID);
                    qi.Cost          = qi.Building.ActualCost;
                    qi.NotifyOnEmpty = false;
                    qi.IsPlayerAdded = qisave.IsPlayerAdded;

                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.X != (int) qisave.PGSVector.X || pgs.Y != (int) qisave.PGSVector.Y)
                            continue;

                        pgs.QItem = qi;
                        qi.pgs    = pgs;
                        break;
                    }
                }

                if (qisave.IsTroop)
                {
                    qi.isTroop = true;
                    qi.TroopType = qisave.UID;
                    qi.Cost = ResourceManager.GetTroopCost(qisave.UID);
                    qi.NotifyOnEmpty = false;
                }

                if (qisave.IsShip)
                {
                    qi.isShip = true;
                    if (!ResourceManager.Ships.GetDesign(qisave.UID, out IShipDesign shipTemplate))
                        continue;

                    qi.sData           = shipTemplate;
                    qi.DisplayName     = qisave.DisplayName;
                    qi.Cost            = qisave.Cost;
                    qi.TradeRoutes     = qisave.TradeRoutes;
                    qi.TransportingColonists  = qisave.TransportingColonists;
                    qi.TransportingFood       = qisave.TransportingFood;
                    qi.TransportingProduction = qisave.TransportingProduction;
                    qi.AllowInterEmpireTrade  = qisave.AllowInterEmpireTrade;
                        
                    if (qisave.AreaOfOperation != null)
                    {
                        foreach (Rectangle aoRect in qisave.AreaOfOperation)
                            qi.AreaOfOperation.Add(aoRect);
                    }
                }

                foreach (Goal g in p.Owner.GetEmpireAI().Goals)
                {
                    if (g.guid != qisave.GoalGUID)
                        continue;
                    qi.Goal = g;
                    qi.NotifyOnEmpty = false;
                }

                if (qisave.IsShip && qi.Goal != null)
                {
                    qi.Goal.ShipToBuild = ResourceManager.Ships.GetDesign(qisave.UID);
                }

                qi.ProductionSpent = qisave.ProgressTowards;
                p.Construction.Enqueue(qi);
            }
        }

        static void RestoreCapitals(SavedGame.UniverseSaveData sData, UniverseScreen us)
        {
            foreach (SavedGame.EmpireSaveData d in sData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                us.GetPlanet(d.CapitalGuid, out Planet capital);
                e.SetCapital(capital);
            }
        }
        
        static void RestoreSolarSystemCQs(SavedGame.UniverseSaveData saveData, UniverseScreen us)
        {
            foreach (SavedGame.SolarSystemSaveData sdata in saveData.SolarSystemDataList)
            {
                foreach (SavedGame.RingSave rsave in sdata.RingList)
                {
                    if (rsave.Planet != null)
                    {
                        Planet p = us.GetPlanet(rsave.Planet.Guid);
                        if (p?.Owner != null)
                        {
                            RestorePlanetConstructionQueue(rsave, p, us);
                        }
                    }
                }
            }
        }

        static void CreateFleetsFromSave(UniverseScreen us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                foreach (SavedGame.FleetSave fleetsave in d.FleetsList)
                {
                    var fleet = new Fleet
                    {
                        Guid = fleetsave.FleetGuid,
                        IsCoreFleet = fleetsave.IsCoreFleet,
                        // @note savegame compatibility uses facing in radians
                        FinalDirection = fleetsave.Facing.RadiansToDirection(),
                        Owner = e
                    };

                    fleet.AddFleetDataNodes(fleetsave.DataNodes);

                    foreach (SavedGame.FleetShipSave ssave in fleetsave.ShipsInFleet)
                    {
                        if (us.Objects.FindShip(ssave.ShipGuid, out Ship ship) && ship.Fleet == null)
                        {
                            ship.RelativeFleetOffset = ssave.FleetOffset;
                            fleet.AddShip(ship);
                        }
                    }

                    foreach (FleetDataNode node in fleet.DataNodes)
                    {
                        foreach (Ship ship in fleet.Ships)
                        {
                            if (!(node.ShipGuid != Guid.Empty) || !(ship.Guid == node.ShipGuid))
                            {
                                continue;
                            }

                            node.Ship = ship;
                            node.ShipName = ship.Name;
                            break;
                        }
                    }

                    fleet.AssignPositions(fleet.FinalDirection);
                    fleet.Name = fleetsave.Name;
                    fleet.TaskStep = fleetsave.TaskStep;
                    fleet.Owner = e;
                    fleet.FinalPosition = fleetsave.Position;
                    fleet.SetSpeed();
                    fleet.SetAutoRequisition(fleetsave.AutoRequisition);
                    e.GetFleetsDict()[fleetsave.Key] = fleet;
                }
            }
        }

        static void CreateAOs(UniverseScreen us)
        {
            foreach (Empire e in us.Empires)
                e.GetEmpireAI().InitializeAOsFromSave(us);
        }

        static void CreateMilitaryTasks(UniverseScreen us, SavedGame.EmpireSaveData d, Empire e)
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
        
        static void CreateGoals(UniverseScreen us, SavedGame.EmpireSaveData esd, Empire e)
        {
            foreach (SavedGame.GoalSave gsave in esd.GSAIData.Goals)
            {
                if (IsShipGoalInvalid(gsave))
                    continue;

                Goal g = Goal.Deserialize(gsave.GoalName, e, gsave);
                if (gsave.FleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                    {
                        if (fleet.Value.Guid == gsave.FleetGuid) g.Fleet = fleet.Value;
                    }
                }

                g.TargetSystem = us.GetSystem(gsave.TargetSystemGuid);
                g.PlanetBuildingAt   = us.GetPlanet(gsave.PlanetWhereBuildingAtGuid);
                g.ColonizationTarget = us.GetPlanet(gsave.MarkedPlanetGuid);
                g.TargetPlanet       = us.GetPlanet(gsave.TargetPlanetGuid);
                g.FinishedShip = us.GetShip(gsave.ColonyShipGuid);
                g.OldShip      = us.GetShip(gsave.OldShipGuid);
                g.TargetShip   = us.GetShip(gsave.TargetShipGuid);

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

        static void CreateShipGoals(UniverseScreen us, SavedGame.EmpireSaveData esd)
        {
            foreach (SavedGame.ShipSaveData shipData in esd.OwnedShips)
            {
                if (!us.Objects.FindShip(shipData.GUID, out Ship ship))
                    continue;

                if (shipData.AISave.WayPoints != null)
                    ship.AI.SetWayPoints(shipData.AISave.WayPoints);

                ship.AI.Target         = us.GetShip(shipData.AISave.AttackTarget);
                ship.AI.EscortTarget   = us.GetShip(shipData.AISave.EscortTarget);
                ship.AI.OrbitTarget    = us.GetPlanet(shipData.AISave.OrbitTarget);
                ship.AI.SystemToDefend = us.GetSystem(shipData.AISave.SystemToDefend);

                foreach (SavedGame.ShipGoalSave sg in shipData.AISave.ShipGoalsList)
                {
                    ship.AI.AddGoalFromSave(sg, us);
                }
            }
        }

        void CreateEmpires(UniverseScreen us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                bool isPlayer = d.Traits.Name == PlayerLoyalty;
                Empire e = CreateEmpireFromEmpireSaveData(d, isPlayer);
                us.AddEmpire(e);
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
            foreach (Empire e in EmpireManager.Empires)
                e.ResetTechsUsableByShips(e.GetOurFactionShips(), unlockBonuses: false);
        }

        static void GiftShipsFromServantEmpire(UniverseScreen us)
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


        static void CreateTasksGoalsRoads(UniverseScreen us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.EmpireSaveData esd in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(esd.Name);

                CreateSpaceRoads(us, esd, e);
                CreateGoals(us, esd, e);
                e.GetEmpireAI().ThreatMatrix.AddFromSave(esd.GSAIData, e);
                e.GetEmpireAI().UsedFleets = esd.GSAIData.UsedFleets;
                CreateMilitaryTasks(us, esd, e);
                CreateShipGoals(us, esd);
            }
        }

        void CreatePlanetImportExportShipLists(UniverseScreen us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.SolarSystemSaveData ssd in saveData.SolarSystemDataList)
                foreach (SavedGame.RingSave ring in ssd.RingList)
                {
                    if (ring.Asteroids)
                        continue;

                    SavedGame.PlanetSaveData savedPlanet = ring.Planet;
                    if (us.GetPlanet(savedPlanet.Guid, out Planet planet))
                    {
                        if (savedPlanet.IncomingFreighters != null)
                        {
                            foreach (Guid freighterGuid in savedPlanet.IncomingFreighters)
                            {
                                if (us.GetShip(freighterGuid, out Ship freighter))
                                    planet.AddToIncomingFreighterList(freighter);
                            }
                        }

                        if (savedPlanet.OutgoingFreighters != null)
                        {
                            foreach (Guid freighterGuid in savedPlanet.OutgoingFreighters)
                            {
                                if (us.GetShip(freighterGuid, out Ship freighter))
                                    planet.AddToOutgoingFreighterList(freighter);
                            }
                        }
                    }
                }
        }

        static void CreateAllObjects(UniverseScreen us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.EmpireData.Traits.Name);
                foreach (SavedGame.ShipSaveData shipSave in d.OwnedShips)
                    Ship.CreateShipFromSave(us, e, shipSave);
            }

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

        void CreateSolarSystems(UniverseScreen us, SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.SolarSystemSaveData ssd in saveData.SolarSystemDataList)
            {
                SolarSystem system = CreateSystemFromData(ssd, us);
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
            Empire.InitializeRelationships(saveData.EmpireDataList);
        }
    }
}