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
            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
            {
                ChangeToStep(OrderShipToColonizeWithEscort);
                Evaluate();
            }
        }

        GoalStep TargetPlanetStatus()
        {
            if (ColonizationTarget.Owner != null)
            {
                foreach (var relationship in empire.AllRelations)
                    empire.GetEmpireAI().ExpansionAI.CheckClaim(relationship.Key,
                                                                relationship.Value,
                                                                ColonizationTarget);

                ReleaseShipFromGoal();

                if (ColonizationTarget.Owner == empire)
                    return GoalStep.GoalComplete;

                Log.Info($"Colonize: {ColonizationTarget.Owner.Name} got there first");
                return GoalStep.GoalFailed;
            }

            if (!empire.isPlayer || empire.AutoColonize)
            {
                if (empire.KnownEnemyStrengthIn(ColonizationTarget.ParentSystem) > 50)
                {
                    Log.Info($"Target system {ColonizationTarget.ParentSystem.Name} has too many enemies");
                    ReleaseShipFromGoal();
                    return GoalStep.GoalFailed;
                }
            }

            return GoalStep.GoToNextStep;
        }

        void ReleaseShipFromGoal()
        {
            if (FinishedShip != null)
            {
                FinishedShip.AI.ClearOrdersAndWayPoints(AIState.AwaitingOrders);
                var nearestRallyPoint = empire.FindNearestRallyPoint(FinishedShip.Center);
                if (nearestRallyPoint != null)
                {
                    FinishedShip.AI.OrderOrbitPlanet(nearestRallyPoint);
                }
            }
        }

        GoalStep OrderShipForColonization()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (!ShipBuilder.PickColonyShip(empire, out Ship colonyShip))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, colonyShip, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.AddShip(colonyShip, this, notifyOnEmpty:empire.isPlayer);
            return GoalStep.GoToNextStep;
        }

        bool IsPlanetBuildingColonyShip()
        {
            if (PlanetBuildingAt == null)
                return false;
            return PlanetBuildingAt.IsColonyShipInQueue();
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (FinishedShip != null) // we already have a ship
                return GoalStep.GoToNextStep;

            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            if (ColonizationTarget.Owner == null)
                return GoalStep.TryAgain;

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
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (FinishedShip == null) // @todo This is a workaround for possible safequeue bug causing this to fail on save load
                return GoalStep.GoalFailed;

            FinishedShip.DoColonize(ColonizationTarget, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (FinishedShip == null)
                return GoalStep.GoalFailed;

            if (FinishedShip.AI.State != AIState.Colonize)
                return GoalStep.GoalFailed;

            if (!FinishedShip.AI.FindGoal(ShipAI.Plan.Colonize, out ShipAI.ShipGoal goal)
                                           || goal.TargetPlanet != ColonizationTarget)
                return GoalStep.GoalFailed;

            if (ColonizationTarget.Owner == null) return GoalStep.TryAgain;

            return GoalStep.GoalComplete;
        }
    }
}
