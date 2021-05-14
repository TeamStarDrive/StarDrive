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
        public float TroopsToTroopsWantedRatio;

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
            ship.AI.SystemToDefend     = null;
            ship.AI.SystemToDefendGuid = Guid.Empty;
            ship.AI.SetPriorityOrder(false);
            DefenseDeficit            -= ship.GetStrength();
            DefensiveForcePool.AddUnique(ship);
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
            if (DefenseDict.Count == 0)
            {
                Log.Error($"Ship {ship.AI.SystemToDefend} not in defensive dictionary");
                return null;
            }
            if (DefenseDict.TryGetValue(ship.AI.SystemToDefend, out SystemCommander systemCommander))
                return systemCommander.AssignIdleDuties(ship);
            return DefenseDict.First().Value.AssignIdleDuties(ship);
        }

        public void Remove(Ship ship, bool addToEmpirePool = true)
        {
            SolarSystem sysToDefend = ship.AI.SystemToDefend;
            if (DefensiveForcePool.RemoveSwapLast(ship))
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

            if (found)
            {
                ship.AI.SystemToDefend = null;
                ship.AI.SystemToDefendGuid = Guid.Empty;
                ship.AI.ClearOrders();
                if (addToEmpirePool && !ship.loyalty.isPlayer && ship.Active && ship.AI.State != AIState.Scrap && ship.loyalty == Us)
                    Us.AddShipToManagedPools(ship);
            }

            DebugInfoScreen.DefenseCoLogsNull(found, ship, sysToDefend);
        }

        public bool Contains(Ship ship)
        {
            return DefensiveForcePool.ContainsRef(ship);
        }

        public void RemoveShipList(Array<Ship> ships)
        {
            foreach (Ship ship in ships)
                Remove(ship);
        }

        void ClearEmptyPlanetsOfTroops()
        {
            foreach (Planet p in Empire.Universe.PlanetsDict.Values)
                //@TODO move this to planet.
                // FB - This code is crappy. And it launches troops into space combat zones as well
                // and it doesnt only clear empty planets but also adds the planet to defense dict. very misleading
            {
                if (Us != EmpireManager.Player 
                    && p.Owner != Us 
                    && !p.EventsOnTiles() 
                    && !p.RecentCombat
                    && !p.TroopsHereAreEnemies(Us))
                {
                    Troop[] troopsToLaunch = p.TroopsHere.Filter(t => t != null && t.Loyalty == Us);
                    p.LaunchTroops(troopsToLaunch);
                }
                else if (p.Owner == Us) //This should stay here.
                {
                    if (p.ParentSystem == null || DefenseDict.ContainsKey(p.ParentSystem))
                        continue;

                    DefenseDict.Add(p.ParentSystem, new SystemCommander(Us, p.ParentSystem));
                }
            }
        }

        void CalculateSystemImportance()
        {
            TotalValue = 0;

            KeyValuePair<SolarSystem, SystemCommander>[] kvs = DefenseDict.ToArray();
            for (int i = 0; i < kvs.Length; i++)
            {
                var kv = kvs[i];
                if (kv.Key.OwnerList.Contains(Us))
                {
                    kv.Value.UpdatePlanetTracker();
                    continue;
                }

                kv.Value.Dispose();
                DefenseDict.Remove(kv.Key);
            }

            foreach (var kv in DefenseDict)
                TotalValue += (int)kv.Value.UpdateSystemValue();

            foreach (var kv in DefenseDict)
                kv.Value.PercentageOfValue = kv.Value.TotalValueToUs / TotalValue.LowerBound(1);

            int ranker = 0;
            int split = DefenseDict.Count / 10;
            int splitStore = split;
            SystemCommander[] commanders = DefenseDict.Select(kv => kv.Value)
                                       .OrderBy(com => com.PercentageOfValue).ToArray();
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
                if (ship.Active)
                {
                    if (ship.AI.SystemToDefend == null)
                        shipsAvailableForAssignment.Add(ship);
                    else
                        ship.AI.State = AIState.SystemDefender;
                }
                else
                if (!ship.AI.HasPriorityOrder && ship.AI.State != AIState.Resupply)
                    Remove(ship);
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

        public SolarSystem GetNearestSystemNeedingTroops(Vector2 fromPos, Empire empire)
        {
            float width = Empire.Universe.UniverseSize;
            return DefenseDict.FindMaxKeyByValuesFiltered(
                com => com.TroopStrengthNeeded > 0
                       && com.System.PlanetList.Count > 0
                       && com.System.PlanetList.Sum(p => p.GetFreeTiles(empire)) > 0,
                com => (1f - ((float)com.TroopCount / com.IdealTroopCount ))
                       * com.TotalValueToUs * ((width - com.System.Position.SqDist(fromPos)) / width)
            );
        }

        void ManageTroops()
        {
            if (Us.isPlayer) 
                return;

            TroopsInSystems troops    = new TroopsInSystems(Us, DefenseDict);
            int rebasedTroops         = RebaseIdleTroops(troops.TroopShips);
            TroopsToTroopsWantedRatio = (troops.TotalCurrentTroops + rebasedTroops) / (float)troops.TotalTroopWanted;

            if (TroopsToTroopsWantedRatio > 1.25f)
                ScrapExcessTroop(troops.TroopShips);
            else
                LaunchExcessTroops();
        }

        void ScrapExcessTroop(Array<Ship> troopShips)
        {
            foreach (Ship troopShip in troopShips)
            {
                if (troopShip.DesignRole == ShipData.RoleName.troop 
                    && troopShip.AI.State == AIState.AwaitingOrders
                    && troopShip.GetOurFirstTroop(out Troop troop)
                    && troop.Level == Us.data.MinimumTroopLevel) // only scrap rookies
                {
                    troopShip.AI.OrderScrapShip();
                    return;
                }
            }
        }

        void LaunchExcessTroops()
        {
            foreach (var kv in DefenseDict)
            {
                if (kv.Key.HostileForcesPresent(Us))
                    continue;

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

        private struct TroopsInSystems
        {
            public readonly int TotalTroopWanted;
            public readonly int TotalCurrentTroops;
            public readonly Array<Ship> TroopShips;

            public TroopsInSystems(Empire empire, Map<SolarSystem, SystemCommander> DefenseDict)
            {
                TotalCurrentTroops         = 0;
                TotalTroopWanted           = 0;
                TroopShips = empire.GetAvailableTroopShips(out int troopsInFleets);
                foreach (var kv in DefenseDict)
                {
                    int currentTroops = kv.Value.TroopCount;
                    for (int i = TroopShips.Count - 1; i >= 0; i--)
                    {
                        Ship troopShip = TroopShips[i];

                        if (troopShip == null || !troopShip.HasOurTroops)
                        {
                            TroopShips.RemoveAtSwapLast(i);
                            continue;
                        }

                        ShipAI troopAI = troopShip.AI;
                        if (troopAI == null)
                        {
                            TroopShips.RemoveAtSwapLast(i);
                            continue;
                        }

                        if ((troopAI.State == AIState.Rebase || troopAI.State == AIState.RebaseToShip)
                            && troopAI.OrderQueue.NotEmpty
                            && troopAI.OrderQueue.Any(goal =>
                                goal.TargetPlanet != null && kv.Key == goal.TargetPlanet.ParentSystem))
                        {
                            currentTroops++;
                            kv.Value.TroopStrengthNeeded--;
                            TroopShips.RemoveAtSwapLast(i);
                        }
                    }

                    kv.Value.TroopCount = currentTroops;
                    TotalCurrentTroops += currentTroops+ troopsInFleets;
                    TotalTroopWanted += kv.Value.IdealTroopCount;
                }
            }
        }

        private int RebaseIdleTroops(Array<Ship> troopShips)
        {
            int totalRebasedTroops = 0;
            for (int i = troopShips.Count - 1; i >= 0; i--)
            {
                Ship troopShip = troopShips[i];

                SolarSystem solarSystem = GetNearestSystemNeedingTroops(troopShip.Center, troopShip.loyalty);

                if (solarSystem == null)
                    break;

                SystemCommander defenseSystem = DefenseDict[solarSystem];

                defenseSystem.TroopStrengthNeeded--;
                defenseSystem.TroopCount++;
                troopShips.RemoveAtSwapLast(i);

                Planet target = defenseSystem.OurPlanets
                    .FindMinFiltered(p => !p.MightBeAWarZone(Us) && p.GetFreeTiles(Us) > 0,
                        planet => planet.CountEmpireTroops(planet.Owner) / defenseSystem.PlanetTroopMin(planet));

                if (target != null)
                {
                    troopShip.AI.OrderRebase(target, true);
                    totalRebasedTroops++;
                }
            }
            return totalRebasedTroops;
        }

        public void ManageForcePool()
        {
            ClearEmptyPlanetsOfTroops();
            CalculateSystemImportance();
            ManageShips();
            ManageTroops();
        }

        public SystemCommander GetSystemCommander(SolarSystem system)
        {
            DefenseDict.TryGetValue(system, out SystemCommander systemCommander);
            return systemCommander;
        }
    }
}