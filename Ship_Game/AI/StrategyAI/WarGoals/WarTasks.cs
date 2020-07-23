using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class WarTasks
    {
        readonly Array<MilitaryTask> Tasks;
        readonly Array<MilitaryTask> NewTasks;
        readonly Empire Owner;
        readonly Empire Target;

        public WarTasks(Empire owner, Empire target)
        {
            Owner    = owner;
            Target   = target;
            Tasks    = new Array<MilitaryTask>();
            NewTasks = new Array<MilitaryTask>();
        }

        public virtual void Update()
        {
            ProcessNewTasks();
            for (int i = Tasks.Count - 1; i >= 0; i--)
            {
                var task = Tasks[i];
                if (task.QueuedForRemoval)
                    Tasks.RemoveAtSwapLast(i);
            }
        }

        public void PurgeAllTasks()
        {
            foreach (var task in Tasks)
            {
                Owner.GetEmpireAI().QueueForRemoval(task);
            }
            Update();
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
                if (planet.Owner == Target && planet.Owner != Owner)
                {
                    while (!IsAlreadyAssaultingPlanet(planet, fleetsPerTarget))
                    {
                        CreateTask(new MilitaryTask(planet, Owner){Priority = priority});

                        if (Owner.canBuildBombers)
                        {
                            var task = new MilitaryTask(planet, Owner) { Priority = priority, type = MilitaryTask.TaskType.GlassPlanet };
                            CreateTask(task);
                        }
                    }
                }
            }
        }

        bool IsAlreadyAssaultingPlanet(Planet planetToAssault, int numberOfFleets = 1)
        {
            int assaults = Tasks.Count(t => t.TargetPlanet == planetToAssault);
            assaults    += NewTasks.Count(t => t.TargetPlanet == planetToAssault);

            return numberOfFleets <= assaults ;
        }

        public void StandardSystemDefense(SolarSystem system, int priority, float strengthWanted)
        {
            if (IsAlreadyDefendingSystem(system)) return;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;
            CreateTask(new MilitaryTask(center, radius,system, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                Priority      = priority
            });
        }

        bool IsAlreadyDefendingSystem(SolarSystem system)
        {
            if (Tasks.Any(t => t.IsDefendingSystem(system))) return true;
            if (NewTasks.Any(t => t.IsDefendingSystem(system))) return true;
            return Owner.GetEmpireAI().IsClearTaskTargetingAO(system);
        }

        void CreateTask(MilitaryTask task)
        {
            Tasks.Add(task);
            NewTasks.Add(task);
        }

        void ProcessNewTasks()
        {
            if (NewTasks.Count == 0) return;
            Owner.GetEmpireAI().AddPendingTasks(NewTasks);
            NewTasks.Clear();
        }

    }
}