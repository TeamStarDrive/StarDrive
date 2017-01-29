using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI
{
    public class OffensiveForcePoolManager
    {
        private readonly Empire Owner;
        private GSAI Gsai => Owner.GetGSAI();
        private ThreatMatrix ThreatMatrix => Gsai.ThreatMatrix;
        private DefensiveCoordinator DefensiveCoordinator => Gsai.DefensiveCoordinator;
        private Array<AO> AreasOfOperations => Gsai.AreasOfOperations;

        public OffensiveForcePoolManager (Empire owner)
        {
            Owner = owner;
        }
        public void ManageAOs()
        {
            float ownerStr = Owner.currentMilitaryStrength;
            for (int index = AreasOfOperations.Count - 1; index >= 0; index--)
            {
                AO areasOfOperation = AreasOfOperations[index];
                if (areasOfOperation.GetPlanet().Owner != Owner)
                {
                    AreasOfOperations.RemoveAt(index);
                    continue;
                }
                areasOfOperation.ThreatLevel = 0;
                areasOfOperation.ThreatLevel =
                    (int) ThreatMatrix.PingRadarStr(areasOfOperation.Position, areasOfOperation.Radius, Owner);

                int min =
                    (int)
                    (ownerStr *
                     ((DefensiveCoordinator.GetDefensiveThreatFromPlanets(areasOfOperation.GetPlanets()) + 1) * .01f));
                if (areasOfOperation.ThreatLevel < min)
                    areasOfOperation.ThreatLevel = min;
            }

            
            Planet[] aoPlanets = GetAOPlanets();

            if (aoPlanets.Length == Owner.GetPlanets().Count)
                return;
            Planet[] ownedPlanets =Owner.GetPlanets().ToArray();
            Planet[] planets = ownedPlanets.Except(aoPlanets);
            if (planets == null || planets.Length == 0) return;

            IOrderedEnumerable<Planet> maxProductionPotential =
                from planet in planets
                where planet != null
                orderby planet.GetMaxProductionPotential() descending
                select planet;

            foreach (Planet coreWorld in maxProductionPotential)
            {
                if (coreWorld == null || coreWorld.GetMaxProductionPotential() <= 5f || !coreWorld.HasShipyard) continue;
                float aoSize = 0;
                foreach (SolarSystem system in coreWorld.system.FiveClosestSystems)
                {
                    if (aoSize < Vector2.Distance(coreWorld.Position, system.Position))
                        aoSize = Vector2.Distance(coreWorld.Position, system.Position);
                }
                float aomax = Empire.Universe.Size.X * .2f;
                if (aoSize > aomax)
                    aoSize = aomax;
                bool flag1 = true;
                foreach (AO areasOfOperation2 in AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Position, coreWorld.Position) >= aoSize)
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
        public Planet[] GetAOPlanets()
        {
            int planetCount = 0;
            foreach (AO ao in AreasOfOperations)
                planetCount += ao.GetPlanets().Length;            
            Planet[] allPlanets = new Planet[planetCount];
            int x = 0;
            foreach (AO ao in AreasOfOperations)
                foreach(Planet planet in ao.GetPlanets())                
                    allPlanets[x++] = planet;
            return allPlanets;
        }
    }
}
