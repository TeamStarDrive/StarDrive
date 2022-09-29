using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class MarkForColonization : Goal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] public sealed override Planet TargetPlanet { get; set; }

        public override bool IsColonizationGoal(Planet planet) => TargetPlanet == planet;

        [StarDataConstructor]
        public MarkForColonization(Empire owner) : base(GoalType.MarkForColonization, owner)
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

        public MarkForColonization(Planet toColonize, Empire owner) : this(owner)
        {
            TargetPlanet = toColonize;
            if (PositiveEnemyPresence(out _) && AIControlsColonization) 
                return;

            // Fast track to colonize if planet is safe and we have a ready Colony Ship
            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
            {
                ChangeToStep(OrderShipToColonize);
            }

            if (!AIControlsColonization) // Fast track for player colonization
                ChangeToStep(OrderShipForColonization);
        }

        // Player ordered an existing colony ship to colonize
        public MarkForColonization(Ship colonyShip, Planet toColonize, Empire owner)
            : this(owner)
        {
            TargetPlanet = toColonize;
            FinishedShip = colonyShip;
            ChangeToStep(WaitForColonizationComplete);
        }

        GoalStep TargetPlanetStatus()
        {
            if (!Owner.isPlayer && PlanetRanker.IsColonizeBlockedByMorals(TargetPlanet.ParentSystem, Owner))
            {
                ReleaseShipFromGoal();
                return GoalStep.GoalFailed;
            }

            if (TargetPlanet.ParentSystem.OwnerList.Count > 0
                && !TargetPlanet.ParentSystem.IsExclusivelyOwnedBy(Owner))
            {
                // Someone got planets in that system, need to check if we warned them
                foreach (Relationship rel in Owner.AllRelations)
                    Owner.AI.ExpansionAI.CheckClaim(rel.Them, rel, TargetPlanet);
            }

            if (TargetPlanet.Owner != null)
            {
                if (TargetPlanet.Owner == Owner)
                    return GoalStep.GoalComplete;

                // If the owner is a faction, fail the goal so next time we also get a claim fleet to invade
                if (TargetPlanet.Owner.IsFaction) 
                    return FinishedShip != null ? GoalStep.GoalFailed : GoalStep.GoToNextStep;

                ReleaseShipFromGoal();
                Log.Info($"Colonize: {TargetPlanet.Owner.Name} got there first");
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CreateClaimTask()
        {
            if (Owner.isPlayer)
                return GoalStep.GoToNextStep;

            if (PositiveEnemyPresence(out float spaceStrength))
            {
                EmpireAI empireAi   = Owner.AI;
                TargetEmpire        = empireAi.ThreatMatrix.GetDominantEmpireInSystem(TargetPlanet.ParentSystem);
                float strMultiplier = Owner.GetFleetStrEmpireMultiplier(TargetEmpire);
                var task            = MilitaryTask.CreateClaimTask(Owner, TargetPlanet, 
                                       (spaceStrength * strMultiplier).LowerBound(20), TargetEmpire, (int)strMultiplier);

                empireAi.AddPendingTask(task);
                empireAi.AddGoal(new StandbyColonyShip(Owner));
            }
            else if (Owner.Fleets.Filter(f => f.FleetTask?.TargetPlanet?.ParentSystem == TargetPlanet.ParentSystem).Length == 0)
            {
                var task = MilitaryTask.CreateGuardTask(Owner, TargetPlanet);
                Owner.AI.AddPendingTask(task);
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CheckIfColonizationIsSafe()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (TryGetClaimTask(out MilitaryTask task))
            {
                if (!PositiveEnemyPresence(out float enemyStr) || task.Fleet != null && task.Fleet.TaskStep == 9)
                    return GoalStep.GoToNextStep;

                if (enemyStr > Owner.OffensiveStrength)
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

            if (!ShipBuilder.PickColonyShip(Owner, out IShipDesign colonyShip))
                return GoalStep.GoalFailed;

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, colonyShip, out Planet planet))
                return GoalStep.TryAgain;

            PlanetBuildingAt = planet;
            planet.Construction.Enqueue(colonyShip, this,
                                        notifyOnEmpty:Owner.isPlayer,
                                        displayName: $"{colonyShip.Name} ({TargetPlanet.Name})");

            planet.Construction.PrioritizeShip(colonyShip, 1);
            return GoalStep.GoToNextStep;
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
            {
                if (TryGetClaimTask(out MilitaryTask task))
                    task.EndTask();

                PlanetBuildingAt?.Construction.Cancel(this);
                return GoalStep.GoalFailed;
            }

            if (FinishedShip != null) // we already have a ship
                return GoalStep.GoToNextStep;

            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            if (TargetPlanet.Owner == null)
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

            FinishedShip.DoColonize(TargetPlanet, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
            if (TargetPlanetStatus() == GoalStep.GoalFailed)
                return GoalStep.GoalFailed;

            if (AIControlsColonization
                && Owner.KnownEnemyStrengthIn(TargetPlanet.ParentSystem) > 10
                && ClaimTaskInvalid(out MilitaryTask possibleTask))
            {
                ReleaseShipFromGoal();
                possibleTask?.EndTask();
                return GoalStep.GoalFailed;
            }

            if (TargetPlanet.Owner == Owner)
            {
                Owner.DecreaseFleetStrEmpireMultiplier(TargetEmpire);
                return GoalStep.GoalComplete;
            }

            if (FinishedShip == null 
                || !FinishedShip.AI.FindGoal(ShipAI.Plan.Colonize, out ShipAI.ShipGoal goal)
                || goal.TargetPlanet != TargetPlanet)
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
                var nearestRallyPoint = Owner.FindNearestRallyPoint(FinishedShip.Position);
                if (nearestRallyPoint != null)
                    FinishedShip.AI.OrderOrbitPlanet(nearestRallyPoint, clearOrders: true);
            }
        }

        bool PositiveEnemyPresence(out float spaceStrength)
        {
            spaceStrength   = Owner.KnownEnemyStrengthIn(TargetPlanet.ParentSystem);
            float groundStr = TargetPlanet.GetGroundStrengthOther(Owner);
            if (TargetPlanet.Owner?.IsFaction  == true && groundStr < 1)
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

            foreach (Ship ship in Owner.OwnedShips)
            {
                if (ship.ShipData.IsColonyShip && !ship.DoingRefit
                    && ship.AI != null && !ship.AI.FindGoal(ShipAI.Plan.Colonize, out _)
                    && NotAssignedToColonizationGoal(ship))
                {
                    return ship;
                }
            }

            return null;
        }

        bool AIControlsColonization => !Owner.isPlayer || Owner.isPlayer && Owner.AutoColonize;

        bool TryGetClaimTask(out MilitaryTask task)
        {
            return Owner.AI.GetDefendClaimTaskFor(TargetPlanet, out task);
        }

        // Checks if the ship is not taken by another colonization goal
        bool NotAssignedToColonizationGoal(Ship colonyShip)
        {
            return !colonyShip.Loyalty.AI.HasGoal(g => g.Type == GoalType.MarkForColonization && g.FinishedShip == colonyShip);
        }

        bool ClaimTaskInvalid(out MilitaryTask possibleTask)
        {
            return !TryGetClaimTask(out possibleTask) 
                || possibleTask.Fleet != null && LifeTime > 5 // Timeout
                || possibleTask.Fleet?.TaskStep != 7; // we lost
        }
    }
}
