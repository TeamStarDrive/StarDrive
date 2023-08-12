using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDUtils;

namespace Ship_Game.AI.Research
{
    public partial class ShipTechLineFocusing
    {
        readonly Empire OwnerEmpire;
        public IShipDesign BestCombatShip { get; private set; }
        public ShipPicker PickShipToResearch;
        ResearchOptions ResearchMods;

        void DebugLog(string text) => OwnerEmpire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        public ShipTechLineFocusing (Empire empire, ResearchOptions researchMods)
        {
            OwnerEmpire        = empire;
            ResearchMods       = researchMods;
            PickShipToResearch = new ShipPicker(researchMods);
        }

        public Array<TechEntry> LineFocusShipTechs(string modifier, Array<TechEntry> availableTechs, string scriptedOrRandom)
        {
            if (BestCombatShip != null)
            {
                if (OwnerEmpire.CanBuildShip(BestCombatShip)
                    || OwnerEmpire.CanBuildStation(BestCombatShip)
                    || BestCombatShip.IsShipyard)
                    BestCombatShip = null;
                else
                if (!BestCombatShip.TechsNeeded.Except(OwnerEmpire.ShipTechs).Any())
                    BestCombatShip = null;
            }
            HashSet<string> shipFilteredTechs = FindBestShip(modifier, availableTechs, scriptedOrRandom);

            //now that we have a target ship to build filter out all the current techs that are not needed to build it.

            availableTechs = ConvertStringToTech(shipFilteredTechs);
            return availableTechs;
        }

        private static bool IsRoleValid(RoleName role)
        {
            switch (role)
            {
                case RoleName.disabled:
                case RoleName.supply:
                case RoleName.troop:
                case RoleName.prototype:
                case RoleName.construction: return false;
                case RoleName.freighter:
                case RoleName.colony:
                case RoleName.ssp:
                case RoleName.platform:
                case RoleName.station:
                case RoleName.troopShip:
                case RoleName.support:
                case RoleName.bomber:
                case RoleName.carrier:
                case RoleName.fighter:
                case RoleName.scout:
                case RoleName.gunboat:
                case RoleName.drone:
                case RoleName.corvette:
                case RoleName.frigate:
                case RoleName.destroyer:
                case RoleName.cruiser:
                case RoleName.battleship:
                case RoleName.capital: break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        private bool ShipHasUndiscoveredTech(IShipDesign ship)
        {
            foreach (string techName in ship.TechsNeeded)
            {
                if (!OwnerEmpire.HasDiscovered(techName))
                    return true;
            }
            return false;
        }

        private bool ShipHasResearchableTech(IShipDesign ship)
        {
            foreach (string techName in ship.TechsNeeded)
            {
                var tech = OwnerEmpire.GetTechEntry(techName);
                if (tech.Locked && tech.ContainsShipTech())
                    return true;
            }
            return false;
        }

        private Array<IShipDesign> GetResearchableShips(Array<IShipDesign> racialShips)
        {
            var researchableShips = new Array<IShipDesign>();
            foreach (IShipDesign shortTermBest in racialShips)
            {
                // don't try to research ships we have all the tech for.
                if (!ShipHasResearchableTech(shortTermBest)) continue;
                // Don't build ships intended for carriers if there arent any carriers.
                if (!OwnerEmpire.CanBuildCarriers && shortTermBest.IsCarrierOnly)
                    continue;
                // filter out bad roles....
                if (!IsRoleValid(shortTermBest.HullRole)) continue;
                if (!IsRoleValid(shortTermBest.Role)) continue;
                if (!shortTermBest.Unlockable) continue;
                if (ShipHasUndiscoveredTech(shortTermBest)) continue;
                if (!shortTermBest.IsShipGoodToBuild(OwnerEmpire)) continue;

                researchableShips.Add(shortTermBest);
            }
            return researchableShips;
        }

        Array<IShipDesign> FilterRacialShips()
        {
            var racialShips = new Array<IShipDesign>();
            foreach (IShipDesign shortTermBest in ResourceManager.Ships.Designs)
            {
                // restrict to to ships available to this empire.
                string shipStyle = shortTermBest.ShipStyle ?? shortTermBest.BaseHull?.Style;
                if (shipStyle.IsEmpty())
                {
                    Log.Warning($"Ship {shortTermBest?.Name} Tech FilterRacialShip found a bad ship");
                    continue;
                }
                if (shortTermBest.ShipStyle == null)
                    continue;

                if (shortTermBest.IsShipyard)
                    continue;

                if (!OwnerEmpire.ShipStyleMatch(shipStyle))
                    continue;

                if (shortTermBest.TechsNeeded.Count == 0)
                {
                    if (OwnerEmpire.Universe.Debug)
                    {
                        Log.Info(OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                    }
                    continue;
                }
                racialShips.Add(shortTermBest);
            }
            return racialShips;
        }

        HashSet<string> FindBestShip(string modifier, Array<TechEntry> availableTechs, string command)
        {
            var shipTechs     = new HashSet<string>();
            var nonShipTechs  = new HashSet<string>();
            bool needShipTech = modifier.Contains("Ship");

            foreach (TechEntry techEntry in availableTechs)
            {
                if (techEntry.ContainsShipTech())
                    shipTechs.Add(techEntry.UID);

                if (techEntry.ContainsNonShipTechOrBonus())
                    nonShipTechs.Add(techEntry.UID);
            }

            // If not researching ship techs then dont research any ship tech.
            if (!needShipTech)
                return nonShipTechs;

            // If we have a best ship already then use that and return.
            // But only if not using a script
            if (ShouldUseExistingCombatShip(command))
                return UseBestShipTechs(shipTechs, nonShipTechs);

            // Doesn't have a best ship so find one
            // Filter out ships we cant use
            Array<IShipDesign> racialShips = FilterRacialShips();
            Array<IShipDesign> researchableShips = GetResearchableShips(racialShips);

            if (researchableShips.Count <= 0)
                return nonShipTechs;

            // If not using a script dont get a best ship.
            // Or if the modder decided they want to use short term researchable tech only
            if (command != "RANDOM" || DisableShipPicker)
            {
                return UseShipTechProgression(researchableShips, shipTechs, nonShipTechs);
            }
            // choose techs by cheapest ship to research while attempting to also research ships in the same tech tree
            BestCombatShip = PickShipToResearch.FindCheapestShipInList(OwnerEmpire, researchableShips, nonShipTechs);
            return UseBestShipTechs(shipTechs, nonShipTechs);
        }

        private HashSet<string> UseBestShipTechs(HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            // Match researchable techs to techs ship needs.
            if (OwnerEmpire.CanBuildShip(BestCombatShip))
                BestCombatShip = null;

            if (BestCombatShip != null)
            {
                var bestShipTechs = shipTechs.Intersect(BestCombatShip.TechsNeeded);
                if (!bestShipTechs.Any())
                {
                    var bestNoneShipTechs = nonShipTechs.Intersect(BestCombatShip.TechsNeeded);
                    if (!bestNoneShipTechs.Any())
                        BestCombatShip = null;
                    else
                        Log.Warning($"ship tech classified as non ship tech {bestNoneShipTechs.First()} for {BestCombatShip}");
                }
                if (BestCombatShip != null)
                    return UseOnlyWantedShipTechs(bestShipTechs, nonShipTechs);
            }
            return UseOnlyWantedShipTechs(shipTechs, nonShipTechs);
        }

        private HashSet<string> UseOnlyWantedShipTechs(IEnumerable<string> shipTechs, HashSet<string> nonShipTechs)
        {
            //combine the wanted shiptechs with the nonshiptechs.
            var generalTech = new HashSet<string>();
            foreach (var bTech in shipTechs)
                generalTech.Add(bTech);
            foreach (var nonShip in nonShipTechs)
                generalTech.Add(nonShip);
            return generalTech;
        }

        private Array<TechEntry> ConvertStringToTech(HashSet<string> shipTechs)
        {
            var bestShipTechs = new Array<TechEntry>();

            foreach (string shipTech in shipTechs)
            {
                if (OwnerEmpire.TryGetTechEntry(shipTech, out TechEntry test))
                {
                    if (test.MaxLevel <= 1 || test.Level < test.MaxLevel) 
                        bestShipTechs.Add(test);
                }
            }
            return bestShipTechs;
        }

        public TechEntry BestShipsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs);

        public TechEntry ShipHullTech(IShipDesign bestShip, Array<TechEntry> availableTechs)
        {
            if (bestShip == null) return null;

            var shipTechs = ConvertStringToTech(bestShip.TechsNeeded);
            foreach (TechEntry tech in shipTechs)
            {
                if (tech.GetUnlockableHulls(OwnerEmpire).Count > 0)
                    return tech;
            }
            return null;
        }
        
        bool DisableShipPicker => GlobalStats.Defaults.DisableShipPicker;
        bool EnableTechLineFocusing => GlobalStats.Defaults.EnableShipTechLineFocusing;

        bool ShouldUseExistingCombatShip(string command) =>
            BestCombatShip != null && command == "RANDOM" && (EnableTechLineFocusing || !DisableShipPicker);
    }
}
