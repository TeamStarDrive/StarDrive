using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class RefitShip : Goal
    {
        public const string ID = "RefitShips";
        public override string UID => ID;

        public RefitShip() : base(GoalType.Refit)
        {
            Steps = new Func<GoalStep>[]
            {
                FindShipAndPlanetToRefit,
                WaitForOldShipAtPlanet,
                BuildNewShip,
                WaitMainGoalCompletion,
                AddShipDataAndFleet
            };
        }

        public RefitShip(Ship oldShip, string toBuildName, Empire owner) : this()
        {
            OldShip     = oldShip;
            ShipLevel   = oldShip.Level;
            ToBuildUID  = toBuildName;
            Fleet       = oldShip.fleet;
            empire      = owner;
            if (oldShip.VanityName != oldShip.Name)
                VanityName = oldShip.VanityName;

            Evaluate();
        }

        GoalStep FindShipAndPlanetToRefit()
        {
            if (ToBuildUID == null)
                return GoalStep.GoalFailed;  // No better ship is available

            PlanetBuildingAt = empire.RallyShipYardNearestTo(OldShip.Center);
            if (PlanetBuildingAt == null)
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit the ship was found
            }

            if (Fleet != null)
            {
                if (Fleet.FindShipNode(OldShip, out FleetDataNode node))
                {
                    Fleet.AssignGoalGuid(node, guid);
                    Fleet.AssignShipName(node, ToBuildUID);
                }
            }

            OldShip.ClearFleet();
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

            if (OldShip.Center.InRadius(PlanetBuildingAt.Center, PlanetBuildingAt.ObjectRadius + 150f))
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

            Ship newShip = ResourceManager.GetShipTemplate(ToBuildUID, false);
            if (newShip == null)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;  // Could not find ship to build in ship dictionary
            }

            var qi = new QueueItem(PlanetBuildingAt)
            {
                sData           = newShip.shipData,
                Cost            = OldShip.RefitCost(newShip),
                Goal            = this,
                isShip          = true,
                isRefit         = true,
                TradeRoutes     = OldShip.TradeRoutes,
                AreaOfOperation = OldShip.AreaOfOperation,
                TransportingColonists  = OldShip.TransportingColonists,
                TransportingFood       = OldShip.TransportingFood,
                TransportingProduction = OldShip.TransportingProduction,
                AllowInterEmpireTrade  = OldShip.AllowInterEmpireTrade
            };

            PlanetBuildingAt.ConstructionQueue.Add(qi);
            OldShip.QueueTotalRemoval();
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
                if (Fleet.FindNodeWithGoalGuid(guid, out FleetDataNode node))
                {
                    Fleet.AddExistingShip(FinishedShip, node);
                    Fleet.RemoveGoalGuid(node);
                    if (Fleet.Ships.Count == 0)
                        Fleet.Position = FinishedShip.Position + RandomMath.Vector2D(3000f);

                    if (Fleet.Position == Vector2.Zero)
                        Fleet.Position = empire.FindNearestRallyPoint(FinishedShip.Center).Center;

                    FinishedShip.RelativeFleetOffset = node.FleetOffset;
                    FinishedShip.AI.OrderMoveTowardsPosition(Fleet.Position + FinishedShip.RelativeFleetOffset, Fleet.Direction, true, null);
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
            Fleet?.RemoveGoalGuid(guid);
        }
    }
}
