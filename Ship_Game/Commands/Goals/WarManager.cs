using System;
using Ship_Game.AI;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class WarManager : Goal
    {
        [StarData] public sealed override Empire TargetEmpire { get; set; }

        [StarDataConstructor]
        public WarManager(Empire owner) : base(GoalType.WarManager, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectTargetSystems,
                ProcessWar,
                RequestPeaceOrEscalate,
            };
        }

        public WarManager(Empire owner, Empire enemy, WarType warType) : this(owner)
        {
            TargetEmpire = enemy;
            Log.Info(ConsoleColor.Green, $"---- War: New War Goal {warType} vs.: {TargetEmpire.Name} ----");
        }

        WarType GetWarType() => Owner.GetRelations(TargetEmpire).ActiveWar.WarType;
        War ActiveWar => Owner.GetRelations(TargetEmpire).ActiveWar;

        // The whole goal is moot once the war is over - completing here also keeps every step's
        // ActiveWar/GetWarType access safe, since they never run while we're not at war.
        protected override GoalStep? PreEvaluate()
            => Owner.IsAtWarWith(TargetEmpire) ? null : GoalStep.GoalComplete;

        GoalStep SelectTargetSystems()
        {
            if (!Owner.GetPotentialTargetPlanets(TargetEmpire, GetWarType(), out Planet[] planetTargets))
            {
                if (!Owner.TryGetMissionsVsEmpire(TargetEmpire, out _))
                    ChangeToStep(RequestPeaceOrEscalate);

                return GoalStep.TryAgain;
            }

            var targetPlanetsSorted = Owner.SortPlanetTargets(planetTargets, GetWarType(), TargetEmpire);
            foreach (Planet planet in targetPlanetsSorted)
            {
                // a system with a known third party hostile to us is left to that fight
                if (Owner.CanAddAnotherWarGoal(TargetEmpire) && !Owner.HasThirdPartyIn(planet.System, TargetEmpire))
                {
                    Owner.AI.AddGoalAndEvaluate(new WarMission(Owner, TargetEmpire, planet));
                    return GoalStep.TryAgain;
                }
            }
            
            return GoalStep.GoToNextStep;
        }

        GoalStep ProcessWar()
        {
            return Owner.GetPotentialTargetPlanets(TargetEmpire, GetWarType(), out _) && Owner.CanAddAnotherWarGoal(TargetEmpire)
                ? GoalStep.RestartGoal 
                : GoalStep.TryAgain;
        }

        GoalStep RequestPeaceOrEscalate()
        {
            if (TargetEmpire.IsDefeated)
                return GoalStep.GoalComplete;

            var warType = GetWarType();
            if (warType == WarType.BorderConflict || warType == WarType.DefensiveWar)
                Owner.GetRelations(TargetEmpire).OfferPeace(Owner, TargetEmpire, "OFFERPEACE_FAIR_WINNING");

            if (Owner.IsAtWarWith(TargetEmpire))
            {
                // Note: If TargetEmpire is the player, it will still be at war since the diplo is on a different thread.
                // But we are checking per goal if the relevant empire is indeed at war to overcome this.
                WarType changeTo = Owner.GetWarEscalation(warType);
                if (warType == changeTo)
                    return GoalStep.TryAgain;

                Owner.GetRelations(TargetEmpire).ActiveWar.ChangeWarType(changeTo);
            }

            return GoalStep.RestartGoal;
        }
    }
}