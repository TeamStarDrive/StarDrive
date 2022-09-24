using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class RefitOrbital : Goal
    {
        [StarDataConstructor]
        public RefitOrbital(Empire owner) : base(GoalType.RefitOrbital, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindOrbitalAndPlanetToRefit,
                BuildNewShip,
                WaitForShipBuilt,
                OrderDeployOrbital,
                WaitForDeployment
            };
        }

        public RefitOrbital(Ship oldShip, string toBuildName, Empire owner) : this(owner)
        {
            OldShip = oldShip;
            ToBuildUID = toBuildName;
        }

        GoalStep FindOrbitalAndPlanetToRefit()
        {
            if (ToBuildUID == null || !GetNewOrbital(out Ship newOrbital))
                return GoalStep.GoalFailed;  // No better orbital is available

            if (OldShip.AI.State == AIState.Refit)
                RemoveOldRefitGoal();

            if (!Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, OldShip.RefitCost(newOrbital), newOrbital, out PlanetBuildingAt))
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit
            }

            OldShip.ClearFleet(returnToManagedPools: false, clearOrders: true);
            OldShip.AI.State = AIState.Refit;
            Planet targetPlanet = OldShip.GetTether();
            if (targetPlanet != null)
                TetherPlanet = targetPlanet;

            return GoalStep.GoToNextStep;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            if (ToBuildUID == null || !GetNewOrbital(out Ship newOrbital))
                return GoalStep.GoalFailed;  // No better orbital is available

            string constructorId = Owner.data.ConstructorShip;
            if (!ResourceManager.Ships.GetDesign(constructorId, out ShipToBuild))
            {
                if (!ResourceManager.Ships.GetDesign(Owner.data.DefaultConstructor, out ShipToBuild))
                {
                    Log.Error($"BuildOrbital: no construction ship with uid={constructorId}");
                    return GoalStep.GoalFailed;
                }
            }

            PlanetBuildingAt.Construction.Enqueue(newOrbital, ShipToBuild, OldShip.RefitCost(newOrbital), this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeployOrbital()
        {
            if (FinishedShip == null || !FinishedShip.Active)
                return GoalStep.GoalFailed; // Ship was removed or destroyed

            if (!OldShipOnPlan)
            {
                FinishedShip.AI.OrderScrapShip();
                return GoalStep.GoalFailed; // Ship was removed or destroyed
            }

            Planet targetPlanet = OldShip.GetTether();
            if (targetPlanet != null)
            {
                Vector2 dirToOrbital = targetPlanet.Position.DirectionToTarget(OldShip.Position);
                float disToOrbital   = targetPlanet.Position.Distance(OldShip.Position);
                TetherOffset         = dirToOrbital.Normalized() * disToOrbital;
            }
            else
            {
                BuildPosition = OldShip.Position;
            }

            FinishedShip.AI.OrderDeepSpaceBuild(this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            if (FinishedShip != null)
                return GoalStep.TryAgain;

            if (OldShip is { Active: true })
                OldShip.AI.ClearOrders(); // Constructor was maybe destroyed

            return GoalStep.GoalComplete;
        }

        bool OldShipOnPlan
        {
            get
            {
                if (OldShip == null)
                    return false; // Ship was removed from game, probably destroyed

                return OldShip.DoingRefit;
            }
        }

        void RemoveOldRefitGoal()
        {
            if (OldShip.AI.FindGoal(ShipAI.Plan.Refit, out _))
                OldShip.Loyalty.AI.FindAndRemoveGoal(GoalType.Refit, g => g.OldShip == OldShip);
        }

        bool GetNewOrbital(out Ship newOrbital)
        {
            newOrbital = ResourceManager.GetShipTemplate(ToBuildUID, false);
            return newOrbital != null;
        }
    }
}
