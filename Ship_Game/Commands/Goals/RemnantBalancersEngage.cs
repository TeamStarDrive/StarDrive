using System;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class RemnantBalancersEngage : Goal
    {
        public const string ID = "RemnantBalancersEngage";
        public override string UID => ID;
        public Planet TargetPlanet;
        private Remnants Remnants;
        private Ship Portal;
        private int BombersLevel;

        public RemnantBalancersEngage() : base(GoalType.RemnantBalancersEngage)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectFirstTargetPlanet,
                SelectPortalToSpawnFrom,
                DetermineNumBombers,
                GatherFleet,
                WaitForCompletion
            };
        }

        public RemnantBalancersEngage(Empire owner, Empire target) : this()
        {
            empire       = owner;
            TargetEmpire = target;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Remnants: New {empire.Name} Engagement: Ancient Balancers for {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Remnants     = empire.Remnants;
            TargetPlanet = ColonizationTarget;
            Portal       = TargetShip;
            BombersLevel = ShipLevel;
        }

        public override bool IsRaid => true;

        bool StillStrongest => EmpireManager.MajorEmpires.FindMaxFiltered(e => !e.data.Defeated, e => e.TotalScore) == TargetEmpire;
        
        bool SelectTargetPlanet()
        {
            bool byLevel = RandomMath.RollDice(Remnants.Level);
            if (byLevel && !Remnants.SelectTargetPlanetByLevel(TargetEmpire, out TargetPlanet))
                if (!Remnants.SelectTargetClosestPlanet(Portal, TargetEmpire, out TargetPlanet))
                    return false; // Could not find a target planet

            ColonizationTarget = TargetPlanet; // Using TargetPlanet for better readability
            return true;
        }

        GoalStep SelectFirstTargetPlanet()
        {
            return SelectTargetPlanet() ? GoalStep.GoToNextStep : GoalStep.GoalComplete;
        }

        GoalStep SelectPortalToSpawnFrom()
        {
            if (!Remnants.GetPortals(out Ship[] portals))
                return GoalStep.GoalFailed;

            Portal     = portals.FindMin(s => s.Center.Distance(TargetPlanet.Center));
            TargetShip = Portal; // Save compatibility
            return GoalStep.GoToNextStep;
        }

        GoalStep DetermineNumBombers()
        {
            ShipLevel = BombersLevel = Remnants.GetNumBombersNeeded(TargetPlanet);
            return GoalStep.GoToNextStep;
        }

        GoalStep GatherFleet()
        {
            if (!StillStrongest)
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

            ship.EmergeFromPortal();
            ship.AI.AddEscortGoal(Portal);
            if (Fleet.GetStrength() < TargetEmpire.CurrentMilitaryStrength / 4)
                return GoalStep.TryAgain;

            Fleet.AutoArrange();
            Fleet.TaskStep = 1;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForCompletion()
        {
            if (Fleet.Ships.Count == 0)
                return GoalStep.GoalFailed; // fleet is dead

            if (Fleet.TaskStep == 7) // Fleet is done attacking and bombing
            {
                if (!StillStrongest)
                {
                    Remnants.ReleaseFleet(Fleet);
                    return GoalStep.GoalComplete;
                }

                // Select a new closest planet
                if (!Remnants.SelectClosestNextPlanet(TargetEmpire, TargetPlanet, out Planet nextPlanet))
                    return GoalStep.GoalComplete;

                TargetPlanet = nextPlanet;
                Fleet.FleetTask.ChangeTargetPlanet(TargetPlanet);
                Fleet.TaskStep = 1;
            }

            return GoalStep.TryAgain;
        }
    }
}