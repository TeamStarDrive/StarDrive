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
            var shipTechs   = ConvertStringToTech(ship.shipData.TechsNeeded);
            for (int i = 0; i < shipTechs.Count; i++)
            {
                TechEntry tech = shipTechs[i];
                if (tech.Locked)
                {
                    totalCost += tech.Tech.HullsUnlocked.Count == 0
                        ? tech.Tech.ActualCost
                        : tech.Tech.ActualCost
                            * OwnerEmpire.PersonalityModifiers.HullTechMultiplier
                            * OwnerEmpire.DifficultyModifiers.HullTechMultiplier;
                }
            }

            return totalCost;
        }

        bool TryExtractNeedTechs(Ship ship, out HashSet<string> techsToAdd, out bool onlyHullLeft)
        {
            onlyHullLeft  = false;
            var shipTechs = ConvertStringToTech(ship.shipData.TechsNeeded);
            if (OwnerEmpire.IsHullUnlocked(ship.shipData.Hull)
                && !shipTechs.Any(t => t.Locked && t.ContainsHullTech()))
            {
                techsToAdd = ship.shipData.TechsNeeded;
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
