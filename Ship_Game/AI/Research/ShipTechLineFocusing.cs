﻿using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public partial class ShipTechLineFocusing
    {
        readonly Empire OwnerEmpire;
        public Ship BestCombatShip { get; private set; }
        public ShipPicker PickShipToResearch;
        ResearchOptions ResearchMods;

        void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

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
                if (OwnerEmpire.ShipsWeCanBuild.Contains(BestCombatShip.Name)
                    || OwnerEmpire.structuresWeCanBuild.Contains(BestCombatShip.Name)
                    || BestCombatShip.shipData.IsShipyard)
                    BestCombatShip = null;
                else
                if (!BestCombatShip.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Any())
                    BestCombatShip = null;
            }
            HashSet<string> shipFilteredTechs = FindBestShip(modifier, availableTechs, scriptedOrRandom);

            //now that we have a target ship to build filter out all the current techs that are not needed to build it.

            availableTechs = ConvertStringToTech(shipFilteredTechs);
            return availableTechs;
        }

        private static bool IsRoleValid(ShipData.RoleName role)
        {
            switch (role)
            {
                case ShipData.RoleName.disabled:
                case ShipData.RoleName.supply:
                case ShipData.RoleName.troop:
                case ShipData.RoleName.prototype:
                case ShipData.RoleName.construction: return false;
                case ShipData.RoleName.freighter:
                case ShipData.RoleName.colony:
                case ShipData.RoleName.ssp:
                case ShipData.RoleName.platform:
                case ShipData.RoleName.station:
                case ShipData.RoleName.troopShip:
                case ShipData.RoleName.support:
                case ShipData.RoleName.bomber:
                case ShipData.RoleName.carrier:
                case ShipData.RoleName.fighter:
                case ShipData.RoleName.scout:
                case ShipData.RoleName.gunboat:
                case ShipData.RoleName.drone:
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.frigate:
                case ShipData.RoleName.destroyer:
                case ShipData.RoleName.cruiser:
                case ShipData.RoleName.battleship:
                case ShipData.RoleName.capital: break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        private bool ShipHasUndiscoveredTech(Ship ship)
        {
            foreach (string techName in ship.shipData.TechsNeeded)
            {
                if (!OwnerEmpire.HasDiscovered(techName))
                    return true;
            }
            return false;
        }

        private bool ShipHasResearchableTech(Ship ship)
        {
            foreach (string techName in ship.shipData.TechsNeeded)
            {
                var tech = OwnerEmpire.GetTechEntry(techName);
                if (!tech.Unlocked && tech.ContainsShipTech())
                    return true;
            }
            return false;
        }

        private Array<Ship> GetResearchableShips(Array<Ship> racialShips)
        {
            var researchableShips = new Array<Ship>();
            foreach (Ship shortTermBest in racialShips)
            {
                // don't try to research ships we have all the tech for.
                if (!ShipHasResearchableTech(shortTermBest)) continue;
                // Don't build ships intended for carriers if there arent any carriers.
                if (!OwnerEmpire.canBuildCarriers && shortTermBest.shipData.CarrierShip)
                    continue;
                // filter out bad roles....
                if (!IsRoleValid(shortTermBest.shipData.HullRole)) continue;
                if (!IsRoleValid(shortTermBest.DesignRole)) continue;
                if (!IsRoleValid(shortTermBest.shipData.Role)) continue;
                if (!shortTermBest.shipData.UnLockable) continue;
                if (ShipHasUndiscoveredTech(shortTermBest)) continue;
                if (!shortTermBest.ShipGoodToBuild(OwnerEmpire)) continue;

                researchableShips.Add(shortTermBest);
            }
            return researchableShips;
        }

        Array<Ship> FilterRacialShips()
        {
            var racialShips = new Array<Ship>();
            foreach (Ship shortTermBest in ResourceManager.GetShipTemplates())
            {
                // restrict to to ships available to this empire.
                string shipStyle = shortTermBest.shipData.ShipStyle ?? shortTermBest.shipData.BaseHull?.ShipStyle;
                if (shipStyle.IsEmpty())
                {
                    Log.Warning($"Ship {shortTermBest?.Name} Tech FilterRacialShip found a bad ship");
                    continue;
                }
                if (shortTermBest.shipData.ShipStyle == null)
                    continue;

                if (shortTermBest.shipData.IsShipyard)
                    continue;

                if (!OwnerEmpire.ShipStyleMatch(shipStyle))
                {
                    if (shipStyle != "Platforms" && shipStyle != "Misc")
                        continue;
                }

                if (shortTermBest.shipData.TechsNeeded.Count == 0)
                {
                    if (Empire.Universe.Debug)
                    {
                        Log.Info(OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                    }
                    continue;
                }
                racialShips.Add(shortTermBest);
            }
            return racialShips;
        }

        SortedList<int, Array<Ship>> BucketShips(Array<Ship> ships, Func<Ship, int> bucketSort)
        {
            //SortRoles
            /*
             * take each ship and create buckets using the bucketSort ascending.
             */
            var roleSorter = new SortedList<int, Array<Ship>>();

            foreach (Ship ship in ships)
            {
                int key = bucketSort(ship);
                if (roleSorter.TryGetValue(key, out Array<Ship> test))
                    test.Add(ship);
                else
                {
                    test = new Array<Ship> { ship };
                    roleSorter.Add(key, test);
                }
            }
            return roleSorter;
        }

        int PickRandomKey(SortedList<int, Array<Ship>> sortedShips, float indexDivisor = 2)
        {
            //choose role
            /*
             * here set the default return to the first array in rolesorter.
             * then iterate through the keys with an ever increasing chance to choose a key.
             */
            int keyChosen = sortedShips.Keys.First();

            int x = (int)(sortedShips.Count / indexDivisor);

            foreach (var role in sortedShips)
            {
                float chance = (float)x++ / sortedShips.Count ;
                float rand = RandomMath.AvgRandomBetween(.001f, 1f, 2);

                if (rand > chance) continue;
                return role.Key;
            }
            return keyChosen;
        }

        //bool GetLineFocusedShip(Array<Ship> researchableShips, HashSet<string> shipTechs)
        //{
        //    BestCombatShip = PickShipToResearch.FindCheapestShipInList(OwnerEmpire, researchableShips, ResearchMods);

        //    return BestCombatShip != null;
        //}

        SortedList<int, Array<Ship>> RoleSorter(SortedList<int, Array<Ship>> hullSorter, int hullKey)
        {
            return BucketShips(hullSorter[hullKey], s =>
            {
                switch (s.DesignRole)
                {
                    case ShipData.RoleName.platform:
                    case ShipData.RoleName.station:
                    case ShipData.RoleName.scout:
                    case ShipData.RoleName.drone:
                    case ShipData.RoleName.fighter:
                    case ShipData.RoleName.freighter:  return 0;
                    case ShipData.RoleName.colony:
                    case ShipData.RoleName.supply:
                    case ShipData.RoleName.troop:
                    case ShipData.RoleName.troopShip:
                    case ShipData.RoleName.support:
                    case ShipData.RoleName.carrier:    return 2;
                    case ShipData.RoleName.gunboat:
                    case ShipData.RoleName.corvette:   return 5;
                    case ShipData.RoleName.bomber:
                    case ShipData.RoleName.frigate:
                    case ShipData.RoleName.destroyer:
                    case ShipData.RoleName.cruiser:    return 1;
                    case ShipData.RoleName.battleship: return 2;
                    case ShipData.RoleName.capital:    return 3;
                    default: return (int)s.DesignRole;
                }
            });
        }

        SortedList<int, Array<Ship>> HullSorter(SortedList<int, Array<Ship>> costSorter, int key)
        {
            return BucketShips(costSorter[key], hull =>
            {
                int countOfHullTechs = hull.shipData.BaseHull.TechsNeeded.Except(OwnerEmpire.ShipTechs).Count();

                return countOfHullTechs < 2 ? 0 : 1;
            });
        }

        SortedList<int, Array<Ship>> TechCostSorter(Array<Ship> shipsToSort)
        {
            return BucketShips(shipsToSort, shortTermBest =>
            {
                int costNormalizer = 5;
                int techCost = OwnerEmpire.TechCost(shortTermBest);
                techCost /=  costNormalizer;
                return techCost;
            });
        }

        SortedList<int, Array<Ship>> TechCountSorter(Array<Ship> shipsToSort, HashSet<string> shipTechs)
        {
            return BucketShips(shipsToSort, shortTermBest =>
            {
                float costNormalizer = 1;
                switch (shortTermBest.DesignRoleType)
                {
                    case ShipData.RoleType.Civilian:      costNormalizer  = 3; break;
                    case ShipData.RoleType.Orbital:       costNormalizer  = 2; break;
                    case ShipData.RoleType.EmpireSupport: costNormalizer  = 3; break;
                    case ShipData.RoleType.Warship:       costNormalizer  = 1; break;
                    case ShipData.RoleType.WarSupport:    costNormalizer  = 2; break;
                    case ShipData.RoleType.Troop:         costNormalizer  = 2; break;
                    case ShipData.RoleType.NotApplicable: costNormalizer  = 100; break;
                }

                costNormalizer += shortTermBest.DesignRole == ShipData.RoleName.carrier && OwnerEmpire.canBuildCarriers  ||
                                  shortTermBest.DesignRole == ShipData.RoleName.bomber  && OwnerEmpire.canBuildBombers  ||
                                  shortTermBest.DesignRole == ShipData.RoleName.troopShip && OwnerEmpire.canBuildTroopShips ||
                                  shortTermBest.DesignRole == ShipData.RoleName.support && OwnerEmpire.canBuildSupportShips ? 1 : 0;

                int techCount = (int)(shortTermBest.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Count() * costNormalizer);
                return techCount;
            });
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
            Array<Ship> racialShips = FilterRacialShips();
            Array<Ship> researchableShips = GetResearchableShips(racialShips);

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
            if (OwnerEmpire.ShipsWeCanBuild.Contains(BestCombatShip?.Name))
                BestCombatShip = null;

            if (BestCombatShip != null)
            {
                var bestShipTechs = shipTechs.Intersect(BestCombatShip.shipData.TechsNeeded).ToArray();
                if (!bestShipTechs.Any())
                {
                    var bestNoneShipTechs = nonShipTechs.Intersect(BestCombatShip.shipData.TechsNeeded).ToArray();
                    if (bestNoneShipTechs.Length == 0)
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
                    // repeater compensator. This needs some deeper logic.
                    // I current just say if you research one level.
                    // Dont research any more.
                    if (test.MaxLevel > 1 && test.Level > 1) continue;
                    bestShipTechs.Add(test);
                }
            }
            return bestShipTechs;
        }

        public bool BestShipNeedsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs) != null;

        public TechEntry BestShipsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs);

        public TechEntry ShipHullTech(Ship bestShip, Array<TechEntry> availableTechs)
        {
            if (bestShip == null) return null;

            var shipTechs = ConvertStringToTech(bestShip.shipData.TechsNeeded);
            foreach (TechEntry tech in shipTechs)
            {
                if (tech.GetUnlockableHulls(OwnerEmpire).Count > 0)
                    return tech;
            }
            return null;
        }

        public bool WasBestShipHullNotChosen(string topic, Array<TechEntry> availableTechs)
        {
            var hullTech = BestShipsHull(availableTechs);

            if (hullTech != null && hullTech.UID != topic)
            {
                BestCombatShip = null;
                return true;
            }
            return false;
        }

        bool DisableShipPicker => GlobalStats.HasMod && GlobalStats.ActiveModInfo.DisableShipPicker;
        bool EnableTechLineFocusing => !GlobalStats.HasMod || GlobalStats.HasMod && GlobalStats.ActiveModInfo.EnableShipTechLineFocusing;

        bool ShouldUseExistingCombatShip(string command) =>
            BestCombatShip != null && command == "RANDOM" && (EnableTechLineFocusing || !DisableShipPicker);
    }
}
