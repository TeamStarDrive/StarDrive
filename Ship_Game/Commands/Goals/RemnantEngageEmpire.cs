using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantEngageEmpire : Goal
    {
        public const string ID = "RemnantEngageEmpire";
        public override string UID => ID;
        public Planet TargetPlanet;
        private Remnants Remnants;
        private Ship Portal;
        private int BombersLevel;

        public RemnantEngageEmpire() : base(GoalType.RemnantBalancersEngage)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectFirstTargetPlanet,
                DetermineNumBombers,
                GatherFleet,
                WaitForCompletion
            };
        }

        public RemnantEngageEmpire(Empire owner, Ship portal, Empire target) : this()
        {
            empire       = owner;
            TargetEmpire = target;
            TargetShip   = portal;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Engagement: {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Remnants     = empire.Remnants;
            TargetPlanet = ColonizationTarget;
            Portal       = TargetShip; // Save compatibility
            BombersLevel = ShipLevel;
        }

        public override bool IsRaid => true;
       
        bool SelectTargetPlanet()
        {
            bool byLevel = RandomMath.RollDice(Remnants.Level);
            if (!byLevel || !Remnants.SelectTargetPlanetByLevel(TargetEmpire, out TargetPlanet))
                if (!Remnants.SelectTargetClosestPlanet(Portal, TargetEmpire, out TargetPlanet))
                    return false; // Could not find a target planet

            ColonizationTarget = TargetPlanet; // Using TargetPlanet for better readability
            return TargetPlanet != null;
        }

        GoalStep SelectFirstTargetPlanet()
        {
            return SelectTargetPlanet() ? GoalStep.GoToNextStep : GoalStep.GoalComplete;
        }

        GoalStep DetermineNumBombers()
        {
            ShipLevel = BombersLevel = Remnants.GetNumBombersNeeded(TargetPlanet);
            return GoalStep.GoToNextStep;
        }

        GoalStep GatherFleet()
        {
            bool checkOnlyDefeated = Remnants.Story == Remnants.RemnantStory.AncientRaidersRandom;
            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal, checkOnlyDefeated))
            {
                Remnants.ReleaseFleet(Fleet);
                return GoalStep.GoalComplete;
            }

            int missingBombers = BombersLevel > 0 ? BombersLevel - Remnants.NumBombersInFleet(Fleet): 0;
            if (!Remnants.AssignShipInPortalSystem(Portal, missingBombers, out Ship ship))
                if (!Remnants.CreateShip(Portal, missingBombers > 0, out ship))
                    return GoalStep.TryAgain;

            if (Fleet == null)
            {
                var task = MilitaryTask.CreateRemnantEngagement(TargetPlanet, empire);
                empire.GetEmpireAI().AddPendingTask(task);
                task.CreateRemnantFleet(empire, ship, "Ancient Fleet", out Fleet);
            }
            else
            {
                Fleet.AddShip(ship);
            }

            ship.AI.AddEscortGoal(Portal);
            if (Fleet.GetStrength() < (TargetEmpire.CurrentMilitaryStrength / 4).LowerBound(Remnants.Level * 100))
                return GoalStep.TryAgain;

            Fleet.AutoArrange();
            Fleet.TaskStep = 1;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForCompletion()
        {
            if (Fleet.Ships.Count == 0)
                return GoalStep.GoalFailed; // fleet is dead

            if (Fleet.TaskStep == 10) // Arrived back to portal
            {
                Remnants.ReleaseFleet(Fleet);
                return GoalStep.GoalComplete;
            }

            if (Fleet.TaskStep != 7) // Cleared enemy at target planet
                return GoalStep.TryAgain;

            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal))
            {
                if (!Remnants.GetClosestPortal(Fleet.AveragePosition(), out Ship closestPortal))
                {
                    Remnants.ReleaseFleet(Fleet);
                    return GoalStep.GoalComplete;
                }

                Fleet.FleetTask.ChangeAO(closestPortal.Center);
                Fleet.TaskStep = 8; // Order fleet to go back to portal
                return GoalStep.TryAgain;
            }

            // Select a new closest planet
            if (!Remnants.TargetNextPlanet(TargetEmpire, TargetPlanet, Remnants.NumBombersInFleet(Fleet), out Planet nextPlanet))
                return GoalStep.GoalComplete;

            TargetPlanet = nextPlanet;
            Fleet.FleetTask.ChangeTargetPlanet(TargetPlanet);
            Fleet.TaskStep = 1;

            return GoalStep.TryAgain;
        }
    }
}