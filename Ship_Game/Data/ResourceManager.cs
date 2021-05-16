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
using Ship_Game.Data;
using Ship_Game.Data.Yaml;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.SpriteSystem;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.AI;

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

        // Ship designs, mapped by ship.Name
        static readonly Map<string, Ship> ShipsDict = new Map<string, Ship>();

        public static Map<string, Technology> TechTree          = new Map<string, Technology>(GlobalStats.CaseControl);
        public static Array<Encounter> Encounters               = new Array<Encounter>();
        public static Map<string, Building> BuildingsDict       = new Map<string, Building>();
        public static Map<string, Good> GoodsDict               = new Map<string, Good>();
        static readonly Map<string, ShipModule> ModuleTemplates = new Map<string, ShipModule>(GlobalStats.CaseControl);
        public static Map<string, Texture2D> ProjTextDict       = new Map<string, Texture2D>();

        public static Array<RandomItem> RandomItemsList = new Array<RandomItem>();
        static Array<string> TroopsDictKeys           = new Array<string>();
        public static IReadOnlyList<string> TroopTypes => TroopsDictKeys;

        public static Map<string, Artifact> ArtifactsDict      = new Map<string, Artifact>();
        public static Map<string, ExplorationEvent> EventsDict = new Map<string, ExplorationEvent>(GlobalStats.CaseControl);
        public static XmlSerializer HeaderSerializer           = new XmlSerializer(typeof(HeaderData));

        public static HostileFleets HostileFleets                = new HostileFleets();
        public static ShipNames ShipNames                        = new ShipNames();
        public static AgentMissionData AgentMissionData          = new AgentMissionData();
        public static MainMenuShipList MainMenuShipList          = new MainMenuShipList();
        public static Map<ShipData.RoleName, ShipRole> ShipRoles = new Map<ShipData.RoleName, ShipRole>();
        public static Map<string, HullBonus> HullBonuses         = new Map<string, HullBonus>();
        public static Map<string, PlanetEdict> PlanetaryEdicts   = new Map<string, PlanetEdict>();

        static RacialTraits RacialTraits;
        static DiplomaticTraits DiplomacyTraits;

        public static SubTexture Blank;

        // This 1x1 White pixel texture is used for drawing Lines and other primitives
        public static Texture2D WhitePixel;

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
        }

        public static Technology Tech(string techUid)
        {
            return TechTree[techUid];
        }

        public static bool TryGetTech(string techUid, out Technology tech)
            => TechTree.TryGetValue(techUid, out tech);

        public static ExplorationEvent Event(string eventName)
        {
            if (EventsDict.TryGetValue(eventName, out ExplorationEvent events))
                return events;
            Log.WarningWithCallStack($"{eventName} not found. Contact mod creator.");
            return EventsDict["default"];
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
                    throw;
                Log.ErrorDialog(ex, $"Mod {GlobalStats.ModName} load failed. Disabling mod and loading vanilla.", 0);
                WaitForExit();
                GlobalStats.ClearActiveMod();
                UnloadAllData(manager);
                LoadAllResources(manager, null);
            }
            Log.Write($"LoadItAll elapsed: {s.Elapsed.TotalSeconds}s");
        }

        static readonly Array<KeyValuePair<string, double>> PerfProfile = new Array<KeyValuePair<string, double>>();
        static Stopwatch PerfStopwatch = new Stopwatch();
        static void BeginPerfProfile()
        {
            PerfProfile.Clear();
            PerfStopwatch.Start();
        }
        static void Profiled(string uniqueName, Action body)
        {
            var sw = Stopwatch.StartNew();
            body();
            double elapsed = sw.Elapsed.TotalSeconds;
            PerfProfile.Add(new KeyValuePair<string, double>(uniqueName, elapsed));
        }
        static void Profiled(Action body) => Profiled(body.Method.Name, body);
        static void EndPerfProfile()
        {
            PerfStopwatch.Stop();
            double elapsed = PerfStopwatch.Elapsed.TotalSeconds;
            PerfProfile.Sort(kv => kv.Value);
            PerfProfile.Reverse();
            foreach (var kv in PerfProfile)
            {
                double percent = (kv.Value / elapsed) * 100.0;
                Log.Write(ConsoleColor.Cyan, $"{kv.Key,-20} {kv.Value*1000:0.0}ms  {percent:0.00}%");
            }
        }

        static void LoadAllResources(ScreenManager manager, ModEntry mod)
        {
            if (mod != null)
                GlobalStats.SetActiveModNoSave(mod);
            else
                GlobalStats.ClearActiveMod();

            InitContentDir();
            CreateCoreGfxResources();
            Log.Write($"Load {(GlobalStats.HasMod ? ModContentDirectory : "Vanilla")}");

            BeginPerfProfile();
            Profiled("LoadLanguage", () => LoadLanguage(GlobalStats.Language)); // must be before LoadFonts
            Profiled(LoadHullBonuses);
            Profiled(LoadHullData); // we need Hull Data for main menu ship
            Profiled(LoadEmpires); // Hotspot #2 187.4ms  8.48%

            Profiled(LoadTroops);
            Profiled(LoadWeapons);
            Profiled(LoadShipModules); // Hotspot #4 124.9ms  5.65%
            Profiled(LoadGoods);
            Profiled(LoadShipRoles);
            Profiled(LoadShipTemplates); // Hotspot #1  502.9ms  22.75%
            Profiled(LoadBuildings);
            Profiled(LoadRandomItems);
            Profiled(LoadDialogs);
            Profiled(LoadTechTree);
            Profiled(LoadEncounters);
            Profiled(LoadExpEvents);
            Profiled(TechValidator);

            Profiled(LoadArtifacts);
            Profiled(LoadPlanetEdicts);
            Profiled(LoadPlanetTypes);
            Profiled(LoadSunZoneData);
            Profiled(LoadBuildRatios);
            Profiled(LoadEcoResearchStrats);
            Profiled(LoadBlackboxSpecific);

            Profiled(TestLoad);
            Profiled(RunExportTasks);

            LoadGraphicsResources(manager);
            HelperFunctions.CollectMemory();
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
            Profiled(LoadProjTexts);
            Profiled(LoadProjectileMeshes);
            Profiled(SunType.LoadSunTypes); // Hotspot #3 174.8ms  7.91%
            Profiled("LoadBeamFX", () =>
            {
                ShieldManager.LoadContent(RootContent);
                Beam.BeamEffect = RootContent.Load<Effect>("Effects/BeamFX");
                BackgroundItem.QuadEffect = new BasicEffect(manager.GraphicsDevice, null) { TextureEnabled = true };
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

            SmallNebulae.Clear();
            MedNebulae.Clear();
            BigNebulae.Clear();
            SmallStars = MediumStars = LargeStars = null;
            FlagTextures = null;
            JunkModels.Clear();
            AsteroidModels.Clear();
            ProjTextDict.ClearAndDispose();
            ProjectileModelDict.Clear();
            ProjectileMeshDict.Clear();
            SunType.Unload();
            ShieldManager.UnloadContent();
            Beam.BeamEffect = null;
            BackgroundItem.QuadEffect?.Dispose(ref BackgroundItem.QuadEffect);
            WhitePixel.Dispose(ref WhitePixel);

            // Texture caches MUST be cleared before triggering content reload!
            Textures.Clear();

            // This is a destructive operation that invalidates ALL game resources!
            // @note It HAS to be done after clearing all ResourceManager texture caches!
            manager.UnsafeUnloadAllGameContent();
        }

        public static void UnloadAllData(ScreenManager manager)
        {
            WaitForExit();
            TroopsDictKeys.Clear();
            BuildingsDict.Clear();
            BuildingsById.Clear();
            ModuleTemplates.Clear();
            TechTree.Clear();
            ArtifactsDict.Clear();
            ShipsDict.Clear();
            GoodsDict.Clear();
            Encounters.Clear();
            EventsDict.Clear();
            RandomItemsList.Clear();

            HostileFleets.Fleets.Clear();
            ShipNames.Clear();
            MainMenuShipList.ModelPaths.Clear();
            AgentMissionData = new AgentMissionData();

            UnloadGraphicsResources(manager);
        }

        static void RunExportTasks()
        {
            if (!GlobalStats.ExportTextures)
                return;

            if (!Debugger.IsAttached)
                Log.ShowConsoleWindow();

            if (GlobalStats.ExportTextures)
                RootContent.RawContent.ExportAllTextures();

            if (GlobalStats.ExportMeshes)
                RootContent.RawContent.ExportAllXnbMeshes();

            if (!Debugger.IsAttached)
                Log.HideConsoleWindow();
        }

        static void TestLoad()
        {
            if (!GlobalStats.TestLoad) return;

            Log.ShowConsoleWindow();
            //TestTechTextures();

            if (!Debugger.IsAttached)
                Log.HideConsoleWindow();
        }

        static FileInfo ModInfo(string file)     => new FileInfo( ModContentDirectory + file );
        static FileInfo ContentInfo(string file) => new FileInfo( ContentDirectory + file );

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
            FileInfo[] mod = Dir.GetFiles(ModContentDirectory + dir, pattern, search);
            string fullModPath = Path.GetFullPath(ModContentDirectory);
            foreach (FileInfo file in mod)
            {
                string name = fileNames ? file.Name : file.FullName.Substring(fullModPath.Length);
                #if false
                if (infos.TryGetValue(name, out FileInfo existing))
                {
                    string newName = ModContentDirectory + file.FullName.Substring(fullModPath.Length);
                    string existingName = existing.FullName;
                    if (existingName.StartsWith(fullModPath))
                        existingName = ModContentDirectory + existingName.Substring(fullModPath.Length);
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
            FileInfo[] files = Dir.GetFiles(ModContentDirectory + dir, ext);
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
                GameLoadingScreen.SetStatus(id, info.RelPath());
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
            return LoadEntities<T>(Dir.GetFiles(ModContentDirectory + dir, "xml"), id);
        }

        class InfoPair<T> where T : class
        {
            public readonly FileInfo Info;
            public readonly T Entity;
            public InfoPair(FileInfo info, T entity) { Info = info; Entity = entity; }
        }

        static Array<InfoPair<T>> LoadEntitiesWithInfo<T>(string dir, string id, bool modOnly = false) where T : class
        {
            var files = modOnly ? Dir.GetFiles(ModContentDirectory + dir, "xml") : GatherFilesUnified(dir, "xml");
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

        static readonly Map<string, Troop> TroopsDict = new Map<string, Troop>();
        static readonly Array<Troop> TroopsList = new Array<Troop>();

        public static Troop[] GetTroopTemplatesFor(Empire e)
            => TroopsList.Filter(t => e.WeCanBuildTroop(t.Name)).Sorted(t => t.ActualCost);

        public static float GetTroopCost(string troopType) => TroopsDict[troopType].ActualCost;
        public static Troop GetTroopTemplate(string troopType)
        {
            if (TroopsDict.TryGetValue(troopType, out Troop troop))
                return troop;

            Log.WarningWithCallStack($"Troop {troopType} Template Not found");
            return TroopsList.First;
        }


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
                troop.Level = forOwner.data.MinimumTroopLevel;
            }
            return troop;
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

        static void CreateCoreGfxResources()
        {
            WhitePixel = new Texture2D(RootContent.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            WhitePixel.SetData(new Color[]{ Color.White });
        }

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
                "Textures/GameScreens", "Textures/MainMenu",
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
            => RacialTraits ?? (RacialTraits = TryDeserialize<RacialTraits>("RacialTraits/RacialTraits.xml"));

        public static DiplomaticTraits DiplomaticTraits
            => DiplomacyTraits ?? (DiplomacyTraits = TryDeserialize<DiplomaticTraits>("Diplomacy/DiplomaticTraits.xml"));

        public static SolarSystemData LoadSolarSystemData(string homeSystemName)
            => TryDeserialize<SolarSystemData>("SolarSystems/" + homeSystemName + ".xml");

        public static Array<SolarSystemData> LoadRandomSolarSystems()
            => LoadEntitiesModOrVanilla<SolarSystemData>("SolarSystems/Random", "LoadSolarSystems");

        public static Texture2D LoadRandomLoadingScreen(GameContentManager content)
        {
            FileInfo[] files = GatherFilesModOrVanilla("LoadingScreen", "xnb");

            FileInfo file = files[RandomMath.InRange(0, files.Length)];
            return content.LoadTexture(file, "xnb");
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
                    newB.IsProjector    = true;
                }

                if (!newB.IsSensor && !(newB.SensorRange > 0.0f))
                {
                    newB.SensorRange = 20000.0f;
                    newB.IsSensor    = true;
                }
            }

            newB.CalcMilitaryStrength();
            return newB;
        }

        static readonly Map<string, DiplomacyDialog> DiplomacyDialogs = new Map<string, DiplomacyDialog>();
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
                    Log.Warning($"Empire '{e.Name}' invalid ShipType ''. Using '{e.Singular}' instead.");
                }
            }
        }

        public static void LoadEncounters()
        {
            Encounters.Clear();
            foreach (var pair in LoadEntitiesWithInfo<Encounter>("Encounter Dialogs", "LoadEncounters"))
            {
                Encounter e = pair.Entity;
                e.FileName = pair.Info.NameNoExt();
                Encounters.Add(e);

                foreach (Message message in e.MessageList)
                    foreach (Response response in message.ResponseOptions)
                        if (TechTree.TryGetValue(response.UnlockTech ?? "", out Technology tech))
                            tech.Unlockable = true;
            }
        }

        static void LoadExpEvents() // Refactored by RedFox
        {
            EventsDict.Clear();
            foreach (var pair in LoadEntitiesWithInfo<ExplorationEvent>("Exploration Events", "LoadExpEvents"))
            {
                pair.Entity.FileName = pair.Info.NameNoExt();
                EventsDict[pair.Entity.FileName] = pair.Entity;
                foreach (var outcome in pair.Entity.PotentialOutcomes)
                {
                    if (TechTree.TryGetValue(outcome.UnlockTech ?? "", out Technology tech))
                        tech.Unlockable = true;
                    if (TechTree.TryGetValue(outcome.SecretTechDiscovered ?? "", out tech))
                        tech.Unlockable = true;
                }
            }
        }

        static TextureAtlas FlagTextures;
        public static SubTexture Flag(int index) => FlagTextures[index];
        public static SubTexture Flag(Empire e) => FlagTextures[e.data.Traits.FlagIndex];
        public static int NumFlags => FlagTextures.Count;
        static void LoadFlagTextures() // Refactored by RedFox
        {
            FlagTextures = LoadAtlas("Flags");
        }

        public static SubTexture FleetIcon(int index)
        {
            return Texture("FleetIcons/"+index);
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
        public static IReadOnlyList<ShipData> Hulls                     => HullsList;

        static void LoadHullBonuses()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseHullBonuses)
            {
                foreach (HullBonus hullBonus in LoadEntities<HullBonus>("HullBonuses", "LoadHullBonuses"))
                    HullBonuses[hullBonus.Hull] = hullBonus;
                GlobalStats.ActiveModInfo.UseHullBonuses = HullBonuses.Count != 0;
            }
        }

        public static ShipData AddHull(ShipData hull)
        {
            if (hull != null) // will be null if ShipData.Parse failed
            {
                if (HullsDict.TryGetValue(hull.Hull, out ShipData existing))
                {
                    HullsList.Remove(existing);
                }

                HullsDict[hull.Hull] = hull;
                HullsList.Add(hull);
            }
            return hull;
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
                        GameLoadingScreen.SetStatus("LoadShipHull", info.RelPath());
                        hulls[i] = ShipData.Parse(info, isEmptyHull:true);
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
                AddHull(sd);
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
                    if (nameNoExt.StartsWith(modelPrefix) &&
                        int.TryParse(nameNoExt.Substring(modelPrefix.Length), out int _))
                    {
                        models.Add(RootContent.Load<Model>(info.RelPath()));
                    }
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
            SmallStars  = LoadAtlas("SmallStars");
            MediumStars = LoadAtlas("MediumStars");
            LargeStars  = LoadAtlas("LargeStars");
        }

        static TextureAtlas Nebulae;
        static Array<TextureBinding> BigNebulae = new Array<TextureBinding>();
        static Array<TextureBinding> MedNebulae = new Array<TextureBinding>();
        static Array<TextureBinding> SmallNebulae = new Array<TextureBinding>();

        // Refactored by RedFox
        static void LoadNebulae()
        {
            BigNebulae.Clear();
            MedNebulae.Clear();
            SmallNebulae.Clear();
            Nebulae?.Dispose();
            Nebulae = RootContent.LoadTextureAtlas("Nebulas", useAssetCache: false);

            for (int i = 0; i < Nebulae.Count; ++i)
            {
                TextureBinding tex = Nebulae.GetBinding(i);
                if      (tex.Width >= 2048) { BigNebulae.Add(tex); }
                else if (tex.Width >= 1024) { MedNebulae.Add(tex); }
                else                        { SmallNebulae.Add(tex); }
            }
        }

        public static SubTexture SmallNebulaRandom()
        {
            return SmallNebulae.RandItem().GetOrLoadTexture();
        }
        public static SubTexture NebulaMedRandom()
        {
            return MedNebulae.RandItem().GetOrLoadTexture(); 
        }
        public static SubTexture NebulaBigRandom()
        {
            return BigNebulae.RandItem().GetOrLoadTexture();
        }
        public static SubTexture BigNebula(int index)
        {
            return BigNebulae[index].GetOrLoadTexture();
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

        public static void LoadProjectileMeshes()
        {
            GameLoadingScreen.SetStatus("LoadProjectileMeshes");
            ProjectileMeshDict.Clear();
            ProjectileModelDict.Clear();
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
                    var projModel = RootContent.Load<Model>(info.RelPath());

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
            ProjTextDict.ClearAndDispose();
            var files = GatherFilesUnified("Model/Projectiles/textures", "xnb", recursive: false);

            var nameTexPairs = Parallel.Select(files, file =>
            {
                string shortName = file.NameNoExt();
                GameLoadingScreen.SetStatus("LoadProjectileTex", shortName);
                Texture2D tex = RootContent.LoadTexture(file, "xnb");
                return (shortName, tex);
            });

            foreach ((string shortName, Texture2D tex) in nameTexPairs)
            {
                if (ProjTextDict.TryGetValue(shortName, out Texture2D existing))
                    Log.Warning($"Projectile Overwrite: {shortName} {existing.Name} -> {tex.Name}");
                ProjTextDict[shortName] = tex;
            }
        }

        static void LoadRandomItems()
        {
            RandomItemsList = LoadEntities<RandomItem>("RandomStuff", "LoadRandomItems");
        }

        static void LoadShipModules()
        {
            // 95% spent loading these XML files:
            var entities = LoadEntitiesWithInfo<ShipModule_Deserialize>("ShipModules", "LoadShipModules");

            foreach (var pair in entities)
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
                if (data.IsCommandModule && data.TargetTracking == 0)  data.TargetTracking = (sbyte) (int)(data.XSIZE * data.YSIZE * 1.25f );
                if (data.IsCommandModule && data.TargetAccuracy == 0)  data.TargetAccuracy = data.TargetTracking;

                // disable Rotation change for 2x2, 3x3, 4x4, ... modules
                // but not for 1x1 weapons
                bool mustRotate = data.isWeapon && data.XSIZE == 1 && data.YSIZE == 1;
                if (data.XSIZE == data.YSIZE && !mustRotate)
                    data.DisableRotation = true;

                ShipModule template = ShipModule.CreateTemplate(data);
                template.SetAttributes();
                ModuleTemplates[template.UID] = template;
            }
        }

        struct ShipDesignInfo
        {
            public FileInfo File;
            public bool IsPlayerDesign;
            public bool IsReadonlyDesign;
        }

        public static Ship AddShipTemplate(ShipData shipData, bool fromSave, bool playerDesign = false, bool readOnly = false)
        {
            Ship shipTemplate = Ship.CreateNewShipTemplate(shipData, fromSave);
            if (shipTemplate == null) // happens if module creation failed
                return null;

            shipTemplate.IsPlayerDesign   = playerDesign;
            shipTemplate.IsReadonlyDesign = readOnly;

            lock (ShipsDict)
            {
                ShipsDict[shipTemplate.Name] = shipTemplate;
            }
            return shipTemplate;
        }

        static void LoadShipTemplates(ShipDesignInfo[] shipDescriptors)
        {
            Log.Info($"Loading {shipDescriptors.Length} Ship Templates");
            void LoadShips(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    FileInfo info = shipDescriptors[i].File;
                    if (info.DirectoryName?.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) != -1)
                        continue;
                    try
                    {
                        GameLoadingScreen.SetStatus("LoadShipTemplate", info.RelPath());
                        ShipData shipData = ShipData.Parse(info, isEmptyHull:false);
                        if (shipData == null || shipData.Role == ShipData.RoleName.disabled)
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

        public static Map<string, Ship>.ValueCollection GetShipTemplates()
        {
            return ShipsDict.Values;
        }

        public static Map<string, Ship>.KeyCollection GetShipTemplateIds()
        {
            return ShipsDict.Keys;
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

        // @note This is used for Unit Tests and is not part of the core game
        // @param shipsList Only load these ships to make loading faster.
        //                  Example:  shipsList: new [] { "Vulcan Scout" }
        public static void LoadStarterShipsForTesting(string[] shipsList = null,
                                                      string[] savedDesigns = null)
        {
            LoadBasicContentForTesting();

            var ships = new Array<FileInfo>();
            ships.AddRange(shipsList != null
                ? shipsList.Select(ship => GetModOrVanillaFile($"StarterShips/{ship}.xml"))
                : GatherFilesModOrVanilla("StarterShips", "xml"));
            if (savedDesigns != null)
                ships.AddRange(savedDesigns.Select(ship => GetModOrVanillaFile($"SavedDesigns/{ship}.xml")));

            ShipsDict.Clear();
            var designs = new Map<string, ShipDesignInfo>();
            CombineOverwrite(designs, ships.ToArray(), readOnly: true, playerDesign: false);
            LoadShipTemplates(designs.Values.ToArray());
        }

        public static void LoadBasicContentForTesting()
        {
            InitContentDir();
            LoadWeapons();
            LoadHullData();
            LoadShipRoles();
            LoadShipModules();
            LoadTroops();
            LoadDialogs(); // for CreateEmpire
            LoadEmpires();
            LoadEcoResearchStrats();
            LoadBuildings();
        }

        public static void LoadPlanetContentForTesting()
        {
            LoadBasicContentForTesting();
            LoadPlanetTypes();
            LoadSunZoneData();
            LoadBuildRatios();
            SunType.LoadSunTypes();
        }

        public static void LoadTechContentForTesting()
        {
            LoadBasicContentForTesting();
            LoadTechTree();
            TechValidator();
            SunType.LoadSunTypes();
        }

        static void TechValidator()
        {
            GameLoadingScreen.SetStatus("TechValidator");
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

            foreach (Technology tech in TechTree.Values)
                tech.ResolveLeadsToTechs();
        }

        static void LoadTechTree()
        {
            bool modTechsOnly = GlobalStats.HasMod && GlobalStats.ActiveModInfo.ClearVanillaTechs;
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
                tech.UpdateTechnologyTypesFromUnlocks();
            }
        }

        static readonly Map<string, Weapon> WeaponsDict = new Map<string, Weapon>();

        // Refactored by RedFox, gets a new weapon instance based on weapon UID
        public static Weapon CreateWeapon(string uid)
        {
            Weapon template = WeaponsDict[uid];
            return template.Clone();
        }

        public static bool CreateWeapon(string uid, out Weapon weapon)
        {
            if (WeaponsDict.TryGetValue(uid, out Weapon template))
            {
                weapon = template.Clone();
                return true;
            }
            weapon = null;
            return false;
        }

        // WARNING: DO NOT MODIFY this Weapon instance! (wish C# has const refs like C++)
        public static Weapon GetWeaponTemplate(string uid)
        {
            return WeaponsDict[uid];
        }

        static void LoadWeapons() // Refactored by RedFox
        {
            WeaponsDict.Clear();
            bool modTechsOnly = GlobalStats.HasMod && GlobalStats.ActiveModInfo.ClearVanillaWeapons;
            foreach (var pair in LoadEntitiesWithInfo<Weapon>("Weapons", "LoadWeapons", modTechsOnly))
            {
                Weapon wep = pair.Entity;
                wep.UID = string.Intern(pair.Info.NameNoExt());
                WeaponsDict[wep.UID] = wep;
                wep.InitializeTemplate();
            }

            foreach (Weapon w in WeaponsDict.Values)
            {
                w.CalcDamagePerSecond();
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
        public static EconomicResearchStrategy GetEconomicStrategy(string name) => EconStrategies[name];
        static void LoadEcoResearchStrats()
        {
            EconStrategies.Clear();
            foreach (var pair in LoadEntitiesWithInfo<EconomicResearchStrategy>("EconomicResearchStrategy", "LoadEconResearchStrats"))
            {
                // the story here: some mods have bugged <Name> refs, so we do manual
                // hand holding to fix their bugs...
                pair.Entity.Name = pair.Info.NameNoExt();
                EconStrategies[pair.Entity.Name] = pair.Entity;
            }
        }


        static readonly Map<SunZone, Array<PlanetCategory>> ZoneDistribution = new Map<SunZone, Array<PlanetCategory>>();
        static readonly Map<BuildRatio, int[]> BuildRatios = new Map<BuildRatio, int[]>();
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
            GameLoadingScreen.SetStatus("PlanetTypes");
            PlanetTypes = YamlParser.DeserializeArray<PlanetType>("PlanetTypes.yaml");
            PlanetTypes.Sort(p => p.Id);
            PlanetTypeMap = new Map<int, PlanetType>(PlanetTypes.Count);
            foreach (PlanetType type in PlanetTypes)
                PlanetTypeMap[type.Id] = type;
        }

        public static PlanetCategory RandomPlanetCategoryFor(SunZone sunZone)
        {
            return ZoneDistribution[sunZone].RandItem();
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
            TryDeserialize("HostileFleets/HostileFleets.xml",    ref HostileFleets);
            TryDeserialize("ShipNames/ShipNames.xml",            ref ShipNames);
            TryDeserialize("MainMenu/MainMenuShipList.xml",      ref MainMenuShipList);
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
    }
}