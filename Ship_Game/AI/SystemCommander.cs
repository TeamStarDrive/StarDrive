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
        public float ValueToUs;
        public int IdealTroopCount;
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
        public Map<Planet, PlanetTracker> PlanetTracker = new Map<Planet, PlanetTracker>();
        

        public SystemCommander(Empire e, SolarSystem system)
        {
            System = system;
            Us = e;
        }
        
        public float UpdateSystemValue()
        {            
            IdealShipStrength = 0;
            PercentageOfValue = 0f;
            ValueToUs = System.combatTimer > 0 ? 5 : 0;
            foreach (Planet p in System.PlanetList)
            {
                if (!PlanetTracker.TryGetValue(p, out PlanetTracker trackedPlanet))
                {
                    trackedPlanet = new PlanetTracker(p, Us);
                    PlanetTracker.Add(p, trackedPlanet);
                }
                ValueToUs += trackedPlanet.UpdateValue();
            }
            foreach (SolarSystem fiveClosestSystem in System.FiveClosestSystems)
            {
                bool noEnemies = false;
                if (!fiveClosestSystem.IsExploredBy(Us))
                    continue;
                foreach (Empire e in fiveClosestSystem.OwnerList)
                {
                    if (e == Us) continue;
                    bool attack = Us.IsEmpireAttackable(e);   
                    if (attack) ValueToUs += 5f;
                    else        ValueToUs += 1f;
                    noEnemies = noEnemies || !attack;
                }
                if (!noEnemies) continue;
                ValueToUs *= 2;
                ValueToUs /= 3;
            }
            return ValueToUs;
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
                if (ship.Active) ship.AI.SystemToDefend = null;
            OurShips.Clear();
            CurrentShipStr = 0;
        }

        public Planet AssignIdleDuties(Ship ship)
        {
            PlanetTracker best = PlanetTracker.FindMinValue(p => p.Value);
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

        public void CalculateTroopNeeds()
        {
            int minTroopLevel = (int)RankImportance + ((int)CurrentGame.Difficulty + 1) * 2;

            // find max number of troops for system.
            Planet[] ourPlanets = System.PlanetList.Filter(p => p.Owner == Us);
            SystemDevelopmentlevel = ourPlanets.Sum(p => p.Level);

            int maxTroops = ourPlanets.Sum(planet => planet.GetPotentialGroundTroops());
            IdealTroopCount = (minTroopLevel + (int)RankImportance) * ourPlanets.Length;
            if (IdealTroopCount > maxTroops)
                IdealTroopCount = maxTroops;

            TroopCount = 0;
            int currentTroops = ourPlanets.Sum(planet => planet.GetDefendingTroopCount());
            TroopCount += currentTroops;

            TroopStrengthNeeded = IdealTroopCount - currentTroops;
        }

        public void CalculateShipNeeds()
        {
            int predicted = (int)Us.GetEmpireAI().ThreatMatrix.PingRadarStrengthLargestCluster(System.Position, 30000, Us);            
            int min = (int)(10f / RankImportance) * (Us.data.DiplomaticPersonality?.Territorialism ?? 50);
            min /= 4;
            IdealShipStrength = Math.Max(predicted, min);
        }

        public void UpdatePlanetTracker()
        {
            Planet[] ourPlanets = System.PlanetList.Filter(planet => planet.Owner == Us);
            foreach(Planet planet in  ourPlanets)
            {
                if (!PlanetTracker.TryGetValue(planet, out PlanetTracker currentValue))
                {
                    var newEntry = new PlanetTracker(planet,Us);
                    PlanetTracker.Add(planet, newEntry);
                    continue;
                }
                if (currentValue.Planet.Owner != Us)
                {
                    PlanetTracker.Remove(currentValue.Planet);
                }
            }
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
            PlanetTracker.Clear();
            System = null;          
        }
    }

    public class PlanetTracker
    {
        public float Value;
        public int TroopsWanted;
        public int TroopsHere;
        public Planet Planet;
        readonly Empire Owner;
        public float Distance;

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
            {
                Value += Planet.PopulationBillion / 10f;
                Value += Planet.GovBuildings ? 1 : 0;
                Value += Planet.HasSpacePort ? 5 : 0;
                Value += Planet.Level;
                if (Owner.data.Traits.Cybernetic > 0) Value += Planet.MineralRichness;
            }
            Value += Planet.EmpireBaseValue(Owner) *.1f;

            if (planetOwner == null || !enemy)
                return Value;        
            
            var them = Owner.GetRelations(planetOwner);
            if (them == null || !them.Known) return Value;
            if (them.Trust < 50f) Value += 2.5f;
            if (them.Trust < 10f) Value += 2.5f;
            if (them.TotalAnger > 2.5f) Value += 2.5f;
            if (them.TotalAnger <= 30f) Value += 2.5f;

            return Value;
        }
    }

}