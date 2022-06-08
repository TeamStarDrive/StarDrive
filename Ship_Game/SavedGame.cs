using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships.AI;
using Ship_Game.Fleets;
using Ship_Game.Universe;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Binary;

namespace Ship_Game
{
    [StarDataType]
    public sealed class HeaderData
    {
        [StarData] public int Version;
        [StarData] public string SaveName;
        [StarData] public string StarDate;
        [StarData] public string PlayerName;
        [StarData] public string RealDate;
        [StarData] public string ModName = "";
        [StarData] public DateTime Time;
    }

    // XNA.Rectangle cannot be serialized, so we need a proxy object
    // TODO: New binary serializer does support Rectangle and RectF
    [StarDataType]
    public struct RectangleData
    {
        [StarData] public int X, Y, Width, Height;
        public RectangleData(in Rectangle r)
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
        public const int SaveGameVersion = 14;

        public bool Verbose;

        public readonly UniverseSaveData SaveData = new();
        public FileInfo SaveFile;

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
            SaveData.UniverseSize          = us.Size;
            SaveData.UniqueObjectIds       = us.UniqueObjectIds;
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
            SaveData.MinAcceptableShipWarpRange = GlobalStats.MinAcceptableShipWarpRange;
            SaveData.TurnTimer             = (byte)GlobalStats.TurnTimer;
            SaveData.IconSize              = GlobalStats.IconSize;
            SaveData.PreventFederations    = GlobalStats.PreventFederations;
            SaveData.GravityWellRange      = GlobalStats.GravityWellRange;
            SaveData.EliminationMode       = GlobalStats.EliminationMode;
            SaveData.EmpireDataList        = us.Empires.ToArr();
            SaveData.SolarSystems          = screenToSave.UState.Systems.ToArr();
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

            var allShips = us.Objects.GetAllShipsSlow();
            SaveData.AllShips = allShips.Select(ShipSaveFromShip);
            SaveData.Projectiles = us.Objects.GetProjectileSaveData();
            SaveData.Beams       = us.Objects.GetBeamSaveData();

            SaveData.Snapshots = new Map<string, Map<int, Snapshot>>();
            foreach (KeyValuePair<string, Map<int, Snapshot>> e in StatTracker.SnapshotsMap)
            {
                SaveData.Snapshots.Add(e.Key, e.Value);
            }
            
            var designs = new HashSet<IShipDesign>();
            foreach (Ship ship in allShips)
                designs.Add(ship.ShipData);
            SaveData.SetDesigns(designs);

            // FogMap is converted to a Base64 string so that it can be included in the savegame
            SaveData.FogMapBytes = Screen.ContentManager.RawContent.TexExport.ToAlphaBytes(Screen.FogMap);
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
            SaveData.SaveAs = saveAs; // filename of the save game

            string destFolder = DefaultSaveGameFolder;
            SaveFile = new FileInfo($"{destFolder}{saveAs}.sav");

            if (!async)
            {
                SaveUniverseData(SaveData, SaveFile);
            }
            else
            {
                // All of this data can be serialized in parallel,
                // because we already built `SaveData` object, which no longer depends on UniverseScreen
                SaveTask = Parallel.Run(() =>
                {
                    SaveUniverseData(SaveData, SaveFile);
                });
            }
        }

        public static ShipSaveData ShipSaveFromShip(Ship ship)
        {
            var sd = new ShipSaveData(ship);
            if (ship.IsSubspaceProjector)
                return sd;

            sd.FoodCount = ship.GetFood();
            sd.ProdCount = ship.GetProduction();
            sd.PopCount = ship.GetColonists();
            sd.TroopList = ship.GetFriendlyAndHostileTroops();
            sd.FightersLaunched = ship.Carrier.FightersLaunched;
            sd.TroopsLaunched = ship.Carrier.TroopsLaunched;
            sd.SendTroopsToShip = ship.Carrier.SendTroopsToShip;
            sd.AreaOfOperation = ship.AreaOfOperation.Select(r => new RectangleData(r));

            sd.RecallFightersBeforeFTL = ship.Carrier.RecallFightersBeforeFTL;
            sd.MechanicalBoardingDefense = ship.MechanicalBoardingDefense;
            sd.OrdnanceInSpace = ship.Carrier.OrdnanceInSpace;
            sd.ScuttleTimer = ship.ScuttleTimer;

            if (ship.IsHomeDefense)
                sd.HomePlanetId = ship.HomePlanet.Id;

            if (ship.TradeRoutes?.NotEmpty == true)
            {
                sd.TradeRoutes = new Array<int>();
                foreach (int planetId in ship.TradeRoutes)
                {
                    sd.TradeRoutes.Add(planetId);
                }
            }

            sd.TransportingFood = ship.TransportingFood;
            sd.TransportingProduction = ship.TransportingProduction;
            sd.TransportingColonists = ship.TransportingColonists;
            sd.AllowInterEmpireTrade = ship.AllowInterEmpireTrade;

            return sd;
        }

        void SaveUniverseData(UniverseSaveData data, FileInfo saveFile)
        {
            var t = new PerfTimer();

            DateTime now = DateTime.Now;

            var header = new HeaderData
            {
                Version    = SaveGameVersion,
                SaveName   = data.SaveAs,
                StarDate   = data.StarDate.ToString("#.0"),
                PlayerName = data.PlayerLoyalty,
                RealDate   = now.ToString("M/d/yyyy") + " " + now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat),
                ModName    = GlobalStats.ModName,
                Time       = now,
            };

            using (var writer = new Writer(new FileStream(saveFile.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096)))
            {
                BinarySerializer.SerializeMultiType(writer, new object[] { header, data }, Verbose);
            }

            SaveTask = null;
            Log.Info($"Binary Total Save elapsed: {t.Elapsed:0.00}s ({saveFile.Length / (1024.0 * 1024.0):0.0}MB)");

            HelperFunctions.CollectMemory();
        }

        public static UniverseSaveData Deserialize(FileInfo saveFile, bool verbose)
        {
            var t = new PerfTimer();

            using var reader = new Reader(saveFile.OpenRead());
            var results = BinarySerializer.DeserializeMultiType(reader, new[]
            {
                typeof(HeaderData),
                typeof(UniverseSaveData)
            }, verbose);

            UniverseSaveData usData = (UniverseSaveData)results[1];

            Log.Info($"Binary Total Load elapsed: {t.Elapsed:0.0}s  ");

            return usData;
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
        public class ShipSaveData
        {
            [StarData] public int Id;
            [StarData] public Empire Owner;
            [StarData] public bool IsSpooling;
            [StarData] public ShipAI AISave;
            [StarData] public Vector2 Position;
            [StarData] public Vector2 Velocity;
            [StarData] public float Rotation;
            [StarData] public ModuleSaveData[] ModuleSaveData;
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
            [StarData] public float MechanicalBoardingDefense;
            [StarData] public float OrdnanceInSpace; // For carriers
            [StarData] public float ScuttleTimer = -1;
            [StarData] public int TetheredTo;
            [StarData] public Vector2 TetherOffset;
            [StarData] public Array<int> TradeRoutes;
            [StarData] public int HomePlanetId;
            [StarData] public bool FightersLaunched;
            [StarData] public bool TroopsLaunched;
            [StarData] public bool TransportingFood;
            [StarData] public bool TransportingProduction;
            [StarData] public bool TransportingColonists;
            [StarData] public bool AllowInterEmpireTrade;
            [StarData] public bool SendTroopsToShip;
            [StarData] public bool RecallFightersBeforeFTL;

            public ShipSaveData() {}

            public ShipSaveData(Ship ship)
            {
                Name = ship.Name;
                Owner = ship.Loyalty;
                IsSpooling = ship.IsSpooling;
                VanityName = ship.VanityName;
                MechanicalBoardingDefense = ship.MechanicalBoardingDefense;
                Id = ship.Id;
                Position = ship.Position;

                BaseStrength = ship.BaseStrength;
                Level      = ship.Level;
                Experience = ship.Experience;
                Kills      = ship.Kills;
                Velocity   = ship.Velocity;

                Hull      = ship.ShipData.Hull;
                Power     = ship.PowerCurrent;
                Ordnance  = ship.Ordinance;
                YRotation = ship.YRotation;
                Rotation  = ship.Rotation;
                InCombat  = ship.InCombat;

                if (ship.GetTether() != null)
                {
                    TetheredTo = ship.GetTether().Id;
                    TetherOffset = ship.TetherOffset;
                }

                AISave = ship.AI;
                ModuleSaveData = ship.GetModuleSaveData();
            }

            public override string ToString() => $"ShipSave {Id} {Name}";
        }

        [StarDataType]
        public class UniverseSaveData : IDisposable
        {
            [StarData] public int SaveGameVersion;
            [StarData] public int UniqueObjectIds;
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
            [StarData] public SolarSystem[] SolarSystems;
            [StarData] public Empire[] EmpireDataList;
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
            [StarData] public Map<string, Map<int, Snapshot>> Snapshots;
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
            [StarData] public ShipSaveData[] AllShips;
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

            // globally stored ship designs
            [StarData] public ShipDesign[] ShipDesigns;
            Map<string, IShipDesign> ShipDesignsCache;

            public void SetDesigns(HashSet<IShipDesign> designs)
            {
                ShipDesigns = designs.Select(d => (ShipDesign)d);
            }

            public IShipDesign GetDesign(string name)
            {
                if (ShipDesignsCache == null)
                {
                    ShipDesignsCache = new();

                    foreach (ShipDesign fromSave in ShipDesigns)
                    {
                        fromSave.IsFromSave = true;

                        if (ResourceManager.Ships.GetDesign(fromSave.Name, out IShipDesign existing) &&
                            existing.AreModulesEqual(fromSave))
                            // use the existing one
                            ShipDesignsCache[fromSave.Name] = existing;
                        else
                            // from save only
                            ShipDesignsCache[fromSave.Name] = fromSave;
                    }
                }
                return ShipDesignsCache[name];
            }

            public void Dispose()
            {
                EmpireDataList = null;
                Snapshots.Clear();
                FogMapBytes = null;
                Projectiles = null;
                Beams = null;
            }
        }
    }
}
