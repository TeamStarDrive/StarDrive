using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class AssaultBombers : Goal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }

        [StarDataConstructor]
        public AssaultBombers(Empire owner) : base(GoalType.AssaultBombers, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                LaunchBoardingShips,
                WaitForCombatEnd
            };
        }

        public AssaultBombers(Planet planet, Empire owner, Empire enemy) : this(owner)
        {
            PlanetBuildingAt = planet;
            TargetEmpire = enemy;
        }

        GoalStep LaunchBoardingShips()
        {
            if (PlanetBuildingAt.Owner != Owner)
                return GoalStep.GoalFailed;

            int numTroopCanLaunch = PlanetBuildingAt.NumTroopsCanLaunchFor(Owner);
            if (numTroopCanLaunch == 0)
                return GoalStep.GoToNextStep; // Cant do anything about it, just wait until combat ends

            int numTroopsWanted = (int)(Owner.PersonalityModifiers.AssaultBomberRatio * numTroopCanLaunch);
            if (numTroopsWanted == 0)
                return GoalStep.GoalFailed;

            var potentialTargets = PlanetBuildingAt.System.ShipList.Filter(s => s.Loyalty == TargetEmpire);
            potentialTargets     = potentialTargets.Sorted(s => s.Position.Distance(PlanetBuildingAt.Position));
            bool launchedTroops  = false;
            foreach (Ship ship in potentialTargets)
            {
                float defenseToOvercome = ship.BoardingDefenseTotal * 1.2f;
                foreach (Troop troop in PlanetBuildingAt.Troops.GetTroopsOf(Owner))
                {
                    if (troop.CanLaunchWounded)
                    {
                        float str = troop.ActualStrengthMax;
                        Ship troopShip = troop.Launch(forceLaunch: true);
                        if (troopShip != null)
                        {
                            defenseToOvercome -= str;
                            numTroopsWanted   -= 1;
                            if (launchedTroops == false)
                            {
                                launchedTroops = true;
                                if (TargetEmpire.isPlayer)
                                    Owner.Universe.Notifications.AddEnemyLaunchedTroopsVsFleet(PlanetBuildingAt, Owner);
                            }

                            float distance = ship.Position.InRadius(PlanetBuildingAt.Position, PlanetBuildingAt.Radius + 1500) ? 300 : 600;
                            troopShip.Position = ship.Position.GenerateRandomPointOnCircle(distance, Owner.Random);
                            troopShip.Rotation = troopShip.Position.DirectionToTarget(ship.Position).ToRadians();
                            troopShip.InitLaunch(LaunchPlan.Hangar, troopShip.RotationDegrees);
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
            if (PlanetBuildingAt.Owner != Owner)
                return GoalStep.GoalFailed;

            return PlanetBuildingAt.System.DangerousForcesPresent(Owner) ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }
    }
}