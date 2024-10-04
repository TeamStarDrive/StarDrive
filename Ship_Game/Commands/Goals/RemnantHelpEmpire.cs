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
    public class RemnantHelpEmpire : FleetGoal
    {
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] public override Planet TargetPlanet { get; set; }

        Remnants Remnants => Owner.Remnants;

        public override bool IsRaid => true;
        public override bool IsRemnantEngageAtPlanet(Planet p) => TargetPlanet == p;

        [StarDataConstructor]
        public RemnantHelpEmpire(Empire owner) : base(GoalType.RemnantEngageEmpire, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectFirstTargetPlanet,
                GatherFleet,
                WaitForCompletion
            };
        }

        public RemnantHelpEmpire(Empire owner, Ship portal, Empire target) : this(owner)
        {
            TargetEmpire = target;
            TargetShip   = portal;
            if (Remnants.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Remnants: New {Owner.Name} Help: {TargetEmpire.Name} ----");
        }

        Ship Portal
        {
            get => TargetShip;
            set => TargetShip = value;
        }

        bool SelectTargetPlanet()
        {
            var planets = TargetEmpire.GetPlanets();
            TargetPlanet = planets.FindMax(p => p.ColonyWarValueTo(TargetEmpire)); // Using TargetPlanet for better readability
            return TargetPlanet != null;
        }

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

        void CreateFleet(Array<Ship> ships)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (i == 0)
                {
                    Task = MilitaryTask.CreateRemnantHelp(TargetPlanet, Owner);
                    Owner.AI.AddPendingTask(Task);
                    Task.CreateRemnantFleet(Owner, ship, $"Ancient Fleet - {TargetPlanet.Name}", out Fleet);
                    Task.TargetEmpire = TargetEmpire;
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

            return Portal != null && Portal.Active;
        }

        GoalStep SelectFirstTargetPlanet()
        {
            return SelectTargetPlanet() ? GoalStep.GoToNextStep : GoalStep.GoalComplete;
        }

        GoalStep GatherFleet()
        {
            if (!IsPortalValidOrRerouted())
                return GoalStep.GoalFailed;

            if (Portal.InCombat && Portal.AI.Target?.System == Portal.System && Portal.HealthPercent > 0.75f)
                return GoalStep.TryAgain;

            if (!Remnants.TargetEmpireStillValid(TargetEmpire))
                return Remnants.Hibernating ? GoalStep.TryAgain : Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            int numShipsinFleet = Remnants.NumShipsInFleet(Fleet);
            Ship singleShip = null;
            if (!Remnants.AssignShipInPortalSystem(Portal, 0, Remnants.RequiredAttackFleetStr(TargetEmpire), out Array<Ship> ships))
                if (!Remnants.CreateShip(Portal, false , numShipsinFleet, out singleShip))
                    return GoalStep.TryAgain;

            if (ships.Count == 0 && singleShip != null)
                ships.Add(singleShip);

            if      (Fleet == null) CreateFleet(ships);
            else if (Fleet.FleetTask == null) Remnants.ReleaseFleet(Fleet, GoalStep.TryAgain);
            else    Fleet.AddShips(ships);

            for (int i = 0; i < ships.Count; i++)
                ships[i].AI.AddEscortGoal(Portal);

            if (Fleet.GetStrength() < Remnants.RequiredAttackFleetStr(TargetEmpire))
                return GoalStep.TryAgain;

            Fleet.AutoArrange();
            Fleet.TaskStep = 1;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForCompletion()
        {
            if (Fleet == null || Fleet.Ships.Count == 0)
            {
                return GoalStep.GoalFailed; // fleet is dead
            }

            if (!IsPortalValidOrRerouted())
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalFailed);

            if (Fleet.TaskStep == 10) // Arrived back to portal
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            if (TargetPlanet == null || Remnants.Hibernating)
                return ReturnToClosestPortal();

            if (TargetPlanet.Owner != TargetEmpire) // Planet was conquered by another empire
            {
                if (SelectTargetPlanet())
                {
                    Fleet.TaskStep = 1;
                    Fleet.FleetTask.SetTargetPlanetAsAO(TargetPlanet);
                    return GoalStep.TryAgain;
                }
                else
                {
                    return ReturnToClosestPortal();
                }
            }

            if (!Remnants.TargetEmpireStillValid(TargetEmpire))
            {
                if (!Remnants.FindValidTarget(out Empire newVictim))
                    return ReturnToClosestPortal();

                TargetEmpire = newVictim;
                Fleet.FleetTask.TargetEmpire = newVictim;
                Fleet.TaskStep = 1;
                if (SelectTargetPlanet())
                    Fleet.FleetTask.SetTargetPlanetAsAO(TargetPlanet);
                
                // New target is too strong, need to get a new fleet
                if (TargetPlanet == null || Remnants.RequiredAttackFleetStr(TargetEmpire) > Fleet.GetStrength())
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

            if (Fleet.TaskStep == 3) // Ready for loyalty Change
            {
                var ships = Fleet.Ships.Clone();
                Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);
                for (int i = ships.Count - 1; i >= 0; i--)
                {
                    Ship s = ships[i];
                    s.KillAllTroops();
                    s.LoyaltyChangeByGift(TargetEmpire, false);
                }

                if (Owner.LegacyEspionageEnabled || TargetEmpire.isPlayer || Owner.Universe.Player.GetEspionage(TargetEmpire).CanDetectRemnantGifts)
                    Remnants.IncrementKillsForStory(Owner.Universe.Player, (int)(Remnants.StepXpTrigger * (TargetEmpire.isPlayer ? 0.1f : 0.05f)));

                if (TargetEmpire.isPlayer)
                    Owner.Universe.Notifications.AddRemnantHelpersGiftMessage(Remnants.StoryStep-1, TargetPlanet.System, Owner);

                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}
