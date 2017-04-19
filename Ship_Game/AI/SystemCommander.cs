using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class SystemCommander
    {
        public SolarSystem System;
        public float ValueToUs;
        public int IdealTroopCount;
        public float TroopStrengthNeeded;
        public int IdealShipStrength;
        public bool IsEnoughShipStrength => GetOurStrength() >= IdealShipStrength;
        public bool IsEnoughTroopStrength => IdealTroopCount >= TroopCount;
        public float PercentageOfValue;
        public int CurrentShipStr;
        public float SystemDevelopmentlevel;
        public float RankImportance;
        public int TroopCount;
        public int TroopsWanted                  => IdealTroopCount - TroopCount;
        public Map<Guid, Ship> ShipsDict         = new Map<Guid, Ship>();
        public Map<Ship, Ship[]> EnemyClumpsDict = new Map<Ship, Ship[]>();
        public Ship[] GetShipList                => ShipsDict.Values.ToArray();
        private readonly Empire Us;
        public Map<Planet, PlanetTracker> PlanetTracker = new Map<Planet, PlanetTracker>();
        

        public SystemCommander(Empire e, SolarSystem system)
        {
            System = system;
            Us = e;
        }
        
        public float UpdateSystemValue()
        {            
            ValueToUs = 0f;
            IdealShipStrength = 0;
            PercentageOfValue = 0f;
            ValueToUs = System.combatTimer > 0 ? 5 : 0;
            foreach (Planet p in System.PlanetList)
            {
                if (!PlanetTracker.TryGetValue(p, out PlanetTracker planett))
                {
                    planett = new PlanetTracker(p, Us);
                    PlanetTracker.Add(p,planett);
                }
                ValueToUs += planett.UpdateValue();
                
                
            }
            foreach (SolarSystem fiveClosestSystem in System.FiveClosestSystems)
            {
                bool noEnemies = false;
                if (!fiveClosestSystem.ExploredDict[Us]) continue;
                foreach (Empire e in fiveClosestSystem.OwnerList)
                {
                    if (e == Us) continue;
                    bool attack = Us.IsEmpireAttackable(e,null);   
                    if (attack) ValueToUs += 5f;
                    else
                    ValueToUs += 1f;
                    noEnemies = noEnemies || !attack;
                }
                if (!noEnemies) continue;
                ValueToUs *= 2;
                ValueToUs /= 3;
            }
            return ValueToUs;
        }
        
        public Array<Ship> RemoveExtraShips()
        {
            var ships = new Array<Ship>();            
            if (CurrentShipStr < IdealShipStrength) return ships;             
            var ships2 = ShipsDict.Values.ToArray();
            for (int index = 0; index < ships2.Length; index++)
            {
                Ship ship = ships2[index];
                float str = ship.BaseStrength;
                if (CurrentShipStr - str > IdealShipStrength)
                {
                    RemoveShip(ship);
                    ships.Add(ship);
                }
            }
            return ships;
            
        }
        public bool RemoveShip(Ship shipToRemove)
        {                                   
            if (ShipsDict.Remove(shipToRemove.guid))
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
            if (CurrentShipStr  > IdealShipStrength ) return false;
            if (!ShipsDict.ContainsValue(ship))
            {
                ShipsDict.Add(ship.guid, ship);
                CurrentShipStr += (int)ship.BaseStrength;                                
            }
            if (ship.AI.SystemToDefend != System)
                ship.AI.OrderSystemDefense(System);            
            return true;
        }
        private void Clear()
        {
            if (ShipsDict == null) return;
            foreach (Ship ship in ShipsDict.Values)
                ship.AI.SystemToDefend = null;
            ShipsDict.Clear();
            CurrentShipStr = 0;
        }
        public Planet AssignIdleDuties(Ship ship)
        {
            var planets = PlanetTracker.Values;
            PlanetTracker best = null;
            foreach(PlanetTracker pl in planets)
            {
                if (best == null || best.Value < pl.Value)
                    best = pl;
            }
            return best.Planet;                        
        }
        public void AssignTargets()
        {
            EnemyClumpsDict = Us.GetGSAI().ThreatMatrix.PingRadarShipClustersByShip(System.Position, 150000, 15000, Us);
            
            if (EnemyClumpsDict.Count != 0)
            {
                int i = 0;
                var clumpsList = new Ship[EnemyClumpsDict.Count];
                foreach (var kv in EnemyClumpsDict)
                    clumpsList[i++] = kv.Key;

                Ship closest = clumpsList.FindMin(ship => System.Position.SqDist(ship.Center));
                i = 0;
                var assignedShips = new Ship[ShipsDict.Count];
                foreach (Ship enemy in EnemyClumpsDict[closest])
                {
                    float assignedStr = 0f;
                    foreach (var kv in ShipsDict)
                    {
                        if (!kv.Value.InCombat && kv.Value.System == System)
                        {
                            if ((assignedStr > 0f && assignedStr >= enemy.GetStrength())
                                || kv.Value.AI.State == AIState.Resupply
                                ||assignedShips.Contains(kv.Value))
                                continue;

                            kv.Value.AI.Intercepting = true;
                            kv.Value.AI.OrderAttackSpecificTarget(enemy);
                            assignedShips[i++]=kv.Value;
                            assignedStr += kv.Value.GetStrength();
                        }
                        else
                        {
                            if (assignedShips.Contains(kv.Value)) continue;
                            assignedStr += kv.Value.GetStrength();
                        }
                    }
                }
                foreach (var kv in ShipsDict)
                {
                    Ship ship = kv.Value;
                    if (!assignedShips.Contains(ship))
                        continue;
                    if (ship.AI.State == AIState.Resupply || ship.System != System)
                        continue;

                    ship.AI.Intercepting = true;
                    ship.AI.OrderAttackSpecificTarget(assignedShips[0].AI.Target as Ship);
                }
            }
            else
            {
                foreach (var kv in ShipsDict)
                {
                    if (kv.Value.AI.State == AIState.Resupply ) continue;
   
                    kv.Value.AI.OrderSystemDefense(System);
                }
            }
        }

        public float GetOurStrength()
        {
            float str = 0f;
            foreach (var kv in ShipsDict)
                str = str + kv.Value.GetStrength();
            return str;
        }

        public void CalculateTroopNeeds()
        {
            int mintroopLevel = (int)(Empire.Universe.GameDifficulty + 1) * 2;
            TroopCount = 0;
            {
                // find max number of troops for system.
                var planets = System.PlanetList.FilterBy(planet => planet.Owner == Us);
                int planetCount = planets.Length;
                int developmentlevel = planets.Sum(development => development.developmentLevel);
                SystemDevelopmentlevel = developmentlevel;
                int maxtroops = System.PlanetList.Where(planet => planet.Owner == Us).Sum(planet => planet.GetPotentialGroundTroops());
                IdealTroopCount = (mintroopLevel + (int)RankImportance) * planetCount;

                if (IdealTroopCount > maxtroops)
                    IdealTroopCount = maxtroops;
                int currentTroops = System.PlanetList.Where(planet => planet.Owner == Us).Sum(planet => planet.GetDefendingTroopCount());
                TroopCount += currentTroops;

                TroopStrengthNeeded = IdealTroopCount - currentTroops;
            }

        }
        public void CalculateShipneeds()
        {
            int predicted = (int)Us.GetGSAI().ThreatMatrix.PingRadarStrengthLargestCluster(System.Position, 300000, Us);
            int min = (int)(10f / RankImportance) * (Us.data.DiplomaticPersonality?.Territorialism ?? 50);
            IdealShipStrength = Math.Max(predicted, min);
            
        }
        public void UpdatePlanetTracker()
        {
            var planetsHere = System.PlanetList.Where(planet => planet.Owner == Us).ToArray();
            foreach(Planet planet in  planetsHere)
            {
                if (!PlanetTracker.TryGetValue(planet, out PlanetTracker currentValue))
                {
                    PlanetTracker newEntry = new PlanetTracker(planet,Us);
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

        private void Destroy()
        {
            Clear();            
            ShipsDict = null;
            EnemyClumpsDict?.Clear();
            EnemyClumpsDict = null;
            PlanetTracker?.Clear();
            PlanetTracker = null;
            System = null;          
        }
    }
    public class PlanetTracker
    {
        public float Value;
        public int TroopsWanted;
        public int TroopsHere;
        public Planet Planet;
        private readonly Empire Empire;
        public PlanetTracker(Planet toTrack, Empire empire)
        {
            Planet = toTrack;
            Empire = empire;

        }
        public float UpdateValue()
        {
            Empire us = Planet.Owner;
            Value = 0;
            bool enemy = Empire.IsEmpireAttackable(Planet.Owner, null);
            if (Planet.Owner == Empire || !enemy)
            {
                Value += Planet.Population / 10000f;
                Value += Planet.GovBuildings ? 1 : 0;
                Value += Planet.HasShipyard ? 5 : 0;
                Value += Planet.developmentLevel;
                if (Empire.data.Traits.Cybernetic > 0) Value += Planet.MineralRichness;
            }
            Value += (Planet.MaxPopulation / 10000f);
            Value += Planet.Fertility;
            Value += Planet.MineralRichness;
            Value += Planet.CommoditiesPresent.Count;

            if (us == null || !enemy)
                return Value;
            if (!us.TryGetRelations(us, out Relationship them) || them == null || !them.Known) return Value;            
            if (them.Trust < 50f) Value += 2.5f;
            if (them.Trust < 10f) Value += 2.5f;
            if (them.TotalAnger > 2.5f) Value += 2.5f;
            if (them.TotalAnger <= 30f) Value += 2.5f;

            return Value;

        }
    }

}