using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class RefitOrbital : DeepSpaceBuildGoal
    {
        [StarData] public override Ship OldShip { get; set; }
        public override bool IsRefitGoalAtPlanet(Planet planet) => PlanetBuildingAt == planet;

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
            Build = new(toBuildName);
            Planet targetPlanet = oldShip.GetTether();
            if (targetPlanet != null)
                Build.TetherPlanet = targetPlanet;
        }

        GoalStep FindOrbitalAndPlanetToRefit()
        {
            if (OldShip.AI.State == AIState.Refit)
                RemoveOldRefitGoal();

            if (!Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, 
                OldShip.RefitCost(Build.Template), Build.Template, out PlanetBuildingAt))
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit
            }

            OldShip.ClearFleet(returnToManagedPools: false, clearOrders: true);
            OldShip.AI.State = AIState.Refit;
            return GoalStep.GoToNextStep;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            IShipDesign constructor = BuildableShip.GetConstructor(Owner);
            PlanetBuildingAt.Construction.Enqueue(Build.Template, constructor, OldShip.RefitCost(Build.Template), this);
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
            if (Build.TetherPlanet != null)
            {
                Vector2 dirToOrbital = targetPlanet.Position.DirectionToTarget(OldShip.Position);
                float disToOrbital = targetPlanet.Position.Distance(OldShip.Position);
                Build.TetherOffset = dirToOrbital.Normalized() * disToOrbital;
            }
            else
            {
                Build.StaticBuildPos = OldShip.Position;
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
    }
}
