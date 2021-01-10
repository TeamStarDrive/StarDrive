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
            Portal       = TargetShip; // Save compatibility
            BombersLevel = ShipLevel;
        }

        public Planet TargetPlanet
        {
            get => ColonizationTarget;
            set => ColonizationTarget = value;
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
            if (!Remnants.TargetNextPlanet(TargetEmpire, planet, 0, out Planet targetPlanet))
                return false; // Could not find a target planet

            TargetPlanet = targetPlanet; // Using TargetPlanet for better readability
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
            float strDiv        = TargetEmpire.TotalPopBillion / (TargetEmpire.isPlayer ? 150 : 60);
            float strMultiplier = ((int)CurrentGame.Difficulty + 1) * 0.4f;
            float str           = TargetEmpire.CurrentMilitaryStrength * strMultiplier / strDiv.LowerBound(1);
            str                 = str.UpperBound(str * Remnants.Level / Remnants.MaxLevel);

            return str.LowerBound(Remnants.Level * Remnants.Level * 200 * strMultiplier);
        }

        float FleetStrNoBombers => (Fleet.GetStrength() - Fleet.GetBomberStrength()).LowerBound(0);

        GoalStep ReturnToPortal()
        {
            if (Fleet.TaskStep < 8)
            {
                if (!Remnants.GetClosestPortal(Fleet.AveragePosition(), out Ship closestPortal))
                    return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

                Fleet.FleetTask.ChangeAO(closestPortal.Center);
                Fleet.TaskStep = 8; // Order fleet to go back to portal
            }

            return GoalStep.TryAgain;
        }

        void RequestBombers(int currentBombers)
        {
            if (Fleet == null)
                return;

            for (int i = 1; i <= BombersLevel - currentBombers; i++)
            {
                if (Remnants.CreateShip(Portal, true, 0, out Ship ship))
                {
                    ship.Position = Portal.Center.GenerateRandomPointInsideCircle(3000);
                    ship.EmergeFromPortal();
                    Fleet.AddShip(ship);
                }
            }
        }

        void CreateFleet(Array<Ship> ships)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (i == 0)
                {
                    var task = MilitaryTask.CreateRemnantEngagement(TargetPlanet, empire);
                    empire.GetEmpireAI().AddPendingTask(task);
                    task.CreateRemnantFleet(empire, ship, $"Ancient Fleet - {TargetPlanet.Name}", out Fleet);
                    continue;
                }

                Fleet.AddShip(ship);
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
            if (Portal.InCombat)
                return GoalStep.TryAgain;

            bool checkOnlyDefeated = Remnants.Story == Remnants.RemnantStory.AncientRaidersRandom;
            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal, checkOnlyDefeated))
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            int numBombersInFleet = Remnants.NumBombersInFleet(Fleet);
            int missingBombers    = BombersLevel > 0 ? BombersLevel - numBombersInFleet : 0;
            int numShipsNoBombers = Remnants.NumShipsInFleet(Fleet) - numBombersInFleet;
            Ship singleShip       = null;
            if (!Remnants.AssignShipInPortalSystem(Portal, missingBombers, RequiredFleetStr(), out Array<Ship> ships))
                if (!Remnants.CreateShip(Portal, missingBombers > 0, numShipsNoBombers, out singleShip))
                    return GoalStep.TryAgain;

            if (ships.Count == 0 && singleShip != null)
                ships.Add(singleShip);

            if      (Fleet == null)           CreateFleet(ships);
            else if (Fleet.FleetTask == null) Remnants.ReleaseFleet(Fleet, GoalStep.TryAgain);
            else                              Fleet.AddShips(ships);

            for (int i = 0; i < ships.Count; i++)
                ships[i].AI.AddEscortGoal(Portal);

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
            if (BombersLevel > 0 && numBombers < BombersLevel / 2)
                RequestBombers(numBombers);

            if (numBombers == Fleet.Ships.Count)
                return ReturnToPortal();

            if (Fleet.TaskStep != 7 && TargetPlanet?.Owner == TargetEmpire) // Not cleared enemy at target planet yet
                return GoalStep.TryAgain;


            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal) && !Remnants.FindValidTarget(Portal, out TargetEmpire))
                return ReturnToPortal();

            if (TargetPlanet == null)
            {
                Log.Warning("Goal Target Planet for active Remnant Goal and Fleet was null, selecting new target.");
                if (!SelectTargetPlanet())
                {
                    Log.Warning($"Could not find a new Remnant target planet vs {TargetEmpire.Name}. Remnant fleet will return home.");
                    return ReturnToPortal();
                }
            }

            // Select a new closest planet
            if (!Remnants.TargetNextPlanet(TargetEmpire, TargetPlanet, Remnants.NumBombersInFleet(Fleet), out Planet nextPlanet))
                return ReturnToPortal();

            Fleet.FleetTask.ChangeTargetPlanet(nextPlanet);
            Fleet.ClearOrders();
            int changeToStep = TargetPlanet.ParentSystem == nextPlanet.ParentSystem ? 5 : 1;
            TargetPlanet     = nextPlanet;
            Fleet.Name       = $"Ancient Fleet - {TargetPlanet.Name}";
            Fleet.TaskStep   = changeToStep;
            ChangeTaskTargetPlanet();
            return GoalStep.TryAgain;
        }
    }
}