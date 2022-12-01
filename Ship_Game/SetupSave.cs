using System;
using System.Configuration;
using System.Globalization;

namespace Ship_Game
{
    public sealed class SetupSave
    {
        public string Name = "";
        public string Date = "";
        public string ModName = "";
        public string ModPath = "";
        public int Version;
        public GameDifficulty GameDifficulty;
        public RaceDesignScreen.StarsAbundance StarEnum;
        public GalSize GalaxySize;
        public int Pacing;
        public ExtraRemnantPresence ExtraRemnant;
        public float FTLModifier;
        public float EnemyFTLModifier;
        public float OptionIncreaseShipMaintenance;
        public float MinAcceptableShipWarpRange;
        public int TurnTimer;
        public bool PreventFederations;
        public float GravityWellRange;
        public RaceDesignScreen.GameMode Mode;
        public int NumOpponents;
        public int ExtraPlanets;
        public float StartingPlanetRichness;
        public bool WarpInSystem;
        public bool FixedPlayerCreditCharge;
        public bool AIUsesPlayerDesigns;
        public bool DisablePirates;
        public bool DisableRemnantStory;
        public bool UseUpkeepByHullSize;
        public float CustomMineralDecay;
        public float VolcanicActivity;

        public SetupSave()
        {
        }

        public SetupSave(GameDifficulty gameDifficulty, RaceDesignScreen.StarsAbundance starsAbundance, 
                         GalSize galaxySize, int pacing, ExtraRemnantPresence extraRemnant, int numOpponents, 
                         RaceDesignScreen.GameMode mode)
        {
            if (GlobalStats.HasMod)
            {
                ModName = GlobalStats.ActiveMod.ModName;
                ModPath = GlobalStats.ActiveMod.ModName;
            }
            Version = SavedGame.SaveGameVersion;
            GameDifficulty                = gameDifficulty;
            StarEnum                      = starsAbundance;
            GalaxySize                    = galaxySize;
            Pacing                        = pacing;
            ExtraRemnant                  = extraRemnant;
            FTLModifier                   = GlobalStats.FTLInSystemModifier;
            EnemyFTLModifier              = GlobalStats.EnemyFTLInSystemModifier;
            OptionIncreaseShipMaintenance = GlobalStats.Settings.ShipMaintenanceMultiplier;
            MinAcceptableShipWarpRange    = GlobalStats.Settings.MinAcceptableShipWarpRange;
            TurnTimer                     = GlobalStats.TurnTimer;
            PreventFederations            = GlobalStats.PreventFederations;
            GravityWellRange              = GlobalStats.Settings.GravityWellRange;
            Mode                          = mode;
            NumOpponents                  = numOpponents;
            ExtraPlanets                  = GlobalStats.ExtraPlanets;
            StartingPlanetRichness        = GlobalStats.Settings.StartingPlanetRichness;
            WarpInSystem                  = GlobalStats.WarpInSystem;
            FixedPlayerCreditCharge       = GlobalStats.FixedPlayerCreditCharge;
            AIUsesPlayerDesigns           = GlobalStats.Settings.AIUsesPlayerDesigns;
            DisablePirates                = GlobalStats.Settings.DisablePirates;
            DisableRemnantStory           = GlobalStats.Settings.DisableRemnantStory;
            UseUpkeepByHullSize           = GlobalStats.UseUpkeepByHullSize;
            CustomMineralDecay            = GlobalStats.Settings.CustomMineralDecay;
            VolcanicActivity              = GlobalStats.Settings.VolcanicActivity;

            string str = DateTime.Now.ToString("M/d/yyyy");
            DateTime now = DateTime.Now;
            Date = string.Concat(str, " ", now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat));
        }
    }
}
