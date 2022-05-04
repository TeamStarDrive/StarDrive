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
using Ship_Game.Data.Serialization;
using Ship_Game.Ships.AI;
using Ship_Game.Fleets;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    public sealed class SerializeAttribute : Attribute
    {
        public int Id { get; set; } = -1;
        public SerializeAttribute() { }
        public SerializeAttribute(int id) { Id = id; }
    }

    [StarDataType]
    public sealed class HeaderData
    {
        [StarData] public int SaveGameVersion;
        [StarData] public string SaveName;
        [StarData] public string StarDate;
        [StarData] public DateTime Time;
        [StarData] public string PlayerName;
        [StarData] public string RealDate;
        [StarData] public string ModName = "";
        [StarData] public string ModPath = "";
        [StarData] public int Version;

        [XmlIgnore][JsonIgnore] public FileInfo FI;
    }

    // XNA.Rectangle cannot be serialized, so we need a proxy object
    [StarDataType]
    public struct RectangleData
    {
        [StarData] public int X, Y, Width, Height;
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
        public const int SaveGameVersion = 12;
        public const string ZipExt = ".sav.gz";

        public readonly UniverseSaveData SaveData = new UniverseSaveData();
        public FileInfo SaveFile;
        public FileInfo PackedFile;
        public FileInfo HeaderFile;
        
        public static bool IsSaving  => GetIsSaving();
        public static bool NotSaving => !IsSaving;
        public static string DefaultSaveGameFolder => Dir.StarDriveAppData + "/Saved Games/";

        static TaskResult SaveTask;
        readonly UniverseScreen Screen;

        public SavedGame(UniverseScreen screenToSave)
        {
            Screen = screenToSave;

            // clean up and submit objects before saving
            UniverseState us = screenToSave.UState;
            us.Objects.UpdateLists(removeInactiveObjects: true);


            SaveData.SaveGameVersion       = SaveGameVersion;
            SaveData.UniqueObjectIds             = us.UniqueObjectIds;
            SaveData.GameDifficulty        = us.Difficulty;
            SaveData.GalaxySize            = us.GalaxySize;
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
            SaveData.BackgroundSeed        = us.BackgroundSeed;
            SaveData.StarDate              = us.StarDate;
            SaveData.FTLModifier           = us.FTLModifier;
            SaveData.EnemyFTLModifier      = us.EnemyFTLModifier;
            SaveData.FTLInNeutralSystems   = us.FTLInNeutralSystems;
            SaveData.GravityWells          = us.GravityWells;
            SaveData.PlayerLoyalty         = screenToSave.PlayerLoyalty;
            SaveData.RandomEvent           = RandomEventManager.ActiveEvent;
            SaveData.CamPos                = screenToSave.CamPos.ToVec3f();
            SaveData.MinAcceptableShipWarpRange      = GlobalStats.MinAcceptableShipWarpRange;
            SaveData.TurnTimer             = (byte)GlobalStats.TurnTimer;
            SaveData.IconSize              = GlobalStats.IconSize;
            SaveData.PreventFederations    = GlobalStats.PreventFederations;
            SaveData.GravityWellRange      = GlobalStats.GravityWellRange;
            SaveData.EliminationMode       = GlobalStats.EliminationMode;
            SaveData.EmpireDataList        = new Array<EmpireSaveData>();
            SaveData.SolarSystemDataList   = new Array<SolarSystemSaveData>();
            SaveData.CustomMineralDecay    = GlobalStats.CustomMineralDecay;
            SaveData.VolcanicActivity      = GlobalStats.VolcanicActivity;
            SaveData.UsePlayerDesigns      = GlobalStats.UsePlayerDesigns;
            SaveData.UseUpkeepByHullSize   = GlobalStats.UseUpkeepByHullSize;

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
            
            foreach (SolarSystem system in screenToSave.UState.Systems)
            {
                SaveData.SolarSystemDataList.Add(new SolarSystemSaveData
                {
                    Name           = system.Name,
                    Id             = system.Id,
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
                empireToSave.EmpireData           = e.data.GetClone();
                empireToSave.Traits               = e.data.Traits;
                empireToSave.ResearchTopic        = e.Research.Topic;
                empireToSave.Money                = e.Money;
                empireToSave.CurrentAutoScout     = e.data.CurrentAutoScout;
                empireToSave.CurrentAutoFreighter = e.data.CurrentAutoFreighter;
                empireToSave.CurrentAutoColony    = e.data.CurrentAutoColony;
                empireToSave.CurrentConstructor   = e.data.CurrentConstructor;
                empireToSave.OwnedShips           = new Array<ShipSaveData>();
                empireToSave.TechTree             = new Array<TechEntry>();
                empireToSave.NormalizedMoneyVal      = e.NormalizedMoney;
                empireToSave.FastVsBigFreighterRatio   = e.FastVsBigFreighterRatio;
                empireToSave.AverageFreighterCargoCap  = e.AverageFreighterCargoCap;
                empireToSave.AverageFreighterFTLSpeed  = e.AverageFreighterFTLSpeed;
                empireToSave.ExpandSearchTimer         = e.GetEmpireAI().ExpansionAI.ExpandSearchTimer;
                empireToSave.MaxSystemsToCheckedDiv    = e.GetEmpireAI().ExpansionAI.MaxSystemsToCheckedDiv;
                empireToSave.WeightedCenter            = e.WeightedCenter;
                empireToSave.RushAllConstruction       = e.RushAllConstruction;
                empireToSave.FleetStrEmpireModifier    = e.FleetStrEmpireMultiplier;
                empireToSave.DiplomacyContactQueue     = e.DiplomacyContactQueue;
                empireToSave.ObsoletePlayerShipModules = e.ObsoletePlayerShipModules;
                empireToSave.CapitalId               = e.Capital?.Id ?? 0;

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
                        Facing          = fleet.Value.FinalDirection.ToRadians(), // @note Save game compatibility uses radians
                        FleetId         = fleet.Value.Id,
                        Position        = fleet.Value.FinalPosition,
                        ShipsInFleet    = new Array<FleetShipSave>(),
                        AutoRequisition = fleet.Value.AutoRequisition
                    };                    
                    foreach (FleetDataNode node in fleet.Value.DataNodes)
                    {
                        // only save ships that are currently alive (race condition when saving during intense battles)
                        if (node.Ship != null && node.Ship.Active)
                            node.ShipId = node.Ship.Id;
                    }
                    fs.DataNodes = fleet.Value.DataNodes;
                    foreach (Ship ship in fleet.Value.Ships)
                    {
                        fs.ShipsInFleet.Add(new FleetShipSave
                        {
                            FleetOffset = ship.RelativeFleetOffset,
                            ShipId = ship.Id
                        });
                    }
                    empireToSave.FleetsList.Add(fs);
                }
                empireToSave.SpaceRoadData = new Array<SpaceRoadSave>();
                foreach (SpaceRoad road in e.SpaceRoadsList)
                {
                    var rdata = new SpaceRoadSave
                    {
                        OriginId = road.Origin.Id,
                        DestinationId = road.Destination.Id,
                        RoadNodes = new Array<RoadNodeSave>()
                    };
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        var ndata = new RoadNodeSave { Position = node.Position };
                        if (node.Platform != null) ndata.PlatformId = node.Platform.Id;
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
                        Type          = g.type,
                        GoalId        = g.Id,
                        GoalName      = g.UID,
                        ShipLevel     = g.ShipLevel,
                        VanityName    = g.VanityName,
                        TetherTarget  = g.TetherPlanetId,
                        TetherOffset  = g.TetherOffset,
                        StarDateAdded = g.StarDateAdded
                    };
                    if (g.FinishedShip != null)       gdata.ColonyShipId       = g.FinishedShip.Id;
                    if (g.ColonizationTarget != null) gdata.MarkedPlanetId     = g.ColonizationTarget.Id;
                    if (g.PlanetBuildingAt != null)   gdata.PlanetBuildingAtId = g.PlanetBuildingAt.Id;
                    if (g.TargetSystem != null)       gdata.TargetSystemId     = g.TargetSystem.Id;
                    if (g.TargetPlanet != null)       gdata.TargetPlanetId     = g.TargetPlanet.Id;
                    if (g.Fleet != null)              gdata.FleetId            = g.Fleet.Id;
                    if (g.OldShip != null)            gdata.OldShipId          = g.OldShip.Id;
                    if (g.TargetShip != null)         gdata.TargetShipId       = g.TargetShip.Id;
                    if (g.TargetEmpire != null)       gdata.TargetEmpireId     = g.TargetEmpire.Id;

                    return gdata;
                });
                
                empireToSave.GSAIData = gsaidata;
                empireToSave.TechTree.AddRange(e.TechEntries.ToArray());

                var sw = new ShipDesignWriter();

                var ships = e.OwnedShips;
                foreach (Ship ship in ships)
                    empireToSave.OwnedShips.Add(ShipSaveFromShip(sw, ship));

                var projectors = e.GetProjectors();
                foreach (Ship ship in projectors)  //fbedard
                    empireToSave.OwnedShips.Add(ProjectorSaveFromShip(sw, ship));

                SaveData.EmpireDataList.Add(empireToSave);
            }

            SaveData.Projectiles = screenToSave.UState.Objects.GetProjectileSaveData();
            SaveData.Beams       = screenToSave.UState.Objects.GetBeamSaveData();

            SaveData.Snapshots = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
            foreach (KeyValuePair<string, SerializableDictionary<int, Snapshot>> e in StatTracker.SnapshotsMap)
            {
                SaveData.Snapshots.Add(e.Key, e.Value);
            }
        }

        static bool GetIsSaving()
        {
            if (SaveTask == null)
                return false;
            if (!SaveTask.IsComplete)
                return true;

            SaveTask = null; // avoids some nasty memory leak issues
            return false;
        }

        public void Save(string saveAs, bool async)
        {
            SaveData.UniverseSize = Screen.UState.Size;
            SaveData.Path = Dir.StarDriveAppData;
            SaveData.SaveAs = saveAs;

            string destFolder = DefaultSaveGameFolder;
            SaveFile = new FileInfo($"{destFolder}{saveAs}.sav");
            PackedFile = new FileInfo(SaveFile.FullName + ".gz");
            HeaderFile = new FileInfo($"{destFolder}Headers/{saveAs}.json");

            // FogMap is converted to a Base64 string so that it can be included in the savegame
            var exporter = Screen.ContentManager.RawContent.TexExport;
            SaveData.FogMapBytes = exporter.ToAlphaBytes(Screen.FogMap);

            // All of this data can be serialized in parallel,
            // because we already built `SaveData` object, which no longer depends on UniverseScreen
            SaveTask = Parallel.Run(() =>
            {
                SaveUniverseData(SaveData, SaveFile, PackedFile, HeaderFile);
            });
            
            // for blocking calls, just wait on the task
            if (!async)
                SaveTask.Wait();
        }

        public static ShipSaveData ShipSaveFromShip(ShipDesignWriter sw, Ship ship)
        {
            var sdata = new ShipSaveData(sw, ship);
            if (ship.GetTether() != null)
            {
                sdata.TetheredTo = ship.GetTether().Id;
                sdata.TetherOffset = ship.TetherOffset;
            }
            sdata.Name = ship.Name;
            sdata.VanityName = ship.VanityName;
            sdata.Hull = ship.ShipData.Hull;
            sdata.Power = ship.PowerCurrent;
            sdata.Ordnance = ship.Ordinance;
            sdata.YRotation = ship.YRotation;
            sdata.Rotation = ship.Rotation;
            sdata.InCombat = ship.InCombat;
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
            sdata.OrdnanceInSpace = ship.Carrier.OrdnanceInSpace;
            sdata.ScuttleTimer = ship.ScuttleTimer;

            if (ship.IsHomeDefense)
                sdata.HomePlanetId = ship.HomePlanet.Id;

            if (ship.TradeRoutes?.NotEmpty == true)
            {
                sdata.TradeRoutes = new Array<int>();
                foreach (int planetId in ship.TradeRoutes)
                {
                    sdata.TradeRoutes.Add(planetId);
                }
            }

            sdata.TransportingFood = ship.TransportingFood;
            sdata.TransportingProduction = ship.TransportingProduction;
            sdata.TransportingColonists = ship.TransportingColonists;
            sdata.AllowInterEmpireTrade = ship.AllowInterEmpireTrade;
            sdata.AISave = new ShipAISave
            {
                State = ship.AI.State,
                DefaultState = ship.AI.DefaultAIState,
                CombatState = ship.AI.CombatState,
                StateBits = ship.AI.StateBits,
            };
            if (ship.AI.Target is Ship targetShip)
            {
                sdata.AISave.AttackTargetId = targetShip.Id;
            }
            sdata.AISave.MovePosition = ship.AI.MovePosition;
            sdata.AISave.WayPoints = new Array<WayPoint>(ship.AI.CopyWayPoints());
            sdata.AISave.ShipGoalsList = new Array<ShipGoalSave>();

            foreach (ShipAI.ShipGoal sg in ship.AI.OrderQueue)
                sdata.AISave.ShipGoalsList.Add(sg.ToSaveData());

            if (ship.AI.OrbitTarget != null)
                sdata.AISave.OrbitTargetId = ship.AI.OrbitTarget.Id;

            if (ship.AI.SystemToDefend != null)
                sdata.AISave.SystemToDefendId = ship.AI.SystemToDefend.Id;

            if (ship.AI.EscortTarget != null)
                sdata.AISave.EscortTargetId = ship.AI.EscortTarget.Id;
            return sdata;
        }

        public static ShipSaveData ProjectorSaveFromShip(ShipDesignWriter sw, Ship ship)
        {
            var sd = new ShipSaveData(sw, ship);
            if (ship.GetTether() != null)
            {
                sd.TetheredTo = ship.GetTether().Id;
                sd.TetherOffset = ship.TetherOffset;
            }
            sd.Name = ship.Name;
            sd.VanityName = ship.VanityName;
            sd.Hull      = ship.ShipData.Hull;
            sd.Power     = ship.PowerCurrent;
            sd.Ordnance  = ship.Ordinance;
            sd.YRotation = ship.YRotation;
            sd.Rotation  = ship.Rotation;
            sd.InCombat  = ship.InCombat;
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

        static void SaveUniverseData(UniverseSaveData data, FileInfo saveFile, 
                                     FileInfo compressedSave, FileInfo headerFile)
        {
            var t = new PerfTimer();
            using (var textWriter = new StreamWriter(saveFile.FullName))
            {
                var ser = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                ser.Serialize(textWriter, data);
            }
            Log.Info($"JSON Total Save elapsed: {t.Elapsed:0.00}s ({saveFile.Length/(1024.0*1024.0):0.0}MB)");

            HelperFunctions.Compress(saveFile, compressedSave); // compress into .sav.gz
            saveFile.Delete(); // delete the bigger .sav file

            // Save the header as well
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

            using (var textWriter = new StreamWriter(headerFile.FullName))
            {
                var ser = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                ser.Serialize(textWriter, header);
            }

            SaveTask = null;

            HelperFunctions.CollectMemory();
        }

        public static UniverseSaveData DeserializeFromCompressedSave(FileInfo compressedSave)
        {
            UniverseSaveData usData;
            var decompressed = new FileInfo(HelperFunctions.Decompress(compressedSave));

            var t = new PerfTimer();
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

            Log.Info($"JSON Total Load elapsed: {t.Elapsed:0.0}s  ");
            decompressed.Delete();

            //HelperFunctions.CollectMemory();
            return usData;
        }

        [StarDataType]
        public class EmpireSaveData
        {
            [StarData] public string Name;
            [StarData] public Array<Relationship> Relations;
            [StarData] public Array<SpaceRoadSave> SpaceRoadData;
            [StarData] public bool IsFaction;
            [StarData] public RacialTrait Traits;
            [StarData] public EmpireData EmpireData;
            [StarData] public Array<ShipSaveData> OwnedShips;
            [StarData] public float Money;
            [StarData] public Array<TechEntry> TechTree;
            [StarData] public GSAISAVE GSAIData;
            [StarData] public string ResearchTopic;
            [StarData] public Array<AO> AOs;
            [StarData] public Array<FleetSave> FleetsList;
            [StarData] public string CurrentAutoFreighter;
            [StarData] public string CurrentAutoColony;
            [StarData] public string CurrentAutoScout;
            [StarData] public string CurrentConstructor;
            [StarData] public float FastVsBigFreighterRatio;
            [StarData] public float AverageFreighterCargoCap;
            [StarData] public int PirateLevel;
            [StarData] public Map<int, int> PirateThreatLevels;
            [StarData] public Map<int, int> PiratePaymentTimers;
            [StarData] public Array<int> SpawnedShips;
            [StarData] public Array<string> ShipsWeCanSpawn;
            [StarData] public float NormalizedMoneyVal;
            [StarData] public int ExpandSearchTimer;
            [StarData] public int MaxSystemsToCheckedDiv;
            [StarData] public int AverageFreighterFTLSpeed;
            [StarData] public Vector2 WeightedCenter;
            [StarData] public bool RushAllConstruction;
            [StarData] public float RemnantStoryTriggerKillsXp;
            [StarData] public bool RemnantStoryActivated;
            [StarData] public int RemnantStoryType;
            [StarData] public float RemnantProduction;
            [StarData] public int RemnantLevel;
            [StarData] public int RemnantStoryStep;
            [StarData] public float RemnantPlayerStepTriggerXp;
            [StarData] public bool OnlyRemnantLeft;
            [StarData] public float RemnantNextLevelUpDate;
            [StarData] public int RemnantHibernationTurns;
            [StarData] public float RemnantActivationXpNeeded;
            [StarData] public Map<int, float> FleetStrEmpireModifier;
            [StarData] public List<KeyValuePair<int, string>> DiplomacyContactQueue;
            [StarData] public Array<string> ObsoletePlayerShipModules;
            [StarData] public int CapitalId;
        }

        [StarDataType]
        public class FleetSave
        {
            [StarData] public bool IsCoreFleet;
            [StarData] public string Name;
            [StarData] public int TaskStep;
            [StarData] public Vector2 Position;
            [StarData] public int FleetId;
            [StarData] public float Facing;
            [StarData] public int Key;
            [StarData] public Array<FleetShipSave> ShipsInFleet;
            [StarData] public Array<FleetDataNode> DataNodes;
            [StarData] public bool AutoRequisition;

            public override string ToString() => $"FleetSave {Name} (core={IsCoreFleet}) {FleetId} {Position}";
        }

        [StarDataType]
        public struct FleetShipSave
        {
            [StarData] public int ShipId;
            [StarData] public Vector2 FleetOffset;

            public override string ToString() => $"FleetShipSave {ShipId} {FleetOffset}";
        }

        [StarDataType]
        public class GoalSave
        {
            [StarData] public GoalType Type;
            [StarData] public int GoalStep;
            [StarData] public int MarkedPlanetId; // @note renamed to: Goal.ColonizationTarget
            [StarData] public int ColonyShipId;   // @note renamed to: Goal.FinishedShip
            [StarData] public Vector2 BuildPosition;
            [StarData] public string ToBuildUID;
            [StarData] public int PlanetBuildingAtId;
            [StarData] public string GoalName;
            [StarData] public int FleetId;
            [StarData] public int GoalId;
            [StarData] public int OldShipId;
            [StarData] public string VanityName;
            [StarData] public int ShipLevel;
            [StarData] public int TetherTarget;
            [StarData] public Vector2 TetherOffset;
            [StarData] public int TargetShipId;
            [StarData] public int TargetEmpireId;
            [StarData] public float StarDateAdded;
            [StarData] public int TargetSystemId;
            [StarData] public int TargetPlanetId;
        }

        [StarDataType]
        public class GSAISAVE
        {
            [StarData] public Array<int> UsedFleets;
            [StarData] public GoalSave[] Goals;
            [StarData] public Array<MilitaryTask> MilitaryTaskList;
            [StarData] public Array<int> PinIds;
            [StarData] public Array<ThreatMatrix.Pin> PinList;
        }

        [StarDataType]
        public class PGSData
        {
            [StarData] public int X;
            [StarData] public int Y;
            [StarData] public Array<Troop> TroopsHere;
            [StarData] public bool Biosphere;
            [StarData] public Building building;
            [StarData] public bool Habitable;
            [StarData] public bool Terraformable;
            [StarData] public bool CrashSiteActive;
            [StarData] public int CrashSiteTroops;
            [StarData] public string CrashSiteShipName;
            [StarData] public string CrashSiteTroopName;
            [StarData] public int CrashSiteEmpireId;
            [StarData] public bool CrashSiteRecoverShip;
            [StarData] public short EventOutcomeNum;
            [StarData] public bool VolcanoHere;
            [StarData] public bool VolcanoActive;
            [StarData] public bool VolcanoErupting;
            [StarData] public float VolcanoActivationChance;
        }

        [StarDataType]
        public class PlanetSaveData
        {
            [StarData] public int Id;
            [StarData] public string SpecialDescription;
            [StarData] public string Name;
            [StarData] public float Scale;
            [StarData] public string Owner;
            [StarData] public float Population;
            [StarData] public float BasePopPerTile;
            [StarData] public float Fertility;
            [StarData] public float Richness;
            [StarData] public int WhichPlanet;
            [StarData] public float OrbitalAngle;
            [StarData] public float OrbitalDistance;
            [StarData] public float Radius;
            [StarData] public bool HasRings;
            [StarData] public float FarmerPercentage;
            [StarData] public float WorkerPercentage;
            [StarData] public float ResearcherPercentage;
            [StarData] public float FoodHere;
            [StarData] public float ProdHere;
            [StarData] public PGSData[] PGSList;
            [StarData] public QueueItemSave[] QISaveList;
            [StarData] public Planet.ColonyType ColonyType;
            [StarData] public Planet.GoodState FoodState;
            [StarData] public int TurnsCrippled;
            [StarData] public Planet.GoodState ProdState;
            [StarData] public string[] ExploredBy;
            [StarData] public float TerraformPoints;
            [StarData] public int[] StationsList;
            [StarData] public bool FoodLock;
            [StarData] public bool ResLock;
            [StarData] public bool ProdLock;
            [StarData] public float ShieldStrength;
            [StarData] public float MaxFertility;
            [StarData] public int[] IncomingFreighters;
            [StarData] public int[] OutgoingFreighters;
            [StarData] public bool GovOrbitals;
            [StarData] public bool GovMilitia;
            [StarData] public int NumShipyards;
            [StarData] public bool DontScrapBuildings;
            [StarData] public int GarrisonSize;
            [StarData] public float BaseFertilityTerraformRatio;
            [StarData] public bool Quarantine;
            [StarData] public bool ManualOrbitals;
            [StarData] public byte WantedPlatforms;
            [StarData] public byte WantedStations;
            [StarData] public byte WantedShipyards;
            [StarData] public bool GovGroundDefense;
            [StarData] public float ManualCivilianBudget;
            [StarData] public float ManualGrdDefBudget;
            [StarData] public float ManualSpcDefBudget;
            [StarData] public bool HasLimitedResourcesBuildings;
            [StarData] public int ManualFoodImportSlots;
            [StarData] public int ManualProdImportSlots;
            [StarData] public int ManualColoImportSlots;
            [StarData] public int ManualFoodExportSlots;
            [StarData] public int ManualProdExportSlots;
            [StarData] public int ManualColoExportSlots;
            [StarData] public float AverageFoodImportTurns;
            [StarData] public float AverageProdImportTurns;
            [StarData] public float AverageFoodExportTurns;
            [StarData] public float AverageProdExportTurns;
            [StarData] public bool IsHomeworld;
            [StarData] public int BombingIntensity;

            public override string ToString() => $"PlanetSD {Name}";
        }

        [StarDataType]
        public struct ProjectileSaveData
        {
            [StarData] public int Id; // unique ID of the object
            [StarData] public int OwnerId; // Ship or Planet
            [StarData] public string Weapon;
            [StarData] public float Duration;
            [StarData] public float Rotation;
            [StarData] public Vector2 Velocity;
            [StarData] public Vector2 Position;
            [StarData] public int Loyalty;
        }

        [StarDataType]
        public struct BeamSaveData
        {
            [StarData] public int Id; // unique ID of the object
            [StarData] public int OwnerId; // Ship or Planet
            [StarData] public string Weapon;
            [StarData] public float Duration;
            [StarData] public Vector2 Source;
            [StarData] public Vector2 Destination;
            [StarData] public Vector2 ActualHitDestination;
            [StarData] public int TargetId; // Ship or Projectile
            [StarData] public int Loyalty;
        }

        [StarDataType]
        public class QueueItemSave
        {
            [StarData] public string UID;
            [StarData] public int GoalId;
            [StarData] public float ProgressTowards;
            [StarData] public bool IsBuilding;
            [StarData] public bool IsTroop;
            [StarData] public bool IsShip;
            [StarData] public string DisplayName;
            [StarData] public float Cost;
            [StarData] public Vector2 PGSVector;
            [StarData] public bool IsPlayerAdded;
            [StarData] public Array<int> TradeRoutes;
            [StarData] public RectangleData[] AreaOfOperation;
            [StarData] public bool TransportingColonists;
            [StarData] public bool TransportingFood;
            [StarData] public bool TransportingProduction;
            [StarData] public bool AllowInterEmpireTrade;
            [StarData] public bool IsMilitary;
            [StarData] public bool Rush;
        }

        [StarDataType]
        public struct RingSave
        {
            [StarData] public PlanetSaveData Planet;
            [StarData] public bool Asteroids;
            [StarData] public float OrbitalDistance;

            public override string ToString() => $"RingSave {OrbitalDistance} Ast:{Asteroids} {Planet}";
        }

        [StarDataType]
        public struct RoadNodeSave
        {
            [StarData] public Vector2 Position;
            [StarData] public int PlatformId;
        }

        [StarDataType]
        public class ShipAISave
        {
            [StarData] public AIState State;
            [StarData] public AIState DefaultState;
            [StarData] public CombatState CombatState;
            [StarData] public ShipAI.Flags StateBits;
            [StarData] public Array<ShipGoalSave> ShipGoalsList;
            [StarData] public Array<WayPoint> WayPoints;
            [StarData] public Vector2 MovePosition;
            [StarData] public int OrbitTargetId;
            [StarData] public int SystemToDefendId;
            [StarData] public int AttackTargetId;
            [StarData] public int EscortTargetId;
        }

        [StarDataType]
        public class ShipGoalSave
        {
            [StarData] public ShipAI.Plan Plan;
            [StarData] public int GoalId;
            [StarData] public string VariableString;
            [StarData] public int FleetId;
            [StarData] public float SpeedLimit;
            [StarData] public Vector2 MovePosition;
            [StarData] public Vector2 Direction;
            [StarData] public int TargetPlanetId;
            [StarData] public TradePlanSave Trade;
            [StarData] public AIState WantedState;
            [StarData] public int TargetShipId;
            [StarData] public MoveOrder MoveOrder;
            [StarData] public float VariableNumber;

            public override string ToString()
            {
                return $"SGSave {Plan} MP={MovePosition} TS={TargetShipId} TP={TargetPlanetId}";
            }
        }

        [StarDataType]
        public class TradePlanSave
        {
            [StarData] public int ExportFrom;
            [StarData] public int ImportTo;
            [StarData] public Goods Goods;
            [StarData] public float BlockadeTimer;
            [StarData] public float StardateAdded;
        }

        [StarDataType]
        public class ShipSaveData
        {
            [StarData] public int Id;
            [StarData] public bool AfterBurnerOn;
            [StarData] public ShipAISave AISave;
            [StarData] public Vector2 Position;
            [StarData] public Vector2 Velocity;
            [StarData] public float Rotation;
            // 200 IQ solution: store text representation of the ship module saves
            // and avoid a bunch of annoying serialization issues
            [StarData] public byte[] ModuleSaveData;
            [StarData] public string Hull; // ShipHull name
            [StarData] public string Name; // ShipData design name
            [StarData] public string VanityName; // User defined name
            [StarData] public float YRotation;
            [StarData] public float Power;
            [StarData] public float Ordnance;
            [StarData] public bool InCombat;
            [StarData] public float BaseStrength;
            [StarData] public int Level;
            [StarData] public float Experience;
            [StarData] public int Kills;
            [StarData] public Array<Troop> TroopList;
            [StarData] public RectangleData[] AreaOfOperation;
            [StarData] public float FoodCount;
            [StarData] public float ProdCount;
            [StarData] public float PopCount;
            [StarData] public int TetheredTo;
            [StarData] public Vector2 TetherOffset;
            [StarData] public bool FightersLaunched;
            [StarData] public bool TroopsLaunched;
            [StarData] public int HomePlanetId;
            [StarData] public bool TransportingFood;
            [StarData] public bool TransportingProduction;
            [StarData] public bool TransportingColonists;
            [StarData] public bool AllowInterEmpireTrade;
            [StarData] public Array<int> TradeRoutes;
            [StarData] public bool SendTroopsToShip;
            [StarData] public bool RecallFightersBeforeFTL;
            [StarData] public float MechanicalBoardingDefense;
            [StarData] public float OrdnanceInSpace; // For carriers
            [StarData] public float ScuttleTimer = -1;

            public ShipSaveData() {}

            public ShipSaveData(ShipDesignWriter sw, Ship ship)
            {
                Name = ship.Name;
                MechanicalBoardingDefense = ship.MechanicalBoardingDefense;
                Id = ship.Id;
                Position   = ship.Position;

                BaseStrength = ship.BaseStrength;
                Level      = ship.Level;
                Experience = ship.Experience;
                Kills      = ship.Kills;
                Velocity   = ship.Velocity;

                ModuleSaveData = ShipDesign.GetModulesBytes(sw, ship);
            }

            public override string ToString() => $"ShipSave {Id} {Name}";
        }

        [StarDataType]
        public class SolarSystemSaveData
        {
            [StarData] public int Id;
            [StarData] public string SunPath; // old SunPath is actually the ID @todo RENAME
            [StarData] public string Name;
            [StarData] public Vector2 Position;
            [StarData] public RingSave[] RingList;
            [StarData] public Array<Asteroid> AsteroidsList;
            [StarData] public Array<Moon> Moons;
            [StarData] public string[] ExploredBy;
            [StarData] public bool PiratePresence;

            public override string ToString()
            {
                return $"{SunPath} {Name} {Position} "+
                       $"Rings:{RingList.Length} Ast:{AsteroidsList.Count} "+
                       $"Moons:{Moons.Count} Pirates:{PiratePresence} ExploredBy:{string.Join(",",ExploredBy)}";
            }
        }

        [StarDataType]
        public struct SpaceRoadSave
        {
            [StarData] public Array<RoadNodeSave> RoadNodes;
            [StarData] public int OriginId;
            [StarData] public int DestinationId;
        }

        [StarDataType]
        public class UniverseSaveData : IDisposable
        {
            [StarData] public int SaveGameVersion;
            [StarData] public int UniqueObjectIds;
            [StarData] public string Path;
            [StarData] public string SaveAs;
            [StarData] public string FileName;
            [StarData] public byte[] FogMapBytes;
            [StarData] public string PlayerLoyalty;
            [StarData] public Vector3 CamPos;
            [StarData] public float UniverseSize;
            [StarData] public float StarDate;
            [StarData] public float GameScale;
            [StarData] public float GamePacing;
            [StarData] public int BackgroundSeed;
            [StarData] public Array<SolarSystemSaveData> SolarSystemDataList;
            [StarData] public Array<EmpireSaveData> EmpireDataList;
            [StarData] public GameDifficulty GameDifficulty;
            [StarData] public bool AutoExplore;
            [StarData] public bool AutoColonize;
            [StarData] public bool AutoFreighters;
            [StarData] public bool AutoProjectors;
            [StarData] public float FTLModifier = 1.0f;
            [StarData] public float EnemyFTLModifier = 1.0f;
            [StarData] public bool FTLInNeutralSystems;
            [StarData] public bool GravityWells;
            [StarData] public RandomEvent RandomEvent;
            [StarData] public SerializableDictionary<string, SerializableDictionary<int, Snapshot>> Snapshots;
            [StarData] public float OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            [StarData] public float MinAcceptableShipWarpRange = GlobalStats.MinAcceptableShipWarpRange;
            [StarData] public int IconSize;
            [StarData] public byte TurnTimer;
            [StarData] public bool PreventFederations;
            [StarData] public float GravityWellRange = GlobalStats.GravityWellRange;
            [StarData] public bool EliminationMode;
            [StarData] public bool AutoPickBestFreighter;
            [StarData] public GalSize GalaxySize;
            [StarData] public float StarsModifier = 1;
            [StarData] public int ExtraPlanets;
            [StarData] public ProjectileSaveData[] Projectiles; // New global projectile list
            [StarData] public BeamSaveData[] Beams; // new global beam list
            [StarData] public bool AutoPickBestColonizer;
            [StarData] public float CustomMineralDecay;
            [StarData] public bool SuppressOnBuildNotifications;
            [StarData] public bool PlanetScreenHideOwned;
            [StarData] public bool PlanetsScreenHideUnhabitable;
            [StarData] public bool ShipListFilterPlayerShipsOnly;
            [StarData] public bool ShipListFilterInFleetsOnly;
            [StarData] public bool ShipListFilterNotInFleets;
            [StarData] public bool DisableInhibitionWarning;
            [StarData] public bool CordrazinePlanetCaptured;
            [StarData] public bool DisableVolcanoWarning;
            [StarData] public float VolcanicActivity;
            [StarData] public bool UsePlayerDesigns;
            [StarData] public bool UseUpkeepByHullSize;

            public void Dispose()
            {
                SolarSystemDataList.Clear();
                EmpireDataList.Clear();
                Snapshots.Clear();
                FogMapBytes = null;
                Projectiles = null;
                Beams = null;
            }
        }
    }
}
