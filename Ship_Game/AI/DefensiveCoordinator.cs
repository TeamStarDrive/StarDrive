using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed class DefensiveCoordinator : IDisposable
    {
        readonly Empire Us;
        public float DefenseDeficit;
        public Map<SolarSystem, SystemCommander> DefenseDict = new Map<SolarSystem, SystemCommander>();
        public Array<Ship> DefensiveForcePool = new Array<Ship>();
        public float EmpireTroopRatio;
        int TotalValue;
        public float UniverseWants;

        public DefensiveCoordinator(Empire e)
        {
            Us = e;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~DefensiveCoordinator()
        {
            Destroy();
        }

        void Destroy()
        {
            DefensiveForcePool = null;
            if (DefenseDict != null)
                foreach (var kv in DefenseDict)
                    kv.Value?.Dispose();
            DefenseDict = null;
        }

        public void AddShip(Ship ship)
        {
            ship.AI.OrderQueue.Clear();
            ship.AI.SystemToDefend = null;
            ship.AI.SystemToDefendGuid = Guid.Empty;
            ship.AI.HasPriorityOrder = false;
            ship.AI.State = AIState.SystemDefender;
            DefenseDeficit -= ship.GetStrength();
            DefensiveForcePool.Add(ship);
        }

        //added by gremlin parallel forcepool
        public float GetForcePoolStrength()
        {
            float strength = 0;
            for (int index = 0; index < DefensiveForcePool.Count; index++)
            {
                Ship ship = DefensiveForcePool[index];
                if (!ship.Active || ship.dying) continue;
                strength += ship.GetStrength();
            }

            return strength;
        }

        public float GetDefensiveThreatFromPlanets(Planet[] planets)
        {
            if (DefenseDict.Count == 0) return 0;
            int count = 0;
            float str = 0;
            for (int index = 0; index < planets.Length; index++)
            {
                Planet planet = planets[index];
                if (!DefenseDict.TryGetValue(planet.ParentSystem, out SystemCommander scom)) continue;
                count++;
                str += scom.RankImportance;
            }

            return str / count;
        }

        public Planet AssignIdleShips(Ship ship)
        {
            return DefenseDict.TryGetValue(ship.AI.SystemToDefend, out SystemCommander systemCommander)
                ? systemCommander.AssignIdleDuties(ship)
                : DefenseDict.First().Value.AssignIdleDuties(ship);
        }

        public void Remove(Ship ship)
        {
            SolarSystem systoDefend = ship.AI.SystemToDefend;
            if (!DefensiveForcePool.Remove(ship))
            {
                if (ship.Active && systoDefend != null)
                    DebugInfoScreen.DefenseCoLogsNotInPool();
            }
            else if (ship.Active && systoDefend == null)
                DebugInfoScreen.DefenseCoLogsSystemNull();


            bool found = false;
            if (systoDefend != null && DefenseDict.TryGetValue(systoDefend, out SystemCommander sysCom))
                //check for specific system commander
            {
                if (!sysCom.RemoveShip(ship))
                {
                    if (ship.Active)
                        DebugInfoScreen.DefenseCoLogsNotInSystem();
                }
                else found = true;

                // return; // when sysdefense is safe enable. 
            }

            foreach (var kv in DefenseDict) //double check for ship in any other sys commanders
            {
                if (!kv.Value.RemoveShip(ship)) continue;
                if (!found)
                    found = true;
                else DebugInfoScreen.DefenseCoLogsMultipleSystems();
            }

            DebugInfoScreen.DefenseCoLogsNull(found, ship, systoDefend);
        }

        public void RemoveFleet(ShipGroup shipGroup)
        {
            foreach (Ship ship in shipGroup.GetShips)
                Remove(ship);
        }

        public void RemoveShipList(Array<Ship> ships)
        {
            foreach (Ship ship in ships)
                Remove(ship);
        }


        void CalculateSystemImportance()
        {
            foreach (Planet p in Empire.Universe.PlanetsDict.Values)
                //@TODO move this to planet. this is removing troops without any safety
            {
                if (p.Owner != Us && !p.EventsOnBuildings() && !p.TroopsHereAreEnemies(Us))
                {
                    p.TroopsHere.ApplyPendingRemovals();
                    foreach (Troop troop in p.TroopsHere.Where(loyalty => loyalty.GetOwner() == Us))
                    {
                        p.TroopsHere.QueuePendingRemoval(troop);
                        troop.Launch();
                    }

                    p.TroopsHere.ApplyPendingRemovals();
                }
                else if (p.Owner == Us) //This should stay here.
                {
                    if (p.ParentSystem == null || DefenseDict.ContainsKey(p.ParentSystem)) continue;
                    DefenseDict.Add(p.ParentSystem, new SystemCommander(Us, p.ParentSystem));
                }
            }

            TotalValue = 0;

            foreach (var kv in DefenseDict.ToArray())
            {
                if (kv.Key.OwnerList.Contains(Us))
                {
                    kv.Value.UpdatePlanetTracker();
                    continue;
                }

                kv.Value.Dispose();
                DefenseDict.Remove(kv.Key);
            }

            foreach (var kv in DefenseDict)
                kv.Value.UpdateSystemValue();

            int ranker = 0;
            int split = DefenseDict.Count / 10;
            int splitStore = split;
            //@Complex Double orderBy is not simple.
            var sComs = DefenseDict.OrderBy(value => value.Value.PercentageOfValue)
                .ThenBy(devlev => devlev.Value.SystemDevelopmentlevel);
            foreach (var kv in sComs)
            {
                split--;
                if (split <= 0)
                {
                    ranker++;
                    split = splitStore;
                    if (ranker > 10)
                        ranker = 10;
                }

                kv.Value.RankImportance = ranker;
            }

            foreach (var kv in sComs)
            {
                kv.Value.RankImportance = (int) (10 * (kv.Value.RankImportance / ranker));
                TotalValue += (int) kv.Value.ValueToUs;
                kv.Value.CalculateShipneeds();
                kv.Value.CalculateTroopNeeds();
            }
        }

        void ManageShips()
        {
            var sComs = DefenseDict.OrderByDescending(rank => rank.Value.RankImportance).ToArray();
            int strToAssign = (int) GetForcePoolStrength();
            float startingStr = strToAssign;
            DefenseDeficit = 0;
            foreach (var kv in sComs)
            {
                strToAssign -= kv.Value.IdealShipStrength;
                DefenseDeficit += kv.Value.IdealShipStrength - kv.Value.CurrentShipStr;
            }

            if (strToAssign < 0f) strToAssign = 0;

            foreach (var kv in DefenseDict)
            {
                kv.Value.PercentageOfValue = kv.Value.ValueToUs / TotalValue;
                int min = (int) (strToAssign * kv.Value.PercentageOfValue);
                if (kv.Value.IdealShipStrength < min) kv.Value.IdealShipStrength = min;
            }

            var assignedShips = new Map<Guid, Ship>();
            var shipsAvailableForAssignment = new Array<Ship>();
            //Remove excess force:
            foreach (var kv in DefenseDict)
            {
                shipsAvailableForAssignment.AddRange(kv.Value.RemoveExtraShips());
            }

            //Add available force to pool:            
            for (int x = DefensiveForcePool.Count - 1; x >= 0; x--)
            {
                Ship ship = DefensiveForcePool[x];
                if (ship.Active
                    && !(ship.AI.HasPriorityOrder || ship.AI.State == AIState.Resupply))
                {
                    if (ship.AI.SystemToDefend == null)
                        shipsAvailableForAssignment.Add(ship);
                    else
                        ship.AI.State = AIState.SystemDefender;
                }
                else Remove(ship);
            }

            //Assign available force:
            foreach (var kv in DefenseDict)
            {
                kv.Value?.AssignTargets();
            }

            if (shipsAvailableForAssignment.Count > 0)
            {
                foreach (var kv in sComs)
                {
                    if (startingStr < 0f) break;

                    foreach (Ship ship in shipsAvailableForAssignment
                        .OrderBy(ship => ship.Center.SqDist(kv.Key.Position)))
                    {
                        if (ship.AI.State == AIState.Resupply
                            || ship.AI.State == AIState.SystemDefender
                            && ship.AI.SystemToDefend != null)
                            continue;
                        if (!ship.Active)
                        {
                            Remove(ship);
                            continue;
                        }

                        if (assignedShips.ContainsKey(ship.guid)) continue;
                        if (startingStr <= 0f || !kv.Value.AddShip(ship)) break;

                        assignedShips.Add(ship.guid, ship);
                        startingStr = startingStr - ship.GetStrength();
                    }
                }
            }
        }

        public SolarSystem GetNearestSystemNeedingTroops(Vector2 fromPos)
        {
            float width = Empire.Universe.UniverseSize;
            return DefenseDict.MaxKeyByValuesFiltered(
                com => (1 - com.TroopCount / com.IdealTroopCount) * com.ValueToUs
                                                                  * ((width - com.System.Position.SqDist(fromPos)) /
                                                                     width),
                com => com.TroopStrengthNeeded > 0
                       && com.System.PlanetList.Count > 0
                       && com.System.PlanetList.Sum(p => p.GetGroundLandingSpots()) > 0
            );
        }

        void ManageTroops()
        {
            if (Us.isPlayer)
            {
                bool flag = false;
                foreach (Planet planet in Us.GetPlanets())
                {
                    if (planet.colonyType != Planet.ColonyType.Military)
                        continue;
                    flag = true;
                    break;
                }

                if (!flag)
                    return;
            }

            Array<Ship> troopShips = Us.GetAvailableTroopShips();
            int totalTroopWanted = 0;
            int totalCurrentTroops = 0;
            foreach (var kv in DefenseDict)
            {
                // find max number of troops for system.

                int currentTroops = kv.Value.TroopCount;
                for (int i = troopShips.Count - 1; i >= 0; i--)
                {
                    Ship troop = troopShips[i];

                    if (troop == null || troop.TroopList.Count <= 0)
                    {
                        troopShips.Remove(troop);
                        continue;
                    }

                    ShipAI troopAI = troop.AI;
                    if (troopAI == null)
                    {
                        troopShips.Remove(troop);
                        continue;
                    }

                    if (troopAI.State == AIState.Rebase &&
                        troopAI.OrderQueue.NotEmpty
                        && troopAI.OrderQueue.Any(goal =>
                            goal.TargetPlanet != null && kv.Key == goal.TargetPlanet.ParentSystem))
                    {
                        currentTroops++;
                        kv.Value.TroopStrengthNeeded--;
                        troopShips.Remove(troop);
                    }
                }

                kv.Value.TroopCount = currentTroops;
                totalCurrentTroops += currentTroops;
                totalTroopWanted += kv.Value.TroopsWanted;
            }


            UniverseWants = totalCurrentTroops / (float) totalTroopWanted;

            for (int x = troopShips.Count - 1; x >= 0; x--)
            {
                Ship troopShip = troopShips[x];

                SolarSystem solarSystem = GetNearestSystemNeedingTroops(troopShip.Center);

                if (solarSystem == null)
                    break;

                SystemCommander defenseSystem = DefenseDict[solarSystem];

                defenseSystem.TroopStrengthNeeded--;
                troopShips.Remove(troopShip);

                Planet target = solarSystem.PlanetList
                    .FindMinFiltered(p => p.Owner == troopShip.loyalty && p.GetGroundLandingSpots() > 0,
                        planet => planet.CountEmpireTroops(planet.Owner));

                if (target == null) continue;
                troopShip.AI.OrderRebase(target, true);
            }

            EmpireTroopRatio = UniverseWants;
            if (UniverseWants > 1.25f)
            {
                foreach (var troop in troopShips)
                {
                    if (troop.DesignRole != ShipData.RoleName.troop) continue;
                    if (troop.AI.State == AIState.AwaitingOrders)
                        troop.AI.OrderScrapShip();
                }
            }

            if (UniverseWants > .8f) return;

            foreach (var kv in DefenseDict)
            foreach (Planet p in kv.Key.PlanetList)
            {
                if (Us.isPlayer && p.colonyType != Planet.ColonyType.Military) continue;
                float devratio = (p.Level + 1) / (kv.Value.SystemDevelopmentlevel + 1);
                if (!kv.Key.CombatInSystem
                    && p.GetDefendingTroopCount() > kv.Value.IdealTroopCount * devratio)
                {
                    Troop l = p.TroopsHere.FirstOrDefault(loyalty => loyalty.GetOwner() == Us);
                    l?.Launch();
                }
            }
        }

        public void ManageForcePool()
        {
            CalculateSystemImportance();
            ManageShips();
            ManageTroops();
        }
    }
}