using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public sealed class DefensiveCoordinator : IDisposable
    {
        readonly Empire Us;
        readonly Empire Player;
        public float DefenseDeficit;
        public Map<SolarSystem, SystemCommander> DefenseDict = new();
        int TotalValue;
        public float TroopsToTroopsWantedRatio;

        public int Id { get; }
        public string Name { get; }
        public Empire OwnerEmpire => Us;
        public DefensiveCoordinator(int id, Empire e, string name)
        {
            Id = id;
            Us = e;
            Player = e.Universe.Player;
            Name = name;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DefensiveCoordinator()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (DefenseDict != null)
                foreach (var kv in DefenseDict)
                    kv.Value?.Dispose();
            DefenseDict = null;
        }

        void ClearEmptyPlanetsOfTroops()
        {
            //@TODO move this to planet.
            // FB - This code is crappy. And it launches troops into space combat zones as well
            // and it doesnt only clear empty planets but also adds the planet to defense dict. very misleading
            // also why are we running this for the player at all, why do we need to add to defense dict for players?
            for (int i = 0; i < Us.Universe.Planets.Count; i++)
            {
                Planet p = Us.Universe.Planets[i];
                if (p.Habitable
                    && Us != Player 
                    && p.Owner != Us 
                    && !p.EventsOnTiles() 
                    && !p.RecentCombat
                    && !p.TroopsHereAreEnemies(Us))
                {
                    p.ForceLaunchAllTroops(Us);
                }
                else if (p.Owner == Us && p.System != null && !DefenseDict.ContainsKey(p.System)) // This should stay here.
                {
                    DefenseDict.Add(p.System, new SystemCommander(this, p.System, Us));
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
                .OrderBy(com => com.PercentageOfValue).ToArr();
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

        public SolarSystem GetNearestSystemNeedingTroops(Vector2 fromPos, Empire empire)
        {
            float width = empire.Universe.Size;
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
                if (troopShip.DesignRole == RoleName.troop 
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
                        foreach (Troop l in p.Troops.GetLaunchableTroops(Us, 1))
                            l.Launch();
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
                                goal.TargetPlanet != null && kv.Key == goal.TargetPlanet.System))
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

                SolarSystem solarSystem = GetNearestSystemNeedingTroops(troopShip.Position, troopShip.Loyalty);

                if (solarSystem == null)
                    break;

                SystemCommander defenseSystem = DefenseDict[solarSystem];

                defenseSystem.TroopStrengthNeeded--;
                defenseSystem.TroopCount++;
                troopShips.RemoveAtSwapLast(i);

                Planet target = defenseSystem.OurPlanets
                    .FindMinFiltered(p => !p.MightBeAWarZone(p.Owner) && p.GetFreeTiles(p.Owner) > 0,
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
            ManageTroops();
        }

        public SystemCommander GetSystemCommander(SolarSystem system)
        {
            DefenseDict.TryGetValue(system, out SystemCommander systemCommander);
            return systemCommander;
        }
    }
}