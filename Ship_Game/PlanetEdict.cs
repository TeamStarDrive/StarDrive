using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    public class PlanetEdict
    {
        public string Name;
        public int Localization;
        public int Description;

        //Slider preference
        public bool PreventStarvation;
        public float FoodPriority;
        public float ProductionPriority;
        public float ResearchPriority;

        //Export/Import Controls
        public float FoodImportThreshold;
        public float FoodStoreThreshold;
        public float FoodExportThreshold;
        public float ProductionImportThreshold;
        public float ProductionStoreThreshold;
        public float ProductionExportThreshold;

        //Building preference

        //Limitations
        public bool BuildsTroops;
        public bool BuildsDefensePlatforms;
    }
}
