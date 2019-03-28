using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ship_Game.Commands.Goals
{
    public class MarkForColonization : Goal
    {
        public const string ID = "MarkForColonization";
        public override string UID => ID;
        bool HasEscort;
        public bool WaitingForEscort { get; private set; }

        public MarkForColonization() : base(GoalType.Colonize)
        {
            Steps = new Func<GoalStep>[]
            {
                OrderShipForColonization,
                EnsureBuildingColonyShip,
                OrderShipToColonizeWithEscort,
                WaitForColonizationComplete
            };

        }
        public MarkForColonization(Planet toColonize, Empire e) : this()
        {
            empire = e;
            ColonizationTarget = toColonize;
        }

        bool IsValid()
        {
            if (ColonizationTarget.Owner == null)
                return true;

            foreach (var relationship in empire.AllRelations)
                empire.GetEmpireAI().CheckClaim(relationship.Key, relationship.Value, ColonizationTarget);

            RemoveEscortTask(); //this is in the wrong place

            if (FinishedShip == null)
                return false;

            var planet = empire.FindNearestRallyPoint(FinishedShip.Center);
            if (planet != null)
            {
                FinishedShip.AI.OrderRebase(planet, true);
                return false;
            }
            FinishedShip.AI.State = AIState.AwaitingOrders;
            return false;
        }

        MilitaryTask GetClaimTask()
        {
            using (empire.GetEmpireAI().TaskList.AcquireReadLock())
            {
                foreach (MilitaryTask escort in empire.GetEmpireAI().TaskList)
                {
                    foreach (Guid held in escort.HeldGoals)
                    {
                        if (held == guid)
                            return escort;
                    }
                }
            }
            return null;
        }

        void RemoveEscortTask()
        {
            MilitaryTask defendClaim = GetClaimTask();
            if (defendClaim != null)
                empire.GetEmpireAI().TaskList.QueuePendingRemoval(defendClaim);
        }

        TaskStatus EscortStatus(float enemyStrength)
        {
            MilitaryTask defendClaim = GetClaimTask();
            if (defendClaim != null)
            {
                HasEscort        = defendClaim.Step > 0;
                WaitingForEscort = !HasEscort;
                return TaskStatus.Running;
            }
            return TaskStatus.Canceled;
        }

        // @return TRUE if WaitingForEscort
        bool UpdateEscortNeeds()
        {
            WaitingForEscort = HasEscort = false;
            if (empire.isPlayer || empire.isFaction)
                return false;
            var escortTask = GetClaimTask();
            if (escortTask != null)
            {
                if (escortTask?.Step > 0 && escortTask.Fleet != null)
                {
                    var fleet = escortTask.Fleet;
                    if (fleet.TaskStep < 3)
                        return true;
                    if (fleet.TaskCombatStatus > ShipGroup.CombatStatus.InCombat)
                        return false;
                }
            }

            float radius = escortTask?.AORadius ?? 125000f;

            float str = empire.GetEmpireAI().ThreatMatrix.PingRadarStr(ColonizationTarget.Center, radius, empire);

            WaitingForEscort = true;

            if (EscortStatus(str) == TaskStatus.Running)
                return true;

            if (empire.data.DiplomaticPersonality.Territorialism < 50 &&
                empire.data.DiplomaticPersonality.Trustworthiness < 50)
            {
                var goalsToHold = new Array<Goal> { this };
                var task        = new MilitaryTask(ColonizationTarget.Center, 125000f, goalsToHold, empire, str)
                {
                    InitialEnemyStrength = str
                };

                task.SetTargetPlanet(ColonizationTarget);
                    empire.GetEmpireAI().TaskList.Add(task);
            }

            var militaryTask = new MilitaryTask
            {
                AO = ColonizationTarget.Center
            };
            militaryTask.type                     = MilitaryTask.TaskType.DefendClaim;
            militaryTask.AORadius                 = 75000f;
            militaryTask.MinimumTaskForceStrength = str;
            militaryTask.SetEmpire(empire);
            militaryTask.SetTargetPlanet(ColonizationTarget);
            militaryTask.HeldGoals.Add(guid);

            empire.GetEmpireAI().TaskList.Add(militaryTask);
            WaitingForEscort = true;
            return true;
        }

        bool GetColonyShipTemplate(out Ship colonyShip)
        {
            if (empire.isPlayer && ResourceManager.GetShipTemplate(empire.data.CurrentAutoColony, out colonyShip))
                return true;
            return ResourceManager.GetShipTemplate(empire.data.DefaultColonyShip, out colonyShip);
        }

        GoalStep OrderShipForColonization()
        {
            if (!IsValid())
                return GoalStep.GoalComplete;

            UpdateEscortNeeds();

            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
                return GoalStep.GoToNextStep;

            if (!GetColonyShipTemplate(out Ship colonyShipType))
            {
                Log.Error($"{empire} failed to find a ColonyShip template! AutoColony:{empire.data.CurrentAutoColony}  Default:{empire.data.DefaultColonyShip}");
                return GoalStep.GoalFailed;
            }

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, colonyShipType, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.AddShip(colonyShipType, this, notifyOnEmpty:empire.isPlayer);
            return GoalStep.GoToNextStep;
        }

        bool IsPlanetBuildingColonyShip()
        {
            if (PlanetBuildingAt == null)
                return false;
            foreach (QueueItem queueItem in PlanetBuildingAt.ConstructionQueue)
                if (queueItem.isShip && ResourceManager.ShipsDict[queueItem.sData.Name].isColonyShip)
                    return true;
            return false;
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (FinishedShip != null) // we already have a ship
                return GoalStep.GoToNextStep;

            if (!IsValid())
                return GoalStep.GoalComplete;

            if (!HasEscort)
                UpdateEscortNeeds();

            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            if (ColonizationTarget.Owner == null)
                return GoalStep.TryAgain;

            foreach (KeyValuePair<Empire, Relationship> them in empire.AllRelations)
                empire.GetEmpireAI().CheckClaim(them.Key, them.Value, ColonizationTarget);

            return GoalStep.GoalComplete;
        }

        Ship FindIdleColonyShip()
        {
            if (FinishedShip != null) return FinishedShip;

            foreach (Ship ship in empire.GetShips())
                if (ship.isColonyShip && ship.AI != null && ship.AI.State != AIState.Colonize)
                    return ship;

            return null;
        }

        GoalStep OrderShipToColonizeWithEscort()
        {
            if (!IsValid())
                return GoalStep.GoalFailed;

            if (UpdateEscortNeeds() && !HasEscort)
                return GoalStep.TryAgain;

            if (FinishedShip == null) // @todo This is a workaround for possible safequeue bug causing this to fail on save load
                return GoalStep.RestartGoal;

            FinishedShip.DoColonize(ColonizationTarget, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
             if (!IsValid())
                return GoalStep.GoalFailed;

            if (UpdateEscortNeeds() && !HasEscort)         return GoalStep.TryAgain;
            if (FinishedShip == null)                      return GoalStep.RestartGoal;
            if (FinishedShip.AI.State != AIState.Colonize) return GoalStep.RestartGoal;
            if (ColonizationTarget.Owner == null)          return GoalStep.TryAgain;

            return GoalStep.GoalComplete;
        }
    }
}
