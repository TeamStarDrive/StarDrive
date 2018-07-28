using Microsoft.Xna.Framework;

namespace Ship_Game.AI {
    public sealed partial class ShipAI
    {
        public void AddShipGoal(Plan plan, Vector2 waypoint, float desiredFacing)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, waypoint, desiredFacing));
        }

        public int GotoStep;

        public void AddShipGoal(Plan plan, Vector2 waypoint, float desiredFacing, Planet targetPlanet, float speedLimit)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, waypoint, desiredFacing, targetPlanet) {SpeedLimit = speedLimit});
        }

        public void AddShipGoal(Plan plan, Vector2 waypoint, float desiredFacing, Planet targetPlanet)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, waypoint, desiredFacing, targetPlanet));
        }

        public class ShipGoal
        {
            public Plan Plan;

            public Goal goal;

            public float VariableNumber;

            public string VariableString;

            public Fleet fleet;

            public Vector2 MovePosition;

            public float DesiredFacing;

            public float FacingVector;

            public Planet TargetPlanet;

            public float SpeedLimit = 1f;

            public ShipGoal(Plan p, Vector2 pos, float facing)
            {
                Plan = p;
                MovePosition = pos;
                DesiredFacing = facing;
            }

            public ShipGoal(Plan p, Vector2 pos, float facing, Planet targetPlanet)
            {
                Plan = p;
                MovePosition = pos;
                DesiredFacing = facing;
                TargetPlanet = targetPlanet;
            }

            public static ShipGoal CreateLandTroopGoal(Planet targetPlanet)
            {
                ShipGoal goal = new ShipGoal(Plan.LandTroop, Vector2.Zero, 0f)
                {
                    TargetPlanet = targetPlanet
                };
                return goal;
            }
        }

        public enum Plan
        {
            Stop,
            Scrap,
            HoldPosition,
            Bombard,
            Exterminate,
            RotateToFaceMovePosition,
            RotateToDesiredFacing,
            MoveToWithin1000,
            MakeFinalApproachFleet,
            MoveToWithin1000Fleet,
            MakeFinalApproach,
            RotateInlineWithVelocity,
            StopWithBackThrust,
            Orbit,
            Colonize,
            Explore,
            Rebase,
            DoCombat,
            MoveTowards,
            Trade,
            DefendSystem,
            TransportPassengers,
            PickupPassengers,
            DropoffPassengers,
            DeployStructure,
            PickupGoods,
            DropOffGoods,
            ReturnToHangar,
            TroopToShip,
            BoardShip,
            SupplyShip,
            Refit,
            LandTroop,
            MoveToWithin7500,
            BombTroops,
            ResupplyEscort
        }
    }
}