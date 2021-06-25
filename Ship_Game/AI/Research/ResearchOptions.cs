using System;
using System.Collections.Generic;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.AI.Research
{
    public class ResearchOptions
    {
        Map<string, float> TechUIDMods             = new Map<string, float>();
        Map<TechnologyType, float> TechTypeMods    = new Map<TechnologyType, float>();
        Map<ShipCosts, float> ShipMods             = new Map<ShipCosts, float>();

        public enum ResearchMods
        {
            TechUID,
            TechType,
            ShipCost
        }

        public enum ShipCosts
        {
            None,
            Carrier,
            Bomber,
            TroopShip,
            Support,
            ColonyShip,
            Freighter,
            AllHulls,
            Orbitals,
            GroundCombat
        }

        //ToDo: put research priority mods here. 
        public enum TechPriorityBonuses
        {}

        public ResearchOptions()
        {
            //Set default values.
            ////ShipCosts
            ShipMods[ShipCosts.Carrier]      = 0.95f;
            ShipMods[ShipCosts.Bomber]       = 0.95f;
            ShipMods[ShipCosts.TroopShip]    = 0.95f;
            ShipMods[ShipCosts.Support]      = 0.95f;
            ShipMods[ShipCosts.ColonyShip]   = 2f;
            ShipMods[ShipCosts.Freighter]    = 2f;
            ShipMods[ShipCosts.AllHulls]     = 2;
            ShipMods[ShipCosts.Orbitals]     = 2f;
            ShipMods[ShipCosts.GroundCombat] = 0.95f;

            //TechUIDS

            //TechTypes
            TechTypeMods[TechnologyType.ShipHull] = 2f;
        }

        public float GetUIDMod(string techUID)
        {
            if (TechUIDMods.TryGetValue(techUID, out float modifier))
                return modifier;
            return 1;
        }
        
        // This may be kinda slow...
        public float GetAnyTypeMod(TechEntry tech)
        {
            foreach(KeyValuePair<TechnologyType, float> kv in TechTypeMods)
            {
                TechnologyType techType = kv.Key;
                if (tech.IsTechnologyType(techType))
                {
                    return kv.Value;
                }
            }
            return 1;
        }

        public float GetPrimaryTypeMod(TechnologyType costType)
        {
            if (TechTypeMods.TryGetValue(costType, out float modifier))
                return modifier;
            return 1;
        }

        public float GetShipMod(ShipCosts costType)
        {
            if (ShipMods.TryGetValue(costType, out float modifier))
                return modifier;
            return 1;
        }

        //ToDo: create loadable options
        static void LoadResearchOptions()
        {
            GameLoadingScreen.SetStatus("ResearchOptions");
            
            //Array<Research> researchMods = YamlParser.DeserializeArray<Research>("Research.yaml");
        }
    }
}