using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class RemnantEngageEmpire : FleetGoal
    {
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] int BombersLevel;
        [StarData] public override Planet TargetPlanet { get; set; }
        
        Remnants Remnants => Owner.Remnants;
        
        public override bool IsRaid => true;
        public override bool IsRemnantEngageAtPlanet(Planet p) => TargetPlanet == p;

        [StarDataConstructor]
        public RemnantEngageEmpire(Empire owner) : base(GoalType.RemnantEngageEmpire, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectFirstTargetPlanet,
                DetermineNumBombers,
                GatherFleet,
                WaitForCompletion
            };
        }

        public RemnantEngageEmpire(Empire owner, Ship portal, Empire target) : this(owner)
        {
            TargetEmpire = target;
            TargetShip = portal;
            if (Remnants.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Remnants: New {Owner.Name} Engagement: {TargetEmpire.Name} ----");
        }

        Ship Portal
        {
            get => TargetShip;
            set => TargetShip = value;
        }

        bool SelectTargetPlanet()
        {
            // Target owned planet in the portal system first, target empire is not relevant
            if (Portal.System != null && Portal.System.PlanetList.Any(p => p.Owner != null))
            {
                TargetPlanet = Portal.System.PlanetList.FindMin(p => p.Position.Distance(Portal.Position));
                return TargetPlanet != null;
            }

            // Find closest planet in the map to Portal and target a planet from the victim's planet list
            var planets   = Owner.Universe.Planets.Filter(p => p.Owner != null);
            Planet planet = planets.FindMin(p => p.Position.Distance(Portal.Position));
            if (!Remnants.TargetNextPlanet(TargetEmpire, planet, 0, out Planet targetPlanet))
                return false; // Could not find a target planet

            TargetPlanet = targetPlanet; // Using TargetPlanet for better readability
            return TargetPlanet != null;
        }

        float FleetStrNoBombers => (Fleet.GetStrength() - Fleet.GetBomberStrength()).LowerBound(0);

        GoalStep ReturnToClosestPortal()
        {
            if (Fleet.TaskStep < 8)
            {
                if (!Remnants.GetClosestPortal(Fleet.AveragePosition(), out Ship closestPortal))
                    return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

                Fleet.FleetTask.ChangeAO(closestPortal.Position);
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
                    ship.Position = Portal.Position.GenerateRandomPointInsideCircle(3000, Owner.Random);
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
                    Task = MilitaryTask.CreateRemnantEngagement(TargetPlanet, Owner);
                    Owner.AI.AddPendingTask(Task);
                    Task.CreateRemnantFleet(Owner, ship, $"Ancient Fleet - {TargetPlanet.Name}", out Fleet);
                    continue;
                }

                Fleet.AddShip(ship);
            }
        }

        bool IsPortalValidOrRerouted()
        {
            if (Portal != null && Portal.Active)
                return true;

            if (Remnants.RerouteGoalPortals(out Ship newPortal))
                Portal = newPortal;

            return Portal != null;
        }

        GoalStep SelectFirstTargetPlanet()
        {
            return SelectTargetPlanet() ? GoalStep.GoToNextStep : GoalStep.GoalComplete;
        }

        GoalStep DetermineNumBombers()
        {
            BombersLevel = Remnants.GetNumBombersNeeded(TargetPlanet);
            return GoalStep.GoToNextStep;
        }

        GoalStep GatherFleet()
        {
            if (!IsPortalValidOrRerouted())
                return GoalStep.GoalFailed;

            if (Portal.InCombat && Portal.AI.Target?.System == Portal.System && Portal.HealthPercent > 0.75f)
                return GoalStep.TryAgain;

            bool checkOnlyDefeated = Remnants.Story == Remnants.RemnantStory.AncientRaidersRandom;
            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal, checkOnlyDefeated))
                return Remnants.Hibernating ? GoalStep.TryAgain : Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            int numBombersInFleet = Remnants.NumBombersInFleet(Fleet);
            int missingBombers    = BombersLevel > 0 ? BombersLevel - numBombersInFleet : 0;
            int numShipsNoBombers = Remnants.NumShipsInFleet(Fleet) - numBombersInFleet;
            Ship singleShip       = null;

            if (!Remnants.AssignShipInPortalSystem(Portal, missingBombers, Remnants.RequiredAttackFleetStr(TargetEmpire), out Array<Ship> ships))
                if (!Remnants.CreateShip(Portal, missingBombers > 0 && !Portal.InCombat, numShipsNoBombers, out singleShip))
                    return GoalStep.TryAgain;

            if (ships.Count == 0 && singleShip != null)
                ships.Add(singleShip);

            if      (Fleet == null)           CreateFleet(ships);
            else if (Fleet.FleetTask == null) Remnants.ReleaseFleet(Fleet, GoalStep.TryAgain);
            else                              Fleet.AddShips(ships);

            for (int i = 0; i < ships.Count; i++)
                ships[i].AI.AddEscortGoal(Portal);

            if (numShipsNoBombers < numBombersInFleet || FleetStrNoBombers < Remnants.RequiredAttackFleetStr(TargetEmpire))
                return GoalStep.TryAgain;

            Fleet.AutoArrange();
            Fleet.TaskStep = 1;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForCompletion()
        {
            if (Fleet == null || Fleet.Ships.Count == 0)
                return GoalStep.GoalFailed; // fleet is dead

            if (!IsPortalValidOrRerouted())
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalFailed);

            if (Portal.InCombat && Remnants.GetHostileStrInPortalSystem(Portal) > Portal.BaseStrength * 0.5f)
            {
                ReturnToClosestPortal(); // Order fleet to return to portal for defense
                return GoalStep.GoalFailed;
            }

            if (Fleet.TaskStep == 10) // Arrived back to portal
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            if (Remnants.Hibernating)
                return ReturnToClosestPortal();

            int numBombers = Remnants.NumBombersInFleet(Fleet);
            if (BombersLevel > 0 && numBombers < BombersLevel / 2)
                RequestBombers(numBombers);

            if (numBombers / 3 >= Fleet.Ships.Count - numBombers)
                return ReturnToClosestPortal();

            if (Fleet.TaskStep != 7 && TargetPlanet?.Owner == TargetEmpire) // Not cleared enemy at target planet yet
                return GoalStep.TryAgain;

            if (!Remnants.TargetEmpireStillValid(TargetEmpire, Portal))
            {
                if (!Remnants.FindValidTarget(out Empire newVictim))
                    return ReturnToClosestPortal();

                TargetEmpire = newVictim;

                // New target is too strong, need to get a new fleet
                if (Remnants.RequiredAttackFleetStr(TargetEmpire) > Fleet.GetStrength())
                    return ReturnToClosestPortal();
            }

            if (TargetPlanet == null)
            {
                Log.Warning("Goal Target Planet for active Remnant Goal and Fleet was null, selecting new target.");
                if (!SelectTargetPlanet())
                {
                    Log.Warning($"Could not find a new Remnant target planet vs {TargetEmpire.Name}. Remnant fleet will return home.");
                    return ReturnToClosestPortal();
                }
            }

            // Select a new closest planet
            if (!Remnants.TargetNextPlanet(TargetEmpire, TargetPlanet, Remnants.NumBombersInFleet(Fleet), out Planet nextPlanet))
                return ReturnToClosestPortal();

            Fleet.ClearOrders();
            int changeToStep = TargetPlanet.System == nextPlanet.System ? 5 : 1;
            TargetPlanet     = nextPlanet;
            Fleet.Name       = $"Ancient Fleet - {TargetPlanet.Name}";
            Fleet.TaskStep   = changeToStep;
            Task.ChangeTargetPlanet(TargetPlanet);
            return GoalStep.TryAgain;
        }
    }
}
