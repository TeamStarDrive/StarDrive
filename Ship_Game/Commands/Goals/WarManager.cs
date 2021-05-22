using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class WarManager : Goal
    {
        public const string ID = "WarManager";
        public override string UID => ID;

        public WarManager() : base(GoalType.WarManager)
        {
            Steps = new Func<GoalStep>[]
            {
                SelectTargetSystems,
                ProcessWar,
                RequestPeaceOrEscalate,
            };
        }

        public WarManager(Empire owner, Empire enemy, WarType warType) : this()
        {
            empire        = owner;
            TargetEmpire  = enemy;
            StarDateAdded = Empire.Universe.StarDate;
            Log.Info(ConsoleColor.Green, $"---- War: New War Goal {warType} vs.: {TargetEmpire.Name} ----");
            Evaluate();
        }

        public override bool IsWarGoal => true; // todo might not be needed

        WarType GetWarType => empire.GetRelations(TargetEmpire).ActiveWar.WarType;

        GoalStep SelectTargetSystems()
        {
            if (!empire.IsAtWarWith(TargetEmpire))
                return GoalStep.GoalComplete;

            StarDateAdded = Empire.Universe.StarDate;
            if (!empire.GetPotentialTargetPlanets(TargetEmpire, GetWarType, out Array<Planet> planetTargets))
            {
                if (!empire.TryGetMissionsVsEmpire(TargetEmpire, out _))
                    ChangeToStep(RequestPeaceOrEscalate);

                return GoalStep.TryAgain;
            }

            var targetPlanetsSorted = empire.SortPlanetTargets(planetTargets, GetWarType, TargetEmpire);
            foreach (Planet planet in targetPlanetsSorted)
            {
                if (!empire.CanAddAnotherWarGoal(TargetEmpire))
                    empire.GetEmpireAI().Goals.Add(new WarMission(empire, TargetEmpire, planet));
                else
                    break;
            }
            
            return GoalStep.GoToNextStep;
        }

        GoalStep ProcessWar()
        {
            return empire.GetPotentialTargetPlanets(TargetEmpire, GetWarType, out _) && empire.CanAddAnotherWarGoal(TargetEmpire) 
                ? GoalStep.RestartGoal 
                : GoalStep.TryAgain;
        }

        GoalStep RequestPeaceOrEscalate()
        {
            if (TargetEmpire.IsEmpireDead())
                return GoalStep.GoalComplete;

            empire.GetRelations(TargetEmpire).OfferPeace(empire, TargetEmpire, "OFFERPEACE_FAIR_WINNING");
            if (empire.IsAtWarWith(TargetEmpire))
            {
                // Note: If TargetEmpire is the player, it will still be at war since the diplo is on a different thread.
                // But we are checking per goal if the relevant empire is indeed at war to overcome this.
                WarType changeTo = WarType.SkirmishWar;
                switch (GetWarType)
                {
                    case WarType.BorderConflict: changeTo = WarType.DefensiveWar;   break;
                    case WarType.DefensiveWar:   changeTo = WarType.ImperialistWar; break;
                }

                empire.GetRelations(TargetEmpire).ActiveWar.ChangeWarType(changeTo);
            }

            return GoalStep.RestartGoal;
        }
    }
}