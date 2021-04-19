using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires.DataPackets;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class EmpireDefense : Goal
    {
        public const string ID = "Empire Defense";
        public override string UID => ID;

        public EmpireDefense() : base(GoalType.EmpireDefense)
        {
            Steps = new Func<GoalStep>[]
            {
                AddDefendSystemTasks,
                AssessDefense
            };
        }

        public EmpireDefense(Empire empire) : this()
        {
            this.empire   = empire;
            StarDateAdded = Empire.Universe.StarDate;
        }


        GoalStep AddDefendSystemTasks()
        {
            var systems = new Array<IncomingThreat>();
            foreach (IncomingThreat threatenedSystem in empire.SystemWithThreat)
            {
                if (!threatenedSystem.ThreatTimedOut && threatenedSystem.HighPriority)
                    systems.Add(threatenedSystem);
            }

            systems.Sort(ts => ts.TargetSystem.WarValueTo(empire));
            for (int i = 0; i < systems.Count; i++)
            {
                var threatenedSystem = systems[i];
                if (!threatenedSystem.TargetSystem.HasPlanetsOwnedBy(empire)
                    || empire.IsAlreadyDefendingSystem(threatenedSystem.TargetSystem))
                {
                    continue;
                }

                var priority = 6 - threatenedSystem.TargetSystem.PlanetList.Filter(p => p.Owner == empire).Sum(p => p.Level).UpperBound(5);
                float minStr = threatenedSystem.Strength.Greater(500) ? threatenedSystem.Strength : 1000;

                if (threatenedSystem.Enemies.Length > 0)
                    minStr *= empire.GetFleetStrEmpireMultiplier(threatenedSystem.Enemies[0]).UpperBound(empire.OffensiveStrength / 5);

                empire.AddDefenseSystemGoal(threatenedSystem.TargetSystem, priority, minStr, 1);
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep AssessDefense()
        {
            var defendSystemTasks = empire.GetEmpireAI().GetDefendSystemTasks();
            foreach (MilitaryTask defendSystem in defendSystemTasks)
            {
                if (defendSystem.Fleet != null)
                    continue; // We have a fleet for this task

                foreach (MilitaryTask possibleTask in empire.GetEmpireAI().GetPotentialTasksToCompare())
                {
                    if (possibleTask != defendSystem)
                    {
                        if (DefenseTaskHasHigherPriority(defendSystem, possibleTask))
                            possibleTask.EndTask();
                    }
                }
            }
            return GoalStep.RestartGoal;
        }

        // todo - by the task priority
        bool DefenseTaskHasHigherPriority(MilitaryTask defenseTask, MilitaryTask possibleTask)
        {
            if (possibleTask == defenseTask)
                return false; // Since we also check other defense tasks, we dont want to compare same task

            SolarSystem system = defenseTask.TargetSystem ?? defenseTask.TargetPlanet.ParentSystem;
            if (system.PlanetList.Any(p => p.Owner == empire && p.HasCapital)
                && !possibleTask.TargetSystem?.PlanetList.Any(p => p.Owner == empire && p.HasCapital) == true)
            {
                return true; // Defend our home systems at all costs (unless the other task also has a home system)!
            }

            Planet target = possibleTask.TargetPlanet;
            float defenseValue = system.PotentialValueFor(empire) * empire.PersonalityModifiers.DefenseTaskWeight;
            float possibleValue = target.ParentSystem.PotentialValueFor(empire);

            if (possibleTask.Fleet != null) // compare fleet distances
            {
                float defenseDist = possibleTask.Fleet.AveragePosition().Distance(system.Position) / 10000;
                float expansionDist = possibleTask.Fleet.AveragePosition().Distance(target.Center) / 10000;
                defenseValue /= defenseDist.LowerBound(1);
                possibleValue /= expansionDist.LowerBound(1);
            }
            else // compare planet distances
            {
                defenseValue /= empire.WeightedCenter.Distance(target.Center).LowerBound(1);
                possibleValue /= empire.WeightedCenter.Distance(target.Center).LowerBound(1);
            }

            return defenseValue.GreaterOrEqual(possibleValue);
        }
    }
}
