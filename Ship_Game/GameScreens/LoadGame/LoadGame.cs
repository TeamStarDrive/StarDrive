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
        UniverseScreen Universe;
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

                UniverseData data = LoadEverything(save, Progress.NextStep()); // 992ms
                Log.Info(ConsoleColor.Blue, $"  LoadEverything         elapsed: {Progress[1].ElapsedMillis}ms");

                Universe = CreateUniverseScreen(data, save, Progress.NextStep()); // 1244ms
                Log.Info(ConsoleColor.Blue, $"  CreateUniverseScreen   elapsed: {Progress[2].ElapsedMillis}ms");

                Progress.Finish();

                Log.Info(ConsoleColor.DarkRed, $"TOTAL LoadUniverseScreen elapsed: {Progress.ElapsedMillis}ms");
                return Universe;
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

        // Universe does not exist yet !
        UniverseData LoadEverything(SavedGame.UniverseSaveData saveData, ProgressCounter step)
        {
            step.Start(9); // arbitrary count... check # of calls below:

            ScreenManager.Instance.RemoveAllObjects();
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
            int numEmpires = saveData.EmpireDataList.Filter(e => !e.IsFaction).Length; 
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
            RestoreCapitals(saveData, data);                   step.Advance();
            RestoreSolarSystemCQs(saveData, data);             step.Finish();
            return data;
        }

        UniverseScreen CreateUniverseScreen(UniverseData data, SavedGame.UniverseSaveData save, ProgressCounter step)
        {
            var us = new UniverseScreen(data, PlayerLoyalty)
            {
                GamePace  = save.GamePacing,
                StarDate  = save.StarDate,
                CamPos    = new Vector3(save.campos.X, save.campos.Y, save.camheight),
                CamHeight = save.camheight,
                Paused    = true,
                CreateSimThread = StartSimThread,
            };

            step.StartAbsolute(0.05f, 0.5f, 2f);

            EmpireHullBonuses.RefreshBonuses();
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep());
            CreateSceneObjects(data);
            AllSystemsLoaded(data, step.NextStep());

            step.NextStep().Start(1); // This last step is a mess, using arbitrary count
            
            GameBase.Base.ResetElapsedTime();
            us.LoadContent();

            CreateAOs(data);
            FinalizeShips(us);
            us.Objects.UpdateLists(removeInactiveObjects: false);

            foreach(Empire empire in EmpireManager.Empires)
            {
                empire.GetEmpireAI().ThreatMatrix.RestorePinGuidsFromSave();
            }

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

                if (ship.loyalty != EmpireManager.Player && !ship.loyalty.isFaction && ship.fleet == null)
                {
                    if (!ship.AddedOnLoad) ship.loyalty.AddShipToManagedPools(ship);
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
                    p.InitializePlanetMesh();
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
        }

        static void RestoreCommodities(Planet p, SavedGame.PlanetSaveData psdata)
        {
            p.FoodHere = psdata.foodHere;
            p.ProdHere = psdata.prodHere;
            p.Population = psdata.Population;
        }

        static Empire CreateEmpireFromEmpireSaveData(SavedGame.EmpireSaveData sdata, bool isPlayer)
        {
            var e = new Empire();
            e.isPlayer = isPlayer;
            //TempEmpireData  Tdata = new TempEmpireData();

            e.isFaction = sdata.IsFaction;
            if (sdata.empireData == null)
            {
                e.data.Traits = sdata.Traits;
                e.EmpireColor = sdata.Traits.Color;
            }
            else
            {
                e.data = sdata.empireData;
                e.data.ResearchQueue = sdata.empireData.ResearchQueue;
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

        SolarSystem CreateSystemFromData(SavedGame.SolarSystemSaveData ssd)
        {
            var system = new SolarSystem
            {
                guid          = ssd.guid,
                Name          = ssd.Name,
                Position      = ssd.Position,
                Sun           = SunType.FindSun(ssd.SunPath), // old SunPath is actually the ID @todo RENAME
            };

            system.SetPiratePresence(ssd.PiratePresence);
            system.AsteroidsList.AddRange(ssd.AsteroidsList);
            system.MoonList.AddRange(ssd.Moons);
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

        static void CreateSpaceRoads(UniverseData data, SavedGame.EmpireSaveData d, Empire e)
        {
            e.SpaceRoadsList = new Array<SpaceRoad>();
            foreach (SavedGame.SpaceRoadSave roadSave in d.SpaceRoadData)
            {
                var road = new SpaceRoad();
                foreach (SolarSystem s in data.SolarSystemsList)
                {
                    if (roadSave.OriginGUID == s.guid) road.Origin = s;
                    if (roadSave.DestGUID == s.guid)   road.Destination = s;
                }

                foreach (SavedGame.RoadNodeSave roadNode in roadSave.RoadNodes)
                {
                    var node = new RoadNode();
                    data.FindShip(roadNode.Guid_Platform, out node.Platform);
                    node.Position = roadNode.Position;
                    road.RoadNodesList.Add(node);
                }

                e.SpaceRoadsList.Add(road);
            }
        }
        
        static void CreateShipFromSave(UniverseData data, SavedGame.ShipSaveData shipSave, Empire e)
        {
            Ship ship = Ship.CreateShipFromSave(e, shipSave);
            if (ship == null) // happens if module creation failed
                return;
            data.MasterShipList.Add(ship);
        }

        
        static void RestorePlanetConstructionQueue(SavedGame.UniverseSaveData saveData, SavedGame.RingSave rsave, Planet p)
        {
            foreach (SavedGame.QueueItemSave qisave in rsave.Planet.QISaveList)
            {
                var qi  = new QueueItem(p);
                qi.Rush = qisave.Rush;
                if (qisave.isBuilding)
                {
                    qi.isBuilding    = true;
                    qi.IsMilitary    = qisave.IsMilitary;
                    qi.Building      = ResourceManager.CreateBuilding(qisave.UID);
                    qi.Cost          = qi.Building.ActualCost;
                    qi.NotifyOnEmpty = false;
                    qi.IsPlayerAdded = qisave.isPlayerAdded;

                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.X != (int) qisave.pgsVector.X || pgs.Y != (int) qisave.pgsVector.Y)
                            continue;

                        pgs.QItem = qi;
                        qi.pgs    = pgs;
                        break;
                    }
                }

                if (qisave.isTroop)
                {
                    qi.isTroop = true;
                    qi.TroopType = qisave.UID;
                    qi.Cost = ResourceManager.GetTroopCost(qisave.UID);
                    qi.NotifyOnEmpty = false;
                }

                if (qisave.isShip)
                {
                    qi.isShip = true;
                    if (!ResourceManager.GetShipTemplate(qisave.UID, out Ship shipTemplate))
                        continue;

                    qi.sData           = shipTemplate.shipData;
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

                if (qisave.isShip && qi.Goal != null)
                {
                    qi.Goal.ShipToBuild = ResourceManager.GetShipTemplate(qisave.UID);
                }

                qi.ProductionSpent = qisave.ProgressTowards;
                p.Construction.Enqueue(qi);
            }
        }

        static void RestoreCapitals(SavedGame.UniverseSaveData sData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData d in sData.EmpireDataList)
            {
                Empire e       = EmpireManager.GetEmpireByName(d.Name);
                data.FindPlanet(d.CapitalGuid, out Planet capital);
                e.SetCapital(capital);
            }
        }
        
        static void RestoreSolarSystemCQs(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.SolarSystemSaveData sdata in saveData.SolarSystemDataList)
            {
                foreach (SavedGame.RingSave rsave in sdata.RingList)
                {
                    if (rsave.Planet != null)
                    {
                        Planet p = data.FindPlanetOrNull(rsave.Planet.guid);
                        if (p?.Owner != null)
                        {
                            RestorePlanetConstructionQueue(saveData, rsave, p);
                        }
                    }
                }
            }
        }

        
        static void CreateFleetsFromSave(SavedGame.UniverseSaveData saveData, UniverseData data)
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
                        FinalDirection = fleetsave.facing.RadiansToDirection(),
                        Owner = e
                    };

                    fleet.AddFleetDataNodes(fleetsave.DataNodes);

                    foreach (SavedGame.FleetShipSave ssave in fleetsave.ShipsInFleet)
                    {
                        foreach (Ship ship in data.MasterShipList)
                        {
                            if (ship.guid != ssave.shipGuid)
                                continue;

                            // fleet saves can be corrupted because in older saves,
                            // so for avoiding bugs, don't add ship to the same fleet twice
                            // @todo @hack This "Core Fleet" stuff is just a temp hack, please solve this issue
                            if (ship.fleet == fleet ||
                                ship.fleet != null && (fleet.Name.IsEmpty() || fleet.Name == "Core Fleet"))
                                continue;

                            ship.RelativeFleetOffset = ssave.fleetOffset;
                            fleet.AddShip(ship);
                        }
                    }

                    foreach (FleetDataNode node in fleet.DataNodes)
                    {
                        foreach (Ship ship in fleet.Ships)
                        {
                            if (!(node.ShipGuid != Guid.Empty) || !(ship.guid == node.ShipGuid))
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

        static void CreateAOs(UniverseData data)
        {
            foreach (Empire e in data.EmpireList)
                e.GetEmpireAI().InitializeAOsFromSave();
        }

        static void CreateMilitaryTasks(SavedGame.EmpireSaveData d, Empire e, UniverseData data)
        {
            for (int i = 0; i < d.GSAIData.MilitaryTaskList.Count; i++)
            {
                d.GSAIData.MilitaryTaskList[i].RestoreFromSaveNoUniverse(e, data);
            }

            e.GetEmpireAI().ReadFromSave(d.GSAIData);
        }

        static bool IsShipGoalInvalid(SavedGame.GoalSave g)
        {
            if (g.type != GoalType.BuildOffensiveShips &&
                g.type != GoalType.IncreaseFreighters)
            {
                return false;
            }

            return g.ToBuildUID != null && !ResourceManager.ShipTemplateExists(g.ToBuildUID);
        }
        
        static void CreateGoals(SavedGame.EmpireSaveData esd, Empire e, UniverseData data)
        {
            foreach (SavedGame.GoalSave gsave in esd.GSAIData.Goals)
            {
                if (IsShipGoalInvalid(gsave))
                    continue;

                Goal g = Goal.Deserialize(gsave.GoalName, e, gsave);
                if (gsave.fleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                    {
                        if (fleet.Value.Guid == gsave.fleetGuid) g.Fleet = fleet.Value;
                    }
                }

                foreach (SolarSystem s in data.SolarSystemsList)
                {
                    if (s.guid == gsave.TargetSystemGuid)
                       g.TargetSystem = s;

                    foreach (Planet p in s.PlanetList)
                    {
                        if (p.guid == gsave.planetWhereBuildingAtGuid) g.PlanetBuildingAt   = p;
                        if (p.guid == gsave.markedPlanetGuid)          g.ColonizationTarget = p;
                        if (p.guid == gsave.TargetPlanetGuid)          g.TargetPlanet       = p;
                    }
                }

                foreach (Ship s in data.MasterShipList)
                {
                    if      (gsave.colonyShipGuid == s.guid) g.FinishedShip = s;
                    else if (gsave.beingBuiltGUID == s.guid) g.ShipToBuild  = s;
                    else if (gsave.OldShipGuid    == s.guid) g.OldShip      = s;
                    else if (gsave.TargetShipGuid == s.guid) g.TargetShip   = s;
                }

                if (g.type == GoalType.Refit && gsave.ToBuildUID != null)
                {
                    Ship shipToBuild = ResourceManager.GetShipTemplate(gsave.ToBuildUID, false);
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

        static void CreateShipGoals(SavedGame.EmpireSaveData esd, UniverseData data, Empire e)
        {
            foreach (SavedGame.ShipSaveData shipData in esd.OwnedShips)
            {
                if (!data.FindShip(shipData.guid, out Ship ship))
                    continue;

                if (shipData.AISave.WayPoints != null)
                    ship.AI.SetWayPoints(shipData.AISave.WayPoints);

                ship.AI.Target         = data.FindShipOrNull(shipData.AISave.AttackTarget);
                ship.AI.EscortTarget   = data.FindShipOrNull(shipData.AISave.EscortTarget);
                ship.AI.OrbitTarget    = data.FindPlanetOrNull(shipData.AISave.OrbitTarget);
                ship.AI.SystemToDefend = data.FindSystemOrNull(shipData.AISave.SystemToDefend);

                foreach (SavedGame.ShipGoalSave sg in shipData.AISave.ShipGoalsList)
                {
                    if (sg.Plan == ShipAI.Plan.DeployStructure || sg.Plan == ShipAI.Plan.DeployOrbital)
                        ship.IsConstructor = true;

                    ship.AI.AddGoalFromSave(sg, data);
                }
            }
        }

        void CreateEmpires(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                bool isPlayer = d.Traits.Name == PlayerLoyalty;
                Empire e = CreateEmpireFromEmpireSaveData(d, isPlayer);
                data.EmpireList.Add(e);
                if (isPlayer)
                {
                    e.AutoColonize          = saveData.AutoColonize;
                    e.AutoExplore           = saveData.AutoExplore;
                    e.AutoFreighters        = saveData.AutoFreighters;
                    e.AutoPickBestFreighter = saveData.AutoPickBestFreighter;
                    e.AutoPickBestColonizer = saveData.AutoPickBestColonizer;
                    e.AutoBuild             = saveData.AutoProjectors;
                }

                EmpireManager.Add(e);
            }
        }

        static void GiftShipsFromServantEmpire(UniverseData data)
        {
            foreach (Empire e in data.EmpireList)
            {
                if (e.data.AbsorbedBy == null)
                    continue;
                Empire servantEmpire = e;
                Empire masterEmpire = EmpireManager.GetEmpireByName(servantEmpire.data.AbsorbedBy);
                
                masterEmpire.AssimilateTech(servantEmpire);
            }
        }


        static void CreateTasksGoalsRoads(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData esd in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(esd.Name);

                CreateSpaceRoads(data, esd, e);
                CreateGoals(esd, e, data);
                e.GetEmpireAI().ThreatMatrix.AddFromSave(esd.GSAIData, e);
                e.GetEmpireAI().UsedFleets = esd.GSAIData.UsedFleets;
                CreateMilitaryTasks(esd, e, data);
                CreateShipGoals(esd, data, e);
            }
        }

        void CreatePlanetImportExportShipLists(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.SolarSystemSaveData ssd in saveData.SolarSystemDataList)
                foreach (SavedGame.RingSave ring in ssd.RingList)
                {
                    if (ring.Asteroids)
                        continue;

                    SavedGame.PlanetSaveData savedPlanet = ring.Planet;
                    if (data.FindPlanet(savedPlanet.guid, out Planet planet))
                    {
                        if (savedPlanet.IncomingFreighters != null)
                        {
                            foreach (Guid freighterGuid in savedPlanet.IncomingFreighters)
                            {
                                data.FindShip(freighterGuid, out Ship freighter);
                                planet.AddToIncomingFreighterList(freighter);
                            }
                        }

                        if (savedPlanet.OutgoingFreighters != null)
                        {
                            foreach (Guid freighterGuid in savedPlanet.OutgoingFreighters)
                            {
                                data.FindShip(freighterGuid, out Ship freighter);
                                planet.AddToOutgoingFreighterList(freighter);
                            }
                        }
                    }
                }
        }

        static void CreateAllObjects(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.empireData.Traits.Name);
                foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
                    CreateShipFromSave(data, shipData, e);
            }

            if (saveData.Projectiles != null) // NULL check: backwards compatibility
            {
                foreach (SavedGame.ProjectileSaveData projData in saveData.Projectiles)
                {
                    var p = Projectile.Create(projData, data);
                    if (p != null) // invalid projectile data, maybe savegame issue
                        data.MasterProjectileList.Add(p);
                }
            }
            if (saveData.Beams != null) // NULL check: backwards compatibility
            {
                foreach (SavedGame.BeamSaveData beamData in saveData.Beams)
                {
                    var b = Beam.Create(beamData, data);
                    if (b != null) // invalid beam data, maybe savegame issue
                        data.MasterProjectileList.Add(b);
                }
            }
        }

        void CreateSolarSystems(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.SolarSystemSaveData ssd in saveData.SolarSystemDataList)
                data.SolarSystemsList.Add(CreateSystemFromData(ssd));
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