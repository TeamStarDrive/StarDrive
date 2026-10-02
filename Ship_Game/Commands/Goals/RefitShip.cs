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
        [StarData] QueueItem RefitItem;

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
                WaitForRefitBuilt,
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

            if (OldShip.AI.State == AIState.Refit && !OldShip.IsLanding)
            {
                InheritFleetFromOldRefitGoal();
                RemoveOldRefitGoal();
            }
        }

        // a ship already refitting was detached from its fleet by the first refit goal,
        // so take over that goal's fleet and its reserved fleet node before removing it
        void InheritFleetFromOldRefitGoal()
        {
            if (Fleet == null
                && Owner.AI.FindGoal(g => g != null && g.Type == GoalType.Refit && g.OldShip == OldShip) is FleetGoal prior
                && prior.Fleet != null)
            {
                Fleet = prior.Fleet;
                if (Fleet.FindNodeWithGoal(prior, out FleetDataNode node))
                {
                    Fleet.AssignGoal(node, this);
                    Fleet.AssignShipName(node, Build.Template.Name);
                }
            }
        }

        GoalStep FindShipAndPlanetToRefit()
        {
            if (OldShip.LandShip is { Trades: true } trading)
            {
                if (!trading.Docked)
                    return GoalStep.TryAgain;
                OldShip.TakeOffAfterTrading();
            }

            if (OldShip.IsLanding)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            if (!FindPortToRefitAt(travelBack: OldShip.Fleet != null))
            {
                OldShip.AI.ClearOrders();
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;  // No planet to refit
            }

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

        bool FindPortToRefitAt(bool travelBack)
        {
            if (!Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, OldShip.RefitCost(Build.Template),
                OldShip, Build.Template, travelBack, out Planet refitPlanet))
            {
                return false;
            }

            PlanetBuildingAt = refitPlanet;
            return true;
        }

        GoalStep SendToAnotherPort()
        {
            if (OldShip.IsLanding)
                OldShip.TakeOffAfterLanding();

            if (!FindPortToRefitAt(travelBack: Fleet != null))
            {
                OldShip.AI.ClearOrders();
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            OldShip.AI.OrderRefitTo(PlanetBuildingAt, this);
            ChangeToStep(WaitForOldShipAtPlanet);
            return GoalStep.TryAgain;
        }

        GoalStep WaitForOldShipAtPlanet()
        {
            if (!OldShipOnPlan)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            if (OldShip.IsPlatformOrStation)
            {
                return OldShip.Position.InRadius(PlanetBuildingAt.Position, PlanetBuildingAt.Radius + 300f)
                    ? GoalStep.GoToNextStep
                    : GoalStep.TryAgain;
            }

            if (PlanetBuildingAt.Owner != Owner && !OldShip.IsLanding)
                return SendToAnotherPort();

            if (OldShip.IsLanding || OldShip.AI.TryLand(LandPlan.Refit, PlanetBuildingAt, PlanetBuildingAt.FindShipyardToLandOn(OldShip)))
                return GoalStep.GoToNextStep;

            if (!OldShip.AI.FindGoal(ShipAI.Plan.Refit, out _))
                OldShip.AI.OrderRefitTo(PlanetBuildingAt, this);

            return GoalStep.TryAgain;
        }

        GoalStep BuildNewShip()
        {
            if (!OldShipWaitingForRefit)
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            if (OldShip.LandShip is { Done: false })
                return GoalStep.TryAgain;

            if (PlanetBuildingAt.Owner != Owner)
            {
                if (!OldShip.IsPlatformOrStation)
                    return SendToAnotherPort();

                OldShip.AI.OrderAwaitOrders();
                RemoveGoalFromFleet(); // do not strand the fleet node on a dead goal
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
                AllowInterEmpireTrade  = OldShip.AllowInterEmpireTrade,
                LaunchShipyard         = OldShip.LandShip?.Shipyard,
                LaunchFromPlanet       = OldShip.LandShip is { Shipyard: null }
            };

            OldShip.QueueTotalRemoval();
            OldShip = null; // clean up dangling reference to avoid serializing it
            RefitItem = qi;
            PlanetBuildingAt.Construction.EnqueueRefitShip(qi);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForRefitBuilt()
        {
            if (FinishedShip == null && PlanetBuildingAt.Owner != Owner)
                return QueueAtAnotherPort();

            return WaitForShipBuilt();
        }

        GoalStep QueueAtAnotherPort()
        {
            if (RefitItem == null || !Owner.FindPlanetToRefitAt(Owner.SafeSpacePorts, RefitItem.Cost, Build.Template, out Planet port))
            {
                RemoveGoalFromFleet();
                return GoalStep.GoalFailed;
            }

            PlanetBuildingAt = port;
            RefitItem.Planet = port;
            RefitItem.ProductionSpent = 0;
            RefitItem.LaunchShipyard = null;
            RefitItem.LaunchFromPlanet = false;
            port.Construction.EnqueueRefitShip(RefitItem);
            return GoalStep.TryAgain;
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
                        Fleet.FinalPosition = FinishedShip.Position + Owner.Random.Vector2D(3000f);

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
