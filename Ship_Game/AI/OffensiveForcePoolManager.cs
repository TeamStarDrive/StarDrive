using System;
using System.Collections.Generic;
using System.Linq;
using SDUtils;

namespace Ship_Game.AI
{
    public class OffensiveForcePoolManager
    {
        readonly Empire Owner;
        EmpireAI EmpireAI => Owner.AI;

        public OffensiveForcePoolManager(Empire owner)
        {
            Owner = owner ?? throw new NullReferenceException(nameof(owner));
        }

        public void ManageAOs()
        {
            RemoveAOsAfterLosingOwnership();

            Planet[] aoPlanets = GetCurrentAOPlanets(out HashSet<SolarSystem> aoSystems);
            // all planets are utilized, no need to create new AO-s
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
                if (coreWorld == null || coreWorld.Prod.NetMaxPotential <= 5f || !coreWorld.HasSpacePort)
                    continue;

                float aoSize = 0;
                foreach (SolarSystem system in coreWorld.ParentSystem.FiveClosestSystems)
                {
                    if (!aoSystems.Contains(system))
                    {
                        float distance = coreWorld.Position.Distance(system.Position);
                        if (aoSize < distance) aoSize = distance;
                    }
                }

                bool hasAnyAOsCloserToCoreWorld = EmpireAI.AreasOfOperations.Any(
                    ao => ao.CoreWorld != null && ao.CoreWorld.Position.Distance(coreWorld.Position) < aoSize);

                if (!hasAnyAOsCloserToCoreWorld)
                {
                    var newAO = new AO(coreWorld.Universe, coreWorld, Owner, aoSize);
                    EmpireAI.AreasOfOperations.Add(newAO);
                }
            }
        }

        void RemoveAOsAfterLosingOwnership()
        {
            for (int i = EmpireAI.AreasOfOperations.Count - 1; i >= 0; i--)
            {
                AO ao = EmpireAI.AreasOfOperations[i];
                // if ao owner mismatches (corrupted save), or if we got coreworld and owner changed
                if (ao.Owner != Owner || (ao.CoreWorld != null && ao.CoreWorld.Owner != Owner))
                {
                    EmpireAI.AreasOfOperations.RemoveAt(i);
                    ao.Clear();
                }
            }
        }

        Planet[] GetCurrentAOPlanets(out HashSet<SolarSystem> systems)
        {
            systems = new();

            int planetCount = EmpireAI.AreasOfOperations.Sum(ao => ao.OurPlanets.Length);
            Planet[] allPlanets = new Planet[planetCount];

            int i = 0;
            foreach (AO ao in EmpireAI.AreasOfOperations)
            {
                foreach (Planet planet in ao.OurPlanets)
                {
                    systems.Add(planet.ParentSystem);
                    allPlanets[i++] = planet;
                }
            }
            return allPlanets;
        }
    }
}
