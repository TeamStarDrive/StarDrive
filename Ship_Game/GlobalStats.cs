using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
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

    public static class GlobalStats
    {
        public static string Version = "";
        public static string ExtendedVersion = "";
        public static string ExtendedVersionNoHash = "";

        public static int ComparisonCounter = 1;
        public static int Comparisons = 0;
        public static bool TakingInput = false;
        public static bool WarpInSystem = true;
        public static float FTLInSystemModifier = 1f;
        public static float EnemyFTLInSystemModifier = 1f;

        //case control for UIDs set by filename. Legacy support.
        public static StringComparer CaseControl;

        // @todo Get rid of all global locks
        public static object ClickableSystemsLock = new object();
        public static object ClickableItemLocker  = new object();
        public static object FleetButtonLocker    = new object();
        public static object BeamEffectLocker     = new object();

        public static bool ShowAllDesigns        = true;
        public static bool SymmetricDesign       = true;
        public static bool FilterOldModules      = true;
        public static int BeamTests              = 0;
        public static int ModuleUpdates          = 0;
        public static int WeaponArcChecks        = 0;
        public static int DistanceCheckTotal     = 0;
        public static bool LimitSpeed            = true;
        public static float GravityWellRange;
        public static bool PlanetaryGravityWells = true;
        public static float CustomMineralDecay   = 1;
        public static float VolcanicActivity     = 1;

        // Option for keyboard hotkey based arc movement
        public static bool AltArcControl; // "Keyboard Fire Arc Locking"
        public static int TimesPlayed = 0;
        public static ModEntry ActiveMod;
        public static bool HasMod => ActiveMod != null;
        public static ModInformation ActiveModInfo;
        public static string ModName = ""; // "Combined Arms"
        public static string ModPath = ""; // "Mods/Combined Arms/"
        public static string ModFile => ModPath.NotEmpty() ? $"{ModPath}{ModName}.xml" : ""; // "Mods/Combined Arms/Combined Arms.xml"
        public static string ModOrVanillaName => HasMod ? ModName : "Vanilla";
        public static string ResearchRootUIDToDisplay = "Colonization";
        public static bool CordrazinePlanetCaptured;

        public static bool ExtraNotifications;
        public static bool PauseOnNotification;
        public static int ExtraPlanets;
        public static bool DisablePirates;
        public static bool DisableRemnantStory;
        public static float ShipMaintenanceMulti = 1;
        public static float MinimumWarpRange;

        public static float StartingPlanetRichness;
        public static int IconSize;
        public static int TurnTimer = 5;

        public static bool PreventFederations;
        public static bool EliminationMode;
        public static bool ZoomTracking;
        public static bool AutoErrorReport = true; // automatic error reporting via Sentry.io

        public static bool DisableAsteroids;
        public static bool FixedPlayerCreditCharge;
        public static bool NotifyEnemyInSystemAfterLoad = true;

        public static int AutoSaveFreq = 300;   //Added by Gretman
        public static ExtraRemnantPresence ExtraRemnantGS;

        // global sticky checkboxes the player changes in game
        public static bool SuppressOnBuildNotifications;
        public static bool PlanetScreenHideOwned;
        public static bool PlanetsScreenHideUnhabitable = true;
        public static bool ShipListFilterPlayerShipsOnly;
        public static bool ShipListFilterInFleetsOnly;
        public static bool ShipListFilterNotInFleets;
        public static bool DisableInhibitionWarning = true;
        public static bool DisableVolcanoWarning;

        public static int CameraPanSpeed    = 2;
        public static float DamageIntensity = 1;

        // Simulation options
        public static bool UnlimitedSpeed = false;
        public static int SimulationFramesPerSecond = 60; // RedFox: physics simulation interval, bigger is slower

        // Dev Options
        public static bool RestrictAIPlayerInteraction;
        public static bool DisableAIEmpires;
        public static bool EnableSaveExportButton = true;

        ////////////////////////////////
        // From old Config
        public static int XRES = 1920;
        public static int YRES = 1080;
        public static WindowMode WindowMode = WindowMode.Fullscreen;
        public static int AntiAlias       = 2;
        public static bool RenderBloom    = true;
        public static bool VSync          = true;
        public static float MusicVolume   = 0.7f;
        public static float EffectsVolume = 1f;
        public static string SoundDevice  = "Default"; // Use windows default device if not explicitly specified
        public static Language Language   = Language.English;

        // Render options
        public static int TextureQuality;         // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public static int TextureSampling = 2;    // 0=Bilinear, 1=Trilinear, 2=Anisotropic
        public static int MaxAnisotropy   = 2;    // # of samples, only applies with TextureSampling = 2
        public static int ShadowDetail    = 3;    // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public static int EffectDetail;           // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public static ObjectVisibility ShipVisibility     = ObjectVisibility.Rendered;
        public static ObjectVisibility AsteroidVisibility = ObjectVisibility.Rendered;

        public static bool DrawNebulas    = true;
        public static bool DrawStarfield  = true;

        // Language options
        public static bool IsEnglish => Language == Language.English;
        public static bool IsRussian => Language == Language.Russian;
        public static bool IsSpanish => Language == Language.Spanish;
        public static bool NotEnglish => Language != Language.English;

        // Debug log options
        public static bool VerboseLogging;
        public static bool TestLoad;
        public static bool PreLoad;

        // Concurrency and Parallelism options
        // Unlimited Parallelism: <= 0
        // Single Threaded: == 1
        // Limited Parallelism: > 1
        public static int MaxParallelism = -1;

        // Unsupported Experimental options
        public static float Unsupported_ProjectorRadius = -1;

        public static bool ExportTextures; // export all XNB and PNG textures into StarDrive/ExportedTextures
        public static bool ExportMeshes; // export all XNB meshes into StarDrive/ExportedMeshes
        public static bool RunLocalizer; // process all localization files

        public static void SetShadowDetail(int shadowDetail)
        {
            // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
            ShadowDetail = shadowDetail.Clamped(0, 3);
            ShipVisibility     = ObjectVisibility.Rendered;
            AsteroidVisibility = ObjectVisibility.Rendered;
            if (ShadowDetail <= 1) ShipVisibility     = ObjectVisibility.RenderedAndCastShadows;
            if (ShadowDetail <= 0) AsteroidVisibility = ObjectVisibility.RenderedAndCastShadows;
        }

        public static bool ModChangeResearchCost => HasMod && ActiveModInfo.ChangeResearchCostBasedOnSize;

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
            ExtendedVersion       = $"BlackBox : {Version}";
            ExtendedVersionNoHash = $"BlackBox : {Version.Split(' ')[0]}";

            GetSetting("GravityWellRange"      , ref GravityWellRange);
            GetSetting("StartingPlanetRichness", ref StartingPlanetRichness);
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
            GetSetting("TestLoad"              , ref TestLoad);
            GetSetting("PreLoad"               , ref PreLoad);
            GetSetting("DamageIntensity"       , ref DamageIntensity);
            GetSetting("DisableAIEmpires"      , ref DisableAIEmpires);
            
            // unsupported
            GetSetting("Unsupported_ProjectorRadius", ref Unsupported_ProjectorRadius);
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
            if (bool.TryParse(GetSetting("UIDCaseCheck"), out bool checkForCase))
                CaseControl = checkForCase ? null : StringComparer.OrdinalIgnoreCase;

            LoadModInfo(ModName);
            Log.Info(ConsoleColor.DarkYellow, "Loaded App Settings");
        }

        public static void ClearActiveMod() => LoadModInfo("");

        public static void LoadModInfo(string modName)
        {
            SetActiveModNoSave(null); // reset

            if (modName.NotEmpty())
            {
                var modInfo = new FileInfo($"Mods/{modName}/{modName}.xml");
                if (modInfo.Exists)
                {
                    var info = new XmlSerializer(typeof(ModInformation)).Deserialize<ModInformation>(modInfo);
                    var me = new ModEntry(info);
                    SetActiveModNoSave(me);
                }
            }
            SaveActiveMod();
        }

        public static void SetActiveModNoSave(ModEntry me)
        {
            if (me != null)
            {
                ModName       = me.ModName;
                ModPath       = "Mods/" + ModName + "/";
                ActiveModInfo = me.mi;
                ActiveMod     = me;
            }
            else
            {
                ModName       = "";
                ModPath       = "";
                ActiveMod     = null;
                ActiveModInfo = null;
            }

        }

        public static void Statreset()
        {
            GetSetting("ExtraNotifications",   ref ExtraNotifications);
            GetSetting("PauseOnNotification",  ref PauseOnNotification);
            GetSetting("ExtraPlanets",         ref ExtraPlanets);
            GetSetting("MinimumWarpRange",     ref MinimumWarpRange);
            GetSetting("ShipMaintenanceMulti", ref ShipMaintenanceMulti);
            GetSetting("IconSize",             ref IconSize);
            GetSetting("preventFederations",   ref PreventFederations);
            GetSetting("EliminationMode",      ref EliminationMode);
            GetSetting("ZoomTracking",         ref ZoomTracking);
            GetSetting("TurnTimer",            ref TurnTimer);
            GetSetting("AltArcControl",        ref AltArcControl);
            GetSetting("LimitSpeed",           ref LimitSpeed);
            GetSetting("DisableAsteroids",     ref DisableAsteroids);
            GetSetting("SimulationFramesPerSecond", ref SimulationFramesPerSecond);
            GetSetting("NotifyEnemyInSystemAfterLoad", ref NotifyEnemyInSystemAfterLoad);
            GetSetting("NotifyEnemyInSystemAfterLoad", ref EnableSaveExportButton);
        }

        public static void SaveSettings()
        {
            XRES = StarDriveGame.Instance.Graphics.PreferredBackBufferWidth;
            YRES = StarDriveGame.Instance.Graphics.PreferredBackBufferHeight;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            WriteSetting(config, "GravityWellRange",       GravityWellRange);
            WriteSetting(config, "StartingPlanetRichness", StartingPlanetRichness);
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

            WriteSetting(config, "ExtraNotifications",  ExtraNotifications);
            WriteSetting(config, "PauseOnNotification", PauseOnNotification);
            WriteSetting(config, "ExtraPlanets",        ExtraPlanets);
            WriteSetting(config, "MinimumWarpRange",    MinimumWarpRange);
            WriteSetting(config, "ShipMaintenanceMulti",ShipMaintenanceMulti);
            WriteSetting(config, "IconSize",            IconSize);
            WriteSetting(config, "PreventFederations",  PreventFederations);
            WriteSetting(config, "EliminationMode",     EliminationMode);
            WriteSetting(config, "ZoomTracking",        ZoomTracking);
            WriteSetting(config, "TurnTimer",           TurnTimer);
            WriteSetting(config, "AltArcControl",       AltArcControl);
            WriteSetting(config, "LimitSpeed",          LimitSpeed);
            WriteSetting(config, "DisableAsteroids",    DisableAsteroids);
            WriteSetting(config, "DisableAsteroids",    EnableSaveExportButton);
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
            WriteSetting(config, "TestLoad",       TestLoad);
            WriteSetting(config, "PreLoad",        PreLoad);

            // Unsupported_ProjectorRadius
            WriteSetting(config, "Unsupported_ProjectorRadius", Unsupported_ProjectorRadius);

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