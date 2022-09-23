using System;
using Ship_Game.AI;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.Data.Serialization;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    public class RefitShip : Goal
    {
        [StarData] string VanityName;
        [StarData] int ShipLevel;

        [StarDataConstructor]
        public RefitShip(Empire owner) : base(GoalType.Refit, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindShipAndPlanetToRefit,
                WaitForOldShipAtPlanet,
                BuildNewShip,
                WaitForShipBuilt,
                AddShipDataAndFleet
            };
        }

        public RefitShip(Ship oldShip, string toBuildName, Empire owner) : this(owner)
        {
            OldShip     = oldShip;
            ShipLevel   = oldShip.Level;
            ToBuildUID  = toBuildName;
            Fleet       = oldShip.Fleet;
            if (oldShip.VanityName != oldShip.Name)
                VanityName = oldShip.VanityName;

            Evaluate();
        }

        GoalStep FindShipAndPlanetToRefit()
        {
            if (ToBuildUID == null || !GetNewShip(out Ship newShip))
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;  // No better ship is available
            }

            if (OldShip.AI.State == AIState.Refit)
                RemoveOldRefitGoal();

            if (!Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, OldShip.RefitCost(newShip), 
                OldShip, newShip, OldShip.Fleet != null, out PlanetBuildingAt))
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit
            }
            
            if (Fleet != null)
            {
                if (Fleet.FindShipNode(OldShip, out FleetDataNode node))
                {
                    Fleet.AssignGoal(node, this);
                    Fleet.AssignShipName(node, ToBuildUID);
                }
            }

            OldShip.ClearFleet(returnToManagedPools: false, clearOrders: true);
            OldShip.AI.OrderRefitTo(PlanetBuildingAt, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForOldShipAtPlanet()
        {
            if (!OldShipOnPlan)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            if (OldShip.Position.InRadius(PlanetBuildingAt.Position, PlanetBuildingAt.Radius + 300f))
                return GoalStep.GoToNextStep;

            return GoalStep.TryAgain;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipWaitingForRefit)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            if (!GetNewShip(out Ship newShip))
                return GoalStep.GoalFailed;  // Could not find ship to build in ship dictionary

            var qi = new QueueItem(PlanetBuildingAt)
            {
                ShipData        = newShip.ShipData,
                Cost            = OldShip.RefitCost(newShip),
                Goal            = this,
                isShip          = true,
                TradeRoutes     = OldShip.TradeRoutes,
                AreaOfOperation = OldShip.AreaOfOperation,
                TransportingColonists  = OldShip.TransportingColonists,
                TransportingFood       = OldShip.TransportingFood,
                TransportingProduction = OldShip.TransportingProduction,
                AllowInterEmpireTrade  = OldShip.AllowInterEmpireTrade
            };

            PlanetBuildingAt.Construction.Enqueue(qi);
            OldShip.QueueTotalRemoval();
            OldShip = null; // clean up dangling reference to avoid serializing it
            return GoalStep.GoToNextStep;
        }

        GoalStep AddShipDataAndFleet()
        {
            if (FinishedShip == null)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            if (VanityName != null)
                FinishedShip.VanityName = VanityName;

            FinishedShip.Level = ShipLevel;
            if (Fleet != null)
            {
                if (Fleet.FindNodeWithGoal(this, out FleetDataNode node))
                {
                    Fleet.AddExistingShip(FinishedShip, node);
                    Fleet.RemoveGoalFromNode(node);
                    if (Fleet.Ships.Count == 0)
                        Fleet.FinalPosition = FinishedShip.Position + RandomMath.Vector2D(3000f);

                    if (Fleet.FinalPosition == Vector2.Zero)
                        Fleet.FinalPosition = Owner.FindNearestRallyPoint(FinishedShip.Position).Position;

                    FinishedShip.RelativeFleetOffset = node.FleetOffset;
                    FinishedShip.AI.OrderMoveTo(Fleet.GetFinalPos(FinishedShip), Fleet.FinalDirection, AIState.AwaitingOrders);
                }
            }

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

        bool OldShipWaitingForRefit
        {
            get
            {
                if (OldShip == null)
                    return false; // Ship was removed from game, probably destroyed

                return OldShip.AI.State == AIState.HoldPosition || OldShip.DoingRefit;
            }
        }

        void RemoveGoalFromFleet()
        {
            Fleet?.RemoveGoal(this);
        }

        void RemoveOldRefitGoal()
        {
            if (OldShip.AI.FindGoal(ShipAI.Plan.Refit, out ShipAI.ShipGoal shipGoal))
                OldShip.Loyalty.GetEmpireAI().FindAndRemoveGoal(GoalType.Refit, g => g.OldShip == OldShip);
        }

        bool GetNewShip(out Ship newShip)
        {
            newShip = ResourceManager.GetShipTemplate(ToBuildUID, false);
            return newShip != null;
        }
    }
}
