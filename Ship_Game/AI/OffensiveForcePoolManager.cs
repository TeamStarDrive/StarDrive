using System.Collections.Generic;
using System.Linq;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public class OffensiveForcePoolManager
    {
        readonly Empire Owner;
        EmpireAI EmpireAI => Owner.AI;
        float ThreatTimer;

        public OffensiveForcePoolManager(Empire owner)
        {
            Owner = owner;
        }

        public void ManageAOs()
        {
            if (ThreatTimer < 0) ThreatTimer = 2f;

            for (int index = EmpireAI.AreasOfOperations.Count - 1; index >= 0; index--)
            {
                AO areasOfOperation = EmpireAI.AreasOfOperations[index];
                Planet aoCoreWorld = areasOfOperation.CoreWorld;
                if (aoCoreWorld?.Owner != Owner)
                {
                    EmpireAI.AreasOfOperations.RemoveAt(index);
                    areasOfOperation.Clear();
                }
            }
            
            Planet[] aoPlanets = GetAOPlanets(out HashSet<SolarSystem> aoSystems);
            if (aoPlanets.Length == Owner.GetPlanets().Count)
                return;

            var ownedPlanets = Owner.GetPlanets().ToArr();
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
                    if (aoSize < coreWorld.Position.Distance(system.Position))
                        aoSize = coreWorld.Position.Distance(system.Position);
                }          
                bool flag1 = true;
                foreach (AO areasOfOperation2 in EmpireAI.AreasOfOperations)
                {

                    if (areasOfOperation2.GetPlanet().Position.Distance(coreWorld.Position) >= aoSize)
                        continue;
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }

                CreateAO(coreWorld, aoSize);
            }
        }
        public Planet[] GetAOPlanets(out HashSet<SolarSystem> systems)
        {
            systems = new HashSet<SolarSystem>();
            int planetCount = 0;
            foreach (AO ao in EmpireAI.AreasOfOperations)
                planetCount += ao.GetOurPlanets().Length;

            Planet[] allPlanets = new Planet[planetCount];
            int x = 0;
            foreach (AO ao in EmpireAI.AreasOfOperations)
                foreach (Planet planet in ao.GetOurPlanets())
                {
                    systems.Add(planet.ParentSystem);
                    allPlanets[x++] = planet;
                }
            return allPlanets;
        }

        Planet[] GetAOCoreWorlds() => EmpireAI.AreasOfOperations.Select(ao => ao.CoreWorld);

        public AO GetAOContaining(Planet planetToCheck)
        {
            return EmpireAI.AreasOfOperations.Find(ao=> ao.CoreWorld == planetToCheck || ao.CoreWorld.ParentSystem == planetToCheck.ParentSystem || ao.GetOurPlanets().Contains(planetToCheck));
        }

        public AO GetAOContaining(Vector2 point)
        {
            return EmpireAI.AreasOfOperations.Find(ao => point.InRadius(ao.Center, ao.Radius));
        }

        public bool IsPlanetCoreWorld(Planet planetToCheck)
        {
            if (planetToCheck.Owner != Owner) return false;
            return EmpireAI.AreasOfOperations.Any(ao=> ao.CoreWorld == planetToCheck);
        }

        public AO CreateAO(Planet coreWorld, float radius)
        {
            var newAO = new AO(coreWorld.Universe, coreWorld, radius);
            EmpireAI.AreasOfOperations.Add(newAO);
            return newAO;
        }

        public AO CreateAO(Planet coreWorld, float radius, AO fromAO)
        {
            var newAO = new AO(coreWorld.Universe, coreWorld, radius, fromAO.WhichFleet, fromAO.CoreFleet);
            EmpireAI.AreasOfOperations.Add(newAO);
            return newAO;
        }

    }
}
