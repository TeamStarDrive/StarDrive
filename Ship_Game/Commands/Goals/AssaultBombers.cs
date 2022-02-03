using Ship_Game.AI;
using Ship_Game.Ships;
using System;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class AssaultBombers : Goal
    {
        public const string ID = "AssaultBombers";
        public override string UID => ID;

        public AssaultBombers() : base(GoalType.AssaultBombers)
        {
            Steps = new Func<GoalStep>[]
            {
                LaunchBoardingShips,
                WaitForCombatEnd
            };
        }

        public AssaultBombers(Planet planet, Empire owner, Empire enemy) : this()
        {
            PlanetBuildingAt = planet;
            empire           = owner;
            TargetEmpire     = enemy;
            Evaluate();
        }

        GoalStep LaunchBoardingShips()
        {
            if (PlanetBuildingAt.Owner != empire)
                return GoalStep.GoalFailed;

            int numTroopCanLaunch = PlanetBuildingAt.NumTroopsCanLaunchFor(empire);
            if (numTroopCanLaunch == 0)
                return GoalStep.GoToNextStep; // Cant do anything about it, just wait until combat ends

            int numTroopsWanted = (int)(empire.PersonalityModifiers.AssaultBomberRatio * numTroopCanLaunch);
            if (numTroopsWanted == 0)
                return GoalStep.GoalFailed;

            var potentialTargets = PlanetBuildingAt.ParentSystem.ShipList.Filter(s => s.Loyalty == TargetEmpire);
            potentialTargets     = potentialTargets.Sorted(s => s.Position.Distance(PlanetBuildingAt.Center));
            bool launchedTroops  = false;
            foreach (Ship ship in potentialTargets)
            {
                float defenseToOvercome = ship.BoardingDefenseTotal * 1.2f;
                for (int i = PlanetBuildingAt.TroopsHere.Count - 1; i >= 0; i--)
                {
                    Troop troop = PlanetBuildingAt.TroopsHere[i];
                    if (troop.Loyalty == empire && troop.CanLaunchWounded)
                    {
                        float str      = troop.ActualStrengthMax;
                        Ship troopShip = troop.Launch(ignoreMovement: true);
                        if (troopShip != null)
                        {
                            defenseToOvercome -= str;
                            numTroopsWanted   -= 1;
                            if (launchedTroops == false)
                            {
                                launchedTroops = true;
                                if (TargetEmpire.isPlayer)
                                    empire.Universum.Notifications.AddEnemyLaunchedTroopsVsFleet(PlanetBuildingAt, empire);
                            }

                            float distance = ship.Position.InRadius(PlanetBuildingAt.Center, PlanetBuildingAt.ObjectRadius + 1500) ? 300 : 600;
                            troopShip.Position = ship.Position.GenerateRandomPointOnCircle(distance);
                            troopShip.Rotation = troopShip.Position.DirectionToTarget(ship.Position).ToRadians();
                            troopShip.AI.OrderTroopToBoardShip(ship);
                            if (numTroopsWanted == 0)
                                return GoalStep.GoToNextStep;

                            if (defenseToOvercome.LessOrEqual(0))
                                break;
                        }
                    }
                }
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForCombatEnd()
        {
            if (PlanetBuildingAt.Owner != empire)
                return GoalStep.GoalFailed;

            return PlanetBuildingAt.ParentSystem.DangerousForcesPresent(empire) ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }
    }
}