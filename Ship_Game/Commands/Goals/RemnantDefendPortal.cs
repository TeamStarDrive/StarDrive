using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class RemnantDefendPortal : FleetGoal
    {
        [StarData] public readonly Ship Portal;
        [StarData] Vector2 EmergePos;

        Remnants Remnants => Owner.Remnants;

        public override bool IsRemnantDefendingPortal(Ship portal) => Portal == portal;

        [StarDataConstructor]
        public RemnantDefendPortal(Empire owner) : base(GoalType.RemnantDefendPortal, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                GatherFleet,
                ManageCombat,
                WaitForFleetRemoval
            };
        }

        public RemnantDefendPortal(Empire owner, Ship portal) : this(owner)
        {
            Portal = portal;
            if (Remnants.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Remnants: New {Owner.Name} Defend Portal at: {Portal.System.Name} ----");
        }

        public bool CreateDefenseFleet()
        {
            EmergePos = Portal.Position.GenerateRandomPointOnCircle(100_000, Owner.Random);
            if (!Remnants.CreateDefenseShips(EmergePos, out Array<Ship> ships))
                return false;

            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                if (i == 0)
                {
                    var task = MilitaryTask.CreateRemnantDefendPortal(Owner, Portal);
                    Owner.AI.AddPendingTask(task);
                    task.CreateRemnantFleet(Owner, ship, $"Ancient Fleet", out Fleet);
                    continue;
                }

                Fleet.AddShip(ship);
                ship.EmergeFromPortal();
            }

            Fleet.AutoArrange();
            Fleet.TaskStep = 1;
            return true;
        }

        bool PortalValid => Portal?.Active ?? false;

        GoalStep GatherFleet()
        {
            return !PortalValid || !CreateDefenseFleet() ? GoalStep.GoalFailed : GoalStep.GoToNextStep;
        }
        GoalStep ManageCombat()
        {
            if (Fleet.Ships.Count == 0)
                return GoalStep.GoalFailed; // fleet is dead


            if (Fleet.IsAnyShipInCombat()) 
                return GoalStep.TryAgain;

            Fleet.FleetTask.ChangeAO(EmergePos);
            Fleet.TaskStep = 3;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForFleetRemoval()
        {
            if (Fleet?.Ships.Count == 0)
            {
                Fleet = null;
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}