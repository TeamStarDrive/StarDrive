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
                ThisStepIsWeird,
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

                var tohold = new Array<Goal>
                {
                    this
                };
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
            bool flag1 = false;
            foreach (Ship ship in empire.GetShips())
            {
                if (ship.isColonyShip && !ship.PlayerShip && (ship.AI != null && ship.AI.State != AIState.Colonize))
                {
                    this.colonyShip = ship;
                    flag1 = true;
                }
            }
            Planet planet1 = null;
            if (!flag1)
            {
                Array<Planet> list = new Array<Planet>();
                foreach (Planet planet2 in this.empire.GetPlanets())
                {
                    if (planet2.HasShipyard && planet2.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                        list.Add(planet2);
                }
                int num1 = 9999999;
                foreach (Planet planet2 in list)
                {
                    int num2 = 0;
                    foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                        num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.NetProductionPerTurn);
                    if (num2 < num1)
                    {
                        num1 = num2;
                        planet1 = planet2;
                    }
                }
                if (planet1 == null)
                    return GoalStep.TryAgain;
                if (this.empire.isPlayer && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentAutoColony))
                {
                    planet1.ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        QueueNumber = planet1.ConstructionQueue.Count,
                        sData = ResourceManager.ShipsDict[this.empire.data.CurrentAutoColony].GetShipData(),
                        Goal = this,
                        Cost = ResourceManager.ShipsDict[this.empire.data.CurrentAutoColony].GetCost(this.empire)
                    });
                    this.PlanetBuildingAt = planet1;
                    return GoalStep.GoToNextStep;
                }
                else
                {
                    QueueItem queueItem = new QueueItem();
                    queueItem.isShip = true;
                    queueItem.QueueNumber = planet1.ConstructionQueue.Count;
                    if (ResourceManager.ShipsDict.ContainsKey(this.empire.data.DefaultColonyShip))
                        queueItem.sData = ResourceManager.ShipsDict[this.empire.data.DefaultColonyShip].GetShipData();
                    else
                    {
                        queueItem.sData = ResourceManager.ShipsDict[ResourceManager.GetEmpireByName(this.empire.data.Traits.Name).DefaultColonyShip].GetShipData();
                        this.empire.data.DefaultColonyShip = ResourceManager.GetEmpireByName(this.empire.data.Traits.Name).DefaultColonyShip;
                    }
                    queueItem.Goal = this;
                    queueItem.NotifyOnEmpty = false;
                    queueItem.Cost = ResourceManager.ShipsDict[this.empire.data.DefaultColonyShip].GetCost(this.empire);
                    planet1.ConstructionQueue.Add(queueItem);
                    this.PlanetBuildingAt = planet1;
                    return GoalStep.GoToNextStep;
                }
            }
            else
            {
                Step = 2;
                return Steps[Step]();
            }
        }

        private GoalStep ThisStepIsWeird() //Weird because build completion is handled externally. need to fix that.
        {
            if (!IsValid()) return GoalStep.GoalComplete;
            if (!HasEscort) NeedsEscort();
            if (this.PlanetBuildingAt != null)
                foreach (QueueItem queueItem in PlanetBuildingAt.ConstructionQueue)
                {
                    if (queueItem.isShip && ResourceManager.ShipsDict[queueItem.sData.Name].isColonyShip)
                        return GoalStep.TryAgain;
                }

            this.PlanetBuildingAt = (Planet) null;
            return GoalStep.RestartGoal;
        }

        private GoalStep Step2()
        {
            if (!IsValid()) return GoalStep.GoalComplete;
            if (!HasEscort) NeedsEscort();
            bool flag3;
            if (colonyShip != null)
            {
                this.colonyShip.DoColonize(this.markedPlanet, this);
                return GoalStep.GoToNextStep;
            }
            
            foreach (Ship ship in empire.GetShips())
            {
                if (!ship.isColonyShip || ship.PlayerShip ||
                    (ship.AI == null || ship.AI.State == AIState.Colonize)) continue;
                colonyShip = ship;
                colonyShip.DoColonize(markedPlanet, this);
                return GoalStep.GoToNextStep;
            }
            return GoalStep.RestartGoal;
         
        }

        private GoalStep FinalStep()
        {
            if (!HasEscort && NeedsEscort())
                return GoalStep.TryAgain;
            if (!IsValid()) return GoalStep.GoalComplete;
            if (this.colonyShip == null)
            {
                return GoalStep.RestartGoal;
            }
            if (this.colonyShip != null && this.colonyShip.Active && this.colonyShip.AI.State != AIState.Colonize)
            {
                return GoalStep.RestartGoal;
            }
            if (this.colonyShip != null && !this.colonyShip.Active && this.markedPlanet.Owner == null)
            {
                return GoalStep.RestartGoal;
            }
            
            return GoalStep.TryAgain;
            
        }
    }
}
