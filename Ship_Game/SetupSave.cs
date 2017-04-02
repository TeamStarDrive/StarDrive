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
        public RaceDesignScreen.GalSize Galaxysize;
        public int Pacing;
        public RaceDesignScreen.ExtraRemnantPresence ExtraRemnant;
        public float FTLModifier;
        public float EnemyFTLModifier;
        public float OptionIncreaseShipMaintenance;
        public float MinimumWarpRange;
        public int TurnTimer;
        public bool preventFederations;
        public float GravityWellRange;
        public RaceDesignScreen.GameMode mode;
        public int numOpponents;
        public int ExtraPlanets;
        public float StartingPlanetRichness;
        public bool PlanetaryGravityWells;
        public bool WarpInSystem;

        public SetupSave()
        { }

        public SetupSave(UniverseData.GameDifficulty gameDifficulty, 
            RaceDesignScreen.StarNum starNum, 
            RaceDesignScreen.GalSize galaxysize, int pacing, 
            RaceDesignScreen.ExtraRemnantPresence extraRemnant, 
            int numOpponents, RaceDesignScreen.GameMode mode)
        {
            if (GlobalStats.ActiveMod != null)
            {
                ModName = GlobalStats.ActiveMod.mi.ModName;
                ModPath = GlobalStats.ActiveMod.ModName;
            }
            this.Version = Convert.ToInt32(ConfigurationManager.AppSettings["SaveVersion"]);
            this.GameDifficulty                = gameDifficulty;
            this.StarEnum                      = starNum;
            this.Galaxysize                    = galaxysize;
            this.Pacing                        = pacing;
            this.ExtraRemnant                  = extraRemnant;
            this.FTLModifier                   = GlobalStats.FTLInSystemModifier;
            this.EnemyFTLModifier              = GlobalStats.EnemyFTLInSystemModifier;
            this.OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
            this.MinimumWarpRange              = GlobalStats.MinimumWarpRange;
            this.TurnTimer                     = GlobalStats.TurnTimer;
            this.preventFederations            = GlobalStats.PreventFederations;
            this.GravityWellRange              = GlobalStats.GravityWellRange;
            this.mode                          = mode;
            this.numOpponents                  = numOpponents;
            this.ExtraPlanets                  = GlobalStats.ExtraPlanets;
            this.StartingPlanetRichness        = GlobalStats.StartingPlanetRichness;
            this.PlanetaryGravityWells         = GlobalStats.PlanetaryGravityWells;
            this.WarpInSystem                  = GlobalStats.WarpInSystem;

            string str = DateTime.Now.ToString("M/d/yyyy");
            DateTime now = DateTime.Now;
            this.Date = string.Concat(str, " ", now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat));
        }
    }
}
