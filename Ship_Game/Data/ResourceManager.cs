using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Yaml;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.SpriteSystem;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.AI;
using Ship_Game.Data.Mesh;
using Ship_Game.Ships.Legacy;
using Ship_Game.Universe;
using Ship_Game.Utils;

#pragma warning disable CA2237, RCS1194 // Mark ISerializable types with serializable

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
        static readonly Map<string, SubTexture> Textures = new();

        public static Map<string, Technology> TechTree = new();
        static readonly Map<string, ShipModule> ModuleTemplates = new();
        public static Array<Encounter> Encounters = new();
        public static Map<string, Building> BuildingsDict = new();
        public static Array<Good> TransportableGoods = new();
        public static Map<string, Texture2D> ProjTextDict = new();

        public static RandomItem[] RandomItemsList = Empty<RandomItem>.Array;

        public static Map<string, Artifact> ArtifactsDict = new();
        public static Map<string, ExplorationEvent> EventsDict = new();

        public static ShipNames ShipNames = new();
        public static AgentMissionData AgentMissionData = new();
        public static Map<RoleName, ShipRole> ShipRoles = new();
        public static Map<string, HullBonus> HullBonuses = new();

        static RacialTraits RacialTraits;
        static DiplomaticTraits DiplomacyTraits;

        /// <summary>
        /// Holds all metadata about Planets and how to Render them
        /// </summary>
        public static PlanetTypes Planets;

        public static SubTexture Blank;

        // This 1x1 White pixel texture is used for drawing Lines and other primitives
        public static Texture2D WhitePixel;

        /// <summary>
        /// Whether to print out verbose loading information. This is disabled in unit tests.
        /// </summary>
        public static bool Verbose { get; private set; } = true;

        /// <summary>
        /// This is where core game content is found. NOT Mod content.
        /// Ex: "C:/Projects/BlackBox/stardrive/Content/"
        /// </summary>
        static string ContentDirectory;

        /// <summary>
        /// This is where mod game content can be found.
        /// Ex: "C:/Projects/BlackBox/stardrive/Mods/Combined Arms/"
        /// (mods don't have a /Content/ dir, they _are_ the content dir)
        /// </summary>
        static string ModContentDirectory;

        // All references to Game1.Instance.Content were replaced by this property
        public static GameContentManager RootContent => GameBase.Base.Content;

        public static void InitContentDir()
        {
            ContentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Content/").Replace('\\', '/');
            ModContentDirectory = GlobalStats.ModPath;
            if (GlobalStats.HasMod && Directory.Exists(ModContentDirectory + "Content"))
            {
                throw new InvalidDataException($"Invalid Mod file tree: {ModContentDirectory+"Content"} must not exist, because the {ModContentDirectory} already is the Content directory!");
            }
        }

        public static Technology Tech(string techUid) => TechTree[techUid];
        public static bool TryGetTech(string techUid, out Technology t) => TechTree.TryGetValue(techUid, out t);
        public static IReadOnlyCollection<Technology> TechsList => TechTree.Values;

        public static ExplorationEvent Event(string eventName)
        {
            if (EventsDict.TryGetValue(eventName, out ExplorationEvent events))
                return events;
            Log.WarningWithCallStack($"Event '{eventName}' not found. Contact mod creator.");
            return EventsDict["Default"];
        }

        /// <summary>
        /// No Error logging on eventDates. returns an event named for the dateString passed.
        /// Else returns null.
        /// </summary>
        public static ExplorationEvent EventByDate(float starDate) =>
            EventsDict.TryGetValue(starDate.ToString(CultureInfo.InvariantCulture), out ExplorationEvent events) ? events : null;

        // This is used for lazy-loading content triggers
        public static int ContentId { get; private set; }

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

        public static void LoadItAll(ScreenManager manager, ModEntry mod)
        {
            Stopwatch s = Stopwatch.StartNew();
            try
            {
                WaitForExit();
                LoadAllResources(manager, mod);
                if (mod != null) // if Mod load succeeded, then:
                    GlobalStats.SaveActiveMod();
            }
            catch (Exception ex)
            {
                if (mod == null)
                    throw; // vanilla load failed, fatally
                Log.ErrorDialog(ex, $"Mod {GlobalStats.ModName} load failed. Disabling mod and loading vanilla.", 0);
                WaitForExit();
                GlobalStats.ClearActiveMod();
                UnloadAllData(manager);
                LoadAllResources(manager, null);
            }
            if (Verbose) Log.Write($"LoadItAll elapsed: {s.Elapsed.TotalSeconds}s");
        }

        static readonly Array<KeyValuePair<string, double>> PerfProfile = new();
        static readonly Stopwatch PerfStopwatch = new();

        static void BeginPerfProfile()
        {
            PerfProfile.Clear();
            PerfStopwatch.Start();
        }
        static void Profiled(string uniqueName, Action body)
        {
            if (Verbose)
            {
                var sw = Stopwatch.StartNew();
                body();
                double elapsed = sw.Elapsed.TotalSeconds;
                PerfProfile.Add(new(uniqueName, elapsed));
            }
            else
            {
                body();
            }
        }
        static void Profiled(Action body) => Profiled(body.Method.Name, body);
        static void EndPerfProfile()
        {
            if (!Verbose)
                return;

            PerfStopwatch.Stop();
            double elapsed = PerfStopwatch.Elapsed.TotalSeconds;
            PerfProfile.Sort(kv => kv.Value);
            PerfProfile.Reverse();

            Log.Write(ConsoleColor.Cyan, $"Load Total Elapsed: {elapsed*1000:0.0}ms");
            foreach (var kv in PerfProfile)
            {
                double percent = (kv.Value / elapsed) * 100.0;
                Log.Write(ConsoleColor.Cyan, $"{kv.Key,-20} {kv.Value*1000:0.0}ms  {percent:0.00}%");
            }

            PerfProfile.Clear();
        }

        static void LoadAllResources(ScreenManager manager, ModEntry mod)
        {
            if (mod != null)
                GlobalStats.SetActiveModNoSave(mod);
            else
                GlobalStats.ClearActiveMod();

            Log.ConfigureStatsReporter();
            LoadContent();

            LoadGraphicsResources(manager);
            HelperFunctions.CollectMemory();
        }

        static void LoadContent(bool loadShips = true)
        {
            InitContentDir();
            CreateCoreGfxResources();
            if (Verbose)
            {
                if (GlobalStats.HasMod)
                    Log.Write($"Load ModPath:{ModContentDirectory}  Mod:{GlobalStats.ModName}  ModVersion:{GlobalStats.ActiveMod.Mod.Version}  GameVersion:{GlobalStats.ExtendedVersion}");
                else
                    Log.Write($"Load Vanilla GameVersion:{GlobalStats.ExtendedVersion}");
            }

            BeginPerfProfile();
            Profiled("LoadLanguage", () => LoadLanguage(GlobalStats.Language)); // must be before LoadFonts
            Profiled(LoadHullBonuses);
            Profiled(LoadHulls); // we need Hull Data for main menu ship
            Profiled(LoadEmpires); // Hotspot #2 187.4ms  8.48%

            Profiled(LoadTroops);
            Profiled(LoadWeapons);
            Profiled(LoadShipModules); // Hotspot #4 124.9ms  5.65%
            Profiled(LoadGoods);
            Profiled(LoadShipRoles);
            if (loadShips)
            {
                Profiled(LoadAllShipDesigns); // Hotspot #1  502.9ms  22.75%
                Profiled(CheckForRequiredShipDesigns);
            }
            Profiled(LoadBuildings);
            Profiled(LoadRandomItems);
            Profiled(LoadDialogs);
            Profiled(LoadTechTree);
            Profiled(LoadEncounters);
            Profiled(LoadExpEvents);

            Profiled(LoadArtifacts);
            Profiled("PlanetTypes", () => PlanetTypes.LoadPlanetTypes(RootContent));
            Profiled(LoadSunZoneData);
            Profiled(LoadBuildRatios);
            Profiled(LoadEcoResearchStrats);
            Profiled(LoadBlackboxSpecific);
        }

        public static void LoadGraphicsResources(ScreenManager manager)
        {
            manager.UpdateGraphicsDevice();

            ++ContentId; // LoadContent will see a new content id

            Profiled(LoadTextureAtlases);
            Profiled(LoadNebulae);
            Profiled(LoadStars);
            Profiled(LoadFlagTextures);

            Profiled(LoadJunk);
            Profiled(LoadAsteroids);
            Profiled(LoadProjectileTextures);
            Profiled(LoadProjectileMeshes);
            // Hotspot #3 174.8ms  7.91%
            Profiled("LoadSunTypes", () => SunType.LoadSunTypes(loadIcons: !GlobalStats.IsUnitTest));
            Profiled("LoadBeamFX", () =>
            {
                Beam.BeamEffect = RootContent.Load<Effect>("Effects/BeamFX");
                Blank = Texture("blank");
            });
            Profiled("LoadFonts", () => Fonts.LoadFonts(RootContent, Localizer.Language));

            // Load non-critical resources:
            void LoadNonCritical()
            {
                Log.Write("Load non-critical resources");
                Profiled("LoadExplosions", () => ExplosionManager.LoadExplosions(RootContent));
                Profiled("LoadNonEssential", () => LoadNonEssentialAtlases(BackgroundLoad));
                Log.Write("Finished loading non-critical resources");
                EndPerfProfile();
            }
            
            GameLoadingScreen.SetStatus("LoadNonCritical");
            //LoadNonCritical();
            BackgroundLoad = Parallel.Run(LoadNonCritical);
        }

        public static void UnloadGraphicsResources(ScreenManager manager)
        {
            WaitForExit();
            manager.ResetHotLoadTargets();

            Mem.Dispose(ref Nebulae);
            // TextureBindings were disposed by Nebulae.Dispose()
            SmallNebulae.Clear();
            MedNebulae.Clear();
            BigNebulae.Clear();

            Mem.Dispose(ref SmallStars);
            Mem.Dispose(ref MediumStars);
            Mem.Dispose(ref LargeStars);
            Mem.Dispose(ref FlagTextures);

            Arcs.Clear();
            ProjTextDict.ClearAndDispose();
            
            // StaticMeshes are loaded through GameContent, so they WILL be auto-disposed
            JunkModels.Clear();
            AsteroidModels.Clear();
            ProjectileMeshes.Clear();

            SunType.Unload();
            Beam.BeamEffect = null;
            Mem.Dispose(ref WhitePixel);

            // Texture caches MUST be cleared before triggering content reload!
            Textures.Clear();

            // This is a destructive operation that invalidates ALL game resources!
            // @note It HAS to be done after clearing all ResourceManager texture caches!
            manager.UnsafeUnloadAllGameContent();
        }

        public static void UnloadAllData(ScreenManager manager)
        {
            WaitForExit();

            TroopsDict.Clear();
            TroopsList.Clear();
            TroopsDictKeys = Empty<string>.Array;

            BuildingsDict.Clear();
            BuildingsById.Clear();

            UnloadShipDesigns();

            foreach (var m in ModuleTemplates)
                m.Value.Dispose();
            ModuleTemplates.Clear();

            HullBonuses.Clear();
            HullsDict.Clear();
            HullsList.Clear();

            TechTree.Clear();
            ArtifactsDict.Clear();
            TransportableGoods.Clear();
            Encounters.Clear();
            EventsDict.Clear();
            RandomItemsList = Empty<RandomItem>.Array;
            WeaponsDict.Clear();

            ShipNames.Clear();

            EconStrategies.Clear();
            ZoneDistribution.Clear();
            BuildRatios.Clear();

            Mem.Dispose(ref Planets);

            DiplomacyDialogs.Clear();
            Empires.Clear();
            MajorEmpires.Clear();
            MinorEmpires.Clear();

            RacialTraits = null;
            DiplomacyTraits = null;
            AgentMissionData = new();
            EmpireHullBonuses.Clear();

            UnloadGraphicsResources(manager);
        }

        static FileInfo ModInfo(string file) => new FileInfo( ModContentDirectory + file );
        public static FileInfo ContentInfo(string file) => new FileInfo( ContentDirectory + file );

        // Gets FileInfo for Mod or Vanilla file. Mod file is checked first
        // Example relativePath: "Textures/myAtlas.xml"
        public static FileInfo GetModOrVanillaFile(string relativePath)
        {
            FileInfo info;
            if (GlobalStats.HasMod)
            {
                info = ModInfo(relativePath);
                if (info.Exists)
                    return info;
            }
            info = ContentInfo(relativePath);
            return info.Exists ? info : null;
        }

        // This first tries to deserialize from Mod folder and then from Content folder
        static T TryDeserialize<T>(string file) where T : class
        {
            FileInfo info = null;
            if (GlobalStats.HasMod)
            {
                info = ModInfo(file);
            }
            if (info == null || !info.Exists)
            {
                info = ContentInfo(file);
                if (!info.Exists)
                    return null;
            }

            try
            {
                using Stream stream = info.OpenRead();
                return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
            }
            catch (Exception e)
            {
                throw new($"Failed to parse XML: {info.RelPath()}", e);
            }
        }

        // The entity value is assigned only IF file exists and Deserialize succeeds
        static void TryDeserialize<T>(string file, ref T entity) where T : class
        {
            var result = TryDeserialize<T>(file);
            if (result != null) entity = result;
        }

        class UniqueFileSet
        {
            readonly string RootPath;
            public readonly Map<string, FileInfo> Files = new(StringComparer.OrdinalIgnoreCase); // must be case insensitive!
            public UniqueFileSet(string rootDirName, string subDir, string pattern, SearchOption search, bool byFileName)
            {
                RootPath = Path.GetFullPath(rootDirName);
                FileInfo[] files = Dir.GetFiles(Path.Combine(RootPath, subDir), pattern, search);
                foreach (FileInfo file in files) TryAddOrReplaceFile(file, byFileName);
            }
            // if `byFileName` is true, then always prefer the newer file: this ensures we discard any stale files
            void TryAddOrReplaceFile(FileInfo file, bool byFileName)
            {
                string name = byFileName ? file.Name : file.FullName.Substring(RootPath.Length);
                if (!byFileName || // if fullPath, then always set the value
                    !Files.TryGetValue(name, out FileInfo existing)) // or if it doesn't exist already
                {
                    Files[name] = file;
                }
                else if (file.LastWriteTimeUtc > existing.LastWriteTimeUtc) // or if it exists, then file must be newer)
                {
                    Log.Warning($"Using newer source '{file.RelPath()}' instead of '{existing.RelPath()}'");
                    Files[name] = file;
                }
                // else: file is older
            }
        }

        /// <summary>
        /// This gathers an union of Mod and Vanilla files. Any vanilla file is replaced by mod files.
        /// It also performs basic duplicate elimination by file.Name, if `alwaysUnify` is set or if ext==xml|hull|design
        /// </summary>
        /// <param name="dir">Directory of the search</param>
        /// <param name="ext">Extension of files to gather</param>
        /// <param name="recursive">Find files recursively</param>
        /// <param name="alwaysUnify">Force file unification by filename, ignore extension restrictions xml|hull|design</param>
        /// <param name="modOnly"></param>
        /// <returns></returns>
        public static FileInfo[] GatherFilesUnified(string dir, string ext,
                                                    bool recursive = true,
                                                    bool alwaysUnify = false,
                                                    bool modOnly = false)
        {
            string pattern = "*." + ext;
            SearchOption search = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // For XML/hull/design files we allow subfolders such as Buildings/Colonization/Aeroponics.xml
            // Which means the unification has to be done via `file.Name` such as: "Aeroponics.xml"
            // For other files such as XNB textures, we use the full path normalized to relative content path
            // such as: "Mods/MyMod/Textures/TechIcons/Aeroponics.xnb" --> "Textures/TechIcons/Aeroponics.xnb"
            bool byFileName = alwaysUnify || ext is "xml" or "hull" or "design";

            if (!GlobalStats.HasMod) // only gather vanilla files
            {
                UniqueFileSet vanilla = new("Content/", dir, pattern, search, byFileName);
                return vanilla.Files.Values.ToArr();
            }
            else if (modOnly) // only mod files
            {
                UniqueFileSet mod = new(ModContentDirectory, dir, pattern, search, byFileName);
                return mod.Files.Values.ToArr();
            }
            else // gather unified, where mod files overwrite vanilla files
            {
                UniqueFileSet vanilla = new("Content/", dir, pattern, search, byFileName);
                UniqueFileSet mod = new(ModContentDirectory, dir, pattern, search, byFileName);

                // base files are always vanilla, and mod files overwrite any conflicting values
                Map<string, FileInfo> uniqueFiles = vanilla.Files;
                foreach (KeyValuePair<string, FileInfo> kv in mod.Files)
                    uniqueFiles[kv.Key] = kv.Value;
                return uniqueFiles.Values.ToArr();
            }
        }

        // This tries to gather only mod files, or only vanilla files
        // No union/mix is made
        public static FileInfo[] GatherFilesModOrVanilla(string dir, string ext)
        {
            if (!GlobalStats.HasMod) return Dir.GetFiles("Content/" + dir, ext);
            FileInfo[] files = Dir.GetFiles(ModContentDirectory + dir, ext);
            return files.Length != 0 ? files : Dir.GetFiles("Content/" + dir, ext);
        }

        static void DebugResourceLoading(string name, FileInfo[] files)
        {
            if (GlobalStats.DebugResourceLoading)
            {
                foreach (FileInfo file in files)
                    Log.Write($"{name}: {file.FullName}");
            }
        }
        
        // Added by RedFox - Generic entity loading, less typing == more fun
        static T LoadEntity<T>(XmlSerializer s, FileInfo info, string id) where T : class
        {
            GameLoadingScreen.SetStatus(id, info.RelPath());
            using FileStream stream = info.OpenRead();
            return (T)s.Deserialize(stream);
        }

        // Loads a list of entities in a folder
        static T[] LoadEntities<T>(FileInfo[] files, string id) where T : class
        {
            DebugResourceLoading(id, files);
            var s = new XmlSerializer(typeof(T));
            return Parallel.Select(files, file => LoadEntity<T>(s, file, id));
        }

        static T[] LoadEntities<T>(string dir, string id, bool modOnly = false) where T : class
        {
            return LoadEntities<T>(GatherFilesUnified(dir, "xml", modOnly: modOnly), id);
        }

        class InfoPair<T> where T : class
        {
            public readonly FileInfo Info;
            public readonly T Entity;
            public InfoPair(FileInfo info, T entity) { Info = info; Entity = entity; }
            public void Deconstruct(out FileInfo file, out T entity)
            {
                file = Info; entity = Entity;
            }
        }

        // special `modOnly` flag to only retrieve mod files
        static InfoPair<T>[] LoadEntitiesWithInfo<T>(string dir, string id, bool modOnly = false) where T : class
        {
            var files = modOnly ? Dir.GetFiles(ModContentDirectory + dir, "xml") : GatherFilesUnified(dir, "xml");
            DebugResourceLoading(id, files);
            var s = new XmlSerializer(typeof(T));
            return Parallel.Select(files, file => new InfoPair<T>(file, LoadEntity<T>(s, file, id)));
        }
        
        class UIDEntityInfo<T> : InfoPair<T> where T : class
        {
            public readonly string UID;
            public UIDEntityInfo(FileInfo info, T entity, string uid) : base(info, entity)
            {
                UID = uid;
            }
            public void Deconstruct(out FileInfo file, out T entity, out string uid)
            {
                file = Info; entity = Entity; uid = UID;
            }
        }

        /// <summary>
        /// Removes any older duplicate entities by using the FileName as an Unique Identifier
        /// </summary>
        /// <param name="dir">Game directory where to search for items</param>
        /// <param name="id">Debug ID of the LoadEntities</param>
        /// <param name="getUid">Lambda expression for getting the default deserialized UID</param>
        /// <param name="modOnly">If true, only files from ModPath will be loaded</param>
        /// <param name="overwriteUid">if true, then UID will be overwritten by file UID</param>
        static UIDEntityInfo<T>[] LoadUniqueEntitiesByFileUID<T>(
            string dir, string id,
            Func<T, string> getUid,
            bool modOnly = false,
            bool overwriteUid = true) where T : class
        {
            InfoPair<T>[] entities = LoadEntitiesWithInfo<T>(dir, id, modOnly:modOnly);
            Map<string, UIDEntityInfo<T>> unique = new(StringComparer.OrdinalIgnoreCase);

            foreach ((FileInfo file, T entity) in entities)
            {
                string uid = string.Intern(getUid(entity));

                // check if UID matches filename
                string fileUid = string.Intern(file.NameNoExt());
                if (uid != fileUid)
                {
                    string overwriting = overwriteUid ? $", overwriting with UID='{fileUid}'" : "";
                    Log.Warning($"{id} UID='{uid}' mismatches FileName='{file.Name}' at {file.RelPath()}{overwriting}");

                    if (overwriteUid) // forcefully fix the UID if allowed, this is a requirement for a lot of templates and prevents duplicate UID-s
                        uid = fileUid;
                }
                
                if (unique.TryGetValue(uid, out UIDEntityInfo<T> previous))
                {
                    Log.Warning($"{id} duplicate UID='{uid}'\n  first: '{previous.Info.RelPath()}'\n  second: '{file.RelPath()}'");
                    // new file is older than previous file? then we discard it completely
                    if (file.LastWriteTimeUtc < previous.Info.LastWriteTimeUtc)
                    {
                        Log.Warning($"{id} discarded old UID='{uid}' '{file}'");
                        continue;
                    }
                }
                
                unique[uid] = new(file, entity, uid); // add new or replace existing older one
            }

            return unique.Values.ToArr();
        }

        static readonly Map<string, Troop> TroopsDict = new();
        static readonly Array<Troop> TroopsList = new();
        static string[] TroopsDictKeys = Empty<string>.Array;

        public static IReadOnlyList<string> TroopTypes => TroopsDictKeys;

        public static Troop[] GetTroopTemplatesFor(Empire e)
            => TroopsList.Filter(t => e.WeCanBuildTroop(t.Name)).Sorted(t => t.ActualCost(e));
        
        public static bool GetTroopTemplate(string troopType, out Troop troop)
        {
            return TroopsDict.TryGetValue(troopType, out troop);
        }

        public static Troop GetTroopTemplate(string troopType)
        {
            if (GetTroopTemplate(troopType, out Troop troop))
                return troop;

            Log.WarningWithCallStack($"Troop {troopType} Template Not found");
            return TroopsList.First;
        }

        public static Troop CreateTroop(string troopType, Empire forOwner)
        {
            TryCreateTroop(troopType, forOwner, out Troop troop);
            return troop;
        }

        public static bool TryCreateTroop(string troopType, Empire forOwner, out Troop troop)
        {
            if (!GetTroopTemplate(troopType, out Troop template))
            {
                Log.WarningWithCallStack($"Troop {troopType} Template Not found");
                troop = null;
                return false;
            }

            troop = template.Clone();
            if (troop.StrengthMax <= 0)
                troop.StrengthMax = troop.Strength;

            if (forOwner != null)
            {
                troop.WhichFrame = forOwner.Random.Int(1, troop.num_idle_frames - 1);
                troop.SetOwner(forOwner);
                troop.HealTroop(troop.ActualStrengthMax);
                troop.Level = forOwner.data.MinimumTroopLevel;
            }
            return troop != null;
        }

        static void LoadTroops()
        {
            TroopsDict.Clear();
            TroopsList.Clear();
            foreach (var pair in LoadEntitiesWithInfo<Troop>("Troops", "LoadTroops"))
            {
                Troop troop = pair.Entity;
                troop.Name = pair.Info.NameNoExt();
                troop.Type = pair.Info.NameNoExt();
                TroopsDict[troop.Name] = troop;
                TroopsList.Add(troop);

                if (troop.StrengthMax <= 0)
                    troop.StrengthMax = troop.Strength;
            }
            TroopsDictKeys = TroopsDict.Keys.ToArr();
        }

        public static MarkovNameGenerator GetRandomNames(Empire empire)
        {
            return GetNameGenerator($"NameGenerators/spynames_{empire?.PortraitName}.txt");
        }

        public static MarkovNameGenerator GetNameGenerator(string relativePath)
        {
            var nameFile = GetModOrVanillaFile(relativePath);
            if (nameFile == null) return null;
            return new MarkovNameGenerator(nameFile.OpenText().ReadToEnd());
        }

        static void DeleteShipFromDir(string dir, string shipName)
        {
            var info = new FileInfo(Path.Combine(dir, shipName + ".design"));
            if (info.Exists)
            {
                info.Delete();
                Log.Info($"Deleted {info.FullName}");
            }
        }

        // Refactored by RedFox
        public static void DeleteShip(UniverseState us, string shipName)
        {
            string appData = Dir.StarDriveAppData;
            DeleteShipFromDir(appData + "/Saved Designs", shipName);
            DeleteShipFromDir(appData + "/WIP", shipName);

            IShipDesign design = Ships.GetDesign(shipName, throwIfError: false);
            Ships.Delete(shipName);

            if (design != null)
            {
                foreach (Empire e in us.Empires)
                    if (e.RemoveBuildableShip(design))
                        e.UpdateShipsWeCanBuild();
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////

        public static Array<FileInfo> GetAllXnbModelFiles(string folder)
        {
            return RootContent.RawContent.GetAllXnbModelFiles(folder);
        }

        //////////////////////////////////////////////////////////////////////////////////////////

        public static void CreateCoreGfxResources()
        {
            WhitePixel = new Texture2D(RootContent.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            WhitePixel.SetData(new []{ Color.White });
        }

        public static SubTexture ErrorTexture   => TextureOrNull("NewUI/x_red");
        public static SubTexture InvalidTexture => TextureOrNull("NewUI/invalid");

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

            SubTexture errorTexture = ErrorTexture;
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
                return new SubTexture(texturePath, proj, texturePath);
            return RootContent.DefaultTexture();
        }

        public static FileInfo[] GatherTextureFiles(string dir, bool recursive)
        {
            string[] extensions = { "png", "gif", "jpg", "xnb", "dds" };
            var allFiles = new Array<FileInfo>();
            foreach (string ext in extensions)
            {
                allFiles.AddRange(GatherFilesUnified(dir, ext, recursive: recursive));
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
            "Beams", "Ships", "Model/Projectiles/textures"
        });
        // For these Atlases, quality suffers too much, so compression is forbidden
        public static readonly HashSet<string> AtlasNoCompressFolders = new HashSet<string>(new []
        {
            "NewUI", "EmpireTopBar", "Popup", "ResearchMenu"
        });

        static TextureAtlas LoadAtlas(string folder)
        {
            var atlas = RootContent.LoadTextureAtlas(folder, useAssetCache: true);
            if (atlas == null) Log.Error($"LoadAtlas {folder} failed");
            return atlas;
        }

        // This is just to speed up initial atlas generation and avoid noticeable framerate hiccups
        static void LoadTextureAtlases()
        {
            // these are essential for main menu, so we load them as blocking
            string[] atlases =
            {
                "Textures",
                "Textures/GameScreens", "Textures/MMenu",
                "Textures/EmpireTopBar", "Textures/NewUI"
            };
            Parallel.ForEach(atlases, atlas => LoadAtlas(atlas));
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
                "Textures/Suns",
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
                "Textures/OrderButtons",
            };

            Parallel.ForEach(atlases, atlas =>
            {
                if (task?.IsCancelRequested != true)
                    LoadAtlas(atlas);
            });
            GameLoadingScreen.SetStatus("Click to Continue");
            Log.Write($"LoadAtlases (background) elapsed:{s.Elapsed.TotalMilliseconds}ms  Total Parallel Tasks: {Parallel.PoolSize}");
        }

        public static ShipModule GetModuleTemplate(string uid) => ModuleTemplates[uid];
        public static bool GetModuleTemplate(string uid, out ShipModule module) => ModuleTemplates.Get(uid, out module);
        public static bool ModuleExists(string uid) => ModuleTemplates.ContainsKey(uid);
        public static IReadOnlyDictionary<string, ShipModule> ShipModules => ModuleTemplates;
        public static ICollection<ShipModule> ShipModuleTemplates => ModuleTemplates.Values;

        public static RacialTraits RaceTraits
            => RacialTraits ??= TryDeserialize<RacialTraits>("RacialTraits/RacialTraits.xml");

        public static DiplomaticTraits DiplomaticTraits
            => DiplomacyTraits ??= TryDeserialize<DiplomaticTraits>("Diplomacy/DiplomaticTraits.xml");

        public static SolarSystemData LoadSolarSystemData(string homeSystemName)
            => TryDeserialize<SolarSystemData>("SolarSystems/" + homeSystemName + ".xml");

        public static SolarSystemData[] LoadRandomSolarSystems()
        {
            return LoadEntities<SolarSystemData>(GatherFilesModOrVanilla("SolarSystems/Random", "xml"), "LoadSolarSystems");
        }

        public static Texture2D LoadRandomLoadingScreen(RandomBase random, GameContentManager transientContent)
        {
            FileInfo[] files = GatherFilesModOrVanilla("LoadingScreen", "xnb");

            FileInfo file = random.Item(files);
            return transientContent.LoadTexture(file);
        }

        // advice is temporary and only sticks around while loading
        public static string LoadRandomAdvice(RandomBase random)
        {
            string adviceFile = "Advice/" + GlobalStats.Language + "/Advice.xml";

            var adviceList = TryDeserialize<Array<string>>(adviceFile);
            return adviceList?[random.InRange(adviceList.Count)] ?? "Advice.xml missing";
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

        static readonly Array<Building> BuildingsById = new();

        public static bool BuildingExists(int buildingId) => 0 < buildingId && buildingId < BuildingsById.Count;

        public static Building GetBuildingTemplate(string whichBuilding)
        {
            if (BuildingsDict.TryGetValue(whichBuilding, out Building template))
                return template;
            throw new($"No building template with UID='{whichBuilding}'");
        }

        public static Building GetBuildingTemplate(int buildingId)
        {
            if (!BuildingExists(buildingId))
                throw new($"No building template with BID={buildingId}");
            return BuildingsById[buildingId];
        }

        public static Building CreateBuilding(Planet p, string whichBuilding) => CreateBuilding(p, GetBuildingTemplate(whichBuilding));
        public static Building CreateBuilding(Planet p, int buildingId) => CreateBuilding(p, GetBuildingTemplate(buildingId));

        public static bool GetBuilding(string whichBuilding, out Building b) => BuildingsDict.Get(whichBuilding, out b);
        public static bool GetBuilding(int buildingId, out Building b)
        {
            if (!BuildingExists(buildingId)) { b = null; return false; }
            else { b = BuildingsById[buildingId]; return true; }
        }

        static void LoadBuildings() // Refactored by RedFox
        {
            bool modOnly = GlobalStats.HasMod && !GlobalStats.Defaults.Mod.UseVanillaBuildings;

            UIDEntityInfo<Building>[] buildings = LoadUniqueEntitiesByFileUID<Building>(
                "Buildings", "LoadBuildings", b => b.Name, modOnly:modOnly, overwriteUid:false
            );

            BuildingsById.Resize(buildings.Length + 1);

            int buildingId = 0;
            foreach ((FileInfo _, Building b, string uid) in buildings)
            {
                b.AssignBuildingId(++buildingId);
                BuildingsById[b.BID] = b;
                BuildingsDict[uid] = b;
                switch (uid)
                {
                    case "Capital City":      Building.CapitalId          = b.BID; break;
                    case "Outpost":           Building.OutpostId          = b.BID; break;
                    case "Biospheres":        Building.BiospheresId       = b.BID; break;
                    case "Space Port":        Building.SpacePortId        = b.BID; break;
                    case "Terraformer":       Building.TerraformerId      = b.BID; break;
                    case "Fissionables":      Building.FissionablesId     = b.BID; break;
                    case "Mine Fissionables": Building.MineFissionablesId = b.BID; break;
                    case "Fuel Refinery":     Building.FuelRefineryId     = b.BID; break;
                    case "Dormant Volcano":   Building.VolcanoId          = b.BID; break;
                    case "Active Volcano":    Building.ActiveVolcanoId    = b.BID; break;
                    case "Erupting Volcano":  Building.EruptingVolcanoId  = b.BID; break;
                    case "Lava1":             Building.Lava1Id            = b.BID; break;
                    case "Lava2":             Building.Lava2Id            = b.BID; break;
                    case "Lava3":             Building.Lava3Id            = b.BID; break;
                    case "Crater1":           Building.Crater1Id          = b.BID; break;
                    case "Crater2":           Building.Crater2Id          = b.BID; break;
                    case "Crater3":           Building.Crater3Id          = b.BID; break;
                    case "Crater4":           Building.Crater4Id          = b.BID; break;
                }
            }
        }

        public static Building CreateBuilding(Planet p, Building template)
        {
            if (template == null)
                throw new NullReferenceException(nameof(template));
            Building newB = template.Clone();
            newB.CalcMilitaryStrength(p);
            return newB;
        }

        static readonly Map<string, DiplomacyDialog> DiplomacyDialogs = new();

        public static DiplomacyDialog GetDiplomacyDialog(string dialogName)
        {
            return DiplomacyDialogs[dialogName];
        }

        static void LoadDialogs() // Refactored by RedFox
        {
            DiplomacyDialogs.Clear();
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

        public static IEmpireData FindEmpire(string nameOrArchetype)
        {
            return Empires.Find(e => e.ArchetypeName.Contains(nameOrArchetype)
                                  || e.Name.Contains(nameOrArchetype));
        }

        static void LoadEmpires() // Refactored by RedFox
        {
            Empires.Clear();
            MajorEmpires.Clear();
            MinorEmpires.Clear();
            
            bool modOnly = GlobalStats.HasMod
                && (GlobalStats.Defaults.Mod.DisableDefaultRaces || !GlobalStats.Defaults.Mod.UseVanillaRaces);

            Empires.AddRange(LoadEntities<EmpireData>("Races", "LoadEmpires", modOnly: modOnly));

            // Humans should always be first,
            // The rest should be sorted by the first initial
            Empires.Sort(data =>
            {
                if (data.ArchetypeName == "Human") return 0; // always the first
                if (data.ArchetypeName == "Dauntless") return 1; // Combined Arms: new expansion race
                int initial = (int)data.ArchetypeName[0]; // by initial
                if (data.IsFactionOrMinorRace) // factions are always last
                    initial += 1000;
                return initial;
            });

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
                    Log.Warning($"Empire '{e.Name}' invalid ShipType ''. Using '{e.Singular}' instead.");
                }
            }
        }

        public static void LoadEncounters()
        {
            Encounters.Clear();
            foreach ((FileInfo file, Encounter e) in LoadEntitiesWithInfo<Encounter>("Encounter Dialogs", "LoadEncounters"))
            {
                e.FileName = file.NameNoExt();
                Encounters.Add(e);

                foreach (Message message in e.MessageList)
                    foreach (Response r in message.ResponseOptions)
                    {
                        if (r.UnlockTech.NotEmpty())
                        {
                            if (TryGetTech(r.UnlockTech, out Technology tech))
                            {
                                tech.Unlockable = true;
                            }
                            else
                            {
                                Log.Error($"Encounter={e.Name} missing UnlockTech='{r.UnlockTech}' Response={r.Text}");
                                r.UnlockTech = null;
                            }
                        }
                    }
            }
        }

        static void LoadExpEvents() // Refactored by RedFox
        {
            EventsDict.Clear();
            foreach ((FileInfo file, ExplorationEvent e) in LoadEntitiesWithInfo<ExplorationEvent>("Exploration Events", "LoadExpEvents"))
            {
                e.FileName = file.NameNoExt();
                EventsDict[e.FileName] = e;
                foreach (Outcome o in e.PotentialOutcomes)
                {
                    if (o.UnlockTech.NotEmpty())
                    {
                        if (TryGetTech(o.UnlockTech, out Technology tech))
                            tech.Unlockable = true;
                        else
                        {
                            Log.Error($"ExplorationEvent={e.Name} missing UnlockTech='{o.UnlockTech}' Outcome={o.TitleText}");
                            o.UnlockTech = null;
                        }
                    }
                    if (o.SecretTechDiscovered.NotEmpty())
                    {
                        if (TryGetTech(o.SecretTechDiscovered, out Technology tech))
                            tech.Unlockable = true;
                        else
                        {
                            Log.Error($"ExplorationEvent missing SecretTechDiscovered='{o.SecretTechDiscovered}' Evt={e.Name} Outcome={o.TitleText}");
                            o.SecretTechDiscovered = null;
                        }
                    }
                }
            }
        }

        static readonly Array<TextureBinding> Arcs = new Array<TextureBinding>();

        public static SubTexture GetArcTexture(float weaponArc)
        {
            if (Arcs.IsEmpty)
            {
                var arcs = TextureAtlas.FromFolder("Textures/Arcs");
                Arcs.Add(arcs.GetBinding("Arc15"));
                Arcs.Add(arcs.GetBinding("Arc20"));
                Arcs.Add(arcs.GetBinding("Arc45"));
                Arcs.Add(arcs.GetBinding("Arc60"));
                Arcs.Add(arcs.GetBinding("Arc90"));
                Arcs.Add(arcs.GetBinding("Arc120"));
                Arcs.Add(arcs.GetBinding("Arc180"));
                Arcs.Add(arcs.GetBinding("Arc360"));
            }

            // @note We're doing loose ARC matching to catch freak angles
            // TODO: maybe there's an easier way to bucket these
            int arcIdx;
            if      (weaponArc >= 240f)  arcIdx = 7; // Arc360
            else if (weaponArc >= 150f)  arcIdx = 6; // Arc180
            else if (weaponArc >= 105f)  arcIdx = 5; // Arc120
            else if (weaponArc >= 75f)   arcIdx = 4; // Arc90
            else if (weaponArc >= 52.5f) arcIdx = 3; // Arc60
            else if (weaponArc >= 32.5f) arcIdx = 2; // Arc45
            else if (weaponArc >= 17.5f) arcIdx = 1; // Arc20
            else                         arcIdx = 0; // Arc15

            return Arcs[arcIdx].GetOrLoadTexture();
        }

        static TextureAtlas FlagTextures;
        
        public static SubTexture Flag(int index) =>
            FlagTextures != null && FlagTextures.TryGetTexture(index, out SubTexture t) ? t : null;
        
        public static SubTexture Flag(Empire e) => Flag(e.data.Traits.FlagIndex);
        
        public static int NumFlags => FlagTextures?.Count ?? 0;
        
        static void LoadFlagTextures() // Refactored by RedFox
        {
            FlagTextures = LoadAtlas("Flags");
        }

        public static SubTexture FleetIcon(int index)
        {
            return Texture("FleetIcons/"+index);
        }

        static void LoadGoods()
        {
            GameLoadingScreen.SetStatus("Goods");
            TransportableGoods = YamlParser.DeserializeArray<Good>("/Goods/Goods.yaml");
        }

        // loads models from a model folder that match "modelPrefixNNN.xnb" format, where N is an integer
        static void LoadNumberedModels(Array<StaticMesh> models, string modelFolder, string modelPrefix)
        {
            models.Clear();
            var files = GatherFilesModOrVanilla(modelFolder, "xnb");
            DebugResourceLoading(modelPrefix, files);
            foreach (FileInfo info in files)
            {
                string nameNoExt = info.NameNoExt();
                try
                {
                    // only accept "prefixNN" format, because there are a bunch of textures in the asteroids folder
                    if (nameNoExt.StartsWith(modelPrefix) &&
                        int.TryParse(nameNoExt.Substring(modelPrefix.Length), out int _))
                    {
                        var model = RootContent.LoadStaticMesh(info.RelPath());
                        models.Add(model);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"LoadNumberedModels {modelFolder} {nameNoExt} failed");
                }
            }
        }

        static readonly Array<StaticMesh> JunkModels = new();
        public static int NumJunkModels => JunkModels.Count;
        public static StaticMesh GetJunkModel(int idx) => JunkModels[idx];
        public static float GetJunkModelRadius(int idx) => JunkModels[idx].Radius;

        static void LoadJunk() // Refactored by RedFox
        {
            LoadNumberedModels(JunkModels, "Model/SpaceJunk/", "spacejunk");
        }

        static readonly Array<StaticMesh> AsteroidModels = new();
        public static int NumAsteroidModels => AsteroidModels.Count;
        public static StaticMesh GetAsteroidModel(int asteroidId) => AsteroidModels[asteroidId];
        public static float GetAsteroidModelRadius(int asteroidId) => AsteroidModels[asteroidId].Radius;

        static void LoadAsteroids()
        {
            LoadNumberedModels(AsteroidModels, "Model/Asteroids/", "asteroid");
        }
        
        // Refactored by RedFox
        // Can be called after game init, to reset `Localizer` with new language tokens
        public static void LoadLanguage(Language language)
        {
            var gameText = new FileInfo(ContentDirectory + "GameText.yaml");
            var modText = new FileInfo(ModContentDirectory + "GameText.yaml");
            Localizer.LoadFromYaml(gameText, modText, language);
        }

        public static TextureAtlas SmallStars, MediumStars, LargeStars;

        static void LoadStars()
        {
            SmallStars  = LoadAtlas("Textures/Stars/SmallStars");
            MediumStars = LoadAtlas("Textures/Stars/MediumStars");
            LargeStars  = LoadAtlas("Textures/Stars/LargeStars");
        }

        static TextureAtlas Nebulae;
        static Array<TextureBinding> BigNebulae = new Array<TextureBinding>();
        static Array<TextureBinding> MedNebulae = new Array<TextureBinding>();
        static Array<TextureBinding> SmallNebulae = new Array<TextureBinding>();

        public static bool HasLoadedNebulae => Nebulae != null && Nebulae.Count > 0;
        public static int NumBigNebulae => BigNebulae.Count;

        // Refactored by RedFox
        static void LoadNebulae()
        {
            BigNebulae.Clear();
            MedNebulae.Clear();
            SmallNebulae.Clear();
            Nebulae?.Dispose();
            Nebulae = RootContent.LoadTextureAtlas("Textures/Nebulas", useAssetCache: false);

            for (int i = 0; i < Nebulae.Count; ++i)
            {
                TextureBinding tex = Nebulae.GetBinding(i);
                if      (tex.Width >= 2048) { BigNebulae.Add(tex); }
                else if (tex.Width >= 1024) { MedNebulae.Add(tex); }
                else                        { SmallNebulae.Add(tex); }
            }
        }

        public static SubTexture SmallNebulaRandom(RandomBase random)
        {
            return random.Item(SmallNebulae).GetOrLoadTexture();
        }
        public static SubTexture NebulaMedRandom(RandomBase random)
        {
            return random.Item(MedNebulae).GetOrLoadTexture(); 
        }
        public static SubTexture NebulaBigRandom(RandomBase random)
        {
            return random.Item(BigNebulae).GetOrLoadTexture();
        }
        public static SubTexture BigNebula(int index)
        {
            return BigNebulae[index].GetOrLoadTexture();
        }

        // Refactored by RedFox
        static readonly Map<string, StaticMesh> ProjectileMeshes = new();

        public static bool ProjectileMesh(string name, out StaticMesh mesh)
            => ProjectileMeshes.TryGetValue(name, out mesh);
        
        public static void LoadProjectileMeshes()
        {
            GameLoadingScreen.SetStatus("LoadProjectileMeshes");
            ProjectileMeshes.Clear();
            LoadProjectileMesh("Model/Projectiles/", "projLong");
            LoadProjectileMesh("Model/Projectiles/", "projTear");
            LoadProjectileMesh("Model/Projectiles/", "projBall");
            LoadProjectileMesh("Model/Projectiles/", "torpedo");
            LoadProjectileMesh("Model/Projectiles/", "missile");
            LoadProjectileMesh("Model/Projectiles/", "spacemine");
            LoadCustomProjectileMeshes("Model/Projectiles/custom");
        }

        static void LoadProjectileMesh(string projectileDir, string nameNoExt)
        {
            string path = projectileDir + nameNoExt;
            try
            {
                var projectileMesh = RootContent.LoadStaticMesh(path);
                ProjectileMeshes[nameNoExt] = projectileMesh;
            }
            catch (Exception e)
            {
                if (e.HResult == -2146233088)
                    return;
                Log.Error(e, $"LoadProjectile {path} failed");
            }
        }

        static void LoadCustomProjectileMeshes(string modelFolder)
        {
            var files = GatherFilesModOrVanilla(modelFolder, "xnb");
            DebugResourceLoading(modelFolder, files);

            foreach (FileInfo info in files)
            {
                if (info.Name.Contains("_")) continue;
                string nameNoExt = info.NameNoExt();
                try
                {
                    var projectileMesh = RootContent.LoadStaticMesh(info.RelPath());
                    ProjectileMeshes[nameNoExt] = projectileMesh;
                }
                catch (Exception e)
                {
                    Log.Error(e, $"LoadCustomProjectileMeshes {modelFolder} {nameNoExt} failed");
                }
            }
        }

        // TODO: would be nice to only load these on-demand, this would greatly reduce memory usage
        static void LoadProjectileTextures()
        {
            ProjTextDict.ClearAndDispose();
            var files = GatherFilesUnified("Model/Projectiles/textures", "*", recursive: false);

            static (string shortName, Texture2D tex) LoadProjectileTexture(FileInfo file)
            {
                GameLoadingScreen.SetStatus("LoadProjectileTex", file.Name);
                string shortName = file.NameNoExt();
                Texture2D tex = RootContent.LoadTexture(file);
                return (shortName, tex);
            }

            //var nameTexPairs = files.Select(LoadProjectileTexture);
            var nameTexPairs = Parallel.Select(files, LoadProjectileTexture);

            foreach ((string shortName, Texture2D tex) in nameTexPairs)
            {
                if (ProjTextDict.TryGetValue(shortName, out Texture2D existing))
                    Log.Warning($"Projectile Overwrite: {shortName} {existing.Name} -> {tex.Name}");
                ProjTextDict[shortName] = tex;
            }
        }

        static void LoadRandomItems()
        {
            bool modOnly = GlobalStats.HasMod && !GlobalStats.Defaults.Mod.UseVanillaBuildings;
            RandomItemsList = LoadEntities<RandomItem>("RandomStuff", "LoadRandomItems", modOnly: modOnly);
        }

        static void LoadShipModules()
        {
            bool modOnly = GlobalStats.HasMod && !GlobalStats.Defaults.Mod.UseVanillaModules;

            UIDEntityInfo<ShipModule_XMLTemplate>[] modules = LoadUniqueEntitiesByFileUID<ShipModule_XMLTemplate>(
                "ShipModules", "LoadShipModules", m => m.UID, modOnly: modOnly
            );

            foreach ((FileInfo file, ShipModule_XMLTemplate data, string uid) in modules)
            {
                // Added by gremlin support tech level disabled folder.
                if (file.DirectoryName?.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) > 0)
                    continue;

                data.UID = uid;
                data.IconTexturePath = string.Intern(data.IconTexturePath);
                if (data.WeaponType != null)
                {
                    data.WeaponType = string.Intern(data.WeaponType);
                    if (!WeaponsDict.ContainsKey(data.WeaponType))
                        Log.Warning($"ShipModule UID='{uid}' missing WeaponType='{data.WeaponType}'");
                }

                if (!Localizer.Contains(data.NameIndex))
                    Log.Warning($"ShipModule UID='{uid}' missing NameIndex: {data.NameIndex}");
                if (!Localizer.Contains(data.DescriptionIndex))
                    Log.Warning($"ShipModule UID='{uid}' missing DescriptionIndex: {data.DescriptionIndex}");

                if (data.IsCommandModule && data.TargetTracking == 0) data.TargetTracking = (sbyte) (int)(data.XSize * data.YSize * 1.25f );
                if (data.IsCommandModule && data.TargetAccuracy == 0) data.TargetAccuracy = data.TargetTracking;

                // disable Rotation change for 2x2, 3x3, 4x4, ... modules
                // but not for 1x1 weapons
                bool mustRotate = data.WeaponType != null && data.XSize == 1 && data.YSize == 1;
                if (data.XSize == data.YSize && !mustRotate)
                    data.DisableRotation = true;

                ShipModule template = ShipModule.CreateTemplate(null, data);
                ModuleTemplates[uid] = template;
            }
        }

        static readonly Map<string, ShipHull> HullsDict = new Map<string, ShipHull>();
        static readonly Array<ShipHull> HullsList = new Array<ShipHull>();

        public static bool Hull(string shipHull, out ShipHull hull) => HullsDict.Get(shipHull, out hull);
        public static IReadOnlyList<ShipHull> Hulls => HullsList;

        static void LoadHullBonuses()
        {
            HullBonuses.Clear();
            if (GlobalStats.Defaults.UseHullBonuses)
            {
                foreach (HullBonus hullBonus in LoadEntities<HullBonus>("HullBonuses", "LoadHullBonuses"))
                    HullBonuses[hullBonus.Hull] = hullBonus;

                // if there are no bonuses, then disable the flag
                GlobalStats.Defaults.UseHullBonuses = HullBonuses.Count != 0;
            }
        }

        public static ShipHull AddHull(ShipHull hull)
        {
            if (hull != null) // will be null if ShipData.Parse failed
            {
                if (HullsDict.TryGetValue(hull.HullName, out ShipHull existing))
                {
                    HullsList.Remove(existing);
                }
                HullsDict[hull.HullName] = hull;
                HullsList.Add(hull);
                if (GlobalStats.DebugResourceLoading)
                    Log.Write($"Added Hull='{hull.HullName}' From='{hull.Source?.FullName}'");
            }
            return hull;
        }

        public static Map<string, LegacyShipData> LoadLegacyShipHulls()
        {
            LegacyShipData LoadLegacyShipHull(FileInfo info)
            {
                try
                {
                    GameLoadingScreen.SetStatus("LoadLegacyShipHull", info.RelPath());
                    return LegacyShipData.Parse(info, isHullDefinition:true);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"LoadLegacyShipHull {info.Name} failed");
                    return null;
                }
            }
            
            FileInfo[] hullFiles = GatherFilesUnified("Hulls", "xml");
            LegacyShipData[] hulls = Parallel.Select(hullFiles, LoadLegacyShipHull);

            var map = new Map<string, LegacyShipData>();
            foreach (var hull in hulls)
                if (hull != null) map[hull.Hull] = hull;
            return map;
        }

        public static void ConvertLegacyHulls(Map<string, LegacyShipData> legacyHulls)
        {
            LegacyShipData[] hulls = legacyHulls.Values.ToArr();
            void ConvertHull(LegacyShipData hull)
            {
                FileInfo source = hull.Source;
                try
                {
                    GameLoadingScreen.SetStatus("ConvertLegacyHulls", source.RelPath());
                    new ShipHull(hull).Save(Path.ChangeExtension(source.FullName, "hull"));
                }
                catch (Exception e)
                {
                    Log.Error(e, $"ConvertLegacyHulls {source.Name} failed");
                }
            }
            Parallel.ForEach(hulls, ConvertHull);
        }

        public static void LoadHulls() // Refactored by RedFox
        {
            HullsDict.Clear();
            HullsList.Clear();

            if (GlobalStats.GenerateNewHullFiles)
            {
                var legacyHulls = LoadLegacyShipHulls();
                ConvertLegacyHulls(legacyHulls);
            }

            ShipHull LoadShipHull(FileInfo info)
            {
                try
                {
                    GameLoadingScreen.SetStatus("LoadShipHull", info.RelPath());
                    return new ShipHull(info);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"LoadShipHull {info.Name} failed");
                    return null;
                }
            }

            FileInfo[] hullFiles = GatherFilesUnified("Hulls", "hull");
            Log.Write($"Loading {hullFiles.Length} Hulls");

            ShipHull[] newHulls = Parallel.Select(hullFiles, LoadShipHull);
            foreach (ShipHull hull in newHulls)
            {
                // error check that all models exist, otherwise the game could crash when this hull is spawned
                if (GetModOrVanillaFile(hull.ModelPath) == null &&
                    GetModOrVanillaFile(hull.ModelPath + ".xnb"/*legacy XNB models*/) == null)
                {
                    throw new FileNotFoundException(
                        $"Could not find ModelPath={hull.ModelPath} for hull {hull.Source.Name} in {ContentDirectory}"
                        + (GlobalStats.HasMod ? $" or {ModContentDirectory}" : ""));
                }
                AddHull(hull);
            }
        }

        public static int GetNumExoticGoods() => TransportableGoods.Filter(g => g.IsGasGiantMineable).Length;

        public static readonly ShipsManager Ships = new();

        public static bool AddShipTemplate(ShipDesign shipDesign, bool playerDesign, bool readOnly = false)
        {
            return Ships.Add(shipDesign, playerDesign, readOnly);
        }

        public static bool ShipTemplateExists(string shipName) => Ships.Exists(shipName);
        public static bool GetShipTemplate(string shipName, out Ship template) => Ships.Get(shipName, out template);
        public static Ship GetShipTemplate(string shipName, bool throwIfError = true)
        {
            return Ships.Get(shipName, throwIfError);
        }

        static void UnloadShipDesigns()
        {
            Ships.Clear();
        }

        // Refactored by RedFox
        public static void LoadAllShipDesigns()
        {
            UnloadShipDesigns();

            if (GlobalStats.GenerateNewShipDesignFiles)
            {
                var oldDesigns = GetLegacyShipDesigns();
                ConvertOldDesigns(oldDesigns.Values.ToArr());
            }

            var designs = GetAllShipDesigns();
            LoadShipDesigns(designs.Values.ToArr());
        }

        struct ShipDesignInfo
        {
            public FileInfo File;
            public bool IsPlayerDesign;
            public bool IsReadonlyDesign;
        }

        static void LoadShipDesigns(ShipDesignInfo[] descriptors)
        {
            Log.Write($"Loading {descriptors.Length} Ship Templates");

            void LoadShips(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    FileInfo info = descriptors[i].File;
                    if (info.DirectoryName?.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) != -1)
                        continue;
                    try
                    {
                        GameLoadingScreen.SetStatus("LoadShipTemplate", info.RelPath());
                        ShipDesign shipDesign = ShipDesign.Parse(info);
                        if (shipDesign == null)
                            continue;

                        if (shipDesign.InvalidModules != null)
                        {
                            bool incompatibleMod = GlobalStats.HasMod && shipDesign.ModName.IsEmpty();
                            if (incompatibleMod) // this is most likely an incompatibility between current mod and Vanilla design
                            {
                                Log.Warning($"Ignoring Vanilla ShipDesign={shipDesign.Name} due to invalid modules: {shipDesign.InvalidModules}");
                                continue;
                            }

                            // special conditions for debugging completely broken designs
                            bool allowDebuggingBrokenDesigns = Log.HasDebugger;
                            if (!allowDebuggingBrokenDesigns)
                            {
                                Log.Warning($"Ignoring ShipDesign={shipDesign.Name} due to invalid modules: {shipDesign.InvalidModules}");
                                continue;
                            }
                        }

                        string nameNoExt = info.NameNoExt();
                        if (nameNoExt != shipDesign.Name)
                        {
                            Log.Warning($"File name '{nameNoExt}' does not match ship name '{shipDesign.Name}'." +
                                         "\n This can prevent loading of ships that have this filename in the XML :" +
                                        $"\n path '{info.FullName}'. Overwriting ship name to '{nameNoExt}'. ");
                            shipDesign.Name = nameNoExt;
                        }

                        if (GlobalStats.FixDesignRoleAndCategory)
                        {
                            // the appropriate fixes are already made to Role and Category
                            // during ShipDesign.InitializeCommonStats()
                            shipDesign.Save(info);
                        }

                        AddShipTemplate(shipDesign, playerDesign: descriptors[i].IsPlayerDesign,
                                                    readOnly:     descriptors[i].IsReadonlyDesign);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"Load.Ship '{info.Name}' failed");
                    }
                }
            }

            Parallel.For(descriptors.Length, LoadShips);
            //LoadShips(0, descriptors.Length); // test without parallel for
        }

        static void ConvertOldDesigns(ShipDesignInfo[] shipDescriptors)
        {
            void ConvertOldDesign(ShipDesignInfo designInfo)
            {
                FileInfo info = designInfo.File;
                try
                {
                    GameLoadingScreen.SetStatus("ConvertOldDesign", info.RelPath());
                    LegacyShipData shipData = LegacyShipData.Parse(info, isHullDefinition: false);
                    if (shipData != null && shipData.Role != LegacyShipData.RoleName.disabled)
                    {
                        shipData.SaveDesign(new FileInfo(Path.ChangeExtension(info.FullName, "design")));
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Load.Ship '{info.Name}' failed");
                }
            }
            Parallel.ForEach(shipDescriptors, ConvertOldDesign);
        }

        static Map<string, ShipDesignInfo> GetAllShipDesigns()
        {
            var designs = new Map<string, ShipDesignInfo>();
            // saved designs are loaded first, to ensure they don't overwrite mod ShipDesigns
            CombineOverwrite(designs, Dir.GetFiles(Dir.StarDriveAppData + "/Saved Designs", "design"), readOnly: false, playerDesign: true);
            if (GlobalStats.HasMod && !GlobalStats.Defaults.Mod.UseVanillaShips) // only get mod files
                CombineOverwrite(designs, Dir.GetFiles(ModContentDirectory + "ShipDesigns", "design"), readOnly: true, playerDesign: false);
            else // first get Vanilla files, then override with ShipDesigns from the mod
                CombineOverwrite(designs, GatherFilesUnified("ShipDesigns", "design"), readOnly: true, playerDesign: false);
            return designs;
        }

        static Map<string, ShipDesignInfo> GetLegacyShipDesigns()
        {
            var designs = new Map<string, ShipDesignInfo>();
            CombineOverwrite(designs, Dir.GetFiles(Dir.StarDriveAppData + "/Saved Designs", "xml"), readOnly: false, playerDesign: true);
            if (GlobalStats.HasMod && !GlobalStats.Defaults.Mod.UseVanillaShips)
            {
                CombineOverwrite(designs, Dir.GetFiles(ModContentDirectory + "StarterShips", "xml"), readOnly: true, playerDesign: false);
                CombineOverwrite(designs, Dir.GetFiles(ModContentDirectory + "SavedDesigns", "xml"), readOnly: true, playerDesign: false);
                CombineOverwrite(designs, Dir.GetFiles(ModContentDirectory + "ShipDesigns", "xml"), readOnly: true, playerDesign: false);
                CombineOverwrite(designs, Dir.GetFiles(ModContentDirectory + "ShipDesignsXML", "xml"), readOnly: true, playerDesign: false);
            }
            else
            {
                CombineOverwrite(designs, GatherFilesUnified("StarterShips", "xml"), readOnly: true, playerDesign: false);
                CombineOverwrite(designs, GatherFilesUnified("SavedDesigns", "xml"), readOnly: true, playerDesign: false);
                CombineOverwrite(designs, GatherFilesUnified("ShipDesigns", "xml"), readOnly: true, playerDesign: false);
                CombineOverwrite(designs, GatherFilesUnified("ShipDesignsXML", "xml"), readOnly: true, playerDesign: false);
            }
            return designs;
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

        static Map<string, FileInfo> TestShipPathCache = new Map<string, FileInfo>();

        // @note This is used for Unit Tests and is not part of the core game
        // @param shipsList Only load these ships to make loading faster.
        //                  Example:  shipsList: new [] { "Vulcan Scout" }
        public static void LoadStarterShipsForTesting(string[] shipsList = null, bool clearAll = false)
        {
            if (clearAll)
                UnloadShipDesigns();

            var ships = new Array<FileInfo>();

            if (shipsList != null)
            {
                if (TestShipPathCache.Count == 0)
                {
                    foreach (FileInfo info in GatherFilesUnified("ShipDesigns", "design"))
                        TestShipPathCache[info.NameNoExt()] = info;
                }

                string[] newShips = shipsList.Filter(name => !ShipTemplateExists(name));
                ships.AddRange(newShips.Select(shipName =>
                {
                    if (TestShipPathCache.TryGetValue(shipName, out FileInfo file))
                        return file;
                    throw new FileNotFoundException($"Could not find ship '{shipName}.design' in any subfolder of Content/ShipDesigns/**");
                }));
            }

            if (ships.Count > 0)
            {
                var designs = new Map<string, ShipDesignInfo>();
                CombineOverwrite(designs, ships.ToArray(), readOnly: true, playerDesign: false);
                LoadShipDesigns(designs.Values.ToArr());
            }
        }

        public static void LoadContentForTesting()
        {
            Verbose = false;
            LoadContent(loadShips:false);

            // essential graphics:
            SunType.LoadSunTypes(loadIcons: false);
            Fonts.LoadFonts(RootContent, Localizer.Language);
            LoadProjectileMeshes();
        }

        static void TechValidator()
        {
            GameLoadingScreen.SetStatus("TechValidator");
            Array<Technology> techs = TechsList.ToArrayList();
            var rootTechs = new Array<Technology>();
            foreach (Technology rootTech in techs)
            {
                if (rootTech.IsRootNode)
                {
                    if (rootTechs.Contains(rootTech))
                        Log.Warning($"Duplicate root tech : '{rootTech}'");
                    rootTechs.Add(rootTech);
                }
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
                if (!notInTree.IsRootNode && notInTree.ComesFrom.Count == 0)
                    notInTree.Discovered = false;
            }

            foreach (Technology tech in techs)
            {
                if (!tech.Unlockable)
                    Log.WarningVerbose($"Tech {tech.UID} has no way to unlock! Source: '{tech.DebugSourceFile}'");
            }

            foreach (Technology tech in TechsList)
                tech.ResolveLeadsToTechs();
        }

        public static void LoadTechTree()
        {
            TechTree.Clear();

            bool modOnly = GlobalStats.HasMod
                && (GlobalStats.Defaults.Mod.ClearVanillaTechs || !GlobalStats.Defaults.Mod.UseVanillaTechs);

            UIDEntityInfo<Technology>[] techs = LoadUniqueEntitiesByFileUID<Technology>(
                "Technology", "LoadTechTree", tech => tech.UID, modOnly: modOnly
            );

            foreach ((FileInfo file, Technology tech, string uid) in techs)
            {
                tech.DebugSourceFile = file.RelPath();
                TechTree[uid] = tech;

                // categorize extra techs
                tech.UpdateTechnologyTypesFromUnlocks();
            }
            
            TechValidator();
        }

        static readonly Map<string, IWeaponTemplate> WeaponsDict = new();

        // Creates a weapon used by a Ship
        // `module` can be null if the weapon does not belong to a specific module
        // `hull` can be null if hull based bonuses don't matter
        public static Weapon CreateWeapon(UniverseState us, string uid, Ship owner, ShipModule module, ShipHull hull)
        {
            IWeaponTemplate template = WeaponsDict[uid];
            return new Weapon(us, template, owner, module, hull);
        }

        // Gets an immutable IWeaponTemplate
        public static IWeaponTemplate GetWeaponTemplate(string uid)
        {
            if (GetWeaponTemplate(uid, out IWeaponTemplate t))
                return t;
            Log.Error($"WeaponTemplate not found: '{uid}', using default VulcanCannon");
            return WeaponsDict["VulcanCannon"];
        }

        public static IWeaponTemplate GetWeaponTemplateOrNull(string uid)
        {
            return GetWeaponTemplate(uid, out IWeaponTemplate t) ? t : null;
        }

        public static bool GetWeaponTemplate(string uid, out IWeaponTemplate template)
        {
            return WeaponsDict.TryGetValue(uid, out template);
        }

        static void LoadWeapons() // Refactored by RedFox
        {
            WeaponsDict.Clear();

            bool modOnly = GlobalStats.HasMod
                && (GlobalStats.Defaults.Mod.ClearVanillaWeapons || !GlobalStats.Defaults.Mod.UseVanillaWeapons);

            UIDEntityInfo<WeaponTemplate>[] weapons = LoadUniqueEntitiesByFileUID<WeaponTemplate>(
                "Weapons", "LoadWeapons", w => w.UID, modOnly: modOnly
            );

            foreach ((FileInfo file, WeaponTemplate w, string uid) in weapons)
            {
                w.DebugSourceFile = file.RelPath();
                w.UID = uid;
                WeaponsDict[uid] = w;
            }

            // some weapons have sub-weapons, which requires two-step initialization
            foreach (WeaponTemplate w in WeaponsDict.Values)
            {
                w.InitializeTemplate();
            }
        }

        static void LoadShipRoles()
        {
            ShipRoles.Clear();
            foreach (ShipRole shipRole in LoadEntities<ShipRole>("ShipRoles", "LoadShipRoles"))
            {
                if (Enum.TryParse(shipRole.Name, out RoleName key))
                    ShipRoles[key] = shipRole;
            }
            if (ShipRoles.Count == 0)
                Log.Error("Failed to load any ShipRoles! Make sure Content/ShipRoles/*.xml exist!");
        }

        static readonly Map<string, EconomicResearchStrategy> EconStrategies = new();

        public static EconomicResearchStrategy GetEconomicStrategy(string name) => EconStrategies[name];

        static void LoadEcoResearchStrats()
        {
            EconStrategies.Clear();
            foreach (var s in YamlParser.DeserializeArray<EconomicResearchStrategy>("EconomicResearchStrategies.yaml"))
                EconStrategies[s.Name] = s;
        }

        static readonly Map<SunZone, Array<PlanetCategory>> ZoneDistribution = new();

        public static PlanetCategory RandomPlanetCategoryFor(SunZone sunZone, RandomBase random)
        {
            return random.Item(ZoneDistribution[sunZone]);
        }

        static void LoadSunZoneData()
        {
            GameLoadingScreen.SetStatus("SunZoneData");
            var zones = YamlParser.DeserializeArray<SunZoneData>("SunZoneData.yaml");
            ZoneDistribution.Clear();
            ZoneDistribution[SunZone.Near]    = SunZoneData.CreateDistribution(zones, SunZone.Near);
            ZoneDistribution[SunZone.Habital] = SunZoneData.CreateDistribution(zones, SunZone.Habital);
            ZoneDistribution[SunZone.Far]     = SunZoneData.CreateDistribution(zones, SunZone.Far);
            ZoneDistribution[SunZone.VeryFar] = SunZoneData.CreateDistribution(zones, SunZone.VeryFar);
        }

        static readonly Map<BuildRatio, int[]> BuildRatios = new Map<BuildRatio, int[]>();

        public static int[] GetFleetRatios(BuildRatio canBuild)
        {
            return BuildRatios[canBuild];
        }

        static void LoadBuildRatios()
        {
            GameLoadingScreen.SetStatus("FleetBuildRatios");
            BuildRatios.Clear();
            var ratios = YamlParser.DeserializeArray<FleetBuildRatios>("FleetBuildRatios.yaml");
            foreach (BuildRatio canBuild in Enum.GetValues(typeof(BuildRatio)))
            {
                BuildRatios[canBuild] = FleetBuildRatios.GetRatiosFor(ratios, canBuild);
            }
        }

        // Added by RedFox
        static void LoadBlackboxSpecific()
        {
            TryDeserialize("ShipNames/ShipNames.xml", ref ShipNames);
            TryDeserialize("AgentMissions/AgentMissionData.xml", ref AgentMissionData);
        }

        public static Video LoadVideo(GameContentManager content, string videoPath)
        {
            string path = "Video/" + videoPath;
            if (GlobalStats.HasMod)
            {
                // Mod videos currently use .xnb pointer which references .wmv
                // but the internal XNA asset system can't handle relative paths correctly
                // for mods, so we need an extra "../" in front of the path
                string modVideo = "Content/../" + GlobalStats.ModPath + path + ".xnb";
                var info = new FileInfo(modVideo);
                if (info.Exists)
                    path = "../" + GlobalStats.ModPath + path + ".xnb";
            }

            Log.Write(ConsoleColor.Green, $"LoadVideo: {path}");
            var video = content.Load<Video>(path);
            if (video != null)
                return video;

            Log.Error($"LoadVideo failed: {path}");
            return content.Load<Video>("Video/Loading 2");
        }

        public static bool GetEncounter(Empire empire, string name, out Encounter encounter)
        {
            encounter = null;
            foreach (Encounter e in Encounters)
            {
                if (e.Faction == empire.data.Traits.Name && e.Name == name)
                {
                    encounter = e;
                    break;
                }
            }

            return encounter != null;
        }

        static IShipDesign Assert(IEmpireData e, string shipName, string usage)
        {
            if (shipName == null) // the ship is not defined
                return null;
            if (Ships.GetDesign(shipName, out IShipDesign design))
                return design;
            string empire = e == null ? "" : $" empire={e.Name,-20}";
            Log.Error($"Assert Ship Exists failed! {usage,-20}  ship={shipName,-20} {empire}");
            return null;
        }

        static void Assert(IEmpireData e, string shipName, string usage, Func<IShipDesign, bool> flag, string flagName)
        {
            IShipDesign design = Assert(e, shipName, usage);
            if (design == null && !e.IsFactionOrMinorRace)
                Log.Error($"Assert ship={usage} not defined for empire={e.Name,-20}");
            if (design != null && !flag(design))
                Log.Error($"Assert Ship.{flagName} failed! {usage,-20}  ship={design.Name,-20}  role={design.Role,-12}  empire={e.Name,-20}");
        }

        // Added by RedFox: makes sure all required ship designs are present
        static void CheckForRequiredShipDesigns()
        {
            foreach (EmpireData e in Empires)
            {
                Assert(e, e.StartingShip,  "StartingShip");
                Assert(e, e.StartingScout, "StartingScout");
                Assert(e, e.ScoutShip,     "ScoutShip");
                Assert(e, e.PrototypeShip, "PrototypeShip");

                Assert(e, e.DefaultSmallTransport, "DefaultSmallTransport", s => s.IsFreighter, "IsFreighter");
                Assert(e, e.DefaultSmallTransport, "DefaultSmallTransport", s => s.IsCandidateForTradingBuild, "IsCandidateForTradingBuild");
                Assert(e, e.FreighterShip, "FreighterShip", s => s.IsFreighter, "IsFreighter");
                Assert(e, e.FreighterShip, "FreighterShip", s => s.IsCandidateForTradingBuild, "IsCandidateForTradingBuild");

                Assert(e, e.DefaultTroopShip,  "DefaultTroopShip",  s => s.IsSingleTroopShip, "IsSingleTroopShip");
                Assert(e, e.DefaultColonyShip, "DefaultColonyShip", s => s.IsColonyShip, "IsColonyShip");
                Assert(e, e.ColonyShip,        "ColonyShip",        s => s.IsColonyShip, "IsColonyShip");
                Assert(e, e.DefaultConstructor, "DefaultConstructor", s => s.IsConstructor, "IsConstructor");
                Assert(e, e.ConstructorShip,    "ConstructorShip",    s => s.IsConstructor, "IsConstructor");
                Assert(e, e.DefaultShipyard,    "DefaultShipyard",    s => s.IsShipyard,    "IsShipyard");

                Assert(e, e.DefaultAssaultShuttle, "DefaultAssaultShuttle");
                Assert(e, e.DefaultSupplyShuttle,  "DefaultSupplyShuttle", s => s.IsSupplyShuttle, "IsSupplyShuttle");
                Assert(e, e.DefaultResearchStation, "DefaultResearchStation", s => s.IsResearchStation, "IsResearchStation");
                Assert(e, e.DefaultMiningShip, "DefaultMiningShip", s => s.BaseCargoSpace > 0, "BaseCargoSpace");
                Assert(e, e.DefaultMiningStation, "DefaultMiningStation", s => s.IsMiningStation, "IsMiningStation");
            }

            string[] requiredShips =
            {
                /*meteors*/"Meteor A", "Meteor B", "Meteor C", "Meteor D", "Meteor E", "Meteor F", "Meteor G",
                /*debug*/"Bondage-Class Mk IIIa Cruiser", "Target Dummy",
                /*hangarhack*/"DynamicAntiShip", "DynamicInterceptor", "DynamicLaunch",
                /*defaults*/"Subspace Projector", "Supply Shuttle", "Assault Shuttle", "Terran Constructor", "Basic Research Station", "Basic Mining Station"
            };

            foreach (string requiredShip in requiredShips)
            {
                Assert(null, requiredShip, "RequiredShip");
            }
        }
    }
}
