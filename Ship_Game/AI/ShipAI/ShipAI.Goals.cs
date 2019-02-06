using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

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

        public void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, null, null, 0f, "", 0f));
        }

        public void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Goal theGoal, 
                                string variableString, float variableNumber)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, null, theGoal, 
                                            0f, variableString, variableNumber));
        }

        public void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, float speedLimit)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, targetPlanet, null, speedLimit, "", 0f));
        }

        public bool AddShipGoal(Plan plan, Planet target, string variableString = "")
        {
            if (target == null)
            {
                Log.Error($"AddShipGoal {plan}: planet was null! Goal discarded.");
                return false;
            }

            OrderQueue.Enqueue(new ShipGoal(plan, target.Center, Vectors.Up, 
                target, null, 0f, variableString, 0f));
            return true;
        }

        public void AddPlanetGoal(Plan plan, Planet planet, AIState newState, bool priority = false)
        {
            if (AddShipGoal(plan, planet))
            {
                State = newState;
                OrbitTarget = planet;
                if (priority)
                    SetPriorityOrder(false);
            }
        }

        public void AddOrbitPlanetGoal(Planet planet, AIState newState = AIState.Orbit)
        {
            AddPlanetGoal(Plan.Orbit, planet, newState);
        }

        public void AddLandTroopGoal(Planet planet)
        {
            AddPlanetGoal(Plan.LandTroop, planet, AIState.AssaultPlanet);
        }

        public class ShipGoal
        {
            // ship goal variables are read-only by design, do not allow writes!
            public readonly Plan Plan;
            public readonly Vector2 MovePosition;
            public readonly Vector2 Direction; // direction param for this goal, can have multiple meanings
            public readonly Planet TargetPlanet;
            public readonly Goal Goal;
            public readonly Fleet Fleet;
            public readonly float SpeedLimit;
            public readonly string VariableString;
            public readonly float VariableNumber;

            public override string ToString() => $"{Plan} pos:{MovePosition} dir:{Direction}";

            public ShipGoal(Plan plan)
            {
                Plan = plan;
            }

            public ShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet planet, Goal theGoal,
                            float speedLimit, string variableString, float variableNumber)
            {
                Plan         = plan;
                MovePosition = pos;
                Direction    = dir;
                TargetPlanet = planet;
                Goal         = theGoal;
                SpeedLimit   = speedLimit;
                VariableString = variableString;
                VariableNumber = variableNumber;
            }

            public ShipGoal(SavedGame.ShipGoalSave sg, UniverseData data, Ship ship)
            {
                Plan = sg.Plan;
                MovePosition = sg.MovePosition;
                Direction = sg.DesiredFacing.RadiansToDirection();

                if (sg.TargetPlanetGuid != Guid.Empty)
                    TargetPlanet = data.FindPlanet(sg.TargetPlanetGuid);

                VariableString = sg.VariableString;
                SpeedLimit = sg.SpeedLimit;

                Empire loyalty = ship.loyalty;
                
                if (sg.fleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> empireFleet in loyalty.GetFleetsDict())
                        if (empireFleet.Value.Guid == sg.fleetGuid)
                            Fleet = empireFleet.Value;
                }

                if (sg.goalGuid != Guid.Empty)
                {
                    foreach (Goal empireGoal in loyalty.GetEmpireAI().Goals)
                    {
                        if (sg.goalGuid == empireGoal.guid)
                            Goal = empireGoal;
                    }
                }
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