using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI
{
    // This is a helper commander of tracking
    // defense ships and assigning targets
    public sealed class SystemCommander
    {
        public SolarSystem System;
        public float TotalValueToUs;
        public float OurPlanetsTotalValue { get; private set; }
        public float OurPlanetsMaxValue { get; private set; }
        public float MaxValueToUs;
        public int IdealTroopCount = 1;
        public float TroopStrengthNeeded;
        public int IdealShipStrength;
        public bool IsEnoughShipStrength => GetOurStrength() >= IdealShipStrength;
        public bool IsEnoughTroopStrength => IdealTroopCount <= TroopCount;
        public float PercentageOfValue;
        public int CurrentShipStr;
        public float SystemDevelopmentlevel;
        public float RankImportance;
        public int TroopCount;
        public int TroopsWanted => IdealTroopCount - TroopCount;
        public Map<Guid, Ship> OurShips         = new Map<Guid, Ship>();
        public ICollection<Ship> GetShipList => OurShips.Values;
        readonly Empire Us;
        public Map<Planet, PlanetTracker> PlanetValues = new Map<Planet, PlanetTracker>();
        readonly int GameDifficultyModifier;
        float PredictionTimer = 0;
        int PredictedStrength = 0;

        float PlanetToSystemDevelopmentRatio(Planet p) => p.Level / SystemDevelopmentlevel;

        public SystemCommander(Empire e, SolarSystem system)
        {
            System = system;
            Us = e;
            GameDifficultyModifier = e.DifficultyModifiers.SysComModifier;
        }

        public float UpdateSystemValue()
        {
            IdealShipStrength = 0;
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

        // @return Ships that were removed or empty array
        public Array<Ship> RemoveExtraShips()
        {
            var removed = new Array<Ship>();
            if (CurrentShipStr < IdealShipStrength)
                return removed;

            foreach (Ship ship in OurShips.AtomicValuesArray())
            {
                float str = ship.BaseStrength;
                if (CurrentShipStr - str > IdealShipStrength)
                {
                    RemoveShip(ship);
                    removed.Add(ship);
                }
            }
            return removed;
        }

        public bool RemoveShip(Ship shipToRemove)
        {
            if (OurShips.Remove(shipToRemove.guid))
            {
                CurrentShipStr -= (int)shipToRemove.BaseStrength;
                shipToRemove.AI.SystemToDefend = null;
                shipToRemove.AI.SystemToDefendGuid = Guid.Empty;
                shipToRemove.AI.ClearOrders();
                return true;
            }
            return false;
        }

        public bool AddShip(Ship ship)
        {
            if (CurrentShipStr > IdealShipStrength) return false;
            if (OurShips.TryGetValue(ship.guid, out Ship existing))
            {
                if (existing != ship) // @todo Why is this check here? Wtf?
                {
                    CurrentShipStr -= (int)existing.BaseStrength;
                    CurrentShipStr += (int)ship.BaseStrength;
                    OurShips[ship.guid] = ship;
                }
            }
            else
            {
                OurShips.Add(ship.guid, ship);
                CurrentShipStr += (int)ship.BaseStrength;
            }

            if (ship.AI.SystemToDefend != System)
                ship.AI.OrderSystemDefense(System);
            return true;
        }

        private void Clear()
        {
            foreach (Ship ship in OurShips.Values)
                if (ship.Active && ship.AI != null) // AI is null if we exit during initialization
                    ship.AI.SystemToDefend = null;
            OurShips.Clear();
            CurrentShipStr = 0;
        }

        public Planet AssignIdleDuties(Ship ship)
        {
            PlanetTracker best = PlanetValues.FindMinValue(p => p.Value);
            return best.Planet;
        }

        public void AssignTargets()
        {
            Array<Ship> hostiles = Us.GetEmpireAI().ThreatMatrix.PingRadarClosestEnemyCluster(System.Position, System.Radius, 15000, Us);
            if (hostiles.NotEmpty)
            {
                var assignedShips = new HashSet<Ship>();
                foreach (Ship hostile in hostiles)
                {
                    float assignedStr = 0f;
                    foreach (Ship ship in OurShips.Values)
                    {
                        if (assignedShips.Contains(ship))
                            continue;

                        if (!ship.InCombat && ship.System == System)
                        {
                            if (assignedStr <= 0f || assignedStr < hostile.GetStrength()
                                && ship.AI.State != AIState.Resupply)
                            {
                                ship.AI.OrderAttackSpecificTarget(hostile);
                                assignedStr += ship.GetStrength();
                                assignedShips.Add(ship);
                            }
                        }
                        else
                        {
                            assignedStr += ship.GetStrength();
                        }
                    }
                }
                foreach (Ship ship in assignedShips)
                {
                    if (ship.AI.State != AIState.Resupply && ship.System == System)
                    {
                        ship.AI.OrderAttackSpecificTarget(assignedShips.First().AI.Target as Ship);
                    }
                }
            }
            else
            {
                foreach (Ship ship in OurShips.Values)
                {
                    if (ship.AI.State != AIState.Resupply)
                        ship.AI.OrderSystemDefense(System);
                }
            }
        }

        public float GetOurStrength()
        {
            float str = 0f;
            foreach (Ship ship in OurShips.Values)
                str += ship.GetStrength();
            return str;
        }

        public Planet[] OurPlanets => System.PlanetList.Filter(p => p.Owner == Us);

        int MinPlanetTroopLevel => (int)(RankImportance * GameDifficultyModifier);

        public float PlanetTroopMin(Planet planet)
        {
            float troopMin = MinPlanetTroopLevel * PlanetToSystemDevelopmentRatio(planet);

            return Math.Max(1, troopMin);
        }

        public float TroopStrengthMin(Planet planet)
        {
            float troopMin = MinPlanetTroopLevel * PlanetToSystemDevelopmentRatio(planet);

            return Math.Max(1, troopMin) * 10f;
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

        public void CalculateShipNeeds()
        {
            if (PredictionTimer <= 0)
            {
                PredictedStrength = (int)Us.GetEmpireAI().ThreatMatrix.PingRadarStrengthLargestCluster(System.Position, 30000, Us);
                PredictedStrength /= (int)(10f / RankImportance);
                PredictionTimer = 2f;
            }
            else PredictionTimer--;

            int min = (int)(10f / RankImportance) * (Us.data.DiplomaticPersonality?.Territorialism ?? 50);
            min /= 4;
            IdealShipStrength = min; // Math.Max(PredictedStrength, min);
        }

        public void UpdatePlanetTracker()
        {
            Planet[] ourPlanets = System.PlanetList.Filter(planet => planet.Owner == Us);
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
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~SystemCommander() { Destroy(); }

        void Destroy()
        {
            Clear();
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