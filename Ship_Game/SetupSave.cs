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
        public float MinimumWarpRange;
        public int TurnTimer;
        public bool PreventFederations;
        public float GravityWellRange;
        public RaceDesignScreen.GameMode Mode;
        public int NumOpponents;
        public int ExtraPlanets;
        public float StartingPlanetRichness;
        public bool PlanetaryGravityWells;
        public bool WarpInSystem;

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
            MinimumWarpRange              = GlobalStats.MinimumWarpRange;
            TurnTimer                     = GlobalStats.TurnTimer;
            PreventFederations            = GlobalStats.PreventFederations;
            GravityWellRange              = GlobalStats.GravityWellRange;
            Mode                          = mode;
            NumOpponents                  = numOpponents;
            ExtraPlanets                  = GlobalStats.ExtraPlanets;
            StartingPlanetRichness        = GlobalStats.StartingPlanetRichness;
            PlanetaryGravityWells         = GlobalStats.PlanetaryGravityWells;
            WarpInSystem                  = GlobalStats.WarpInSystem;

            string str = DateTime.Now.ToString("M/d/yyyy");
            DateTime now = DateTime.Now;
            Date = string.Concat(str, " ", now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat));
        }
    }
}
