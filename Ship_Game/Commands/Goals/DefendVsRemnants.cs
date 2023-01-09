using Ship_Game.AI;
using System;
using System.Linq;
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
            var remnantsTargetingUs = TargetEmpire.GetActiveFleetsTargetingEmpire(Owner).ToArrayList();

            var defenseTasks = Owner.AI.GetDefendVsRemnantTasks();
            foreach (Fleet remnantFleet in remnantsTargetingUs)
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
            if (Fleet == null || Fleet.Ships.Count == 0) // Fleet is destroyed
            {
                float str =  Owner.KnownEnemyStrengthIn(TargetPlanet.ParentSystem, TargetEmpire);
                var task  = MilitaryTask.CreateDefendVsRemnant(TargetPlanet, Owner, str);
                Owner.AI.AddPendingTask(task); // Try creating a new fleet to defend (it will create a new defend goal)
                return GoalStep.GoalFailed;
            }

            // We failed to defend and there is no need to defend another planet
            if (TargetPlanet.Owner != Owner && !TryChangeTargetPlanet())
            {
                Fleet.FleetTask.EndTask();
                return GoalStep.GoalComplete;
            }

            // We won this fight
            if (!RemnantGoalExists())
            {
                Fleet.FleetTask.EndTask();
                Owner.DecreaseFleetStrEmpireMultiplier(Owner.Universe.Remnants);
                return GoalStep.GoalComplete;
            }
            else
            {
                return GoalStep.TryAgain;
            }
        }
    }
}