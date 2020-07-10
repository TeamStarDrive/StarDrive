using System.Collections.Generic;
using Ship_Game.AI.Tasks;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class WarTasks
    {
        readonly Array<MilitaryTask> Tasks;
        readonly Empire Owner;
        readonly Empire Target;

        public WarTasks(Empire owner, Empire target)
        {
            Owner = owner;
            Target = target;
            Tasks = new Array<MilitaryTask>();
        }

        public Array<MilitaryTask> GetNewTasks()
        {
            return Tasks;
        }

        public void StandardAssault(IEnumerable<SolarSystem> systemsToAttack, int priority)
        {
            foreach (var system in systemsToAttack)
            {
                StandardAssault(system, priority);
            }
        }

        public void StandardAssault(SolarSystem system, int priority, int fleetsPerTarget = 1)
        {
            foreach (var planet in system.PlanetList.SortedDescending(p => p.ColonyBaseValue(Owner)))
            {
                if (planet.Owner == Target)
                {
                    while (!IsAlreadyAssaultingPlanet(planet, fleetsPerTarget))
                    {
                        Tasks.Add(new MilitaryTask(planet, Owner){Priority = priority});
                    }
                }
            }
        }

        bool IsAlreadyAssaultingPlanet(Planet planetToAssault, int numberOfFleets = 1)
        {
            int assaults = Tasks.Count(t => t.TargetPlanet == planetToAssault);
            assaults    += Owner.GetEmpireAI().CountAssaultsOnPlanet(planetToAssault);

            return numberOfFleets <= assaults ;
        }
    }
}