using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private static int GetConstructionQueueTurnsRemaining(Planet p)
        {
            int totalTurns = 0;
            foreach (QueueItem queueItem in p.ConstructionQueue)
            {
                float remainingWork = queueItem.Cost - queueItem.productionTowards;
                int turnsUntilItemComplete = (int)(remainingWork / p.NetProductionPerTurn);
                totalTurns += turnsUntilItemComplete;

            }
            return totalTurns;
        }

        private static Planet FindPlanetForConstruction(Empire e)
        {
            var candidates = new Array<Planet>();
            foreach (Planet p in e.GetPlanets())
            {
                if (p.HasShipyard && p.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                    candidates.Add(p);
            }
            return candidates.FindMin(p => GetConstructionQueueTurnsRemaining(p));
        }

        private bool IsValid()
        {
            if (markedPlanet.Owner == null) return true;
            foreach (var relationship in empire.AllRelations)
                empire.GetGSAI().CheckClaim(relationship, GetMarkedPlanet());
            
            using (empire.GetGSAI().TaskList.AcquireReadLock())
            {
                foreach (MilitaryTask task in empire.GetGSAI().TaskList)
                {
                    foreach (Guid held in task.HeldGoals)
                    {
                        if (held != guid)
                            continue;

                        empire.GetGSAI().TaskList.QueuePendingRemoval(task);
                        break;
                    }
                }
            }

            if (colonyShip == null) return false;
            var planet = empire.FindNearestRallyPoint(colonyShip.Center);
            if (planet != null)
                colonyShip.AI.OrderRebase(planet, true);
            return false;
        }

        private bool NeedsEscort()
        {
            if (empire.isPlayer || empire.isFaction) return false;
            
            float str = empire.GetGSAI().ThreatMatrix.PingRadarStr(markedPlanet.Center, 100000f, empire);
            if (str < 10)
                return false;
            using (empire.GetGSAI().TaskList.AcquireReadLock())
            {
                foreach (MilitaryTask escort in empire.GetGSAI().TaskList)
                {
                    foreach (Guid held in escort.HeldGoals)
                    {
                        if (held != guid)
                            continue;
                        HasEscort = escort.WhichFleet != -1;
                        return false;
                    }
                }
            }
            
            if (empire.data.DiplomaticPersonality.Territorialism < 50 &&
                empire.data.DiplomaticPersonality.Trustworthiness < 50)
            {

                var tohold = new Array<Goal> { this };
                var task =
                    new MilitaryTask(markedPlanet.Center, 125000f, tohold, empire, str);
                {
                    empire.GetGSAI().TaskList.Add(task);
                }
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
                planet.ConstructionQueue.Add(new QueueItem
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
            else
            {
                Ship shipTypeToBuild = ResourceManager.ShipsDict[empire.data.DefaultColonyShip];
                planet.ConstructionQueue.Add(new QueueItem
                {
                    isShip        = true,
                    QueueNumber   = planet.ConstructionQueue.Count,
                    sData         = shipTypeToBuild.shipData,
                    Goal          = this,
                    Cost          = shipTypeToBuild.GetCost(empire),
                    NotifyOnEmpty = false, // @todo wtf is this???
                });
                PlanetBuildingAt = planet;
                return GoalStep.GoToNextStep;
            }
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

            this.PlanetBuildingAt = (Planet) null;
            return GoalStep.RestartGoal;
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
            if (!IsValid()) return GoalStep.GoalComplete;
            if (!HasEscort) NeedsEscort();
            if (markedPlanet.Owner != null) // Planet is owned by someone?
            {
                foreach (KeyValuePair<Empire, Relationship> them in empire.AllRelations)
                    empire.GetGSAI().CheckClaim(them, markedPlanet);
                return GoalStep.TryAgain;
            }

            colonyShip = FindIdleColonyShip();
            if (colonyShip == null)
                return GoalStep.RestartGoal;

            colonyShip.DoColonize(markedPlanet, this);
            return GoalStep.GoToNextStep;
        }

        private GoalStep FinalStep()
        {
            if (!HasEscort && NeedsEscort())
                return GoalStep.TryAgain;
            if (!IsValid()) return GoalStep.GoalComplete;

            if (colonyShip == null) // @todo This is a workaround for a bug
                return GoalStep.RestartGoal;
            if (colonyShip != null && colonyShip.Active && colonyShip.AI.State != AIState.Colonize)
                return GoalStep.RestartGoal;
            if (colonyShip != null && !colonyShip.Active && markedPlanet.Owner == null)
                return GoalStep.RestartGoal;
            if (markedPlanet.Owner == null)
                return GoalStep.TryAgain;

            foreach (KeyValuePair<Empire, Relationship> them in empire.AllRelations)
                empire.GetGSAI().CheckClaim(them, markedPlanet);
            colonyShip.AI.State = AIState.AwaitingOrders;
            return GoalStep.GoalComplete;
        }
    }
}
