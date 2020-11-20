using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI
{
    public class OffensiveForcePoolManager
    {
        readonly Empire Owner;
        EmpireAI EmpireAI => Owner.GetEmpireAI();
        ThreatMatrix ThreatMatrix => EmpireAI.ThreatMatrix;
        DefensiveCoordinator DefensiveCoordinator => EmpireAI.DefensiveCoordinator;
        Array<AO> AreasOfOperations => EmpireAI.AreasOfOperations;
        float ThreatTimer = 0;

        public OffensiveForcePoolManager (Empire owner)
        {
            Owner = owner;
        }

        public void ManageAOs()
        {
            
            if (ThreatTimer < 0) ThreatTimer = 2f;

            for (int index = AreasOfOperations.Count - 1; index >= 0; index--)
            {
                AO areasOfOperation = AreasOfOperations[index];
                Planet aoCoreWorld = areasOfOperation.CoreWorld;
                if (aoCoreWorld?.Owner != Owner)
                {
                    AreasOfOperations.RemoveAt(index);
                    areasOfOperation.ClearOut();
                    areasOfOperation.Dispose();
                }
            }
            
            Planet[] aoPlanets = GetAOPlanets(out HashSet<SolarSystem> aoSystems);
            if (aoPlanets.Length == Owner.GetPlanets().Count)
                return;

            var ownedPlanets = Owner.GetPlanets().ToArray();
            Planet[] planets = ownedPlanets.UniqueExclude(aoPlanets);
            if (planets.Length == 0)
                return;

            IOrderedEnumerable<Planet> maxProductionPotential =
                from planet in planets
                orderby planet.Prod.NetMaxPotential descending
                select planet;

            foreach (Planet coreWorld in maxProductionPotential)
            {
                if (coreWorld == null || coreWorld.Prod.NetMaxPotential <= 5f || !coreWorld.HasSpacePort) continue;
                float aoSize = 0;
                foreach (SolarSystem system in coreWorld.ParentSystem.FiveClosestSystems)
                {
                    if (aoSystems.Contains(system)) continue;                                       
                    if (aoSize < Vector2.Distance(coreWorld.Center, system.Position))
                        aoSize = Vector2.Distance(coreWorld.Center, system.Position);
                }          
                bool flag1 = true;
                foreach (AO areasOfOperation2 in AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Center, coreWorld.Center) >= aoSize)
                        continue;
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }

                AO aO2 = new AO(coreWorld, aoSize);
                AreasOfOperations.Add(aO2);
            }
        }
        public Planet[] GetAOPlanets(out HashSet<SolarSystem> systems)
        {
            systems = new HashSet<SolarSystem>();
            int planetCount = 0;
            foreach (AO ao in AreasOfOperations)
                planetCount += ao.GetOurPlanets().Length;            
            Planet[] allPlanets = new Planet[planetCount];
            int x = 0;
            foreach (AO ao in AreasOfOperations)
                foreach (Planet planet in ao.GetOurPlanets())
                {
                    systems.Add(planet.ParentSystem);
                    allPlanets[x++] = planet;
                }
            return allPlanets;
        }

        Planet[] GetAOCoreWorlds() => AreasOfOperations.Select(ao => ao.CoreWorld);

        public AO GetAOContaining(Planet planetToCheck)
        {
            return AreasOfOperations.Find(ao=> ao.CoreWorld == planetToCheck || ao.CoreWorld.ParentSystem == planetToCheck.ParentSystem || ao.GetOurPlanets().Contains(planetToCheck));
        }

        public AO GetAOContaining(Vector2 point)
        {
            return AreasOfOperations.Find(ao => point.InRadius(ao.Center, ao.Radius));
        }

        public bool IsPlanetCoreWorld(Planet planetToCheck)
        {
            if (planetToCheck.Owner != Owner) return false;
            return AreasOfOperations.Any(ao=> ao.CoreWorld == planetToCheck);
        }

        public AO CreateAO(Planet coreWorld, float radius)
        {
            var newAO = new AO(coreWorld, radius);
            AreasOfOperations.Add(newAO);
            return newAO;
        }

    }
}
