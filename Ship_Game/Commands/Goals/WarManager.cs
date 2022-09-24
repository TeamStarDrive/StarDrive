using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class WarManager : Goal
    {
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
            TargetEmpire  = enemy;
            Log.Info(ConsoleColor.Green, $"---- War: New War Goal {warType} vs.: {TargetEmpire.Name} ----");
        }

        WarType GetWarType() => Owner.GetRelations(TargetEmpire).ActiveWar.WarType;

        GoalStep SelectTargetSystems()
        {
            if (!Owner.IsAtWarWith(TargetEmpire))
                return GoalStep.GoalComplete;

            if (!Owner.GetPotentialTargetPlanets(TargetEmpire, GetWarType(), out Planet[] planetTargets))
            {
                if (!Owner.TryGetMissionsVsEmpire(TargetEmpire, out _))
                    ChangeToStep(RequestPeaceOrEscalate);

                return GoalStep.TryAgain;
            }

            var targetPlanetsSorted = Owner.SortPlanetTargets(planetTargets, GetWarType(), TargetEmpire);
            foreach (Planet planet in targetPlanetsSorted)
            {
                if (Owner.CanAddAnotherWarGoal(TargetEmpire))
                {
                    Owner.AI.AddGoal(new WarMission(Owner, TargetEmpire, planet));
                    return GoalStep.TryAgain;
                }
            }
            
            return GoalStep.GoToNextStep;
        }

        GoalStep ProcessWar()
        {
            if (!Owner.IsAtWarWith(TargetEmpire))
                return GoalStep.GoalComplete;

            return Owner.GetPotentialTargetPlanets(TargetEmpire, GetWarType(), out _) && Owner.CanAddAnotherWarGoal(TargetEmpire) 
                ? GoalStep.RestartGoal 
                : GoalStep.TryAgain;
        }

        GoalStep RequestPeaceOrEscalate()
        {
            if (!Owner.IsAtWarWith(TargetEmpire) || TargetEmpire.IsEmpireDead())
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