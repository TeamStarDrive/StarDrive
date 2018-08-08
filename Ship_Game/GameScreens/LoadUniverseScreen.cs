using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class LoadUniverseScreen : GameScreen
    {
        private UniverseData data;
        private SavedGame.UniverseSaveData savedData;
        private float GamePace;
        private float GameScale;
        private string PlayerLoyalty;
        //private bool ReadyToRoll;
        private string text;
        private Texture2D LoadingImage;
        private int systemToMake;
        private ManualResetEvent GateKeeper = new ManualResetEvent(false);
        private bool Loaded;
        private UniverseScreen us;
        private float percentloaded;
        private bool ready;

        public LoadUniverseScreen(FileInfo activeFile) : base(null/*no parent*/)
        {
            GlobalStats.RemnantKills = 0;
            GlobalStats.RemnantArmageddon = false;
            GlobalStats.Statreset();
            var bgw = new BackgroundWorker();
            bgw.DoWork += DecompressFile;
            bgw.RunWorkerCompleted += LoadEverything;
            bgw.RunWorkerAsync(activeFile);
        }

        private static Empire CreateEmpireFromEmpireSaveData(SavedGame.EmpireSaveData sdata, bool isPlayer)
        {
            var e = new Empire();
            e.isPlayer = isPlayer;
            //TempEmpireData  Tdata = new TempEmpireData();

            e.isFaction = sdata.IsFaction;
            if (sdata.empireData == null)
            {
                e.data.Traits = sdata.Traits;
                e.EmpireColor = new Color((byte)sdata.Traits.R, (byte)sdata.Traits.G, (byte)sdata.Traits.B);
            }
            else
            {
                e.data = new EmpireData();
                
                foreach (string key in e.data.WeaponTags.Keys)
                {
                    if (sdata.empireData.WeaponTags.ContainsKey(key))
                        continue;
                    sdata.empireData.WeaponTags.Add(key, new WeaponTagModifier());
                }
                e.data = sdata.empireData;
                
                e.data.ResearchQueue = sdata.empireData.ResearchQueue;
                e.ResearchTopic      = sdata.ResearchTopic ?? "";
                e.PortraitName       = e.data.PortraitName;
                e.dd                 = ResourceManager.DDDict[e.data.DiplomacyDialogPath];
                e.EmpireColor = new Color((byte)e.data.Traits.R, (byte)e.data.Traits.G, (byte)e.data.Traits.B);
                e.data.CurrentAutoScout     = sdata.CurrentAutoScout     ?? e.data.ScoutShip;
                e.data.CurrentAutoColony    = sdata.CurrentAutoColony    ?? e.data.ColonyShip;
                e.data.CurrentAutoFreighter = sdata.CurrentAutoFreighter ?? e.data.FreighterShip;
                e.data.CurrentConstructor   = sdata.CurrentConstructor   ?? e.data.ConstructorShip;
                if (sdata.empireData.DefaultTroopShip.IsEmpty())
                    e.data.DefaultTroopShip = e.data.PortraitName + " " + "Troop";
            }
            foreach(TechEntry tech in sdata.TechTree)
            {
                if (!ResourceManager.TryGetTech(tech.UID, out _))
                    Log.Warning($"LoadTech ignoring invalid tech: {tech.UID}");
                else e.TechnologyDict.Add(tech.UID, tech);
            }            
            e.InitializeFromSave();
            e.Money = sdata.Money;
            e.Research = sdata.Research;
            e.GetGSAI().AreasOfOperations = sdata.AOs;            
  
            return e;
        }

        private static Planet CreatePlanetFromPlanetSaveData(SolarSystem forSystem, SavedGame.PlanetSaveData psdata)
        {
            var p = new Planet
            {
                ParentSystem = forSystem,
                guid = psdata.guid,
                Name = psdata.Name
            };
            if (!string.IsNullOrEmpty(psdata.Owner))
            {
                p.Owner = EmpireManager.GetEmpireByName(psdata.Owner);
                p.Owner.AddPlanet(p);
            }
            if(!string.IsNullOrEmpty(psdata.SpecialDescription))
            {
                p.SpecialDescription = psdata.SpecialDescription;
            }
            if (psdata.Scale > 0f)
            {
                p.Scale = psdata.Scale;
            }
            else
            {
                float scale = RandomMath.RandomBetween(1f, 2f);
                p.Scale = scale;
            }
            p.colonyType = psdata.ColonyType;
            if (!psdata.GovernorOn)
            {
                p.GovernorOn = false;
                p.colonyType = Planet.ColonyType.Colony;
            }
            p.OrbitalAngle          = psdata.OrbitalAngle;
            p.FS                    = psdata.FoodState;
            p.PS                    = psdata.ProdState;
            p.FoodLocked            = psdata.FoodLock;
            p.ProdLocked            = psdata.ProdLock;
            p.ResLocked             = psdata.ResLock;
            p.OrbitalRadius         = psdata.OrbitalDistance;
            p.MaxPopulation         = psdata.PopulationMax;
                   
            p.Fertility             = psdata.Fertility;
            p.MineralRichness       = psdata.Richness;
            p.TerraformPoints       = psdata.TerraformPoints;
            p.HasRings              = psdata.HasRings;
            p.PlanetType            = psdata.WhichPlanet;
            p.ShieldStrengthCurrent = psdata.ShieldStrength;
            p.LoadAttributes();
            p.CrippledTurns         = psdata.Crippled_Turns;
            p.PlanetTilt            = RandomMath.RandomBetween(45f, 135f);
            p.ObjectRadius          = 1000f * (float)(1 + (Math.Log(p.Scale) / 1.5));
            foreach (Guid guid in psdata.StationsList)
                p.Shipyards[guid]   = null; // reserve shipyards
            p.FarmerPercentage      = psdata.farmerPercentage;
            p.WorkerPercentage      = psdata.workerPercentage;
            p.ResearcherPercentage  = psdata.researcherPercentage;

            

            if (p.HasRings)
            {
                p.RingTilt = RandomMath.RandomBetween(-80f, -45f);
            }
            foreach (SavedGame.PGSData d in psdata.PGSList)
            {
                var pgs = new PlanetGridSquare(d.x, d.y, d.building, d.Habitable)
                {
                    Biosphere = d.Biosphere
                };
                if (pgs.Biosphere)
                {
                    p.BuildingList.Add(ResourceManager.CreateBuilding("Biospheres"));
                }
                p.TilesList.Add(pgs);
                foreach (Troop t in d.TroopsHere)
                {
                    if (!ResourceManager.TroopTypes.Contains(t.Name))
                        continue;
                    var fix = ResourceManager.GetTroopTemplate(t.Name);
                    t.first_frame = fix.first_frame;
                    t.WhichFrame = fix.first_frame;
                    pgs.TroopsHere.Add(t);
                    p.TroopsHere.Add(t);
                    t.SetPlanet(p);
                }

                if (pgs.building == null)
                    continue;

                var building = ResourceManager.GetBuildingTemplate(pgs.building.Name);
                pgs.building.Scrappable = building.Scrappable;
                p.BuildingList.Add(pgs.building);
                pgs.building.CreateWeapon();
            }

            return p;
        }

        private static void RestoreCommodities(Planet p, SavedGame.PlanetSaveData psdata)
        {
            p.SbCommodities.AddGood("Food", psdata.foodHere, false);
            p.SbCommodities.AddGood("Production", psdata.prodHere, false);
            p.SbCommodities.AddGood("Colonists_1000", psdata.Population, false);
        }

        private SolarSystem CreateSystemFromData(SavedGame.SolarSystemSaveData ssdata)
        {
            var system = new SolarSystem
            {
                Name          = ssdata.Name,
                Position      = ssdata.Position,
                SunPath       = ssdata.SunPath,
                AsteroidsList = new BatchRemovalCollection<Asteroid>(),
                MoonList      = new Array<Moon>()
            };
            foreach (Asteroid roid in ssdata.AsteroidsList)
            {
                roid.Initialize();
                system.AsteroidsList.Add(roid);
            }
            foreach (Moon moon in ssdata.Moons)
            {
                moon.Initialize();
                system.MoonList.Add(moon);
            }
            system.SetExploredBy(ssdata.EmpiresThatKnowThisSystem);
            system.RingList = new Array<SolarSystem.Ring>();
            foreach (SavedGame.RingSave ring in ssdata.RingList)
            {
                if (ring.Asteroids)
                {
                    system.RingList.Add(new SolarSystem.Ring { Asteroids = true });
                }
                else
                {
                    Planet p = CreatePlanetFromPlanetSaveData(system, ring.Planet);
                    p.Center = system.Position.PointOnCircle(p.OrbitalAngle, p.OrbitalRadius);
                    
                    foreach (Building b in p.BuildingList)
                    {
                        if (b.Name != "Space Port")
                            continue;
                        p.Station = new SpaceStation
                        {
                            planet       = p,
                            Position     = p.Center,
                            ParentSystem = p.ParentSystem
                        };
                        p.Station.LoadContent(ScreenManager);
                        p.HasShipyard = true;
                    }
                    
                    if (p.Owner != null && !system.OwnerList.Contains(p.Owner))
                    {
                        system.OwnerList.Add(p.Owner);
                    }
                    system.PlanetList.Add(p);
                    p.SetExploredBy(ssdata.EmpiresThatKnowThisSystem);

                    system.RingList.Add(new SolarSystem.Ring
                    {
                        planet    = p,
                        Asteroids = false
                    });
                    RestoreCommodities(p, ring.Planet);
                    p.UpdateIncomes(true);  //fbedard: needed for OrderTrade()
                    
                }
            }
            return system;
        }
        
        private void DecompressFile(object info, DoWorkEventArgs e)
        {
            SavedGame.UniverseSaveData usData = SavedGame.DeserializeFromCompressedSave((FileInfo)e.Argument);

            if (usData.SaveGameVersion != SavedGame.SaveGameVersion)
                Log.Error("Incompatible savegame version! Got v{0} but expected v{1}", usData.SaveGameVersion, SavedGame.SaveGameVersion);

            GlobalStats.RemnantKills         = usData.RemnantKills;
            GlobalStats.RemnantActivation    = usData.RemnantActivation;
            GlobalStats.RemnantArmageddon    = usData.RemnantArmageddon;
            GlobalStats.GravityWellRange     = usData.GravityWellRange;
            GlobalStats.IconSize             = usData.IconSize;
            GlobalStats.MinimumWarpRange     = usData.MinimumWarpRange;
            GlobalStats.ShipMaintenanceMulti = usData.OptionIncreaseShipMaintenance;
            GlobalStats.PreventFederations   = usData.preventFederations;
            GlobalStats.EliminationMode      = usData.EliminationMode;
            if (usData.TurnTimer == 0)
                usData.TurnTimer = 5;
            GlobalStats.TurnTimer = usData.TurnTimer;

            savedData     = usData;
            GamePace      = usData.GamePacing;
            GameScale     = usData.GameScale;
            PlayerLoyalty = usData.PlayerLoyalty;
            UniverseScreen.GamePaceStatic  = GamePace;
            UniverseScreen.GameScaleStatic = GameScale;
            RandomEventManager.ActiveEvent = null;
            StatTracker.SnapshotsDict.Clear();
            StatTracker.SnapshotsDict = usData.Snapshots;
            UniverseData.UniverseWidth = usData.Size.X;
            GateKeeper.Set();
        }

        protected override void Destroy()
        {
            lock (this)
            {
                GateKeeper?.Dispose(ref GateKeeper);
                data = null;
            }
            base.Destroy();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);
            ScreenManager.SpriteBatch.Begin();
            var artRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
            ScreenManager.SpriteBatch.Draw(LoadingImage, artRect, Color.White);
            var meterBar = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 150, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 25, 300, 25);
            var pb = new ProgressBar(meterBar)
            {
                Max = 100f,
                Progress = percentloaded * 100f
            };
            pb.Draw(ScreenManager.SpriteBatch);
            var cursor = new Vector2(ScreenCenter.X - 250f, meterBar.Y - Fonts.Arial12Bold.MeasureString(text).Y - 5f);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, Color.White);
            if (ready)
            {
                percentloaded = 1f;
                cursor.Y = cursor.Y - Fonts.Pirulen16.LineSpacing - 10f;
                const string begin = "Click to Continue!";
                cursor.X = ScreenCenter.X - Fonts.Pirulen16.MeasureString(begin).X / 2f;
                TimeSpan totalGameTime = Game1.Instance.GameTime.TotalGameTime;
                float f = (float)Math.Sin(totalGameTime.TotalSeconds);
                f = Math.Abs(f) * 255f;
                var flashColor = new Color(255, 255, 255, (byte)f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, begin, cursor, flashColor);
            }
            ScreenManager.SpriteBatch.End();
        }


        public void Go()
        {
            foreach (Empire e in data.EmpireList)
            {
                e.GetGSAI().InitialzeAOsFromSave(data);
            }
            foreach (Ship ship in us.MasterShipList)
            {
                if (!ship.Active)
                {
                    us.MasterShipList.QueuePendingRemoval(ship);
                    continue;
                }
                if (ship.loyalty != EmpireManager.Player && ship.fleet == null)
                {
                    if (!ship.AddedOnLoad) ship.loyalty.ForcePoolAdd(ship);
                }
                else if (ship.AI.State == AIState.SystemDefender)
                {
                    ship.loyalty.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }
            us.MasterShipList.ApplyPendingRemovals();
            GameAudio.StopGenericMusic(immediate: false);
            us.EmpireUI.empire = us.player;
            ScreenManager.AddScreenNoLoad(us);
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (ready && input.InGameSelect)
            {
                Go();
            }
            return false;
        }

        public override void LoadContent()
        {
            LoadingImage = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            text = HelperFunctions.ParseText(Fonts.Arial12Bold, ResourceManager.LoadRandomAdvice(), 500f);
            base.LoadContent();
        }

        private void LoadEverything(object sender, RunWorkerCompletedEventArgs ev)
        {
            ScreenManager.RemoveAllObjects();
            data = new UniverseData();
            RandomEventManager.ActiveEvent = savedData.RandomEvent;
            data.loadFogPath           = savedData.FogMapName;
            data.difficulty            = UniverseData.GameDifficulty.Normal;
            data.difficulty            = savedData.gameDifficulty;
            data.Size                  = savedData.Size;
            data.FTLSpeedModifier      = savedData.FTLModifier;
            data.EnemyFTLSpeedModifier = savedData.EnemyFTLModifier;
            data.GravityWells          = savedData.GravityWells;    
                        
            EmpireManager.Clear();
            if (Empire.Universe != null && Empire.Universe.MasterShipList != null)
                Empire.Universe.MasterShipList.Clear();
         
            foreach (SavedGame.EmpireSaveData d in savedData.EmpireDataList)
            {
                bool isPlayer = d.Traits.Name == PlayerLoyalty;
                Empire e = CreateEmpireFromEmpireSaveData(d, isPlayer);
                data.EmpireList.Add(e);
                if (isPlayer)
                {
                    e.AutoColonize   = savedData.AutoColonize;
                    e.AutoExplore    = savedData.AutoExplore;
                    e.AutoFreighters = savedData.AutoFreighters;
                    e.AutoBuild      = savedData.AutoProjectors;
                }
                EmpireManager.Add(e);
            }
            foreach (Empire e in data.EmpireList)
            {
                if (e.data.AbsorbedBy == null)
                {
                    continue;
                }

                Empire servantEmpire = e;
                Empire masterEmpire = EmpireManager.GetEmpireByName(servantEmpire.data.AbsorbedBy);
                foreach (KeyValuePair<string, TechEntry> masterEmpireTech in masterEmpire.GetTDict())
                {
                    if (masterEmpireTech.Value.Unlocked)
                        masterEmpire.UnlockHullsSave(masterEmpireTech.Value, servantEmpire.data.Traits.ShipType);
                }
            }
            foreach (SavedGame.EmpireSaveData d in savedData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                foreach (Relationship r in d.Relations)
                {
                    Empire empire = EmpireManager.GetEmpireByName(r.Name);
                    e.AddRelationships(empire, r);
                    r.ActiveWar?.SetCombatants(e, empire);
                    r.Risk = new EmpireRiskAssessment(r);

                }
            }
            data.SolarSystemsList = new Array<SolarSystem>();
            foreach (SavedGame.SolarSystemSaveData sdata in savedData.SolarSystemDataList)
            {
                SolarSystem system = CreateSystemFromData(sdata);
                system.guid = sdata.guid;
                data.SolarSystemsList.Add(system);
            }
            foreach (SavedGame.EmpireSaveData d in savedData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.empireData.Traits.Name);
                foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
                {
                    AddShipFromSaveData(shipData, e);
                }
            }
            foreach (SavedGame.EmpireSaveData d in savedData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                foreach (SavedGame.FleetSave fleetsave in d.FleetsList)
                {
                    var fleet = new Fleet
                    {
                        Guid        = fleetsave.FleetGuid,
                        IsCoreFleet = fleetsave.IsCoreFleet,
                        Facing      = fleetsave.facing
                    };
                    foreach (SavedGame.FleetShipSave ssave in fleetsave.ShipsInFleet)
                    {
                        foreach (Ship ship in data.MasterShipList)
                        {
                            if (ship.guid != ssave.shipGuid)
                                continue;

                            // fleet saves can be corrupted because in older saves,
                            // so for avoiding bugs, don't add ship to the same fleet twice
                            // @todo @hack This "Core Fleet" stuff is just a temp hack, please solve this issue
                            if (ship.fleet == fleet || ship.fleet != null && (fleet.Name.IsEmpty() || fleet.Name == "Core Fleet"))
                                continue;

                            ship.RelativeFleetOffset = ssave.fleetOffset;
                            fleet.AddShip(ship);
                        }
                    }
                    foreach (FleetDataNode node in fleetsave.DataNodes)
                    {
                        fleet.DataNodes.Add(node);
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
                    fleet.AssignPositions(fleet.Facing);
                    fleet.Name = fleetsave.Name;
                    fleet.TaskStep = fleetsave.TaskStep;
                    fleet.Owner = e;
                    fleet.Position = fleetsave.Position;

                    if (e.GetFleetsDict().ContainsKey(fleetsave.Key))
                    {
                        e.GetFleetsDict()[fleetsave.Key] = fleet;
                    }
                    else
                    {
                        e.GetFleetsDict().Add(fleetsave.Key, fleet);
                    }
                    e.GetFleetsDict()[fleetsave.Key].SetSpeed();
                    fleet.FindAveragePositionset();
                    fleet.Setavgtodestination();
                    
                }
            }
            foreach (SavedGame.EmpireSaveData d in savedData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                e.SpaceRoadsList = new Array<SpaceRoad>();
                foreach (SavedGame.SpaceRoadSave roadsave in d.SpaceRoadData)
                {
                    var road = new SpaceRoad();
                    foreach (SolarSystem s in data.SolarSystemsList)
                    {
                        if (roadsave.OriginGUID == s.guid)
                        {
                            road.SetOrigin(s);
                        }
                        if (roadsave.DestGUID != s.guid)
                        {
                            continue;
                        }
                        road.SetDestination(s);
                    }
                    foreach (SavedGame.RoadNodeSave nodesave in roadsave.RoadNodes)
                    {
                        var node = new RoadNode();
                        foreach (Ship s in data.MasterShipList)
                            if (nodesave.Guid_Platform == s.guid)
                                node.Platform = s;
                        node.Position = nodesave.Position;
                        road.RoadNodesList.Add(node);
                    }
                    e.SpaceRoadsList.Add(road);
                }
                foreach (SavedGame.GoalSave gsave in d.GSAIData.Goals)
                {
                    if (gsave.type == GoalType.BuildShips && gsave.ToBuildUID != null 
                        && !ResourceManager.ShipsDict.ContainsKey(gsave.ToBuildUID))
                        continue;

                    Goal g = Goal.Deserialize(gsave.GoalName, e, gsave);
                    if (gsave.fleetGuid != Guid.Empty)
                    {
                        foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                        {
                            if (fleet.Value.Guid == gsave.fleetGuid) g.SetFleet(fleet.Value);
                        }
                    }
                    foreach (SolarSystem s in data.SolarSystemsList)
                    {
                        foreach (Planet p in s.PlanetList)
                        {
                            if (p.guid == gsave.planetWhereBuildingAtGuid) g.SetPlanetWhereBuilding(p);
                            if (p.guid == gsave.markedPlanetGuid)          g.SetMarkedPlanet(p);
                        }
                    }
                    foreach (Ship s in data.MasterShipList)
                    {
                        if (gsave.colonyShipGuid == s.guid) g.SetColonyShip(s);
                        if (gsave.beingBuiltGUID == s.guid) g.SetBeingBuilt(s);
                    }
                    e.GetGSAI().Goals.Add(g);
                }
                for (int i = 0; i < d.GSAIData.PinGuids.Count; i++)
                {
                    e.GetGSAI().ThreatMatrix.Pins.Add(d.GSAIData.PinGuids[i], d.GSAIData.PinList[i]);
                }
                e.GetGSAI().UsedFleets = d.GSAIData.UsedFleets;
                lock (GlobalStats.TaskLocker)
                {
                    foreach (MilitaryTask task in d.GSAIData.MilitaryTaskList)
                    {
                        task.SetEmpire(e);
                        e.GetGSAI().TaskList.Add(task);
                        if (task.TargetPlanetGuid != Guid.Empty)
                        {
                            foreach (SolarSystem s in data.SolarSystemsList)
                            {
                                bool stop = false;
                                foreach (Planet p in s.PlanetList)
                                {
                                    if (p.guid != task.TargetPlanetGuid)
                                        continue;
                                    task.SetTargetPlanet(p);
                                    stop = true;
                                    break;
                                }
                                if (stop) break;
                            }
                        }
                        foreach (Guid guid in task.HeldGoals)
                        {
                            foreach (Goal g in e.GetGSAI().Goals)
                            {
                                if (g.guid == guid)
                                    g.Held = true;
                            }
                        }
                        try
                        {
                            if (task.WhichFleet != -1)
                                e.GetFleetsDict()[task.WhichFleet].FleetTask = task;
                        }
                        catch
                        {
                            task.WhichFleet = 0;
                        }
                    }
                }
                foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
                {
                    foreach (Ship ship in data.MasterShipList)
                    {
                        if (ship.guid != shipData.guid)
                            continue;
                        foreach (Vector2 waypoint in shipData.AISave.ActiveWayPoints)
                        {
                            ship.AI.WayPoints.Enqueue(waypoint);
                        }
                        foreach (SavedGame.ShipGoalSave sg in shipData.AISave.ShipGoalsList)
                        {
                            var g = new ShipAI.ShipGoal(sg.Plan, sg.MovePosition, sg.FacingVector);
                            foreach (SolarSystem s in data.SolarSystemsList)
                            {
                                foreach (Planet p in s.PlanetList)
                                {
                                    if (sg.TargetPlanetGuid == p.guid)
                                    {
                                        g.TargetPlanet = p;
                                        ship.AI.ColonizeTarget = p;
                                    }
                                    if (p.guid == shipData.AISave.startGuid) ship.AI.start = p;
                                    if (p.guid == shipData.AISave.endGuid)   ship.AI.end   = p;
                                }
                            }
                            if (sg.fleetGuid != Guid.Empty)
                            {
                                foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                                {
                                    if (fleet.Value.Guid == sg.fleetGuid)
                                        g.fleet = fleet.Value;
                                }
                            }
                            g.VariableString = sg.VariableString;
                            g.DesiredFacing  = sg.DesiredFacing;
                            g.SpeedLimit     = sg.SpeedLimit;
                            foreach (Goal goal in ship.loyalty.GetGSAI().Goals)
                            {
                                if (sg.goalGuid == goal.guid)
                                    g.goal = goal;
                            }
                            ship.AI.OrderQueue.Enqueue(g);
                            if (g.Plan == ShipAI.Plan.DeployStructure)
                                ship.isConstructor = true;
                        }
                    }
                }
            }
            foreach (SavedGame.SolarSystemSaveData sdata in savedData.SolarSystemDataList)
            {
                foreach (SavedGame.RingSave rsave in sdata.RingList)
                {
                    Planet p = null;
                    foreach (SolarSystem s in data.SolarSystemsList)
                    {
                        foreach (Planet p1 in s.PlanetList)
                        {
                            if (p1.guid != rsave.Planet?.guid) continue;
                            p = p1;
                            break;
                        }
                    }
                    if (p?.Owner == null)
                        continue;
                    foreach (SavedGame.QueueItemSave qisave in rsave.Planet.QISaveList)
                    {
                        var qi = new QueueItem(p);
                        if (qisave.isBuilding)
                        {
                            qi.isBuilding = true;
                            qi.Building = ResourceManager.BuildingsDict[qisave.UID];
                            qi.Cost = qi.Building.Cost * savedData.GamePacing;
                            qi.NotifyOnEmpty = false;
                            qi.IsPlayerAdded = qisave.isPlayerAdded;
                            foreach (PlanetGridSquare pgs in p.TilesList)
                            {
                                if (pgs.x != (int)qisave.pgsVector.X || pgs.y != (int)qisave.pgsVector.Y)
                                    continue;
                                pgs.QItem = qi;
                                qi.pgs = pgs;
                                break;
                            }
                        }
                        if (qisave.isTroop)
                        {
                            qi.isTroop = true;
                            qi.troopType = qisave.UID;
                            qi.Cost = ResourceManager.GetTroopCost(qisave.UID);
                            qi.NotifyOnEmpty = false;
                        }
                        if (qisave.isShip)
                        {
                            qi.isShip = true;
                            if (!ResourceManager.ShipsDict.ContainsKey(qisave.UID))
                                continue;

                            Ship shipTemplate = ResourceManager.GetShipTemplate(qisave.UID);
                            qi.sData = shipTemplate.shipData;
                            qi.DisplayName = qisave.DisplayName;
                            qi.Cost = shipTemplate.GetCost(p.Owner);

                            if (qi.sData.HasFixedCost)
                            {
                                qi.Cost = qi.sData.FixedCost;
                            }
                            if (qisave.IsRefit)
                            {
                                qi.isRefit = true;
                                qi.Cost = qisave.RefitCost;
                            }
                        }
                        foreach (Goal g in p.Owner.GetGSAI().Goals)
                        {
                            if (g.guid != qisave.GoalGUID)
                                continue;
                            qi.Goal = g;
                            qi.NotifyOnEmpty = false;
                        }
                        if (qisave.isShip && qi.Goal != null)
                        {
                            qi.Goal.beingBuilt = ResourceManager.GetShipTemplate(qisave.UID);
                        }
                        qi.productionTowards = qisave.ProgressTowards;
                        p.ConstructionQueue.Add(qi);
                    }
                }
            }

            Loaded = true;
            UniverseData.UniverseWidth = data.Size.X ;
        }

        private void AddShipFromSaveData(SavedGame.ShipSaveData shipSave, Empire e)
        {
            Ship ship = Ship.CreateShipFromSave(e, shipSave);
            if (ship == null) // happens if module creation failed
                return;

            e.AddShip(ship);
            if (ship.PlayerShip)
                data.playerShip = ship;
            data.MasterShipList.Add(ship);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (!GateKeeper.WaitOne(0) || ready || !Loaded)
                return;

            if (us == null)
            {
                us = new UniverseScreen(data, PlayerLoyalty)
                {
                    GamePace       = GamePace,
                    GameScale      = GameScale,
                    GameDifficulty = data.difficulty,
                    StarDate       = savedData.StarDate,
                    ScreenManager  = ScreenManager,
                    CamPos         = new Vector3(savedData.campos.X, savedData.campos.Y, savedData.camheight),
                    CamHeight      = savedData.camheight,
                    player         = EmpireManager.Player
                };

                EmpireShipBonuses.RefreshBonuses();
            }

            SolarSystem system = data.SolarSystemsList[systemToMake];
            percentloaded = systemToMake / (float)data.SolarSystemsList.Count;
            foreach (Planet p in system.PlanetList)
            {
                p.ParentSystem = system;
                p.InitializePlanetMesh(this);
            }
            foreach (Asteroid roid in system.AsteroidsList) AddObject(roid.So);
            foreach (Moon moon in system.MoonList)           AddObject(moon.So);

            ++systemToMake;
            if (systemToMake == data.SolarSystemsList.Count)
                AllSystemsLoaded();

            Game1.Instance.ResetElapsedTime();
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        private void AllSystemsLoaded()
        {
            if (systemToMake == data.SolarSystemsList.Count)
            {
                foreach (Ship ship in data.MasterShipList)
                {                    
                    ship.InitializeShip(loadingFromSavegame: true);
                    if (ship.Carrier.HasHangars)
                    {
                        foreach (ShipModule hangar in ship.Carrier.AllActiveHangars)
                        {
                            foreach (Ship othership in ship.loyalty.GetShips())
                            {
                                if (hangar.HangarShipGuid != othership.guid)
                                    continue;
                                hangar.SetHangarShip(othership);
                                othership.Mothership = ship;
                            }
                        }
                    }
                    foreach (SolarSystem s in data.SolarSystemsList)
                    {
                        Guid orbitTargetGuid = ship.AI.OrbitTargetGuid;
                        foreach (Planet p in s.PlanetList)
                        {
                            foreach (Guid station in p.Shipyards.Keys.ToArray())
                            {
                                if (station == ship.guid)
                                {
                                    p.Shipyards[station] = ship;
                                    ship.TetherToPlanet(p);
                                }
                            }
                            if (p.guid != orbitTargetGuid)
                                continue;
                            ship.AI.OrbitTarget = p;
                            if (ship.AI.State != AIState.Orbit)
                                continue;
                            ship.AI.OrderToOrbit(p, true);
                        }
                    }
                    if (ship.AI.State == AIState.SystemDefender)
                    {
                        Guid systemToDefendGuid = ship.AI.SystemToDefendGuid;
                        foreach (SolarSystem s in data.SolarSystemsList)
                        {
                            if (s.guid != systemToDefendGuid)
                                continue;
                            ship.AI.SystemToDefend = s;
                            ship.AI.State = AIState.SystemDefender;
                        }
                    }
                    if (ship.shipData.IsShipyard && !ship.IsTethered())
                        ship.Active = false;
                    Guid escortTargetGuid = ship.AI.EscortTargetGuid;
                    foreach (Ship s in data.MasterShipList)
                    {
                        if (s.guid == escortTargetGuid)
                            ship.AI.EscortTarget = s;
                        if (s.guid != ship.AI.TargetGuid)
                            continue;
                        ship.AI.Target = s;
                    }
                    foreach (Projectile p in ship.Projectiles)
                    {
                        p.FirstRun = false;
                    }
                }
                foreach (SolarSystem sys in data.SolarSystemsList)
                {
                    var dysSys = new Array<SysDisPair>();
                    foreach (SolarSystem toCheck in data.SolarSystemsList)
                    {
                        if (sys == toCheck)
                        {
                            continue;
                        }
                        float distance = sys.Position.Distance(toCheck.Position);
                        if (dysSys.Count >= 5)
                        {
                            int indexOfFarthestSystem = 0;
                            float farthest = 0f;
                            for (int i = 0; i < 5; i++)
                            {
                                if (dysSys[i].Distance > farthest)
                                {
                                    indexOfFarthestSystem = i;
                                    farthest = dysSys[i].Distance;
                                }
                            }
                            if (distance >= farthest)
                            {
                                continue;
                            }
                            dysSys[indexOfFarthestSystem] = new SysDisPair
                            {
                                System = toCheck,
                                Distance = distance
                            };
                        }
                        else
                        {
                            dysSys.Add(new SysDisPair
                            {
                                System = toCheck,
                                Distance = distance
                            });
                        }
                    }
                    foreach (SysDisPair sp in dysSys)
                    {
                        sys.FiveClosestSystems.Add(sp.System);
                    }
                }

                // Finally fucking fixes the 'LOOK AT ME PA I'M ZOOMED RIGHT IN' vanilla bug when loading a saved game: the universe screen uses camheight separately to the campos z vector to actually do zoom.
                us.LoadContent();

                Log.Info("LoadUniverseScreen.UpdateAllSystems(0.01)");
                us.UpdateAllSystems(0.01f);
                ResourceManager.MarkShipDesignsUnlockable();

                ready = true;
            }
        }

        private struct SysDisPair
        {
            public SolarSystem System;
            public float Distance;
        }
    }
}