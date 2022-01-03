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
        public UniverseData.GameDifficulty GameDifficulty;
        public RaceDesignScreen.StarNum StarEnum;
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
        public bool PlanetaryGravityWells;
        public bool WarpInSystem;
        public bool FixedPlayerCreditCharge;
        public bool UsePlayerDesigns;
        public bool DisablePirates;
        public bool DisableRemnantStory;
        public bool UseUpkeepByHullSize;
        public float CustomMineralDecay;
        public float VolcanicActivity;

        public SetupSave()
        {
        }

        public SetupSave(UniverseData.GameDifficulty gameDifficulty, RaceDesignScreen.StarNum starNum, 
                         GalSize galaxySize, int pacing, ExtraRemnantPresence extraRemnant, int numOpponents, 
                         RaceDesignScreen.GameMode mode)
        {
            if (GlobalStats.HasMod)
            {
                ModName = GlobalStats.ActiveMod.mi.ModName;
                ModPath = GlobalStats.ActiveMod.ModName;
            }
            Version = Convert.ToInt32(ConfigurationManager.AppSettings["SaveVersion"]);
            GameDifficulty                = gameDifficulty;
            StarEnum                      = starNum;
            GalaxySize                    = galaxySize;
            Pacing                        = pacing;
            ExtraRemnant                  = extraRemnant;
            FTLModifier                   = GlobalStats.FTLInSystemModifier;
            EnemyFTLModifier              = GlobalStats.EnemyFTLInSystemModifier;
            OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            MinAcceptableShipWarpRange    = GlobalStats.MinAcceptableShipWarpRange;
            TurnTimer                     = GlobalStats.TurnTimer;
            PreventFederations            = GlobalStats.PreventFederations;
            GravityWellRange              = GlobalStats.GravityWellRange;
            Mode                          = mode;
            NumOpponents                  = numOpponents;
            ExtraPlanets                  = GlobalStats.ExtraPlanets;
            StartingPlanetRichness        = GlobalStats.StartingPlanetRichness;
            PlanetaryGravityWells         = GlobalStats.PlanetaryGravityWells;
            WarpInSystem                  = GlobalStats.WarpInSystem;
            FixedPlayerCreditCharge       = GlobalStats.FixedPlayerCreditCharge;
            UsePlayerDesigns              = GlobalStats.UsePlayerDesigns;
            DisablePirates                = GlobalStats.DisablePirates;
            DisableRemnantStory           = GlobalStats.DisableRemnantStory;
            UseUpkeepByHullSize           = GlobalStats.UseUpkeepByHullSize;
            CustomMineralDecay            = GlobalStats.CustomMineralDecay;
            VolcanicActivity              = GlobalStats.VolcanicActivity;

            string str = DateTime.Now.ToString("M/d/yyyy");
            DateTime now = DateTime.Now;
            Date = string.Concat(str, " ", now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat));
        }
    }
}
