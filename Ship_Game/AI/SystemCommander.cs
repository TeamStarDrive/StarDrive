using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;

namespace Ship_Game.AI
{
    // Per-system tracker of troop counts and planet values.
    // Once held an inert ship-defense roster; that responsibility migrated to
    // Fleets + MilitaryTask. What remains is the troop/value bookkeeping read
    // by DefensiveCoordinator.ManageForcePool, MilitaryTask_Requistions and
    // RunDiplomaticPlanner.
    public sealed class SystemCommander
    {
        public readonly DefensiveCoordinator Owner;
        public SolarSystem System;
        readonly Empire Us;

        public float TotalValueToUs;
        public float OurPlanetsTotalValue { get; private set; }
        public float OurPlanetsMaxValue { get; private set; }
        public int IdealTroopCount = 1;
        public float TroopStrengthNeeded;
        public bool IsEnoughTroopStrength => IdealTroopCount <= TroopCount;
        public float PercentageOfValue;
        public float SystemDevelopmentlevel;
        public float RankImportance;
        public int TroopCount;
        public int TroopsWanted => IdealTroopCount - TroopCount;

        public Map<Planet, PlanetTracker> PlanetValues = new Map<Planet, PlanetTracker>();
        Planet[] CachedOurPlanets = Empty<Planet>.Array;
        readonly int GameDifficultyModifier;

        float PlanetToSystemDevelopmentRatio(Planet p) => p.Level / SystemDevelopmentlevel;

        public SystemCommander(DefensiveCoordinator owner, SolarSystem system, Empire e)
        {
            Owner = owner;
            System = system;
            Us = e;
            GameDifficultyModifier = e.DifficultyModifiers.SysComModifier;
        }

        public float UpdateSystemValue()
        {
            PercentageOfValue = 0f;
            OurPlanetsTotalValue = 0;
            OurPlanetsMaxValue = 0;
            TotalValueToUs = System.DangerousForcesPresent(Us) ? 5 : 0;
            foreach (Planet p in System.PlanetList)
            {
                if (!PlanetValues.TryGetValue(p, out PlanetTracker trackedPlanet))
                {
                    trackedPlanet = new PlanetTracker(p, Us);
                    PlanetValues.Add(p, trackedPlanet);
                }
                TotalValueToUs += trackedPlanet.UpdateValue();

                if (p.Owner == Us)
                {
                    OurPlanetsTotalValue += trackedPlanet.Value;
                    OurPlanetsMaxValue = Math.Max(OurPlanetsMaxValue, trackedPlanet.Value);
                }

                if (p.Owner != Us && Us.IsEmpireAttackable(p.Owner))
                    TotalValueToUs += 100;
            }
            CreatePlanetRatio();
            CheckNearbySystemsForEnemies();
            return TotalValueToUs;
        }

        private void CreatePlanetRatio()
        {
            foreach (var kv in PlanetValues)
            {
                kv.Value.CalculateRankInSystem(OurPlanetsMaxValue);
                kv.Value.CalculateRatioInSystem(OurPlanetsTotalValue);
            }
        }

        private void CheckNearbySystemsForEnemies()
        {
            foreach (SolarSystem system in System.FiveClosestSystems)
                if (system.IsExploredBy(Us))
                    foreach (Empire e in system.OwnerList)
                        if (e != Us)
                            TotalValueToUs += Us.IsEmpireAttackable(e) ? 5 : 0;
        }

        public Planet[] OurPlanets => CachedOurPlanets;

        int MinPlanetTroopLevel => (int)(RankImportance * GameDifficultyModifier);

        public float PlanetTroopMin(Planet planet)
        {
            float troopMultiplier = !Us.IsAtWarWithMajorEmpire && Us.ActiveWarPreparations == 0 ? 0.5f : 1;
            float troopMin        = MinPlanetTroopLevel * PlanetToSystemDevelopmentRatio(planet) * troopMultiplier;
            return troopMin.LowerBound(1);
        }

        public void CalculateTroopNeeds()
        {
            // find max number of troops for system.
            Planet[] ourPlanets = OurPlanets;
            SystemDevelopmentlevel = ourPlanets.Sum(p => p.Level);

            int idealTroopCount = (int)ourPlanets.Sum(PlanetTroopMin).Clamped(1, int.MaxValue);

            TroopCount          = 0;
            int currentTroops   = ourPlanets.Sum(planet => planet.GetDefendingTroopCount());
            TroopCount         += currentTroops;
            IdealTroopCount     = idealTroopCount;
            TroopStrengthNeeded = idealTroopCount - currentTroops;
        }

        public void UpdatePlanetTracker()
        {
            Planet[] ourPlanets = System.PlanetList.Filter(planet => planet.Owner == Us);
            CachedOurPlanets = ourPlanets;
            foreach(Planet planet in  ourPlanets)
            {
                if (!PlanetValues.TryGetValue(planet, out PlanetTracker currentValue))
                {
                    var newEntry = new PlanetTracker(planet,Us);
                    PlanetValues.Add(planet, newEntry);
                    continue;
                }
                if (currentValue.Planet.Owner != Us)
                {
                    PlanetValues.Remove(currentValue.Planet);
                }
            }
        }

        public PlanetTracker GetPlanetValues(Planet planet)
        {
            PlanetValues.TryGetValue(planet, out PlanetTracker planetTracker);
            return planetTracker;
        }

        // Walks troopShips, drops dead/invalid entries, and credits any in-flight
        // rebases targeting this system against TroopCount / TroopStrengthNeeded.
        // Survivors stay in the list for the coordinator to reassign.
        public void CreditIncomingRebases(Array<Ship> troopShips)
        {
            int currentTroops = TroopCount;
            for (int i = troopShips.Count - 1; i >= 0; i--)
            {
                Ship troopShip = troopShips[i];
                if (troopShip == null || !troopShip.HasOurTroops)
                {
                    troopShips.RemoveAtSwapLast(i);
                    continue;
                }

                ShipAI troopAI = troopShip.AI;
                if (troopAI == null)
                {
                    troopShips.RemoveAtSwapLast(i);
                    continue;
                }

                if ((troopAI.State == AIState.Rebase || troopAI.State == AIState.RebaseToShip)
                    && troopAI.OrderQueue.NotEmpty
                    && troopAI.OrderQueue.Any(g => g.TargetPlanet != null && System == g.TargetPlanet.System))
                {
                    currentTroops++;
                    TroopStrengthNeeded--;
                    troopShips.RemoveAtSwapLast(i);
                }
            }
            TroopCount = currentTroops;
        }

        // Picks the most under-garrisoned non-war-zone owned planet in this system
        // and routes troopShip there, updating local counters. Returns false (and
        // touches nothing) if no viable landing planet exists.
        public bool AbsorbIdleTroop(Ship troopShip)
        {
            // Match OrderRebase's capacity check so a planet whose free tiles are
            // already reserved by other in-flight rebases isn't picked here, then
            // silently rejected downstream with bookkeeping mutations stranded.
            Planet target = OurPlanets.FindMinFiltered(
                p => !p.MightBeAWarZone(p.Owner) && p.FreeTilesWithRebaseOnTheWay(p.Owner) > 0,
                p => p.CountEmpireTroops(p.Owner) / PlanetTroopMin(p));

            if (target == null)
                return false;

            TroopStrengthNeeded--;
            TroopCount++;
            troopShip.AI.OrderRebase(target, true);
            return true;
        }

        // Per-system half of LaunchExcessTroops: any owned planet whose defending
        // garrison exceeds the min gets one launchable troop kicked into space so
        // the coordinator's next pass can rebase it where it's needed.
        public void LaunchExcessTroops()
        {
            if (System.HostileForcesPresent(Us))
                return;

            Planet[] ourPlanets = OurPlanets;
            for (int i = 0; i < ourPlanets.Length; i++)
            {
                Planet p = ourPlanets[i];
                if (p.GetDefendingTroopCount() > PlanetTroopMin(p))
                {
                    foreach (Troop l in p.Troops.GetLaunchableTroops(Us, 1))
                        l.Launch();
                }
            }
        }

        public void Dispose()
        {
            PlanetValues.Clear();
            System = null;
        }
    }

    public class PlanetTracker
    {
        public float Value;
        public int TroopsHere;
        public readonly Planet Planet;
        readonly Empire Owner;
        public float Distance;
        public float RankInSystem { get; private set; }
        public float RatioInSystem { get; private set; }

        public PlanetTracker(Planet toTrack, Empire empire)
        {
            Planet = toTrack;
            Owner = empire;
        }

        public float UpdateValue()
        {
            Empire planetOwner = Planet.Owner;
            Value = 0;
            bool enemy = Owner.IsEmpireAttackable(Planet.Owner);

            if (Planet.Owner == Owner || !enemy)
                Value = Planet.ColonyBaseValue(Owner);

            if (planetOwner == null || !enemy)
                return Value;

            Relationship rel = Owner.GetRelationsOrNull(planetOwner);
            if (rel == null || !rel.Known) return Value;
            if (rel.Trust < 50f) Value += 10f;
            if (rel.Trust < 10f) Value += 10f;
            if (rel.TotalAnger > 2.5f) Value += 10f;
            if (rel.TotalAnger <= 30f) Value += 10f;

            return Value;
        }

        public void CalculateRankInSystem(float maxValue) => RankInSystem = CalculateRatio(maxValue);
        public void CalculateRatioInSystem(float totalValue) => RatioInSystem = CalculateRatio(totalValue);
        private float CalculateRatio(float value)
        {
            return Value / value.LowerBound(1);
        }

    }

}
