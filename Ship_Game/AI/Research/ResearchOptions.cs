using System;
using System.Collections.Generic;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.AI.Research
{
    public class ResearchOptions
    {
        static readonly Map<TechnologyType, float> TechTypeMods = new Map<TechnologyType, float>();
        static readonly Map<ShipCosts, float> ShipMods          = new Map<ShipCosts, float>();
        static readonly Map<ResearchArea, float> Priority       = new Map<ResearchArea, float>();

        /// <summary>
        /// Used for loading yamls
        /// </summary>
        public enum ResearchMods
        {
            TechType,
            ShipCost,
            AreaPriority
        }

        /// <summary>
        /// ShipCost cost multiplier for various ship categories.
        /// </summary>
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
            /// <summary>
            /// Add a random to the tech cost of ship for the ship picker.
            /// this will be a random of -techCost * randomize tp +techCost * randomize
            /// </summary>
            Randomize,
            /// <summary>
            /// How much bonus a similar tech gives in ship cost.
            /// </summary>
            LineFocusIntensity
        }

        /// <summary>
        /// When choosing a tech category how much extra bonus will be given to research it.
        /// the value of 1 adds 1-100 plus empire racial default.
        /// </summary>
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
            //ShipCosts
            ShipMods.Clear();
            ShipMods[ShipCosts.Carrier]            = 0.95f;
            ShipMods[ShipCosts.Bomber]             = 0.95f;
            ShipMods[ShipCosts.TroopShip]          = 0.95f;
            ShipMods[ShipCosts.Support]            = 0.95f;
            ShipMods[ShipCosts.ColonyShip]         = 2f;
            ShipMods[ShipCosts.Freighter]          = 2f;
            ShipMods[ShipCosts.AllHulls]           = 1;
            ShipMods[ShipCosts.Orbitals]           = 2f;
            ShipMods[ShipCosts.GroundCombat]       = 0.95f;
            /// TechCost * random = randomizer
            /// random is -randomizer to +randomizer
            ShipMods[ShipCosts.Randomize]          = 0f;
            ShipMods[ShipCosts.LineFocusIntensity] = 1;

            //TechUIDS

            //TechTypes
            TechTypeMods.Clear();
            TechTypeMods[TechnologyType.ShipHull] = 1f;

            //Tech Priorities
            Priority.Clear();
            Priority[ResearchArea.Colonization] = 1;
            Priority[ResearchArea.Economic]     = 1;
            Priority[ResearchArea.GroundCombat] = 0.5f;
            Priority[ResearchArea.Industry]     = 1;
            Priority[ResearchArea.General]      = 1;
            Priority[ResearchArea.Research]     = 1;
            Priority[ResearchArea.ShipTech]     = 1;
            LoadResearchOptions();
        }

        // This may be kinda slow...
        float GetAnyTypeMod(TechEntry tech)
        {
            foreach (KeyValuePair<TechnologyType, float> kv in TechTypeMods)
            {
                TechnologyType techType = kv.Key;
                if (tech.IsTechnologyType(techType))
                {
                    return kv.Value;
                }
            }
            return 1;
        }

        /// <summary>
        /// tech type cost updater modifier.
        /// </summary>
        public float CostMultiplier(TechEntry tech)
        {
            float lowPriorityMultiplier = tech.Tech.LowPriorityCostMultiplier.Clamped(0.1f, 10);
            float typeMod = GetAnyTypeMod(tech);
            return lowPriorityMultiplier * typeMod;
        }

        /// <summary>
        /// Ship cost multiplies.
        /// </summary>
        public float CostMultiplier(ShipCosts costType)
        {
            if (ShipMods.TryGetValue(costType, out float modifier))
                return modifier;
            return 1;
        }

        /// <summary>
        /// Research category priority bonus
        /// </summary>/returns>
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

            Array<ResearchSettings> researchMods = YamlParser.DeserializeArray<ResearchSettings>("Research.yaml");

            foreach (var area in researchMods)
            {
                switch (area.ResearchArea)
                {
                    case ResearchMods.TechType: area.ConvertTo(TechTypeMods); break;
                    case ResearchMods.ShipCost: area.ConvertTo(ShipMods);     break;
                    case ResearchMods.AreaPriority: area.ConvertTo(Priority); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        [StarDataType]
        public class ResearchSettings
        {
            [StarData] public readonly ResearchMods ResearchArea;
            [StarData] public readonly Setting[] AreaSettings;

            public void ConvertTo<TEnum>(Map<TEnum, float> addTo) where TEnum : struct
            {
                foreach (var item in AreaSettings)
                {
                    TEnum techType;
                    if (Enum.TryParse(item.Key, out techType))
                        addTo[techType] = item.Value;
                }
            }

            public override string ToString() => $"Research Area {ResearchArea} ItemCount= {AreaSettings.Length}";
        }

        [StarDataType]
        public class Setting
        {
            [StarData] public string Key;
            [StarData] public float Value;
        }
    }
}