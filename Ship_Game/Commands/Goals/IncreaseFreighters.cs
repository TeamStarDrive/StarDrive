﻿using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class IncreaseFreighters : BuildShipsGoalBase
    {
        public const string ID = "IncreaseFreighters";
        public override string UID => ID;

        public IncreaseFreighters() : base(GoalType.IncreaseFreighters)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,
                ReportGoalCompleteToEmpire
            };
        }

        public IncreaseFreighters(Empire empire) : this()
        {
            this.empire = empire;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!GetFreighter(out Ship freighter))
                return GoalStep.GoalFailed;

            if (!FindPlanetToBuildShipAt(SpacePortType.Safe, freighter, out Planet planet))
                return GoalStep.GoalFailed;

            planet.Construction.Enqueue(freighter, this, notifyOnEmpty: false);
            if (empire.IsIndustrialists || RandomMath.RollDie(empire.MaxFreightersInQueue) == 1)
                planet.Construction.PrioritizeShip(freighter);

            return GoalStep.GoToNextStep;
        }

        GoalStep ReportGoalCompleteToEmpire()
        {
            empire.ReportGoalComplete(this);
            return GoalStep.GoalComplete;
        }
    }
}
