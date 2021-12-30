using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public partial class ShipTechLineFocusing
    {
        HashSet<string> UseShipTechProgression(Array<Ship> researchableShips, HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            // Filter out all current ship techs that aren't in researchableShips.
            BestCombatShip                = null;
            bool containsOnlyHullTech     = true;
            var sortedShips               = researchableShips.Sorted(ExtractTechCost);
            HashSet<string> goodShipTechs = new HashSet<string>();
            foreach (var ship in sortedShips)
            {
                if (!TryExtractNeedTechs(ship, out HashSet<string> techs, out bool onlyHullLeft))
                    continue;

                var researchableTechs = shipTechs.Intersect(techs).ToArray();
                if (researchableTechs.Length > 0)
                {
                    if (onlyHullLeft && CanResearchOnlyHull(researchableTechs)) // shortcut to hull
                        return UseOnlyWantedShipTechs(researchableTechs, new HashSet<string>());

                    if (goodShipTechs.Count == 0 || onlyHullLeft || goodShipTechs.Count > 0 && containsOnlyHullTech)
                    {
                        if (BestCombatShip == null)
                            BestCombatShip = ship;

                        // Add the cheapest ship tech to research, but also the hull which
                        // if researched, will unlock more ships
                        foreach (var techName in researchableTechs)
                            goodShipTechs.Add(techName);
                    }

                    if (!onlyHullLeft)
                        containsOnlyHullTech = false;
                }
            }

            return UseOnlyWantedShipTechs(goodShipTechs, nonShipTechs);
        }

        bool CanResearchOnlyHull(string[] techsNames)
        {
            foreach (string techName in techsNames)
            {
                if (OwnerEmpire.TryGetTechEntry(techName, out TechEntry tech)
                    && tech.GetUnLockableHulls(OwnerEmpire).Count > 0
                    && CanResearchHullInTimelyManner(tech))
                {
                    return true;
                }
            }

            return false;
        }

        bool CanResearchHullInTimelyManner(TechEntry tech)
        {
            int turnsThreshold  = (int)OwnerEmpire.TotalPopBillion.Clamped(1, 200);
            float netResearch   = OwnerEmpire.Research.MaxResearchPotential.LowerBound(1);
            float researchTurns = tech.Tech.ActualCost / netResearch;
            return researchTurns <= turnsThreshold;
        }

        float ExtractTechCost(Ship ship)
        {
            float totalCost = 0;
            var shipTechs   = ConvertStringToTech(ship.ShipData.TechsNeeded);
            for (int i = 0; i < shipTechs.Count; i++)
            {
                TechEntry tech = shipTechs[i];
                if (tech.Locked)
                {
                    float multiplier = 1f;
                    // On different difficulties / personalities, hull techs are more expensive or cheaper to research
                    if (tech.Tech.HullsUnlocked.Count != 0)
                    {
                        multiplier = OwnerEmpire.PersonalityModifiers.HullTechMultiplier
                                   * OwnerEmpire.DifficultyModifiers.HullTechMultiplier;
                    }

                    totalCost += tech.Tech.ActualCost * multiplier;
                }
            }

            return totalCost;
        }

        bool TryExtractNeedTechs(Ship ship, out HashSet<string> techsToAdd, out bool onlyHullLeft)
        {
            onlyHullLeft  = false;
            var shipTechs = ConvertStringToTech(ship.ShipData.TechsNeeded);
            if (OwnerEmpire.IsHullUnlocked(ship.ShipData.Hull)
                && !shipTechs.Any(t => t.Locked && t.ContainsHullTech()))
            {
                techsToAdd = ship.ShipData.TechsNeeded;
                return true;
            }

            string hullTech = "";
            techsToAdd      = new HashSet<string>();
            for (int i = 0; i < shipTechs.Count; i++)
            {
                TechEntry tech = shipTechs[i];
                if (tech.Locked)
                {
                    if (tech.GetUnlockableHulls(OwnerEmpire).Count > 0)
                    {
                        if (hullTech.IsEmpty())
                            hullTech = tech.UID;
                        else  // this ship is more than one hull away, so ignore it
                            return false;
                    }
                    else
                    {
                        techsToAdd.Add(tech.UID);
                    }
                }
            }

            // If there are no new techs to research besides the hull, it means that ship can put to use
            // once the hull is researched, so its time to research the hull
            if (techsToAdd.Count == 0 && hullTech.NotEmpty() && ship.BaseStrength > 0)
            {
                techsToAdd.Add(hullTech);
                onlyHullLeft = true;
            }

            return techsToAdd.Count > 0;
        }
    }
}
