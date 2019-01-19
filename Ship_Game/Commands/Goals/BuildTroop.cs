using System;
using System.Linq;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class BuildTroop : Goal
    {
        public const string ID = "Build Troop";
        public override string UID => ID;

        public BuildTroop() : base(GoalType.BuildTroop)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion
            };
        }

        public BuildTroop(Troop toCopy, Empire owner) : this()
        {
            ToBuildUID = toCopy.Name;
            empire = owner;
            if (ToBuildUID.IsEmpty())
                Log.Error($"Missing Troop {ToBuildUID}");
        }

        GoalStep FindPlanetToBuildAt()
        {
            // find a planet
            Planet planet = empire.GetPlanets()
                .Filter(p => p.AllowInfantry && p.colonyType != Planet.ColonyType.Research
                     && p.Prod.NetMaxPotential > 5f
                     &&(p.ProdHere - 2*p.TotalCostOfTroopsInQueue()) > 0)
                .OrderBy(p => !p.HasSpacePort)
                .ThenByDescending(p => p.Prod.GrossIncome)
                .FirstOrDefault();

            if (planet == null)
                return GoalStep.GoalFailed;

            // submit troop into queue
            Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
            planet.Construction.AddTroop(troopTemplate, this);
            return GoalStep.GoToNextStep;
        }
    }
}
