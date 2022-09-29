using Ship_Game.AI;
using System;
using SDUtils;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;

namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class DefendVsRemnants : FleetGoal
    {
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] public sealed override Planet TargetPlanet { get; set; }

        [StarDataConstructor]
        public DefendVsRemnants(Empire owner) : base(GoalType.DefendVsRemnants, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForFleet,
            };
        }

        public DefendVsRemnants(Planet targetPlanet, Empire owner, Fleet fleet) : this(owner)
        {
            Fleet = fleet;
            TargetPlanet = targetPlanet;
            TargetEmpire = owner.Universe.Remnants;
        }

        bool RemnantGoalExists()
        {
            var goals = TargetEmpire.AI.GetRemnantEngagementGoalsFor(TargetPlanet);
            return goals.Length != 0;
        }

        bool TryChangeTargetPlanet()
        {
            var remnantFleets = TargetEmpire.Fleets;
            if (!remnantFleets.Any(f => f.FleetTask?.TargetPlanet?.Owner == Owner))
                return false;

            var defenseTasks = Owner.AI.GetDefendVsRemnantTasks();
            foreach (Fleet remnantFleet in remnantFleets.Filter(f => f.FleetTask?.TargetPlanet?.Owner == Owner))
            {
                // Check if we have other defense task vs. this remnant fleet target planet
                foreach (MilitaryTask task in defenseTasks)
                {
                    if (task.TargetPlanet == remnantFleet.FleetTask.TargetPlanet)
                        continue;

                    TargetPlanet = remnantFleet.FleetTask.TargetPlanet;
                    Fleet.TaskStep = 0;
                    Fleet.FleetTask.ChangeTargetPlanet(TargetPlanet);
                    return true;
                }
            }

            return false;
        }

        GoalStep WaitForFleet()
        {
            if (Fleet == null || Fleet.Ships.Count == 0)
            {
                float str = TargetPlanet.ParentSystem.GetKnownStrengthHostileTo(Owner);
                var task  = MilitaryTask.CreateDefendVsRemnant(TargetPlanet, Owner, str);
                Owner.AI.AddPendingTask(task); // Try creating a new fleet to defend
                return GoalStep.GoalFailed;
            }

            if (TargetPlanet.Owner != Owner && !TryChangeTargetPlanet())
            {
                Owner.DecreaseFleetStrEmpireMultiplier(Owner.Universe.Remnants);
                return GoalStep.GoalComplete;
            }

            return RemnantGoalExists() ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }
    }
}