using System;
using Ship_Game.AI;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    public class WarManager : Goal
    {
        public const string ID = "WarManager";
        public override string UID => ID;

        public WarManager(int id, UniverseState us)
            : base(GoalType.WarManager, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectTargetSystems,
                ProcessWar,
                RequestPeaceOrEscalate,
            };
        }

        public WarManager(Empire owner, Empire enemy, WarType warType)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire        = owner;
            TargetEmpire  = enemy;
            StarDateAdded = empire.Universum.StarDate;
            Log.Info(ConsoleColor.Green, $"---- War: New War Goal {warType} vs.: {TargetEmpire.Name} ----");
        }

        WarType GetWarType() => empire.GetRelations(TargetEmpire).ActiveWar.WarType;

        GoalStep SelectTargetSystems()
        {
            if (!empire.IsAtWarWith(TargetEmpire))
                return GoalStep.GoalComplete;

            if (!empire.GetPotentialTargetPlanets(TargetEmpire, GetWarType(), out Planet[] planetTargets))
            {
                if (!empire.TryGetMissionsVsEmpire(TargetEmpire, out _))
                    ChangeToStep(RequestPeaceOrEscalate);

                return GoalStep.TryAgain;
            }

            var targetPlanetsSorted = empire.SortPlanetTargets(planetTargets, GetWarType(), TargetEmpire);
            foreach (Planet planet in targetPlanetsSorted)
            {
                if (empire.CanAddAnotherWarGoal(TargetEmpire))
                {
                    empire.GetEmpireAI().Goals.Add(new WarMission(empire, TargetEmpire, planet));
                    return GoalStep.TryAgain;
                }
            }
            
            return GoalStep.GoToNextStep;
        }

        GoalStep ProcessWar()
        {
            if (!empire.IsAtWarWith(TargetEmpire))
                return GoalStep.GoalComplete;

            return empire.GetPotentialTargetPlanets(TargetEmpire, GetWarType(), out _) && empire.CanAddAnotherWarGoal(TargetEmpire) 
                ? GoalStep.RestartGoal 
                : GoalStep.TryAgain;
        }

        GoalStep RequestPeaceOrEscalate()
        {
            if (!empire.IsAtWarWith(TargetEmpire) || TargetEmpire.IsEmpireDead())
                return GoalStep.GoalComplete;

            var warType = GetWarType();
            if (warType == WarType.BorderConflict || warType == WarType.DefensiveWar)
                empire.GetRelations(TargetEmpire).OfferPeace(empire, TargetEmpire, "OFFERPEACE_FAIR_WINNING");

            if (empire.IsAtWarWith(TargetEmpire))
            {
                // Note: If TargetEmpire is the player, it will still be at war since the diplo is on a different thread.
                // But we are checking per goal if the relevant empire is indeed at war to overcome this.
                WarType changeTo = empire.GetWarEscalation(warType);
                if (warType == changeTo)
                    return GoalStep.TryAgain;

                empire.GetRelations(TargetEmpire).ActiveWar.ChangeWarType(changeTo);
            }

            return GoalStep.RestartGoal;
        }
    }
}