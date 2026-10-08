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
                WaitForCompletion,
                DefendPortal
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
            // Target the colony closest to the portal in its own system first, target empire is not relevant
            Planet portalSystemColony = PortalSystem?.PlanetList.FindMinFiltered(IsAttackableColony, p => p.Position.SqDist(Portal.Position));
            if (portalSystemColony != null)
            {
                TargetPlanet = portalSystemColony;
                return true;
            }

            // Find closest planet in the map to Portal and target a planet from the victim's planet list
            var planets   = Owner.Universe.Planets.Filter(p => p.Owner != null);
            Planet planet = planets.FindMin(p => p.Position.Distance(Portal.Position));
            if (!Remnants.TargetNextPlanet(TargetEmpire, planet, 0, out Planet targetPlanet))
                return false; // Could not find a target planet

            TargetPlanet = targetPlanet; // Using TargetPlanet for better readability
            return TargetPlanet != null;
        }

        // the portal leaves its system for a moment to chase enemies, so its system is the one its goal recorded
        SolarSystem PortalSystem => (Owner.AI.FindGoal(g => g is RemnantPortal p && p.TargetShip == Portal) as RemnantPortal)?.PortalSystem ?? Portal.System;

        bool IsAttackableColony(Planet p) => p?.Owner != null && Owner.IsEmpireAttackable(p.Owner);

        bool IsColonyInPortalSystem(Planet p) => IsAttackableColony(p) && p.System == PortalSystem;

        // a raid on a colony in a portal system does not change the Remnants' strength estimate of the target empire
        bool CountsForFleetStr => TargetPlanet == null || !UState.HasRemnantPortal(TargetPlanet.System);

        float FleetStrNoBombers => (Fleet.GetStrength() - Fleet.GetBomberStrength()).LowerBound(0);

        GoalStep ReturnToClosestPortalAndReroute()
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

        void ReturnToPortal()
        {
            if (!IsPortalValidOrRerouted())
            {
                Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);
            }
            else
            {
                Fleet.FleetTask.ChangeAO(Portal.Position);
                Fleet.TaskStep = 8; // Order fleet to go back to portal
            }
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
                    Task.TargetEmpire = TargetEmpire;
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

            return Portal != null && Portal.Active;
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

            if (!Remnants.TargetEmpireStillValid(TargetEmpire))
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
            {
                if (CountsForFleetStr)
                    Owner.IncreaseFleetStrEmpireMultiplier(TargetEmpire);
                return GoalStep.GoalFailed; // fleet is dead
            }

            if (!IsPortalValidOrRerouted())
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalFailed);

            if (PortalUnderAttack)
            {
                ReturnToPortal(); // Order fleet to return to portal for defense
                return GoalStep.GoToNextStep;
            }

            if (Fleet.TaskStep == 10) // Arrived back to portal
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            if (Remnants.Hibernating)
                return ReturnToClosestPortalAndReroute();

            int numBombers = Remnants.NumBombersInFleet(Fleet);
            if (BombersLevel > 0 && numBombers < BombersLevel / 2)
                RequestBombers(numBombers);

            if (numBombers / 3 >= Fleet.Ships.Count - numBombers) // only bombers and some combat ships left
            {
                if (Fleet.TaskStep < 8 && CountsForFleetStr) // counted once, not on every turn of the way back
                    Owner.IncreaseFleetStrEmpireMultiplier(TargetEmpire);
                return ReturnToClosestPortalAndReroute();
            }
            if (Fleet.TaskStep != 7 && (TargetPlanet?.Owner == TargetEmpire || IsColonyInPortalSystem(TargetPlanet))) // Not cleared enemy at target planet yet
                return GoalStep.TryAgain;

            if (!Remnants.TargetEmpireStillValid(TargetEmpire, 
                stickToSameRandomTarget: TargetPlanet?.System.HasPlanetsOwnedBy(TargetEmpire) == true 
                && Owner.Universe.P.Difficulty >= GameDifficulty.Hard))
            {
                if (!Remnants.FindValidTarget(out Empire newVictim))
                    return ReturnToClosestPortalAndReroute();

                TargetEmpire = newVictim;

                // New target is too strong, need to get a new fleet
                if (Remnants.RequiredAttackFleetStr(TargetEmpire) > Fleet.GetStrength())
                    return ReturnToClosestPortalAndReroute();
            }

            if (TargetPlanet == null)
            {
                Log.Warning("Goal Target Planet for active Remnant Goal and Fleet was null, selecting new target.");
                if (!SelectTargetPlanet())
                {
                    Log.Warning($"Could not find a new Remnant target planet vs {TargetEmpire.Name}. Remnant fleet will return home.");
                    return ReturnToClosestPortalAndReroute();
                }
            }

            // Select a new closest planet
            if (!Remnants.TargetNextPlanet(TargetEmpire, TargetPlanet, Remnants.NumBombersInFleet(Fleet), out Planet nextPlanet))
                return ReturnToClosestPortalAndReroute();

            Fleet.ClearOrders();
            int changeToStep = TargetPlanet.System == nextPlanet.System ? 5 : 1;
            TargetPlanet     = nextPlanet;
            Fleet.Name       = $"Ancient Fleet - {TargetPlanet.Name}";
            Fleet.TaskStep   = changeToStep;
            Task.ChangeTargetPlanet(TargetPlanet);
            return GoalStep.TryAgain;
        }

        bool PortalUnderAttack => Portal.InCombat && Remnants.GetHostileStrInPortalSystem(Portal) > Portal.BaseStrength * 0.75f;

        // the raid is called off: its fleet holds at the portal until the fight is over, then it is released
        GoalStep DefendPortal()
        {
            if (Fleet == null || Fleet.Ships.Count == 0)
                return GoalStep.GoalFailed; // lost defending the portal, not a failed raid

            if (!IsPortalValidOrRerouted())
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalFailed);

            if (Fleet.TaskStep == 10 && !PortalUnderAttack)
                return Remnants.ReleaseFleet(Fleet, GoalStep.GoalComplete);

            return GoalStep.TryAgain;
        }
    }
}
