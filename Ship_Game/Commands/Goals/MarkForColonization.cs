using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class MarkForColonization : Goal
    {
        public const string ID = "MarkForColonization";
        public override string UID => ID;
        private bool HasEscort;
        public bool WaitingForEscort { get; private set; }

        public MarkForColonization() : base(GoalType.Colonize)
        {
            Steps = new Func<GoalStep>[]
            {
                OrderShipForColonization,
                EnsureBuildingColonyShip,
                Step2,
                FinalStep
            };

        }
        public MarkForColonization(Planet toColonize, Empire e) : this()
        {
            empire = e;
            markedPlanet = toColonize;
            colonyShip = null;
        }

        private static Planet FindPlanetForConstruction(Empire e) => e.BestBuildPlanets.FindMin(p => p.TotalTurnsInConstruction);            

        private bool IsValid()
        {
            if (markedPlanet.Owner == null) return true;
            foreach (var relationship in empire.AllRelations)
                empire.GetGSAI().CheckClaim(relationship, GetMarkedPlanet());
                        
            RemoveEscortTask();

            if (colonyShip == null) return false;
            var planet = empire.FindNearestRallyPoint(colonyShip.Center);
            if (planet != null)
            {
                colonyShip.AI.OrderRebase(planet, true);
                return false;
            }
            colonyShip.AI.State = AIState.AwaitingOrders;
            return false;
        }

        private MilitaryTask GetClaimTask()
        {
            using (empire.GetGSAI().TaskList.AcquireReadLock())
            {
                foreach (MilitaryTask escort in empire.GetGSAI().TaskList)
                {
                    foreach (Guid held in escort.HeldGoals)
                    {
                        if (held != guid)
                            continue;                      
                        return escort;
                    }
                }
            }
            return null;
        }

        private void RemoveEscortTask()
        {
            MilitaryTask defendClaim = GetClaimTask();
            if (defendClaim != null)            
                empire.GetGSAI().TaskList.QueuePendingRemoval(defendClaim);
        }


        private TaskStatus EscortStatus(float enemyStrength)
        {
            MilitaryTask defendClaim = GetClaimTask();
            if (defendClaim != null)
            {
                HasEscort = defendClaim.Step > 2 && enemyStrength < 10;
                WaitingForEscort = !HasEscort;
                return TaskStatus.Running;
            }
            return TaskStatus.Canceled;
        }

        private bool NeedsEscort()
        {
            WaitingForEscort = HasEscort = false;
            if (empire.isPlayer || empire.isFaction) return false;
            
            float str = empire.GetGSAI().ThreatMatrix.PingRadarStr(markedPlanet.Center, 150000f, empire);
            if (str < 10)
                return false;
            WaitingForEscort = true;

            if (EscortStatus(str) == TaskStatus.Running)            
                return true;
            
            
            if (empire.data.DiplomaticPersonality.Territorialism < 50 &&
                empire.data.DiplomaticPersonality.Trustworthiness < 50)
            {

                var tohold = new Array<Goal> { this };
                var task =
                    new MilitaryTask(markedPlanet.Center, 125000f, tohold, empire, str);
                task.SetTargetPlanet(markedPlanet);
                    empire.GetGSAI().TaskList.Add(task);                                    
            }

            var militaryTask = new MilitaryTask
            {
                AO = markedPlanet.Center
            };
            militaryTask.SetEmpire(empire);
            militaryTask.AORadius                 = 75000f;
            militaryTask.SetTargetPlanet(markedPlanet);
            militaryTask.TargetPlanetGuid         = markedPlanet.guid;
            militaryTask.MinimumTaskForceStrength = str;
            militaryTask.HeldGoals.Add(guid);
            militaryTask.type                     = MilitaryTask.TaskType.DefendClaim;
            {
                empire.GetGSAI().TaskList.Add(militaryTask);
            }
            WaitingForEscort = true;
            return true;
        }

        private GoalStep OrderShipForColonization()
        {
            if (!IsValid()) return GoalStep.GoalComplete;
            NeedsEscort();

            colonyShip = FindIdleColonyShip();
            if (colonyShip != null)
            {
                Step = 2;
                return Steps[Step]();
            }

            Planet planet = FindPlanetForConstruction(empire);
            if (planet == null)
                return GoalStep.TryAgain;

            if (empire.isPlayer && ResourceManager.ShipsDict.TryGetValue(empire.data.CurrentAutoColony, out Ship autoColony))
            {
                planet.ConstructionQueue.Add(new QueueItem(planet)
                {
                    isShip      = true,
                    QueueNumber = planet.ConstructionQueue.Count,
                    sData       = autoColony.shipData,
                    Goal        = this,
                    Cost        = autoColony.GetCost(empire)
                });
                PlanetBuildingAt = planet;
                return GoalStep.GoToNextStep;
            }

            Ship shipTypeToBuild = ResourceManager.ShipsDict[empire.data.DefaultColonyShip];
            planet.ConstructionQueue.Add(new QueueItem(planet)
            {
                isShip        = true,
                QueueNumber   = planet.ConstructionQueue.Count,
                sData         = shipTypeToBuild.shipData,
                Goal          = this,
                Cost          = shipTypeToBuild.GetCost(empire),
                NotifyOnEmpty = false // @todo wtf is this???
            });
            PlanetBuildingAt = planet;
            return GoalStep.GoToNextStep;
        }

        private bool IsPlanetBuildingColonyShip()
        {
            if (PlanetBuildingAt == null)
                return false;
            foreach (QueueItem queueItem in PlanetBuildingAt.ConstructionQueue)
                if (queueItem.isShip && ResourceManager.ShipsDict[queueItem.sData.Name].isColonyShip)
                    return true;
            return false;
        }

        private GoalStep EnsureBuildingColonyShip()
        {
            if (!IsValid()) return GoalStep.GoalComplete;
            if (!HasEscort) NeedsEscort();
            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }
            if (markedPlanet.Owner == null)
                return GoalStep.TryAgain;

            foreach (KeyValuePair<Empire, Relationship> them in empire.AllRelations)
                empire.GetGSAI().CheckClaim(them, markedPlanet);
            return GoalStep.GoalComplete;
        }

        private Ship FindIdleColonyShip()
        {
            if (colonyShip != null)
                return colonyShip;

            foreach (Ship ship in empire.GetShips())
                if (ship.isColonyShip && !ship.PlayerShip && (ship.AI != null && ship.AI.State != AIState.Colonize))
                    return ship;

            return null;
        }

        private GoalStep Step2()
        {
            if (!IsValid())
                return GoalStep.GoalComplete;
            NeedsEscort();
            if (!HasEscort && WaitingForEscort)
                return GoalStep.TryAgain;

            colonyShip = FindIdleColonyShip();
            if (colonyShip == null)
                return GoalStep.RestartGoal;

            colonyShip.DoColonize(markedPlanet, this);
            return GoalStep.GoToNextStep;
        }

        private GoalStep FinalStep()
        {
            if (!IsValid())
                return GoalStep.GoalComplete;

            NeedsEscort();
            if (!HasEscort && WaitingForEscort)
                return GoalStep.TryAgain;

            if (colonyShip == null) // @todo This is a workaround for a bug
                return GoalStep.RestartGoal;
            if (colonyShip != null && colonyShip.Active && colonyShip.AI.State != AIState.Colonize)
                return GoalStep.RestartGoal;
            if (colonyShip != null && !colonyShip.Active && markedPlanet.Owner == null)
                return GoalStep.RestartGoal;
            if (markedPlanet.Owner == null)
                return GoalStep.TryAgain;

            //foreach (KeyValuePair<Empire, Relationship> them in empire.AllRelations)
            //    empire.GetGSAI().CheckClaim(them, markedPlanet);
            //colonyShip.AI.State = AIState.AwaitingOrders;
            return GoalStep.GoalComplete;
        }
    }
}
