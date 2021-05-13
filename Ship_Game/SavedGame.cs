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
using System.Threading;
using System.Xml.Serialization;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Ships.AI;
using Ship_Game.Fleets;

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

        [XmlIgnore][JsonIgnore] public FileInfo FI;
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
        // Every time the savegame layout changes significantly,
        // this version needs to be bumped to avoid loading crashes
        public const int SaveGameVersion = 5;

        public static bool NewFormat = true; // use new save format ?
        public const string NewExt = ".sav";
        public const string OldExt = ".xml";
        public const string NewZipExt = ".sav.gz";
        public const string OldZipExt = ".xml.gz";

        readonly UniverseSaveData SaveData = new UniverseSaveData();
        static Thread SaveThread;

        public static bool IsSaving  => SaveThread != null && SaveThread.IsAlive;
        public static bool NotSaving => SaveThread == null || !SaveThread.IsAlive;

        public SavedGame(UniverseScreen screenToSave, string saveAs)
        {
            // clean up and submit objects before saving
            screenToSave.Objects.Update(FixedSimTime.Zero);

            SaveData.SaveGameVersion       = SaveGameVersion;
            SaveData.gameDifficulty        = CurrentGame.Difficulty;
            SaveData.GalaxySize            = CurrentGame.GalaxySize;
            SaveData.StarsModifier         = CurrentGame.StarsModifier;
            SaveData.ExtraPlanets          = CurrentGame.ExtraPlanets;
            SaveData.AutoColonize          = EmpireManager.Player.AutoColonize;
            SaveData.AutoExplore           = EmpireManager.Player.AutoExplore;
            SaveData.AutoFreighters        = EmpireManager.Player.AutoFreighters;
            SaveData.AutoPickBestFreighter = EmpireManager.Player.AutoPickBestFreighter;
            SaveData.AutoPickBestColonizer = EmpireManager.Player.AutoPickBestColonizer;
            SaveData.AutoProjectors        = EmpireManager.Player.AutoBuild;
            SaveData.GamePacing            = CurrentGame.Pace;
            SaveData.GameScale             = 1f;
            SaveData.StarDate              = screenToSave.StarDate;
            SaveData.FTLModifier           = screenToSave.FTLModifier;
            SaveData.EnemyFTLModifier      = screenToSave.EnemyFTLModifier;
            SaveData.GravityWells          = screenToSave.GravityWells;
            SaveData.PlayerLoyalty         = screenToSave.PlayerLoyalty;
            SaveData.RandomEvent           = RandomEventManager.ActiveEvent;
            SaveData.campos                = new Vector2(screenToSave.CamPos.X, screenToSave.CamPos.Y);
            SaveData.camheight             = screenToSave.CamHeight;
            SaveData.MinimumWarpRange      = GlobalStats.MinimumWarpRange;
            SaveData.TurnTimer             = (byte)GlobalStats.TurnTimer;
            SaveData.IconSize              = GlobalStats.IconSize;
            SaveData.preventFederations    = GlobalStats.PreventFederations;
            SaveData.GravityWellRange      = GlobalStats.GravityWellRange;
            SaveData.EliminationMode       = GlobalStats.EliminationMode;
            SaveData.EmpireDataList        = new Array<EmpireSaveData>();
            SaveData.SolarSystemDataList   = new Array<SolarSystemSaveData>();
            SaveData.CustomMineralDecay    = GlobalStats.CustomMineralDecay;
            SaveData.VolcanicActivity      = GlobalStats.VolcanicActivity;

            SaveData.SuppressOnBuildNotifications  = GlobalStats.SuppressOnBuildNotifications;
            SaveData.PlanetScreenHideOwned         = GlobalStats.PlanetScreenHideOwned;;
            SaveData.PlanetsScreenHideUnhabitable  = GlobalStats.PlanetsScreenHideUnhabitable;
            SaveData.OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            SaveData.ShipListFilterPlayerShipsOnly = GlobalStats.ShipListFilterPlayerShipsOnly;
            SaveData.ShipListFilterInFleetsOnly    = GlobalStats.ShipListFilterInFleetsOnly;
            SaveData.ShipListFilterNotInFleets     = GlobalStats.ShipListFilterNotInFleets;
            SaveData.DisableInhibitionWarning      = GlobalStats.DisableInhibitionWarning;
            SaveData.CordrazinePlanetCaptured      = GlobalStats.CordrazinePlanetCaptured;
            SaveData.DisableVolcanoWarning         = GlobalStats.DisableVolcanoWarning;
            
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                SaveData.SolarSystemDataList.Add(new SolarSystemSaveData
                {
                    Name           = system.Name,
                    guid           = system.guid,
                    Position       = system.Position,
                    SunPath        = system.Sun.Id,
                    AsteroidsList  = system.AsteroidsList.Clone(),
                    Moons          = system.MoonList.Clone(),
                    ExploredBy     = system.ExploredByEmpires.Select(e => e.data.Traits.Name),
                    RingList       = system.RingList.Select(ring => ring.Serialize()),
                    PiratePresence = system.PiratePresence
                });
            }

            foreach (Empire e in EmpireManager.Empires)
            {
                var empireToSave = new EmpireSaveData
                {
                    IsFaction = e.isFaction,
                    Relations = new Array<Relationship>()
                };
                foreach (OurRelationsToThem relation in e.AllRelations)
                {
                    empireToSave.Relations.Add(relation.Rel);
                }
                empireToSave.Name                 = e.data.Traits.Name;
                empireToSave.empireData           = e.data.GetClone();
                empireToSave.Traits               = e.data.Traits;
                empireToSave.ResearchTopic        = e.Research.Topic;
                empireToSave.Money                = e.Money;
                empireToSave.CurrentAutoScout     = e.data.CurrentAutoScout;
                empireToSave.CurrentAutoFreighter = e.data.CurrentAutoFreighter;
                empireToSave.CurrentAutoColony    = e.data.CurrentAutoColony;
                empireToSave.CurrentConstructor   = e.data.CurrentConstructor;
                empireToSave.OwnedShips           = new Array<ShipSaveData>();
                empireToSave.TechTree             = new Array<TechEntry>();
                e.SaveMoneyHistory(empireToSave);
                empireToSave.FastVsBigFreighterRatio   = e.FastVsBigFreighterRatio;
                empireToSave.AverageFreighterCargoCap  = e.AverageFreighterCargoCap;
                empireToSave.AverageFreighterFTLSpeed  = e.AverageFreighterFTLSpeed;
                empireToSave.ExpandSearchTimer         = e.GetEmpireAI().ExpansionAI.ExpandSearchTimer;
                empireToSave.MaxSystemsToCheckedDiv    = e.GetEmpireAI().ExpansionAI.MaxSystemsToCheckedDiv;
                empireToSave.EmpireDefense             = e.GetEmpireAI().EmpireDefense;
                empireToSave.WeightedCenter            = e.WeightedCenter;
                empireToSave.RushAllConstruction       = e.RushAllConstruction;
                empireToSave.FleetStrEmpireModifier    = e.FleetStrEmpireMultiplier;
                empireToSave.DiplomacyContactQueue     = e.DiplomacyContactQueue;
                empireToSave.ObsoletePlayerShipModules = e.ObsoletePlayerShipModules;

                if (e.WeArePirates)
                {
                    empireToSave.PirateLevel         = e.Pirates.Level;
                    empireToSave.PirateThreatLevels  = e.Pirates.ThreatLevels;
                    empireToSave.PiratePaymentTimers = e.Pirates.PaymentTimers;
                    empireToSave.SpawnedShips        = e.Pirates.SpawnedShips;
                    empireToSave.ShipsWeCanSpawn     = e.Pirates.ShipsWeCanSpawn;
                }

                if (e.WeAreRemnants)
                {
                    empireToSave.RemnantStoryActivated      = e.Remnants.Activated;
                    empireToSave.RemnantStoryTriggerKillsXp = e.Remnants.StoryTriggerKillsXp;
                    empireToSave.RemnantStoryType           = (int)e.Remnants.Story;
                    empireToSave.RemnantProduction          = e.Remnants.Production;
                    empireToSave.RemnantLevel               = e.Remnants.Level;
                    empireToSave.RemnantStoryStep           = e.Remnants.StoryStep;
                    empireToSave.RemnantPlayerStepTriggerXp = e.Remnants.PlayerStepTriggerXp;
                    empireToSave.OnlyRemnantLeft            = e.Remnants.OnlyRemnantLeft;
                    empireToSave.RemnantNextLevelUpDate     = e.Remnants.NextLevelUpDate;
                    empireToSave.RemnantHibernationTurns    = e.Remnants.HibernationTurns;
                    empireToSave.RemnantActivationXpNeeded  = e.Remnants.ActivationXpNeeded;
                }

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
                        Name            = fleet.Value.Name,
                        IsCoreFleet     = fleet.Value.IsCoreFleet,
                        TaskStep        = fleet.Value.TaskStep,
                        Key             = fleet.Key,
                        facing          = fleet.Value.FinalDirection.ToRadians(), // @note Save game compatibility uses radians
                        FleetGuid       = fleet.Value.Guid,
                        Position        = fleet.Value.FinalPosition,
                        ShipsInFleet    = new Array<FleetShipSave>(),
                        AutoRequisition = fleet.Value.AutoRequisition
                    };                    
                    foreach (FleetDataNode node in fleet.Value.DataNodes)
                    {
                        // only save ships that are currently alive (race condition when saving during intense battles)
                        if (node.Ship != null && node.Ship.Active)
                            node.ShipGuid = node.Ship.guid;
                    }
                    fs.DataNodes = fleet.Value.DataNodes;
                    foreach (Ship ship in fleet.Value.Ships)
                    {
                        fs.ShipsInFleet.Add(new FleetShipSave
                        {
                            fleetOffset = ship.RelativeFleetOffset,
                            shipGuid = ship.guid
                        });
                    }
                    empireToSave.FleetsList.Add(fs);
                }
                empireToSave.SpaceRoadData = new Array<SpaceRoadSave>();
                foreach (SpaceRoad road in e.SpaceRoadsList)
                {
                    var rdata = new SpaceRoadSave
                    {
                        OriginGUID = road.Origin.guid,
                        DestGUID = road.Destination.guid,
                        RoadNodes = new Array<RoadNodeSave>()
                    };
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        var ndata = new RoadNodeSave { Position = node.Position };
                        if (node.Platform != null) ndata.Guid_Platform = node.Platform.guid;
                        rdata.RoadNodes.Add(ndata);
                    }
                    empireToSave.SpaceRoadData.Add(rdata);
                }
                var gsaidata = new GSAISAVE
                {
                    UsedFleets = e.GetEmpireAI().UsedFleets
                };

                e.GetEmpireAI().ThreatMatrix.WriteToSave(gsaidata);
                e.GetEmpireAI().WriteToSave(gsaidata);

                Array<Goal> goals = e.GetEmpireAI().Goals;
                gsaidata.Goals = goals.Select(g =>
                {
                    var gdata = new GoalSave
                    {
                        BuildPosition = g.BuildPosition,
                        GoalStep      = g.Step,
                        ToBuildUID    = g.ToBuildUID,
                        type          = g.type,
                        GoalGuid      = g.guid,
                        GoalName      = g.UID,
                        ShipLevel     = g.ShipLevel,
                        VanityName    = g.VanityName,
                        TetherTarget  = g.TetherTarget,
                        TetherOffset  = g.TetherOffset,
                    };
                    if (g.FinishedShip != null)       gdata.colonyShipGuid            = g.FinishedShip.guid;
                    if (g.ColonizationTarget != null) gdata.markedPlanetGuid          = g.ColonizationTarget.guid;
                    if (g.PlanetBuildingAt != null)   gdata.planetWhereBuildingAtGuid = g.PlanetBuildingAt.guid;
                    if (g.Fleet != null)              gdata.fleetGuid                 = g.Fleet.Guid;
                    if (g.ShipToBuild != null)        gdata.beingBuiltGUID            = g.ShipToBuild.guid;
                    if (g.OldShip != null)            gdata.OldShipGuid               = g.OldShip.guid;
                    if (g.TargetShip != null)         gdata.TargetShipGuid            = g.TargetShip.guid;
                    if (g.TargetEmpire != null)       gdata.TargetEmpireId            = g.TargetEmpire.Id;

                    return gdata;
                });
                empireToSave.GSAIData = gsaidata;

                empireToSave.TechTree.AddRange(e.TechEntries.ToArray());

                foreach (Ship ship in e.OwnedShips)
                    empireToSave.OwnedShips.Add(ShipSaveFromShip(ship));

                foreach (Ship ship in e.GetProjectors())  //fbedard
                    empireToSave.OwnedShips.Add(ProjectorSaveFromShip(ship));

                SaveData.EmpireDataList.Add(empireToSave);
            }

            SaveData.Projectiles = screenToSave.Objects.GetProjectileSaveData();
            SaveData.Beams       = screenToSave.Objects.GetBeamSaveData();

            SaveData.Snapshots = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
            foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> e in StatTracker.SnapshotsMap)
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

        public static ShipSaveData ShipSaveFromShip(Ship ship)
        {
            var sdata = new ShipSaveData
            {
                guid = ship.guid,
                data = ship.ToShipData(),
                Position = ship.Position,
                experience = ship.experience,
                kills = ship.kills,
                Velocity = ship.Velocity

            };
            if (ship.GetTether() != null)
            {
                sdata.TetheredTo = ship.GetTether().guid;
                sdata.TetherOffset = ship.TetherOffset;
            }
            sdata.Name = ship.Name;
            sdata.VanityName = ship.VanityName;
            sdata.Hull = ship.shipData.Hull;
            sdata.Power = ship.PowerCurrent;
            sdata.Ordnance = ship.Ordinance;
            sdata.yRotation = ship.yRotation;
            sdata.Rotation = ship.Rotation;
            sdata.InCombatTimer = ship.InCombatTimer;
            sdata.FoodCount = ship.GetFood();
            sdata.ProdCount = ship.GetProduction();
            sdata.PopCount = ship.GetColonists();
            sdata.TroopList = ship.GetFriendlyAndHostileTroops();
            sdata.FightersLaunched = ship.Carrier.FightersLaunched;
            sdata.TroopsLaunched = ship.Carrier.TroopsLaunched;
            sdata.SendTroopsToShip = ship.Carrier.SendTroopsToShip;
            sdata.AreaOfOperation = ship.AreaOfOperation.Select(r => new RectangleData(r));

            sdata.RecallFightersBeforeFTL = ship.Carrier.RecallFightersBeforeFTL;
            sdata.MechanicalBoardingDefense = ship.MechanicalBoardingDefense;

            if (ship.IsHomeDefense)
                sdata.HomePlanetGuid = ship.HomePlanet.guid;

            if (ship.TradeRoutes?.NotEmpty == true)
            {
                sdata.TradeRoutes = new Array<Guid>();
                foreach (Guid planetGuid in ship.TradeRoutes)
                {
                    sdata.TradeRoutes.Add(planetGuid);
                }
            }

            sdata.TransportingFood = ship.TransportingFood;
            sdata.TransportingProduction = ship.TransportingProduction;
            sdata.TransportingColonists = ship.TransportingColonists;
            sdata.AllowInterEmpireTrade = ship.AllowInterEmpireTrade;
            sdata.AISave = new ShipAISave
            {
                State = ship.AI.State
            };
            if (ship.AI.Target is Ship targetShip)
            {
                sdata.AISave.AttackTarget = targetShip.guid;
            }
            sdata.AISave.DefaultState = ship.AI.DefaultAIState;
            sdata.AISave.MovePosition = ship.AI.MovePosition;
            sdata.AISave.WayPoints = new Array<WayPoint>(ship.AI.CopyWayPoints());
            sdata.AISave.ShipGoalsList = new Array<ShipGoalSave>();
            sdata.AISave.PriorityOrder = ship.AI.HasPriorityOrder;
            sdata.AISave.PriorityTarget = ship.AI.HasPriorityTarget;

            foreach (ShipAI.ShipGoal sg in ship.AI.OrderQueue)
            {
                var s = new ShipGoalSave
                {
                    Plan = sg.Plan,
                    Direction = sg.Direction,
                    VariableString = sg.VariableString,
                    SpeedLimit = sg.SpeedLimit,
                    MovePosition = sg.MovePosition,
                    fleetGuid = sg.Fleet?.Guid ?? Guid.Empty,
                    goalGuid = sg.Goal?.guid ?? Guid.Empty,
                    TargetPlanetGuid = sg.TargetPlanet?.guid ?? Guid.Empty,
                    TargetShipGuid = sg.TargetShip?.guid ?? Guid.Empty,
                    MoveType = sg.MoveType,
                    VariableNumber = sg.VariableNumber
                };

                if (sg.Trade != null)
                {
                    s.Trade = new TradePlanSave
                    {
                        Goods = sg.Trade.Goods,
                        ExportFrom = sg.Trade.ExportFrom?.guid ?? Guid.Empty,
                        ImportTo = sg.Trade.ImportTo?.guid ?? Guid.Empty,
                        BlockadeTimer = sg.Trade.BlockadeTimer,
                    };
                }
                sdata.AISave.ShipGoalsList.Add(s);
            }

            if (ship.AI.OrbitTarget != null)
                sdata.AISave.OrbitTarget = ship.AI.OrbitTarget.guid;

            if (ship.AI.ColonizeTarget != null)
                sdata.AISave.ColonizeTarget = ship.AI.ColonizeTarget.guid;

            if (ship.AI.SystemToDefend != null)
                sdata.AISave.SystemToDefend = ship.AI.SystemToDefend.guid;

            if (ship.AI.EscortTarget != null)
                sdata.AISave.EscortTarget = ship.AI.EscortTarget.guid;
            return sdata;
        }

        public static ShipSaveData ProjectorSaveFromShip(Ship ship)
        {
            var sd = new ShipSaveData
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
                sd.TetheredTo = ship.GetTether().guid;
                sd.TetherOffset = ship.TetherOffset;
            }
            sd.Name = ship.Name;
            sd.VanityName = ship.VanityName;
            sd.Hull          = ship.shipData.Hull;
            sd.Power         = ship.PowerCurrent;
            sd.Ordnance      = ship.Ordinance;
            sd.yRotation     = ship.yRotation;
            sd.Rotation      = ship.Rotation;
            sd.InCombatTimer = ship.InCombatTimer;
            sd.AISave = new ShipAISave
            {
                State           = ship.AI.State,
                DefaultState    = ship.AI.DefaultAIState,
                MovePosition    = ship.AI.MovePosition,
                WayPoints       = new Array<WayPoint>(),
                ShipGoalsList   = new Array<ShipGoalSave>()
            };
            return sd;
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
                    PerfTimer t = new PerfTimer();
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

            PerfTimer t = new PerfTimer();
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

            //HelperFunctions.CollectMemory();
            return usData;
        }

        public class EmpireSaveData
        {
            [Serialize(0)] public string Name;
            [Serialize(1)] public Array<Relationship> Relations;
            [Serialize(2)] public Array<SpaceRoadSave> SpaceRoadData;
            [Serialize(3)] public bool IsFaction;
            [Serialize(5)] public RacialTrait Traits;
            [Serialize(6)] public EmpireData empireData;
            [Serialize(7)] public Array<ShipSaveData> OwnedShips;
            [Serialize(8)] public float Money;
            [Serialize(9)] public Array<TechEntry> TechTree;
            [Serialize(10)] public GSAISAVE GSAIData;
            [Serialize(11)] public string ResearchTopic;
            [Serialize(12)] public Array<AO> AOs;
            [Serialize(13)] public Array<FleetSave> FleetsList;
            [Serialize(14)] public string CurrentAutoFreighter;
            [Serialize(15)] public string CurrentAutoColony;
            [Serialize(16)] public string CurrentAutoScout;
            [Serialize(17)] public string CurrentConstructor;
            [Serialize(18)] public float FastVsBigFreighterRatio;
            [Serialize(19)] public int AverageFreighterCargoCap;
            [Serialize(20)] public int PirateLevel;
            [Serialize(21)] public Map<int, int> PirateThreatLevels;
            [Serialize(22)] public Map<int, int> PiratePaymentTimers;
            [Serialize(23)] public Array<Guid> SpawnedShips;
            [Serialize(24)] public Array<string> ShipsWeCanSpawn;
            [Serialize(25)] public Array<float> NormalizedMoney;
            [Serialize(26)] public int ExpandSearchTimer;
            [Serialize(27)] public int MaxSystemsToCheckedDiv;
            [Serialize(28)] public AI.StrategyAI.WarGoals.War EmpireDefense;
            [Serialize(29)] public int AverageFreighterFTLSpeed;
            [Serialize(30)] public Vector2 WeightedCenter;
            [Serialize(31)] public bool RushAllConstruction;
            [Serialize(32)] public float RemnantStoryTriggerKillsXp;
            [Serialize(33)] public bool RemnantStoryActivated;
            [Serialize(34)] public int RemnantStoryType;
            [Serialize(35)] public float RemnantProduction;
            [Serialize(36)] public int RemnantLevel;
            [Serialize(37)] public int RemnantStoryStep;
            [Serialize(38)] public float RemnantPlayerStepTriggerXp;
            [Serialize(39)] public bool OnlyRemnantLeft;
            [Serialize(40)] public float RemnantNextLevelUpDate;
            [Serialize(41)] public int RemnantHibernationTurns;
            [Serialize(42)] public float RemnantActivationXpNeeded;
            [Serialize(43)] public Map<int, float> FleetStrEmpireModifier;
            [Serialize(44)] public List<KeyValuePair<int, string>> DiplomacyContactQueue;
            [Serialize(45)] public Array<string> ObsoletePlayerShipModules;
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
            [Serialize(9)] public bool AutoRequisition;

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
            [Serialize(2)] public Guid markedPlanetGuid; // @note renamed to: Goal.ColonizationTarget
            [Serialize(3)] public Guid colonyShipGuid;   // @note renamed to: Goal.FinishedShip
            [Serialize(4)] public Vector2 BuildPosition;
            [Serialize(5)] public string ToBuildUID;
            [Serialize(6)] public Guid planetWhereBuildingAtGuid;
            [Serialize(7)] public string GoalName;
            [Serialize(8)] public Guid beingBuiltGUID;
            [Serialize(9)] public Guid fleetGuid;
            [Serialize(10)] public Guid GoalGuid;
            [Serialize(11)] public Guid OldShipGuid;
            [Serialize(12)] public string VanityName;
            [Serialize(13)] public int ShipLevel;
            [Serialize(14)] public Guid TetherTarget;
            [Serialize(15)] public Vector2 TetherOffset;
            [Serialize(16)] public Guid TargetShipGuid;
            [Serialize(17)] public int TargetEmpireId;
            [Serialize(18)] public float StarDateAdded;
        }

        public class GSAISAVE
        {
            [Serialize(0)] public Array<int> UsedFleets;
            [Serialize(1)] public GoalSave[] Goals;
            [Serialize(2)] public Array<MilitaryTask> MilitaryTaskList;
            [Serialize(3)] public Array<Guid> PinGuids;
            [Serialize(4)] public Array<ThreatMatrix.Pin> PinList;
            [Serialize(5)] public WarTasks WarTaskClass;
        }

        public class PGSData
        {
            [Serialize(0)] public int x;
            [Serialize(1)] public int y;
            [Serialize(2)] public Array<Troop> TroopsHere;
            [Serialize(3)] public bool Biosphere;
            [Serialize(4)] public Building building;
            [Serialize(5)] public bool Habitable;
            [Serialize(6)] public bool Terraformable;
            [Serialize(7)] public bool CrashSiteActive;
            [Serialize(8)] public int CrashSiteTroops;
            [Serialize(9)] public string CrashSiteShipName;
            [Serialize(10)] public string CrashSiteTroopName;
            [Serialize(11)] public int CrashSiteEmpireId;
            [Serialize(12)] public bool CrashSiteRecoverShip;
            [Serialize(13)] public short EventOutcomeNum;
            [Serialize(14)] public bool VolcanoHere;
            [Serialize(15)] public bool VolcanoActive;
            [Serialize(16)] public bool VolcanoErupting;
            [Serialize(17)] public float VolcanoActivationChance;
        }

        public class PlanetSaveData
        {
            [Serialize(0)] public Guid guid;
            [Serialize(1)] public string SpecialDescription;
            [Serialize(2)] public string Name;
            [Serialize(3)] public float Scale;
            [Serialize(4)] public string Owner;
            [Serialize(5)] public float Population;
            [Serialize(6)] public float BasePopPerTile;
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
            [Serialize(19)] public PGSData[] PGSList;
            [Serialize(20)] public QueueItemSave[] QISaveList;
            [Serialize(21)] public Planet.ColonyType ColonyType;
            [Serialize(22)] public Planet.GoodState FoodState;
            [Serialize(23)] public int Crippled_Turns;
            [Serialize(24)] public Planet.GoodState ProdState;
            [Serialize(25)] public string[] ExploredBy;
            [Serialize(26)] public float TerraformPoints;
            [Serialize(27)] public Guid[] StationsList;
            [Serialize(28)] public bool FoodLock;
            [Serialize(29)] public bool ResLock;
            [Serialize(30)] public bool ProdLock;
            [Serialize(31)] public float ShieldStrength;
            [Serialize(32)] public float MaxFertility;
            [Serialize(33)] public Guid[] IncomingFreighters;
            [Serialize(34)] public Guid[] OutgoingFreighters;
            [Serialize(35)] public bool GovOrbitals;
            [Serialize(36)] public bool GovMilitia;
            [Serialize(37)] public int NumShipyards;
            [Serialize(38)] public bool DontScrapBuildings;
            [Serialize(39)] public int GarrisonSize;
            [Serialize(40)] public float BaseFertilityTerraformRatio;
            [Serialize(41)] public bool Quarantine;
            [Serialize(42)] public bool ManualOrbitals;
            [Serialize(43)] public byte WantedPlatforms;
            [Serialize(44)] public byte WantedStations;
            [Serialize(45)] public byte WantedShipyards;
            [Serialize(46)] public bool GovGroundDefense;
            [Serialize(47)] public float ManualCivilianBudget;
            [Serialize(48)] public float ManualGrdDefBudget;
            [Serialize(49)] public float ManualSpcDefBudget;
            [Serialize(50)] public bool HasLimitedResourcesBuildings;
        }

        public struct ProjectileSaveData
        {
            [Serialize(0)] public Guid Owner; // Ship or Planet
            [Serialize(1)] public string Weapon;
            [Serialize(2)] public float Duration;
            [Serialize(3)] public float Rotation;
            [Serialize(4)] public Vector2 Velocity;
            [Serialize(5)] public Vector2 Position;
            [Serialize(6)] public int Loyalty;
        }

        public struct BeamSaveData
        {
            [Serialize(0)] public Guid Owner; // Ship or Planet
            [Serialize(1)] public string Weapon;
            [Serialize(2)] public float Duration;
            [Serialize(3)] public Vector2 Source;
            [Serialize(4)] public Vector2 Destination;
            [Serialize(5)] public Vector2 ActualHitDestination;
            [Serialize(6)] public Guid Target; // Ship or Projectile
            [Serialize(7)] public int Loyalty;
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
            [Serialize(7)] public float Cost;
            [Serialize(8)] public Vector2 pgsVector;
            [Serialize(9)] public bool isPlayerAdded;
            [Serialize(10)] public Array<Guid> TradeRoutes;
            [Serialize(11)] public RectangleData[] AreaOfOperation;
            [Serialize(12)] public bool TransportingColonists;
            [Serialize(13)] public bool TransportingFood;
            [Serialize(14)] public bool TransportingProduction;
            [Serialize(15)] public bool AllowInterEmpireTrade;
            [Serialize(16)] public bool IsMilitary;
            [Serialize(17)] public bool Rush;
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
            [Serialize(0)] public AIState State;
            [Serialize(1)] public AIState DefaultState;
            [Serialize(2)] public Array<ShipGoalSave> ShipGoalsList;
            // NOTE: Old Vector2 waypoints are no longer compatible
            // Renaming essentially clears all waypoints
            [Serialize(3)] public Array<WayPoint> WayPoints;
            [Serialize(4)] public Vector2 MovePosition;
            [Serialize(5)] public Guid OrbitTarget;
            [Serialize(6)] public Guid ColonizeTarget;
            [Serialize(7)] public Guid SystemToDefend;
            [Serialize(8)] public Guid AttackTarget;
            [Serialize(9)] public Guid EscortTarget;
            [Serialize(10)] public bool PriorityOrder;
            [Serialize(11)] public bool PriorityTarget;
        }

        public class ShipGoalSave
        {
            [Serialize(0)] public ShipAI.Plan Plan;
            [Serialize(1)] public Guid goalGuid;
            [Serialize(2)] public string VariableString;
            [Serialize(3)] public Guid fleetGuid;
            [Serialize(4)] public float SpeedLimit;
            [Serialize(5)] public Vector2 MovePosition;
            [Serialize(6)] public Vector2 Direction;
            [Serialize(7)] public Guid TargetPlanetGuid;
            [Serialize(8)] public TradePlanSave Trade;
            [Serialize(9)] public AIState WantedState;
            [Serialize(10)] public Guid TargetShipGuid;
            [Serialize(11)] public ShipAI.MoveTypes MoveType;
            [Serialize(12)] public float VariableNumber;
        }

        public class TradePlanSave
        {
            [Serialize(0)] public Guid ExportFrom;
            [Serialize(1)] public Guid ImportTo;
            [Serialize(2)] public Goods Goods;
            [Serialize(3)] public float BlockadeTimer;
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
            [Serialize(10)] public float yRotation;
            [Serialize(11)] public float Power;
            [Serialize(12)] public float Ordnance;
            [Serialize(13)] public float InCombatTimer;
            [Serialize(14)] public float experience;
            [Serialize(15)] public int kills;
            [Serialize(16)] public Array<Troop> TroopList;
            [Serialize(17)] public RectangleData[] AreaOfOperation;
            [Serialize(18)] public float FoodCount;
            [Serialize(19)] public float ProdCount;
            [Serialize(20)] public float PopCount;
            [Serialize(21)] public Guid TetheredTo;
            [Serialize(22)] public Vector2 TetherOffset;
            [Serialize(23)] public bool FightersLaunched;
            [Serialize(24)] public bool TroopsLaunched;
            [Serialize(25)] public Guid HomePlanetGuid;
            [Serialize(26)] public bool TransportingFood;
            [Serialize(27)] public bool TransportingProduction;
            [Serialize(28)] public bool TransportingColonists;
            [Serialize(29)] public bool AllowInterEmpireTrade;
            [Serialize(30)] public Array<Guid> TradeRoutes;
            [Serialize(31)] public bool SendTroopsToShip;
            [Serialize(32)] public bool RecallFightersBeforeFTL;
            [Serialize(33)] public float MechanicalBoardingDefense;
        }

        public class SolarSystemSaveData
        {
            [Serialize(0)] public Guid guid;
            [Serialize(1)] public string SunPath; // old SunPath is actually the ID @todo RENAME
            [Serialize(2)] public string Name;
            [Serialize(3)] public Vector2 Position;
            [Serialize(4)] public RingSave[] RingList;
            [Serialize(5)] public Array<Asteroid> AsteroidsList;
            [Serialize(6)] public Array<Moon> Moons;
            [Serialize(7)] public string[] ExploredBy;
            [Serialize(8)] public bool PiratePresence;
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
            [Serialize(34)] public bool AutoPickBestFreighter;
            [Serialize(35)] public GalSize GalaxySize;
            [Serialize(36)] public float StarsModifier = 1;
            [Serialize(37)] public int ExtraPlanets;
            [Serialize(38)] public ProjectileSaveData[] Projectiles; // New global projectile list
            [Serialize(39)] public BeamSaveData[] Beams; // new global beam list
            [Serialize(40)] public bool AutoPickBestColonizer;
            [Serialize(41)] public float CustomMineralDecay;
            [Serialize(42)] public bool SuppressOnBuildNotifications;
            [Serialize(43)] public bool PlanetScreenHideOwned;
            [Serialize(44)] public bool PlanetsScreenHideUnhabitable;
            [Serialize(45)] public bool ShipListFilterPlayerShipsOnly;
            [Serialize(46)] public bool ShipListFilterInFleetsOnly;
            [Serialize(47)] public bool ShipListFilterNotInFleets;
            [Serialize(48)] public bool DisableInhibitionWarning;
            [Serialize(49)] public bool CordrazinePlanetCaptured;
            [Serialize(50)] public bool DisableVolcanoWarning;
            [Serialize(51)] public float VolcanicActivity;
        }
    }
}
