using Microsoft.Xna.Framework;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed class DefensiveCoordinator : IDisposable
    {
        readonly Empire Us;
        public float DefenseDeficit;
        public Map<SolarSystem, SystemCommander> DefenseDict = new Map<SolarSystem, SystemCommander>();
        public Array<Ship> DefensiveForcePool = new Array<Ship>();
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
            ship.AI.ClearOrders(AIState.SystemDefender);
            ship.AI.SystemToDefend = null;
            ship.AI.SystemToDefendGuid = Guid.Empty;
            ship.AI.HasPriorityOrder = false;
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

        public Planet AssignIdleShips(Ship ship)
        {
            return DefenseDict.TryGetValue(ship.AI.SystemToDefend, out SystemCommander systemCommander)
                ? systemCommander.AssignIdleDuties(ship)
                : DefenseDict.First().Value.AssignIdleDuties(ship);
        }

        public void Remove(Ship ship)
        {
            SolarSystem sysToDefend = ship.AI.SystemToDefend;
            if (DefensiveForcePool.Remove(ship))
            {
                if (ship.Active && sysToDefend == null)
                    DebugInfoScreen.DefenseCoLogsSystemNull();
            }
            else
            {
                if (ship.Active && sysToDefend != null)
                    DebugInfoScreen.DefenseCoLogsNotInPool();
            }

            bool found = false;

            // check for specific system commander
            if (sysToDefend != null && DefenseDict.TryGetValue(sysToDefend, out SystemCommander sysCom))
            {
                if (!sysCom.RemoveShip(ship))
                {
                    if (ship.Active)
                        DebugInfoScreen.DefenseCoLogsNotInSystem();
                }
                else found = true;
            }

            // double check for ship in any other sys commanders
            foreach (SystemCommander com in DefenseDict.Values)
            {
                if (com.RemoveShip(ship))
                {
                    found = true;
                    DebugInfoScreen.DefenseCoLogsMultipleSystems(ship);
                    break;
                }
            }

            DebugInfoScreen.DefenseCoLogsNull(found, ship, sysToDefend);
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
                    foreach (Troop troop in p.TroopsHere.Where(loyalty => loyalty.Loyalty == Us))
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
            SystemCommander[] commanders = DefenseDict.Select(kv=>kv.Value)
                                       .OrderBy(com => com.PercentageOfValue)
                                       .ThenBy(com => com.SystemDevelopmentlevel).ToArray();
            foreach (SystemCommander com in commanders)
            {
                split--;
                if (split <= 0)
                {
                    ranker++;
                    split = splitStore;
                    if (ranker > 10)
                        ranker = 10;
                }
                com.RankImportance = ranker;
            }
            foreach (SystemCommander com in commanders)
            {
                com.RankImportance = (int) (10 * (com.RankImportance / ranker));
                TotalValue += (int) com.ValueToUs;
                com.CalculateShipNeeds();
                com.CalculateTroopNeeds();
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
            return DefenseDict.FindMaxKeyByValuesFiltered(
                com => com.TroopStrengthNeeded > 0
                       && com.System.PlanetList.Count > 0
                       && com.System.PlanetList.Sum(p => p.GetGroundLandingSpots()) > 0,
                com => (1f - ((float)com.TroopCount / com.IdealTroopCount ))
                       * com.ValueToUs * ((width - com.System.Position.SqDist(fromPos)) / width)
            );
        }

        void ManageTroops()
        {
            TroopsInSystems troops = new TroopsInSystems(Us, DefenseDict);
            int rebasedTroops      = 0;
            if (!Us.isPlayer)
                rebasedTroops      = RebaseIdleTroops(troops.TroopShips);
            UniverseWants          = (troops.TotalCurrentTroops + rebasedTroops) / (float) troops.TotalTroopWanted;

            if (Us.isPlayer) return;

            if (UniverseWants > 1.25f)
            {
                foreach (var troop in troops.TroopShips)
                {
                    if (troop.DesignRole != ShipData.RoleName.troop) continue;
                    if (troop.AI.State == AIState.AwaitingOrders)
                        troop.AI.OrderScrapShip();
                }
            }
            else
            {
                foreach (var kv in DefenseDict)
                {
                    if (kv.Key.HostileForcesPresent(Us)) continue;
                    var sysCom = kv.Value;
                    foreach (Planet p in kv.Value.OurPlanets)
                    {
                        if (p.GetDefendingTroopCount() > sysCom.PlanetTroopMin(p))
                        {
                            Troop l = p.TroopsHere.Find(loyalty => loyalty.Loyalty == Us);
                            l?.Launch();
                        }
                    }
                }
            }
        }

        private struct TroopsInSystems
        {
            public readonly int TotalTroopWanted;
            public readonly int TotalCurrentTroops;
            public readonly Array<Ship> TroopShips;

            public TroopsInSystems(Empire empire, Map<SolarSystem, SystemCommander> DefenseDict)
            {
                TotalCurrentTroops = 0;
                TotalTroopWanted = 0;
                TroopShips = empire.GetAvailableTroopShips();
                foreach (var kv in DefenseDict)
                {
                    int currentTroops = kv.Value.TroopCount;
                    for (int i = TroopShips.Count - 1; i >= 0; i--)
                    {
                        Ship troop = TroopShips[i];

                        if (troop == null || troop.TroopList.Count <= 0)
                        {
                            TroopShips.RemoveAtSwapLast(i);
                            continue;
                        }

                        ShipAI troopAI = troop.AI;
                        if (troopAI == null)
                        {
                            TroopShips.RemoveAtSwapLast(i);
                            continue;
                        }

                        if (troopAI.State == AIState.Rebase &&
                            troopAI.OrderQueue.NotEmpty
                            && troopAI.OrderQueue.Any(goal =>
                                goal.TargetPlanet != null && kv.Key == goal.TargetPlanet.ParentSystem))
                        {
                            currentTroops++;
                            kv.Value.TroopStrengthNeeded--;
                            TroopShips.RemoveAtSwapLast(i);
                        }
                    }

                    kv.Value.TroopCount = currentTroops;
                    TotalCurrentTroops += currentTroops;
                    TotalTroopWanted += kv.Value.IdealTroopCount;
                }
            }
        }

        private int RebaseIdleTroops(Array<Ship> troopShips)
        {
            int totalRebasedTroops = 0;
            for (int x = troopShips.Count - 1; x >= 0; x--)
            {
                Ship troopShip = troopShips[x];

                SolarSystem solarSystem = GetNearestSystemNeedingTroops(troopShip.Center);

                if (solarSystem == null)
                    break;

                SystemCommander defenseSystem = DefenseDict[solarSystem];

                defenseSystem.TroopStrengthNeeded--;
                defenseSystem.TroopCount++;
                troopShips.RemoveAtSwapLast(x);

                Planet target = defenseSystem.OurPlanets
                    .FindMinFiltered(p => p.GetGroundLandingSpots() > 0,
                        planet => planet.CountEmpireTroops(planet.Owner) / defenseSystem.PlanetTroopMin(planet));

                if (target == null) continue;
                troopShip.AI.OrderRebase(target, true);
                totalRebasedTroops++;
            }
            return totalRebasedTroops;
        }

        public void ManageForcePool()
        {
            CalculateSystemImportance();
            ManageShips();
            ManageTroops();
        }
    }
}