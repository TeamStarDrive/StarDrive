﻿using System;
using System.Collections.Generic;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using static Ship_Game.AI.Research.ResearchOptions.ShipCosts;

namespace Ship_Game.AI.Research
{
    /// <summary>
    /// The ship picker chooses ship research by finding the cheapest ship deign to research.
    /// its also compares already researched tech to the ships it can currently research and attempts
    /// to research ships that share the same tech. AKA LineFocusing.
    /// This should make it very efficient in its research process.
    /// It will also attempt to research designs that are within the same tech range as non ship techs. 
    /// </summary>
    public class ShipPicker
    {
        public Map<ShipData.RoleName, int> MostTechs;
        public Array<TechEntry> KnownTechs;
        ResearchOptions Options;
        public ShipPicker(ResearchOptions options)
        {
            MostTechs  = new Map<ShipData.RoleName, int>();
            KnownTechs = new Array<TechEntry>();
            Options    = options;
        }

        /// <summary>
        /// Populate the known tech list with techs that are not hulls.
        /// </summary>
        void PopulateKnownTechs(Empire empire)
        {
            foreach (var techName in empire.ShipTechs)
            {
                var tech = empire.GetTechEntry(techName);
                if (!tech.ContainsHullTech())
                    KnownTechs.AddUnique(tech);
            }
        }

        public Ship FindCheapestShipInList(Empire empire, Array<Ship> ships, HashSet<string> techs)
        {

            PopulateKnownTechs(empire);

            var buildableShips = ResourceManager.GetShipTemplates().Filter(t => empire.ShipsWeCanBuild.Contains(t.Name));

            PopulateMostTechs(buildableShips, KnownTechs);
            PopulateMostTechs(ships, KnownTechs);

            float minTechCost = float.MaxValue;
            foreach (var techName in techs)
            {
                var tech       = empire.GetTechEntry(techName);
                float techCost = tech.Tech.ActualCost;
                if (!tech.Unlocked && tech.Tech.RootNode == 0)
                    minTechCost = Math.Min(techCost, minTechCost);
            }
            minTechCost = Math.Max(1, minTechCost);

            // find cheapest ship to research in current set of ships.
            // adjust cost of some techs to make ships more or less wanted.
            var pickedShip = ships.FindMin(s => GetModifiedShipCost(s, empire, minTechCost));
            return pickedShip;
        }

        public int GetModifiedShipCost(Ship s, Empire empire, float minTechCost)
        {
            float techScore = 0;

            // first adjust cost by the techs in the ship.
            foreach (string techName in s.shipData.TechsNeeded)
            {
                var tech = empire.GetTechEntry(techName);
                if (!tech.Unlocked && tech.Tech.RootNode == 0)
                {
                    var cost = tech.Tech.ActualCost;

                    if (tech.IsTechnologyType(TechnologyType.GroundCombat)) cost *= Options.CostMultiplier(GroundCombat);
                    if (tech.IsTechnologyType(TechnologyType.ShipHull)) cost *= Options.CostMultiplier(AllHulls);

                    techScore += cost * Options.CostMultiplier(tech);
                }
            }

            var tags = new Array<WeaponTag>();
            foreach (var weapon in s.Weapons)
            {
                foreach (var tag in weapon.ActiveWeaponTags)
                    tags.AddUnique(tag);
            }

            foreach (var tag in tags)
            {
                techScore *= Options.CostMultiplier(tag);
            }


            // now adjust cost by the role of the ship.
            switch (s.DesignRole)
            {
                case ShipData.RoleName.platform:
                case ShipData.RoleName.station: techScore *= Options.CostMultiplier(Orbitals); break;
                case ShipData.RoleName.colony: techScore *= Options.CostMultiplier(ColonyShip); break;
                case ShipData.RoleName.freighter: techScore *= Options.CostMultiplier(Freighter); break;
                case ShipData.RoleName.troopShip when !empire.canBuildTroopShips: techScore *= Options.CostMultiplier(TroopShip); break;
                case ShipData.RoleName.support when !empire.canBuildSupportShips: techScore *= Options.CostMultiplier(Support); break;
                case ShipData.RoleName.bomber when !empire.canBuildBombers: techScore *= Options.CostMultiplier(Bomber); break;
                case ShipData.RoleName.carrier when !empire.canBuildCarriers: techScore *= Options.CostMultiplier(Carrier); break;
            }

            // adjust cost by how much it varies from already known tech.
            float researchDepth = MostTechs.TryGetValue(s.DesignRole, out int depth) ? depth : 1;
            float techsResearched = CountTechsAlreadyResearched(s, KnownTechs);
            float techRatio = Math.Min(researchDepth / techsResearched, 200);

            // adjust cost by how much more expensive it is then the least non ship tech
            float costRatio = Math.Min(techScore / (minTechCost * Options.CostMultiplier(BalanceToInfraIntensity)), 200);
            float randomBase = techScore * Options.CostMultiplier(Randomize);

            // introduce a random to the cost of the ship to vary what is researched.
            float random = randomBase > 0 ? RandomMath.AvgRandomBetween(-randomBase, randomBase) : 0;

            return (int)((techScore + random) * costRatio * techRatio);
        }

        /// <summary>
        /// With the provided lists of ships and techs, populate a dictionary of design roles that use the most of those techs
        /// </summary>
        public void PopulateMostTechs(IReadOnlyList<Ship> ships, Array<TechEntry> knownTechs)
        {
            foreach (var ship in ships)
            {
                int knownNumber = CountTechsAlreadyResearched(ship, KnownTechs);

                if (MostTechs.TryGetValue(ship.DesignRole, out int known))
                {
                    if (knownNumber > known)
                        MostTechs[ship.DesignRole] = knownNumber;
                }
                else
                    MostTechs[ship.DesignRole] = 1;
            }
        }

        /// <summary>
        /// find the techs that are already researched for a ship.
        /// but only count items that have a tech it leads to already researched.
        /// This provides line focusing.
        /// </summary>
        int CountTechsAlreadyResearched(Ship ship, Array<TechEntry> knowTechs)
        {
            int alreadyResearched = 0;
            int lineFocusBonus = (int)Options.CostMultiplier(LineFocusIntensity);
            foreach (var techName in ship.shipData.TechsNeeded)
            {
                var tech = knowTechs.Find(t => t.UID == techName);

                if (tech?.IsRoot == false)
                {
                    alreadyResearched += lineFocusBonus;
                }
            }
            return alreadyResearched > 0 ? alreadyResearched : 1;
        }
    }
}