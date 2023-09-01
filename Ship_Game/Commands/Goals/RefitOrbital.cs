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
        [StarData] public bool Rush { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override Ship OldShip { get; set; }
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

        public RefitOrbital(Ship oldShip, IShipDesign toBuild, Empire owner, bool rush = false) : this(owner)
        {
            OldShip = oldShip;
            Planet targetPlanet = oldShip.GetTether();
            Rush= rush;
            if (targetPlanet != null)
                Initialize(toBuild.Name, Vector2.Zero, targetPlanet, Vector2.Zero);
            else
                Initialize(toBuild.Name, OldShip.Position, OldShip.System);
        }

        GoalStep FindOrbitalAndPlanetToRefit()
        {
            if (OldShip.AI.State == AIState.Refit)
                RemoveOldRefitGoal();

            if (!Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, OldShip.RefitCost(ToBuild), ToBuild, out Planet buildAt))
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit
            }

            PlanetBuildingAt = buildAt;
            OldShip.ClearFleet(returnToManagedPools: false, clearOrders: true);
            OldShip.AI.ChangeAIState(AIState.Refit);
            return GoalStep.GoToNextStep;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipOnPlan)
                return GoalStep.GoalFailed;

            float refitCost = OldShip.RefitCost(ToBuild);
            IShipDesign constructor = BuildableShip.GetConstructor(Owner, OldShip.System, refitCost);
            PlanetBuildingAt.Construction.Enqueue(ToBuild, constructor, refitCost, this, Rush);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeployOrbital()
        {
            if (!ConstructionShipOk)
                return GoalStep.GoalFailed; // Ship was removed or destroyed

            if (!OldShipOnPlan)
            {
                FinishedShip.AI.OrderScrapShip();
                return GoalStep.GoalFailed; // Ship was removed or destroyed
            }

            Planet targetPlanet = OldShip.GetTether();
            if (TetherPlanet != null)
            {
                Vector2 dirToOrbital = targetPlanet.Position.DirectionToTarget(OldShip.Position);
                float disToOrbital = targetPlanet.Position.Distance(OldShip.Position);
                TetherOffset = dirToOrbital.Normalized() * disToOrbital;
            }
            else
            {
                StaticBuildPos = OldShip.Position;
            }

            FinishedShip.AI.OrderDeepSpaceBuild(this, OldShip.RefitCost(ToBuild), ToBuild.Grid.Radius);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            if (!ConstructionShipOk)
            {
                if (OldShip is { Active: true })
                {
                    OldShip.AI.ClearOrders(); // Constructor was maybe destroyed
                    return GoalStep.GoalFailed;
                }

                return GoalStep.GoalComplete;
            }

            if (FinishedShip.Construction.TryConstruct(BuildPosition) && FinishedShip.System != null)
                FinishedShip.System.TryLaunchBuilderShip(FinishedShip, Owner);

            return GoalStep.TryAgain;
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

        bool ConstructionShipOk => FinishedShip?.Active == true;
    }
}
