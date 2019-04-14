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
            HashSet<string> allAvailableShipTechs = FindBestShip(modifier, availableTechs, scriptedOrRandom);
            DebugLog($"Best Ship : {BestCombatShip?.shipData.HullRole} : {BestCombatShip?.GetStrength()}");
            DebugLog($" : {BestCombatShip?.Name}");

            //now that we have a target ship to build filter out all the current techs that are not needed to build it.

            if (!GlobalStats.HasMod || !GlobalStats.ActiveModInfo.UseManualScriptedResearch)
                availableTechs = BestShipTechs(allAvailableShipTechs, availableTechs);
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
                if (shortTermBest.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Any() == false) continue;
                //Dont build ships intended for carriers if there arent any carriers.
                if (!OwnerEmpire.canBuildCarriers && shortTermBest.shipData.CarrierShip)
                    continue;
                //filter Hullroles....
                if (!IsRoleValid(shortTermBest.shipData.HullRole)) continue;
                if (!IsRoleValid(shortTermBest.DesignRole)) continue;
                if (!IsRoleValid(shortTermBest.shipData.Role)) continue;

                if (OwnerEmpire.ShipsWeCanBuild.Contains(shortTermBest.Name))
                    continue;
                if (!shortTermBest.ShipGoodToBuild(OwnerEmpire)) continue;
                if (!shortTermBest.shipData.UnLockable) continue;
                if (ShipHasUndiscoveredTech(shortTermBest)) continue;

                researchableShips.Add(shortTermBest);
            }
            return researchableShips;
        }

        private Array<Ship> FilterRacialShips()
        {
            var racialShips = new Array<Ship>();
            foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values) //.Values.OrderBy(tech => tech.shipData.TechScore))
            {

                //restrict to racial ships or otherwise unlocked ships.
                if (shortTermBest?.shipData?.ShipStyle == null)
                {
                    Log.Warning($"Ship {shortTermBest?.Name} Tech FilterRacialShip found a bad ship");
                    continue;
                }
                if (shortTermBest.shipData.ShipStyle == null) continue;
                if (shortTermBest.shipData.IsShipyard)
                    continue;
                var shipStyle = shortTermBest.shipData.ShipStyle;
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

        int PickRandomKey(SortedList<int, Array<Ship>> sortedShips)
        {
            //choose role
            /*
             * here set the default return to the first array in rolesorter.
             * then iterate through the keys with an ever increasing chance to choose a key.
             */
            int keyChosen = sortedShips.Keys.First();

            int x = 0;

            foreach (var role in sortedShips)
            {
                float chance = (float)++x / (sortedShips.Count * 2);

                float rand = RandomMath.AvgRandomBetween(.01f, 1f);
                if (rand > chance) continue;
                return role.Key;
            }
            return keyChosen;
        }

        string[] Neededtechs(HashSet<string> CurrentshipTechs, HashSet<string> techToFilter) =>
            techToFilter.Except(OwnerEmpire.ShipTechs).Except(CurrentshipTechs).ToArray();

        private bool GetLineFocusedShip(Array<Ship> researchableShips, HashSet<string> shipTechs)
        {
            //There is a bug here. researchable ships contains ships that have already been researched or at least
            //have no techs that can be researched?


            // Bucket ships by how many techs they have that are not already researched
            SortedList<int, Array<Ship>> techSorter = TechSorter(researchableShips, shipTechs);

            //Bail if there aren't any ships to research
            if (techSorter.Count == 0)
                return false;

            SortedList<int, Array<Ship>> newHull = NewHull(shipTechs, techSorter, techSorter.First().Key);

            SortedList<int, Array<Ship>> costSorter = CostSorter(newHull);

            /* This is part that chooses the  hull
            takes the first entry from the least techs needed list.
             then sorts it by the number of techs needed for the hull
             */

            SortedList<int, Array<Ship>> hullSorter = HullSorter(costSorter);
            int hullKey = PickRandomKey(hullSorter);
            //sort roles
            SortedList<int, Array<Ship>> roleSorter = RoleSorter(hullSorter, hullKey);

            //choose Ship
            int roleKey = PickRandomKey(roleSorter);

            var ships = roleSorter[roleKey];

            for (int x = 0; x <= ships.Count -1; x++)
            {
                var ship     = ships[x];
                float chance = (float)(x + 1) / ships.Count * 2;
                float rand   = RandomMath.RandomBetween(.01f, 1f);

                if (rand > chance)
                    continue;
                return (BestCombatShip = ship) != null;
            }
            return false;
        }

        private SortedList<int, Array<Ship>> NewHull(HashSet<string> shipTechs, SortedList<int, Array<Ship>> roleSorter, int roleKey)
        {
            var newHull = BucketShips(roleSorter[roleKey],
                hull =>
                {
                    if (hull.DesignRole == ShipData.RoleName.station ||
                       hull.DesignRole == ShipData.RoleName.platform ||
                       hull.DesignRole == ShipData.RoleName.freighter) return 1;

                    return hull.BaseHull.TechsNeeded.Intersect(shipTechs).Any()
                        ? 0
                        : 1;
                });
            return newHull;
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
                            return 9;
                        case ShipData.RoleName.colony:
                        case ShipData.RoleName.supply:
                        case ShipData.RoleName.troop:
                            return 10;
                        case ShipData.RoleName.freighter:
                            return 8;
                        case ShipData.RoleName.troopShip:
                        case ShipData.RoleName.support:
                        case ShipData.RoleName.bomber:
                        case ShipData.RoleName.carrier:
                            return 0;
                        case ShipData.RoleName.scout:
                        case ShipData.RoleName.drone:
                            return 1;
                        case ShipData.RoleName.fighter:
                        case ShipData.RoleName.gunboat:
                        case ShipData.RoleName.corvette:
                            return 2;
                        case ShipData.RoleName.frigate:
                        case ShipData.RoleName.destroyer:
                            return 3;
                        case ShipData.RoleName.cruiser:
                            return 4;
                        case ShipData.RoleName.capital:
                            return 5;

                        default:
                            return (int)s.DesignRole;
                    }
                    return (int) s.DesignRole;
                });
            return roleSorter;
        }

        private SortedList<int, Array<Ship>> HullSorter(SortedList<int, Array<Ship>> costSorter)
        {
            var key = costSorter.First().Key;
            var hullSorter = BucketShips(costSorter[key],
                hull => hull.shipData.BaseHull.TechsNeeded.Except(OwnerEmpire.ShipTechs).Count());
            return hullSorter;
        }

        private SortedList<int, Array<Ship>> CostSorter(SortedList<int, Array<Ship>> techSorter)
        {
            var key = PickRandomKey(techSorter);
            var costSorter = BucketShips(techSorter[key], ship =>
                ship.shipData.TechScore / (int) (1 + OwnerEmpire.Research * 100));
            return costSorter;
        }

        private SortedList<int, Array<Ship>> TechSorter(Array<Ship> researchableShips, HashSet<string> shipTechs)
        {
            var techSorter = BucketShips(researchableShips,
                shortTermBest =>
                    shortTermBest.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Except(shipTechs).Any() ? 0 : 1);
            return techSorter;
        }

        private HashSet<string> FindBestShip(string modifier, Array<TechEntry> availableTechs, string command)
        {
            HashSet<string> shipTechs       = new HashSet<string>();
            HashSet<string> nonShipTechs    = new HashSet<string>();

            foreach (TechEntry techEntry in availableTechs)
            {
                foreach (var type in techEntry.TechnologyTypes)
                {
                    switch (type)
                    {
                        case TechnologyType.ShipHull:
                        case TechnologyType.ShipDefense:
                        case TechnologyType.ShipWeapons:
                        case TechnologyType.ShipGeneral:
                            shipTechs.Add(techEntry.UID);
                            break;
                        case TechnologyType.General:
                        case TechnologyType.Colonization:
                        case TechnologyType.Economic:
                        case TechnologyType.Industry:
                        case TechnologyType.Research:
                        case TechnologyType.GroundCombat:
                            nonShipTechs.Add(techEntry.UID);
                            break;
                    }

                }
            }
            if (!modifier.Contains("ShipWeapons") && !modifier.Contains("ShipDefense") &&
                !modifier.Contains("ShipGeneral") && !modifier.Contains("ShipHull"))
            {
                nonShipTechs.ExceptWith(shipTechs);
                return nonShipTechs;
            }

            if (BestCombatShip != null && command == "RANDOM")
            {
                //foreach (var bTech in BestCombatShip.shipData.TechsNeeded.Intersect(shipTechs))
                //    nonShipTechs.Add(bTech);
                DebugLog(
                    $"Best Ship : {BestCombatShip.shipData.HullRole} : {BestCombatShip.GetStrength()}");
                DebugLog($" : {BestCombatShip.Name}");
                return shipTechs;
            }

            //now look through are cheapest to research designs that get use closer to the goal ship using pretty much the same logic.
            Array<Ship> racialShips        = FilterRacialShips();
            Array<Ship> researchableShips = GetResearchableShips(racialShips);

            if (researchableShips.Count <= 0) return nonShipTechs;

            GetLineFocusedShip(researchableShips, shipTechs);

            //foreach (var tech in BestCombatShip.shipData.TechsNeeded.Intersect(shipTechs))
            //    nonShipTechs.Add(tech);
            return shipTechs;
        }

        private Array<TechEntry> BestShipTechs(HashSet<string> shipTechs, Array<TechEntry> availableTechs)
        {
            var bestShipTechs = new Array<TechEntry>();

            // use the shiptech choosers which just chooses tech in the list.
            TechEntry[] repeatingTechs = OwnerEmpire.TechEntries.Filter(t => t.MaxLevel > 1);

            foreach (string shipTech in shipTechs)
            {
                if (OwnerEmpire.TryGetTechEntry(shipTech, out TechEntry test))
                {
                    bool skipRepeater = false;
                    // repeater compensator. This needs some deeper logic.
                    // I current just say if you research one level.
                    // Dont research any more.
                    if (test.MaxLevel > 1)
                    {
                        foreach (TechEntry repeater in repeatingTechs)
                        {
                            if (test == repeater && (repeater.Level > 0))
                            {
                                skipRepeater = true;
                                break;
                            }
                        }
                        if (skipRepeater)
                            continue;
                    }
                    bestShipTechs.Add(test);
                }
            }

            bestShipTechs = availableTechs.Intersect(bestShipTechs).ToArrayList();
            return bestShipTechs;
        }

        public bool BestShipNeedsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs) != null;

        public TechEntry BestShipsHull(Array<TechEntry> availableTechs) => ShipHullTech(BestCombatShip, availableTechs);

        public TechEntry ShipHullTech(Ship bestShip, Array<TechEntry> availableTechs)
        {
            if (bestShip == null) return null;

            var shipTechs = BestShipTechs(bestShip.shipData.TechsNeeded, availableTechs);
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
