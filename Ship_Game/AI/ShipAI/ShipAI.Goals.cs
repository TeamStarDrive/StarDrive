using Microsoft.Xna.Framework;

namespace Ship_Game.AI 
{
    public sealed partial class ShipAI
    {
        public void AddToOrderQueue(ShipGoal goal)
        {
            OrderQueue.Enqueue(goal);
        }

        public void AddShipGoal(Plan plan)
        {
            OrderQueue.Enqueue(new ShipGoal(plan));
        }
        public void AddShipGoal(Plan plan, Planet target)
        {
            OrderQueue.Enqueue(new ShipGoal(plan){ TargetPlanet = target });
        }

        public void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir));
        }

        public void AddShipGoal(Plan plan, Vector2 pos, Planet targetPlanet)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, Vector2.Zero, targetPlanet));
        }

        public void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, float speedLimit)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, targetPlanet) { SpeedLimit = speedLimit });
        }

        public int GotoStep;


        public class ShipGoal
        {
            public Plan Plan;
            public Goal goal;
            public float VariableNumber;
            public string VariableString;

            public Fleet fleet;
            public Vector2 MovePosition;
            public Vector2 DesiredDirection;
            public Vector2 Direction; // @note Escort Direction?
            public Planet TargetPlanet;
            public float SpeedLimit = 1f;

            public override string ToString() => $"{Plan} pos:{MovePosition} dir:{DesiredDirection}";

            public ShipGoal(Plan p)
            {
                Plan = p;
            }

            public ShipGoal(Plan p, Vector2 pos, Vector2 dir)
            {
                Plan = p;
                MovePosition = pos;
                DesiredDirection = dir;
            }

            public ShipGoal(Plan p, Vector2 pos, Vector2 dir, Planet targetPlanet)
            {
                Plan = p;
                MovePosition = pos;
                DesiredDirection = dir;
                TargetPlanet = targetPlanet;
            }

            public static ShipGoal CreateLandTroopGoal(Planet targetPlanet)
            {
                var goal = new ShipGoal(Plan.LandTroop, Vector2.Zero, Vectors.Up)
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
            MakeFinalApproach,
            RotateInlineWithVelocity,
            Orbit,
            Colonize,
            Explore,
            Rebase,
            DoCombat,
            Trade,
            DefendSystem,
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
            ResupplyEscort,
            ReturnHome
        }
    }
}