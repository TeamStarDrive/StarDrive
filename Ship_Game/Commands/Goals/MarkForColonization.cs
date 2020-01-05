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
            if (FinishedShip == null)
                return false;

            if (ColonizationTarget.Owner != null)
            {
                foreach (var relationship in empire.AllRelations)
                    empire.GetEmpireAI().ExpansionAI.CheckClaim(relationship.Key, relationship.Value, ColonizationTarget);
                
                var planet = empire.FindNearestRallyPoint(FinishedShip.Center);
                if (planet != null)
                {
                    FinishedShip?.AI.OrderRebase(planet, true);
                }
                return false;
            }

            var system = ColonizationTarget.ParentSystem;
            float str = empire.GetEmpireAI().ThreatMatrix.PingNetRadarStr(system.Position, system.Radius, empire);
            if (str > 50) 
                return false;

            if (ColonizationTarget.Owner == null)
                return true;


            FinishedShip.AI.State = AIState.AwaitingOrders;
            return false;
        }

        // @return TRUE bad guys
        bool TargetTooStrong()
        {
            if (ColonizationTarget.ParentSystem.ShipList.IsEmpty)
                return false; //cheap shortcut
            float enemyStr = empire.GetEmpireAI().ThreatMatrix.PingRadarStr(ColonizationTarget.Center
                , ColonizationTarget.ParentSystem.Radius, empire, true);
            if (enemyStr > 50)
            {
                Log.Warning($"System {ColonizationTarget.ParentSystem} had enemies cancelling goal");
                return true;
            }
            WaitingForEscort = false;
            return false;
        }

        GoalStep OrderShipForColonization()
        {
            if (!IsValid())
                return GoalStep.GoalComplete;

            if (TargetTooStrong())
                return GoalStep.GoalFailed;

            FinishedShip = FindIdleColonyShip();
            if (FinishedShip != null)
                return GoalStep.GoToNextStep;

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
            if (FinishedShip != null) // we already have a ship
                return GoalStep.GoToNextStep;

            if (!IsValid())
                return GoalStep.GoalComplete;

            if (TargetTooStrong())
                return GoalStep.GoalFailed;

            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            if (ColonizationTarget.Owner == null)
                return GoalStep.TryAgain;

            foreach (KeyValuePair<Empire, Relationship> them in empire.AllRelations)
                empire.GetEmpireAI().ExpansionAI.CheckClaim(them.Key, them.Value, ColonizationTarget);

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

            if (FinishedShip == null) // @todo This is a workaround for possible safequeue bug causing this to fail on save load
                return GoalStep.RestartGoal;

            FinishedShip.DoColonize(ColonizationTarget, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForColonizationComplete()
        {
             if (!IsValid())
                return GoalStep.GoalFailed;

             if (TargetTooStrong())
                 return GoalStep.GoalFailed;
            if (FinishedShip == null)                      return GoalStep.RestartGoal;
            if (FinishedShip.AI.State != AIState.Colonize) return GoalStep.RestartGoal;
            if (ColonizationTarget.Owner == null)          return GoalStep.TryAgain;

            return GoalStep.GoalComplete;
        }
    }
}
