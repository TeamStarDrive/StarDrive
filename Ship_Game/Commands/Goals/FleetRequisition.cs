using System;
using System.Linq;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class FleetRequisition : Goal
    {
        public const string ID = "FleetRequisition";
        public override string UID => ID;

        public FleetRequisition() : base(GoalType.FleetRequisition)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetForFleetRequisition,
                DummyStepTryAgain,
                DoSomeStuffWithFleets
            };
        }
        public FleetRequisition(ShipAI.ShipGoal goal, ShipAI ai) : this()
        {
            FleetDataNode node = ai.Owner.fleet.DataNodes.First(thenode => thenode.Ship == ai.Owner);
            beingBuilt = ResourceManager.ShipsDict[goal.VariableString];
            
            beingBuilt.fleet = ai.Owner.fleet;
            beingBuilt.RelativeFleetOffset = node.FleetOffset;
            SetFleet(ai.Owner.fleet);
            SetPlanetWhereBuilding(ai.OrbitTarget);
        }

        public FleetRequisition(string shipName, Empire owner) : this()
        {
            empire = owner;
            ToBuildUID = shipName;
            beingBuilt = ResourceManager.GetShipTemplate(shipName);
        }
        private GoalStep FindPlanetForFleetRequisition()
        {            

            Planet planet1 = empire.PlanetToBuildAt(beingBuilt.GetCost(empire));
            
            if (planet1 == null)
                return GoalStep.TryAgain;
            PlanetBuildingAt = planet1;
            planet1.ConstructionQueue.Add(new QueueItem()
            {
                isShip        = true,
                QueueNumber   = planet1.ConstructionQueue.Count,
                sData         = beingBuilt.GetShipData(),
                Goal          = this,
                Cost          = beingBuilt.GetCost(empire),
                NotifyOnEmpty = false
            });
            return GoalStep.GoToNextStep;
        }

        private GoalStep DoSomeStuffWithFleets()
        {
            if (fleet == null)
                return GoalStep.GoalComplete;

            using (fleet.DataNodes.AcquireWriteLock())
                foreach (FleetDataNode current in fleet.DataNodes)
                {
                    if (current.GoalGUID != guid) continue;
                    if (fleet.Ships.Count == 0)
                        fleet.Position = beingBuilt.Position +
                                         new Vector2(RandomMath.RandomBetween(-3000f, 3000f)
                                             , RandomMath.RandomBetween(-3000f, 3000f));

                    
                    
                    var ship = beingBuilt;
                    current.Ship = ship;
                    if (fleet.Position == Vector2.Zero)
                        fleet.Position = empire.FindNearestRallyPoint(ship.Center).Center;
                    ship.RelativeFleetOffset = current.FleetOffset;
                    current.GoalGUID = Guid.Empty;
                    fleet.AddShip(ship);
                    ship.AI.SetPriorityOrder();
                    ship.AI.OrderMoveToFleetPosition(
                        fleet.Position + ship.FleetOffset, ship.fleet.Facing, 
                        new Vector2(0.0f, -1f), true, fleet.Speed, fleet);
                }
            return GoalStep.TryAgain;
        }
    }
}
