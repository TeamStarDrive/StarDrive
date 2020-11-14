using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class WarTasks
    {
        public Array<MilitaryTask> NewTasks;
        public Array<Guid> HardTargets;
        Empire Owner;
        Empire Target;
        Campaign OwnerCampaign;

        public WarTasks(Empire owner, Empire target, Campaign campaign)
        {
            Owner         = owner;
            Target        = target;
            NewTasks      = new Array<MilitaryTask>();
            OwnerCampaign = campaign;
            HardTargets   = new Array<Guid>();
        }

        public void RestoreFromSave(Empire owner, Empire target, Campaign campaign)
        {
            Owner         = owner;
            Target        = target;
            OwnerCampaign = campaign;
            
            NewTasks.ForEach(t=> t.RestoreFromSaveFromUniverse(owner));
        }

        public virtual void Update()
        {
            for (int i = 0; i < NewTasks.Count; i++)
            {
                var task = NewTasks[i];

                if (task.Step == 0)
                    task.EndTask();
                if (task.TargetPlanet != null && !task.TargetPlanet.Owner?.IsAtWarWith(Owner) == true)
                    task.EndTask();

                if (task.QueuedForRemoval)
                {
                    CreateTaskAfterActionReport(task);
                    NewTasks.RemoveAtSwapLast(i);
                }
                else
                {
                    task.RallyAO   = OwnerCampaign.RallyAO;
                }
            }

            NewTasks.SortedDescending(t => t.Priority);
                
            for (var i = 0; i < NewTasks.Count; i++)
            {
                NewTasks[i].Evaluate(Owner);
            }
        }

        void CreateTaskAfterActionReport(MilitaryTask task)
        {
            bool planetObjectiveWon = task.TargetPlanet?.Owner == null ||  task.TargetPlanet.Owner == Owner;
            if (task.TargetPlanet != null && planetObjectiveWon)
                HardTargets.Add(task.TargetPlanet.guid);
            
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
                if (OwnerCampaign.GetWarType() == WarType.EmpireDefense)
                {
                    if (!Owner.IsEmpireHostile(planet.Owner)) 
                        continue;
                }
                else if (planet.Owner != Target || planet.Owner == Owner) 
                    continue;

                if (IsAlreadyAssaultingPlanet(planet))               continue;
                
                CreateTask(new MilitaryTask(planet, Owner)
                {
                    Priority = priority
                });

                if (Owner.canBuildBombers && !IsAlreadyGlassingPlanet(planet))
                {
                    var task = new MilitaryTask(planet, Owner)
                    {
                        Priority = priority + 1,
                        type = MilitaryTask.TaskType.GlassPlanet
                    };
                    CreateTask(task);
                }
            }
        }

        public bool IsAlreadyAssaultingSystem(SolarSystem system)
        {
            bool assaults     = NewTasks.Any(t => t.type == MilitaryTask.TaskType.AssaultPlanet && t.TargetPlanet.ParentSystem == system);
            return  assaults;// || Owner.GetEmpireAI().IsAssaultingSystem(system, OwnerCampaign);
        }

        bool IsAlreadyAssaultingPlanet(Planet planetToAssault)
        {
            bool assaults    = NewTasks.Any(t => t.type == MilitaryTask.TaskType.AssaultPlanet && t.TargetPlanet == planetToAssault);
            return assaults;// || Owner.GetEmpireAI().IsAssaultingPlanet(planetToAssault, OwnerCampaign);
        }

        bool IsAlreadyGlassingPlanet(Planet planetToAssault)
        {
            bool assaults    = NewTasks.Any(t => t.type == MilitaryTask.TaskType.GlassPlanet && t.TargetPlanet == planetToAssault);
            return assaults;// || Owner.GetEmpireAI().IsGlassingPlanet(planetToAssault, OwnerCampaign);
        }

        bool IsAlreadyDefendingSystem(SolarSystem system)
        {
            bool defending    = NewTasks.Any(t => t.IsDefendingSystem(system));
            return defending;
                                        
        }

        bool IsAlreadyClearingArea(Vector2 center, float radius)
        {
            bool clearing    = NewTasks.Any(t => t.type == MilitaryTask.TaskType.ClearAreaOfEnemies && t.AO.InRadius(center, radius));
            return clearing;
        }

        public void StandardSystemDefense(SolarSystem system, int priority, float strengthWanted)
        {
            if (IsAlreadyDefendingSystem(system)) return;
            
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;
            CreateTask(new MilitaryTask(center, radius, system, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                Priority      = priority
            });
        }

        public void StandardAreaClear(Vector2 center, float radius, int priority, float strengthWanted)
        {
            if (IsAlreadyClearingArea(center, radius)) return;
            CreateTask(new MilitaryTask(center, radius, null, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                Priority      = priority
            });
        }

        public void PurgeAllTasks()
        {
            foreach(var task in NewTasks)
                task.EndTask();
        }

        void CreateTask(MilitaryTask task)
        {
            //var planet = task.TargetPlanet;
            //if (planet != null)
            //{
            //    int fails = HardTargets.Count(p=>p == planet.guid);
            //    task.FleetCount += fails;
            //}
            NewTasks.Add(task);
        }
    }
}