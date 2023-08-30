using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using SDGraphics;
using SDUtils;
using SynapseGaming.LightingSystem.Core;

namespace Ship_Game;

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
    // Configuration file version
    // If this is different from the baseline exe configuration
    // Then a refresh of the config file will be required
    public static int ConfigVersion;

    // 1.Major.Patch commit
    public static string Version = ""; // "1.30.13000 develop/f83ab4a"
    public static string ExtendedVersion = ""; // "Mars : 1.20.12000 develop/f83ab4a"
    public static string ExtendedVersionNoHash = ""; // "Mars : 1.20.12000"
        
    // Global GamePlay options for BB+ and for Mods which are loaded from Globals.yaml
    public static GamePlayGlobals Defaults;
    public static GamePlayGlobals VanillaDefaults;

    // Active Mod information
    public static ModEntry ActiveMod;
    public static bool HasMod => ActiveMod != null;
    public static string ModFile => ModPath.NotEmpty() ? $"{ModPath}Globals.yaml" : ""; // "Mods/Combined Arms/Globals.yaml"
    public static string ModOrVanillaName => HasMod ? ModName : "Vanilla";
    public static string ModVersion => ActiveMod?.Mod.Version ?? "";

    /// <returns>TRUE if `modName` is compatible with the current ModName</returns>
    public static bool IsValidForCurrentMod(string modName) => modName.IsEmpty() || modName == ModName;

    /// <returns>TRUE if `modName` is compatible with the current Mod setting for UseVanillaShips</returns>
    public static bool IsShipValidForCurrentMod(string modName)
    {
        if (HasMod && !Defaults.Mod.UseVanillaShips) // this mod declares that it's incompatible with vanilla ships
            return modName == ModName; // strict equality is then required

        // this mod declares that it's COMPATIBLE with vanilla ships, so it will be able to load both of them
        return IsValidForCurrentMod(modName);
    }

    // "Combined Arms" or "" if there's no active mod, this name can have special characters
    // WARNING: this is no longer guaranteed to be same as ModPath !! Use this sparingly
    public static string ModName = "";

    // "Mods/Combined Arms/" or "Mods/ExampleMod/", this needs to be a sanitized path
    public static string ModPath = "";


    /// <summary>
    /// only enabled for SDUnitTests, to disable certain features
    /// and some resource loading which is not needed for tests
    /// </summary>
    public static bool IsUnitTest;

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
    // graphics options to turn of background nebula rendering
    // used for testing too
    public static bool DrawNebulas = true;
    public static bool DrawStarfield = true;
        
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
    // debug log all resource loading paths `--resource-debug`
    public static bool DebugResourceLoading;

    // DEV CLI OPTION
    // debug log all Asset load events `--asset-debug`
    public static bool DebugAssetLoading;

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
        Version = (Assembly.GetEntryAssembly()?
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            as AssemblyInformationalVersionAttribute[])?[0].InformationalVersion ?? "";

        ExtendedVersion = $"Mars : {Version}";
        ExtendedVersionNoHash = $"Mars : {Version.Split(' ')[0]}";
            
        var config = OpenUserConfiguration();
        GetSetting(config, "ConfigVersion", ref ConfigVersion);
        GetSetting(config, "RestrictAIPlayerInteraction", ref RestrictAIPlayerInteraction);
        GetSetting(config, "XRES", ref XRES);
        GetSetting(config, "YRES", ref YRES);
        GetSetting(config, "MaxParallelism", ref MaxParallelism);
        GetSetting(config, "WindowMode", ref WindowMode);
        GetSetting(config, "AutoSaveFreq", ref AutoSaveFreq);
        GetSetting(config, "AntiAliasSamples", ref AntiAlias);
        GetSetting(config, "PostProcessBloom", ref RenderBloom);
        GetSetting(config, "VSync", ref VSync);
        GetSetting(config, "TextureQuality", ref TextureQuality);
        GetSetting(config, "TextureSampling", ref TextureSampling);
        GetSetting(config, "MaxAnisotropy", ref MaxAnisotropy);
        GetSetting(config, "ShadowDetail", ref ShadowDetail);
        GetSetting(config, "EffectDetail", ref EffectDetail);
        GetSetting(config, "AutoErrorReport", ref AutoErrorReport);
        GetSetting(config, "ActiveMod", ref ModPath);

        GetSetting(config, "NotifyEmptyPlanetQueue", ref NotifyEmptyPlanetQueue);
        GetSetting(config, "PauseOnNotification", ref PauseOnNotification);
        GetSetting(config, "IconSize", ref IconSize);
        GetSetting(config, "ZoomTracking", ref ZoomTracking);
        GetSetting(config, "CameraPanSpeed", ref CameraPanSpeed);
        GetSetting(config, "AltArcControl", ref AltArcControl);
        GetSetting(config, "DisableAsteroids", ref DisableAsteroids);
        GetSetting(config, "EnableEngineTrails", ref EnableEngineTrails);
        GetSetting(config, "MaxDynamicLightSources", ref MaxDynamicLightSources);
        GetSetting(config, "SimulationFramesPerSecond", ref SimulationFramesPerSecond);
        GetSetting(config, "NotifyEnemyInSystemAfterLoad", ref NotifyEnemyInSystemAfterLoad);

        if (TryGetSetting(config, "MusicVolume", out int musicVol)) MusicVolume = musicVol / 100f;
        if (TryGetSetting(config, "EffectsVolume", out int fxVol)) EffectsVolume = fxVol / 100f;
        GetSetting(config, "SoundDevice", ref SoundDevice);
        GetSetting(config, "Language", ref Language);
        GetSetting(config, "VerboseLogging", ref VerboseLogging);
        GetSetting(config, "TimesPlayed", ref TimesPlayed);

        #if DEBUG
            VerboseLogging = true;
        #endif
        #if AUTOFAST
            RestrictAIPlayerInteraction = true;
        #endif

        Log.Write(ConsoleColor.DarkYellow, "Loaded App Settings");

        // update TimesPlayed stats
        WriteSetting(config, "TimesPlayed", ++TimesPlayed);
        config.Save();

        LoadModInfo(ModPath);
    }

    public static void ClearActiveMod() => LoadModInfo("");

    public static void LoadModInfo(string modPath)
    {
        SetActiveModNoSave(null); // reset

        // always initialize vanilla defaults, it's used as a backup
        if (VanillaDefaults == null)
        {
            var defaultSettings = new FileInfo("Content/Globals.yaml");
            VanillaDefaults = GamePlayGlobals.Deserialize(defaultSettings);
        }

        if (modPath.NotEmpty())
        {
            var modInfo = new FileInfo(Path.Combine(modPath, "Globals.yaml"));
            if (modInfo.Exists)
            {
                GamePlayGlobals settings = GamePlayGlobals.Deserialize(modInfo);
                var me = new ModEntry(settings);
                SetActiveModNoSave(me);
            }
            else
            {
                Log.Warning($"LoadModInfo failed because mod file not found: {modInfo.FullName}");
            }
        }
        else // load vanilla
        {
            Defaults = VanillaDefaults;
        }
        SaveActiveMod();
    }

    public static void SetActiveModNoSave(ModEntry me)
    {
        if (me != null)
        {
            ModName = me.Mod.Name;
            ModPath = me.Mod.Path;
            ActiveMod = me;
            Defaults = me.Settings;

            if (!Directory.Exists(ModPath))
                Log.Error($"SetActiveMod ModPath does not exist: {ModPath}");
        }
        else
        {
            ModName = "";
            ModPath = "";
            ActiveMod = null;
            Defaults = VanillaDefaults;
        }
    }

    static Configuration OpenUserConfiguration()
    {
        string configFile = Dir.StarDriveAppData + "/StarDrive.user.config";
        Configuration exeCfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        // if the AppData config file doesn't exist, create one based on current defaults
        if (!File.Exists(configFile))
        {
            exeCfg.SaveAs(configFile, ConfigurationSaveMode.Full);
        }

        Configuration roamingCfg = ConfigurationManager.OpenMappedExeConfiguration(new()
        {
            ExeConfigFilename = configFile,
        }, ConfigurationUserLevel.None);

        // check if base version has changed, which will require us to overwrite the settings
        int baseVersion = 1;
        GetSetting(exeCfg, "ConfigVersion", ref baseVersion);
        GetSetting(roamingCfg, "ConfigVersion", ref ConfigVersion);
        if (baseVersion != ConfigVersion)
        {
            // upgrade by adding any missing values
            var exeSettings = exeCfg.AppSettings.Settings;
            var roamingSettings = roamingCfg.AppSettings.Settings;
            foreach (string setting in exeSettings.AllKeys)
                if (roamingSettings[setting] == null)
                    roamingSettings.Add(setting, exeSettings[setting].Value);

            // overwrite the version
            roamingSettings["ConfigVersion"].Value = exeSettings["ConfigVersion"].Value;

            // force the exe config to save itself,
            // this should synchronize any changed fields
            roamingCfg.Save(ConfigurationSaveMode.Full);

            // force all Configurations to reload their appSettings
            ConfigurationManager.RefreshSection("appSettings");

            GetSetting(roamingCfg, "ConfigVersion", ref ConfigVersion);
            if (baseVersion != ConfigVersion)
                Log.Error("AppConfig upgrade failed");
        }
        return roamingCfg;
    }

    public static void SaveSettings()
    {
        XRES = StarDriveGame.Instance.Graphics.PreferredBackBufferWidth;
        YRES = StarDriveGame.Instance.Graphics.PreferredBackBufferHeight;

        Configuration config = OpenUserConfiguration();

        WriteSetting(config, "ConfigVersion", ConfigVersion);
        WriteSetting(config, "RestrictAIPlayerInteraction", RestrictAIPlayerInteraction);
        WriteSetting(config, "XRES", XRES);
        WriteSetting(config, "YRES", YRES);
        WriteSetting(config, "MaxParallelism", MaxParallelism);
        WriteSetting(config, "WindowMode", WindowMode);
        WriteSetting(config, "AutoSaveFreq", AutoSaveFreq);
        WriteSetting(config, "AntiAliasSamples", AntiAlias);
        WriteSetting(config, "PostProcessBloom", RenderBloom);
        WriteSetting(config, "VSync", VSync);
        WriteSetting(config, "TextureQuality", TextureQuality);
        WriteSetting(config, "TextureSampling", TextureSampling);
        WriteSetting(config, "MaxAnisotropy", MaxAnisotropy);
        WriteSetting(config, "ShadowDetail", ShadowDetail);
        WriteSetting(config, "EffectDetail", EffectDetail);
        WriteSetting(config, "AutoErrorReport", AutoErrorReport);
        WriteSetting(config, "ActiveMod", ModPath);

        WriteSetting(config, "NotifyEmptyPlanetQueue", NotifyEmptyPlanetQueue);
        WriteSetting(config, "PauseOnNotification", PauseOnNotification);
        WriteSetting(config, "IconSize", IconSize);
        WriteSetting(config, "ZoomTracking", ZoomTracking);
        WriteSetting(config, "CameraPanSpeed", CameraPanSpeed);
        WriteSetting(config, "AltArcControl", AltArcControl);
        WriteSetting(config, "DisableAsteroids", DisableAsteroids);
        WriteSetting(config, "EnableEngineTrails", EnableEngineTrails);
        WriteSetting(config, "MaxDynamicLightSources", MaxDynamicLightSources);
        WriteSetting(config, "SimulationFramesPerSecond", SimulationFramesPerSecond);
        WriteSetting(config, "NotifyEnemyInSystemAfterLoad", NotifyEnemyInSystemAfterLoad);

        WriteSetting(config, "MusicVolume", (int)(MusicVolume * 100));
        WriteSetting(config, "EffectsVolume", (int)(EffectsVolume * 100));
        WriteSetting(config, "SoundDevice", SoundDevice);
        WriteSetting(config, "Language", Language);
        WriteSetting(config, "VerboseLogging", VerboseLogging);

        config.Save();
    }

    public static void SaveActiveMod()
    {
        var config = OpenUserConfiguration();
        WriteSetting(config, "ActiveMod", ModPath);
        config.Save();
    }


    delegate bool ParseMethod<T>(string s, out T value);

    // Only assigns the ref parameter is parsing succeeds. This avoid overwriting default values
    static void GetSetting<T>(Configuration config, string name, ref T maybeOut, ParseMethod<T> parser)
    {
        string setting = GetSetting(config, name);
        if (parser(setting, out T parsed))
        {
            maybeOut = parsed;
        }
    }

    static void GetSetting(Configuration config, string name, ref float maybeOut) => GetSetting(config, name, ref maybeOut, float.TryParse);
    static void GetSetting(Configuration config, string name, ref int maybeOut) => GetSetting(config, name, ref maybeOut, int.TryParse);
    static void GetSetting(Configuration config, string name, ref bool maybeOut) => GetSetting(config, name, ref maybeOut, bool.TryParse);
    static void GetSetting(Configuration config, string name, ref string maybeOut)
    {
        GetSetting(config, name, ref maybeOut, 
            (string s, out string parsed) => (parsed = !string.IsNullOrEmpty(s) ? s : null) != null);
    }
    static void GetSetting<T>(Configuration config, string name, ref T maybeOut) where T : struct
    {
        GetSetting(config, name, ref maybeOut, Enum.TryParse<T>);
    }

    static bool TryGetSetting(Configuration config, string name, out int value)
    {
        return int.TryParse(GetSetting(config, name), out value);
    }

    static string GetSetting(Configuration config, string name)
    {
        var element = config.AppSettings.Settings[name];
        return element?.Value;
    }


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