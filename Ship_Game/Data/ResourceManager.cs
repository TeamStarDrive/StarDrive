using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SgMotion;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Yaml;
using Ship_Game.SpriteSystem;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public class ResourceManagerFailure : Exception
    {
        public ResourceManagerFailure(string what) : base(what) {}
    }

    /**
     * Oh boy, so this is a monster class.
     * The whole content pipeline is divided into ~3 layers and ~2 steps. There are some oddballs though.
     *
     * Layer 1: ResourceManager -- Explores [Vanilla]+[Mod] files and prepares them.
     *                             Most game XML data files are loaded here.
     *                             Mesh & Texture access is cached here.
     *
     *     @todo ResourceManager should be split into: 1. MeshCache 2. TextureCache 3. DataCache
     *
     * Layer 2: GameContentManager -- This will [Vanilla]/[Mod] reroute any resource access and ensures there
     *                                is no content duplication.
     *                                Some special resource cases like TextureAtlas are also handled there.
     *                                Caching is also done at this level. All Disposable objects are recorded.
     *
     * Layer 3: XNA ContentManager & RawContentLoader -- Loads XNB files and also provides .png / .fbx / .obj support.
     *
     */
    public sealed class ResourceManager // Refactored by RedFox
    {
        // Dictionaries set to ignore case actively replace the xml UID settings, if there, to the filename.
        // the dictionary uses the file name as the key for the item. Case in these cases is not useful
        static readonly Map<string, SubTexture> Textures = new Map<string, SubTexture>();
        public static Map<string, Ship> ShipsDict                = new Map<string, Ship>();
        public static Map<string, Technology> TechTree           = new Map<string, Technology>(GlobalStats.CaseControl);
        static readonly Array<ToolTip> ToolTips          = new Array<ToolTip>();
        public static Array<Encounter> Encounters                = new Array<Encounter>();
        public static Map<string, Building> BuildingsDict        = new Map<string, Building>();
        public static Map<string, Good> GoodsDict                = new Map<string, Good>();
        public static Map<string, Weapon> WeaponsDict            = new Map<string, Weapon>();
        static readonly Map<string, ShipModule> ModuleTemplates = new Map<string, ShipModule>(GlobalStats.CaseControl);
        public static Map<string, Texture2D> ProjTextDict               = new Map<string, Texture2D>();

        public static Array<RandomItem> RandomItemsList       = new Array<RandomItem>();
        static readonly Map<string, Troop> TroopsDict = new Map<string, Troop>();
        static Array<string> TroopsDictKeys           = new Array<string>();
        public static IReadOnlyList<string> TroopTypes        => TroopsDictKeys;

        public static Map<string, Artifact> ArtifactsDict      = new Map<string, Artifact>();
        public static Map<string, ExplorationEvent> EventsDict = new Map<string, ExplorationEvent>(GlobalStats.CaseControl);
        public static XmlSerializer HeaderSerializer           = new XmlSerializer(typeof(HeaderData));

        static Map<string, SoundEffect> SoundEffectDict;

        public static HostileFleets HostileFleets                = new HostileFleets();
        public static ShipNames ShipNames                        = new ShipNames();
        public static AgentMissionData AgentMissionData          = new AgentMissionData();
        public static MainMenuShipList MainMenuShipList          = new MainMenuShipList();
        public static Map<ShipData.RoleName, ShipRole> ShipRoles = new Map<ShipData.RoleName, ShipRole>();
        public static Map<string, HullBonus> HullBonuses         = new Map<string, HullBonus>();
        public static Map<string, PlanetEdict> PlanetaryEdicts   = new Map<string, PlanetEdict>();

        static RacialTraits RacialTraits;
        static DiplomaticTraits DiplomacyTraits;

        // All references to Game1.Instance.Content were replaced by this property
        public static GameContentManager RootContent => StarDriveGame.Instance.Content;

        public static Technology Tech(string techUid)
        {
            return TechTree[techUid];
        }

        public static bool TryGetTech(string techUid, out Technology tech) => TechTree.TryGetValue(techUid, out tech);

        public static ExplorationEvent Event(string eventName, string defaultEvent = "default")
        {
            if (EventsDict.TryGetValue(eventName, out ExplorationEvent events)) return events;
            Log.WarningWithCallStack($"{eventName} not found. Contact mod creator.");
            return EventsDict["default"];
        }

        // This is used for lazy-loading content triggers
        public static int ContentId { get; private set; }

        public static void LoadItAll(ScreenManager manager, ModEntry mod, bool reset = true)
        {
            Stopwatch s = Stopwatch.StartNew();
            try
            {
                LoadAllResources(manager, mod, reset);
                if (mod != null) // if Mod load succeeded, then:
                    GlobalStats.SaveActiveMod();
            }
            catch (Exception ex)
            {
                if (mod == null)
                    throw;
                Log.ErrorDialog(ex, $"Mod {GlobalStats.ModName} load failed. Disabling mod and loading vanilla.", isFatal: false);
                GlobalStats.ClearActiveMod();
                LoadAllResources(manager, null, true);
            }
            Log.Write($"LoadItAll elapsed: {s.Elapsed.TotalSeconds}s");
        }

        static TaskResult BackgroundLoad; // non-critical background load task
        public static bool IsLoadCancelRequested => BackgroundLoad != null && BackgroundLoad.IsCancelRequested;

        public static void WaitForExit()
        {
            if (BackgroundLoad != null && !BackgroundLoad.IsComplete)
            {
                Log.Write("Cannot exit immediately. CancelAndWait.");
                BackgroundLoad.CancelAndWait();
            }
        }

        static void LoadAllResources(ScreenManager manager, ModEntry mod, bool reset)
        {
            if (BackgroundLoad != null && !BackgroundLoad.IsComplete)
            {
                Log.Write("ResourceManager was already loading. CancelAndWait.");
                BackgroundLoad.CancelAndWait();
            }

            if (reset) Reset(manager);

            if (mod != null)
                GlobalStats.SetActiveModNoSave(mod);
            else
                GlobalStats.ClearActiveMod();

            Log.Write($"Load {(GlobalStats.HasMod ? GlobalStats.ModPath : "Vanilla")}");
            LoadLanguage(); // @todo Slower than expected [0.36]
            LoadToolTips();
            LoadHullData(); // we need Hull Data for main menu ship
            LoadEmpires();  // empire for NewGame @todo Very slow [0.54%]

            LoadTextureAtlases();

            LoadNebulae();
            LoadStars();
            LoadFlagTextures(); // @todo Very slow for some reason [1.04%]
            LoadHullBonuses();

            LoadTroops();
            LoadWeapons();     // @todo Also slow [0.87%]
            LoadShipModules(); // @todo Extremely slow [1.29%]
            LoadGoods();
            LoadShipTemplates(); // @note Extremely fast :))) Loads everything in [0.16%]
            LoadShipRoles();
            LoadJunk();      // @todo SLOW [0.47%]
            LoadAsteroids(); // @todo SLOW [0.40%]
            LoadProjTexts(); // @todo SLOW [0.47%]
            LoadBuildings();
            LoadProjectileMeshes();
            LoadRandomItems();
            LoadDialogs();  // @todo SLOW [0.44%]
            LoadTechTree(); // @todo This is VERY slow, about 4x slower than loading textures [0.97%]
            LoadEncounters();
            LoadExpEvents();
            TechValidator();

            LoadArtifacts();
            LoadPlanetEdicts();
            LoadPlanetTypes();
            LoadSunZoneData();
            SunType.LoadAll();
            LoadEconomicResearchStrategies();
            LoadBlackboxSpecific();
            ShieldManager.LoadContent(RootContent);
            Beam.BeamEffect = RootContent.Load<Effect>("Effects/BeamFX");
            BackgroundItem.QuadEffect = new BasicEffect(ScreenManager.Instance.GraphicsDevice, null) { TextureEnabled = true };
            TestLoad();

            ++ContentId; // LoadContent will see a new content id
            if (reset) // now reload manager content, otherwise we're f**ked as soon as game calls Draw
                manager.LoadContent();

            // Load non-critical resources:
            void LoadNonCritical()
            {
                Log.Write("Load non-critical resources");
                ExplosionManager.Initialize(RootContent);
                LoadNonEssentialAtlases(BackgroundLoad);
                Log.Write("Finished loading non-critical resources");
            }

            //LoadNonCritical();
            BackgroundLoad = Parallel.Run(LoadNonCritical);

            HelperFunctions.CollectMemory();
        }

        public static void Reset(ScreenManager manager)
        {
            manager.ResetHotLoadTargets();
            WeaponsDict.Clear();
            TroopsDict.Clear();
            TroopsDictKeys.Clear();
            BuildingsDict.Clear();
            BuildingsById.Clear();
            ModuleTemplates.Clear();
            TechTree.Clear();
            ArtifactsDict.Clear();
            ShipsDict.Clear();
            SoundEffectDict = null;
            ToolTips.Clear();
            GoodsDict.Clear();
            Encounters.Clear();
            EventsDict.Clear();
            RandomItemsList.Clear();
            ProjectileMeshDict.Clear();
            ProjTextDict.Clear();

            HostileFleets.Fleets.Clear();
            ShipNames.Clear();
            MainMenuShipList.ModelPaths.Clear();
            AgentMissionData = new AgentMissionData();

            // Texture caches MUST be cleared before triggering content reload!
            Textures.Clear();

            // This is a destructive operation that invalidates ALL game resources!
            // @note It HAS to be done after clearing all ResourceManager texture caches!
            manager.UnloadAllGameContent();
        }


        static void TestLoad()
        {
            if (!GlobalStats.TestLoad) return;

            Log.ShowConsoleWindow();
            TestHullLoad();
            //TestTechTextures();

            Log.HideConsoleWindow();
        }

        static void TestHullLoad()
        {
            if (!Log.TestMessage("TEST - LOAD ALL HULL MODELS\n", waitForYes: true))
                return;

            bool oldValue = RootContent.EnableLoadInfoLog;
            RootContent.EnableLoadInfoLog = true;
            foreach (ShipData hull in HullsList.OrderBy(race => race.ShipStyle).ThenBy(role => role.Role))
            {
                try
                {
                    Log.TestMessage($"Loading model {hull.ModelPath} for hull {hull.Name}\n", Log.Importance.Regular);
                    hull.PreLoadModel();
                }
                catch (Exception e)
                {
                    Log.TestMessage($"Failure loading model {hull.ModelPath} for hull {hull.Name}\n{e}", Log.Importance.Critical);
                }

            }
            HelperFunctions.CollectMemory();
            Log.TestMessage("Hull Model Load Finished", waitForEnter: true);
            RootContent.EnableLoadInfoLog = oldValue;
        }

        public static bool PreLoadModels(Empire empire)
        {
            if (!GlobalStats.PreLoad)
                return true;
            Log.Warning($"Preloading Ship Models for {empire.Name}. ");
            try
            {
                foreach (KeyValuePair<string, TechEntry> kv in empire.TechnologyDict)
                {
                    kv.Value.LoadShipModelsFromDiscoveredTech(empire);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Model PreLoad failed");
                Log.OpenURL("https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/1464/xna-texture-loading-consumes-excessive");
                return false;
            }
            return true;
        }

        // Gets FileInfo for Mod or Vanilla file. Mod file is checked first
        // Example relativePath: "Textures/myAtlas.xml"
        public static FileInfo GetModOrVanillaFile(string relativePath)
        {
            FileInfo info;
            if (GlobalStats.HasMod)
            {
                info = new FileInfo(GlobalStats.ModPath + relativePath);
                if (info.Exists)
                    return info;
            }
            info = new FileInfo("Content/" + relativePath);
            return info.Exists ? info : null;
        }

        // This first tries to deserialize from Mod folder and then from Content folder
        static T TryDeserialize<T>(string file) where T : class
        {
            FileInfo info = null;
            if (GlobalStats.HasMod) info = new FileInfo(GlobalStats.ModPath + file);
            if (info == null || !info.Exists) info = new FileInfo("Content/" + file);
            if (!info.Exists)
                return null;
            using (Stream stream = info.OpenRead())
                return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
        }

        // The entity value is assigned only IF file exists and Deserialize succeeds
        static void TryDeserialize<T>(string file, ref T entity) where T : class
        {
            var result = TryDeserialize<T>(file);
            if (result != null) entity = result;
        }

        // This gathers an union of Mod and Vanilla files. Any vanilla file is replaced by mod files.
        public static FileInfo[] GatherFilesUnified(string dir, string ext, bool recursive = true)
        {
            string pattern = "*." + ext;
            SearchOption search = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (!GlobalStats.HasMod)
                return Dir.GetFiles("Content/" + dir, pattern, search);

            var infos = new Map<string, FileInfo>();

            // For XML files we allow subfolders such as Buildings/Colonization/Aeroponics.xml
            // Which means the unification has to be done via `file.Name`
            // For other files such as XNB textures, we use the full path normalized to relative content path
            // such as: "Mods/MyMod/Textures/TechIcons/Aeroponics.xnb" --> "Textures/TechIcons/Aeroponics.xnb"
            bool fileNames = ext == "xml";

            FileInfo[] vanilla = Dir.GetFiles("Content/" + dir, pattern, search);
            string vanillaPath = Path.GetFullPath("Content/");
            foreach (FileInfo file in vanilla)
            {
                string name = fileNames ? file.Name : file.FullName.Substring(vanillaPath.Length);
                infos[name] = file;
            }

            // now pull everything from the mod folder and replace all matches
            FileInfo[] mod = Dir.GetFiles(GlobalStats.ModPath + dir, pattern, search);
            string fullModPath = Path.GetFullPath(GlobalStats.ModPath);
            foreach (FileInfo file in mod)
            {
                string name = fileNames ? file.Name : file.FullName.Substring(fullModPath.Length);
                #if false
                if (infos.TryGetValue(name, out FileInfo existing))
                {
                    string newName = GlobalStats.ModPath + file.FullName.Substring(fullModPath.Length);
                    string existingName = existing.FullName;
                    if (existingName.StartsWith(fullModPath))
                        existingName = GlobalStats.ModPath + existingName.Substring(fullModPath.Length);
                    else
                        existingName = existingName.Substring(vanillaPath.Length);
                    Log.Info($"ModReplace {existingName,64} -> {newName}");
                }
                #endif
                infos[name] = file;
            }

            return infos.Values.ToArray();
        }

        // This tries to gather only mod files, or only vanilla files
        // No union/mix is made
        public static FileInfo[] GatherFilesModOrVanilla(string dir, string ext)
        {
            if (!GlobalStats.HasMod) return Dir.GetFiles("Content/" + dir, ext);
            FileInfo[] files = Dir.GetFiles(GlobalStats.ModPath + dir, ext);
            return files.Length != 0 ? files : Dir.GetFiles("Content/" + dir, ext);
        }

        // Loads a list of entities in a folder
        public static Array<T> LoadEntitiesModOrVanilla<T>(string dir, string where) where T : class
        {
            var result = new Array<T>();
            var files = GatherFilesModOrVanilla(dir, "xml");
            if (files.Length != 0)
            {
                var s = new XmlSerializer(typeof(T));
                foreach (FileInfo info in files)
                    if (LoadEntity(s, info, where, out T entity))
                        result.Add(entity);
            }
            else Log.Error($"{where}: No files in '{dir}'");
            return result;
        }

        // Added by RedFox - Generic entity loading, less typing == more fun
        static bool LoadEntity<T>(XmlSerializer s, FileInfo info, string id, out T entity) where T : class
        {
            try
            {
                using (FileStream stream = info.OpenRead())
                    entity = (T)s.Deserialize(stream);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Deserialize {id} failed");
                entity = null;
                return false;
            }
        }

        static Array<T> LoadEntities<T>(FileInfo[] files, string id) where T : class
        {
            var list = new Array<T>();
            list.Resize(files.Length);
            var s = new XmlSerializer(typeof(T));
            Parallel.For(files.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    if (LoadEntity(s, files[i], id, out T entity))
                        list[i] = entity; // no need to lock; `list` internal array is not modified
            });
            return list;
        }

        static Array<T> LoadEntities<T>(string dir, string id) where T : class
        {
            return LoadEntities<T>(GatherFilesUnified(dir, "xml"), id);
        }

        static Array<T> LoadVanillaEntities<T>(string dir, string id) where T : class
        {
            return LoadEntities<T>(Dir.GetFiles("Content/" + dir, "xml"), id);
        }

        static Array<T> LoadModEntities<T>(string dir, string id) where T : class
        {
            return LoadEntities<T>(Dir.GetFiles(GlobalStats.ModPath + dir, "xml"), id);
        }

        class InfoPair<T> where T : class
        {
            public readonly FileInfo Info;
            public readonly T Entity;
            public InfoPair(FileInfo info, T entity) { Info = info; Entity = entity; }
        }

        static Array<InfoPair<T>> LoadEntitiesWithInfo<T>(string dir, string id, bool modOnly = false) where T : class
        {
            var files = modOnly ? Dir.GetFiles(GlobalStats.ModPath + dir, "xml") : GatherFilesUnified(dir, "xml");
            var list = new Array<InfoPair<T>>();
            list.Resize(files.Length);
            var s = new XmlSerializer(typeof(T));
            Parallel.For(files.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    FileInfo info = files[i];
                    if (LoadEntity(s, info, id, out T entity))
                        list[i] = new InfoPair<T>(info, entity); // no need to lock; `list` internal array is not modified
                }
            });
            return list;
        }

        public static float GetTroopCost(string troopType) => TroopsDict[troopType].ActualCost;
        public static Troop GetTroopTemplate(string troopType)
        {
            if (TroopsDict.TryGetValue(troopType, out Troop troopO)) return troopO;
            Log.WarningWithCallStack($"Troop {troopType} Template Not found");
            return TroopsDict.First().Value;
        }
        public static Array<Troop> GetTroopTemplates() => new Array<Troop>(TroopsDict.Values);

        public static Troop CreateTroop(string troopType, Empire forOwner)
        {
            Troop troop = TroopsDict[troopType].Clone();
            if (troop.StrengthMax <= 0)
                troop.StrengthMax = troop.Strength;

            troop.WhichFrame = (int)RandomMath.RandomBetween(1, troop.num_idle_frames - 1);

            if (forOwner != null)
            {
                troop.SetOwner(forOwner);
                troop.HealTroop(troop.ActualStrengthMax);
            }
            return troop;
        }

        static void LoadTroops()
        {
            foreach (var pair in LoadEntitiesWithInfo<Troop>("Troops", "LoadTroops"))
            {
                Troop troop = pair.Entity;
                troop.Name = pair.Info.NameNoExt();
                troop.Type = pair.Info.NameNoExt();
                TroopsDict[troop.Name] = troop;

                if (troop.StrengthMax <= 0)
                    troop.StrengthMax = troop.Strength;
            }
            TroopsDictKeys = new Array<string>(TroopsDict.Keys);
        }

        public static MarkovNameGenerator GetRandomNames(Empire empire)
        {
            string nameFileName = $"NameGenerators/spynames_{empire?.PortraitName}.txt";
            return GetNameGenerator(nameFileName, 3, 5);
        }

        public static MarkovNameGenerator GetNameGenerator(string relativePath, int order, int minLength)
        {
            var nameFile = GetModOrVanillaFile(relativePath);
            if (nameFile == null) return null;
            return new MarkovNameGenerator(nameFile.OpenText().ReadToEnd(), order, minLength);
        }

        // Added by RedFox
        static void DeleteShipFromDir(string dir, string shipName)
        {
            foreach (FileInfo info in Dir.GetFiles(dir, shipName + ".xml", SearchOption.TopDirectoryOnly))
            {
                // @note ship.Name is always the same as fileNameNoExt
                //       part of "shipName.xml", so we can skip parsing the XML-s
                if (info.NameNoExt() == shipName)
                {
                    info.Delete();
                    return;
                }
            }
        }

        // Refactored by RedFox
        public static void DeleteShip(string shipName)
        {
            DeleteShipFromDir("Content/StarterShips", shipName);
            DeleteShipFromDir("Content/SavedDesigns", shipName);

            string appData = Dir.StarDriveAppData;
            DeleteShipFromDir(appData + "/Saved Designs", shipName);
            DeleteShipFromDir(appData + "/WIP", shipName);
            GetShipTemplate(shipName).Deleted = true;
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e.ShipsWeCanBuild.Remove(shipName))
                    e.UpdateShipsWeCanBuild();
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////

        public static Array<FileInfo> GetAllXnbModelFiles(string folder)
        {
            return RootContent.RawContent.GetAllXnbModelFiles(folder);
        }

        //////////////////////////////////////////////////////////////////////////////////////////

        // Load texture with its abstract path such as
        // "Explosions/smaller/shipExplosion"
        public static SubTexture TextureOrNull(string textureName)
        {
            if (Textures.TryGetValue(textureName, out SubTexture loaded))
                return loaded;
            loaded = RootContent.LoadSubTexture("Textures/" + textureName);
            Textures[textureName] = loaded; // save even if null
            return loaded;
        }
        public static SubTexture TextureOrDefault(string textureName, string defaultTex)
        {
            SubTexture loaded = TextureOrNull(textureName);
            return loaded ?? Texture(defaultTex);
        }

        public static SubTexture Texture(string textureName)
        {
            SubTexture loaded = TextureOrNull(textureName);
            if (loaded != null)
                return loaded;

            Log.WarningWithCallStack($"Texture path not found: '{textureName}' replacing with 'NewUI/x_red'");

            SubTexture errorTexture = TextureOrNull("NewUI/x_red");
            Textures[textureName] = errorTexture; // so we don't get this error again
            return errorTexture;
        }

        public static Texture2D Texture2D(string textureNamePath)
        {
            return RootContent.Load<Texture2D>("Textures/" + textureNamePath);
        }

        public static bool TextureLoaded(string texturePath)
        {
            return Textures.ContainsKey(texturePath);
        }

        public static SubTexture ProjTexture(string texturePath)
        {
            if (ProjTextDict.TryGetValue(texturePath, out Texture2D proj))
                return new SubTexture(texturePath, proj);
            return RootContent.DefaultTexture();
        }

        public static FileInfo[] GatherTextureFiles(string dir, bool recursive)
        {
            string[] extensions = { "png", "gif", "jpg", "xnb", "dds" };
            var allFiles = new Array<FileInfo>();
            foreach (string ext in extensions)
            {
                allFiles.AddRange(GatherFilesUnified(dir, ext, recursive));
            }
            return allFiles.ToArray();
        }

        // Any non-trivial 3D-models can't handle texture atlases
        // It's a significant limitation :(
        // This is a HACK to circumvent the issue
        public static readonly HashSet<string> AtlasExcludeTextures = new HashSet<string>(new[]
        {
            "Atmos", "clouds"
        });
        public static readonly HashSet<string> AtlasExcludeFolder = new HashSet<string>(new[]
        {
            "Beams", "Ships"
        });

        static void LoadAtlas(string folder)
        {
            var atlas = RootContent.LoadTextureAtlas(folder, useCache: true);
            if (atlas == null) Log.Warning($"LoadAtlas {folder} failed");
        }

        // This is just to speed up initial atlas generation and avoid noticeable framerate hiccups
        static void LoadTextureAtlases()
        {
            // these are essential for main menu, so we load them as blocking
            LoadAtlas("Textures");
            LoadAtlas("Textures/GameScreens");
            LoadAtlas("Textures/MainMenu");
            LoadAtlas("Textures/EmpireTopBar");
            LoadAtlas("Textures/NewUI");
        }

        static void LoadNonEssentialAtlases(TaskResult task)
        {
            Stopwatch s = Stopwatch.StartNew();
            // these are non-critical and can be loaded in background
            var atlases = new[]
            {
                // Main Menu
                "Textures/Races",     // NewGame screen
                "Textures/UI",        // NewGame screen
                "Textures/ShipIcons", // LoadGame screen
                "Textures/Popup",     // Options screen

                // UniverseScreen
                "Textures/Arcs",
                "Textures/SelectionBox",
                "Textures/Minimap",
                "Textures/Ships",
                "Textures/hqspace",
                "Textures/PlanetGlows",
                "Textures/TacticalIcons",
                "Textures/Planets",
                "Textures/PlanetTiles",
                "Textures/Buildings",
                "Textures/ResearchMenu",
                "Textures/TechIcons",
                "Textures/Modules",
                "Textures/TroopIcons",
                "Textures/Portraits",
                "Textures/Ground_UI",
                "Textures/Troops",
                "Textures/OrderButtons",
            };
            foreach (string atlas in atlases)
            {
                if (task?.IsCancelRequested == true)
                    break;
                LoadAtlas(atlas);
            }
            Log.Write($"LoadAtlases (background) elapsed:{s.Elapsed.TotalMilliseconds}ms");
        }

        public static ShipModule GetModuleTemplate(string uid) => ModuleTemplates[uid];
        public static bool GetModuleTemplate(string uid, out ShipModule module) => ModuleTemplates.Get(uid, out module);
        public static bool ModuleExists(string uid) => ModuleTemplates.ContainsKey(uid);
        public static IReadOnlyDictionary<string, ShipModule> ShipModules => ModuleTemplates;
        public static ICollection<ShipModule> ShipModuleTemplates => ModuleTemplates.Values;

        public static RacialTraits RaceTraits
            => RacialTraits ?? (RacialTraits = TryDeserialize<RacialTraits>("RacialTraits/RacialTraits.xml"));

        public static DiplomaticTraits DiplomaticTraits
            => DiplomacyTraits ?? (DiplomacyTraits = TryDeserialize<DiplomaticTraits>("Diplomacy/DiplomaticTraits.xml"));

        public static SolarSystemData LoadSolarSystemData(string homeSystemName)
            => TryDeserialize<SolarSystemData>("SolarSystems/" + homeSystemName + ".xml");

        public static Array<SolarSystemData> LoadRandomSolarSystems()
            => LoadEntitiesModOrVanilla<SolarSystemData>("SolarSystems/Random", "LoadSolarSystems");

        public static Texture2D LoadRandomLoadingScreen(GameContentManager content)
        {
            var files = GatherFilesModOrVanilla("LoadingScreen", "xnb");

            FileInfo file = files[RandomMath.InRange(0, files.Length)];
            return content.Load<Texture2D>(file.CleanResPath());
        }

        // advice is temporary and only sticks around while loading
        public static string LoadRandomAdvice()
        {
            string adviceFile = "Advice/" + GlobalStats.Language + "/Advice.xml";

            var adviceList = TryDeserialize<Array<string>>(adviceFile);
            return adviceList?[RandomMath.InRange(adviceList.Count)] ?? "Advice.xml missing";
        }

        static void LoadArtifacts() // Refactored by RedFox
        {
            foreach (var arts in LoadEntities<Array<Artifact>>("Artifacts", "LoadArtifacts"))
            {
                foreach (Artifact art in arts)
                {
                    art.Name = string.Intern(art.Name);
                    ArtifactsDict[art.Name] = art;
                }
            }
        }

        static readonly Array<Building> BuildingsById = new Array<Building>();
        public static bool BuildingExists(int buildingId) => 0 < buildingId && buildingId < BuildingsById.Count;
        public static Building GetBuildingTemplate(string whichBuilding) => BuildingsDict[whichBuilding];
        public static Building GetBuildingTemplate(int buildingId) => BuildingsById[buildingId];
        public static Building CreateBuilding(string whichBuilding) => CreateBuilding(GetBuildingTemplate(whichBuilding));
        public static Building CreateBuilding(int buildingId) => CreateBuilding(GetBuildingTemplate(buildingId));
        public static bool GetBuilding(string whichBuilding, out Building b) => BuildingsDict.Get(whichBuilding, out b);
        public static bool GetBuilding(int buildingId, out Building b)
        {
            if (!BuildingExists(buildingId)) { b = null; return false; }
            else { b = BuildingsById[buildingId]; return true; }
        }

        static void LoadBuildings() // Refactored by RedFox
        {
            Array<Building> buildings = LoadEntities<Building>("Buildings", "LoadBuildings");
            BuildingsById.Resize(buildings.Count + 1);

            int buildingId = 0;
            foreach (Building b in buildings)
            {
                b.AssignBuildingId(++buildingId);
                BuildingsById[b.BID] = b;
                BuildingsDict[string.Intern(b.Name)] = b;
                switch (b.Name)
                {
                    case "Capital City":      Building.CapitalId          = b.BID; break;
                    case "Outpost":           Building.OutpostId          = b.BID; break;
                    case "Biospheres":        Building.BiospheresId       = b.BID; break;
                    case "Space Port":        Building.SpacePortId        = b.BID; break;
                    case "Terraformer":       Building.TerraformerId      = b.BID; break;
                    case "Fissionables":      Building.FissionablesId     = b.BID; break;
                    case "Mine Fissionables": Building.MineFissionablesId = b.BID; break;
                    case "Fuel Refinery":     Building.FuelRefineryId     = b.BID; break;
                }
            }
        }

        public static Building CreateBuilding(Building template)
        {
            Building newB = template.Clone();

            // comp fix to ensure functionality of vanilla buildings
            if (newB.IsCapitalOrOutpost)
            {
                // @todo What is going on here? Is this correct?
                if (!newB.IsProjector && !(newB.ProjectorRange > 0f))
                {
                    // @todo NullReference bug here!
                    newB.ProjectorRange = Empire.Universe?.SubSpaceProjectors.Radius ?? 0f;
                    newB.IsProjector = true;
                }
                if (!newB.IsSensor && !(newB.SensorRange > 0.0f))
                {
                    newB.SensorRange = 20000.0f;
                    newB.IsSensor = true;
                }
            }
            newB.CreateWeapon();
            return newB;
        }

        static readonly Map<string, DiplomacyDialog> DiplomacyDialogs = new Map<string, DiplomacyDialog>();
        public static DiplomacyDialog GetDiplomacyDialog(string dialogName)
        {
            return DiplomacyDialogs[dialogName];
        }
        static void LoadDialogs() // Refactored by RedFox
        {
            string dir = "DiplomacyDialogs/" + GlobalStats.Language + "/";
            foreach (var pair in LoadEntitiesWithInfo<DiplomacyDialog>(dir, "LoadDialogs"))
            {
                string nameNoExt = pair.Info.NameNoExt();
                DiplomacyDialogs[nameNoExt] = pair.Entity;
            }
        }

        static readonly Array<IEmpireData> Empires      = new Array<IEmpireData>();
        static readonly Array<IEmpireData> MajorEmpires = new Array<IEmpireData>();
        static readonly Array<IEmpireData> MinorEmpires = new Array<IEmpireData>(); // IsFactionOrMinorRace

        public static IReadOnlyList<IEmpireData> AllRaces   => Empires;
        public static IReadOnlyList<IEmpireData> MajorRaces => MajorEmpires;
        public static IReadOnlyList<IEmpireData> MinorRaces => MinorEmpires;

        static void LoadEmpires() // Refactored by RedFox
        {
            Empires.Clear();
            MajorEmpires.Clear();
            MinorEmpires.Clear();

            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.DisableDefaultRaces)
                Empires.AddRange(LoadModEntities<EmpireData>("Races", "LoadEmpires"));
            else
                Empires.AddRange(LoadEntities<EmpireData>("Races", "LoadEmpires"));

            foreach (IEmpireData e in Empires)
            {
                if (e.IsFactionOrMinorRace)
                    MinorEmpires.Add(e);
                else
                    MajorEmpires.Add(e);

                // HACK: Fix empires with invalid ShipType
                RacialTrait t = ((EmpireData)e).Traits;
                if (t.ShipType.IsEmpty())
                {
                    t.ShipType = e.Singular;
                    Log.Warning($"Empire {e.Name} invalid ShipType ''. Using '{e.Singular}' instead.");
                }
            }
        }

        public static void LoadEncounters()
        {
            Encounters = LoadEntities<Encounter>("Encounter Dialogs", "LoadEncounters");

            foreach (Encounter encounter in Encounters)
            {
                foreach (Message message in encounter.MessageList)
                {
                    foreach (Response response in message.ResponseOptions)
                    {
                        if (TechTree.TryGetValue(response.UnlockTech ?? "", out Technology tech))
                        {
                            if (tech.Unlockable)
                                continue;
                            tech.Unlockable = true;
                            if (GlobalStats.VerboseLogging)
                                Log.WarningVerbose($"Technology can be unlocked by encounter '{encounter.Name}' : '{tech.UID}'");
                        }
                    }
                }
            }
        }

        static void LoadExpEvents() // Refactored by RedFox
        {
            foreach (var pair in LoadEntitiesWithInfo<ExplorationEvent>("Exploration Events", "LoadExpEvents"))
            {
                EventsDict[pair.Info.NameNoExt()] = pair.Entity;
                foreach (var outcome in pair.Entity.PotentialOutcomes)
                {
                    if (TechTree.TryGetValue(outcome.UnlockTech ?? "", out Technology tech))
                    {
                        if (tech.Unlockable) continue;
                        tech.Unlockable = true;
                        Log.WarningVerbose($"Technology can be unlocked by event '{pair.Entity.Name}' : '{tech.UID}'");
                    }
                    if (TechTree.TryGetValue(outcome.SecretTechDiscovered ?? "", out tech))
                    {
                        if (tech.Unlockable) continue;
                        tech.Unlockable = true;
                        Log.WarningVerbose($"Secret Technology can be unlocked by event '{pair.Entity.Name}' : '{tech.UID}'");
                    }
                }
            }
        }

        static TextureAtlas FlagTextures;
        public static SubTexture Flag(int index) => FlagTextures[index];
        public static SubTexture Flag(Empire e) => FlagTextures[e.data.Traits.FlagIndex];
        public static int NumFlags => FlagTextures.Count;
        static void LoadFlagTextures() // Refactored by RedFox
        {
            FlagTextures = RootContent.LoadTextureAtlas("Flags");
        }

        static void LoadGoods() // Refactored by RedFox
        {
            foreach (var pair in LoadEntitiesWithInfo<Good>("Goods", "LoadGoods"))
            {
                Good good           = pair.Entity;
                good.UID            = string.Intern(pair.Info.NameNoExt());
                GoodsDict[good.UID] = good;
            }
        }

        static readonly Map<string, ShipData> HullsDict = new Map<string, ShipData>();
        static readonly Array<ShipData> HullsList       = new Array<ShipData>();

        public static bool Hull(string shipHull, out ShipData hullData) => HullsDict.Get(shipHull, out hullData);
        public static ShipData Hull(string shipHull)                    => HullsDict[shipHull];
        public static IReadOnlyList<ShipData> Hulls                     => HullsList;

        static void LoadHullBonuses()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses)
            {
                foreach (HullBonus hullBonus in LoadEntities<HullBonus>("HullBonuses", "LoadHullBonuses"))
                    HullBonuses[hullBonus.Hull] = hullBonus;
                GlobalStats.ActiveModInfo.useHullBonuses = HullBonuses.Count != 0;
            }
        }

        static void LoadHullData() // Refactored by RedFox
        {
            HullsDict.Clear();
            HullsList.Clear();
            FileInfo[] hullFiles = GatherFilesUnified("Hulls", "xml");
            var hulls = new ShipData[hullFiles.Length];

            void LoadHulls(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    FileInfo info = hullFiles[i];
                    try
                    {
                        string dirName     = info.Directory?.Name ?? "";
                        ShipData shipData  = ShipData.Parse(info);
                        shipData.Hull      = dirName + "/" + shipData.Hull;
                        shipData.ShipStyle = dirName;
                        shipData.Role      = shipData.Role == ShipData.RoleName.carrier ? ShipData.RoleName.capital : shipData.Role;
                        shipData.UpdateBaseHull();
                        hulls[i] = shipData;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"LoadHullData {info.Name} failed");
                    }
                }
            }
            Parallel.For(hullFiles.Length, LoadHulls);
            //LoadHulls(0, hullFiles.Length);
            foreach (ShipData sd in hulls) // Finalize HullsDict:
            {
                if (sd == null) continue; // will be null if ShipData.Parse failed
                HullsDict[sd.Hull] = sd;
                HullsList.Add(sd);
            }
        }


        // loads models from a model folder that match "modelPrefixNNN.xnb" format, where N is an integer
        static void LoadNumberedModels(Array<Model> models, string modelFolder, string modelPrefix)
        {
            models.Clear();
            foreach (FileInfo info in GatherFilesModOrVanilla(modelFolder, "xnb"))
            {
                string nameNoExt = info.NameNoExt();
                try
                {
                    // only accept "prefixNN" format, because there are a bunch of textures in the asteroids folder
                    if (!nameNoExt.StartsWith(modelPrefix) || !int.TryParse(nameNoExt.Substring(modelPrefix.Length), out int _))
                        continue;
                    models.Add(RootContent.Load<Model>(info.CleanResPath()));
                }
                catch (Exception e)
                {
                    Log.Error(e, $"LoadNumberedModels {modelFolder} {nameNoExt} failed");
                }
            }
        }

        static readonly Array<Model> JunkModels = new Array<Model>();
        public static int NumJunkModels => JunkModels.Count;
        public static Model GetJunkModel(int idx)
        {
            return JunkModels[idx];
        }
        static void LoadJunk() // Refactored by RedFox
        {
            LoadNumberedModels(JunkModels, "Model/SpaceJunk/", "spacejunk");
        }

        static readonly Array<Model> AsteroidModels = new Array<Model>();
        public static int NumAsteroidModels => AsteroidModels.Count;
        public static Model GetAsteroidModel(int asteroidId)
        {
            return AsteroidModels[asteroidId];
        }
        static void LoadAsteroids()
        {
            LoadNumberedModels(AsteroidModels, "Model/Asteroids/", "asteroid");
        }

        static void LoadLanguage() // Refactored by RedFox
        {
            foreach (var loc in LoadVanillaEntities<LocalizationFile>("Localization/English/", "LoadLanguage"))
                Localizer.AddTokens(loc.TokenList);

            if (GlobalStats.NotEnglish)
            {
                foreach (var loc in LoadVanillaEntities<LocalizationFile>($"Localization/{GlobalStats.Language}/", "LoadLanguage"))
                    Localizer.AddTokens(loc.TokenList);
            }

            // Now replace any vanilla tokens with mod tokens
            if (GlobalStats.HasMod)
            {
                foreach (var loc in LoadModEntities<LocalizationFile>("Localization/English/", "LoadLanguage"))
                    Localizer.AddTokens(loc.TokenList);

                if (GlobalStats.NotEnglish)
                {
                    foreach (var loc in LoadModEntities<LocalizationFile>($"Localization/{GlobalStats.Language}/", "LoadLanguage"))
                        Localizer.AddTokens(loc.TokenList);
                }
            }

        }


        public static TextureAtlas SmallStars, MediumStars, LargeStars;

        static void LoadStars()
        {
            SmallStars  = RootContent.LoadTextureAtlas("SmallStars");
            MediumStars = RootContent.LoadTextureAtlas("MediumStars");
            LargeStars  = RootContent.LoadTextureAtlas("LargeStars");
        }

        static readonly Array<Texture2D> BigNebulae   = new Array<Texture2D>();
        static readonly Array<Texture2D> MedNebulae   = new Array<Texture2D>();
        static readonly Array<Texture2D> SmallNebulae = new Array<Texture2D>();

        // Refactored by RedFox
        static void LoadNebulae()
        {
            BigNebulae.Clear();
            MedNebulae.Clear();
            SmallNebulae.Clear();

            FileInfo[] files = Dir.GetFiles("Content/Nebulas", "xnb");
            var nebulae = new Texture2D[files.Length];
            Parallel.For(files.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    nebulae[i] = RootContent.Load<Texture2D>("Nebulas/" + files[i].NameNoExt());
            });
            foreach (Texture2D tex in nebulae)
            {
                if      (tex.Width >= 2048) { BigNebulae.Add(tex); }
                else if (tex.Width >= 1024) { MedNebulae.Add(tex); }
                else                        { SmallNebulae.Add(tex); }
            }
        }
        public static SubTexture SmallNebulaRandom()
        {
            return new SubTexture("small_nebula", RandomMath.RandItem(SmallNebulae));
        }
        public static SubTexture NebulaMedRandom()
        {
            return new SubTexture("med_nebula", RandomMath.RandItem(MedNebulae));
        }
        public static SubTexture NebulaBigRandom()
        {
            return new SubTexture("big_nebula", RandomMath.RandItem(BigNebulae));
        }
        public static SubTexture BigNebula(int index)
        {
            return new SubTexture("big_nebula", BigNebulae[index]);
        }


        // Refactored by RedFox
        public static Map<string, ModelMesh> ProjectileMeshDict = new Map<string, ModelMesh>();
        public static Map<string, Model> ProjectileModelDict    = new Map<string, Model>();

        static void LoadProjectileMesh(string projectileDir, string nameNoExt)
        {
            string path = projectileDir + nameNoExt;
            try
            {
                var projModel = RootContent.Load<Model>(path);
                ProjectileMeshDict[nameNoExt]  = projModel.Meshes[0];
                ProjectileModelDict[nameNoExt] = projModel;
            }
            catch (Exception e)
            {
                if (e.HResult == -2146233088)
                    return;
                Log.Error(e, $"LoadProjectile {path} failed");
            }
        }

        static void LoadProjectileMeshes()
        {
            const string projectileDir = "Model/Projectiles/";
            LoadProjectileMesh(projectileDir, "projLong");
            LoadProjectileMesh(projectileDir, "projTear");
            LoadProjectileMesh(projectileDir, "projBall");
            LoadProjectileMesh(projectileDir, "torpedo");
            LoadProjectileMesh(projectileDir, "missile");
            LoadProjectileMesh(projectileDir, "spacemine");
            if (GlobalStats.HasMod)
                LoadCustomProjectileMeshes($"{projectileDir}custom");
        }

        static void LoadCustomProjectileMeshes(string modelFolder)
        {
            foreach (FileInfo info in GatherFilesModOrVanilla(modelFolder, "xnb"))
            {
                if (info.Name.Contains("_")) continue;
                string nameNoExt = info.NameNoExt();
                try
                {
                    var projModel = RootContent.Load<Model>(info.CleanResPath());

                    ProjectileMeshDict[nameNoExt] = projModel.Meshes[0];
                    ProjectileModelDict[nameNoExt] = projModel;

                }
                catch (Exception e)
                {
                    Log.Error(e, $"LoadNumberedModels {modelFolder} {nameNoExt} failed");
                }
            }
        }


        static void LoadProjTexts()
        {
            foreach (FileInfo info in GatherFilesUnified("Model/Projectiles/textures", "xnb"))
            {
                var tex = RootContent.Load<Texture2D>(info.CleanResPath());
                ProjTextDict[info.NameNoExt()] = tex;
            }
        }

        static void LoadRandomItems()
        {
            RandomItemsList = LoadEntities<RandomItem>("RandomStuff", "LoadRandomItems");
        }

        static void LoadShipModules()
        {
            foreach (var pair in LoadEntitiesWithInfo<ShipModule_Deserialize>("ShipModules", "LoadShipModules"))
            {
                // Added by gremlin support tech level disabled folder.
                if (pair.Info.DirectoryName?.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) > 0)
                    continue;
                ShipModule_Deserialize data = pair.Entity;

                data.UID = string.Intern(pair.Info.NameNoExt());
                data.IconTexturePath = string.Intern(data.IconTexturePath);
                if (data.WeaponType != null)
                    data.WeaponType = string.Intern(data.WeaponType);

                if (GlobalStats.VerboseLogging)
                {
                    if (ModuleTemplates.ContainsKey(data.UID))
                        Log.Info($"ShipModule UID already found. Conflicting name:  {data.UID}");
                    if (!Localizer.Contains(data.NameIndex))
                        Log.Warning($"{data.UID} missing NameIndex: {data.NameIndex}");

                }
                if (data.IsCommandModule && data.TargetTracking == 0 && data.FixedTracking == 0)
                {
                    data.TargetTracking = (sbyte)((data.XSIZE * data.YSIZE) / 3);
                }

                if (data.IsRotable == null)
                {
                    data.IsRotable = data.XSIZE != data.YSIZE
                        || (data.WeaponType.NotEmpty() && data.ModuleType != ShipModuleType.Turret);
                }

                ModuleTemplates[data.UID] = ShipModule.CreateTemplate(data);

            }

            //Log.Info("Num ShipModuleFlyweight: {0}", ShipModuleFlyweight.TotalNumModules);

            foreach (var entry in ModuleTemplates)
                entry.Value.SetAttributes();
        }


        struct ShipDesignInfo
        {
            public FileInfo File;
            public bool IsPlayerDesign;
            public bool IsReadonlyDesign;
        }

        public static Ship AddShipTemplate(ShipData shipData, bool fromSave, bool playerDesign = false, bool readOnly = false)
        {
            Ship shipTemplate = Ship.CreateShipFromShipData(EmpireManager.Void, shipData, fromSave: fromSave, isTemplate: true);
            if (shipTemplate == null) // happens if module creation failed
                return null;

            shipTemplate.IsPlayerDesign   = playerDesign;
            shipTemplate.IsReadonlyDesign = readOnly;

            lock (ShipsDict)
            {
                ShipsDict[shipData.Name] = shipTemplate;
            }
            return shipTemplate;
        }

        static void LoadShipTemplates(ShipDesignInfo[] shipDescriptors)
        {
            void LoadShips(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    FileInfo info = shipDescriptors[i].File;
                    if (info.DirectoryName?.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) != -1)
                        continue;
                    try
                    {
                        ShipData shipData = ShipData.Parse(info);
                        if (shipData.Role == ShipData.RoleName.disabled)
                            continue;

                        if (info.NameNoExt() != shipData.Name)
                            Log.Warning($"File name '{info.NameNoExt()}' does not match ship name '{shipData.Name}'." +
                                         "\n This can prevent loading of ships that have this filename in the XML :" +
                                        $"\n path '{info.PathNoExt()}'");

                        AddShipTemplate(shipData, fromSave: false,
                                              playerDesign: shipDescriptors[i].IsPlayerDesign,
                                                  readOnly: shipDescriptors[i].IsReadonlyDesign);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"Load.Ship '{info.Name}' failed");
                    }
                }
            }

            Parallel.For(shipDescriptors.Length, LoadShips);
            //LoadShips(0, shipDescriptors.Length); // test without parallel for
        }

        public static Ship GetShipTemplate(string shipName, bool throwIfError = true)
        {
            if (throwIfError)
                return ShipsDict[shipName];
            ShipsDict.TryGetValue(shipName, out Ship ship);
            return ship;
        }

        public static bool ShipTemplateExists(string shipName)
        {
            return ShipsDict.ContainsKey(shipName);
        }

        public static bool GetShipTemplate(string shipName, out Ship template)
        {
            return ShipsDict.TryGetValue(shipName, out template);
        }

        static void CombineOverwrite(Map<string, ShipDesignInfo> designs, FileInfo[] filesToAdd, bool readOnly, bool playerDesign)
        {
            foreach (FileInfo info in filesToAdd)
            {
                string commonIdentifier = info.NameNoExt();
                designs[commonIdentifier] = new ShipDesignInfo
                {
                    File             = info,
                    IsPlayerDesign   = playerDesign,
                    IsReadonlyDesign = readOnly
                };
            }
        }

        // Refactored by RedFox
        static void LoadShipTemplates()
        {
            ShipsDict.Clear();

            var designs = new Map<string, ShipDesignInfo>();
            CombineOverwrite(designs, GatherFilesModOrVanilla("StarterShips", "xml"), readOnly: true, playerDesign: false);
            CombineOverwrite(designs, GatherFilesUnified("SavedDesigns", "xml"), readOnly: true, playerDesign: false);
            CombineOverwrite(designs, GatherFilesUnified("ShipDesigns", "xml"), readOnly: true, playerDesign: false);
            CombineOverwrite(designs, Dir.GetFiles(Dir.StarDriveAppData + "/Saved Designs", "xml"), readOnly: false, playerDesign: true);
            LoadShipTemplates(designs.Values.ToArray());
        }

        static void TechValidator()
        {
            Array<Technology> techs = TechTree.Values.ToArrayList();
            var rootTechs = new Array<Technology>();
            foreach (Technology rootTech in techs)
            {
                if (rootTech.RootNode == 0)
                    continue;
                if (rootTechs.Contains(rootTech))
                    Log.Warning($"Duplicate root tech : '{rootTech}'");
                rootTechs.Add(rootTech);
            }

            void WalkTechTree(Technology technology)
            {
                technology.Unlockable = true;

                foreach (Technology.LeadsToTech leadsTo in technology.LeadsTo)
                {
                    Technology tech = techs.Find(lead => lead.UID == leadsTo.UID);
                    if (tech == null)
                    {
                        Log.Warning($"Technology : '{technology.UID}' can not locate lead to tech : '{leadsTo.UID}'");
                        continue;
                    }

                    tech.ComesFrom.Add(new Technology.LeadsToTech(technology.UID));
                    WalkTechTree(tech);
                }
            }

            foreach (Technology rootTech in rootTechs)
            {
                WalkTechTree(rootTech);
            }
            foreach (Technology notInTree in techs)
            {
                if (notInTree.RootNode != 1 && notInTree.ComesFrom.Count == 0)
                    notInTree.Discovered = false;
            }

            foreach (Technology tech in techs)
            {
                if (!tech.Unlockable)
                    Log.WarningVerbose($"Tech {tech.UID} has no way to unlock! Source: '{tech.DebugSourceFile}'");
            }
        }

        static void LoadTechTree()
        {
            bool modTechsOnly = GlobalStats.HasMod && GlobalStats.ActiveModInfo.clearVanillaTechs;
            Array<InfoPair<Technology>> techs = LoadEntitiesWithInfo<Technology>("Technology", "LoadTechTree", modTechsOnly);

            var duplicateTech = new Map<string, Technology>();

            foreach (InfoPair<Technology> pair in techs)
            {
                Technology tech = pair.Entity;
                tech.DebugSourceFile = pair.Info.RelPath();

                // tech XML-s have their own UID-s but unfortunately there are many mismatches in data files
                // so we only use the XML UID to detect potential duplications
                string xmlUid = tech.UID;

                if (duplicateTech.TryGetValue(xmlUid, out Technology previous))
                    Log.Warning($"Possibly duplicate tech '{xmlUid}' !\n  first: {previous.DebugSourceFile}\n  second: {tech.DebugSourceFile}");
                else
                    duplicateTech.Add(xmlUid, tech);

                // CA relies on tech XML filenames for the tech UID
                string fileUid = string.Intern(pair.Info.NameNoExt());
                tech.UID = fileUid;
                TechTree[fileUid] = tech;

                // categorize extra techs
                Technology.GetTechnologyTypesFromUnlocks(tech, tech.TechnologyTypes);
            }

            foreach (Technology tech in TechTree.Values)
                tech.ResolveLeadsToTechs();
        }

        static void LoadToolTips()
        {
            foreach (var tooltips in LoadEntities<Tooltips>("Tooltips", "LoadToolTips"))
            {
                ToolTips.Capacity = tooltips.ToolTipsList.Count;
                foreach (ToolTip tip in tooltips.ToolTipsList)
                {
                    int idx = tip.TIP_ID - 1;
                    while (ToolTips.Count <= idx) ToolTips.Add(null); // sparse List
                    ToolTips[idx] = tip;
                }
            }
        }

        static readonly HashSet<int> MissingTooltips = new HashSet<int>();
        public static ToolTip GetToolTip(int tipId)
        {
            if (tipId > ToolTips.Count)
            {
                if (!MissingTooltips.Contains(tipId))
                {
                    MissingTooltips.Add(tipId);
                    Log.Warning($"Missing ToolTip: {tipId}");
                }
                return null;
            }
            return ToolTips[tipId - 1];
        }

        // Refactored by RedFox, gets a new weapon instance based on weapon UID
        public static Weapon CreateWeapon(string uid)
        {
            Weapon template = WeaponsDict[uid];
            return template.Clone();
        }

        // WARNING: DO NOT MODIFY this Weapon instance! (wish C# has const refs like C++)
        public static Weapon GetWeaponTemplate(string uid)
        {
            return WeaponsDict[uid];
        }

        static void LoadWeapons() // Refactored by RedFox
        {
            bool modTechsOnly = GlobalStats.HasMod && GlobalStats.ActiveModInfo.clearVanillaWeapons;
            foreach (var pair in LoadEntitiesWithInfo<Weapon>("Weapons", "LoadWeapons", modTechsOnly))
            {
                Weapon wep           = pair.Entity;
                wep.UID              = string.Intern(pair.Info.NameNoExt());
                WeaponsDict[wep.UID] = wep;
                wep.InitializeTemplate();
            }
        }

        static void LoadShipRoles()
        {
            ShipRoles.Clear();
            foreach (ShipRole shipRole in LoadEntities<ShipRole>("ShipRoles", "LoadShipRoles"))
            {
                if (Enum.TryParse(shipRole.Name, out ShipData.RoleName key))
                    ShipRoles[key] = shipRole;
            }
            if (ShipRoles.Count == 0)
                Log.Error("Failed to load any ShipRoles! Make sure Content/ShipRoles/*.xml exist!");
        }

        static void LoadPlanetEdicts()
        {
            foreach (var planetEdict in LoadEntities<PlanetEdict>("PlanetEdicts", "LoadPlanetEdicts"))
                PlanetaryEdicts[planetEdict.Name] = planetEdict;
        }


        static readonly Map<string, EconomicResearchStrategy> EconStrategies = new Map<string, EconomicResearchStrategy>();
        public static EconomicResearchStrategy GetEconomicStrategy(string name)
        {
            return EconStrategies[name];
        }
        static void LoadEconomicResearchStrategies()
        {
            foreach (var pair in LoadEntitiesWithInfo<EconomicResearchStrategy>("EconomicResearchStrategy", "LoadEconResearchStrats"))
            {
                // the story here: some mods have bugged <Name> refs, so we do manual
                // hand holding to fix their bugs...
                pair.Entity.Name = pair.Info.NameNoExt();
                EconStrategies[pair.Entity.Name] = pair.Entity;
            }
        }


        static readonly Map<SunZone, Array<PlanetCategory>> ZoneDistribution = new Map<SunZone, Array<PlanetCategory>>();
        static Array<PlanetType> PlanetTypes;
        static Map<int, PlanetType> PlanetTypeMap;

        public static PlanetType RandomPlanet() => RandomMath.RandItem(PlanetTypes);

        public static PlanetType RandomPlanet(PlanetCategory category)
        {
            return RandomMath.RandItem(PlanetTypes.Filter(p => p.Category == category));
        }

        public static PlanetType PlanetOrRandom(int planetId)
        {
            return PlanetTypeMap.TryGetValue(planetId, out PlanetType type)
                 ? type : RandomPlanet();
        }
        public static PlanetType Planet(int planetId) => PlanetTypeMap[planetId];

        static void LoadPlanetTypes()
        {
            using (var parser = new YamlParser("PlanetTypes.yaml"))
            {
                PlanetTypes = parser.DeserializeArray<PlanetType>();
            }

            PlanetTypes.Sort(p => p.Id);
            PlanetTypeMap = new Map<int, PlanetType>(PlanetTypes.Count);
            foreach (PlanetType type in PlanetTypes)
                PlanetTypeMap[type.Id] = type;
        }

        static void LoadSunZoneData()
        {
            using (var parser = new YamlParser("SunZoneData.yaml"))
            {
                var zones = parser.DeserializeArray<SunZoneData>();
                ZoneDistribution[SunZone.Near]    = SunZoneData.CreateDistribution(zones, SunZone.Near);
                ZoneDistribution[SunZone.Habital] = SunZoneData.CreateDistribution(zones, SunZone.Habital);
                ZoneDistribution[SunZone.Far]     = SunZoneData.CreateDistribution(zones, SunZone.Far);
                ZoneDistribution[SunZone.VeryFar] = SunZoneData.CreateDistribution(zones, SunZone.VeryFar);
            }
        }

        public static PlanetCategory RandomPlanetCategoryFor(SunZone sunZone)
        {
            return ZoneDistribution[sunZone].RandItem();
        }

        // Added by RedFox
        static void LoadBlackboxSpecific()
        {
            TryDeserialize("HostileFleets/HostileFleets.xml",    ref HostileFleets);
            TryDeserialize("ShipNames/ShipNames.xml",            ref ShipNames);
            TryDeserialize("MainMenu/MainMenuShipList.xml",      ref MainMenuShipList);
            TryDeserialize("AgentMissions/AgentMissionData.xml", ref AgentMissionData);

            FileInfo[] sfxFiles = GatherFilesUnified("SoundEffects", "xnb");
            if (sfxFiles.Length != 0)
            {
                SoundEffectDict = new Map<string, SoundEffect>();
                foreach (FileInfo info in sfxFiles)
                {
                    var se = RootContent.Load<SoundEffect>(info.CleanResPath());
                    SoundEffectDict[info.NameNoExt()] = se;
                }
            }
        }

        public static bool GetModSoundEffect(string cueName, out SoundEffect sfx)
        {
            sfx = null;
            return SoundEffectDict?.TryGetValue(cueName, out sfx) == true;
        }

        public static Video LoadVideo(GameContentManager content, string videoPath)
        {
            var video = content.Load<Video>("Video/" + videoPath);
            if (video != null) return video;
            Log.Error($"Video path {videoPath} found no file");//Loading 2
            return content.Load<Video>("Video/Loading 2");

        }
    }
}