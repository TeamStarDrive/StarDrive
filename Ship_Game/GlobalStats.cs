using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using SDGraphics;
using Ship_Game.Data.Yaml;
using SynapseGaming.LightingSystem.Core;

namespace Ship_Game
{
    public enum Language
    {
        English,
        Russian,
        Spanish
    }

    public enum WindowMode
    {
        Fullscreen,
        Windowed,
        Borderless
    }

    /// <summary>
    /// This contains immutable global settings and also contains
    /// global options which can be changed from game OptionsScreen
    ///
    /// It should NOT contain any Universe related configurable parameters,
    /// @see UniverseParams for Universe persistent state
    /// </summary>
    public static class GlobalStats
    {
        // 1.Major.Patch commit
        public static string Version = ""; // "1.30.13000 develop/f83ab4a"
        public static string ExtendedVersion = ""; // "Mars : 1.20.12000 develop/f83ab4a"
        public static string ExtendedVersionNoHash = ""; // "Mars : 1.20.12000"
        
        // Global GamePlay options for BB+ and for Mods which are loaded from Globals.yaml
        public static GamePlayGlobals Settings;
        public static GamePlayGlobals DefaultSettings;

        // Active Mod information
        public static ModEntry ActiveMod;
        public static bool HasMod => ActiveMod != null;
        public static string ModName = ""; // "Combined Arms" or "" if there's no active mod
        public static string ModPath = ""; // "Mods/Combined Arms/"
        public static string ModFile => ModPath.NotEmpty() ? $"{ModPath}{ModName}.xml" : ""; // "Mods/Combined Arms/Combined Arms.xml"
        public static string ModOrVanillaName => HasMod ? ModName : "Vanilla";
        
        // TODO: get rid of this global state variable
        public static bool TakingInput = false;
        
        // statistics for # of times player has launched the game
        public static int TimesPlayed;


        ////////////////////////////////////////////////////////////////
        //////// PERF Settings which help to increase performance
        
        // PERF global option to limit dynamic lights in scenes, thus increasing performance
        public static int MaxDynamicLightSources = 100;

        // PERF global option to Disable asteroids for increased performance
        public static bool DisableAsteroids;

        // PERF this is a graphics performance toggle, disabling engine trails makes everything much faster
        public static bool EnableEngineTrails = true;
        
        // PERF
        // global option for controlling physics simulation interval, bigger is slower but more precise
        public static int SimulationFramesPerSecond = 60;


        ////////////////////////////////////////////////////////////////
        //////// USER_EXPERIENCE Settings that modify user experience
        //////// via inputs or visuals, not related to graphics or perf
        
        // USER_EXPERIENCE
        // automatic error reporting via Sentry.io
        public static bool AutoErrorReport = true;
        
        // USER_EXPERIENCE
        // global option, if enabled, scrolling will always zoom into selected objects
        // Otherwise you can use Shift+Scroll to zoom to selected objects
        public static bool ZoomTracking;
        
        // USER_EXPERIENCE
        // global option how fast the camera pans across the universe when using WASD keys
        public static float CameraPanSpeed = 2;

        // USER_EXPERIENCE
        // global option for keyboard hotkey based arc movement
        public static bool AltArcControl; // "Keyboard Fire Arc Locking"
        
        // USER_EXPERIENCE
        // global option for notifying when planet construction queue is empty
        public static bool NotifyEmptyPlanetQueue;

        // USER_EXPERIENCE
        // global option for pausing on notifications, default should be OFF
        public static bool PauseOnNotification;
        
        // USER_EXPERIENCE
        // global option for Icon size
        public static int IconSize;

        // USER_EXPERIENCE
        // autosave frequency in seconds
        public static int AutoSaveFreq = 300;

        // USER_EXPERIENCE
        // this is a global user setting, changed in game settings screen
        public static bool NotifyEnemyInSystemAfterLoad = true;

        // USER_EXPERIENCE
        // global option for Ships will try to keep their distance from nearby friends to prevent stacking
        public static bool EnableShipFlocking = true;
        

        ////////////////////////////////////////////////////////////////
        //////// DEV Constants for experimental features

        // DEV CONSTANT ONLY
        // If true, use software cursors (rendered by the game engine)
        // otherwise use OS Cursor (rendered by the OS ontop of current window)
        public const bool UseSoftwareCursor = true;
        
        // DEV CONSTANT ONLY
        // graphics options to turn of background nebula rendering, used for testing too
        public const bool DrawNebulas = true;
        public const bool DrawStarfield = true;
        
        // DEV CONSTANT ONLY
        // If TRUE, then all ShipDesign's DesignSlot[] arrays will be lazy-loaded on demand
        // this is done to greatly reduce memory usage
        // TODO: This is still experimental, since Ship Templates also need to be lazily instantiated
        public const bool LazyLoadShipDesignSlots = false;


        ////////////////////////////////////////////////////////////////
        //////// DEV Command Line Options

        // DEV CLI OPTION
        // If TRUE, the game will attempt to convert any old XML Hull Designs
        // into new .hull designs. This should only be enabled on demand because it's slow.
        public static bool GenerateNewHullFiles = false;
        
        // DEV CLI OPTION
        // If TRUE, the game will attempt to convert any old XML SHIP Designs
        // into new .design files. This should only be enabled on demand because it's slow.
        public static bool GenerateNewShipDesignFiles = false;
        
        // DEV CLI OPTION
        // If enabled, this will fix all .design file's Role and Category fields
        // modifying all ship designs
        public static bool FixDesignRoleAndCategory = false;
                
        // DEV CLI OPTION
        // export all XNB and PNG textures into StarDrive/ExportedTextures
        public static bool ExportTextures;

        // DEV CLI OPTION
        // export all XNB meshes into StarDrive/ExportedMeshes into "obj" or "fbx"
        public static string ExportMeshes;

        // DEV CLI OPTION
        // process all localization files
        public static int RunLocalizer;

        // DEV CLI OPTION
        // Continue into the game after running Localizer or other Tools
        public static bool ContinueToGame;


        ////////////////////////////////////////////////////////////////
        //////// DEV AppConfig Options

        // DEV APP CONFIG OPTION
        // Dev Option for AUTOPERF
        public static bool RestrictAIPlayerInteraction;

        // DEV APP CONFIG OPTION
        // Debug log options
        public static bool VerboseLogging;
        
        // DEV APP CONFIG OPTION
        // Concurrency and Parallelism options
        // Unlimited Parallelism: <= 0
        // Single Threaded: == 1
        // Limited Parallelism: > 1
        public static int MaxParallelism = -1;


        ////////////////////////////////
        //////// GRAPHICS OPTIONS
        public static int XRES = 1920;
        public static int YRES = 1080;
        public static WindowMode WindowMode = WindowMode.Fullscreen;
        public static int AntiAlias = 2;
        public static bool RenderBloom = true;
        public static bool VSync = true;
        // Render quality & detail options
        public static int TextureQuality;      // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public static int TextureSampling = 2; // 0=Bilinear, 1=Trilinear, 2=Anisotropic
        public static int MaxAnisotropy = 2;   // # of samples, only applies with TextureSampling = 2
        public static int ShadowDetail = 3;    // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public static int EffectDetail;        // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public static ObjectVisibility ShipVisibility = ObjectVisibility.Rendered;
        public static ObjectVisibility AsteroidVisibility = ObjectVisibility.Rendered;

        
        ////////////////////////////////
        // Music, Sound & Language settings
        public static float MusicVolume   = 0.7f;
        public static float EffectsVolume = 1f;
        public static string SoundDevice  = "Default"; // Use windows default device if not explicitly specified

        // Language options
        public static Language Language = Language.English;
        public static bool IsEnglish => Language == Language.English;
        public static bool IsRussian => Language == Language.Russian;

        public static void SetShadowDetail(int shadowDetail)
        {
            // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
            ShadowDetail = shadowDetail.Clamped(0, 3);

            ShipVisibility = ObjectVisibility.Rendered;
            if (ShadowDetail <= 1) ShipVisibility = ObjectVisibility.RenderedAndCastShadows;

            if (AsteroidVisibility != ObjectVisibility.None)
            {
                AsteroidVisibility = ObjectVisibility.Rendered;
                if (ShadowDetail <= 0) AsteroidVisibility = ObjectVisibility.RenderedAndCastShadows;
            }
        }

        public static float GetShadowQuality(int shadowDetail)
        {
            switch (shadowDetail) // 1.0f highest, 0.0f lowest
            {
                case 0: return 1.00f; // 0: High
                case 1: return 0.66f; // 1: Medium
                case 2: return 0.33f; // 2: Low
                default:
                case 3: return 0.00f; // 3: Off
            }
        }

        public static void LoadConfig()
        {
            try
            {
                NameValueCollection mgr = ConfigurationManager.AppSettings;
            }
            catch (ConfigurationErrorsException)
            {
                return; // configuration file is missing
            }

            Version = (Assembly.GetEntryAssembly()?
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                as AssemblyInformationalVersionAttribute[])?[0].InformationalVersion ?? "";

            ExtendedVersion       = $"Mars : {Version}";
            ExtendedVersionNoHash = $"Mars : {Version.Split(' ')[0]}";

            GetSetting("TimesPlayed"           , ref TimesPlayed);
            GetSetting("perf"                  , ref RestrictAIPlayerInteraction);
            GetSetting("AutoSaveFreq"          , ref AutoSaveFreq);
            GetSetting("WindowMode"            , ref WindowMode);
            GetSetting("AntiAliasSamples"      , ref AntiAlias);
            GetSetting("PostProcessBloom"      , ref RenderBloom);
            GetSetting("VSync"                 , ref VSync);
            GetSetting("TextureQuality"        , ref TextureQuality);
            GetSetting("TextureSampling"       , ref TextureSampling);
            GetSetting("MaxAnisotropy"         , ref MaxAnisotropy);
            GetSetting("ShadowDetail"          , ref ShadowDetail);
            GetSetting("EffectDetail"          , ref EffectDetail);
            GetSetting("AutoErrorReport"       , ref AutoErrorReport);
            GetSetting("ActiveMod"             , ref ModName);
            GetSetting("CameraPanSpeed"        , ref CameraPanSpeed);
            GetSetting("VerboseLogging"        , ref VerboseLogging);

            Statreset();

        #if DEBUG
            VerboseLogging = true;
        #endif
        #if AUTOFAST
            RestrictAIPlayerInteraction = true;
        #endif

            if (int.TryParse(GetSetting("MusicVolume"), out int musicVol)) MusicVolume = musicVol / 100f;
            if (int.TryParse(GetSetting("EffectsVolume"), out int fxVol))  EffectsVolume = fxVol / 100f;
            GetSetting("SoundDevice", ref SoundDevice);
            GetSetting("Language", ref Language);
            GetSetting("MaxParallelism", ref MaxParallelism);
            GetSetting("XRES", ref XRES);
            GetSetting("YRES", ref YRES);

            // update TimesPlayed stats
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            WriteSetting(config, "TimesPlayed", ++TimesPlayed);

            LoadModInfo(ModName);
            Log.Info(ConsoleColor.DarkYellow, "Loaded App Settings");
        }

        public static void ClearActiveMod() => LoadModInfo("");

        public static void LoadModInfo(string modName)
        {
            SetActiveModNoSave(null); // reset

            if (modName.NotEmpty())
            {
                var modInfo = new FileInfo($"Mods/{modName}/Globals.yaml");
                if (modInfo.Exists)
                {
                    var settings = YamlParser.DeserializeOne<GamePlayGlobals>(modInfo);
                    var me = new ModEntry(settings);
                    SetActiveModNoSave(me);
                }
            }
            else // load vanilla
            {
                if (DefaultSettings == null)
                {
                    var defaultSettings = new FileInfo("Content/Globals.yaml");
                    DefaultSettings = YamlParser.DeserializeOne<GamePlayGlobals>(defaultSettings);
                }

                Settings = DefaultSettings;
            }
            SaveActiveMod();
        }

        public static void SetActiveModNoSave(ModEntry me)
        {
            if (me != null)
            {
                ModName = me.ModName;
                ModPath = "Mods/" + ModName + "/";
                ActiveMod = me;
                Settings = me.Settings;
            }
            else
            {
                ModName = "";
                ModPath = "";
                ActiveMod = null;
                Settings = DefaultSettings;
            }

        }

        public static void Statreset()
        {
            GetSetting("NotifyEmptyPlanetQueue", ref NotifyEmptyPlanetQueue);
            GetSetting("PauseOnNotification",  ref PauseOnNotification);
            GetSetting("IconSize",             ref IconSize);
            GetSetting("ZoomTracking",         ref ZoomTracking);
            GetSetting("AltArcControl",        ref AltArcControl);
            GetSetting("DisableAsteroids",     ref DisableAsteroids);
            GetSetting("EnableEngineTrails",   ref EnableEngineTrails);
            GetSetting("MaxDynamicLightSources", ref MaxDynamicLightSources);
            GetSetting("SimulationFramesPerSecond", ref SimulationFramesPerSecond);
            GetSetting("NotifyEnemyInSystemAfterLoad", ref NotifyEnemyInSystemAfterLoad);
        }

        public static void SaveSettings()
        {
            XRES = StarDriveGame.Instance.Graphics.PreferredBackBufferWidth;
            YRES = StarDriveGame.Instance.Graphics.PreferredBackBufferHeight;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            
            WriteSetting(config, "perf", RestrictAIPlayerInteraction);
            WriteSetting(config, "AutoSaveFreq",     AutoSaveFreq);
            WriteSetting(config, "WindowMode",       WindowMode);
            WriteSetting(config, "AntiAliasSamples", AntiAlias);
            WriteSetting(config, "PostProcessBloom", RenderBloom);
            WriteSetting(config, "VSync",            VSync);
            WriteSetting(config, "TextureQuality",   TextureQuality);
            WriteSetting(config, "TextureSampling",  TextureSampling);
            WriteSetting(config, "MaxAnisotropy",    MaxAnisotropy);
            WriteSetting(config, "ShadowDetail",     ShadowDetail);
            WriteSetting(config, "EffectDetail",     EffectDetail);
            WriteSetting(config, "AutoErrorReport",  AutoErrorReport);
            WriteSetting(config, "ActiveMod",        ModName);

            WriteSetting(config, "NotifyEmptyPlanetQueue", NotifyEmptyPlanetQueue);
            WriteSetting(config, "PauseOnNotification", PauseOnNotification);
            WriteSetting(config, "IconSize",            IconSize);
            WriteSetting(config, "ZoomTracking",        ZoomTracking);
            WriteSetting(config, "AltArcControl",       AltArcControl);
            WriteSetting(config, "DisableAsteroids",    DisableAsteroids);
            WriteSetting(config, "EnableEngineTrails",  EnableEngineTrails);
            WriteSetting(config, "MaxDynamicLightSources", MaxDynamicLightSources);
            WriteSetting(config, "SimulationFramesPerSecond", SimulationFramesPerSecond);
            WriteSetting(config, "NotifyEnemyInSystemAfterLoad", NotifyEnemyInSystemAfterLoad);

            WriteSetting(config, "MusicVolume",   (int)(MusicVolume * 100));
            WriteSetting(config, "EffectsVolume", (int)(EffectsVolume * 100));
            WriteSetting(config, "SoundDevice",    SoundDevice);
            WriteSetting(config, "Language",       Language);
            WriteSetting(config, "MaxParallelism", MaxParallelism);
            WriteSetting(config, "XRES",           XRES);
            WriteSetting(config, "YRES",           YRES);
            WriteSetting(config, "CameraPanSpeed", CameraPanSpeed);
            WriteSetting(config, "VerboseLogging", VerboseLogging);

            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void SaveActiveMod()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            WriteSetting(config, "ActiveMod", ModName);
            config.Save();
        }


        // Only assigns the ref parameter is parsing succeeds. This avoid overwriting default values
        static bool GetSetting(string name, ref float f)
        {
            if (!float.TryParse(ConfigurationManager.AppSettings[name], out float v)) return false;
            f = v;
            return true;
        }
        static bool GetSetting(string name, ref int i)
        {
            if (!int.TryParse(ConfigurationManager.AppSettings[name], out int v)) return false;
            i = v;
            return true;
        }
        static bool GetSetting(string name, ref bool b)
        {
            if (!bool.TryParse(ConfigurationManager.AppSettings[name], out bool v)) return false;
            b = v;
            return true;
        }
        static bool GetSetting(string name, ref string s)
        {
            string v = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrEmpty(v)) return false;
            s = v;
            return true;
        }
        static bool GetSetting<T>(string name, ref T e) where T : struct
        {
            if (!Enum.TryParse(ConfigurationManager.AppSettings[name], out T v)) return false;
            e = v;
            return true;
        }
        static string GetSetting(string name) => ConfigurationManager.AppSettings[name];



        static void WriteSetting(Configuration config, string name, float v)
        {
            WriteSetting(config, name, v.ToString(CultureInfo.InvariantCulture));
        }
        static void WriteSetting<T>(Configuration config, string name, T v) where T : struct
        {
            WriteSetting(config, name, v.ToString());
        }
        static void WriteSetting(Configuration config, string name, string value)
        {
            var setting = config.AppSettings.Settings[name];
            if (setting != null) setting.Value = value;
            else config.AppSettings.Settings.Add(name, value);
        }
    }
}
