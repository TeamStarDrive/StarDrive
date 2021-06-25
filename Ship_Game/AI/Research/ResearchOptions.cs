using System;
using System.Collections.Generic;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.AI.Research
{
    public class ResearchOptions
    {
        Map<TechnologyType, float> TechTypeMods    = new Map<TechnologyType, float>();
        Map<ShipCosts, float> ShipMods             = new Map<ShipCosts, float>();
        Map<ResearchArea, float> Priority          = new Map<ResearchArea, float>();

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
            GroundCombat,
            Randomize
        }

        public enum ResearchArea
        {
            ShipTech,
            Research,
            Colonization,
            Economic,
            Industry,
            General,
            GroundCombat
        }

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
            /// Random= TechCost * randomizer
            /// TechCost += randomBetween -Random and Random.
            ShipMods[ShipCosts.Randomize]    = 0f;

            //TechUIDS

            //TechTypes
            TechTypeMods[TechnologyType.ShipHull] = 1f;

            //Tech Priorities
            Priority[ResearchArea.Colonization] = 1;
            Priority[ResearchArea.Economic]     = 1;
            Priority[ResearchArea.GroundCombat] = 1;
            Priority[ResearchArea.Industry]     = 1;
            Priority[ResearchArea.General]      = 1;
            Priority[ResearchArea.Research]     = 1;
            Priority[ResearchArea.ShipTech]     = 1;
        }
        
        // This may be kinda slow...
        float GetAnyTypeMod(TechEntry tech)
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

        public float CostMultiplier(TechEntry tech)
        {
            float lowPriorityMultiplier = tech.Tech.LowPriorityCostMultiplier.Clamped(0.1f, 10);
            float typeMod = GetAnyTypeMod(tech);
            return lowPriorityMultiplier * typeMod;
        }

        public float CostMultiplier(ShipCosts costType)
        {
            if (ShipMods.TryGetValue(costType, out float modifier))
                return modifier;
            return 1;
        }

        public float GetPriority(ResearchArea priorities)
        {
            if (Priority.TryGetValue(priorities, out float modifier))
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