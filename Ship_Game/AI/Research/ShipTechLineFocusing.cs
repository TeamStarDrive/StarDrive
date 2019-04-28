using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public class ShipTechLineFocusing
    {
        readonly Empire OwnerEmpire;
        public Ship BestCombatShip { get; private set; }

        void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        public ShipTechLineFocusing (Empire empire)
        {
            OwnerEmpire = empire;
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
                case ShipData.RoleName.construction:
                    return false;
                case ShipData.RoleName.freighter:
                case ShipData.RoleName.colony:

                case ShipData.RoleName.platform:
                    break;
                case ShipData.RoleName.station:
                    break;
                case ShipData.RoleName.troopShip:
                    break;
                case ShipData.RoleName.support:
                    break;
                case ShipData.RoleName.bomber:
                    break;
                case ShipData.RoleName.carrier:
                    break;
                case ShipData.RoleName.fighter:
                    break;
                case ShipData.RoleName.scout:
                    break;
                case ShipData.RoleName.gunboat:
                    break;
                case ShipData.RoleName.drone:
                    break;
                case ShipData.RoleName.corvette:
                    break;
                case ShipData.RoleName.frigate:
                    break;
                case ShipData.RoleName.destroyer:
                    break;
                case ShipData.RoleName.cruiser:
                    break;
                case ShipData.RoleName.capital:
                    break;

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

        private Array<Ship> GetResearchableShips(Array<Ship> racialShips)
        {
            var researchableShips = new Array<Ship>();
            foreach (Ship shortTermBest in racialShips)
            {
                // don't try to research ships we have all the tech for. 
                if (shortTermBest.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Any() == false) continue;
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

        private Array<Ship> FilterRacialShips()
        {
            var racialShips = new Array<Ship>();
            foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values) 
            {

                //restrict to to ships available to this empire. 
                string shipStyle = shortTermBest?.shipData?.ShipStyle ?? shortTermBest?.shipData?.BaseHull?.ShipStyle;
                if (shipStyle.IsEmpty())
                {
                    Log.Warning($"Ship {shortTermBest?.Name} Tech FilterRacialShip found a bad ship");
                    continue;
                }
                if (shortTermBest.shipData.ShipStyle == null)
                    continue;

                if (shortTermBest.shipData.IsShipyard)
                    continue;

                if (shipStyle != OwnerEmpire.data.Traits.ShipType)
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

        private SortedList<int, Array<Ship>> BucketShips(Array<Ship> ships, Func<Ship, int> bucketSort)
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

        int PickRandomKey(SortedList<int, Array<Ship>> sortedShips, float indexDivisor)
        {
            //choose role
            /*
             * here set the default return to the first array in rolesorter.
             * then iterate through the keys with an ever increasing chance to choose a key.
             */
            int keyChosen = sortedShips.Keys.First();

            int x = (int)(sortedShips.Count / indexDivisor);
            float rand = RandomMath.AvgRandomBetween(.001f, 1f);
            foreach (var role in sortedShips)
            {
                float chance = (float)x++ / sortedShips.Count ;

               
                if (rand > chance) continue;
                return role.Key;
            }
            return keyChosen;
        }

        string[] Neededtechs(HashSet<string> CurrentshipTechs, HashSet<string> techToFilter) =>
            techToFilter.Except(OwnerEmpire.ShipTechs).Except(CurrentshipTechs).ToArray();

        private bool GetLineFocusedShip(Array<Ship> researchableShips, HashSet<string> shipTechs)
        {
            // Bucket ships by how many techs they have that are not already researched
            researchableShips.Sort(s => s.shipData.TechScore / ((int)OwnerEmpire.MaxResearchPotential * 100));
            SortedList<int, Array<Ship>> techSorter = TechSorter(researchableShips, shipTechs);

            //Bail if there aren't any ships to research
            if (techSorter.Count == 0)
                return false;
            int techKey = PickRandomKey(techSorter, 1.4f);

            /* This is part that chooses the  hull
            takes the first entry from the least techs needed list.
             then sorts it by the number of techs needed for the hull
             */

            SortedList<int, Array<Ship>> hullSorter = HullSorter(techSorter, techKey);
            int hullKey = PickRandomKey(hullSorter, 2);
            //sort roles
            SortedList<int, Array<Ship>> roleSorter = RoleSorter(hullSorter, hullKey);
            int roleKey = PickRandomKey(roleSorter, roleSorter.Count);

            //choose Ship
            var ships = roleSorter[roleKey];

            for (int x = 0; x <= ships.Count -1; x++)
            {
                var ship     = ships[x];
                float chance = (float)(x + 1) / ships.Count;
                float rand   = RandomMath.RandomBetween(.01f, 1f);

                if (rand > chance)
                    continue;
                return (BestCombatShip = ship) != null;
            }
            return false;
        }

        private SortedList<int, Array<Ship>> RoleSorter(SortedList<int, Array<Ship>> hullSorter, int hullKey)
        {
            var roleSorter = BucketShips(hullSorter[hullKey],
                s =>
                {
                    switch (s.DesignRole)
                    {
                        case ShipData.RoleName.platform:
                        case ShipData.RoleName.station:
                        case ShipData.RoleName.scout:
                        case ShipData.RoleName.drone:
                        case ShipData.RoleName.fighter:
                        case ShipData.RoleName.freighter: return 0;
                        case ShipData.RoleName.colony:
                        case ShipData.RoleName.supply:
                        case ShipData.RoleName.troop:                        
                        case ShipData.RoleName.troopShip:
                        case ShipData.RoleName.support:
                        case ShipData.RoleName.bomber:                        
                        case ShipData.RoleName.carrier: return 3;
                        case ShipData.RoleName.gunboat:
                        case ShipData.RoleName.corvette:  return 4;
                        case ShipData.RoleName.frigate:
                        case ShipData.RoleName.destroyer: 
                        case ShipData.RoleName.cruiser: return 1;                        
                        case ShipData.RoleName.capital:   return 2;
                        default: return (int)s.DesignRole;
                    }
                });
            return roleSorter;
        }

        private SortedList<int, Array<Ship>> HullSorter(SortedList<int, Array<Ship>> costSorter, int key)
        {
            var hullSorter = BucketShips(costSorter[key],
                hull =>
                {
                    int countOfHullTechs = hull.shipData.BaseHull.TechsNeeded.Except(OwnerEmpire.ShipTechs).Count();
                    if (hull.DesignRole < ShipData.RoleName.troopShip)
                        countOfHullTechs += 1;
                        return countOfHullTechs < 2
                            ? 0
                            : 1;
                });
            return hullSorter;
        }

        private SortedList<int, Array<Ship>> CostSorter(SortedList<int, Array<Ship>> shipToBeSorted, int key)
        {
            var research = Math.Max(OwnerEmpire.Research, 1);
            int scoreDivisor = (int)(research * 100);
            var costSorter = BucketShips(shipToBeSorted[key], ship =>
            {
                var score = ship.shipData.TechScore / scoreDivisor;
                if (ship.DesignRole < ShipData.RoleName.troopShip)
                    score += 4;
                return score;
            });
            return costSorter;
        }

        private SortedList<int, Array<Ship>> TechSorter(Array<Ship> researchableShips, HashSet<string> shipTechs)
        {
            var techSorter = BucketShips(researchableShips,
                shortTermBest =>
                {
                    var techCost = OwnerEmpire.TechCost(shortTermBest);
                    techCost /= (int)(OwnerEmpire.MaxResearchPotential + 1) * 100;
                    return techCost;                    
                });
            return techSorter;
        }

        private HashSet<string> FindBestShip(string modifier, Array<TechEntry> availableTechs, string command)
        {
            HashSet<string> shipTechs       = new HashSet<string>();
            HashSet<string> nonShipTechs    = new HashSet<string>();

            foreach (TechEntry techEntry in availableTechs)
            {
                if (techEntry.IsShipTech())
                    shipTechs.Add(techEntry.UID);
                else
                    nonShipTechs.Add(techEntry.UID);
            }
            //if not researching shiptechs then dont research any shiptechs.
            if (!modifier.Contains("Ship"))
                return nonShipTechs;
            
            // if we have a best ship already then use that and return.
            // But only if not using a script
            if (BestCombatShip != null && command == "RANDOM")
                return UseBestShipTechs(shipTechs, nonShipTechs);

            //doesn't have a best ship so find one.
            //filter out ships we cant use
            Array<Ship> racialShips        = FilterRacialShips();
            Array<Ship> researchableShips = GetResearchableShips(racialShips);

            if (researchableShips.Count <= 0) return nonShipTechs;
            // If not using a script dont get a best ship.
            if (command != "RANDOM")
                return UseResearchableShipTechs(researchableShips, shipTechs, nonShipTechs);

            //now find a new ship to research that uses most of the tech we already have.
            GetLineFocusedShip(researchableShips, shipTechs);
            if (BestCombatShip != null)
                return UseBestShipTechs(shipTechs, nonShipTechs);            
            return shipTechs;
        }

        private HashSet<string> UseBestShipTechs(HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            //filter out all current shiptechs that dont match the best ships techs.
            IEnumerable<string> bestShipTechs = shipTechs.Intersect(BestCombatShip.shipData.TechsNeeded);
            return UseOnlyWantedShipTechs(bestShipTechs, nonShipTechs);
        }
        private HashSet<string> UseResearchableShipTechs(Array<Ship> researchableShips, HashSet<string> shipTechs, HashSet<string> nonShipTechs)
        {
            //filter out all current shiptechs that arent in researchableShips.
            HashSet<string> goodShipTechs = new HashSet<string>();
            foreach (var ship in researchableShips)
            {
                var researchableTechs = shipTechs.Intersect(ship.shipData.TechsNeeded);
                foreach (var tech in researchableTechs)
                    goodShipTechs.Add(tech);
            }
            return UseOnlyWantedShipTechs(goodShipTechs, nonShipTechs);
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
                    bool skipRepeater = false;
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
    }
}
