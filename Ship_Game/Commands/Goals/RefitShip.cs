using System;
using Ship_Game.AI;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.Data.Serialization;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    public class RefitShip : FleetGoal
    {
        [StarData] string VanityName;
        [StarData] int ShipLevel;
        [StarData] public bool Rush { get; set; }
        [StarData] public sealed override BuildableShip Build { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override Ship OldShip { get; set; }

        public override IShipDesign ToBuild => Build.Template;
        public override bool IsRefitGoalAtPlanet(Planet planet) => PlanetBuildingAt == planet;

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

        public RefitShip(Ship oldShip, IShipDesign design, Empire owner, bool rush = false) : this(owner)
        {
            Build = new(design);

            OldShip = oldShip;
            ShipLevel = oldShip.Level;
            Fleet = oldShip.Fleet;
            Rush = rush;
            if (oldShip.VanityName != oldShip.Name)
                VanityName = oldShip.VanityName;

            if (OldShip.AI.State == AIState.Refit)
                RemoveOldRefitGoal();
        }

        GoalStep FindShipAndPlanetToRefit()
        {
            if (!Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, OldShip.RefitCost(Build.Template), 
                OldShip, Build.Template, OldShip.Fleet != null, out Planet refitPlanet))
            {
                OldShip.AI.ClearOrders();
                return GoalStep.GoalFailed;  // No planet to refit
            }

            PlanetBuildingAt = refitPlanet;
            
            if (Fleet != null)
            {
                if (Fleet.FindShipNode(OldShip, out FleetDataNode node))
                {
                    Fleet.AssignGoal(node, this);
                    Fleet.AssignShipName(node, Build.Template.Name);
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

            var qi = new QueueItem(PlanetBuildingAt)
            {
                ShipData        = Build.Template,
                Cost            = OldShip.RefitCost(Build.Template),
                Goal            = this,
                isShip          = true,
                Rush            = Rush || OldShip.Loyalty.RushAllConstruction,
                TradeRoutes     = OldShip.TradeRoutes,
                AreaOfOperation = OldShip.AreaOfOperation,
                QType           = OldShip.IsFreighter ? QueueItemType.Freighter : QueueItemType.CombatShip,
                TransportingColonists  = OldShip.TransportingColonists,
                TransportingFood       = OldShip.TransportingFood,
                TransportingProduction = OldShip.TransportingProduction,
                AllowInterEmpireTrade  = OldShip.AllowInterEmpireTrade
            };

            PlanetBuildingAt.Construction.EnqueueRefitShip(qi);
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

                    FinishedShip.RelativeFleetOffset = node.RelativeFleetOffset;
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
                OldShip.Loyalty.AI.FindAndRemoveGoal(GoalType.Refit, g => g.OldShip == OldShip);
        }
    }
}
