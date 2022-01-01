using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires.Components;

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
            StarDateAdded = empire.Universum.StarDate;
        }


        GoalStep AddDefendSystemTasks()
        {
            var systems = new Array<IncomingThreat>();
            foreach (IncomingThreat threatenedSystem in empire.SystemsWithThreat)
            {
                if (!threatenedSystem.ThreatTimedOut && threatenedSystem.HighPriority)
                    systems.Add(threatenedSystem);
            }

            systems.Sort(ts => ts.TargetSystem.WarValueTo(empire));
            for (int i = 0; i < systems.Count; i++)
            {
                var threatenedSystem = systems[i];
                if (!threatenedSystem.TargetSystem.HasPlanetsOwnedBy(empire)
                    || empire.HasWarTaskTargetingSystem(threatenedSystem.TargetSystem))
                {
                    continue;
                }

                float minStr = threatenedSystem.Strength.Greater(500) ? threatenedSystem.Strength : 1000;
                if (threatenedSystem.Enemies.Length > 0)
                    minStr *= empire.GetFleetStrEmpireMultiplier(threatenedSystem.Enemies[0]).UpperBound(empire.OffensiveStrength / 5);

                empire.AddDefenseSystemGoal(threatenedSystem.TargetSystem, minStr, 1);
            }

            return GoalStep.GoToNextStep;
        }

        GoalStep AssessDefense()
        {
            var defendSystemTasks = empire.GetEmpireAI().GetDefendSystemTasks();
            foreach (MilitaryTask defendSystem in defendSystemTasks)
            {
                if (defendSystem.Fleet != null || defendSystem.Fleet == null && defendSystem.Goal?.LifeTime > 5)
                    continue; // We have a fleet for this task or too old to prioritize

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

        bool DefenseTaskHasHigherPriority(MilitaryTask defenseTask, MilitaryTask possibleTask)
        {
            SolarSystem system = defenseTask.TargetSystem ?? defenseTask.TargetPlanet.ParentSystem;
            if (system.PlanetList.Any(p => p.Owner == empire && p.HasCapital)
                && !possibleTask.TargetSystem?.PlanetList.Any(p => p.Owner == empire && p.HasCapital) == true)
            {
                return true; // Defend our home systems at all costs (unless the other task also has a home system)!
            }

            if (possibleTask.Type == MilitaryTask.TaskType.ClearAreaOfEnemies)
                return false;

            Planet target            = possibleTask.TargetPlanet;
            SolarSystem targetSystem = target?.ParentSystem ?? possibleTask.TargetSystem;

            if (system == targetSystem)
                return false; // The checked task has the same target system, no need to cancel it

            if (possibleTask.Type == MilitaryTask.TaskType.DefendPostInvasion
                && !empire.SystemsWithThreat.Any(t => !t.ThreatTimedOut && t.TargetSystem == targetSystem)
                && !targetSystem?.DangerousForcesPresent(empire) == true)
            {
                return true; // Cancel idle post invasion fleets if we need to defend
            }

            float defenseValue  = system.PotentialValueFor(empire) * empire.PersonalityModifiers.DefenseTaskWeight;
            float possibleValue = targetSystem?.PotentialValueFor(empire) ?? 0;

            if (possibleTask.Fleet != null) // compare fleet distances
            {
                float defenseDist = possibleTask.Fleet.AveragePosition().Distance(system.Position) / 10000;
                float expansionDist = possibleTask.Fleet.AveragePosition().Distance(target.Center) / 10000;
                defenseValue /= defenseDist.LowerBound(1);
                possibleValue /= expansionDist.LowerBound(1);
            }
            else // compare planet distances
            {
                Vector2 possiblePos = target?.Center ?? targetSystem?.Position ?? possibleTask.TargetShip.Position;
                Vector2 defensePos  = system.Position;
                defenseValue /= empire.WeightedCenter.Distance(defensePos).LowerBound(1);
                possibleValue /= empire.WeightedCenter.Distance(possiblePos).LowerBound(1);
            }

            return defenseValue > possibleValue;
        }
    }
}
