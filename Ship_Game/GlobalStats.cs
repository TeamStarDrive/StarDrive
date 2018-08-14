using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
    public enum Language
    {
        English,
        French,
        German,
        Polish,
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

        public static int ComparisonCounter = 1;
        public static int Comparisons = 0;
        public static bool HardcoreRuleset = false;
        public static bool TakingInput = false;
        public static bool WarpInSystem = true;
        public static float FTLInSystemModifier = 1f;
        public static float EnemyFTLInSystemModifier = 1f;

        //case control for UIDs set by filename. Legacy support.
        public static StringComparer CaseControl;

        // @todo Get rid of all global locks
        public static object ClickableSystemsLock = new object();
        public static object ExplosionLocker      = new object();
        public static object OwnedPlanetsLock     = new object();
        public static object ClickableItemLocker  = new object();
        public static object TaskLocker           = new object();
        public static object FleetButtonLocker    = new object();
        public static object BeamEffectLocker     = new object();

        public static bool ShowAllDesigns        = true;
        public static int ModulesMoved           = 0;
        public static int DSCombatScans          = 0;
        public static int BeamTests              = 0;
        public static int ModuleUpdates          = 0;
        public static int WeaponArcChecks        = 0;
        public static int CombatScans            = 0;
        public static int DistanceCheckTotal     = 0;
        public static bool LimitSpeed            = true;
        public static float GravityWellRange;
        public static bool PlanetaryGravityWells = true;
        public static bool AutoCombat            = true;

        // Option for keyboard hotkey based arc movement
        public static bool AltArcControl; // "Keyboard Fire Arc Locking"
        public static int TimesPlayed                 = 0;
        public static ModEntry ActiveMod;
        public static bool HasMod                     => ActiveMod != null;
        public static ModInformation ActiveModInfo;
        public static string ModName                  = "";
        public static string ModPath                  = ""; // "Mods/MyMod/"
        public static string ResearchRootUIDToDisplay = "Colonization";
        public static int RemnantKills;
        public static int RemnantActivation;
        public static bool RemnantArmageddon          = false;
        public static int CordrazinePlanetsCaptured;

        public static bool ExtraNotifications;
        public static bool PauseOnNotification;
        public static int ExtraPlanets;
        public static float ShipMaintenanceMulti = 1;
        public static float MinimumWarpRange;

        public static float StartingPlanetRichness;
        public static int IconSize;
        public static int TurnTimer = 5;

        public static bool PreventFederations;
        public static bool EliminationMode;
        public static bool ZoomTracking;
        public static bool AutoErrorReport = true; // automatic error reporting via Sentry.io
        public static bool UnlimitedSpeed = false;

        public static bool WarpBehaviorsSetting;
        public static bool WarpBehaviorsEnabled => WarpBehaviorsSetting
                                                || ActiveModInfo?.UseShieldWarpBehavior == true;
        public static bool DisableAsteroids;

        public static int ShipCountLimit;
        public static float spaceroadlimit          = .025f;
        public static int FreighterLimit            = 50;
        public static int ScriptedTechWithin        = 6;
        
        public static float DefensePlatformLimit    = .025f;
        public static ReaderWriterLockSlim UiLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public static int BeamOOM                   = 0;
        public static string bugTracker             = "";

        public static int AutoSaveFreq = 300;   //Added by Gretman
        public static bool CornersGame = false;     //Also added by Gretman
        public static int ExtraRemnantGS;

        public static int CameraPanSpeed    = 2;
        public static float DamageIntensity = 1;

        //Dev Options
        public static bool RestrictAIPlayerInteraction;
        public static bool DisableAIEmpires;

        ////////////////////////////////
        // From old Config
        public static int XRES;
        public static int YRES;
        public static WindowMode WindowMode = WindowMode.Fullscreen;
        public static bool ForceFullSim   = true;
        public static int AntiAlias       = 2;
        public static bool RenderBloom    = true;
        public static float MusicVolume   = 0.7f;
        public static float EffectsVolume = 1f;
        public static Language Language   = Language.English;

        // Render options
        public static int TextureQuality;    //0=High, 1=Medium, 2=Low, 3=Off
        public static int TextureSampling = 2;    //0=Bilinear, 1=Trilinear, 2=Anisotropic
        public static int MaxAnisotropy   = 2;    //# of samples, only applies with TextureSampling = 2
        public static float ShadowQuality = 1.0f; //1.0f highest, 0.0f lowest
        public static int ShadowDetail;    //0=High, 1=Medium, 2=Low, 3=Off
        public static int EffectDetail;    //0=High, 1=Medium, 2=Low, 3=Off
        public static bool DrawNebulas    = true;
        public static bool DrawStarfield  = true;

        public static bool IsEnglish => Language == Language.English;
        public static bool IsFrench  => Language == Language.French;
        public static bool IsGerman  => Language == Language.German;
        public static bool IsPolish  => Language == Language.Polish;
        public static bool IsRussian => Language == Language.Russian;
        public static bool IsSpanish => Language == Language.Spanish;

        public static bool IsGermanOrPolish       => IsGerman || IsPolish;
        public static bool IsGermanFrenchOrPolish => IsGerman || IsPolish || IsFrench;

        public static bool NotEnglish             => Language != Language.English;
        public static bool NotGerman              => Language != Language.German;
        public static bool NotEnglishOrSpanish    => IsGerman || IsPolish || IsRussian || IsFrench;

        ////////////////////////////////
        /// debug log info
        /// 
        public static bool VerboseLogging;
        public static bool TestLoad;
        public static bool PreLoad;

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
                as AssemblyInformationalVersionAttribute[])?[0].InformationalVersion;
       
            ExtendedVersion = $"BlackBox : {Version}";
            GetSetting("GravityWellRange"      , ref GravityWellRange);
            GetSetting("StartingPlanetRichness", ref StartingPlanetRichness);
            GetSetting("perf"                  , ref RestrictAIPlayerInteraction);
            GetSetting("AutoSaveFreq"          , ref AutoSaveFreq);
            GetSetting("ForceFullSim"          , ref ForceFullSim);
            GetSetting("WindowMode"            , ref WindowMode);
            GetSetting("AntiAliasSamples"      , ref AntiAlias);
            GetSetting("PostProcessBloom"      , ref RenderBloom);
            GetSetting("TextureQuality"        , ref TextureQuality);   
            GetSetting("TextureSampling"       , ref TextureSampling);  
            GetSetting("MaxAnisotropy"         , ref MaxAnisotropy);    
            GetSetting("ShadowQuality"         , ref ShadowQuality); 
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
            Statreset();

        #if DEBUG
            VerboseLogging = true;
#endif

#if PERF
            RestrictAIPlayerInteraction = true;
#endif
            if (int.TryParse(GetSetting("MusicVolume"), out int musicVol)) MusicVolume = musicVol / 100f;
            if (int.TryParse(GetSetting("EffectsVolume"), out int fxVol))  EffectsVolume = fxVol / 100f;
            GetSetting("Language", ref Language);
            GetSetting("XRES", ref XRES);
            GetSetting("YRES", ref YRES);
            if (bool.TryParse(GetSetting("UIDCaseCheck"), out bool checkForCase))
                CaseControl = checkForCase ? null : StringComparer.OrdinalIgnoreCase;            
            
            LoadModInfo(ModName);
            Log.Info(ConsoleColor.DarkYellow, "Loaded App Settings");
        }

        public static void LoadModInfo(string modName)
        {
            ModName = modName;
            ModPath = "";
            if (modName == "")
            {
                ActiveMod     = null;
                ActiveModInfo = null;
                SaveActiveMod();
                return;
            }

            var info = new FileInfo($"Mods/{modName}.xml");
            if (info.Exists)
            {
                ModPath = "Mods/" + ModName + "/";
                var modInfo = new FileInfo($"{ModPath}/{modName}.xml");
                ActiveModInfo = new XmlSerializer(typeof(ModInformation)).Deserialize<ModInformation>(modInfo.Exists ? modInfo : info);
                ActiveMod     = new ModEntry(ActiveModInfo);
            }
            else
            {
                ModName       = "";
                ActiveMod     = null;
                ActiveModInfo = null;
            }
            SaveActiveMod();
        }

        public static void LoadModInfo(ModEntry me)
        {
            ModName       = me.ModName;
            ModPath       = "Mods/" + ModName + "/";
            ActiveModInfo = me.mi;
            ActiveMod     = me;
            SaveActiveMod();
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
            GetSetting("shipcountlimit",       ref ShipCountLimit);
            GetSetting("EliminationMode",      ref EliminationMode);
            GetSetting("ZoomTracking",         ref ZoomTracking);
            GetSetting("TurnTimer",            ref TurnTimer);
            GetSetting("AltArcControl",        ref AltArcControl);
            GetSetting("FreighterLimit",       ref FreighterLimit);
            GetSetting("LimitSpeed",           ref LimitSpeed);
            GetSetting("EnableWarpBehaviors",  ref WarpBehaviorsSetting);
            GetSetting("DisableAsteroids",     ref DisableAsteroids);
        }

        public static void SaveSettings()
        {
            XRES = Game1.Instance.Graphics.PreferredBackBufferWidth;
            YRES = Game1.Instance.Graphics.PreferredBackBufferHeight;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            WriteSetting(config, "GravityWellRange",       GravityWellRange);
            WriteSetting(config, "StartingPlanetRichness", StartingPlanetRichness);
            WriteSetting(config, "perf", RestrictAIPlayerInteraction);
            WriteSetting(config, "AutoSaveFreq",     AutoSaveFreq);
            WriteSetting(config, "ForceFullSim",     ForceFullSim);
            WriteSetting(config, "WindowMode",       WindowMode);
            WriteSetting(config, "AntiAliasSamples", AntiAlias);
            WriteSetting(config, "PostProcessBloom", RenderBloom);
            WriteSetting(config, "TextureQuality",   TextureQuality);
            WriteSetting(config, "TextureSampling",  TextureSampling);
            WriteSetting(config, "MaxAnisotropy",    MaxAnisotropy);
            WriteSetting(config, "ShadowQuality",    ShadowQuality);
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
            WriteSetting(config, "ShipCountLimit",      ShipCountLimit);
            WriteSetting(config, "ZoomTracking",        ZoomTracking);
            WriteSetting(config, "TurnTimer",           TurnTimer);
            WriteSetting(config, "AltArcControl",       AltArcControl);
            WriteSetting(config, "FreighterLimit",      FreighterLimit);
            WriteSetting(config, "LimitSpeed",          LimitSpeed);
            WriteSetting(config, "EnableWarpBehaviors", WarpBehaviorsSetting);
            WriteSetting(config, "DisableAsteroids",    DisableAsteroids);

            WriteSetting(config, "MusicVolume",   (int)(MusicVolume * 100));
            WriteSetting(config, "EffectsVolume", (int)(EffectsVolume * 100));
            WriteSetting(config, "Language",           Language);
            WriteSetting(config, "XRES",               XRES);
            WriteSetting(config, "YRES",               YRES);
            WriteSetting(config, "CameraPanSpeed",     CameraPanSpeed);
            WriteSetting(config, "VerboseLogging",     VerboseLogging);
            WriteSetting(config, "TestLoad",           TestLoad);
            WriteSetting(config, "PreLoad",            PreLoad);

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
        public static bool GetSetting(string name, ref float f)
        {
            if (!float.TryParse(ConfigurationManager.AppSettings[name], out float v)) return false;
            f = v;
            return true;
        }
        public static bool GetSetting(string name, ref int i)
        {
            if (!int.TryParse(ConfigurationManager.AppSettings[name], out int v)) return false;
            i = v;
            return true;
        }
        public static bool GetSetting(string name, ref bool b)
        {
            if (!bool.TryParse(ConfigurationManager.AppSettings[name], out bool v)) return false;
            b = v;
            return true;
        }
        public static bool GetSetting(string name, ref string s)
        {
            string v = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrEmpty(v)) return false;
            s = v;
            return true;
        }
        public static bool GetSetting<T>(string name, ref T e) where T : struct
        {
            if (!Enum.TryParse(ConfigurationManager.AppSettings[name], out T v)) return false;
            e = v;
            return true;
        }
        public static string GetSetting(string name) => ConfigurationManager.AppSettings[name];



        private static void WriteSetting(Configuration config, string name, float v)
        {
            WriteSetting(config, name, v.ToString(CultureInfo.InvariantCulture));
        }
        private static void WriteSetting<T>(Configuration config, string name, T v) where T : struct
        {
            WriteSetting(config, name, v.ToString());
        }
        private static void WriteSetting(Configuration config, string name, string value)
        {
            var setting = config.AppSettings.Settings[name];
            if (setting != null) setting.Value = value;
            else config.AppSettings.Settings.Add(name, value);
        }



        // @todo Why is this here??
        public static void IncrementCordrazineCapture()
        {
            CordrazinePlanetsCaptured += 1;
            if (CordrazinePlanetsCaptured == 1)
            {
                Empire.Universe.NotificationManager.AddNotify(ResourceManager.EventsDict["OwlwokFreedom"]);
            }
        }

        // @todo Why is this here??
        public static void IncrementRemnantKills(int exp)
        {
            RemnantKills = RemnantKills + exp;
            if (ActiveModInfo != null && ActiveModInfo.RemnantTechCount > 0)
            {
                if (RemnantKills >= 5 + (int)Empire.Universe.GameDifficulty* 3 && RemnantActivation < ActiveModInfo.RemnantTechCount)
                {
                    RemnantActivation += 1;
                    Empire.Universe.NotificationManager.AddNotify(ResourceManager.EventsDict["RemnantTech1"]);
                    RemnantKills = 0;
                }
            }
            else
            {
                if (RemnantKills >= 5 && RemnantActivation == 0)    //Edited by Gretman, to make sure the remnant event only appears once
                {
                    Empire.Universe.NotificationManager.AddNotify(ResourceManager.EventsDict["RemnantTech1"]);
                    RemnantActivation = 1;
                }
            }
        }
    }
}