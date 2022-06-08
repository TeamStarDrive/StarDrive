using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class ScoutSystem : Goal
    {
        public const string ID = "Scout System";
        public override string UID => ID;

        public ScoutSystem(int id, UniverseState us) : base(GoalType.ScoutSystem, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                DelayedStart,
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                SelectSystem,
                WaitForArrival,
                SniffAround
            };
        }
        public ScoutSystem(Empire empire)
            : this(empire.Universum.CreateId(), empire.Universum)
        {
            this.empire   = empire;
            StarDateAdded = empire.Universum.StarDate;
        }

        GoalStep DelayedStart()
        {
            if (empire.Universum.StarDate - StarDateAdded > 5)
            {
                StarDateAdded = empire.Universum.StarDate;
                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (FinishedShip != null)
            {
                ChangeToStep(SelectSystem);
                return GoalStep.TryAgain; 
            }
            if (!empire.ChooseScoutShipToBuild(out IShipDesign scout))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildShipAt(empire.SafeSpacePorts, scout, out Planet planet))
                return GoalStep.TryAgain;

            var queue    = planet.Construction.GetConstructionQueue();
            int priority = queue.Count > 0 && !planet.HasColonyShipFirstInQueue() && queue[0].ProductionNeeded > scout.GetCost(empire) * 2 ? 0 : 1;

            planet.Construction.Enqueue(scout, this, notifyOnEmpty: false);
            planet.Construction.PrioritizeShip(scout, priority, 2);

            return GoalStep.GoToNextStep;
        }
       
        GoalStep SelectSystem()
        {
            if (FinishedShip == null)
                return GoalStep.GoalFailed;

            if (!empire.GetEmpireAI().ExpansionAI.AssignScoutSystemTarget(FinishedShip, out SolarSystem targetSystem))
                return GoalStep.GoalFailed;

            FinishedShip.AI.OrderScout(targetSystem, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForArrival()
        {
            if (!ShipOnPlan)
                return GoalStep.RestartGoal;

            return FinishedShip.System != FinishedShip.AI.ExplorationTarget
                ? GoalStep.TryAgain
                : GoalStep.GoToNextStep;
        }

        GoalStep SniffAround()
        {
            if (!ShipOnPlan)
                return GoalStep.RestartGoal;

            if (FinishedShip.AI.BadGuysNear
                && FinishedShip.System == FinishedShip.AI.ExplorationTarget
                && FinishedShip.System.ShipList.Any(s => s.AI.Target == FinishedShip))
            {
                FinishedShip.AI.ClearOrders();
                if (FinishedShip.TryGetScoutFleeVector(out Vector2 escapePos))
                    FinishedShip.AI.OrderMoveToNoStop(escapePos, FinishedShip.Direction.DirectionToTarget(escapePos), AIState.Flee);
                else
                    FinishedShip.AI.OrderFlee();

                return GoalStep.RestartGoal;
            }

            return FinishedShip.Position.InRadius(FinishedShip.AI.ExplorationTarget.Position, 20000) 
                    ? GoalStep.GoalComplete 
                    : GoalStep.TryAgain;
        }

        bool ShipOnPlan => FinishedShip?.Active == true 
                           && FinishedShip.AI.ExplorationTarget != null;
    }
}
