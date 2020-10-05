﻿using System;
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
            // Find closest planet in the map to Portal and target a planet from the victim's planet list
            var planets   = Empire.Universe.PlanetsDict.Values.ToArray();
            Planet planet = planets.FindMin(p => p.Center.Distance(Portal.Center));
            if (!Remnants.TargetNextPlanet(TargetEmpire, planet, 0, out TargetPlanet))
                return false; // Could not find a target planet

            ColonizationTarget = TargetPlanet; // Using TargetPlanet for better readability
            return TargetPlanet != null;
        }

        void ChangeTaskTargetPlanet()
        {
            var tasks = empire.GetEmpireAI().GetTasks();
            tasks.Filter(t => t.Fleet == Fleet);
            switch (tasks.Count)
            {
                case 0:                                                                                  return;
                case 1:  tasks[0].ChangeTargetPlanet(TargetPlanet);                                      break;
                default: Log.Warning($"Found multiple Remnant tasks with the tame fleet: {Fleet.Name}"); break;
            }
        }

        float RequiredFleetStr()
        {
            float str;
            if (TargetEmpire.isPlayer)
                str = TargetEmpire.CurrentMilitaryStrength / 4 * ((int)CurrentGame.Difficulty).LowerBound(1);
            else
                str = TargetEmpire.CurrentMilitaryStrength / (TargetEmpire.GetPlanets().Count / 2f).LowerBound(1);

            str = str.UpperBound(str * Remnants.Level / Remnants.MaxLevel);
            return str.LowerBound(Remnants.Level * Remnants.Level * 100);
        }

        float FleetStrNoBombers => (Fleet.GetStrength() - Fleet.GetBomberStrength()).LowerBound(0);

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

            int missingBombers = BombersLevel > 0 ? BombersLevel - Remnants.NumBombersInFleet(Fleet): 0;
            if (!Remnants.AssignShipInPortalSystem(Portal, missingBombers, out Ship ship))
                if (!Remnants.CreateShip(Portal, missingBombers > 0, Remnants.NumShipsInFleet(Fleet), out ship))
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

            if (Fleet.TaskStep != 7 && TargetPlanet.Owner != null) // Cleared enemy at target planet
                return GoalStep.TryAgain;

            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal))
            {
                if (!Remnants.GetClosestPortal(Fleet.AveragePosition(), out Ship closestPortal))
                    return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

                Fleet.FleetTask.ChangeAO(closestPortal.Center);
                if (Fleet.TaskStep < 8)
                    Fleet.TaskStep = 8; // Order fleet to go back to portal

                return GoalStep.TryAgain;
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