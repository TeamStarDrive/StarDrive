using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Gameplay;

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
                CreateClaimTask,
                CheckIfColonizationIsSafe,
                OrderShipForColonization,
                EnsureBuildingColonyShip,
                OrderShipToColonize,
                WaitForColonizationComplete
            };
        }

        public MarkForColonization(Planet toColonize, Empire e) : this()
        {
            empire             = e;
            ColonizationTarget = toColonize;
            StarDateAdded      = Empire.Universe.StarDate;
            if (PositiveEnemyPresence(out _) && AIControlsColonization) 
                return;

            // Fast track to colonize if planet is safe and we have a ready Colony Ship
            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
            {
                ChangeToStep(OrderShipToColonize);
                Evaluate();
            }

            if (!AIControlsColonization) // Fast track for player colonization
                ChangeToStep(OrderShipForColonization);
        }

        // Player ordered an existing colony ship to colonize
        public MarkForColonization(Ship colonyShip, Planet toColonize, Empire e) : this()
        {
            empire             = e;
            ColonizationTarget = toColonize;
            FinishedShip       = colonyShip;
            StarDateAdded      = Empire.Universe.StarDate;
            ChangeToStep(WaitForColonizationComplete);
        }

        GoalStep TargetPlanetStatus()
        {
            if (!empire.isPlayer && PlanetRanker.IsColonizeBlockedByMorals(ColonizationTarget.ParentSystem, empire))
                return GoalStep.GoalFailed;

            if (ColonizationTarget.Owner != null)
            {
                if (ColonizationTarget.Owner == empire)
                    return GoalStep.GoalComplete;

                // If the owner is a faction, fail the goal so next time we also get a claim fleet to invade
                if (ColonizationTarget.Owner.isFaction) 
                    return FinishedShip != null ? GoalStep.GoalFailed : GoalStep.GoToNextStep;

                foreach ((Empire them, Relationship rel) in empire.AllRelations)
                    empire.GetEmpireAI().ExpansionAI.CheckClaim(them, rel, ColonizationTarget);

                ReleaseShipFromGoal();
                Log.Info($"Colonize: {ColonizationTarget.Owner.Name} got there first");
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CreateClaimTask()
        {
            if (empire.isPlayer)
                return GoalStep.GoToNextStep;

            if (PositiveEnemyPresence(out float spaceStrength))
            {
                EmpireAI empireAi   = empire.GetEmpireAI();
                TargetEmpire        = empireAi.ThreatMatrix.GetDominantEmpireInSystem(ColonizationTarget.ParentSystem);
                float strMultiplier = empire.GetFleetStrEmpireMultiplier(TargetEmpire);
                var task            = MilitaryTask.CreateClaimTask(empire, ColonizationTarget, (spaceStrength * strMultiplier).LowerBound(20));
                empireAi.AddPendingTask(task);
                empireAi.Goals.Add(new StandbyColonyShip(empire));
            }
            else if (!ColonizationTarget.ParentSystem.HasPlanetsOwnedBy(empire)
                     && empire.GetFleetsDict().FilterValues(f => f.FleetTask?.TargetPlanet?.ParentSystem == ColonizationTarget.ParentSystem).Length == 0)
            {
                var task = MilitaryTask.CreateGuardTask(empire, ColonizationTarget);
                empire.GetEmpireAI().AddPendingTask(task);
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CheckIfColonizationIsSafe()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (TryGetClaimTask(out MilitaryTask task))
            {
                if (!PositiveEnemyPresence(out float enemyStr) || task.Fleet?.TaskStep == 9)
                    return GoalStep.GoToNextStep;

                if (enemyStr > empire.OffensiveStrength)
                {
                    task.Fleet?.FleetTask.EndTask();
                    task.EndTask();
                    return GoalStep.GoalFailed;
                }

                return GoalStep.TryAgain; // Claim task still in progress
            }

            // Check if there is enemy presence without a claim task
            if (PositiveEnemyPresence(out _) && AIControlsColonization)
            {
                ReleaseShipFromGoal();
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep OrderShipForColonization()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
                return GoalStep.GoToNextStep;

            if (!ShipBuilder.PickColonyShip(empire, out Ship colonyShip))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, colonyShip, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(colonyShip, this, notifyOnEmpty:empire.isPlayer);
            planet.Construction.PrioritizeShip(colonyShip, 1);
            return GoalStep.GoToNextStep;
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
            {
                if (TryGetClaimTask(out MilitaryTask task))
                    task.EndTask();

                return GoalStep.GoalFailed;
            }

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

        GoalStep OrderShipToColonize()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (FinishedShip == null) // @todo This is a workaround for possible safequeue bug causing this to fail on save load
            {
                if (TryGetClaimTask(out MilitaryTask task))
                    task.EndTask();

                return GoalStep.GoalFailed;
            }

            FinishedShip.DoColonize(ColonizationTarget, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (AIControlsColonization 
                && empire.KnownEnemyStrengthIn(ColonizationTarget.ParentSystem) > 10
                && (!TryGetClaimTask(out MilitaryTask task) || task.Fleet?.TaskStep != 7)) // we lost
            {

                ReleaseShipFromGoal();
                task?.EndTask();
                return GoalStep.GoalFailed;
            }

            if (ColonizationTarget.Owner == empire)
            {
                empire.DecreaseFleetStrEmpireMultiplier(TargetEmpire);
                return GoalStep.GoalComplete;
            }

            if (FinishedShip == null 
                || !FinishedShip.AI.FindGoal(ShipAI.Plan.Colonize, out ShipAI.ShipGoal goal)
                || goal.TargetPlanet != ColonizationTarget)
            {
                if (TryGetClaimTask(out MilitaryTask claimTask))
                    claimTask.EndTask();

                return GoalStep.GoalFailed;
            }

            return GoalStep.TryAgain;
        }

        void ReleaseShipFromGoal()
        {
            if (FinishedShip != null)
            {
                FinishedShip.AI.ClearOrdersAndWayPoints(AIState.AwaitingOrders);
                var nearestRallyPoint = empire.FindNearestRallyPoint(FinishedShip.Center);
                if (nearestRallyPoint != null)
                    FinishedShip.AI.OrderOrbitPlanet(nearestRallyPoint);
            }
        }

        bool PositiveEnemyPresence(out float spaceStrength)
        {
            spaceStrength   = empire.KnownEnemyStrengthIn(ColonizationTarget.ParentSystem);
            float groundStr = ColonizationTarget.GetGroundStrengthOther(empire);
            if (ColonizationTarget.Owner?.isFaction  == true && groundStr < 1)
                groundStr += 40; // So AI will know to send fleets to remnant colonies, even if they are empty

            return spaceStrength > 10 || groundStr > 0;
        }

        bool IsPlanetBuildingColonyShip()
        {
            if (PlanetBuildingAt == null)
                return false;

            return PlanetBuildingAt.IsColonyShipInQueue();
        }

        Ship FindIdleColonyShip()
        {
            if (FinishedShip != null)
                return FinishedShip;

            foreach (Ship ship in empire.GetShips())
            {
                if (ship.isColonyShip && ship.AI != null && !ship.AI.FindGoal(ShipAI.Plan.Colonize, out _))
                    return ship;
            }

            return null;
        }

        bool AIControlsColonization => !empire.isPlayer || empire.isPlayer && empire.AutoColonize;

        bool TryGetClaimTask(out MilitaryTask task)
        {
            return empire.GetEmpireAI().GetDefendClaimTaskFor(ColonizationTarget, out task);
        }
    }
}
