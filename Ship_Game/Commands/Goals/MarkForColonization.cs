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
    public class MarkForColonization : FleetGoal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] public sealed override Planet TargetPlanet { get; set; }
        [StarData] public bool IsManualColonizationOrder;
        [StarData] readonly bool CheckNewOwnersInSystem;

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

        public MarkForColonization(Planet toColonize, Empire owner, bool isManual = false) : this(owner)
        {
            TargetPlanet = toColonize;
            IsManualColonizationOrder = isManual;
            if (!owner.isPlayer
                && (TargetPlanet.System.OwnerList.Count == 0
                    || TargetPlanet.System.OwnerList.Count > 0 && TargetPlanet.System.IsExclusivelyOwnedBy(Owner)))
            {
                CheckNewOwnersInSystem = true;
            }

            if (AIControlsColonization && PositiveEnemyPresence(out _)) 
                return;

            // Fast track to colonize if planet is safe and we have a ready Colony Ship
            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
                ChangeToStep(OrderShipToColonize);
            else if (!AIControlsColonization) // skip check safe colonization for player colonization
                ChangeToStep(OrderShipForColonization);
        }

        // Player ordered an existing colony ship to colonize
        public MarkForColonization(Ship colonyShip, Planet toColonize, Empire owner)
            : this(owner)
        {
            TargetPlanet = toColonize;
            FinishedShip = colonyShip;
            IsManualColonizationOrder = true;

            colonyShip.AI.OrderColonization(toColonize);
            ChangeToStep(WaitForColonizationComplete);
        }

        GoalStep CreateClaimTask()
        {
            if (Owner.isPlayer)
                return GoalStep.GoToNextStep;

            if (PositiveEnemyPresence(out float spaceStrength))
            {
                EmpireAI empireAi = Owner.AI;
                TargetEmpire = empireAi.ThreatMatrix.GetStrongestHostileAt(TargetPlanet.System);
                float strMultiplier = Owner.GetFleetStrEmpireMultiplier(TargetEmpire);
                Task = MilitaryTask.CreateClaimTask(Owner, TargetPlanet, 
                    (spaceStrength * strMultiplier).LowerBound(20), TargetEmpire, (int)strMultiplier);

                empireAi.AddPendingTask(Task);
            }
            else if (Owner.Universe.P.Difficulty > GameDifficulty.Normal && !Owner.AnyActiveFleetsTargetingSystem(TargetPlanet.System))
            {
                // This task is independent and not related to this goal Task var
                Owner.AI.AddPendingTask(MilitaryTask.CreateGuardTask(Owner, TargetPlanet));
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep CheckIfColonizationIsSafe()
        {
            if (!PlanetCanBeColonized())
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (Task != null)
            {
                if (Task.Fleet != null && FinishedShip == null)
                {
                    FinishedShip = FindIdleColonyShip();
                    if (FinishedShip != null)
                    {
                        FinishedShip.AI.OrderMoveAndStandByColonize(TargetPlanet, this);
                        // send a scout (even scouts on other missions) to verify str in system before timers run out
                        if (!TargetPlanet.System.HasPlanetsOwnedBy(Owner)
                            && Owner.TryFindClosestScoutTo(TargetPlanet.Position, out Ship scout))
                        {
                            scout.AI.OrderScout(TargetPlanet.System, this);
                        }
                    }
                }
                 
                if (!PositiveEnemyPresence(out float enemyStr) || Task.Fleet != null && Task.Fleet.TaskStep == 9)
                {
                    if (TargetPlanet.Owner != null && TargetPlanet.GetGroundStrength(Owner) == 0) // ground invasion failed
                    {
                        ReleaseColonyShipAndTask();
                        return GoalStep.GoalFailed;
                    }

                     if (FinishedShip?.Active == true)
                    {
                        ChangeToStep(OrderShipToColonize);
                        return GoalStep.TryAgain;
                    }

                    return GoalStep.GoToNextStep;
                }

                if (enemyStr > Owner.OffensiveStrength)
                {
                    ReleaseColonyShipAndTask();
                    return GoalStep.GoalFailed;
                }

                return GoalStep.TryAgain; // Claim task still in progress
            }

            // Check if there is enemy presence without a claim task
            if (PositiveEnemyPresence(out _) && AIControlsColonization)
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep OrderShipForColonization()
        {
            if (!PlanetCanBeColonized())
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (FinishedShip == null) 
                FinishedShip = FindIdleColonyShip();

            if (FinishedShip != null)
                return GoalStep.GoToNextStep;

            if (!ShipBuilder.PickColonyShip(Owner, out IShipDesign colonyShip))
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, colonyShip, out Planet planet, portQuality: 1f))
                return GoalStep.TryAgain;

            PlanetBuildingAt = planet;
            planet.Construction.Enqueue(ship: colonyShip, 
                                        type: Task != null ? QueueItemType.ColonyShipClaim : QueueItemType.ColonyShip, 
                                        goal: this, 
                                        notifyOnEmpty:Owner.isPlayer, 
                                        displayName: $"{colonyShip.Name} ({TargetPlanet.Name})");

            return GoalStep.GoToNextStep;
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (!PlanetCanBeColonized())
            {
                PlanetBuildingAt?.Construction.Cancel(this);
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (FinishedShip == null)
            {
                FinishedShip = FindIdleColonyShip();
                if (FinishedShip != null)
                {
                    PlanetBuildingAt?.Construction.Cancel(this);
                    FinishedShip.OrderColonization(TargetPlanet, this);
                    ChangeToStep(WaitForColonizationComplete);
                    return GoalStep.TryAgain;
                }
            }
            else // we have a ship
            {
                return GoalStep.GoToNextStep;
            }

            if (IsPlanetBuildingColonyShip(out int queueIndex))
            {
                TryRushColonyShip(queueIndex);
            }
            else
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            if (TargetPlanet.Owner == null)
                return GoalStep.TryAgain;

            ReleaseColonyShipAndTask();
            return GoalStep.GoalComplete;
        }

        GoalStep OrderShipToColonize()
        {
            if (!PlanetCanBeColonized())
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (FinishedShip == null) // @todo This is a workaround for possible safequeue bug causing this to fail on save load
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            FinishedShip.OrderColonization(TargetPlanet, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
            if (!PlanetCanBeColonized())
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (AIControlsColonization
                && Owner.KnownEnemyStrengthIn(TargetPlanet.System) > 10
                && ClaimTaskInvalid())
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            if (TargetPlanet.Owner == Owner)
            {
                Owner.DecreaseFleetStrEmpireMultiplier(TargetEmpire);
                ReleaseColonyShipAndTask();
                return GoalStep.GoalComplete;
            }

            if (FinishedShip == null 
                || !FinishedShip.AI.FindGoal(ShipAI.Plan.Colonize, out ShipAI.ShipGoal goal)
                || goal.TargetPlanet != TargetPlanet)
            {
                ReleaseColonyShipAndTask();
                return GoalStep.GoalFailed;
            }

            return GoalStep.TryAgain;
        }

        bool PlanetCanBeColonized()
        {
            if (!Owner.isPlayer && (PlanetRanker.IsColonizeBlockedByMorals(TargetPlanet.System, Owner)
                                   || TargetPlanet.Owner?.ParentEmpire != null && !Owner.IsAtWarWith(TargetPlanet.Owner.ParentEmpire)))
            {
                return false;
            }

            if (CheckNewOwnersInSystem
                && TargetPlanet.System.OwnerList.Count > 0
                && !TargetPlanet.System.IsExclusivelyOwnedBy(Owner))
            {
                // Someone got planets in that system, need to check if we warned them
                foreach (Relationship rel in Owner.AllRelations)
                    Owner.AI.ExpansionAI.CheckClaim(rel.Them, rel, TargetPlanet);
            }

            if (TargetPlanet.Owner != null && TargetPlanet.Owner != Owner)
            {
                if (TargetPlanet.Owner.IsFaction)
                    return true;

                Log.Info($"Colonize: {TargetPlanet.Owner.Name} got there first");
                return false;
            }

            return true;
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
            spaceStrength = Owner.KnownEnemyStrengthIn(TargetPlanet.System);
            float groundStr = TargetPlanet.GetGroundStrengthOther(Owner);
            if (TargetPlanet.Owner?.IsFaction  == true && groundStr < 1)
                groundStr += 40; // So AI will know to send fleets to remnant colonies, even if they are empty

            return spaceStrength > 10 || groundStr > 0;
        }

        bool IsPlanetBuildingColonyShip(out int queueIndex)
        {
            queueIndex = 0;
            if (PlanetBuildingAt == null)
                return false;

            return PlanetBuildingAt.IsColonyShipInQueue(this, out queueIndex);
        }

        Ship FindIdleColonyShip()
        {
            foreach (Ship ship in Owner.OwnedShips)
            {
                if (ship.ShipData.IsColonyShip && !ship.DoingRefit
                    && ship.AI != null && ship.Active && !ship.AI.FindGoal(ShipAI.Plan.Colonize, out _)
                    && NotAssignedToColonizationGoal(ship))
                {
                    return ship;
                }
            }

            return null;
        }

        void ReleaseColonyShipAndTask()
        {
            Task?.EndTask();
            ReleaseShipFromGoal();
        }

        bool AIControlsColonization => !Owner.isPlayer || (Owner.isPlayer && Owner.AutoColonize && !IsManualColonizationOrder);

        // Checks if the ship is not taken by another colonization goal
        bool NotAssignedToColonizationGoal(Ship colonyShip)
        {
            return !colonyShip.Loyalty.AI.HasGoal(g => g.Type == GoalType.MarkForColonization && g.FinishedShip == colonyShip);
        }

        bool ClaimTaskInvalid()
        {
            return Task?.Fleet != null && LifeTime > 5 // Timeout
                || Task?.Fleet?.TaskStep != 7; // we lost
        }

        void TryRushColonyShip(int queueIndex)
        {
            if (Owner.isPlayer || queueIndex != 0 || !Owner.AI.SafeToRushColonyShips)
                return;

            float rush = 50f.UpperBound(PlanetBuildingAt.ProdHere);
            PlanetBuildingAt.Construction.RushProduction(0, rush);
        }
    }
}
