using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class WarTasks
    {
        public Array<Guid> TaskGuids;
        readonly Array<MilitaryTask> NewTasks;
        Empire Owner;
        Empire Target;
        Campaign OwnerCampaign;

        public WarTasks(Empire owner, Empire target, Campaign campaign)
        {
            Owner     = owner;
            Target    = target;
            NewTasks  = new Array<MilitaryTask>();
            TaskGuids = new Array<Guid>();
        }

        public void RestoreFromSave(Empire owner, Empire target, Campaign campaign)
        {
            Owner         = owner;
            Target        = target;
            OwnerCampaign = campaign;
        }

        public virtual void Update()
        {
            ProcessNewTasks();
        }

        public void PurgeAllTasks()
        {
            foreach (var task in TaskGuids) 
                Owner.GetEmpireAI().EndTaskByGuid(task);
            TaskGuids.Clear();

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
                if (planet.Owner != Target || planet.Owner == Owner) continue;

                while (!IsAlreadyAssaultingPlanet(planet, fleetsPerTarget))
                {
                    CreateTask(new MilitaryTask(planet, Owner){Priority = priority});

                    if (Owner.canBuildBombers)
                    {
                        var task = new MilitaryTask(planet, Owner)
                        {
                            Priority = priority,
                            type = MilitaryTask.TaskType.GlassPlanet,
                            OwnerCampaign = OwnerCampaign
                        };
                        CreateTask(task);
                    }
                }
            }
        }

        bool IsAlreadyAssaultingPlanet(Planet planetToAssault, int numberOfFleets = 1)
        {
            int assaults    = NewTasks.Count(t => t.TargetPlanet == planetToAssault);
            if (numberOfFleets <= assaults) return true;

            assaults += Owner.GetEmpireAI().CountAssaultsOnPlanet(planetToAssault);
            return numberOfFleets <= assaults ;
        }

        public void StandardSystemDefense(SolarSystem system, int priority, float strengthWanted)
        {
            if (IsAlreadyDefendingSystem(system)) return;
            Vector2 center = system.Position;
            float radius   = system.Radius * 1.5f;
            CreateTask(new MilitaryTask(center, radius, system, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                OwnerCampaign = OwnerCampaign,
                Priority      = priority
            });
        }

        public void StandardAreaClear(Vector2 center, float radius, int priority, float strengthWanted)
        {
            if (IsAlreadyClearingArea(center, radius)) return;
            CreateTask(new MilitaryTask(center, radius, null, strengthWanted, MilitaryTask.TaskType.ClearAreaOfEnemies)
            {
                OwnerCampaign = OwnerCampaign,
                Priority      = priority
            });
        }

        bool IsAlreadyDefendingSystem(SolarSystem system)
        {
            if (NewTasks.Any(t => t.IsDefendingSystem(system))) return true;
            return Owner.GetEmpireAI().IsClearTaskTargetingAO(system);
        }

        bool IsAlreadyClearingArea(Vector2 center, float radius)
        {
            if (NewTasks.Any(t => t.type == MilitaryTask.TaskType.ClearAreaOfEnemies && t.AO.InRadius(center, radius))) return true;
            return Owner.GetEmpireAI().IsClearTaskTargetingAO(center, radius);
        }

        void CreateTask(MilitaryTask task)
        {
            NewTasks.Add(task);
            TaskGuids.Add(task.TaskGuid);
        }

        void ProcessNewTasks()
        {
            if (NewTasks.Count == 0) return;
            Owner.GetEmpireAI().AddPendingTasks(NewTasks);
            NewTasks.Clear();
        }
    }
}