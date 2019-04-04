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
                case ShipData.RoleName.freighter:
                case ShipData.RoleName.colony:
                    return false;
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
            foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values.OrderBy(tech => tech.shipData.TechScore))
            {
                try
                {
                    //restrict to racial ships or otherwise unlocked ships.
                    if (shortTermBest.shipData.ShipStyle == null
                        || shortTermBest.shipData.ShipStyle != "Platforms" && shortTermBest.shipData.ShipStyle != "Misc"
                                                                           && shortTermBest.shipData.ShipStyle != OwnerEmpire.data.Traits.ShipType)
                        continue;

                    if (shortTermBest.shipData.TechsNeeded.Count == 0)
                    {
                        if (Empire.Universe.Debug)
                        {
                            Log.Info(OwnerEmpire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                        }
                        continue;
                    }
                }
                catch
                {
                    Log.Warning($"Ship {shortTermBest.Name} has not shipData");
                    continue;
                }
                racialShips.Add(shortTermBest);
            }
            return racialShips;
        }

        private int ChooseRole(Array<Ship> ships, SortedList<int, Array<Ship>> roleSorter, Func<Ship, int> func)
        {
            //SortRoles
            /*
             * take each ship in ships and make a sorted list based on the hull role index.
             */
            foreach (Ship ship in ships)
            {
                int key = func(ship);
                if (roleSorter.TryGetValue(key, out Array<Ship> test))
                    test.Add(ship);
                else
                {
                    test = new Array<Ship> { ship };
                    roleSorter.Add(key, test);
                }
            }
            //choose role
            /*
             * here set the default return to the first array in rolesorter.
             * then iterater through the keys with an every increasing chance to choose a key.
             */
            int keyChosen = roleSorter.Keys.First();

            int x = 0;
            foreach (var role in roleSorter)
            {
                float chance = (float)++x / roleSorter.Count;

                float rand = RandomMath.AvgRandomBetween(.01f, 1f);
                if (rand > chance) continue;
                return role.Key;
            }
            return keyChosen;
        }

        private bool GetLineFocusedShip(Array<Ship> researchableShips, HashSet<string> shipTechs)
        {
            var techSorter = new SortedList<int, Array<Ship>>();
            foreach (Ship shortTermBest in researchableShips)
            {
                //forget the cost of tech that provide these ships. These are defined in techentry class.
                if (!OwnerEmpire.canBuildCarriers && shortTermBest.shipData.CarrierShip)
                    continue;

                /*try to line focus to main goal but if we cant, line focus as best as possible.
                 * To do this use a sorted list with a key set to the count of techs needed minus techs we already have.
                 * since i dont know which key the ship will be added to this seems the easiest without a bunch of extra steps.
                 * Now this list can be used to not just get the one with fewest techs but add a random to get a little variance.
                 */
                Array<string> currentTechs =
                    new Array<string>(shortTermBest.shipData.TechsNeeded.Except(OwnerEmpire.ShipTechs).Except(shipTechs));

                int key = currentTechs.Count;

                /* this is kind of funky but the idea is to add a key and list if it doesnt already exist.
                 Because i dont know how many will be in it.
                 */
                if (techSorter.TryGetValue(key, out Array<Ship> test))
                    test.Add(shortTermBest);
                else
                {
                    test = new Array<Ship> { shortTermBest };
                    techSorter.Add(key, test);
                }
            }

            var hullSorter = new SortedList<int, Array<Ship>>();

            //This is part that chooses the bestShip hull
            /* takes the first entry from the least techs needed list. then sorts it the hull role needed
             */
            //try to fix sentry bug :https://sentry.io/blackboxmod/blackbox/issues/533939032/events/26436104750/
            if (techSorter.Count == 0)
                return false;

            int keyChosen = ChooseRole(techSorter[techSorter.Keys.First()], hullSorter, h => (int)h.shipData.HullRole);
            //sort roles
            var roleSorter = new SortedList<int, Array<Ship>>();
            keyChosen = ChooseRole(hullSorter[keyChosen], roleSorter,s => (int)s.DesignRole);

            //choose Ship
            Array<Ship> ships = new Array<Ship>(roleSorter[keyChosen].
                OrderByDescending(ship => ship.shipData.TechsNeeded.Count));
            for (int x = 1; x <= ships.Count; x++)
            {
                var ship     = ships[x - 1];
                float chance = (float)x / ships.Count;
                float rand   = RandomMath.RandomBetween(.01f, 1f);

                if (rand > chance)
                    continue;
                return (BestCombatShip = ship) != null;
            }
            return false;
        }

        private HashSet<string> FindBestShip(string modifier, Array<TechEntry> availableTechs, string command)
        {
            HashSet<string> shipTechs       = new HashSet<string>();
            HashSet<string> nonShipTechs    = new HashSet<string>();

            foreach (TechEntry bestshiptech in availableTechs)
            {
                switch (bestshiptech.TechnologyType)
                {
                    case TechnologyType.General:
                    case TechnologyType.Colonization:
                    case TechnologyType.Economic:
                    case TechnologyType.Industry:
                    case TechnologyType.Research:
                    case TechnologyType.GroundCombat:
                        nonShipTechs.Add(bestshiptech.UID);
                        continue;
                    case TechnologyType.ShipHull:
                        break;
                    case TechnologyType.ShipDefense:
                        break;
                    case TechnologyType.ShipWeapons:
                        break;
                    case TechnologyType.ShipGeneral:
                        break;
                }
                shipTechs.Add(bestshiptech.UID);
            }
            if (!modifier.Contains("ShipWeapons") && !modifier.Contains("ShipDefense") &&
                !modifier.Contains("ShipGeneral") && !modifier.Contains("ShipHull"))
                return nonShipTechs;

            if (BestCombatShip != null && command == "RANDOM")
            {
                foreach (var bTech in BestCombatShip.shipData.TechsNeeded)
                    nonShipTechs.Add(bTech);
                DebugLog(
                    $"Best Ship : {BestCombatShip.shipData.HullRole} : {BestCombatShip.GetStrength()}");
                DebugLog($" : {BestCombatShip.Name}");
                return nonShipTechs;
            }

            //now look through are cheapest to research designs that get use closer to the goal ship using pretty much the same logic.
            Array<Ship> racialShips        = FilterRacialShips();
            Array<Ship> shipsReasearchable = GetResearchableShips(racialShips);

            if (shipsReasearchable.Count <= 0) return nonShipTechs;

            if (!GetLineFocusedShip(shipsReasearchable, shipTechs))
                return nonShipTechs;
            foreach (var tech in BestCombatShip.shipData.TechsNeeded)
                nonShipTechs.Add(tech);
            return nonShipTechs;
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

    }
}
