using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    public sealed class SerializeAttribute : Attribute
    {
        public int Id { get; set; } = -1;
        public SerializeAttribute() { }
        public SerializeAttribute(int id) { Id = id; }
    }

    public sealed class HeaderData
    {
        public int SaveGameVersion;
        public string SaveName;
        public string StarDate;
        public DateTime Time;
        public string PlayerName;
        public string RealDate;
        public string ModName = "";
        public string ModPath = "";
        public int Version;

        [XmlIgnore][JsonIgnore]public FileInfo FI;
    }

    // XNA.Rectangle cannot be serialized, so we need a proxy object
    public struct RectangleData
    {
        public int X, Y, Width, Height;
        public RectangleData(Rectangle r)
        {
            X = r.X;
            Y = r.Y;
            Width = r.Width;
            Height = r.Height;
        }
        public static implicit operator Rectangle(RectangleData r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height);
        }
    }

    public sealed class SavedGame
    {
        // Every time the savegame layout changes significantly, this version needs to be bumped to avoid loading crashes
        public const int SaveGameVersion = 2;

        public static bool NewFormat = true; // use new save format ?
        public const string NewExt = ".sav";
        public const string OldExt = ".xml";
        public const string NewZipExt = ".sav.gz";
        public const string OldZipExt = ".xml.gz";

        private readonly UniverseSaveData SaveData = new UniverseSaveData();
        private static Thread SaveThread;

        public static bool IsSaving  => SaveThread != null && SaveThread.IsAlive;
        public static bool NotSaving => SaveThread == null || !SaveThread.IsAlive;

        public SavedGame(UniverseScreen screenToSave, string saveAs)
        {
            SaveData.SaveGameVersion     = SaveGameVersion;
            SaveData.RemnantKills        = GlobalStats.RemnantKills;
            SaveData.RemnantActivation   = GlobalStats.RemnantActivation;
            SaveData.RemnantArmageddon   = GlobalStats.RemnantArmageddon;
            SaveData.gameDifficulty      = CurrentGame.Difficulty;
            SaveData.AutoColonize        = EmpireManager.Player.AutoColonize;
            SaveData.AutoExplore         = EmpireManager.Player.AutoExplore;
            SaveData.AutoFreighters      = EmpireManager.Player.AutoFreighters;
            SaveData.AutoProjectors      = EmpireManager.Player.AutoBuild;
            SaveData.GamePacing          = CurrentGame.Pace;
            SaveData.GameScale           = screenToSave.GameScale;
            SaveData.StarDate            = screenToSave.StarDate;
            SaveData.FTLModifier         = screenToSave.FTLModifier;
            SaveData.EnemyFTLModifier    = screenToSave.EnemyFTLModifier;
            SaveData.GravityWells        = screenToSave.GravityWells;
            SaveData.PlayerLoyalty       = screenToSave.PlayerLoyalty;
            SaveData.RandomEvent         = RandomEventManager.ActiveEvent;
            SaveData.campos              = new Vector2(screenToSave.CamPos.X, screenToSave.CamPos.Y);
            SaveData.camheight           = screenToSave.CamHeight;
            SaveData.MinimumWarpRange    = GlobalStats.MinimumWarpRange;
            SaveData.TurnTimer           = (byte)GlobalStats.TurnTimer;
            SaveData.IconSize            = GlobalStats.IconSize;
            SaveData.preventFederations  = GlobalStats.PreventFederations;
            SaveData.GravityWellRange    = GlobalStats.GravityWellRange;
            SaveData.EliminationMode     = GlobalStats.EliminationMode;
            SaveData.EmpireDataList      = new Array<EmpireSaveData>();
            SaveData.SolarSystemDataList = new Array<SolarSystemSaveData>();
            SaveData.OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            

            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                var sysSave = new SolarSystemSaveData
                {
                    Name          = system.Name,
                    Position      = system.Position,
                    SunPath       = system.SunPath,
                    AsteroidsList = new Array<Asteroid>(),
                    Moons         = new Array<Moon>()
                };
                foreach (Asteroid roid in system.AsteroidsList)
                {
                    sysSave.AsteroidsList.Add(roid);
                }
                foreach (Moon moon in system.MoonList)
                    sysSave.Moons.Add(moon);
                sysSave.guid = system.guid;
                sysSave.RingList = new Array<RingSave>();
                foreach (SolarSystem.Ring ring in system.RingList)
                {
                    var rsave = new RingSave
                    {
                        Asteroids = ring.Asteroids,
                        OrbitalDistance = ring.Distance
                    };
                    if (ring.planet == null)
                    {
                        sysSave.RingList.Add(rsave);
                    }
                    else
                    {
                        var pdata = new PlanetSaveData
                        {
                            Crippled_Turns       = ring.planet.CrippledTurns,
                            guid                 = ring.planet.guid,
                            FoodState            = ring.planet.FS,
                            ProdState            = ring.planet.PS,
                            FoodLock             = ring.planet.Food.PercentLock,
                            ProdLock             = ring.planet.Prod.PercentLock,
                            ResLock              = ring.planet.Res.PercentLock,
                            Name                 = ring.planet.Name,
                            Scale                = ring.planet.Scale,
                            ShieldStrength       = ring.planet.ShieldStrengthCurrent,
                            Population           = ring.planet.Population,
                            PopulationMax        = ring.planet.MaxPopBase,
                            Fertility            = ring.planet.Fertility,
                            MaxFertility         = ring.planet.MaxFertility,
                            Richness             = ring.planet.MineralRichness,
                            Owner                = ring.planet.Owner?.data.Traits.Name ?? "",
                            WhichPlanet          = ring.planet.Type.Id,
                            OrbitalAngle         = ring.planet.OrbitalAngle,
                            OrbitalDistance      = ring.planet.OrbitalRadius,
                            HasRings             = ring.planet.HasRings,
                            Radius               = ring.planet.ObjectRadius,
                            farmerPercentage     = ring.planet.Food.Percent,
                            workerPercentage     = ring.planet.Prod.Percent,
                            researcherPercentage = ring.planet.Res.Percent,
                            foodHere             = ring.planet.FoodHere,
                            TerraformPoints      = ring.planet.TerraformPoints,
                            prodHere             = ring.planet.ProdHere,
                            ColonyType           = ring.planet.colonyType,
                            StationsList         = new Array<Guid>(),
                            SpecialDescription = ring.planet.SpecialDescription
                        };
                        foreach (var station in ring.planet.Shipyards)
                        {
                            if (station.Value.Active) pdata.StationsList.Add(station.Key);
                        }
                        pdata.QISaveList = new Array<QueueItemSave>();
                        if (ring.planet.Owner != null)
                        {
                            foreach (QueueItem item in ring.planet.ConstructionQueue)
                            {
                                QueueItemSave qi = new QueueItemSave
                                {
                                    isBuilding = item.isBuilding,
                                    IsRefit = item.isRefit
                                };
                                if (qi.IsRefit)
                                {
                                    qi.RefitCost = item.Cost;
                                }
                                if (qi.isBuilding)
                                {
                                    qi.UID = item.Building.Name;
                                }
                                qi.isShip      = item.isShip;
                                qi.DisplayName = item.DisplayName;
                                if (qi.isShip)
                                {
                                    qi.UID = item.sData.Name;
                                }
                                qi.isTroop = item.isTroop;
                                if (qi.isTroop)
                                {
                                    qi.UID = item.troopType;
                                }
                                qi.ProgressTowards = item.productionTowards;
                                if (item.Goal != null)
                                {
                                    qi.GoalGUID = item.Goal.guid;
                                }
                                if (item.pgs != null)
                                {
                                    qi.pgsVector = new Vector2(item.pgs.x, item.pgs.y);
                                }
                                qi.isPlayerAdded = item.IsPlayerAdded;
                                pdata.QISaveList.Add(qi);
                            }
                        }
                        pdata.PGSList = new Array<PGSData>();
                        foreach (PlanetGridSquare tile in ring.planet.TilesList)
                        {
                            var pgs = new PGSData
                            {
                                x          = tile.x,
                                y          = tile.y,
                                Habitable  = tile.Habitable,
                                Biosphere  = tile.Biosphere,
                                building   = tile.building,
                                TroopsHere = tile.TroopsHere
                            };
                            pdata.PGSList.Add(pgs);
                        }
                        pdata.EmpiresThatKnowThisPlanet = new Array<string>();
                        foreach (Empire exploredBy in system.ExploredByEmpires)
                        {
                            pdata.EmpiresThatKnowThisPlanet.Add(exploredBy.data.Traits.Name);
                        }
                        rsave.Planet = pdata;
                        sysSave.RingList.Add(rsave);
                    }
                    sysSave.EmpiresThatKnowThisSystem = new Array<string>();
                    foreach (Empire exploredBy in system.ExploredByEmpires)
                    {
                        // RedFox: @todo This is a duplicate?? 
                        // Crunchy: No? not a duplicate. there is planet exploration and system exploration. although one may infer the other. 
                        // RedFox: If it can infer, then we could get rid of it?
                        sysSave.EmpiresThatKnowThisSystem.Add(exploredBy.data.Traits.Name);
                    }
                }
                SaveData.SolarSystemDataList.Add(sysSave);
            }            
            foreach (Empire e in EmpireManager.Empires)
            {
                var empireToSave = new EmpireSaveData
                {
                    IsFaction = e.isFaction,
                    Relations = new Array<Relationship>()
                };
                foreach (KeyValuePair<Empire, Relationship> relation in e.AllRelations)
                {
                    empireToSave.Relations.Add(relation.Value);
                }
                empireToSave.Name                 = e.data.Traits.Name;
                empireToSave.empireData           = e.data.GetClone();
                empireToSave.Traits               = e.data.Traits;
                empireToSave.Research             = e.Research;
                empireToSave.ResearchTopic        = e.ResearchTopic;
                empireToSave.Money                = e.Money;
                empireToSave.CurrentAutoScout     = e.data.CurrentAutoScout;
                empireToSave.CurrentAutoFreighter = e.data.CurrentAutoFreighter;
                empireToSave.CurrentAutoColony    = e.data.CurrentAutoColony;
                empireToSave.CurrentConstructor   = e.data.CurrentConstructor;
                empireToSave.OwnedShips           = new Array<ShipSaveData>();
                empireToSave.TechTree             = new Array<TechEntry>();
                
                foreach (AO area in e.GetEmpireAI().AreasOfOperations)
                {
                    area.PrepareForSave();
                }
                empireToSave.AOs = e.GetEmpireAI().AreasOfOperations;
                empireToSave.FleetsList = new Array<FleetSave>();
                foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                {
                    if (fleet.Value.DataNodes == null) continue;
                    var fs = new FleetSave
                    {
                        Name        = fleet.Value.Name,
                        IsCoreFleet = fleet.Value.IsCoreFleet,
                        TaskStep    = fleet.Value.TaskStep,
                        Key         = fleet.Key,
                        facing      = fleet.Value.Facing,
                        FleetGuid   = fleet.Value.Guid,
                        Position    = fleet.Value.Position,
                        ShipsInFleet = new Array<FleetShipSave>()
                    };                    
                    foreach (FleetDataNode node in fleet.Value.DataNodes)
                    {
                        if (node.Ship== null)
                        {
                            continue;
                        }
                        node.ShipGuid = node.Ship.guid;
                    }
                    fs.DataNodes = fleet.Value.DataNodes;
                    foreach (Ship ship in fleet.Value.Ships)
                    {
                        FleetShipSave ssave = new FleetShipSave
                        {
                            fleetOffset = ship.RelativeFleetOffset,
                            shipGuid = ship.guid
                        };
                        fs.ShipsInFleet.Add(ssave);
                    }
                    empireToSave.FleetsList.Add(fs);
                }
                empireToSave.SpaceRoadData = new Array<SpaceRoadSave>();
                foreach (SpaceRoad road in e.SpaceRoadsList)
                {
                    var rdata = new SpaceRoadSave
                    {
                        OriginGUID = road.GetOrigin().guid,
                        DestGUID = road.GetDestination().guid,
                        RoadNodes = new Array<RoadNodeSave>()
                    };
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        RoadNodeSave ndata = new RoadNodeSave
                        {
                            Position = node.Position
                        };
                        if (node.Platform != null)
                        {
                            ndata.Guid_Platform = node.Platform.guid;
                        }
                        rdata.RoadNodes.Add(ndata);
                    }
                    empireToSave.SpaceRoadData.Add(rdata);
                }
                var gsaidata = new GSAISAVE
                {
                    UsedFleets = e.GetEmpireAI().UsedFleets,
                    Goals      = new Array<GoalSave>(),
                    PinGuids   = new Array<Guid>(),
                    PinList    = new Array<ThreatMatrix.Pin>()
                };
                foreach (KeyValuePair<Guid, ThreatMatrix.Pin> guid in e.GetEmpireAI().ThreatMatrix.Pins)
                {
                    gsaidata.PinGuids.Add(guid.Key);
                    gsaidata.PinList.Add(guid.Value);
                }
                gsaidata.MilitaryTaskList = new Array<MilitaryTask>();
                foreach (MilitaryTask task in e.GetEmpireAI().TaskList)
                {
                    gsaidata.MilitaryTaskList.Add(task);
                    if (task.TargetPlanet == null)
                    {
                        continue;
                    }
                    task.TargetPlanetGuid = task.TargetPlanet.guid;
                }
                foreach (Goal g in e.GetEmpireAI().Goals)
                {
                    var gdata = new GoalSave
                    {
                        BuildPosition = g.BuildPosition,
                        GoalStep      = g.Step,
                        ToBuildUID    = g.ToBuildUID,
                        type          = g.type,
                        GoalGuid      = g.guid,
                        GoalName      = g.UID
                    };
                    if (g.GetColonyShip() != null)
                    {
                        gdata.colonyShipGuid = g.GetColonyShip().guid;
                    }
                    if (g.GetMarkedPlanet() != null)
                    {
                        gdata.markedPlanetGuid = g.GetMarkedPlanet().guid;
                    }
                    if (g.GetPlanetWhereBuilding() != null)
                    {
                        gdata.planetWhereBuildingAtGuid = g.GetPlanetWhereBuilding().guid;
                    }
                    if (g.GetFleet() != null)
                    {
                        gdata.fleetGuid = g.GetFleet().Guid;
                    }
                    if (g.beingBuilt != null)
                    {
                        gdata.beingBuiltGUID = g.beingBuilt.guid;
                    }
                    gsaidata.Goals.Add(gdata);
                }
                empireToSave.GSAIData = gsaidata;
                foreach (KeyValuePair<string, TechEntry> tech in e.GetTDict())
                {
                    empireToSave.TechTree.Add(tech.Value);
                }

                foreach (Ship ship in e.GetShips())
                {
                    var sdata = new ShipSaveData
                    {
                        guid       = ship.guid,
                        data       = ship.ToShipData(),
                        Position   = ship.Position,
                        experience = ship.experience,
                        kills      = ship.kills,
                        Velocity   = ship.Velocity
                        
                    };
                    if (ship.GetTether() != null)
                    {
                        sdata.TetheredTo   = ship.GetTether().guid;
                        sdata.TetherOffset = ship.TetherOffset;
                    }
                    sdata.Name       = ship.Name;
                    sdata.VanityName = ship.VanityName;
                    if (ship.PlayerShip)
                    {
                        sdata.IsPlayerShip = true;
                    }
                    sdata.Hull             = ship.shipData.Hull;
                    sdata.Power            = ship.PowerCurrent;
                    sdata.Ordnance         = ship.Ordinance;
                    sdata.yRotation        = ship.yRotation;
                    sdata.Rotation         = ship.Rotation;
                    sdata.InCombatTimer    = ship.InCombatTimer;
                    sdata.FoodCount        = ship.GetFood();
                    sdata.ProdCount        = ship.GetProduction();
                    sdata.PopCount         = ship.GetColonists();
                    sdata.TroopList        = ship.TroopList;
                    sdata.FightersLaunched = ship.FightersLaunched;
                    sdata.TroopsLaunched   = ship.TroopsLaunched;

                    sdata.AreaOfOperation = ship.AreaOfOperation
                        .Select(r => new RectangleData(r)).ToArrayList();

                    if (ship.HomePlanet != null)
                        sdata.HomePlanetGuid = ship.HomePlanet.guid;

                    sdata.AISave = new ShipAISave
                    {
                        FoodOrProd = ship.AI.GetTradeTypeString(),
                        state      = ship.AI.State
                    };
                    if (ship.AI.Target is Ship targetShip)
                    {
                        sdata.AISave.AttackTarget = targetShip.guid;
                    }
                    sdata.AISave.defaultstate = ship.AI.DefaultAIState;
                    if (ship.AI.start != null)
                    {
                        sdata.AISave.startGuid = ship.AI.start.guid;
                    }
                    if (ship.AI.end != null)
                    {
                        sdata.AISave.endGuid = ship.AI.end.guid;
                    }
                    sdata.AISave.GoToStep = ship.AI.GotoStep;
                    sdata.AISave.MovePosition = ship.AI.MovePosition;
                    sdata.AISave.ActiveWayPoints = new Array<Vector2>();
                    foreach (Vector2 waypoint in ship.AI.WayPoints.GetWayPoints())
                    {
                        sdata.AISave.ActiveWayPoints.Add(waypoint);
                    }
                    sdata.AISave.ShipGoalsList = new Array<ShipGoalSave>();
                    foreach (ShipAI.ShipGoal sgoal in ship.AI.OrderQueue)
                    {
                        var gsave = new ShipGoalSave
                        {
                            DesiredFacing = sgoal.DesiredFacing
                        };
                        if (sgoal.fleet != null)
                        {
                            gsave.fleetGuid = sgoal.fleet.Guid;
                        }
                        gsave.FacingVector = sgoal.FacingVector;
                        if (sgoal.goal != null)
                        {
                            gsave.goalGuid = sgoal.goal.guid;
                        }
                        gsave.MovePosition = sgoal.MovePosition;
                        gsave.Plan = sgoal.Plan;
                        if (sgoal.TargetPlanet != null)
                        {
                            gsave.TargetPlanetGuid = sgoal.TargetPlanet.guid;
                        }
                        gsave.VariableString = sgoal.VariableString;
                        gsave.SpeedLimit = sgoal.SpeedLimit;
                        sdata.AISave.ShipGoalsList.Add(gsave);
                    }
                    if (ship.AI.OrbitTarget != null)
                    {
                        sdata.AISave.OrbitTarget = ship.AI.OrbitTarget.guid;
                    }
                    if (ship.AI.ColonizeTarget != null)
                    {
                        sdata.AISave.ColonizeTarget = ship.AI.ColonizeTarget.guid;
                    }
                    if (ship.AI.SystemToDefend != null)
                    {
                        sdata.AISave.SystemToDefend = ship.AI.SystemToDefend.guid;
                    }
                    if (ship.AI.EscortTarget != null)
                    {
                        sdata.AISave.EscortTarget = ship.AI.EscortTarget.guid;
                    }
                    sdata.Projectiles = new Array<ProjectileSaveData>();
                    foreach (Projectile p in ship.Projectiles)
                    {
                        var pdata = new ProjectileSaveData
                        {
                            Velocity = p.Velocity,
                            Rotation = p.Rotation,
                            Weapon   = p.Weapon.UID,
                            Position = p.Center,
                            Duration = p.Duration
                        };
                        sdata.Projectiles.Add(pdata);
                    }
                    empireToSave.OwnedShips.Add(sdata);
                }

                foreach (Ship ship in e.GetProjectors())  //fbedard
                {
                    var sdata = new ShipSaveData
                    {
                        guid       = ship.guid,
                        data       = ship.ToShipData(),
                        Position   = ship.Position,
                        experience = ship.experience,
                        kills      = ship.kills,
                        Velocity   = ship.Velocity
                    };
                    if (ship.GetTether() != null)
                    {
                        sdata.TetheredTo = ship.GetTether().guid;
                        sdata.TetherOffset = ship.TetherOffset;
                    }
                    sdata.Name = ship.Name;
                    sdata.VanityName = ship.VanityName;
                    if (ship.PlayerShip)
                    {
                        sdata.IsPlayerShip = true;
                    }
                    sdata.Hull          = ship.shipData.Hull;
                    sdata.Power         = ship.PowerCurrent;
                    sdata.Ordnance      = ship.Ordinance;
                    sdata.yRotation     = ship.yRotation;
                    sdata.Rotation      = ship.Rotation;
                    sdata.InCombatTimer = ship.InCombatTimer;
                    sdata.AISave        = new ShipAISave
                    {
                        FoodOrProd      = ship.AI.GetTradeTypeString(),
                        state           = ship.AI.State,
                        defaultstate    = ship.AI.DefaultAIState,
                        GoToStep        = ship.AI.GotoStep,
                        MovePosition    = ship.AI.MovePosition,
                        ActiveWayPoints = new Array<Vector2>(),
                        ShipGoalsList   = new Array<ShipGoalSave>()
                    };
                    sdata.Projectiles = new Array<ProjectileSaveData>();
                    empireToSave.OwnedShips.Add(sdata);
                }

                SaveData.EmpireDataList.Add(empireToSave);
            }
            SaveData.Snapshots = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
            foreach (var e in StatTracker.SnapshotsDict)
            {
                SaveData.Snapshots.Add(e.Key, e.Value);
            }
            string path = Dir.StarDriveAppData;
            SaveData.path       = path;
            SaveData.SaveAs     = saveAs;
            SaveData.Size       = new Vector2(screenToSave.UniverseSize);
            SaveData.FogMapName = saveAs + "fog";
            screenToSave.FogMap.Save($"{path}/Saved Games/Fog Maps/{saveAs}fog.png", ImageFileFormat.Png);
            SaveThread = new Thread(SaveUniverseDataAsync) {Name = "Save Thread: " + saveAs};
            SaveThread.Start(SaveData);
        }

        private static void SaveUniverseDataAsync(object universeSaveData)
        {
            var data = (UniverseSaveData)universeSaveData;
            try
            {
                string ext = NewFormat ? NewExt : OldExt;
                var info = new FileInfo($"{data.path}/Saved Games/{data.SaveAs}{ext}");
                using (FileStream writeStream = info.OpenWrite())
                {
                    PerfTimer t = PerfTimer.StartNew();
                    if (NewFormat)
                    {
                        using (var textWriter = new StreamWriter(writeStream))
                        {
                            var ser = new JsonSerializer
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                DefaultValueHandling = DefaultValueHandling.Ignore
                            };
                            ser.Serialize(textWriter, data);
                        }
                        Log.Warning($"JSON Total Save elapsed: {t.Elapsed}s");
                    }
                    else
                    {
                        var ser = new XmlSerializer(typeof(UniverseSaveData));
                        ser.Serialize(writeStream, data);
                        Log.Warning($"XML Total Save elapsed: {t.Elapsed}s");
                    }
                }
                HelperFunctions.Compress(info);
                info.Delete();
            }
            catch (Exception e)
            {
                Log.Error(e, "SaveUniverseData failed");
                return;
            }

            DateTime now = DateTime.Now;
            var header = new HeaderData
            {
                SaveGameVersion = SaveGameVersion,
                PlayerName = data.PlayerLoyalty,
                StarDate   = data.StarDate.ToString("#.0"),
                Time       = now,
                SaveName   = data.SaveAs,
                RealDate   = now.ToString("M/d/yyyy") + " " + now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat),
                ModPath    = GlobalStats.ActiveMod?.ModName    ?? "",
                ModName    = GlobalStats.ActiveMod?.mi.ModName ?? "",
                Version    = Convert.ToInt32(ConfigurationManager.AppSettings["SaveVersion"])
            };
            using (var wf = new StreamWriter(data.path + "/Saved Games/Headers/" + data.SaveAs + ".xml"))
                new XmlSerializer(typeof(HeaderData)).Serialize(wf, header);

            HelperFunctions.CollectMemory();
        }

        public static UniverseSaveData DeserializeFromCompressedSave(FileInfo compressedSave)
        {
            UniverseSaveData usData;
            var decompressed = new FileInfo(HelperFunctions.Decompress(compressedSave));

            PerfTimer t = PerfTimer.StartNew();
            if (decompressed.Extension == NewExt) // new save format
            {
                using (FileStream stream = decompressed.OpenRead())
                using (var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    var ser = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    };
                    usData = ser.Deserialize<UniverseSaveData>(reader);
                }

                Log.Warning($"JSON Total Load elapsed: {t.Elapsed}s  ");
            }
            else // old 100MB XML savegame format (haha)
            {
                long mem = GC.GetTotalMemory(false);

                XmlSerializer serializer1;
                try
                {
                    serializer1 = new XmlSerializer(typeof(UniverseSaveData));
                }
                catch
                {
                    var attrOpts = new XmlAttributeOverrides();
                    attrOpts.Add(typeof(SolarSystemSaveData), "MoonList", new XmlAttributes { XmlIgnore = true });
                    attrOpts.Add(typeof(EmpireSaveData), "MoonList", new XmlAttributes { XmlIgnore = true });
                    serializer1 = new XmlSerializer(typeof(UniverseSaveData), attrOpts);
                }

                long serSize = GC.GetTotalMemory(false) - mem;

                using (FileStream stream = decompressed.OpenRead())
                    usData = (UniverseSaveData)serializer1.Deserialize(stream);

                Log.Warning($"XML Total Load elapsed: {t.Elapsed}s  mem: {serSize / (1024f * 1024f)}MB");
            }
            decompressed.Delete();

            HelperFunctions.CollectMemory();
            return usData;
        }

        public class EmpireSaveData
        {
            [Serialize(0)] public string Name;
            [Serialize(1)] public Array<Relationship> Relations;
            [Serialize(2)] public Array<SpaceRoadSave> SpaceRoadData;
            [Serialize(3)] public bool IsFaction;
            [Serialize(4)] public bool isMinorRace; // @todo This field is deprecated
            [Serialize(5)] public RacialTrait Traits;
            [Serialize(6)] public EmpireData empireData;
            [Serialize(7)] public Array<ShipSaveData> OwnedShips;
            [Serialize(8)] public float Research;
            [Serialize(9)] public float Money;
            [Serialize(10)] public Array<TechEntry> TechTree;
            [Serialize(11)] public GSAISAVE GSAIData;
            [Serialize(12)] public string ResearchTopic;
            [Serialize(13)] public Array<AO> AOs;
            [Serialize(14)] public Array<FleetSave> FleetsList;
            [Serialize(15)] public string CurrentAutoFreighter;
            [Serialize(16)] public string CurrentAutoColony;
            [Serialize(17)] public string CurrentAutoScout;
            [Serialize(18)] public string CurrentConstructor;
        }

        public class FleetSave
        {
            [Serialize(0)] public bool IsCoreFleet;
            [Serialize(1)] public string Name;
            [Serialize(2)] public int TaskStep;
            [Serialize(3)] public Vector2 Position;
            [Serialize(4)] public Guid FleetGuid;
            [Serialize(5)] public float facing;
            [Serialize(6)] public int Key;
            [Serialize(7)] public Array<FleetShipSave> ShipsInFleet;
            [Serialize(8)] public Array<FleetDataNode> DataNodes;

            public override string ToString() => $"FleetSave {Name} (core={IsCoreFleet}) {FleetGuid} {Position}";
        }

        public struct FleetShipSave
        {
            [Serialize(0)] public Guid shipGuid;
            [Serialize(1)] public Vector2 fleetOffset;

            public override string ToString() => $"FleetShipSave {shipGuid} {fleetOffset}";
        }

        public class GoalSave
        {
            [Serialize(0)] public GoalType type;
            [Serialize(1)] public int GoalStep;
            [Serialize(2)] public Guid markedPlanetGuid;
            [Serialize(3)] public Guid colonyShipGuid;
            [Serialize(4)] public Vector2 BuildPosition;
            [Serialize(5)] public string ToBuildUID;
            [Serialize(6)] public Guid planetWhereBuildingAtGuid;
            [Serialize(7)] public string GoalName;
            [Serialize(8)] public Guid beingBuiltGUID;
            [Serialize(9)] public Guid fleetGuid;
            [Serialize(10)] public Guid GoalGuid;
        }

        public class GSAISAVE
        {
            [Serialize(0)] public Array<int> UsedFleets;
            [Serialize(1)] public Array<GoalSave> Goals;
            [Serialize(2)] public Array<MilitaryTask> MilitaryTaskList;
            [Serialize(3)] public Array<Guid> PinGuids;
            [Serialize(4)] public Array<ThreatMatrix.Pin> PinList;
        }

        public class PGSData
        {
            [Serialize(0)] public int x;
            [Serialize(1)] public int y;
            [Serialize(2)] public Array<Troop> TroopsHere;
            [Serialize(3)] public bool Biosphere;
            [Serialize(4)] public Building building;
            [Serialize(5)] public bool Habitable;
            [Serialize(6)] public int foodbonus;
            [Serialize(7)] public int resbonus;
            [Serialize(8)] public int prodbonus;
        }

        public class PlanetSaveData
        {
            [Serialize(0)] public Guid guid;
            [Serialize(1)] public string SpecialDescription;
            [Serialize(2)] public string Name;
            [Serialize(3)] public float Scale;
            [Serialize(4)] public string Owner;
            [Serialize(5)] public float Population;
            [Serialize(6)] public float PopulationMax;
            [Serialize(7)] public float Fertility;
            [Serialize(8)] public float Richness;
            [Serialize(9)] public int WhichPlanet;
            [Serialize(10)] public float OrbitalAngle;
            [Serialize(11)] public float OrbitalDistance;
            [Serialize(12)] public float Radius;
            [Serialize(13)] public bool HasRings;
            [Serialize(14)] public float farmerPercentage;
            [Serialize(15)] public float workerPercentage;
            [Serialize(16)] public float researcherPercentage;
            [Serialize(17)] public float foodHere;
            [Serialize(18)] public float prodHere;
            [Serialize(19)] public Array<PGSData> PGSList;
            [Serialize(20)] public bool GovernorOn;
            [Serialize(21)] public Array<QueueItemSave> QISaveList;
            [Serialize(22)] public Planet.ColonyType ColonyType;
            [Serialize(23)] public Planet.GoodState FoodState;
            [Serialize(24)] public int Crippled_Turns;
            [Serialize(25)] public Planet.GoodState ProdState;
            [Serialize(26)] public Array<string> EmpiresThatKnowThisPlanet;
            [Serialize(27)] public float TerraformPoints;
            [Serialize(28)] public Array<Guid> StationsList;
            [Serialize(29)] public bool FoodLock;
            [Serialize(30)] public bool ResLock;
            [Serialize(31)] public bool ProdLock;
            [Serialize(32)] public float ShieldStrength;
            [Serialize(33)] public float MaxFertility;
        }

        public struct ProjectileSaveData
        {
            [Serialize(0)] public string Weapon;
            [Serialize(1)] public float Duration;
            [Serialize(2)] public float Rotation;
            [Serialize(3)] public Vector2 Velocity;
            [Serialize(4)] public Vector2 Position;
        }

        public class QueueItemSave
        {
            [Serialize(0)] public string UID;
            [Serialize(1)] public Guid GoalGUID;
            [Serialize(2)] public float ProgressTowards;
            [Serialize(3)] public bool isBuilding;
            [Serialize(4)] public bool isTroop;
            [Serialize(5)] public bool isShip;
            [Serialize(6)] public string DisplayName;
            [Serialize(7)] public bool IsRefit;
            [Serialize(8)] public float RefitCost;
            [Serialize(9)] public Vector2 pgsVector;
            [Serialize(10)] public bool isPlayerAdded;
        }

        public struct RingSave
        {
            [Serialize(0)] public PlanetSaveData Planet;
            [Serialize(1)] public bool Asteroids;
            [Serialize(2)] public float OrbitalDistance;
        }

        public struct RoadNodeSave
        {
            [Serialize(0)] public Vector2 Position;
            [Serialize(1)] public Guid Guid_Platform;
        }

        public class ShipAISave
        {
            [Serialize(0)] public AIState state;
            [Serialize(1)] public int numFood;
            [Serialize(2)] public int numProd;
            [Serialize(3)] public string FoodOrProd;
            [Serialize(4)] public AIState defaultstate;
            [Serialize(5)] public Array<ShipGoalSave> ShipGoalsList;
            [Serialize(6)] public Array<Vector2> ActiveWayPoints;
            [Serialize(7)] public Guid startGuid;
            [Serialize(8)] public Guid endGuid;
            [Serialize(9)] public int GoToStep;
            [Serialize(10)] public Vector2 MovePosition;
            [Serialize(11)] public Guid OrbitTarget;
            [Serialize(12)] public Guid ColonizeTarget;
            [Serialize(13)] public Guid SystemToDefend;
            [Serialize(14)] public Guid AttackTarget;
            [Serialize(15)] public Guid EscortTarget;
        }

        public class ShipGoalSave
        {
            [Serialize(0)] public ShipAI.Plan Plan;
            [Serialize(1)] public Guid goalGuid;
            [Serialize(2)] public string VariableString;
            [Serialize(3)] public Guid fleetGuid;
            [Serialize(4)] public float SpeedLimit;
            [Serialize(5)] public Vector2 MovePosition;
            [Serialize(6)] public float DesiredFacing;
            [Serialize(7)] public float FacingVector;
            [Serialize(8)] public Guid TargetPlanetGuid;
        }

        public class ShipSaveData
        {
            [Serialize(0)] public Guid guid;
            [Serialize(1)] public bool AfterBurnerOn;
            [Serialize(2)] public ShipAISave AISave;
            [Serialize(3)] public Vector2 Position;
            [Serialize(4)] public Vector2 Velocity;
            [Serialize(5)] public float Rotation;
            [Serialize(6)] public ShipData data;
            [Serialize(7)] public string Hull;
            [Serialize(8)] public string Name;
            [Serialize(9)] public string VanityName;
            [Serialize(10)] public bool IsPlayerShip;
            [Serialize(11)] public float yRotation;
            [Serialize(12)] public float Power;
            [Serialize(13)] public float Ordnance;
            [Serialize(14)] public float InCombatTimer;
            [Serialize(15)] public float experience;
            [Serialize(16)] public int kills;
            [Serialize(17)] public Array<Troop> TroopList;
            [Serialize(18)] public Array<RectangleData> AreaOfOperation;
            [Serialize(19)] public float FoodCount;
            [Serialize(20)] public float ProdCount;
            [Serialize(21)] public float PopCount;
            [Serialize(22)] public Guid TetheredTo;
            [Serialize(23)] public Vector2 TetherOffset;
            [Serialize(24)] public Array<ProjectileSaveData> Projectiles;
            [Serialize(25)] public bool FightersLaunched;
            [Serialize(26)] public bool TroopsLaunched;
            [Serialize(27)] public Guid HomePlanetGuid;
        }

        public class SolarSystemSaveData
        {
            [Serialize(0)] public Guid guid;
            [Serialize(1)] public string SunPath;
            [Serialize(2)] public string Name;
            [Serialize(3)] public Vector2 Position;
            [Serialize(4)] public Array<RingSave> RingList;
            [Serialize(5)] public Array<Asteroid> AsteroidsList;
            [Serialize(6)] public Array<Moon> Moons;
            [Serialize(7)] public Array<string> EmpiresThatKnowThisSystem;            
        }

        public struct SpaceRoadSave
        {
            [Serialize(0)] public Array<RoadNodeSave> RoadNodes;
            [Serialize(1)] public Guid OriginGUID;
            [Serialize(2)] public Guid DestGUID;
        }

        public class UniverseSaveData
        {
            [Serialize(0)] public int SaveGameVersion;
            [Serialize(1)] public string path;
            [Serialize(2)] public string SaveAs;
            [Serialize(3)] public string FileName;
            [Serialize(4)] public string FogMapName;
            [Serialize(5)] public string PlayerLoyalty;
            [Serialize(6)] public Vector2 campos;
            [Serialize(7)] public float camheight;
            [Serialize(8)] public Vector2 Size;
            [Serialize(9)] public float StarDate;
            [Serialize(10)] public float GameScale;
            [Serialize(11)] public float GamePacing;
            [Serialize(12)] public Array<SolarSystemSaveData> SolarSystemDataList;
            [Serialize(13)] public Array<EmpireSaveData> EmpireDataList;
            [Serialize(14)] public UniverseData.GameDifficulty gameDifficulty;
            [Serialize(15)] public bool AutoExplore;
            [Serialize(16)] public bool AutoColonize;
            [Serialize(17)] public bool AutoFreighters;
            [Serialize(18)] public bool AutoProjectors;
            [Serialize(19)] public int RemnantKills;
            [Serialize(20)] public int RemnantActivation;
            [Serialize(21)] public bool RemnantArmageddon;
            [Serialize(22)] public float FTLModifier = 1.0f;
            [Serialize(23)] public float EnemyFTLModifier = 1.0f;
            [Serialize(24)] public bool GravityWells;
            [Serialize(25)] public RandomEvent RandomEvent;
            [Serialize(26)] public SerializableDictionary<string, SerializableDictionary<int, Snapshot>> Snapshots;
            [Serialize(27)] public float OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            [Serialize(28)] public float MinimumWarpRange = GlobalStats.MinimumWarpRange;
            [Serialize(29)] public int IconSize;
            [Serialize(30)] public byte TurnTimer;
            [Serialize(31)] public bool preventFederations;
            [Serialize(32)] public float GravityWellRange = GlobalStats.GravityWellRange;
            [Serialize(33)] public bool EliminationMode;
        }

    }
}