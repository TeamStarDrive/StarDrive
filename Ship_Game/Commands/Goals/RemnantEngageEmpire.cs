using System;
using System.Runtime.Remoting.Messaging;
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
            // Target owned planet in the portal system first, target empire is not relevant
            if (Portal.System != null && Portal.System.PlanetList.Any(p => p.Owner != null))
            {
                TargetPlanet = Portal.System.PlanetList.FindMin(p => p.Center.Distance(Portal.Center));
                return TargetPlanet != null;
            }

            // Find closest planet in the map to Portal and target a planet from the victim's planet list
            var planets   = Empire.Universe.PlanetsDict.Values.ToArray().Filter(p => p.Owner != null);
            Planet planet = planets.FindMin(p => p.Center.Distance(Portal.Center));
            if (!Remnants.TargetNextPlanet(TargetEmpire, planet, 0, out TargetPlanet))
                return false; // Could not find a target planet

            ColonizationTarget = TargetPlanet; // Using TargetPlanet for better readability
            return TargetPlanet != null;
        }

        void ChangeTaskTargetPlanet()
        {
            var tasks = empire.GetEmpireAI().GetTasks().Filter(t => t.Fleet == Fleet);
            switch (tasks.Length)
            {
                case 0:                                                                                  return;
                case 1:  tasks[0].ChangeTargetPlanet(TargetPlanet);                                      break;
                default: Log.Warning($"Found multiple Remnant tasks with the same fleet: {Fleet.Name}"); break;
            }
        }

        float RequiredFleetStr()
        {
            float strMultiplier = ((int)CurrentGame.Difficulty + 1) * 0.5f;
            float str           = TargetEmpire.CurrentMilitaryStrength * strMultiplier / 3;
            str                 = str.UpperBound(str * Remnants.Level / Remnants.MaxLevel);

            return str.LowerBound(Remnants.Level * Remnants.Level * 200 * strMultiplier);
        }

        float FleetStrNoBombers => (Fleet.GetStrength() - Fleet.GetBomberStrength()).LowerBound(0);

        GoalStep ReturnToPortal()
        {
            Fleet.FleetTask.ChangeAO(Portal.Center);
            if (Fleet.TaskStep < 8)
                Fleet.TaskStep = 8; // Order fleet to go back to portal

            return GoalStep.TryAgain;
        }

        void RequestBombers()
        {
            if (Fleet == null)
                return;

            for (int i = 1; i <= BombersLevel; i++)
            {
                if (Remnants.CreateShip(Portal, true, 0, out Ship ship))
                {
                    ship.Position = Portal.Center.GenerateRandomPointInsideCircle(3000);
                    ship.EmergeFromPortal();
                    Fleet.AddShip(ship);
                }
            }
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
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            int numBombersInFleet = Remnants.NumBombersInFleet(Fleet);
            int missingBombers    = BombersLevel > 0 ? BombersLevel - numBombersInFleet : 0;
            int numShipsNoBombers = Remnants.NumShipsInFleet(Fleet) - numBombersInFleet;
            if (!Remnants.AssignShipInPortalSystem(Portal, missingBombers, out Ship ship))
                if (!Remnants.CreateShip(Portal, missingBombers > 0, numShipsNoBombers, out ship))
                    return GoalStep.TryAgain;

            if (Fleet == null)
            {
                var task = MilitaryTask.CreateRemnantEngagement(TargetPlanet, empire);
                empire.GetEmpireAI().AddPendingTask(task);
                task.CreateRemnantFleet(empire, ship, $"Ancient Fleet - {TargetPlanet.Name}", out Fleet);
            }
            else
            {
                Fleet.AddShip(ship);
            }

            ship.AI.AddEscortGoal(Portal);
            if (FleetStrNoBombers < RequiredFleetStr())
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
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            if (Remnants.Hibernating)
                return ReturnToPortal();

            int numBombers = Remnants.NumBombersInFleet(Fleet);
            if (BombersLevel > 0 && numBombers == 0)
                RequestBombers();

            if (numBombers == Fleet.Ships.Count)
                return ReturnToPortal();

            if (Fleet.TaskStep != 7 && TargetPlanet.Owner != null) // Cleared enemy at target planet
                return GoalStep.TryAgain;

            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal))
            {
                if (!Remnants.GetClosestPortal(Fleet.AveragePosition(), out Ship closestPortal))
                    return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

                return ReturnToPortal();
            }

            // Select a new closest planet
            if (!Remnants.TargetNextPlanet(TargetEmpire, TargetPlanet, Remnants.NumBombersInFleet(Fleet), out Planet nextPlanet))
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            Fleet.FleetTask.ChangeTargetPlanet(nextPlanet);
            int changeToStep = TargetPlanet.ParentSystem == nextPlanet.ParentSystem ? 5 : 1;
            TargetPlanet     = nextPlanet;
            Fleet.Name       = $"Ancient Fleet - {TargetPlanet.Name}";
            Fleet.TaskStep   = changeToStep;
            ChangeTaskTargetPlanet();
            return GoalStep.TryAgain;
        }
    }
}